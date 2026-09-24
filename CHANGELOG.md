# Changelog

All notable changes to **OpenCode Studio** are documented here.
The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/).

## [1.1.2] - 2026-09-24

### Changed
- **Always uses the latest installed opencode.** On start the server re-checks the binary (its path and modification time) and restarts if it was updated (or the v1/v2 mode changed), so a fresh start automatically picks up the latest installed version.
- The real PID of the server process is now written to `server.json`.
- The import wizard is now a plain **Settings** window with two tabs (**Import** and **Settings**); the welcome texts were removed.
- The first-run wizard button is now **Save selected** (it applies your choices, including settings, not only imports).
- Removed the "OpenCode Studio Usage Statistics" menu item and its window for now (statistics will return later; a button in the panel opens the OpenCode Console in the meantime).

## [1.1.1] - 2026-09-24

### Added
- **OpenCode v1 and v2 support.** The extension now secures the embedded server with its own password and sends Basic Auth on every request (and to WebView2), so it works both with stable opencode v1 and with opencode v2 / the v2 beta, which protect the server after the CVE-2026-22812 fix. The misleading "not in PATH" error is gone.
- Detect the OpenCode 2 beta binary (`opencode2`) in addition to `opencode`, plus a native **Use OpenCode v2.x (preview)** switch in the settings window and in Tools -> Options -> OpenCode Studio. Changing it restarts the server with the selected binary. `OPENCODESTUDIO_OPENCODE_BIN` can force a binary path.
- **Visual Studio 2022 and 2026 support** — one VSIX for both.

### Fixed
- **OpenCode v2 (beta) reconnect loop.** The v2 server answers every path with its web app, so the strict `{"healthy":true}` health check always failed and the extension kept restarting the server ("connection lost - reconnecting"). Health now only requires a successful HTTP response, which works for both v1 and v2.
- Server processes are now killed **with their child tree** (`taskkill /T`). Previously a `.cmd` shim left the Node process alive, so it kept holding the port and every restart picked a new random port.
- Readiness no longer depends on the server's exact startup log line.

## [1.1.0] - 2026-09-22

### Added
- **Statistics button in the OpenCode panel.** It sits just left of the context button in the prompt footer, and on opencode.ai pages it docks to the corner. One click opens your OpenCode profile and console right inside the tool window - check real usage and subscription limits without leaving Visual Studio, and click again to return to your chats.
- **Sign-in to the OpenCode Console** inside the embedded browser (Google, GitHub or email). The session cookie is captured after the return navigation and stored encrypted with DPAPI.
- **Tools -> Options -> OpenCode Studio -> Usage & Limits** settings page, plus a checkbox in the first-run wizard.
- Reworked bilingual documentation (English and Russian) and a Marketplace description with a preview.

### Fixed
- **Stable server port** - the preferred port, then the last used one, then a free one. The embedded UI keeps a constant origin, so local settings, theme and chat history no longer reset between restarts.
- Sign-in reliably returns to the app after authentication and restarts the monitor.
- The injected button no longer triggers the context tooltip.

### Notes
- The built-in usage dashboard (remaining Go limits and spend charts inside the extension) is in progress - see the roadmap issue on GitHub.

## [1.0.10] - 2026-09-20

### Added
- Reworked bilingual documentation (English and Russian) with a preview screenshot, feature overview and contacts (Telegram, email, Boosty).
- A dedicated **Syncing chat history** guide: chats are grouped by project path, so matching the original project paths makes the full history appear everywhere.
- Marketplace description with a preview section.

## [1.0.9] - 2026-09-20

### Changed
- The tool window now opens the OpenCode web UI **home**, so all projects and every chat are visible - exactly like the OpenCode desktop app.
- The server uses a **stable port** (4096 when free), keeping the WebView2 origin constant so the web UI remembers its state between restarts.
- The extension no longer creates an empty session on every project change.

## [1.0.8] - 2026-09-20

### Fixed
- **Chat history now loads in the tool window.** The navigation URL now encodes the project directory exactly as OpenCode reports it (a Windows path with backslashes); previously a normalized forward-slash path produced a different base64 and the web UI could not resolve the project.
- API responses are now decoded as UTF-8 explicitly, so non-ASCII (e.g. Cyrillic) project paths match correctly on .NET Framework.

## [1.0.7] - 2026-09-20

### Fixed
- **Chat history now syncs** with the OpenCode CLI and desktop app: OpenCode Studio uses the shared default OpenCode environment instead of an isolated one.
- **Multiple Visual Studio windows share one OpenCode server** (reused via a registry file), so sessions and live agent state stay in sync between windows.
- **History no longer appears to reset** when a solution is opened or closed: the last project root is remembered as a stable fallback.

### Changed
- The import wizard now targets the shared environment and is intended for importing from external folders/backups.

## [1.0.6] - 2026-09-20

### Added
- New **OpenCode Studio** brand and icon set (light and dark variants).
- Command, tool window and menu icons.
- First-run **import wizard** with multi-source detection (CLI, Desktop, custom folder).
- **Isolated OpenCode environment** under `%LOCALAPPDATA%\OpenCodeStudio`.
- File logging to `%LOCALAPPDATA%\OpenCodeStudio\logs\opencode-studio.log`.
- `git`-agnostic CI workflow for building the VSIX.

### Changed
- **Re-architected**: WebView2 now talks directly to the OpenCode server origin instead of a `*.vsoc-app` proxy.
- Removed the COM host bridge, request proxy and injected scripts.
- Tool window caption is now "OpenCode Studio".
- Server no longer shuts down after being idle.

### Fixed
- Server URL detection no longer aborts on the first read (the `ReadLineAsync` race).
- Connection drops are recovered automatically with a progress page and retries.
- Prefers the `.exe` OpenCode executable; runs `.cmd`/`.bat` shims via `cmd.exe`.

## [1.0.0] - 2026-09-19

### Added
- Initial public release: OpenCode web UI embedded in a Visual Studio tool window.

[1.1.2]: https://github.com/Jevkray/OpenCodeStudio/releases/tag/v1.1.2
[1.1.1]: https://github.com/Jevkray/OpenCodeStudio/releases/tag/v1.1.1
[1.1.0]: https://github.com/Jevkray/OpenCodeStudio/releases/tag/v1.1.0
[1.0.10]: https://github.com/Jevkray/OpenCodeStudio/releases/tag/v1.0.10
[1.0.9]: https://github.com/Jevkray/OpenCodeStudio/releases/tag/v1.0.9
[1.0.8]: https://github.com/Jevkray/OpenCodeStudio/releases/tag/v1.0.8
[1.0.7]: https://github.com/Jevkray/OpenCodeStudio/releases/tag/v1.0.7
[1.0.6]: https://github.com/Jevkray/OpenCodeStudio/releases/tag/v1.0.6
[1.0.0]: https://github.com/Jevkray/OpenCodeStudio/releases/tag/v1.0.0
