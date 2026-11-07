using System.Runtime.InteropServices;
using System.Text.Json;

namespace NetWatchLite.Wallboard.WebView2;

/// <summary>Read-only deployment probe. It never creates a window, browser profile, or configuration backup.</summary>
internal static class PackageCheck
{
    internal static async Task<int> RunAsync(string[] args)
    {
        if (args.Length < 2 || !Path.IsPathFullyQualified(args[1]))
        {
            return 2;
        }

        try
        {
            var configuration = await WallboardConfigReader.LoadAsync();
            var report = new
            {
                RuntimeVersion = Environment.Version.ToString(),
                RuntimeDirectory = RuntimeEnvironment.GetRuntimeDirectory(),
                ContentDirectory = PortablePaths.ContentDirectory,
                ConfigurationPath = WallboardConfigReader.GetConfigurationFilePath(),
                WritableConfigurationPath = WallboardConfigReader.ResolveWritableWallboardFilePath(
                    AppContext.BaseDirectory, WallboardConfigReader.ActiveEnvironmentName),
                EnvironmentName = WallboardConfigReader.ActiveEnvironmentName,
                configuration.AppTitle,
                PanelCount = configuration.Panels.Count,
                EmbeddedIconAvailable = typeof(AppIcon).Assembly.GetManifestResourceInfo(AppIcon.ResourceName) is not null,
                Arguments = args
            };
            // Refuse to overwrite an existing file, especially an operator's configuration.
            await using var stream = new FileStream(args[1], FileMode.CreateNew, FileAccess.Write);
            await JsonSerializer.SerializeAsync(stream, report, new JsonSerializerOptions { WriteIndented = true });
            return 0;
        }
        catch
        {
            return 1;
        }
    }
}
