using NetWatchLite.Wallboard.WebView2;
using Xunit;

namespace NetWatchLite.Wallboard.Tests;

public sealed class PortableLayoutTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "NetWatchTests", Guid.NewGuid().ToString("N"), "portable with spaces");
    private string AppDirectory => Path.Combine(_root, "app");

    public PortableLayoutTests() => Directory.CreateDirectory(AppDirectory);

    private void MarkPortable()
    {
        File.WriteAllText(Path.Combine(_root, "portable-layout.json"), "{\"schemaVersion\":1}");
        File.WriteAllText(Path.Combine(_root, "NetWatch-Lite-Wallboard.exe"), "test launcher marker");
    }

    [Fact]
    public void FlatPackagesRetainTheirOriginalContentDirectory()
    {
        Assert.Equal(_root, PortablePaths.ResolveContentDirectory(_root));
        Assert.Equal(Path.Combine(_root, "wallboard.json"), WallboardConfigReader.ResolveWritableWallboardFilePath(_root, ""));
    }

    [Fact]
    public void MarkedAppPayloadUsesTheParentPortableRoot()
    {
        MarkPortable();
        Assert.Equal(_root, PortablePaths.ResolveContentDirectory(AppDirectory + Path.DirectorySeparatorChar));
    }

    [Fact]
    public void AnUnmarkedDirectoryNamedAppNeverRedirectsConfiguration()
    {
        Assert.Equal(AppDirectory, PortablePaths.ResolveContentDirectory(AppDirectory));
        File.WriteAllText(Path.Combine(_root, "portable-layout.json"), "{}");
        Assert.Equal(AppDirectory, PortablePaths.ResolveContentDirectory(AppDirectory));
    }

    [Theory]
    [InlineData("")]
    [InlineData("demo")]
    [InlineData("production")]
    public async Task ProfileRoundTripsAndBackupsStayAtTheRoot(string profile)
    {
        MarkPortable();
        var fileName = profile.Length == 0 ? "wallboard.json" : $"wallboard.{profile}.json";
        var expectedPath = Path.Combine(_root, fileName);
        await File.WriteAllTextAsync(expectedPath, "{\"appTitle\":\"Existing custom title\",\"panels\":[{\"url\":\"https://example.org/\"}]}");
        var original = await File.ReadAllTextAsync(expectedPath);
        var readPath = WallboardConfigReader.ResolveWallboardFilePath(AppDirectory, profile);
        var writePath = WallboardConfigReader.ResolveWritableWallboardFilePath(AppDirectory, profile);
        Assert.Equal(expectedPath, readPath);
        Assert.Equal(expectedPath, writePath);
        var configuration = await WallboardConfigReader.LoadFileAsync(readPath);
        Assert.Equal("Existing custom title", configuration.AppTitle);
        configuration.AppTitle = "Saved title";
        await WallboardConfigReader.SaveFileAsync(configuration, writePath);
        Assert.Equal("Saved title", (await WallboardConfigReader.LoadFileAsync(readPath)).AppTitle);
        Assert.Equal(original, await File.ReadAllTextAsync(Path.Combine(_root, Path.GetFileNameWithoutExtension(fileName) + ".backup.json")));
        Assert.Empty(Directory.GetFiles(AppDirectory, "wallboard*.json"));
    }

    [Fact]
    public void MissingProfileLoadsDefaultButSavesItsOwnFilename()
    {
        MarkPortable();
        File.WriteAllText(Path.Combine(_root, "wallboard.json"), "{}");
        Assert.Equal(Path.Combine(_root, "wallboard.json"), WallboardConfigReader.ResolveWallboardFilePath(AppDirectory, "new-profile"));
        Assert.Equal(Path.Combine(_root, "wallboard.new-profile.json"), WallboardConfigReader.ResolveWritableWallboardFilePath(AppDirectory, "new-profile"));
    }

    [Fact]
    public void DevelopmentBuildsStillSaveIntoRepositoryData()
    {
        var build = Path.Combine(_root, "src", "Project", "bin", "Release", "net10.0-windows");
        Assert.Equal(Path.Combine(_root, "Data", "wallboard.production.json"),
            WallboardConfigReader.ResolveWritableWallboardFilePath(build, "production"));
    }

    [Theory]
    [InlineData("pages/status.html?view=tv", "pages", "status.html")]
    [InlineData("/status/index.html?view=tv", "wwwroot/status", "index.html")]
    public void PackagedPagesResolveFromTheRootNotApp(string url, string directory, string name)
    {
        MarkPortable();
        var expected = Path.Combine(_root, directory.Replace('/', Path.DirectorySeparatorChar), name);
        Directory.CreateDirectory(Path.GetDirectoryName(expected)!);
        File.WriteAllText(expected, "<title>Local fixture</title>");
        var uri = WebViewPanelControl.ResolvePanelUri(url, AppDirectory);
        Assert.Equal(expected, uri.LocalPath);
        Assert.Equal("?view=tv", uri.Query);
    }

    [Fact]
    public async Task PackageProbeCannotOverwriteAnExistingFile()
    {
        var path = Path.Combine(_root, "existing.json");
        await File.WriteAllTextAsync(path, "Preserve this file");
        Assert.Equal(1, await PackageCheck.RunAsync(["--package-check", path]));
        Assert.Equal("Preserve this file", await File.ReadAllTextAsync(path));
    }

    public void Dispose() => Directory.Delete(_root, recursive: true);
}
