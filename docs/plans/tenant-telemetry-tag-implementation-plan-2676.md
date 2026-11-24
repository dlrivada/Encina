# Implementation Plan: One Tenant Telemetry Rule for Every Package (Shared Helper, Tag Only When Multi-Tenancy Is On, No Baggage)

> **Issue**: [#2676](https://github.com/dlrivada/Encina/issues/2676)
> **Type**: Feature (observability, multi-tenancy)
> **Related**: ADR-054 (D3 one constants class, D4 tenant tag and no baggage), SPEC-002 AC-044, #1791 (spike), #1257 (tenancy of the SPEC-002 capabilities), #721, #722, #723, #724, #725, #759 (Encina-level instrumentation that calls this helper), #726, #727, #222, #273 (observability siblings that call it), #906 (observability guide, owns the cardinality text), #1788 (static scan), ADR-062
> **Complexity**: Medium (a small public helper; the work is migrating 13 existing tag sites in 10 files so no package keeps its own rule)
> **Estimated Scope**: ~150 lines of production code for the helper + ~250 lines of migration + ~900 lines of tests, in seven pull requests of at most 15 files

---

## Summary

### What exists today (read on 2026-11-23, main `1dee4ba61`)

- The issue names `ITenantContext`; **that type does not exist in `src`**. The tenant of a dispatch is `IRequestContext.TenantId` (`src/Encina/Abstractions/IRequestContext.cs`), the ambient context is held by `IRequestContextAccessor` (`RequestContextAccessor` is a singleton backed by an `AsyncLocal`, `src/Encina/Core/RequestContextAccessor.cs`), and "multi-tenancy is enabled" is the presence of the `MultiTenancyMarker` in the container (`src/Encina/Abstractions/MultiTenancyMarker.cs`, registered by `AddEncinaTenancy`, `src/Encina.Tenancy/ServiceCollectionExtensions.cs:109`). `QueryCachingPipelineBehavior` already uses it the way this plan needs: an optional trailing constructor parameter `MultiTenancyMarker? multiTenancy = null` stored as `_multiTenant` (`src/Encina.Caching/Behaviors/QueryCachingPipelineBehavior.cs:74-86`).
- **No package applies one rule.** Thirteen sites in ten files tag the tenant today, under three different keys, and none checks that multi-tenancy is enabled:
  - `encina.tenant_id`: `RetryMetrics.cs:89` (core, `src/Encina/Recoverability/`), `RetryTelemetry.cs:28` (`Encina.Polly`), `OutboundTelemetry.cs:36` and `:54` (`Encina.Refit`), `TransferBlockingPipelineBehavior.cs:196` (`Encina.Compliance.CrossBorderTransfer`), `DPIARequiredPipelineBehavior.cs:197` (`Encina.Compliance.DPIA`), `DataMinimizationPipelineBehavior.cs:237` (`Encina.Compliance.PrivacyByDesign`), `ProcessorValidationPipelineBehavior.cs:199` (`Encina.Compliance.ProcessorAgreements`).
  - `tenancy.tenant_id`: `TenancyActivitySource.cs:61` and `:78` (`Encina.Tenancy`) and the public constant `ActivityTagNames.Tenancy.TenantId` used by `TenancyActivityEnricher.cs:33` and `:53` (`Encina.OpenTelemetry`).
  - `dsr.tenant_id`: `DataSubjectRightsDiagnostics.TagTenantId`, set at `DefaultDSRService.cs:429`.
- The callers of the shared emitters are `RetryPipelineBehavior` (three calls to `RetryMetrics`, one to `RetryTelemetry`), `RecoverabilityPipelineBehavior` (`context.TenantId`, lines 187, 215, 244), `RestApiRequestHandler`, `DelayedRetryProcessor` and `OutboxBatchProcessor`. The last two call `RetryMetrics.RecordSkipped(..., tenantId: null)` (`OutboxBatchProcessor.cs:329`, `DelayedRetryProcessor.cs:384`) because they work on stored messages and neither the outbox message nor the delayed-retry entry carries a tenant (grep of `src/Encina.Messaging/Outbox` and `Recoverability`: no `TenantId` besides these two nulls). They have no tenant source, so this plan does not touch them (Design Choice 6). The other behaviors are built by DI, so an optional trailing constructor parameter keeps every `new X(` site compiling (counted on main: `RecoverabilityPipelineBehavior` 17, `RetryPipelineBehavior` 15, `RestApiRequestHandler` 14, `TransferBlockingPipelineBehavior` 8, `DPIARequiredPipelineBehavior` 12, `DataMinimizationPipelineBehavior` 13, `ProcessorValidationPipelineBehavior` 12, `DefaultDSRService` 31).
- The new instrumentation of the sibling plans (#721 `StoreTelemetry`, #723 `CacheTelemetry`, #724 `InstrumentedDistributedLockProvider`, #725 `TransportTelemetry`) all say "tenant tag through the helper of #2676, in its own pull request". Nothing in them writes a tag of its own, so this plan is the only owner of the key, the on/off rule and the value.
- The issue's provider matrix lists the 10 database providers because their stores will be instrumented by #721, #722 and #759. The `parameters.Add("TenantId", ...)` lines in the ADO.NET and Dapper providers are SQL parameters of tenant isolation, not telemetry, and are not touched.
- `ActivityTagNames` in core is `internal` (`src/Encina/Diagnostics/ActivityTagNames.cs`), so a package other than `Encina.OpenTelemetry` cannot read a constant from it; the helper must be public in core. #177 puts `MessagingSemanticConventions` in the same namespace (`Encina.Diagnostics`), so the namespace is settled by precedent.

### What this plan delivers

- `Encina.Diagnostics.TenantTelemetry`, a `public static class` in core: the constant `AttributeName` (`encina.tenant_id`, Design Choice 2), `ValueFor(IRequestContext? context, bool multiTenancyEnabled)` and `ValueFor(string? tenantId, bool multiTenancyEnabled)` (return the tag value or `null`), `SetTenantTag(this Activity? activity, string? tenantId, bool multiTenancyEnabled)` and `AddTenantTag(this ref TagList tags, string? tenantId, bool multiTenancyEnabled)`. No tag is emitted when multi-tenancy is off, when the tenant is `null`, empty or whitespace; the helper never invents a tenant (no `default`) and never composes an id.
- Migration of the 13 sites: each emitter stops writing its own literal and uses `TenantTelemetry.AttributeName`; each behavior that decides the value takes `MultiTenancyMarker? multiTenancy = null` as its last optional constructor parameter and calls `ValueFor`. A host with no marker gets no tenant tag, which is the fail-to-no-tag path of the issue.
- `ActivityTagNames.Tenancy.TenantId` (public, `Encina.OpenTelemetry`) is removed and its two readers use the helper constant; the `tenancy.*` keys that describe resolution (`strategy`, `entity_type`, `outcome`) are not tenant ids and stay.
- Shared pieces and owner: this plan owns `TenantTelemetry`; #721, #722, #723, #724, #725, #759, #726, #727, #222 and #273 call it; #906 owns the cardinality paragraph of the guide.
- Affected packages: `Encina` (core), `Encina.Polly`, `Encina.Refit`, `Encina.Messaging`, `Encina.Tenancy`, `Encina.OpenTelemetry`, and the compliance packages `CrossBorderTransfer`, `DPIA`, `PrivacyByDesign`, `ProcessorAgreements`, `DataSubjectRights`. Provider category: none (the helper is provider-independent; the database providers call it through the instrumentation of #721, #722 and #759).

---

## Design Choices

<details>
<summary><strong>1. How the helper learns that multi-tenancy is on</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Pure functions that take the flag as a `bool`; each emitter obtains it from the `MultiTenancyMarker` through an optional trailing constructor parameter (the precedent of `QueryCachingPipelineBehavior`)** | No global state; parallel tests and several containers in one process cannot disturb each other; works for singleton, scoped and static emitters alike; fails to no tag when the marker is absent | Eight classes gain one optional constructor parameter |
| **B) A static flag set by `AddEncinaTenancy`** | No constructor change | Process-wide mutable state; two containers (a test host with tenancy and one without) read the last writer; hidden coupling to registration order |
| **C) A singleton policy object registered by `AddEncina` and injected everywhere** | One object to mock | A new registered type; emitters that are static (`RetryMetrics`) cannot take it without a signature change; `AddEncina` would have to know about tenancy |

### Chosen Option: **A - Pure functions and the marker flag** (recommended, pending the maintainer)

### Rationale

- A wins because it is the pattern already on `main`, adds no global state and keeps static emitters static (their callers pass the value). B wins only for a host that cannot change constructors and runs one container per process; C wins if the maintainer wants one injectable switch to turn the tenant tag off for a deployment that has multi-tenancy on.

**Class:** Dilemma

</details>

<details>
<summary><strong>2. The attribute key</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `encina.tenant_id`** | Already used by 8 of the 13 sites (7 files), including the SPEC-002 compliance behaviors; lowercase dotted, in the `encina.*` namespace of ADR-054 D3 | The four `tenancy.tenant_id` sites (2 in `TenancyActivitySource`, 2 in the enricher) and the `dsr.tenant_id` site are renamed |
| **B) `tenancy.tenant_id`** | Already a public constant of `Encina.OpenTelemetry` | Renames the 9 other sites; the `tenancy.*` prefix names a package, not the cross-cutting rule |
| **C) `tenant.id`** | Short; matches the shape of OpenTelemetry names | No OpenTelemetry semantic convention defines a tenant attribute today, so it would look standard and not be; renames all 13 sites |

