using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;
using WebView2Control = Microsoft.Web.WebView2.WinForms.WebView2;

namespace NetWatchLite.Wallboard.WebView2;

/// <summary>
/// User control that wraps one WebView2 browser instance, title bar, refresh button, restart button,
/// and panel-level navigation status. The main form treats this control as one panel slot.
/// </summary>
internal sealed class WebViewPanelControl : UserControl
{
    private readonly Label _statusLabel = new();
    private readonly ToolTip _panelToolTip = new();
    private readonly WebView2Control _webView = new();
    private readonly System.Windows.Forms.Timer _refreshTimer = new();
    private readonly System.Windows.Forms.Timer _recoveryTimer = new();
    private readonly PanelRecoveryPolicy _recovery;
    private readonly bool _automaticRecoveryEnabled;
    private WallboardPanel? _panel;
    private bool _disposed;
    private bool _isActivePanel;
    private Uri? _targetUri;
    private bool _showingErrorPage;
    private int _scheduledRecoveryGeneration;

    public bool BrowserProcessFailed { get; private set; }

    /// <summary>
    /// Builds the panel title bar, refresh/restart buttons, hosted WebView2 control, and refresh timer.
    /// </summary>
    public WebViewPanelControl(ThemePalette theme, PanelRecoveryPolicy recovery, bool automaticRecoveryEnabled)
    {
        _recovery = recovery;
        _automaticRecoveryEnabled = automaticRecoveryEnabled;
        BackColor = theme.PanelBackColor;
        Dock = DockStyle.Fill;
        Margin = new Padding(4);

        var titleBar = new Panel
        {
            BackColor = theme.PanelTitleBackColor,
            Dock = DockStyle.Top,
            Height = 34,
            Padding = new Padding(10, 0, 6, 0)
        };

        var titleLabel = new Label
        {
            AutoEllipsis = true,
            Dock = DockStyle.Fill,
            Font = new Font("Segoe UI", 9F, FontStyle.Bold),
            ForeColor = theme.PrimaryTextColor,
            TextAlign = ContentAlignment.MiddleLeft
        };

        _statusLabel.Dock = DockStyle.Right;
        _statusLabel.Font = new Font("Segoe UI", 8F);
        _statusLabel.ForeColor = theme.SecondaryTextColor;
        _statusLabel.TextAlign = ContentAlignment.MiddleRight;
        _statusLabel.Width = 170;

        var refreshButton = new Button
        {
            Dock = DockStyle.Right,
            FlatStyle = FlatStyle.Flat,
            BackColor = theme.ButtonBackColor,
            ForeColor = theme.PrimaryTextColor,
            Text = "Refresh",
            Width = 76
        };
        refreshButton.FlatAppearance.BorderColor = theme.BorderColor;
        refreshButton.Click += (_, _) => RefreshPanel();
        _panelToolTip.SetToolTip(refreshButton, "Refresh this panel only");

        var restartButton = new Button
        {
            Dock = DockStyle.Right,
            FlatStyle = FlatStyle.Flat,
            BackColor = theme.ButtonBackColor,
            ForeColor = theme.PrimaryTextColor,
            Text = "Restart",
            Width = 76
        };
        restartButton.FlatAppearance.BorderColor = theme.BorderColor;
        restartButton.Click += (_, _) => RestartRequested?.Invoke(this, EventArgs.Empty);
        _panelToolTip.SetToolTip(restartButton, "Restart this panel only");

        titleBar.Controls.Add(titleLabel);
        titleBar.Controls.Add(_statusLabel);
        titleBar.Controls.Add(refreshButton);
        titleBar.Controls.Add(restartButton);

        _webView.BackColor = theme.WebViewBackColor;
        _webView.DefaultBackgroundColor = theme.WebViewBackColor;
        _webView.Dock = DockStyle.Fill;
        _webView.NavigationCompleted += OnNavigationCompleted;
        _webView.KeyDown += OnWebViewKeyDown;

        Controls.Add(_webView);
        Controls.Add(titleBar);

        _refreshTimer.Tick += (_, _) => RefreshPanel(resetRecovery: false);
        _recoveryTimer.Tick += (_, _) =>
        {
            _recoveryTimer.Stop();
            if (!_disposed && _isActivePanel)
            {
                _statusLabel.Text = "Reconnecting...";
                AutomaticRestartRequested?.Invoke(this, new RecoveryRequestedEventArgs(_scheduledRecoveryGeneration));
            }
        };

        TitleLabel = titleLabel;
    }

