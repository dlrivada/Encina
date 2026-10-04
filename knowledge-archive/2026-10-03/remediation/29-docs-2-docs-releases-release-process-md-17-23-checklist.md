<!--
title: [DEBT] RELEASE-PROCESS.md does not require a green CI Full run before the tag, so a release can publish with failing tests
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

The release how-to `docs/releases/RELEASE-PROCESS.md` does not stop a release with failing tests.

- Pre-release checklist item 1, "`main` is green" (`:17-23`), checks only `--workflow ci.yml`. `ci.yml` runs on push to `main` and on `workflow_dispatch` (`ci.yml:3-8`) with the same test projects and shards as `ci-full.yml`, but each test job is gated by a path filter (`ci.yml:124`, `:185`, `:259`: `needs.changes.outputs.<filter> == 'true' || github.event_name == 'workflow_dispatch'`; `workflow_dispatch` is the only bypass). A green `ci.yml` run on `main` can therefore mean the test jobs were skipped (#1721).
- Step 2 (`:115-127`), the tagging step, never asks for a successful `ci-full.yml` run on the commit to tag.
- Step 3 (`:138-157`) verifies the package only after the tag push (`:151-155`), when the `pack` job has already published. `pack` runs under `always() && needs.build.result == 'success'` (`.github/workflows/ci-full.yml:541`), which `RELEASE-PROCESS.md:142` itself quotes, so a red test suite on the tag run still publishes. The page's only mitigation is the clause "check every job of the run, not only the push" (`:142`), performed after the publish.

## Location

- **File(s)**: `docs/releases/RELEASE-PROCESS.md` (`:17-23`, `:115-127`, `:151-157`)
- **Package(s)**: none (documentation only)

## Current Behavior

Following the page to the letter, a releaser tags a commit without any check that the full test suite passed on it, and learns that tests were red only after the package was pushed to GitHub Packages.

## Expected Behavior

The pre-tag checks include a requirement that the latest `ci-full.yml` run on the commit to tag is `success` for every `test-*` job, with the `gh run list --workflow ci-full.yml` command that shows it, so the releaser stops before the tag exists.

## Root Cause

The checklist looks only at `ci.yml`. The tag-triggered publish lives in `ci-full.yml`, whose `pack` job does not depend on the results of the test jobs, and the page documents that behaviour only as a post-publish check.

## Proposed Fix

Add a pre-tag item (in "Before you start" or as the first action of Step 2) that requires the latest `ci-full.yml` run on the commit to tag to be `success` for every `test-*` job, with an example command such as `gh run list --repo dlrivada/Encina --workflow ci-full.yml --limit 3`. Keep that item until `ci-full.yml` gates the publish on the test results, then reduce it to a pointer to the workflow. Note in item 1 that a green `ci.yml` run may have skipped its test jobs.

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
- #1721 - path filters can skip the test jobs of `ci.yml`
