using System.Reflection;
using Microsoft.Web.WebView2.Core;
using NetWatchLite.Wallboard.WebView2;
using Xunit;
using BrowserControl = Microsoft.Web.WebView2.WinForms.WebView2;

namespace NetWatchLite.Wallboard.Tests;

public sealed partial class WebViewIntegrationTests
{
    private static T PrivateValue<T>(object instance, string field) =>
        (T)instance.GetType().GetField(field, BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(instance)!;

    private static object? CallPrivate(object instance, string method, params object?[] arguments) =>
        instance.GetType().GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(instance, arguments);

    private static WebViewPanelControl[] WallboardPanels(WallboardForm form) => PrivateValue<List<WebViewPanelControl>>(form, "_activePanels").ToArray();
    private static WebViewPanelControl? HomePage(WallboardForm form) => PrivateValue<WebViewPanelControl?>(form, "_homePanel");
    private static bool PageLoaded(WebViewPanelControl panel) => !panel.IsDisposed && PrivateValue<Label>(panel, "_statusLabel").Text.StartsWith("Loaded ");
    private static System.Windows.Forms.Timer Timer(object owner, string name) => PrivateValue<System.Windows.Forms.Timer>(owner, name);

    private static WallboardConfiguration HomeConfiguration(string directory) => new()
    {
        HomePageEnabled = true, HomePageUrl = new Uri(Path.Combine(directory, "page.html")).AbsoluteUri,
        DefaultLayout = 2, RotationEnabled = true, RotationSeconds = 3600,
        AutoRestartEnabled = true, AutoRestartHours = 1, MemoryMonitorEnabled = true,
        Panels = Enumerable.Range(1, 4).Select(index => new WallboardPanel
        { Name = $"Panel {index}", Url = new Uri(Path.Combine(directory, "page.html")).AbsoluteUri, RefreshSeconds = 3600 }).ToList()
    };

    [WebViewFact]
    [Trait("Category", "Desktop")]
    public Task SettingsKeepsHomeFieldsOnItsCloneAndExposesThemAtMinimumSize() => RunOnUiThreadAsync(async _ =>
    {
        var original = HomeConfiguration(CreateTestDirectory());
        original.StartupView = "home";
        using var settings = new SettingsForm(original, _ => { });
        settings.Size = settings.MinimumSize;
        settings.Show();
        await Task.Delay(60);
        var enabled = (CheckBox)settings.Controls.Find("HomePageEnabled", true).Single();
        var url = (TextBox)settings.Controls.Find("HomePageUrl", true).Single();
        var startup = (ComboBox)settings.Controls.Find("StartupView", true).Single();
        Assert.True(enabled.Checked);
        Assert.Equal(original.HomePageUrl, url.Text);
        Assert.Equal("Inicio", startup.SelectedItem);
        foreach (var control in new Control[] { enabled, url, startup })
            Assert.True(control.Parent!.ClientRectangle.Contains(control.Bounds));
        url.Text = "https://example.net/new-home";
        startup.SelectedItem = "Wallboard";
        Assert.True((bool)CallPrivate(settings, "TryReadWallboardSettings")!);
        var clone = PrivateValue<WallboardConfiguration>(settings, "_configuration");
        Assert.Equal(url.Text, clone.HomePageUrl);
        Assert.Equal("wallboard", clone.StartupView);
        Assert.NotEqual(original.HomePageUrl, clone.HomePageUrl);
        Assert.Equal("home", original.StartupView);
        enabled.Checked = false;
        Assert.False(url.Enabled);
        Assert.False(startup.Enabled);
        settings.Close();
    }, TimeSpan.FromSeconds(20));

    [WebViewFact]
    [Trait("Category", "WebView2")]
    public Task HomeReleasesPanelsAndTimersAndRestoresPagingAndCookiesAcrossRepeatedSwitches()
    {
        var directory = CreateTestDirectory();
        var configuration = HomeConfiguration(directory);
        var profile = Path.Combine(directory, "mode-profile");
        return RunOnUiThreadAsync(async form =>
        {
            var wallboard = (WallboardForm)form;
            await WaitUntilAsync(() => WallboardPanels(wallboard).Length == 2 && WallboardPanels(wallboard).All(PageLoaded));
            var browser = WallboardPanels(wallboard)[0].Controls.OfType<BrowserControl>().Single();
            var cookie = browser.CoreWebView2.CookieManager.CreateCookie("netwatch-mode-test", "retained", "example.org", "/");
            cookie.Expires = DateTime.UtcNow.AddDays(1);
            browser.CoreWebView2.CookieManager.AddOrUpdateCookie(cookie);
            CallPrivate(wallboard, "NavigatePage", 1);
            await WaitUntilAsync(() => WallboardPanels(wallboard).Length == 2 && WallboardPanels(wallboard).All(PageLoaded) &&
                PrivateValue<Label>(WallboardPanels(wallboard)[0], "_statusLabel").Text.StartsWith("Loaded ") &&
                WallboardPanels(wallboard)[0].Controls.OfType<Panel>().SelectMany(panel => panel.Controls.OfType<Label>()).Any(label => label.Text == "Panel 3"));
            var protection = PrivateValue<MemoryProtectionPolicy>(wallboard, "_memoryProtection");
            var protectionOptions = new WallboardConfiguration { MemoryProtectionEnabled = true, MemoryWarningMegabytes = 256, MemoryProtectionSustainedMinutes = 1 };
            var now = DateTimeOffset.UtcNow;
            Assert.False(protection.Observe(new(now, 300L * 1024 * 1024, 0, 1, false), protectionOptions, true));
            Assert.True(protection.Observe(new(now.AddMinutes(1), 300L * 1024 * 1024, 0, 1, false), protectionOptions, true));
            for (var cycle = 0; cycle < 6; cycle++)
            {
                var panels = WallboardPanels(wallboard);
                if (cycle == 0)
                {
                    var renderLock = PrivateValue<SemaphoreSlim>(wallboard, "_renderLock");
                    await renderLock.WaitAsync();
                    var queuedRestart = (Task)CallPrivate(wallboard, "RestartPanelAsync", panels[0], null)!;
                    var modeChange = wallboard.SetViewModeAsync(WallboardViewMode.Home);
                    renderLock.Release();
                    await Task.WhenAll(queuedRestart, modeChange);
                }
                else await wallboard.SetViewModeAsync(WallboardViewMode.Home);
                await WaitUntilAsync(() => HomePage(wallboard) is { } home && PageLoaded(home));
                Assert.Empty(WallboardPanels(wallboard));
                Assert.All(panels, panel =>
                {
                    Assert.True(panel.IsDisposed);
                    Assert.False(Timer(panel, "_refreshTimer").Enabled);
                    Assert.False(Timer(panel, "_recoveryTimer").Enabled);
                });
                var home = HomePage(wallboard)!;
                Assert.Equal(ThemePalette.FromName(configuration.Theme).ActiveButtonBackColor,
                    PrivateValue<RadioButton>(wallboard, "_homeModeButton").FlatAppearance.CheckedBackColor);
                Assert.False(Timer(home, "_refreshTimer").Enabled);
                Assert.False(Timer(home, "_recoveryTimer").Enabled);
                Assert.False(Timer(wallboard, "_rotationTimer").Enabled);
                Assert.False(Timer(wallboard, "_autoRestartTimer").Enabled);
                Assert.True(Timer(wallboard, "_memoryTimer").Enabled);
                Assert.False(home.Controls.Find("PanelHeader", true).Single().Visible);
                Assert.Equal(1, protection.Attempts);
                Assert.Equal(1, PrivateValue<int>(wallboard, "_currentPage"));
                CallPrivate(wallboard, "RotatePage");
                CallPrivate(wallboard, "SetLayout", 8);
                await (Task)CallPrivate(wallboard, "RestartVisiblePanelsCoreAsync", RuntimeEventKind.ScheduledRestart)!;
                await (Task)CallPrivate(wallboard, "RecoverHighMemoryAsync")!;
                Assert.Same(home, HomePage(wallboard));
                Assert.Equal(2, PrivateValue<int>(wallboard, "_layout"));
                Assert.Empty(WallboardPanels(wallboard));
                var homeBrowser = home.Controls.OfType<BrowserControl>().Single();
                Assert.Equal(profile, homeBrowser.CoreWebView2.Environment.UserDataFolder);
                Assert.Contains(await homeBrowser.CoreWebView2.CookieManager.GetCookiesAsync("https://example.org/"), item => item.Name == "netwatch-mode-test" && item.Value == "retained");
                await wallboard.SetViewModeAsync(WallboardViewMode.Wallboard);
                await WaitUntilAsync(() => WallboardPanels(wallboard).Length == 2 && WallboardPanels(wallboard).All(PageLoaded));
                Assert.True(home.IsDisposed);
                Assert.Null(HomePage(wallboard));
                Assert.Equal(1, PrivateValue<int>(wallboard, "_currentPage"));
                Assert.True(Timer(wallboard, "_rotationTimer").Enabled);
                Assert.True(Timer(wallboard, "_autoRestartTimer").Enabled);
                Assert.Equal(1, protection.Attempts);
                var sample = PrivateValue<MemoryMonitor>(wallboard, "_memoryMonitor").Sample(Environment.ProcessId,
                    WallboardPanels(wallboard)[0].Controls.OfType<BrowserControl>().Single().CoreWebView2.Environment.GetProcessInfos().Select(info => info.ProcessId),
                    true, DateTimeOffset.UtcNow);
                Assert.InRange(sample.BrowserProcessCount, 1, 12);
                Assert.True(sample.TotalBytes < 1536L * 1024 * 1024);
            }
            output.WriteLine("Six Inicio/Wallboard round trips preserved page/layout/cookie, disposed inactive controls, stopped wallboard timers and stayed under the neutral 1536 MiB budget.");
        }, TimeSpan.FromSeconds(120), () => new WallboardForm(profile, _ => Task.FromResult(WallboardConfigReader.Normalize(configuration))));
    }

    [WebViewFact]
    [Trait("Category", "WebView2")]
    public Task InicioStartupRapidSwitchingMinimizeAndReloadNeverWakeWallboard()
    {
        var directory = CreateTestDirectory();
        var configuration = HomeConfiguration(directory);
        configuration.StartupView = "home";
        return RunOnUiThreadAsync(async form =>
        {
            var wallboard = (WallboardForm)form;
            await WaitUntilAsync(() => HomePage(wallboard) is { } home && PageLoaded(home));
            Assert.Equal(WallboardViewMode.Home, wallboard.CurrentView);
            Assert.Empty(WallboardPanels(wallboard));
            var oldHome = HomePage(wallboard)!;
            var first = wallboard.SetViewModeAsync(WallboardViewMode.Wallboard);
            var second = wallboard.SetViewModeAsync(WallboardViewMode.Home);
            var third = wallboard.SetViewModeAsync(WallboardViewMode.Wallboard);
            var last = wallboard.SetViewModeAsync(WallboardViewMode.Home);
            await Task.WhenAll(first, second, third, last);
            await WaitUntilAsync(() => HomePage(wallboard) is { } home && PageLoaded(home));
            Assert.True(oldHome.IsDisposed);
            Assert.Empty(WallboardPanels(wallboard));
            Assert.False(Timer(wallboard, "_rotationTimer").Enabled);
            var current = HomePage(wallboard)!;
            wallboard.WindowState = FormWindowState.Minimized;
            await WaitUntilAsync(() => HomePage(wallboard) is null);
            Assert.True(current.IsDisposed);
            wallboard.WindowState = FormWindowState.Normal;
            await WaitUntilAsync(() => HomePage(wallboard) is { } home && PageLoaded(home));
            Assert.Equal(WallboardViewMode.Home, wallboard.CurrentView);
            configuration.StartupView = "wallboard";
            await (Task)CallPrivate(wallboard, "ReloadConfigurationAsync", false)!;
            Assert.Equal(WallboardViewMode.Home, wallboard.CurrentView);
            Assert.Empty(WallboardPanels(wallboard));
            configuration.HomePageEnabled = false;
            await (Task)CallPrivate(wallboard, "ReloadConfigurationAsync", false)!;
            await WaitUntilAsync(() => WallboardPanels(wallboard).Length == 2 && WallboardPanels(wallboard).All(PageLoaded));
            Assert.Equal(WallboardViewMode.Wallboard, wallboard.CurrentView);
            Assert.Null(HomePage(wallboard));
            await wallboard.SetViewModeAsync(WallboardViewMode.Home);
            Assert.Equal(WallboardViewMode.Wallboard, wallboard.CurrentView);
            configuration.HomePageEnabled = true;
            configuration.StartupView = "home";
            await (Task)CallPrivate(wallboard, "ReloadConfigurationAsync", true)!;
            await WaitUntilAsync(() => HomePage(wallboard) is { } home && PageLoaded(home));
            Assert.Equal(WallboardViewMode.Home, wallboard.CurrentView);
            Assert.Empty(WallboardPanels(wallboard));
        }, TimeSpan.FromSeconds(90), () => new WallboardForm(Path.Combine(directory, "rapid-profile"), _ => Task.FromResult(WallboardConfigReader.Normalize(configuration))));
    }

    [WebViewFact]
    [Trait("Category", "WebView2")]
    public Task ClosingDuringAViewChangeCancelsAndDisposesBothViews()
    {
        var directory = CreateTestDirectory();
        var configuration = HomeConfiguration(directory);
        return RunOnUiThreadAsync(async form =>
        {
            var wallboard = (WallboardForm)form;
            await WaitUntilAsync(() => WallboardPanels(wallboard).Length == 2 && WallboardPanels(wallboard).All(PageLoaded));
            var oldPanels = WallboardPanels(wallboard);
            var pending = wallboard.SetViewModeAsync(WallboardViewMode.Home);
            var pendingHome = HomePage(wallboard);
            wallboard.Close();
            await pending.WaitAsync(TimeSpan.FromSeconds(10));
            Assert.All(oldPanels, panel => Assert.True(panel.IsDisposed));
            if (pendingHome is not null) Assert.True(pendingHome.IsDisposed);
            Assert.Null(HomePage(wallboard));
            Assert.Empty(WallboardPanels(wallboard));
        }, TimeSpan.FromSeconds(60), () => new WallboardForm(Path.Combine(directory, "close-profile"), _ => Task.FromResult(configuration)));
    }

    [WebViewFact]
    [Trait("Category", "WebView2")]
    public Task FailedHomePageStaysInInicioAndCanBeRetriedWithoutStartingPanels()
    {
        var directory = CreateTestDirectory();
        var configuration = HomeConfiguration(directory);
        var missing = Path.Combine(directory, "not-yet-created.html");
        configuration.HomePageUrl = new Uri(missing).AbsoluteUri;
        configuration.StartupView = "home";
        return RunOnUiThreadAsync(async form =>
        {
            var wallboard = (WallboardForm)form;
            await WaitUntilAsync(() => HomePage(wallboard)?.Controls.Find("HomeLoadError", true).Single().Visible == true);
            Assert.Equal(WallboardViewMode.Home, wallboard.CurrentView);
            Assert.Empty(WallboardPanels(wallboard));
            var failed = HomePage(wallboard)!;
            await File.WriteAllTextAsync(missing, "<!doctype html><html><body>Home retry fixture</body></html>");
            ((Button)failed.Controls.Find("RetryHomePage", true).Single()).PerformClick();
            await WaitUntilAsync(() => HomePage(wallboard) is { } home && home != failed && PageLoaded(home));
            Assert.True(failed.IsDisposed);
            Assert.False(HomePage(wallboard)!.Controls.Find("HomeLoadError", true).Single().Visible);
            Assert.Empty(WallboardPanels(wallboard));
            Assert.False(Timer(wallboard, "_autoRestartTimer").Enabled);
        }, TimeSpan.FromSeconds(60), () => new WallboardForm(Path.Combine(directory, "retry-profile"), _ => Task.FromResult(WallboardConfigReader.Normalize(configuration))));
    }
}
