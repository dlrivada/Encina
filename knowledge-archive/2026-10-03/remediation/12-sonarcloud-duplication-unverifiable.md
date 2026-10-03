<!-- issue
title: [DEBT] SonarCloud duplication threshold claim from #12 is unverifiable today
labels: technical-debt, area-ci-cd
milestone: v0.14.0 — Hardening
-->

## Type

- [ ] Failing tests
- [ ] Missing tests
- [ ] Code quality (warnings, analyzers)
- [ ] Performance optimization
- [ ] Refactoring needed
- [x] Documentation gap
- [ ] Incorrect implementation
- [x] Other

## Description

Found by the SPEC-003 audit of #12 (adversarial-reviewer pass).

Issue #12's title and goal were "Reduce code duplication in Dapper/ADO providers to meet
SonarCloud ≤3% threshold." The issue was closed with centralization work (moving behavior into
`Encina.Messaging`) plus a decision to **exclude** the remaining Store-level duplication from
SonarCloud's copy-paste-detection (CPD) analysis entirely, first via `sonar-project.properties`
and, since April 2026, via `/d:sonar.cpd.exclusions` in `.github/workflows/sonarcloud.yml:161-164`.

That exclusion list has since grown well beyond the original Dapper/ADO stores — it now also
excludes `Encina.Testing*`, `Encina.EntityFrameworkCore`, `Encina.MongoDB`, `Encina.Caching.*`,
`Encina.DistributedLock.*` and `Encina.Marten` from CPD analysis entirely. As a result, the
issue's own success criterion ("≤3% threshold") is unverifiable: SonarCloud is not measuring
duplication in most of the codebase's provider packages, so there is no dashboard, report or
number anywhere that says what the actual duplication percentage is, or whether it is trending in
either direction.

## Location (File(s)/Package(s))

- **File(s)**: `.github/workflows/sonarcloud.yml` (the `sonar.cpd.exclusions` parameter)
- **Package(s)**: N/A — this is a CI measurement gap, not a code defect

## Current Behavior

`sonar.cpd.exclusions` in `.github/workflows/sonarcloud.yml:163` excludes entire provider
packages from SonarCloud's duplication analysis. No document, dashboard or report states the
current measured duplication percentage for the codebase, or for the excluded packages
specifically, so "the ≤3% threshold is met" cannot be confirmed or denied.

## Expected Behavior

Either:
1. Narrow the CPD exclusions back to only the specific Store SQL files that have a documented
   reason to duplicate (per the new ADR proposed in the sibling remediation issue
   `12-sql-duplication-decision-docs.md`), so SonarCloud actually measures duplication in the rest
   of each excluded package; or
2. If package-wide exclusion is intentional (e.g., because most of a provider package's code is
   itself SQL-dialect-specific), record that decision explicitly (in the same ADR) together with
   an alternative way to track duplication trends for the excluded code — for example, a periodic
   manual SonarCloud scan without the exclusion, or a lint rule that flags near-duplicate blocks
   above a size threshold.

## Root Cause

The CPD exclusion mechanism was scoped once (Dec 2025) for the specific Store classes discussed
in #12, then broadened during an unrelated CI migration (commit `93ef58e4`, April 2026, "remove
sonar-project.properties... move inclusions/exclusions/cpd settings as /d: parameters") without
anyone re-evaluating whether the wider scope was still justified by the original rationale
(SQL-dialect duplication). No one has revisited the actual duplication numbers since.

## Proposed Fix

1. Run a one-off SonarCloud (or `dotnet-format`/manual) duplication scan without the
   `sonar.cpd.exclusions` parameter to get today's real numbers for the excluded packages.
2. Based on the result, either narrow the exclusion list to the genuinely SQL-dialect-duplicated
   files, or explicitly re-justify the broader exclusion in the ADR from the sibling remediation
   issue.
3. Add a comment in `.github/workflows/sonarcloud.yml` next to `sonar.cpd.exclusions` linking to
   that ADR, so future scope changes get the same scrutiny.

## Priority

- [ ] **High** - Blocks functionality or causes failures in production
- [ ] **Medium** - Should be fixed before 1.0 release
- [x] **Low** - Nice to have, can be deferred

## Effort Estimate

- [ ] Small (< 1 hour)
- [x] Medium (1-4 hours)
- [ ] Large (> 4 hours)

## Related Issues

#12 (SPEC-003 audit source), see also `12-sql-duplication-decision-docs.md` (the ADR this issue's
fix should reference)
