using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Windows.Forms;

namespace NetWatchLite.Wallboard.Launcher
{
    // Uses Windows .NET Framework, not the bundled .NET 10 payload.
    internal static class Launcher
    {
        [STAThread]
        private static int Main(string[] args)
        {
            string root = AppDomain.CurrentDomain.BaseDirectory;
            bool verifyOnly = args.Length > 0 && args[0] == "--verify-only";
            bool packageCheck = args.Length > 0 && args[0] == "--package-check";
            if (verifyOnly && !(args.Length == 1 || (args.Length == 3 && args[1] == "--report"))) return 2;
            try
            {
                byte[] expected;
                using (var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(PackageIntegrity.ResourceName))
                {
                    if (stream == null || stream.Length > PackageIntegrity.MaximumManifestBytes)
                        throw new InvalidDataException("Embedded integrity manifest missing or too large.");
                    using (var buffer = new MemoryStream()) { stream.CopyTo(buffer); expected = buffer.ToArray(); }
                }
                var errors = PackageIntegrity.Validate(root, expected);
                if (verifyOnly)
                {
                    if (args.Length == 3) WriteReport(root, args[2], errors);
                    return errors.Count == 0 ? 0 : 1;
                }
                if (errors.Count > 0)
                {
                    if (!packageCheck) ShowFailure(errors);
                    return 1;
                }
                string executable = Path.Combine(root, "app", "NetWatch-Lite-Wallboard.exe");

                var start = new ProcessStartInfo(executable);
                start.UseShellExecute = false;
                start.WorkingDirectory = root;
                start.Arguments = string.Join(" ", args.Select(QuoteArgument));
                using (var child = Process.Start(start))
                {
                    // Package checks need the real application's exit code; normal startup stays lightweight.
                    if (packageCheck)
                    {
                        child.WaitForExit();
                        return child.ExitCode;
                    }
                }
                return 0;
            }
            catch (Exception ex)
            {
                if (!verifyOnly && !packageCheck) ShowFailure(new List<string> { ex.Message });
                return verifyOnly ? 2 : 1;
            }
        }

        private static void WriteReport(string root, string path, List<string> errors)
        {
            string full = Path.GetFullPath(path);
            string app = Path.Combine(root, "app") + Path.DirectorySeparatorChar;
            if (!Path.IsPathRooted(path) || !string.Equals(path, full, StringComparison.OrdinalIgnoreCase) ||
                full.StartsWith(app, StringComparison.OrdinalIgnoreCase))
                throw new ArgumentException("Use a new absolute report path outside app/.");
            using (var stream = new FileStream(full, FileMode.CreateNew, FileAccess.Write))
            using (var writer = new StreamWriter(stream, new UTF8Encoding(false)))
                writer.Write(errors.Count == 0 ? "PASS: portable payload integrity." : string.Join(Environment.NewLine, errors));
        }

        private static void ShowFailure(List<string> errors)
        {
            string report = string.Join(Environment.NewLine, errors);
            string logPath = "A diagnostic log could not be written.";
            try
            {
                string folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "NetWatchLite", "WallboardLogs");
                Directory.CreateDirectory(folder);
                string log = Path.Combine(folder, "portable-startup.log");
                // Bound this launcher's own log; existing application logs and all configuration remain untouched.
                var mode = File.Exists(log) && new FileInfo(log).Length >= 1024 * 1024 ? FileMode.Create : FileMode.Append;
                using (var stream = new FileStream(log, mode, FileAccess.Write, FileShare.Read))
                using (var writer = new StreamWriter(stream, new UTF8Encoding(false)))
                    writer.WriteLine(DateTimeOffset.UtcNow.ToString("O") + Environment.NewLine + report.Substring(0, Math.Min(report.Length, 32768)));
                logPath = log;
            }
            catch { /* Logging must not hide the primary package error. */ }
            Application.EnableVisualStyles();
            MessageBox.Show("The portable package is incomplete, changed, or unreadable. Startup was blocked.\n\n" +
                string.Join(Environment.NewLine, errors.Take(12)) +
                "\n\nExtract the COMPLETE ZIP into a NEW folder. Preserve your wallboard*.json profiles and local pages. Do not copy only the EXE or mix app/ files.\n\nDiagnostics: " + logPath,
                "NetWatch Lite Wallboard - Portable validation", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        internal static string QuoteArgument(string argument)
        {
            var result = new StringBuilder("\"");
            int slashes = 0;
            foreach (char character in argument)
            {
                if (character == '\\') { slashes++; continue; }
                if (character == '"') result.Append('\\', slashes * 2 + 1);
                else result.Append('\\', slashes);
                result.Append(character);
                slashes = 0;
            }
            result.Append('\\', slashes * 2);
            return result.Append('"').ToString();
        }
    }
}
