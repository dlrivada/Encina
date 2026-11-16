# Implementation Plan: In-process caches for security and audit metadata resolution

> **Issue**: [#707](https://github.com/dlrivada/Encina/issues/707)
> **Type**: Feature
> **Complexity**: Low (4 phases, 2 pull requests of 10 and 4 files)
> **Estimated Scope**: about 150 lines of production code, about 450 lines of tests and benchmarks
> **Milestone**: v0.18.0 — Integration & Platform Capabilities

---

## Summary

Issue #707 asks for `ConcurrentDictionary` caches of three things: the claims extraction of `SecurityContext`, the audit metadata of `DefaultAuditEntryFactory` (attribute lookup and naming-convention parsing), and the PII masking patterns "compiled per field name". Re-checked against `origin/main` (db1e7f688) only part of that exists:

| Premise of the issue | State in the code |
|----------------------|-------------------|
| `DefaultAuditEntryFactory` calls `GetCustomAttribute<AuditableAttribute>()` and `ExtractFromTypeName()` on every entry | True, under the real name `DefaultOperationAuditEntryFactory` (`src/Encina.Security.Audit/DefaultOperationAuditEntryFactory.cs`, lines 95 and 138 through `ResolveEntityTypeAndAction`): both run on every audited request, uncached |
| Entity-id property lookup is deterministic per type | Already cached (`RequestMetadataExtractor.EntityIdPropertyCache`) |
| `ShouldAuditCache` exists | True (`AuditPipelineBehavior.cs`, line 49, one static dictionary per closed generic) |
| Masking patterns are compiled per field name | False. `DefaultSensitiveDataRedactor` has no regular expression: sensitive field names are a `HashSet<string>` checked with `Contains` (`IsSensitiveField`). What it does allocate per call is the effective set `BuildEffectivePatterns(additionalFields)` whenever `[Auditable(SensitiveFields = ...)]` is present |
| `SecurityContext` claims extraction runs per request and is worth caching per principal | The constructor (`src/Encina.Security/SecurityContext.cs`) has no caller in `src/`: hosts create the context and set it through `ISecurityContextAccessor`; there is no in-tree hot path to cache |
| (not in the issue) | `SecurityPipelineBehavior.DiscoverAttributes` (`src/Encina.Security/SecurityPipelineBehavior.cs`, lines 117-135) runs two `GetCustomAttributes` calls and a LINQ sort per request, uncached: the same pattern the issue targets |

The plan therefore caches (a) the per-type audit metadata (attribute, entity type, action) in one immutable `RequestAuditMetadata` per request type, (b) the effective sensitive-field set per attribute instance held by that metadata, and (c) the security attribute discovery per closed generic behavior. It does not cache `SecurityContext` (Design Choice 2) and it drops the masking-regex item as obsolete (Design Choice 1). No behavior changes: a cached value is a pure function of the `Type`.

**Affected packages:** `Encina.Security.Audit` (factory, redactor, new metadata cache) and `Encina.Security` (behavior). Nothing in `Encina.Security`'s public API changes.

**Provider category**: none (provider-independent, type-level metadata). The issue's "Cache Integration Epic" caches are in-process dictionaries, not `ICacheProvider` (AGENTS.md section 5 caching category does not apply).

**Sibling plans (caching family)**: none of the sibling plans owns a type this plan changes.

---

## Design Choices

<details>
<summary><strong>1. Which items of the issue are implemented</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Implement what exists: audit metadata per type, effective sensitive-field set per attribute, security attribute discovery per closed generic; drop the regex item and the `SecurityContext` item** | Every cache has a real per-request cost behind it; the acceptance of the issue is met where its premise holds | Two of the three headings of the issue are closed without code (recorded in the plan and the changelog fragment) |
| **B) Implement the issue literally** (a regex cache that has nothing to cache, a `SecurityContext` cache keyed by a principal hash) | Matches the text | Adds code for a cost that does not exist; the principal-hash cache is a security risk (Design Choice 2) |
| **C) Close the issue as obsolete** | No code | Leaves the two real per-request reflection costs (audit factory, security behavior) |

### Chosen Option: **A - implement what exists** (recommended, pending the maintainer)

### Rationale

