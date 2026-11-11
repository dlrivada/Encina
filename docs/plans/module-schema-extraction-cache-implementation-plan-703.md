# Implementation Plan: In-memory cache for module SQL schema extraction

> **Issue**: [#703](https://github.com/dlrivada/Encina/issues/703)
> **Type**: Feature
> **Complexity**: Low (7 phases, 1 package touched, 2 pull requests)
> **Estimated Scope**: ~150-250 lines of production code + ~700 lines of tests

---

## Summary

`SqlSchemaExtractor.ExtractSchemas(string? sql)` (`src/Encina/Modules/Isolation/SqlSchemaExtractor.cs`) runs up to five source-generated regexes over the SQL text each time `ModuleSchemaRegistry.ValidateSqlAccess(moduleName, sql)` is called. The same SQL text always yields the same schema set, so the extraction is a pure function and can be cached. The plan adds a bounded, thread-safe, in-process cache keyed by the **SQL text itself**, in `Encina` core, behind the unchanged `ExtractSchemas` signature, and gates the whole change on a benchmark of the current cost.

Code facts that scope the work:

- **Callers**: `ModuleSchemaRegistry.ValidateSqlAccess` and `SqlSchemaExtractor.ValidateSchemaAccess` call `ExtractSchemas`; the runtime caller is `SchemaValidatingCommand` in `Encina.ADO.SqlServer|PostgreSQL|MySQL` (the private `ValidateSchemaAccess()` method of each; whether the Dapper, EF Core and MongoDB packages have an equivalent wrapper is checked when the work starts), which validates **only** when `ModuleIsolationOptions.Strategy == DevelopmentValidationOnly` (the default) and a module is executing. Under `SchemaWithPermissions` and `ConnectionPerModule` the extraction never runs. The hot path is therefore every command of a default-strategy application, in development and, if left on, in production.
- **Fail-open on timeout**: when a regex times out (1,000 ms), `ExtractSchemas` catches `RegexMatchTimeoutException` and returns the schemas found so far. A cache that stored that partial result would make an incomplete answer permanent for that SQL. The cache must not store a result produced after a timeout.
- **Immutable result**: `ExtractSchemas` returns an `ImmutableHashSet<string>`, which can be shared between callers without copying.
- **Not hash-keyed**: the issue's original sketch keyed by `sql.GetHashCode()`; a 32-bit collision in a module isolation gate would return another statement's schemas. The key is the SQL string.

Provider category: none (the extraction works on text and is shared by every database provider that validates). Affected package: `Encina` (`Modules/Isolation`). Estimated size: about 12 files, one pull request after the benchmark pull request.

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

C wins when the statement set is mostly parameterized SQL (small and stable), which is what the module isolation use case produces, and when core should stay free of new dependencies. A wins for applications with a large steady-state statement set, where a reset would recompute the hot set repeatedly; B wins if the maintainer is willing to add `Microsoft.Extensions.Caching.Memory` to core. Two guards apply to every option: SQL text longer than a fixed limit (proposed 8,192 characters) is extracted without caching, so one huge statement cannot pin memory, and the entry count is capped (proposed 4,096 entries).

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
<summary><strong>4. Key, scope, timeouts and thread safety</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Key = the SQL string (ordinal); process-wide `internal static` cache inside `Encina`; results produced after a regex timeout, null and whitespace SQL are never cached; `ConcurrentDictionary`, duplicate computation under a race is harmless** | No collision is possible; identical results across instances so no distributed cache is needed; behavior of every caller is unchanged | Static state in tests (needs an internal reset hook) |
| **B) Key = `GetHashCode()` or a 64-bit hash** | Smaller keys | A collision returns another statement's schemas in a security gate |
| **C) Distributed cache through `ICacheProvider`** | Shared across instances | Recomputing costs less than a network fetch; makes core depend on `Encina.Caching` (the reverse of the layering) |

### Chosen Option: **A — SQL text as key, in-process, no partial results**

### Rationale

Equal SQL gives equal schemas on every instance, so a local cache loses nothing. A partial result after a timeout is the fail-open fallback of an extraction that did not finish; caching it would freeze that fallback.

**Class:** Settled

**Settled by:** `AGENTS.md` §3 (compliance and security gates fail closed), §1 (pay for what you use; layering of core), and the pure-function property of `SqlSchemaExtractor` documented in its own remarks.

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

1. `src/Encina/Modules/Isolation/SchemaExtractionCache.cs` (`internal static` or `internal sealed`): fields `ConcurrentDictionary<string, IReadOnlySet<string>>` held in a volatile reference, `MaxEntries` (4,096) and `MaxSqlLength` (8,192) constants; `bool TryGet(string sql, out IReadOnlySet<string> schemas)`, `void Add(string sql, IReadOnlySet<string> schemas)` (skips when too long; when the count reaches `MaxEntries` replaces the dictionary with a new one, D1 option C), `internal static void Clear()` for tests.
2. Keep `Encina.UnitTests` access through the existing `InternalsVisibleTo`.

</details>

<details>
<summary>Prompt for AI Agents — Phase 2</summary>

```text
CONTEXT: Phase 1's gate passed. D1 is implemented as the maintainer answered, the generational reset until then.
TASK: Create SchemaExtractionCache with TryGet, Add and Clear as described; keep every method at cyclomatic complexity of 3 or less.
KEY RULES: The key is the SQL string, compared ordinally; never store null, whitespace, over-long SQL or a result flagged as incomplete; no GetHashCode keys; thread safe without locks on the read path.
REFERENCE FILES: src/Encina/Modules/Isolation/SqlSchemaExtractor.cs, src/Encina/Modules/Isolation/ModuleSchemaRegistry.cs.
```

