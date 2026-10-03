<!--
title: [DEBT] RELEASE-PROCESS.md says the pack-on-failed-tests case was never exercised, but a CI Full run shows it
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

`docs/releases/RELEASE-PROCESS.md:142`, in Step 3, says of the `pack` job of `ci-full.yml`: "by its condition it is not skipped when a test job fails; that was never exercised, so check every job of the run, not only the push."

It was exercised. CI Full run 35652357800 (`workflow_dispatch`, 2026-09-21) has 18 failed test jobs (1 `test-property`, 1 `test-guard`, 8 `test-unit` shards, 4 `test-integration` shards, 4 `test-ef-providers` shards) while `build`, `test-contract`, `pack` and `coverage` concluded `success`. That run was not a tag push, so its "Publish to GitHub Packages" step was skipped. The v0.13.0 tag push (run 35736044176, event `push`) also ran `pack`, which concluded `failure` with the 403 the page itself describes at `:157`. The condition (`if: always() && needs.build.result == 'success'`, `ci-full.yml:541`) is therefore proven to let `pack` run on red tests.

The sentence understates the risk and points the reader at a hypothetical, when the run history already holds the evidence.

## Location

- **File(s)**: `docs/releases/RELEASE-PROCESS.md:142`; the condition at `.github/workflows/ci-full.yml:532-541`
- **Package(s)**: None (documentation only)

## Current Behavior

The page tells the reader the failing-tests case "was never exercised" and leaves them to treat it as unproven.

## Expected Behavior

The page states that the case happened, cites CI Full run 35652357800 and says that a `v*` tag push in the same state publishes, so the reader treats a green `ci-full.yml` run as a pre-condition of the tag and not as an afterthought.

## Root Cause

Not established. No date is verified for when the sentence was written, so whether the run already existed at that time is unknown.

## Proposed Fix

Rewrite the second half of `RELEASE-PROCESS.md:142` so that it cites the run: "by its condition it is not skipped when a test job fails, and it was not: in CI Full run 35652357800 (`workflow_dispatch`, 2026-09-21) `pack` concluded `success` while 18 test jobs had failed. That run was not a tag push, so nothing was published; on a tag push the same result publishes the package. Check every job of the run, not only the push." When the workflow itself is gated on the test jobs, replace the sentence with a pointer to that gate.

## Priority

- [ ] **High** - Blocks functionality or causes failures in production
- [ ] **Medium** - Should be fixed before 1.0 release
- [x] **Low** - Nice to have, can be deferred

## Effort Estimate

- [x] Small (< 1 hour)
- [ ] Medium (1-4 hours)
- [ ] Large (> 4 hours)

## Related Issues

- #29 (This issue)
