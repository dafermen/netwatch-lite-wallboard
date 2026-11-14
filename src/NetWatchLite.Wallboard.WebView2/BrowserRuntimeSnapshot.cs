namespace NetWatchLite.Wallboard.WebView2;

/// <summary>Local diagnostics only. Paths and browser-provided strings must not enter safe support exports.</summary>
internal sealed record BrowserRuntimeSnapshot(string RequestedFolder, string? EffectiveFolder, string? Version)
{
    internal string Describe() => $"WebView2 requested data: {RequestedFolder}{Environment.NewLine}" +
        $"WebView2 effective data: {EffectiveFolder ?? "unavailable (environment not ready)"}{Environment.NewLine}" +
        $"WebView2 Runtime: {Version ?? "unavailable (environment not ready)"}";
}
