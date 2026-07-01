using OpenAiSubCli.Codex;
using Spectre.Console;
using Spectre.Console.Cli;

namespace OpenAiSubCli.Commands;

/// <summary>
/// <c>openai-sub login</c> — delegates ChatGPT subscription login to the
/// official Codex CLI. This CLI never handles OAuth codes or tokens.
/// </summary>
public sealed class LoginCommand(ICodexAdapter adapter, IAnsiConsole console) : Command<LoginCommand.Settings>
{
    public sealed class Settings : CommandSettings;

    public override int Execute(CommandContext context, Settings settings)
    {
        if (!CodexGuidance.EnsureAvailable(adapter, console, out _))
        {
            return 1;
        }

        console.MarkupLine("[grey]Launching official Codex login...[/]");
        var exitCode = adapter.Login();

        if (exitCode == 0)
        {
            console.MarkupLine("[green]Login flow completed.[/] Run [blue]openai-sub status[/] to verify.");
        }
        else
        {
            console.MarkupLine($"[red]Codex login exited with code {exitCode}.[/]");
        }

        return exitCode;
    }
}
