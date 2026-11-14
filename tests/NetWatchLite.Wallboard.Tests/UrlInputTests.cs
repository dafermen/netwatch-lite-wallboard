using FsCheck.Xunit;
using NetWatchLite.Wallboard.WebView2;
using Xunit;

namespace NetWatchLite.Wallboard.Tests;

public sealed class UrlInputTests
{
    [Theory]
    [InlineData("www.google.com", "https://www.google.com")]
    [InlineData(" google.com ", "https://google.com")]
    [InlineData("www.ma\u00f1ana.example", "https://www.ma\u00f1ana.example")]
    [InlineData("example.org/status?team=one", "https://example.org/status?team=one")]
    [InlineData("www.example.org:8443/status", "https://www.example.org:8443/status")]
    [InlineData("localhost:8080", "https://localhost:8080")]
    [InlineData("127.0.0.1:8080", "https://127.0.0.1:8080")]
    [InlineData("https://[::1]:8443/status", "https://[::1]:8443/status")]
    [InlineData("https://example.org/status", "https://example.org/status")]
    [InlineData("http://intranet/status", "http://intranet/status")]
    [InlineData("file:///C:/Company/Home.html", "file:///C:/Company/Home.html")]
    [InlineData("C:\\Company\\Home.html", "C:\\Company\\Home.html")]
    [InlineData("pages/home.html", "pages/home.html")]
    [InlineData("pages/report,one.html", "pages/report,one.html")]
    [InlineData("/company/index.html", "/company/index.html")]
    [InlineData("home.html", "home.html")]
    [InlineData("Company Home.html", "Company Home.html")]
    public void EditorCompletesWebHostsWithoutChangingExplicitUrlsOrLocalPages(string input, string expected)
    {
        Assert.True(UrlInput.TryNormalize(input, out var result));
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("www.google,com")]
    [InlineData("google,com")]
    [InlineData("www.google.com\\status")]
    [InlineData("https://www.google,com")]
    [InlineData("https://exa mple.org")]
    [InlineData("https://\u200d.example.org")]
    [InlineData("https://")]
    [InlineData("https:///example.org")]
    [InlineData("javascript:alert(1)")]
    [InlineData("data:text/html,example")]
    [InlineData("ftp://example.org/")]
    [InlineData("mailto:user@example.org")]
    [InlineData("user@example.org")]
    public void EditorRejectsMalformedHostsAndUnsupportedSchemesWithoutGuessing(string? input)
    {
        Assert.False(UrlInput.TryNormalize(input, out _));
    }

    [Theory]
    [InlineData("custom.local/page.html")]
    [InlineData("www.legacy.example.org")]
    public void AnUnchangedExistingValueIsValidatedButNeverSilentlyReinterpreted(string original)
    {
        Assert.True(UrlInput.TryNormalize(original, out var result, original));
        Assert.Equal(original, result);
        Assert.True(UrlInput.TryNormalize("www.new.example.org", out var edited, original));
        Assert.Equal("https://www.new.example.org", edited);
        Assert.False(UrlInput.TryNormalize("www.google,com", out _, "www.google,com"));
    }

    [Fact]
    public async Task ReadingLegacyConfigurationDoesNotRunEditorCompletionOrRewriteFiles()
    {
        var root = Path.Combine(Path.GetTempPath(), "NetWatchUrlInput", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            var path = Path.Combine(root, "wallboard.custom.json");
            const string original = """
                {"homePageEnabled":true,"homePageUrl":"www.legacy.example.org","startupView":"home",
                 "panels":[{"name":"Local","url":"custom.local/page.html","refreshSeconds":90}]}
                """;
            await File.WriteAllTextAsync(path, original);
            var configuration = await WallboardConfigReader.LoadFileAsync(path);
            Assert.Equal("www.legacy.example.org", configuration.HomePageUrl);
            Assert.Equal("custom.local/page.html", configuration.Panels.Single().Url);
            Assert.Equal(original, await File.ReadAllTextAsync(path));
            Assert.Single(Directory.GetFiles(root));
        }
        finally { Directory.Delete(root, recursive: true); }
    }

    [Property(MaxTest = 500)]
    public void SuccessfulEditorCompletionIsIdempotentAndFitsTheExistingReaderContract(string? input)
    {
        if (!UrlInput.TryNormalize(input, out var result)) return;
        Assert.True(WallboardConfigReader.IsValidHomePageUrl(result));
        Assert.True(UrlInput.TryNormalize(result, out var second));
        Assert.Equal(result, second);
    }
}
