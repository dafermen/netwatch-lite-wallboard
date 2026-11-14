using NetWatchLite.Wallboard.WebView2;
using Xunit;

namespace NetWatchLite.Wallboard.Tests;

public sealed partial class WebViewIntegrationTests
{
    [WebViewFact]
    [Trait("Category", "Desktop")]
    public Task SettingsCompletesEditedUrlsAndRejectsTyposBeforeSaving() => RunOnUiThreadAsync(async _ =>
    {
        var original = HomeConfiguration(CreateTestDirectory());
        var messages = new List<string>();
        using var settings = new SettingsForm(original, _ => { }, validationMessage: messages.Add);
        settings.Show();
        await Task.Delay(60);
        var homeUrl = (TextBox)settings.Controls.Find("HomePageUrl", true).Single();
        homeUrl.Text = "www.google.com";
        Assert.True((bool)CallPrivate(settings, "TryReadWallboardSettings")!);
        var clone = PrivateValue<WallboardConfiguration>(settings, "_configuration");
        Assert.Equal("https://www.google.com", homeUrl.Text);
        Assert.Equal(homeUrl.Text, clone.HomePageUrl);
        Assert.NotEqual(original.HomePageUrl, clone.HomePageUrl);
        var panelUrl = PrivateValue<TextBox>(settings, "_panelUrlTextBox");
        panelUrl.Text = "www.example.org/status";
        Assert.Equal("www.example.org/status", panelUrl.Text);
        object?[] arguments = [null, true];
        Assert.True((bool)CallPrivate(settings, "TryReadPanelEditor", arguments)!);
        Assert.Equal("https://www.example.org/status", ((WallboardPanel)arguments[0]!).Url);
        Assert.Equal("https://www.example.org/status", panelUrl.Text);
        homeUrl.Text = "www.google,com";
        await (Task)CallPrivate(settings, "SaveConfigurationAsync")!;
        Assert.Single(messages);
        Assert.Contains("typing errors", messages[0]);
        Assert.Equal("https://www.google.com", clone.HomePageUrl);
        Assert.True(homeUrl.Focused);
        panelUrl.Text = "https://www.google,com";
        Assert.False((bool)CallPrivate(settings, "TryReadPanelEditor", new object?[] { null, true })!);
        Assert.Equal(2, messages.Count);
        Assert.True(panelUrl.Focused);
        Assert.NotEqual("https://www.example.org/status", original.Panels[0].Url);
        settings.Close();
    }, TimeSpan.FromSeconds(25));

