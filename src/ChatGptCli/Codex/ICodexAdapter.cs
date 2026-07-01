namespace ChatGptCli.Codex;

/// <summary>
/// Abstraction over the official Codex CLI. This layer isolates the rest of the
/// application from changes to Codex CLI arguments and output, and enforces the
/// hard boundary that subscription auth is always delegated to Codex.
/// </summary>
public interface ICodexAdapter
{
    /// <summary>Detects whether the Codex CLI is installed and runnable.</summary>
    CodexAvailability GetAvailability(CancellationToken cancellationToken = default);

    /// <summary>Queries ChatGPT subscription login status via <c>codex login status</c>.</summary>
    CodexLoginStatus GetLoginStatus(CancellationToken cancellationToken = default);

    /// <summary>
    /// Delegates interactive login to <c>codex login</c>. Console streams are
    /// inherited so the official browser flow behaves exactly as usual.
    /// </summary>
    int Login(CancellationToken cancellationToken = default);

    /// <summary>
    /// Runs a non-interactive chat turn via <c>codex exec</c>. The prompt is
    /// passed through stdin so it never appears in the process command line.
    /// </summary>
    int Chat(string prompt, CancellationToken cancellationToken = default);
}