### Chosen Option: **A - `encina.tenant_id`** (recommended, pending the maintainer)

### Rationale

- A wins on the fewest renames and on the `encina.*` namespace; pre-1.0 means the rename of the other five is done completely, with no alias (`AGENTS.md` section 3). B wins if dashboards of the Tenancy package already exist outside the repository; C wins if OpenTelemetry later standardises `tenant.id`.

**Class:** Dilemma

</details>

<details>
<summary><strong>3. The value that is emitted</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) The tenant id verbatim (ADR-054 D4: "the tenant id is a span attribute and a metric tag")** | Matches the decision and the SPEC-002 AC-044 test ("with the tenant as an attribute"); operators can find a tenant's spans | A tenant id that is a personal name or an email reaches the telemetry backend; unbounded cardinality on metrics (documented, #906) |
| **B) A keyed hash of the id (stable pseudonym)** | No readable tenant in telemetry; the same tenant always gets the same value | Operators cannot read it; needs a secret and rotation rules; ADR-054 D4 says "the tenant id" |
| **C) A configurable choice between A and B** | Fits both deployments | A switch to document and test everywhere; the maintainer excluded user switches elsewhere in ADR-054 |

### Chosen Option: **A - Verbatim id** (recommended, pending the maintainer)

### Rationale

