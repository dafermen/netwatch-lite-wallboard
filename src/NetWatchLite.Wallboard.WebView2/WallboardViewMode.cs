namespace NetWatchLite.Wallboard.WebView2;

internal enum WallboardViewMode { Wallboard, Home }

internal static class WallboardViewOptions
{
    internal static bool CanShowHome(WallboardConfiguration configuration) =>
        configuration.HomePageEnabled && WallboardConfigReader.IsValidHomePageUrl(configuration.HomePageUrl);

    internal static WallboardViewMode StartupMode(WallboardConfiguration configuration) =>
        CanShowHome(configuration) && string.Equals(configuration.StartupView?.Trim(), "home", StringComparison.OrdinalIgnoreCase)
            ? WallboardViewMode.Home : WallboardViewMode.Wallboard;
}
