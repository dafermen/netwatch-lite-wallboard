namespace NetWatchLite.Wallboard.WebView2;

/// <summary>Applies the embedded multi-resolution icon without depending on external asset paths.</summary>
internal static class AppIcon
{
    internal const string ResourceName = "NetWatchLite.Wallboard.AppIcon.ico";

    internal static void ApplyTo(Form form)
    {
        using var stream = typeof(AppIcon).Assembly.GetManifestResourceStream(ResourceName)
            ?? throw new InvalidOperationException("The embedded application icon is missing.");
        using var resource = new Icon(stream);
        var ownedIcon = (Icon)resource.Clone();
        form.Icon = ownedIcon;
        form.ShowIcon = true;
        // Form.Icon can be shared, so WinForms does not own its lifetime.
        form.Disposed += (_, _) => ownedIcon.Dispose();
    }
}