- The issue is `priority-low`, `complexity-low`; the real cost is reflection plus regex matching per request on two hot paths.
- Option C wins if profiling shows the two costs are negligible; the benchmark of Phase 1 is how that is measured, and it is written first. The worker does not apply a threshold of its own: it records the allocation and time numbers of the baseline in the PR description and reports them, and the orchestrator brings them to the maintainer with this Dilemma before Option C is chosen or the implementation continues.
- Class: Dilemma

</details>

<details>
<summary><strong>2. `SecurityContext` per principal</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) No cache** | No in-tree caller exists; no staleness risk | Hosts that build the context several times per request keep paying |
| **B) `ConditionalWeakTable<ClaimsPrincipal, SecurityContext>` keyed by the principal reference (and the options instance) | Entries die with the principal; a reference key cannot mix two principals | `ClaimsPrincipal` is mutable (`AddIdentity`, `RemoveClaim`): a cached context can hold roles that were removed or miss ones that were added, and the context feeds authorization decisions |
| **C) Cache by a hash of the claims** | Works across principal instances | Two principals can collide; a hash lookup costs about as much as the extraction |

### Chosen Option: **A - no cache** (recommended, pending the maintainer)

### Rationale

- A stale `SecurityContext` is an authorization defect, and the security gates fail closed (AGENTS.md section 3): a cache that can serve a role the principal no longer has fails open.
- Option B wins for a host that constructs the context repeatedly per request and measures it; the host can cache the context itself, next to the code that knows when the principal changes.
- Class: Dilemma

</details>

<details>
<summary><strong>3. Cache placement and key</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Static `ConcurrentDictionary<Type, T>` (internal), the pattern of `ShouldAuditCache` and `EntityIdPropertyCache`** | Same pattern, same `Clear()` test hook; values are pure functions of the `Type` alone | Pins types of unloadable assemblies (as the two existing caches do) |
| **B) Instance field of the factory** | Dies with the factory | The factory is registered as a singleton in practice; no gain; the behaviors are transient per closed generic, so their caches must be static |

### Chosen Option: **A - static, keyed by `Type`**

### Rationale

- **Class:** Trivial
- **Settled by:** the issue text of #707 ("extend same pattern" of `ShouldAuditCache`) and the existing code (`AuditPipelineBehavior.cs` line 49, `RequestMetadataExtractor.cs` line 13); `docs/engineering/prompts/implementation-plan-prompt.md` (pipeline behaviors: static per-generic-type attribute caching).
- This applies to caches whose value depends on the `Type` alone. A cache whose value also depends on an instance's options (the redactor's effective sensitive-field set) is not static: see Design Choice 6.

</details>

<details>
<summary><strong>4. Effective sensitive-field set</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `IPiiMasker` gains an overload taking a prepared field set** | Typed, no hidden cache | A public interface change that every custom masker must implement |
| **B) `RequestAuditMetadata` freezes the attribute's `SensitiveFields` into one array; `DefaultSensitiveDataRedactor` caches the effective `HashSet<string>` in an instance field `ConditionalWeakTable<string[], HashSet<string>>` keyed by that array's reference (placement: Design Choice 6)** | No API change; the cached array is never mutated by callers (the metadata owns it); a caller that passes a fresh array each time simply misses, and the table does not retain it | A reference-keyed cache is an implicit contract between the factory and the redactor |

### Chosen Option: **B - reference-keyed cache in the redactor** (recommended, pending the maintainer)

### Rationale

- The masking contract stays fail closed: the cache holds field names, never a masked payload or a mask result, so a masking failure still throws from the same place (`RedactJsonString` and `MaskForAudit`).
- Option A wins when custom maskers should also benefit from the prepared set.
- Class: Dilemma

</details>

<details>
<summary><strong>5. What is cached and what never is</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Cache only successful, pure results: attribute, entity type, action, the frozen field array; never an exception, never a payload, never a request value** | A failing lookup (a malformed attribute) throws again each time, as today; no tenant, user or request data can persist | None |
| **B) Also cache failures (negative caching)** | Saves repeated reflection on a broken type | Hides a fault behind a stale entry; a failure is rare |

### Chosen Option: **A - successes only**

### Rationale

