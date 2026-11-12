using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace NetWatchLite.Wallboard.WebView2;

internal enum RuntimeEventKind { ManualRestart, ScheduledRestart, AutomaticRecovery, MemoryRecovery, RecoveryFailed, ProfileChanged, UnexpectedError }
internal sealed record RuntimeEvent(DateTimeOffset Timestamp, RuntimeEventKind Kind);

internal sealed class RuntimeEventHistory
{
    private readonly Queue<RuntimeEvent> _events = new();
    internal void Record(RuntimeEventKind kind)
    {
        _events.Enqueue(new(DateTimeOffset.UtcNow, kind));
        while (_events.Count > 100) _events.Dequeue();
    }
    internal RuntimeEvent[] Snapshot() => _events.ToArray();
}

internal static class SupportReport
{
    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter() }
    };

    // An allowlist, not a scrubbed copy of raw diagnostics/logs: no operator strings enter the report.
    internal static string Create(WallboardConfiguration configuration, int visiblePanels,
        IEnumerable<MemorySnapshot> memory, IEnumerable<RuntimeEvent> events, int memoryRecoveries)
    {
        var report = new
        {
            SchemaVersion = 1,
            GeneratedUtc = DateTimeOffset.UtcNow,
            ApplicationVersion = typeof(SupportReport).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion,
            RuntimeVersion = Environment.Version.ToString(),
            OperatingSystemVersion = Environment.OSVersion.Version.ToString(),
            Architecture = System.Runtime.InteropServices.RuntimeInformation.ProcessArchitecture.ToString(),
            PanelCount = configuration.Panels.Count,
            VisiblePanels = visiblePanels,
            configuration.AutomaticRecoveryEnabled,
            configuration.MemoryMonitorEnabled,
            configuration.MemoryWarningMegabytes,
            configuration.MemoryProtectionEnabled,
            configuration.MemoryProtectionSustainedMinutes,
            configuration.MemoryProtectionCooldownMinutes,
            MemoryRecoveries = memoryRecoveries,
            MaximumMemoryRecoveries = MemoryProtectionPolicy.MaximumAttempts,
            MemoryEstimate = "Combined working sets; shared pages may be counted more than once.",
            RecentMemory = memory.TakeLast(120).ToArray(),
            RecentEvents = events.TakeLast(100).ToArray()
        };
        return JsonSerializer.Serialize(report, Options);
    }

    internal static async Task WriteNewAsync(string path, string json)
    {
        var full = Path.GetFullPath(path);
        var app = Path.GetFullPath(AppContext.BaseDirectory).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
        if (!string.Equals(PortablePaths.ContentDirectory.TrimEnd(Path.DirectorySeparatorChar), app.TrimEnd(Path.DirectorySeparatorChar), StringComparison.OrdinalIgnoreCase) &&
            full.StartsWith(app, StringComparison.OrdinalIgnoreCase)) throw new IOException("Choose a report path outside the protected app directory.");
        if (System.Text.Encoding.UTF8.GetByteCount(json) > 256 * 1024) throw new IOException("Support report is too large.");
        await using var stream = new FileStream(full, FileMode.CreateNew, FileAccess.Write, FileShare.None);
        await using var writer = new StreamWriter(stream, new System.Text.UTF8Encoding(false));
        await writer.WriteAsync(json);
    }
}
