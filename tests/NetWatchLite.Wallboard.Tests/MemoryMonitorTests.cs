using NetWatchLite.Wallboard.WebView2;
using Xunit;

namespace NetWatchLite.Wallboard.Tests;

public sealed class MemoryMonitorTests
{
    [Fact]
    public void SharedProcessIdsAndHostAreCountedOnlyOnce()
    {
        var monitor = new MemoryMonitor(id => id * 100);
        var sample = monitor.Sample(1, [1, 2, 2, 3, -1, 0], true, DateTimeOffset.UtcNow);
        Assert.Equal(100, sample.HostBytes);
        Assert.Equal(500, sample.BrowserBytes);
        Assert.Equal(600, sample.TotalBytes);
        Assert.Equal(2, sample.BrowserProcessCount);
        Assert.False(sample.IsPartial);
    }

    [Fact]
    public void ExitedProcessesProduceAnExplicitPartialSample()
    {
        var monitor = new MemoryMonitor(id => id == 2 ? null : 100);
        var sample = monitor.Sample(1, [2, 3], true, DateTimeOffset.UtcNow);
        Assert.True(sample.IsPartial);
        Assert.Equal(200, sample.TotalBytes);
        Assert.Equal(1, sample.BrowserProcessCount);
    }

    [Fact]
    public void DiscoveryFailureIsNotPresentedAsCompleteMemoryUsage()
    {
        var monitor = new MemoryMonitor(_ => 100);
        Assert.True(monitor.Sample(1, [], false, DateTimeOffset.UtcNow).IsPartial);
    }

    [Fact]
    public void HistoryIsBoundedAndPeakSurvivesLowerSamples()
    {
        long bytes = 1000;
        var monitor = new MemoryMonitor(_ => bytes, historyCapacity: 3);
        monitor.Sample(1, [], true, DateTimeOffset.UtcNow);
        bytes = 100;
        for (var index = 0; index < 10000; index++)
        {
            monitor.Sample(1, [], true, DateTimeOffset.UtcNow);
        }

        Assert.Equal(3, monitor.History.Count);
        Assert.Equal(1000, monitor.PeakBytes);
        Assert.Equal(100, monitor.Latest!.TotalBytes);
    }

    [Fact]
    public void ReportsIncludeTheBreakdownAndEstimateLimitations()
    {
        var monitor = new MemoryMonitor(_ => 1048576);
        monitor.Sample(1, [2], true, DateTimeOffset.UtcNow);
        Assert.Contains("Host: 1 MB", monitor.Describe());
        Assert.Contains("WebView2: 1 MB", monitor.Describe());
        Assert.Contains("estimate", monitor.Describe());
    }
}
