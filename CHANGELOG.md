# Changelog

All notable changes to **OpenCode Studio** are documented here.
The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/).

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

[1.0.7]: https://github.com/Jevkray/OpenCodeStudio/releases/tag/v1.0.7
[1.0.6]: https://github.com/Jevkray/OpenCodeStudio/releases/tag/v1.0.6
[1.0.0]: https://github.com/Jevkray/OpenCodeStudio/releases/tag/v1.0.0
