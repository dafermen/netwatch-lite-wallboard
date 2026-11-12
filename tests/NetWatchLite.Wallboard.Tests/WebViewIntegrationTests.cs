using System.Diagnostics;
using System.Globalization;
using Microsoft.Web.WebView2.Core;
using NetWatchLite.Wallboard.WebView2;
using Xunit;
using Xunit.Abstractions;
using BrowserControl = Microsoft.Web.WebView2.WinForms.WebView2;

namespace NetWatchLite.Wallboard.Tests;

public sealed class WebViewFactAttribute : FactAttribute
{
    public WebViewFactAttribute()
    {
        if (Environment.GetEnvironmentVariable("NETWATCH_WEBVIEW2_TESTS") != "1")
        {
            Skip = "Requires an interactive Windows session and NETWATCH_WEBVIEW2_TESTS=1.";
        }
    }
}

[CollectionDefinition("WebView2", DisableParallelization = true)]
public sealed class WebViewCollection;

[Collection("WebView2")]
public sealed class WebViewIntegrationTests(ITestOutputHelper output)
{
    [WebViewFact]
    [Trait("Category", "WebView2")]
    public Task RecoverySettingFitsAtTheMinimumWindowSize() => RunOnUiThreadAsync(async _ =>
    {
        using var settings = new SettingsForm(new WallboardConfiguration(), _ => { });
        settings.StartPosition = FormStartPosition.Manual;
        settings.Location = new Point(-2000, 0);
        settings.Size = settings.MinimumSize;
        settings.Show();
        await Task.Delay(100);
        static IEnumerable<Control> Descendants(Control parent) => parent.Controls.Cast<Control>()
            .SelectMany(child => Descendants(child).Prepend(child));
        var recovery = Assert.Single(Descendants(settings).OfType<CheckBox>(),
            checkBox => checkBox.Text == "Recover failed panels automatically");
        Assert.True(recovery.Checked);
        Assert.True(recovery.Visible);
        Assert.True(recovery.Parent!.ClientRectangle.Contains(recovery.Bounds));
        var protection = Assert.Single(Descendants(settings).OfType<CheckBox>(), checkBox => checkBox.Name == "MemoryProtection");
        Assert.False(protection.Checked);
        Assert.True(protection.Visible);
        Assert.True(protection.Parent!.ClientRectangle.Contains(protection.Bounds));
        var selector = Assert.Single(Descendants(settings).OfType<ComboBox>(), control => control.Name == "ProfileSelector");
        Assert.True(selector.Visible);
        Assert.True(selector.Parent!.ClientRectangle.Contains(selector.Bounds));
        var switchProfile = Assert.Single(Descendants(settings).OfType<Button>(), control => control.Name == "SwitchProfile");
        Assert.InRange(switchProfile.Height, 32, 40);
        var delete = Assert.Single(Descendants(settings).OfType<Button>(), button => button.Text == "Delete");
        var editor = Assert.IsType<TableLayoutPanel>(delete.Parent!.Parent);
        Assert.True(editor.AutoScroll);
        editor.ScrollControlIntoView(delete.Parent);
        await Task.Delay(50);
        var deleteBounds = delete.RectangleToScreen(delete.ClientRectangle);
        for (Control? ancestor = delete.Parent; ancestor is not null; ancestor = ancestor.Parent)
        {
            Assert.True(ancestor.RectangleToScreen(ancestor.ClientRectangle).Contains(deleteBounds),
                $"Delete must be reachable. Ancestor {ancestor.GetType().Name}: {ancestor.RectangleToScreen(ancestor.ClientRectangle)}; Delete: {deleteBounds}; Scroll: {editor.AutoScrollPosition}.");
        }
    }, TimeSpan.FromSeconds(15));

