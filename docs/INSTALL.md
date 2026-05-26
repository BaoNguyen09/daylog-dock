# Install Daylog Dock

Daylog Dock is a native Windows app plus a PowerToys Command Palette Dock extension.

You need both:

- A Windows version supported by the current PowerToys release.
- Microsoft PowerToys with Command Palette Dock support.
- The Daylog Dock installer from GitHub Releases or, later, `https://daylog.thienbao.dev`.

## Install

1. Install or update PowerToys.
2. Open PowerToys and enable Command Palette.
3. Download the latest Daylog Dock package.
4. Open the installer package.
5. Launch Command Palette.
6. Run `Reload Command Palette extensions`.
7. Add or pin `Daylog Dock` to the Dock.
8. Click the dock item and choose your journal folder.

Daylog writes one markdown file per day:

```text
<chosen-folder>\Daylog\yyyy-MM\yyyy-MM-dd.md
```

## Daily Use

- Click the `Daylog Dock` dock item to open the editor.
- Type in the note area.
- Daylog autosaves silently after you pause typing.
- Use `<`, `today`, and `>` to move between days.
- Future notes are allowed up to 30 days ahead.
- Past notes are available back to the first saved Daylog entry.
- Use `folder` to change the journal folder.
- Use `History` to jump to saved dates.

There is no Save button by design. The note is the file.

## If The Dock Does Not Show

Try this order:

1. Make sure PowerToys Command Palette and Dock are enabled.
2. Restart Command Palette.
3. Run `Reload Command Palette extensions`.
4. Restart PowerToys.
5. Reinstall Daylog Dock.

If the standalone Daylog editor opens but the dock item does not appear, the app is installed but the Command Palette extension did not register or reload.

## Windows Warning

For public releases, Daylog should be signed. If Windows warns about an unknown publisher, only continue if you downloaded the installer from the official GitHub release or `https://daylog.thienbao.dev`.

Do not ask normal users to install a self-signed certificate. Self-signed builds are only for private testing.

## Uninstall

Use Windows Settings:

```text
Settings > Apps > Installed apps > Daylog Dock > Uninstall
```

Your markdown files are not deleted. They stay in your chosen journal folder.
