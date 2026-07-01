# OpenAI Subscription Auth CLI — plan.md

版本日期：2026-07-01  
目標語言：C# / .NET CLI  
專案定位：討論如何建立一個 CLI，讓使用者以合法、穩定、可維護的方式登入並使用 OpenAI 服務。

---

## 1. 核心判斷

目前不應直接把「OpenAI 訂閱 OAuth」視為一般第三方 CLI 可自由串接的公開登入機制。

官方文件中可明確確認的是：

1. **Codex 支援兩種登入方式**
   - Sign in with ChatGPT for subscription access
   - API key for usage-based access
   - Codex CLI 與 IDE extension 支援兩者。

2. **Codex 的 ChatGPT 登入是 Codex 產品內建流程**
   - CLI 會開啟瀏覽器完成登入。
   - 登入後瀏覽器會把 access token 回傳給 CLI 或 IDE extension。
   - Codex 會快取登入資訊，並在使用期間自動 refresh token。

3. **OpenAI 對 CI/CD 的建議不是自行呼叫 OAuth token endpoint**
   - 官方文件明確說明：支援模式是讓 Codex 自己 refresh session。
   - 不建議自行呼叫 refresh API。
   - 該文件也明確排除「generic OAuth clients outside Codex」。

4. **Apps SDK / GPT Actions 的 OAuth 是反向場景**
   - 這是讓 ChatGPT 作為 OAuth client，連到你的 MCP server 或 API。
   - 使用者在 ChatGPT UI 觸發工具時登入你的服務。
   - 這不是讓你的 CLI 以 OAuth 登入 OpenAI 訂閱帳號。

因此，本專案必須先把目標拆成幾條路線：

- 路線 A（主線）：Codex subscription CLI，透過官方 Codex CLI 或其支援的 auth cache 進行整合。
- 路線 B：等待 OpenAI 正式開放「第三方 CLI OAuth / Sign in with ChatGPT」規格後，再實作原生 OAuth。
- 路線 C：建立 ChatGPT App / MCP server，讓 ChatGPT 透過 OAuth 使用我們的服務；這不是本 CLI 的主線。

---

## 2. 專案目標

建立一個 C# CLI，提供統一命令列介面，讓使用者可以：

1. 檢查目前可用的 OpenAI 驗證模式。
2. 偵測本機是否已安裝 Codex CLI。
3. 在合法支援範圍內，委派 Codex CLI 執行需要 ChatGPT subscription access 的工作。
4. 以 CLI 方式進行簡單對話（由 Codex 代理執行）。
5. 將驗證狀態、安全邊界、錯誤原因清楚呈現給使用者。
6. 預留未來接入 OpenAI 官方第三方 OAuth 規格的抽象層。

---

## 3. 非目標

以下行為不列入專案範圍：

1. 不逆向 ChatGPT 網頁登入流程。
2. 不硬抓、重放或仿冒 OpenAI OAuth endpoint。
3. 不讀取、複製或匯出 Codex 的 refresh token 供其他用途。
4. 不宣稱能讓任意 OpenAI API 呼叫消耗 ChatGPT Plus / Pro / Business 訂閱額度。
5. 不建立會繞過 OpenAI API billing、workspace policy、RBAC、資料保留政策的工具。
6. 不在未受信任環境中使用使用者的 ChatGPT session token。

---

## 4. 建議架構

```text
openai-subscription-cli/
├─ src/
│  ├─ OpenAiSubCli/
│  │  ├─ Program.cs
│  │  ├─ Commands/
│  │  │  ├─ LoginCommand.cs
│  │  │  ├─ StatusCommand.cs
│  │  │  └─ ChatCommand.cs
│  │  ├─ Auth/
│  │  │  ├─ IAuthProvider.cs
│  │  │  ├─ CodexDelegatedAuthProvider.cs
│  │  │  ├─ CodexAuthDetector.cs
│  │  │  ├─ FutureOAuthAuthProvider.cs
│  │  │  └─ SessionBoundaryPolicy.cs
│  │  ├─ Codex/
│  │  │  ├─ CodexLocator.cs
│  │  │  ├─ ICodexAdapter.cs
│  │  │  ├─ CodexProcessRunner.cs
│  │  │  └─ CodexResultParser.cs
│  │  └─ Security/
│  │     ├─ SecretRedactor.cs
│  │     └─ EnvironmentValidator.cs
│  └─ OpenAiSubCli.Tests/
├─ docs/
│  ├─ auth-boundary.md
│  ├─ supported-flows.md
│  └─ threat-model.md
├─ .github/
│  └─ workflows/
│     └─ release.yml
├─ README.md
└─ plan.md
```

