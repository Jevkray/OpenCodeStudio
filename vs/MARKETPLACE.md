![OpenCode Studio](https://raw.githubusercontent.com/Jevkray/OpenCodeStudio/main/vs/Resources/PreviewImage.png)

# OpenCode Studio

**The OpenCode AI coding agent, natively inside Visual Studio.**

A fast, self-contained tool window with a built-in usage dashboard and a first-run import wizard.

![Marketplace](https://img.shields.io/visual-studio-marketplace/v/jevkray.OpenCodeStudio?label=marketplace&color=7C3AED)
![Installs](https://img.shields.io/visual-studio-marketplace/i/jevkray.OpenCodeStudio?color=7C3AED)
![Visual Studio](https://img.shields.io/badge/Visual%20Studio-2022%20%7C%202026-7C3AED)
![License](https://img.shields.io/badge/license-MIT-7C3AED)
![.NET Framework](https://img.shields.io/badge/.NET%20Framework-4.7.2-7C3AED)

---

## Overview

**OpenCode Studio** embeds the full OpenCode web interface into a Visual Studio tool window using WebView2. Unlike a proxy-based integration, it points the browser straight at the OpenCode server that it launches itself - fewer moving parts, fewer bugs, and it stays in sync with every OpenCode release.

Everything runs locally on `127.0.0.1`. No account, no telemetry, no data leaves your machine.

## Features

| Feature | Description |
|---------|-------------|
| Native web UI | The complete OpenCode interface (projects, sessions, tabs, themes) rendered directly - no shim, no injected scripts. |
| Per-project sessions | Detects your solution / Git root and opens the matching OpenCode session automatically. |
| Usage dashboard | A dedicated window with cost, tokens, requests and OpenCode-style charts, with live refresh. |
| First-run import wizard | Detects every OpenCode installation (CLI, Desktop, custom folder) and imports settings, themes, credentials and chat history in one click. |
| Isolated environment | Runs in its own `%LOCALAPPDATA%\OpenCodeStudio` environment, so your global CLI/Desktop setup is never modified. |
| Auto-reconnect | If the server drops, the window shows a progress page and restores the session by itself. |
| VS-aware chrome | Loading and error pages follow your Visual Studio color theme. |
| File logging | Diagnostics written to `%LOCALAPPDATA%\OpenCodeStudio\logs\opencode-studio.log`. |
| VS 2022 & 2026 | One VSIX for both. |

## Getting started

```
View   -> Other Windows -> OpenCode Studio      (the agent)
Tools  -> OpenCode Studio Usage Statistics     (cost & tokens)
Tools  -> OpenCode Studio Import Settings...   (setup wizard)
```

On the **first launch** the setup wizard appears automatically. It scans your machine for OpenCode data and lets you pull in whatever you want:

```
  +--------------------------------------------------------------+
  |  Import from                                                 |
  +--------------------------------------------------------------+
  |  OpenCode CLI / Desktop (global)     opencode.json, auth, db |
  |  OpenCode Studio environment         currently in use        |
  |  OpenCode Desktop                    settings, drafts        |
  |  Current project (.opencode)         project-local config    |
  |  Custom folder...                    a backup or another PC  |
  +--------------------------------------------------------------+
```

Every overwrite is backed up with a `.bak-<timestamp>` suffix, so nothing is ever lost. You can re-open the wizard any time from **Tools**.

**Requirement:** the OpenCode CLI must be installed and available on your `PATH`.

## Usage statistics

Open **Tools -> OpenCode Studio Usage Statistics** for a local dashboard built from the OpenCode API - no external services:

- **KPI cards** - total cost, tokens, sessions and requests.
- **Cost by day** - a smooth area chart.
- **Tokens by type** - input / output / reasoning / cache in a donut.
- **Cost by model** and **cost per request** for the current session.
- **Session table** with model, agent, tokens and cost.

## Architecture

OpenCode Studio keeps a deliberately small, decoupled core.

```
  OpenCodeStudioPackage            VS package & commands
    |
  ServerController                server + session lifecycle
    |- OpenCodeServerService      launches `opencode serve`
    |- OpenCodeSessionService     /session, /project API
    |- ConnectionMonitor          periodic health checks
    |- ProcessBinding             dies with Visual Studio
    |
  OpenCodeToolWindowControl       WebView2 host
    |- navigates to http://127.0.0.1:<port>/{dir}/session
    |
  UsageStatsWindow                WebView2 dashboard
  FirstRunWindow                  import wizard
  ImportService / OpenCodeEnvironment / Log
```

**Design principles**

- **No proxy layer.** WebView2 talks to the real server origin, so the UI is always authentic and update-proof.
- **Isolated data.** OpenCode runs with its own `XDG_*` directories under `%LOCALAPPDATA%\OpenCodeStudio\opencode`.
- **Clean service boundaries.** Server lifecycle, session API, monitoring and import are separate, testable services.
- **Local by default.** Binding to loopback, no outbound calls except to the providers you configured.

## Requirements

| Component | Version |
|-----------|---------|
| Visual Studio | 2022 (17.0) - 2026 |
| OpenCode CLI | installed and on `PATH` |
| WebView2 Runtime | preinstalled with Windows 10/11 and VS |

## Privacy

OpenCode Studio is **local-first**:

- the server binds to `127.0.0.1` only;
- credentials are stored by OpenCode itself in your own environment;
- the extension sends no telemetry and has no analytics;
- the usage dashboard reads from your local server.

## Links

- Source code: https://github.com/Jevkray/OpenCodeStudio
- Русская версия описания: https://github.com/Jevkray/OpenCodeStudio/blob/main/README.ru.md
- Issues & feedback: https://github.com/Jevkray/OpenCodeStudio/issues

## License

Released under the [MIT License](https://github.com/Jevkray/OpenCodeStudio/blob/main/LICENSE).
