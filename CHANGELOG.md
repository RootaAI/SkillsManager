# Changelog

All notable changes to this project are documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Planned

- Frontmatter `description` shown next to each skill in the list.
- Find in editor (Ctrl+F).
- Rename skill (folder rename with the same validation as New Skill).
- Bilingual UI (English / 中文, following the system language).

## [1.1.0] - 2026-08-25

The manager grew from a Copilot Cowork tool into a manager for **every**
agent skills tree on the machine.

### Added

- **Skill libraries** — a dropdown switches between built-in libraries
  (Copilot Cowork, **Claude Code** at `%USERPROFILE%\.claude\skills`,
  **OpenAI Codex** at `%CODEX_HOME%\skills` falling back to
  `%USERPROFILE%\.codex\skills`) and any number of custom
  `<root>\<skill>\SKILL.md` folders added via the ⚙ Manage dialog.
  The last-used library is remembered.
- Settings persisted as human-editable JSON at
  `%LOCALAPPDATA%\SkillsManager\settings.json`; a missing or corrupt file
  falls back to defaults instead of failing.
- **External-change guard** — Save now detects that SKILL.md changed on disk
  after it was loaded (OneDrive sync from another device, another editor)
  and asks before overwriting.
- **Ctrl+S** saves from anywhere in the window.
- Skill-name validation now also rejects reserved Windows device names
  (CON, NUL, COM1, ...), trailing dots, and control characters — names that
  Windows would mangle or refuse, silently changing the skill's identity.
- `SkillsManager.Core`: cross-platform class library holding all testable
  logic (library resolution, settings JSON, name validation, line-ending
  normalization) with an xunit test suite that runs on Linux CI.
- CI now runs the test suite on every push and pull request.

### Changed

- Window title is now "Skills Manager — <library>"; the audit log qualifies
  skill names with the library id (e.g. `claude:my-skill`).
- Saved files are always written with LF line endings and no BOM
  (unchanged for Cowork, now guaranteed for every library).
- README rewritten around multi-agent libraries; added a Chinese README
  (`README.zh-CN.md`) with download and SmartScreen guidance.

### Released

- **Prebuilt Windows binaries** (planned since 1.0.0) — the tag-triggered
  pipeline publishes a self-contained, single-file `SkillsManager.exe`
  bundle on the GitHub Releases page; no .NET install required.

## [1.0.0] - 2026-08-25

Initial public release.

### Added

- Skill list with live name filter — one row per skill folder.
- In-place `SKILL.md` editor with explicit Save and an unsaved-changes guard
  when switching skills or closing the app.
- Paste normalization: every paste route (Ctrl+V, Shift+Ins, context menu) is
  intercepted and line endings are normalized to CRLF, so Markdown pasted from
  browsers or editors keeps its line structure.
- New Skill dialog — creates `<Skills>\<name>\SKILL.md`, auto-creating the
  whole tree on first use; invalid folder-name characters are rejected.
- Open Folder (jump to Explorer) and Refresh (rescan after external changes).
- Activity log at `%LOCALAPPDATA%\SkillsManager\SkillsManagerLog.txt`
  (`timestamp | user@machine | ACTION | skill name`, 5 MB cap).
- Skills-root resolution that follows OneDrive Known Folder Move, with
  `%OneDriveCommercial%` / `%OneDrive%` and a local fallback.
- Per-monitor-V2 DPI awareness.
- No in-app delete, by design — deleting through Explorer keeps the Recycle
  Bin and OneDrive version history as safety nets.

[1.1.0]: https://github.com/RootaAI/SkillsManager/releases/tag/v1.1.0
[1.0.0]: https://github.com/RootaAI/SkillsManager/releases/tag/v1.0.0