- **Class:** Trivial
- **Settled by:** `ConcurrentDictionary.GetOrAdd` stores nothing when the value factory throws, so today's behavior (a malformed attribute throws on every call) is preserved without extra code; a fix needs a rebuild, which clears a static cache anyway.
- The cached `AuditableAttribute` and `SecurityAttribute` instances become shared across requests (today `GetCustomAttribute` returns fresh ones). No code in `Encina.Security*` assigns an attribute property after construction (grep of `src/Encina.Security*` found none), so sharing changes no behavior; the cache holds nothing tenant-, user- or request-specific (AGENTS.md section 6 function 10).

</details>

<details>
<summary><strong>6. Placement of the redactor's effective-set cache</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Instance field of each `DefaultSensitiveDataRedactor`** | The cached set contains that redactor's own `GlobalSensitiveFields` (`DefaultSensitiveDataRedactor.cs` lines 102-111) and is never served to another redactor with different options, so masking cannot fail open | One table per redactor (the redactor is a singleton in practice, so one in the process) |
| **B) Static table keyed by the array and the redactor instance (composite key)** | One shared structure | A composite key adds code and a second weak reference for no gain; a mistake in the key serves one redactor's set to another and masking fails open |

### Chosen Option: **A - instance field** (recommended, pending the maintainer)

### Rationale

- The effective set is the redactor's `_sensitivePatterns` (defaults plus its options' `GlobalSensitiveFields`) united with the attribute's fields; a static table keyed only by the array would serve a set built without global field `X` to a second redactor whose options add `X`, and `X` would stop being redacted. Compliance gates fail closed (AGENTS.md section 3).
- Option B wins only if a measured need appears for sharing the table across redactor instances, which no in-tree host has.
- A unit test builds two redactors with different `GlobalSensitiveFields` and passes both the same cached array: each redacts its own global field (Phase 1 task 5).
- Class: Dilemma

</details>

---

## Implementation Phases

### Phase 1: Benchmark and audit metadata cache (pull request 1)

<details>
<summary><strong>Tasks</strong></summary>

PR 1 holds exactly these 10 files:

| # | File | Change |
|---|------|--------|
| 1 | `src/Encina.Security.Audit/RequestAuditMetadata.cs` | new (internal sealed record and its static cache) |
| 2 | `src/Encina.Security.Audit/DefaultOperationAuditEntryFactory.cs` | modify (use the cached metadata) |
| 3 | `src/Encina.Security.Audit/DefaultSensitiveDataRedactor.cs` | modify (reference-keyed effective-set cache) |
| 4 | `tests/Encina.BenchmarkTests/Encina.Benchmarks/Security/Audit/AuditEntryFactoryBenchmarks.cs` | new (written and run first; baseline and after) |
| 5 | `tests/Encina.BenchmarkTests/Encina.Benchmarks/Security/Audit/AuditBenchmark.md` | modify (the justification now says the factory is benchmarked and the store is not) |
| 6 | `tests/Encina.UnitTests/Security/Audit/RequestAuditMetadataTests.cs` | new |
| 7 | `tests/Encina.UnitTests/Security/Audit/DefaultSensitiveDataRedactorTests.cs` | modify |
| 8 | `tests/Encina.UnitTests/Security/Audit/DefaultOperationAuditEntryFactoryTests.cs` | modify |
| 9 | `.github/coverage-manifest/Encina.Security.Audit.json` | modify (`mechanical-fixer`) |
| 10 | `changelog.d/707-cache-audit-and-security-metadata.changed.md` | new (`mechanical-fixer`) |

Tasks:

