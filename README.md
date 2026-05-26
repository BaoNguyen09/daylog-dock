# Daylog Dock

Daylog Dock is a native Windows Command Palette Dock extension for quick daily markdown capture.

It writes local files to:

```text
<chosen-folder>\Daylog\yyyy-MM\yyyy-MM-dd.md
```

Daylog keeps the monthly folder split. Flat `Daylog\yyyy-MM-dd.md` is fine for NTFS at normal journal sizes, but `Daylog\yyyy-MM\yyyy-MM-dd.md` is cleaner in Explorer, keeps yearly history from becoming one long directory, and matches common daily-note tooling that supports automatic date-based subfolders.

Each daily file is one editable markdown note. The compact editor loads that file into the text box and writes the whole note back to disk.

## Scope

Implemented MVP surfaces:

- First-run folder choice with native folder picker and path fallback.
- Explicit Command Palette Dock band titled `Daylog Dock`.
- Dock band launches a native WinUI editor (`-Editor`) with multiline notepad feel and live autosave (~650ms debounce).
- CmdPal capture fallback is markdown-only (no Save/Open form).
- Lazy daily file creation.
- Visible selected-day note preview.
- Previous / Today / Next navigation with a 30-day future cap and past navigation bounded by the first saved daily log.
- Bottom rail with typography controls, light/dark mode, and history calendar.
- History calendar jump: saved past dates are selectable, empty past dates are disabled, today and future dates remain available through the 30-day cap.
- `Open Today`, `Reveal Folder`, `History`, and `Settings`.
- Hidden recent history for the newest 14 daily files.

Non-goals are intentionally excluded: in-app AI chat, video, PDF export, full markdown renderer, search, tags, sync, calendar heatmap, and Obsidian dependency.

## Validate

This repo pins the validated .NET SDK feature band in `global.json`.

Run local checks:

```powershell
.\scripts\validate.ps1 -SkipBuild
```

After deploy, run the WinUI autosave check (closes any running `DaylogDockExtension` processes first):

```powershell
.\scripts\verify-deploy.ps1
```

Run full validation once NuGet restore is allowed:

```powershell
.\scripts\validate.ps1
```

If prior failed restore artifacts get in the way:

```powershell
.\scripts\validate.ps1 -Clean
```

Current validation status: full build passes after an approved NuGet restore. Local debug registration uses loose-package MSIX registration and may require Developer Mode or sideloading depending on Windows policy.

## Install And Release Docs

- User install guide: `docs\INSTALL.md`
- Public release plan: `docs\PUBLIC_RELEASE.md`
- Landing page plan for `daylog.thienbao.dev`: `docs\LANDING_PAGE.md`
- GitHub Pages setup: `docs\GITHUB_PAGES.md`

Public distribution should start with GitHub Releases plus the future `daylog.thienbao.dev` download page. The dock integration requires PowerToys Command Palette, so all public install copy should say that plainly.

## Standalone app (startup)

Install Daylog as its own app under `%LOCALAPPDATA%\Programs\Daylog` and add a Windows Startup entry (minimized, dock-ready):

```powershell
.\scripts\install-daylog-standalone.ps1
```

On sign-in, Daylog starts in the background. The Command Palette dock band toggles the same editor window (single instance).

Remove:

```powershell
.\scripts\uninstall-daylog-standalone.ps1
```

## Deploy

See `DEPLOY.md` for local developer deploy, Command Palette reload, Dock pinning, and runtime markdown checks.
