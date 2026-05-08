using System.Text.Json;

namespace NetWatchLite.Wallboard.WebView2;

/// <summary>
/// Reads and normalizes <c>wallboard.json</c> for the desktop wallboard executable.
/// </summary>
internal static class WallboardConfigReader
{
    private const string WallboardFileName = "wallboard.json";
    private static readonly int[] SupportedLayouts = [1, 2, 3, 4, 6, 8];

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
        WriteIndented = true
    };

    /// <summary>
    /// Loads <c>wallboard.json</c> from the executable folder or development source folder.
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

            return Normalize(configuration);
        }
        catch
        {
            return CreateDefaultConfiguration();
        }
    }

    /// <summary>
    /// Saves a normalized <c>wallboard.json</c> to the active runtime or development path.
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
            var backupPath = Path.Combine(
                Path.GetDirectoryName(filePath) ?? string.Empty,
                $"{Path.GetFileNameWithoutExtension(filePath)}.backup.json");
            File.Copy(filePath, backupPath, overwrite: true);
        }

        var temporaryPath = $"{filePath}.tmp";
        var expectedJson = JsonSerializer.Serialize(normalized, JsonOptions);

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
    /// Resolves the runtime configuration path, with a development fallback for local debugging.
    /// </summary>
    /// <returns>Absolute path to the wallboard JSON file that should be read.</returns>
    private static string ResolveWallboardFilePath()
    {
        var runtimePath = Path.Combine(AppContext.BaseDirectory, WallboardFileName);

        if (File.Exists(runtimePath))
        {
            return runtimePath;
        }

        var developmentPath = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "..",
            "..",
            "..",
            "..",
            "..",
            "Data",
            WallboardFileName));

        return File.Exists(developmentPath)
            ? developmentPath
            : runtimePath;
    }

    /// <summary>
    /// Converts parsed JSON into safe runtime values and removes invalid panels.
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
            Panels = panels.Count == 0 ? CreateDefaultConfiguration().Panels : panels
        };
    }

    /// <summary>
    /// Determines whether a panel has a usable absolute or root-relative URL.
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

        if (url.StartsWith('/'))
        {
            return true;
        }

        return Uri.TryCreate(url, UriKind.Absolute, out var uri)
            && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
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
            RotationEnabled = true,
            RotationSeconds = 20,
            DefaultLayout = 4,
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
}
