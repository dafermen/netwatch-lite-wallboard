using Microsoft.Web.WebView2.Core;

namespace NetWatchLite.Wallboard.WebView2;

/// <summary>
/// Main Windows form for the operational wallboard.
/// </summary>
internal sealed class WallboardForm : Form
{
    private static readonly int[] SupportedLayouts = [1, 2, 3, 4, 6, 8];

    private readonly Panel _topBar = new();
    private readonly Label _titleLabel = new();
    private readonly Label _pageLabel = new();
    private readonly Dictionary<int, Button> _layoutButtons = [];
    private readonly Button _refreshButton = new();
    private readonly Button _reloadButton = new();
    private readonly Button _settingsButton = new();
    private readonly CheckBox _rotationCheckBox = new();
    private readonly TableLayoutPanel _grid = new();
    private readonly System.Windows.Forms.Timer _rotationTimer = new();
    private readonly List<WebViewPanelControl> _activePanels = [];
    private WallboardConfiguration _configuration = new();
    private CoreWebView2Environment? _webViewEnvironment;
    private int _layout = 4;
    private int _currentPage;
    private bool _isFullscreen;
    private FormBorderStyle _previousBorderStyle;
    private FormWindowState _previousWindowState;
    private Rectangle _previousBounds;

    /// <summary>
    /// Creates the window, top bar, panel grid, timers, and keyboard handlers.
    /// </summary>
    public WallboardForm()
    {
        Text = "NetWatch Lite Wallboard";
        BackColor = Color.FromArgb(5, 7, 10);
        ForeColor = Color.White;
        KeyPreview = true;
        MinimumSize = new Size(1280, 720);
        WindowState = FormWindowState.Maximized;

        BuildTopBar();
        BuildGrid();

        Controls.Add(_grid);
        Controls.Add(_topBar);

        _rotationTimer.Tick += (_, _) => RotatePage();
        Load += async (_, _) => await InitializeAsync();
        FormClosing += (_, _) => StopActivePanelTimers();
        KeyDown += OnKeyDown;
    }

    /// <summary>
    /// Initializes the shared WebView2 environment and loads the first wallboard configuration.
    /// </summary>
    private async Task InitializeAsync()
    {
        var userDataFolder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "NetWatchLite",
            "WallboardWebView2");

        _webViewEnvironment = await CoreWebView2Environment.CreateAsync(
            browserExecutableFolder: null,
            userDataFolder: userDataFolder);

