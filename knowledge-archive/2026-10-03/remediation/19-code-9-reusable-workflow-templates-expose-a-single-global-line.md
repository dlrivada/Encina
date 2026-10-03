<!--
title: [DEBT] Reusable workflow templates expose a single global coverage threshold and compare it with python
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

The reusable workflow templates expose a single global line-coverage threshold and enforce it with `python`.

- `.github/workflows/templates/encina-test.yml:44-47` declares the input `coverage-threshold` ("Minimum line coverage percentage"), and `:216-244` computes one percentage and fails below it.
- `.github/workflows/templates/encina-full-ci.yml:50` declares the same input, and `:276-303` does the same computation.
- The comparison runs `python3 -c` / `python -c` (`encina-test.yml:240-241`, `encina-full-ci.yml:299-300`), which AGENTS.md section 2 forbids (scripting only in PowerShell or C# file-based apps).
- `encina-test.yml:17` and `encina-full-ci.yml:20` show `coverage-threshold: '80'` as the documented usage, and `docs/ci-cd-templates.md` documents the input and recommends values (`:49`, `:87`, `:111`, `:132`, `:207`, `:255`, `:293`).
- `tests/Encina.UnitTests/Workflows/WorkflowTemplateTests.cs:160` asserts that the input exists.
- No workflow in this repository calls the templates; only comments and documentation (`docs/releases/v0.11.0`, `docs/ci-cd-templates.md`, `docs/INVENTORY.md`, `CHANGELOG.md`) refer to them.

AGENTS.md section 9 says there is no project-wide percentage.

## Location

- **File(s)**: `.github/workflows/templates/encina-test.yml`, `.github/workflows/templates/encina-full-ci.yml`, `docs/ci-cd-templates.md`, `tests/Encina.UnitTests/Workflows/WorkflowTemplateTests.cs:160`
- **Package(s)**: none (repository workflow templates, not an Encina package)

## Current Behavior

A caller of the templates can set one global coverage percentage that the template enforces with a python one-liner, and the documentation recommends doing so.

## Expected Behavior

No template holds a global coverage threshold, and no workflow script uses python.

## Root Cause

The templates were written for the single-threshold model and are not used by any workflow in this repository, so nothing exercised or reviewed them against the per-flag model.

## Proposed Fix

Decide between deleting the templates (together with `docs/ci-cd-templates.md`, the `docs/INVENTORY.md` entries and the whole `WorkflowTemplateTests` fixture) or replacing the global threshold with a per-flag check built on the same mechanism as the repository's own gate. Either way, the `coverage-threshold` input, the python comparison, the documentation of the input and the test row that locks it in all go with it.

## Priority

- [ ] **High** - Blocks functionality or causes failures in production
- [ ] **Medium** - Should be fixed before 1.0 release
- [x] **Low** - Nice to have, can be deferred

## Effort Estimate

- [ ] Small (< 1 hour)
- [x] Medium (1-4 hours)
- [ ] Large (> 4 hours)

## Related Issues

- #19 (This issue)
