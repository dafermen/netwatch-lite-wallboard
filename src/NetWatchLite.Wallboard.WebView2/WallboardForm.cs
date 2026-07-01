using System.Diagnostics;
using Microsoft.Web.WebView2.Core;

namespace NetWatchLite.Wallboard.WebView2;

/// <summary>
/// Main Windows form for the operational wallboard.
/// This form is the application coordinator: it owns layout, paging, rotation,
/// fullscreen behavior, global refresh, and settings reloads. Individual page rendering
/// and browser hosting are delegated to WebViewPanelControl.
/// </summary>
internal sealed class WallboardForm : Form
{
    private static readonly int[] SupportedLayouts = [1, 2, 3, 4, 6, 8];

    private readonly Panel _topBar = new();
    private readonly Label _titleLabel = new();
    private readonly Label _pageLabel = new();
    private readonly Label _shortcutLabel = new();
    private readonly Dictionary<int, Button> _layoutButtons = [];
    private readonly Button _refreshButton = new();
    private readonly Button _restartAllButton = new();
    private readonly Button _settingsButton = new();
    private readonly Button _previousPageButton = new();
    private readonly Button _nextPageButton = new();
    private readonly CheckBox _rotationCheckBox = new();
    private readonly Label _memoryLabel = new();
    private readonly ToolTip _topBarToolTip = new();
    private readonly TableLayoutPanel _grid = new();
    private readonly System.Windows.Forms.Timer _rotationTimer = new();
    private readonly System.Windows.Forms.Timer _autoRestartTimer = new();
    private readonly System.Windows.Forms.Timer _memoryTimer = new();
    private readonly System.Windows.Forms.Timer _topBarAutoHideTimer = new();
    private readonly List<WebViewPanelControl> _activePanels = [];
    private readonly Dictionary<WebViewPanelControl, ActivePanelSlot> _activePanelSlots = [];
    private readonly SemaphoreSlim _renderLock = new(1, 1);
    private WallboardConfiguration _configuration = new();
    private CoreWebView2Environment? _webViewEnvironment;
    private string _webViewUserDataFolder = string.Empty;
    private int _layout = 4;
    private int _currentPage;
    private bool _isFullscreen;
    private bool _lowPowerSuspended;
    private FormBorderStyle _previousBorderStyle;
    private FormWindowState _previousWindowState;
    private Rectangle _previousBounds;
    private ThemePalette _theme = ThemePalette.FromName("dark");

    /// <summary>
    /// Creates the window, top bar, panel grid, timers, and keyboard handlers.
    /// The actual WebView2 environment is initialized later during the Load event because it is async.
    /// </summary>
    public WallboardForm()
    {
        Text = "NetWatch Lite Wallboard";
        BackColor = _theme.WindowBackColor;
        ForeColor = _theme.PrimaryTextColor;
        KeyPreview = true;
        MinimumSize = new Size(1280, 720);
        WindowState = FormWindowState.Maximized;

        BuildTopBar();
        BuildGrid();
        ApplyTheme();

        Controls.Add(_grid);
        Controls.Add(_topBar);

        _rotationTimer.Tick += (_, _) => RunSafely(() => RotatePage(), "rotating wallboard pages");
        _autoRestartTimer.Tick += (_, _) => _ = RunSafelyAsync(RestartVisiblePanelsAsync, "scheduled panel restart");
        _memoryTimer.Tick += (_, _) => RunSafely(UpdateMemoryStatus, "updating memory monitor");
        _topBarAutoHideTimer.Tick += (_, _) => RunSafely(HideTopBarIfIdle, "auto-hiding top bar");
        Load += (_, _) => _ = RunSafelyAsync(InitializeAsync, "initializing the wallboard");
        FormClosing += OnFormClosing;
        Resize += (_, _) => _ = RunSafelyAsync(HandleLowPowerResizeAsync, "updating low power mode");
        MouseMove += OnWallboardMouseMove;
        KeyDown += OnKeyDown;
    }

    /// <summary>
    /// Initializes the shared WebView2 environment and loads the first wallboard configuration.
    /// All panels share this environment so browser profile data, cookies, and authentication state
    /// behave like one normal browser session across the wallboard.
    /// </summary>
    private async Task InitializeAsync()
    {
        _webViewUserDataFolder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "NetWatchLite",
            "WallboardWebView2");

        _webViewEnvironment = await CoreWebView2Environment.CreateAsync(
            browserExecutableFolder: null,
            userDataFolder: _webViewUserDataFolder);