    [WebViewFact]
    public Task ProfileSwitchRequiresExplicitDiscardEvenForInvalidPanelEdits() => RunOnUiThreadAsync(async _ =>
    {
        var allow = false;
        var confirmations = 0;
        var original = new WallboardConfiguration { Panels = [new() { Name = "Original", Url = "https://example.com/" }] };
        using var settings = new SettingsForm(original, _ => { }, () => { confirmations++; return allow; });
        settings.Show();
        await Task.Delay(100);
        var combo = (ComboBox)settings.Controls.Find("ProfileSelector", true).Single();
        Assert.True(combo.Items.Count >= 2);
        var targetIndex = combo.SelectedIndex == 0 ? 1 : 0;
        combo.SelectedIndex = targetIndex;
        var url = (TextBox)typeof(SettingsForm).GetField("_panelUrlTextBox", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!.GetValue(settings)!;
        url.Text = "javascript:invalid";
        var button = (Button)settings.Controls.Find("SwitchProfile", true).Single();
        button.PerformClick();
        Assert.Null(settings.RequestedProfileName);
        Assert.False(settings.IsDisposed);
        Assert.Equal(1, confirmations);
        allow = true;
        button.PerformClick();
        Assert.NotNull(settings.RequestedProfileName);
        Assert.Equal(2, confirmations);
        Assert.Equal("https://example.com/", Assert.Single(original.Panels).Url);
    }, TimeSpan.FromSeconds(15));

    [WebViewFact]
    [Trait("Category", "WebView2")]
    public Task RendererCrashIsRecoveredAndRealBrowserMemoryIsMeasured() => RunOnUiThreadAsync(async form =>
    {
        var directory = CreateTestDirectory();
        var environment = await CoreWebView2Environment.CreateAsync(userDataFolder: Path.Combine(directory, "profile"));
        var policy = new PanelRecoveryPolicy();
        var panel = await CreatePanelAsync(form, environment, directory, policy);
        var browser = panel.Controls.OfType<BrowserControl>().Single();
        var monitor = new MemoryMonitor();
        var sample = monitor.Sample(Environment.ProcessId,
            environment.GetProcessInfos().Select(info => info.ProcessId), true, DateTimeOffset.UtcNow);
        Assert.True(sample.BrowserProcessCount > 0);
        Assert.True(sample.BrowserBytes > 0);
        output.WriteLine(monitor.Describe());

        var recovered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        panel.AutomaticRestartRequested += async (_, e) =>
        {
            try
            {
                Assert.True(panel.TryBeginRecovery(e.Generation));
                form.Controls.Remove(panel);
                panel.Dispose();
                var replacement = await CreatePanelAsync(form, environment, directory, policy);
                Assert.False(replacement.IsDisposed);
                Assert.Equal(1, policy.Attempts);
                recovered.TrySetResult();
            }
            catch (Exception ex)
            {
                recovered.TrySetException(ex);
            }
        };

        // This Chromium diagnostic URL crashes only the isolated test renderer.
        browser.CoreWebView2.Navigate("edge://crash");
        await recovered.Task.WaitAsync(TimeSpan.FromSeconds(30));
    }, TimeSpan.FromSeconds(60));

    [WebViewFact]
    [Trait("Category", "Soak")]
    public Task RepeatedPanelDisposalKeepsTheSessionResponsive()
    {
        var duration = GetSoakDuration();
        return RunOnUiThreadAsync(async form =>
        {
            var directory = CreateTestDirectory();
            var environment = await CoreWebView2Environment.CreateAsync(userDataFolder: Path.Combine(directory, "profile"));
            var monitor = new MemoryMonitor(historyCapacity: 1440);
            var reportPath = Environment.GetEnvironmentVariable("NETWATCH_SOAK_REPORT") ?? Path.Combine(directory, "soak.csv");
            using var report = new StreamWriter(reportPath);
            await report.WriteLineAsync("utc,host_bytes,webview2_bytes,total_bytes,browser_processes,partial");
            var stopwatch = Stopwatch.StartNew();
            var cycles = 0;
            while (stopwatch.Elapsed < duration)
            {
                using var panel = await CreatePanelAsync(form, environment, directory, new PanelRecoveryPolicy());
                var sample = monitor.Sample(Environment.ProcessId,
                    environment.GetProcessInfos().Select(info => info.ProcessId), true, DateTimeOffset.UtcNow);
                await report.WriteLineAsync(FormattableString.Invariant(
                    $"{sample.Timestamp:O},{sample.HostBytes},{sample.BrowserBytes},{sample.TotalBytes},{sample.BrowserProcessCount},{sample.IsPartial}"));
                await report.FlushAsync();
                Assert.InRange(sample.BrowserProcessCount, 1, 12);
                Assert.True(sample.TotalBytes < 1536L * 1024 * 1024, "The neutral single-panel fixture exceeded the 1536 MB test budget.");
                await Task.Delay(1000);
                form.Controls.Remove(panel);
                cycles++;
            }

            Assert.True(cycles > 0);
            output.WriteLine($"Completed {cycles} disposal/recreation cycles in {stopwatch.Elapsed}. CSV: {reportPath}");
            output.WriteLine(monitor.Describe());
        }, duration + TimeSpan.FromMinutes(1));
    }

    [WebViewFact]
    [Trait("Category", "WebView2")]
    public Task MainFormRecoversTheSharedBrowserDespiteOneEnvironmentFactoryFailure()
    {
        var directory = CreateTestDirectory();
        var profile = Path.Combine(directory, "main-profile");
        var factoryCalls = 0;
        var configuration = new WallboardConfiguration
        {
            DefaultLayout = 2, LowPowerModeEnabled = false,
            Panels = [
                new() { Name = "First", Url = new Uri(Path.Combine(directory, "page.html")).AbsoluteUri, RefreshSeconds = 3600 },
                new() { Name = "Second", Url = new Uri(Path.Combine(directory, "page.html")).AbsoluteUri, RefreshSeconds = 3600 }
            ]
        };

        return RunOnUiThreadAsync(async form =>
        {
            WebViewPanelControl[] Panels() => form.Controls.OfType<TableLayoutPanel>().Single()
                .Controls.OfType<WebViewPanelControl>().ToArray();
            static bool Loaded(WebViewPanelControl panel) => panel.Controls.Cast<Control>()
                .SelectMany(control => control.Controls.OfType<Label>()).Any(label => label.Text.StartsWith("Loaded "));
            await WaitUntilAsync(() => Panels().Length == 2 && Panels().All(Loaded));
            var oldPanels = Panels();
            var browser = oldPanels[0].Controls.OfType<BrowserControl>().Single();
            var processId = browser.CoreWebView2.BrowserProcessId;
            Assert.Equal(processId, oldPanels[1].Controls.OfType<BrowserControl>().Single().CoreWebView2.BrowserProcessId);
            Assert.Equal(profile, browser.CoreWebView2.Environment.UserDataFolder);

            using (var ownedBrowser = Process.GetProcessById(checked((int)processId)))
            {
                Assert.Equal("msedgewebview2", ownedBrowser.ProcessName, ignoreCase: true);
                // The unique profile and both controls above prove this PID belongs to this test.
                ownedBrowser.Kill();
            }

            await WaitUntilAsync(() => Panels().Length == 2 && Panels().All(Loaded) &&
                Panels().All(panel => !oldPanels.Contains(panel)), TimeSpan.FromSeconds(60));
            Assert.True(factoryCalls >= 2);
            Assert.All(oldPanels, panel => Assert.True(panel.IsDisposed));
            Assert.NotEqual(processId, Panels()[0].Controls.OfType<BrowserControl>().Single().CoreWebView2.BrowserProcessId);
        }, TimeSpan.FromSeconds(90), () => new WallboardForm(profile,
            _ => Task.FromResult(configuration), folder =>
            {
                factoryCalls++;
                return factoryCalls == 2
                    ? Task.FromException<CoreWebView2Environment>(new InvalidOperationException("Injected environment factory failure"))
                    : CoreWebView2Environment.CreateAsync(userDataFolder: folder);
            }));
    }

    private static async Task WaitUntilAsync(Func<bool> condition, TimeSpan? timeout = null)
    {
        var stopwatch = Stopwatch.StartNew();
        while (!condition())
        {
            if (stopwatch.Elapsed > (timeout ?? TimeSpan.FromSeconds(30)))
            {
                throw new TimeoutException("The test UI did not reach the expected state.");
            }

            await Task.Delay(50);
        }
    }

    private static TimeSpan GetSoakDuration() =>
        double.TryParse(Environment.GetEnvironmentVariable("NETWATCH_SOAK_MINUTES"), NumberStyles.Float,
            CultureInfo.InvariantCulture, out var minutes)
            ? TimeSpan.FromMinutes(Math.Clamp(minutes, 0.1, 1440))
            : TimeSpan.FromMinutes(1);

    private static string CreateTestDirectory()
    {
        var path = Path.Combine(Path.GetTempPath(), "NetWatchWebViewTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        File.WriteAllText(Path.Combine(path, "page.html"), "<!doctype html><html><body><h1>Neutral test panel</h1></body></html>");
        return path;
    }

    private static async Task<WebViewPanelControl> CreatePanelAsync(
        Form form, CoreWebView2Environment environment, string directory, PanelRecoveryPolicy policy)
    {
        var panel = new WebViewPanelControl(ThemePalette.FromName("dark"), policy, true);
        form.Controls.Add(panel);
        var browser = panel.Controls.OfType<BrowserControl>().Single();
        var navigation = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        browser.NavigationCompleted += (_, e) =>
        {
            if (e.IsSuccess)
            {
                navigation.TrySetResult();
            }
            else
            {
                navigation.TrySetException(new InvalidOperationException($"Fixture navigation failed: {e.WebErrorStatus}"));
            }
        };

        try
        {
            await panel.LoadPanelAsync(new WallboardPanel
            {
                Name = "Isolated test panel",
                Url = new Uri(Path.Combine(directory, "page.html")).AbsoluteUri,
                RefreshSeconds = 3600
            }, environment);
            await navigation.Task.WaitAsync(TimeSpan.FromSeconds(20));
            return panel;
        }
        catch
        {
            panel.Dispose();
            throw;
        }
    }

    private static async Task RunOnUiThreadAsync(Func<Form, Task> action, TimeSpan timeout, Func<Form>? createForm = null)
    {
        var completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var thread = new Thread(() =>
        {
            Exception? failure = null;
            try
            {
                using var form = createForm?.Invoke() ?? new Form();
                form.WindowState = FormWindowState.Normal;
                form.Size = new Size(900, 600);
                form.ShowInTaskbar = false;
                form.StartPosition = FormStartPosition.Manual;
                form.Location = new Point(-2000, 0);
                form.Shown += async (_, _) =>
                {
                    try { await action(form); }
                    catch (Exception ex) { failure = ex; }
                    finally { form.Close(); }
                };
                Application.Run(form);
            }
            catch (Exception ex)
            {
                failure = ex;
            }

            if (failure is null) { completion.TrySetResult(); }
            else { completion.TrySetException(failure); }
        }) { IsBackground = true };
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        await completion.Task.WaitAsync(timeout);
    }
}
