namespace NetWatchLite.Wallboard.WebView2;

/// <summary>
/// Application bootstrapper for the Windows WebView2 wallboard executable.
/// All application behavior lives in forms and helper classes; Program only configures WinForms
/// process defaults and opens the main wallboard window.
/// </summary>
internal static class Program
{
    /// <summary>
    /// Configures WinForms high-DPI defaults and starts the main wallboard form.
    /// The STAThread attribute is required for WinForms and WebView2 COM interop on Windows.
    /// </summary>
    [STAThread]
    private static void Main()
    {
        Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
        Application.ThreadException += (_, e) =>
            AppErrorLog.ShowUnexpectedError(null, "handling a UI event", e.Exception);
        AppDomain.CurrentDomain.UnhandledException += (_, e) =>
        {
            if (e.ExceptionObject is Exception exception)
            {
                AppErrorLog.Log("handling an unhandled application exception", exception);
            }
            else
            {
                AppErrorLog.LogMessage(
                    "handling an unhandled application exception",
                    e.ExceptionObject?.ToString() ?? "Unknown exception object");
            }
        };
        TaskScheduler.UnobservedTaskException += (_, e) =>
        {
            AppErrorLog.Log("handling an unobserved task exception", e.Exception);
            e.SetObserved();
        };

        ApplicationConfiguration.Initialize();

        try
        {
            Application.Run(new WallboardForm());
        }
        catch (Exception ex)
        {
            AppErrorLog.ShowUnexpectedError(null, "running the wallboard", ex);
        }
    }
}
