using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Security.Cryptography;
using System.Xml;

namespace NetWatchLite.Wallboard.Launcher
{
    // Kept compatible with the Windows .NET Framework compiler: do not use modern C# syntax here.
    internal static class PackageIntegrity
    {
        internal const string ManifestName = "integrity-manifest.xml";
        internal const string ResourceName = "NetWatchLite.Wallboard.Integrity.xml";
        internal const int MaximumManifestBytes = 4 * 1024 * 1024;
        private const int MaximumFiles = 4096;
        internal static readonly string[] RequiredFiles =
        {
            "portable-layout.json", "app/NetWatch-Lite-Wallboard.exe", "app/NetWatch-Lite-Wallboard.dll",
            "app/NetWatch-Lite-Wallboard.deps.json", "app/NetWatch-Lite-Wallboard.runtimeconfig.json",
            "app/coreclr.dll", "app/hostfxr.dll", "app/hostpolicy.dll", "app/System.Private.CoreLib.dll",
            "app/System.Windows.Forms.dll", "app/WebView2Loader.dll", "app/Microsoft.Web.WebView2.Core.dll",
            "app/Microsoft.Web.WebView2.WinForms.dll"
        };

        internal static List<string> Validate(string root, byte[] expectedManifest)
        {
            var errors = new List<string>();
            try
            {
                if (expectedManifest.Length == 0 || expectedManifest.Length > MaximumManifestBytes)
                    throw new InvalidDataException("Invalid embedded manifest length.");
                string manifestPath = Path.Combine(root, ManifestName);
                if (!File.Exists(manifestPath))
                {
                    errors.Add("Missing: " + ManifestName);
                    return errors;
                }
                RejectRedirectedPath(root, ManifestName);
                // The external copy is never parsed. Only the bounded, embedded bytes are trusted as the reference.
                using (var external = new FileStream(manifestPath, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    if (external.Length != expectedManifest.Length)
                    {
                        errors.Add("Changed manifest: " + ManifestName);
                        return errors;
                    }
                    for (int i = 0; i < expectedManifest.Length; i++)
                    {
                        if (external.ReadByte() != expectedManifest[i])
                        {
                            errors.Add("Changed manifest: " + ManifestName);
                            return errors;
                        }
                    }
                }

                var entries = ReadEntries(expectedManifest);
                var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                foreach (var entry in entries) names.Add(entry.Path);
                foreach (var entry in entries)
                {
                    try
                    {
                        string path = ResolveProtectedPath(root, entry.Path);
                        RejectRedirectedPath(root, entry.Path);
                        if (!File.Exists(path)) { errors.Add("Missing: " + entry.Path); continue; }
                        using (var input = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read))
                        {
                            if (input.Length != entry.Length)
                            {
                                errors.Add("Changed file length: " + entry.Path);
                                continue;
                            }
                            using (var sha = SHA256.Create())
                            {
                                string actual = BitConverter.ToString(sha.ComputeHash(input)).Replace("-", "");
                                if (!string.Equals(actual, entry.Hash, StringComparison.OrdinalIgnoreCase))
                                    errors.Add("Changed or corrupt file: " + entry.Path);
                            }
                        }
                    }
                    catch (Exception ex) { errors.Add("Cannot verify " + entry.Path + ": " + ex.Message); }
                }
                FindUnexpectedFiles(root, names, errors);
            }
            catch (Exception ex) { errors.Add("Cannot validate package: " + ex.Message); }
            return errors;
        }

