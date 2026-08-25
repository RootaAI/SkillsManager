# Changelog

All notable changes to this project are documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Planned

- **Prebuilt Windows binaries** — publish a self-contained, single-file
  `SkillsManager.exe` (no .NET runtime install required) on the GitHub
  Releases page for non-developer end users, built automatically by CI on
  each version tag.

- **Configurable skills root** — a setting to point the manager at any
  folder-per-skill tree instead of the fixed Copilot Cowork resolution
  order. The same `<root>\<skill-name>\SKILL.md` convention is used by
  other agent ecosystems — e.g. Claude Code skills under
  `~/.claude/skills` — so one setting would make the tool useful beyond
  Copilot Cowork (managing Copilot and Claude skills side by side).

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

[1.0.0]: https://github.com/RootaAI/SkillsManager/releases/tag/v1.0.0