    [WebViewFact]
    [Trait("Category", "WebView2")]
    public Task TopBarPollingRevealsOverTheBrowserAndKeepsCommandsReachable()
    {
        var directory = CreateTestDirectory();
        var configuration = HomeConfiguration(directory);
        configuration.AutoHideTopBarEnabled = true;
        return RunOnUiThreadAsync(async form =>
        {
            var wallboard = (WallboardForm)form;
            await WaitUntilAsync(() => WallboardPanels(wallboard).Length == 2 && WallboardPanels(wallboard).All(PageLoaded));
            var timer = Timer(wallboard, "_topBarAutoHideTimer");
            Assert.Equal(200, timer.Interval);
            Assert.True(timer.Enabled);
            timer.Stop();
            var bar = PrivateValue<Panel>(wallboard, "_topBar");
            var top = wallboard.PointToScreen(new Point(100, 1));
            var content = wallboard.PointToScreen(new Point(100, 300));
            wallboard.UpdateTopBarVisibility(top, 1000, true);
            wallboard.UpdateTopBarVisibility(content, 3999, true);
            Assert.True(bar.Visible);
            wallboard.UpdateTopBarVisibility(content, 4000, true);
            Assert.False(bar.Visible);
            wallboard.UpdateTopBarVisibility(wallboard.PointToScreen(new Point(-10, 1)), 5000, true);
            Assert.False(bar.Visible);
            wallboard.UpdateTopBarVisibility(top, 6000, false);
            Assert.False(bar.Visible);
            // No WinForms mouse event is raised: this models a hosted browser consuming it.
            wallboard.UpdateTopBarVisibility(top, 6100, true);
            Assert.True(bar.Visible);
            wallboard.UpdateTopBarVisibility(wallboard.PointToScreen(new Point(300, 40)), 15000, true);
            Assert.True(bar.Visible);
            using (var dialog = new Form())
            {
                dialog.Show(wallboard);
                wallboard.UpdateTopBarVisibility(content, 25000, true);
                Assert.True(bar.Visible);
                dialog.Close();
            }
            wallboard.UpdateTopBarVisibility(content, 28000, true);
            Assert.False(bar.Visible);
            await wallboard.SetViewModeAsync(WallboardViewMode.Home);
            await WaitUntilAsync(() => HomePage(wallboard) is { } home && PageLoaded(home));
            timer.Stop();
            wallboard.UpdateTopBarVisibility(top, 30000, true);
            wallboard.UpdateTopBarVisibility(content, 33000, true);
            Assert.False(bar.Visible);
            wallboard.UpdateTopBarVisibility(top, 33200, true);
            Assert.True(bar.Visible);
            foreach (var mode in new[] { WallboardViewMode.Home, WallboardViewMode.Wallboard })
            {
                await wallboard.SetViewModeAsync(mode);
                var minimum = wallboard.MinimumSize;
                wallboard.MinimumSize = Size.Empty;
                foreach (var width in new[] { 1280, 1024, 800 })
                {
                    wallboard.Size = new Size(width, 720);
                    wallboard.PerformLayout();
                    var commands = wallboard.Controls.Find("GlobalCommands", true).Single();
                    foreach (var button in commands.Controls.OfType<Button>().Where(button => button.Visible))
                    {
                        Assert.True(commands.ClientRectangle.Contains(button.Bounds), $"Clipped command: {button.Text}");
                        Assert.True(wallboard.RectangleToScreen(wallboard.ClientRectangle).Contains(button.RectangleToScreen(button.ClientRectangle)), $"Off-window command: {button.Text}, width {width}");
                        Assert.True(TextRenderer.MeasureText(button.Text, button.Font).Width <= button.ClientSize.Width - 6, button.Text);
                    }
                    if (mode == WallboardViewMode.Wallboard)
                    {
                        var layouts = PrivateValue<FlowLayoutPanel>(wallboard, "_wallboardControls");
                        Assert.True(layouts.Visible);
                        Assert.True(wallboard.RectangleToScreen(wallboard.ClientRectangle).Contains(layouts.RectangleToScreen(layouts.ClientRectangle)));
                    }
                }
                wallboard.MinimumSize = minimum;
            }
        }, TimeSpan.FromSeconds(60), () => new WallboardForm(Path.Combine(directory, "toolbar-profile"), _ => Task.FromResult(configuration)));
    }