    private Label TitleLabel { get; }

    /// <summary>
    /// Raised when the hosted WebView2 receives a wallboard-level shortcut key.
    /// </summary>
    public event EventHandler<PanelShortcutRequestedEventArgs>? ShortcutRequested;

    /// <summary>
    /// Raised when the operator asks the main form to destroy and recreate this panel.
    /// </summary>
    public event EventHandler? RestartRequested;

    public event EventHandler<RecoveryRequestedEventArgs>? AutomaticRestartRequested;

    public bool TryBeginRecovery(int generation) =>
        !_disposed && _isActivePanel && _recovery.TryBeginAttempt(generation);

    /// <summary>
    /// Initializes WebView2 and navigates to the configured panel URL.
    /// </summary>
    /// <param name="panel">Panel declaration to render.</param>
    /// <param name="environment">Shared WebView2 environment used by all panels.</param>
    /// <param name="cancellationToken">Token used to cancel initialization.</param>
    public async Task LoadPanelAsync(
        WallboardPanel panel,
        CoreWebView2Environment environment,
        CancellationToken cancellationToken = default)
    {
        if (_disposed)
        {
            return;
        }

        _isActivePanel = true;
        _panel = panel;
        _targetUri = ResolvePanelUri(panel.Url);

        TitleLabel.Text = panel.Name;
        _statusLabel.Text = $"{panel.RefreshSeconds}s refresh";

        await _webView.EnsureCoreWebView2Async(environment).WaitAsync(TimeSpan.FromSeconds(30), cancellationToken);

        if (cancellationToken.IsCancellationRequested || _disposed)
        {
            return;
        }

        ConfigureWebView();
        Navigate();
        ScheduleRefresh(panel.RefreshSeconds);
    }

    /// <summary>
    /// Reloads this panel without affecting the other visible panels.
    /// </summary>
    public void RefreshPanel(bool resetRecovery = true)
    {
        if (_disposed)
        {
            return;
        }

        RunPanelAction(
            () =>
            {
                if (_webView.CoreWebView2 is null || BrowserProcessFailed)
                {
                    if (resetRecovery)
                    {
                        RestartRequested?.Invoke(this, EventArgs.Empty);
                    }

                    return;
                }

                if (resetRecovery)
                {
                    _recoveryTimer.Stop();
                    _recovery.Reset();
                }
                if (_targetUri is null)
                {
                    return;
                }

                if (_panel is not null)
                {
                    _targetUri = ResolvePanelUri(_panel.Url);
                }

                _statusLabel.Text = "Refreshing...";
                _showingErrorPage = false;
                _webView.CoreWebView2.Navigate(BuildRefreshUri(_targetUri).ToString());
            },
            $"refreshing panel '{_panel?.Name ?? "Panel"}'");
    }

    /// <summary>
    /// Stops all timers owned by this panel.
    /// </summary>
    public void StopTimers()
    {
        _isActivePanel = false;
        _refreshTimer.Stop();
        _recoveryTimer.Stop();
        _recovery.MarkInactive();
    }

    /// <summary>
    /// Displays a panel-level failure page when initialization fails before WebView2 can navigate.
    /// </summary>
    /// <param name="panelName">Panel name shown to the operator.</param>
    /// <param name="exception">Exception that prevented the panel from loading.</param>
    public void ShowPanelError(string panelName, Exception exception)
    {
        if (_disposed)
        {
            return;
        }

        _statusLabel.Text = "Panel error";
        _refreshTimer.Stop();

        var message = System.Net.WebUtility.HtmlEncode(
            $"{panelName} could not be initialized: {exception.Message}");
        TryShowErrorHtml(message);
        ScheduleRecovery();
    }

