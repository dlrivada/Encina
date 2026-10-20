# Docs stage - issue #9 (delta rules-2026-10, rule (a) only)

## Pages reviewed

Issue #9 delivered a test fix in `tests/Encina.Dapper.Sqlite.ContractTests/` (the project was deleted by the test consolidation commit `65826302`, 2026-01-16; ADR-024 removed only `src/Encina.Dapper.Sqlite`) and shipped no page. The one destination the record names is ADR-024, so the review covers it, plus the sibling pages that still describe SQLite. Dedup sources read: the delta docs stage of #2 (`docs/knowledge/audits/2/delta-2026-10/docs.md`, which already classified the SQLite hits repo-wide), the bodies of the open umbrella issues #1847, #1850, #1856, #1864 (searched for sqlite, tutorial, learning path and emoji) and the open issues #1806, #1808, #1811, #1363, #1372.

| Page | Result |
| --- | --- |
| `docs/architecture/adr/024-remove-sqlite-provider-pre-1.0.md` (152 lines) | Findings 1 and 2 |
| `docs/architecture/adr/index.md:36` | Links ADR-024; no finding |
| `docs/architecture/data-access-providers.md` | Explains the removal and links ADR-024; no finding |
| `src/Encina.Testing.Respawn/README.md` (:96, :224-:234) | Samples checked against `RespawnerFactory.CreateSqlite` (`RespawnerFactory.cs:99`), `SqliteRespawner` (`SqliteRespawner.cs:35`), `InitializeAsync`/`ResetAsync` (`DatabaseRespawner.cs:76`, `:121`); accurate, no emoji, no finding (same verdict as the #2 delta) |
| `docs/features/temporal-tables.md`, `docs/ci-cd-templates.md`, `docs/security/SECURITY-HOTSPOTS-JUSTIFICATIONS.md` | Classified as legitimate by the #2 delta (generic database knowledge, template parameter, existing `SqliteRespawner.cs`); not re-reviewed |
| `docs/index.md:57`, `docs/contributing/README.md:67`, `docs/testing/load-test-baselines.md:37,:41` | Stale SQLite claims already tracked: #1372, #1811, #1808 |

Command for the hit table: `Get-ChildItem docs,README.md,CONTRIBUTING.md,src -Recurse -Include *.md | Select-String -Pattern 'sqlite'` (case-insensitive) gives 649 hits in 79 files (78 under `docs/`, 1 under `src/`; re-run from the worktree root with relative paths, files counted with `($h.Path | Sort-Object -Unique).Count`), most in `docs/plans/`, `docs/releases/` and `docs/knowledge/` (historical or internal records).

Point 5 (feature adequacy) does not apply: the issue delivered no feature, so no concept page, reference or tutorial entry is owed. No emoji in ADR-024 (code-unit count over U+2300-U+23FF, U+2600-U+27BF, U+2B00-U+2BFF and surrogates: 0).

## Findings

1. **Minor** — `docs/architecture/adr/024-remove-sqlite-provider-pre-1.0.md:46-52` (heading "Maintenance Burden"). Rule (a) points 1 and 3 fail. The "Estimated effort distribution across remaining providers" is an untyped fenced block (` ``` ` with no language) holding a hand-drawn bar chart with percentages (`PostgreSQL ... 15%`, `MySQL 15%`, `SQL Server 15%`, `MongoDB 10%`, `SQLite 45%`, and `~45%` again at `:106`). The figures have no source, date or command, and the chart contradicts its own labels: each non-SQLite row draws 14 or 10 filled cells out of 20 (70% and 50% of the width) for 15% and 10%, while the SQLite row draws 46 filled cells, so bar lengths do not follow the numbers. A reader cannot tell whether this is a measurement or an opinion. Expected: either drop the percentages and keep the qualitative argument, or state them as an estimate with the basis (who, when, how) and render the comparison as a Mermaid chart or a table whose numbers match the picture. No open issue covers it (searched open issues for "ADR-024" in the body: #1806, #1808, #1811, #1750 cover other defects).

2. **Minor** — `docs/architecture/adr/024-remove-sqlite-provider-pre-1.0.md:1`. Rule (a) point 4 (placement, `encina-docs` SKILL section 2: every page under `docs/` starts with just-the-docs front matter) fails. The first line is `# ADR-024: ...`, not `---`; `docs/architecture/adr/index.md` has front matter and sets `has_children: true`, so the page cannot declare `parent: ADRs` or a `nav_order`, and the ADR is not attached to the navigation by the theme. Measured with `Get-ChildItem docs\architecture\adr\*.md | ? { (Get-Content $_ -TotalCount 1) -ne '---' }`: 8 of the 33 numbered ADRs lack it (024, 027, 028, 029, 030, 031, 034, 036); only 024 belongs to this audit, the other seven are for their own audits. No open issue covers ADR front matter (#1363 mentions front matter only for the agent tier table, #1664 covers the coverage methodology page). Related, not a duplicate: if the ADRs are meant to be unlisted, the decision belongs in `docs/_config.yml` defaults rather than in a missing header.

## Informational (not findings)

- The ADR's own content matches the issue's gotcha: the SQLite datetime format incompatibility is in the table at `:23` and the `@NowUtc` rule at `:38`. Its "Related" section links ADR-009 (`:145`) and the index lists it (`adr/index.md:36`), so neighbours and placement are fine apart from finding 2.
- The ADR has no Mermaid or C4 diagram; it is a decision record made of tables and short lists, so no scannability gap beyond finding 1.
- Out-of-scope accuracy items found while reading, already tracked: `docs/index.md:57` says 13 providers across SQLite / SQL Server / PostgreSQL / MySQL (#1372); `AGENTS.md:58` cites the git-ignored `.backup/` folder and CI Full still runs a Sqlite job (#1806). Neither is repeated as a finding.
- No `csharp` block in ADR-024; the Respawn README sample compiles in principle against `src/`.

## Lessons for the pipeline

- When an issue's only destination is an ADR, run the rule (a) checks on the ADR itself: its fenced blocks (language tag, hand-typed percentages), its front matter and whether a hand-drawn chart agrees with its numbers; count the bar cells instead of trusting the labels.
- For a removed provider, read the previous delta docs stage of the sibling issue (#2) first and reuse its per-page classification; only the destination page of the new issue is new work.
- ADR front matter is a one-command check across `docs/architecture/adr/`; report the count but scope the finding to the ADR under audit so each audit's issue stays single-page.
- Count the files of a search as unique paths of the same Select-String output (`($h.Path | Sort-Object -Unique).Count`) and state the command; a remembered file count was wrong here (62 stated, 79 measured; audit #9 verifier FAIL).
