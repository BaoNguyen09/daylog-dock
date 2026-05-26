# Landing Page Plan

Future domain:

```text
https://daylog.thienbao.dev
```

The domain is not ready yet. Use this doc later when the site is built.

The static site lives in `site/`. GitHub Pages setup steps live in `docs/GITHUB_PAGES.md`.

## Page Goal

Make it obvious what Daylog Dock is, who it is for, and how to install it.

One clear sentence:

```text
Daylog Dock is a quiet daily markdown note app for Windows, built into the PowerToys Command Palette Dock.
```

## Primary CTA

```text
Download for Windows
```

The button should point to the latest GitHub Release until a dedicated download endpoint exists.

Secondary CTA:

```text
Source on GitHub
```

## Page Structure

1. Product name: `Daylog Dock`.
2. One-line description.
3. Screenshot of the editor.
4. Download button.
5. Source code link.
6. Three short benefits:
   - One note per day.
   - Local markdown files.
   - Opens from Command Palette Dock.
7. Install steps:
   - Install PowerToys.
   - Enable Command Palette Dock.
   - Install Daylog Dock.
   - Reload extensions.
   - Choose a folder and write.
8. Privacy:
   - Files stay local.
   - No account.
   - No sync.
   - No AI features.
9. Contribute link.

Keep the page plain. This is a writing instrument, not a dashboard.

## Copy Draft

```text
Daylog Dock

A quiet daily markdown note app for Windows, built into the PowerToys Command Palette Dock.

One note per day. Local files. No account. No sync.

[Download for Windows] [Source on GitHub]
```

```text
How it works

Daylog creates one markdown file for each day:

<chosen-folder>\Daylog\yyyy-MM\yyyy-MM-dd.md

Click the dock item, write, and close it. Daylog autosaves silently.
```

```text
Install

1. Install PowerToys.
2. Enable Command Palette Dock.
3. Install Daylog Dock.
4. Reload Command Palette extensions.
5. Add Daylog Dock to the Dock.
```

## DNS Later

When ready:

1. Pick a host: GitHub Pages, Vercel, Netlify, or Cloudflare Pages.
2. Add a `daylog` DNS record for `thienbao.dev`.
3. Point it to the host target.
4. Enable HTTPS.
5. Set the canonical URL to `https://daylog.thienbao.dev`.
6. Set Open Graph URL to `https://daylog.thienbao.dev`.
7. Link the primary download button to the latest GitHub Release.

## Website Release Checklist

Before publishing:

1. Confirm the download link works.
2. Confirm the GitHub source link works.
3. Confirm screenshots match the current app.
4. Confirm install steps mention PowerToys Command Palette.
5. Confirm privacy copy is accurate.
6. Confirm the app version on the page matches the latest release.
