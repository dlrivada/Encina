<!--
title: [DEBT] RELEASE-PROCESS.md never asks for a green CI Full run on the commit to tag, and the package is verified only after it is published
labels: technical-debt
milestone: 
kind: docs
-->

## Type

- [ ] Failing tests
- [ ] Missing tests
- [ ] Code quality (warnings, analyzers)
- [ ] Performance optimization
- [ ] Refactoring needed
- [x] Documentation gap
- [ ] Incorrect implementation
- [ ] Other

## Description

The release how-to `docs/releases/RELEASE-PROCESS.md` does not stop a release that has failing tests.

Checklist item 1, "`main` is green" (`:17-23`), looks only at the latest `ci.yml` runs (`gh run list --repo dlrivada/Encina --branch main --workflow ci.yml --limit 3`, `:20`). `ci.yml` runs on push to `main`, on pull requests and on `workflow_dispatch` (`ci.yml:3-8`), with the same six test projects as `ci-full.yml` (`test-unit`, `test-integration`, `test-contract`, `test-property`, `test-guard`, `test-ef-providers`). But each test job is gated by a path filter (`ci.yml:124`, `:185`, `:259` and the same shape on the other test jobs; `|| github.event_name == 'workflow_dispatch'` is the only bypass), so a green `ci.yml` run on `main` can mean the test jobs were skipped (#1721). The page never asks for a successful `ci-full.yml` run on the commit to tag.

Step 2 (`:115-127`) tags the merge commit and pushes the tag without that check. Step 3 (`:138-157`) then tells the reader to confirm the package was pushed (`:151-155`, "Then confirm the package was pushed"), which happens after the tag run has finished and `pack` has already published. The `pack` job of `.github/workflows/ci-full.yml` runs under `if: always() && needs.build.result == 'success'` (`:541`), so on a tag push a red test job does not stop the publish step (`:569-578`). The page states this condition itself at `:142`; its only mitigation is the trailing clause "check every job of the run, not only the push", a check made after the publish.

## Location

- **File(s)**: `docs/releases/RELEASE-PROCESS.md:17-23` (checklist item 1), `:115-127` (Step 2), `:138-157` (Step 3); the workflow condition at `.github/workflows/ci-full.yml:532-541`
- **Package(s)**: None (documentation only)

## Current Behavior

A maintainer who follows the page exactly checks `ci.yml` on `main`, which may be green because its test jobs were skipped, tags, and learns whether the full suite passed only from the tag run, by which time `pack` has published the package regardless of the test results.

## Expected Behavior

Before `git tag` (Step 2), the page requires that the latest `ci-full.yml` run on the commit to tag concluded `success` for every `test-*` job, and gives the command to check it. The reader never publishes first and verifies afterwards.

## Root Cause

The page's pre-conditions cover only `ci.yml`, whose test jobs run only when path filters select them, and the `pack` job is not gated on the test jobs, so nothing in front of the tag requires a green full suite. No date is verified for when either was written.

## Proposed Fix

Add a pre-tag item to "Before you start" (or to Step 2, before `git tag`): "The latest `ci-full.yml` run on the commit to tag is `success` for every `test-*` job", with the commands that show it, for example:

```powershell
gh run list --repo dlrivada/Encina --workflow ci-full.yml --limit 3
gh run view <run-id> --repo dlrivada/Encina --json jobs --jq '.jobs[] | select(.name | startswith("test-")) | {name, conclusion}'
```

When no such run exists for the commit, the item says to start CI Full by hand first (`workflow_dispatch` is a trigger of the workflow, `ci-full.yml:8`). Keep the item until `pack` itself is gated on the test jobs, then reduce it to a pointer to that gate.

## Priority

- [ ] **High** - Blocks functionality or causes failures in production
- [x] **Medium** - Should be fixed before 1.0 release
- [ ] **Low** - Nice to have, can be deferred

## Effort Estimate

- [x] Small (< 1 hour)
- [ ] Medium (1-4 hours)
- [ ] Large (> 4 hours)

## Related Issues

- #29 (This issue)
- #1721 - `ci.yml` path filters decide whether the test jobs run
