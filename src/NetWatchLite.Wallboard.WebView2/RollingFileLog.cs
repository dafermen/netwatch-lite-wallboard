using System.Text;

namespace NetWatchLite.Wallboard.WebView2;

internal static class RollingFileLog
{
    private static readonly object Sync = new();
    internal const int MaximumFileBytes = 1024 * 1024;
    internal const int MaximumEntryBytes = 32 * 1024;
    internal const int BackupCount = 3;

    internal static void Append(string path, string text, int maximumBytes = MaximumFileBytes, int entryBytes = MaximumEntryBytes)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(maximumBytes, 128);
        ArgumentOutOfRangeException.ThrowIfLessThan(entryBytes, 128);
        var limit = Math.Min(maximumBytes, entryBytes);
        // Bound encoding work as well as bytes written, even for a very large exception message.
        var prefix = text[..Math.Min(text.Length, limit / 4)];
        if (prefix.Length < text.Length) prefix += "\n[entry truncated]\n";
        var bytes = Encoding.UTF8.GetBytes(prefix);
        if (bytes.Length > limit) Array.Resize(ref bytes, limit);
        lock (Sync)
        {
            var directory = Path.GetDirectoryName(Path.GetFullPath(path))!;
            Directory.CreateDirectory(directory);
            RejectRedirect(directory);
            RejectRedirect(path);
            for (var index = 1; index <= BackupCount; index++) RejectRedirect(path + "." + index);
            if (File.Exists(path) && new FileInfo(path).Length + bytes.Length > maximumBytes)
            {
                File.Delete(path + "." + BackupCount);
                for (var index = BackupCount - 1; index >= 1; index--)
                    if (File.Exists(path + "." + index)) File.Move(path + "." + index, path + "." + (index + 1));
                // Old unbounded logs retain only a bounded tail when first rotated.
                using (var input = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read))
                using (var output = new FileStream(path + ".1", FileMode.CreateNew, FileAccess.Write))
                {
                    input.Position = Math.Max(0, input.Length - maximumBytes);
                    input.CopyTo(output);
                }
                File.Delete(path);
            }
            using var stream = new FileStream(path, FileMode.Append, FileAccess.Write, FileShare.Read);
            stream.Write(bytes);
        }
    }

    private static void RejectRedirect(string path)
    {
        if ((File.Exists(path) || Directory.Exists(path)) && (File.GetAttributes(path) & FileAttributes.ReparsePoint) != 0)
            throw new IOException("Redirected log path refused.");
    }
}