    [WebViewFact]
    [Trait("Category", "WebView2")]
    public Task FullscreenUsesMonitorBoundsAndRestoresNormalAndMaximizedWindows()
    {
        var directory = CreateTestDirectory();
        var configuration = HomeConfiguration(directory);
        configuration.StartupView = "home";
        return RunOnUiThreadAsync(async form =>
        {
            var wallboard = (WallboardForm)form;
            await WaitUntilAsync(() => HomePage(wallboard) is { } home && PageLoaded(home));
            var initialHome = HomePage(wallboard);
            var toggle = (Button)wallboard.Controls.Find("ToggleFullscreen", true).Single();
            var minimum = wallboard.MinimumSize;
            foreach (var monitor in Screen.AllScreens)
            {
                wallboard.WindowState = FormWindowState.Normal;
                wallboard.Location = monitor.Bounds.Location;
                wallboard.Size = new Size(1280, 720);
                await Task.Delay(50);
                var expectedMonitor = Screen.FromHandle(wallboard.Handle);
                var before = wallboard.Bounds;
                var border = wallboard.FormBorderStyle;
                var topmost = wallboard.TopMost;
                toggle.PerformClick();
                await Task.Delay(50);
                Assert.Equal(expectedMonitor.Bounds, wallboard.Bounds);
                Assert.Equal(FormWindowState.Normal, wallboard.WindowState);
                Assert.Equal(FormBorderStyle.None, wallboard.FormBorderStyle);
                Assert.True(wallboard.TopMost);
                Assert.Equal("Exit fullscreen", toggle.Text);
                Assert.True((bool)CallPrivate(wallboard, "TryHandleShortcut", Keys.Escape, Keys.None)!);
                Assert.Equal(before, wallboard.Bounds);
                Assert.Equal(border, wallboard.FormBorderStyle);
                Assert.Equal(minimum, wallboard.MinimumSize);
                Assert.Equal(topmost, wallboard.TopMost);
                Assert.False(wallboard.IsDisposed);
                Assert.Same(initialHome, HomePage(wallboard));
            }
            var restore = wallboard.Bounds;
            wallboard.TopMost = true;
            wallboard.WindowState = FormWindowState.Maximized;
            await Task.Delay(50);
            Assert.True((bool)CallPrivate(wallboard, "TryHandleShortcut", Keys.F11, Keys.None)!);
            Assert.Equal(Screen.FromHandle(wallboard.Handle).Bounds, wallboard.Bounds);
            Assert.True((bool)CallPrivate(wallboard, "TryHandleShortcut", Keys.F11, Keys.None)!);
            Assert.Equal(FormWindowState.Maximized, wallboard.WindowState);
            Assert.True(wallboard.TopMost);
            wallboard.WindowState = FormWindowState.Normal;
            Assert.Equal(restore, wallboard.Bounds);
            wallboard.TopMost = false;
            Assert.False(configuration.StartFullscreen);
        }, TimeSpan.FromSeconds(45), () => new WallboardForm(Path.Combine(directory, "fullscreen-profile"), _ => Task.FromResult(configuration)));
    }

    [WebViewFact]
    [Trait("Category", "WebView2")]
    public Task FullscreenStartupHasAReachableCloseCommandWithoutChangingOptions()
    {
        var directory = CreateTestDirectory();
        var configuration = HomeConfiguration(directory);
        configuration.StartupView = "home";
        configuration.StartFullscreen = true;
        configuration.AutoHideTopBarEnabled = true;
        return RunOnUiThreadAsync(async form =>
        {
            var wallboard = (WallboardForm)form;
            await WaitUntilAsync(() => PrivateValue<bool>(wallboard, "_isFullscreen") && HomePage(wallboard) is { } home && PageLoaded(home));
            Assert.Equal(Screen.FromHandle(wallboard.Handle).Bounds, wallboard.Bounds);
            var home = HomePage(wallboard)!;
            var commands = wallboard.Controls.Find("GlobalCommands", true).Single();
            var close = (Button)wallboard.Controls.Find("CloseWallboard", true).Single();
            Assert.True(close.Visible);
            Assert.True(commands.ClientRectangle.Contains(close.Bounds));
            close.PerformClick();
            Assert.True(wallboard.IsDisposed);
            Assert.True(home.IsDisposed);
            Assert.False(Timer(wallboard, "_topBarAutoHideTimer").Enabled);
            Assert.True(configuration.StartFullscreen);
            Assert.True(configuration.AutoHideTopBarEnabled);
        }, TimeSpan.FromSeconds(45), () => new WallboardForm(Path.Combine(directory, "startup-fullscreen-profile"), _ => Task.FromResult(configuration)));
    }

