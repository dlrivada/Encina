<!--
title: [DEBT] RELEASE-PROCESS.md says the pack-on-red-tests condition was never exercised, but CI Full run 35652357800 exercised it
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

`docs/releases/RELEASE-PROCESS.md:142` says of the `pack` job of CI Full: "by its condition it is not skipped when a test job fails; that was never exercised". It was exercised. CI Full run 35652357800 (`workflow_dispatch`, 2026-09-21) has 18 failed test jobs (1 `test-property`, 1 `test-guard`, 8 `test-unit` shards, 4 `test-integration` shards, 4 `test-ef-providers` shards) while `build`, `test-contract`, `pack` and `coverage` concluded `success`. That run was not a tag push, so the publish step was skipped. The v0.13.0 tag push (run 35736044176, event `push`) also ran `pack`, whose conclusion was `failure`: the 403 the page mentions at `:157`.

The condition `always() && needs.build.result == 'success'` (`.github/workflows/ci-full.yml:541`) is therefore proven to let `pack` run on red tests. The sentence understates the risk.

## Location

- **File(s)**: `docs/releases/RELEASE-PROCESS.md:142`
- **Package(s)**: none (documentation only)

## Current Behavior

The page presents the behaviour as a theoretical reading of the condition ("by its condition ... that was never exercised"), which lets a releaser treat a red test suite on the tag run as unlikely to reach the publish step.

## Expected Behavior

The sentence states that the behaviour has been observed and cites the run: CI Full run 35652357800 ran `pack` to `success` while 18 test jobs had failed, so a `v*` tag push with a red suite reaches the "Publish to GitHub Packages" step.

## Root Cause

The sentence rests on the workflow condition alone; the run history holds a run that exercises it.

## Proposed Fix

Replace "that was never exercised" with the run reference above (run id, date, the number of failed test jobs and the `pack` conclusion), keeping the instruction to check every job of the run. When `ci-full.yml` is gated on the test results, rewrite the sentence to describe the gated behaviour.

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