---

## 5. CLI 命令設計

### 5.1 login（取得 ChatGPT subscription access）

```bash
openai-sub login
```

行為：

- 先檢查 `codex` 是否存在。
- 若存在，執行 `codex login`。
- 登入流程完全委派官方 Codex CLI。
- 本 CLI 不接觸 OAuth code、access token、refresh token。

---

### 5.2 status（環境與授權狀態）

```bash
openai-sub status
```

輸出內容：

- .NET runtime 版本
- Codex CLI 是否存在
- Codex 登入能力是否可用（能力探測）
- 目前可用模式
- 安全警告

---

### 5.3 chat（簡單對話）

```bash
openai-sub chat "你好，請用繁體中文回覆"
```

行為：

- 呼叫 Codex CLI 進行對話請求（例如 `codex exec`）。
- 長 prompt 優先由 stdin 傳遞，避免出現在命令列參數。
- 收集 stdout / stderr。
- 將結果以 CLI 友善格式輸出。

設計原則：

- 本 CLI 是 wrapper / orchestrator。
- 不直接冒充 Codex client。
- 不自行實作 Codex OAuth。

---

## 6. 驗證模式抽象

```csharp
public interface IAuthProvider
{
    string Name { get; }
    Task<AuthStatus> GetStatusAsync(CancellationToken cancellationToken);
    Task<LoginResult> LoginAsync(CancellationToken cancellationToken);
    Task LogoutAsync(CancellationToken cancellationToken);
}
```

第一版實作：

```text
CodexDelegatedAuthProvider
CodexAuthDetector
```

保留但不啟用：

```text
FutureOAuthAuthProvider
```

`FutureOAuthAuthProvider` 只在 OpenAI 正式提供第三方 CLI OAuth client registration、authorization endpoint、token endpoint、scope、audience、refresh 規則後啟用。

---

## 7. 安全設計

### 7.1 憑證與狀態儲存

優先順序：

1. 由官方 Codex CLI 管理 ChatGPT session（本 CLI 不直接處理 token）
2. 本 CLI 僅儲存非敏感設定（例如輸出格式、預設行為）
3. 若需持久化 CLI 端設定，優先使用 OS 安全儲存機制
4. 純文字檔只作為開發模式，並預設關閉

建議套件：

- `System.CommandLine`
- `Microsoft.Extensions.Configuration`
- `Microsoft.Extensions.DependencyInjection`
- `Microsoft.Extensions.Logging`
- `Spectre.Console`
- `CredentialManagement` 或跨平台 keyring 套件，需另外評估品質與維護狀態

---

### 7.2 Token 邊界

本 CLI 不處理以下資料：

- ChatGPT OAuth authorization code
- ChatGPT access token
- ChatGPT refresh token
- Codex auth.json 內容

可檢查但不可讀取秘密內容：

- Codex CLI 是否可執行
- Codex auth 狀態是否存在
- Codex process 是否成功執行

---

### 7.3 Log Redaction

所有 log 必須遮蔽：

- `Authorization` header
- `api_key`
- `access_token`
- `refresh_token`
- `id_token`
- `auth.json` 內容
- 任何長度與格式疑似 token 的字串

---

## 8. 第一階段 MVP

### 8.1 功能

1. 建立 .NET CLI 專案。
2. 加入 `status` 命令。
3. 加入 `login` 命令，委派 `codex login`。
4. 加入 `chat` 命令，委派 Codex CLI 對話能力（長 prompt 走 stdin）。

---

### 8.2 驗收條件

```bash
openai-sub status
```

能清楚顯示：

```text
Codex CLI: found / missing
Codex subscription path: delegated / unavailable
Native OpenAI OAuth: not supported by this version
```

```bash
openai-sub login
```

必須只做：

```text
Launching official Codex login...
```

```bash
openai-sub chat "hello"
```

預期：

```text
會透過 Codex subscription path 回傳可讀文字結果（或明確錯誤）。
```

---

## 9. 第二階段

1. 加入設定檔：

```bash
openai-sub config set default-provider codex
```