    /// <summary>
    /// Applies kiosk-style WebView2 settings suitable for operations screens.
    /// </summary>
    private void ConfigureWebView()
    {
        if (_webView.CoreWebView2 is null)
        {
            return;
        }

        _webView.CoreWebView2.Settings.AreDefaultContextMenusEnabled = false;
        _webView.CoreWebView2.Settings.AreDevToolsEnabled = false;
        _webView.CoreWebView2.Settings.IsStatusBarEnabled = false;
        _webView.CoreWebView2.Settings.IsZoomControlEnabled = false;
        _webView.CoreWebView2.Settings.AreBrowserAcceleratorKeysEnabled = false;
        _webView.CoreWebView2.ProcessFailed -= OnWebViewProcessFailed;
        _webView.CoreWebView2.ProcessFailed += OnWebViewProcessFailed;
    }

    /// <summary>
    /// Forwards keyboard shortcuts when focus is on the WebView2 control itself.
    /// </summary>
    private void OnWebViewKeyDown(object? sender, KeyEventArgs e)
    {
        var keyCode = e.KeyCode;
        var modifiers = e.Modifiers;

        if ((modifiers == Keys.Control && keyCode is (Keys.F or Keys.R or Keys.S)) ||
            (modifiers == Keys.None && keyCode is (Keys.F or Keys.R or Keys.C or Keys.Escape)))
        {
            ShortcutRequested?.Invoke(this, new PanelShortcutRequestedEventArgs(keyCode, modifiers));
            e.Handled = true;
            e.SuppressKeyPress = true;
        }
    }

    /// <summary>
    /// Starts the panel-specific refresh timer.
    /// </summary>
    private void ScheduleRefresh(int refreshSeconds)
    {
        _refreshTimer.Stop();
        if (_recovery.IsPending || _showingErrorPage)
        {
            return;
        }
        _refreshTimer.Interval = Math.Clamp(refreshSeconds, 1, int.MaxValue / 1000) * 1000;
        _refreshTimer.Start();
    }

    /// <summary>
    /// Navigates the WebView2 control to the resolved target URI.
    /// </summary>
    private void Navigate()
    {
        RunPanelAction(
            () =>
            {
                if (_webView.CoreWebView2 is null || _targetUri is null)
                {
                    return;
                }

                _statusLabel.Text = "Loading...";
                _showingErrorPage = false;
                _webView.CoreWebView2.Navigate(BuildRefreshUri(_targetUri).ToString());
            },
            $"navigating panel '{_panel?.Name ?? "Panel"}'");
    }

    /// <summary>
    /// Updates status text after navigation and renders a friendly error page on failure.
    /// </summary>
    private void OnNavigationCompleted(object? sender, CoreWebView2NavigationCompletedEventArgs e)
    {
        try
        {
            if (_disposed)
            {
                return;
            }

            if (!_isActivePanel)
            {
                return;
            }

            // NavigateToString is a local error page, not a recovered operational page.
            if (_showingErrorPage)
            {
                return;
            }

            if (e.IsSuccess)
            {
                _recoveryTimer.Stop();
                _recovery.MarkHealthy(DateTimeOffset.UtcNow);
                if (_panel is not null && !_refreshTimer.Enabled)
                {
                    ScheduleRefresh(_panel.RefreshSeconds);
                }
                _statusLabel.Text = $"Loaded {DateTime.Now:T}";
                return;
            }

            if (e.WebErrorStatus == CoreWebView2WebErrorStatus.OperationCanceled)
            {
                return;
            }

            var name = _panel?.Name ?? "Panel";
            var message = System.Net.WebUtility.HtmlEncode($"{name} failed to load: {e.WebErrorStatus}");
            TryShowErrorHtml(message);
            _statusLabel.Text = "Load failed";
            ScheduleRecovery();
        }
        catch (Exception ex)
        {
            AppErrorLog.Log($"handling navigation completion for panel '{_panel?.Name ?? "Panel"}'", ex);

            try
            {
                _statusLabel.Text = "Navigation error";
            }
            catch
            {
                // A navigation completion failure should not cascade into another UI exception.
            }
        }
    }

