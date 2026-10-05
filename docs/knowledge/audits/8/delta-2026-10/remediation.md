Remediation for #8:
- tests 1 (Major): draft 8-delta-2026-10-tests-1-github-coverage-manifest-encina-dapper-sqlserver-json-encina.md
- tests 2 (Minor): draft 8-delta-2026-10-tests-2-tests-encina-guardtests-dapper-mysql-scheduledmessagestoreda.md
- docs 1 (Blocker): draft 8-delta-2026-10-docs-1-tests-encina-testinfrastructure-readme-md-50-75-heading.md
- docs 2 (Major): draft 8-delta-2026-10-docs-2-tests-encina-testinfrastructure-readme-md-11-14-203.md
- docs 3 (Major): draft 8-delta-2026-10-docs-3-tests-encina-testinfrastructure-readme-md-205-207-228.md
- docs 4 (Major): merged into docs 1 (same location)
- docs 5 (Minor): draft 8-delta-2026-10-docs-5-docs-features-scheduling-md-1-placement-encina-docs.md
- docs 6 (Minor): draft 8-delta-2026-10-docs-6-docs-features-scheduling-md-55-62-heading-processing.md
- docs 7 (Minor): merged into docs 6 (same location)
- docs 8 (Minor): draft 8-delta-2026-10-docs-8-docs-tutorials-index-md-docs-tutorials-quickstart-md.md

## Lessons for the pipeline
- The tests stage cites the MySQL past-date check as source `:155` (the `throw`; the `if` is at `:154`) while the docs stage cites SqlServer as `:153-154` (`if` and `throw`); the tests-2 draft cites the verified pair `:154-155`.
- The docs stage reports 11 fixtures in `tests/Encina.TestInfrastructure/Fixtures/`; the folder holds 12 `.cs` files directly (the abstract `DatabaseFixture<TContainer>` base and 11 fixture classes) plus the `EntityFrameworkCore` and `Sharding` folders. A file count must say whether the base class is included.
- docs 2 (emoji count): the stage's own lesson already notes that the first count missed the two hourglass lines; the draft uses the final 23.
- Finding docs 7 says the `SchedulerOrchestrator.cs:556` error table "already carries" the past-time code; that table is the orchestrator's error-code constants (`InvalidScheduleTime`, `SchedulerOrchestrator.cs:561`), not the store's `ArgumentException` that the page lacks, so the draft states the store contract separately and does not cite the table as the place to add it.
- The Glob tool returned no result for existing worktree paths again (`tests/Encina.Testing.Examples/*.csproj`); `Encina.slnx:153` was used as the second check.
- Finding docs 2 cites "encina-docs SKILL section 3" for the no-emoji rule, but that section has no emoji rule (grep over the SKILL: 0 hits); the rule is `.claude/agents/docs-reviewer.md` point 1, which the docs-2 draft cites alone.
- The docs-3 Root Cause no longer says the per-class fixture design was replaced by the shared collections: nothing dated in the stage supports that order of events, so it says "not established".
- `docs/features/index.md` has no link to `scheduling.md` (0 matches for "scheduling"); the docs-5 draft adds that link to the fix because the front matter alone does not make the page reachable from the index. The docs stage did not mention it.
- After the verifier's corrections the docs-8 draft covers only the integration-test tutorial (the scheduled-jobs half is #1856), the docs-5 and docs-6 drafts list #1856 as a same-page related issue, and the tests-1 "Real database" checkbox is the template line verbatim; the previous drafts were no longer in the output folder, so all 8 were rewritten from the stage artifacts.