        await ReloadConfigurationAsync();
    }

    /// <summary>
    /// Builds the control strip with layout, refresh, reload, rotation, and shortcut labels.
    /// </summary>
    private void BuildTopBar()
    {
        _topBar.BackColor = Color.FromArgb(10, 13, 17);
        _topBar.Dock = DockStyle.Top;
        _topBar.Height = 48;
        _topBar.Padding = new Padding(8, 6, 8, 6);

        var leftPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Left,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            Width = 560
        };

        foreach (var layout in SupportedLayouts)
        {
            var button = new Button();
            ConfigureButton(button, layout.ToString(), 44);
            button.Click += (_, _) => SetLayout(layout);
            _layoutButtons[layout] = button;
            leftPanel.Controls.Add(button);
        }

        ConfigureButton(_refreshButton, "Refresh");
        ConfigureButton(_reloadButton, "Reload JSON");
        ConfigureButton(_settingsButton, "Settings");

        _refreshButton.Click += (_, _) => RefreshVisiblePanels();
        _reloadButton.Click += async (_, _) => await ReloadConfigurationAsync();
        _settingsButton.Click += async (_, _) => await OpenSettingsAsync();

        _rotationCheckBox.Text = "Auto";
        _rotationCheckBox.AutoSize = true;
        _rotationCheckBox.ForeColor = Color.White;
        _rotationCheckBox.Margin = new Padding(10, 8, 0, 0);
        _rotationCheckBox.CheckedChanged += (_, _) => ScheduleRotation();

        leftPanel.Controls.AddRange([
            _refreshButton,
            _reloadButton,
            _settingsButton,
            _rotationCheckBox
        ]);

        _titleLabel.Dock = DockStyle.Fill;
        _titleLabel.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
        _titleLabel.TextAlign = ContentAlignment.MiddleCenter;

        var rightPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Right,
            FlowDirection = FlowDirection.RightToLeft,
            WrapContents = false,
            Width = 430
        };

        var shortcutLabel = new Label
        {
            AutoSize = true,
            ForeColor = Color.FromArgb(230, 237, 243),
            Margin = new Padding(0, 8, 0, 0),
            Text = "F: Fullscreen · R: Refresh · Ctrl+,: Settings · ESC"
        };

        _pageLabel.AutoSize = true;
        _pageLabel.ForeColor = Color.FromArgb(139, 155, 173);
        _pageLabel.Margin = new Padding(0, 8, 16, 0);

        rightPanel.Controls.Add(shortcutLabel);
        rightPanel.Controls.Add(_pageLabel);

        _topBar.Controls.Add(_titleLabel);
        _topBar.Controls.Add(leftPanel);
        _topBar.Controls.Add(rightPanel);
    }

    /// <summary>
    /// Builds the table layout that hosts the active panel controls.
    /// </summary>
    private void BuildGrid()
    {
        _grid.BackColor = Color.FromArgb(5, 7, 10);
        _grid.Dock = DockStyle.Fill;
        _grid.Padding = new Padding(4);
    }

    /// <summary>
    /// Reloads <c>wallboard.json</c>, resets paging, and renders the first page.
    /// </summary>
    private async Task ReloadConfigurationAsync()
    {
        _configuration = await WallboardConfigReader.LoadAsync();
        _layout = NormalizeLayout(_configuration.DefaultLayout);
        _currentPage = 0;
        _titleLabel.Text = _configuration.AppTitle;
        _rotationCheckBox.Checked = _configuration.RotationEnabled;
        await RenderCurrentPageAsync();
        ScheduleRotation();
    }

    /// <summary>
    /// Opens the visual JSON editor and reloads the wallboard when changes are saved.
    /// </summary>
    private async Task OpenSettingsAsync()
    {
        using var settingsForm = new SettingsForm(_configuration);

        if (settingsForm.ShowDialog(this) == DialogResult.OK)
        {
            await ReloadConfigurationAsync();
        }
    }

    /// <summary>
    /// Recreates the visible panel grid for the current page and layout.
    /// </summary>
    private async Task RenderCurrentPageAsync()
    {
        if (_webViewEnvironment is null)
        {
            return;
        }

        StopActivePanelTimers();
        _activePanels.Clear();
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

        var panels = GetVisiblePanels();

        for (var index = 0; index < panels.Count; index++)
        {
            var control = new WebViewPanelControl();
            _activePanels.Add(control);
            _grid.Controls.Add(control, index % _grid.ColumnCount, index / _grid.ColumnCount);
            await control.LoadPanelAsync(panels[index], _webViewEnvironment);
        }

        UpdateLayoutButtons();
        UpdatePageLabel();
    }

    /// <summary>
    /// Switches between supported panel layouts.
    /// </summary>
    /// <param name="layout">Requested layout size. Supported values are 1, 2, 3, 4, 6, and 8.</param>
    private void SetLayout(int layout)
    {
        _layout = NormalizeLayout(layout);
        _currentPage = 0;
        _ = RenderCurrentPageAsync();
        ScheduleRotation();
    }

    /// <summary>
    /// Starts or stops the page rotation timer based on the current configuration and checkbox state.
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
        _ = RenderCurrentPageAsync();
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
    /// Returns the panels that belong to the active page.
    /// </summary>
    /// <returns>Visible panel declarations.</returns>
    private List<WallboardPanel> GetVisiblePanels()
    {
        var start = _currentPage * _layout;
        return _configuration.Panels
            .Skip(start)
            .Take(_layout)
            .ToList();
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
        _pageLabel.Text = $"Page {_currentPage + 1} / {GetPageCount()}";
    }

    /// <summary>
    /// Highlights the active layout button.
    /// </summary>
    private void UpdateLayoutButtons()
    {
        foreach (var (layout, button) in _layoutButtons)
        {
            button.BackColor = _layout == layout
                ? Color.FromArgb(0, 80, 96)
                : Color.FromArgb(23, 29, 36);
        }
    }

    /// <summary>
    /// Toggles borderless fullscreen mode while preserving the previous window state.
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
            return;
        }

        TopMost = false;
        FormBorderStyle = _previousBorderStyle;
        WindowState = _previousWindowState;
        Bounds = _previousBounds;
        _isFullscreen = false;
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
        if (e.KeyCode == Keys.F)
        {
            ToggleFullscreen();
            e.Handled = true;
            return;
        }

        if (e.KeyCode == Keys.R)
        {
            RefreshVisiblePanels();
            e.Handled = true;
            return;
        }

        if (e.Control && e.KeyCode == Keys.Oemcomma)
        {
            _ = OpenSettingsAsync();
            e.Handled = true;
            return;
        }

        if (e.KeyCode == Keys.Escape)
        {
            ExitFullscreen();
            e.Handled = true;
        }
    }

    /// <summary>
    /// Stops all panel refresh timers before replacing panels or closing the form.
    /// </summary>
    private void StopActivePanelTimers()
    {
        foreach (var panel in _activePanels)
        {
            panel.StopTimers();
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
}
