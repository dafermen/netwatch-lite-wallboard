using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
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
            try
            {
                string root = AppDomain.CurrentDomain.BaseDirectory;
                string executable = Path.Combine(root, "app", "NetWatch-Lite-Wallboard.exe");
                if (!File.Exists(executable) || !File.Exists(Path.Combine(root, "portable-layout.json")))
                    throw new FileNotFoundException("Extract the complete portable ZIP into a new folder. Do not copy only the executable.");

                var start = new ProcessStartInfo(executable);
                start.UseShellExecute = false;
                start.WorkingDirectory = root;
                start.Arguments = string.Join(" ", args.Select(QuoteArgument));
                using (var child = Process.Start(start))
                {
                    // Package checks need the real application's exit code; normal startup stays lightweight.
                    if (args.Length > 0 && args[0] == "--package-check")
                    {
                        child.WaitForExit();
                        return child.ExitCode;
                    }
                }
                return 0;
            }
            catch (Exception ex)
            {
                if (args.Length == 0 || args[0] != "--package-check")
                    MessageBox.Show(ex.Message, "NetWatch Lite Wallboard", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return 1;
            }
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
