using NetWatchLite.Wallboard.WebView2;
using Xunit;

namespace NetWatchLite.Wallboard.Tests;

public sealed class MemoryProtectionPolicyTests
{
    private static readonly DateTimeOffset Start = DateTimeOffset.UnixEpoch;
    private static WallboardConfiguration Configuration() => new()
    {
        MemoryProtectionEnabled = true, MemoryWarningMegabytes = 256,
        MemoryProtectionSustainedMinutes = 1, MemoryProtectionCooldownMinutes = 5
    };
    private static MemorySnapshot High(int seconds, bool partial = false)
        => new(Start.AddSeconds(seconds), 300L * 1024 * 1024, 0, 1, partial);

    [Fact]
    public void SustainedMemoryIsRequiredAndCooldownAndSessionBudgetAreEnforced()
    {
        var policy = new MemoryProtectionPolicy();
        var configuration = Configuration();
        Assert.False(policy.Observe(High(0), configuration, true));
        Assert.False(policy.Observe(High(59), configuration, true));
        Assert.True(policy.Observe(High(60), configuration, true));
        for (var seconds = 61; seconds < 360; seconds += 30) Assert.False(policy.Observe(High(seconds), configuration, true));
        Assert.False(policy.Observe(High(360), configuration, true));
        Assert.True(policy.Observe(High(420), configuration, true));
        Assert.True(policy.IsExhausted);
        for (var seconds = 421; seconds < 3600; seconds += 60) Assert.False(policy.Observe(High(seconds), configuration, true));
        Assert.Equal(2, policy.Attempts);
    }

    [Theory]
    [InlineData("disabled")]
    [InlineData("partial")]
    [InlineData("low")]
    [InlineData("suspended")]
    [InlineData("gap")]
    [InlineData("clock-backwards")]
    public void DisabledPartialLowSuspendedOrDiscontinuousSamplesCannotTriggerRecovery(string kind)
    {
        var policy = new MemoryProtectionPolicy();
        var configuration = Configuration();
        Assert.False(policy.Observe(High(0), configuration, true));
        var sample = kind switch
        {
            "partial" => High(60, true),
            "low" => High(60) with { HostBytes = 0 },
            "gap" => High(120),
            "clock-backwards" => High(-60),
            _ => High(60)
        };
        configuration.MemoryProtectionEnabled = kind != "disabled";
        Assert.False(policy.Observe(sample, configuration, kind != "suspended"));
        Assert.Equal(0, policy.Attempts);
        configuration.MemoryProtectionEnabled = true;
        policy.SuspendTracking();
        Assert.False(policy.Observe(High(600), configuration, true));
    }

    [Fact]
    public void ConfigurationReloadTrackingResetDoesNotRestoreTheSessionBudget()
    {
        var policy = new MemoryProtectionPolicy();
        var configuration = Configuration();
        policy.Observe(High(0), configuration, true);
        Assert.True(policy.Observe(High(60), configuration, true));
        policy.SuspendTracking();
        Assert.Equal(1, policy.Attempts);
        Assert.False(policy.Observe(High(120), configuration, true));
    }
}
