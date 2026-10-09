## Scope
Issue #31 (created 2025-12-24T13:21:57Z, closed 2025-12-24T17:56:25Z, 4 h 34 min; state_reason COMPLETED) has no linked PR, no commit referencing it, and one owner comment (17:56:24Z). The timeline holds only label/milestone/rename events. The closest change is commit ccf83b00 (2025-12-24 18:42:25 +0100 = 17:42:25Z, 14 minutes before the close at 17:56:25Z and the owner comment at 17:56:24Z; "Closes #29, #30"), which edited `.github/workflows/sonarcloud.yml` to re-enable ContractTests/PropertyTests.

Scope today (the issue named seven workflows):
- `.github/workflows/dotnet-ci.yml`: DELETED in commit 59a5a867 (2026-02-07), merged into `.github/workflows/ci.yml` (`git log --follow --diff-filter=D` verified). Scope is `ci.yml` and `ci-full.yml`.
- `.github/workflows/sonarcloud.yml`: exists; now static analysis only, no test step (comment cites Codecov, ADR-023, #911).
- `.github/workflows/codeql.yml`, `sbom.yml`, `benchmarks.yml`, `load-tests.yml`, `mutation-tests.yml`: all exist today.
- ContractTests/PropertyTests: `tests/Encina.ContractTests` and `tests/Encina.PropertyTests` run as jobs in `ci.yml` and `ci-full.yml` (grep verified for ContractTests).
- README badges: README.md carries badges for ci.yml, sonarcloud.yml, codeql.yml, sbom.yml, benchmarks.yml; none for load-tests.yml or mutation-tests.yml (dashboard badges instead).
No `src/` scope.

## Destinations
- Decision "ContractTests and PropertyTests re-enabled": present as jobs in `.github/workflows/ci.yml` and `ci-full.yml`.
- Workflow consolidation / SonarCloud static-only: present in `docs/architecture/adr/023-coverage-strategy-codecov-sonarcloud.md` (file exists).
- Pre-draft candidate "reviewer checklist: all workflows green before merging": no source quotes it; dropped (pre-draft rule with no source). The CI result gate (`ci-result`) is the actual enforcement, not verified further here.
- Pre-draft candidate "README badges display correctly": issue body only, never delivered by this issue; recorded nowhere as a decision.
- Gap recorded as gotcha: only 4 of 7 workflows reported green; Proposed Solution checkboxes all unticked; no "known issues" doc written. Pre-draft open questions answered: sbom/benchmarks/load-tests/mutation-tests were not reported; ContractTests/PropertyTests re-enabled via sonarcloud.yml change in ccf83b00 (4 lines).

## Successor and duplicate issues
No successor or duplicate named. Related, verified CLOSED: #29 "[INFRA] Re-enable ContractTests in SonarCloud workflow", #30 "[INFRA] Re-enable PropertyTests in SonarCloud workflow" (both audited siblings; record for #29 exists in docs/knowledge/issues).

## Lessons for the pipeline
- (issue-archivist) The pre-draft had `closed_at: 12/24/2025` and `linked_prs: []` in a different format; normalised to ISO and the schema-1 fields (repeat of the audit #29 lesson).
- (issue-archivist) A "verify everything is green" issue closed by one comment is evidence of a claim, not of verification; record which items the comment covers versus the checklist (4 of 7 workflows here) as a gotcha rather than a delivered decision.
