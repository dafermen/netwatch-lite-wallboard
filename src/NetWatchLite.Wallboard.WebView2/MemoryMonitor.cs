using System.ComponentModel;
using System.Diagnostics;
using System.Text;

namespace NetWatchLite.Wallboard.WebView2;

internal sealed record MemorySnapshot(
    DateTimeOffset Timestamp,
    long HostBytes,
    long BrowserBytes,
    int BrowserProcessCount,
    bool IsPartial)
{
    public long TotalBytes => HostBytes + BrowserBytes;
}

/// <summary>Samples distinct process IDs; bounded history avoids a monitoring-induced leak.</summary>
internal sealed class MemoryMonitor
{
    private readonly Func<int, long?> _readWorkingSet;
    private readonly Queue<MemorySnapshot> _history = new();
    private readonly int _historyCapacity;

    public MemoryMonitor(Func<int, long?>? readWorkingSet = null, int historyCapacity = 1440)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(historyCapacity);
        _readWorkingSet = readWorkingSet ?? ReadWorkingSet;
        _historyCapacity = historyCapacity;
    }

    public long PeakBytes { get; private set; }
    public MemorySnapshot? Latest { get; private set; }
    public IReadOnlyList<MemorySnapshot> History => _history.ToArray();

    public MemorySnapshot Sample(
        int hostProcessId,
        IEnumerable<int> browserProcessIds,
        bool browserDiscoveryAvailable,
        DateTimeOffset timestamp)
    {
        var host = _readWorkingSet(hostProcessId);
        var partial = !browserDiscoveryAvailable || host is null;
        long browserBytes = 0;
        var browserCount = 0;

        foreach (var processId in browserProcessIds.Where(id => id > 0 && id != hostProcessId).Distinct())
        {
            var bytes = _readWorkingSet(processId);
            if (bytes is null)
            {
                partial = true;
                continue;
            }

            browserBytes += Math.Max(0, bytes.Value);
            browserCount++;
        }

        var snapshot = new MemorySnapshot(timestamp, Math.Max(0, host ?? 0), browserBytes, browserCount, partial);
        Latest = snapshot;
        PeakBytes = Math.Max(PeakBytes, snapshot.TotalBytes);
        _history.Enqueue(snapshot);
        while (_history.Count > _historyCapacity)
        {
            _history.Dequeue();
        }

        return snapshot;
    }

    public string Describe()
    {
        if (Latest is not { } sample)
        {
            return "Memory monitor has not sampled yet.";
        }

        const double megabyte = 1024d * 1024;
        var text = new StringBuilder();
        text.AppendLine($"Memory sampled (UTC): {sample.Timestamp:u}");
        text.AppendLine($"Host: {sample.HostBytes / megabyte:N0} MB");
        text.AppendLine($"WebView2: {sample.BrowserBytes / megabyte:N0} MB ({sample.BrowserProcessCount} processes)");
        text.AppendLine($"Estimated total: {sample.TotalBytes / megabyte:N0} MB; peak: {PeakBytes / megabyte:N0} MB");
        text.AppendLine($"Partial sample: {sample.IsPartial}; retained samples: {_history.Count}/{_historyCapacity}");
        text.AppendLine("Working sets can share physical pages. This is an estimate, not private RAM per panel.");
        text.AppendLine("Recent samples (UTC, host MB, WebView2 MB, total MB, partial):");
        foreach (var entry in _history.TakeLast(10))
        {
            text.AppendLine($"{entry.Timestamp:u}, {entry.HostBytes / megabyte:F0}, {entry.BrowserBytes / megabyte:F0}, {entry.TotalBytes / megabyte:F0}, {entry.IsPartial}");
        }

        return text.ToString();
    }

    private static long? ReadWorkingSet(int processId)
    {
        try
        {
            using var process = Process.GetProcessById(processId);
            return process.WorkingSet64;
        }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException or Win32Exception or NotSupportedException)
        {
            // Processes may exit or deny access between enumeration and sampling.
            return null;
        }
    }
}
