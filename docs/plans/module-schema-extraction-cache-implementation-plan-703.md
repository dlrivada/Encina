# Implementation Plan: In-memory cache for module SQL schema extraction

> **Issue**: [#703](https://github.com/dlrivada/Encina/issues/703)
> **Type**: Feature
> **Complexity**: Low (7 phases, 1 package touched, 2 pull requests)
> **Estimated Scope**: ~150-250 lines of production code + ~700 lines of tests (13 files in the larger pull request)

---

## Summary

`SqlSchemaExtractor.ExtractSchemas(string? sql)` (`src/Encina/Modules/Isolation/SqlSchemaExtractor.cs`) runs up to five source-generated regexes over the SQL text each time `ModuleSchemaRegistry.ValidateSqlAccess(moduleName, sql)` is called. The same SQL text always yields the same schema set, so the extraction is a pure function and can be cached. The plan adds a bounded, thread-safe, in-process cache keyed by the **SQL text itself**, in `Encina` core, behind the unchanged `ExtractSchemas` signature, and gates the whole change on a benchmark of the current cost.

Code facts that scope the work:

- **Callers**: `ModuleSchemaRegistry.ValidateSqlAccess` and `SqlSchemaExtractor.ValidateSchemaAccess` call `ExtractSchemas`; the runtime callers (verified on main) are `SchemaValidatingCommand` in `Encina.ADO.SqlServer|PostgreSQL|MySQL` and in `Encina.Dapper.SqlServer|PostgreSQL|MySQL` (six packages, each with a private `ValidateSchemaAccess()` that calls `ValidateSqlAccess`) and `ModuleSchemaValidationInterceptor` in `Encina.EntityFrameworkCore`; `Encina.MongoDB` only registers `IModuleSchemaRegistry` and has no SQL wrapper. Each caller validates **only** when `ModuleIsolationOptions.Strategy == DevelopmentValidationOnly` (the default) and a module is executing. Under `SchemaWithPermissions` and `ConnectionPerModule` the extraction never runs. The hot path is therefore every command of a default-strategy application, in development and, if left on, in production.
- **Fail-open on timeout**: when a regex times out (1,000 ms), `ExtractSchemas` catches `RegexMatchTimeoutException` and returns the schemas found so far. A cache that stored that partial result would make an incomplete answer permanent for that SQL. The cache must not store a result produced after a timeout.
- **Immutable result**: `ExtractSchemas` returns `IReadOnlySet<string>` backed by an `ImmutableHashSet<string>` (case-insensitive), which can be shared between callers without copying; the cache stores that same instance.
- **Not hash-keyed**: the issue's original sketch keyed by `sql.GetHashCode()`; a 32-bit collision in a module isolation gate would return another statement's schemas. The key is the SQL string.

Provider category: none (the extraction works on text and is shared by every database provider that validates). Affected package: `Encina` (`Modules/Isolation`). Estimated size: two pull requests, 1 file (the benchmark) and 13 files (cache, tests, manifest, docs), each far below 1,500 lines (see Estimated File Count).

---

## Design Choices

<details>
<summary><strong>1. How the cache is bounded (Dilemma D1)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Least-recently-used eviction at a fixed size** | Predictable memory; keeps the hot set when the statement set is larger than the bound | A small LRU structure to write and test; a lock or per-access bookkeeping on the hot path |
| **B) `MemoryCache` with a size limit and sliding expiry** | Reuses a framework type | `Encina` core has no reference to `Microsoft.Extensions.Caching.Memory` today (it references DI, Hosting, Logging, Options, LanguageExt, `System.IO.Hashing`); adds a package to core for one cache |
| **C) A fixed maximum entry count in a `ConcurrentDictionary`; when full, the whole dictionary is replaced by a fresh one (generational reset)** | Simplest; no dependency; lock-free reads; memory is bounded; self-heals after a burst of distinct SQL | A reset recomputes the hot set once; thrashes when the hot set is larger than the bound |

### Chosen Option: **C — fixed maximum with generational reset** (recommended, pending the maintainer)

### Rationale

