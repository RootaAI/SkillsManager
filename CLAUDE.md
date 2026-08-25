# CLAUDE.md

Guidance for Claude Code when working in this repository.

## What this is

A Windows desktop (.NET 8 WinForms) manager for AI agent skill folders.
A "skill" is a folder containing one `SKILL.md`; the folder name is the
skill's identity. The app manages several "libraries" (skills roots):
built-in Copilot Cowork (OneDrive Documents), Claude Code
(`%USERPROFILE%\.claude\skills`), Codex (`%CODEX_HOME%\skills` →
`%USERPROFILE%\.codex\skills`), plus user-added custom folders.

## Project layout

- `SkillsManager.csproj` + root `.cs` files — the WinForms shell
  (`net8.0-windows`). All UI is created in code: no Designer.cs, no .resx,
  no NuGet dependencies.
  - `SkillsManagerForm.cs` — the whole UI: library picker, filterable skill
    list, SKILL.md editor with paste normalization (WM_PASTE interception),
    external-change guard on save, Ctrl+S.
  - `SettingsStore.cs` — file I/O for settings.json (shape lives in Core).
  - `AuditLogger.cs` — append-only log under `%LOCALAPPDATA%\SkillsManager`.
- `SkillsManager.Core/` — cross-platform `net8.0` class library holding all
  testable logic: `LibraryCatalog` (root resolution behind
  `ISkillsEnvironment`), `AppSettings` (JSON), `SkillName` (validation),
  `TextUtil` (line-ending normalization). Put new logic HERE, not in the
  form, so it stays testable.
- `SkillsManager.Core.Tests/` — xunit suite; runs on any OS.

## Build & test

```
dotnet test SkillsManager.Core.Tests        # works everywhere, run this always
dotnet build SkillsManager.sln              # full build needs Windows
```

The WinForms shell cannot be built with distro-packaged Linux SDKs (they
omit the WindowsDesktop MSBuild SDK; `-p:EnableWindowsTargeting=true` does
not fix that). On Linux, type-check form changes by shadow-compiling the
root `.cs` files (minus `Program.cs`) against the
`Microsoft.WindowsDesktop.App.Ref` NuGet reference assemblies; the
authoritative Windows build runs in CI (`.github/workflows/ci.yml`).

## Conventions

- Development workflow follows `.claude/skills/senior-coding/SKILL.md`:
  plan first, TDD for all Core logic, root-cause debugging, surgical diffs.
- SKILL.md files are written UTF-8 **without BOM**, **LF** line endings;
  the editor shows CRLF (WinForms requirement) — conversions go through
  `TextUtil` only.
- Windows filename rules are enforced in `SkillName` on every OS (skills
  sync to Windows via OneDrive), including reserved device names and
  trailing dots.
- No in-app delete, by design: deletion goes through Explorer (Recycle
  Bin + OneDrive version history). Do not add one.
- `<Version>` in `SkillsManager.csproj` stays in sync with the newest
  `CHANGELOG.md` entry; releases are published by pushing a `v*` tag
  (`.github/workflows/release.yml`).
- Docs come in pairs: any user-facing change updates both `README.md`
  (English) and `README.zh-CN.md` (中文), plus `CHANGELOG.md`
  (Keep a Changelog format).
- File headers carry the Roota AI attribution comment — keep it on new files.