1. **Baseline first**: write `AuditEntryFactoryBenchmarks` over `DefaultOperationAuditEntryFactory.Create` (a request type with `[Auditable]` and `SensitiveFields`, and one without) with `BenchmarkSwitcher.FromAssembly(...).Run(args, config)`; verify the filter with `--list flat --filter "*AuditEntryFactory*"`, run `--job short`, and record allocations and time in the PR description. The worker applies no threshold: it reports the baseline numbers and the orchestrator takes them to the maintainer with Design Choice 1 (Option C) before the implementation continues.
2. **Create** `internal sealed record RequestAuditMetadata(AuditableAttribute? Attribute, string EntityType, string Action, string[]? SensitiveFields, bool? IncludePayload)` and `static RequestAuditMetadata For(Type requestType)` backed by `ConcurrentDictionary<Type, RequestAuditMetadata>`; the factory delegate calls `requestType.GetCustomAttribute<AuditableAttribute>()` and `RequestMetadataExtractor.ExtractFromTypeName(requestType)` once, copies `SensitiveFields` into a private array, and applies the same precedence as today (`attribute.EntityType ?? convention`). `internal static void Clear()` for tests, as `RequestMetadataExtractor.ClearCache` does.
3. **Modify** `DefaultOperationAuditEntryFactory.Create<TRequest, TResponse>`: replace the two per-call lookups by `RequestAuditMetadata.For(typeof(TRequest))` and pass `metadata.SensitiveFields` where it passes `auditableAttribute?.SensitiveFields` today. The entry produced is identical.
4. **Modify** `DefaultSensitiveDataRedactor`: `BuildEffectivePatterns` is called through a `private readonly ConditionalWeakTable<string[], HashSet<string>>` instance field (never static, Design Choice 6) when the argument is a `string[]` (the cached array); other `IEnumerable<string>` arguments keep today's per-call path. The cached set is read-only after construction and the constructor-built default set is not exposed.
5. **Tests**: every test that asserts identity or counts on the static cache (`ShouldBeSameAs`, one instance per type) uses request types private to its own test class (nested types), so a parallel test class calling `Clear()` cannot make it flaky; the classes that call `Clear()` are put in one non-parallel xUnit collection. `For` returns the same instance on a second call (`ShouldBeSameAs`); two request types do not share an entry; a type with `[Auditable(EntityType = "X", Action = "Y")]` takes the attribute values, a type without it takes the convention; the cached field array is a copy (mutating the attribute's array after the first call does not change the next result); the redactor returns the same redaction for the cached and an uncached call with the same fields, and a fresh array each call still redacts correctly; two redactors built with different `GlobalSensitiveFields` and given the same cached array each redact their own global field (the set of one is never served to the other); the factory produces equal entries (apart from `Id` and timestamps) on the first and the hundredth call; a concurrent test (`Parallel.For`, 64 iterations, 8 types) sees no exception and one instance per type; a masker that throws still returns `Left(audit.masking_failed)` on a repeated call (the failure is not cached). Guard tests: no public symbol is added, so the guard flag is unchanged.
6. **Manifest**: targets for the new file and the two touched files (Phase 3 table).

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 1</strong></summary>

```
CONTEXT:
Encina is a .NET 10 / C# 14 library, pre-1.0. Issue #707 caches per-type audit metadata. DefaultOperationAuditEntryFactory
(src/Encina.Security.Audit) reads the AuditableAttribute and parses the type name on every entry; DefaultSensitiveDataRedactor
builds an effective HashSet per call when SensitiveFields is present. There is no regex in the masking path.

TASK:
1. Write AuditEntryFactoryBenchmarks first and record the baseline; report the numbers (no threshold of your own) and wait for the orchestrator.
2. Add RequestAuditMetadata (static ConcurrentDictionary<Type, ...>, internal Clear()) and use it in the factory.
3. Add the reference-keyed ConditionalWeakTable<string[], HashSet<string>> to the redactor as an INSTANCE field, never static (its set contains that redactor's GlobalSensitiveFields).
4. Write the unit tests of the plan; ask mechanical-fixer for the manifest entry and the changelog fragment.

KEY RULES:
- Cache only successful, pure results keyed by Type; never an exception, a payload or a request value.
- Masking stays fail closed: a throwing masker still produces Left(audit.masking_failed) on every call.
- No public API change, no new error code, no logging, no telemetry (the caches hold no tenant data).
- BenchmarkSwitcher, not BenchmarkRunner.Run<T>(); outputs under artifacts/performance.
- Zero warnings; CRAP of every added method at most 10.

REFERENCE FILES:
- src/Encina.Security.Audit/DefaultOperationAuditEntryFactory.cs
- src/Encina.Security.Audit/DefaultSensitiveDataRedactor.cs
- src/Encina.Security.Audit/RequestMetadataExtractor.cs
- src/Encina.Security.Audit/AuditPipelineBehavior.cs
- tests/Encina.UnitTests/Security/Audit/RequestMetadataExtractorTests.cs
```

</details>

### Phase 2: Security attribute discovery (pull request 2)

<details>
<summary><strong>Tasks</strong></summary>

PR 2 holds exactly these 4 files:

| # | File | Change |
|---|------|--------|
| 1 | `src/Encina.Security/SecurityPipelineBehavior.cs` | modify |
| 2 | `tests/Encina.UnitTests/Security/SecurityPipelineBehaviorTests.cs` | modify |
| 3 | `.github/coverage-manifest/Encina.Security.json` | modify (`mechanical-fixer`) |
| 4 | `changelog.d/707-cache-security-attribute-discovery.changed.md` | new (`mechanical-fixer`) |

Tasks:

1. **Modify** `SecurityPipelineBehavior<TRequest, TResponse>` (the `List<SecurityAttribute>` parameters of `AuthorizeAsync` line 142, `DenyMissingContext` line 186 and `EvaluateAttributesAsync` line 219, and the return of `DiscoverAttributes` line 118): a `private static readonly` profile per closed generic type, built once: `bool AllowsAnonymous` (`typeof(TRequest).GetCustomAttributes(typeof(AllowAnonymousAttribute), inherit: true).Length > 0`) and the `SecurityAttribute[]` already ordered by `Order` then `GetAttributeTypePriority`. `DiscoverAttributes` keeps its instance-level logic that depends on options and logging: it still logs `AllowAnonymousBypass` on every request and still reads `_options.RequireAuthenticatedByDefault` on every request, only the reflection and the sort move into the static profile. The list returned to `AuthorizeAsync` is the cached array (read-only); `AuthorizeAsync`, `DenyMissingContext` and `EvaluateAttributesAsync` all take `IReadOnlyList<SecurityAttribute>` instead of `List<SecurityAttribute>` (private changes).
2. **Tests**: attribute order unchanged (`Order`, then type priority) on the first and later calls; `[AllowAnonymous]` still bypasses and still logs on each call; `RequireAuthenticatedByDefault` toggled between two behavior instances with different options still changes the outcome (the option is not cached); two closed generics do not share a profile; the denial and allow results of every existing test are unchanged.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 2</strong></summary>

```
CONTEXT:
Phase 1 is merged. SecurityPipelineBehavior.DiscoverAttributes runs two GetCustomAttributes calls and a LINQ sort per request.

TASK:
Move the reflection and the ordering into a static per-closed-generic profile; keep options and logging per call.

KEY RULES:
- RequireAuthenticatedByDefault is an instance option: never cache its effect.
- AllowAnonymousBypass is logged on every request exactly as today.
- The security gate keeps failing closed: a missing security context still denies; no existing test changes its expectation.
- No public API change.

REFERENCE FILES:
- src/Encina.Security/SecurityPipelineBehavior.cs
- tests/Encina.UnitTests/Security/SecurityPipelineBehaviorTests.cs
- src/Encina.Security.Audit/AuditPipelineBehavior.cs (ShouldAuditCache)
```

</details>

### Phase 3: Observability, logging and coverage targets

<details>
<summary><strong>Tasks</strong></summary>

1. **No EventIds and no new range**: the caches are silent (no log on a hit or a miss); a failing lookup throws as before and the existing behaviors already log it.
2. **No telemetry**: ADR-054 D4 (tenant tag when multi-tenancy is on) does not apply because no span or metric is added; the caches hold no tenant, user or claim data, so there is nothing to tag.
3. **No new error code**, so no ADR-062 category is introduced.
4. **Coverage targets (per flag, new or touched files)**, entered by `mechanical-fixer` in the PR that adds the tests:

| File | Unit | Guard | Contract | Integration | Justification |
|------|:----:|:-----:|:--------:|:-----------:|---------------|
| `Encina.Security.Audit/RequestAuditMetadata.cs` | 95 | 0 | 0 | 0 | One small internal type driven completely by unit tests; no public parameter and no InternalsVisibleTo for the guard project, so guard tests reach it only through the factory whose guard target stays |
| `Encina.Security.Audit/DefaultOperationAuditEntryFactory.cs` | keeps 100 | keeps 90 | 0 | 0 | The existing targets are not lowered; the changed lines are covered by the factory unit tests |
| `Encina.Security.Audit/DefaultSensitiveDataRedactor.cs` | keeps 98 | keeps 87 | 0 | 0 | The new cache branch is covered by the redactor unit tests |
| `Encina.Security/SecurityPipelineBehavior.cs` | keeps 90 | keeps 70 | keeps 60 | 0 | The new static profile is exercised by every behavior test |

5. **Property, load, integration**: no property test (no non-trivial invariant beyond equality of cached and uncached results, asserted in the unit tests); no integration test (no I/O); a load test is not written because the only shared state is a `ConcurrentDictionary` covered by the concurrent unit test; the manifest `reason` of each flag says so.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 3</strong></summary>

```
CONTEXT:
The caches and tests exist. The manifest entries remain.

TASK:
Measure per-flag coverage of the touched files with coverage-report.cs and record targets with one-sentence justifications
through mechanical-fixer; run --check-justifications.

KEY RULES:
- Never lower an existing target (target-ratchet).
- Add no log, metric or error code: the plan reserves none.

REFERENCE FILES:
- .github/coverage-manifest/Encina.Security.Audit.json
- .github/coverage-manifest/Encina.Security.json
```

</details>

### Phase 4: Documentation and finalization

<details>
<summary><strong>Tasks</strong></summary>

1. XML documentation on `RequestAuditMetadata` (internal, remarks only) and the changed members; no new public API, so no `PublicAPI.Unshipped.txt` change.
2. The changelog fragments named in the phases (section `changed`: "audit entry creation and the security attribute discovery resolve their type metadata once per request type"); the fragment states that the issue's regex and `SecurityContext` items were not implemented and why.
3. `docs-writer`: the audit and security guides mention nothing about caching today; no page changes unless the benchmark result is worth recording (then the observability or performance guide gets one sentence citing the benchmark file, never a typed figure).
4. Verification: `dotnet build --configuration Release` with zero warnings; `dotnet test` of `Encina.UnitTests` (Security namespaces) and `Encina.GuardTests`; per-flag coverage and the CRAP table of changed methods.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 4</strong></summary>

```
CONTEXT:
Phases 1 to 3 are done. Finalization remains.

TASK:
Confirm XML docs, the two changelog fragments and a clean Release build; run the unit and guard projects for the Security namespaces.

KEY RULES:
- No hand-typed performance figures in documentation or the fragment; cite the benchmark file.
- Fragment section is changed; never edit CHANGELOG.md.

REFERENCE FILES:
- changelog.d/README.md
- tests/Encina.BenchmarkTests/Encina.Benchmarks/Security/Audit/AuditBenchmark.md
```

</details>

---

## Research

### Relevant Standards & Specifications

| Standard | Relevance |
|----------|-----------|
| AGENTS.md section 3 | Security gates fail closed; errors are never swallowed |
| AGENTS.md section 6 | Cross-cutting evaluation (12 functions) |
| AGENTS.md section 9 | Per-flag coverage targets, CRAP at most 10 on changed methods, `BenchmarkSwitcher` |
| ADR-036 | Operation audit store and entry factory |
| ADR-054 D4 | Tenant tag on telemetry; not applicable, no telemetry added |

### Existing Encina Infrastructure to Leverage

| Component | Location | Usage in This Feature |
|-----------|----------|----------------------|
| `DefaultOperationAuditEntryFactory` | `src/Encina.Security.Audit` | Per-request attribute and convention lookups to cache |
| `RequestMetadataExtractor` | `src/Encina.Security.Audit` | `ExtractFromTypeName` (source generated regex) and the existing entity-id cache pattern |
| `DefaultSensitiveDataRedactor` | `src/Encina.Security.Audit` | Effective sensitive-field set per call |
| `AuditPipelineBehavior.ShouldAuditCache` | `src/Encina.Security.Audit/AuditPipelineBehavior.cs` | The pattern to extend |
| `SecurityPipelineBehavior.DiscoverAttributes` | `src/Encina.Security` | Per-request reflection to cache |
| `SecurityContext` | `src/Encina.Security/SecurityContext.cs` | Evaluated and left unchanged |

### Event ID Allocation

| Package | Range | Notes |
|---------|-------|-------|
| none | none | No logging is added; no range is registered |

### Estimated File Count

| Category | New Files | Modified Files | Notes |
|----------|-----------|----------------|-------|
| Production code | 1 | 3 | New: `RequestAuditMetadata`; modified: the factory, the redactor, `SecurityPipelineBehavior` |
| Tests | 2 | 4 | New: `RequestAuditMetadataTests`, the benchmark class; modified: factory, redactor and behavior tests, `AuditBenchmark.md` |
| Documentation | 0 | 0 | None (Phase 4) |
| Configuration | 2 | 2 | Two changelog fragments; two manifests |
| **Total** | **5** | **9** | 2 pull requests: 10 and 4 files, each at most 15 files and about 400 lines |

---

## Combined AI Agent Prompts

<details>
<summary><strong>Complete Implementation Prompt - All Phases</strong></summary>

```
PROJECT CONTEXT:
Encina is a .NET 10 / C# 14 library, pre-1.0, no backward compatibility. Issue #707 caches type-level metadata that the audit
factory and the security behavior resolve by reflection on every request.

IMPLEMENTATION OVERVIEW:
1. Benchmark DefaultOperationAuditEntryFactory first; report the numbers and wait for the orchestrator.
2. RequestAuditMetadata (static ConcurrentDictionary<Type, ...>) used by the factory.
3. Reference-keyed INSTANCE cache of the effective sensitive-field set in DefaultSensitiveDataRedactor.
4. Static per-closed-generic attribute profile in SecurityPipelineBehavior.
5. No SecurityContext cache, no regex cache (the premises do not hold), no logging, no telemetry, no new error code.
Two pull requests: Phase 1, Phase 2.

KEY PATTERNS:
- Cache only successful pure results keyed by Type; masking and authorization stay fail closed.
- Options-dependent decisions (RequireAuthenticatedByDefault, exclusion lists) are never cached.
- Apply Design Choices 1, 2, 4 and 6 as the Maintainer Decisions section says.

REFERENCE FILES:
- src/Encina.Security.Audit/DefaultOperationAuditEntryFactory.cs
- src/Encina.Security.Audit/DefaultSensitiveDataRedactor.cs
- src/Encina.Security/SecurityPipelineBehavior.cs
- .github/coverage-manifest/Encina.Security.Audit.json
- .github/coverage-manifest/Encina.Security.json
```

</details>

---

## Cross-Cutting Integration Matrix

| # | Function | Status | Notes |
|---|----------|--------|-------|
| 1 | Caching | ✅ Include | In-process caches (static `ConcurrentDictionary` per type; per-instance `ConditionalWeakTable` in the redactor), not `ICacheProvider`. |
| 2 | OpenTelemetry | ❌ N/A | No telemetry is added; the caches hold no tenant data, so no tenant tag applies (ADR-054 D4). |
| 3 | Structured Logging | ❌ N/A | The caches are silent; no EventId range is needed. |
| 4 | Health Checks | ❌ N/A | No external dependency. |
| 5 | Validation | ❌ N/A | No new input. |
| 6 | Resilience | ❌ N/A | No external call. |
| 7 | Distributed Locks | ❌ N/A | `ConcurrentDictionary.GetOrAdd` needs no lock; the factory function is pure. |
| 8 | Transactions | ❌ N/A | No mutation outside the process. |
| 9 | Idempotency | ❌ N/A | No message or request is deduplicated; `GetOrAdd` may run its pure factory twice for one key, which is not ADR-018 idempotency (duplicate messages). |
| 10 | Multi-Tenancy | ✅ Include | Caches are keyed by `Type` only and never hold tenant, user or claim values; `SecurityContext` is not cached for that reason (Design Choice 2). |
| 11 | Module Isolation | ❌ N/A | Not module scoped. |
| 12 | Audit Trail | ✅ Include | The audit entry content and the fail-closed masking must not change; the unit tests compare cached and uncached entries. |

**Deferred items:** none.

---

## Prerequisites & Dependencies

| Prerequisite | Status | Notes |
|--------------|--------|-------|
| `ShouldAuditCache` and `EntityIdPropertyCache` patterns | ✅ Available | The pattern to follow |
| `Encina.Benchmarks` references `Encina.Security.Audit` | ✅ Available | `tests/Encina.BenchmarkTests/Encina.Benchmarks/Encina.Benchmarks.csproj` |
| Maintainer answers to Design Choices 1, 2, 4 and 6 | Pending | Asked when work starts |

No blocking prerequisites.

---

## Next Steps

1. The orchestrator asks the maintainer the Dilemmas (Design Choices 1, 2, 4, 6) when work starts, records the answers under `## Maintainer Decisions`, then briefs two workers (Phase 1, Phase 2).
2. After merge, comment on #707 with the benchmark result and the two items that were not implemented, with the reason.