- A wins because ADR-054 D4 and AC-044 name the id and the helper is the single place to change later. B wins for a deployment whose tenant ids are personal data (for example a tenant that is a named person or a small practice): then the value belongs to the data-minimisation reading of GDPR Art. 5(1)(c); C wins if the maintainer wants both and accepts the switch. No other package may write the tenant id into a tag, which is why every call site goes through the helper.

**Class:** Dilemma

</details>

<details>
<summary><strong>4. Migrating the existing sites now</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Migrate all 13 sites in this issue, in small pull requests, and remove `ActivityTagNames.Tenancy.TenantId`** | One rule everywhere; no package keeps its own key or its own unconditional tag | Eight classes gain an optional parameter |
| **B) Add the helper and let each package migrate later** | Smallest first pull request | Three keys and an unconditional tag live on until someone remembers; the acceptance criterion "every package applies it" stays false |

### Chosen Option: **A - Migrate now**

### Rationale

- `AGENTS.md` section 3 ("If something must change, change it completely") and ADR-054 settled point 6 (no compatibility layers, renames done completely) leave no room for keeping the old keys.

**Class:** Settled
**Settled by:** `AGENTS.md` section 3 (no legacy or compatibility layers); ADR-054 settled point 6 and D4

</details>

<details>
<summary><strong>5. Baggage, cardinality, errors and time</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) The helper sets a tag only; it never reads or writes `Baggage`; the cardinality risk is documented in the observability guide (#906) and not capped in code; the helper reads no clock and logs nothing** | Exactly ADR-054 D4; allocation-free when off | A deployment with thousands of tenants must drop the tag in its own view |
| **B) Cap the distinct tenant values and fold the rest into `other`** | Bounded cardinality | State in a static helper; a value that is not the tenant; not decided by ADR-054 |

### Chosen Option: **A - Tag only, documented**

### Rationale

- ADR-054 D4 ("The cardinality risk of the tenant metric tag is documented in the observability guide") and its negative consequences.

**Class:** Trivial
**Settled by:** ADR-054 D4 (no baggage; cardinality documented in #906)

</details>

<details>
<summary><strong>6. The batch processors that have no tenant</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `OutboxBatchProcessor` and `DelayedRetryProcessor` stay un-tagged: their skipped-retry counter keeps `tenantId: null` and this plan does not touch them** | No constructor parameter, no PublicAPI line and no `new X(` churn (28 sites) for a tag that could never be emitted; the honest statement is that stored messages carry no tenant | The skipped-retry counter of the two background processors has no tenant dimension |
| **B) Add a `TenantId` to the outbox message and the delayed-retry entry, then tag per message** | Tenant dimension everywhere | A schema and entity change across all 10 database providers, outside the scope of a telemetry rule; its own issue |

### Chosen Option: **A - Stay un-tagged, no change** (recommended, pending the maintainer)

### Rationale