C wins when the statement set is mostly parameterized SQL (small and stable), which is what the module isolation use case produces, and when core should stay free of new dependencies. A wins for applications with a large steady-state statement set, where a reset would recompute the hot set repeatedly; B wins if the maintainer is willing to add `Microsoft.Extensions.Caching.Memory` to core. Two guards apply to every option, with their values registered as D6: SQL text longer than a fixed limit is extracted without caching, so one huge statement cannot pin memory, and the entry count is capped.

**Class:** Dilemma

</details>

<details>
<summary><strong>2. Whether validation results are cached (Dilemma D2)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Cache only the extraction; `ValidateSqlAccess` recomputes the set check each time** | Always correct after modules are registered or changed (`RegisterModule` mutates `_modules`); the check is a few set lookups once schemas are known | Leaves the (small) set work on the hot path |
| **B) Also cache `(moduleName, sql)` results and clear them on any registry change** | Saves the remaining set work | Invalidation logic on a security gate; a missed invalidation returns a stale allow |

### Chosen Option: **A — cache the extraction only** (recommended, pending the maintainer)

### Rationale

A is correct by construction because the registry state is never part of the cached value. B wins only if the benchmark shows the set check dominates the remaining cost, and then the invalidation must be proven by tests. `RegisterModule` is private and runs from the constructor today, so the registry is immutable after construction, which makes B safer than the issue assumed; the benefit would still have to be measured.

**Class:** Dilemma

</details>

<details>
<summary><strong>3. What benchmark result justifies the cache (Dilemma D3)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) A cache hit is at least 2x faster than uncached extraction and allocates nothing, on the SQL samples already used by `SqlSchemaExtractorTests` (simple, join, CTE, long statement)** | Objective and cheap to run; a lookup still hashes the whole string, so a smaller gain means the cache is not worth its code | The 2x figure is a judgment |
| **B) Extraction accounts for a stated share (for example 5 percent) of a full command round trip in `SchemaValidatingCommand`** | Ties the change to real request time | Needs an end-to-end benchmark with a database; noisy |
| **C) No gate; ship the cache** | No benchmark cost | The issue itself says the real cost must be measured before the cache is justified |

### Chosen Option: **A — hit at least 2x faster with no allocation** (recommended, pending the maintainer)

### Rationale

B wins when the maintainer wants proof in end-to-end terms; C wins only if the maintainer accepts the cache without data. If the gate fails, the issue closes with the benchmark as its result and no production change merges.

**Class:** Dilemma

</details>

<details>
<summary><strong>4. Key and what is never cached</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Key = the SQL string (ordinal); a result produced after a regex timeout, null SQL and whitespace SQL are never cached** | No collision is possible; the cached value always equals what a full uncached extraction returns | Keys hold the full SQL text (bounded by D6) |
| **B) Key = `GetHashCode()` or a 64-bit hash** | Smaller keys | A collision returns another statement's schemas in a security gate (fails open); caching a post-timeout partial set freezes the fail-open fallback |

### Chosen Option: **A — SQL text as key, no partial results**

### Rationale

Only A keeps the isolation gate from answering with another statement's schemas or with an incomplete set. B violates the cited rule, so no other option satisfies it. Distributed caching through `ICacheProvider` was rejected as not applicable: recomputing the extraction costs less than a network fetch and would make core depend on `Encina.Caching` (the reverse of the layering).

**Class:** Settled

**Settled by:** `AGENTS.md` §3 (compliance and security gates fail closed: a hash collision or a cached partial result would let a gate allow what a full extraction would deny).

</details>

<details>
<summary><strong>5. Scope of the cache: process-wide static or per instance (Dilemma D5)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Process-wide `internal static` cache inside `SchemaExtractionCache`, used by the public static `ExtractSchemas`** | `ExtractSchemas` is a public static method, so every caller (the registry, `SqlSchemaExtractor.ValidateSchemaAccess`, the six ADO and Dapper wrappers through the registry) benefits with no signature change; equal SQL gives equal schemas on every instance, so sharing loses nothing | Static state: the test-only `Clear()` hook and cross-test coupling (tests clear before and after) |
| **B) A cache instance owned by `ModuleSchemaRegistry` (injected or created in its constructor), with `ExtractSchemas` left uncached** | No static state, no `Clear()` hook; tests isolate by construction | The static `ExtractSchemas` and `SqlSchemaExtractor.ValidateSchemaAccess` stay uncached unless their signatures change (a breaking change, acceptable pre-1.0); each registry has its own cache and duplicates entries; more wiring (DI and a constructor parameter) |

