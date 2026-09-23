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

One or more **entries**, exactly what would have been written under that section of `## [Unreleased]` in `CHANGELOG.md`. An entry is a top-level bullet (a line starting with `- ` at the very start of the line) plus everything that continues it: indented continuation paragraphs, indented nested bullets, and fenced code blocks (whether indented under the bullet or flush left). A blank line ends the entry unless the next line is more of that continuation. Multiple entries can live in the same fragment file if they belong to the same issue and section.

Simple case — one bullet, one line:

```markdown
- **Short bold summary** (#1125). The rest of the sentence, in the same style as existing CHANGELOG entries.
```

An entry with a continuation paragraph and a code block:

```markdown
- **Short bold summary** (#1125). The one-line version of the change.

  A continuation paragraph with more detail than fits on the bullet line, indented so it is
  still part of the same entry.

  ```csharp
  // Fenced code is part of the entry too, indented or not.
  services.AddEncinaFluentValidation(typeof(MyValidator).Assembly);
  ```

- **A second, unrelated entry** (#1125). Still the same fragment file, same section.
```

`--check` rejects anything that is not blank or part of a well-formed entry: text that is not indented and does not start with `- `, an empty bullet (`- ` with no text), or a fenced code block left open at the end of the file.

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

CI also runs `--check-unreleased-unchanged <base-ref>` on every pull request whose head branch does not start with `release/`: it fails the build if the PR's diff touches any line inside `## [Unreleased]` in `CHANGELOG.md`, which is the sign that someone hand-edited it instead of adding a fragment here.

## Rules

- One fragment file per change per section; do not hand-edit `## [Unreleased]` in `CHANGELOG.md` directly — CI rejects a pull request that does.
- Fragments live directly under `changelog.d/`, never in a subfolder.
- Every non-blank line must be part of a well-formed entry (see "Content" above); `--check` rejects anything else, including an empty bullet.
- The section in the file name must be one of the six Keep a Changelog sections, lower case, exact; `--check` rejects anything else, including a wrong-case extension (`.MD`) or a non-`.md` file.
- Fragments are merged deterministically: by section (Added, Changed, Deprecated, Removed, Fixed, Security), then by issue number, then by file name. `--preview` and `--release` never drop content silently: anything in `## [Unreleased]` that is not the pending note, a known section heading or a well-formed entry makes them exit with an error listing the offending lines instead of writing anything.
