using System.ComponentModel;
using ChatGptCli.Codex;
using Spectre.Console;
using Spectre.Console.Cli;

namespace ChatGptCli.Commands;

/// <summary>
/// <c>chatgpt-cli chat</c> — a simple one-shot conversation, delegated to
/// <c>codex exec</c>. The prompt is passed through stdin so it never appears
/// on the process command line.
/// </summary>
public sealed class ChatCommand(ICodexAdapter adapter, IAnsiConsole console) : Command<ChatCommand.Settings>
{
    public sealed class Settings : CommandSettings
    {
        [CommandArgument(0, "[prompt]")]
        [Description("The message to send. If omitted, the prompt is read from stdin.")]
        public string? Prompt { get; init; }
    }

    public override int Execute(CommandContext context, Settings settings)
    {
        if (!CodexGuidance.EnsureAvailable(adapter, console, out _))
        {
            return 1;
        }

        var prompt = ResolvePrompt(settings.Prompt);
        if (string.IsNullOrWhiteSpace(prompt))
        {
            console.MarkupLine("[red]No prompt provided.[/] Usage: [blue]chatgpt-cli chat \"your message\"[/]");
            return 1;
        }

        var loginStatus = adapter.GetLoginStatus();
        if (!loginStatus.LoggedIn)
        {
            console.MarkupLine("[yellow]Not logged in.[/] Run [blue]chatgpt-cli login[/] first.");
            return 1;
        }

        return adapter.Chat(prompt);
    }

    private static string? ResolvePrompt(string? argument)
    {
        if (!string.IsNullOrWhiteSpace(argument))
        {
            return argument;
        }

        // Allow piped input: `echo "hi" | chatgpt-cli chat`.
        if (Console.IsInputRedirected)
        {
            var piped = Console.In.ReadToEnd();
            if (!string.IsNullOrWhiteSpace(piped))
            {
                return piped;
            }
        }

        return null;
    }
}
