using System.ComponentModel;
using OpenAiSubCli.Codex;
using Spectre.Console;
using Spectre.Console.Cli;

namespace OpenAiSubCli.Commands;

/// <summary>
/// <c>openai-sub chat</c> — a simple one-shot conversation, delegated to
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
            console.MarkupLine("[red]No prompt provided.[/] Usage: [blue]openai-sub chat \"your message\"[/]");
            return 1;
        }

        var loginStatus = adapter.GetLoginStatus();
        if (!loginStatus.LoggedIn)
        {
            console.MarkupLine("[yellow]Not logged in.[/] Run [blue]openai-sub login[/] first.");
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

        // Allow piped input: `echo "hi" | openai-sub chat`.
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
