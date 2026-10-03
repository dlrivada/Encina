<!--
title: [DEBT] No automated gate enforces that each coverage flag reaches its own manifest target
labels: technical-debt
milestone: 
kind: debt
-->

## Type

- [ ] Failing tests
- [ ] Missing tests
- [x] Code quality (warnings, analyzers)
- [ ] Performance optimization
- [ ] Refactoring needed
- [ ] Documentation gap
- [ ] Incorrect implementation
- [ ] Other

## Description

AGENTS.md section 9 states as a MUST that each coverage flag independently reaches its own target from `.github/coverage-manifest/{Package}.json`, and that nothing is pushed or merged until every applicable flag does. No job in the repository compares a flag's coverage with its target, so the rule is enforced only by hand. The backlog this hides is real: #1389 tracks the guard flag below target across the package family, and #1450 asks for a per-flag PR comment as a fallback.

How the report and the workflows behave today:

- `coverage-report.cs` only copies the manifest targets into its JSON output (`perFlagTarget`, `.github/scripts/coverage-report.cs:657-661` and `:760`); nothing compares a flag's coverage with it.
- The script's only non-zero exits are the `--check-stale-manifest` and `--check-missing-manifest` modes (`:143-173`).
- The markdown report's `Gap` column is a hard-coded empty cell (`:715`).
- A comment at `:604-607` says flags without data "penalize via the flag target check", but no such check exists.
- A package's per-flag aggregate silently omits a flag that produced no data (`:592-600`), and `FlagPct` renders it as `-` (`:928-935`).
- A Cobertura parse error is printed and skipped (`:507-510`).
- `.github/workflows/ci-full.yml:475` runs the script with `|| true`. The only follow-up gate (`:480-496`) fails solely when no DocRef index exists or every entry is `noData`.
- The `coverage` job runs with `if: always()` (`ci-full.yml:433`), so a run with a failed integration shard is published as if complete.
- `.github/workflows/ci.yml` gates only CRAP on changed methods (`:493-543`).
- `codecov.yml:15-24` sets the project `target: auto`, the patch target to 60% and `require_ci_to_pass: false` (`:6`), with no per-flag target.
- The methodology page says "the real enforcement happens at the per-package level where manifests carry explicit numbers" (`docs/testing/coverage-measurement-methodology.md:192`), but the only enforcement is manual.

## Location

- **File(s)**: `.github/scripts/coverage-report.cs` (`:143-173`, `:507-510`, `:592-600`, `:604-607`, `:657-661`, `:715`, `:760`, `:928-935`), `.github/workflows/ci-full.yml` (`:433`, `:475`, `:480-496`), `.github/workflows/ci.yml` (`:493-543`), `codecov.yml` (`:6`, `:15-24`), `docs/testing/coverage-measurement-methodology.md` (`:192`), `AGENTS.md` section 9
- **Package(s)**: none (repository tooling, not an Encina package)

## Current Behavior

A flag below its manifest target, a flag with no coverage data and a Cobertura file that fails to parse all leave the report and every blocking job green. The dashboard colours cells, but nothing fails.

## Expected Behavior

The repository either enforces the AGENTS.md section 9 rule mechanically or states honestly that per-flag targets are advisory. When enforced, a flag below its target, a flag with no data for an applicable file and a Cobertura parse error each produce a non-zero exit from the check.

## Root Cause

The per-flag target model replaced the single project-wide threshold, but the comparison step was never written. The surrounding code still assumes it exists (the comment at `coverage-report.cs:604-607`, the methodology wording at `:192`).

## Proposed Fix

Decide between two options and record the decision:

1. Add a per-flag target check to `coverage-report.cs` (or to a script next to it): report-only first, then blocking with a ratchet so existing shortfalls do not block unrelated PRs. It treats a missing flag and a parse error as failures, not as `-`. Drop `|| true` from the `ci-full.yml` step, or gate on the check's exit code.
2. Reword AGENTS.md section 9 to say that targets are advisory and shown on the dashboard, not gated.

Whichever option is chosen, the methodology page and `docs/en/guides/TESTING.md` must describe the same thing.

## Priority

- [ ] **High** - Blocks functionality or causes failures in production
- [x] **Medium** - Should be fixed before 1.0 release
- [ ] **Low** - Nice to have, can be deferred

## Effort Estimate

- [ ] Small (< 1 hour)
- [ ] Medium (1-4 hours)
- [x] Large (> 4 hours)

## Related Issues

- #19 (This issue)
- #1389 - tracks the guard flag below target across the package family
- #1450 - asks for a per-flag PR comment as a fallback
- #1360 - partially related (it covers only part of this finding)
