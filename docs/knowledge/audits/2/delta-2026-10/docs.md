## Pages reviewed

Delta rules-2026-10, rule (a) only (third pass, after verifier FAIL 2). Issue #2 delivered Oracle and SQLite ADO SQL scripts. Both packages are gone (`src/Encina.ADO.Oracle`, `src/Encina.Dapper.Oracle`, `src/*ADO.Sqlite*` and `src/*Dapper.Sqlite*` do not exist in this checkout; `.backup/` is absent). No page describes the scripts themselves, so the pages in scope are the live pages that still present those providers.

Search (run with `Select-String` inside the wia-2 worktree, case-insensitive, per line):

- Regex: `(?i)oracle|sqlite`
- Files searched: 270 `*.md` files = every `docs/**/*.md` except `docs/plans/`, `docs/architecture/adr/`, `docs/releases/`, `docs/specifications/` and `docs/knowledge/`, plus every `README.md` outside `docs/`, `artifacts/`, `.claude/`, `.backup/`, `.git/` and `node_modules/`.
- Result: 148 matching lines in 31 pages. Every page is accounted for in the table below (finding number, duplicate of an open issue, or explicit skip with reason).

| Page | Hits | Classification |
| --- | --: | --- |
| `docs/testing/integration-tests.md` | 17 | Finding 4 |
| `docs/benchmarks/provider-sql-dialect-comparison.md` | 16 | Skip: SQLite is the benchmark target; `tests/Encina.BenchmarkTests` still builds against SQLite (`AdoConnectionFactory.cs`, `DapperTypeHandlers.cs`), so the page describes the benchmark, not an Encina provider |
| `docs/INVENTORY.md` | 14 | Skip: dated generated snapshot (2 February 2026), see Informational |
| `docs/engineering/PROJECT-HISTORY.md` | 12 | Skip: history page, each hit is a dated record with an issue number; line 706 already corrects the "13 providers" claim |
| `docs/infrastructure/docker-infrastructure.md` | 11 | Finding 1 |
| `docs/testing/benchmarks/benchmark-results.md` | 9 | Skip: dated benchmark run (28 January 2026) of SQLite InMemory benchmarks that still exist in `tests/Encina.BenchmarkTests`; a result record, not a provider claim |
| `docs/testing/aspire-migration-guide.md` | 8 | Finding 8 |
| `docs/comparacion-nestjs.md` | 8 | Skip: `nav_exclude: true`, dated 2025-12-21 comparison written in Spanish before ADR-009 and ADR-024, a historical analysis (hits :70, :71, :141, :622, :623, :1233, :1234, :2000, including `AddEncinaADOOracle` at :2000) |
| `src/Encina.Testing.Respawn/README.md` | 7 | Skip: accurate. `src/Encina.Testing.Respawn/SqliteRespawner.cs` and `RespawnerFactory.cs` still exist; the README labels SQLite "unsupported" and Oracle "Not included" |
| `docs/security/SECURITY-HOTSPOTS-JUSTIFICATIONS.md` | 5 | Skip: dated justification of the existing `SqliteRespawner.cs` hotspot |
| `docs/features/temporal-tables.md` | 5 | Skip: lists SQLite as a database without temporal tables and shows a trigger example for it; generic database knowledge, not a claim that Encina ships a SQLite provider |
| `docs/testing/load-test-baselines.md` | 5 | Finding 10 |
| `docs/architecture/data-access-providers.md` | 4 | Skip: correct, explains the removal and links ADR-009 and ADR-024 |
| `docs/ci-cd-templates.md` | 4 | Skip: `nav_exclude: true` template reference whose `test-databases` parameter takes a consumer's own database names (:67, :151, :164, :188), not Encina providers |
| `docs/engineering/ENGINEERING-HANDBOOK.md` | 3 | Skip: frozen snapshot of CLAUDE.md (2026-09-25), "not maintained" banner |
| `README.md` | 2 | Finding 2 |
| `docs/contributing/README.md` | 2 | Findings 3 and 6 |
| `docs/reports/coverage-baseline-2026-03-24.md` | 2 | Skip: self-labelled "Historical Snapshot" of SonarCloud (rows `ADO.Sqlite` :130, `Dapper.Sqlite` :135) |
| `docs/engineering/prompts/implementation-plan-prompt.md` | 2 | Skip: line 13 states SQLite is out of the count; line 172 is a generic SQLite testing note in a prompt, not a provider claim |
| `docs/testing/testcontainers-direct-usage.md` | 1 | Finding 7 |
| `src/Encina.Aspire.Testing/README.md` | 1 | Finding 9 |
| `docs/index.md` | 1 | Duplicate of open #1372 (line 57 lists SQLite among 13 database providers; #1372 covers the counts in `docs/index.md`) |
| `docs/messaging/index.md` | 1 | Finding 5 (duplicate of open #1177 item 6) |
| `docs/guides/health-checks.md` | 1 | Skip: names the third-party package `AspNetCore.HealthChecks.Oracle` (:229), not an Encina package |
| `docs/features/database-providers.md` | 1 | Skip: correct, says Oracle and SQLite are not part of the list and links ADR-009 and ADR-024 |
| `docs/features/cdc-debezium.md` | 1 | Skip: names Oracle as a database Debezium supports (:23), a third-party fact |
| `docs/engineering/PHASE0-BASELINE.md` | 1 | Skip: historical pre-remediation snapshot (status line says so) |
| `docs/engineering/orchestrator-memory-inventory.md` | 1 | Skip: names a memory file `sqlite-patterns`, not a provider claim |
| `docs/engineering/agents-md-traceability.md` | 1 | Skip: correct mapping row "Oracle and SQLite out of the matrix" |
| `docs/roadmap-documentacion.md` | 1 | Skip: `nav_exclude: true` Spanish planning checklist (:310) |
| `tests/Encina.TestInfrastructure/README.md` | 1 | Skip: correct, says SQLite was removed from the provider matrix (:236) |

## Findings

1. **Major** — `docs/infrastructure/docker-infrastructure.md:52`, `:174` and `:178` (headings "Services", "Provider Coverage Matrix"). Check 2 and 5 (accuracy against `src/`) fail. The page lists `Encina.ADO.Oracle` and `Encina.Dapper.Oracle` with a green check in the "Provider Coverage Matrix". Neither package exists under `src/` (Test-Path false for both), and ADR-009 removed Oracle from the 1.0 matrix. The same page still documents Oracle in the profile table (`:23`), credentials (`:113`), the connection-string sample (`:130`) and the troubleshooting section "Oracle Startup Issues" (`:261`), so a reader sets up a container for a provider that cannot be used.
2. **Minor** — `README.md:306` (heading "Dapper / ADO.NET / PostgreSQL / MySQL / Oracle") and `README.md:429` (health-check table row "Databases: PostgreSQL, MySQL, SQL Server, Oracle, MongoDB, Marten"). Check 2 fails: both name Oracle as a supported provider while the package does not exist and AGENTS.md section 5 puts Oracle out of the matrix (ADR-009).
3. **Minor** — `docs/contributing/README.md:67` (one paragraph of 1,015 characters, measured). Check 2 fails: it says `src/` holds "the two unsupported SQLite packages" and names `Encina.ADO.Sqlite` and `Encina.Dapper.Sqlite` as members of the ADO and Dapper families, citing "repository listing". `src/` has no `*Sqlite*` project directory for them in this checkout (ADR-024 moved them to `.backup/`, which is gitignored). The "105 projects" count at the same line is also a hand-typed figure (check 3), not a citation with a date and command.
4. **Major** — `docs/testing/integration-tests.md:426` and `:430` (the "Provider Matrix" table). Check 2 fails: it lists two Oracle rows. The `Dapper.Oracle` row (`:426`) has a check in all four columns (Integration, Contract, Property, Load: ✅ ✅ ✅ ✅). The `ADO.Oracle` row (`:430`) has a cross in Integration and checks in the other three (❌ ✅ ✅ ✅), which the note at `:432` explains as Testcontainers-based tests in the Contract, Property and Load projects instead of a separate Integration project. Either way the table presents both as providers with test coverage; no such packages or test projects exist (ADR-009). The same page keeps Oracle in the credentials table (`:80`), the image table (`:91`), the licence note (`:120`), the collection-naming example `ADO-Oracle` and `OracleFixture` (`:215`, `:221`, `:224`), the "Oracle-Specific Issues" section (`:362`-`:370`), the Aspire comparison (`:381`, `:394`) and the Oracle registry link (`:442`), so a reader is told to write tests for a provider that cannot be built. This page is the entry point for the integration-test rules in AGENTS.md section 9.
5. **Minor** — `docs/messaging/index.md:45` (provider table). Check 2 fails: row `Dapper (Oracle) | Encina.Dapper.Oracle | Enterprise, Oracle DB` lists a removed package as a persistence provider. This is already tracked as open #1177 item 6; remediation records it as a duplicate of #1177, not as a new issue.
6. **Major** — `docs/contributing/README.md:67` and `:112` (the "Multi-Provider Implementation Rule" and repository-layout passages). Rule (a) point 1 (visual and scannable) fails. Line 67 is a single 1,015-character paragraph with seven "(source: ...)" clauses that enumerates package families (core, ADO, Dapper, EF Core, MongoDB) and their members: a structure that a table (family, packages, source) would show at a glance. Line 112 (597 characters) enumerates the ten providers as 3+3+3+1 in running prose, which AGENTS.md section 5 already presents as a family/provider table. Both pages explain structure, so this is major, not minor. Lines 67 and 112 should be fixed together with finding 3 (one draft covers the SQLite accuracy and the table rewrite).
7. **Major** — `docs/testing/testcontainers-direct-usage.md:321` (fixture table). Check 2 fails: the row `OracleFixture | Testcontainers (Generic) | Oracle` names a fixture that exists nowhere in the repository (Grep over `*.cs` and `*.md` finds `OracleFixture` only in docs), and the Oracle provider was removed by ADR-009. Same root cause as finding 4 (`OracleFixture` at `integration-tests.md:215`-`:224`); remediation may merge it into the draft for finding 4.
8. **Minor** — `docs/testing/aspire-migration-guide.md:22`, `:39`, `:496`-`:515` (section "Oracle Database") and `:818`. Check 2 fails: the guide presents Oracle as a database a reader must keep on Testcontainers (decision node "Do you need Oracle, NATS, or MQTT?", table row "Oracle database | Testcontainers | Not supported in Aspire", a full `OracleFixture : DatabaseFixture<IContainer>` example with image `gvenzl/oracle-free:23-slim-faststart`, and a migration checklist item "check for Oracle/NATS/MQTT blockers"). Oracle has no Encina provider and no fixture exists; the example cannot be used with any current package. Minor, not major: the guide is a how-to for a generic Testcontainers pattern, and the page is not the entry point for provider rules. Remediation may merge it with findings 4 and 7 as one "Oracle fixtures in the testing guides" draft.
9. **Minor** — `src/Encina.Aspire.Testing/README.md:42` (mermaid diagram, subgraph "Database Tests (Real DB)"). Check 2 fails: the node lists `Oracle` next to SqlServer, PostgreSQL, MySQL and Redis as a Testcontainers database to test against; `src/Encina.Aspire.Testing/*.cs` has no Oracle reference and Oracle is out of the matrix (ADR-009).
10. **Minor** — `docs/testing/load-test-baselines.md:37`, `:41` (table rows `ado-sqlite`, `dapper-sqlite`) and the Database row "13 DB providers". Check 2 fails: no load-test profile or code uses `ado-sqlite` or `dapper-sqlite` (searched all files under `tests/Encina.LoadTests`: 0 hits); only `efcore-sqlite` remains in `tests/Encina.LoadTests/profiles/nbomber.database-*.json`, which is itself a leftover of the SQLite removal (ADR-024). The page publishes target figures for two providers that cannot be run. The count "13" is also tracked by #1372. `:45`, `:164` and `:421` (`efcore-sqlite`) match the profile files today, so they are stale only because of the code-side leftover.

## Informational (not findings)

- `docs/INVENTORY.md:1356`, `:1361`, `:7355` and `:7359` mark `Encina.Dapper.Oracle` and `Encina.ADO.Oracle` as "Completo" and show them in the tree. The file states "Documento generado: 2 de febrero de 2026" in its first lines, so it is a dated, generated snapshot (historical, written before the Oracle removal commit of 2026-01-26 was reflected). The adjacent SQLite rows at `:1357` and `:1362` already read "Removed". Skipped as historical, not a finding; the staleness of its Oracle rows would be fixed by regenerating the file.
- `docs/comparacion-nestjs.md` is skipped explicitly as historical: front matter `nav_exclude: true`, a dated 2025-12-21 analysis, Spanish prose that predates the English-only rule. It lists Oracle as a Dapper and ADO provider at :70, :71, :1233, :1234 and shows `services.AddEncinaADOOracle(connectionString);` at :2000. Reason for skip: a historical comparison document hidden from navigation, not a guide.
- `docs/reports/coverage-baseline-2026-03-24.md:130` and `:135` (`ADO.Sqlite`, `Dapper.Sqlite` rows) are skipped explicitly: the page is titled "Coverage Baseline Report (Historical Snapshot)" and carries a note that it is a snapshot.
- `docs/guides/health-checks.md:229` names `AspNetCore.HealthChecks.Oracle`, a third-party NuGet package, not an Encina package. No finding.
- `.github/workflows/ci-full.yml:312`-`:326` and `:381` still run an EF Core "Sqlite" job while `ci.yml:406` states SQLite was removed (ADR-024). This is workflow code, not a page; it is outside rule (a). It may deserve an issue in the code-side stage; not duplicated here.
- Point 1 (visual): `docker-infrastructure.md` and `integration-tests.md` use tables and code blocks; their failure is accuracy (findings 1 and 4), not form. The wall of text is `docs/contributing/README.md` (finding 6).
- Point 4 (placement): not assessed; #2 created no page.
- Point 5 (feature docs): #2 is a debt fix of SQL scripts, not a feature. No concept page, reference or tutorial entry is expected.
- `docs/plans/*` pages (for example `multi-provider-implementation-plan.md`) still list ADO.Oracle and ADO.Sqlite; they are dated plans, excluded from the search by design.
- AGENTS.md:58 says Oracle code is in `.backup/oracle/`, which does not exist in the checkout (it is gitignored). Not a #2 page. Remediation wording should say "Oracle was removed (ADR-009)" and not repeat the `.backup/oracle/` claim.

## Lessons for the pipeline

- A repo-wide "removed package" search must use the removed subject's bare name (`Oracle`, `Sqlite`, case-insensitive) and classify every hit; a regex of full package names misses fixtures (`OracleFixture`), extension methods (`AddEncinaADOOracle`), diagram nodes and prose lists. The docs stage lists the regex and a per-page hit-count table in `docs.md` so the verifier can compare.
- When the search path filter excludes by directory name (`.claude`), run it from relative paths: the worktree itself lives under `.claude/worktrees/`, so an absolute-path exclusion silently drops the root `README.md`.
- A page can name a removed provider legitimately (benchmarks that still build against SQLite, `SqliteRespawner.cs`); check `src/` and `tests/` before classifying a hit as stale.
