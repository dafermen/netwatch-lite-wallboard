using System.Text.Json;

namespace NetWatchLite.Wallboard.WebView2;

/// <summary>
/// Reads, normalizes, saves, backs up, and verifies <c>wallboard.json</c> for the desktop
/// wallboard executable. Keeping this logic in one class gives the rest of the app a simple
/// contract: every configuration returned from here is already safe to render.
/// </summary>
internal static class WallboardConfigReader
{
    private const string WallboardFileName = "wallboard.json";
    private static readonly int[] SupportedLayouts = [1, 2, 3, 4, 6, 8];
    private static readonly string[] SupportedAlarmSounds =
    [
        "Exclamation",
        "Asterisk",
        "Beep",
        "Hand",
        "Question"
    ];

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
        WriteIndented = true
    };

    /// <summary>
    /// Loads <c>wallboard.json</c> from the executable folder or development source folder.
    /// Invalid or missing JSON never prevents the application from starting; the caller receives
    /// a small built-in default configuration instead.
    /// </summary>
    /// <param name="cancellationToken">Token used to cancel JSON file IO.</param>
    /// <returns>A normalized wallboard configuration. Invalid files fall back to defaults.</returns>
    public static async Task<WallboardConfiguration> LoadAsync(CancellationToken cancellationToken = default)
    {
        var filePath = ResolveWallboardFilePath();

        if (!File.Exists(filePath))
        {
            return CreateDefaultConfiguration();
        }

        try
        {
            await using var stream = File.OpenRead(filePath);
            var configuration = await JsonSerializer.DeserializeAsync<WallboardConfiguration>(
                stream,
                JsonOptions,
                cancellationToken);

            // Normalize immediately after deserialization so UI and rendering code do not need to
            // defend against unsupported layouts, empty names, invalid URLs, or bad monitoring rules.
            return Normalize(configuration);
        }
        catch (Exception ex)
        {
            AppErrorLog.Log($"loading configuration from '{filePath}'", ex);
            return CreateDefaultConfiguration();
        }
    }

    /// <summary>
    /// Saves a normalized <c>wallboard.json</c> to the active runtime or development path.
    /// The write is intentionally conservative: normalize, back up the previous file, write through
    /// a temporary file, replace the active file, then read the result back for verification.
    /// </summary>
    /// <param name="configuration">Configuration to persist.</param>
    /// <param name="cancellationToken">Token used to cancel JSON file IO.</param>
    /// <returns>Absolute path to the saved JSON file.</returns>
    public static async Task<string> SaveAsync(
        WallboardConfiguration configuration,
        CancellationToken cancellationToken = default)
    {
        var filePath = ResolveWallboardFilePath();
        var normalized = Normalize(configuration);
        var directory = Path.GetDirectoryName(filePath);

        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        if (File.Exists(filePath))
        {
            // Keep one easy-to-find backup beside the active JSON. This is mainly for operators who
            // edit settings live and need a quick rollback if a saved panel list is not what they meant.
            var backupPath = Path.Combine(
                Path.GetDirectoryName(filePath) ?? string.Empty,
                $"{Path.GetFileNameWithoutExtension(filePath)}.backup.json");
            File.Copy(filePath, backupPath, overwrite: true);
        }

        var temporaryPath = $"{filePath}.tmp";
        var expectedJson = JsonSerializer.Serialize(normalized, JsonOptions);

        // Write to a sibling temp file first so a failed write is less likely to leave a truncated
        // wallboard.json in place.
        await File.WriteAllTextAsync(temporaryPath, expectedJson, cancellationToken);
        File.Move(temporaryPath, filePath, overwrite: true);

        var savedJson = await File.ReadAllTextAsync(filePath, cancellationToken);

        if (!string.Equals(savedJson, expectedJson, StringComparison.Ordinal))
        {
            throw new IOException("The saved JSON could not be verified after writing.");
        }

        return filePath;
    }

    /// <summary>
    /// Returns the active JSON path used by the app.
    /// </summary>
    /// <returns>Absolute path to <c>wallboard.json</c>.</returns>
    public static string GetConfigurationFilePath()
    {
        return ResolveWallboardFilePath();
    }

    /// <summary>
    /// Resolves the runtime configuration path, preferring the repository's Data/wallboard.json
    /// when running from a local build output. Published builds use the JSON beside the executable.
    /// </summary>
    /// <returns>Absolute path to the wallboard JSON file that should be read.</returns>
    private static string ResolveWallboardFilePath()
    {
        var runtimePath = Path.Combine(AppContext.BaseDirectory, WallboardFileName);
        var developmentPath = ResolveDevelopmentWallboardFilePath();

        if (IsDevelopmentOutputDirectory(AppContext.BaseDirectory) &&
            File.Exists(developmentPath))
        {
            return developmentPath;
        }

        if (File.Exists(runtimePath))
        {
            return runtimePath;
        }

        return File.Exists(developmentPath)
            ? developmentPath
            : runtimePath;
    }

    /// <summary>
    /// Builds the repository development JSON path from a normal bin/Debug or bin/Release output.
    /// </summary>
    /// <returns>Expected Data/wallboard.json path for local development.</returns>
    private static string ResolveDevelopmentWallboardFilePath()
    {
        return Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "..",
            "..",
            "..",
            "..",
            "..",
            "Data",
            WallboardFileName));
    }

    /// <summary>
    /// Detects SDK build output folders so local settings are saved back to the repository JSON.
    /// </summary>
    /// <param name="baseDirectory">Application base directory.</param>
    /// <returns>True when the app appears to be running from bin/Debug or bin/Release.</returns>
    private static bool IsDevelopmentOutputDirectory(string baseDirectory)
    {
        var normalized = Path.GetFullPath(baseDirectory)
            .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        var segments = normalized.Split(
            [Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar],
            StringSplitOptions.RemoveEmptyEntries);

        for (var index = 0; index < segments.Length - 1; index++)
        {
            if (string.Equals(segments[index], "bin", StringComparison.OrdinalIgnoreCase) &&
                (string.Equals(segments[index + 1], "Debug", StringComparison.OrdinalIgnoreCase) ||
                 string.Equals(segments[index + 1], "Release", StringComparison.OrdinalIgnoreCase)))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Converts parsed JSON into safe runtime values and removes invalid panels.
    /// This method is the central schema boundary between user-editable JSON and runtime objects.
    /// </summary>
    /// <param name="configuration">Configuration parsed from JSON.</param>
    /// <returns>Normalized configuration with supported layout and timing values.</returns>
    private static WallboardConfiguration Normalize(WallboardConfiguration? configuration)
    {
        if (configuration is null)
        {
            return CreateDefaultConfiguration();
        }

        var panels = (configuration.Panels ?? [])
            .Where(IsValidPanel)
            .Select(panel => new WallboardPanel
            {
                Name = string.IsNullOrWhiteSpace(panel.Name) ? "Monitoring Panel" : panel.Name.Trim(),
                Url = panel.Url.Trim(),
                RefreshSeconds = panel.RefreshSeconds <= 0 ? 30 : panel.RefreshSeconds,
                Monitoring = NormalizeMonitoring(panel.Monitoring)
            })
            .ToList();

        return new WallboardConfiguration
        {
            AppTitle = string.IsNullOrWhiteSpace(configuration.AppTitle)
                ? "NetWatch Lite Wallboard"
                : configuration.AppTitle.Trim(),
            RotationEnabled = configuration.RotationEnabled,
            RotationSeconds = configuration.RotationSeconds <= 0 ? 20 : configuration.RotationSeconds,
            DefaultLayout = NormalizeLayout(configuration.DefaultLayout),
            AlarmSound = NormalizeAlarmSound(configuration.AlarmSound),
            SeverityColors = NormalizeSeverityColors(configuration.SeverityColors),
            Panels = panels.Count == 0 ? CreateDefaultConfiguration().Panels : panels
        };
    }

    /// <summary>
    /// Determines whether a panel has a usable absolute or local URL.
    /// WebView2 can navigate HTTP/HTTPS and file URLs directly. Root-relative URLs are treated as
    /// local static pages under wwwroot, while relative URLs are treated as files shipped beside the
    /// executable. Local URLs are resolved later by WebViewPanelControl.
    /// </summary>
    /// <param name="panel">Panel declaration parsed from JSON.</param>
    /// <returns>True when the panel can be navigated by WebView2.</returns>
    private static bool IsValidPanel(WallboardPanel panel)
    {
        if (string.IsNullOrWhiteSpace(panel.Url))
        {
            return false;
        }

        var url = panel.Url.Trim();

        if (url.StartsWith('/') || IsRelativeLocalUrl(url))
        {
            return true;
        }

        return Uri.TryCreate(url, UriKind.Absolute, out var uri)
            && (uri.Scheme == Uri.UriSchemeHttp ||
                uri.Scheme == Uri.UriSchemeHttps ||
                uri.Scheme == Uri.UriSchemeFile);
    }

    /// <summary>
    /// Allows packaged local pages such as docs/scraping-test-page.html without tying the JSON to
    /// one machine-specific absolute path.
    /// </summary>
    /// <param name="url">Panel URL text.</param>
    /// <returns>True when the value is a safe relative local file path.</returns>
    private static bool IsRelativeLocalUrl(string url)
    {
        return Uri.TryCreate(url, UriKind.Relative, out _)
            && !Path.IsPathRooted(url)
            && !url.Contains(':', StringComparison.Ordinal)
            && !url.StartsWith('\\')
            && !url.Contains("..", StringComparison.Ordinal);
    }

    /// <summary>
    /// Creates a single-panel fallback configuration used when JSON is missing or invalid.
    /// </summary>
    /// <returns>Safe default wallboard configuration.</returns>
    private static WallboardConfiguration CreateDefaultConfiguration()
    {
        return new WallboardConfiguration
        {
            AppTitle = "NetWatch Lite Wallboard",
            RotationEnabled = false,
            RotationSeconds = 20,
            DefaultLayout = 4,
            AlarmSound = "Exclamation",
            SeverityColors = new AlarmSeverityColors(),
            Panels =
            [
                new WallboardPanel
                {
                    Name = "Operations Overview",
                    Url = "https://example.com/",
                    RefreshSeconds = 30,
                    Monitoring = null
                }
            ]
        };
    }

    /// <summary>
    /// Converts optional panel monitoring settings into safe runtime values.
    /// Disabled monitoring and enabled monitoring with no usable rules both become null. That keeps
    /// WebViewPanelControl's runtime decision simple: null means no alarm polling timer.
    /// </summary>
    /// <param name="monitoring">Monitoring settings parsed from JSON.</param>
    /// <returns>Normalized settings, or null when monitoring should stay disabled.</returns>
    private static PanelMonitoringOptions? NormalizeMonitoring(PanelMonitoringOptions? monitoring)
    {
        if (monitoring is null || !monitoring.Enabled)
        {
            return null;
        }

        var rules = (monitoring.Rules ?? [])
            .Where(IsValidMonitoringRule)
            .Select(rule => new PanelMonitoringRule
            {
                Name = string.IsNullOrWhiteSpace(rule.Name) ? "DOM Alert" : rule.Name.Trim(),
                Type = NormalizeRuleType(rule.Type),
                Selector = rule.Selector.Trim(),
                Contains = string.IsNullOrWhiteSpace(rule.Contains) ? null : rule.Contains.Trim(),
                Severity = NormalizeSeverity(rule.Severity),
                DetailsSelector = string.IsNullOrWhiteSpace(rule.DetailsSelector)
                    ? null
                    : rule.DetailsSelector.Trim(),
                SoundEnabled = rule.SoundEnabled
            })
            .ToList();

        if (rules.Count == 0)
        {
            return null;
        }

        return new PanelMonitoringOptions
        {
            Enabled = true,
            PollSeconds = Math.Clamp(monitoring.PollSeconds, 1, 300),
            SoundEnabled = monitoring.SoundEnabled,
            RepeatSoundSeconds = Math.Clamp(monitoring.RepeatSoundSeconds, 1, 300),
            Rules = rules
        };
    }

    /// <summary>
    /// Determines whether a monitoring rule has enough information to run safely.
    /// The selector is required because it is the only way the JavaScript scanner can find candidate
    /// DOM elements. Unsupported types are normalized to "exists" before this check.
    /// </summary>
    /// <param name="rule">Rule parsed from JSON.</param>
    /// <returns>True when the rule can be evaluated in the browser DOM.</returns>
    private static bool IsValidMonitoringRule(PanelMonitoringRule rule)
    {
        return !string.IsNullOrWhiteSpace(rule.Selector)
            && NormalizeRuleType(rule.Type) is "exists" or "domText" or "domClass";
    }

    /// <summary>
    /// Converts a rule type into a supported detector name.
    /// </summary>
    /// <param name="type">Configured rule type.</param>
    /// <returns>Supported rule type.</returns>
    private static string NormalizeRuleType(string? type)
    {
        return type?.Trim().ToLowerInvariant() switch
        {
            "domtext" => "domText",
            "domclass" => "domClass",
            "exists" => "exists",
            _ => "exists"
        };
    }

    /// <summary>
    /// Converts alert severity into a supported visual level.
    /// </summary>
    /// <param name="severity">Configured severity.</param>
    /// <returns>critical, warning, or info.</returns>
    private static string NormalizeSeverity(string? severity)
    {
        return severity?.Trim().ToLowerInvariant() switch
        {
            "critical" => "critical",
            "info" => "info",
            _ => "warning"
        };
    }

    /// <summary>
    /// Converts a JSON layout value into one of the supported wallboard layouts.
    /// </summary>
    /// <param name="layout">Panel count requested by configuration.</param>
    /// <returns>The requested layout when supported; otherwise four panels.</returns>
    private static int NormalizeLayout(int layout)
    {
        return SupportedLayouts.Contains(layout) ? layout : 4;
    }

    /// <summary>
    /// Converts configured alarm sound text into one of the supported Windows SystemSounds names.
    /// </summary>
    /// <param name="alarmSound">Configured alarm sound.</param>
    /// <returns>Supported sound name.</returns>
    private static string NormalizeAlarmSound(string? alarmSound)
    {
        var normalized = alarmSound?.Trim();

        return SupportedAlarmSounds.FirstOrDefault(
            sound => string.Equals(sound, normalized, StringComparison.OrdinalIgnoreCase))
            ?? "Exclamation";
    }

    /// <summary>
    /// Normalizes the optional severity color block into safe #RRGGBB values.
    /// </summary>
    /// <param name="colors">Configured color block.</param>
    /// <returns>Normalized color block.</returns>
    private static AlarmSeverityColors NormalizeSeverityColors(AlarmSeverityColors? colors)
    {
        return new AlarmSeverityColors
        {
            Critical = NormalizeHexColor(colors?.Critical, "#CC1220"),
            Warning = NormalizeHexColor(colors?.Warning, "#CC6700"),
            Info = NormalizeHexColor(colors?.Info, "#005C8A")
        };
    }

    /// <summary>
    /// Converts a hex color string into normalized uppercase #RRGGBB text.
    /// </summary>
    /// <param name="value">Configured color.</param>
    /// <param name="fallback">Fallback color.</param>
    /// <returns>Normalized color.</returns>
    private static string NormalizeHexColor(string? value, string fallback)
    {
        var color = value?.Trim();

        if (string.IsNullOrWhiteSpace(color))
        {
            return fallback;
        }

        if (!color.StartsWith('#'))
        {
            color = $"#{color}";
        }

        if (color.Length != 7)
        {
            return fallback;
        }

        for (var index = 1; index < color.Length; index++)
        {
            if (!Uri.IsHexDigit(color[index]))
            {
                return fallback;
            }
        }

        return color.ToUpperInvariant();
    }
}
