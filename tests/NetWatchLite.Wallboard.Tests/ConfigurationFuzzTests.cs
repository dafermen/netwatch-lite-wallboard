using System.Text;
using System.Text.Json;
using NetWatchLite.Wallboard.WebView2;
using Xunit;
using Xunit.Abstractions;

namespace NetWatchLite.Wallboard.Tests;

public sealed class ConfigurationFuzzTests(ITestOutputHelper output)
{
    [Fact]
    [Trait("Category", "Fuzz")]
    public async Task BoundedByteMutationsEitherNormalizeOrRejectWithoutWritingFiles()
    {
        const int seed = 824107;
        var random = new Random(seed);
        var root = Path.Combine(Path.GetTempPath(), "NetWatchFuzz", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        var path = Path.Combine(root, "wallboard.fuzz.json");
        var sentinel = Path.Combine(root, "wallboard.json");
        await File.WriteAllTextAsync(sentinel, "unchanged sentinel");
        var corpus = new[]
        {
            "{}", "null", "[]", "\"text\"", "{\"panels\":[null,{}, {\"url\":\"https://example.org/%FF?q=%00\"}]}",
            "{\"rotationSeconds\":2147483648}", "{\"theme\":{},\"panels\":{}}",
            new string('[', 80) + "0" + new string(']', 80),
            "{\"panels\":[{\"url\":\"javascript:alert(1)\"}],\"future\":true}",
            "{\"appTitle\":\"\\uD800\",\"rotationSeconds\":-2147483648,}"
        }.Select(Encoding.UTF8.GetBytes).ToArray();
        var accepted = 0;
        var rejected = 0;
        try
        {
            for (var index = 0; index < 1000; index++)
            {
                var bytes = corpus[index % corpus.Length].ToArray();
                if (index >= corpus.Length)
                {
                    switch (random.Next(3))
                    {
                        case 0: bytes = bytes[..random.Next(bytes.Length + 1)]; break;
                        case 1: bytes[random.Next(bytes.Length)] = (byte)random.Next(256); break;
                        default: bytes = bytes.Concat(new[] { (byte)random.Next(256) }).ToArray(); break;
                    }
                }
                await File.WriteAllBytesAsync(path, bytes);
                try
                {
                    using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));
                    var result = await WallboardConfigReader.LoadProfileForSelectionAsync(root, "fuzz", timeout.Token);
                    Assert.NotEmpty(result.Panels);
                    Assert.InRange(result.RotationSeconds, 1, int.MaxValue / 1000);
                    accepted++;
                }
                catch (JsonException) { rejected++; }
                Assert.Equal(bytes, await File.ReadAllBytesAsync(path));
                Assert.Equal("unchanged sentinel", await File.ReadAllTextAsync(sentinel));
                Assert.Equal(2, Directory.GetFiles(root).Length);
            }
            Assert.True(accepted > 0 && rejected > 0);
            output.WriteLine($"Seed={seed}; cases=1000; accepted={accepted}; rejected={rejected}; max input < 1 KiB.");
        }
        finally { Directory.Delete(root, recursive: true); }
    }
}