    /// <summary>
    /// Renders a simple local error page inside the panel.
    /// </summary>
    private void ShowErrorHtml(string message)
    {
        _showingErrorPage = true;
        var errorHtml = """
            <!doctype html>
            <html>
            <head>
              <meta charset="utf-8">
              <style>
                body {{
                  align-items: center;
                  background: #02060a;
                  color: #8b9bad;
                  display: flex;
                  font-family: Segoe UI, Arial, sans-serif;
                  height: 100vh;
                  justify-content: center;
                  margin: 0;
                }}
                div {{
                  border: 1px solid #3d4a58;
                  padding: 24px;
                  text-align: center;
                }}
                strong {{
                  color: #ff4d5f;
                  display: block;
                  font-size: 18px;
                  margin-bottom: 8px;
                }}
              </style>
            </head>
            <body>
              <div>
                <strong>Unable to load panel</strong>
                {{message}}
              </div>
            </body>
            </html>
            """.Replace("{{message}}", message, StringComparison.Ordinal);

        if (_webView.CoreWebView2 is not null)
        {
            _webView.NavigateToString(errorHtml);
        }
    }

    /// <summary>
    /// Handles browser-process failures reported by WebView2.
    /// </summary>
    private void OnWebViewProcessFailed(object? sender, CoreWebView2ProcessFailedEventArgs e)
    {
        if (_disposed || !_isActivePanel)
        {
            return;
        }

        var panelName = _panel?.Name ?? "Panel";
        AppErrorLog.LogMessage(
            $"WebView2 process failure in panel '{panelName}'",
            $"Kind: {e.ProcessFailedKind}; Reason: {e.Reason}; ExitCode: {e.ExitCode}");

        if (e.ProcessFailedKind is not (CoreWebView2ProcessFailedKind.BrowserProcessExited or
            CoreWebView2ProcessFailedKind.RenderProcessExited or CoreWebView2ProcessFailedKind.RenderProcessUnresponsive))
        {
            return; // WebView2 handles helper-process recovery itself.
        }

        BrowserProcessFailed |= e.ProcessFailedKind == CoreWebView2ProcessFailedKind.BrowserProcessExited;
        _refreshTimer.Stop();
        _statusLabel.Text = "Browser process failed";
        // A dead browser cannot render an error page. Native status and recovery remain available.
        ScheduleRecovery();
    }

    private void TryShowErrorHtml(string message)
    {
        try
        {
            ShowErrorHtml(message);
        }
        catch (Exception ex)
        {
            AppErrorLog.Log("displaying panel error", ex);
        }
    }

    private void ScheduleRecovery()
    {
        _refreshTimer.Stop();
        if (_disposed || !_isActivePanel || _recovery.IsPending)
        {
            return;
        }

        if (!_automaticRecoveryEnabled || !_recovery.TrySchedule(DateTimeOffset.UtcNow, out var delay))
        {
            _statusLabel.Text = "Failed - use Restart";
            return;
        }

        _statusLabel.Text = $"Retry {_recovery.Attempts}/3 in {delay.TotalSeconds:N0}s";
        _scheduledRecoveryGeneration = _recovery.Generation;
        AppErrorLog.LogMessage($"Recovering panel '{_panel?.Name}'", _statusLabel.Text);
        _recoveryTimer.Interval = (int)delay.TotalMilliseconds;
        _recoveryTimer.Start();
    }

    /// <summary>
    /// Runs a synchronous panel operation through the local diagnostics path.
    /// </summary>
    private void RunPanelAction(Action action, string context)
    {
        if (_disposed)
        {
            return;
        }

        try
        {
            action();
        }
        catch (Exception ex)
        {
            AppErrorLog.Log(context, ex);

            try
            {
                _refreshTimer.Stop();
                _statusLabel.Text = "Panel error";
                ScheduleRecovery();
            }
            catch
            {
                // Avoid cascading failures while already recovering from a panel exception.
            }
        }
    }

