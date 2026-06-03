using System.Text.Json.Serialization;

namespace NetWatchLite.Wallboard.WebView2;

/// <summary>
/// Declares one configured monitoring page rendered inside a native WebView2 panel.
/// A panel is intentionally small: it describes what to show, how often to refresh it,
/// and, optionally, how the already-rendered page DOM should be inspected for alarms.
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

    /// <summary>
    /// Optional DOM monitoring rules used to raise native wallboard alerts for this panel.
    /// When this value is null, the panel behaves as a plain browser panel with no DOM polling.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public PanelMonitoringOptions? Monitoring { get; set; }
}

/// <summary>
/// DOM monitoring options for one panel.
/// These values are loaded from the optional panel-level "monitoring" JSON object.
/// The runtime normalizes timing values and removes invalid rule sets before panels are rendered.
/// </summary>
internal sealed class PanelMonitoringOptions
{
    /// <summary>
    /// Enables DOM polling for this panel.
    /// Disabled monitoring is normalized to null so the WebView panel does not start its alarm timer.
    /// </summary>
    public bool Enabled { get; set; }

    /// <summary>
    /// Number of seconds between DOM checks.
    /// The configuration reader clamps this to a safe range so accidental very large or zero values
    /// do not break the WinForms timer configuration.
    /// </summary>
    public int PollSeconds { get; set; } = 3;

    /// <summary>
    /// Enables repeated audible alerts for matched rules.
    /// This is the panel-level master switch. A matching rule can still opt out with SoundEnabled = false.
    /// </summary>
    public bool SoundEnabled { get; set; } = true;

    /// <summary>
    /// Number of seconds between repeated alert sounds.
    /// This limits repeated notification sounds while a visual alarm remains active.
    /// </summary>
    public int RepeatSoundSeconds { get; set; } = 5;

    /// <summary>
    /// Rules evaluated against the hosted page DOM.
    /// Each rule is converted to JSON and passed into a WebView2 ExecuteScriptAsync call.
    /// </summary>
    public List<PanelMonitoringRule> Rules { get; set; } = [];
}

/// <summary>
/// One DOM rule that can raise a native wallboard alert.
/// Rules are declarative so operators can tune selectors and text matching without recompiling.
/// </summary>
internal sealed class PanelMonitoringRule
{
    /// <summary>
    /// Friendly rule name shown in the alarm banner.
    /// This also becomes the fallback detail text when no detail element has readable text.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Rule type: exists, domText, or domClass.
    /// "exists" raises an alarm when the selector finds a visible element.
    /// "domText" raises an alarm when the selected visible element contains the configured text.
    /// "domClass" currently follows the same text/visibility matcher and is kept as a named extension point.
    /// </summary>
    public string Type { get; set; } = "exists";

    /// <summary>
    /// CSS selector used to find candidate elements.
    /// The selector is passed directly to document.querySelectorAll inside the loaded WebView page.
    /// </summary>
    public string Selector { get; set; } = string.Empty;

    /// <summary>
    /// Optional text that must be present in the selected elements.
    /// Text matching is case-insensitive and uses normalized whitespace.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Contains { get; set; }

    /// <summary>
    /// Alert severity: critical, warning, or info.
    /// When multiple rules match, the JavaScript scan returns the highest ranked severity first.
    /// </summary>
    public string Severity { get; set; } = "warning";

    /// <summary>
    /// Optional selector used to collect details shown in the alarm banner.
    /// If omitted, text is collected from the elements that matched Selector.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? DetailsSelector { get; set; }

    /// <summary>
    /// Optional per-rule override for sound.
    /// Set this to false for noisy warning indicators that should be visible but quiet.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? SoundEnabled { get; set; }
}
