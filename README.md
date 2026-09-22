<div align="center">

<img src="https://raw.githubusercontent.com/Jevkray/OpenCodeStudio/main/vs/Resources/PreviewImage.png" width="240" alt="OpenCode Studio" />

# 🟣 OpenCode Studio

**The OpenCode AI coding agent — natively inside Visual Studio.**

*A fast, self-contained tool window with a usage dashboard, a first-run import wizard and shared chat history.*

[![Marketplace](https://img.shields.io/visual-studio-marketplace/v/jevkray.OpenCodeStudio?label=marketplace&color=7C3AED)](https://marketplace.visualstudio.com/items?itemName=jevkray.OpenCodeStudio)
[![Installs](https://img.shields.io/visual-studio-marketplace/i/jevkray.OpenCodeStudio?color=7C3AED)](https://marketplace.visualstudio.com/items?itemName=jevkray.OpenCodeStudio)
[![Rating](https://img.shields.io/visual-studio-marketplace/r/jevkray.OpenCodeStudio?color=7C3AED)](https://marketplace.visualstudio.com/items?itemName=jevkray.OpenCodeStudio)
![Visual Studio](https://img.shields.io/badge/Visual%20Studio-2022%20%7C%202026-7C3AED)
![License](https://img.shields.io/badge/license-MIT-7C3AED)
![.NET](https://img.shields.io/badge/.NET%20Framework-4.7.2-7C3AED)

**[English](README.md) · [Русский](README.ru.md)**

</div>

---

> [!TIP]
> **One environment, everywhere.** OpenCode Studio shares the same data as the OpenCode CLI and desktop app — your providers, settings, credentials and **every chat** are already there. No setup, no duplication.

## 🆕 What's new

- **1.1.0** - Statistics button in the OpenCode panel: open your profile and check real usage without leaving Visual Studio.

## ✨ Why OpenCode Studio?

| | |
|---|---|
| 🧠 | **Real OpenCode, not a clone** — the complete official web UI, rendered directly in Visual Studio. |
| 🔄 | **Shared history** — the same chats as the CLI and desktop app, across every Visual Studio window. |
| 🗂️ | **All chats visible** — opens the web UI home, listing every project and session. |
| 📊 | **Usage dashboard** — cost, tokens and requests with OpenCode-style charts. |
| 📈 | **Profile & usage in one click** — the statistics button opens your OpenCode console inside the panel, so you can check real spend and limits without switching to a browser. |
| 📥 | **First-run wizard** — import settings, themes, credentials and history in one click. |
| 🔁 | **Auto-reconnect** — recovers the session by itself if the server drops. |
| 🔒 | **Local-first** — loopback only, zero telemetry. |
| 🪶 | **Lightweight** — one VSIX for Visual Studio 2022 and 2026. |

## 📸 Preview

![OpenCode Studio running inside Visual Studio](https://raw.githubusercontent.com/Jevkray/OpenCodeStudio/main/vs/Resources/PreviewWork.jpg)

> 💬 *The agent chat right next to your solution — with a live session panel showing model, provider, token usage, cost, last activity and the context breakdown.*

## 🚀 Quick start

```text
  View   →  Other Windows  →  OpenCode Studio        🔹 the agent
  Tools  →  OpenCode Studio Usage Statistics        📊 cost & tokens
  Tools  →  OpenCode Studio Import Settings...      📥 setup wizard
```

> [!NOTE]
> **Requirement:** the [OpenCode CLI](https://opencode.ai) must be installed and available on your `PATH`.

<details>
<summary><b>📦 Installation</b></summary>

**From the Visual Studio Marketplace (recommended)**

1. In Visual Studio open **Extensions → Manage Extensions**.
2. Search for **OpenCode Studio** and install it.
3. Restart Visual Studio.

**From a VSIX**

1. Download the latest `OpenCodeStudio-x.y.z.vsix` from [Releases](https://github.com/Jevkray/OpenCodeStudio/releases).
2. Close all Visual Studio instances and double-click the file.

</details>

## 🔗 Syncing chat history

OpenCode stores every session **against the project directory it was created in**. Because OpenCode Studio shares the same data as the CLI and desktop app, all chats are already available — but grouped by project path.

> [!IMPORTANT]
> If a project lives at a **different absolute path** than when its chats were created (another drive, another user name, a moved or renamed folder), those sessions stay under the original path. **Line up the project paths and everything synchronizes.**

1. Open each project at the **same absolute path** it originally used — e.g. `C:\Users\<you>\source\repos\MyApp`.
2. If the path changed, move the project back, or import the old data via **Tools → OpenCode Studio Import Settings...** pointing at the folder with the original `opencode.db`.
3. Reopen the tool window — the sessions appear under the matching project.

Once the paths match, **all chats sync** between the CLI, the desktop app and every Visual Studio window. 🎉

## 📊 Usage statistics

> [!NOTE]
> **Tools → OpenCode Studio Usage Statistics** opens a **local** dashboard built from the OpenCode API — no external services.

| 📈 Panel | What it shows |
|---|---|
| 💰 KPI cards | Total cost, tokens, sessions, requests |
| 🗓️ Cost by day | A smooth area chart |
| 🍩 Tokens by type | Input / output / reasoning / cache |
| 🤖 Cost by model | Spend per model |
| 🔍 Cost per request | For the current session |
| 📋 Session table | Model, agent, tokens and cost |

## 🧱 Architecture

OpenCode Studio keeps a deliberately small, decoupled core.

```text
 ┌──────────────────────────────────────────────────────────────┐
 │  OpenCodeStudioPackage            VS package & commands      │
 ├──────────────────────────────────────────────────────────────┤
 │  ServerController                server + session lifecycle  │
 │    ├── OpenCodeServerService      launches `opencode serve`  │
 │    ├── OpenCodeSessionService     /session, /project API     │
 │    ├── ConnectionMonitor          periodic health checks     │
 │    └── ProcessBinding             dies with Visual Studio    │
 ├──────────────────────────────────────────────────────────────┤
 │  OpenCodeToolWindowControl        WebView2 host              │
 │    └── opens http://127.0.0.1:<port>/ (the web UI home)      │
 ├──────────────────────────────────────────────────────────────┤
 │  UsageStatsWindow                 WebView2 dashboard         │
 │  FirstRunWindow                   import wizard              │
 │  ImportService / OpenCodeEnvironment / Log                   │
 └──────────────────────────────────────────────────────────────┘
```

> [!TIP]
> **Design principles**
> - **No proxy layer** — WebView2 talks to the real server origin, so the UI is authentic and update-proof.
> - **Shared data** — runs in your default environment, in sync with the CLI and desktop app.
> - **One server per machine** — every Visual Studio window reuses the same OpenCode server.
> - **Local by default** — loopback only, no outbound calls except your providers.

## 🧩 Requirements

| Component | Version |
|---|---|
| 🖥️ Visual Studio | 2022 (17.0) – 2026 |
| ⚡ OpenCode CLI | installed and on `PATH` |
| 🛠️ .NET SDK *(build from source)* | .NET 10 |
| 🌐 WebView2 Runtime | bundled with Windows 10/11 and VS |

## 🔨 Building from source

```bash
git clone https://github.com/Jevkray/OpenCodeStudio.git
cd OpenCodeStudio
dotnet build vs/OpenCodeStudio.csproj -c Release
```

The VSIX is produced at `vs/bin/Release/net472/OpenCodeStudio.vsix`. For a debug run, open `OpenCodeStudio.sln` and press **F5** to launch the experimental instance. 🧪

## 🛡️ Privacy

- 🖧 the server binds to `127.0.0.1` only
- 🔑 credentials are stored by OpenCode itself in your own environment
- 🚫 no telemetry, no analytics
- 📊 the dashboard reads from your local server
- 📈 the usage overlay signs in to the OpenCode Console and reads your usage and limits from `opencode.ai` using your own session cookie (stored encrypted with DPAPI) — no other data leaves your machine.

## 💬 Contacts & Support

<div align="center">

[![Telegram](https://img.shields.io/badge/Telegram-@eugenekray-2CA5E0?logo=telegram&logoColor=white)](https://t.me/eugenekray)
[![Email](https://img.shields.io/badge/Email-krasovskyworks@gmail.com-EA4335?logo=gmail&logoColor=white)](mailto:krasovskyworks@gmail.com)
[![Boosty](https://img.shields.io/badge/Support-Boosty-FF6A00?logo=boosty&logoColor=white)](https://boosty.to/jevkray)
[![Issues](https://img.shields.io/badge/Issues-GitHub-181717?logo=github&logoColor=white)](https://github.com/Jevkray/OpenCodeStudio/issues)

</div>

## 📜 License

Released under the [MIT License](LICENSE).

<div align="center"><sub>Built for the OpenCode community · Not affiliated with OpenCode or Microsoft.</sub></div>
