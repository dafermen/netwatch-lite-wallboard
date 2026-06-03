namespace NetWatchLite.Wallboard.WebView2;

/// <summary>
/// Root JSON configuration for the Windows WebView2 wallboard.
/// This class mirrors the top-level shape of wallboard.json. The configuration reader normalizes
/// user-provided values before the main form uses them.
/// </summary>
internal sealed class WallboardConfiguration
{
    /// <summary>
    /// Title displayed in the center of the wallboard top bar.
    /// </summary>
    public string AppTitle { get; set; } = "NetWatch Lite Wallboard";

    /// <summary>
    /// Enables automatic rotation between pages when panel count exceeds the active layout.
    /// Rotation has no effect when all configured panels fit on the current page.
    /// </summary>
    public bool RotationEnabled { get; set; } = true;

    /// <summary>
    /// Number of seconds between automatic page rotations.
    /// </summary>
    public int RotationSeconds { get; set; } = 20;

    /// <summary>
    /// Default number of panels shown at once. Supported values are 1, 2, 3, 4, 6, and 8.
    /// </summary>
    public int DefaultLayout { get; set; } = 4;

    /// <summary>
    /// List of monitoring panels rendered by the wallboard.
    /// Order matters: it controls grid placement and the sequence used during automatic rotation.
    /// </summary>
    public List<WallboardPanel> Panels { get; set; } = [];
}
