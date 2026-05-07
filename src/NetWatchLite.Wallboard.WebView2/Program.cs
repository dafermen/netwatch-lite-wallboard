namespace NetWatchLite.Wallboard.WebView2;

/// <summary>
/// Application bootstrapper for the Windows WebView2 wallboard executable.
/// </summary>
internal static class Program
{
    /// <summary>
    /// Configures WinForms high-DPI defaults and starts the main wallboard form.
    /// </summary>
    [STAThread]
    private static void Main()
    {
        ApplicationConfiguration.Initialize();
        Application.Run(new WallboardForm());
    }
}
