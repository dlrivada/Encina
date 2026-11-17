# Implementation Plan: In-memory cache for specification expression compilation and composition

> **Issue**: [#706](https://github.com/dlrivada/Encina/issues/706)
> **Type**: Feature
> **Complexity**: Medium (3 phases, 1 pull request of 8 files)
> **Estimated Scope**: about 90 lines of production code, about 450 lines of tests and benchmark
> **Milestone**: v0.18.0 - Integration & Platform Capabilities (caching, performance)

---

## Summary

`Specification<T>` (`src/Encina.DomainModeling/Specification.cs`) rebuilds work on every call, exactly as the issue says, and the code confirms each point on `origin/main` (ec129cae5):

- `ToFunc()` is `ToExpression().Compile()` (line 47) and `IsSatisfiedBy` calls it on every evaluation (lines 54-58), so a loop of N entities compiles N times.
- `AndSpecification`, `OrSpecification` and `NotSpecification` (lines 99-172) call `ToExpression()` on their children and build a new `Expression.Lambda` each time.
- `QuerySpecification<T>.ToExpression()` (lines 321-341) folds `_criteria` with `CombineWithAnd` (lines 509-519) on every call.

Re-checked against the code, the issue needs three corrections, and the plan makes each one:

1. **"Same specification always produces the same delegate" is not true for every specification.** `ToExpression()` is abstract, so a user specification may read a mutable property or capture a clock value when it builds the tree; a plain `_compiledFunc ??= ...` would then return a stale delegate and break the issue's own acceptance criterion "no behavioral change". The plan keeps the issue's cache by default and adds an explicit opt-out for such specifications (Design Choice 1).
2. **The EF Core evaluator needs no change.** `SpecificationEvaluator` applies includes to an `IQueryable` per call (`SpecificationEvaluator.cs` lines 195-203); an `IQueryable` cannot be stored, and the include expressions themselves are already immutable lists on the specification. The issue's "Encina.EntityFrameworkCore" affected package is dropped (Design Choice 2).
3. **`QuerySpecification` can be mutated after the first evaluation.** `AddCriteria` and `ClearCriteria` (lines 350-354 and 493-496) are protected and run in derived classes at any time, so the cached combined criteria must be dropped by those two methods (Design Choice 3).

The feature is a pure in-memory optimization inside `Encina.DomainModeling`; it does not use `ICacheProvider`, so the caching provider matrix of `AGENTS.md` section 5 does not apply. The 22 files of `src/` that call `ToExpression()` (the six `SpecificationSqlBuilder` classes, the sharded and EF repositories, the MongoDB filter builder, `Repository.cs`) benefit without any change: the only production caller of `IsSatisfiedBy` is `DomainDsl.cs`, so the compile cost is paid mostly by application code that filters collections in memory.

**Affected packages:** `Encina.DomainModeling` only. **Provider category:** none (provider-independent).

---

## Design Choices

<details>
<summary><strong>1. The caching contract: when a cached expression or delegate may be reused</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Always cache per instance, as the issue sketches (`_compiledFunc ??= ...`), and document that `ToExpression()` must return an equivalent expression for the lifetime of the instance** | Smallest change; no new API; the gain applies to every specification | A specification with mutable state or a value captured when the tree is built silently returns stale results after the first evaluation; the failure is a wrong query result, not an exception |
| **B) Cache by default, with `protected internal virtual bool IsExpressionStable => true`; a derived class whose expression depends on mutable state overrides it with `false` and nothing is cached for it, and a composite caches only when both children are stable** | Same gain for the common immutable specification; an explicit, documented escape hatch for the unsafe case; the composite stays correct when it wraps an unstable child | One new member on a public base class (PublicAPI line); an unstable specification must know to opt out |
| **C) Opt-in: nothing is cached unless the specification overrides a member to say so** | No behavior can change by default | Almost no gain: the composites and the common specifications would not opt in, and `IsSatisfiedBy` in a loop would keep compiling |
| **D) A static structural cache (`ConcurrentDictionary` keyed by the expression tree)** | Correct for any specification | Hashing and comparing the tree on every call costs about as much as the work saved; unbounded growth with closures (each captured constant is a new key); a static shared mutable structure in a library |