### Chosen Option: **A — process-wide static cache** (recommended, pending the maintainer)

### Rationale

A is smallest and keeps the public signature, which is why it is recommended; B wins if the maintainer prefers no mutable static state or wants the cache lifetime tied to the registry. Both satisfy `AGENTS.md` §3 and §1 equally, so this is not Settled.

**Class:** Dilemma

</details>

<details>
<summary><strong>6. Bound values and worst-case memory (Dilemma D6)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) 4,096 entries and 8,192 characters per key** | Covers the 5,000-character benchmark sample and large generated statements; entry count far above the statement set of a typical parameterized application | Worst case about 4,096 x 8,192 chars x 2 bytes = 67,108,864 bytes (64 MiB) of key text, plus a small immutable set per entry (a handful of short schema names) |
| **B) 1,024 entries and 4,096 characters per key** | Worst case about 8 MiB of key text | The 5,000-character sample and large statements are never cached; more resets on applications with many distinct statements |

### Chosen Option: **A — 4,096 entries, 8,192 characters** (recommended, pending the maintainer)

### Rationale

The 64 MiB figure is the theoretical ceiling that needs 4,096 distinct statements all near the limit; typical parameterized applications hold a few hundred short statements. B wins if the maintainer wants a hard memory ceiling an order of magnitude lower. SQL longer than the key limit is extracted without caching. The constants are the same for both options; only their values differ.

**Class:** Dilemma

</details>

<details>
<summary><strong>7. Expiry, concurrent reset and duplicate computation</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) No TTL; the generational reset is the only eviction; a reset while readers hold the old dictionary is harmless (they finish on immutable, correct values; the reference swap is atomic); two threads that miss on the same SQL both compute and the second add is ignored; entries are counted with an `Interlocked` counter instead of `ConcurrentDictionary.Count` (which takes every lock), so the bound may overshoot by a few entries under a race** | An entry is a pure function of its key and never goes stale; no timer or timestamp on a hot path; no locks | The bound is approximate |
| **B) A time-to-live per entry** | Frees memory of statements that stopped being used | Timestamps and a sweeper for no correctness gain |

### Chosen Option: **A — no TTL, lock-free, approximate bound**

### Rationale

None of these choices can change a returned value: readers only ever see a hit or a miss, and duplicate computation yields equal sets. They are implementation details of the cache with no observable effect on the isolation decision.

**Class:** Trivial

</details>

---

## Implementation Phases

### Phase 1: Benchmark the current extraction (go/no-go)

<details>
<summary>Tasks</summary>

1. `tests/Encina.BenchmarkTests/Encina.Benchmarks/SqlSchemaExtractorBenchmarks.cs`: `ExtractSchemas` on four samples (simple select, join with brackets and quotes, statement with a CTE and subquery, a 5,000-character statement), with `[MemoryDiagnoser]`, registered through the existing `BenchmarkSwitcher` program; verify the filter with `--list flat --filter "*SqlSchemaExtractor*"` and run with `--job short`; results go to `artifacts/performance/`.
2. Report the numbers in the pull request. Apply the gate of D3: if it fails, stop here and close the issue with the data.

</details>

<details>
<summary>Prompt for AI Agents — Phase 1</summary>

```text
CONTEXT: The issue asks to measure the regex cost before caching. SqlSchemaExtractor.ExtractSchemas is a public static method in src/Encina/Modules/Isolation/SqlSchemaExtractor.cs.
TASK: Add the benchmark class with the four SQL samples and run it; record the results and apply the D3 gate.
KEY RULES: BenchmarkSwitcher.FromAssembly(...).Run(args, config), never BenchmarkRunner.Run<T>(); never return IQueryable; outputs under artifacts/performance/; no hard-coded absolute paths.
REFERENCE FILES: tests/Encina.BenchmarkTests/Encina.Benchmarks/MediatorBenchmarks.cs, tests/Encina.BenchmarkTests/Encina.Benchmarks/CacheOptimizationBenchmarks.cs, tests/Encina.UnitTests/Core/Modules/Isolation/SqlSchemaExtractorTests.cs.
```

