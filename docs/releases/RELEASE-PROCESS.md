---
title: "How to cut a release by hand"
layout: default
nav_exclude: true
---

# How to cut a release by hand

This guide is for the maintainer. It shows how to publish an Encina release (tag, GitHub Release, version bump) by hand under the current branch protection, the way v0.13.0's version bumps and tag were done (steps 1, 2 and 5; v0.13.0 has no GitHub Release yet, so step 4 follows the earlier v0.10.0 to v0.12.0 pre-releases), until [#1100](https://github.com/dlrivada/Encina/issues/1100) automates it. It assumes you can use `git`, `gh` and PowerShell and have write access to the repository.

Every command runs from the repository root. Replace `0.14.0` with the version you are releasing, the date with today's date and the title with the milestone name without its `vX.Y.Z` prefix (for example `Hardening`).

## Before you start

Check each item; do not continue if one fails.

1. `main` is green. Look at the latest `CI` run on `main`:

   ```powershell
   gh run list --repo dlrivada/Encina --branch main --workflow ci.yml --limit 3
   ```

   Every row shows `completed` and `success`.

2. The milestone has no open issue that blocks the release. Issues you decide to leave out are moved to another milestone first:

   ```powershell
   gh issue list --repo dlrivada/Encina --milestone "v0.14.0 — Hardening" --state open
   ```

   The list is empty or contains only issues you have decided to move.

3. No open pull request is marked as release-blocking:

   ```powershell
   gh pr list --repo dlrivada/Encina --state open
   ```

4. Your checkout is clean and up to date:

   ```powershell
   git switch main
   git pull --ff-only
   git status --short
   ```

   `git status --short` prints nothing.

## Step 1: prepare the release branch

The branch name must start with `release/`. The `ci.yml` step that runs `changelog-fragments.cs --check-unreleased-unchanged` is skipped only for such branches, and the next step edits the `[Unreleased]` section on purpose.

```powershell
git switch -c release/v0.14.0
```

### Fold the changelog fragments

Check that the fragments are valid, preview the result, then fold them into a dated section. The command deletes the fragment files it folds in.

```powershell
dotnet run .github/scripts/changelog-fragments.cs -- --check
dotnet run .github/scripts/changelog-fragments.cs -- --preview
dotnet run .github/scripts/changelog-fragments.cs -- --release 0.14.0 2026-10-01 "Hardening"
```

How to check it worked: `CHANGELOG.md` has a new `## [0.14.0] - 2026-10-01 - Hardening` heading under `## [Unreleased]`, which now holds only the pending-fragments note, and `changelog.d/` contains only `README.md`. The script refuses an empty release, and a version that already has a section in `CHANGELOG.md`.

```powershell
Select-String -Path CHANGELOG.md -Pattern '^## \[' | Select-Object -First 3
Get-ChildItem changelog.d
```

The reference for the fragment format and for the script's other modes is [`changelog.d/README.md`](../../changelog.d/README.md).

### Set the release version

In [`Directory.Build.props`](../../Directory.Build.props), `VersionPrefix` already holds the version of the release (the previous bump set it, together with `VersionSuffix` `dev`). Clear `VersionSuffix` so the tagged build packs `0.14.0` and not `0.14.0-dev`:

```xml
<VersionPrefix>0.14.0</VersionPrefix>
<VersionSuffix></VersionSuffix>
```

If `VersionPrefix` differs from the release you are cutting, fix it here as well. Edit the file with an editor, not with a search-and-replace script.

How to check it worked:

```powershell
dotnet msbuild src/Encina/Encina.csproj -getProperty:Version
```

The command prints `0.14.0`, with no `-dev` suffix.

### Write the release notes page

Create `docs/releases/v0.14.0/README.md`, modelled on [`docs/releases/v0.13.0/README.md`](v0.13.0/README.md): the title `# v0.14.0 - <Title>`, a quote block with the milestone link and the status, and the issue table. Write the status so it does not claim the release exists before the tag does. The v0.13.0 page uses this wording:

```markdown
> **Status**: Closed on 2026-09-22 by the merge of #1101; the `v0.13.0` tag is created on that merge commit and the release is published from it.
```

### Open the pull request and merge it

```powershell
git add CHANGELOG.md changelog.d Directory.Build.props docs/releases/v0.14.0
git commit -m "chore(release): bump version to 0.14.0"
git push -u origin release/v0.14.0
gh pr create --repo dlrivada/Encina --base main --head release/v0.14.0 --title "chore(release): bump version to 0.14.0" --body "Cuts v0.14.0. Refs #1100."
gh pr merge --repo dlrivada/Encina --auto --squash
```

How to check it worked: the PR passes the same required checks as any other PR and merges; `gh pr view --repo dlrivada/Encina release/v0.14.0 --json state,mergeCommit` shows `MERGED` and the merge commit. A CodeRabbit thread must be resolved like on any PR before it merges.

## Step 2: tag the merge commit

Tag the commit that the release PR created on `main`, not a branch tip. Branch protection covers branches, so the tag push is not blocked.