- A wins because a marker on a processor whose tenant is always `null` can never emit a tag (review of this plan, PR #4183). B wins if the maintainer wants tenant visibility on background retries: it needs a per-message tenant, which belongs to the stores of every provider and is a separate issue.

**Class:** Dilemma

</details>

### Register of defaults and semantics

| Id | Choice | Class | Basis |
|----|--------|-------|-------|
| C1 | No tag when multi-tenancy is off; none when the tenant is `null`, empty or whitespace (a blank tenant is "no tenant", never the reserved `default` segment); the helper never composes an id and never substitutes `default` (whether the value is verbatim or hashed is Design Choice 3) | Settled | ADR-054 D4; SPEC-002 AC-044; fail to no tag |
| C2 | No new `EncinaError` code, so no ADR-062 `ErrorCategory`; the helper returns `null` or a string and never throws on a valid argument; null `activity` returns `null` | Settled | ADR-062; `AGENTS.md` section 3 |
| C3 | No logging and no EventId: the issue puts logs out of scope and the helper is a pure function | Settled | `AGENTS.md` section 7 (no `[LoggerMessage]`, no range) |
| C4 | Gate semantics: the tenant tag is telemetry, not a gate; a missing tenant never denies or changes an operation, it only omits the tag. The fail-closed gates of the packages are untouched | Settled | `AGENTS.md` section 3 (gates fail closed; this is not one) |
| C5 | The marker is read by the emitter's constructor, never by resolving a scoped service from a singleton | Settled | `AGENTS.md` section 3 (no scoped service in a singleton); precedent `QueryCachingPipelineBehavior.cs:74-86` |
| C6 | `RetryMetrics` instrument names with a `_total` suffix (`encina.retry.attempts_total`) are not renamed here; the rename of the plain `Encina` meters and `_total` names belongs to the ADR-054 D3 follow-up issue and #1787 | Settled | ADR-054 settled point 4 and "What each point implies" |

---

## Implementation Phases

### Phase 1: The Helper (pull request T1)

<details>
<summary><strong>Tasks</strong></summary>

1. **Create** `src/Encina/Diagnostics/TenantTelemetry.cs` (`public static class TenantTelemetry`, namespace `Encina.Diagnostics`, XML docs with an `<example>`): `public const string AttributeName = "encina.tenant_id"`; `public static string? ValueFor(IRequestContext? context, bool multiTenancyEnabled)`; `public static string? ValueFor(string? tenantId, bool multiTenancyEnabled)` (returns `null` unless the flag is on and `!string.IsNullOrWhiteSpace(tenantId)`); `public static Activity? SetTenantTag(this Activity? activity, string? tenantId, bool multiTenancyEnabled)` (returns the same activity, so it chains with `SetTag`); `public static void AddTenantTag(this ref TagList tags, string? tenantId, bool multiTenancyEnabled)`. Single-question methods carry `// crap-exempt` only where they are one `switch`; none here needs it (two branches each).
2. **Edit** `src/Encina/PublicAPI.Unshipped.txt` and the manifest `.github/coverage-manifest/Encina.json` (per-file entry, targets below) and the changelog fragment `changelog.d/2676-tenant-telemetry-helper.added.md`, all through `mechanical-fixer`.
3. Tests: new `tests/Encina.UnitTests/Diagnostics/TenantTelemetryTests.cs` (flag off with a tenant: no tag; flag on with `null`, empty and whitespace: no tag; flag on with a tenant: exactly one tag with the key `encina.tenant_id` and the id as Design Choice 3 settles it (verbatim under the recommended option A); the helper never reads `Activity.Current.Baggage` and sets none; allocation-free when off, measured with `GC.GetAllocatedBytesForCurrentThread`); new `tests/Encina.GuardTests/Core/Diagnostics/TenantTelemetryGuardTests.cs` (null `IRequestContext` is a valid input and returns `null` with a valid-result test; null `Activity` returns `null`).

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 1</strong></summary>

```text
CONTEXT:
Encina (.NET 10, C# 14, pre-1.0). Issue #2676, plan docs/plans/tenant-telemetry-tag-implementation-plan-2676.md, phase 1.
ADR-054 D4: the tenant id is a span attribute and a metric tag only when multi-tenancy is enabled; no baggage.
"Enabled" is the MultiTenancyMarker in the container (src/Encina/Abstractions/MultiTenancyMarker.cs). ITenantContext does not exist.

TASK:
1. Create src/Encina/Diagnostics/TenantTelemetry.cs (public static, namespace Encina.Diagnostics) with AttributeName
   ("encina.tenant_id"), two ValueFor overloads, SetTenantTag(this Activity?) and AddTenantTag(this ref TagList).
2. Unit tests and guard tests as in the plan; PublicAPI, manifest and changelog through mechanical-fixer.

KEY RULES:
- No tag when the flag is off or the tenant is null, empty or whitespace; never the string "default"; never baggage.
- Nullable parameters that accept null get valid-result tests in Encina.GuardTests, not exception tests.

REFERENCE FILES:
- src/Encina/Abstractions/MultiTenancyMarker.cs, src/Encina.Caching/Behaviors/QueryCachingPipelineBehavior.cs (lines 53-86)
- docs/architecture/adr/054-observability-conventions-and-trace-propagation.md (D4)
```

</details>

---

### Phase 2: Polly and the Retry Metrics (pull request T2)

<details>
<summary><strong>Tasks</strong></summary>

1. **Modify** `src/Encina/Recoverability/RetryMetrics.cs` (line 89: `tags.Add(TenantTelemetry.AttributeName, tenantId)`; the signature `RecordAttempt(string requestName, string errorCode, string? tenantId)` is unchanged because the caller passes the already-decided value).
2. **Modify** `src/Encina.Polly/Diagnostics/RetryTelemetry.cs` (line 28 uses the constant) and `src/Encina.Polly/Behaviors/RetryPipelineBehavior.cs`: add `MultiTenancyMarker? multiTenancy = null` as the last constructor parameter and compute `var tenant = TenantTelemetry.ValueFor(context, _multiTenant)` once, passing it to `RetryMetrics.RecordAttempt` (lines 132, 138, 209) and `RetryTelemetry.StartRetry` (line 74).
3. `src/Encina.Polly/PublicAPI.Unshipped.txt` (constructor line), changelog `changelog.d/2676-tenant-telemetry-polly.changed.md`, through `mechanical-fixer`.
4. Tests: edit `tests/Encina.UnitTests/Polly/RetryPipelineBehaviorOutboundTests.cs` (tenant tag present only with the marker; absent without it; blank tenant) and `tests/Encina.UnitTests/Recoverability/OutboundRulesTests.cs` (key constant); new `tests/Encina.GuardTests/Polly/RetryPipelineBehaviorTenantGuardTests.cs` (null marker is valid and yields no tag).

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 2</strong></summary>

```text
CONTEXT:
Issue #2676 plan docs/plans/tenant-telemetry-tag-implementation-plan-2676.md, phase 2. Phase 1 merged (TenantTelemetry exists in core).

TASK:
1. Replace the literal "encina.tenant_id" in RetryMetrics and RetryTelemetry with TenantTelemetry.AttributeName.
2. RetryPipelineBehavior takes MultiTenancyMarker? multiTenancy = null (last parameter) and passes TenantTelemetry.ValueFor(context, flag).
3. Tests: tenant tag with and without the marker, blank tenant; PublicAPI and changelog through mechanical-fixer.

KEY RULES:
- Optional parameter last (CS1737); existing "new RetryPipelineBehavior(" sites (15 on main) must still compile.
- No EncinaError.Message or raw exception message in any tag.

REFERENCE FILES:
- src/Encina.Polly/Behaviors/RetryPipelineBehavior.cs, src/Encina.Polly/Diagnostics/RetryTelemetry.cs, src/Encina/Recoverability/RetryMetrics.cs
```

</details>

---

### Phase 3: Refit and the Recoverability Behavior (pull request T3)

<details>
<summary><strong>Tasks</strong></summary>

1. **Modify** `src/Encina.Refit/Diagnostics/OutboundTelemetry.cs` (lines 36 and 54 use the constant) and `src/Encina.Refit/Handlers/RestApiRequestHandler.cs` (optional `MultiTenancyMarker? multiTenancy = null`, value through `ValueFor`).
2. **Modify** `src/Encina.Messaging/Recoverability/RecoverabilityPipelineBehavior.cs`: the optional parameter on **both** constructors (public, line 65, and internal, line 76) and `TenantTelemetry.ValueFor(context, _multiTenant)` for the `context.TenantId` it already passes at lines 187, 215 and 244. `OutboxBatchProcessor` and `DelayedRetryProcessor` are not changed: they pass `tenantId: null` and have no tenant source (Design Choice 6).
3. `src/Encina.Refit/PublicAPI.Unshipped.txt` and `src/Encina.Messaging/PublicAPI.Unshipped.txt` (the public constructor line of each; the internal constructor has none) and changelog `changelog.d/2676-tenant-telemetry-refit-messaging.changed.md` through `mechanical-fixer`.
4. Tests: edit `tests/Encina.UnitTests/Refit/RestApiRequestHandlerOutboundTests.cs`; new `tests/Encina.UnitTests/Messaging/Recoverability/RecoverabilityTenantTagTests.cs` (tag present with the marker and a tenant, absent without the marker, absent for a blank tenant, on the three skip reasons); new guard test `tests/Encina.GuardTests/Messaging/RecoverabilityTenantTagGuardTests.cs` (null marker valid).

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 3</strong></summary>

```text
CONTEXT:
Issue #2676 plan docs/plans/tenant-telemetry-tag-implementation-plan-2676.md, phase 3. Phases 1 and 2 merged.

TASK:
1. OutboundTelemetry uses TenantTelemetry.AttributeName; RestApiRequestHandler and RecoverabilityPipelineBehavior (both constructors)
   take MultiTenancyMarker? multiTenancy = null (last parameter) and compute the value with ValueFor once.
   Do not touch OutboxBatchProcessor or DelayedRetryProcessor (tenantId: null, no tenant source, Design Choice 6).
2. Unit and guard tests; PublicAPI and changelog through mechanical-fixer.

KEY RULES:
- Existing "new X(" sites (RecoverabilityPipelineBehavior 17, RestApiRequestHandler 14) still compile.
- The marker is read in the constructor; no scoped service is injected.

REFERENCE FILES:
- src/Encina.Messaging/Recoverability/RecoverabilityPipelineBehavior.cs, src/Encina.Refit/
```

</details>

---

### Phase 4: Compliance Behaviors (pull requests T4a and T4b)

<details>
<summary><strong>Tasks</strong></summary>

1. T4a: **Modify** `TransferBlockingPipelineBehavior.cs` (line 196), `DPIARequiredPipelineBehavior.cs` (line 197), `DataMinimizationPipelineBehavior.cs` (line 237): the key constant, the optional marker parameter and `ValueFor`. T4b: **Modify** `ProcessorValidationPipelineBehavior.cs` (line 199) and `DefaultDSRService.cs` (line 429; the service uses `ValueFor(aggregate.TenantId, flag)`) and `src/Encina.Compliance.DataSubjectRights/Diagnostics/DataSubjectRightsDiagnostics.cs` (the internal constant `TagTenantId` is removed; this is the third changed src file of T4b).
2. The five `PublicAPI.Unshipped.txt` files of the packages (constructor lines) and two changelog fragments (`changelog.d/2676-tenant-telemetry-compliance-a.changed.md` and `...-b.changed.md`) through `mechanical-fixer`. Manifest entries (`.github/coverage-manifest/`, through `mechanical-fixer`) for the four files that have no per-file `targets` today: `TransferBlockingPipelineBehavior.cs`, `DPIARequiredPipelineBehavior.cs`, `DataMinimizationPipelineBehavior.cs` (T4a, three manifests) and `ProcessorValidationPipelineBehavior.cs` (T4b); values in the targets table.
3. Tests: edit the existing behavior tests of each class (tag present only with the marker); new guard tests for the null marker (one file per pull request). T4b also edits `tests/Encina.UnitTests/Compliance/DataSubjectRights/DataSubjectRightsDenyTelemetryTests.cs` (line 65 reads `DataSubjectRightsDiagnostics.TagTenantId`; it reads `TenantTelemetry.AttributeName` and asserts the tag only with the marker, so the removal compiles in the same pull request).
4. The compliance behaviors keep their fail-closed gates: the helper only decides the tag, never whether the request runs.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 4</strong></summary>

```text
CONTEXT:
Issue #2676 plan docs/plans/tenant-telemetry-tag-implementation-plan-2676.md, phase 4. Phase 1 merged.

TASK:
1. Replace the "encina.tenant_id" and "dsr.tenant_id" literals in the five compliance classes with TenantTelemetry.AttributeName and
   ValueFor; each class takes MultiTenancyMarker? multiTenancy = null as its last constructor parameter.
2. Tests per class; PublicAPI and changelog through mechanical-fixer.

KEY RULES:
- Do not change any gate decision; the tenant tag is telemetry only.
- Existing "new X(" sites (TransferBlocking 8, DPIARequired 12, DataMinimization 13, ProcessorValidation 12, DefaultDSRService 31) compile unchanged.

REFERENCE FILES:
- the five classes named above and their tests under tests/Encina.UnitTests/Compliance/
```

</details>

---

### Phase 5: Tenancy and the OpenTelemetry Enricher (pull request T5)

<details>
<summary><strong>Tasks</strong></summary>

1. **Modify** `src/Encina.Tenancy/Diagnostics/TenancyActivitySource.cs` (lines 61 and 78 use the constant: `AddEncinaTenancy` always registers the marker, so these two sites are on by definition) and `src/Encina.OpenTelemetry/Enrichers/TenancyActivityEnricher.cs` (lines 33 and 53), and **remove** `ActivityTagNames.Tenancy.TenantId` from `src/Encina.OpenTelemetry/ActivityTagNames.cs`.
2. `src/Encina.OpenTelemetry/PublicAPI.Unshipped.txt` (the removed public constant), the manifest entry of `TenancyActivityEnricher.cs` in `.github/coverage-manifest/Encina.OpenTelemetry.json` (no per-file `targets` today) and changelog `changelog.d/2676-tenant-telemetry-tenancy.changed.md` through `mechanical-fixer`.
3. Tests: edit `tests/Encina.UnitTests/Tenancy/TenancyActivitySourceTests.cs`, `tests/Encina.UnitTests/OpenTelemetry/Enrichers/TenancyActivityEnricherTests.cs` and `tests/Encina.UnitTests/OpenTelemetry/ActivityTagNamesTests.cs` (line 229 asserts the removed constant).

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 5</strong></summary>

```text
CONTEXT:
Issue #2676 plan docs/plans/tenant-telemetry-tag-implementation-plan-2676.md, phase 5. Phase 1 merged.

TASK:
1. TenancyActivitySource and TenancyActivityEnricher tag the tenant with TenantTelemetry.AttributeName; remove ActivityTagNames.Tenancy.TenantId.
2. Update the three unit test files; PublicAPI and changelog through mechanical-fixer.

KEY RULES:
- No alias for the old key (pre-1.0, AGENTS.md section 3). Grep src and tests for "Tenancy.TenantId" and "tenancy.tenant_id" before removing.

REFERENCE FILES:
- src/Encina.Tenancy/Diagnostics/TenancyActivitySource.cs, src/Encina.OpenTelemetry/Enrichers/TenancyActivityEnricher.cs, src/Encina.OpenTelemetry/ActivityTagNames.cs
```

</details>

---

### Phase 6: Proof and Documentation (pull request T6)

<details>
<summary><strong>Tasks</strong></summary>

1. **Create** `tests/Encina.IntegrationTests/Observability/TenantTelemetryIntegrationTests.cs` (`[Trait("Category", "Integration")]`, no Docker): two service providers built with `ValidateOnBuild` and `ValidateScopes`, one with `AddEncinaTenancy` and one without; an in-memory exporter asserts that a `Send` that reaches the retry behavior emits `encina.tenant_id` only in the first, and that no baggage item exists in either.
2. **Edit** `tests/Encina.IntegrationTests/Encina.IntegrationTests.csproj`: add `<ProjectReference Include="..\..\src\Encina.Tenancy\Encina.Tenancy.csproj" />` (the project references Polly, OpenTelemetry and Refit but not `Encina.Tenancy`, and the test needs `AddEncinaTenancy`).
3. The observability guide paragraph on the tenant tag and its cardinality, through `docs-writer`, after #906 lands; changelog `changelog.d/2676-tenant-telemetry-docs.added.md` through `mechanical-fixer`. Per-category proof (one span and one metric per database, cache, lock and transport category) lands with #721, #722, #723, #724, #725 and #759, which call the helper.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 6</strong></summary>

```text
CONTEXT:
Issue #2676 plan docs/plans/tenant-telemetry-tag-implementation-plan-2676.md, phase 6. Phases 1 to 5 merged.

TASK:
1. Integration test with two service providers (with and without AddEncinaTenancy) and an in-memory exporter.
2. The documentation paragraph goes to docs-writer; changelog to mechanical-fixer.

KEY RULES:
- Tests execute the real behavior and exporter; no Thread.Sleep; no baggage assertion may be skipped.

REFERENCE FILES:
- tests/Encina.IntegrationTests/ (existing Observability or OpenTelemetry folders), docs/plans/observability-docs-implementation-plan-906.md
```

</details>

---

## Research

### Relevant Standards & Specifications

| Source | Provision | Use in this feature |
|--------|-----------|---------------------|
| ADR-054 D4 | Tenant is an attribute and a tag only when multi-tenancy is on; no baggage; cardinality documented | The rule |
| ADR-054 D3 and settled point 6 | One constants class; no compatibility layers | One key, no alias |
| SPEC-002 AC-044 | Tests with an in-memory exporter assert the tenant as an attribute | Phase 6 test |
| GDPR Art. 5(1)(c), Art. 25 | Data minimisation, data protection by design (spike report section 4.4; not legal advice) | Why the tag is conditional (Design Choice 3) |
| .NET metrics guidance | Keep tag combinations per instrument under about 1000; never tag with unbounded ids | Documented risk, not capped |

### Existing Encina Infrastructure to Leverage

| Component | Location | Usage in this feature |
|-----------|----------|-----------------------|
| `MultiTenancyMarker` | `src/Encina/Abstractions/MultiTenancyMarker.cs` | The "enabled" signal |
| `QueryCachingPipelineBehavior` | `src/Encina.Caching/Behaviors/QueryCachingPipelineBehavior.cs:53-86` | The optional-marker constructor pattern |
| `IRequestContext.TenantId`, `IRequestContextAccessor` | `src/Encina/Abstractions/` | The tenant source (`ITenantContext` does not exist) |
| The 13 existing tag sites | listed in Summary | Migrated |

### Event ID Allocation

| Package | Range | Notes |
|---------|-------|-------|
| none | none | No logging (register row C3); no entry in `EventIdRanges.cs`, the block registry or `PublicAPI` for a range |

### Estimated File Count

| PR | Content | New src | Changed src | New tests | Edited tests | Manifest | Other | Files | Lines (est.) |
|----|---------|---------|-------------|-----------|--------------|----------|-------|-------|--------------|
| T1 | Phase 1 | 1 | 0 | 2 | 0 | 1 | 2 (PublicAPI, changelog) | 6 | ~330 |
| T2 | Phase 2 | 0 | 3 | 1 | 2 | 0 | 2 (PublicAPI, changelog) | 8 | ~260 |
| T3 | Phase 3 | 0 | 3 | 2 | 1 | 0 | 3 (2 PublicAPI, changelog) | 9 | ~300 |
| T4a | Phase 4 | 0 | 3 | 1 | 3 | 3 | 4 (3 PublicAPI, changelog) | 14 | ~330 |
| T4b | Phase 4 | 0 | 3 | 1 | 3 | 1 | 3 (2 PublicAPI, changelog) | 11 | ~280 |
| T5 | Phase 5 | 0 | 3 | 0 | 3 | 1 | 2 (PublicAPI, changelog) | 9 | ~190 |
| T6 | Phase 6 | 0 | 0 | 1 | 0 | 0 | 3 (guide page, changelog, `Encina.IntegrationTests.csproj`) | 4 | ~150 |
| **Total** | | **1** | **15** | **8** | **12** | **6** | **19** | **61** | **~1,840** |

Columns sum: 1 + 15 + 8 + 12 + 6 + 19 = 61 = 6 + 8 + 9 + 14 + 11 + 9 + 4. Every PR is at most 15 files (largest T4a, 14) and at most 1500 lines. Test doubles: the optional constructor parameters change no interface, so no fake needs an edit; the `new X(` sites are listed in the Summary. The guard-test `InternalsVisibleTo` question does not arise (the helper is public). Per-file coverage targets (flags not listed are 0 with the reason in the justification):

| File | Unit | Guard | Contract | Property | Integration | Justification |
|------|------|-------|----------|----------|-------------|---------------|
| `src/Encina/Diagnostics/TenantTelemetry.cs` | 100 | 80 | 0 | 0 | 60 | Every branch (flag off, blank, valid, `Activity` null) is a unit case; guard covers the nullable-argument valid results; contract is 0 because the helper has no interface or abstract class to keep; property is 0 because there is no complex logic; integration drives the real marker detection |
| `src/Encina.Polly/Behaviors/RetryPipelineBehavior.cs` | 95 | 5 | 0 | 0 | 75 | Current manifest values, unchanged (never lowered); the tenant branch is table-driven in unit tests, the null marker is a guard valid-result test |
| `src/Encina.Polly/Diagnostics/RetryTelemetry.cs` | 95 | 0 | 0 | 0 | 30 | Current values; internal static with no validated parameter |
| `src/Encina/Recoverability/RetryMetrics.cs` | 98 | 15 | 0 | 0 | 95 | Current values; the tag branch is covered, existing guard tests cover the public arguments |
| `src/Encina.Refit/Diagnostics/OutboundTelemetry.cs` | 95 | 0 | 0 | 60 | 60 | Current values (property 60, integration 60); internal static, both tag sites run in the unit tests |
| `src/Encina.Refit/Handlers/RestApiRequestHandler.cs` | 95 | 5 | 0 | 70 | 85 | Current values; the new parameter has with and without-marker unit cases and a guard valid-result test |
| `src/Encina.Messaging/Recoverability/RecoverabilityPipelineBehavior.cs` | 95 | 5 | 65 | 0 | 0 | Current values (contract 65); both constructors take the parameter, three skip reasons each have a tagged and an untagged unit case |
| `src/Encina.Compliance.DataSubjectRights/Services/DefaultDSRService.cs` | 97 | 7 | 52 | 38 | 61 | Current values; the tag branch has a with-marker and a without-marker unit case |
| `src/Encina.Compliance.DataSubjectRights/Diagnostics/DataSubjectRightsDiagnostics.cs` | 100 | 0 | 73 | 61 | 65 | Current values; only a constant is removed |
| `src/Encina.Tenancy/Diagnostics/TenancyActivitySource.cs` | 90 | 0 | 0 | 0 | 0 | Current value (unit 90); the two sites change a key only, no parameter is added |
| `src/Encina.Compliance.CrossBorderTransfer/Pipeline/TransferBlockingPipelineBehavior.cs`, `src/Encina.Compliance.DPIA/DPIARequiredPipelineBehavior.cs`, `src/Encina.Compliance.PrivacyByDesign/DataMinimizationPipelineBehavior.cs`, `src/Encina.Compliance.ProcessorAgreements/ProcessorValidationPipelineBehavior.cs` (each, new manifest entry) | 90 | 40 | 0 | 0 | 0 | No per-file targets exist today; each new branch has with-marker and without-marker unit cases and the optional parameter has a guard valid-result test; contract is 0 because the behavior implements only the pipeline interface already covered by the shared contract tests, property and integration are 0 because there is no complex logic and the integration proof is T6 |
| `src/Encina.OpenTelemetry/Enrichers/TenancyActivityEnricher.cs` (new manifest entry) | 90 | 0 | 0 | 0 | 0 | No per-file targets exist today; the two sites change a key only; guard is 0 because no parameter is added |

The migration pull requests record each file's measured values against its manifest target in their description; a target is never lowered (`target-ratchet`).

---

## Combined AI Agent Prompts

<details>
<summary><strong>Full combined prompt for all phases</strong></summary>

```text
You are implementing issue #2676 - one tenant telemetry rule for every package - in seven small pull requests (T1 helper, T2 Polly,
T3 Refit and Messaging, T4a and T4b compliance, T5 Tenancy and OpenTelemetry, T6 proof and guide).

PROJECT CONTEXT:
- Encina .NET 10 / C# 14, pre-1.0. Edit with Edit/Write only; PublicAPI, manifest and changelog through mechanical-fixer; the guide
  paragraph through docs-writer.
- ADR-054 D4: the tenant id is a span attribute and a metric tag only when multi-tenancy is enabled (the MultiTenancyMarker is
  registered); no baggage. ITenantContext does not exist: the tenant is IRequestContext.TenantId.

IMPLEMENTATION OVERVIEW:
1. T1: public static Encina.Diagnostics.TenantTelemetry (AttributeName "encina.tenant_id", ValueFor x2, SetTenantTag, AddTenantTag).
2. T2-T5: every existing site stops writing its own literal; deciders take MultiTenancyMarker? multiTenancy = null (last parameter)
   and call ValueFor; remove ActivityTagNames.Tenancy.TenantId.
3. T6: integration test with and without AddEncinaTenancy; guide paragraph.

KEY PATTERNS:
- No tag when the flag is off or the tenant is null, empty or whitespace; never "default"; never baggage; never a message text.
- A missing tenant never changes a gate or an operation; the tag is telemetry only.
- No scoped service in a singleton: the flag is read in the constructor.

REFERENCE FILES:
- src/Encina.Caching/Behaviors/QueryCachingPipelineBehavior.cs, src/Encina/Abstractions/MultiTenancyMarker.cs,
  docs/architecture/adr/054-observability-conventions-and-trace-propagation.md
```

</details>

---

## Cross-Cutting Integration Matrix

| # | Function | Status | Notes |
|---|----------|--------|-------|
| 1 | Caching | ❌ N/A | The helper reads no cacheable data |
| 2 | OpenTelemetry | ✅ Phases 1-6 | This is the feature |
| 3 | Structured Logging | ❌ N/A | Logs are out of scope; no EventId (register row C3) |
| 4 | Health Checks | ❌ N/A | No checkable dependency |
| 5 | Validation | ❌ N/A | Receives no input beyond a nullable id |
| 6 | Resilience | ❌ N/A | No external call; the retry behaviors are only migrated |
| 7 | Distributed Locks | ❌ N/A | No shared state |
| 8 | Transactions | ❌ N/A | No multi-operation guarantee |
| 9 | Idempotency | ❌ N/A | Recording twice is harmless |
| 10 | Multi-Tenancy | ✅ Phases 1-6 | The feature is the tenant telemetry rule; reads the tenant from `IRequestContext` and the marker |
| 11 | Module Isolation | ❌ N/A | No module dimension (a module tag would be a separate ADR-054 decision) |
| 12 | Audit Trail | ❌ N/A | Telemetry is not an audit record |

---

## Prerequisites & Dependencies

### Required Prerequisites

| Prerequisite | Status | Notes |
|-------------|--------|-------|
| ADR-054 | Accepted | D4 is the decision |

### Recommended (Not Blocking)

| Dependency | Issue | Notes |
|-----------|-------|-------|
| Observability guide | #906 | Owns the cardinality text; T6 waits for it |
| Static scan | #1788 | Can later forbid tenant-id literals outside the helper |
| Encina-level instrumentation | #721, #722, #723, #724, #725, #759 | Each calls the helper in its own tenant pull request; they wait for T1 only |

---

## Next Steps

1. Link this plan from #2676 and ask the maintainer to answer Design Choices 1, 2, 3 and 6 when work starts.
2. Implement T1 first (the siblings wait only for it), then T2 to T5 in any order, then T6.
3. Maintainer action: amend the issue text that names `ITenantContext` and the "36 packages" scope to this plan's helper and 13-site migration (this plan never comments on an issue).
