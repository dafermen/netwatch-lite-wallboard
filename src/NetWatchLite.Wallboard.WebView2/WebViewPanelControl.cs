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
        PropertyNameCaseInsensitive = true
    };

    private readonly Panel _alarmBanner = new();
    private readonly Label _alarmTitleLabel = new();
    private readonly Label _alarmDetailsLabel = new();
    private readonly Button _alarmSilenceButton = new();
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
    private string _alarmSeverity = "warning";
    private string _alarmSignature = string.Empty;
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

        titleBar.Controls.Add(titleLabel);
        titleBar.Controls.Add(_statusLabel);
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
        _alarmSilenceButton.Width = 88;
        _alarmSilenceButton.FlatAppearance.BorderColor = Color.FromArgb(255, 190, 80);
        _alarmSilenceButton.Click += (_, _) => SilenceAlarmSound();

        _alarmBanner.Controls.Add(_alarmDetailsLabel);
        _alarmBanner.Controls.Add(_alarmTitleLabel);
        _alarmBanner.Controls.Add(_alarmSilenceButton);

        _webView.BackColor = Color.Black;
        _webView.DefaultBackgroundColor = Color.Black;
        _webView.Dock = DockStyle.Fill;
        _webView.NavigationCompleted += OnNavigationCompleted;

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
    /// Initializes WebView2 and navigates to the configured panel URL.
    /// This method is called every time the main form renders a visible panel slot. It resolves the URL,
    /// configures timers, initializes the browser, navigates, and starts the independent refresh cycle.
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
        ConfigureMonitoringTimer(panel.Monitoring);

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

                _statusLabel.Text = "Refreshing...";
                ClearAlarmState();
                _webView.CoreWebView2.Navigate(_targetUri.ToString());
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
        _refreshTimer.Stop();
        _alarmPollTimer.Stop();
        _alarmPulseTimer.Stop();
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
                _webView.CoreWebView2.Navigate(_targetUri.ToString());
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
            if (e.IsSuccess)
            {
                _statusLabel.Text = $"Loaded {DateTime.Now:T}";

                if (_panel?.Monitoring?.Enabled == true)
                {
                    // Start recurring DOM checks and also run one immediate check so a currently visible
                    // alarm appears without waiting for the first timer interval.
                    _alarmPollTimer.Start();
                    _ = DetectConfiguredAlertsSafelyAsync();
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
    private void ConfigureMonitoringTimer(PanelMonitoringOptions? monitoring)
    {
        _alarmPollTimer.Stop();
        _alarmSoundEnabled = monitoring?.SoundEnabled ?? true;
        _alarmSoundInterval = TimeSpan.FromSeconds(Math.Max(1, monitoring?.RepeatSoundSeconds ?? 5));
        _alarmPollTimer.Interval = Math.Max(1, monitoring?.PollSeconds ?? 3) * 1000;
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

        if (_webView.CoreWebView2 is null || monitoring?.Enabled != true || monitoring.Rules.Count == 0)
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
                        return includesText(element, contains);
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

            if (snapshot?.Active == true)
            {
                ShowAlarmState(snapshot);
                return;
            }

            ClearAlarmState();
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
            // A changed signature means this is a materially different alarm. Reset silence and sound
            // timing so the operator is notified about the new condition.
            _alarmSignature = signature;
            _alarmSilenced = false;
            _lastAlarmSoundUtc = DateTime.MinValue;
        }

        _alarmActive = true;
        _alarmSeverity = NormalizeSeverity(snapshot.Severity);
        _alarmSoundEnabled = _panel?.Monitoring?.SoundEnabled == true && snapshot.SoundEnabled;
        _alarmBanner.Visible = true;
        _alarmTitleLabel.Text = $"{FormatSeverity(_alarmSeverity)} - {snapshot.Title} - {_panel?.Name ?? "Panel"}";
        _alarmDetailsLabel.Text = detailText;
        _alarmSilenceButton.Text = _alarmSilenced ? "Silenced" : "Silence";
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
        _alarmSilenced = false;
        _alarmSeverity = "warning";
        _alarmSignature = string.Empty;
        _alarmBanner.Visible = false;
        Padding = Padding.Empty;
        BackColor = Color.Black;
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
        if (_alarmSilenced || !_alarmSoundEnabled)
        {
            return;
        }

        var nowUtc = DateTime.UtcNow;

        if (nowUtc - _lastAlarmSoundUtc < _alarmSoundInterval)
        {
            return;
        }

        _lastAlarmSoundUtc = nowUtc;
        SystemSounds.Exclamation.Play();
    }

    /// <summary>
    /// Acknowledges the current alarm sound while keeping the visual alarm visible.
    /// </summary>
    private void SilenceAlarmSound()
    {
        _alarmSilenced = true;
        _alarmSilenceButton.Text = "Silenced";
        _statusLabel.Text = $"Alarm silenced {DateTime.Now:T}";
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
    private static Color GetAlarmBannerColor(string severity, bool pulseOn)
    {
        return severity switch
        {
            "critical" => pulseOn ? Color.FromArgb(204, 18, 32) : Color.FromArgb(92, 8, 14),
            "info" => pulseOn ? Color.FromArgb(0, 92, 138) : Color.FromArgb(5, 45, 72),
            _ => pulseOn ? Color.FromArgb(204, 103, 0) : Color.FromArgb(92, 46, 0)
        };
    }

    /// <summary>
    /// Gets the pulsing border color for one severity.
    /// </summary>
    /// <param name="severity">Normalized severity.</param>
    /// <param name="pulseOn">Whether the pulse is in its bright phase.</param>
    /// <returns>Border color.</returns>
    private static Color GetAlarmBorderColor(string severity, bool pulseOn)
    {
        return severity switch
        {
            "critical" => pulseOn ? Color.FromArgb(255, 59, 48) : Color.FromArgb(255, 149, 0),
            "info" => pulseOn ? Color.FromArgb(0, 190, 255) : Color.FromArgb(0, 122, 204),
            _ => pulseOn ? Color.FromArgb(255, 149, 0) : Color.FromArgb(255, 204, 0)
        };
    }

    /// <summary>
    /// Resolves an absolute URL or root-relative local URL into a URI WebView2 can navigate.
    /// Root-relative local URLs are useful for teams that publish static pages with the application.
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
}
