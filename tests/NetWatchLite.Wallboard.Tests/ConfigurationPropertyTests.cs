using System.Text.Json;
using FsCheck.Xunit;
using NetWatchLite.Wallboard.WebView2;
using Xunit;

namespace NetWatchLite.Wallboard.Tests;

public sealed class ConfigurationPropertyTests
{
    [Property(MaxTest = 500)]
    public void NormalizationIsIdempotentAndTimerSafe(int interval, int layout, int threshold, string? theme)
    {
        var input = new WallboardConfiguration
        {
            RotationSeconds = interval, DefaultLayout = layout, MemoryWarningMegabytes = threshold,
            AutoRestartHours = interval, MemoryProtectionSustainedMinutes = interval,
            MemoryProtectionCooldownMinutes = interval, Theme = theme!,
            Panels = [new() { Url = "https://example.org/", RefreshSeconds = interval }]
        };
        var normalized = WallboardConfigReader.Normalize(input);
        Assert.Equal(JsonSerializer.Serialize(normalized), JsonSerializer.Serialize(WallboardConfigReader.Normalize(normalized)));
        Assert.InRange(normalized.RotationSeconds, 1, int.MaxValue / 1000);
        Assert.InRange(normalized.Panels[0].RefreshSeconds, 1, int.MaxValue / 1000);
        Assert.InRange(normalized.MemoryWarningMegabytes, 256, 65536);
        Assert.InRange(normalized.AutoRestartHours, 1, 24);
        Assert.InRange(normalized.MemoryProtectionSustainedMinutes, 1, 60);
        Assert.InRange(normalized.MemoryProtectionCooldownMinutes, 5, 1440);
        Assert.Contains(normalized.DefaultLayout, new[] { 1, 2, 3, 4, 6, 8 });
        Assert.Contains(normalized.Theme, new[] { "dark", "light", "high-contrast" });
    }

    [Property(MaxTest = 500)]
    public void AcceptedProfileNamesCannotEscapeTheirDirectory(string? name)
    {
        if (name is null || !WallboardConfigReader.IsSelectableProfileName(name)) return;
        var root = Path.Combine(Path.GetTempPath(), "netwatch-property-root");
        var path = WallboardConfigReader.ResolveWritableWallboardFilePath(root, name);
        Assert.Equal(root, Path.GetDirectoryName(path));
        Assert.DoesNotContain("..", Path.GetFileName(path));
    }

    [Property(MaxTest = 300)]
    public void NormalizationPreservesExplicitFeatureSwitches(bool rotation, bool recovery, bool protection)
    {
        var normalized = WallboardConfigReader.Normalize(new WallboardConfiguration
        { RotationEnabled = rotation, AutomaticRecoveryEnabled = recovery, MemoryProtectionEnabled = protection });
        Assert.Equal(rotation, normalized.RotationEnabled);
        Assert.Equal(recovery, normalized.AutomaticRecoveryEnabled);
        Assert.Equal(protection, normalized.MemoryProtectionEnabled);
    }
}
