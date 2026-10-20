Remediation for #2:
- docs 1 (Major): draft 2-delta-2026-10-docs-1-docs-infrastructure-docker-infrastructure-md-52-174-and.md
- docs 2 (Minor): draft 2-delta-2026-10-docs-2-readme-md-306-heading-dapper-ado-net-postgresql.md
- docs 3 (Minor): merged into docs 6 (same location)
- docs 4 (Major): draft 2-delta-2026-10-docs-4-docs-testing-integration-tests-md-426-and-430.md
- docs 5 (Minor): duplicate of #1177 (manual override)
- docs 6 (Major): draft 2-delta-2026-10-docs-6-docs-contributing-readme-md-67-and-112-the.md
- docs 7 (Major): draft 2-delta-2026-10-docs-7-docs-testing-testcontainers-direct-usage-md-321-fixture.md
- docs 8 (Minor): draft 2-delta-2026-10-docs-8-docs-testing-aspire-migration-guide-md-22-39.md
- docs 9 (Minor): draft 2-delta-2026-10-docs-9-src-encina-aspire-testing-readme-md-42-mermaid.md
- docs 10 (Minor): draft 2-delta-2026-10-docs-10-docs-testing-load-test-baselines-md-37-41.md

## Lessons for the pipeline
- docs 3 said ADR-024 moved the SQLite packages to `.backup/`, "which is gitignored"; `.backup/` does not exist in the audit worktree, and AGENTS.md section 5 and ADR-024 still describe code kept in `.backup/`. The drafts therefore say only that the packages are not in `Encina.slnx` (Oracle was removed, ADR-009; SQLite, ADR-024).
- The glob tool returned no results for existing paths in the audit worktree (for example `docs/architecture/adr/*`), while grep found them: an absent-path claim needs a grep or the solution file as a second check.
- docs 5: recorded as duplicate of #1177 by manual override
- docs 4, 7 and 8 share one root cause (the Oracle `OracleFixture` leftover in the testing guides) and docs 1, 2 and 9 the same Oracle removal in other pages; the dedup pass left them as separate drafts, so the verifier may want to merge docs 4, 7 and 8.
- `src/Encina.EntityFrameworkCore/Encina.EntityFrameworkCore.csproj:20` and `tests/Encina.TestInfrastructure/Encina.TestInfrastructure.csproj:32` and `:52` still reference Oracle NuGet packages (`Oracle.ManagedDataAccess.Core`, `Oracle.EntityFrameworkCore`) although Oracle was removed (ADR-009); the code stage did not report them, so a code or debt finding may be missing.
