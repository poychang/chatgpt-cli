namespace ChatGptCli.Codex;

/// <summary>
/// Result of a delegated Codex process invocation.
/// </summary>
public sealed record CodexResult(int ExitCode, string StandardOutput, string StandardError)
{
    public bool Success => ExitCode == 0;
}

/// <summary>
/// Availability of the official Codex CLI on this machine.
/// </summary>
public sealed record CodexAvailability(bool Found, string? Version, string? ExecutablePath);

/// <summary>
/// ChatGPT subscription (Codex-managed) login status, as reported by the
/// official Codex CLI. This CLI never reads or stores the underlying tokens.
/// </summary>
public sealed record CodexLoginStatus(bool LoggedIn, string RawOutput);