### Chosen Option: **B - cache by default with an explicit stability flag** (recommended, pending the maintainer)

### Rationale

- A wins when the maintainer decides that every specification is an immutable value by contract and wants no new member; B wins as soon as one real specification reads mutable state (a settable property is enough), because B keeps the gain for the others and makes the exception explicit instead of silent. C is the safe choice if the maintainer prefers no default change; its gain is nearly zero. D is rejected: it costs more than it saves.
- The flag is `protected internal` so the internal composites read it on any `Specification<T>` instance and application subclasses override it. The decision is taken now because the choice changes the public base class: the plan builds B and notes that dropping the flag (option A) removes one member and one test group.
- Class: Dilemma

</details>

<details>
<summary><strong>2. What is cached, and what is not</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) The compiled delegate (`Specification<T>`), the combined expression of `AndSpecification`, `OrSpecification` and `NotSpecification`, and the combined criteria of `QuerySpecification<T>`; nothing else** | Exactly the three costs the code shows (lines 47, 99-172, 321-341); every cached object is an immutable expression tree or a delegate | The include expressions are not cached |
| **B) Also cache the EF `Include` application in `SpecificationEvaluator`** | The issue lists it | The result of `query.Include(...)` is an `IQueryable` bound to one context and one query; it cannot be stored, and EF Core already caches the translation of an equal expression tree in its query cache |

### Chosen Option: **A - the three costs of the code**

### Rationale

- **Class:** Trivial
- **Settled by:** `SpecificationEvaluator.cs` lines 195-203 apply `Include` to the incoming `IQueryable`, and comparable products: the EF Core query cache keys a compiled query by the structure of the expression tree, so caching the tree instance saves allocation, not translation. The issue's "Encina.EntityFrameworkCore" package is therefore unchanged and the plan edits no file there.

</details>

<details>
<summary><strong>3. Invalidating the combined criteria of `QuerySpecification` when it is mutated</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `AddCriteria` and `ClearCriteria` set the cached combined expression and the cached delegate to null** | The two methods are the only writers of `_criteria`; the cache can never be stale | Two assignments in two methods |
| **B) Leave the cache as is and document "do not add criteria after the first evaluation"** | No code | A derived class that adds a criterion conditionally after a first `IsSatisfiedBy` gets a wrong result with no error |

### Chosen Option: **A - invalidate in the two mutators**

### Rationale

- **Class:** Settled
- **Settled by:** #706 acceptance criterion "No behavioral change - cached results must be identical" and `Specification.cs` lines 350-354 and 493-496 (`_criteria.Add` and `_criteria.Clear` are the only writers, both protected). B violates the criterion in a concrete sequence (evaluate, add a criterion, evaluate), so A is the only option that complies.

</details>

<details>
<summary><strong>4. Thread-safe lazy initialization</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `LazyInitializer.EnsureInitialized` over a nullable field holding the delegate (and one for the combined expression); two threads may compile at the same moment, one result is published, both results are equal** | No per-instance allocation until the first use; a specification evaluated once costs nothing extra; no lock | The work can run twice under contention |
| **B) `Lazy<T>` with `ExecutionAndPublication`** | The work runs exactly once | One `Lazy<T>` object per specification instance even when it is never evaluated, and specifications are created per request |
| **C) A `lock` per instance** | Exactly once | A lock object per instance and a lock on a hot path |

### Chosen Option: **A - publish by compare-and-swap** (recommended, pending the maintainer)

### Rationale

- B wins when a single compilation is measurably expensive and a duplicated one under contention is not acceptable; A wins because compilation is a pure function of an immutable tree (a duplicate is waste, not a defect) and the common specification is created and evaluated by one thread. The benchmark of PR 1 reports both the cached and the first-call cost, so the maintainer can see the price of A's duplicate work.
- Class: Dilemma

</details>

<details>
<summary><strong>5. Telemetry and logging</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) None: no counter, no log, no span** | `IsSatisfiedBy` is a hot path; a counter increment per call costs more than the dictionary lookup it would count | No hit-rate visibility |
| **B) A hit and miss counter on the existing `SpecificationActivitySource` meter** | Visibility | Per-call cost on the path the feature optimizes; the hit rate is a property of the application's specification lifetime, not a runtime signal anyone acts on |

