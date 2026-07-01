using System.Diagnostics;
using System.Text;

namespace ChatGptCli.Codex;

/// <summary>
/// Default <see cref="ICodexAdapter"/> that shells out to the official Codex CLI.
/// It never touches OAuth codes, access tokens, refresh tokens, or auth.json.
/// </summary>
public sealed class CodexProcessAdapter : ICodexAdapter
{
    private readonly string? _executablePath;

    public CodexProcessAdapter(string? executablePath = null)
    {
        _executablePath = executablePath ?? CodexLocator.Find();
    }

    public CodexAvailability GetAvailability(CancellationToken cancellationToken = default)
    {
        if (_executablePath is null)
        {
            return new CodexAvailability(false, null, null);
        }

        try
        {
            var result = Capture(new[] { "--version" }, cancellationToken);
            if (result.Success)
            {
                var version = result.StandardOutput.Trim();
                return new CodexAvailability(true, string.IsNullOrEmpty(version) ? null : version, _executablePath);
            }
        }
        catch (Exception)
        {
            // Fall through to "found but not runnable".
        }

        return new CodexAvailability(true, null, _executablePath);
    }

    public CodexLoginStatus GetLoginStatus(CancellationToken cancellationToken = default)
    {
        var result = Capture(new[] { "login", "status" }, cancellationToken);
        var combined = (result.StandardOutput + "\n" + result.StandardError).Trim();

        // `codex login status` exits 0 and prints an account line when logged in.
        var loggedIn = result.Success &&
                       !combined.Contains("not logged in", StringComparison.OrdinalIgnoreCase) &&
                       !combined.Contains("no credentials", StringComparison.OrdinalIgnoreCase);

        return new CodexLoginStatus(loggedIn, combined);
    }

    public int Login(CancellationToken cancellationToken = default)
        => RunInherited(new[] { "login" }, stdin: null, cancellationToken);

    public int Chat(string prompt, CancellationToken cancellationToken = default)
    {
        // `codex exec -` reads the prompt from stdin, keeping it off the command line.
        return RunInherited(new[] { "exec", "-" }, stdin: prompt, cancellationToken);
    }

    private CodexResult Capture(IReadOnlyList<string> args, CancellationToken cancellationToken)
    {
        var psi = CreateStartInfo(args);
        psi.RedirectStandardOutput = true;
        psi.RedirectStandardError = true;
        psi.RedirectStandardInput = false;

        using var process = Start(psi);
        var stdout = new StringBuilder();
        var stderr = new StringBuilder();

        process.OutputDataReceived += (_, e) => { if (e.Data is not null) stdout.AppendLine(e.Data); };
        process.ErrorDataReceived += (_, e) => { if (e.Data is not null) stderr.AppendLine(e.Data); };
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        WaitForExit(process, cancellationToken);
        return new CodexResult(process.ExitCode, stdout.ToString(), stderr.ToString());
    }

    private int RunInherited(IReadOnlyList<string> args, string? stdin, CancellationToken cancellationToken)
    {
        var psi = CreateStartInfo(args);
        psi.RedirectStandardOutput = false;
        psi.RedirectStandardError = false;
        psi.RedirectStandardInput = stdin is not null;

        using var process = Start(psi);

        if (stdin is not null)
        {
            process.StandardInput.Write(stdin);
            process.StandardInput.Close();
        }

        WaitForExit(process, cancellationToken);
        return process.ExitCode;
    }

    private ProcessStartInfo CreateStartInfo(IReadOnlyList<string> args)
    {
        if (_executablePath is null)
        {
            throw new InvalidOperationException("Codex CLI is not installed on this machine.");
        }

        var psi = new ProcessStartInfo
        {
            FileName = _executablePath,
            UseShellExecute = false,
        };

        foreach (var arg in args)
        {
            psi.ArgumentList.Add(arg);
        }

        return psi;
    }

    private static Process Start(ProcessStartInfo psi)
    {
        var process = Process.Start(psi);
        if (process is null)
        {
            throw new InvalidOperationException("Failed to start the Codex CLI process.");
        }

        return process;
    }

    private static void WaitForExit(Process process, CancellationToken cancellationToken)
    {
        try
        {
            process.WaitForExit();
        }
        catch (OperationCanceledException)
        {
            TryKill(process);
            throw;
        }

        if (cancellationToken.IsCancellationRequested)
        {
            TryKill(process);
            cancellationToken.ThrowIfCancellationRequested();
        }
    }

    private static void TryKill(Process process)
    {
        try
        {
            if (!process.HasExited)
            {
                process.Kill(entireProcessTree: true);
            }
        }
        catch
        {
            // Best effort.
        }
    }
}