    /// <summary>
    /// Releases timers, event handlers, and the hosted WebView2 browser instance.
    /// </summary>
    protected override void Dispose(bool disposing)
    {
        if (disposing && !_disposed)
        {
            _disposed = true;
            _isActivePanel = false;

            _refreshTimer.Stop();
            _recoveryTimer.Stop();
            _recovery.MarkInactive();

            if (_webView.CoreWebView2 is not null)
            {
                _webView.CoreWebView2.ProcessFailed -= OnWebViewProcessFailed;
            }

            _webView.NavigationCompleted -= OnNavigationCompleted;
            _webView.KeyDown -= OnWebViewKeyDown;

            _refreshTimer.Dispose();
            _recoveryTimer.Dispose();
            _panelToolTip.Dispose();
            _webView.Dispose();
        }

        base.Dispose(disposing);
    }

    /// <summary>
    /// Resolves an absolute URL or local URL into a URI WebView2 can navigate.
    /// Local development builds prefer files from the repository so editing local HTML is reflected
    /// immediately. Published builds use files beside the executable to stay portable.
    /// </summary>
    private static Uri ResolvePanelUri(string url)
    {
        if (Uri.TryCreate(url, UriKind.Absolute, out var absoluteUri))
        {
            return absoluteUri;
        }

        var rootRelative = url.StartsWith('/');
        var separatorIndex = url.IndexOf('?');
        var pathPart = separatorIndex >= 0 ? url[..separatorIndex] : url;
        var queryPart = separatorIndex >= 0 ? url[(separatorIndex + 1)..] : string.Empty;
        var relativePath = pathPart.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
        var localRoot = rootRelative ? "wwwroot" : string.Empty;
        var developmentPath = ResolveDevelopmentLocalPath(localRoot, relativePath);
        var runtimePath = Path.Combine(AppContext.BaseDirectory, localRoot, relativePath);

        if (IsDevelopmentOutputDirectory(AppContext.BaseDirectory) &&
            File.Exists(developmentPath))
        {
            runtimePath = developmentPath;
        }
        else if (!File.Exists(runtimePath))
        {
            runtimePath = developmentPath;
        }

        var builder = new UriBuilder(new Uri(runtimePath));

        if (!string.IsNullOrWhiteSpace(queryPart))
        {
            builder.Query = queryPart;
        }

        return builder.Uri;
    }

    /// <summary>
    /// Adds a cache-busting query to local file reloads so WebView2 rereads edited HTML.
    /// </summary>
    private static Uri BuildRefreshUri(Uri uri)
    {
        if (!uri.IsFile)
        {
            return uri;
        }

        var builder = new UriBuilder(uri);
        var query = builder.Query.TrimStart('?');
        var refreshToken = $"nwReload={DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}";
        builder.Query = string.IsNullOrWhiteSpace(query)
            ? refreshToken
            : $"{query}&{refreshToken}";
        return builder.Uri;
    }

    /// <summary>
    /// Builds a repository-local path from a normal bin/Debug or bin/Release output path.
    /// </summary>
    private static string ResolveDevelopmentLocalPath(string localRoot, string relativePath)
    {
        return Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "..",
            "..",
            "..",
            "..",
            "..",
            localRoot,
            relativePath));
    }

    /// <summary>
    /// Detects SDK build output folders so local pages are loaded from the editable source tree.
    /// </summary>
    private static bool IsDevelopmentOutputDirectory(string baseDirectory)
    {
        var normalized = Path.GetFullPath(baseDirectory)
            .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        var segments = normalized.Split(
            [Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar],
            StringSplitOptions.RemoveEmptyEntries);

        for (var index = 0; index < segments.Length - 1; index++)
        {
            if (string.Equals(segments[index], "bin", StringComparison.OrdinalIgnoreCase) &&
                (string.Equals(segments[index + 1], "Debug", StringComparison.OrdinalIgnoreCase) ||
                 string.Equals(segments[index + 1], "Release", StringComparison.OrdinalIgnoreCase)))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Shortcut key data forwarded from WebView2 to the wallboard form.
    /// </summary>
    public sealed class PanelShortcutRequestedEventArgs : EventArgs
    {
        public PanelShortcutRequestedEventArgs(Keys keyCode, Keys modifiers)
        {
            KeyCode = keyCode;
            Modifiers = modifiers;
        }

        public Keys KeyCode { get; }

        public Keys Modifiers { get; }
    }

    public sealed class RecoveryRequestedEventArgs(int generation) : EventArgs
    {
        public int Generation { get; } = generation;
    }
}
