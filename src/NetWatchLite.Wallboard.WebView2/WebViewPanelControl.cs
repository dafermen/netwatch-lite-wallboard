using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;
using WebView2Control = Microsoft.Web.WebView2.WinForms.WebView2;

namespace NetWatchLite.Wallboard.WebView2;

/// <summary>
/// User control that wraps one WebView2 browser instance, title bar, refresh button, and timer.
/// </summary>
internal sealed class WebViewPanelControl : UserControl
{
    private readonly Label _statusLabel = new();
    private readonly WebView2Control _webView = new();
    private readonly System.Windows.Forms.Timer _refreshTimer = new();
    private WallboardPanel? _panel;
    private Uri? _targetUri;

    /// <summary>
    /// Builds the panel title bar, refresh button, and hosted WebView2 control.
    /// </summary>
    public WebViewPanelControl()
    {
        BackColor = Color.Black;
        Dock = DockStyle.Fill;
        Margin = new Padding(4);

        var titleBar = new Panel
        {
            BackColor = Color.FromArgb(12, 17, 23),
            Dock = DockStyle.Top,
            Height = 34,
            Padding = new Padding(10, 0, 6, 0)
        };

        var titleLabel = new Label
        {
            AutoEllipsis = true,
            Dock = DockStyle.Fill,
            Font = new Font("Segoe UI", 9F, FontStyle.Bold),
            ForeColor = Color.White,
            TextAlign = ContentAlignment.MiddleLeft
        };

        _statusLabel.Dock = DockStyle.Right;
        _statusLabel.Font = new Font("Segoe UI", 8F);
        _statusLabel.ForeColor = Color.FromArgb(139, 155, 173);
        _statusLabel.TextAlign = ContentAlignment.MiddleRight;
        _statusLabel.Width = 170;

        var refreshButton = new Button
        {
            Dock = DockStyle.Right,
            FlatStyle = FlatStyle.Flat,
            ForeColor = Color.White,
            Text = "↻",
            Width = 38
        };
        refreshButton.FlatAppearance.BorderColor = Color.FromArgb(61, 74, 88);
        refreshButton.Click += (_, _) => RefreshPanel();

        titleBar.Controls.Add(titleLabel);
        titleBar.Controls.Add(_statusLabel);
        titleBar.Controls.Add(refreshButton);

        _webView.BackColor = Color.Black;
        _webView.DefaultBackgroundColor = Color.Black;
        _webView.Dock = DockStyle.Fill;
        _webView.NavigationCompleted += OnNavigationCompleted;

        Controls.Add(_webView);
        Controls.Add(titleBar);

        _refreshTimer.Tick += (_, _) => RefreshPanel();

        TitleLabel = titleLabel;
    }

    private Label TitleLabel { get; }

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
        _panel = panel;
        _targetUri = ResolvePanelUri(panel.Url);

        TitleLabel.Text = panel.Name;
        _statusLabel.Text = $"{panel.RefreshSeconds}s refresh";

        await _webView.EnsureCoreWebView2Async(environment);

        if (cancellationToken.IsCancellationRequested)
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
    public void RefreshPanel()
    {
        if (_webView.CoreWebView2 is null || _targetUri is null)
        {
            return;
        }

        _statusLabel.Text = "Refreshing...";
        _webView.CoreWebView2.Navigate(_targetUri.ToString());
    }

    /// <summary>
    /// Stops the independent refresh timer for this panel.
    /// </summary>
    public void StopTimers()
    {
        _refreshTimer.Stop();
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
    }

    /// <summary>
    /// Starts the panel-specific refresh timer.
    /// </summary>
    /// <param name="refreshSeconds">Refresh interval in seconds.</param>
    private void ScheduleRefresh(int refreshSeconds)
    {
        _refreshTimer.Stop();
        _refreshTimer.Interval = Math.Max(1, refreshSeconds) * 1000;
        _refreshTimer.Start();
    }

    /// <summary>
    /// Navigates the WebView2 control to the resolved target URI.
    /// </summary>
    private void Navigate()
    {
        if (_webView.CoreWebView2 is null || _targetUri is null)
        {
            return;
        }

        _statusLabel.Text = "Loading...";
        _webView.CoreWebView2.Navigate(_targetUri.ToString());
    }

    /// <summary>
    /// Updates status text after navigation and renders a friendly error page on failure.
    /// </summary>
    /// <param name="sender">Event sender.</param>
    /// <param name="e">Navigation result details.</param>
    private void OnNavigationCompleted(object? sender, CoreWebView2NavigationCompletedEventArgs e)
    {
        if (e.IsSuccess)
        {
            _statusLabel.Text = $"Loaded {DateTime.Now:T}";
            return;
        }

        var name = _panel?.Name ?? "Panel";
        var message = System.Net.WebUtility.HtmlEncode($"{name} failed to load: {e.WebErrorStatus}");
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
        _webView.NavigateToString(errorHtml);
        _statusLabel.Text = "Load failed";
    }

    /// <summary>
    /// Resolves an absolute URL or root-relative sample URL into a URI WebView2 can navigate.
    /// </summary>
    /// <param name="url">Panel URL from JSON.</param>
    /// <returns>Absolute URI for WebView2 navigation.</returns>
    private static Uri ResolvePanelUri(string url)
    {
        if (Uri.TryCreate(url, UriKind.Absolute, out var absoluteUri))
        {
            return absoluteUri;
        }

        var separatorIndex = url.IndexOf('?');
        var pathPart = separatorIndex >= 0 ? url[..separatorIndex] : url;
        var queryPart = separatorIndex >= 0 ? url[(separatorIndex + 1)..] : string.Empty;
        var relativePath = pathPart.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
        var runtimePath = Path.Combine(AppContext.BaseDirectory, "wwwroot", relativePath);

        if (!File.Exists(runtimePath))
        {
            runtimePath = Path.GetFullPath(Path.Combine(
                AppContext.BaseDirectory,
                "..",
                "..",
                "..",
                "..",
                "..",
                "wwwroot",
                relativePath));
        }

        var builder = new UriBuilder(new Uri(runtimePath));

        if (!string.IsNullOrWhiteSpace(queryPart))
        {
            builder.Query = queryPart;
        }

        return builder.Uri;
    }
}
