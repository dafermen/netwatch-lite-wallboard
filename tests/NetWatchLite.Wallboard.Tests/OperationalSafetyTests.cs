using System.Text;
using System.Text.Json;
using NetWatchLite.Wallboard.WebView2;
using Xunit;

namespace NetWatchLite.Wallboard.Tests;

public sealed class OperationalSafetyTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "NetWatchOperationsTests", Guid.NewGuid().ToString("N"));
    public OperationalSafetyTests() => Directory.CreateDirectory(_root);

    [Fact]
    public void LogRotationAndConcurrentEntriesStayBoundedWithoutTouchingOtherFiles()
    {
        var path = Path.Combine(_root, "errors.log");
        var profile = Path.Combine(_root, "wallboard.json");
        File.WriteAllText(profile, "Do not touch");
        Parallel.For(0, 1000, index => RollingFileLog.Append(path, $"event {index}\n", 512, 128));
        var files = Directory.GetFiles(_root, "errors.log*");
        Assert.InRange(files.Length, 1, 4);
        Assert.All(files, file => Assert.InRange(new FileInfo(file).Length, 1, 512));
        Assert.Equal("Do not touch", File.ReadAllText(profile));
    }

    [Fact]
    public void OversizedOldLogAndLargeUnicodeEntryAreBounded()
    {
        var path = Path.Combine(_root, "errors.log");
        File.WriteAllText(path, new string('x', 10000));
        RollingFileLog.Append(path, new string('\u4E2D', 10000), 512, 256);
        Assert.All(Directory.GetFiles(_root), file => Assert.InRange(new FileInfo(file).Length, 1, 512));
        var text = new UTF8Encoding(false, true).GetString(File.ReadAllBytes(path));
        Assert.Contains("truncated", text);
    }

    [Fact]
    public void LockedLogFailsWithoutChangingExistingContent()
    {
        var path = Path.Combine(_root, "errors.log");
        File.WriteAllText(path, "original");
        using (var file = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.None))
            Assert.Throws<IOException>(() => RollingFileLog.Append(path, "new"));
        Assert.Equal("original", File.ReadAllText(path));
    }

    [Theory]
    [InlineData("../production")]
    [InlineData("Prod")]
    [InlineData("a.b")]
    [InlineData("backup")]
    [InlineData("prod:stream")]
    public async Task ExplicitProfileSelectionRejectsUnsafeNames(string name)
        => await Assert.ThrowsAsync<ArgumentException>(() => WallboardConfigReader.LoadProfileForSelectionAsync(_root, name));

    [Fact]
    public async Task ProfileDiscoveryExcludesBackupsAndSelectionNeverFallsBackOrWrites()
    {
        File.WriteAllText(Path.Combine(_root, "wallboard.json"), "{}");
        File.WriteAllText(Path.Combine(_root, "wallboard.night.json"), "{\"appTitle\":\"Night\"}");
        File.WriteAllText(Path.Combine(_root, "wallboard.night.backup.json"), "backup");
        File.WriteAllText(Path.Combine(_root, "wallboard.BIG.json"), "{}");
        var before = Directory.GetFiles(_root).ToDictionary(path => path, File.ReadAllBytes);
        var profiles = WallboardConfigReader.GetAvailableProfiles(_root);
        Assert.Contains("", profiles);
        Assert.Contains("night", profiles);
        Assert.Contains("big", profiles);
        Assert.DoesNotContain("night.backup", profiles);
        var selected = await WallboardConfigReader.LoadProfileForSelectionAsync(_root, "night");
        Assert.Equal("Night", selected.AppTitle);
        await Assert.ThrowsAsync<FileNotFoundException>(() => WallboardConfigReader.LoadProfileForSelectionAsync(_root, "missing"));
        foreach (var entry in before) Assert.Equal(entry.Value, File.ReadAllBytes(entry.Key));
        Assert.Equal(before.Count, Directory.GetFiles(_root).Length);
    }

    [Fact]
    public async Task MalformedExplicitProfileDoesNotSilentlySelectDefault()
    {
        File.WriteAllText(Path.Combine(_root, "wallboard.json"), "{}");
        File.WriteAllText(Path.Combine(_root, "wallboard.broken.json"), "{broken");
        await Assert.ThrowsAsync<JsonException>(() => WallboardConfigReader.LoadProfileForSelectionAsync(_root, "broken"));
    }

    [Fact]
    public async Task SupportReportOmitsOperatorStringsAndRefusesOverwritingFiles()
    {
        const string secret = "PRIVATE_TOKEN_DO_NOT_EXPORT";
        var configuration = new WallboardConfiguration
        {
            AppTitle = secret,
            Theme = secret,
            Panels = [new() { Name = secret, Url = $"https://user:{secret}@internal.example/{secret}?token={secret}#session" }]
        };
        var events = new RuntimeEventHistory();
        for (var index = 0; index < 1000; index++) events.Record(RuntimeEventKind.AutomaticRecovery);
        Assert.Equal(100, events.Snapshot().Length);
        var samples = Enumerable.Range(0, 200).Select(_ => new MemorySnapshot(DateTimeOffset.UtcNow, 100, 200, 2, false));
        var json = SupportReport.Create(configuration, 1, samples, events.Snapshot(), 0);
        Assert.DoesNotContain(secret, json);
        Assert.DoesNotContain("internal.example", json);
        Assert.DoesNotContain("url", json, StringComparison.OrdinalIgnoreCase);
        using var parsed = JsonDocument.Parse(json);
        Assert.Equal(120, parsed.RootElement.GetProperty("recentMemory").GetArrayLength());
        Assert.Equal(100, parsed.RootElement.GetProperty("recentEvents").GetArrayLength());
        var path = Path.Combine(_root, "report.json");
        await SupportReport.WriteNewAsync(path, json);
        await Assert.ThrowsAsync<IOException>(() => SupportReport.WriteNewAsync(path, "replace"));
        Assert.Equal(json, File.ReadAllText(path));
    }

    [Fact]
    public async Task NewMemoryOptionsDefaultOffClampAndRoundTrip()
    {
        Assert.False(WallboardConfigReader.Normalize(new()).MemoryProtectionEnabled);
        foreach (var value in new[] { int.MinValue, -1, 0, 1, int.MaxValue })
        {
            var normalized = WallboardConfigReader.Normalize(new WallboardConfiguration
            {
                MemoryProtectionEnabled = true,
                MemoryProtectionSustainedMinutes = value,
                MemoryProtectionCooldownMinutes = value
            });
            Assert.InRange(normalized.MemoryProtectionSustainedMinutes, 1, 60);
            Assert.InRange(normalized.MemoryProtectionCooldownMinutes, 5, 1440);
            var path = Path.Combine(_root, "roundtrip.json");
            await WallboardConfigReader.SaveFileAsync(normalized, path);
            var loaded = await WallboardConfigReader.LoadFileAsync(path);
            Assert.True(loaded.MemoryProtectionEnabled);
            Assert.Equal(normalized.MemoryProtectionSustainedMinutes, loaded.MemoryProtectionSustainedMinutes);
            Assert.Equal(normalized.MemoryProtectionCooldownMinutes, loaded.MemoryProtectionCooldownMinutes);
        }
    }

    public void Dispose() => Directory.Delete(_root, recursive: true);
}