### Chosen Option: **A - none**

### Rationale

- **Class:** Trivial
- **Settled by:** ADR-054 (telemetry follows the rule "no cost without a listener", and a per-call instrument on a hot path is a cost the optimization exists to remove) and #706 ("In-memory optimization only"). The gain is measured by the benchmark of PR 1, not by a production counter.

</details>

---

## Implementation Phases

### Phase 1: Cache in `Specification<T>` and the composites (the single pull request)

<details>
<summary><strong>Tasks</strong></summary>

The pull request holds exactly these 8 files (about 600 lines):

| # | File | Change |
|---|------|--------|
| 1 | `src/Encina.DomainModeling/Specification.cs` | modify |
| 2 | `src/Encina.DomainModeling/PublicAPI.Unshipped.txt` | modify (`mechanical-fixer`: `IsExpressionStable`) |
| 3 | `tests/Encina.UnitTests/DomainModeling/SpecificationExpressionCachingTests.cs` | new |
| 4 | `tests/Encina.PropertyTests/Database/Specification/SpecificationCachingProperties.cs` | new |
| 5 | `tests/Encina.BenchmarkTests/Encina.Benchmarks/SpecificationCachingBenchmarks.cs` | new |
| 6 | `.github/coverage-manifest/Encina.DomainModeling.json` | modify (`mechanical-fixer`: the `Specification.cs` targets) |
| 7 | `changelog.d/706-specification-expression-cache.changed.md` | new (`mechanical-fixer`) |
| 8 | `docs/features/specification-expression-caching.md` | new (`docs-writer`) |

No guard test file changes: the new member has no parameter, and the guard tests of the existing `SpecificationGuardTests.cs` keep covering the null checks of `And`, `Or` and the protected builder methods. No contract test: no interface changes (`ISpecification<T>.ToFunc()` keeps its signature). No load test: the cache holds no shared mutable state beyond one atomic field per instance (justification in the manifest `reason`).

Tasks:

1. **Modify `Specification<T>`**: add `protected internal virtual bool IsExpressionStable => true`; keep `ToExpression()` abstract; change `ToFunc()` to return the delegate cached in one nullable field published by compare-and-swap (`LazyInitializer.EnsureInitialized`, Design Choice 4) when `IsExpressionStable` is true, and to compile on every call when it is false. `IsSatisfiedBy` is unchanged (it calls `ToFunc()`).
2. **Modify `AndSpecification<T>`, `OrSpecification<T>`, `NotSpecification<T>`**: each overrides `IsExpressionStable` as the conjunction of its children's flags and caches the built `Expression.Lambda` in one nullable field when stable; the parameter name stays `x`; the expression shape is unchanged so the SQL builders and EF translate exactly as today.
3. **Modify `QuerySpecification<T>`**: cache the combined criteria in `ToExpression()`; `AddCriteria` and `ClearCriteria` reset the cached expression and delegate (Design Choice 3); a derived class that overrides `ToExpression()` is covered by the same flag: it is stable by default, so its delegate is compiled once, and a derived class that returns a different expression after mutation declares itself unstable.
4. **XML documentation**: `<remarks>` on `Specification<T>`, `ToFunc()` and `IsExpressionStable` state the contract (an expression that depends on mutable state or a value captured at build time must override the flag to `false`), with an example of each case; the existing sentence "Caching the compiled function is recommended" is replaced.
5. **Unit tests** (execute the real classes): `ToFunc()` twice returns the same delegate instance for a stable specification and different instances for an unstable one; `And/Or/Not` return the same expression instance on repeated calls; a composite with one unstable child recompiles; `AddCriteria` after an evaluation changes the result (sequence: evaluate, add criterion, evaluate); `ClearCriteria` likewise; `IsSatisfiedBy` is correct before and after caching for `And`, `Or`, `Not` over the specifications of `SpecificationPropertyTests`; concurrent first calls from 32 tasks return working delegates and one published instance afterwards (no sleeps, a barrier); a stable specification evaluated once costs no more than one compile.
6. **Property tests** (FsCheck): for random trees of `And/Or/Not` over the three property-test specifications and random entities, `IsSatisfiedBy` equals the result of compiling a freshly built equal tree (cached equals uncached), and the result is stable across repeated calls.
7. **Benchmark**: `SpecificationCachingBenchmarks` compares N evaluations of `IsSatisfiedBy` on one specification instance before and after (a baseline method that calls `ToExpression().Compile()` each time versus `ToFunc()`), for a simple specification, an `And` of three and a `QuerySpecification` with four criteria; it also reports the first-call cost. `BenchmarkSwitcher` already runs the project; verify the filter with `--list flat`.
8. **Manifest, changelog and documentation** through the specialists: `docs-writer` writes `docs/features/specification-expression-caching.md` (the contract, the opt-out, the measured gain by benchmark reference, no hand-typed figures); `mechanical-fixer` writes the changelog fragment ("`Specification<T>` now caches its compiled delegate and the expressions of `And`, `Or`, `Not` and `QuerySpecification`; a specification whose expression depends on mutable state overrides the new `IsExpressionStable`") and the manifest targets of the coverage table below.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 1</strong></summary>

