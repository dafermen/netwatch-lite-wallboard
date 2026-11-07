using System.Drawing;
using NetWatchLite.Wallboard.WebView2;
using Xunit;

namespace NetWatchLite.Wallboard.Tests;

public sealed class AppIconTests
{
    [Fact]
    public void EmbeddedIconContainsEveryTransparentNonemptyFrame()
    {
        using var resource = typeof(AppIcon).Assembly.GetManifestResourceStream(AppIcon.ResourceName);
        Assert.NotNull(resource);
        using var reader = new BinaryReader(resource);
        Assert.Equal(0, reader.ReadUInt16());
        Assert.Equal(1, reader.ReadUInt16());
        var expectedSizes = new[] { 16, 24, 32, 48, 64, 128, 256 };
        Assert.Equal(expectedSizes.Length, reader.ReadUInt16());
        foreach (var size in expectedSizes)
        {
            Assert.Equal(size == 256 ? 0 : size, reader.ReadByte());
            Assert.Equal(size == 256 ? 0 : size, reader.ReadByte());
            reader.ReadBytes(2);
            Assert.Equal(1, reader.ReadUInt16());
            Assert.Equal(32, reader.ReadUInt16());
            var length = reader.ReadUInt32();
            var offset = reader.ReadUInt32();
            var nextEntry = resource.Position;
            resource.Position = offset;
            using var imageData = new MemoryStream(reader.ReadBytes(checked((int)length)));
            using var bitmap = new Bitmap(imageData);
            Assert.Equal(size, bitmap.Width);
            Assert.Equal(size, bitmap.Height);
            Assert.Equal(0, bitmap.GetPixel(0, 0).A);
            Assert.Contains(Enumerable.Range(0, size), y =>
                Enumerable.Range(0, size).Any(x => bitmap.GetPixel(x, y).A == 255));
            resource.Position = nextEntry;
        }
    }

    [Fact]
    public async Task MainSettingsAndDiagnosticsOwnIndependentIcons()
    {
        var completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var thread = new Thread(() =>
        {
            try
            {
                var configuration = new WallboardConfiguration();
                using var main = new WallboardForm();
                using var settings = new SettingsForm(configuration, _ => { });
                using var diagnostics = new DiagnosticsForm(configuration, 1, 0, 1, 0, Path.GetTempPath(), () => "Not sampled");
                foreach (var form in new Form[] { main, settings, diagnostics })
                {
                    Assert.True(form.ShowIcon);
                    Assert.NotNull(form.Icon);
                    using var bitmap = form.Icon.ToBitmap();
                    Assert.True(bitmap.Width >= 16);
                }
                Assert.NotSame(main.Icon, settings.Icon);
                Assert.NotSame(settings.Icon, diagnostics.Icon);
                main.Dispose();
                using var remainingIcon = settings.Icon!.ToBitmap();
                Assert.True(remainingIcon.Width >= 16);
                completion.SetResult();
            }
            catch (Exception ex) { completion.SetException(ex); }
        }) { IsBackground = true };
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        await completion.Task.WaitAsync(TimeSpan.FromSeconds(20));
    }
}
