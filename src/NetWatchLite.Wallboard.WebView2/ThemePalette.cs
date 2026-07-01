namespace NetWatchLite.Wallboard.WebView2;

/// <summary>
/// Central color palette used by the wallboard shell and panel chrome.
/// Web pages keep their own colors; the theme only affects the native Windows UI around them.
/// </summary>
internal sealed class ThemePalette
{
    private ThemePalette(
        string name,
        Color windowBackColor,
        Color topBarBackColor,
        Color panelBackColor,
        Color panelTitleBackColor,
        Color buttonBackColor,
        Color activeButtonBackColor,
        Color borderColor,
        Color primaryTextColor,
        Color secondaryTextColor,
        Color warningTextColor,
        Color webViewBackColor)
    {
        Name = name;
        WindowBackColor = windowBackColor;
        TopBarBackColor = topBarBackColor;
        PanelBackColor = panelBackColor;
        PanelTitleBackColor = panelTitleBackColor;
        ButtonBackColor = buttonBackColor;
        ActiveButtonBackColor = activeButtonBackColor;
        BorderColor = borderColor;
        PrimaryTextColor = primaryTextColor;
        SecondaryTextColor = secondaryTextColor;
        WarningTextColor = warningTextColor;
        WebViewBackColor = webViewBackColor;
    }

    public string Name { get; }

    public Color WindowBackColor { get; }

    public Color TopBarBackColor { get; }

    public Color PanelBackColor { get; }

    public Color PanelTitleBackColor { get; }

    public Color ButtonBackColor { get; }

    public Color ActiveButtonBackColor { get; }

    public Color BorderColor { get; }

    public Color PrimaryTextColor { get; }

    public Color SecondaryTextColor { get; }

    public Color WarningTextColor { get; }

    public Color WebViewBackColor { get; }

    public static ThemePalette FromName(string? themeName)
    {
        return NormalizeName(themeName) switch
        {
            "light" => new ThemePalette(
                "light",
                Color.FromArgb(241, 245, 249),
                Color.FromArgb(226, 232, 240),
                Color.FromArgb(203, 213, 225),
                Color.FromArgb(248, 250, 252),
                Color.FromArgb(226, 232, 240),
                Color.FromArgb(14, 116, 144),
                Color.FromArgb(148, 163, 184),
                Color.FromArgb(15, 23, 42),
                Color.FromArgb(71, 85, 105),
                Color.FromArgb(185, 28, 28),
                Color.White),
            "high-contrast" => new ThemePalette(
                "high-contrast",
                Color.Black,
                Color.Black,
                Color.Black,
                Color.Black,
                Color.Black,
                Color.FromArgb(255, 214, 10),
                Color.White,
                Color.White,
                Color.FromArgb(255, 214, 10),
                Color.FromArgb(255, 77, 95),
                Color.Black),
            _ => new ThemePalette(
                "dark",
                Color.FromArgb(5, 7, 10),
                Color.FromArgb(10, 13, 17),
                Color.Black,
                Color.FromArgb(12, 17, 23),
                Color.FromArgb(23, 29, 36),
                Color.FromArgb(0, 80, 96),
                Color.FromArgb(61, 74, 88),
                Color.White,
                Color.FromArgb(139, 155, 173),
                Color.FromArgb(255, 170, 80),
                Color.Black)
        };
    }

    public static string NormalizeName(string? themeName)
    {
        var normalized = themeName?.Trim().ToLowerInvariant();

        return normalized switch
        {
            "light" => "light",
            "highcontrast" or "high-contrast" or "high contrast" => "high-contrast",
            _ => "dark"
        };
    }
}
