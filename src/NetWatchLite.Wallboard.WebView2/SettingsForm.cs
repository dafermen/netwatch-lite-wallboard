using System.Text.Json;

namespace NetWatchLite.Wallboard.WebView2;

/// <summary>
/// Secondary window used to edit <c>wallboard.json</c> without hand-editing JSON.
/// The form edits a deep clone of the live configuration. Saving writes the clone through
/// WallboardConfigReader; canceling simply discards the clone and leaves the running wallboard unchanged.
/// </summary>
internal sealed class SettingsForm : Form
{
    private static readonly int[] SupportedLayouts = [1, 2, 3, 4, 6, 8];
    private static readonly Color WindowBackColor = Color.FromArgb(17, 24, 39);
    private static readonly Color SurfaceColor = Color.FromArgb(31, 41, 55);
    private static readonly Color InputColor = Color.FromArgb(15, 23, 42);
    private static readonly Color BorderColor = Color.FromArgb(75, 85, 99);
    private static readonly Color PrimaryTextColor = Color.FromArgb(243, 244, 246);
    private static readonly Color SecondaryTextColor = Color.FromArgb(209, 213, 219);
    private static readonly Color MutedTextColor = Color.FromArgb(156, 163, 175);
    private static readonly Color AccentColor = Color.FromArgb(8, 145, 178);
    private static readonly Color SelectionColor = Color.FromArgb(14, 116, 144);

