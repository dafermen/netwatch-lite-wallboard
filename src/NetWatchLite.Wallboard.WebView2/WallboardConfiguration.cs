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
    public bool RotationEnabled { get; set; } = false;

    /// <summary>
    /// Number of seconds between automatic page rotations.
    /// </summary>
    public int RotationSeconds { get; set; } = 20;

    /// <summary>
    /// Default number of panels shown at once. Supported values are 1, 2, 3, 4, 6, and 8.
    /// </summary>
    public int DefaultLayout { get; set; } = 4;

    /// <summary>
    /// When enabled, the wallboard releases visible WebView2 panels while the window is minimized and
    /// recreates them when the operator restores the window.
    /// </summary>
    public bool LowPowerModeEnabled { get; set; } = true;

    /// <summary>
    /// Enables scheduled recreation of the visible WebView2 panels for long-running sessions.
    /// </summary>
    public bool AutoRestartEnabled { get; set; } = false;

    /// <summary>
    /// Number of hours between scheduled visible-panel restarts.
    /// </summary>
    public int AutoRestartHours { get; set; } = 6;

    /// <summary>
    /// Starts the application in fullscreen mode for TV/kiosk scenarios.
    /// </summary>
    public bool StartFullscreen { get; set; } = false;

    /// <summary>
    /// Asks for confirmation before closing the application.
    /// </summary>
    public bool ConfirmExit { get; set; } = false;

    /// <summary>
    /// Shows a lightweight memory indicator and restart recommendation in the top bar.
    /// </summary>
    public bool MemoryMonitorEnabled { get; set; } = false;

    /// <summary>
    /// Working set threshold in megabytes used by the memory recommendation indicator.
    /// </summary>
    public int MemoryWarningMegabytes { get; set; } = 1500;

    /// <summary>
    /// Hides the top bar after the mouse is idle, leaving more room for TV wallboards.
    /// </summary>
    public bool AutoHideTopBarEnabled { get; set; } = false;

    /// <summary>
    /// Native shell theme. Supported values are dark, light, and high-contrast.
    /// </summary>
    public string Theme { get; set; } = "dark";

    /// <summary>
    /// List of panels rendered by the wallboard.
    /// Order matters: it controls grid placement and the sequence used during automatic rotation.
    /// </summary>
    public List<WallboardPanel> Panels { get; set; } = [];
}
