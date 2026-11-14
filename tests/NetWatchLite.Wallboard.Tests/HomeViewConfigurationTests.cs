using System.Text.Json;
using FsCheck.Xunit;
using NetWatchLite.Wallboard.WebView2;
using Xunit;

namespace NetWatchLite.Wallboard.Tests;

public sealed class HomeViewConfigurationTests
{
    [Fact]
    public async Task OldProfilesContinueToStartInWallboardWithoutEnablingHome()
    {
        var configuration = await WallboardConfigReader.LoadFileAsync(Path.Combine(AppContext.BaseDirectory, "fixtures", "wallboard-v0.3.0.json"));
        Assert.False(configuration.HomePageEnabled);
        Assert.Empty(configuration.HomePageUrl);
        Assert.Equal("wallboard", configuration.StartupView);
        Assert.Equal(WallboardViewMode.Wallboard, WallboardViewOptions.StartupMode(configuration));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("javascript:alert(1)")]
    [InlineData("data:text/html,example")]
    [InlineData("mailto:example@example.org")]
    public void UnsafeOrMissingHomeUrlsCannotEnableHomeOrSelectItAtStartup(string? url)
    {
        var result = WallboardConfigReader.Normalize(new WallboardConfiguration { HomePageEnabled = true, HomePageUrl = url!, StartupView = "home" });
        Assert.False(result.HomePageEnabled);
        Assert.Empty(result.HomePageUrl);
        Assert.Equal("wallboard", result.StartupView);
    }

    [Theory]
    [InlineData("https://example.org/", " HOME ", "home")]
    [InlineData("http://example.org/", "wallboard", "wallboard")]
    [InlineData("pages/home.html", "home", "home")]
    [InlineData("/company/index.html", "invalid", "wallboard")]
    [InlineData("file:///C:/Company/index.html", null, "wallboard")]
    public void HomeUsesExistingPageUrlRulesAndOnlyExplicitStartupHome(string url, string? startup, string expected)
    {
        var result = WallboardConfigReader.Normalize(new WallboardConfiguration { HomePageEnabled = true, HomePageUrl = " " + url + " ", StartupView = startup! });
        Assert.True(result.HomePageEnabled);
        Assert.Equal(url, result.HomePageUrl);
        Assert.Equal(expected, result.StartupView);
    }

    [Fact]
    public void DisabledHomeRetainsItsUrlWithoutChangingStartupBehavior()
    {
        var result = WallboardConfigReader.Normalize(new WallboardConfiguration
        { HomePageEnabled = false, HomePageUrl = "https://example.org/", StartupView = "home" });
        Assert.False(result.HomePageEnabled);
        Assert.Equal("https://example.org/", result.HomePageUrl);
        Assert.Equal("wallboard", result.StartupView);
    }

    [Fact]
    public async Task SaveAndBackupPreserveHomeFieldsAndDoNotTouchAnotherProfile()
    {
        var directory = Path.Combine(Path.GetTempPath(), "NetWatchHome", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        try
        {
            var target = Path.Combine(directory, "wallboard.home.json");
            var other = Path.Combine(directory, "wallboard.production.json");
            await File.WriteAllTextAsync(other, "unchanged other profile");
            var configuration = await WallboardConfigReader.LoadFileAsync(Path.Combine(AppContext.BaseDirectory, "fixtures", "wallboard-home.json"));
            await WallboardConfigReader.SaveFileAsync(configuration, target);
            var original = await File.ReadAllBytesAsync(target);
            configuration.HomePageUrl = "https://example.net/company";
            await WallboardConfigReader.SaveFileAsync(configuration, target);
            var saved = await WallboardConfigReader.LoadFileAsync(target);
            Assert.True(saved.HomePageEnabled);
            Assert.Equal("home", saved.StartupView);
            Assert.Equal(configuration.HomePageUrl, saved.HomePageUrl);
            Assert.Equal(2, saved.DefaultLayout);
            Assert.Equal(new[] { 60, 90 }, saved.Panels.Select(panel => panel.RefreshSeconds));
            Assert.Equal(original, await File.ReadAllBytesAsync(Path.Combine(directory, "wallboard.home.backup.json")));
            Assert.Equal("unchanged other profile", await File.ReadAllTextAsync(other));
        }
        finally { Directory.Delete(directory, recursive: true); }
    }

    [Fact]
    public void SafeSupportReportDoesNotExportTheHomeUrl()
    {
        var configuration = new WallboardConfiguration { HomePageEnabled = true, HomePageUrl = "https://private.example.org/?secret=home-secret", StartupView = "home" };
        var report = SupportReport.Create(configuration, 0, [], [], 0);
        Assert.DoesNotContain("private.example", report);
        Assert.DoesNotContain("home-secret", report);
        Assert.DoesNotContain("homePageUrl", report);
    }

    [Property(MaxTest = 300)]
    public void HomeNormalizationIsIdempotentAndDoesNotMutatePanelOptions(bool enabled, string? url, string? startup)
    {
        var original = new WallboardConfiguration { HomePageEnabled = enabled, HomePageUrl = url!, StartupView = startup!, AutomaticRecoveryEnabled = false };
        var result = WallboardConfigReader.Normalize(original);
        Assert.Equal(JsonSerializer.Serialize(result), JsonSerializer.Serialize(WallboardConfigReader.Normalize(result)));
        Assert.False(result.AutomaticRecoveryEnabled);
        Assert.Equal(enabled && WallboardConfigReader.IsValidHomePageUrl(url), result.HomePageEnabled);
        if (result.StartupView == "home") Assert.True(WallboardViewOptions.CanShowHome(result));
    }
}
