using System.Text.Json;

namespace NetWatchLite.Wallboard.WebView2;

/// <summary>
/// Reads and normalizes <c>wallboard.json</c> for the desktop wallboard executable.
/// </summary>
internal static class WallboardConfigReader
{
    private const string WallboardFileName = "wallboard.json";

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
            DefaultLayout = configuration.DefaultLayout == 2 ? 2 : 4,
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
                    Name = "Sample Panel",
                    Url = "/wallboard-sample.html?panel=Sample%20Panel",
                    RefreshSeconds = 30
                }
            ]
        };
    }
}