    private readonly TextBox _titleTextBox = new();
    private readonly CheckBox _rotationCheckBox = new();
    private readonly CheckBox _lowPowerModeCheckBox = new();
    private readonly CheckBox _autoRestartCheckBox = new();
    private readonly CheckBox _startFullscreenCheckBox = new();
    private readonly CheckBox _confirmExitCheckBox = new();
    private readonly CheckBox _memoryMonitorCheckBox = new();
    private readonly CheckBox _autoHideTopBarCheckBox = new();
    private readonly NumericUpDown _rotationSecondsInput = new();
    private readonly NumericUpDown _autoRestartHoursInput = new();
    private readonly NumericUpDown _memoryWarningInput = new();
    private readonly ComboBox _defaultLayoutComboBox = new();
    private readonly ComboBox _themeComboBox = new();
    private readonly DataGridView _panelGrid = new();
    private readonly TextBox _panelNameTextBox = new();
    private readonly TextBox _panelUrlTextBox = new();
    private readonly NumericUpDown _panelRefreshInput = new();
    private readonly Label _filePathLabel = new();
    private readonly Label _statusLabel = new();
    private readonly WallboardConfiguration _configuration;
    private readonly Action<IWin32Window> _showDiagnostics;
    private static readonly JsonSerializerOptions ConfigurationJsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
        WriteIndented = true
    };
    private bool _isLoadingSelection;

    /// <summary>
    /// Builds the settings editor from the current wallboard configuration.
    /// The incoming configuration is cloned immediately so UI edits are isolated until Save is pressed.
    /// </summary>
    /// <param name="configuration">Current runtime configuration.</param>
    public SettingsForm(WallboardConfiguration configuration, Action<IWin32Window> showDiagnostics)
    {
        _configuration = CloneConfiguration(configuration);
        _showDiagnostics = showDiagnostics;

        Text = "Wallboard Settings";
        BackColor = WindowBackColor;
        ForeColor = PrimaryTextColor;
        Font = new Font("Segoe UI", 9F);
        FormBorderStyle = FormBorderStyle.Sizable;
        MaximizeBox = true;
        MinimizeBox = false;
        SizeGripStyle = SizeGripStyle.Show;
        MinimumSize = new Size(1120, 780);
        Size = new Size(1280, 820);
        StartPosition = FormStartPosition.CenterParent;

        BuildLayout();
        LoadConfigurationIntoControls();
        RefreshPanelGrid();
        WireChangeHandlers();
        SetStatus("Ready");
    }

    /// <summary>
    /// Builds the settings, panel list, editor, and command buttons.
    /// The layout is intentionally made from code instead of designer files so the repository stays
    /// portable and the form can be reviewed as normal C# source.
    /// </summary>
    private void BuildLayout()
    {
        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 4,
            Padding = new Padding(14),
            BackColor = WindowBackColor
        };
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 184));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 28));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 64));

        var settingsGroup = BuildTopSettings();
        var body = BuildBody();

        _filePathLabel.AutoEllipsis = true;
        _filePathLabel.Dock = DockStyle.Fill;
        _filePathLabel.ForeColor = MutedTextColor;
        _filePathLabel.TextAlign = ContentAlignment.MiddleLeft;

        var footer = BuildFooter();

        root.Controls.Add(settingsGroup, 0, 0);
        root.Controls.Add(body, 0, 1);
        root.Controls.Add(BuildStatusRow(), 0, 2);
        root.Controls.Add(footer, 0, 3);

        Controls.Add(root);
    }

    /// <summary>
    /// Builds the top settings area.
    /// </summary>
    /// <returns>Top settings group.</returns>
    private TableLayoutPanel BuildTopSettings()
    {
        var top = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1
        };
        top.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 46));
        top.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 54));
        top.Controls.Add(BuildSettingsGroup(), 0, 0);
        top.Controls.Add(BuildRuntimeGroup(), 1, 0);
        return top;
    }

    /// <summary>
    /// Builds the status row with the JSON path and unsaved-change feedback.
    /// </summary>
    /// <returns>Status row layout.</returns>
    private TableLayoutPanel BuildStatusRow()
    {
        var statusRow = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1
        };
        statusRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 72));
        statusRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 28));

        _statusLabel.AutoEllipsis = true;
        _statusLabel.Dock = DockStyle.Fill;
        _statusLabel.ForeColor = SecondaryTextColor;
        _statusLabel.TextAlign = ContentAlignment.MiddleRight;

        statusRow.Controls.Add(_filePathLabel, 0, 0);
        statusRow.Controls.Add(_statusLabel, 1, 0);
        return statusRow;
    }

    /// <summary>
    /// Builds top-level wallboard settings controls.
    /// </summary>
    /// <returns>Group box containing title, rotation, and layout controls.</returns>
    private GroupBox BuildSettingsGroup()
    {
        var group = CreateGroupBox("Wallboard");

        var panel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 6,
            RowCount = 2,
            Padding = new Padding(10, 14, 10, 12)
        };
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 36));
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 12));
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 12));
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 16));
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 12));
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 12));
        panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 26));
        panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));

        _titleTextBox.Width = 420;
        _titleTextBox.BackColor = InputColor;
        _titleTextBox.Dock = DockStyle.Fill;
        _titleTextBox.ForeColor = PrimaryTextColor;
        _titleTextBox.BorderStyle = BorderStyle.FixedSingle;

        _rotationCheckBox.Text = "Auto rotation";
        _rotationCheckBox.AutoSize = true;
        _rotationCheckBox.ForeColor = PrimaryTextColor;
        _rotationCheckBox.Margin = new Padding(0, 4, 10, 0);

        _lowPowerModeCheckBox.Text = "Low power";
        _lowPowerModeCheckBox.AutoSize = true;
        _lowPowerModeCheckBox.ForeColor = PrimaryTextColor;
        _lowPowerModeCheckBox.Margin = new Padding(0, 4, 10, 0);

        ConfigureNumericInput(_rotationSecondsInput, minimum: 1, maximum: 3600, width: 110);

        _defaultLayoutComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
        _defaultLayoutComboBox.Dock = DockStyle.Fill;
        _defaultLayoutComboBox.Width = 110;
        _defaultLayoutComboBox.BackColor = InputColor;
        _defaultLayoutComboBox.ForeColor = PrimaryTextColor;
        foreach (var layout in SupportedLayouts)
        {
            _defaultLayoutComboBox.Items.Add(layout);
        }

        panel.Controls.Add(CreateInlineLabel("Title"), 0, 0);
        panel.Controls.Add(CreateInlineLabel("Auto"), 1, 0);
        panel.Controls.Add(CreateInlineLabel("Rotation"), 2, 0);
        panel.Controls.Add(CreateInlineLabel("Default layout"), 3, 0);
        panel.Controls.Add(CreateInlineLabel("Power"), 4, 0);
        panel.Controls.Add(CreateInlineLabel(string.Empty), 5, 0);
        panel.Controls.Add(_titleTextBox, 0, 1);
        panel.Controls.Add(_rotationCheckBox, 1, 1);
        panel.Controls.Add(_rotationSecondsInput, 2, 1);
        panel.Controls.Add(_defaultLayoutComboBox, 3, 1);
        panel.Controls.Add(_lowPowerModeCheckBox, 4, 1);
        panel.SetColumnSpan(_lowPowerModeCheckBox, 2);

        group.Controls.Add(panel);
        return group;
    }

    /// <summary>
    /// Builds long-running session, kiosk, memory, top-bar, and theme options.
    /// </summary>
    /// <returns>Runtime options group.</returns>
    private GroupBox BuildRuntimeGroup()
    {
        var group = CreateGroupBox("Runtime");

        var panel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 4,
            RowCount = 4,
            Padding = new Padding(10, 14, 10, 12)
        };
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 26));
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 24));
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 24));
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 26));
        panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 24));
        panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
        panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 24));
        panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));

        ConfigureComboBox(_themeComboBox, ["Dark", "Light", "High contrast"]);
        ConfigureNumericInput(_autoRestartHoursInput, minimum: 1, maximum: 24, width: 90);
        ConfigureNumericInput(_memoryWarningInput, minimum: 256, maximum: 65536, width: 110);
        ConfigureRuntimeCheckBox(_autoRestartCheckBox, "Auto restart");
        ConfigureRuntimeCheckBox(_startFullscreenCheckBox, "Start fullscreen");
        ConfigureRuntimeCheckBox(_confirmExitCheckBox, "Confirm exit");
        ConfigureRuntimeCheckBox(_memoryMonitorCheckBox, "Memory monitor");
        ConfigureRuntimeCheckBox(_autoHideTopBarCheckBox, "Auto-hide bar");

        panel.Controls.Add(CreateInlineLabel("Theme"), 0, 0);
        panel.Controls.Add(CreateInlineLabel("Restart"), 1, 0);
        panel.Controls.Add(CreateInlineLabel("Hours"), 2, 0);
        panel.Controls.Add(CreateInlineLabel("Memory MB"), 3, 0);
        panel.Controls.Add(_themeComboBox, 0, 1);
        panel.Controls.Add(_autoRestartCheckBox, 1, 1);
        panel.Controls.Add(_autoRestartHoursInput, 2, 1);
        panel.Controls.Add(_memoryWarningInput, 3, 1);
        panel.Controls.Add(CreateInlineLabel("Startup"), 0, 2);
        panel.Controls.Add(CreateInlineLabel("Close"), 1, 2);
        panel.Controls.Add(CreateInlineLabel("Memory"), 2, 2);
        panel.Controls.Add(CreateInlineLabel("TV mode"), 3, 2);
        panel.Controls.Add(_startFullscreenCheckBox, 0, 3);
        panel.Controls.Add(_confirmExitCheckBox, 1, 3);
        panel.Controls.Add(_memoryMonitorCheckBox, 2, 3);
        panel.Controls.Add(_autoHideTopBarCheckBox, 3, 3);

        group.Controls.Add(panel);
        return group;
    }

    /// <summary>
    /// Builds the panel list and CRUD editor.
    /// </summary>
    /// <returns>Two-column body layout.</returns>
    private TableLayoutPanel BuildBody()
    {
        var body = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
            Padding = new Padding(0, 10, 0, 8)
        };
        body.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 66));
        body.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 34));

        var listGroup = CreateGroupBox("Panels");
        ConfigurePanelGrid();
        listGroup.Controls.Add(_panelGrid);

        var editorGroup = BuildPanelEditorGroup();

        body.Controls.Add(listGroup, 0, 0);
        body.Controls.Add(editorGroup, 1, 0);
        return body;
    }

    /// <summary>
    /// Configures the read-only panel list.
    /// Panel details are edited in the fields on the right; the grid acts as an ordered selector.
    /// </summary>
    private void ConfigurePanelGrid()
    {
        _panelGrid.AllowUserToAddRows = false;
        _panelGrid.AllowUserToDeleteRows = false;
        _panelGrid.AllowUserToResizeRows = false;
        _panelGrid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        _panelGrid.BackgroundColor = SurfaceColor;
        _panelGrid.BorderStyle = BorderStyle.None;
        _panelGrid.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
        _panelGrid.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
        _panelGrid.Dock = DockStyle.Fill;
        _panelGrid.EditMode = DataGridViewEditMode.EditProgrammatically;
        _panelGrid.EnableHeadersVisualStyles = false;
        _panelGrid.GridColor = BorderColor;
        _panelGrid.MultiSelect = false;
        _panelGrid.ReadOnly = true;
        _panelGrid.RowHeadersVisible = false;
        _panelGrid.RowTemplate.Height = 30;
        _panelGrid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        _panelGrid.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(55, 65, 81);
        _panelGrid.ColumnHeadersDefaultCellStyle.ForeColor = PrimaryTextColor;
        _panelGrid.ColumnHeadersDefaultCellStyle.SelectionBackColor = Color.FromArgb(55, 65, 81);
        _panelGrid.ColumnHeadersDefaultCellStyle.SelectionForeColor = PrimaryTextColor;
        _panelGrid.DefaultCellStyle.BackColor = SurfaceColor;
        _panelGrid.DefaultCellStyle.ForeColor = PrimaryTextColor;
        _panelGrid.DefaultCellStyle.SelectionBackColor = SelectionColor;
        _panelGrid.DefaultCellStyle.SelectionForeColor = Color.White;
        _panelGrid.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(24, 34, 50);
        _panelGrid.AlternatingRowsDefaultCellStyle.ForeColor = PrimaryTextColor;
        _panelGrid.AlternatingRowsDefaultCellStyle.SelectionBackColor = SelectionColor;
        _panelGrid.AlternatingRowsDefaultCellStyle.SelectionForeColor = Color.White;
        _panelGrid.SelectionChanged += (_, _) => LoadSelectedPanelIntoEditor();

        _panelGrid.Columns.Add(CreateTextColumn("Name", "Name", 22));
        _panelGrid.Columns.Add(CreateTextColumn("URL", "URL", 50));
        _panelGrid.Columns.Add(CreateTextColumn("Refresh", "Refresh", 14));
    }

    /// <summary>
    /// Builds form fields and commands for adding or editing one panel.
    /// </summary>
    /// <returns>Group box containing the panel editor.</returns>
    private GroupBox BuildPanelEditorGroup()
    {
        var group = CreateGroupBox("Panel Details");

        var editor = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 4,
            Padding = new Padding(12, 16, 12, 12)
        };
        editor.RowStyles.Add(new RowStyle(SizeType.Absolute, 68));
        editor.RowStyles.Add(new RowStyle(SizeType.Absolute, 68));
        editor.RowStyles.Add(new RowStyle(SizeType.Absolute, 68));
        editor.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        ConfigureTextInput(_panelNameTextBox);
        ConfigureTextInput(_panelUrlTextBox);
        _panelNameTextBox.Dock = DockStyle.Fill;
        _panelUrlTextBox.Dock = DockStyle.Fill;
        ConfigureNumericInput(_panelRefreshInput, minimum: 1, maximum: 3600, width: 110);

        var commandGrid = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 200,
            ColumnCount = 2,
            RowCount = 4,
            Padding = new Padding(0, 8, 0, 0)
        };
        commandGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        commandGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        commandGrid.RowStyles.Add(new RowStyle(SizeType.Absolute, 46));
        commandGrid.RowStyles.Add(new RowStyle(SizeType.Absolute, 46));
        commandGrid.RowStyles.Add(new RowStyle(SizeType.Absolute, 46));
        commandGrid.RowStyles.Add(new RowStyle(SizeType.Absolute, 46));

        commandGrid.Controls.Add(CreateGridCommandButton("New Panel", (_, _) => ClearPanelEditor()), 0, 0);
        commandGrid.Controls.Add(CreateGridCommandButton("Add Panel", (_, _) => AddPanel()), 1, 0);
        commandGrid.Controls.Add(CreateGridCommandButton("Apply", (_, _) => ApplySelectedPanel()), 0, 1);
        commandGrid.Controls.Add(CreateGridCommandButton("Duplicate", (_, _) => DuplicateSelectedPanel()), 1, 1);
        commandGrid.Controls.Add(CreateGridCommandButton("Move Up", (_, _) => MoveSelectedPanel(-1)), 0, 2);
        commandGrid.Controls.Add(CreateGridCommandButton("Move Down", (_, _) => MoveSelectedPanel(1)), 1, 2);

        var deleteButton = CreateGridCommandButton("Delete", (_, _) => DeleteSelectedPanel());
        deleteButton.BackColor = Color.FromArgb(92, 38, 38);
        commandGrid.Controls.Add(deleteButton, 0, 3);
        commandGrid.SetColumnSpan(deleteButton, 2);

        editor.Controls.Add(CreateField("Name", _panelNameTextBox), 0, 0);
        editor.Controls.Add(CreateField("URL", _panelUrlTextBox), 0, 1);
        editor.Controls.Add(CreateField("Refresh seconds", _panelRefreshInput), 0, 2);
        editor.Controls.Add(commandGrid, 0, 3);

        group.Controls.Add(editor);
        return group;
    }

    /// <summary>
    /// Builds Save and Cancel buttons.
    /// </summary>
    /// <returns>Footer layout.</returns>
    private TableLayoutPanel BuildFooter()
    {
        var footer = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
            Padding = new Padding(0, 10, 0, 0)
        };
        footer.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        footer.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 270));

        var utilityPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false
        };

        var primaryPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.RightToLeft,
            WrapContents = false
        };

        var saveButton = CreateCommandButton(
            "Save Changes",
            (_, _) => _ = RunSafelyAsync(SaveConfigurationAsync, "saving wallboard settings"));
        saveButton.Width = 140;
        saveButton.BackColor = AccentColor;

        var cancelButton = CreateCommandButton("Cancel", (_, _) => Close());
        cancelButton.Width = 110;
        var exportButton = CreateCommandButton("Export JSON", (_, _) => ExportConfiguration());
        exportButton.Width = 120;
        var importButton = CreateCommandButton("Import JSON", (_, _) => ImportConfiguration());
        importButton.Width = 120;
        var reloadButton = CreateCommandButton("Reload JSON", (_, _) => RequestJsonReload());
        reloadButton.Width = 120;
        var diagnosticsButton = CreateCommandButton("Diagnostics", (_, _) => _showDiagnostics(this));
        diagnosticsButton.Width = 120;

        utilityPanel.Controls.Add(reloadButton);
        utilityPanel.Controls.Add(diagnosticsButton);
        utilityPanel.Controls.Add(importButton);
        utilityPanel.Controls.Add(exportButton);
        primaryPanel.Controls.Add(saveButton);
        primaryPanel.Controls.Add(cancelButton);

        footer.Controls.Add(utilityPanel, 0, 0);
        footer.Controls.Add(primaryPanel, 1, 0);
        return footer;
    }

    /// <summary>
    /// Closes Settings and asks the main wallboard to reload the active JSON from disk.
    /// </summary>
    private void RequestJsonReload()
    {
        var result = MessageBox.Show(
            this,
            "Reload wallboard.json from disk? Unsaved changes in this settings window will be discarded.",
            "Reload JSON",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Question);

        if (result != DialogResult.Yes)
        {
            return;
        }

        DialogResult = DialogResult.Retry;
        Close();
    }

    /// <summary>
    /// Wires change handlers after initial values have been loaded.
    /// </summary>
    private void WireChangeHandlers()
    {
        _titleTextBox.TextChanged += (_, _) => RunSafely(() => MarkUnsavedChanges(), "tracking title changes");
        _rotationCheckBox.CheckedChanged += (_, _) => RunSafely(() => MarkUnsavedChanges(), "tracking rotation changes");
        _lowPowerModeCheckBox.CheckedChanged += (_, _) => RunSafely(() => MarkUnsavedChanges(), "tracking low power mode changes");
        _autoRestartCheckBox.CheckedChanged += (_, _) => RunSafely(() => MarkUnsavedChanges(), "tracking auto restart changes");
        _startFullscreenCheckBox.CheckedChanged += (_, _) => RunSafely(() => MarkUnsavedChanges(), "tracking fullscreen startup changes");
        _confirmExitCheckBox.CheckedChanged += (_, _) => RunSafely(() => MarkUnsavedChanges(), "tracking confirm exit changes");
        _memoryMonitorCheckBox.CheckedChanged += (_, _) => RunSafely(() => MarkUnsavedChanges(), "tracking memory monitor changes");
        _autoHideTopBarCheckBox.CheckedChanged += (_, _) => RunSafely(() => MarkUnsavedChanges(), "tracking top bar auto-hide changes");
        _rotationSecondsInput.ValueChanged += (_, _) => RunSafely(() => MarkUnsavedChanges(), "tracking rotation interval changes");
        _autoRestartHoursInput.ValueChanged += (_, _) => RunSafely(() => MarkUnsavedChanges(), "tracking auto restart interval changes");
        _memoryWarningInput.ValueChanged += (_, _) => RunSafely(() => MarkUnsavedChanges(), "tracking memory threshold changes");
        _defaultLayoutComboBox.SelectedIndexChanged += (_, _) => RunSafely(() => MarkUnsavedChanges(), "tracking layout changes");
        _themeComboBox.SelectedIndexChanged += (_, _) => RunSafely(() => MarkUnsavedChanges(), "tracking theme changes");
        _panelNameTextBox.TextChanged += (_, _) => RunSafely(
            () => ApplySelectedPanelEditorChanges(showValidation: false),
            "tracking panel name changes");
        _panelUrlTextBox.TextChanged += (_, _) => RunSafely(
            () => ApplySelectedPanelEditorChanges(showValidation: false),
            "tracking panel URL changes");
        _panelRefreshInput.ValueChanged += (_, _) => RunSafely(
            () => ApplySelectedPanelEditorChanges(showValidation: false),
            "tracking panel refresh changes");
    }

    /// <summary>
    /// Copies the current configuration into form controls.
    /// </summary>
    private void LoadConfigurationIntoControls()
    {
        _titleTextBox.Text = _configuration.AppTitle;
        _rotationCheckBox.Checked = _configuration.RotationEnabled;
        _lowPowerModeCheckBox.Checked = _configuration.LowPowerModeEnabled;
        _autoRestartCheckBox.Checked = _configuration.AutoRestartEnabled;
        _startFullscreenCheckBox.Checked = _configuration.StartFullscreen;
        _confirmExitCheckBox.Checked = _configuration.ConfirmExit;
        _memoryMonitorCheckBox.Checked = _configuration.MemoryMonitorEnabled;
        _autoHideTopBarCheckBox.Checked = _configuration.AutoHideTopBarEnabled;
        _rotationSecondsInput.Value = Math.Clamp(_configuration.RotationSeconds, 1, 3600);
        _autoRestartHoursInput.Value = Math.Clamp(_configuration.AutoRestartHours, 1, 24);
        _memoryWarningInput.Value = Math.Clamp(_configuration.MemoryWarningMegabytes, 256, 65536);
        _defaultLayoutComboBox.SelectedItem = SupportedLayouts.Contains(_configuration.DefaultLayout)
            ? _configuration.DefaultLayout
            : 4;
        _themeComboBox.SelectedItem = GetThemeDisplayName(_configuration.Theme);
        _filePathLabel.Text = $"JSON file: {WallboardConfigReader.GetConfigurationFilePath()}";
    }

    /// <summary>
    /// Rebuilds the panel table from the in-memory configuration.
    /// </summary>
    /// <param name="selectedIndex">Panel index to select after refresh.</param>
    private void RefreshPanelGrid(int selectedIndex = 0)
    {
        _isLoadingSelection = true;
        _panelGrid.Rows.Clear();

        foreach (var panel in _configuration.Panels)
        {
            _panelGrid.Rows.Add(panel.Name, panel.Url, panel.RefreshSeconds);
        }

        _panelGrid.ClearSelection();

        if (_panelGrid.Rows.Count > 0)
        {
            var index = Math.Clamp(selectedIndex, 0, _panelGrid.Rows.Count - 1);
            _panelGrid.Rows[index].Selected = true;
            _panelGrid.CurrentCell = _panelGrid.Rows[index].Cells[0];
        }

        _isLoadingSelection = false;
        LoadSelectedPanelIntoEditor();
    }

    /// <summary>
    /// Copies the selected panel values into editor fields.
    /// </summary>
    private void LoadSelectedPanelIntoEditor()
    {
        if (_isLoadingSelection)
        {
            return;
        }

        var index = GetSelectedPanelIndex();

        if (index < 0)
        {
            ClearPanelEditor();
            return;
        }

        var panel = _configuration.Panels[index];

        _isLoadingSelection = true;

        try
        {
            _panelNameTextBox.Text = panel.Name;
            _panelUrlTextBox.Text = panel.Url;
            _panelRefreshInput.Value = Math.Clamp(panel.RefreshSeconds, 1, 3600);
        }
        finally
        {
            _isLoadingSelection = false;
        }
    }

    /// <summary>
    /// Clears panel editor fields for a new panel.
    /// </summary>
    private void ClearPanelEditor()
    {
        _panelGrid.ClearSelection();
        _panelNameTextBox.Clear();
        _panelUrlTextBox.Clear();
        _panelRefreshInput.Value = 30;
        _panelNameTextBox.Focus();
    }

    /// <summary>
    /// Adds a new panel from editor fields.
    /// </summary>
    private void AddPanel()
    {
        if (!TryReadPanelEditor(out var panel))
        {
            return;
        }

        _configuration.Panels.Add(panel);
        RefreshPanelGrid(_configuration.Panels.Count - 1);
        MarkUnsavedChanges("Panel added");
    }

    /// <summary>
    /// Applies editor fields to the selected panel.
    /// </summary>
    private void ApplySelectedPanel()
    {
        if (ApplySelectedPanelEditorChanges(showValidation: true))
        {
            SetStatus("Panel edits applied");
        }
    }

    /// <summary>
    /// Applies editor fields to the selected panel.
    /// </summary>
    /// <param name="showValidation">Shows validation messages when true.</param>
    /// <returns>True when there is no selected panel or the selected panel was updated.</returns>
    private bool ApplySelectedPanelEditorChanges(bool showValidation)
    {
        if (_isLoadingSelection)
        {
            return true;
        }

        var index = GetSelectedPanelIndex();

        if (index < 0)
        {
            if (!PanelEditorHasContent())
            {
                return true;
            }

            if (!showValidation)
            {
                MarkUnsavedChanges();
                return true;
            }

            if (!TryReadPanelEditor(out var newPanel, showValidation))
            {
                return false;
            }

            _configuration.Panels.Add(newPanel);
            RefreshPanelGrid(_configuration.Panels.Count - 1);
            MarkUnsavedChanges("Panel added");
            return true;
        }

        if (!TryReadPanelEditor(out var panel, showValidation))
        {
            return false;
        }

        _configuration.Panels[index] = panel;
        UpdatePanelGridRow(index, panel);
        MarkUnsavedChanges();
        return true;
    }

    /// <summary>
    /// Checks whether the new-panel editor contains any user-entered value.
    /// </summary>
    /// <returns>True when any panel editor field has a non-default value.</returns>
    private bool PanelEditorHasContent()
    {
        return !string.IsNullOrWhiteSpace(_panelNameTextBox.Text)
            || !string.IsNullOrWhiteSpace(_panelUrlTextBox.Text)
            || _panelRefreshInput.Value != 30;
    }

    /// <summary>
    /// Duplicates the selected panel below itself.
    /// </summary>
    private void DuplicateSelectedPanel()
    {
        var index = GetSelectedPanelIndex();

        if (index < 0)
        {
            ShowValidationMessage("Select a panel to duplicate.");
            return;
        }

        var source = _configuration.Panels[index];
        _configuration.Panels.Insert(index + 1, new WallboardPanel
        {
            Name = $"{source.Name} Copy",
            Url = source.Url,
            RefreshSeconds = source.RefreshSeconds
        });
        RefreshPanelGrid(index + 1);
        MarkUnsavedChanges("Panel duplicated");
    }

    /// <summary>
    /// Deletes the selected panel after confirmation.
    /// </summary>
    private void DeleteSelectedPanel()
    {
        var index = GetSelectedPanelIndex();

        if (index < 0)
        {
            ShowValidationMessage("Select a panel to delete.");
            return;
        }

        var result = MessageBox.Show(
            this,
            "Delete the selected panel?",
            "Confirm Delete",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Warning);

        if (result != DialogResult.Yes)
        {
            return;
        }

        _configuration.Panels.RemoveAt(index);
        RefreshPanelGrid(Math.Min(index, _configuration.Panels.Count - 1));
        MarkUnsavedChanges("Panel deleted");
    }

    /// <summary>
    /// Moves the selected panel up or down.
    /// </summary>
    /// <param name="offset">-1 to move up, 1 to move down.</param>
    private void MoveSelectedPanel(int offset)
    {
        var index = GetSelectedPanelIndex();
        var targetIndex = index + offset;

        if (index < 0 || targetIndex < 0 || targetIndex >= _configuration.Panels.Count)
        {
            return;
        }

        (_configuration.Panels[index], _configuration.Panels[targetIndex]) =
            (_configuration.Panels[targetIndex], _configuration.Panels[index]);
        RefreshPanelGrid(targetIndex);
        MarkUnsavedChanges("Panel reordered");
    }

    /// <summary>
    /// Saves settings and closes the form when validation passes.
    /// This is the only path that persists the cloned configuration back to disk.
    /// </summary>
    private async Task SaveConfigurationAsync()
    {
        if (!TryReadWallboardSettings())
        {
            return;
        }

        if (!ApplySelectedPanelEditorChanges(showValidation: true))
        {
            return;
        }

        if (_configuration.Panels.Count == 0)
        {
            ShowValidationMessage("Add at least one panel before saving.");
            return;
        }

        try
        {
            var savedPath = await WallboardConfigReader.SaveAsync(_configuration);
            MessageBox.Show(
                this,
                $"Saved and verified wallboard configuration.\n\n{savedPath}",
                "Settings Saved",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
            DialogResult = DialogResult.OK;
            Close();
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                this,
                $"Unable to save wallboard.json.\n\n{ex.Message}",
                "Save Failed",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
    }

    /// <summary>
    /// Reads top-level wallboard settings from controls.
    /// </summary>
    /// <returns>True when values are valid.</returns>
    private bool TryReadWallboardSettings()
    {
        var title = _titleTextBox.Text.Trim();

        if (string.IsNullOrWhiteSpace(title))
        {
            ShowValidationMessage("Enter a wallboard title.");
            _titleTextBox.Focus();
            return false;
        }

        _configuration.AppTitle = title;
        _configuration.RotationEnabled = _rotationCheckBox.Checked;
        _configuration.RotationSeconds = (int)_rotationSecondsInput.Value;
        _configuration.DefaultLayout = _defaultLayoutComboBox.SelectedItem is int layout ? layout : 4;
        _configuration.LowPowerModeEnabled = _lowPowerModeCheckBox.Checked;
        _configuration.AutoRestartEnabled = _autoRestartCheckBox.Checked;
        _configuration.AutoRestartHours = (int)_autoRestartHoursInput.Value;
        _configuration.StartFullscreen = _startFullscreenCheckBox.Checked;
        _configuration.ConfirmExit = _confirmExitCheckBox.Checked;
        _configuration.MemoryMonitorEnabled = _memoryMonitorCheckBox.Checked;
        _configuration.MemoryWarningMegabytes = (int)_memoryWarningInput.Value;
        _configuration.AutoHideTopBarEnabled = _autoHideTopBarCheckBox.Checked;
        _configuration.Theme = GetThemeNameFromDisplay(_themeComboBox.SelectedItem?.ToString());
        return true;
    }

    /// <summary>
    /// Reads and validates one panel from editor fields.
    /// </summary>
    /// <param name="panel">Panel produced from controls.</param>
    /// <returns>True when fields produce a valid panel.</returns>
    private bool TryReadPanelEditor(out WallboardPanel panel, bool showValidation = true)
    {
        var name = _panelNameTextBox.Text.Trim();
        var url = _panelUrlTextBox.Text.Trim();
        panel = new WallboardPanel();

        if (string.IsNullOrWhiteSpace(name))
        {
            if (showValidation)
            {
                ShowValidationMessage("Enter a panel name.");
                _panelNameTextBox.Focus();
            }

            return false;
        }

        if (!IsValidPanelUrl(url))
        {
            if (showValidation)
            {
                ShowValidationMessage("Enter an HTTP/HTTPS URL, a file:/// URL, or a local path such as pages/status.html or /status/index.html.");
                _panelUrlTextBox.Focus();
            }

            return false;
        }

        panel = new WallboardPanel
        {
            Name = name,
            Url = url,
            RefreshSeconds = (int)_panelRefreshInput.Value
        };
        return true;
    }

    /// <summary>
    /// Updates a grid row without rebuilding the whole panel list.
    /// </summary>
    /// <param name="index">Panel index.</param>
    /// <param name="panel">Panel values to show.</param>
    private void UpdatePanelGridRow(int index, WallboardPanel panel)
    {
        if (index < 0 || index >= _panelGrid.Rows.Count)
        {
            return;
        }

        _panelGrid.Rows[index].Cells[0].Value = panel.Name;
        _panelGrid.Rows[index].Cells[1].Value = panel.Url;
        _panelGrid.Rows[index].Cells[2].Value = panel.RefreshSeconds;
    }

    /// <summary>
    /// Updates status text after a local edit.
    /// </summary>
    /// <param name="message">Optional status text.</param>
    private void MarkUnsavedChanges(string message = "Unsaved changes")
    {
        SetStatus(message);
    }

    /// <summary>
    /// Updates the settings status label.
    /// </summary>
    /// <param name="message">Status text.</param>
    private void SetStatus(string message)
    {
        _statusLabel.Text = message;
    }

    /// <summary>
    /// Returns the currently selected panel index.
    /// </summary>
    /// <returns>Selected panel index, or -1.</returns>
    private int GetSelectedPanelIndex()
    {
        return _panelGrid.SelectedRows.Count == 0 ? -1 : _panelGrid.SelectedRows[0].Index;
    }

    /// <summary>
    /// Validates absolute HTTP/HTTPS URLs, file URLs, and root-relative local paths.
    /// </summary>
    /// <param name="url">URL text from the editor.</param>
    /// <returns>True when the app can navigate to the value.</returns>
    private static bool IsValidPanelUrl(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            return false;
        }

        if (url.StartsWith('/') || IsRelativeLocalUrl(url))
        {
            return true;
        }

        return Uri.TryCreate(url, UriKind.Absolute, out var uri)
            && (uri.Scheme == Uri.UriSchemeHttp ||
                uri.Scheme == Uri.UriSchemeHttps ||
                uri.Scheme == Uri.UriSchemeFile);
    }

    /// <summary>
    /// Allows packaged local pages without requiring
    /// machine-specific absolute file:/// paths in wallboard.json.
    /// </summary>
    /// <param name="url">Panel URL text.</param>
    /// <returns>True when the value is a safe relative local file path.</returns>
    private static bool IsRelativeLocalUrl(string url)
    {
        return Uri.TryCreate(url, UriKind.Relative, out _)
            && !Path.IsPathRooted(url)
            && !url.Contains(':', StringComparison.Ordinal)
            && !url.StartsWith('\\')
            && !url.Contains("..", StringComparison.Ordinal);
    }

    /// <summary>
    /// Creates a deep copy of the configuration for editing.
    /// The clone avoids accidental live mutation while the modal settings window is open.
    /// </summary>
    /// <param name="configuration">Source configuration.</param>
    /// <returns>Editable copy.</returns>
    private static WallboardConfiguration CloneConfiguration(WallboardConfiguration configuration)
    {
        return new WallboardConfiguration
        {
            AppTitle = configuration.AppTitle,
            RotationEnabled = configuration.RotationEnabled,
            RotationSeconds = configuration.RotationSeconds,
            DefaultLayout = configuration.DefaultLayout,
            LowPowerModeEnabled = configuration.LowPowerModeEnabled,
            AutoRestartEnabled = configuration.AutoRestartEnabled,
            AutoRestartHours = configuration.AutoRestartHours,
            StartFullscreen = configuration.StartFullscreen,
            ConfirmExit = configuration.ConfirmExit,
            MemoryMonitorEnabled = configuration.MemoryMonitorEnabled,
            MemoryWarningMegabytes = configuration.MemoryWarningMegabytes,
            AutoHideTopBarEnabled = configuration.AutoHideTopBarEnabled,
            Theme = configuration.Theme,
            Panels = configuration.Panels
                .Select(panel => new WallboardPanel
                {
                    Name = panel.Name,
                    Url = panel.Url,
                    RefreshSeconds = panel.RefreshSeconds
                })
                .ToList()
        };
    }

    /// <summary>
    /// Replaces the cloned configuration object contents without replacing the readonly field itself.
    /// </summary>
    /// <param name="configuration">Configuration to copy into the editor.</param>
    private void ReplaceEditableConfiguration(WallboardConfiguration configuration)
    {
        _configuration.AppTitle = configuration.AppTitle;
        _configuration.RotationEnabled = configuration.RotationEnabled;
        _configuration.RotationSeconds = configuration.RotationSeconds;
        _configuration.DefaultLayout = configuration.DefaultLayout;
        _configuration.LowPowerModeEnabled = configuration.LowPowerModeEnabled;
        _configuration.AutoRestartEnabled = configuration.AutoRestartEnabled;
        _configuration.AutoRestartHours = configuration.AutoRestartHours;
        _configuration.StartFullscreen = configuration.StartFullscreen;
        _configuration.ConfirmExit = configuration.ConfirmExit;
        _configuration.MemoryMonitorEnabled = configuration.MemoryMonitorEnabled;
        _configuration.MemoryWarningMegabytes = configuration.MemoryWarningMegabytes;
        _configuration.AutoHideTopBarEnabled = configuration.AutoHideTopBarEnabled;
        _configuration.Theme = configuration.Theme;
        _configuration.Panels = configuration.Panels;
    }

    /// <summary>
    /// Applies editor-safe defaults to imported JSON before controls are repopulated.
    /// Final runtime normalization still happens in WallboardConfigReader.SaveAsync.
    /// </summary>
    /// <param name="configuration">Imported configuration.</param>
    /// <returns>Configuration safe for this editor to display.</returns>
    private static WallboardConfiguration NormalizeConfigurationForEditor(WallboardConfiguration configuration)
    {
        var panels = (configuration.Panels ?? [])
            .Where(panel => panel is not null && IsValidPanelUrl(panel.Url))
            .Select(panel => new WallboardPanel
            {
                Name = string.IsNullOrWhiteSpace(panel.Name) ? "Wallboard Panel" : panel.Name.Trim(),
                Url = panel.Url.Trim(),
                RefreshSeconds = panel.RefreshSeconds <= 0 ? 30 : panel.RefreshSeconds
            })
            .ToList();

        return new WallboardConfiguration
        {
            AppTitle = string.IsNullOrWhiteSpace(configuration.AppTitle)
                ? "NetWatch Lite Wallboard"
                : configuration.AppTitle.Trim(),
            RotationEnabled = configuration.RotationEnabled,
            RotationSeconds = configuration.RotationSeconds <= 0 ? 20 : configuration.RotationSeconds,
            DefaultLayout = SupportedLayouts.Contains(configuration.DefaultLayout)
                ? configuration.DefaultLayout
                : 4,
            LowPowerModeEnabled = configuration.LowPowerModeEnabled,
            AutoRestartEnabled = configuration.AutoRestartEnabled,
            AutoRestartHours = Math.Clamp(configuration.AutoRestartHours, 1, 24),
            StartFullscreen = configuration.StartFullscreen,
            ConfirmExit = configuration.ConfirmExit,
            MemoryMonitorEnabled = configuration.MemoryMonitorEnabled,
            MemoryWarningMegabytes = Math.Clamp(configuration.MemoryWarningMegabytes, 256, 65536),
            AutoHideTopBarEnabled = configuration.AutoHideTopBarEnabled,
            Theme = ThemePalette.NormalizeName(configuration.Theme),
            Panels = panels.Count == 0
                ?
                [
                    new WallboardPanel
                    {
                        Name = "Operations Overview",
                        Url = "https://example.com/",
                        RefreshSeconds = 30
                    }
                ]
                : panels
        };
    }

    /// <summary>
    /// Runs asynchronous settings work through the central diagnostics path.
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
            AppErrorLog.ShowUnexpectedError(this, context, ex);
        }
    }

    /// <summary>
    /// Exports the currently edited configuration to a user-selected JSON file.
    /// The active application configuration is not changed by export.
    /// </summary>
    private void ExportConfiguration()
    {
        if (!TryReadWallboardSettings())
        {
            return;
        }

        if (!ApplySelectedPanelEditorChanges(showValidation: true))
        {
            return;
        }

        if (_configuration.Panels.Count == 0)
        {
            ShowValidationMessage("Add at least one panel before exporting.");
            return;
        }

        using var dialog = new SaveFileDialog
        {
            AddExtension = true,
            DefaultExt = "json",
            FileName = "wallboard.json",
            Filter = "JSON files (*.json)|*.json|All files (*.*)|*.*",
            OverwritePrompt = true,
            Title = "Export Wallboard Configuration"
        };

        if (dialog.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        try
        {
            var json = JsonSerializer.Serialize(_configuration, ConfigurationJsonOptions);
            File.WriteAllText(dialog.FileName, json);
            SetStatus("Configuration exported");
        }
        catch (Exception ex)
        {
            AppErrorLog.ShowUnexpectedError(this, "exporting wallboard configuration", ex);
        }
    }

    /// <summary>
    /// Imports a wallboard JSON file into this editor. The imported configuration is not persisted to
    /// the active wallboard.json until the user presses Save Changes.
    /// </summary>
    private void ImportConfiguration()
    {
        using var dialog = new OpenFileDialog
        {
            CheckFileExists = true,
            Filter = "JSON files (*.json)|*.json|All files (*.*)|*.*",
            Multiselect = false,
            Title = "Import Wallboard Configuration"
        };

        if (dialog.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        try
        {
            var json = File.ReadAllText(dialog.FileName);
            var imported = JsonSerializer.Deserialize<WallboardConfiguration>(
                json,
                ConfigurationJsonOptions);

            if (imported is null)
            {
                ShowValidationMessage("The selected JSON file does not contain a wallboard configuration.");
                return;
            }

            var result = MessageBox.Show(
                this,
                "Import this configuration into the settings editor?\n\n" +
                "Current unsaved edits in this window will be replaced. The active wallboard.json will not change until you press Save Changes.",
                "Import Configuration",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (result != DialogResult.Yes)
            {
                return;
            }

            ReplaceEditableConfiguration(NormalizeConfigurationForEditor(imported));
            LoadConfigurationIntoControls();
            RefreshPanelGrid();
            MarkUnsavedChanges($"Imported {Path.GetFileName(dialog.FileName)}");
        }
        catch (JsonException ex)
        {
            MessageBox.Show(
                this,
                $"The selected file is not valid wallboard JSON.\n\n{ex.Message}",
                "Import Failed",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
        catch (Exception ex)
        {
            AppErrorLog.ShowUnexpectedError(this, "importing wallboard configuration", ex);
        }
    }

    /// <summary>
    /// Runs synchronous settings work through the central diagnostics path.
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
            AppErrorLog.ShowUnexpectedError(this, context, ex);
        }
    }

    /// <summary>
    /// Creates a dark group box.
    /// </summary>
    /// <param name="text">Group label.</param>
    /// <returns>Configured group box.</returns>
    private static GroupBox CreateGroupBox(string text)
    {
        return new GroupBox
        {
            Dock = DockStyle.Fill,
            Text = text,
            ForeColor = SecondaryTextColor,
            Padding = new Padding(8),
            BackColor = WindowBackColor
        };
    }

    /// <summary>
    /// Creates a compact label used in grid-based settings rows.
    /// </summary>
    /// <param name="text">Label text.</param>
    /// <returns>Configured label.</returns>
    private static Label CreateInlineLabel(string text)
    {
        return new Label
        {
            AutoEllipsis = true,
            Dock = DockStyle.Fill,
            ForeColor = MutedTextColor,
            Padding = new Padding(0, 3, 8, 0),
            Text = text,
            TextAlign = ContentAlignment.BottomLeft
        };
    }

    /// <summary>
    /// Wraps a label and input control in a compact vertical field.
    /// </summary>
    /// <param name="label">Field label.</param>
    /// <param name="control">Input control.</param>
    /// <returns>Field container.</returns>
    private static Panel CreateField(string label, Control control)
    {
        var container = new Panel
        {
            Width = Math.Max(control.Width + 8, 140),
            Height = 64,
            Margin = new Padding(0, 0, 16, 0),
            Padding = new Padding(0, 2, 0, 8)
        };

        var labelControl = new Label
        {
            Dock = DockStyle.Top,
            Height = 24,
            Text = label,
            ForeColor = MutedTextColor,
            TextAlign = ContentAlignment.MiddleLeft
        };

        control.Dock = DockStyle.Bottom;
        container.Controls.Add(control);
        container.Controls.Add(labelControl);
        return container;
    }

    /// <summary>
    /// Creates a read-only text column for the panel table.
    /// </summary>
    /// <param name="name">Column name.</param>
    /// <param name="headerText">Visible header.</param>
    /// <param name="fillWeight">Relative width.</param>
    /// <returns>Configured column.</returns>
    private static DataGridViewTextBoxColumn CreateTextColumn(
        string name,
        string headerText,
        float fillWeight)
    {
        return new DataGridViewTextBoxColumn
        {
            Name = name,
            HeaderText = headerText,
            FillWeight = fillWeight,
            SortMode = DataGridViewColumnSortMode.NotSortable
        };
    }

    /// <summary>
    /// Applies shared text box styling.
    /// </summary>
    /// <param name="textBox">Text box to configure.</param>
    private static void ConfigureTextInput(TextBox textBox)
    {
        textBox.BackColor = InputColor;
        textBox.BorderStyle = BorderStyle.FixedSingle;
        textBox.ForeColor = PrimaryTextColor;
        textBox.Width = 300;
    }

    /// <summary>
    /// Applies shared numeric input styling and range.
    /// </summary>
    /// <param name="input">Numeric input to configure.</param>
    /// <param name="minimum">Minimum allowed value.</param>
    /// <param name="maximum">Maximum allowed value.</param>
    /// <param name="width">Control width.</param>
    private static void ConfigureNumericInput(
        NumericUpDown input,
        int minimum,
        int maximum,
        int width)
    {
        input.BackColor = InputColor;
        input.ForeColor = PrimaryTextColor;
        input.Minimum = minimum;
        input.Maximum = maximum;
        input.Width = width;
    }

    /// <summary>
    /// Applies shared combo-box styling.
    /// </summary>
    /// <param name="comboBox">Combo box to configure.</param>
    /// <param name="items">Items to display.</param>
    private static void ConfigureComboBox(ComboBox comboBox, string[] items)
    {
        comboBox.BackColor = InputColor;
        comboBox.DropDownStyle = ComboBoxStyle.DropDownList;
        comboBox.Dock = DockStyle.Fill;
        comboBox.ForeColor = PrimaryTextColor;
        comboBox.Items.Clear();
        comboBox.Items.AddRange(items);
    }

    /// <summary>
    /// Applies shared runtime checkbox styling.
    /// </summary>
    /// <param name="checkBox">Checkbox to configure.</param>
    /// <param name="text">Displayed text.</param>
    private static void ConfigureRuntimeCheckBox(CheckBox checkBox, string text)
    {
        checkBox.AutoSize = true;
        checkBox.ForeColor = PrimaryTextColor;
        checkBox.Margin = new Padding(0, 4, 6, 0);
        checkBox.Text = text;
    }

    /// <summary>
    /// Converts the JSON theme name into the settings display label.
    /// </summary>
    private static string GetThemeDisplayName(string? themeName)
    {
        return ThemePalette.NormalizeName(themeName) switch
        {
            "light" => "Light",
            "high-contrast" => "High contrast",
            _ => "Dark"
        };
    }

    /// <summary>
    /// Converts the settings display label into the JSON theme name.
    /// </summary>
    private static string GetThemeNameFromDisplay(string? displayName)
    {
        return ThemePalette.NormalizeName(displayName);
    }

    /// <summary>
    /// Creates a styled command button.
    /// </summary>
    /// <param name="text">Button text.</param>
    /// <param name="handler">Click handler.</param>
    /// <returns>Configured button.</returns>
    private static Button CreateCommandButton(string text, EventHandler handler)
    {
        var button = new Button
        {
            Text = text,
            FlatStyle = FlatStyle.Flat,
            ForeColor = PrimaryTextColor,
            BackColor = SurfaceColor,
            Height = 36,
            Width = 120,
            Margin = new Padding(4)
        };
        button.FlatAppearance.BorderColor = BorderColor;
        button.Click += handler;
        return button;
    }

    /// <summary>
    /// Creates a command button that fills a cell in the panel editor grid.
    /// </summary>
    /// <param name="text">Button text.</param>
    /// <param name="handler">Click handler.</param>
    /// <returns>Configured grid button.</returns>
    private static Button CreateGridCommandButton(string text, EventHandler handler)
    {
        var button = CreateCommandButton(text, handler);
        button.Dock = DockStyle.Fill;
        button.Margin = new Padding(4);
        return button;
    }

    /// <summary>
    /// Shows a validation warning owned by this form.
    /// </summary>
    /// <param name="message">Warning text.</param>
    private void ShowValidationMessage(string message)
    {
        MessageBox.Show(this, message, "Settings", MessageBoxButtons.OK, MessageBoxIcon.Warning);
    }
}
