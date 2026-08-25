# SkillsManager

A standalone Windows desktop manager for **Microsoft Copilot Cowork skills**.

Cowork recognizes a skill by its **folder name**: every skill is a folder under
the OneDrive-synced `Documents\Cowork\Skills`, containing one `SKILL.md`.
Because every file has the same name, folders are the only identity — which
makes hand-managing skills in Explorer error-prone. This tool gives them a
proper editor.

## Features

- **Skill list** — one row per skill folder, filterable by name.
- **In-place SKILL.md editor** — explicit Save, unsaved-changes guard on
  switch/close, word wrap for Markdown prose.
- **Paste-safe editor** — pasted Markdown from browsers/VS Code/Copilot
  carries lone-LF line endings, which a stock WinForms TextBox renders as one
  run-together line; the editor intercepts `WM_PASTE` and normalizes to CRLF
  so bullets stay line-by-line.
- **New Skill** — creates `<Skills>\<name>\SKILL.md` (the whole tree is
  auto-created on first use); invalid folder characters rejected.
- **Open Folder / Refresh** — jump to Explorer, rescan after external changes.
- **Activity log** — SKILL-CREATE / SKILL-SAVE lines in
  `%LOCALAPPDATA%\SkillsManager\SkillsManagerLog.txt`.
- **Deliberately no Delete** — deletion goes through Open Folder → Explorer,
  which uses the Recycle Bin and OneDrive version history; safer than any
  in-app permanent delete.

## Skills root resolution

First existing candidate wins; if none exists yet, the first candidate is
created on first save:

1. `<Documents>\Cowork\Skills` — `SpecialFolder.MyDocuments`, which follows
   OneDrive Known Folder Move automatically (usually the OneDrive path).
2. `%OneDriveCommercial%\Documents\Cowork\Skills`
3. `%OneDrive%\Documents\Cowork\Skills`
4. Fallback: `%LOCALAPPDATA%\SkillsManager\Cowork\Skills` (no Documents or
   OneDrive available at all).

## Build

.NET 8 SDK, Windows target:

```
dotnet build SkillsManager.csproj
```

(On a non-Windows build host add `-p:EnableWindowsTargeting=true`.)

No NuGet dependencies. All UI is created in code — no Designer.cs, no .resx.

---

Built by **Roota AI** · Find us on Rednote (小红书): **若塔AI**
