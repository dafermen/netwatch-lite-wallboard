namespace NetWatchLite.Wallboard.WebView2;

/// <summary>
/// Secondary window used to edit <c>wallboard.json</c> without hand-editing JSON.
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
    private readonly NumericUpDown _rotationSecondsInput = new();
    private readonly ComboBox _defaultLayoutComboBox = new();
    private readonly DataGridView _panelGrid = new();
    private readonly TextBox _panelNameTextBox = new();
    private readonly TextBox _panelUrlTextBox = new();
    private readonly NumericUpDown _panelRefreshInput = new();
    private readonly Label _filePathLabel = new();
    private readonly Label _statusLabel = new();
    private readonly WallboardConfiguration _configuration;
    private bool _isLoadingSelection;

    /// <summary>
    /// Builds the settings editor from the current wallboard configuration.
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

        _panelGrid.Columns.Add(CreateTextColumn("Name", "Name", 24));
        _panelGrid.Columns.Add(CreateTextColumn("URL", "URL", 56));
        _panelGrid.Columns.Add(CreateTextColumn("Refresh", "Refresh", 20));
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
        editor.RowStyles.Add(new RowStyle(SizeType.Absolute, 142));
        editor.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        ConfigureTextInput(_panelNameTextBox);
        ConfigureTextInput(_panelUrlTextBox);
        ConfigureNumericInput(_panelRefreshInput, minimum: 1, maximum: 3600, width: 90);

        var commandGrid = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 3
        };
        commandGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        commandGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        commandGrid.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));
        commandGrid.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));
        commandGrid.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));

        commandGrid.Controls.Add(CreateCommandButton("New Panel", (_, _) => ClearPanelEditor()), 0, 0);
        commandGrid.Controls.Add(CreateCommandButton("Add Panel", (_, _) => AddPanel()), 1, 0);
        commandGrid.Controls.Add(CreateCommandButton("Apply", (_, _) => ApplySelectedPanel()), 0, 1);
        commandGrid.Controls.Add(CreateCommandButton("Duplicate", (_, _) => DuplicateSelectedPanel()), 1, 1);
        commandGrid.Controls.Add(CreateCommandButton("Move Up", (_, _) => MoveSelectedPanel(-1)), 0, 2);
        commandGrid.Controls.Add(CreateCommandButton("Move Down", (_, _) => MoveSelectedPanel(1)), 1, 2);

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

        var saveButton = CreateCommandButton("Save Changes", async (_, _) => await SaveConfigurationAsync());
        saveButton.Width = 140;
        saveButton.BackColor = AccentColor;

        var cancelButton = CreateCommandButton("Cancel", (_, _) => Close());
        cancelButton.Width = 110;

        footer.Controls.Add(saveButton);
        footer.Controls.Add(cancelButton);
        return footer;
    }

    /// <summary>
    /// Wires change handlers after initial values have been loaded.
    /// </summary>
    private void WireChangeHandlers()
    {
        _titleTextBox.TextChanged += (_, _) => MarkUnsavedChanges();
        _rotationCheckBox.CheckedChanged += (_, _) => MarkUnsavedChanges();
        _rotationSecondsInput.ValueChanged += (_, _) => MarkUnsavedChanges();
        _defaultLayoutComboBox.SelectedIndexChanged += (_, _) => MarkUnsavedChanges();
        _panelNameTextBox.TextChanged += (_, _) => ApplySelectedPanelEditorChanges(showValidation: false);
        _panelUrlTextBox.TextChanged += (_, _) => ApplySelectedPanelEditorChanges(showValidation: false);
        _panelRefreshInput.ValueChanged += (_, _) => ApplySelectedPanelEditorChanges(showValidation: false);
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
    /// Creates a deep copy of the configuration for editing.
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
                    RefreshSeconds = panel.RefreshSeconds
                })
                .ToList()
        };
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
