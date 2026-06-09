using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;
using System.Media;
using System.Runtime.InteropServices;
using System.Text.Json;
using WebView2Control = Microsoft.Web.WebView2.WinForms.WebView2;

namespace NetWatchLite.Wallboard.WebView2;

/// <summary>
/// User control that wraps one WebView2 browser instance, title bar, refresh button, timers,
/// and optional native alarm chrome. The main form treats this control as one panel slot;
/// this class owns the browser-specific details for that slot.
/// </summary>
internal sealed class WebViewPanelControl : UserControl
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly Panel _alarmBanner = new();
    private readonly Label _alarmTitleLabel = new();
    private readonly Label _alarmDetailsLabel = new();
    private readonly Button _alarmSilenceButton = new();
    private readonly Button _scrapingToggleButton = new();
    private readonly Label _statusLabel = new();
    private readonly WebView2Control _webView = new();
    private readonly System.Windows.Forms.Timer _alarmPollTimer = new();
    private readonly System.Windows.Forms.Timer _alarmPulseTimer = new();
    private readonly System.Windows.Forms.Timer _refreshTimer = new();
    private WallboardPanel? _panel;
    private bool _alarmActive;
    private bool _alarmPulseOn;
    private bool _alarmSilenced;
    private bool _alarmSoundEnabled = true;
    private bool _isActivePanel;
    private bool _monitoringAvailable;
    private bool _scrapingPaused;
    private Color _criticalAlarmColor = Color.FromArgb(204, 18, 32);
    private Color _warningAlarmColor = Color.FromArgb(204, 103, 0);
    private Color _infoAlarmColor = Color.FromArgb(0, 92, 138);
    private string _alarmSeverity = "warning";
    private string _alarmSignature = string.Empty;
    private string _alarmSoundName = "Exclamation";
    private TimeSpan _alarmSoundInterval = TimeSpan.FromSeconds(5);
    private DateTime _lastAlarmSoundUtc = DateTime.MinValue;
    private Uri? _targetUri;

    /// <summary>
    /// Builds the panel title bar, refresh button, hosted WebView2 control, and alarm banner.
    /// Timers are created once with event handlers here, then configured per panel when LoadPanelAsync runs.
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
            Text = "R",
            Width = 38
        };
        refreshButton.FlatAppearance.BorderColor = Color.FromArgb(61, 74, 88);
        refreshButton.Click += (_, _) => RefreshPanel();

        _scrapingToggleButton.BackColor = Color.FromArgb(23, 29, 36);
        _scrapingToggleButton.Dock = DockStyle.Right;
        _scrapingToggleButton.FlatStyle = FlatStyle.Flat;
        _scrapingToggleButton.Font = new Font("Segoe UI", 8F, FontStyle.Bold);
        _scrapingToggleButton.ForeColor = Color.White;
        _scrapingToggleButton.Text = "Stop Scraping";
        _scrapingToggleButton.Visible = false;
        _scrapingToggleButton.Width = 116;
        _scrapingToggleButton.FlatAppearance.BorderColor = Color.FromArgb(61, 74, 88);
        _scrapingToggleButton.Click += (_, _) => ToggleScraping();

        titleBar.Controls.Add(titleLabel);
        titleBar.Controls.Add(_statusLabel);
        titleBar.Controls.Add(_scrapingToggleButton);
        titleBar.Controls.Add(refreshButton);

        _alarmBanner.BackColor = Color.FromArgb(128, 8, 16);
        _alarmBanner.Dock = DockStyle.Top;
        _alarmBanner.Height = 64;
        _alarmBanner.Padding = new Padding(14, 7, 14, 7);
        _alarmBanner.Visible = false;

        _alarmTitleLabel.AutoEllipsis = true;
        _alarmTitleLabel.Dock = DockStyle.Top;
        _alarmTitleLabel.Font = new Font("Segoe UI", 11F, FontStyle.Bold);
        _alarmTitleLabel.ForeColor = Color.White;
        _alarmTitleLabel.Height = 24;
        _alarmTitleLabel.Text = "PLC ALARM DETECTED";
        _alarmTitleLabel.TextAlign = ContentAlignment.MiddleLeft;

        _alarmDetailsLabel.AutoEllipsis = true;
        _alarmDetailsLabel.Dock = DockStyle.Fill;
        _alarmDetailsLabel.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
        _alarmDetailsLabel.ForeColor = Color.FromArgb(255, 232, 156);
        _alarmDetailsLabel.TextAlign = ContentAlignment.MiddleLeft;

        _alarmSilenceButton.BackColor = Color.FromArgb(23, 29, 36);
        _alarmSilenceButton.Dock = DockStyle.Right;
        _alarmSilenceButton.FlatStyle = FlatStyle.Flat;
        _alarmSilenceButton.Font = new Font("Segoe UI", 8F, FontStyle.Bold);
        _alarmSilenceButton.ForeColor = Color.White;
        _alarmSilenceButton.Text = "Silence";
        _alarmSilenceButton.Width = 112;
        _alarmSilenceButton.FlatAppearance.BorderColor = Color.FromArgb(255, 190, 80);
        _alarmSilenceButton.Click += (_, _) => SilenceAlarmSound();

        _alarmBanner.Controls.Add(_alarmDetailsLabel);
        _alarmBanner.Controls.Add(_alarmTitleLabel);
        _alarmBanner.Controls.Add(_alarmSilenceButton);

        _webView.BackColor = Color.Black;
        _webView.DefaultBackgroundColor = Color.Black;
        _webView.Dock = DockStyle.Fill;
        _webView.NavigationCompleted += OnNavigationCompleted;
        _webView.KeyDown += OnWebViewKeyDown;

        Controls.Add(_webView);
        Controls.Add(_alarmBanner);
        Controls.Add(titleBar);

        _refreshTimer.Tick += (_, _) => RefreshPanel();
        _alarmPollTimer.Interval = 3000;
        _alarmPollTimer.Tick += (_, _) => _ = DetectConfiguredAlertsSafelyAsync();
        _alarmPulseTimer.Interval = 650;
        _alarmPulseTimer.Tick += (_, _) => RunPanelAction(PulseAlarmBanner, "pulsing alarm banner");

        TitleLabel = titleLabel;
    }

    private Label TitleLabel { get; }

    /// <summary>
    /// Raised when the operator pauses or resumes DOM scraping for this panel slot.
    /// The main form stores the value so it survives layout changes and panel recreation.
    /// </summary>
    public event EventHandler<bool>? ScrapingPausedChanged;

    /// <summary>
    /// Raised when the hosted WebView2 receives a wallboard-level shortcut key.
    /// </summary>
    public event EventHandler<PanelShortcutRequestedEventArgs>? ShortcutRequested;

    /// <summary>
    /// Initializes WebView2 and navigates to the configured panel URL.
    /// This method is called every time the main form renders a visible panel slot. It resolves the URL,
    /// configures timers, initializes the browser, navigates, and starts the independent refresh cycle.
    /// </summary>
    /// <param name="panel">Panel declaration to render.</param>
    /// <param name="alarmSoundName">Built-in Windows sound name used for audible alarm alerts.</param>
    /// <param name="severityColors">Configured alarm colors by severity.</param>
    /// <param name="scrapingPaused">Whether DOM monitoring should start paused for this panel.</param>
    /// <param name="environment">Shared WebView2 environment used by all panels.</param>
    /// <param name="cancellationToken">Token used to cancel initialization.</param>
    public async Task LoadPanelAsync(
        WallboardPanel panel,
        string alarmSoundName,
        AlarmSeverityColors severityColors,
        bool scrapingPaused,
        CoreWebView2Environment environment,
        CancellationToken cancellationToken = default)
    {
        _isActivePanel = true;
        _panel = panel;
        _alarmSoundName = NormalizeAlarmSoundName(alarmSoundName);
        ApplySeverityColors(severityColors);
        _targetUri = ResolvePanelUri(panel.Url);
        ConfigureMonitoringTimer(panel.Monitoring, scrapingPaused);

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
        RunPanelAction(
            () =>
            {
                if (_webView.CoreWebView2 is null || _targetUri is null)
                {
                    return;
                }

                if (_panel is not null)
                {
                    _targetUri = ResolvePanelUri(_panel.Url);
                }

                _statusLabel.Text = "Refreshing...";
                ClearAlarmState();
                _webView.CoreWebView2.Navigate(BuildRefreshUri(_targetUri).ToString());
            },
            $"refreshing panel '{_panel?.Name ?? "Panel"}'");
    }

    /// <summary>
    /// Stops all timers owned by this panel.
    /// The main form calls this before removing controls so old refresh or alarm callbacks cannot run
    /// against a panel that is no longer visible.
    /// </summary>
    public void StopTimers()
    {
        _isActivePanel = false;
        _refreshTimer.Stop();
        _alarmPollTimer.Stop();
        _alarmPulseTimer.Stop();
        ClearAlarmState();
    }

    /// <summary>
    /// Displays a panel-level failure page when initialization fails before WebView2 can navigate.
    /// </summary>
    /// <param name="panelName">Panel name shown to the operator.</param>
    /// <param name="exception">Exception that prevented the panel from loading.</param>
    public void ShowPanelError(string panelName, Exception exception)
    {
        _statusLabel.Text = "Panel error";
        _refreshTimer.Stop();
        _alarmPollTimer.Stop();
        _alarmPulseTimer.Stop();
        ClearAlarmState();

        var message = System.Net.WebUtility.HtmlEncode(
            $"{panelName} could not be initialized: {exception.Message}");
        ShowErrorHtml(message);
    }

    /// <summary>
    /// Applies kiosk-style WebView2 settings suitable for operations screens.
    /// Operators normally interact with the monitored application itself, not browser chrome, so default
    /// context menus, DevTools, status bar, zoom controls, and browser accelerator keys are disabled.
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
    /// <param name="sender">WebView2 control.</param>
    /// <param name="e">Key details.</param>
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
    /// <param name="refreshSeconds">Refresh interval in seconds.</param>
    private void ScheduleRefresh(int refreshSeconds)
    {
        _refreshTimer.Stop();
        _refreshTimer.Interval = Math.Max(1, refreshSeconds) * 1000;
        _refreshTimer.Start();
    }

    /// <summary>
    /// Navigates the WebView2 control to the resolved target URI.
    /// Any active alarm is cleared before navigation because the old DOM is about to be replaced.
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
                ClearAlarmState();
                _webView.CoreWebView2.Navigate(BuildRefreshUri(_targetUri).ToString());
            },
            $"navigating panel '{_panel?.Name ?? "Panel"}'");
    }

    /// <summary>
    /// Updates status text after navigation and renders a friendly error page on failure.
    /// DOM monitoring starts only after successful navigation because ExecuteScriptAsync needs a loaded page.
    /// </summary>
    /// <param name="sender">Event sender.</param>
    /// <param name="e">Navigation result details.</param>
    private void OnNavigationCompleted(object? sender, CoreWebView2NavigationCompletedEventArgs e)
    {
        try
        {
            if (!_isActivePanel)
            {
                _alarmPollTimer.Stop();
                ClearAlarmState();
                return;
            }

            if (e.IsSuccess)
            {
                _statusLabel.Text = $"Loaded {DateTime.Now:T}";

                if (_panel?.Monitoring?.Enabled == true)
                {
                    if (_scrapingPaused)
                    {
                        _alarmPollTimer.Stop();
                        ClearAlarmState();
                        _statusLabel.Text = "Scraping stopped";
                    }
                    else
                    {
                        // Start recurring DOM checks and also run one immediate check so a currently visible
                        // alarm appears without waiting for the first timer interval.
                        _statusLabel.Text = $"Scraping active {DateTime.Now:T}";
                        _alarmPollTimer.Start();
                        _ = DetectConfiguredAlertsSafelyAsync();
                    }
                }
                else
                {
                    _alarmPollTimer.Stop();
                    ClearAlarmState();
                }

                return;
            }

            _alarmPollTimer.Stop();
            ClearAlarmState();

            var name = _panel?.Name ?? "Panel";
            var message = System.Net.WebUtility.HtmlEncode($"{name} failed to load: {e.WebErrorStatus}");
            ShowErrorHtml(message);
            _statusLabel.Text = "Load failed";
        }
        catch (Exception ex)
        {
            AppErrorLog.Log($"handling navigation completion for panel '{_panel?.Name ?? "Panel"}'", ex);

            try
            {
                _alarmPollTimer.Stop();
                ClearAlarmState();
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
    /// <param name="message">HTML-encoded message to display.</param>
    private void ShowErrorHtml(string message)
    {
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
    /// Configures the panel-specific DOM monitoring timer.
    /// This only prepares interval and sound settings; the timer starts after successful navigation.
    /// </summary>
    /// <param name="monitoring">Optional monitoring settings for this panel.</param>
    /// <param name="scrapingPaused">Whether monitoring should start paused.</param>
    private void ConfigureMonitoringTimer(PanelMonitoringOptions? monitoring, bool scrapingPaused)
    {
        _alarmPollTimer.Stop();
        _monitoringAvailable = monitoring?.Enabled == true && monitoring.Rules.Count > 0;
        _scrapingPaused = _monitoringAvailable && scrapingPaused;
        _alarmSoundEnabled = monitoring?.SoundEnabled ?? true;
        _alarmSoundInterval = TimeSpan.FromSeconds(Math.Max(1, monitoring?.RepeatSoundSeconds ?? 5));
        _alarmPollTimer.Interval = Math.Max(1, monitoring?.PollSeconds ?? 3) * 1000;
        UpdateScrapingButton();
    }

    /// <summary>
    /// Polls the hosted page DOM using the panel's configured monitoring rules.
    /// The important detail: this is not an external HTTP scraper. The code serializes rules, injects
    /// them into a small JavaScript function, and asks WebView2 to run that function inside the page
    /// that is already loaded and rendered.
    /// </summary>
    private async Task DetectConfiguredAlertsAsync()
    {
        var monitoring = _panel?.Monitoring;

        if (!_isActivePanel ||
            _scrapingPaused ||
            _webView.CoreWebView2 is null ||
            monitoring?.Enabled != true ||
            monitoring.Rules.Count == 0)
        {
            ClearAlarmState();
            return;
        }

        try
        {
            var rulesJson = JsonSerializer.Serialize(monitoring.Rules, JsonOptions);
            var resultJson = await _webView.CoreWebView2.ExecuteScriptAsync("""
                (() => {
                  const rules = __RULES__;
                  const severityRank = { critical: 3, warning: 2, info: 1 };

                  // Rules may arrive with camelCase or PascalCase property names depending on how
                  // they were serialized or manually edited. Accept both to keep the JSON forgiving.
                  const valueOf = (object, camelName, pascalName) => object?.[camelName] ?? object?.[pascalName];
                  const normalize = value => String(value ?? '').trim().replace(/\s+/g, ' ');
                  const queryAll = selector => {
                    try {
                      return Array.from(document.querySelectorAll(selector));
                    } catch {
                      // A malformed selector should not crash monitoring for the whole panel.
                      return [];
                    }
                  };
                  const isVisible = element => {
                    if (!element) return false;
                    const style = getComputedStyle(element);
                    return style.display !== 'none' &&
                      style.visibility !== 'hidden' &&
                      style.opacity !== '0' &&
                      element.getClientRects().length > 0;
                  };
                  const includesText = (element, needle) => {
                    if (!needle) return true;
                    return normalize(element.textContent)
                      .toLowerCase()
                      .includes(String(needle).toLowerCase());
                  };
                  const includesClass = (element, needle) => {
                    if (!needle) return true;
                    return Array.from(element.classList)
                      .some(className => className.toLowerCase() === String(needle).toLowerCase());
                  };
                  const collectDetails = (rule, matchedElements) => {
                    const detailElements = rule.detailsSelector
                      ? queryAll(rule.detailsSelector).filter(isVisible)
                      : matchedElements;
                    const details = detailElements
                      .map(element => normalize(element.textContent))
                      .filter(Boolean);
                    return details.length > 0 ? details : [rule.name || 'DOM Alert'];
                  };

                  const matches = [];

                  for (const rule of rules) {
                    const name = normalize(valueOf(rule, 'name', 'Name') || 'DOM Alert');
                    const type = normalize(valueOf(rule, 'type', 'Type') || 'exists');
                    const selector = valueOf(rule, 'selector', 'Selector');
                    const contains = valueOf(rule, 'contains', 'Contains');
                    const detailsSelector = valueOf(rule, 'detailsSelector', 'DetailsSelector');
                    const severity = valueOf(rule, 'severity', 'Severity') || 'warning';
                    const soundEnabled = valueOf(rule, 'soundEnabled', 'SoundEnabled');
                    const elements = queryAll(selector).filter(isVisible);
                    const matchedElements = elements.filter(element => {
                      if (type === 'domText') {
                        return includesText(element, contains);
                      }

                      if (type === 'domClass') {
                        return includesClass(element, contains);
                      }

                      return includesText(element, contains);
                    });

                    if (matchedElements.length === 0) continue;

                    matches.push({
                      name,
                      severity,
                      soundEnabled,
                      details: collectDetails({ name, detailsSelector }, matchedElements)
                    });
                  }

                  if (matches.length === 0) {
                    return { active: false, title: '', severity: 'info', soundEnabled: false, alarms: [] };
                  }

                  // The C# side shows one banner. Sorting here makes the banner title/severity reflect
                  // the most urgent active rule while still returning details from every matching rule.
                  matches.sort((left, right) =>
                    (severityRank[right.severity] || 2) - (severityRank[left.severity] || 2));

                  const severity = matches[0].severity || 'warning';
                  const soundEnabled = matches.some(match => match.soundEnabled !== false);
                  const alarms = matches.flatMap(match =>
                    match.details.map(detail => `${match.name}: ${detail}`));

                  return {
                    active: true,
                    title: matches[0].name || 'DOM Alert',
                    severity,
                    soundEnabled,
                    alarms
                  };
                })();
                """.Replace("__RULES__", rulesJson, StringComparison.Ordinal));

            // ExecuteScriptAsync returns JSON text. Deserialize the small snapshot into a DTO so the
            // native UI code can stay strongly typed.
            var snapshot = JsonSerializer.Deserialize<AlarmSnapshot>(resultJson, JsonOptions);

            if (!_isActivePanel)
            {
                ClearAlarmState();
                return;
            }

            if (snapshot?.Active == true)
            {
                ShowAlarmState(snapshot);
                return;
            }

            ClearAlarmState();
            SetScrapingCheckedStatus();
        }
        catch (InvalidOperationException)
        {
            ClearAlarmState();
        }
        catch (JsonException)
        {
            ClearAlarmState();
        }
        catch (COMException)
        {
            ClearAlarmState();
        }
        catch (Exception ex)
        {
            AppErrorLog.Log($"detecting configured alerts for panel '{_panel?.Name ?? "Panel"}'", ex);
            ClearAlarmState();
        }
    }

    /// <summary>
    /// Runs DOM detection from timer/event callbacks without exposing async exceptions to WinForms.
    /// </summary>
    private async Task DetectConfiguredAlertsSafelyAsync()
    {
        try
        {
            await DetectConfiguredAlertsAsync();
        }
        catch (Exception ex)
        {
            AppErrorLog.Log($"running alarm polling for panel '{_panel?.Name ?? "Panel"}'", ex);
            ClearAlarmState();
        }
    }

    /// <summary>
    /// Shows the native wallboard alarm layer for configured DOM alerts.
    /// A native banner is used instead of modifying the monitored page so the app can alert even when
    /// the underlying site is remote, authenticated, or outside our control.
    /// </summary>
    /// <param name="snapshot">Alert details extracted from the WebView DOM.</param>
    private void ShowAlarmState(AlarmSnapshot snapshot)
    {
        if (!_isActivePanel)
        {
            return;
        }

        var alarms = snapshot.Alarms
            .Where(alarm => !string.IsNullOrWhiteSpace(alarm))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        var detailText = alarms.Length > 0
            ? string.Join("  |  ", alarms)
            : snapshot.Title;
        var signature = $"{snapshot.Severity}\n{snapshot.Title}\n{string.Join("\n", alarms)}";

        if (!string.Equals(_alarmSignature, signature, StringComparison.Ordinal))
        {
            // A changed signature means this is a materially different alarm. Reset the repeat-sound
            // interval, but keep the operator's manual sound mute until they explicitly enable sound.
            _alarmSignature = signature;
            _lastAlarmSoundUtc = DateTime.MinValue;
        }

        _alarmActive = true;
        _alarmSeverity = NormalizeSeverity(snapshot.Severity);
        _alarmSoundEnabled = _panel?.Monitoring?.SoundEnabled == true && snapshot.SoundEnabled;
        _alarmBanner.Visible = true;
        _alarmTitleLabel.Text = $"{FormatSeverity(_alarmSeverity)} - {snapshot.Title} - {_panel?.Name ?? "Panel"}";
        _alarmDetailsLabel.Text = detailText;
        _alarmSilenceButton.Text = _alarmSilenced ? "Enable Sound" : "Silence";
        _statusLabel.Text = $"Alarm {DateTime.Now:T}";
        Padding = new Padding(3);
        BackColor = GetAlarmBorderColor(_alarmSeverity, pulseOn: true);

        if (!_alarmPulseTimer.Enabled)
        {
            _alarmPulseTimer.Start();
        }

        PlayAlarmSound();
    }

    /// <summary>
    /// Hides native alarm chrome when the source page no longer reports a matching rule.
    /// </summary>
    private void ClearAlarmState()
    {
        if (!_alarmActive && !_alarmBanner.Visible)
        {
            return;
        }

        _alarmActive = false;
        _alarmPulseTimer.Stop();
        _alarmPulseOn = false;
        _alarmSeverity = "warning";
        _alarmSignature = string.Empty;
        _alarmBanner.Visible = false;
        Padding = Padding.Empty;
        BackColor = Color.Black;
    }

    /// <summary>
    /// Updates the panel status after a successful DOM polling pass with no active alarm.
    /// </summary>
    private void SetScrapingCheckedStatus()
    {
        if (_panel?.Monitoring?.Enabled == true && !_scrapingPaused)
        {
            _statusLabel.Text = $"Scraping checked {DateTime.Now:T}";
        }
    }

    /// <summary>
    /// Pulses the native alarm chrome without changing the hosted page content.
    /// </summary>
    private void PulseAlarmBanner()
    {
        if (!_alarmActive)
        {
            return;
        }

        _alarmPulseOn = !_alarmPulseOn;
        _alarmBanner.BackColor = GetAlarmBannerColor(_alarmSeverity, _alarmPulseOn);
        BackColor = GetAlarmBorderColor(_alarmSeverity, _alarmPulseOn);
    }

    /// <summary>
    /// Plays a short Windows notification sound at a controlled interval.
    /// This method respects both the silence button and the configured repeat interval.
    /// </summary>
    private void PlayAlarmSound()
    {
        if (!_isActivePanel || _alarmSilenced || !_alarmSoundEnabled)
        {
            return;
        }

        var nowUtc = DateTime.UtcNow;

        if (nowUtc - _lastAlarmSoundUtc < _alarmSoundInterval)
        {
            return;
        }

        // The current alert tone is intentionally centralized here. Settings stores a friendly
        // SystemSounds name, and this switch maps that JSON value to the actual Windows sound.
        _lastAlarmSoundUtc = nowUtc;
        PlayConfiguredSystemSound();
    }

    /// <summary>
    /// Toggles alarm sound for this panel while keeping the visual alarm visible.
    /// Once silenced, sound stays muted across page refreshes until the operator enables it again.
    /// </summary>
    private void SilenceAlarmSound()
    {
        _alarmSilenced = !_alarmSilenced;
        _alarmSilenceButton.Text = _alarmSilenced ? "Enable Sound" : "Silence";
        _lastAlarmSoundUtc = _alarmSilenced ? _lastAlarmSoundUtc : DateTime.MinValue;
        _statusLabel.Text = _alarmSilenced
            ? $"Alarm sound muted {DateTime.Now:T}"
            : $"Alarm sound enabled {DateTime.Now:T}";

        if (!_alarmSilenced && _alarmActive)
        {
            PlayAlarmSound();
        }
    }

    /// <summary>
    /// Pauses or resumes DOM monitoring for this panel without changing wallboard.json.
    /// This is useful when operators need to temporarily stop selector checks during maintenance,
    /// testing, or a known noisy alarm while keeping the page itself visible and refreshing.
    /// </summary>
    private void ToggleScraping()
    {
        if (!_monitoringAvailable)
        {
            return;
        }

        _scrapingPaused = !_scrapingPaused;
        ScrapingPausedChanged?.Invoke(this, _scrapingPaused);
        UpdateScrapingButton();

        if (_scrapingPaused)
        {
            _alarmPollTimer.Stop();
            ClearAlarmState();
            _statusLabel.Text = $"Scraping stopped {DateTime.Now:T}";
            return;
        }

        _statusLabel.Text = $"Scraping active {DateTime.Now:T}";

        if (_webView.CoreWebView2 is not null)
        {
            _alarmPollTimer.Start();
            _ = DetectConfiguredAlertsSafelyAsync();
        }
    }

    /// <summary>
    /// Keeps the scraping toggle button aligned with the panel's monitoring state.
    /// </summary>
    private void UpdateScrapingButton()
    {
        _scrapingToggleButton.Visible = _monitoringAvailable;
        _scrapingToggleButton.Enabled = _monitoringAvailable;
        _scrapingToggleButton.Text = _scrapingPaused ? "Start Scraping" : "Stop Scraping";
        _scrapingToggleButton.BackColor = _scrapingPaused
            ? Color.FromArgb(0, 80, 96)
            : Color.FromArgb(23, 29, 36);
    }

    /// <summary>
    /// Plays the Windows SystemSounds value selected in wallboard settings.
    /// </summary>
    private void PlayConfiguredSystemSound()
    {
        switch (_alarmSoundName)
        {
            case "Asterisk":
                SystemSounds.Asterisk.Play();
                break;
            case "Beep":
                SystemSounds.Beep.Play();
                break;
            case "Hand":
                SystemSounds.Hand.Play();
                break;
            case "Question":
                SystemSounds.Question.Play();
                break;
            default:
                SystemSounds.Exclamation.Play();
                break;
        }
    }

    /// <summary>
    /// Converts saved alarm sound text into one of the supported built-in Windows sounds.
    /// </summary>
    /// <param name="alarmSoundName">Configured sound name.</param>
    /// <returns>Supported sound name.</returns>
    private static string NormalizeAlarmSoundName(string? alarmSoundName)
    {
        var normalized = alarmSoundName?.Trim();

        if (string.Equals(normalized, "Asterisk", StringComparison.OrdinalIgnoreCase))
        {
            return "Asterisk";
        }

        if (string.Equals(normalized, "Beep", StringComparison.OrdinalIgnoreCase))
        {
            return "Beep";
        }

        if (string.Equals(normalized, "Hand", StringComparison.OrdinalIgnoreCase))
        {
            return "Hand";
        }

        if (string.Equals(normalized, "Question", StringComparison.OrdinalIgnoreCase))
        {
            return "Question";
        }

        return "Exclamation";
    }

    /// <summary>
    /// Handles browser-process failures reported by WebView2. These events are especially useful for
    /// diagnosing unexpected blank panels or crashes inside the embedded browser runtime.
    /// </summary>
    /// <param name="sender">Event sender.</param>
    /// <param name="e">WebView2 process failure details.</param>
    private void OnWebViewProcessFailed(object? sender, CoreWebView2ProcessFailedEventArgs e)
    {
        var panelName = _panel?.Name ?? "Panel";
        AppErrorLog.LogMessage(
            $"WebView2 process failure in panel '{panelName}'",
            $"Kind: {e.ProcessFailedKind}; Reason: {e.Reason}; ExitCode: {e.ExitCode}");

        RunPanelAction(
            () =>
            {
                _refreshTimer.Stop();
                _alarmPollTimer.Stop();
                _alarmPulseTimer.Stop();
                ClearAlarmState();
                _statusLabel.Text = "Browser process failed";
                ShowErrorHtml(System.Net.WebUtility.HtmlEncode(
                    $"{panelName} browser process failed. See diagnostic log: {AppErrorLog.LogFilePath}"));
            },
            $"handling WebView2 process failure for panel '{panelName}'");
    }

    /// <summary>
    /// Runs a synchronous panel operation through the local diagnostics path.
    /// </summary>
    /// <param name="action">Action to execute.</param>
    /// <param name="context">Human-readable context for the log.</param>
    private void RunPanelAction(Action action, string context)
    {
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
                _alarmPollTimer.Stop();
                _alarmPulseTimer.Stop();
                _statusLabel.Text = "Panel error";
            }
            catch
            {
                // Avoid cascading failures while already recovering from a panel exception.
            }
        }
    }

    /// <summary>
    /// Converts alert severity into supported visual levels.
    /// </summary>
    /// <param name="severity">Severity returned by a matched rule.</param>
    /// <returns>critical, warning, or info.</returns>
    private static string NormalizeSeverity(string? severity)
    {
        return severity?.Trim().ToLowerInvariant() switch
        {
            "critical" => "critical",
            "info" => "info",
            _ => "warning"
        };
    }

    /// <summary>
    /// Formats severity for the alarm banner title.
    /// </summary>
    /// <param name="severity">Normalized severity.</param>
    /// <returns>Display label.</returns>
    private static string FormatSeverity(string severity)
    {
        return severity switch
        {
            "critical" => "CRITICAL ALERT",
            "info" => "INFO ALERT",
            _ => "WARNING ALERT"
        };
    }

    /// <summary>
    /// Gets the pulsing banner background color for one severity.
    /// </summary>
    /// <param name="severity">Normalized severity.</param>
    /// <param name="pulseOn">Whether the pulse is in its bright phase.</param>
    /// <returns>Banner color.</returns>
    private Color GetAlarmBannerColor(string severity, bool pulseOn)
    {
        var color = GetConfiguredSeverityColor(severity);
        return pulseOn ? color : ScaleColor(color, 0.45);
    }

    /// <summary>
    /// Gets the pulsing border color for one severity.
    /// </summary>
    /// <param name="severity">Normalized severity.</param>
    /// <param name="pulseOn">Whether the pulse is in its bright phase.</param>
    /// <returns>Border color.</returns>
    private Color GetAlarmBorderColor(string severity, bool pulseOn)
    {
        var color = GetConfiguredSeverityColor(severity);
        return pulseOn ? ScaleColor(color, 1.25) : ScaleColor(color, 0.75);
    }

    /// <summary>
    /// Applies configured severity colors to this panel with safe fallbacks.
    /// </summary>
    /// <param name="colors">Configured colors.</param>
    private void ApplySeverityColors(AlarmSeverityColors? colors)
    {
        _criticalAlarmColor = ParseHexColor(colors?.Critical, Color.FromArgb(204, 18, 32));
        _warningAlarmColor = ParseHexColor(colors?.Warning, Color.FromArgb(204, 103, 0));
        _infoAlarmColor = ParseHexColor(colors?.Info, Color.FromArgb(0, 92, 138));
    }

    /// <summary>
    /// Gets the configured base color for one normalized severity.
    /// </summary>
    /// <param name="severity">Normalized severity.</param>
    /// <returns>Configured severity color.</returns>
    private Color GetConfiguredSeverityColor(string severity)
    {
        return severity switch
        {
            "critical" => _criticalAlarmColor,
            "info" => _infoAlarmColor,
            _ => _warningAlarmColor
        };
    }

    /// <summary>
    /// Parses a #RRGGBB color with fallback.
    /// </summary>
    /// <param name="hexColor">Configured hex color.</param>
    /// <param name="fallback">Fallback color.</param>
    /// <returns>Parsed color.</returns>
    private static Color ParseHexColor(string? hexColor, Color fallback)
    {
        try
        {
            return string.IsNullOrWhiteSpace(hexColor)
                ? fallback
                : ColorTranslator.FromHtml(hexColor);
        }
        catch
        {
            return fallback;
        }
    }

    /// <summary>
    /// Lightens or darkens a color while clamping RGB values to valid byte ranges.
    /// </summary>
    /// <param name="color">Base color.</param>
    /// <param name="factor">Scale factor.</param>
    /// <returns>Scaled color.</returns>
    private static Color ScaleColor(Color color, double factor)
    {
        return Color.FromArgb(
            ClampColor(color.R * factor),
            ClampColor(color.G * factor),
            ClampColor(color.B * factor));
    }

    /// <summary>
    /// Clamps a scaled color channel.
    /// </summary>
    /// <param name="value">Scaled channel value.</param>
    /// <returns>Color channel.</returns>
    private static int ClampColor(double value)
    {
        return Math.Clamp((int)Math.Round(value), 0, 255);
    }

    /// <summary>
    /// Resolves an absolute URL or local URL into a URI WebView2 can navigate.
    /// Local development builds prefer files from the repository so editing docs/*.html is reflected
    /// immediately. Published builds use files beside the executable to stay portable.
    /// </summary>
    /// <param name="url">Panel URL from JSON.</param>
    /// <returns>Absolute URI for WebView2 navigation.</returns>
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
    /// <param name="uri">Current panel URI.</param>
    /// <returns>URI to navigate for a manual or timed refresh.</returns>
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
    /// <param name="localRoot">Optional local root such as wwwroot.</param>
    /// <param name="relativePath">Relative panel path.</param>
    /// <returns>Expected source-tree file path.</returns>
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
    /// <param name="baseDirectory">Application base directory.</param>
    /// <returns>True when the app appears to be running from bin/Debug or bin/Release.</returns>
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
    /// Small DTO returned by the JavaScript DOM scan.
    /// This keeps the C# alarm rendering code independent from raw JavaScript object shapes.
    /// </summary>
    private sealed class AlarmSnapshot
    {
        public bool Active { get; set; }

        public string Title { get; set; } = "DOM Alert";

        public string Severity { get; set; } = "warning";

        public bool SoundEnabled { get; set; } = true;

        public string[] Alarms { get; set; } = [];
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
}