</details>

### Phase 2: The bounded cache component

<details>
<summary>Tasks</summary>

1. `src/Encina/Modules/Isolation/SchemaExtractionCache.cs` (`internal static` or `internal sealed`): fields `ConcurrentDictionary<string, IReadOnlySet<string>>` held in a volatile reference, `MaxEntries` and `MaxSqlLength` constants (values per D6, recommended 4,096 and 8,192), an `Interlocked` entry counter (D7); `bool TryGet(string? sql, out IReadOnlySet<string> schemas)` (returns false for null or whitespace), `void Add(string? sql, IReadOnlySet<string> schemas)` (no-op for null, whitespace or SQL longer than `MaxSqlLength`; throws `ArgumentNullException` for a null `schemas`; when the count reaches `MaxEntries` replaces the dictionary with a new one, D1 option C), `internal static void Clear()` for tests (D5 option A).
2. Keep `Encina.UnitTests`, `Encina.GuardTests` and `Encina.PropertyTests` access through the existing `InternalsVisibleTo` entries of `Encina.csproj` (`Encina.LoadTests` has none, so the load test uses only the public `ExtractSchemas`).

</details>

<details>
<summary>Prompt for AI Agents — Phase 2</summary>

```text
CONTEXT: Phase 1's gate passed. D1 is implemented as the maintainer answered, the generational reset until then.
TASK: Create SchemaExtractionCache with TryGet, Add and Clear as described (bound values per D6 as the maintainer answered); keep every method at cyclomatic complexity of 3 or less.
KEY RULES: The key is the SQL string, compared ordinally; never store null, whitespace, over-long SQL or a result flagged as incomplete; no GetHashCode keys; thread safe without locks on the read path.
REFERENCE FILES: src/Encina/Modules/Isolation/SqlSchemaExtractor.cs, src/Encina/Modules/Isolation/ModuleSchemaRegistry.cs.
```

</details>

### Phase 3: Integrate into `ExtractSchemas`

<details>
<summary>Tasks</summary>

1. Split `ExtractSchemas` into a public wrapper (null/whitespace check, then `ExtractCached(sql, ExtractCore)`) and a private `ExtractCore(string sql)` returning `(IReadOnlySet<string> Schemas, bool Completed)` that holds the current regex logic and returns `Completed = false` from the `RegexMatchTimeoutException` handler. The testing seam is `internal static IReadOnlySet<string> ExtractCached(string sql, Func<string, (IReadOnlySet<string> Schemas, bool Completed)> extractor)`: it looks up the cache, calls `extractor` on a miss and stores only a `Completed` result. The regex timeout (1,000 ms) is a constant in each generated-regex attribute and cannot be triggered cheaply, so the timeout test passes a stub extractor that returns `Completed = false`; the production wrapper passes the static method group (no delegate allocation).
2. `ValidateSqlAccess`, `ValidateSchemaAccess` and every caller stay unchanged (D2 option A). `ExtractTableReferences` is not cached (violation reporting only).
3. Update the XML remarks of `SqlSchemaExtractor` to describe the cache and the timeout rule.

</details>

<details>
<summary>Prompt for AI Agents — Phase 3</summary>

```text
CONTEXT: SqlSchemaExtractor catches RegexMatchTimeoutException and returns the partial set; that result must never be cached.
TASK: Refactor ExtractSchemas into the wrapper, the internal ExtractCached seam and ExtractCore returning (Schemas, Completed); wire the cache, keep the public signature and every observable result identical.
KEY RULES: No behavior change for any input; no [Obsolete]; update XML docs; the property test (Phase 6) proves cached equals uncached.
REFERENCE FILES: src/Encina/Modules/Isolation/SqlSchemaExtractor.cs, src/Encina.ADO.SqlServer/Modules/SchemaValidatingCommand.cs, tests/Encina.UnitTests/Core/Modules/Isolation/SqlSchemaExtractorTests.cs.
```

