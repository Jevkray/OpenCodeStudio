<p align="center">
  <img src="https://raw.githubusercontent.com/Jevkray/OpenCodeStudio/main/vs/Resources/PreviewImage.png" width="240" alt="OpenCode Studio" />
</p>

<h1 align="center">OpenCode Studio</h1>

<p align="center">
  <b>The OpenCode AI coding agent, natively inside Visual Studio.</b><br/>
  A fast, self-contained tool window with a built-in usage dashboard and a first-run import wizard.
</p>

<p align="center">
  <a href="https://marketplace.visualstudio.com/items?itemName=jevkray.OpenCodeStudio"><img src="https://img.shields.io/visual-studio-marketplace/v/jevkray.OpenCodeStudio?label=marketplace&color=7C3AED" alt="Marketplace"></a>
  <a href="https://marketplace.visualstudio.com/items?itemName=jevkray.OpenCodeStudio"><img src="https://img.shields.io/visual-studio-marketplace/i/jevkray.OpenCodeStudio?color=7C3AED" alt="Installs"></a>
  <img src="https://img.shields.io/badge/Visual%20Studio-2022%20%7C%202026-7C3AED" alt="Visual Studio 2022 / 2026">
  <img src="https://img.shields.io/badge/license-MIT-7C3AED" alt="License: MIT">
  <img src="https://img.shields.io/badge/.NET%20Framework-4.7.2-7C3AED" alt=".NET Framework 4.7.2">
</p>

<p align="center">
  <a href="README.md">English</a> ·
  <a href="README.ru.md">Русский</a>
</p>

---

```text
  ╭──────────────────────────────────────────────────────────────╮
  │                                                              │
  │    O P E N C O D E   S T U D I O                             │
  │                                                              │
  │    OpenCode, natively inside Visual Studio                   │
  │    Dark-violet. Local-first. Zero configuration.             │
  │                                                              │
  ╰──────────────────────────────────────────────────────────────╯
```

## Overview

<p align="center">
  <img src="https://raw.githubusercontent.com/Jevkray/OpenCodeStudio/main/vs/Resources/PreviewWork.jpg" width="max" alt="OpenCode Studio" />