```powershell
git switch main
git pull --ff-only
git log --oneline -1
git tag -a v0.14.0 -m "Release v0.14.0 - Hardening"
git push origin v0.14.0
```

`git log --oneline -1` must show the release PR's merge commit (`chore(release): bump version to 0.14.0 (#NNNN)`); do not tag until it does.

How to check it worked:

```powershell
git ls-remote --tags origin v0.14.0
git tag -n1 v0.14.0
```

The first command prints the tag's object id and `refs/tags/v0.14.0`; the second prints the tag message.

## Step 3: what runs on the tag

Two workflows start on any tag matching `v*`:

- [`ci-full.yml`](../../.github/workflows/ci-full.yml) (`CI Full`, trigger `push: tags: ["v*"]`) builds and runs the full test suite. Its `pack` job packs `src/Encina/Encina.csproj` and, because the ref is a `v*` tag, pushes the package to GitHub Packages with `dotnet nuget push`. `pack` runs even when a test job fails, so check every job, not only the push.
- [`sbom.yml`](../../.github/workflows/sbom.yml) (`SBOM`, trigger `push: tags: ["v*"]`) generates the software bill of materials.

How to check it worked:

```powershell
gh run list --repo dlrivada/Encina --event push --branch v0.14.0 --limit 5
```

Both `CI Full` and `SBOM` appear; wait until each shows `completed`. If `pack` fails, open the run with `gh run view <run-id> --repo dlrivada/Encina --log-failed`. Nothing in these workflows publishes to NuGet.org or creates the GitHub Release.

## Step 4: create the GitHub Release from the changelog

Extract the release's section from `CHANGELOG.md` into a file under `artifacts/` and pass it to `gh release create`. Releases before 1.0 are published as pre-releases, as the earlier v0.10.0 to v0.12.0 releases are.

```powershell
New-Item -ItemType Directory -Force artifacts/release | Out-Null
$lines = Get-Content CHANGELOG.md
$start = ($lines | Select-String -Pattern '^## \[0\.14\.0\] - ').LineNumber
$next = ($lines | Select-String -Pattern '^## \[' | Where-Object { $_.LineNumber -gt $start } | Select-Object -First 1).LineNumber
$end = if ($next) { $next - 2 } else { $lines.Count - 1 }
$lines[$start..$end] | Set-Content artifacts/release/v0.14.0-notes.md
gh release create v0.14.0 --repo dlrivada/Encina --verify-tag --prerelease --title "v0.14.0 - Hardening" --notes-file artifacts/release/v0.14.0-notes.md
```

`$lines` is zero-based while `LineNumber` is one-based, so `$lines[$start]` is the first line after the heading. The file contains the section's body without its `## [0.14.0]` heading.

How to check it worked:

```powershell
gh release view v0.14.0 --repo dlrivada/Encina --json tagName,isPrerelease,name
```

The output shows `tagName` `v0.14.0` and `isPrerelease` `true`. Open the release page once and confirm the notes read as the CHANGELOG section.

## Step 5: bump to the next development version

Open a second pull request that sets the next version, as [#1106](https://github.com/dlrivada/Encina/pull/1106) did after v0.13.0. In `Directory.Build.props`:

```xml
<VersionPrefix>0.15.0</VersionPrefix>
<VersionSuffix>dev</VersionSuffix>
```

```powershell
git switch main
git pull --ff-only
git switch -c chore/version-0.15.0-dev
git add Directory.Build.props
git commit -m "chore: bump version to 0.15.0-dev"
git push -u origin chore/version-0.15.0-dev
gh pr create --repo dlrivada/Encina --base main --head chore/version-0.15.0-dev --title "chore: bump version to 0.15.0-dev" --body "Next development version after v0.14.0. Refs #1100."
gh pr merge --repo dlrivada/Encina --auto --squash
```

How to check it worked: after the merge, `Select-String -Path Directory.Build.props -Pattern 'Version(Prefix|Suffix)'` on an updated `main` shows `0.15.0` and `dev`.

## Why this is manual

[`release-on-milestone.yml`](../../.github/workflows/release-on-milestone.yml) pushes straight to the protected `main`, which it can no longer do, and a pull request opened with its `GITHUB_TOKEN` would not trigger CI. The maintainer decided on 2026-10-02 ([#1100](https://github.com/dlrivada/Encina/issues/1100)) to keep releases manual until 1.0. The workflow is harmless meanwhile: its job runs only for milestone titles ending in `.0` (`if: endsWith(github.event.milestone.title, '.0')`), and the current milestones are named like `v0.14.0 — Hardening`.

The manual process is temporary: [SPEC-000](../specifications/SPEC-000-encina-1.0-baseline-and-release-scope.md) REQ-018 and REQ-019 require the 1.0 packages to come from CI, so #1100 stays open.

## Related

- [`changelog.d/README.md`](../../changelog.d/README.md): reference for fragments and `changelog-fragments.cs`.
- [`docs/releases/v0.13.0/README.md`](v0.13.0/README.md): the release notes page used as the model.
- [Contributing guide](../contributing/README.md): how a change travels to `main`.
