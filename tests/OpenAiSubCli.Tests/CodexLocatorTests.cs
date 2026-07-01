using OpenAiSubCli.Codex;
using Xunit;

namespace OpenAiSubCli.Tests;

public class CodexLocatorTests
{
    [Fact]
    public void Find_WhenPathEmpty_ReturnsNull()
    {
        var original = Environment.GetEnvironmentVariable("PATH");
        try
        {
            Environment.SetEnvironmentVariable("PATH", string.Empty);
            Assert.Null(CodexLocator.Find());
        }
        finally
        {
            Environment.SetEnvironmentVariable("PATH", original);
        }
    }

    [Fact]
    public void Find_WhenCodexOnPath_ReturnsPath()
    {
        var original = Environment.GetEnvironmentVariable("PATH");
        var tempDir = Directory.CreateTempSubdirectory().FullName;
        try
        {
            var exeName = OperatingSystem.IsWindows() ? "codex.exe" : "codex";
            var exePath = Path.Combine(tempDir, exeName);
            File.WriteAllText(exePath, "stub");

            Environment.SetEnvironmentVariable("PATH", tempDir);
            Assert.Equal(exePath, CodexLocator.Find());
        }
        finally
        {
            Environment.SetEnvironmentVariable("PATH", original);
            Directory.Delete(tempDir, recursive: true);
        }
    }
}
