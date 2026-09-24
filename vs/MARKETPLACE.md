# 🟣 OpenCode Studio

**The OpenCode AI coding agent — natively inside Visual Studio.**

*A fast, self-contained tool window with a first-run import wizard and shared chat history. Works with Visual Studio 2022 and 2026.*

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

- **1.1.2**
  - Always uses the **latest installed opencode**: on start the server re-checks the binary (path + modification time) and restarts if it changed; `server.json` now records the real server process PID.
  - The first-run wizard is now a **Settings** window with **Import** and **Settings** tabs; the button is **Save selected** and applies both imports and settings.
  - The **"OpenCode Studio Usage Statistics"** menu item and its window are temporarily removed (statistics will return later); a button in the panel opens the OpenCode Console in the meantime.
- **1.1.1**
  - Works with **opencode v1 and v2 (including the v2 beta)**: the extension sets the server password itself, sends HTTP Basic Auth on every request and passes the credentials to WebView2 (the v2 server requires auth after the CVE-2026-22812 fix). The misleading "not in PATH" error is gone.
  - Auto-detects the v2 beta binary (`opencode2`) and adds a native **"Use OpenCode v2.x (preview)"** switch (settings window and **Tools → Options**); `OPENCODESTUDIO_OPENCODE_BIN` forces a binary path.
  - One VSIX for **Visual Studio 2022 and 2026**.
  - Fixes: no more v2 reconnect loop (health check no longer demands `{"healthy":true}`), the whole process tree is terminated (`taskkill /T`, so the port no longer "jumps"), and readiness no longer depends on the startup log line.
- **1.1.0** - Statistics button in the OpenCode panel: open your profile and check real usage without leaving Visual Studio.

## ✨ Highlights

- 🧠 **Real OpenCode, not a clone** — the complete official web UI, rendered directly in Visual Studio.
- 🔀 **opencode v1 and v2 (beta)** — a native **"Use OpenCode v2.x (preview)"** toggle in the first-run wizard and in **Tools → Options → OpenCode Studio** switches between versions (restarting the server); the extension auto-detects `opencode2` and handles the v2 server's HTTP Basic Auth.
- 🔄 **Shared history** — the same chats as the CLI and desktop app, across every Visual Studio window.
- 🗂️ **All chats visible** — opens the web UI home, listing every project and session.
- 📈 **Profile & usage in one click** - the statistics button opens your OpenCode console inside the panel, so you can check real spend and limits without switching to a browser.
- 📥 **First-run wizard** — import settings, themes, credentials and history in one click.
- 🔁 **Auto-reconnect** — recovers the session by itself if the server drops.
- 🔒 **Local-first** — loopback only, zero telemetry.
- 🪶 **Lightweight** — one VSIX for Visual Studio 2022 and 2026.

## 🚀 Getting started

```
View   ->  Other Windows  ->  OpenCode Studio        (the agent)
Tools  ->  OpenCode Studio Settings                 (setup wizard)
```

> **Requirement:** the OpenCode CLI (v1 or v2 beta) must be installed and available on your `PATH`.

## 🔗 Syncing chat history

OpenCode stores every session **against the project directory it was created in**. Because OpenCode Studio shares the same data as the CLI and desktop app, all chats are already available — but grouped by project path.

If a project lives at a **different absolute path** than when its chats were created (another drive, another user name, a moved or renamed folder), those sessions stay under the original path. **Line up the project paths and everything synchronizes.**

1. Open each project at the **same absolute path** it originally used.
2. If the path changed, move the project back, or import the old data via **Tools -> OpenCode Studio Settings** pointing at the folder with the original `opencode.db`.
3. Reopen the tool window — the sessions appear under the matching project.

Once the paths match, all chats sync between the CLI, the desktop app and every Visual Studio window. 🎉

## Usage statistics

Statistics are not part of this release yet. Use the statistics button in the OpenCode panel to open the OpenCode Console (usage and limits) directly.

## 🛡️ Privacy

- 🖧 the server binds to `127.0.0.1` only
- 🔑 credentials are stored by OpenCode itself in your own environment
- 🚫 no telemetry, no analytics
- 📈 the usage overlay signs in to the OpenCode Console and reads your usage and limits from `opencode.ai` using your own session cookie (stored encrypted with DPAPI) — no other data leaves your machine.

## 💬 Contacts & Support

- 📨 Telegram: https://t.me/eugenekray
- ✉️ Email: krasovskyworks@gmail.com
- 💜 Support the project: https://boosty.to/jevkray
- 🐙 Source code and issues: https://github.com/Jevkray/OpenCodeStudio

## 📜 License

Released under the [MIT License](https://github.com/Jevkray/OpenCodeStudio/blob/main/LICENSE).