    [WebViewFact]
    [Trait("Category", "Desktop")]
    public Task HomeErrorActionsRemainNativeWhenNoBrowserCanBeInitialized() => RunOnUiThreadAsync(_ =>
    {
        using var home = new WebViewPanelControl(ThemePalette.FromName("dark"), new PanelRecoveryPolicy(), true, true);
        _.Controls.Add(home);
        var retry = 0;
        var edit = 0;
        var back = 0;
        home.RestartRequested += (_, _) => retry++;
        home.EditHomeUrlRequested += (_, _) => edit++;
        home.WallboardRequested += (_, _) => back++;
        home.ShowPanelError("Inicio", new InvalidOperationException("Isolated initialization failure"));
        Assert.True(home.HasHomeLoadError);
        foreach (var name in new[] { "RetryHomePage", "EditHomeUrl", "BackToWallboard" })
        {
            var button = (Button)home.Controls.Find(name, true).Single();
            Assert.True(button.Visible);
            Assert.True(button.Parent!.ClientRectangle.Contains(button.Bounds));
            button.PerformClick();
        }
        Assert.Equal((1, 1, 1), (retry, edit, back));
        var keys = new List<Keys>();
        home.ShortcutRequested += (_, request) => keys.Add(request.KeyCode);
        var fullscreenKey = new KeyEventArgs(Keys.F11);
        CallPrivate(home, "OnWebViewKeyDown", null, fullscreenKey);
        Assert.Equal(new[] { Keys.F11 }, keys);
        Assert.True(fullscreenKey.Handled);
        Assert.True(fullscreenKey.SuppressKeyPress);
        Assert.False(Timer(home, "_refreshTimer").Enabled);
        Assert.False(Timer(home, "_recoveryTimer").Enabled);
        return Task.CompletedTask;
    }, TimeSpan.FromSeconds(20));

    [WebViewFact]
    [Trait("Category", "WebView2")]
    public Task HomeErrorPinsTheToolbarAndOffersEditOrAnExplicitReturnToWallboard()
    {
        var directory = CreateTestDirectory();
        var configuration = HomeConfiguration(directory);
        configuration.StartupView = "home";
        configuration.AutoHideTopBarEnabled = true;
        configuration.HomePageUrl = new Uri(Path.Combine(directory, "missing-home.html")).AbsoluteUri;
        return RunOnUiThreadAsync(async form =>
        {
            var wallboard = (WallboardForm)form;
            await WaitUntilAsync(() => HomePage(wallboard)?.HasHomeLoadError == true);
            var home = HomePage(wallboard)!;
            var bar = PrivateValue<Panel>(wallboard, "_topBar");
            Timer(wallboard, "_topBarAutoHideTimer").Stop();
            bar.Visible = false;
            wallboard.UpdateTopBarVisibility(wallboard.PointToScreen(new Point(300, 400)), 100000, true);
            Assert.True(bar.Visible);
            Assert.Empty(WallboardPanels(wallboard));
            Exception? dialogFailure = null;
            var inspected = false;
            using var inspectDialog = new System.Windows.Forms.Timer { Interval = 50 };
            inspectDialog.Tick += (_, _) =>
            {
                var settings = Application.OpenForms.OfType<SettingsForm>().SingleOrDefault();
                if (settings is null) return;
                inspectDialog.Stop();
                try
                {
                    var url = (TextBox)settings.Controls.Find("HomePageUrl", true).Single();
                    Assert.Equal(configuration.HomePageUrl, url.Text);
                    Assert.True(url.Focused);
                    Assert.Equal(url.Text, url.SelectedText);
                    Assert.True(bar.Visible);
                    inspected = true;
                }
                catch (Exception error) { dialogFailure = error; }
                finally { settings.Close(); }
            };
            inspectDialog.Start();
            ((Button)home.Controls.Find("EditHomeUrl", true).Single()).PerformClick();
            if (dialogFailure is not null) throw dialogFailure;
            Assert.True(inspected);
            Assert.False(PrivateValue<bool>(wallboard, "_settingsOpen"));
            Assert.Same(home, HomePage(wallboard));
            Assert.Equal(WallboardViewMode.Home, wallboard.CurrentView);
            Assert.Equal("home", configuration.StartupView);
            ((Button)home.Controls.Find("BackToWallboard", true).Single()).PerformClick();
            await WaitUntilAsync(() => WallboardPanels(wallboard).Length == 2 && WallboardPanels(wallboard).All(PageLoaded));
            Assert.Equal(WallboardViewMode.Wallboard, wallboard.CurrentView);
            Assert.True(home.IsDisposed);
            Assert.Null(HomePage(wallboard));
            Assert.True(configuration.HomePageEnabled);
            Assert.Equal("home", configuration.StartupView);
            Assert.True(Timer(wallboard, "_rotationTimer").Enabled);
        }, TimeSpan.FromSeconds(60), () => new WallboardForm(Path.Combine(directory, "error-actions-profile"), _ => Task.FromResult(configuration)));
    }
}
