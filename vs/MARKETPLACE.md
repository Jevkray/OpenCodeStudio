# 🟣 OpenCode Studio

**The OpenCode AI coding agent — natively inside Visual Studio.**

*A fast, self-contained tool window with a usage dashboard, a first-run import wizard and shared chat history. Works with Visual Studio 2022 and 2026.*

![Marketplace](https://img.shields.io/visual-studio-marketplace/v/jevkray.OpenCodeStudio?label=marketplace&color=7C3AED)
![Installs](https://img.shields.io/visual-studio-marketplace/i/jevkray.OpenCodeStudio?color=7C3AED)
![Visual Studio](https://img.shields.io/badge/Visual%20Studio-2022%20%7C%202026-7C3AED)
![License](https://img.shields.io/badge/license-MIT-7C3AED)

---

## 📸 Preview

![OpenCode Studio running inside Visual Studio](https://raw.githubusercontent.com/Jevkray/OpenCodeStudio/main/vs/Resources/PreviewWork.jpg)

> 💬 *The agent chat right next to your solution — with a live session panel showing model, provider, token usage, cost, last activity and the context breakdown.*

---

## 🆕 What's new

- **1.1.3** - Sign-in returns to the app and opens the usage panel automatically.
- **1.1.2** - Popover clamped to the window; sign-in completes in-panel (OAuth kept in the embedded browser).
- **1.1.1** - Usage button in the OpenCode footer with an in-panel popover (sign-in, loading, infographics).
- **1.1.0** - Usage overlay with remaining Go limits and today's spend in the OpenCode panel, plus a usage dashboard window.

## ✨ Highlights

- 🧠 **Real OpenCode, not a clone** — the complete official web UI, rendered directly in Visual Studio.
- 🔄 **Shared history** — the same chats as the CLI and desktop app, across every Visual Studio window.
- 🗂️ **All chats visible** — opens the web UI home, listing every project and session.
- 📊 **Usage dashboard** — cost, tokens and requests with OpenCode-style charts.
- 📥 **First-run wizard** — import settings, themes, credentials and history in one click.
- 🔁 **Auto-reconnect** — recovers the session by itself if the server drops.
- 🔒 **Local-first** — loopback only, zero telemetry.
- 🪶 **Lightweight** — one VSIX for Visual Studio 2022 and 2026.

## 🚀 Getting started

```
View   ->  Other Windows  ->  OpenCode Studio        (the agent)
Tools  ->  OpenCode Studio Usage Statistics         (cost & tokens)
Tools  ->  OpenCode Studio Import Settings...       (setup wizard)
```

> **Requirement:** the OpenCode CLI must be installed and available on your `PATH`.

## 🔗 Syncing chat history

OpenCode stores every session **against the project directory it was created in**. Because OpenCode Studio shares the same data as the CLI and desktop app, all chats are already available — but grouped by project path.

If a project lives at a **different absolute path** than when its chats were created (another drive, another user name, a moved or renamed folder), those sessions stay under the original path. **Line up the project paths and everything synchronizes.**

1. Open each project at the **same absolute path** it originally used.
2. If the path changed, move the project back, or import the old data via **Tools -> OpenCode Studio Import Settings...** pointing at the folder with the original `opencode.db`.
3. Reopen the tool window — the sessions appear under the matching project.

Once the paths match, all chats sync between the CLI, the desktop app and every Visual Studio window. 🎉

## 📊 Usage statistics

**Tools -> OpenCode Studio Usage Statistics** opens a **local** dashboard built from the OpenCode API:

| 📈 Panel | What it shows |
|---|---|
| 💰 KPI cards | Total cost, tokens, sessions, requests |
| 🗓️ Cost by day | A smooth area chart |
| 🍩 Tokens by type | Input / output / reasoning / cache |
| 🤖 Cost by model | Spend per model |
| 🔍 Cost per request | For the current session |
| 📋 Session table | Model, agent, tokens and cost |

## 🛡️ Privacy

- 🖧 the server binds to `127.0.0.1` only
- 🔑 credentials are stored by OpenCode itself in your own environment
- 🚫 no telemetry, no analytics
- 📊 the dashboard reads from your local server
- 📈 the usage overlay signs in to the OpenCode Console and reads your usage and limits from `opencode.ai` using your own session cookie (stored encrypted with DPAPI) — no other data leaves your machine.

## 💬 Contacts & Support

- 📨 Telegram: https://t.me/eugenekray
- ✉️ Email: krasovskyworks@gmail.com
- 💜 Support the project: https://boosty.to/jevkray
- 🐙 Source code and issues: https://github.com/Jevkray/OpenCodeStudio

## 📜 License

Released under the [MIT License](https://github.com/Jevkray/OpenCodeStudio/blob/main/LICENSE).
