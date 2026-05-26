# Daylog Dock Validation

## Objective

Build and validate the MVP Windows Command Palette Dock extension for Daylog Dock using the native Command Palette extension stack only: C#, .NET, Windows App SDK / WinUI, Microsoft.CommandPalette.Extensions, Microsoft.CommandPalette.Extensions.Toolkit, and MSIX.

## Completion Audit

| Requirement | Evidence | Status |
| --- | --- | --- |
| Read primary build brief | `build-agent-brief.md` was read before implementation. | Done |
| Native stack only; no Electron, Tauri, React, Next.js, or web wrapper | `DaylogDockExtension/DaylogDockExtension.csproj` is a C# Windows Command Palette MSIX project with `Microsoft.CommandPalette.Extensions`, `Microsoft.Windows.CsWinRT`, `Shmuelie.WinRTServer`, and MSIX tooling packages. `scripts/verify-static.ps1` scans source/project files for banned web-wrapper terms. | Verified |
| First-run folder selection | Unconfigured provider state returns `PickFolderCommand`; settings also provides native picker plus path fallback. | Built, not runtime-verified |
| Dock band titled `Daylog Dock` | `DaylogDockExtensionCommandsProvider.DisplayName = "Daylog Dock"` and `GetDockBands()` returns the band item titled from `DisplayName`. | Built, not runtime-verified |
| Clicking dock band opens compact capture page | Configured band item uses `new CapturePage(_state)`. | Built, not runtime-verified |
| Lazy daily markdown creation after date changes | `EnsureTodayFile()` / `EnsureDailyFile()` are called from capture/open/reveal/append paths. Smoke test writes entries for `2026-05-15` and `2026-05-16`. | Service-verified |
| Single-note editor | `DaylogEditorWindow` loads `ReadDailyText()` into a multiline `TextBox` and autosaves via `SaveDailyText()`. Smoke test verifies overwrite behavior. | Service-verified |
| Capture writes local markdown | `DaylogMarkdownStore.SaveDailyText()` writes the whole selected daily note back to disk. Legacy append helpers remain covered for timestamped sections. | Service-verified |
| Default file layout `<chosen-folder>\Daylog\yyyy-MM\yyyy-MM-dd.md` | `DaylogMarkdownStore.FilePathFor()` combines root, `Daylog`, month, and date filename. Smoke test verifies exact generated paths. | Service-verified |
| Monthly folders vs flat Daylog folder | Kept monthly folder split after research: NTFS can handle many files, but monthly folders keep human browsing cleaner and align with daily-note tools that support automatic date subfolders. | Decided |
| Minimal hidden history last 7 to 14 daily files | `HistoryPage` calls `RecentFiles(14)`. Smoke test verifies count, newest-first order, entry count, and preview. | Service-verified |
| Date navigation | `DaylogState` enforces `Today + 30 days` as future limit and earliest saved daily file as past boundary. Smoke test covers both bounds. | Service-verified |
| Actions: Save, Open Today, Reveal Folder, History, Settings/Change Folder | Implemented in `CapturePage`, `DaylogShellCommands.cs`, `HistoryPage`, and `ChangeFolderPage`; static verifier checks action hooks. | Built and package-registered |
| Exclude non-goals: AI chat, video, PDF export, full sidebar library, search, tags, sync, calendar heatmap, Obsidian dependency, full markdown renderer | Implementation contains none of those feature surfaces. `scripts/verify-static.ps1` fails on the listed terms in source/project files. | Verified |
| Build gate | `.\scripts\validate.ps1` passes static verifier, smoke tests, restore, and native build. | Passed |
| Local deploy gate | `dotnet build ... -t:Deploy` was attempted and failed because the target is unavailable. `Add-AppxPackage -Register` against the generated MSIX layout now succeeds after Developer Mode/sideloading was available. | Passed |
| Command Palette reload/dock/runtime capture/Open Today/Reveal Folder | Package registration launches `DaylogDockExtension.exe` from the rebuilt output. Command Palette UI reload and hands-on button checks still require user interaction in the desktop host. | Partially verified |

## Commands Run

Static plus smoke validation:

```powershell
.\scripts\validate.ps1 -SkipBuild
```

Result:

```text
Static Daylog Dock verification passed.
Daylog markdown smoke tests passed.
```

Full validation after approved NuGet restore:

```powershell
.\scripts\validate.ps1
```

Result:

```text
Static Daylog Dock verification passed.
Daylog markdown smoke tests passed.
Build succeeded.
0 Warning(s)
0 Error(s)
```

CLI deploy target attempt:

```powershell
dotnet build .\DaylogDockExtension.sln -p:Platform=x64 -p:Configuration=Debug -t:Deploy
```

Result:

```text
error MSB4057: The target "Deploy" does not exist in the project.
```

Loose MSIX layout registration attempt:

```powershell
Add-AppxPackage -Register -ForceApplicationShutdown -Path 'C:\Users\thien\OneDrive\Desktop\projects\daylog\DaylogDockExtension\DaylogDockExtension\bin\x64\Debug\net9.0-windows10.0.26100.0\win-x64\AppxManifest.xml'
```

Result:

```text
Add-AppxPackage completed successfully.
DaylogDockExtension.exe started from the rebuilt win-x64 output.
```

## Next Required Manual Gate

Reload Command Palette extensions, pin/add `Daylog Dock` to the Dock, and run the runtime checklist in `DEPLOY.md`.