```
CONTEXT:
Encina is a .NET 10 / C# 14 library, pre-1.0, no backward compatibility. Issue #706: Specification<T>.ToFunc() compiles on every call and
And/Or/Not/QuerySpecification rebuild their expression on every ToExpression(). All of it is in src/Encina.DomainModeling/Specification.cs.

TASK:
1. Add protected internal virtual bool IsExpressionStable => true to Specification<T>; cache the compiled delegate and the composite
   expressions only when it is true, publishing with compare-and-swap (no lock, no Lazy<T>).
2. AndSpecification/OrSpecification/NotSpecification: stable only when every child is stable; keep the expression shape and the
   parameter name unchanged.
3. QuerySpecification<T>: cache the combined criteria; AddCriteria and ClearCriteria reset the cache.
4. Write the unit tests, the FsCheck property (cached equals uncached) and the benchmark; ask mechanical-fixer for the PublicAPI line,
   the manifest targets and the changelog fragment; ask docs-writer for the feature page.

KEY RULES:
- No behavior change: a cached result must equal the uncached result for every stable specification.
- The EF Core evaluator, the SQL builders and the MongoDB builder are NOT edited.
- XML docs on every public or protected member; zero warnings; CRAP of every method at most 10.
- Tests execute the real classes; no Thread.Sleep; BenchmarkSwitcher, never BenchmarkRunner.Run<T>.

REFERENCE FILES:
- src/Encina.DomainModeling/Specification.cs (lines 31-172, 215-341, 493-519)
- tests/Encina.PropertyTests/Database/Specification/SpecificationPropertyTests.cs
- tests/Encina.GuardTests/DomainModeling/SpecificationGuardTests.cs
- .github/coverage-manifest/Encina.DomainModeling.json
```

</details>

### Phase 2: Coverage targets

<details>
<summary><strong>Tasks</strong></summary>

Baseline (from the manifest on `origin/main`, measured values in its justifications): `Specification.cs` has unit 83, guard 14, property 74 (measured 85.47, 14.53 and 76.07). Measure again before the pull request opens: run each flag's test project with `dotnet test tests\Encina.<Flag>Tests --collect "XPlat Code Coverage" --results-directory artifacts\coverage\<Flag>Tests`, then `dotnet run --file .github/scripts/coverage-report.cs -- --input artifacts/coverage --output artifacts/coverage-report` and `dotnet run --file .github/scripts/coverage-report.cs -- --check-justifications`.

| File | unit | guard | property | Justification |
|------|------|-------|----------|---------------|
| `Specification.cs` | 90 | 14 | 80 | The new branches (stable and unstable, cached and fresh, concurrent first call, mutation after evaluation) are all unit tested; the guard target stays because the new member has no parameter; the property test over random `And/Or/Not` trees reaches the cached composite paths |

The ratchet forbids lowering a target, so the three values only go up. The measured values replace the estimates before the pull request opens.

No new `PublicAPI` symbol beyond `IsExpressionStable`: the composites are internal.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 2</strong></summary>

```
CONTEXT:
Phase 1 of the plan for issue #706 is implemented. This phase measures and records per-flag coverage of Specification.cs.

TASK:
Run the unit, guard and property test projects with coverage, compare with the targets of the plan, and ask mechanical-fixer to record the
targets and the one-sentence justifications in .github/coverage-manifest/Encina.DomainModeling.json.

KEY RULES:
- A target never goes down; measured values replace the estimates.
- Documentation never types coverage figures; it cites cov: markers.

REFERENCE FILES:
- .github/coverage-manifest/Encina.DomainModeling.json
- docs/testing/coverage-measurement-methodology.md
```

