Remediation for #13:
- tests 1 (Major): draft 13-delta-2026-10-tests-1-github-coverage-manifest-encina-caching-json-entry-behaviors.md
- tests 2 (Minor): draft 13-delta-2026-10-tests-2-github-coverage-manifest-encina-caching-json-entry-behaviors.md
- tests 3 (Minor): draft 13-delta-2026-10-tests-3-github-coverage-manifest-encina-caching-json-entry-behaviors.md
- tests 4 (Minor): draft 13-delta-2026-10-tests-4-github-coverage-manifest-encina-caching-garnet-valkey-dragon.md
- docs 1 (Major): draft 13-delta-2026-10-docs-1-readme-md-317-340-caching-point-2-c.md
- docs 2 (Major): draft 13-delta-2026-10-docs-2-src-encina-caching-src-encina-caching-memory-hybrid.md
- docs 3 (Major): duplicate of #84 (manual override)
- docs 4 (Major): draft 13-delta-2026-10-docs-4-docs-tutorials-index-md-and-docs-guides-index.md
- docs 5 (Major): draft 13-delta-2026-10-docs-5-docs-features-query-caching-md-headings-query-interception.md
- docs 6 (Blocker): draft 13-delta-2026-10-docs-6-docs-features-query-caching-md-231-239-expected.md
- docs 7 (Minor): draft 13-delta-2026-10-docs-7-docs-index-md-58-point-4-accuracy-of.md
- docs 8 (Minor): draft 13-delta-2026-10-docs-8-docs-features-query-caching-md-related-documentation-line.md

## Lessons for the pipeline
- docs 3: recorded as duplicate of #84 by manual override
- (2026-10-09, #13) Docs 7 reads the `8` in the `docs/index.md:58` Caching row as a provider count, but the table is headed "Packages (112)" and `Encina.slnx:14-21` lists eight `Encina.Caching*` projects (the core package plus seven providers), so the count is right and the defect is that Highlights does not name the core package; the docs-7 draft says so and does not claim Memcached is missing. A count finding should first identify what the table column counts.
- (2026-10-09, #13) Docs 8 says the query-caching page has "no link to ADR-003", but `docs/architecture/adr/003-caching-strategy.md` is "Caching Strategy for Handler Resolution" (handler wrappers and compiled expressions), not query-result caching, so the docs-8 draft does not ask for that link and reports that no ADR in the index records the query-caching design. A docs finding that asks for an ADR link should read the ADR's title and Context first.
- (2026-10-09, #13) Docs 6 counts five hand-typed figures plus `< 10 microseconds`: the table at `docs/features/query-caching.md:233-237` has five rows and line 239 adds a sixth figure and a "1ms target". A benchmark class for those operations exists (`tests/Encina.BenchmarkTests/Encina.Benchmarks/EntityFrameworkCore/QueryCacheInterceptorBenchmarks.cs`), which the docs stage did not mention, so the draft asks for citations to its published results.
- (2026-10-09, #13) Docs 5 cites lines 113 and 146 of `docs/features/query-caching.md` as the "Query Interception Flow" and "Cache Invalidation" headings; those are the opening fences, and the headings are at lines 111 and 142. The docs-5 draft cites the headings and the fenced ranges (113-140, 146-164).
- (2026-10-09, #13) The guard target 17 proposed by tests 1 (16 of 91 coverable lines) is derived from the uncovered-line list of the coverage report, not from a guard test run, and the contract targets of 0 in tests 1 and 2 are provisional against `AGENTS.md` section 9 ("contract: if public API"); the tests-1 draft says to confirm the guard figure after the new guard class exists.
- (2026-10-09, #13) The Glob tool returned no files for existing paths in the audit worktree (for example `src/Encina.Caching*` and `docs/architecture/adr/003*`) while Grep found them, and Grep with a nested `glob` pattern also missed files that a Grep on the folder itself found; every absence claim in the drafts was checked with a Grep on the folder.