2. 加入互動式選單。
3. 加入 `doctor` 命令檢查環境。
4. 加入 shell completion。
5. 加入 Windows / macOS / Linux 單檔發行。
6. 加入 Native AOT 評估。
7. 加入整合測試。

---

## 10. 第三階段：等待官方 OAuth 條件成熟

只有在 OpenAI 正式提供以下內容後，才實作原生 OAuth：

1. 第三方 CLI client registration 機制。
2. 明確授權的 authorization endpoint。
3. 明確授權的 token endpoint。
4. 支援 PKCE 的 public client flow。
5. 明確 scope。
6. 明確 audience。
7. 明確 refresh token 政策。
8. 明確 rate limit 與 billing / subscription credit 邊界。
9. 明確 ToS / policy 允許第三方 CLI 使用 ChatGPT subscription entitlement。

原生 OAuth 可行後，命令可設計為：

```bash
openai-sub auth login --chatgpt
```

但在第一版中，此命令應回應：

```text
Native ChatGPT OAuth for third-party CLI is not supported by this version.
Use `openai-sub login` to delegate subscription authentication to the official Codex CLI.
```

---

## 11. 風險清單

| 風險 | 說明 | 對策 |
|---|---|---|
| 誤把 Codex OAuth 當成一般 OpenAI OAuth | 可能導致不穩定或違反支援範圍 | 僅委派官方 Codex CLI |
| 使用者期待 ChatGPT Plus 可折抵 API | 官方目前區分 ChatGPT subscription 與 API billing | CLI 顯示明確提示 |
| Token 洩漏 | CLI 常見風險 | OS credential store、redaction、避免讀取 Codex token |
| CI/CD 濫用 session | 官方僅建議 trusted private infrastructure | 預設不支援，文件說明限制 |
| OpenAI 文件變更 | OAuth / Codex / Apps SDK 仍可能演進 | 每個版本更新 auth-boundary.md |

---

## 12. 開發順序

```text
Phase 0：官方文件確認與授權邊界文件化
Phase 1：建立 CLI skeleton
Phase 2：login 命令（Codex delegation）
Phase 3：status 命令（環境與授權狀態）
Phase 4：chat 命令（簡單對話）
Phase 5：安全儲存與 redaction
Phase 6：等待官方第三方 OAuth 規格，再決定是否擴充
```

---

## 13. 初始技術選型

```text
Language: C#
Target: .NET 10
CLI framework: Spectre.Console.Cli
Output: Spectre.Console
Config: Microsoft.Extensions.Configuration
DI: Microsoft.Extensions.DependencyInjection
Logging: Microsoft.Extensions.Logging
Testing: xUnit
Packaging: dotnet publish single-file
Release: GitHub Actions
```

首版目標框架為 `.NET 10`（本機已安裝對應 SDK 與 runtime）。

---

## 14. README 初始定位句

```text
openai-sub is a C# CLI that provides a safe command-line interface for delegated Codex subscription workflows. It does not reverse-engineer or directly implement ChatGPT OAuth. Subscription access is delegated to the official Codex CLI where supported.
```

---

## 15. 官方參考資料

- OpenAI Codex Authentication  
  https://developers.openai.com/codex/auth

- Maintain Codex account auth in CI/CD  
  https://developers.openai.com/codex/auth/ci-cd-auth

- OpenAI Apps SDK Authentication  
  https://developers.openai.com/apps-sdk/build/auth

- GPT Action Authentication  
  https://developers.openai.com/api/docs/actions/authentication

---

## 16. 本次討論新增（可行性收斂）

1. **MVP 聚焦 Codex delegation（降低首版風險）**  
   先交付：`login`、`status`、`chat`。  
   不納入 API key flow、設定管理、進階任務編排。

2. **Codex 狀態改為能力探測（避免綁 auth cache）**  
   以 `codex --version` 或官方可預期命令回傳判定可用性，不依賴特定快取檔案路徑/格式。

3. **增加 `ICodexAdapter` 抽象層（提高維護性）**  
   在 `CodexProcessRunner` 之外加一層介面，隔離未來 Codex CLI 參數與輸出變動風險。

4. **`chat` 命令改走 stdin 傳遞長 prompt（降低外洩面）**  
   避免把長 prompt 直接放在命令列參數，減少被程序清單或歷史紀錄看到的機會。

5. **版本策略先鎖 `.NET 8 LTS`（優先穩定）**  
   `.NET 10` 作為評估/實驗路線，不作為首版預設目標。
