- A repo-wide "removed package" search must use the removed subject's bare name (`Oracle`, `Sqlite`, case-insensitive) and classify every hit; a regex of full package names misses fixtures (`OracleFixture`), extension methods (`AddEncinaADOOracle`), diagram nodes and prose lists. The docs stage lists the regex and a per-page hit-count table in `docs.md` so the verifier can compare.
Applied: role:docs-reviewer For a removed package or provider, search the bare name case-insensitively, classify every hit (finding, duplicate #n, or skip with reason) and put the regex and a per-page hit table in docs.md.
- When the search path filter excludes by directory name (`.claude`), run it from relative paths: the worktree itself lives under `.claude/worktrees/`, so an absolute-path exclusion silently drops the root `README.md`.
Applied: role:docs-reviewer Run repo searches from relative paths inside the worktree; an absolute path contains .claude/worktrees and a .claude exclusion drops everything.
- A page can name a removed provider legitimately (benchmarks that still build against SQLite, `SqliteRespawner.cs`); check `src/` and `tests/` before classifying a hit as stale.
Applied: role:docs-reviewer Before calling a mention of a removed provider stale, check src/ and tests/ for a surviving legitimate use.
- docs 3 said ADR-024 moved the SQLite packages to `.backup/`, "which is gitignored"; `.backup/` does not exist in the audit worktree, and AGENTS.md section 5 and ADR-024 still describe code kept in `.backup/`. The drafts therefore say only that the packages are not in `Encina.slnx` (Oracle was removed, ADR-009; SQLite, ADR-024).
Applied: issue #1806 (AGENTS.md and ADR-024 cite a git-ignored .backup/ folder)
- The glob tool returned no results for existing paths in the audit worktree (for example `docs/architecture/adr/*`), while grep found them: an absent-path claim needs a grep or the solution file as a second check.
Applied: role:remediation-drafter An absent-path claim needs a second check (Grep or Encina.slnx); the Glob tool has returned nothing for existing worktree paths.
- docs 5: recorded as duplicate of #1177 by manual override
Applied: not applied: record of the -DuplicateOf override, not a lesson; #1177 item 6 covers it.
- docs 4, 7 and 8 share one root cause (the Oracle `OracleFixture` leftover in the testing guides) and docs 1, 2 and 9 the same Oracle removal in other pages; the dedup pass left them as separate drafts, so the verifier may want to merge docs 4, 7 and 8.
Applied: not applied: the dedup groups by location on purpose (one draft per page to fix); the drafts share the ADR-009 reference, which is enough to fix them together.
- `src/Encina.EntityFrameworkCore/Encina.EntityFrameworkCore.csproj:20` and `tests/Encina.TestInfrastructure/Encina.TestInfrastructure.csproj:32` and `:52` still reference Oracle NuGet packages (`Oracle.ManagedDataAccess.Core`, `Oracle.EntityFrameworkCore`) although Oracle was removed (ADR-009); the code stage did not report them, so a code or debt finding may be missing.
Applied: issue #1806, comment https://github.com/dlrivada/Encina/issues/1806#issuecomment-5996354323
- When the correction is a describing sentence that disagrees with a table cell, re-reading the cell and grepping the old phrase over the stage files and the drafts directory is enough to close it; keep that as the narrow re-check after a FAIL.
Applied: role:audit-verifier After a narrow FAIL correction, re-read the cited source and grep the old phrase across stage files and drafts; the rest needs only a consistency check.
- An issue opened by the orchestrator during the loop (here #1806, from this very audit) can overlap a draft after the remediation stage ran; the verifier's duplicate search should always list the issues created since the draft was generated and read their bodies.
Applied: role:audit-verifier The duplicate search also lists issues created since the drafts were generated and reads their bodies.
