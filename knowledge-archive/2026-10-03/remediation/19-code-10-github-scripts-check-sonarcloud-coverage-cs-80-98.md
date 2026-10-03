<!--
title: [DEBT] Delete the orphan check-sonarcloud-coverage.cs script that hard-codes an 80% project-wide coverage verdict
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

`.github/scripts/check-sonarcloud-coverage.cs:80-98` hard-codes an 80% project-wide coverage verdict. It prints "meets quality gate threshold (80%)", "is acceptable but below target (80%)" and "needs improvement - target is 80%" (`:81-92`). Nothing references the script: a repository search finds only its own header (`:1`, "Run: dotnet run --file scripts/check-sonarcloud-coverage.cs").

ADR-023 decision 3 says SonarCloud no longer receives coverage, so the output would always be "no coverage data". The script is an orphan that holds a global-percentage target, against AGENTS.md section 9 (no project-wide percentage) and section 3 (no code without a current purpose).

## Location

- **File(s)**: `.github/scripts/check-sonarcloud-coverage.cs:80-98`
- **Package(s)**: none (repository tooling, not an Encina package)

## Current Behavior

The script exists, is run by nobody, and states an 80% coverage target if it is ever run.

## Expected Behavior

The script does not exist.

## Root Cause

It was written when SonarCloud received coverage; ADR-023 moved coverage measurement elsewhere and the script was left behind.

## Proposed Fix

Delete `.github/scripts/check-sonarcloud-coverage.cs`.

## Priority

- [ ] **High** - Blocks functionality or causes failures in production
- [ ] **Medium** - Should be fixed before 1.0 release
- [x] **Low** - Nice to have, can be deferred

## Effort Estimate

- [x] Small (< 1 hour)
- [ ] Medium (1-4 hours)
- [ ] Large (> 4 hours)

## Related Issues

- #19 (This issue)