</details>

### Phase 3: Documentation and finalization

<details>
<summary><strong>Tasks</strong></summary>

1. `docs/features/specification-expression-caching.md` through `docs-writer` (part of the single pull request, file 8): when the cache applies, the stability contract with one safe and one unsafe example, the benchmark reference, no hand-typed figures.
2. `ROADMAP.md`: no change (a performance improvement inside a shipped component, no milestone item moves). `docs/INVENTORY.md`: no change (no file or package is added to `src/`).
3. No ADR: the decision is local to one class and recorded in the plan (Design Choice 1).
4. Build with `dotnet build --configuration Release` (0 warnings), run the three test projects and the benchmark filter, `dotnet run --file .github/scripts/coverage-report.cs -- --check-justifications`.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 3</strong></summary>

```
CONTEXT:
Issue #706 is implemented and measured. This phase finalizes the documentation and the verification.

TASK:
Ask docs-writer for docs/features/specification-expression-caching.md, ask mechanical-fixer for the changelog fragment, run the build, the
tests and the coverage checks, and report the measured per-flag coverage of Specification.cs against its targets.

KEY RULES:
- State the stability contract and the opt-out in the page; never promise a gain for a specification that is created and evaluated once.
- No hand-typed coverage or benchmark figures in documentation.

REFERENCE FILES:
- changelog.d/README.md
- .claude/skills/encina-docs/SKILL.md
```

</details>

---

## Research

### Relevant Standards & Specifications

| Standard | Relevance |
|----------|-----------|
| AGENTS.md section 1 (pre-1.0, best solution, pay-for-what-you-use) | The base class changes completely where it is better; the optimization is internal and adds one member |
| AGENTS.md section 3 (no `[Obsolete]`, no compatibility layers) | The old sentence "caching is recommended" is replaced, no shim |
| AGENTS.md section 9 (tests execute real code, per-flag targets, benchmarks use `BenchmarkSwitcher`) | Unit, property and benchmark tests; manifest targets that only go up |
| ADR-054 | No instrument on a hot path; no cost without a listener |
| #712 | The Cache Integration epic that this issue belongs to |

### Existing Encina Infrastructure to Leverage

| Component | Location | Usage in This Feature |
|-----------|----------|----------------------|
| Specification, And/Or/Not, QuerySpecification | src/Encina.DomainModeling/Specification.cs | the only production file edited |
| ISpecification | src/Encina.DomainModeling/ISpecification.cs | `ToFunc()` and `IsSatisfiedBy` signatures stay |
| SpecificationEvaluator | src/Encina.EntityFrameworkCore/Repository/SpecificationEvaluator.cs | consumer of `ToExpression()`; unchanged |
| SpecificationSqlBuilder (6 providers) | src/Encina.{ADO,Dapper}.{SqlServer,PostgreSQL,MySQL}/Repository | consumers of `ToExpression()`; unchanged |
| SpecificationPropertyTests | tests/Encina.PropertyTests/Database/Specification/SpecificationPropertyTests.cs | specifications and entity for the cached-equals-uncached property |
| SpecificationGuardTests | tests/Encina.GuardTests/DomainModeling/SpecificationGuardTests.cs | existing guard coverage of the builder methods |
| SpecificationEvaluatorBenchmarks | tests/Encina.BenchmarkTests/Encina.Benchmarks/EntityFrameworkCore | benchmark project and style |

### Event ID Allocation

| Package | Range | Notes |
|---------|-------|-------|
| `Encina.DomainModeling` | none | No logging is added (Design Choice 5); the existing `DomainModelingSpecification` range 1400-1499 is untouched; the block 400-449 of the family is not used by this plan |

### Estimated File Count

| Category | New Files | Modified Files | Notes |
|----------|-----------|----------------|-------|
| Production code | 0 | 1 | `Specification.cs` |
| Tests | 3 | 0 | unit, property, benchmark |
| Documentation | 1 | 0 | feature page |
| Configuration | 1 | 2 | New: changelog fragment; modified: `PublicAPI.Unshipped.txt`, `Encina.DomainModeling.json` |
| **Total** | **5** | **3** | 1 pull request of 8 files (about 600 lines) |

