# Changelog

All notable changes to **OpenCode Studio** are documented here.
The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/).

## [1.1.0] - 2026-09-22

### Added
- Usage overlay inside the OpenCode panel: remaining day/week/month limits for an OpenCode Go subscription and today's spend, with an **Использование** button.
- A usage dashboard window with per-period limits (percent and dollars), a per-model breakdown and a stacked daily histogram with a month switcher.
- Sign-in to the OpenCode Console (session cookie captured from the embedded browser, stored encrypted with DPAPI).
- New **Tools -> Options -> OpenCode Studio -> Usage & Limits** settings page.
- A dedicated checkbox in the first-run wizard to enable the usage overlay.

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

[1.1.0]: https://github.com/Jevkray/OpenCodeStudio/releases/tag/v1.1.0
[1.0.10]: https://github.com/Jevkray/OpenCodeStudio/releases/tag/v1.0.10
[1.0.9]: https://github.com/Jevkray/OpenCodeStudio/releases/tag/v1.0.9
[1.0.8]: https://github.com/Jevkray/OpenCodeStudio/releases/tag/v1.0.8
[1.0.7]: https://github.com/Jevkray/OpenCodeStudio/releases/tag/v1.0.7
[1.0.6]: https://github.com/Jevkray/OpenCodeStudio/releases/tag/v1.0.6
[1.0.0]: https://github.com/Jevkray/OpenCodeStudio/releases/tag/v1.0.0