</details>

### Phase 4: Cross-Cutting Integration

<details>
<summary>Tasks</summary>

1. Module isolation: the decision of the gate is preserved exactly (same result as uncached); this is the integration.
2. OpenTelemetry hit and miss counters: deferred and only if the Phase 1 data shows a need (ModuleIsolation range 1900-1999 and `ModuleMetrics` exist in `src/Encina/Modules/Diagnostics/`); no issue is opened because the work is conditional on the benchmark.
3. Everything else: not applicable (matrix below). No distributed lock, tenancy or audit dimension: schemas do not depend on the tenant.

</details>

<details>
<summary>Prompt for AI Agents — Phase 4</summary>

```text
CONTEXT: See the matrix at the end of this plan.
TASK: Add no instrumentation unless the benchmark pull request shows a need; confirm nothing else is required.
KEY RULES: Hot path: no logging, no allocation on a hit; no new EventIds.
REFERENCE FILES: src/Encina/Modules/Diagnostics/ModuleMetrics.cs, src/Encina/Diagnostics/EventIdRanges.cs.
```

</details>

### Phase 5: Observability

<details>
<summary>Tasks</summary>

1. No `ActivitySource`, no log messages (hot path, nothing worth logging).
2. If Phase 4 triggers the conditional counters: two instruments on the existing module metrics class, tagged by outcome only, and an EventId range check (none expected).

</details>

<details>
<summary>Prompt for AI Agents — Phase 5</summary>

```text
CONTEXT: The module metrics class exists; the cache is a hot path.
TASK: Do nothing unless the conditional counters were approved; then add them on ModuleMetrics with an outcome tag.
KEY RULES: No SQL text in any tag or log; no EventId outside EventIdRanges.ModuleIsolation (1900-1999).
REFERENCE FILES: src/Encina/Modules/Diagnostics/ModuleMetrics.cs, src/Encina/Modules/Diagnostics/ModuleLog.cs.
```

</details>

### Phase 6: Testing — 7 test types

<details>
<summary>Tasks</summary>

Per-flag targets for the files this work adds or changes, entered in `.github/coverage-manifest/Encina.json` **in pull request 2, the same pull request as the tests that meet them** (a manifest entry never precedes its tests). Each target has a one-sentence justification in the manifest's `justifications`; targets never go down, and the baseline of `SqlSchemaExtractor.cs` is measured at work start (`dotnet test tests\Encina.<Flag>Tests --collect "XPlat Code Coverage"` per flag, then `coverage-report.cs`) and the target is set to the measured value or higher, never below it.

| File | unit | guard | contract | property | integration |
|------|:---:|:---:|:---:|:---:|:---:|
| `Modules/Isolation/SchemaExtractionCache.cs` | 100 | 80 | 0 | 80 | 0 |
| `Modules/Isolation/SqlSchemaExtractor.cs` | 95 | 25 | 0 | 40 | 0 |

| File and flag | Justification |
|---------------|---------------|
| `SchemaExtractionCache.cs` unit 100 | The file has a handful of branches (hit, miss, too long, null, whitespace, reset at the bound), every one reachable from a unit test through the `InternalsVisibleTo` of `Encina.UnitTests`. |
| `SchemaExtractionCache.cs` guard 80 | Guard tests reach the null, whitespace and null-`schemas` branches of `TryGet` and `Add` (the type is internal and `Encina.GuardTests` has `InternalsVisibleTo`); the reset and eviction lines are unit-only. |
| `SchemaExtractionCache.cs` contract 0 | An internal type has no public contract; `ExtractSchemas` keeps its signature and `IModuleSchemaRegistry` stays covered by the existing contract tests. |
| `SchemaExtractionCache.cs` property 80 | The cached-equals-uncached property drives hit, miss and store for generated SQL; the over-long and reset branches are unit-only. |
| `SchemaExtractionCache.cs` integration 0 | Pure in-memory code with no external dependency; the ADO and Dapper wrappers' existing integration tests keep exercising the wiring (justification file below). |
| `SqlSchemaExtractor.cs` unit 95 | Every regex branch and the new wrapper, `ExtractCached` and `ExtractCore` are reachable from unit tests; the remaining 5 percent is generated-regex plumbing. |
| `SqlSchemaExtractor.cs` guard 25 | Guard tests reach only the null and whitespace early returns of the three public methods; extraction lines belong to the unit flag. |
| `SqlSchemaExtractor.cs` contract 0 | The public signatures are unchanged and no interface lives in the file, so there is no new contract; the existing contract tests of `IModuleSchemaRegistry` keep running. |
| `SqlSchemaExtractor.cs` property 40 | The property test reaches the extraction through the generated-SQL equivalence of cached and uncached results, covering the main patterns but not every keyword branch. |
| `SqlSchemaExtractor.cs` integration 0 | Pure text processing; same justification as the cache file. |

