<!--
title: [BUG] CI Full pack job runs under always() and publishes on a tag push even when test jobs, contract tests included, failed
labels: bug
milestone: v0.14.0 — Hardening
kind: bug
-->

## Description

The `pack` job of `.github/workflows/ci-full.yml` is not gated on the result of the test jobs. It declares them in `needs:` (`:532-538`: `test-unit`, `test-integration`, `test-contract`, `test-property`, `test-guard`, `test-ef-providers`) but its condition is `if: always() && needs.build.result == 'success'` (`:541`). `always()` runs the job whatever the result of the jobs it needs, and the only result the condition inspects is the one of `build`. The test jobs therefore only order the jobs; nothing checks whether they passed.

The last step of `pack`, "Publish to GitHub Packages" (`:569-578`), runs `dotnet nuget push` and has the condition `startsWith(github.ref, 'refs/tags/v')` (`:570`). The workflow is triggered by `push: tags: ["v*"]` (`:6-7`), so a `v*` tag push with a red test suite, the contract suite included, still publishes the package.

The run history shows the condition lets `pack` run on red tests: CI Full run 35652357800 (`workflow_dispatch`, 2026-09-21) had 18 failed test jobs (1 `test-property`, 1 `test-guard`, 8 `test-unit` shards, 4 `test-integration` shards, 4 `test-ef-providers` shards), and `pack` concluded `success` (`build`, `test-contract` and `coverage` also succeeded). That run was not a tag push, so its publish step was skipped.

This breaks the AGENTS.md section 8 statement "CI enforces: all tests pass" for the release path, and it is the opposite of the fail-closed single-gate design that `ci.yml` follows: `ci-result` (`ci.yml:639-651`) fails when any needed job is `failure` or `cancelled` (`ci.yml:650`), while CI Full has no aggregate job.

## Steps to Reproduce

1. Make any job named in `pack`'s `needs:` fail, for example a failing test in `Encina.ContractTests` (`test-contract`, `ci-full.yml:198-215`).
2. Push a tag that matches `v*`, so `ci-full.yml` starts (`:6-7`). `build` succeeds.
3. The test job fails; `pack` still starts because of `always()` and `needs.build.result == 'success'` (`:541`).
4. The "Publish to GitHub Packages" step runs because the ref starts with `refs/tags/v` (`:570`) and pushes `artifacts/*.nupkg` to `https://nuget.pkg.github.com/<owner>/index.json` (`:572-578`).

The `workflow_dispatch` run 35652357800 reproduces steps 1 to 3 without a tag (step 4 is skipped there).

## Expected Behavior

`pack`, and above all its publish step, runs only when `build` and every test job it needs concluded `success`. A failing test job, `test-contract` included, stops the release package from being pushed.

## Actual Behavior

`pack` runs after any test job fails, and on a `v*` tag it pushes the package. Only a failure of `build` stops it.

## Root Cause

`always()` in the job condition discards the results of the jobs listed in `needs:`, and the only result the condition checks is `build`'s (`ci-full.yml:541`). The workflow also has no aggregate job that checks every needed result, unlike `ci.yml`'s `ci-result`.

## Environment

- **Encina Version**: 0.14.0-dev
- **.NET Version**: .NET 10
- **OS**: Not applicable (found by static review of the code, not at runtime)
- **Package(s) Affected**: none (CI workflow `.github/workflows/ci-full.yml`; the package it packs and publishes is `src/Encina/Encina.csproj`, `:561`)

## Code Sample

```yaml
  pack:
    needs:
      - test-unit
      - test-integration
      - test-contract
      - test-property
      - test-guard
      - test-ef-providers
    runs-on: ubuntu-latest
    timeout-minutes: 10
    if: always() && needs.build.result == 'success'
```

```yaml
      - name: Publish to GitHub Packages
        if: startsWith(github.ref, 'refs/tags/v')
```

## Stack Trace

```
Not applicable: no exception is thrown. Run 35652357800: 18 test jobs failed (1 test-property, 1 test-guard, 8 test-unit shards, 4 test-integration shards, 4 test-ef-providers shards); pack concluded success.
```

## Additional Context

Fix direction: drop `always()` from the `pack` condition, or keep it and require `success` from every needed job, for example `needs.build.result == 'success' && !contains(needs.*.result, 'failure') && !contains(needs.*.result, 'cancelled')` (the same expression style as `ci.yml:503-504`), or add an aggregate result job and make `pack` depend on it. Whichever is chosen, the publish step must not run when any test job failed.

Verification: workflow wiring is not exercised by any test project, so the fix is verified by a CI Full `workflow_dispatch` run in which a test job is made to fail and `pack` is skipped, and by a run in which every job passes and `pack` succeeds.

Related Issues:

- #29 (This issue)
