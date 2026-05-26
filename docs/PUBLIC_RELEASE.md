# Public Release Plan

This is the simple distribution plan for Daylog Dock.

## Goal

Let people download Daylog Dock, install it, and use the PowerToys Command Palette dock integration without going through the Microsoft Store first.

The first public channel should be:

```text
GitHub Releases + daylog.thienbao.dev download page
```

Microsoft Store can wait until the app has more users and the release process is stable.

## What Users Need

Daylog Dock is not a standalone web app. The dock experience depends on PowerToys Command Palette.

Public install requirements:

- A Windows version supported by the current PowerToys release.
- PowerToys installed.
- Command Palette and Dock enabled in PowerToys.
- Daylog Dock installed as MSIX or MSIX bundle.

If a user does not install PowerToys, they can only use the standalone editor if we choose to ship that separately. They will not get the dock.

## Recommended Release Path

### Phase 1: Public Beta

Ship signed MSIX packages through GitHub Releases.

Release assets:

- `DaylogDock.msix` or `DaylogDock.msixbundle`
- `DaylogDock.appinstaller`
- `SHA256SUMS.txt`
- Release notes

Website:

- `https://daylog.thienbao.dev`
- Primary button points to the latest GitHub Release.
- Secondary link points to the source code.

This is the fastest useful public release. It keeps the app open source and avoids Microsoft Store review at the start.

### Phase 2: Easier Install

Add a `winget` package after the app has a stable signed release.

This gives users:

```powershell
winget install ThienBao.DaylogDock
```

Do this after the package identity, publisher name, and signing setup are stable.

### Phase 3: Microsoft Store

Use the Microsoft Store later if it becomes worth the overhead.

Reasons to wait:

- Store packaging and approval add friction.
- Daylog is still early.
- The first audience will likely be developers and writers who are comfortable with GitHub Releases.

Reasons to use the Store later:

- Easier trust for non-technical users.
- Automatic updates.
- Cleaner install path.
- Fewer Windows trust warnings.

## Signing

Public builds should be signed with a trusted certificate.

Do not make normal users import a self-signed certificate. That is fine for internal testing, but it is a bad public install experience.

Recommended options:

- Trusted code signing certificate.
- Microsoft Trusted Signing if available for the project.
- Timestamp the signature so old releases remain valid.

Keep the package identity stable once public users install it. Changing identity later can break update paths.

## Release Checklist

Before each release:

1. Update version.
2. Run validation.
3. Build Release x64 package.
4. Sign the package.
5. Install on a clean Windows user profile.
6. Confirm Command Palette reload shows `Daylog Dock`.
7. Confirm the dock opens the editor.
8. Confirm autosave writes markdown.
9. Confirm previous, today, next, folder, dark mode, and history still work.
10. Generate checksums.
11. Draft GitHub Release.
12. Update `daylog.thienbao.dev` download link.

## Release Notes Template

```markdown
# Daylog Dock v0.x.y

## Install

1. Install PowerToys and enable Command Palette Dock.
2. Download Daylog Dock.
3. Install the package.
4. Reload Command Palette extensions.
5. Add Daylog Dock to the Dock.

## Changed

- ...

## Fixed

- ...

## Known Issues

- ...
```

## Source Links

Use official docs when implementing the release scripts:

- MSIX signing: `https://learn.microsoft.com/windows/msix/package/signing-package-overview`
- App Installer files: `https://learn.microsoft.com/windows/msix/app-installer/app-installer-file-overview`
- PowerToys Command Palette: `https://learn.microsoft.com/windows/powertoys/command-palette/overview`
- PowerToys Command Palette Dock announcement: `https://devblogs.microsoft.com/commandline/powertoys-0-98-is-here-new-keyboard-manager-ux-the-command-palette-dock-and-better-cursorwrap/`
- WinGet package submission: `https://learn.microsoft.com/windows/package-manager/package/`
- Microsoft Store publishing: `https://learn.microsoft.com/windows/apps/publish/publish-your-app/overview`
