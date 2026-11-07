namespace NetWatchLite.Wallboard.WebView2;

/// <summary>Separates editable portable content from the application payload without using the working directory.</summary>
internal static class PortablePaths
{
    public static string ContentDirectory => ResolveContentDirectory(AppContext.BaseDirectory);

    internal static string ResolveContentDirectory(string baseDirectory)
    {
        var directory = new DirectoryInfo(baseDirectory);
        var parent = directory.Parent;
        if (directory.Name.Equals("app", StringComparison.OrdinalIgnoreCase) && parent is not null &&
            File.Exists(Path.Combine(parent.FullName, "portable-layout.json")) &&
            File.Exists(Path.Combine(parent.FullName, "NetWatch-Lite-Wallboard.exe")))
        {
            return parent.FullName;
        }

        // Flat portable builds and normal bin/Debug or bin/Release builds retain their original layout.
        return directory.FullName;
    }
}
