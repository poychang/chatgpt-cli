using System.Runtime.InteropServices;
using ChatGptCli.Codex;
using Spectre.Console;
using Spectre.Console.Cli;

namespace ChatGptCli.Commands;

/// <summary>
/// <c>chatgpt-cli status</c> — reports whether the environment meets the
/// requirements to run this CLI and the current ChatGPT subscription state.
/// </summary>
public sealed class StatusCommand(ICodexAdapter adapter, IAnsiConsole console) : Command<StatusCommand.Settings>
{
    public sealed class Settings : CommandSettings;

    public override int Execute(CommandContext context, Settings settings)
    {
        var availability = adapter.GetAvailability();

        var table = new Table().Border(TableBorder.Rounded);
        table.AddColumn("Check");
        table.AddColumn("Status");

        table.AddRow(".NET runtime", Markup.Escape(RuntimeInformation.FrameworkDescription));

        if (availability.Found)
        {
            var version = availability.Version is null ? "found" : $"found ({availability.Version})";
            table.AddRow("Codex CLI", $"[green]{Markup.Escape(version)}[/]");
        }
        else
        {
            table.AddRow("Codex CLI", "[red]missing[/]");
        }

        CodexLoginStatus? loginStatus = null;
        if (availability.Found)
        {
            loginStatus = adapter.GetLoginStatus();
            table.AddRow(
                "ChatGPT subscription",
                loginStatus.LoggedIn ? "[green]logged in[/]" : "[yellow]not logged in[/]");
            table.AddRow(
                "Subscription path",
                loginStatus.LoggedIn ? "[green]delegated (ready)[/]" : "[yellow]delegated (login required)[/]");
        }
        else
        {
            table.AddRow("ChatGPT subscription", "[grey]unknown (Codex required)[/]");
            table.AddRow("Subscription path", "[red]unavailable[/]");
        }

        table.AddRow("Native OpenAI OAuth", "[grey]not supported by this version[/]");

        console.Write(table);

        if (!availability.Found)
        {
            console.WriteLine();
            CodexGuidance.RenderMissing(console);
            return 1;
        }

        if (loginStatus is { LoggedIn: false })
        {
            console.WriteLine();
            console.MarkupLine("Run [blue]chatgpt-cli login[/] to sign in with your ChatGPT subscription.");
        }

        return 0;
    }
}
