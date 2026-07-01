using OpenAiSubCli.Codex;

namespace OpenAiSubCli.Tests;

/// <summary>
/// In-memory <see cref="ICodexAdapter"/> for tests. Records delegated calls and
/// returns scripted results without ever launching a real process.
/// </summary>
public sealed class FakeCodexAdapter : ICodexAdapter
{
    public CodexAvailability Availability { get; set; } = new(true, "codex 1.0.0", "/usr/bin/codex");
    public CodexLoginStatus LoginStatus { get; set; } = new(true, "Logged in");
    public int LoginExitCode { get; set; }
    public int ChatExitCode { get; set; }

    public bool LoginCalled { get; private set; }
    public string? LastChatPrompt { get; private set; }

    public CodexAvailability GetAvailability(CancellationToken cancellationToken = default) => Availability;

    public CodexLoginStatus GetLoginStatus(CancellationToken cancellationToken = default) => LoginStatus;

    public int Login(CancellationToken cancellationToken = default)
    {
        LoginCalled = true;
        return LoginExitCode;
    }

    public int Chat(string prompt, CancellationToken cancellationToken = default)
    {
        LastChatPrompt = prompt;
        return ChatExitCode;
    }
}