</p>
**OpenCode Studio** embeds the full [OpenCode](https://opencode.ai) web interface into a Visual Studio tool window using WebView2. Unlike a proxy-based integration, it points the browser **straight at the OpenCode server that it launches itself** — fewer moving parts, fewer bugs, and it stays in sync with every OpenCode release.

Everything runs **locally** on `127.0.0.1`. No account, no telemetry, no data leaves your machine.

## Features

| | Feature | Description |
|---|---------|-------------|
| 🧩 | **Native web UI** | The complete OpenCode interface (projects, sessions, tabs, themes) rendered directly — no shim, no injected scripts. |
| 🗂️ | **Per-project sessions** | Detects your solution / Git root and opens the matching OpenCode session automatically. |
| 📊 | **Usage dashboard** | A dedicated window with cost, tokens, requests and OpenCode-style charts, with live refresh. |
| 📥 | **First-run import wizard** | Detects every OpenCode installation (CLI, Desktop, custom folder) and imports settings, themes, credentials and chat history in one click. |
| 🔒 | **Isolated environment** | Runs in its own `%LOCALAPPDATA%\OpenCodeStudio` environment, so your global CLI/Desktop setup is never modified. |
| 🔁 | **Auto-reconnect** | If the server drops, the window shows a progress page and restores the session by itself. |
| 🎨 | **VS-aware chrome** | Loading and error pages follow your Visual Studio color theme. |
| 🧾 | **File logging** | Diagnostics written to `%LOCALAPPDATA%\OpenCodeStudio\logs\opencode-studio.log`. |
| ✅ | **VS 2022 & 2026** | One VSIX for both. |

## Installation

**From the Visual Studio Marketplace (recommended)**

1. In Visual Studio open **Extensions → Manage Extensions**.
2. Search for **OpenCode Studio** and install it.
3. Restart Visual Studio.

**From a VSIX**

1. Download the latest `OpenCodeStudio-x.y.z.vsix` from [Releases](https://github.com/Jevkray/OpenCodeStudio/releases).
2. Close all Visual Studio instances and double-click the file.

> **Requirement:** the [OpenCode CLI](https://opencode.ai) must be installed and available on your `PATH`.

## Getting started

```text
  View   → Other Windows → OpenCode Studio      (the agent)
  Tools  → OpenCode Studio Usage Statistics     (cost & tokens)
  Tools  → OpenCode Studio Import Settings...   (setup wizard)
```

On the **first launch** the setup wizard appears automatically. It scans your machine for OpenCode data and lets you pull in whatever you want:

```text
  ╭──────────────────────────────────────────────────────────────╮
  │  Import from                                                 │
  ├──────────────────────────────────────────────────────────────┤
  │  ■ OpenCode CLI / Desktop (global)   opencode.json, auth, db │
  │  ■ OpenCode Studio environment        currently in use        │
  │  ■ OpenCode Desktop                   settings, drafts        │
  │  ■ Current project (.opencode)        project-local config    │
  │  ■ Custom folder...                   a backup or another PC  │
  ╰──────────────────────────────────────────────────────────────╯
```

Every overwrite is backed up with a `.bak-<timestamp>` suffix, so nothing is ever lost. You can re-open the wizard any time from **Tools**.

## Usage statistics

Open **Tools → OpenCode Studio Usage Statistics** for a local dashboard built from the OpenCode API — no external services:

- **KPI cards** — total cost, tokens, sessions and requests.
- **Cost by day** — a smooth area chart.
- **Tokens by type** — input / output / reasoning / cache in a donut.
- **Cost by model** and **cost per request** for the current session.
- **Session table** with model, agent, tokens and cost.

## Architecture

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
  │    └── navigates to http://127.0.0.1:<port>/{dir}/session    │
  ├──────────────────────────────────────────────────────────────┤
  │  UsageStatsWindow                 WebView2 dashboard         │
  │  FirstRunWindow                   import wizard              │
  │  ImportService / OpenCodeEnvironment / Log                   │
  └──────────────────────────────────────────────────────────────┘
```

**Design principles**

- **No proxy layer.** WebView2 talks to the real server origin, so the UI is always authentic and update-proof.
- **Isolated data.** OpenCode runs with its own `XDG_*` directories under `%LOCALAPPDATA%\OpenCodeStudio\opencode`.
- **Clean service boundaries.** Server lifecycle, session API, monitoring and import are separate, testable services.
- **Local by default.** Binding to loopback, no outbound calls except to the providers you configured.

## Requirements

| Component | Version |
|-----------|---------|
| Visual Studio | 2022 (17.0) – 2026 |
| OpenCode CLI | installed and on `PATH` |
| .NET SDK (build from source) | .NET 10 |
| WebView2 Runtime | preinstalled with Windows 10/11 and VS |

## Building from source

```bash
git clone https://github.com/Jevkray/OpenCodeStudio.git
cd OpenCodeStudio
dotnet build vs/OpenCodeStudio.csproj -c Release
```

The VSIX is produced at `vs/bin/Release/net472/OpenCodeStudio.vsix`.
For a debug run, open `OpenCodeStudio.sln` in Visual Studio and press **F5** to launch the experimental instance.

## Privacy

OpenCode Studio is **local-first**:

- the server binds to `127.0.0.1` only;
- credentials are stored by OpenCode itself in your own environment;
- the extension sends no telemetry and has no analytics;
- the usage dashboard reads from your local server.

## License

Released under the [MIT License](LICENSE).

<p align="center"><sub>Built for the OpenCode community · Not affiliated with OpenCode or Microsoft.</sub></p>
