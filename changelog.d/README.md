# Changelog fragments

PRs developed in parallel used to all append to `## [Unreleased]` in `CHANGELOG.md`, and almost every one conflicted on that file (and often on `PublicAPI.Unshipped.txt`, appended at the end too). This directory removes that conflict hot spot: instead of editing `CHANGELOG.md` directly, a change that is user-visible drops one small file here.

## File name

```
changelog.d/<issue>-<short-slug>.<section>.md
```

- `<issue>` — the GitHub issue number the change closes or references (no `#`, no leading zeros beyond the number itself).
- `<short-slug>` — a few lowercase words separated by hyphens, matching `[a-z0-9]+(-[a-z0-9]+)*`.
- `<section>` — one of the [Keep a Changelog](https://keepachangelog.com/) sections, lowercase:
  `added`, `changed`, `deprecated`, `removed`, `fixed`, `security`.

Example: `changelog.d/1125-define-eventids.fixed.md`.

A change that touches more than one section (e.g. both fixes something and removes something) gets one fragment file per section.

## Content

One or more markdown bullets, each starting with `- `, exactly what would have been written under that section of `## [Unreleased]` in `CHANGELOG.md`. Multiple related bullets can live in the same fragment file if they belong to the same issue and section.

```markdown
- **Short bold summary** (#1125). The rest of the sentence, in the same style as existing CHANGELOG entries.
```

## Tooling

`changelog-fragments.cs` ([`.github/scripts/changelog-fragments.cs`](../.github/scripts/changelog-fragments.cs)) is the only thing that reads or writes this directory:

```powershell
# Validate every fragment (also run by CI on every pull request)
dotnet run .github/scripts/changelog-fragments.cs -- --check

# Preview the merged "## [Unreleased]" section without changing anything
dotnet run .github/scripts/changelog-fragments.cs -- --preview

# Release: fold the merged Unreleased content into CHANGELOG.md as a dated section,
# then delete the fragment files that were folded in (maintainer only)
dotnet run .github/scripts/changelog-fragments.cs -- --release 0.14.0 2026-10-01 "Some Title"
```

This file (`README.md`) is ignored by the tooling — it is not a fragment.

## Rules

- One fragment file per change per section; do not hand-edit `## [Unreleased]` in `CHANGELOG.md` directly.
- Every non-blank line must start with `- ` (a markdown bullet); `--check` rejects anything else.
- The section in the file name must be one of the six Keep a Changelog sections; `--check` rejects anything else.
- Fragments are merged deterministically: by section (Added, Changed, Deprecated, Removed, Fixed, Security), then by issue number, then by file name.
