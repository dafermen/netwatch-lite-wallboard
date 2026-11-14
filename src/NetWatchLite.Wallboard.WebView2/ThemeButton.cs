namespace NetWatchLite.Wallboard.WebView2;

/// <summary>Keeps disabled commands readable on dark/custom high-contrast dialog surfaces.</summary>
internal sealed class ThemeButton : Button
{
    [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
    internal Color DisabledTextColor { get; set; } = SystemColors.GrayText;

    protected override void OnPaint(PaintEventArgs pevent)
    {
        base.OnPaint(pevent);
        if (Enabled) return;
        pevent.Graphics.Clear(BackColor);
        ControlPaint.DrawBorder(pevent.Graphics, ClientRectangle, FlatAppearance.BorderColor, ButtonBorderStyle.Solid);
        TextRenderer.DrawText(pevent.Graphics, Text, Font, ClientRectangle, DisabledTextColor,
            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine);
    }
}
