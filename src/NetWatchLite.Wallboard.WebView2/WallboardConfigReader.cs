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
    private const string EnvironmentVariableName = "NETWATCH_WALLBOARD_ENV";
    private static readonly int[] SupportedLayouts = [1, 2, 3, 4, 6, 8];
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
        WriteIndented = true
    };

    /// <summary>
    /// Optional runtime profile selected by --environment, --env, --config-env, or
    /// NETWATCH_WALLBOARD_ENV. Empty means the default wallboard.json is active.
    /// </summary>
    public static string ActiveEnvironmentName { get; } = ResolveActiveEnvironmentName();

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
            // defend against unsupported layouts, empty names, invalid URLs, or bad timing values.
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
        var filePath = ResolveWritableWallboardFilePath();
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
    /// Returns the active JSON filename, including the environment suffix when one is selected.
    /// </summary>
    /// <returns>wallboard.json or wallboard.{environment}.json.</returns>
    public static string GetConfigurationFileName()
    {
        return string.IsNullOrWhiteSpace(ActiveEnvironmentName)
            ? WallboardFileName
            : $"wallboard.{ActiveEnvironmentName}.json";
    }

    /// <summary>
    /// Resolves the runtime configuration path, preferring the repository's Data/wallboard.json
    /// when running from a local build output. Published builds use the JSON beside the executable.
    /// </summary>
    /// <returns>Absolute path to the wallboard JSON file that should be read.</returns>
    private static string ResolveWallboardFilePath()
    {
        var fileName = GetConfigurationFileName();
        var defaultRuntimePath = Path.Combine(AppContext.BaseDirectory, WallboardFileName);
        var defaultDevelopmentPath = ResolveDevelopmentWallboardFilePath(WallboardFileName);
        var runtimePath = Path.Combine(AppContext.BaseDirectory, fileName);
        var developmentPath = ResolveDevelopmentWallboardFilePath(fileName);

        if (IsDevelopmentOutputDirectory(AppContext.BaseDirectory) &&
            File.Exists(developmentPath))
        {
            return developmentPath;
        }

        if (File.Exists(runtimePath))
        {
            return runtimePath;
        }

        if (!string.IsNullOrWhiteSpace(ActiveEnvironmentName))
        {
            if (IsDevelopmentOutputDirectory(AppContext.BaseDirectory) &&
                File.Exists(defaultDevelopmentPath))
            {
                return defaultDevelopmentPath;
            }

            if (File.Exists(defaultRuntimePath))
            {
                return defaultRuntimePath;
            }
        }

        return File.Exists(developmentPath)
            ? developmentPath
            : runtimePath;
    }

    /// <summary>
    /// Resolves the path used for saving. Unlike load, an environment profile should be created at
    /// its own wallboard.{environment}.json path instead of falling back to wallboard.json.
    /// </summary>
    /// <returns>Writable path for the active configuration profile.</returns>
    private static string ResolveWritableWallboardFilePath()
    {
        var fileName = GetConfigurationFileName();

        if (IsDevelopmentOutputDirectory(AppContext.BaseDirectory))
        {
            return ResolveDevelopmentWallboardFilePath(fileName);
        }

        return Path.Combine(AppContext.BaseDirectory, fileName);
    }

    /// <summary>
    /// Builds the repository development JSON path from a normal bin/Debug or bin/Release output.
    /// </summary>
    /// <returns>Expected Data/wallboard.json path for local development.</returns>
    private static string ResolveDevelopmentWallboardFilePath(string fileName)
    {
        return Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "..",
            "..",
            "..",
            "..",
            "..",
            "Data",
            fileName));
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
                Name = string.IsNullOrWhiteSpace(panel.Name) ? "Wallboard Panel" : panel.Name.Trim(),
                Url = panel.Url.Trim(),
                RefreshSeconds = panel.RefreshSeconds <= 0 ? 30 : panel.RefreshSeconds
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
            LowPowerModeEnabled = configuration.LowPowerModeEnabled,
            AutoRestartEnabled = configuration.AutoRestartEnabled,
            AutoRestartHours = Math.Clamp(configuration.AutoRestartHours, 1, 24),
            StartFullscreen = configuration.StartFullscreen,
            ConfirmExit = configuration.ConfirmExit,
            MemoryMonitorEnabled = configuration.MemoryMonitorEnabled,
            MemoryWarningMegabytes = Math.Clamp(configuration.MemoryWarningMegabytes, 256, 65536),
            AutoHideTopBarEnabled = configuration.AutoHideTopBarEnabled,
            Theme = ThemePalette.NormalizeName(configuration.Theme),
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
    /// Allows packaged local pages without tying the JSON to
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
            LowPowerModeEnabled = true,
            AutoRestartEnabled = false,
            AutoRestartHours = 6,
            StartFullscreen = false,
            ConfirmExit = false,
            MemoryMonitorEnabled = false,
            MemoryWarningMegabytes = 1500,
            AutoHideTopBarEnabled = false,
            Theme = "dark",
            Panels =
            [
                new WallboardPanel
                {
                    Name = "Operations Overview",
                    Url = "https://example.com/",
                    RefreshSeconds = 30
                }
            ]
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
    /// Reads command-line or environment profile selection and converts it to a safe filename suffix.
    /// </summary>
    /// <returns>Normalized environment suffix, or empty for the default profile.</returns>
    private static string ResolveActiveEnvironmentName()
    {
        var args = Environment.GetCommandLineArgs();
        string? environmentName = null;

        for (var index = 1; index < args.Length; index++)
        {
            var arg = args[index];

            if (arg.StartsWith("--environment=", StringComparison.OrdinalIgnoreCase))
            {
                environmentName = arg["--environment=".Length..];
                break;
            }

            if (arg.StartsWith("--env=", StringComparison.OrdinalIgnoreCase))
            {
                environmentName = arg["--env=".Length..];
                break;
            }

            if (arg.StartsWith("--config-env=", StringComparison.OrdinalIgnoreCase))
            {
                environmentName = arg["--config-env=".Length..];
                break;
            }

            if ((string.Equals(arg, "--environment", StringComparison.OrdinalIgnoreCase) ||
                 string.Equals(arg, "--env", StringComparison.OrdinalIgnoreCase) ||
                 string.Equals(arg, "--config-env", StringComparison.OrdinalIgnoreCase)) &&
                index + 1 < args.Length)
            {
                environmentName = args[index + 1];
                break;
            }
        }

        environmentName ??= Environment.GetEnvironmentVariable(EnvironmentVariableName);
        return NormalizeEnvironmentName(environmentName);
    }

    /// <summary>
    /// Keeps environment names safe for wallboard.{environment}.json file lookup.
    /// </summary>
    /// <param name="environmentName">Raw command-line or environment value.</param>
    /// <returns>Lowercase alphanumeric, dash, and underscore suffix.</returns>
    private static string NormalizeEnvironmentName(string? environmentName)
    {
        var raw = environmentName?.Trim();

        if (string.IsNullOrWhiteSpace(raw))
        {
            return string.Empty;
        }

        var characters = raw
            .Where(character => char.IsLetterOrDigit(character) || character is '-' or '_')
            .Select(char.ToLowerInvariant)
            .ToArray();

        return characters.Length == 0
            ? string.Empty
            : new string(characters);
    }
}
