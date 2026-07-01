using ChatGptCli.Codex;
using ChatGptCli.Commands;
using ChatGptCli.Infrastructure;
using Spectre.Console;
using Spectre.Console.Cli;

var registrar = new SimpleTypeRegistrar();
registrar.RegisterLazy(typeof(ICodexAdapter), () => new CodexProcessAdapter());
registrar.RegisterInstance(typeof(IAnsiConsole), AnsiConsole.Console);

var app = new CommandApp(registrar);
app.Configure(config =>
{
    config.SetApplicationName("chatgpt-cli");

    config.AddCommand<LoginCommand>("login")
        .WithDescription("Sign in with your ChatGPT subscription (delegated to the official Codex CLI).");

    config.AddCommand<StatusCommand>("status")
        .WithDescription("Check environment requirements and ChatGPT subscription status.");

    config.AddCommand<ChatCommand>("chat")
        .WithDescription("Send a simple chat message using your ChatGPT subscription.")
        .WithExample("chat", "\"Summarize this in Traditional Chinese\"");
});

return app.Run(args);
