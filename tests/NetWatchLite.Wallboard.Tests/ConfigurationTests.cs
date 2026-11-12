using NetWatchLite.Wallboard.WebView2;
using Xunit;

namespace NetWatchLite.Wallboard.Tests;

public sealed class ConfigurationTests : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), "NetWatchTests", Guid.NewGuid().ToString("N"));

    public ConfigurationTests() => Directory.CreateDirectory(_directory);

    [Fact]
    public async Task OldConfigurationRemainsCompatibleAndEnablesRecoveryByDefault()
    {
        var configuration = await WallboardConfigReader.LoadFileAsync(
            Path.Combine(AppContext.BaseDirectory, "fixtures", "wallboard-v0.3.0.json"));
        Assert.Equal("Compatibility fixture", configuration.AppTitle);
        Assert.Equal(4, configuration.DefaultLayout);
        Assert.Equal("high-contrast", configuration.Theme);
        Assert.True(configuration.AutomaticRecoveryEnabled);
        Assert.False(configuration.MemoryProtectionEnabled);
        Assert.Equal(5, configuration.MemoryProtectionSustainedMinutes);
        Assert.Equal(30, configuration.MemoryProtectionCooldownMinutes);
        Assert.Equal(60, Assert.Single(configuration.Panels).RefreshSeconds);
    }

    [Fact]
    public async Task SaveReadAndBackupPreserveTheActiveConfiguration()
    {
        var path = Path.Combine(_directory, "wallboard.production.json");
        var configuration = new WallboardConfiguration
        {
            AppTitle = "Original",
            AutomaticRecoveryEnabled = false,
            MemoryMonitorEnabled = true,
            Panels = [new() { Name = "Panel", Url = "https://example.org/", RefreshSeconds = 45 }]
        };

        await WallboardConfigReader.SaveFileAsync(configuration, path);
        var original = await File.ReadAllTextAsync(path);
        configuration.AppTitle = "Updated";
        await WallboardConfigReader.SaveFileAsync(configuration, path);
        var loaded = await WallboardConfigReader.LoadFileAsync(path);

        Assert.Equal("Updated", loaded.AppTitle);
        Assert.False(loaded.AutomaticRecoveryEnabled);
        Assert.True(loaded.MemoryMonitorEnabled);
        Assert.Equal(45, Assert.Single(loaded.Panels).RefreshSeconds);
        Assert.Equal(original, await File.ReadAllTextAsync(Path.Combine(_directory, "wallboard.production.backup.json")));
        Assert.False(File.Exists(path + ".tmp"));
    }

    [Fact]
    public async Task MissingAndMalformedFilesHaveUsableFallbacks()
    {
        var missing = await WallboardConfigReader.LoadFileAsync(Path.Combine(_directory, "missing.json"));
        Assert.NotEmpty(missing.Panels);
        var path = Path.Combine(_directory, "invalid.json");
        await File.WriteAllTextAsync(path, "{\"panels\": [");
        var malformed = await WallboardConfigReader.LoadFileAsync(path);
        Assert.NotEmpty(malformed.Panels);
    }

    [Fact]
    public async Task NullAndInvalidPanelsDoNotDiscardTheValidOnes()
    {
        var path = Path.Combine(_directory, "panels.json");
        await File.WriteAllTextAsync(path, """
            { "panels": [null, {"url":"javascript:alert(1)"}, {"name":"Valid","url":"https://example.org/"}] }
            """);
        var loaded = await WallboardConfigReader.LoadFileAsync(path);
        Assert.Equal("Valid", Assert.Single(loaded.Panels).Name);
    }

    [Fact]
    public async Task CancellationIsNotSilentlyReplacedWithDefaultConfiguration()
    {
        var path = Path.Combine(_directory, "canceled.json");
        await File.WriteAllTextAsync(path, "{}");
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => WallboardConfigReader.LoadFileAsync(path, cancellation.Token));
    }

    [Fact]
    public void NormalizedNumbersCannotOverflowTimers()
    {
        var random = new Random(1457);
        foreach (var value in new[] { int.MinValue, -1, 0, 1, int.MaxValue }.Concat(
            Enumerable.Range(0, 1000).Select(_ => (int)random.NextInt64(int.MinValue, (long)int.MaxValue + 1))))
        {
            var result = WallboardConfigReader.Normalize(new WallboardConfiguration
            {
                RotationSeconds = value,
                AutoRestartHours = value,
                MemoryWarningMegabytes = value,
                DefaultLayout = value,
                Panels = [new() { Url = "https://example.org/", RefreshSeconds = value }]
            });

            Assert.InRange(result.RotationSeconds, 1, int.MaxValue / 1000);
            Assert.InRange(Assert.Single(result.Panels).RefreshSeconds, 1, int.MaxValue / 1000);
            Assert.InRange(result.AutoRestartHours, 1, 24);
            Assert.InRange(result.MemoryWarningMegabytes, 256, 65536);
            Assert.Contains(result.DefaultLayout, new[] { 1, 2, 3, 4, 6, 8 });
        }
    }

    [Fact]
    public async Task UnknownFieldsAndMissingOptionalFieldsAreAccepted()
    {
        var path = Path.Combine(_directory, "minimal.json");
        await File.WriteAllTextAsync(path, """{"futureField": 12, "panels":[{"url":"https://example.org/"}]}""");
        var loaded = await WallboardConfigReader.LoadFileAsync(path);
        Assert.True(loaded.AutomaticRecoveryEnabled);
        Assert.Equal(30, Assert.Single(loaded.Panels).RefreshSeconds);
    }

    public void Dispose()
    {
        if (Directory.Exists(_directory))
        {
            Directory.Delete(_directory, recursive: true);
        }
    }
}
