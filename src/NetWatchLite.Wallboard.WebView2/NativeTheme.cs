namespace NetWatchLite.Wallboard.WebView2;

/// <summary>Styles native dialogs only; never traverses a WebView or changes website content.</summary>
internal static class NativeTheme
{
    internal static void Apply(Control root, ThemePalette theme)
    {
        root.BackColor = theme.WindowBackColor;
        root.ForeColor = theme.PrimaryTextColor;
        switch (root)
        {
            case Button button:
                if (button is ThemeButton themed) themed.DisabledTextColor = theme.SecondaryTextColor;
                button.BackColor = button.Tag as string == "primary" ? theme.ActiveButtonBackColor : theme.ButtonBackColor;
                button.ForeColor = button.Tag as string == "primary" ? theme.AccentTextColor : theme.PrimaryTextColor;
                if (button.Tag as string == "danger") button.ForeColor = theme.WarningTextColor;
                button.FlatAppearance.BorderColor = theme.BorderColor;
                break;
            case ComboBox combo:
                combo.FlatStyle = FlatStyle.Flat;
                combo.BackColor = theme.PanelTitleBackColor;
                break;
            case TextBoxBase or NumericUpDown:
                root.BackColor = theme.PanelTitleBackColor;
                break;
            case DataGridView grid:
                grid.BackgroundColor = theme.WindowBackColor;
                grid.GridColor = theme.BorderColor;
                grid.DefaultCellStyle.BackColor = theme.PanelTitleBackColor;
                grid.DefaultCellStyle.ForeColor = theme.PrimaryTextColor;
                grid.DefaultCellStyle.SelectionBackColor = theme.ActiveButtonBackColor;
                grid.DefaultCellStyle.SelectionForeColor = theme.AccentTextColor;
                grid.AlternatingRowsDefaultCellStyle = new DataGridViewCellStyle(grid.DefaultCellStyle)
                { BackColor = theme.TopBarBackColor };
                grid.ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle(grid.DefaultCellStyle)
                { BackColor = theme.ButtonBackColor };
                break;
        }
        foreach (Control child in root.Controls)
            Apply(child, theme);
    }
}
