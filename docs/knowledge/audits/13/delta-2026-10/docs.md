## Pages reviewed

Delta rules-2026-10, rule (a) only. Issue #13 shipped no page (rejected-reasoned, 0 PRs), so the scope is the pages that describe the caching architecture the decision protects:

- `docs/features/query-caching.md` (310 lines; EF Core query cache, the only caching feature page)
- `README.md` section "Caching" (line 317)
- `docs/index.md` line 58 (caching row) and line 18, `docs/features/index.md` lines 65-71, `docs/tutorials/index.md`
- `src/Encina.Caching*` (8 package folders), searched for READMEs

## Findings

1. **Major** — `README.md:317-340` ("Caching"), point 2 (C# correct against `src/`): the sample marks a query with `[Cache(Duration = 300)]`. `CacheAttribute` (`src/Encina.Caching/Attributes/CacheAttribute.cs:51`) has `DurationSeconds { get; init; }` (line 57) and a get-only computed `Duration` (line 135), so `Duration = 300` does not compile. Fix: `[Cache(DurationSeconds = 300)]`. The other lines of the block resolve (`AddEncinaCaching` with `EnableQueryCaching`/`DefaultDuration` in `CachingOptions.cs:12,42`, `AddEncinaRedisCache`, `AddQueryCaching(Action<QueryCacheOptions>)`, `ExcludeType<T>()`, `UseQueryCaching(IServiceProvider)`).

2. **Major** — `src/Encina.Caching/`, `src/Encina.Caching.Memory/`, `.Hybrid/`, `.Redis/`, `.Garnet/`, `.Valkey/`, `.Dragonfly/`, `.KeyDB/`, point 5 and `AGENTS.md` section 8 ("each satellite package has its own README"): none of the 8 packages has a `README.md` (`Get-ChildItem src\Encina.Caching* -Filter *.md -Recurse` returned nothing). The four Redis-protocol packages hold only `ServiceCollectionExtensions.cs`, so a reader cannot learn from a package page that they reuse `RedisCacheProvider`, the exact fact the #13 decision rests on.

3. **Major** — `docs/features/`, point 5 (feature needs a concept page and a reference): the handler-level caching delivered by `Encina.Caching` (`QueryCachingPipelineBehavior<,>`, `CacheInvalidationPipelineBehavior<,>`, `DistributedIdempotencyPipelineBehavior<,>`, `[Cache]`, `AddEncinaCaching`, the 8 providers) has no page. The only caching page is `query-caching.md`, which covers the EF Core interceptor and sends the reader to `README.md#caching` for the rest (its "Related Documentation"). `AddEncinaCaching` appears in no page under `docs/` except plans and `comparacion-nestjs.md`. `CachingOptions` (17 public properties at `src/Encina.Caching/Configuration/CachingOptions.cs:12-111`, plus 3 on the nested `CacheSerializerOptions` at `:117-135` (`SerializerType`, `EnableCompression`, `CompressionThreshold`); 20 is the file total) has no option table. Related: open issue #84 "[FEATURE] Documentation: Caching Overview" (milestone v0.21.0) asks for the same caching overview, provider comparison, options and `[Cache]` usage, so this finding is a duplicate of #84; the option table and a diagram are acceptance items to add there.

4. **Major** — `docs/tutorials/index.md` and `docs/guides/index.md`, point 5: caching is in no tutorial and no guide (tutorials: `quickstart.md` only; guides: health checks, pipeline behavior, id generation, reference-table scaling). There is no learning path that reaches caching. `docs/features/index.md:71` links only `query-caching.md` under "Messaging & Caching".

5. **Major** — `docs/features/query-caching.md`, headings "Query Interception Flow" (line 111) and "Cache Invalidation" (line 142), point 1: the page explains a 5-stage flow with plain untyped fenced blocks of ASCII arrows (blocks at lines 113-140 and 146-164); it has 0 Mermaid diagrams (`Select-String -Pattern mermaid` count 0). A `sequenceDiagram` or `flowchart` is what the encina-docs SKILL section 2 asks for any flow of more than three steps. The 8-provider layout (core behaviors plus provider packages) that the #13 decision documents has no diagram anywhere.

6. **Blocker** — `docs/features/query-caching.md:231-239` ("Expected Overhead"), point 3: five hand-typed performance figures (`< 1 microsecond`, `< 2 microseconds`, `< 5 microseconds`, `< 1 microsecond`, `< 50 microseconds`) and `< 10 microseconds` (line 239), introduced by "Based on BenchmarkDotNet measurements" with no performance citation, date or command. House rule 2 (encina-docs SKILL section 3) requires a docref citation to the performance dashboard or removal.

7. **Minor** — `docs/index.md:58`, point 4 (accuracy of the entry page): the Caching row sits in the "Packages (112)" table (`docs/index.md:52`), so its `8` counts packages, and `Encina.slnx:14-21` lists 8 `Encina.Caching*` projects (core `Encina.Caching`, Memory, Redis, Garnet, Valkey, Dragonfly, KeyDB, Hybrid). The count is right, but the Highlights list names only 7 (Memory, Hybrid, Redis, Valkey, Dragonfly, Garnet, KeyDB) and omits the core `Encina.Caching` package; the missing name is not Memcached (planned in `AGENTS.md` section 5, no folder under `src/`). The hand-typed counts on `docs/index.md:18` ("112 packages", "13,000+ tests", "13 database providers", and the "8 cache providers" banner) and "Packages (112)" at `:52` are already tracked by open issue #1372 and are not part of this finding.

8. **Minor** — `docs/features/query-caching.md`, "Related Documentation" (line 293), point 4 (placement): the page is a "Mixed" feature page under `docs/features/` with Overview, Configuration, How It Works, Performance, Troubleshooting in one page. Its Related Documentation links no neighbouring pages: it points at `README.md#caching`, the issue (#291) and the CHANGELOG. No ADR records the query-caching design: `docs/architecture/adr/index.md` has one caching row, ADR-003 "Caching Strategy for Handler Resolution" (line 15), which covers handler wrapper caches, not query caching. Reference, explanation and troubleshooting are not split per the SKILL section 4 method.

## Informational (not findings)

- Rule (a) point 1 emoji check: `query-caching.md` has 0 emoji lines (Unicode ranges U+2600-U+27BF, U+23F3, U+2B50, surrogate pairs). The page uses tables and code blocks well (6 tables); the gap is diagrams only (finding 5).
- Not a finding: the decision "no CacheOrchestrator" is encoded in code and the knowledge record `docs/knowledge/issues/13.md` (destinations `executable-rule`); the record itself says a one-line note is optional. Whether it also deserves a sentence on the new caching feature page (finding 3) is left to the writer of that page; ADR-003 does not mention an orchestrator (searched "Orchestrator" in `003-caching-strategy.md`).
- `query-caching.md` identifiers checked and present: `AddQueryCaching` (`QueryCachingExtensions.cs:68,108`), `UseQueryCaching` (`:170`), `ExcludeType<TEntity>` (`QueryCacheOptions.cs:110`), `ThrowOnCacheErrors`, `ExcludedEntityTypes`, `KeyPrefix`, `DefaultExpiration`, `AddEncinaMemoryCache`, `AddEncinaHybridCache`, `AddEncinaRedisCache`. The default `KeyPrefix` literal and the other defaults were not re-checked against `QueryCacheOptions.cs`.
- Not run: lychee, markdownlint and `cov-docs-render` (point 2 to 5 scope only; no covref present on the reviewed pages).

## Lessons for the pipeline

- A rejected-reasoned issue with no pages still has a rule (a) target: the pages of the area the decision protects (here caching). Search the area's `src/` folders for READMEs and `docs/` for a feature page, tutorial and guide entry before concluding nothing is in scope.
- Compare a README sample's attribute properties with the attribute class (get-only computed `Duration` vs init `DurationSeconds`); names exist, so symbol-existence checks pass it.
