using System.Security.Cryptography;
using System.Text;
using System.Xml;
using NetWatchLite.Wallboard.Launcher;
using Xunit;

namespace NetWatchLite.Wallboard.Tests;

public sealed class PackageIntegrityTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "NetWatchIntegrityTests", Guid.NewGuid().ToString("N"));
    private readonly byte[] _manifest;

    public PackageIntegrityTests()
    {
        Directory.CreateDirectory(_root);
        foreach (var path in PackageIntegrity.RequiredFiles.Concat(["app/es/example&messages.dll"]))
        {
            var full = Path.Combine(_root, path.Replace('/', Path.DirectorySeparatorChar));
            Directory.CreateDirectory(Path.GetDirectoryName(full)!);
            File.WriteAllText(full, "Immutable fixture: " + path);
        }
        _manifest = CreateManifest();
        File.WriteAllBytes(Path.Combine(_root, PackageIntegrity.ManifestName), _manifest);
    }

    private byte[] CreateManifest()
    {
        using var output = new MemoryStream();
        using (var writer = XmlWriter.Create(output, new XmlWriterSettings { Encoding = new UTF8Encoding(false), Indent = true }))
        {
            writer.WriteStartElement("package");
            writer.WriteAttributeString("schema", "1");
            foreach (var file in Directory.GetFiles(_root, "*", SearchOption.AllDirectories).Order(StringComparer.Ordinal))
            {
                var relative = Path.GetRelativePath(_root, file).Replace('\\', '/');
                if (relative != "portable-layout.json" && !relative.StartsWith("app/", StringComparison.Ordinal)) continue;
                writer.WriteStartElement("file");
                writer.WriteAttributeString("path", relative);
                writer.WriteAttributeString("length", new FileInfo(file).Length.ToString(System.Globalization.CultureInfo.InvariantCulture));
                writer.WriteAttributeString("sha256", Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(file))));
                writer.WriteEndElement();
            }
            writer.WriteEndElement();
        }
        return output.ToArray();
    }

    private List<string> ValidateDocument(Action<XmlDocument> change)
    {
        var document = new XmlDocument { XmlResolver = null };
        using (var input = new MemoryStream(_manifest)) document.Load(input);
        change(document);
        var expected = Encoding.UTF8.GetBytes(document.OuterXml);
        File.WriteAllBytes(Path.Combine(_root, PackageIntegrity.ManifestName), expected);
        return PackageIntegrity.Validate(_root, expected);
    }

    [Fact]
    public void CompletePayloadIncludingNestedXmlEscapedNamesPasses()
        => Assert.Empty(PackageIntegrity.Validate(_root, _manifest));

    [Theory]
    [InlineData("app/hostfxr.dll")]
    [InlineData("app/NetWatch-Lite-Wallboard.dll")]
    [InlineData("portable-layout.json")]
    public void MissingProtectedFileIsRejected(string relative)
    {
        File.Delete(Path.Combine(_root, relative.Replace('/', Path.DirectorySeparatorChar)));
        Assert.Contains(PackageIntegrity.Validate(_root, _manifest), error => error.Contains(relative, StringComparison.Ordinal));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void LengthChangesAndSameLengthCorruptionAreRejected(bool sameLength)
    {
        var path = Path.Combine(_root, "app", "coreclr.dll");
        var bytes = File.ReadAllBytes(path);
        if (sameLength) bytes[0] ^= 1;
        else bytes = bytes.Concat(new byte[] { 42 }).ToArray();
        File.WriteAllBytes(path, bytes);
        Assert.Contains(PackageIntegrity.Validate(_root, _manifest), error => error.Contains("app/coreclr.dll", StringComparison.Ordinal));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void MissingOrAlteredExternalManifestIsRejected(bool altered)
    {
        var path = Path.Combine(_root, PackageIntegrity.ManifestName);
        if (altered)
        {
            var bytes = (byte[])_manifest.Clone();
            bytes[^1] ^= 1;
            File.WriteAllBytes(path, bytes);
        }
        else File.Delete(path);
        Assert.Contains(PackageIntegrity.Validate(_root, _manifest), error => error.Contains(PackageIntegrity.ManifestName, StringComparison.Ordinal));
    }

    [Fact]
    public void EditableProfilesPagesAndBrowserDataAreNotInspectedOrRewritten()
    {
        foreach (var name in new[] { "wallboard.json", "wallboard.production.json", "wallboard.night.json" })
            File.WriteAllText(Path.Combine(_root, name), "{deliberately malformed editable fixture");
        Directory.CreateDirectory(Path.Combine(_root, "pages"));
        Directory.CreateDirectory(Path.Combine(_root, "browser-data"));
        File.WriteAllText(Path.Combine(_root, "pages", "dashboard.html"), "Operator page");
        File.WriteAllText(Path.Combine(_root, "browser-data", "Cookies"), "Do not touch");
        var before = Directory.GetFiles(_root, "*", SearchOption.AllDirectories)
            .ToDictionary(path => path, path => SHA256.HashData(File.ReadAllBytes(path)));
        Assert.Empty(PackageIntegrity.Validate(_root, _manifest));
        foreach (var pair in before) Assert.Equal(pair.Value, SHA256.HashData(File.ReadAllBytes(pair.Key)));
        Assert.Equal(before.Count, Directory.GetFiles(_root, "*", SearchOption.AllDirectories).Length);
    }

    [Theory]
    [InlineData("../outside.dll")]
    [InlineData("app/../../outside.dll")]
    [InlineData("app/../outside.dll")]
    [InlineData("C:/outside.dll")]
    [InlineData("/app/outside.dll")]
    [InlineData("app/file.dll:stream")]
    [InlineData("app//file.dll")]
    [InlineData("app/./file.dll")]
    [InlineData("app/file.")]
    [InlineData("app/file ")]
    [InlineData("app\\file.dll")]
    [InlineData("wallboard.production.json")]
    public void UnsafeOrMutableManifestPathsAreRejected(string path)
    {
        var errors = ValidateDocument(document => ((XmlElement)document.DocumentElement!.FirstChild!).SetAttribute("path", path));
        Assert.NotEmpty(errors);
    }

    [Theory]
    [InlineData("schema")]
    [InlineData("hash")]
    [InlineData("length")]
    [InlineData("duplicate")]
    [InlineData("required")]
    [InlineData("empty")]
    [InlineData("element")]
    public void InvalidManifestMetadataFailsClosed(string kind)
    {
        Assert.NotEmpty(ValidateDocument(document =>
        {
            var package = document.DocumentElement!;
            var file = (XmlElement)package.FirstChild!;
            switch (kind)
            {
                case "schema": package.SetAttribute("schema", "2"); break;
                case "hash": file.SetAttribute("sha256", new string('Z', 64)); break;
                case "length": file.SetAttribute("length", "-1"); break;
                case "duplicate":
                    var clone = (XmlElement)document.SelectSingleNode("/package/file[@path='app/hostfxr.dll']")!.CloneNode(true);
                    clone.SetAttribute("path", "app/HOSTFXR.DLL");
                    package.AppendChild(clone);
                    break;
                case "required": package.RemoveChild(document.SelectSingleNode("/package/file[@path='app/hostfxr.dll']")!); break;
                case "empty": while (package.HasChildNodes) package.RemoveChild(package.FirstChild!); break;
                case "element": package.AppendChild(document.CreateElement("unexpected")); break;
            }
        }));
    }

    [Fact]
    public void DtdsAndOversizedReferenceManifestsAreRejected()
    {
        var bytes = Encoding.UTF8.GetBytes("<!DOCTYPE package [<!ENTITY external SYSTEM 'file:///never-read'>]><package schema='1'>&external;</package>");
        File.WriteAllBytes(Path.Combine(_root, PackageIntegrity.ManifestName), bytes);
        Assert.NotEmpty(PackageIntegrity.Validate(_root, bytes));
        Assert.NotEmpty(PackageIntegrity.Validate(_root, new byte[PackageIntegrity.MaximumManifestBytes + 1]));
    }

    [Fact]
    public void ExtraPayloadFilesAreRejectedButRootFilesAreAllowed()
    {
        File.WriteAllText(Path.Combine(_root, "operator-notes.txt"), "Allowed");
        Assert.Empty(PackageIntegrity.Validate(_root, _manifest));
        File.WriteAllText(Path.Combine(_root, "app", "stale.dll"), "Unexpected stale program file");
        Assert.Contains(PackageIntegrity.Validate(_root, _manifest), error => error.Contains("app/stale.dll", StringComparison.Ordinal));
    }

    public void Dispose() => Directory.Delete(_root, recursive: true);
}
