using System.Diagnostics;
using System.Reflection;
using System.Text;

namespace NetWatchLite.Wallboard.WebView2;

/// <summary>
/// Read-only diagnostics window used by support to quickly verify what the running wallboard loaded.
/// It intentionally avoids live editing; Settings remains the only configuration editor.
/// </summary>
internal sealed class DiagnosticsForm : Form
{
    private readonly WallboardConfiguration _configuration;
    private readonly int _layout;
    private readonly int _currentPage;
    private readonly int _pageCount;
    private readonly int _activePanelCount;
    private readonly string _webViewUserDataFolder;
    private readonly TextBox _diagnosticsTextBox = new();

    /// <summary>
    /// Builds the diagnostics window.
    /// </summary>
    /// <param name="configuration">Current runtime configuration.</param>
    /// <param name="layout">Active layout size.</param>
    /// <param name="currentPage">Current page number.</param>
    /// <param name="pageCount">Total page count.</param>
    /// <param name="activePanelCount">Number of visible panel controls.</param>
    /// <param name="webViewUserDataFolder">WebView2 profile path.</param>
    public DiagnosticsForm(
        WallboardConfiguration configuration,
        int layout,
        int currentPage,
        int pageCount,
        int activePanelCount,
        string webViewUserDataFolder)
    {
        _configuration = configuration;
        _layout = layout;
        _currentPage = currentPage;
        _pageCount = pageCount;
        _activePanelCount = activePanelCount;
        _webViewUserDataFolder = webViewUserDataFolder;

        Text = "Diagnostics";
        BackColor = Color.FromArgb(17, 24, 39);
        ForeColor = Color.White;
        Font = new Font("Segoe UI", 9F);
        FormBorderStyle = FormBorderStyle.Sizable;
        MaximizeBox = true;
        MinimizeBox = false;
        MinimumSize = new Size(760, 540);
        Size = new Size(940, 680);
        SizeGripStyle = SizeGripStyle.Show;
        StartPosition = FormStartPosition.CenterParent;

        BuildLayout();
        RefreshDiagnosticsText();
    }

    /// <summary>
    /// Builds the read-only diagnostics text surface and command buttons.
    /// </summary>
    private void BuildLayout()
    {
        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            Padding = new Padding(14)
        };
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 52));

        _diagnosticsTextBox.BackColor = Color.FromArgb(15, 23, 42);
        _diagnosticsTextBox.BorderStyle = BorderStyle.FixedSingle;
        _diagnosticsTextBox.Dock = DockStyle.Fill;
        _diagnosticsTextBox.Font = new Font("Consolas", 10F);
        _diagnosticsTextBox.ForeColor = Color.FromArgb(243, 244, 246);
        _diagnosticsTextBox.Multiline = true;
        _diagnosticsTextBox.ReadOnly = true;
        _diagnosticsTextBox.ScrollBars = ScrollBars.Both;
        _diagnosticsTextBox.WordWrap = false;

        var footer = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.RightToLeft,
            Padding = new Padding(0, 8, 0, 0),
            WrapContents = false
        };
        footer.Controls.Add(CreateButton("Close", (_, _) => Close()));
        footer.Controls.Add(CreateButton("Copy", (_, _) => Clipboard.SetText(_diagnosticsTextBox.Text)));
        footer.Controls.Add(CreateButton("Refresh", (_, _) => RefreshDiagnosticsText()));

        root.Controls.Add(_diagnosticsTextBox, 0, 0);
        root.Controls.Add(footer, 0, 1);
        Controls.Add(root);
    }

    /// <summary>
    /// Rebuilds the diagnostics text from the current snapshot.
    /// </summary>
    private void RefreshDiagnosticsText()
    {
        var assembly = Assembly.GetExecutingAssembly().GetName();
        var process = Process.GetCurrentProcess();
        var monitoringPanels = _configuration.Panels.Count(panel => panel.Monitoring?.Enabled == true);
        var builder = new StringBuilder();

        builder.AppendLine("NetWatch Lite Wallboard Diagnostics");
        builder.AppendLine($"Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        builder.AppendLine($"Version: {assembly.Version}");
        builder.AppendLine($"Process: {process.ProcessName} ({process.Id})");
        builder.AppendLine();
        builder.AppendLine("Paths");
        builder.AppendLine($"Configuration: {WallboardConfigReader.GetConfigurationFilePath()}");
        builder.AppendLine($"Error log: {AppErrorLog.LogFilePath}");
        builder.AppendLine($"App base: {AppContext.BaseDirectory}");
        builder.AppendLine($"WebView2 data: {_webViewUserDataFolder}");
        builder.AppendLine();
        builder.AppendLine("Runtime");
        builder.AppendLine($"Title: {_configuration.AppTitle}");
        builder.AppendLine($"Layout: {_layout}");
        builder.AppendLine($"Page: {_currentPage} / {_pageCount}");
        builder.AppendLine($"Visible panels: {_activePanelCount}");
        builder.AppendLine($"Configured panels: {_configuration.Panels.Count}");
        builder.AppendLine($"Panels with DOM monitoring: {monitoringPanels}");
        builder.AppendLine($"Rotation enabled: {_configuration.RotationEnabled}");
        builder.AppendLine($"Rotation seconds: {_configuration.RotationSeconds}");
        builder.AppendLine($"Alarm sound: {_configuration.AlarmSound}");
        builder.AppendLine($"Critical color: {_configuration.SeverityColors.Critical}");
        builder.AppendLine($"Warning color: {_configuration.SeverityColors.Warning}");
        builder.AppendLine($"Info color: {_configuration.SeverityColors.Info}");
        builder.AppendLine();
        builder.AppendLine("Panels");

        for (var index = 0; index < _configuration.Panels.Count; index++)
        {
            var panel = _configuration.Panels[index];
            builder.AppendLine($"{index + 1}. {panel.Name}");
            builder.AppendLine($"   URL: {panel.Url}");
            builder.AppendLine($"   Refresh: {panel.RefreshSeconds}s");
            builder.AppendLine($"   Monitoring: {(panel.Monitoring?.Enabled == true ? "On" : "Off")}");

            if (panel.Monitoring?.Enabled == true)
            {
                builder.AppendLine($"   Poll: {panel.Monitoring.PollSeconds}s");
                builder.AppendLine($"   Rules: {panel.Monitoring.Rules.Count}");
            }
        }

        _diagnosticsTextBox.Text = builder.ToString();
    }

    /// <summary>
    /// Creates a styled diagnostics command button.
    /// </summary>
    /// <param name="text">Button text.</param>
    /// <param name="handler">Click handler.</param>
    /// <returns>Button.</returns>
    private static Button CreateButton(string text, EventHandler handler)
    {
        var button = new Button
        {
            BackColor = Color.FromArgb(31, 41, 55),
            FlatStyle = FlatStyle.Flat,
            ForeColor = Color.White,
            Height = 36,
            Margin = new Padding(4),
            Text = text,
            Width = 104
        };
        button.FlatAppearance.BorderColor = Color.FromArgb(75, 85, 99);
        button.Click += handler;
        return button;
    }
}
