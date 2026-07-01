using OpenAiSubCli.Codex;
using Spectre.Console;

namespace OpenAiSubCli.Commands;

/// <summary>
/// Shared helpers for presenting the hard dependency on the official Codex CLI.
/// </summary>
internal static class CodexGuidance
{
    public const string InstallUrl = "https://developers.openai.com/codex";

    public static void RenderMissing(IAnsiConsole console)
    {
        console.MarkupLine("[red]Codex CLI not found.[/]");
        console.MarkupLine(
            "This CLI delegates ChatGPT subscription access to the official Codex CLI, " +
            "so Codex must be installed and available on your PATH.");
        console.MarkupLine($"Install guide: [link]{InstallUrl}[/]");
    }

    public static bool EnsureAvailable(ICodexAdapter adapter, IAnsiConsole console, out CodexAvailability availability)
    {
        availability = adapter.GetAvailability();
        if (!availability.Found)
        {
            RenderMissing(console);
            return false;
        }

        return true;
    }
}

