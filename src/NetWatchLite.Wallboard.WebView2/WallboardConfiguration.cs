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
    /// Built-in Windows sound used for audible alarms. Supported values are Exclamation, Asterisk,
    /// Beep, Hand, and Question. Invalid values are normalized by WallboardConfigReader.
    /// </summary>
    public string AlarmSound { get; set; } = "Exclamation";

    /// <summary>
    /// Hex colors used for alarm banners and panel borders by severity.
    /// The runtime normalizes invalid values to safe defaults.
    /// </summary>
    public AlarmSeverityColors SeverityColors { get; set; } = new();

    /// <summary>
    /// List of monitoring panels rendered by the wallboard.
    /// Order matters: it controls grid placement and the sequence used during automatic rotation.
    /// </summary>
    public List<WallboardPanel> Panels { get; set; } = [];
}

/// <summary>
/// Operator-configurable alarm colors stored as HTML-style hex strings.
/// Keeping these as strings makes wallboard.json easy to read and edit by hand.
/// </summary>
internal sealed class AlarmSeverityColors
{
    /// <summary>
    /// Color used for critical alarms.
    /// </summary>
    public string Critical { get; set; } = "#CC1220";

    /// <summary>
    /// Color used for warning alarms.
    /// </summary>
    public string Warning { get; set; } = "#CC6700";

    /// <summary>
    /// Color used for informational alarms.
    /// </summary>
    public string Info { get; set; } = "#005C8A";
}
