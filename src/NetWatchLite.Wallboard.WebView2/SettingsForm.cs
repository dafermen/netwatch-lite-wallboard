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
    private static readonly JsonSerializerOptions MonitoringJsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
        WriteIndented = true
    };

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
    private readonly NumericUpDown _rotationSecondsInput = new();
    private readonly ComboBox _defaultLayoutComboBox = new();
    private readonly DataGridView _panelGrid = new();
    private readonly TextBox _panelNameTextBox = new();
    private readonly TextBox _panelUrlTextBox = new();
    private readonly NumericUpDown _panelRefreshInput = new();
    private readonly Label _filePathLabel = new();
    private readonly Label _statusLabel = new();
    private readonly WallboardConfiguration _configuration;
    private static readonly JsonSerializerOptions ConfigurationJsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
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
    public SettingsForm(WallboardConfiguration configuration)
    {
        _configuration = CloneConfiguration(configuration);

        Text = "Wallboard Settings";
        BackColor = WindowBackColor;
        ForeColor = PrimaryTextColor;
        Font = new Font("Segoe UI", 9F);
        FormBorderStyle = FormBorderStyle.Sizable;
        MaximizeBox = true;
        MinimizeBox = false;
        MinimumSize = new Size(980, 640);
        Size = new Size(1120, 720);
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
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 104));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 28));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));

        var settingsGroup = BuildSettingsGroup();
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

        var panel = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            Padding = new Padding(10, 16, 10, 10),
            WrapContents = false
        };

        _titleTextBox.Width = 300;
        _titleTextBox.BackColor = InputColor;
        _titleTextBox.ForeColor = PrimaryTextColor;
        _titleTextBox.BorderStyle = BorderStyle.FixedSingle;

        _rotationCheckBox.Text = "Auto rotation";
        _rotationCheckBox.AutoSize = true;
        _rotationCheckBox.ForeColor = PrimaryTextColor;
        _rotationCheckBox.Margin = new Padding(18, 5, 10, 0);

        ConfigureNumericInput(_rotationSecondsInput, minimum: 1, maximum: 3600, width: 74);

        _defaultLayoutComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
        _defaultLayoutComboBox.Width = 82;
        _defaultLayoutComboBox.BackColor = InputColor;
        _defaultLayoutComboBox.ForeColor = PrimaryTextColor;
        foreach (var layout in SupportedLayouts)
        {
            _defaultLayoutComboBox.Items.Add(layout);
        }

        panel.Controls.Add(CreateField("Title", _titleTextBox));
        panel.Controls.Add(_rotationCheckBox);
        panel.Controls.Add(CreateField("Rotation seconds", _rotationSecondsInput));
        panel.Controls.Add(CreateField("Default layout", _defaultLayoutComboBox));

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
        body.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 62));
        body.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 38));

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
    /// Panel details are edited in the fields on the right; the grid acts as an ordered selector and
    /// quick summary, including whether monitoring is enabled for each panel.
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
        _panelGrid.Columns.Add(CreateTextColumn("Monitoring", "Monitoring", 14));
    }

    /// <summary>
    /// Builds form fields and commands for adding or editing one panel.
    /// </summary>
    /// <returns>Group box containing the panel editor.</returns>
    private GroupBox BuildPanelEditorGroup()
    {
        var group = CreateGroupBox("Panel Editor");

        var editor = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 5,
            Padding = new Padding(12, 18, 12, 12)
        };
        editor.RowStyles.Add(new RowStyle(SizeType.Absolute, 58));
        editor.RowStyles.Add(new RowStyle(SizeType.Absolute, 58));
        editor.RowStyles.Add(new RowStyle(SizeType.Absolute, 58));
        editor.RowStyles.Add(new RowStyle(SizeType.Absolute, 184));
        editor.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        ConfigureTextInput(_panelNameTextBox);
        ConfigureTextInput(_panelUrlTextBox);
        ConfigureNumericInput(_panelRefreshInput, minimum: 1, maximum: 3600, width: 90);

        var commandGrid = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 4
        };
        commandGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        commandGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        commandGrid.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));
        commandGrid.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));
        commandGrid.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));
        commandGrid.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));

        commandGrid.Controls.Add(CreateCommandButton("New Panel", (_, _) => ClearPanelEditor()), 0, 0);
        commandGrid.Controls.Add(CreateCommandButton("Add Panel", (_, _) => AddPanel()), 1, 0);
        commandGrid.Controls.Add(CreateCommandButton("Apply", (_, _) => ApplySelectedPanel()), 0, 1);
        commandGrid.Controls.Add(CreateCommandButton("Duplicate", (_, _) => DuplicateSelectedPanel()), 1, 1);
        commandGrid.Controls.Add(CreateCommandButton("Move Up", (_, _) => MoveSelectedPanel(-1)), 0, 2);
        commandGrid.Controls.Add(CreateCommandButton("Move Down", (_, _) => MoveSelectedPanel(1)), 1, 2);
        var editMonitoringButton = CreateCommandButton("Edit Monitoring JSON", (_, _) => EditSelectedPanelMonitoring());
        commandGrid.Controls.Add(editMonitoringButton, 0, 3);
        commandGrid.SetColumnSpan(editMonitoringButton, 2);

        var deleteButton = CreateCommandButton("Delete", (_, _) => DeleteSelectedPanel());
        deleteButton.BackColor = Color.FromArgb(92, 38, 38);

        editor.Controls.Add(CreateField("Name", _panelNameTextBox), 0, 0);
        editor.Controls.Add(CreateField("URL", _panelUrlTextBox), 0, 1);
        editor.Controls.Add(CreateField("Refresh seconds", _panelRefreshInput), 0, 2);
        editor.Controls.Add(commandGrid, 0, 3);
        editor.Controls.Add(deleteButton, 0, 4);

        group.Controls.Add(editor);
        return group;
    }

    /// <summary>
    /// Builds Save and Cancel buttons.
    /// </summary>
    /// <returns>Footer panel.</returns>
    private FlowLayoutPanel BuildFooter()
    {
        var footer = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.RightToLeft,
            Padding = new Padding(0, 8, 0, 0),
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

        footer.Controls.Add(saveButton);
        footer.Controls.Add(cancelButton);
        footer.Controls.Add(exportButton);
        footer.Controls.Add(importButton);
        return footer;
    }

    /// <summary>
    /// Wires change handlers after initial values have been loaded.
    /// </summary>
    private void WireChangeHandlers()
    {
        _titleTextBox.TextChanged += (_, _) => RunSafely(() => MarkUnsavedChanges(), "tracking title changes");
        _rotationCheckBox.CheckedChanged += (_, _) => RunSafely(() => MarkUnsavedChanges(), "tracking rotation changes");
        _rotationSecondsInput.ValueChanged += (_, _) => RunSafely(() => MarkUnsavedChanges(), "tracking rotation interval changes");
        _defaultLayoutComboBox.SelectedIndexChanged += (_, _) => RunSafely(() => MarkUnsavedChanges(), "tracking layout changes");
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
        _rotationSecondsInput.Value = Math.Clamp(_configuration.RotationSeconds, 1, 3600);
        _defaultLayoutComboBox.SelectedItem = SupportedLayouts.Contains(_configuration.DefaultLayout)
            ? _configuration.DefaultLayout
            : 4;
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
            _panelGrid.Rows.Add(panel.Name, panel.Url, panel.RefreshSeconds, GetMonitoringStatus(panel));
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

        panel.Monitoring = CloneMonitoring(_configuration.Panels[index].Monitoring);
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
            RefreshSeconds = source.RefreshSeconds,
            Monitoring = CloneMonitoring(source.Monitoring)
        });
        RefreshPanelGrid(index + 1);
        MarkUnsavedChanges("Panel duplicated");
    }

    /// <summary>
    /// Opens an advanced JSON editor for the selected panel's monitoring rules.
    /// Monitoring is kept as JSON because selector-based alerting is highly page-specific. A general
    /// purpose text editor is more flexible than trying to predict every possible DOM rule in form fields.
    /// </summary>
    private void EditSelectedPanelMonitoring()
    {
        var index = GetSelectedPanelIndex();

        if (index < 0)
        {
            ShowValidationMessage("Select a panel before editing monitoring JSON.");
            return;
        }

        if (!ApplySelectedPanelEditorChanges(showValidation: true))
        {
            return;
        }

        // Apply the basic panel editor first so the monitoring dialog title and saved panel row stay
        // synchronized with any name, URL, or refresh edits the user has already typed.
        var panel = _configuration.Panels[index];
        var initialJson = panel.Monitoring is null
            ? string.Empty
            : JsonSerializer.Serialize(panel.Monitoring, MonitoringJsonOptions);

        using var editor = new MonitoringJsonEditorForm(panel.Name, initialJson, CreateDefaultMonitoringJson());

        while (editor.ShowDialog(this) == DialogResult.OK)
        {
            if (TryParseMonitoringJson(editor.JsonText, out var monitoring, out var errorMessage))
            {
                panel.Monitoring = monitoring;
                UpdatePanelGridRow(index, panel);
                MarkUnsavedChanges(monitoring is null ? "Monitoring disabled" : "Monitoring JSON applied");
                return;
            }

            MessageBox.Show(
                editor,
                errorMessage,
                "Invalid Monitoring JSON",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
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
                ShowValidationMessage("Enter an HTTP/HTTPS URL or a root-relative local path such as /status/index.html.");
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
        _panelGrid.Rows[index].Cells[3].Value = GetMonitoringStatus(panel);
    }

    /// <summary>
    /// Returns the compact monitoring status shown in the panel list.
    /// </summary>
    /// <param name="panel">Panel configuration.</param>
    /// <returns>On or Off.</returns>
    private static string GetMonitoringStatus(WallboardPanel panel)
    {
        return panel.Monitoring?.Enabled == true ? "On" : "Off";
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
    /// Validates absolute HTTP/HTTPS URLs and root-relative local paths.
    /// </summary>
    /// <param name="url">URL text from the editor.</param>
    /// <returns>True when the app can navigate to the value.</returns>
    private static bool IsValidPanelUrl(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            return false;
        }

        if (url.StartsWith('/'))
        {
            return true;
        }

        return Uri.TryCreate(url, UriKind.Absolute, out var uri)
            && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
    }

    /// <summary>
    /// Parses and validates advanced panel monitoring JSON.
    /// Empty text and literal null intentionally disable monitoring. Enabled monitoring must have at
    /// least one selector-based rule so the runtime never starts a polling timer with nothing to evaluate.
    /// </summary>
    /// <param name="json">Monitoring JSON text.</param>
    /// <param name="monitoring">Parsed monitoring settings, or null when disabled.</param>
    /// <param name="errorMessage">Validation failure message.</param>
    /// <returns>True when JSON is valid.</returns>
    private static bool TryParseMonitoringJson(
        string json,
        out PanelMonitoringOptions? monitoring,
        out string errorMessage)
    {
        monitoring = null;
        errorMessage = string.Empty;

        if (string.IsNullOrWhiteSpace(json) ||
            string.Equals(json.Trim(), "null", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        try
        {
            monitoring = JsonSerializer.Deserialize<PanelMonitoringOptions>(json, MonitoringJsonOptions);
        }
        catch (JsonException ex)
        {
            errorMessage = $"The monitoring JSON is not valid.\n\n{ex.Message}";
            return false;
        }

        if (monitoring is null || !monitoring.Enabled)
        {
            monitoring = null;
            return true;
        }

        // Clamp timer values here so the editor preview/save path behaves like WallboardConfigReader's
        // runtime normalization path.
        monitoring.PollSeconds = Math.Clamp(monitoring.PollSeconds, 1, 300);
        monitoring.RepeatSoundSeconds = Math.Clamp(monitoring.RepeatSoundSeconds, 1, 300);

        var validRules = new List<PanelMonitoringRule>();

        foreach (var rule in monitoring.Rules ?? [])
        {
            if (string.IsNullOrWhiteSpace(rule.Selector))
            {
                errorMessage = "Every monitoring rule must include a non-empty selector.";
                return false;
            }

            // Normalize each rule in place before saving so the persisted JSON is predictable even when
            // the user enters mixed casing or extra whitespace.
            rule.Name = string.IsNullOrWhiteSpace(rule.Name) ? "DOM Alert" : rule.Name.Trim();
            rule.Type = NormalizeMonitoringRuleType(rule.Type);
            rule.Selector = rule.Selector.Trim();
            rule.Contains = string.IsNullOrWhiteSpace(rule.Contains) ? null : rule.Contains.Trim();
            rule.Severity = NormalizeMonitoringSeverity(rule.Severity);
            rule.DetailsSelector = string.IsNullOrWhiteSpace(rule.DetailsSelector)
                ? null
                : rule.DetailsSelector.Trim();
            validRules.Add(rule);
        }

        if (validRules.Count == 0)
        {
            errorMessage = "Enabled monitoring must include at least one valid rule.";
            return false;
        }

        monitoring.Rules = validRules;
        return true;
    }

    /// <summary>
    /// Creates a starter monitoring JSON block for a panel with no monitoring configuration.
    /// The template demonstrates the most common use case: watch for a visible alarm modal title and
    /// collect details from the modal body.
    /// </summary>
    /// <returns>Formatted JSON template.</returns>
    private static string CreateDefaultMonitoringJson()
    {
        var monitoring = new PanelMonitoringOptions
        {
            Enabled = true,
            PollSeconds = 3,
            SoundEnabled = true,
            RepeatSoundSeconds = 5,
            Rules =
            [
                new PanelMonitoringRule
                {
                    Name = "PLC Alarm Modal",
                    Type = "domText",
                    Selector = "#divAlarm.on .alarmTitle",
                    Contains = "PLC Alarm Detected",
                    Severity = "critical",
                    DetailsSelector = "#divAlarm.on .alarmDiv"
                }
            ]
        };

        return JsonSerializer.Serialize(monitoring, MonitoringJsonOptions);
    }

    /// <summary>
    /// Normalizes monitoring rule type names for saved JSON.
    /// </summary>
    /// <param name="type">Configured type.</param>
    /// <returns>Supported type.</returns>
    private static string NormalizeMonitoringRuleType(string? type)
    {
        return type?.Trim().ToLowerInvariant() switch
        {
            "domtext" => "domText",
            "domclass" => "domClass",
            "exists" => "exists",
            _ => "exists"
        };
    }

    /// <summary>
    /// Normalizes monitoring severity names for saved JSON.
    /// </summary>
    /// <param name="severity">Configured severity.</param>
    /// <returns>Supported severity.</returns>
    private static string NormalizeMonitoringSeverity(string? severity)
    {
        return severity?.Trim().ToLowerInvariant() switch
        {
            "critical" => "critical",
            "info" => "info",
            _ => "warning"
        };
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
            Panels = configuration.Panels
                .Select(panel => new WallboardPanel
                {
                    Name = panel.Name,
                    Url = panel.Url,
                    RefreshSeconds = panel.RefreshSeconds,
                    Monitoring = CloneMonitoring(panel.Monitoring)
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
                Name = string.IsNullOrWhiteSpace(panel.Name) ? "Monitoring Panel" : panel.Name.Trim(),
                Url = panel.Url.Trim(),
                RefreshSeconds = panel.RefreshSeconds <= 0 ? 30 : panel.RefreshSeconds,
                Monitoring = NormalizeMonitoringForEditor(panel.Monitoring)
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
    /// Normalizes imported monitoring enough for the settings editor and panel grid.
    /// </summary>
    /// <param name="monitoring">Imported monitoring options.</param>
    /// <returns>Editor-safe monitoring options, or null when disabled/invalid.</returns>
    private static PanelMonitoringOptions? NormalizeMonitoringForEditor(PanelMonitoringOptions? monitoring)
    {
        if (monitoring is null || !monitoring.Enabled)
        {
            return null;
        }

        var rules = (monitoring.Rules ?? [])
            .Where(rule => !string.IsNullOrWhiteSpace(rule.Selector))
            .Select(rule => new PanelMonitoringRule
            {
                Name = string.IsNullOrWhiteSpace(rule.Name) ? "DOM Alert" : rule.Name.Trim(),
                Type = NormalizeMonitoringRuleType(rule.Type),
                Selector = rule.Selector.Trim(),
                Contains = string.IsNullOrWhiteSpace(rule.Contains) ? null : rule.Contains.Trim(),
                Severity = NormalizeMonitoringSeverity(rule.Severity),
                DetailsSelector = string.IsNullOrWhiteSpace(rule.DetailsSelector)
                    ? null
                    : rule.DetailsSelector.Trim(),
                SoundEnabled = rule.SoundEnabled
            })
            .ToList();

        if (rules.Count == 0)
        {
            return null;
        }

        return new PanelMonitoringOptions
        {
            Enabled = true,
            PollSeconds = Math.Clamp(monitoring.PollSeconds, 1, 300),
            SoundEnabled = monitoring.SoundEnabled,
            RepeatSoundSeconds = Math.Clamp(monitoring.RepeatSoundSeconds, 1, 300),
            Rules = rules
        };
    }

    /// <summary>
    /// Creates an editable copy of optional advanced monitoring settings.
    /// Rules are copied one by one because they are mutable objects edited by the JSON dialog.
    /// </summary>
    /// <param name="monitoring">Source monitoring settings.</param>
    /// <returns>Deep copy, or null when monitoring is disabled.</returns>
    private static PanelMonitoringOptions? CloneMonitoring(PanelMonitoringOptions? monitoring)
    {
        if (monitoring is null)
        {
            return null;
        }

        return new PanelMonitoringOptions
        {
            Enabled = monitoring.Enabled,
            PollSeconds = monitoring.PollSeconds,
            SoundEnabled = monitoring.SoundEnabled,
            RepeatSoundSeconds = monitoring.RepeatSoundSeconds,
            Rules = monitoring.Rules
                .Select(rule => new PanelMonitoringRule
                {
                    Name = rule.Name,
                    Type = rule.Type,
                    Selector = rule.Selector,
                    Contains = rule.Contains,
                    Severity = rule.Severity,
                    DetailsSelector = rule.DetailsSelector,
                    SoundEnabled = rule.SoundEnabled
                })
                .ToList()
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
            Height = 50,
            Margin = new Padding(0, 0, 12, 0)
        };

        var labelControl = new Label
        {
            Dock = DockStyle.Top,
            Height = 20,
            Text = label,
            ForeColor = MutedTextColor
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
            Height = 34,
            Width = 120,
            Margin = new Padding(4)
        };
        button.FlatAppearance.BorderColor = BorderColor;
        button.Click += handler;
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

/// <summary>
/// Modal text editor for a panel's advanced monitoring JSON.
/// </summary>
internal sealed class MonitoringJsonEditorForm : Form
{
    private static readonly JsonSerializerOptions MonitoringJsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
        WriteIndented = true
    };

    private static readonly Color WindowBackColor = Color.FromArgb(17, 24, 39);
    private static readonly Color SurfaceColor = Color.FromArgb(31, 41, 55);
    private static readonly Color InputColor = Color.FromArgb(15, 23, 42);
    private static readonly Color BorderColor = Color.FromArgb(75, 85, 99);
    private static readonly Color PrimaryTextColor = Color.FromArgb(243, 244, 246);
    private static readonly Color SecondaryTextColor = Color.FromArgb(209, 213, 219);
    private static readonly Color AccentColor = Color.FromArgb(8, 145, 178);

    private readonly TextBox _jsonTextBox = new();
    private readonly TextBox _ruleNameTextBox = new();
    private readonly ComboBox _ruleTypeComboBox = new();
    private readonly TextBox _ruleSelectorTextBox = new();
    private readonly TextBox _ruleContainsTextBox = new();
    private readonly ComboBox _ruleSeverityComboBox = new();
    private readonly TextBox _ruleDetailsSelectorTextBox = new();
    private readonly CheckBox _ruleSoundCheckBox = new();
    private readonly string _templateJson;

    /// <summary>
    /// Builds the JSON editor dialog.
    /// </summary>
    /// <param name="panelName">Panel name shown in the title.</param>
    /// <param name="jsonText">Initial monitoring JSON.</param>
    /// <param name="templateJson">Starter template inserted on demand.</param>
    public MonitoringJsonEditorForm(string panelName, string jsonText, string templateJson)
    {
        _templateJson = templateJson;
        Text = $"Monitoring JSON - {panelName}";
        BackColor = WindowBackColor;
        ForeColor = PrimaryTextColor;
        Font = new Font("Segoe UI", 9F);
        FormBorderStyle = FormBorderStyle.Sizable;
        MaximizeBox = true;
        MinimizeBox = false;
        MinimumSize = new Size(760, 520);
        Size = new Size(900, 680);
        StartPosition = FormStartPosition.CenterParent;

        BuildLayout(jsonText);
    }

    /// <summary>
    /// Current JSON text from the editor.
    /// </summary>
    public string JsonText => _jsonTextBox.Text;

    /// <summary>
    /// Builds the instruction text, JSON editor, and command buttons.
    /// </summary>
    /// <param name="jsonText">Initial JSON text.</param>
    private void BuildLayout(string jsonText)
    {
        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 4,
            Padding = new Padding(14),
            BackColor = WindowBackColor
        };
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 180));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 50));

        var hintLabel = new Label
        {
            Dock = DockStyle.Fill,
            ForeColor = SecondaryTextColor,
            TextAlign = ContentAlignment.MiddleLeft,
            Text = "Edit this panel's monitoring block. Leave empty to disable DOM monitoring."
        };

        _jsonTextBox.AcceptsReturn = true;
        _jsonTextBox.AcceptsTab = true;
        _jsonTextBox.BackColor = InputColor;
        _jsonTextBox.BorderStyle = BorderStyle.FixedSingle;
        _jsonTextBox.Dock = DockStyle.Fill;
        _jsonTextBox.Font = new Font("Consolas", 10F);
        _jsonTextBox.ForeColor = PrimaryTextColor;
        _jsonTextBox.Multiline = true;
        _jsonTextBox.ScrollBars = ScrollBars.Both;
        _jsonTextBox.Text = jsonText;
        _jsonTextBox.WordWrap = false;

        var footer = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.RightToLeft,
            Padding = new Padding(0, 8, 0, 0),
            WrapContents = false
        };

        var applyButton = CreateDialogButton("Apply", DialogResult.OK);
        applyButton.BackColor = AccentColor;
        var cancelButton = CreateDialogButton("Cancel", DialogResult.Cancel);
        var templateButton = CreateDialogButton("Insert Template", DialogResult.None);
        templateButton.Width = 130;
        templateButton.Click += (_, _) => InsertTemplate();
        var disableButton = CreateDialogButton("Disable", DialogResult.OK);
        disableButton.Click += (_, _) => _jsonTextBox.Clear();

        footer.Controls.Add(applyButton);
        footer.Controls.Add(cancelButton);
        footer.Controls.Add(disableButton);
        footer.Controls.Add(templateButton);

        root.Controls.Add(hintLabel, 0, 0);
        root.Controls.Add(BuildRuleBuilder(), 0, 1);
        root.Controls.Add(_jsonTextBox, 0, 2);
        root.Controls.Add(footer, 0, 3);
        Controls.Add(root);

        AcceptButton = applyButton;
        CancelButton = cancelButton;
    }

    /// <summary>
    /// Builds a basic rule builder that appends selector-based rules to the JSON editor.
    /// Advanced users can still edit the generated JSON directly.
    /// </summary>
    /// <returns>Rule builder group.</returns>
    private GroupBox BuildRuleBuilder()
    {
        var group = new GroupBox
        {
            Dock = DockStyle.Fill,
            Text = "Basic Rule Builder",
            ForeColor = SecondaryTextColor,
            Padding = new Padding(8),
            BackColor = WindowBackColor
        };

        var grid = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 4,
            RowCount = 3,
            Padding = new Padding(6, 10, 6, 6)
        };
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 26));
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 24));
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 26));
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 24));
        grid.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));
        grid.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));
        grid.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));

        ConfigureTextInput(_ruleNameTextBox);
        ConfigureTextInput(_ruleSelectorTextBox);
        ConfigureTextInput(_ruleContainsTextBox);
        ConfigureTextInput(_ruleDetailsSelectorTextBox);
        ConfigureComboBox(_ruleTypeComboBox, ["exists", "domText", "domClass"]);
        ConfigureComboBox(_ruleSeverityComboBox, ["warning", "critical", "info"]);

        _ruleNameTextBox.Text = "DOM Alert";
        _ruleSoundCheckBox.Text = "Sound";
        _ruleSoundCheckBox.Checked = true;
        _ruleSoundCheckBox.ForeColor = PrimaryTextColor;
        _ruleSoundCheckBox.AutoSize = true;
        _ruleSoundCheckBox.Margin = new Padding(8, 24, 0, 0);

        var addButton = CreateDialogButton("Add Rule", DialogResult.None);
        addButton.BackColor = AccentColor;
        addButton.Click += (_, _) => AddRuleFromBuilder();

        var detailsField = CreateField("Details selector", _ruleDetailsSelectorTextBox);
        grid.Controls.Add(CreateField("Name", _ruleNameTextBox), 0, 0);
        grid.Controls.Add(CreateField("Type", _ruleTypeComboBox), 1, 0);
        grid.Controls.Add(CreateField("Selector", _ruleSelectorTextBox), 2, 0);
        grid.Controls.Add(CreateField("Text contains", _ruleContainsTextBox), 3, 0);
        grid.Controls.Add(CreateField("Severity", _ruleSeverityComboBox), 0, 1);
        grid.Controls.Add(detailsField, 1, 1);
        grid.SetColumnSpan(detailsField, 2);
        grid.Controls.Add(_ruleSoundCheckBox, 3, 1);
        grid.Controls.Add(addButton, 3, 2);

        group.Controls.Add(grid);
        return group;
    }

    /// <summary>
    /// Reads the basic rule builder fields and appends a rule to the monitoring JSON editor.
    /// </summary>
    private void AddRuleFromBuilder()
    {
        var selector = _ruleSelectorTextBox.Text.Trim();

        if (string.IsNullOrWhiteSpace(selector))
        {
            MessageBox.Show(
                this,
                "Enter a CSS selector for the rule.",
                "Rule Builder",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
            _ruleSelectorTextBox.Focus();
            return;
        }

        if (!TryGetMonitoringFromEditor(out var monitoring, out var errorMessage))
        {
            MessageBox.Show(
                this,
                errorMessage,
                "Rule Builder",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
            return;
        }

        monitoring ??= new PanelMonitoringOptions
        {
            Enabled = true,
            PollSeconds = 3,
            SoundEnabled = true,
            RepeatSoundSeconds = 5,
            Rules = []
        };

        monitoring.Enabled = true;
        monitoring.Rules ??= [];
        monitoring.Rules.Add(new PanelMonitoringRule
        {
            Name = string.IsNullOrWhiteSpace(_ruleNameTextBox.Text)
                ? "DOM Alert"
                : _ruleNameTextBox.Text.Trim(),
            Type = _ruleTypeComboBox.SelectedItem?.ToString() ?? "exists",
            Selector = selector,
            Contains = string.IsNullOrWhiteSpace(_ruleContainsTextBox.Text)
                ? null
                : _ruleContainsTextBox.Text.Trim(),
            Severity = _ruleSeverityComboBox.SelectedItem?.ToString() ?? "warning",
            DetailsSelector = string.IsNullOrWhiteSpace(_ruleDetailsSelectorTextBox.Text)
                ? null
                : _ruleDetailsSelectorTextBox.Text.Trim(),
            SoundEnabled = _ruleSoundCheckBox.Checked ? null : false
        });

        _jsonTextBox.Text = JsonSerializer.Serialize(monitoring, MonitoringJsonOptions);
        _ruleSelectorTextBox.Clear();
        _ruleContainsTextBox.Clear();
        _ruleDetailsSelectorTextBox.Clear();
        _ruleSoundCheckBox.Checked = true;
        _ruleSelectorTextBox.Focus();
    }

    /// <summary>
    /// Parses the current JSON editor content so the rule builder can append to existing settings.
    /// </summary>
    /// <param name="monitoring">Parsed monitoring options, or null when the editor is empty.</param>
    /// <param name="errorMessage">Parse error shown to the operator.</param>
    /// <returns>True when the current editor content can be used.</returns>
    private bool TryGetMonitoringFromEditor(
        out PanelMonitoringOptions? monitoring,
        out string errorMessage)
    {
        monitoring = null;
        errorMessage = string.Empty;
        var json = _jsonTextBox.Text.Trim();

        if (string.IsNullOrWhiteSpace(json) ||
            string.Equals(json, "null", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        try
        {
            monitoring = JsonSerializer.Deserialize<PanelMonitoringOptions>(
                json,
                MonitoringJsonOptions);
            return true;
        }
        catch (JsonException ex)
        {
            errorMessage = $"The current monitoring JSON is invalid. Fix it or clear it before adding a rule.\n\n{ex.Message}";
            return false;
        }
    }

    /// <summary>
    /// Applies shared text box styling inside the monitoring dialog.
    /// </summary>
    /// <param name="textBox">Text box to configure.</param>
    private static void ConfigureTextInput(TextBox textBox)
    {
        textBox.BackColor = InputColor;
        textBox.BorderStyle = BorderStyle.FixedSingle;
        textBox.ForeColor = PrimaryTextColor;
        textBox.Width = 180;
    }

    /// <summary>
    /// Applies shared combo box styling and values inside the monitoring dialog.
    /// </summary>
    /// <param name="comboBox">Combo box to configure.</param>
    /// <param name="items">Values to add.</param>
    private static void ConfigureComboBox(ComboBox comboBox, string[] items)
    {
        comboBox.BackColor = InputColor;
        comboBox.DropDownStyle = ComboBoxStyle.DropDownList;
        comboBox.ForeColor = PrimaryTextColor;
        comboBox.Width = 150;
        comboBox.Items.Clear();
        comboBox.Items.AddRange(items);

        if (comboBox.Items.Count > 0)
        {
            comboBox.SelectedIndex = 0;
        }
    }

    /// <summary>
    /// Wraps a dialog label and control in a compact vertical field.
    /// </summary>
    /// <param name="label">Field label.</param>
    /// <param name="control">Input control.</param>
    /// <returns>Field container.</returns>
    private static Panel CreateField(string label, Control control)
    {
        var container = new Panel
        {
            Dock = DockStyle.Fill,
            Margin = new Padding(0, 0, 8, 0)
        };

        var labelControl = new Label
        {
            Dock = DockStyle.Top,
            Height = 18,
            Text = label,
            ForeColor = SecondaryTextColor
        };

        control.Dock = DockStyle.Bottom;
        container.Controls.Add(control);
        container.Controls.Add(labelControl);
        return container;
    }

    /// <summary>
    /// Creates a styled dialog button.
    /// </summary>
    /// <param name="text">Button text.</param>
    /// <param name="dialogResult">Dialog result assigned to the button.</param>
    /// <returns>Configured button.</returns>
    private static Button CreateDialogButton(string text, DialogResult dialogResult)
    {
        var button = new Button
        {
            Text = text,
            DialogResult = dialogResult,
            FlatStyle = FlatStyle.Flat,
            ForeColor = PrimaryTextColor,
            BackColor = SurfaceColor,
            Height = 34,
            Width = 110,
            Margin = new Padding(4)
        };
        button.FlatAppearance.BorderColor = BorderColor;
        return button;
    }

    /// <summary>
    /// Inserts the starter monitoring template after confirming replacement of existing text.
    /// </summary>
    private void InsertTemplate()
    {
        var currentText = _jsonTextBox.Text.Trim();

        if (!string.IsNullOrWhiteSpace(currentText) &&
            !string.Equals(currentText, "null", StringComparison.OrdinalIgnoreCase))
        {
            var result = MessageBox.Show(
                this,
                "Replace the current monitoring JSON with the starter template?",
                "Insert Template",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (result != DialogResult.Yes)
            {
                return;
            }
        }

        _jsonTextBox.Text = _templateJson;
        _jsonTextBox.Focus();
    }
}