---

## Combined AI Agent Prompts

<details>
<summary><strong>Complete Implementation Prompt - All Phases</strong></summary>

```
PROJECT CONTEXT:
Encina is a .NET 10 / C# 14 library, pre-1.0, no backward compatibility. Issue #706: cache the compiled delegate of Specification<T> and
the composed expressions of And/Or/Not/QuerySpecification inside Encina.DomainModeling, in memory only.

IMPLEMENTATION OVERVIEW:
1. Specification.cs: IsExpressionStable (protected internal virtual, default true); ToFunc() caches an immutable pair by compare-and-swap;
   composites cache their lambda only when every child is stable; QuerySpecification caches its combined criteria and resets it in
   AddCriteria and ClearCriteria.
2. Tests: unit (identity, mutation after evaluation, unstable child, concurrent first call), FsCheck (cached equals uncached), benchmark.
3. PublicAPI line, manifest targets, changelog fragment and the feature page through the specialists.

KEY PATTERNS:
- No behavior change: stable specifications return equal results cached or not; unstable ones are never cached.
- The EF evaluator, SQL builders and MongoDB builder are not edited; includes are not cached (they act on an IQueryable).
- Apply Design Choices 1 and 4 as the maintainer answers them; until then build the recommended option of each.
- No telemetry, no logs; zero warnings; CRAP of every method at most 10.

REFERENCE FILES:
- src/Encina.DomainModeling/Specification.cs
- tests/Encina.PropertyTests/Database/Specification/SpecificationPropertyTests.cs
- tests/Encina.GuardTests/DomainModeling/SpecificationGuardTests.cs
- .github/coverage-manifest/Encina.DomainModeling.json
```

</details>

---

## Cross-Cutting Integration Matrix

| # | Function | Status | Notes |
|---|----------|--------|-------|
| 1 | Caching | ✅ Include | The feature is an in-memory cache of compiled delegates and composed expressions; it deliberately does not use `ICacheProvider` (an expression tree is not serializable and the cost is per process). |
| 2 | OpenTelemetry | ❌ N/A | A hot path; no instrument (Design Choice 5); the gain is measured by the benchmark. |
| 3 | Structured Logging | ❌ N/A | Nothing to log; no EventId is allocated. |
| 4 | Health Checks | ❌ N/A | No dependency to check. |
| 5 | Validation | ❌ N/A | No new input; existing null checks are unchanged. |
| 6 | Resilience | ❌ N/A | No external call. |
| 7 | Distributed Locks | ❌ N/A | In-process publication by compare-and-swap; no shared state across processes. |
| 8 | Transactions | ❌ N/A | No persistence. |
| 9 | Idempotency | ❌ N/A | A pure function of an immutable tree; repeated calls return equal results. |
| 10 | Multi-Tenancy | ❌ N/A | A cached expression holds no tenant or request data; an unstable specification (for example one that captures a tenant id at build time) opts out through `IsExpressionStable`. |
| 11 | Module Isolation | ❌ N/A | Not module scoped. |
| 12 | Audit Trail | ❌ N/A | An optimization with no compliance implication. |

**Deferred items:** none. The EF `Include` application is not cached by design (Design Choice 2); no follow-up issue.

---

## Prerequisites & Dependencies

| Prerequisite | Status | Notes |
|--------------|--------|-------|
| `Encina.DomainModeling` `Specification<T>` | ✅ Available | Single file edited |
| Benchmark project `Encina.Benchmarks` | ✅ Available | References `Encina.DomainModeling` (the EF benchmarks in the same project use specifications) |
| Maintainer answers to Design Choices 1 and 4 | Pending | Asked when work starts |

No blocking prerequisites.

---

## Next Steps

1. The orchestrator asks the maintainer the Dilemmas (Design Choices 1 and 4) when work starts, records the answers in a `## Maintainer Decisions` section, then briefs one worker for the single pull request.
2. After the pull request merges, comment on #706 with the measured benchmark so the maintainer can judge the gain for a specification created once and one evaluated in a loop.