        private static List<Entry> ReadEntries(byte[] bytes)
        {
            var settings = new XmlReaderSettings();
            settings.DtdProcessing = DtdProcessing.Prohibit;
            settings.XmlResolver = null;
            settings.MaxCharactersInDocument = MaximumManifestBytes;
            var document = new XmlDocument();
            document.XmlResolver = null;
            using (var stream = new MemoryStream(bytes))
            using (var reader = XmlReader.Create(stream, settings)) document.Load(reader);
            var package = document.DocumentElement;
            if (package == null || package.Name != "package" || package.GetAttribute("schema") != "1")
                throw new InvalidDataException("Unsupported integrity manifest schema.");
            var result = new List<Entry>();
            var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (XmlNode node in package.ChildNodes)
            {
                var file = node as XmlElement;
                if (file == null || file.Name != "file") throw new InvalidDataException("Invalid manifest element.");
                string relative = file.GetAttribute("path");
                ValidateRelativePath(relative);
                if (!names.Add(relative)) throw new InvalidDataException("Duplicate manifest path: " + relative);
                long length;
                string hash = file.GetAttribute("sha256");
                if (!long.TryParse(file.GetAttribute("length"), NumberStyles.None, CultureInfo.InvariantCulture, out length) ||
                    length < 0 || hash.Length != 64 || !IsHex(hash))
                    throw new InvalidDataException("Invalid manifest metadata: " + relative);
                result.Add(new Entry(relative, length, hash));
                if (result.Count > MaximumFiles) throw new InvalidDataException("Integrity manifest contains too many files.");
            }
            foreach (string required in RequiredFiles)
                if (!names.Contains(required)) throw new InvalidDataException("Manifest omits required file: " + required);
            return result;
        }

        private static bool IsHex(string text)
        {
            foreach (char value in text)
                if (!(value >= '0' && value <= '9') && !(value >= 'a' && value <= 'f') && !(value >= 'A' && value <= 'F')) return false;
            return true;
        }

        private static void ValidateRelativePath(string relative)
        {
            if (string.IsNullOrEmpty(relative) ||
                !(relative == "portable-layout.json" || relative.StartsWith("app/", StringComparison.Ordinal)) ||
                relative.IndexOf('\\') >= 0 || relative.IndexOf(':') >= 0 || Path.IsPathRooted(relative))
                throw new InvalidDataException("Invalid protected path: " + relative);
            foreach (string segment in relative.Split('/'))
            {
                if (segment.Length == 0 || segment == "." || segment == ".." || segment.EndsWith(".") ||
                    segment.EndsWith(" ") || segment.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
                    throw new InvalidDataException("Invalid protected path: " + relative);
            }
        }

        private static string ResolveProtectedPath(string root, string relative)
        {
            ValidateRelativePath(relative);
            string prefix = Path.GetFullPath(root).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
            string full = Path.GetFullPath(Path.Combine(prefix, relative.Replace('/', Path.DirectorySeparatorChar)));
            if (!full.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                throw new InvalidDataException("Protected path is outside the package.");
            return full;
        }

        private static void RejectRedirectedPath(string root, string relative)
        {
            string path = Path.GetFullPath(root);
            foreach (string segment in relative.Replace('\\', '/').Split('/'))
            {
                path = Path.Combine(path, segment);
                if ((File.Exists(path) || Directory.Exists(path)) &&
                    (File.GetAttributes(path) & FileAttributes.ReparsePoint) != 0)
                    throw new InvalidDataException("Redirected payload path: " + relative);
            }
        }

        private static void FindUnexpectedFiles(string root, HashSet<string> expected, List<string> errors)
        {
            string app = Path.Combine(root, "app");
            if (!Directory.Exists(app)) return;
            string prefix = Path.GetFullPath(root).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
            var directories = new Queue<string>();
            directories.Enqueue(app);
            int visited = 0;
            int fileCount = 0;
            while (directories.Count > 0)
            {
                if (++visited > MaximumFiles) throw new InvalidDataException("Too many payload directories.");
                string directory = directories.Dequeue();
                string relativeDirectory = directory.Substring(prefix.Length).Replace('\\', '/');
                try
                {
                    RejectRedirectedPath(root, relativeDirectory);
                    foreach (string path in Directory.EnumerateFiles(directory))
                    {
                        if (++fileCount > MaximumFiles) { errors.Add("Too many payload files."); return; }
                        string relative = path.Substring(prefix.Length).Replace('\\', '/');
                        if (!expected.Contains(relative)) errors.Add("Unexpected payload file: " + relative);
                    }
                    foreach (string child in Directory.EnumerateDirectories(directory))
                    {
                        if (visited + directories.Count >= MaximumFiles) { errors.Add("Too many payload directories."); return; }
                        directories.Enqueue(child);
                    }
                }
                catch (Exception ex) { errors.Add("Cannot inspect " + relativeDirectory + ": " + ex.Message); }
            }
        }

        private sealed class Entry
        {
            internal readonly string Path;
            internal readonly long Length;
            internal readonly string Hash;
            internal Entry(string path, long length, string hash) { Path = path; Length = length; Hash = hash; }
        }
    }
}
