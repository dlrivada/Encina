# Implementation Plan: Cached value inspection in cache diagnostics, behind an explicit policy

> **Issue**: [#4415](https://github.com/dlrivada/Encina/issues/4415)
> **Type**: Feature
> **Complexity**: Medium (3 code pull requests of 9, 15 and 7 files, and a documentation pull request of 2 files)
> **Estimated Scope**: about 450 lines of production code, about 1,100 lines of tests
> **Milestone**: v0.18.0 — Integration & Platform Capabilities

---

## Summary

Issue #4415 lets the cache diagnostics of #276 (open, plan merged) return the deserialized cached value of one entry, only with `Diagnostics.ShowValues` on and behind a second authorization policy. This is a security and personal-data feature: cached values can hold personal data (the reference application caches health-related query results), so every rule below fails closed and no value is ever written to a log, a tag or the outbox. Re-checked against `origin/main` (326e65ebf):

| Premise of the issue | State in the code and the plans |
|----------------------|---------------------------------|
| The endpoint "re-reads the entry through `ICacheProvider` and the serialization codec of #274" | `ICacheProvider.GetAsync<T>(key, ct)` needs the closed type. The behavior stores `CacheEntry<TResponse>` (`GetAsync<CacheEntry<TResponse>>`, `src/Encina.Caching/Behaviors/QueryCachingPipelineBehavior.cs:212`; the class is `internal sealed`, line 353, with `Value` and `CachedAtUtc`), and only the behavior knows `TResponse`. A non-generic endpoint cannot make that call. The plan therefore stores, per registry entry, a value reader delegate that the behavior builds for its own closed type (it holds no value). The codec of #274 is used by the provider inside `GetAsync`, so the re-read decodes through it with no direct call. |
| The registry may supply the value | It must not: #276 states the registry never holds a value; the delegate re-reads from the provider every time. |
| One details endpoint | #276's `GET {prefix}/keys/{id}` returns metadata. A separate route `GET {prefix}/keys/{id}/value` carries the second policy, so metadata access never changes (Design Choice 4). |
| A second policy is easy to add | `MapDPIAEndpoints` applies the policy to a group with `group.RequireAuthorization(authorizationPolicy)` and validates it at startup (`src/Encina.AspNetCore/DPIAEndpointExtensions.cs:150-154`, `:529`, `:536`); #276's `MapEncinaCacheDiagnostics(authorizationPolicy, prefix)` copies that, and the issue's `valuesPolicy:` is an added optional parameter. |
| A value read is an audit event | `IReadAuditStore` ("who read which data and why", `src/Encina.Security.Audit/Abstractions/IReadAuditStore.cs`, `ReadAuditEntry` with `EntityType`, `EntityId`, `UserId`, `TenantId`, `Purpose`, `AccessMethod`, `EntityCount`) exists; ADR-036 keeps read evidence in its own store (Design Choice 2). `Encina.Caching` has no reference to `Encina.Security.Audit` today; #4414 adds it. |
| Tenancy | There is no `ITenantContext`; the tenant is `IRequestContext.TenantId`, and `MultiTenancyMarker` (`src/Encina/Abstractions/MultiTenancyMarker.cs`) is the signal that multi-tenancy is on. |

The plan adds the registry value reader, an `ICacheValueInspector` with an internal implementation (write-ahead read audit, tenant check, size limit), the `ShowValues` option and the second policy on the mapping. Caching (8): the re-read goes through `ICacheProvider`, so it behaves identically on Memory, Hybrid, Redis, Valkey, Dragonfly, Garnet, KeyDB and the planned Memcached of #277.

**Shared pieces and ownership (family cache-patterns-2):** #276 owns the registry, `ICacheDiagnostics`, the options and the endpoints; this plan modifies them after #276 PR 3. #4414 owns `ICacheAuditRecorder`, `CacheAuditEvent` (with the `EntityId` this plan fills with the opaque id), `CacheAuditSchema` and the `CachingTenantAudit` range 528-533; this plan consumes the recorder and its `ValueRead` kind (unmapped in #4414 PR 1, where it returns the `Left`; this plan's PR 2 adds the `IReadAuditStore` mapping) and so starts after #4414 PR 1. #4416 owns the single endpoint identity helper `src/Encina.AspNetCore/CacheDiagnosticsIdentity.cs` (`cache.diagnostics_identity_required`); this plan's PR 3 reuses it and so starts after #4416 PR 1 (the order #4414, #4416, #4415 is fixed). This plan owns `CachingValueInspection` 534-537.

**Affected packages:** `Encina.Caching` (reader, inspector, option), `Encina.AspNetCore` (route and policy), `Encina` (EventId registry). **Provider category:** Caching (8), behavior-level.

---

## Design Choices

<details>
<summary><strong>1. Whether and how values can be read (Dilemma D1, the issue's D1 to D3)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) A second required policy name, `ShowValues` default false, tenant scoped, every read audited** | The safest readable option; usable in staging | A production-reachable path to personal data exists when both are on |
| **B) Development environment only: the mapping throws when `IHostEnvironment` is not Development** | Production can never read values | No inspection in staging or production incidents |
| **C) No value inspection (the state of #276)** | Nothing to leak | The developer cannot see a wrong cached result |

### Chosen Option: **A - second policy, option, tenant scoped** (recommended, pending the maintainer)

### Rationale

- Option B wins when production access to values is never wanted; C wins for health-data deployments that accept no path at all. Every rule below (tenant check, fail-closed audit, no logging of values) also holds under B. Under C there is no work.
- Class: Dilemma

</details>

<details>
<summary><strong>2. Which audit store records a value read (Dilemma D2)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `IReadAuditStore` through `ICacheAuditRecorder`: `AccessMethod.Api`, `EntityType` `CacheEntry`, `EntityId` the opaque id, `EntityCount` 1, `Purpose` the constant `cache-diagnostics`** | The store designed for "who read which data" (ADR-036, SPEC-002 REQ-007), with its own retention | An application that registers only the operation store must also register a read store |
| **B) `IOperationAuditStore` with `Action` `CacheValueRead`** | One store for all cache audit (#4414, #4416) | Read volume mixed with administrative evidence; the read store's retention rules do not apply |

### Chosen Option: **A - read audit store** (recommended, pending the maintainer)

### Rationale

- Option B wins in a deployment that runs only the operation store. The recorder dispatches by event kind, so the choice changes one mapping. A start validator fails the host when `ShowValues` is on and the chosen store has no descriptor.
- Class: Dilemma

</details>

<details>
<summary><strong>3. The read is audited before the value is returned (Dilemma D3)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Write-ahead, fail closed: the audit event is written first; when the write fails the value is not returned and the endpoint answers `cache.diagnostics_value_audit_failed` (ADR-062 `ErrorCategory.Internal`, rule R4: a store failure is `Internal`; constant message "The cache value read could not be audited.")** | No disclosure of personal data without evidence; the pattern of the ABAC decision audit (#751) | An audit outage blocks value inspection |
| **B) Return the value, then audit best effort (the shape #4416 recommends for deletes)** | Inspection survives an audit outage | Personal data read with no evidence |

### Chosen Option: **A - write-ahead, fail closed** (recommended, pending the maintainer)

### Rationale

- A value read discloses data that can be personal; a delete does not. Option B wins only where value inspection is a development convenience and an audit outage must not block it. This is a Dilemma, like the same question in #4416, so the two plans agree on the class and may differ in the recommendation for the reason above; AGENTS.md section 3 requires gates to fail closed when a lookup fails, which supports A but does not cover a failed audit write by itself.
- Cancellation propagates as `OperationCanceledException`, never as a `Left`.
- Class: Dilemma

</details>

<details>
<summary><strong>4. A separate route, a separate interface and tenant scoping</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `GET {prefix}/keys/{id}/value` in its own route group with the second policy; `ICacheValueInspector` is a separate public interface registered next to `ICacheDiagnostics`; the id must belong to the caller's tenant (string equality), otherwise `cache.diagnostics_key_not_found`** | Metadata access is unchanged; the policy is applied by the framework, not by an imperative check; `ICacheDiagnostics` keeps its public API | Two route groups |
| **B) Add the value to the details response and a member to `ICacheDiagnostics`** | One route | The details policy would then expose values; an interface change |

### Chosen Option: **A - separate route and interface**

### Rationale

- **Class:** Trivial
- **Settled by:** the precedent `DPIAEndpointExtensions.cs:153-154` (a policy applied per `MapGroup`) and AGENTS.md section 1 (pay-for-what-you-use). Tenant rule: under multi-tenancy a missing tenant is denied with `cache.diagnostics_tenant_required` (`InvalidRequest`, created by #276); another tenant's id is indistinguishable from an absent one (`cache.diagnostics_key_not_found`, `NotFound`, ADR-062 R1: a runtime entity addressed by a caller-supplied id).

</details>

<details>
<summary><strong>5. What the response holds when the value cannot be shown (Dilemma D5)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) A 200 answer with `valueOmitted` set to `too_large` (above `MaxValueBytes`, default 65,536) or `not_serializable`, and no value** | No new error code; the audit event of the attempt exists either way | The caller must read a field to know |
| **B) An error answer for both cases** | Explicit | A category would have to be invented (`FailedPrecondition` is forbidden before #3420) |

### Chosen Option: **A - omit with a reason** (recommended, pending the maintainer)

### Rationale

- Option B wins for clients that treat any missing value as a failure; the category would then need the maintainer's choice. The value is serialized with `System.Text.Json` from its runtime type and the response carries `Cache-Control: no-store` and `Content-Type: application/json`. The value is never logged (EventId 536 carries the reason only) and never reaches an `Activity` tag, a metric or the outbox.
- Class: Dilemma

</details>

<details>
<summary><strong>6. Attempts on an id that does not exist (Dilemma D6)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Audit every attempt that passes the option and tenant checks, found or not: the event is written after the registry lookup and before any value is read, with `Metadata` `found` true or false (an id of another tenant counts as not found); an id that is not exactly 64 lowercase hexadecimal characters is rejected before the lookup with `cache.diagnostics_key_not_found` and writes no event, so arbitrary caller text never reaches the plaintext read-audit row** | Probing existing-shaped ids leaves evidence; no extra round trip; no caller-chosen text is stored | A read store row per well-formed probe; a malformed probe leaves no audit trace by design (it can reach nothing, and it is not stored) |
| **B) Audit only reads that find an entry** | Fewer rows | A caller holding both policies can probe ids with no trace |

