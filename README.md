# SkillsManager

[中文](README.zh-CN.md) | **English**

A standalone Windows desktop manager for **AI agent skills** — one tool for
every skills tree on the machine:

- **Microsoft Copilot Cowork** — the OneDrive-synced `Documents\Cowork\Skills`
- **Claude Code** — `%USERPROFILE%\.claude\skills`
- **OpenAI Codex** — `%CODEX_HOME%\skills` (default `%USERPROFILE%\.codex\skills`)
- **Any custom folder** following the same one-folder-per-skill convention

All of these runtimes recognize a skill by its **folder name**: every skill is
a folder containing one `SKILL.md`. Because every file has the same name,
folders are the only identity — which makes hand-managing skills in Explorer
error-prone. This tool gives them a proper editor.

## Download

Grab the latest `SkillsManager-vX.Y.Z-win-x64.zip` from the
[Releases page](https://github.com/RootaAI/SkillsManager/releases), unzip, and
run `SkillsManager.exe` — self-contained, no .NET install required.

First launch may show a SmartScreen prompt (unsigned new binary, routine for
open-source tools): click **More info** → **Run anyway**. It only asks once.

## Features

- **Library switcher** — a dropdown flips between Copilot Cowork, Claude Code,
  Codex, and custom libraries; the last-used library is remembered.
- **Custom libraries** — the ⚙ button registers any
  `<root>\<skill-name>\SKILL.md` tree (a team share, a repo's
  `.claude/skills`, ...). Stored in human-editable
  `%LOCALAPPDATA%\SkillsManager\settings.json`.
- **Skill list** — one row per skill folder, filterable by name.
- **In-place SKILL.md editor** — explicit Save (button or **Ctrl+S**),
  unsaved-changes guard on switch/close, word wrap for Markdown prose.
- **External-change guard** — if the file changed on disk after it was loaded
  (OneDrive sync from another device, another editor), Save asks before
  overwriting instead of silently destroying that version.
- **Paste-safe editor** — pasted Markdown from browsers/VS Code/Copilot
  carries lone-LF line endings, which a stock WinForms TextBox renders as one
  run-together line; the editor intercepts `WM_PASTE` and normalizes line
  endings so bullets stay line-by-line.
- **New Skill** — creates `<root>\<name>\SKILL.md` (the whole tree is
  auto-created on first use); invalid folder characters, reserved Windows
  device names (CON, NUL, ...), and trailing-dot traps are rejected up front.
- **Open Folder / Refresh** — jump to Explorer, rescan after external changes.
- **Activity log** — SKILL-CREATE / SKILL-SAVE lines (library-qualified) in
  `%LOCALAPPDATA%\SkillsManager\SkillsManagerLog.txt`.
- **Deliberately no Delete** — deletion goes through Open Folder → Explorer,
  which uses the Recycle Bin (and OneDrive version history on synced roots);
  safer than any in-app permanent delete.

## Library root resolution

**Copilot Cowork** — first existing candidate wins; if none exists yet, the
first candidate is created on first save:

1. `<Documents>\Cowork\Skills` — `SpecialFolder.MyDocuments`, which follows
   OneDrive Known Folder Move automatically (usually the OneDrive path).
2. `%OneDriveCommercial%\Documents\Cowork\Skills`
3. `%OneDrive%\Documents\Cowork\Skills`
4. Fallback: `%LOCALAPPDATA%\SkillsManager\Cowork\Skills` (no Documents or
   OneDrive available at all).

**Claude Code** — `%USERPROFILE%\.claude\skills` ·
**Codex** — `%CODEX_HOME%\skills`, else `%USERPROFILE%\.codex\skills`.

## Build

.NET 8 SDK, Windows target:

```
dotnet build SkillsManager.sln
```

(On a non-Windows build host add `-p:EnableWindowsTargeting=true` and use the
official Microsoft SDK — distro-packaged SDKs omit the WindowsDesktop
component.) Run the tests:

```
dotnet test SkillsManager.Core.Tests
```

The app has no NuGet dependencies. All UI is created in code — no Designer.cs,
no .resx. Testable logic (library resolution, settings, name validation,
line-ending normalization) lives in the cross-platform `SkillsManager.Core`,
covered by xunit.

---

Built by **Roota AI** · Find us on Rednote (小红书): **若塔AI**
