namespace NetWatchLite.Wallboard.WebView2;

/// <summary>Bounds retries across WebView control replacements, not just navigation events.</summary>
internal sealed class PanelRecoveryPolicy
{
    private static readonly int[] RetrySeconds = [5, 15, 30];
    private DateTimeOffset? _healthySince;

    public int Attempts { get; private set; }
    public bool IsPending { get; private set; }
    public int Generation { get; private set; }
    public bool IsExhausted => Attempts >= RetrySeconds.Length && !IsPending;

    public bool TrySchedule(DateTimeOffset now, out TimeSpan delay)
    {
        delay = TimeSpan.Zero;
        if (IsPending)
        {
            return false;
        }

        if (_healthySince is { } healthySince && now - healthySince >= TimeSpan.FromMinutes(1))
        {
            Attempts = 0;
        }

        _healthySince = null;
        if (Attempts >= RetrySeconds.Length)
        {
            return false;
        }

        delay = TimeSpan.FromSeconds(RetrySeconds[Attempts++]);
        IsPending = true;
        Generation++;
        return true;
    }

    public void MarkHealthy(DateTimeOffset now)
    {
        if (IsPending)
        {
            IsPending = false;
            Generation++;
        }

        _healthySince ??= now;
    }

    public bool TryBeginAttempt(int generation)
    {
        if (!IsPending || Generation != generation)
        {
            return false;
        }

        IsPending = false;
        return true;
    }

    public void MarkInactive()
    {
        IsPending = false;
        _healthySince = null;
        Generation++;
    }

    public void Reset()
    {
        Attempts = 0;
        IsPending = false;
        _healthySince = null;
        Generation++;
    }
}
