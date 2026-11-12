namespace NetWatchLite.Wallboard.WebView2;

/// <summary>Reserves at most two sustained-high-memory recoveries per application session.</summary>
internal sealed class MemoryProtectionPolicy
{
    private DateTimeOffset? _highSince;
    private DateTimeOffset? _lastSample;
    private DateTimeOffset? _lastRecovery;
    internal int Attempts { get; private set; }
    internal const int MaximumAttempts = 2;
    internal bool IsExhausted => Attempts >= MaximumAttempts;

    internal bool Observe(MemorySnapshot sample, WallboardConfiguration configuration, bool canRecover)
    {
        var now = sample.Timestamp;
        if (!configuration.MemoryProtectionEnabled || !canRecover || sample.IsPartial ||
            sample.TotalBytes < (long)configuration.MemoryWarningMegabytes * 1024 * 1024 || IsExhausted ||
            (_lastRecovery is { } last && now - last < TimeSpan.FromMinutes(configuration.MemoryProtectionCooldownMinutes)))
        {
            SuspendTracking();
            return false;
        }
        if (_lastSample is { } previous && (now < previous || now - previous > TimeSpan.FromSeconds(90)))
            _highSince = null;
        _lastSample = now;
        _highSince ??= now;
        if (now - _highSince.Value < TimeSpan.FromMinutes(configuration.MemoryProtectionSustainedMinutes)) return false;
        Attempts++;
        _lastRecovery = now;
        SuspendTracking();
        return true;
    }

    internal void SuspendTracking()
    {
        _highSince = null;
        _lastSample = null;
    }
}
