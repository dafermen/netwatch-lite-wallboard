namespace NetWatchLite.Wallboard.WebView2;

/// <summary>
/// Root JSON configuration for the Windows WebView2 wallboard.
/// </summary>
internal sealed class WallboardConfiguration
{
    /// <summary>
    /// Title displayed in the center of the wallboard top bar.
    /// </summary>
    public string AppTitle { get; set; } = "NetWatch Lite Wallboard";

    /// <summary>
    /// Enables automatic rotation between pages when panel count exceeds the active layout.
    /// </summary>
    public bool RotationEnabled { get; set; } = true;

    /// <summary>
    /// Number of seconds between automatic page rotations.
    /// </summary>
    public int RotationSeconds { get; set; } = 20;

    /// <summary>
    /// Default number of panels shown at once. Supported values are 2 and 4.
    /// </summary>
    public int DefaultLayout { get; set; } = 4;

    /// <summary>
    /// List of monitoring panels rendered by the wallboard.
    /// </summary>
    public List<WallboardPanel> Panels { get; set; } = [];
}
