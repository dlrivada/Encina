---
title: "How to cut a release by hand"
layout: default
nav_exclude: true
---

# How to cut a release by hand

This guide is for the maintainer. It shows how to publish an Encina release (tag, GitHub Release, version bump) by hand under the current branch protection, until [#1100](https://github.com/dlrivada/Encina/issues/1100) automates it. It assumes you can use `git`, `gh` and PowerShell and have write access to the repository.

Every command runs from the repository root. Replace `0.14.0` with the version you are releasing, the date with today's date and the title with the milestone name without its `vX.Y.Z` prefix (for example `Hardening`).

## Release readiness gate

Every release of the 1.0 sequence passes through the release readiness gate, a tool that checks the SPEC-000 and SPEC-002 acceptance criteria that CI can answer and lists the ones only you can answer. The reason for its design is [ADR-038](../architecture/adr/038-release-readiness-gate.md). This section is the reference you need while following the steps below; the day-to-day commands (adding an item, answering a manual item) are in [How to run the release readiness gate](release-readiness.md).

The tool derives the release profile from the name of the release branch, `release/v<version>`:

| Branch name | Profile | Meaning |
| --- | --- | --- |
| `release/v0.14.0` (any `X.Y.0` without a prerelease, except `1.0.0`) | `minor` | each minor tag of the 1.0 sequence |
| `release/v1.0.0-rc.1` (a `-rc.N` prerelease) | `rc` | a release candidate |
| `release/v1.0.0` | `final` | the 1.0 release |

Any other name is an error and the tool exits with code 2: a branch that does not match `release/v<version>`, a version that is not valid SemVer, build metadata, a prerelease other than `rc.N` and a patch version such as `release/v0.14.1`. The tool never falls back to the lightest profile. Check a name before you create the branch:

```powershell
dotnet run --file .github/scripts/release-readiness.cs -- --from-branch release/v0.14.0
```

The output is `version=0.14.0` and `profile=minor`.

Each item of the registry has a severity per profile: `block` fails the gate, `warn` is reported without failing it, `skip` is not checked. A blocking item fails when its fact is missing, could not be read or is older than 24 hours, and a manual item that nobody answered is `PENDING`, which also fails the gate. The gate runs on pull requests whose head branch starts with `release/`, as the `release-readiness` job that feeds `ci-result`, so the required checks of the branch protection stay `build`, `ci-result` and `Analyze` ([SPEC-000](../specifications/SPEC-000-encina-1.0-baseline-and-release-scope.md) REQ-020).

### Set up the GitHub App for alert facts

The `GITHUB_TOKEN` of a workflow cannot read open secret-scanning alerts or open Dependabot alerts. The gate reads them with a GitHub App token. You create the App once; the gate never assumes it exists.

1. Create a GitHub App owned by the repository owner. Give it only two repository permissions, both read-only: "Secret scanning alerts" and "Dependabot alerts".
2. Install it on `dlrivada/Encina` only, not on all repositories.
3. Add the repository variable `RELEASE_GATE_APP_ID` (the App ID) and the repository secret `RELEASE_GATE_APP_PRIVATE_KEY` (the App's private key). The workflow mints a short-lived token from them with `actions/create-github-app-token` and passes it to the tool as the environment variable `RELEASE_GATE_APP_TOKEN`.

The tool uses the token only for the two alert collectors. When the token is missing, or the API call fails, the fact is recorded with an error and the items `secret-scanning-no-alerts` and `dependabot-no-alerts` fail: the gate fails closed.

The only fallback is manual and visible. The items `attest-secret-scanning` and `attest-dependabot` are accepted only when the matching automatic fact has an error, and only when their line in `docs/releases/v<version>/checklist.md` is ticked with an evidence link. The evaluation output then says that the attestation replaced the fact. When the fact was collected normally, the attestation is not needed and is ignored. Alert data is never typed into `facts.json` by hand.

To run the gate locally with the alert facts, set the variable in your shell for the session (the value comes from a token you mint for the App; do not write it into a file in the repository):

```powershell
$env:RELEASE_GATE_APP_TOKEN = '<installation token>'
```

## Before you start

Check each item; do not continue if one fails. Items 1 to 4 are the manual checks; item 5 runs the gate in dry-run mode.

1. `main` is green. Look at the latest `CI` run on `main`:

   ```powershell
   gh run list --repo dlrivada/Encina --branch main --workflow ci.yml --limit 3
   ```

   Every row shows `completed` and `success`. A green `ci.yml` run can still have skipped its test jobs, for example when the change touched only documentation, so it does not prove the tests passed. The section "Step 2: tag the merge commit" requires a green `CI Full` run on the commit you tag.

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

5. The registry is valid, and you know how far the repository is from a green gate. A dry run writes its facts under `artifacts/`, not into the release folder:

   ```powershell
   dotnet run --file .github/scripts/release-readiness.cs -- --check-registry
   dotnet run --file .github/scripts/release-readiness.cs -- --collect --from-branch release/v0.14.0 --out artifacts/release/facts-dry-run.json
   dotnet run --file .github/scripts/release-readiness.cs -- --evaluate --facts artifacts/release/facts-dry-run.json --from-branch release/v0.14.0
   ```

   The first command prints `Registry OK: ... (N items, M retired)`. The last one prints one line per item (`PASS`, `FAIL`, `WARN`, `PENDING` or `SKIP`) and a `SUMMARY` line ending in `result=PASS` or `result=FAIL`. Items that read the files Step 1 creates, such as `release-notes-page` and `version-no-suffix`, fail until Step 1 is done.

## Step 1: prepare the release branch

The branch name must start with `release/`. The `ci.yml` step that runs `changelog-fragments.cs --check-unreleased-unchanged` is skipped only for such branches, and the next step edits the `[Unreleased]` section on purpose.

```powershell
git switch -c release/v0.14.0
```

### Fold the changelog fragments

Check that the fragments are valid, preview the result, then fold them into a dated section. The command deletes the fragment files it folds in. This is the intended path, but it was not used for v0.13.0: PR #1101 renamed the `[Unreleased]` heading by hand. Treat the first release made this way as its dry run, and read the `--preview` output before running `--release`.

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

### Render the checklist, collect the facts and write the evidence

With the changelog folded, the version set and the notes page written, render the sign-off file for the profile of the branch, collect the facts for the release branch and generate the evidence report from them. All three go into the release folder and are committed with the pull request ([SPEC-000](../specifications/SPEC-000-encina-1.0-baseline-and-release-scope.md) AC-022):

```powershell
dotnet run --file .github/scripts/release-readiness.cs -- --render-checklist --profile minor --version 0.14.0 --out docs/releases/v0.14.0/checklist.md
dotnet run --file .github/scripts/release-readiness.cs -- --collect --from-branch release/v0.14.0 --out docs/releases/v0.14.0/facts.json
dotnet run --file .github/scripts/release-readiness.cs -- --evidence --facts docs/releases/v0.14.0/facts.json --signoff docs/releases/v0.14.0/checklist.md --out docs/releases/v0.14.0/evidence.md
```

Use the profile of your branch in the first command (`minor`, `rc` or `final`). Open `checklist.md`, tick each manual item you can answer and add `evidence: <link>` to its line, then regenerate `evidence.md` with the third command. [How to run the release readiness gate](release-readiness.md#answer-a-manual-item) shows the line format. Check the verdict:

```powershell
dotnet run --file .github/scripts/release-readiness.cs -- --evaluate --facts docs/releases/v0.14.0/facts.json --from-branch release/v0.14.0 --signoff docs/releases/v0.14.0/checklist.md
```

How to check it worked: the `SUMMARY` line ends in `result=PASS`, and the exit code is 0. The evaluation rejects facts older than 24 hours, so run it soon after the collection.

### Open the pull request and merge it

```powershell
git add CHANGELOG.md changelog.d Directory.Build.props docs/releases/v0.14.0
git commit -m "chore(release): bump version to 0.14.0"
git push -u origin release/v0.14.0
gh pr create --repo dlrivada/Encina --base main --head release/v0.14.0 --title "chore(release): bump version to 0.14.0" --body "Cuts v0.14.0. Refs #1100."
gh pr merge release/v0.14.0 --repo dlrivada/Encina --auto --squash
```

How to check it worked: the PR passes the same required checks as any other PR and merges; `gh pr view --repo dlrivada/Encina release/v0.14.0 --json state,mergeCommit` shows `MERGED` and the merge commit. A CodeRabbit thread must be resolved like on any PR before it merges.

## Step 2: tag the merge commit

Tag the commit that the release PR created on `main`, not a branch tip. Branch protection covers branches, so the tag push is not blocked.

First require a green `CI Full` run on that exact commit. `ci.yml` may skip test jobs, while `CI Full` runs all of them, so a completed, successful run on the commit to tag, started by `schedule` or `workflow_dispatch`, with every job `success`, is the evidence that the tests pass. The gate item `ci-full-on-sha` checks exactly that. Collect the facts for the merge commit and evaluate only that item:

```powershell
git switch main
git pull --ff-only
$sha = git rev-parse HEAD
dotnet run --file .github/scripts/release-readiness.cs -- --collect --from-branch release/v0.14.0 --sha $sha --out artifacts/release/facts-tag.json
dotnet run --file .github/scripts/release-readiness.cs -- --evaluate --facts artifacts/release/facts-tag.json --from-branch release/v0.14.0 --only ci-full-on-sha
```

The evaluation prints `PASS ci-full-on-sha` with the run id and the number of jobs, and the exit code is 0. A `FAIL ci-full-on-sha` line names the problem: no completed run on that commit, or the jobs that did not end in `success`. If no run exists for the commit, start one on `main`, wait for it, and run the last two commands again:

```powershell
gh workflow run ci-full.yml --repo dlrivada/Encina --ref main
gh run list --repo dlrivada/Encina --workflow ci-full.yml --limit 3 --json databaseId,headSha,event,status
gh run watch <run-id> --repo dlrivada/Encina
```

Before you use a run id, check that its `headSha` equals `$sha` and that its `status` is the run you just started (`queued` or `in_progress`), not an older one.

Do not tag until the gate passes. Then tag:

```powershell
git log --oneline -1
git tag -a v0.14.0 -m "Release v0.14.0 — Hardening"
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

- [`ci-full.yml`](../../.github/workflows/ci-full.yml) (`CI Full`, trigger `push: tags: ["v*"]`) builds and runs the full test suite. Its `pack` job packs `src/Encina/Encina.csproj` and, because the ref is a `v*` tag, pushes the package to GitHub Packages with `dotnet nuget push`. The `pack` job runs only when `build` and all six `test-*` jobs concluded `success` (#1745), so a failed, skipped or cancelled test job blocks the publish. The gate exists because CI Full run 35652357800 (`workflow_dispatch`, 2026-09-21) ran `pack` to success while 18 test jobs had failed; it was not a tag push, so nothing was published.
- [`sbom.yml`](../../.github/workflows/sbom.yml) (`SBOM`, trigger `push: tags: ["v*"]`) generates the software bill of materials.

How to check it worked:

```powershell
gh run list --repo dlrivada/Encina --event push --branch v0.14.0 --limit 5
```

Both `CI Full` and `SBOM` appear; wait until each shows `completed`. Then confirm the package was pushed, using the run id from the list above:

```powershell
gh run view <run-id> --repo dlrivada/Encina --json jobs --jq '.jobs[] | select(.name=="pack") | .steps[] | select(.name=="Publish to GitHub Packages") | .conclusion'
```

The output is `success`. Empty output means `pack` was blocked by a red or skipped job. If a test shard flakes on the tag-triggered `CI Full`, `pack` is skipped and nothing publishes; rerun the failed jobs with `gh run rerun <run-id> --repo dlrivada/Encina --failed`. For v0.13.0 it was `failure` (the push got a 403 before the job-level `packages: write` permission was added), and nothing else in the run showed it. If it fails, open the run with `gh run view <run-id> --repo dlrivada/Encina --log-failed`. Nothing in these workflows publishes to NuGet.org or creates the GitHub Release.

## Step 4: create the GitHub Release from the changelog

Extract the release's section from `CHANGELOG.md` into a file under `artifacts/` and pass it to `gh release create`. Releases before 1.0 are published as pre-releases, as the earlier v0.10.0 to v0.12.0 releases are.

```powershell
New-Item -ItemType Directory -Force artifacts/release | Out-Null
$lines = Get-Content CHANGELOG.md
$start = ($lines | Select-String -Pattern '^## \[0\.14\.0\] - ').LineNumber
$next = ($lines | Select-String -Pattern '^## \[' | Where-Object { $_.LineNumber -gt $start } | Select-Object -First 1).LineNumber
$end = if ($next) { $next - 2 } else { $lines.Count - 1 }
$body = ($lines[$start..$end] -join "`n").Trim()
$body = ($body -replace '(\r?\n)+---\s*$', '').Trim()
$body.Length
```

The last line prints the body's length in characters. GitHub limits a release body to about 125,000 characters (the REST API for releases rejects a longer one). If the number is below that, write the full section and create the release:

```powershell
Set-Content artifacts/release/v0.14.0-notes.md $body
gh release create v0.14.0 --repo dlrivada/Encina --verify-tag --prerelease --title "v0.14.0 — Hardening" --notes-file artifacts/release/v0.14.0-notes.md
```

If it is above the limit, use a short body that links to the tagged CHANGELOG section and to the release notes page. The anchor is the heading in lower case with the brackets and dots removed and spaces turned into hyphens (`## [0.14.0] - 2026-10-01 - Hardening` becomes `#0140---2026-10-01---hardening`):

```powershell
$short = @'
Release notes: [docs/releases/v0.14.0/README.md](https://github.com/dlrivada/Encina/blob/v0.14.0/docs/releases/v0.14.0/README.md)

Full changelog: [CHANGELOG.md](https://github.com/dlrivada/Encina/blob/v0.14.0/CHANGELOG.md#0140---2026-10-01---hardening)
'@
Set-Content artifacts/release/v0.14.0-notes.md $short
gh release create v0.14.0 --repo dlrivada/Encina --verify-tag --prerelease --title "v0.14.0 — Hardening" --notes-file artifacts/release/v0.14.0-notes.md
```

The v0.13.0 section is about 186,000 characters, so a release for it needs the short body.

`$lines` is zero-based while `LineNumber` is one-based, so `$lines[$start]` is the first line after the heading. The file contains the section's body without its `## [0.14.0]` heading, trimmed of blank lines and of a trailing `---` separator. The snippet assumes the section was produced by `changelog-fragments.cs --release` (a blank line before the next `## [` heading); check the extracted text before creating the release. Release titles use an em dash, like the existing releases (`v0.12.0 — Database & Repository`).

How to check it worked:

```powershell
gh release view v0.14.0 --repo dlrivada/Encina --json tagName,isPrerelease,name
```

The output shows `tagName` `v0.14.0` and `isPrerelease` `true`. Open the release page once and confirm the notes read as the CHANGELOG section, or that both links of the short body open. GitHub may not render `CHANGELOG.md` or scroll to its anchor at this size (about 570 KiB), so the release notes page is the primary link.

If `gh release create` fails after the release was created, or you run the step a second time, do not create it again. Update the existing release instead: `gh release edit v0.14.0 --repo dlrivada/Encina --notes-file artifacts/release/v0.14.0-notes.md`.

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
gh pr merge chore/version-0.15.0-dev --repo dlrivada/Encina --auto --squash
```

How to check it worked: after the merge, `Select-String -Path Directory.Build.props -Pattern 'Version(Prefix|Suffix)'` on an updated `main` shows `0.15.0` and `dev`.

## Step 6: close the milestone

Find the milestone number, then close it:

```powershell
gh api --paginate repos/dlrivada/Encina/milestones --jq '.[] | [.number, .title] | @tsv'
gh api -X PATCH repos/dlrivada/Encina/milestones/<number> -f state=closed
```

How to check it worked: the milestone no longer appears in the first command's output, which lists open milestones only.

Keep the descriptive suffix in milestone titles (`v0.14.0 — Hardening`, as today). A milestone titled exactly `vX.Y.0` would start `release-on-milestone.yml`, which pushes to `main` and fails under the branch protection. This holds until #1100 replaces the workflow.

## Why this is manual

[`release-on-milestone.yml`](../../.github/workflows/release-on-milestone.yml) pushes straight to the protected `main`, which it can no longer do, and a pull request opened with its `GITHUB_TOKEN` would not trigger CI. The maintainer decided on 2026-10-02 ([#1100](https://github.com/dlrivada/Encina/issues/1100)) to keep releases manual until 1.0. The workflow is harmless meanwhile: its job runs only for milestone titles ending in `.0` (`if: endsWith(github.event.milestone.title, '.0')`), and the current milestones are named like `v0.14.0 — Hardening`.

The manual process is temporary: [SPEC-000](../specifications/SPEC-000-encina-1.0-baseline-and-release-scope.md) REQ-018 and REQ-019 require the 1.0 packages to come from CI, so #1100 stays open.

## Related

- [`changelog.d/README.md`](../../changelog.d/README.md): reference for fragments and `changelog-fragments.cs`.
- [`docs/releases/v0.13.0/README.md`](v0.13.0/README.md): the release notes page used as the model.
- [Contributing guide](../contributing/README.md): how a change travels to `main`.
- [How to run the release readiness gate](release-readiness.md): adding and retiring items, answering manual items, running the gate locally.
- [ADR-038](../architecture/adr/038-release-readiness-gate.md): why the gate is built this way.