1. **Unit** (`tests/Encina.UnitTests/Core/Modules/Isolation/`; the existing `SqlSchemaExtractorTests.cs` is extended and `SchemaExtractionCacheTests.cs` is new): hit on equal SQL; two different SQL texts never share an entry (pairs of strings that differ only in case, in whitespace or in a trailing character each return their own schemas, because the key is compared ordinally, never by a hash); entry bound and generational reset; over-long SQL not cached; null and whitespace not cached; a timeout result not cached (`ExtractCached` with a stub extractor returning `Completed = false`, asserted to miss again on the second call).
2. **Guard** (`tests/Encina.GuardTests/Core/Modules/`; `SqlSchemaExtractorGuardTests.cs` extended and `SchemaExtractionCacheGuardTests.cs` new): null, empty and whitespace SQL for `TryGet` and `Add`, a null `schemas` throwing `ArgumentNullException`, and the existing null and whitespace handling of the three `SqlSchemaExtractor` methods.
3. **Contract**: not applicable (target 0 above). No public API is added, so AGENTS.md section 9 asks for no contract test, and no `.md` justification is written for contract (never allowed); the existing contract tests of `IModuleSchemaRegistry` keep running unchanged.
4. **Property** (`tests/Encina.PropertyTests/Core/`, new file): for any generated SQL, cached equals uncached; a second call returns the same instance.
5. **Integration**: `.md` justification (pure in-memory, non-database feature): `tests/Encina.IntegrationTests/Core/Modules/Isolation/SchemaExtractionCache.md`; the ADO, Dapper and EF wrappers' existing integration tests keep covering the wiring.
6. **Load** (`tests/Encina.LoadTests/Modules/SchemaExtractionLoadTests.cs`, new; public `ExtractSchemas` only): concurrent calls from many tasks over a mix of repeated and distinct SQL, asserting the results equal the uncached ones and the retained memory stays bounded; run locally with `DOTNET_JitObjectStackAllocationConditionalEscape=0`.
7. **Benchmark**: the Phase 1 class (created in pull request 1) is extended in pull request 2 with the cached variants; cached versus uncached comparison documented.

</details>

<details>
<summary>Prompt for AI Agents — Phase 6</summary>

```text
CONTEXT: Per-flag coverage targets live in .github/coverage-manifest/Encina.json; targets never go down; tests execute real code; the cache is static, so tests call SchemaExtractionCache.Clear() for isolation.
TASK: Write the unit, guard, property, load and benchmark tests, the integration justification file and the manifest entries with their one-sentence justifications; measure every flag and the CRAP table for the changed methods.
KEY RULES: Shouldly; no Thread.Sleep; a justification file only for integration of a non-database feature, never for contract; the key test must prove two different SQL texts never share an entry; manifest entries land in the same pull request as the tests.
REFERENCE FILES: tests/Encina.UnitTests/Core/Modules/Isolation/SqlSchemaExtractorTests.cs, tests/Encina.GuardTests/Core/Modules/SqlSchemaExtractorGuardTests.cs, tests/Encina.PropertyTests/Core, tests/Encina.LoadTests/Messaging (shape of an existing load test).
```

</details>

### Phase 7: Documentation & Finalization

<details>
<summary>Tasks</summary>

