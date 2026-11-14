using NetWatchLite.Wallboard.WebView2;
using Xunit;

namespace NetWatchLite.Wallboard.Tests;

public sealed class PanelRecoveryPolicyTests
{
    private static readonly DateTimeOffset Start = new(2026, 11, 6, 0, 0, 0, TimeSpan.Zero);

    [Fact]
    public void RetriesAreProgressiveAndBounded()
    {
        var policy = new PanelRecoveryPolicy();
        foreach (var seconds in new[] { 5, 15, 30 })
        {
            Assert.True(policy.TrySchedule(Start, out var delay));
            Assert.Equal(TimeSpan.FromSeconds(seconds), delay);
            Assert.True(policy.TryBeginAttempt(policy.Generation));
        }

        Assert.False(policy.TrySchedule(Start, out _));
        Assert.True(policy.IsExhausted);
    }

    [Fact]
    public void DuplicateFailuresDoNotConsumeAnotherAttempt()
    {
        var policy = new PanelRecoveryPolicy();
        Assert.True(policy.TrySchedule(Start, out _));
        Assert.False(policy.TrySchedule(Start, out _));
        Assert.Equal(1, policy.Attempts);
    }

    [Fact]
    public void BriefSuccessfulNavigationDoesNotResetTheBudget()
    {
        var policy = new PanelRecoveryPolicy();
        policy.TrySchedule(Start, out _);
        Assert.True(policy.TryBeginAttempt(policy.Generation));
        policy.MarkHealthy(Start);
        Assert.True(policy.TrySchedule(Start.AddSeconds(59), out var delay));
        Assert.Equal(TimeSpan.FromSeconds(15), delay);
    }

    [Fact]
    public void ANewFailureEndsThePreviousHealthyPeriod()
    {
        var policy = new PanelRecoveryPolicy();
        policy.TrySchedule(Start, out _);
        policy.TryBeginAttempt(policy.Generation);
        policy.MarkHealthy(Start);
        Assert.True(policy.TrySchedule(Start.AddSeconds(59), out _));
        policy.TryBeginAttempt(policy.Generation);
        Assert.True(policy.TrySchedule(Start.AddSeconds(61), out var delay));
        Assert.Equal(TimeSpan.FromSeconds(30), delay);
        Assert.Equal(3, policy.Attempts);
    }

    [Fact]
    public void OneHealthyMinuteRestoresTheBudget()
    {
        var policy = new PanelRecoveryPolicy();
        policy.TrySchedule(Start, out _);
        Assert.True(policy.TryBeginAttempt(policy.Generation));
        policy.MarkHealthy(Start);
        policy.MarkHealthy(Start.AddSeconds(30));
        Assert.True(policy.TrySchedule(Start.AddMinutes(1), out var delay));
        Assert.Equal(TimeSpan.FromSeconds(5), delay);
    }

    [Fact]
    public void ManualResetCancelsPendingRecoveryAndRestoresAttempts()
    {
        var policy = new PanelRecoveryPolicy();
        policy.TrySchedule(Start, out _);
        policy.Reset();
        Assert.False(policy.IsPending);
        Assert.Equal(0, policy.Attempts);
        Assert.True(policy.TrySchedule(Start, out _));
    }

    [Fact]
    public void SuccessfulNavigationInvalidatesQueuedRecovery()
    {
        var policy = new PanelRecoveryPolicy();
        policy.TrySchedule(Start, out _);
        var generation = policy.Generation;
        policy.MarkHealthy(Start.AddSeconds(6));
        Assert.False(policy.TryBeginAttempt(generation));
    }

    [Fact]
    public void StaleRecoveryCannotStartAfterManualReset()
    {
        var policy = new PanelRecoveryPolicy();
        policy.TrySchedule(Start, out _);
        var generation = policy.Generation;
        policy.Reset();
        policy.TrySchedule(Start, out _);
        Assert.False(policy.TryBeginAttempt(generation));
        Assert.True(policy.TryBeginAttempt(policy.Generation));
    }

    [Fact]
    public void InactiveTimeDoesNotRestoreTheRetryBudget()
    {
        var policy = new PanelRecoveryPolicy();
        policy.TrySchedule(Start, out _);
        policy.TryBeginAttempt(policy.Generation);
        policy.MarkHealthy(Start);
        policy.MarkInactive();
        policy.MarkHealthy(Start.AddMinutes(5));
        Assert.True(policy.TrySchedule(Start.AddMinutes(5).AddSeconds(1), out var delay));
        Assert.Equal(TimeSpan.FromSeconds(15), delay);
    }

    [Fact]
    public void ExhaustionWaitsUntilTheLastPendingAttemptBegins()
    {
        var policy = new PanelRecoveryPolicy();
        Assert.False(policy.IsExhausted);
        for (var index = 0; index < 3; index++)
        {
            Assert.True(policy.TrySchedule(Start, out _));
            Assert.False(policy.IsExhausted);
            Assert.True(policy.TryBeginAttempt(policy.Generation));
        }
        Assert.True(policy.IsExhausted);
    }

    [Fact]
    public void CancelAndResetAdvanceGenerationAndClearPendingState()
    {
        var policy = new PanelRecoveryPolicy();
        Assert.Equal(0, policy.Generation);
        policy.TrySchedule(Start, out _);
        var generation = policy.Generation;
        policy.MarkHealthy(Start);
        Assert.Equal(generation + 1, policy.Generation);
        Assert.False(policy.IsPending);
        policy.TrySchedule(Start, out _);
        generation = policy.Generation;
        policy.MarkInactive();
        Assert.Equal(generation + 1, policy.Generation);
        Assert.False(policy.IsPending);
        Assert.False(policy.TryBeginAttempt(generation));
        generation = policy.Generation;
        policy.Reset();
        Assert.Equal(generation + 1, policy.Generation);
        Assert.False(policy.IsPending);
        Assert.Equal(0, policy.Attempts);
    }

    [Fact]
    public void ResetDoesNotReuseAnEarlierHealthyPeriod()
    {
        var policy = new PanelRecoveryPolicy();
        policy.MarkHealthy(Start);
        policy.Reset();
        policy.MarkHealthy(Start.AddMinutes(5));
        Assert.True(policy.TrySchedule(Start.AddMinutes(5).AddSeconds(1), out var delay));
        Assert.Equal(TimeSpan.FromSeconds(5), delay);
        Assert.Equal(1, policy.Attempts);
    }
}