</details>

### Phase 3: Integrate into `ExtractSchemas`

<details>
<summary>Tasks</summary>

1. Split `ExtractSchemas` into a public wrapper (null/whitespace check, cache lookup, store) and a private `ExtractCore(string sql, out bool completed)` that holds the current regex logic and sets `completed = false` in the `RegexMatchTimeoutException` handler; only `completed == true` results are stored.
2. `ValidateSqlAccess`, `ValidateSchemaAccess` and every caller stay unchanged (D2 option A). `ExtractTableReferences` is not cached (violation reporting only).
3. Update the XML remarks of `SqlSchemaExtractor` to describe the cache and the timeout rule.

</details>

<details>
<summary>Prompt for AI Agents — Phase 3</summary>

```text
CONTEXT: SqlSchemaExtractor catches RegexMatchTimeoutException and returns the partial set; that result must never be cached.
TASK: Refactor ExtractSchemas into wrapper plus ExtractCore, wire the cache, keep the public signature and every observable result identical.
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

Proposed per-flag targets for the new code (manifest `.github/coverage-manifest/Encina.json`; entries for `SchemaExtractionCache.cs` and the changed `SqlSchemaExtractor.cs`; measured values replace these):

| File | unit | guard | contract | property | integration |
|------|:---:|:---:|:---:|:---:|:---:|
| `Modules/Isolation/SchemaExtractionCache.cs` | 100 | 20 | 0 | 80 | 0 |
| `Modules/Isolation/SqlSchemaExtractor.cs` | 95 | 25 | 0 | 40 | 0 |

1. **Unit** (`tests/Encina.UnitTests/Core/Modules/Isolation/`): hit on equal SQL; two SQL strings with equal `GetHashCode()` (constructed with a test double or a known collision pair) return their own schemas; entry bound and generational reset; over-long SQL not cached; null and whitespace not cached; a timeout result not cached (a pattern forced to time out through a very long adversarial input or a seam on `ExtractCore`).
2. **Guard**: null and empty SQL handling in `SqlSchemaExtractorGuardTests`.
3. **Contract**: `.md` justification: `IModuleSchemaRegistry` is unchanged and its existing contract tests keep running.
4. **Property**: for any generated SQL, cached equals uncached (`tests/Encina.PropertyTests/...`); a second call returns the same instance.
5. **Integration**: `.md` justification (pure in-memory); the ADO wrappers' existing integration tests keep covering the wiring.
6. **Load**: concurrent `ExtractSchemas` from many tasks over a mix of repeated and distinct SQL, asserting the results equal the uncached ones and memory stays bounded (`tests/Encina.LoadTests/Core/...`; run locally with `DOTNET_JitObjectStackAllocationConditionalEscape=0`).
7. **Benchmark**: Phase 1 class extended with the cached variants; cached versus uncached comparison documented.

</details>

<details>
<summary>Prompt for AI Agents — Phase 6</summary>

```text
CONTEXT: Per-flag coverage targets live in .github/coverage-manifest/Encina.json; targets never go down; tests execute real code; the cache is static, so tests call SchemaExtractionCache.Clear() for isolation.
TASK: Write the unit, guard, property, load and benchmark tests and the two justification files; measure every flag and the CRAP table for the changed methods.
KEY RULES: Shouldly; no Thread.Sleep; justification files only for contract and integration of a non-database feature; the collision test must prove two different SQL texts never share an entry.
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
| Production code | 2 | `SchemaExtractionCache.cs` (new), `SqlSchemaExtractor.cs` (edit) |
| Coverage manifest | 1 | `Encina.json` entries |
| Tests | 5 | unit (extend and new), guard (extend), property, load |
| Benchmark | 1 | `SqlSchemaExtractorBenchmarks.cs` (pull request 1) |
| Justifications | 2 | contract, integration |
| Documentation | 2 | guide note, changelog |
| **Total** | **~13** | Two pull requests: (1) benchmark and gate, (2) cache, tests, docs |

---

## Combined AI Agent Prompts

<details>
<summary>Combined prompt for all phases</summary>

```text
PROJECT CONTEXT: Encina is a pre-1.0 .NET 10 / C# 14 library. Module isolation validates SQL against allowed schemas in DevelopmentValidationOnly mode; SqlSchemaExtractor.ExtractSchemas runs regexes on every command. The module isolation check is a gate and must fail closed. Zero warnings, no backward compatibility.

IMPLEMENTATION OVERVIEW: Phase 1 benchmark and go/no-go gate (D3); Phase 2 bounded SchemaExtractionCache (D1); Phase 3 ExtractSchemas wrapper plus ExtractCore with a completed flag; Phase 4 and 5 no instrumentation unless the data demands it; Phase 6 tests by flag including a collision test, a property test of cached versus uncached and a concurrent load test; Phase 7 docs. D2 stays at extraction-only caching. Dilemmas are implemented as the maintainer answers, the recommended options until then.

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
- **Behavior note**: on a regex timeout the extractor returns a partial set (documented as acceptable for development-time validation); this plan does not change that and does not cache it.

---

## Next Steps

1. Ask the maintainer D1, D2 and D3 when work on #703 starts; record the answers in `## Maintainer Decisions`.
2. Pull request 1: the benchmark (Phase 1) with its numbers; stop if the gate fails.
3. Pull request 2: Phases 2 to 7 (about 12 files).