### Chosen Option: **A - audit attempts** (recommended, pending the maintainer)

### Rationale

- Option B wins when read-store volume matters more than probe evidence; ids are opaque hashes, so probing is already impractical, which is why B is a valid choice. The metadata key `found` is one of the names #4414 owns in `CacheAuditSchema` (Design Choice 3b there); this plan adds no constant and does not edit that file. An id of the wrong shape is never audited (Design Choice 6, shape check).
- Class: Dilemma

</details>

---

## Implementation Phases

### Phase 1: The value reader on the registry (pull request 1)

<details>
<summary><strong>Tasks</strong></summary>

PR 1 needs #276 PR 1 merged and holds exactly these 9 files:

| # | File | Change |
|---|------|--------|
| 1 | `src/Encina.Caching/Abstractions/CacheValueReader.cs` | new (public delegate `ValueTask<object?> CacheValueReader(ICacheProvider provider, string key, CancellationToken cancellationToken)`) |
| 2 | `src/Encina.Caching/Diagnostics/CacheKeyRegistry.cs` | modify (an entry keeps its optional reader; never a value) |
| 3 | `src/Encina.Caching/Diagnostics/CacheOutcomeAccessor.cs` | modify (`ICacheKeyRecorder.Record` gains a trailing `CacheValueReader? reader = null`) |
| 4 | `src/Encina.Caching/Behaviors/QueryCachingPipelineBehavior.cs` | modify (a static per closed type reader, passed at the existing `Record` call) |
| 5 | `src/Encina.Caching/PublicAPI.Unshipped.txt` | modify (`mechanical-fixer`: the delegate and the changed `Record` line, RS0017) |
| 6 | `tests/Encina.UnitTests/Caching/CacheValueReaderTests.cs` | new |
| 7 | `tests/Encina.GuardTests/Caching/CacheDiagnosticsGuardTests.cs` | modify (a valid-result test for a null reader, #3798; file created by #276 PR 1) |
| 8 | `.github/coverage-manifest/Encina.Caching.json` | modify (`mechanical-fixer`) |
| 9 | `changelog.d/4415-cache-value-reader.added.md` | new (`mechanical-fixer`) |

Tasks:

1. **Create** the delegate. In the behavior add `private static readonly CacheValueReader ValueReader = async (provider, key, ct) => (await provider.GetAsync<CacheEntry<TResponse>>(key, ct))?.Value;` (a static field per closed generic type is safe here: it holds a stateless delegate, no options or instance state), passed to `ICacheKeyRecorder.Record` only when a recorder exists.
2. **Interface change:** `ICacheKeyRecorder` is new in #276 and its only implementer on `origin/main` is none (a grep of `src` and `tests` for `ICacheKeyRecorder` finds no file today); after #276 PR 1 the implementers are `CacheKeyRegistry` and any test double there: the grep is repeated when the PR starts and every double is updated in this PR (the optional trailing parameter keeps callers compiling).
3. **Tests:** the reader returns the stored value through a real `MemoryCacheProvider`; a miss returns null; a registry entry exposes its reader and never a value; with diagnostics off no reader is allocated; with the parameter null the behavior behaves as before.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 1</strong></summary>

```
CONTEXT:
Encina is a .NET 10 / C# 14 library, pre-1.0. #276 PR 1 created CacheKeyRegistry and ICacheKeyRecorder. CacheEntry<T> is internal to
QueryCachingPipelineBehavior.cs; only the behavior knows the response type.

TASK:
1. Create the CacheValueReader delegate; store it per registry entry; pass a static per-closed-type reader from the behavior.
2. Write the unit tests and the null-reader valid-result guard test; ask mechanical-fixer for PublicAPI, the manifest rows and the fragment.

KEY RULES:
- The registry never holds a value; the reader re-reads from the provider on demand.
- With diagnostics off the behavior allocates nothing extra; keep Handle at CRAP 10 or below (extract a private method).
- Update every implementer and test double of ICacheKeyRecorder found by grep.

REFERENCE FILES:
- src/Encina.Caching/Behaviors/QueryCachingPipelineBehavior.cs
- src/Encina.Caching/Abstractions/ICacheProvider.cs
- tests/Encina.UnitTests/Caching/QueryCachingPipelineBehaviorTests.cs
```

</details>

### Phase 2: The inspector, the option, the audit and the EventIds (pull request 2)

<details>
<summary><strong>Tasks</strong></summary>

PR 2 needs PR 1, #4414 PR 1 and #276 PR 2 (the PR that creates `ICacheDiagnostics` with `SearchKeysAsync` and the id lookup that the inspector's registry lookup and the id-shape test call; it builds on #276 PR 1's registry) merged, and holds exactly these 15 files:

| # | File | Change |
|---|------|--------|
| 1 | `src/Encina.Caching/Abstractions/ICacheValueInspector.cs` | new (public interface, `CacheValueRequest` record, `CacheValueView` record) |
| 2 | `src/Encina.Caching/Diagnostics/CacheValueInspector.cs` | new (internal sealed) |
| 3 | `src/Encina.Caching/Configuration/CacheDiagnosticsOptions.cs` | modify (`ShowValues` default false, `MaxValueBytes`) |
| 4 | `src/Encina.Caching/CacheDiagnosticsServiceCollectionExtensions.cs` | modify (register `ICacheValueInspector` through a factory: the real inspector when `ShowValues` is on, resolving the recorder and the read store requirement then, and the null-object `DisabledCacheValueInspector` (a nested internal type in `CacheValueInspector.cs`, answering `cache.diagnostics_values_disabled`) when it is off, so an application that uses diagnostics without audit builds with `ValidateOnBuild`; the start validator of Design Choice 2) |
| 5 | `src/Encina.Caching/Audit/OperationStoreCacheAuditRecorder.cs` | modify (the `ValueRead` kind, unmapped in #4414 PR 1 where it returns the `Left`, now writes a `ReadAuditEntry` with `EntityType` `CacheEntry`, `EntityId` from `CacheAuditEvent.EntityId`, `AccessMethod.Api`, `EntityCount` 1 and `Purpose` `cache-diagnostics` through `IReadAuditStore` in the same per-write scope, taking the metadata names from #4414's `CacheAuditSchema` (including `found`); file created by #4414 PR 1) |
| 6 | `src/Encina.Caching/Diagnostics/CacheValueInspectionLog.cs` | new (EventIds 534-537) |
| 7 | `src/Encina/Diagnostics/EventIdRanges.cs` | modify (`CachingValueInspection = (534, 537)`) |
| 8 | `tests/Encina.UnitTests/Testing/Architecture/EncinaEventIdAllocationTests.cs` | modify (add the range to the `Encina.Caching` entry) |
| 9 | `docs/engineering/eventid-block-registry.md` | modify (a row `CachingValueInspection \| 534-537 \| Caching \| historical` and the block row split; read `EventIdRegistryAgreementTests` first) |
| 10 | `src/Encina/PublicAPI.Unshipped.txt` | modify (`mechanical-fixer`) |
| 11 | `src/Encina.Caching/PublicAPI.Unshipped.txt` | modify (`mechanical-fixer`) |
| 12 | `tests/Encina.UnitTests/Caching/CacheValueInspectorTests.cs` | new (includes the DI and validator assertions) |
| 13 | `tests/Encina.GuardTests/Caching/CacheValueInspectorGuardTests.cs` | new |
| 14 | `.github/coverage-manifest/Encina.Caching.json` | modify (`mechanical-fixer`) |
| 15 | `changelog.d/4415-cache-value-inspector.added.md` | new (`mechanical-fixer`) |

Tasks:

1. **Create** `ICacheValueInspector.GetValueAsync(CacheValueRequest request, CancellationToken ct)` returning `ValueTask<Either<EncinaError, CacheValueView>>`; `CacheValueRequest` carries `TenantId` (`string?`), `Id`, `UserId` (required, non-blank: the endpoint rejects an unauthenticated identity first). `CacheValueView` carries the redacted key, the cached-at instant, the value as a JSON string or the `valueOmitted` reason.
2. **Create** `CacheValueInspector`: in this order: (a) `ShowValues` off returns `cache.diagnostics_values_disabled` (`ErrorCategory.Forbidden`, constant message "Cached value inspection is disabled."); (b) under multi-tenancy (`MultiTenancyMarker` registered, checked with `IServiceProviderIsService`) a null tenant returns `cache.diagnostics_tenant_required`; (c0) the shape check of the caller-supplied id, before anything is stored: the id must be exactly 64 lowercase hexadecimal characters (`[0-9a-f]{64}`, the lowercase hex of a SHA-256 hash). #276's plan says only "an opaque `id` (a hash of the key)" and fixes no shape, so this plan states the shape here as the requirement it places on whichever #276 pull request first computes the id: #276 does not say where, and its plan lists no generator file, so it is #276 PR 1 (the registry that records the key) or #276 PR 2 (`CacheDiagnostics`, where `CacheKeyInfo.Id` is first produced); the briefs of both #276 workers repeat the shape, because this plan cannot edit the merged #276 plan and keeps the check in one internal static `CacheEntryIdFormat.IsWellFormed(string)` nested in `CacheValueInspector.cs` (no new file; the length and the alphabet are its two constants). A unit test takes the ids the real registry generates through `ICacheDiagnostics.SearchKeysAsync` of #276 PR 2 and asserts every one is well formed, so a generator that differs from this rule fails the test instead of silently refusing real ids. A malformed id answers `cache.diagnostics_key_not_found`, writes no audit event and stores nothing, so arbitrary caller text never reaches the plaintext read-audit row; (c) the registry lookup by equality of tenant and id (metadata only, no value is read); (d) the audit through `ICacheAuditRecorder` (`ValueRead`, `EntityId` the presented opaque id, Design Choices 3 and 6) written before any value is read, and a `Left` returns `cache.diagnostics_value_audit_failed` and logs EventId 534 with the code; (d2) only now an unknown or other-tenant id returns `cache.diagnostics_key_not_found`; (e) the reader re-reads the entry from the provider, the value is serialized with `System.Text.Json` from its runtime type, over `MaxValueBytes` or not serializable gives `valueOmitted` (EventId 536 with the reason only), a reader exception is logged by exception type (EventId 537) and answers `valueOmitted` `unavailable`; (f) EventId 535 logs that a value was served, with no key and no tenant. Nothing is logged that came from the value or the key.
3. **Modify** the recorder, options and registration as the table says. The validator uses `IServiceProviderIsService` and fails the host when `ShowValues` is on and `ICacheAuditRecorder` or the read store descriptor is missing.
4. **Tests:** a malformed or over-long id writes no audit event and returns not found; values returned only with the option on; tenant A never reads tenant B's id (equality, and an id of another tenant equals "not found"); a missing tenant under multi-tenancy is denied before any registry access; an audit `Left` and an audit exception both deny and log 534 and the value is never returned; one `ReadAuditEntry` per read with user, tenant, id and purpose and no key or value; over-size and non-serializable values give `valueOmitted`; a throwing reader gives 537 and no leak; logs captured by a fake logger never contain the value; cancellation propagates; DI is built with `ValidateOnBuild` and `ValidateScopes`. Both DI states are tested with `ValidateOnBuild` and `ValidateScopes`: `ShowValues` off without any audit registration builds and answers `cache.diagnostics_values_disabled`; on without the recorder fails the start validator. Contract of the arguments (decided): a null request throws `ArgumentNullException` and a blank `Id` or `UserId` throws `ArgumentException` (the endpoint always supplies them, so a blank one is a programming error, never a `Left`); an unknown id is the `Left` `cache.diagnostics_key_not_found`. Guard tests go through the public `ICacheValueInspector` resolved from a provider built by the registration (the internal class is not constructible from `Encina.GuardTests`, which `Encina.Caching` grants no `InternalsVisibleTo`): null request, blank `Id`, blank `UserId`, null services in the registration; a null `TenantId` is valid in single-tenant mode and gets a valid-result test.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 2</strong></summary>

```
CONTEXT:
PR 1 is merged: registry entries hold a CacheValueReader. #4414 PR 1 created ICacheAuditRecorder and OperationStoreCacheAuditRecorder.
IReadAuditStore and ReadAuditEntry are in src/Encina.Security.Audit.

TASK:
1. Create ICacheValueInspector and CacheValueInspector; add ShowValues and MaxValueBytes; extend the recorder for ValueRead; add the validator.
2. Create CacheValueInspectionLog (EventIds 534-537) and register CachingValueInspection = (534, 537) in EventIdRanges.cs, the assembly map and the registry doc.
3. Write the unit, DI and guard tests; ask mechanical-fixer for the PublicAPI files, the manifest rows and the fragment.

KEY RULES:
- Fail closed everywhere: option off, no tenant, another tenant's id, a failed audit write all deny; the audit is written before the value is read.
- Never log, tag or store a value, a raw key or EncinaError.Message; codes and exception types only.
- Error codes with constant messages and named ADR-062 categories; cancellation propagates, never a Left.
- Follow the maintainer's answers to Design Choices 1, 2, 3, 5 and 6, given in your brief.

REFERENCE FILES:
- src/Encina.Caching/KeyGeneration/CacheKeyRedactor.cs
- src/Encina.Security.Audit/Abstractions/IReadAuditStore.cs
- src/Encina/Abstractions/MultiTenancyMarker.cs
- src/Encina/Errors/ErrorCategory.cs
```

</details>

### Phase 3: The route and the second policy (pull request 3)

<details>
<summary><strong>Tasks</strong></summary>

PR 3 needs PR 2, #276 PR 3 and #4416 PR 1 merged and holds exactly these 7 files:

| # | File | Change |
|---|------|--------|
| 1 | `src/Encina.AspNetCore/CacheDiagnosticsEndpointExtensions.cs` | modify (`valuesPolicy` parameter, the value route group, the startup rules, the shared identity helper) |
| 2 | `src/Encina.AspNetCore/PublicAPI.Unshipped.txt` | modify (`mechanical-fixer`: the changed `MapEncinaCacheDiagnostics` signature, RS0017) |
| 3 | `tests/Encina.UnitTests/AspNetCore/CacheValueEndpointTests.cs` | new (test server) |
| 4 | `tests/Encina.GuardTests/AspNetCore/CacheDiagnosticsEndpointGuardTests.cs` | modify (blank `valuesPolicy`; file created by #276 PR 3) |
| 5 | `tests/Encina.IntegrationTests/Infrastructure/Caching/CacheValueInspectionIntegrationTests.cs` | new (shared Redis fixture) |
| 6 | `.github/coverage-manifest/Encina.AspNetCore.json` | modify (`mechanical-fixer`) |
| 7 | `changelog.d/4415-cache-value-endpoint.added.md` | new (`mechanical-fixer`) |

Tasks:

1. **Modify** `MapEncinaCacheDiagnostics(this IEndpointRouteBuilder, string authorizationPolicy, string prefix = "/cache", string? valuesPolicy = null)`: with `ShowValues` on `valuesPolicy` is required (`ArgumentException`-style startup failure naming the parameter), non-blank and registered like the first policy; with `ShowValues` off a non-null `valuesPolicy` throws (an unused second policy hides a misconfiguration); with `valuesPolicy` null the value route is not mapped at all. The route group of `GET {prefix}/keys/{id}/value` requires both policies. The handler reads `IRequestContext`, rejects an unauthenticated `Identity` with `cache.diagnostics_identity_required` (`Unauthenticated`) through `CacheDiagnosticsIdentity.RequireAuthenticated` of #4416, calls `ICacheValueInspector` and maps errors through the existing `ProblemDetailsExtensions`; responses carry `Cache-Control: no-store`.
2. **Tests (test server):** 401 without authentication, 403 with only the first policy, 200 with both, the mapping throws on a blank or unregistered `valuesPolicy` and on the option and parameter mismatch, tenant isolation over HTTP, the audit row exists after a read, the raw key and any value never appear in a header or a log.
3. **Integration test:** a real Redis provider through the shared fixture (`[Trait("Category", "Integration")]`, `[Collection("Redis")]`-style, never its own container): a cached `[Cache]` query result written through the real codec is read back over HTTP, another tenant's id gets 404, and the in-memory read store holds the event.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 3</strong></summary>

```
CONTEXT:
PRs 1 and 2 are merged: the registry holds readers and ICacheValueInspector exists. #276 PR 3 created MapEncinaCacheDiagnostics.

TASK:
1. Add the valuesPolicy parameter and the value route group with both policies; validate at startup.
2. Write the test-server tests and the Redis integration test; ask mechanical-fixer for PublicAPI, the manifest rows and the fragment.

KEY RULES:
- Fail closed: no valuesPolicy means no value route; a policy without the option, or the option without a policy, fails at startup.
- No tenant or identity denies before any data access; no value, raw key or EncinaError.Message in a log, header or tag.
- Integration tests use the shared Redis collection fixture and never call the fixture's DisposeAsync.

REFERENCE FILES:
- src/Encina.AspNetCore/DPIAEndpointExtensions.cs
- tests/Encina.IntegrationTests/Infrastructure/Caching/Fixtures/RedisFixture.cs
- tests/Encina.IntegrationTests/Infrastructure/Caching/RedisCacheProviderIntegrationTests.cs
```

</details>

### Phase 4: Documentation (pull request 4)

<details>
<summary><strong>Tasks</strong></summary>

PR 4 holds exactly these 2 files, written by `docs-writer`: `docs/features/cache-diagnostics.md` (the page #276 creates; a "Reading cached values" section with a clear warning about personal data, the two policies, the audit and what is never logged) and `src/Encina.AspNetCore/README.md` (one line). XML documentation with an `<example>` on every public type is part of PRs 1 to 3.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 4</strong></summary>

```
CONTEXT:
PRs 1 to 3 are merged.

TASK:
Ask docs-writer for the section and the README line; verify links and lint.

KEY RULES:
- No hand-typed figures; cite tests by name; one Diataxis quadrant per page; state that values can hold personal data.

REFERENCE FILES:
- .claude/skills/encina-docs/SKILL.md
- docs/features/cache-diagnostics.md (after #276)
```

</details>

### Phase 5: Cross-cutting integration, observability and coverage targets

<details>
<summary><strong>Tasks</strong></summary>

1. **EventIds:** `CachingValueInspection` 534-537, emitted only by `Encina.Caching`: 534 value read denied because the audit write failed, 535 value served (no key, no tenant), 536 value omitted (reason), 537 value reader failed (exception type). Verified free against the registry (`500-549` assigned to cache-patterns-2) and the sibling plans (#272 500-507, #274 508-513, #275 514-519, #4411 520-523, #4412 524-525, #4413 526-527, #4414 528-533); 538-549 belong to #4417.
2. **Error codes (ADR-062):** `cache.diagnostics_values_disabled` `Forbidden`; `cache.diagnostics_value_audit_failed` `Internal` (ADR-062 R4, a store failure); `cache.diagnostics_identity_required` `Unauthenticated`; `cache.diagnostics_tenant_required` (`InvalidRequest`) and `cache.diagnostics_key_not_found` (`NotFound`) are #276's. All messages are constants.
3. **Coverage targets (per flag, set in the PR that adds each file; measured values go in the PR body):**

| File | Unit | Guard | Contract | Integration | Justification |
|------|:----:|:-----:|:--------:|:-----------:|---------------|
| `Encina.Caching/Abstractions/CacheValueReader.cs` | 0 | 0 | 0 | 0 | A delegate declaration, no executable line |
| `Encina.Caching/Abstractions/ICacheValueInspector.cs` | 0 | 0 | 0 | 0 | An interface and two records without validation |
| `Encina.Caching/Diagnostics/CacheValueInspector.cs` | 95 | 20 | 0 | 0 | Every deny path, the omit reasons and the audit failure are unit tested; the guard flag reaches the argument checks through the public interface in `CacheValueInspectorGuardTests.cs` (the class is internal and `Encina.Caching` grants no `InternalsVisibleTo` to `Encina.GuardTests`), hence 20 |
| `Encina.Caching/Diagnostics/CacheValueInspectionLog.cs` | 90 | 0 | 0 | 0 | Generated bodies run in the inspector tests; internal |
| `Encina.Caching/Behaviors/QueryCachingPipelineBehavior.cs` | 98 | 20 | 0 | 0 | The row on `origin/main` today (unit 98, guard 20, contract 0); the static reader and the pass-through are unit tested; never lowered |
| `Encina.Caching/Diagnostics/CacheKeyRegistry.cs` | 100 | 0 | 0 | 0 | The numbers #276 sets (unit 100, guard 0); the reader field is covered by `CacheValueReaderTests.cs`; no row exists on `origin/main` until #276 PR 1 |
| `Encina.Caching/Diagnostics/CacheOutcomeAccessor.cs` | 100 | 50 | 0 | 0 | The numbers #276 sets (unit 100, guard 50); the optional parameter's valid-result test is in the guard file of PR 1 |
| `Encina.Caching/Configuration/CacheDiagnosticsOptions.cs` | 100 | 80 | 0 | 0 | The numbers #276 sets (unit 100, guard 80); `ShowValues` and `MaxValueBytes` are covered by the inspector tests |
| `Encina.Caching/CacheDiagnosticsServiceCollectionExtensions.cs` | 100 | 100 | 0 | 0 | The numbers #276 sets (unit 100, guard 100); both factory branches are DI tested, and the guard file resolves the real inspector with `ShowValues` on and the null-object with it off so the factory and validator lines run; the worker measures the guard flag and changes the number only with the measurement in the PR body |
| `Encina.Caching/Audit/OperationStoreCacheAuditRecorder.cs` | 95 | 15 | 0 | 0 | The numbers #4414 sets (unit 95, guard 15); the `ValueRead` mapping is covered by `CacheValueInspectorTests.cs` |
| `Encina.AspNetCore/CacheDiagnosticsEndpointExtensions.cs` | 90 | 70 | 0 | 30 | Unit 90 and guard 70 are the numbers #276 sets (no row exists on `origin/main` until #276 PR 3); this plan adds an integration target of 30 in PR 3, the PR that adds the Redis test (the value route and its startup rules run there; the other handlers are not reached) |

   The worker measures every flag per PR with `coverage-report.cs`; no target is lowered (`target-ratchet`) and the CRAP table of changed methods stays at 10 or below.
4. **Property, contract, load, benchmark:** none: one inspector implementation, a development tool off the request path.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 5</strong></summary>

```
CONTEXT:
The code pull requests are merged. Verification remains.

TASK:
Verify the per-flag targets and the EventId registration; run coverage-report.cs --check-justifications; fix omissions through mechanical-fixer.

KEY RULES:
- Never lower an existing target (target-ratchet); add no log, metric or error code beyond the plan.

REFERENCE FILES:
- .github/coverage-manifest/Encina.Caching.json
- .github/coverage-manifest/Encina.AspNetCore.json
```

</details>

---

## Research

### Relevant Standards & Specifications

| Standard | Relevance |
|----------|-----------|
| AGENTS.md section 3 | Gates fail closed; `EncinaError.Message` never reaches logs or plaintext storage |
| ADR-036, SPEC-002 REQ-007 | Read audit as evidence of access to personal data |
| ADR-062 | Categories of the four codes |
| ADR-021 | EventId range registered before use |
| ADR-054 D4 | No telemetry is added, so no tenant tag is needed |
| GDPR Art. 15 and Art. 32 | Records of access and access control to personal data (the reference application caches health data) |

### Existing Encina Infrastructure to Leverage

| Component | Location | Usage in This Feature |
|-----------|----------|----------------------|
| `QueryCachingPipelineBehavior`, `CacheEntry<T>` | `src/Encina.Caching/Behaviors/QueryCachingPipelineBehavior.cs` | Where the closed type is known |
| `CacheKeyRedactor` | `src/Encina.Caching/KeyGeneration/CacheKeyRedactor.cs` | The redacted key in the view |
| `IReadAuditStore`, `ReadAuditEntry` | `src/Encina.Security.Audit/` | The value read evidence |
| `MapDPIAEndpoints` | `src/Encina.AspNetCore/DPIAEndpointExtensions.cs` | Policy per route group, startup validation |
| `MultiTenancyMarker`, `IRequestContext` | `src/Encina/Abstractions/` | Tenancy-on signal; tenant and identity |
| `ICacheAuditRecorder` (#4414), registry and endpoints (#276) | `src/Encina.Caching/`, `src/Encina.AspNetCore/` (after those plans) | Write path, registry, route |
| `RedisFixture` | `tests/Encina.IntegrationTests/Infrastructure/Caching/Fixtures/RedisFixture.cs` | The shared container |

### Event ID Allocation

| Package | Range | Notes |
|---------|-------|-------|
| `Encina.Caching` (new range `CachingValueInspection`, block 500-549) | 534-537 | This plan, see Phase 5 |

### Estimated File Count

| Category | New Files | Modified Files | Notes |
|----------|-----------|----------------|-------|
| Production code | 5 | 11 | Delegate, interface, inspector, log; behavior, registry, accessor, options, DI, recorder, endpoints, ranges, PublicAPI x3 |
| Tests | 5 | 3 | Unit (3), guard (1), integration (1); allocation, guard files |
| Documentation | 0 | 3 | Diagnostics page, README, registry doc |
| Configuration | 3 | 3 | Three fragments; manifests |
| **Total** | **13** | **20** | 4 pull requests of 9, 15, 7 and 2 files |

---

## Combined AI Agent Prompts

<details>
<summary><strong>Complete Implementation Prompt - All Phases</strong></summary>

```
PROJECT CONTEXT:
Encina is a .NET 10 / C# 14 library, pre-1.0. Issue #4415 lets the cache diagnostics of #276 return one cached value behind ShowValues and a
second authorization policy. Cached values can hold personal data: every rule fails closed and nothing from a value is logged.

IMPLEMENTATION OVERVIEW:
1. PR 1: a value reader delegate per registry entry, built by the behavior for its closed type.
2. PR 2: ICacheValueInspector, the option, write-ahead read audit, EventIds 534-537.
3. PR 3: GET {prefix}/keys/{id}/value with the second policy, tests and the Redis integration test.
4. PR 4: documentation by docs-writer.

KEY PATTERNS:
- The audit event is written before the value is read; a failed write denies the read.
- Tenant equality everywhere; a missing tenant or identity denies before data access.
- Follow the maintainer's answers to Design Choices 1, 2, 3, 5 and 6, given in your brief.

REFERENCE FILES:
- src/Encina.Caching/Behaviors/QueryCachingPipelineBehavior.cs
- src/Encina.AspNetCore/DPIAEndpointExtensions.cs
- src/Encina.Security.Audit/Abstractions/IReadAuditStore.cs
- .github/coverage-manifest/Encina.Caching.json
```

</details>

---

## Cross-Cutting Integration Matrix

| # | Function | Status | Notes |
|---|----------|--------|-------|
| 1 | Caching | ✅ Include | Extends #276; the re-read goes through `ICacheProvider` for all 8 providers. |
| 2 | OpenTelemetry | ❌ N/A | A development tool; a value must never reach a tag, so no instrument or span is added. |
| 3 | Structured Logging | ✅ Include | `CacheValueInspectionLog` 534-537; counts and codes only, never the value or the key. |
| 4 | Health Checks | ❌ N/A | No backend of its own. |
| 5 | Validation | ✅ Include | Option, policy and store-presence validation at startup. |
| 6 | Resilience | ❌ N/A | Read-only tooling; an audit outage denies the read by design (fail closed). |
| 7 | Distributed Locks | ❌ N/A | No shared state is coordinated. |
| 8 | Transactions | ❌ N/A | The audit write runs in its own scope. |
| 9 | Idempotency | ❌ N/A | Reads. |
| 10 | Multi-Tenancy | ✅ Include | Only the caller's tenant's entries; a missing tenant is denied. |
| 11 | Module Isolation | ⏭️ Defer | Follows the module dimension of #746 (open). |
| 12 | Audit Trail | ✅ Include | Every value read is audited before it is returned. |

---

## Prerequisites & Dependencies

| Prerequisite | Status | Notes |
|--------------|--------|-------|
| #276 PRs 1 to 3 (registry, `ICacheDiagnostics`, endpoints) | Open | Plan merged |
| #4414 PR 1 (`ICacheAuditRecorder`) | Open | PR 2 of this plan needs it |
| #274 PRs 1 to 3 (codec used by the Redis provider) | Open | The integration test reads through it |
| Maintainer answers to Design Choices 1, 2, 3, 5 and 6 | Pending | Asked when work starts; Design Choice 1 option C ends the work |

Order inside the family: #4414 PR 1, then #4416, then this plan; #4417 is independent.

---

## Next Steps

1. The orchestrator asks the maintainer the Dilemmas when work starts, records the answers under `## Maintainer Decisions`, then briefs the workers in PR order.
