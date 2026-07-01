# chatgpt-cli

chatgpt-cli is a C# CLI that provides a safe command-line interface for delegated
Codex subscription workflows. It does not reverse-engineer or directly implement
ChatGPT OAuth. Subscription access is delegated to the official Codex CLI where
supported.

## Requirements

- [.NET 10 SDK](https://dotnet.microsoft.com/)
- The official [Codex CLI](https://developers.openai.com/codex) installed and on
  your `PATH`. This is a hard dependency: ChatGPT subscription access is always
  delegated to Codex.

## Commands

| Command | Description |
| --- | --- |
| `chatgpt-cli login` | Sign in with your ChatGPT subscription (delegated to `codex login`). |
| `chatgpt-cli status` | Check environment requirements and subscription status. |
| `chatgpt-cli chat "..."` | Send a simple chat message using your ChatGPT subscription. |

The prompt for `chat` is passed to Codex through stdin, so it never appears on
the process command line. You can also pipe input: `echo "hi" | chatgpt-cli chat`.

## Build & run

```bash
dotnet build
dotnet run --project src/ChatGptCli -- status
```

## Security boundary

This CLI never reads, stores, or transmits ChatGPT OAuth codes, access tokens,
refresh tokens, or `auth.json`. All subscription authentication and token refresh
is handled by the official Codex CLI. See `plan.md` for the full auth boundary.
