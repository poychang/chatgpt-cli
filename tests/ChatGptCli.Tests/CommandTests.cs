using ChatGptCli.Codex;
using ChatGptCli.Commands;
using ChatGptCli.Infrastructure;
using Spectre.Console.Testing;
using Xunit;

namespace ChatGptCli.Tests;

public class CommandTests
{
    private static CommandAppTester CreateApp(ICodexAdapter adapter)
    {
        var registrar = new SimpleTypeRegistrar();
        registrar.RegisterInstance(typeof(ICodexAdapter), adapter);
        var app = new CommandAppTester(registrar);
        app.Configure(config =>
        {
            config.AddCommand<LoginCommand>("login");
            config.AddCommand<StatusCommand>("status");
            config.AddCommand<ChatCommand>("chat");
        });
        return app;
    }

    [Fact]
    public void Login_WhenCodexMissing_ReturnsError()
    {
        var adapter = new FakeCodexAdapter { Availability = new CodexAvailability(false, null, null) };
        var app = CreateApp(adapter);

        var result = app.Run("login");

        Assert.Equal(1, result.ExitCode);
        Assert.False(adapter.LoginCalled);
        Assert.Contains("Codex CLI not found", result.Output);
    }

    [Fact]
    public void Login_WhenCodexAvailable_DelegatesToCodex()
    {
        var adapter = new FakeCodexAdapter();
        var app = CreateApp(adapter);

        var result = app.Run("login");

        Assert.Equal(0, result.ExitCode);
        Assert.True(adapter.LoginCalled);
    }

    [Fact]
    public void Status_WhenCodexMissing_ReportsUnavailable()
    {
        var adapter = new FakeCodexAdapter { Availability = new CodexAvailability(false, null, null) };
        var app = CreateApp(adapter);

        var result = app.Run("status");

        Assert.Equal(1, result.ExitCode);
        Assert.Contains("missing", result.Output);
    }

    [Fact]
    public void Status_WhenLoggedIn_ReportsReady()
    {
        var adapter = new FakeCodexAdapter();
        var app = CreateApp(adapter);

        var result = app.Run("status");

        Assert.Equal(0, result.ExitCode);
        Assert.Contains("delegated", result.Output);
    }

    [Fact]
    public void Chat_WhenNotLoggedIn_PromptsToLogin()
    {
        var adapter = new FakeCodexAdapter { LoginStatus = new CodexLoginStatus(false, "not logged in") };
        var app = CreateApp(adapter);

        var result = app.Run("chat", "hello");

        Assert.Equal(1, result.ExitCode);
        Assert.Null(adapter.LastChatPrompt);
        Assert.Contains("login", result.Output);
    }

    [Fact]
    public void Chat_WhenLoggedIn_DelegatesPromptToCodex()
    {
        var adapter = new FakeCodexAdapter();
        var app = CreateApp(adapter);

        var result = app.Run("chat", "hello world");

        Assert.Equal(0, result.ExitCode);
        Assert.Equal("hello world", adapter.LastChatPrompt);
    }
}
