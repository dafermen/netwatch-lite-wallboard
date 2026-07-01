namespace NetWatchLite.Wallboard.WebView2;

/// <summary>
/// Declares one configured page rendered inside a native WebView2 panel.
/// A panel is intentionally small: it describes what to show and how often to refresh it.
/// </summary>
internal sealed class WallboardPanel
{
    /// <summary>
    /// Display name shown in the panel title bar.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Absolute HTTP/HTTPS URL or root-relative local sample URL to load.
    /// </summary>
    public string Url { get; set; } = string.Empty;

    /// <summary>
    /// Independent refresh interval for this panel, in seconds.
    /// </summary>
    public int RefreshSeconds { get; set; } = 30;
}