1. XML docs updated (`SqlSchemaExtractor` remarks: cache, bound, timeout rule).
2. A short note in the module isolation guide (via `docs-writer`): validation runs in `DevelopmentValidationOnly` only, and what the cache does.
3. `changelog.d/703-module-schema-extraction-cache.changed.md` (only if the gate passed).
4. No ADR (no new dependency) unless D1 picks `MemoryCache` for core (ADR number assigned when the ADR pull request opens).
5. `dotnet build --configuration Release` with zero warnings; all tests green; every flag at its target; the benchmark result attached to the pull request.

</details>

<details>
<summary>Prompt for AI Agents — Phase 7</summary>

```text
CONTEXT: Code and tests are done; docs are English with no hand-typed figures.
TASK: Update the XML docs, the module isolation guide note and the changelog fragment through the specialists that own them.
KEY RULES: Never edit CHANGELOG.md; state that the cache keys on the SQL text and never on a hash.
REFERENCE FILES: changelog.d/README.md, .claude/skills/encina-docs/SKILL.md, docs/ (module isolation guide).
```

</details>

---

## Research

### Relevant Standards & Specifications

| Standard / Spec | Relevance | Notes |
|-----------------|-----------|-------|
| `AGENTS.md` §3 | Gates fail closed | No hash keys, no cached partial result |
| `AGENTS.md` §9 | Benchmarks for hot paths; CRAP at or below 10 | Phase 1 gate; small methods |
| `ModuleIsolationStrategy` | Where the extraction runs at all | Only `DevelopmentValidationOnly` |
| ADR-024 | SQLite prefix-style tables are matched by the extractor | Behavior preserved |

### Existing Encina Infrastructure to Leverage

| Component | Location | Usage in this feature |
|-----------|----------|-----------------------|
| `SqlSchemaExtractor` | `src/Encina/Modules/Isolation/SqlSchemaExtractor.cs` | The cached function |
| `ModuleSchemaRegistry`, `IModuleSchemaRegistry` | `src/Encina/Modules/Isolation/` | `ValidateSqlAccess` consumer, unchanged |
| `SchemaValidatingCommand` | `src/Encina.ADO.SqlServer|PostgreSQL|MySQL/Modules/` | Runtime caller on every command |
| `ModuleMetrics`, `ModuleLog` | `src/Encina/Modules/Diagnostics/` | Conditional instruments |
| `SqlSchemaExtractorTests`, `SqlSchemaExtractorGuardTests` | `tests/Encina.UnitTests/Core/Modules/Isolation/`, `tests/Encina.GuardTests/Core/Modules/` | Existing coverage to extend |
| `Encina.Benchmarks` project | `tests/Encina.BenchmarkTests/Encina.Benchmarks` | Home of the benchmark |

### Event ID Allocation

| Package | Range | Notes |
|---------|-------|-------|
| `Encina` (module isolation) | none | The registered range 1900-1999 stays untouched; used only if the conditional counters add log messages |

### Estimated File Count

| Category | Files | Notes |
|----------|------:|-------|
| Production code | 2 | `SchemaExtractionCache.cs` (new), `SqlSchemaExtractor.cs` (edit); no public API change, so no `PublicAPI` file |
| Coverage manifest | 1 | `Encina.json` entries, in pull request 2 |
| Tests | 6 | unit (extend and new), guard (extend and new), property (new), load (new) |
| Benchmark | 1 | `SqlSchemaExtractorBenchmarks.cs`: created in pull request 1, extended in pull request 2 |
| Justifications | 1 | integration only (contract is not applicable, never a `.md`) |
| Documentation | 2 | guide note, changelog fragment |
| **Pull request 1** | **1** | the benchmark |
| **Pull request 2** | **13** | 2 production, 1 manifest, 6 tests, 1 benchmark extension, 1 justification, 2 documentation (about 250 production lines plus about 700 test lines plus docs, under 1,500) |
| **Distinct files** | **13** | 14 file touches; the benchmark is counted in both pull requests |

No existing test calls a changed signature (the public `ExtractSchemas` signature is unchanged and `ExtractCore` is new), so no caller edits are added. Neither pull request uses a type created by a later one: pull request 2 extends the benchmark that pull request 1 created.

---

## Combined AI Agent Prompts

