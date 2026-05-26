# GitHub Pages Setup

There is no live Daylog Dock website until this folder is pushed to GitHub and GitHub Pages deploys.

Recommended repository:

```text
https://github.com/BaoNguyen09/daylog-dock
```

Default GitHub Pages URL after the workflow succeeds:

```text
https://baonguyen09.github.io/daylog-dock/
```

Future custom domain:

```text
https://daylog.thienbao.dev
```

## What Is Already In This Repo

- `site/index.html`: static landing page.
- `site/styles.css`: landing page styling.
- `.github/workflows/pages.yml`: GitHub Pages deploy workflow.
- `site/CNAME.example`: custom-domain value for later.

## First Publish

From the product folder:

```powershell
git init
git add .
git commit -m "initial daylog dock release"
git branch -M main
gh repo create BaoNguyen09/daylog-dock --public --source . --remote origin --push
```

Then in GitHub:

1. Open `https://github.com/BaoNguyen09/daylog-dock`.
2. Go to `Settings > Pages`.
3. Set source to `GitHub Actions`.
4. Open `Actions`.
5. Run or wait for `Deploy Website`.
6. Open `https://baonguyen09.github.io/daylog-dock/`.

## Connect The Domain Later

Only do this after the GitHub Pages URL works.

1. In GitHub, open `Settings > Pages`.
2. Set custom domain to:

```text
daylog.thienbao.dev
```

3. In your DNS provider, add this record:

```text
Type: CNAME
Name: daylog
Target: baonguyen09.github.io
```

4. Wait for DNS to propagate.
5. In GitHub Pages, enable `Enforce HTTPS`.
6. Copy `site/CNAME.example` to `site/CNAME`.
7. Commit and push the `site/CNAME` file.

After that, the public URL is:

```text
https://daylog.thienbao.dev
```

## If You Use A Different Repo Name

The default Pages URL changes:

```text
https://baonguyen09.github.io/<repo-name>/
```

The custom domain can still be:

```text
https://daylog.thienbao.dev
```