        await ReloadConfigurationAsync();
    }

    /// <summary>
    /// Builds the control strip with layout, page navigation, rotation, and global actions.
    /// The top bar is intentionally compact because this app is usually displayed on TVs or NOC screens.
    /// </summary>
    private void BuildTopBar()
    {
        _topBar.Dock = DockStyle.Top;
        _topBar.Height = 48;
        _topBar.Padding = new Padding(8, 6, 8, 6);
        _topBar.MouseMove += OnWallboardMouseMove;

        var topLayout = new TableLayoutPanel
        {
            ColumnCount = 3,
            Dock = DockStyle.Fill,
            RowCount = 1
        };
        topLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 610));
        topLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        topLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 640));

        var leftPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            Margin = Padding.Empty
        };

        foreach (var layout in SupportedLayouts)
        {
            var button = new Button();
            ConfigureButton(button, layout.ToString(), 44);
            button.Click += (_, _) => SetLayout(layout);
            _layoutButtons[layout] = button;
            leftPanel.Controls.Add(button);
        }

        ConfigureButton(_previousPageButton, "<", 42);
        ConfigureButton(_nextPageButton, ">", 42);
        ConfigureButton(_refreshButton, "Refresh All", 112);
        ConfigureButton(_restartAllButton, "Restart All", 112);
        ConfigureButton(_settingsButton, "Settings", 110);

        _previousPageButton.Click += (_, _) => NavigatePage(-1);
        _nextPageButton.Click += (_, _) => NavigatePage(1);
        _refreshButton.Click += (_, _) => RefreshVisiblePanels();
        _restartAllButton.Click += (_, _) => _ = RunSafelyAsync(RestartVisiblePanelsAsync, "restarting visible panels");
        _settingsButton.Click += (_, _) => _ = RunSafelyAsync(OpenSettingsAsync, "opening settings");
        _topBarToolTip.SetToolTip(_previousPageButton, "Previous page");
        _topBarToolTip.SetToolTip(_nextPageButton, "Next page");
        _topBarToolTip.SetToolTip(_refreshButton, "Refresh all visible panels");
        _topBarToolTip.SetToolTip(_restartAllButton, "Restart all visible panels");
        _topBarToolTip.SetToolTip(_settingsButton, "Open wallboard settings");

        _rotationCheckBox.Text = "Auto";
        _rotationCheckBox.AutoSize = true;
        _rotationCheckBox.Margin = new Padding(10, 8, 0, 0);
        _rotationCheckBox.CheckedChanged += (_, _) => RunSafely(ScheduleRotation, "updating rotation");

        _pageLabel.AutoSize = false;
        _pageLabel.AutoEllipsis = false;
        _pageLabel.Margin = new Padding(10, 8, 0, 0);
        _pageLabel.TextAlign = ContentAlignment.MiddleLeft;
        _pageLabel.Width = 130;

        leftPanel.Controls.AddRange([
            _rotationCheckBox,
            _previousPageButton,
            _nextPageButton,
            _pageLabel
        ]);

        _titleLabel.Dock = DockStyle.Fill;
        _titleLabel.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
        _titleLabel.TextAlign = ContentAlignment.MiddleCenter;

        var rightPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            Margin = Padding.Empty
        };

        _shortcutLabel.AutoSize = false;
        _shortcutLabel.Margin = new Padding(0, 8, 18, 0);
        _shortcutLabel.Text = "F/R/C/ESC";
        _shortcutLabel.TextAlign = ContentAlignment.MiddleLeft;
        _shortcutLabel.Width = 86;

        _memoryLabel.AutoSize = false;
        _memoryLabel.Margin = new Padding(0, 8, 12, 0);
        _memoryLabel.TextAlign = ContentAlignment.MiddleRight;
        _memoryLabel.Width = 150;
        _memoryLabel.Visible = false;

        rightPanel.Controls.Add(_shortcutLabel);
        rightPanel.Controls.Add(_memoryLabel);
        rightPanel.Controls.Add(_refreshButton);
        rightPanel.Controls.Add(_restartAllButton);
        rightPanel.Controls.Add(_settingsButton);

        topLayout.Controls.Add(leftPanel, 0, 0);
        topLayout.Controls.Add(_titleLabel, 1, 0);
        topLayout.Controls.Add(rightPanel, 2, 0);
        _topBar.Controls.Add(topLayout);
    }

    /// <summary>
    /// Builds the table layout that hosts the active panel controls.
    /// </summary>
    private void BuildGrid()
    {
        _grid.Dock = DockStyle.Fill;
        _grid.Padding = new Padding(4);
        _grid.MouseMove += OnWallboardMouseMove;
    }

    /// <summary>
    /// Reloads <c>wallboard.json</c>, resets paging, and renders the first page.
    /// This is used at startup and after the settings dialog saves or requests a JSON reload.
    /// </summary>
    private async Task ReloadConfigurationAsync()
    {
        _rotationTimer.Stop();
        _configuration = await WallboardConfigReader.LoadAsync();
        _theme = ThemePalette.FromName(_configuration.Theme);
        ApplyTheme();
        _layout = NormalizeLayout(_configuration.DefaultLayout);
        _currentPage = 0;
        _titleLabel.Text = _configuration.AppTitle;
        _rotationCheckBox.Checked = _configuration.RotationEnabled;
        await RenderCurrentPageAsync();
        ScheduleRotation();
        ScheduleAutoRestart();
        ScheduleMemoryMonitor();
        ScheduleTopBarAutoHide();

        if (_configuration.StartFullscreen && !_isFullscreen)
        {
            ToggleFullscreen();
        }
    }

    /// <summary>
    /// Opens the visual settings editor and reloads the wallboard when changes are saved.
    /// SettingsForm works on a cloned configuration, so canceling the dialog leaves this live form untouched.
    /// </summary>
    private async Task OpenSettingsAsync()
    {
        ExitFullscreen();

        ShowTopBar();
        using var settingsForm = new SettingsForm(_configuration, OpenDiagnostics);
        settingsForm.Shown += (_, _) =>
        {
            settingsForm.BringToFront();
            settingsForm.Activate();
        };
        var result = settingsForm.ShowDialog(this);

        if (result is DialogResult.OK or DialogResult.Retry)
        {
            await ReloadConfigurationAsync();
        }
    }

    /// <summary>
    /// Opens a read-only diagnostics window with runtime paths and configuration summary.
    /// </summary>
    private void OpenDiagnostics(IWin32Window owner)
    {
        using var diagnosticsForm = new DiagnosticsForm(
            _configuration,
            _layout,
            _currentPage + 1,
            GetPageCount(),
            _activePanels.Count,
            _webViewUserDataFolder);
        diagnosticsForm.ShowDialog(owner);
    }

    /// <summary>
    /// Recreates the visible panel grid for the current page and layout.
    /// Existing panel timers are stopped before controls are removed so old WebView refresh timers
    /// do not continue firing after their controls leave the grid.
    /// </summary>
    private async Task RenderCurrentPageAsync()
    {
        await _renderLock.WaitAsync();

        try
        {
            if (_webViewEnvironment is null)
            {
                return;
            }

            if (_lowPowerSuspended)
            {
                return;
            }

            DisposeActivePanels();
            _activePanels.Clear();
            _activePanelSlots.Clear();
            _grid.Controls.Clear();
            _grid.ColumnStyles.Clear();
            _grid.RowStyles.Clear();
            var (columns, rows) = GetGridDimensions(_layout);
            _grid.ColumnCount = columns;
            _grid.RowCount = rows;

            for (var column = 0; column < _grid.ColumnCount; column++)
            {
                _grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F / _grid.ColumnCount));
            }

            for (var row = 0; row < _grid.RowCount; row++)
            {
                _grid.RowStyles.Add(new RowStyle(SizeType.Percent, 100F / _grid.RowCount));
            }

            var panelEntries = GetVisiblePanelEntries();

            for (var index = 0; index < panelEntries.Count; index++)
            {
                var (panel, panelKey) = panelEntries[index];
                await AddPanelControlAsync(panel, panelKey, index % _grid.ColumnCount, index / _grid.ColumnCount);
            }

            UpdateLayoutButtons();
            UpdatePageLabel();
            UpdatePageNavigationButtons();
        }
        finally
        {
            _renderLock.Release();
        }
    }

    /// <summary>
    /// Switches between supported panel layouts.
    /// </summary>
    /// <param name="layout">Requested layout size. Supported values are 1, 2, 3, 4, 6, and 8.</param>
    private void SetLayout(int layout)
    {
        _layout = NormalizeLayout(layout);
        _currentPage = 0;
        _ = RunSafelyAsync(RenderCurrentPageAsync, "changing wallboard layout");
        ScheduleRotation();
    }

    /// <summary>
    /// Starts or stops the page rotation timer based on the current configuration and checkbox state.
    /// Rotation is useful only when the configured panel list spans more than one page.
    /// </summary>
    private void ScheduleRotation()
    {
        _rotationTimer.Stop();

        if (!_rotationCheckBox.Checked || GetPageCount() <= 1)
        {
            return;
        }

        _rotationTimer.Interval = Math.Max(1, _configuration.RotationSeconds) * 1000;
        _rotationTimer.Start();
    }

    /// <summary>
    /// Advances to the next page and wraps back to the first page.
    /// </summary>
    private void RotatePage()
    {
        _currentPage = (_currentPage + 1) % GetPageCount();
        _ = RunSafelyAsync(RenderCurrentPageAsync, "rendering the next rotated page");
    }

    /// <summary>
    /// Moves to the previous or next configured page and wraps at the ends.
    /// </summary>
    /// <param name="direction">Negative for previous page, positive for next page.</param>
    private void NavigatePage(int direction)
    {
        var pageCount = GetPageCount();

        if (pageCount <= 1)
        {
            return;
        }

        _currentPage = (_currentPage + Math.Sign(direction) + pageCount) % pageCount;
        _ = RunSafelyAsync(RenderCurrentPageAsync, "navigating wallboard pages");
        ScheduleRotation();
    }

    /// <summary>
    /// Refreshes all panels currently visible in the grid.
    /// </summary>
    private void RefreshVisiblePanels()
    {
        foreach (var panel in _activePanels)
        {
            panel.RefreshPanel();
        }
    }

    /// <summary>
    /// Starts the scheduled visible-panel restart timer when enabled.
    /// </summary>
    private void ScheduleAutoRestart()
    {
        _autoRestartTimer.Stop();

        if (!_configuration.AutoRestartEnabled)
        {
            return;
        }

        _autoRestartTimer.Interval = Math.Clamp(_configuration.AutoRestartHours, 1, 24) * 60 * 60 * 1000;
        _autoRestartTimer.Start();
    }

    /// <summary>
    /// Starts or stops the lightweight memory monitor.
    /// </summary>
    private void ScheduleMemoryMonitor()
    {
        _memoryTimer.Stop();
        _memoryLabel.Visible = _configuration.MemoryMonitorEnabled;

        if (!_configuration.MemoryMonitorEnabled)
        {
            return;
        }

        _memoryTimer.Interval = 60 * 1000;
        UpdateMemoryStatus();
        _memoryTimer.Start();
    }

    /// <summary>
    /// Starts or stops top-bar auto-hide behavior.
    /// </summary>
    private void ScheduleTopBarAutoHide()
    {
        _topBarAutoHideTimer.Stop();
        _topBar.Visible = true;

        if (!_configuration.AutoHideTopBarEnabled)
        {
            return;
        }

        _topBarAutoHideTimer.Interval = 3000;
        _topBarAutoHideTimer.Start();
    }

    /// <summary>
    /// Handles the per-panel Restart button by recreating only the selected WebView2 control.
    /// </summary>
    private void OnPanelRestartRequested(object? sender, EventArgs e)
    {
        if (sender is WebViewPanelControl control)
        {
            _ = RunSafelyAsync(() => RestartPanelAsync(control), "restarting panel");
        }
    }

    /// <summary>
    /// Recreates one visible panel without changing the current page or other visible panels.
    /// </summary>
    private async Task RestartPanelAsync(WebViewPanelControl control)
    {
        await _renderLock.WaitAsync();

        try
        {
            if (_lowPowerSuspended ||
                _webViewEnvironment is null ||
                !_activePanelSlots.TryGetValue(control, out var slot))
            {
                return;
            }

            _grid.Controls.Remove(control);
            _activePanels.Remove(control);
            _activePanelSlots.Remove(control);
            control.ShortcutRequested -= OnPanelShortcutRequested;
            control.RestartRequested -= OnPanelRestartRequested;
            control.StopTimers();
            control.Dispose();

            await AddPanelControlAsync(slot.Panel, slot.PanelKey, slot.Column, slot.Row);
            UpdatePageLabel();
        }
        finally
        {
            _renderLock.Release();
        }
    }

    /// <summary>
    /// Recreates all visible WebView2 controls on the active page.
    /// </summary>
    private async Task RestartVisiblePanelsAsync()
    {
        if (_lowPowerSuspended)
        {
            return;
        }

        await RenderCurrentPageAsync();
        UpdateMemoryStatus();
    }

    /// <summary>
    /// Releases WebView2 panel resources while the window is minimized when low power mode is enabled.
    /// Restoring the window rebuilds the current page and restarts rotation if applicable.
    /// </summary>
    private async Task HandleLowPowerResizeAsync()
    {
        if (!_configuration.LowPowerModeEnabled)
        {
            return;
        }

        if (WindowState == FormWindowState.Minimized)
        {
            if (_lowPowerSuspended)
            {
                return;
            }

            _lowPowerSuspended = true;
            _rotationTimer.Stop();
            DisposeActivePanels();
            _activePanels.Clear();
            _activePanelSlots.Clear();
            _grid.Controls.Clear();
            _pageLabel.Text = "Low power";
            return;
        }

        if (!_lowPowerSuspended)
        {
            return;
        }

        _lowPowerSuspended = false;
        await RenderCurrentPageAsync();
        ScheduleRotation();
    }

    /// <summary>
    /// Returns the panels that belong to the active page.
    /// Panel order in wallboard.json is preserved; paging simply slices that ordered list.
    /// </summary>
    /// <returns>Visible panel declarations.</returns>
    private List<WallboardPanel> GetVisiblePanels()
    {
        return GetVisiblePanelEntries()
            .Select(entry => entry.Panel)
            .ToList();
    }

    /// <summary>
    /// Returns visible panels together with their stable runtime keys.
    /// </summary>
    /// <returns>Visible panel/key pairs.</returns>
    private List<(WallboardPanel Panel, string Key)> GetVisiblePanelEntries()
    {
        var start = _currentPage * _layout;
        var count = Math.Min(_layout, Math.Max(0, _configuration.Panels.Count - start));
        var entries = new List<(WallboardPanel Panel, string Key)>(count);

        for (var offset = 0; offset < count; offset++)
        {
            var panelIndex = start + offset;
            var panel = _configuration.Panels[panelIndex];
            entries.Add((panel, GetPanelRuntimeKey(panel, panelIndex)));
        }

        return entries;
    }

    /// <summary>
    /// Calculates the number of pages needed for the configured panels and active layout.
    /// </summary>
    /// <returns>Total page count, always at least one.</returns>
    private int GetPageCount()
    {
        return Math.Max(1, (int)Math.Ceiling(_configuration.Panels.Count / (double)_layout));
    }

    /// <summary>
    /// Updates the top-right page indicator.
    /// </summary>
    private void UpdatePageLabel()
    {
        _pageLabel.Text = $"Page {_currentPage + 1} of {GetPageCount()}";
    }

    /// <summary>
    /// Enables page navigation only when the current layout spans multiple pages.
    /// </summary>
    private void UpdatePageNavigationButtons()
    {
        var enabled = GetPageCount() > 1;
        _previousPageButton.Enabled = enabled;
        _nextPageButton.Enabled = enabled;
    }

    /// <summary>
    /// Highlights the active layout button.
    /// </summary>
    private void UpdateLayoutButtons()
    {
        foreach (var (layout, button) in _layoutButtons)
        {
            button.BackColor = _layout == layout
                ? _theme.ActiveButtonBackColor
                : _theme.ButtonBackColor;
        }
    }

    /// <summary>
    /// Updates the memory label and recommends a restart when the process working set crosses the threshold.
    /// </summary>
    private void UpdateMemoryStatus()
    {
        if (!_configuration.MemoryMonitorEnabled)
        {
            _memoryLabel.Visible = false;
            return;
        }

        using var process = Process.GetCurrentProcess();
        var memoryMegabytes = process.WorkingSet64 / 1024 / 1024;
        var threshold = Math.Clamp(_configuration.MemoryWarningMegabytes, 256, 65536);
        _memoryLabel.Visible = true;
        _memoryLabel.ForeColor = memoryMegabytes >= threshold
            ? _theme.WarningTextColor
            : _theme.SecondaryTextColor;
        _memoryLabel.Text = memoryMegabytes >= threshold
            ? $"Memory {memoryMegabytes:N0} MB - Restart"
            : $"Memory {memoryMegabytes:N0} MB";
    }

    /// <summary>
    /// Applies the active theme to the native wallboard shell.
    /// </summary>
    private void ApplyTheme()
    {
        BackColor = _theme.WindowBackColor;
        ForeColor = _theme.PrimaryTextColor;
        _topBar.BackColor = _theme.TopBarBackColor;
        _grid.BackColor = _theme.WindowBackColor;
        _titleLabel.ForeColor = _theme.PrimaryTextColor;
        _pageLabel.ForeColor = _theme.SecondaryTextColor;
        _shortcutLabel.ForeColor = _theme.SecondaryTextColor;
        _rotationCheckBox.ForeColor = _theme.PrimaryTextColor;
        _memoryLabel.ForeColor = _theme.SecondaryTextColor;

        foreach (var button in _layoutButtons.Values)
        {
            ApplyButtonTheme(button);
        }

        ApplyButtonTheme(_previousPageButton);
        ApplyButtonTheme(_nextPageButton);
        ApplyButtonTheme(_refreshButton);
        ApplyButtonTheme(_restartAllButton);
        ApplyButtonTheme(_settingsButton);
        UpdateLayoutButtons();
    }

    /// <summary>
    /// Applies the active theme to one top-bar command button.
    /// </summary>
    private void ApplyButtonTheme(Button button)
    {
        button.BackColor = _theme.ButtonBackColor;
        button.ForeColor = _theme.PrimaryTextColor;
        button.FlatAppearance.BorderColor = _theme.BorderColor;
    }

    /// <summary>
    /// Toggles borderless fullscreen mode while preserving the previous window state.
    /// The previous border style, state, and bounds are stored so leaving fullscreen restores the user
    /// to the same window shape they had before entering NOC/TV mode.
    /// </summary>
    private void ToggleFullscreen()
    {
        if (!_isFullscreen)
        {
            _previousBorderStyle = FormBorderStyle;
            _previousWindowState = WindowState;
            _previousBounds = Bounds;
            FormBorderStyle = FormBorderStyle.None;
            WindowState = FormWindowState.Maximized;
            TopMost = true;
            _isFullscreen = true;
            ScheduleTopBarAutoHide();
            return;
        }

        TopMost = false;
        _topBar.Visible = true;
        FormBorderStyle = _previousBorderStyle;
        WindowState = _previousWindowState;
        Bounds = _previousBounds;
        _isFullscreen = false;
        ScheduleTopBarAutoHide();
    }

    /// <summary>
    /// Leaves fullscreen mode if the window is currently fullscreen.
    /// </summary>
    private void ExitFullscreen()
    {
        if (_isFullscreen)
        {
            ToggleFullscreen();
        }
    }

    /// <summary>
    /// Handles wallboard keyboard shortcuts.
    /// </summary>
    /// <param name="sender">Event sender.</param>
    /// <param name="e">Keyboard event details.</param>
    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (TryHandleShortcut(e.KeyCode, e.Modifiers))
        {
            e.Handled = true;
            return;
        }
    }

    /// <summary>
    /// Converts supported shortcut combinations into wallboard actions.
    /// </summary>
    /// <param name="keyCode">Pressed key.</param>
    /// <param name="modifiers">Pressed modifiers.</param>
    /// <returns>True when the shortcut was handled.</returns>
    private bool TryHandleShortcut(Keys keyCode, Keys modifiers)
    {
        if (modifiers == Keys.Control)
        {
            switch (keyCode)
            {
                case Keys.F:
                    HandleShortcut(Keys.F);
                    return true;
                case Keys.R:
                    HandleShortcut(Keys.R);
                    return true;
                case Keys.S:
                    HandleShortcut(Keys.S);
                    return true;
            }
        }

        if (modifiers != Keys.None)
        {
            return false;
        }

        if (keyCode == Keys.F)
        {
            HandleShortcut(Keys.F);
            return true;
        }

        if (keyCode == Keys.R)
        {
            HandleShortcut(Keys.R);
            return true;
        }

        if (keyCode == Keys.C)
        {
            HandleShortcut(Keys.C);
            return true;
        }

        if (keyCode == Keys.Escape)
        {
            HandleShortcut(Keys.Escape);
            return true;
        }

        return false;
    }

    /// <summary>
    /// Handles shortcuts forwarded by a focused WebView2 panel.
    /// </summary>
    /// <param name="sender">Panel control.</param>
    /// <param name="e">Shortcut key data.</param>
    private void OnPanelShortcutRequested(object? sender, WebViewPanelControl.PanelShortcutRequestedEventArgs e)
    {
        TryHandleShortcut(e.KeyCode, e.Modifiers);
    }

    /// <summary>
    /// Builds a stable in-session identity for one panel, including its JSON order when known.
    /// </summary>
    /// <param name="panel">Panel configuration.</param>
    /// <param name="panelIndex">Zero-based panel index in wallboard.json, or -1 when unknown.</param>
    /// <returns>Runtime key for this panel.</returns>
    private static string GetPanelRuntimeKey(WallboardPanel panel, int panelIndex)
    {
        return $"{panelIndex}|{panel.Name.Trim()}|{panel.Url.Trim()}";
    }

    /// <summary>
    /// Handles shortcuts even when focus is inside a hosted WebView2 control.
    /// KeyDown is enough for normal WinForms controls, but browser-hosted content can capture keys
    /// before the form sees them. ProcessCmdKey gives the main form a reliable final shortcut hook.
    /// </summary>
    /// <param name="msg">Windows message.</param>
    /// <param name="keyData">Pressed key.</param>
    /// <returns>True when the key was handled by the wallboard.</returns>
    protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
    {
        var keyCode = keyData & Keys.KeyCode;
        var modifiers = keyData & Keys.Modifiers;

        if (TryHandleShortcut(keyCode, modifiers))
        {
            return true;
        }

        return base.ProcessCmdKey(ref msg, keyData);
    }

    /// <summary>
    /// Applies the wallboard-level shortcut actions from both KeyDown and ProcessCmdKey.
    /// </summary>
    /// <param name="keyCode">Shortcut key.</param>
    private void HandleShortcut(Keys keyCode)
    {
        switch (keyCode)
        {
            case Keys.F:
                ToggleFullscreen();
                break;
            case Keys.R:
                RefreshVisiblePanels();
                break;
            case Keys.C:
            case Keys.S:
                _ = RunSafelyAsync(OpenSettingsAsync, "opening settings from keyboard shortcut");
                break;
            case Keys.Escape:
                if (_isFullscreen)
                {
                    ExitFullscreen();
                    break;
                }

                Close();
                break;
        }
    }

    /// <summary>
    /// Confirms close when kiosk protection is enabled, then releases timers and panels.
    /// </summary>
    private void OnFormClosing(object? sender, FormClosingEventArgs e)
    {
        if (_configuration.ConfirmExit &&
            e.CloseReason == CloseReason.UserClosing)
        {
            var result = MessageBox.Show(
                this,
                "Close NetWatch Lite Wallboard?",
                "Confirm Exit",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (result != DialogResult.Yes)
            {
                e.Cancel = true;
                return;
            }
        }

        _rotationTimer.Stop();
        _autoRestartTimer.Stop();
        _memoryTimer.Stop();
        _topBarAutoHideTimer.Stop();
        DisposeActivePanels();
    }

    /// <summary>
    /// Shows the top bar when the mouse moves near the top edge while auto-hide is enabled.
    /// </summary>
    private void OnWallboardMouseMove(object? sender, MouseEventArgs e)
    {
        if (!_configuration.AutoHideTopBarEnabled)
        {
            return;
        }

        var screenPoint = sender is Control control
            ? control.PointToScreen(e.Location)
            : PointToScreen(e.Location);
        var clientPoint = PointToClient(screenPoint);

        if (clientPoint.Y <= 8)
        {
            ShowTopBar();
        }
    }

    /// <summary>
    /// Makes the top bar visible and restarts the hide delay.
    /// </summary>
    private void ShowTopBar()
    {
        _topBar.Visible = true;

        if (_configuration.AutoHideTopBarEnabled)
        {
            _topBarAutoHideTimer.Stop();
            _topBarAutoHideTimer.Start();
        }
    }

    /// <summary>
    /// Hides the top bar after the idle delay when the pointer is not near the top edge.
    /// </summary>
    private void HideTopBarIfIdle()
    {
        if (!_configuration.AutoHideTopBarEnabled)
        {
            return;
        }

        var clientPoint = PointToClient(Cursor.Position);

        if (clientPoint.Y > _topBar.Height + 8)
        {
            _topBar.Visible = false;
        }
    }

    /// <summary>
    /// Stops all panel refresh timers before replacing panels or closing the form.
    /// </summary>
    private void StopActivePanelTimers()
    {
        foreach (var panel in _activePanels)
        {
            try
            {
                panel.StopTimers();
            }
            catch (Exception ex)
            {
                AppErrorLog.Log("stopping panel timers", ex);
            }
        }
    }

    /// <summary>
    /// Creates, wires, places, and loads one visible WebView panel control.
    /// </summary>
    private async Task<WebViewPanelControl> AddPanelControlAsync(
        WallboardPanel panel,
        string panelKey,
        int column,
        int row)
    {
        if (_webViewEnvironment is null)
        {
            throw new InvalidOperationException("WebView2 environment is not initialized.");
        }

        var control = new WebViewPanelControl(_theme);
        control.ShortcutRequested += OnPanelShortcutRequested;
        control.RestartRequested += OnPanelRestartRequested;
        _activePanels.Add(control);
        _activePanelSlots[control] = new ActivePanelSlot(panel, panelKey, column, row);
        _grid.Controls.Add(control, column, row);

        try
        {
            await control.LoadPanelAsync(
                panel,
                _webViewEnvironment);
        }
        catch (Exception ex)
        {
            AppErrorLog.Log($"loading panel '{panel.Name}'", ex);
            control.ShowPanelError(panel.Name, ex);
        }

        return control;
    }

    /// <summary>
    /// Fully releases active panel controls before rerendering or closing the wallboard.
    /// Stopping timers is not enough for WebView2: the hosted browser control must be disposed so
    /// Chromium renderer resources and event handlers are released during long-running rotation.
    /// </summary>
    private void DisposeActivePanels()
    {
        foreach (var panel in _activePanels)
        {
            try
            {
                panel.ShortcutRequested -= OnPanelShortcutRequested;
                panel.RestartRequested -= OnPanelRestartRequested;
                panel.StopTimers();
                panel.Dispose();
            }
            catch (Exception ex)
            {
                AppErrorLog.Log("disposing active panel", ex);
            }
        }

        _activePanelSlots.Clear();
    }

    /// <summary>
    /// Runs asynchronous UI work and converts unexpected failures into a log entry plus operator message.
    /// WinForms event handlers cannot naturally await Task-returning methods, so this helper prevents
    /// async work from becoming an unobserved exception that closes the application.
    /// </summary>
    /// <param name="action">Async action to execute.</param>
    /// <param name="context">Human-readable context for logs and messages.</param>
    private async Task RunSafelyAsync(Func<Task> action, string context)
    {
        try
        {
            await action();
        }
        catch (Exception ex)
        {
            _rotationTimer.Stop();
            AppErrorLog.ShowUnexpectedError(this, context, ex);
        }
    }

    /// <summary>
    /// Runs synchronous UI work through the same diagnostics path used by async operations.
    /// </summary>
    /// <param name="action">Action to execute.</param>
    /// <param name="context">Human-readable context for logs and messages.</param>
    private void RunSafely(Action action, string context)
    {
        try
        {
            action();
        }
        catch (Exception ex)
        {
            _rotationTimer.Stop();
            AppErrorLog.ShowUnexpectedError(this, context, ex);
        }
    }

    /// <summary>
    /// Applies the shared top-bar button styling.
    /// </summary>
    /// <param name="button">Button to style.</param>
    /// <param name="text">Button label.</param>
    private static void ConfigureButton(Button button, string text, int? width = null)
    {
        button.FlatStyle = FlatStyle.Flat;
        button.ForeColor = Color.White;
        button.Margin = new Padding(0, 0, 8, 0);
        button.Text = text;
        button.Width = width ?? (text.Length > 9 ? 98 : 82);
        button.Height = 32;
        button.BackColor = Color.FromArgb(23, 29, 36);
        button.FlatAppearance.BorderColor = Color.FromArgb(61, 74, 88);
    }

    /// <summary>
    /// Converts a requested layout into one of the supported wallboard sizes.
    /// </summary>
    /// <param name="layout">Requested number of panels visible at once.</param>
    /// <returns>The requested layout when supported; otherwise four panels.</returns>
    private static int NormalizeLayout(int layout)
    {
        return SupportedLayouts.Contains(layout) ? layout : 4;
    }

    /// <summary>
    /// Maps the active panel count to a dense NOC-friendly grid.
    /// </summary>
    /// <param name="layout">Number of panels visible at once.</param>
    /// <returns>Column and row counts for the table layout.</returns>
    private static (int Columns, int Rows) GetGridDimensions(int layout)
    {
        return layout switch
        {
            1 => (1, 1),
            2 => (2, 1),
            3 => (3, 1),
            4 => (2, 2),
            6 => (3, 2),
            8 => (4, 2),
            _ => (2, 2)
        };
    }

    /// <summary>
    /// Runtime placement metadata for a visible panel control.
    /// </summary>
    private sealed record ActivePanelSlot(WallboardPanel Panel, string PanelKey, int Column, int Row);
}
