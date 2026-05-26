# Daylog Dock Local Deploy

This file is for local developer deploy and testing.

For public distribution, use `docs\PUBLIC_RELEASE.md`.

## Prerequisite

Run validation from this folder:

```powershell
.\scripts\validate.ps1
```

Do not deploy until this passes.

If restore or build artifacts misbehave:

```powershell
.\scripts\validate.ps1 -Clean
```

## Register the Debug package (CLI)

```powershell
.\scripts\register-extension.ps1
```

This stops stale processes, removes old registrations, builds, registers the MSIX layout, and verifies `DaylogDockExtension.Editor.exe` is bundled beside the COM host.

Manual register:

```powershell
Add-AppxPackage -Register -ForceApplicationShutdown -Path .\DaylogDockExtension\bin\x64\Debug\net9.0-windows10.0.26100.0\win-x64\AppxManifest.xml
```

Or run the automated deploy + editor autosave check:

```powershell
.\scripts\verify-deploy.ps1
```

`verify-deploy.ps1` builds (unless `-SkipBuild`), registers the package, launches the WinUI editor, types a marker through UI Automation, and confirms the daily markdown file was written under a temp folder. It resets `settings.json` to an empty folder when finished.

## Deploy through Visual Studio

1. Open `DaylogDockExtension.sln`.
2. Select `Debug` and `x64`.
3. Select the package launch profile, not unpackaged.
4. Use **Build > Deploy Solution**. Build alone is not enough; deploy registers the MSIX COM server.
5. Open Command Palette.
6. Run `Reload Command Palette extensions`.
7. Find `Daylog Dock`.
8. Pin or add `Daylog Dock` to the Dock.

## Runtime checks (WinUI editor)

1. First run should show `Daylog Dock` with **Choose folder** (or open the editor and pick a folder from Settings).
2. Choose your journal folder.
3. Click the **Daylog Dock** dock band. A native **Daylog Dock** WinUI window should open (not the old Command Palette form with Save/Open buttons).
4. Type in the multiline editor:

```text
Daylog Dock validation entry.
```

5. Wait about one second for silent autosave.
6. Confirm this file exists:

```text
<chosen-folder>\Daylog\yyyy-MM\yyyy-MM-dd.md
```

7. Confirm the file contains your note text (whole-file save, not timestamp sections unless you use legacy append paths).
8. From the editor header: **folder** changes the Daylog root.
9. From Command Palette context menu: **Open Today**, **Reveal Folder**, **History**, **Settings** still work.
10. In the WinUI editor, use `<` and `>` to move days. Future navigation stops 30 days ahead; past navigation stops at the first saved daily log.

## CLI deploy notes

`dotnet build -t:Deploy` is not available on these projects outside Visual Studio (`MSB4057`). Use `Add-AppxPackage -Register` against the generated `AppxManifest.xml` instead.

Unsigned loose-package registration requires **Developer Mode** or sideloading enabled on the machine.
