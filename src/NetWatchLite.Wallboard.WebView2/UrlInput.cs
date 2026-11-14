namespace NetWatchLite.Wallboard.WebView2;

internal static class UrlInput
{
    private static readonly HashSet<string> LocalExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".html", ".htm", ".xhtml", ".asp", ".aspx", ".php", ".jsp", ".pdf",
        ".svg", ".png", ".jpg", ".jpeg", ".gif", ".txt", ".json", ".xml", ".css", ".js"
    };

    internal static bool TryNormalize(string? input, out string value, string? existingValue = null)
    {
        value = input?.Trim() ?? string.Empty;
        if (value.Length == 0) return false;
        if (value.StartsWith("http:", StringComparison.OrdinalIgnoreCase) ||
            value.StartsWith("https:", StringComparison.OrdinalIgnoreCase))
            return IsWebUrl(value);
        if (value.StartsWith("file:", StringComparison.OrdinalIgnoreCase))
            return WallboardConfigReader.IsValidHomePageUrl(value);
        if (value.Contains("://", StringComparison.Ordinal)) return false;

        // Only editor input is completed; persisted/local URL resolution remains unchanged.
        if (!value.StartsWith('/') && !value.StartsWith('\\') && !Path.IsPathRooted(value))
        {
            var end = value.IndexOfAny(['/', '?', '#', '\\']);
            var authority = end < 0 ? value : value[..end];
            var www = authority.StartsWith("www.", StringComparison.OrdinalIgnoreCase);
            var looksLikeWebsite = www || authority.Equals("localhost", StringComparison.OrdinalIgnoreCase) ||
                ((authority.Contains('.') || authority.Contains(':') || authority.Contains(',')) &&
                 !LocalExtensions.Contains(Path.GetExtension(authority)));
            if (looksLikeWebsite)
            {
                var candidate = "https://" + value;
                if (authority.Contains('@') || !IsWebUrl(candidate)) return false;
                value = value == existingValue?.Trim() ? value : candidate;
                return true;
            }
        }
        return WallboardConfigReader.IsValidHomePageUrl(value);
    }

    private static bool IsWebUrl(string value)
    {
        try
        {
            return !value.Contains('\\') && Uri.TryCreate(value, UriKind.Absolute, out var uri) &&
                (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps) &&
                Uri.CheckHostName(uri.IdnHost) != UriHostNameType.Unknown;
        }
        catch (UriFormatException) { return false; }
    }
}