<details>
<summary>Combined prompt for all phases</summary>

```text
PROJECT CONTEXT: Encina is a pre-1.0 .NET 10 / C# 14 library. Module isolation validates SQL against allowed schemas in DevelopmentValidationOnly mode; SqlSchemaExtractor.ExtractSchemas runs regexes on every command. The module isolation check is a gate and must fail closed. Zero warnings, no backward compatibility.

IMPLEMENTATION OVERVIEW: Phase 1 benchmark and go/no-go gate (D3); Phase 2 bounded SchemaExtractionCache (D1); Phase 3 ExtractSchemas wrapper plus ExtractCore with a completed flag; Phase 4 and 5 no instrumentation unless the data demands it; Phase 6 tests by flag including a collision test, a property test of cached versus uncached and a concurrent load test; Phase 7 docs. D2 stays at extraction-only caching; D5 (static scope) and D6 (4,096 entries, 8,192 characters) are recommended values. Dilemmas are implemented as the maintainer answers, the recommended options until then.

KEY PATTERNS: Key is the SQL string compared ordinally, never GetHashCode; never cache null, whitespace, over-long SQL or a result produced after a regex timeout; immutable shared results; no allocation on a hit; ConcurrentDictionary with a generational reset; BenchmarkSwitcher only; CRAP at or below 10; targets never go down.

REFERENCE FILES: src/Encina/Modules/Isolation/SqlSchemaExtractor.cs, src/Encina/Modules/Isolation/ModuleSchemaRegistry.cs, src/Encina.ADO.SqlServer/Modules/SchemaValidatingCommand.cs, tests/Encina.UnitTests/Core/Modules/Isolation/SqlSchemaExtractorTests.cs, tests/Encina.BenchmarkTests/Encina.Benchmarks/MediatorBenchmarks.cs, .github/coverage-manifest/Encina.json.
```

</details>

---

## Cross-Cutting Integration Matrix

| # | Function | Status | Notes |
|---|----------|--------|-------|
| 1 | Caching | ✅ | An in-process cache inside core, not `ICacheProvider`: the result is identical across instances and cheaper to recompute than to fetch. |
| 2 | OpenTelemetry | ⏭️ | Hit and miss counters only if the Phase 1 data shows a need; conditional, so no issue is opened now. |
| 3 | Structured Logging | ❌ | Hot path with nothing worth logging. |
| 4 | Health Checks | ❌ | The feature introduces no dependency to check. |
| 5 | Validation | ❌ | No new input; null and whitespace SQL keep the current behavior. |
| 6 | Resilience | ❌ | No external call. |
| 7 | Distributed Locks | ❌ | In-process, lock-free; a duplicate computation under a race is harmless. |
| 8 | Transactions | ❌ | No transactional state. |
| 9 | Idempotency | ❌ | The extraction is a pure function. |
| 10 | Multi-Tenancy | ❌ | Schema extraction does not depend on the tenant. |
| 11 | Module Isolation | ✅ | The cache preserves the isolation decision exactly (same result as uncached, nothing partial stored). |
| 12 | Audit Trail | ❌ | No audit events are generated by this feature. |

---

## Prerequisites & Dependencies

- **Benchmark first**: Phase 1 is its own pull request; Phases 2 to 7 start only if the D3 gate passes.
- **Related, not blocking**: the module producer finding recorded for #746 (only the EF package sets `IModuleExecutionContext`), because without a module the ADO and Dapper wrappers skip validation and the extraction never runs there.
- **Behavior note**: on a regex timeout the extractor returns a partial set (documented as acceptable for development-time validation); this plan does not change that and does not cache it. That fail-open behavior of a validation gate sits against AGENTS.md section 3; follow-up issue to be opened (draft: artifacts/decisions/plan-batch/followups/module-schema-extractor-timeout-fail-open.md).

---

## Next Steps

1. Ask the maintainer D1, D2, D3, D5 and D6 when work on #703 starts; record the answers in `## Maintainer Decisions`.
2. Pull request 1: the benchmark (Phase 1, 1 file) with its numbers; stop if the gate fails.
3. Pull request 2: Phases 2 to 7 (13 files).
