using System.Text;

namespace NetWatchLite.Wallboard.WebView2;

/// <summary>
/// Centralized crash and diagnostics log for the wallboard.
/// Unexpected UI, WebView2, and background task failures should be written here before the app
/// attempts to recover or shows a message to the operator.
/// </summary>
internal static class AppErrorLog
{
    private static readonly object SyncRoot = new();

    /// <summary>
    /// Directory used for persistent diagnostic logs. The location is per-user and does not require
    /// administrator permissions.
    /// </summary>
    public static string LogDirectory => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "NetWatchLite",
        "WallboardLogs");

    /// <summary>
    /// Full path to the rolling text log used by this application.
    /// </summary>
    public static string LogFilePath => Path.Combine(LogDirectory, "wallboard-errors.log");

    /// <summary>
    /// Writes an exception with contextual information.
    /// </summary>
    /// <param name="context">Short description of what the app was doing.</param>
    /// <param name="exception">Exception to record.</param>
    public static void Log(string context, Exception exception)
    {
        var builder = new StringBuilder()
            .AppendLine("============================================================")
            .AppendLine($"Time: {DateTimeOffset.Now:O}")
            .AppendLine($"Context: {context}")
            .AppendLine($"Exception: {exception.GetType().FullName}")
            .AppendLine($"Message: {exception.Message}")
            .AppendLine("Stack:")
            .AppendLine(exception.ToString());

        Write(builder.ToString());
    }

    /// <summary>
    /// Writes a diagnostic message that is not tied to a caught exception.
    /// </summary>
    /// <param name="context">Short description of what happened.</param>
    /// <param name="message">Message to record.</param>
    public static void LogMessage(string context, string message)
    {
        var builder = new StringBuilder()
            .AppendLine("============================================================")
            .AppendLine($"Time: {DateTimeOffset.Now:O}")
            .AppendLine($"Context: {context}")
            .AppendLine($"Message: {message}");

        Write(builder.ToString());
    }

    /// <summary>
    /// Logs an unexpected error and shows a concise message to the operator.
    /// </summary>
    /// <param name="owner">Optional message box owner.</param>
    /// <param name="context">Short description of what failed.</param>
    /// <param name="exception">Exception to record.</param>
    public static void ShowUnexpectedError(IWin32Window? owner, string context, Exception exception)
    {
        Log(context, exception);

        try
        {
            MessageBox.Show(
                owner,
                $"An unexpected error occurred while {context}.\n\n" +
                $"{exception.Message}\n\n" +
                $"A diagnostic log was written to:\n{LogFilePath}",
                "NetWatch Lite Wallboard Error",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
        catch
        {
            // If the UI subsystem itself is failing, the file log is still the source of truth.
        }
    }

    private static void Write(string text)
    {
        try
        {
            lock (SyncRoot)
            {
                Directory.CreateDirectory(LogDirectory);
                File.AppendAllText(LogFilePath, text, Encoding.UTF8);
            }
        }
        catch
        {
            // Logging must never become the reason the app crashes.
        }
    }
}
