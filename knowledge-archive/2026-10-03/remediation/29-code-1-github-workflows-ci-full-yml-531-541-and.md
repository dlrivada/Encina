<!--
title: [BUG] CI Full `pack` job runs and publishes the package when test jobs fail
labels: bug
milestone: v0.14.0 — Hardening
kind: bug
-->

## Description

In `.github/workflows/ci-full.yml` the `pack` job is declared with `if: always() && needs.build.result == 'success'` (`ci-full.yml:541`). The six test jobs are listed under `needs:` (`:532-538`: `test-unit`, `test-integration`, `test-contract`, `test-property`, `test-guard`, `test-ef-providers`) only to order the jobs. `always()` discards their results and nothing else checks them, so `pack` runs after any test job fails, `test-contract` included. On a `v*` tag push the step "Publish to GitHub Packages" (`:569-578`, `if: startsWith(github.ref, 'refs/tags/v')`) then runs `dotnet nuget push` for the package, so a release can be published from a commit whose test suite is red.

CI Full has no aggregate job like the `ci-result` gate that `ci.yml` uses, so no other job turns a red test job into a failed run that blocks publishing. `test-contract` has the same wiring as the other test jobs: it declares `needs: build` (`ci-full.yml:199`) and sits in `pack`'s `needs:` list (`:535`).

## Steps to Reproduce

1. Make one of the test jobs of `.github/workflows/ci-full.yml` fail (for example a failing test in `tests/Encina.ContractTests`, which `test-contract` runs at `ci-full.yml:210-215`).
2. Run CI Full, either with `workflow_dispatch` or by pushing a `v*` tag (the workflow triggers on both, `ci-full.yml:3-8`).
3. Look at the conclusion of the `pack` job.

CI Full run 35652357800 (`workflow_dispatch`, 2026-09-21) shows it: `test-property`, `test-guard`, `test-unit`, `test-integration` and four `test-ef-providers` shards failed, and `pack` concluded `success`. That run was not a tag push, so the publish step was skipped. On a `v*` tag push the same wiring reaches the publish step.

## Expected Behavior

The `pack` job, and in particular the "Publish to GitHub Packages" step, does not run unless every test job it depends on concluded `success`. A red test job, `test-contract` included, stops the publish. This is what AGENTS.md section 8 states ("CI enforces: all tests pass") and what the single-gate design of `ci.yml` (`ci-result`) already does.

## Actual Behavior

`pack` runs when any test job fails. With a `v*` tag the package is pushed to GitHub Packages with `dotnet nuget push artifacts/*.nupkg --source "$GITHUB_PACKAGES_SOURCE" --api-key "$NUGET_API_KEY" --skip-duplicate` (`ci-full.yml:575-578`).

## Environment

- **Encina Version**: 0.14.0-dev
- **.NET Version**: .NET 10
- **OS**: Not applicable (found by static review of the code, not at runtime)
- **Package(s) Affected**: none (CI workflow `.github/workflows/ci-full.yml`; the package published by the `pack` job is `src/Encina/Encina.csproj`)

## Root Cause

`always()` in the job-level `if:` makes the job run whatever the result of its `needs:`. The condition then checks only `needs.build.result`; no `needs.<test job>.result` is checked anywhere in the job. `build` is not in `pack`'s `needs:` list (`:532-538` lists only the six test jobs), so the condition refers to a job outside that list.

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

Not applicable: a workflow condition defect, no exception is thrown.

## Additional Context

Proposed fix: drop `always()`, or keep it and require every test job result in the condition, for example `needs.test-unit.result == 'success' && needs.test-integration.result == 'success' && ...` for all six jobs, so the "Publish to GitHub Packages" step cannot run after a red test job. When changing the condition, also decide how `build` is referenced, since it is not in the `needs:` list. Acceptance: a CI Full run in which one test job is forced to fail ends with `pack` skipped (or failed) and no publish step run; a run with all test jobs green still packs and, on a `v*` tag, publishes.

`docs/releases/RELEASE-PROCESS.md` (Step 3) currently tells the releaser to check every job of the run after the tag push because of this behaviour; update it when the workflow is gated.

Related issues: #29 (This issue).
