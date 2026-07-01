using System.Runtime.InteropServices;

namespace OpenAiSubCli.Codex;

/// <summary>
/// Locates the official Codex CLI executable on the current machine.
/// </summary>
public static class CodexLocator
{
    private const string ExecutableName = "codex";

    /// <summary>
    /// Resolves the Codex executable path by scanning PATH. Returns null when
    /// Codex is not installed.
    /// </summary>
    public static string? Find()
    {
        var pathVar = Environment.GetEnvironmentVariable("PATH");
        if (string.IsNullOrEmpty(pathVar))
        {
            return null;
        }

        var isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
        var candidates = isWindows
            ? new[] { ExecutableName + ".exe", ExecutableName + ".cmd", ExecutableName + ".bat", ExecutableName }
            : new[] { ExecutableName };

        foreach (var dir in pathVar.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries))
        {
            foreach (var candidate in candidates)
            {
                try
                {
                    var full = Path.Combine(dir.Trim('"'), candidate);
                    if (File.Exists(full))
                    {
                        return full;
                    }
                }
                catch (ArgumentException)
                {
                    // Ignore malformed PATH entries.
                }
            }
        }

        return null;
    }
}
