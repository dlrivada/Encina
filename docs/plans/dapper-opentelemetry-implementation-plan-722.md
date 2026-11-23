# Implementation Plan: OpenTelemetry for the Dapper Store Providers (Verification of the Shared Store Telemetry on the Three Dapper Providers)

> **Issue**: [#722](https://github.com/dlrivada/Encina/issues/722)
> **Type**: Feature (observability)
> **Related**: #721 (owns `StoreTelemetry` and `DatabaseIdentity`, this plan uses them), ADR-054 (D3, D4, D6), #2676 (tenant tag helper), #1787, #906
> **Complexity**: Low (no production mechanism of its own; proof, a coherence test and the package README)
> **Estimated Scope**: ~0 lines of new production code + ~500 lines of tests, in one pull request of 6 files

---

## Summary

### What exists today (read on 2026-11-20, main `0336d31c7`)

- No Dapper package emits telemetry: `src/Encina.Dapper.SqlServer`, `.PostgreSQL` and `.MySQL` contain no `new ActivitySource(` and no `new Meter(`. `Encina.Dapper.Sqlite` is not in `src` (SQLite is outside the matrix, ADR-024), so the issue's "4 providers" are 3.
- The Dapper stores implement the same `Encina.Messaging` interfaces as the ADO stores, and `AddEncinaOpenTelemetry` decorates those interfaces for every provider (`src/Encina.OpenTelemetry/ServiceCollectionExtensions.cs`, eight `DecorateService` calls). The Dapper monitors register `IDatabaseHealthMonitor` with the provider names `dapper-sqlserver`, `dapper-postgresql` and `dapper-mysql` (`src/Encina.Dapper.*/Health/*DatabaseHealthMonitor.cs`).
- The plan of #721 adds provider identity (`db.system.name`, `encina.db.access`) and the store instruments to those decorators. Once it lands, Dapper stores emit them with no Dapper code change; what is missing is proof on real Dapper databases and a check that ADO and Dapper emit the same instruments and keys.
- Dapper integration tests use the shared collections `Dapper-SqlServer`, `Dapper-PostgreSQL` and `Dapper-MySQL` (`tests/Encina.IntegrationTests/Dapper/Collections.cs`).
- `InstrumentedBulkOperations<>` and `InstrumentedFunctionalRepository<,>` are never registered in production (the issue's "bulk operations" are therefore not traced); #721 defers that wiring to a follow-up issue.

### What this plan delivers

- Integration proof for the 3 Dapper providers (span tags, counter, histogram, error path) with the shared fixtures.
- No contract test of its own: the cross-access check (the same store operation on an ADO identity and a Dapper identity differs only in `encina.db.access` and the provider-specific `db.system.name`) is one case of #721 S2's `StoreTelemetryContractTests`, so the two plans do not duplicate a test.
- A section in the `Encina.OpenTelemetry` README listing the instruments and tags.
- No production code: the issue's items "ActivitySource for Dapper store operations" and "bulk operations" are answered by Design Choices 1 and 2.
- Shared pieces and owner: everything is #721's; this plan only consumes it. Affected packages: tests, `Encina.OpenTelemetry` README. Provider category: Database (10); proves the 3 Dapper providers.

---

## Design Choices

<details>
<summary><strong>1. Keep #722 as the Dapper verification pull request, or merge it into #721</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Keep #722 as a test-and-docs pull request that starts after #721 S1** | The Dapper proof is reviewed on its own; the issue's acceptance criteria stay traceable | The issue becomes a verification issue, not an implementation one |
| **B) Fold #722 into #721 and close it with the Dapper tests inside #721's S4** | One issue, one mechanism | #721 S4 grows from 5 to 8 files; the Dapper acceptance criteria lose their own issue |

### Chosen Option: **A - Keep it as the verification pull request** (recommended, pending the maintainer)

### Rationale

- A wins while the maintainer wants each provider family to have its own closing issue; B wins if the maintainer prefers fewer issues for one mechanism (the decorators are provider-agnostic, so a Dapper-specific implementation never existed).

**Class:** Dilemma

</details>

<details>
<summary><strong>2. Dapper-specific instrumentation of query execution and bulk operations</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) None: store-level telemetry only, no Dapper hook** | ADR-054 D6.2 and D6.3: no driver-level duplication and never `db.query.text`; Dapper adds no seam of its own (it extends `IDbConnection`) | The issue's "query execution" span is the store span |
| **B) A wrapper `IDbConnection` or `DbCommand` per Dapper package** | Per-query spans | Duplicates driver instrumentation (D6.2), risks query text, 3 packages of new code and a connection decorator beside `SchemaValidatingConnection` |
| **C) Bulk operations wired now** | Matches the issue | The wiring mechanism is #721's deferred Design Choice 5 |

### Chosen Option: **A - Store level only** (recommended, pending the maintainer)

### Rationale

- A wins on ADR-054 D6; B wins only for an application that needs per-statement timing and accepts enabling a driver instrumentation package instead (the documentation says which one).

**Class:** Dilemma

</details>

<details>
<summary><strong>3. Cost, time, tenant and error text</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Reuse #721's guarded instruments, `TimeProvider`, tenant helper of #2676 and code-only errors; this plan adds none** | One rule, one place | None |
| **B) Dapper-specific variants** | None | Divergence between ADO and Dapper |

### Chosen Option: **A - Reuse**

### Rationale

- The behavior is #721's by construction; the tests of this plan assert it for Dapper.

**Class:** Trivial
**Settled by:** ADR-054 settled points 2 and 7 and D4; `AGENTS.md` section 5 (provider coherence: providers differ only in implementation)

</details>

<details>
<summary><strong>4. Proof level for the Dapper providers</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) One integration test class per provider on the shared collection fixtures** | Real database, real store, 3 providers (`AGENTS.md` section 9) | 3 containers already shared with other Dapper tests |
| **B) Mocks only (as `InstrumentedWrappersIntegrationTests`)** | Fast | Proves nothing about the Dapper stores; "database features: real databases, never a justification file" |

### Chosen Option: **A - Real databases**

### Rationale

- `AGENTS.md` section 9 requires integration tests with real databases for database features, with `[Collection("Dapper-<Db>")]`, `ClearAllDataAsync()` in `InitializeAsync()` and no per-class fixture.

**Class:** Trivial
**Settled by:** `AGENTS.md` section 9 (integration test fixtures)

</details>

### Register of defaults and semantics

| Id | Choice | Class | Basis |
|----|--------|-------|-------|
| C1 | No new `EncinaError` code, so no ADR-062 `ErrorCategory`; no logging, no EventId (no EventId range needed) | Settled | ADR-062; `AGENTS.md` section 7 |
| C2 | No tenant-qualified id; with multi-tenancy off no tenant tag; the tenant tag is #721 S3 | Settled | ADR-054 D4 |
| C3 | `error.type` is the code or the exception type, never the message | Settled | ADR-054 settled point 2 |

---

## Implementation Phases

### Phase 1: Dapper Integration Proof (single pull request D1)

> **Goal**: the Dapper stores are proven to emit what #721 defines, on real databases.

<details>
<summary><strong>Tasks</strong></summary>

1. **Create** `tests/Encina.IntegrationTests/Dapper/SqlServer/Observability/StoreTelemetryIntegrationTests.cs`, `.../Dapper/PostgreSQL/Observability/...` and `.../Dapper/MySQL/Observability/...`: `[Collection("Dapper-<Db>")]`, `[Trait("Category", "Integration")]`, `[Trait("Provider", "Dapper.<Db>")]`; each builds a provider with the Dapper store registration and `AddEncinaOpenTelemetry` (called after the provider registration), adds and processes an outbox message and asserts with an `ActivityListener` and a `MeterListener` the span tags (`db.system.name`, `encina.db.access = dapper`), `encina.store.operations`, `encina.store.operation.duration`, and `encina.outcome = error` with the code when the store fails, plus that no tenant tag is present when multi-tenancy is off.
2. **Modify** `.github/coverage-manifest/Encina.OpenTelemetry.json` (integration target of `Telemetry/StoreTelemetry.cs` raised from #721 S4's 40 to 55 with the Dapper proof) through `mechanical-fixer`; the `Encina.OpenTelemetry` README section through `docs-writer`; the changelog fragment `changelog.d/722-dapper-store-telemetry.added.md` through `mechanical-fixer`.
3. **Verify** before pushing: dotnet build --configuration Release with 0 errors and 0 warnings, dotnet test of the touched test project with all tests passing, and the per-flag coverage of Telemetry/StoreTelemetry.cs against the manifest targets (coverage-report.cs, "Per-file targets" table).

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 1</strong></summary>

```text
CONTEXT:
Issue #722, plan docs/plans/dapper-opentelemetry-implementation-plan-722.md. #721 (S1, S2, S4) merged: StoreTelemetry and
DatabaseIdentity exist in src/Encina.OpenTelemetry/Telemetry; the message-store decorators record encina.store.operations
and encina.store.operation.duration with db.system.name and encina.db.access.

TASK:
1. Write one integration test class per Dapper provider (SqlServer, PostgreSQL, MySQL) with the shared collection fixtures; also assert no tenant tag when multi-tenancy is off.
2. Manifest and changelog through mechanical-fixer, README through docs-writer. No contract test here (it is #721 S2's).
3. Run dotnet build --configuration Release (0 errors, 0 warnings) and dotnet test (all pass); report the measured per-flag coverage of StoreTelemetry.cs against its manifest targets.

KEY RULES:
- [Collection("Dapper-<Db>")] fixtures only; ClearAllDataAsync in InitializeAsync; never dispose the fixture; no Thread.Sleep.
- AddEncinaOpenTelemetry must be called after the Dapper registration, or nothing is decorated.
- No production code in this pull request.

REFERENCE FILES:
- tests/Encina.IntegrationTests/Dapper/SqlServer/Outbox/OutboxStoreDapperTests.cs, tests/Encina.IntegrationTests/Dapper/Collections.cs
- src/Encina.OpenTelemetry/ServiceCollectionExtensions.cs
```

</details>

---

## Research

### Relevant Standards & Specifications

| Source | Provision | Use in this feature |
|--------|-----------|---------------------|
| ADR-054 | D3, D4, D6.2, D6.3 | No driver-level instrumentation, no query text |
| `AGENTS.md` section 5 and 9 | Provider coherence; real-database integration tests | Phase 1 |

### Existing Encina Infrastructure to Leverage

| Component | Location | Usage in this feature |
|-----------|----------|-----------------------|
| `StoreTelemetry`, `DatabaseIdentity` | `src/Encina.OpenTelemetry/Telemetry/` (from #721) | Asserted |
| Dapper monitors | `src/Encina.Dapper.*/Health/` | Provider names |
| Dapper collections | `tests/Encina.IntegrationTests/Dapper/Collections.cs` | Fixtures |

### Event ID Allocation

| Package | Range | Notes |
|---------|-------|-------|
| none | none | No logging code (register row C1) |

### Estimated File Count

| PR | Content | New src | Changed src | New tests | Edited tests | Manifest | Other | Files | Lines (est.) |
|----|---------|---------|-------------|-----------|--------------|----------|-------|-------|--------------|
| D1 | Phase 1 | 0 | 0 | 3 | 0 | 1 | 2 (README, changelog) | 6 | ~420 |
| **Total** | | **0** | **0** | **3** | **0** | **1** | **2** | **6** | **~420** |

Columns sum: 3 + 1 + 2 = 6. No production code, so no guard tests are added; the guard flag of the files #721 owns is unchanged. The integration test project already references `Encina.OpenTelemetry`, so there is no csproj edit.

Per-file coverage targets: `Telemetry/StoreTelemetry.cs` (owned by #721, unit 90, integration 40): this plan raises the integration target from #721 S4's 40 to 55 (the manifest edit lands after S4, so the two never conflict) because the 3 Dapper tests run the same lines as the 3 ADO tests on a second access technology; contract and guard targets stay at 0 and 0 (internal static type without a validated parameter, covered by #721's tests).

---

## Combined AI Agent Prompts

<details>
<summary><strong>Full combined prompt for all phases</strong></summary>

```text
You are implementing issue #722 - Dapper store telemetry proof - in one pull request, after #721 S1, S2 and S4 merged.

PROJECT CONTEXT:
- Encina .NET 10 / C# 14, pre-1.0. The decorators in Encina.OpenTelemetry are provider-agnostic; #721 adds the instruments and tags.
- Edit with Edit/Write only; manifests and changelog through mechanical-fixer; README through docs-writer.

IMPLEMENTATION OVERVIEW:
1. Three integration test classes for Dapper SqlServer, PostgreSQL, MySQL with shared collection fixtures.
2. No contract test: the ADO versus Dapper identity check is a case of #721 S2's contract test.

KEY PATTERNS:
- No production code, no Dapper-specific instrumentation, never db.query.text.

REFERENCE FILES:
- tests/Encina.IntegrationTests/Dapper/, src/Encina.OpenTelemetry/Telemetry/
```

</details>

---

## Cross-Cutting Integration Matrix

| # | Function | Status | Notes |
|---|----------|--------|-------|
| 1 | Caching | ❌ N/A | Instrumentation observes stores; it adds no cached reads or writes |
| 2 | OpenTelemetry | ✅ Phase 1 | Provider tags and integration proof for the three Dapper providers |
| 3 | Structured Logging | ❌ N/A | No EventId is added (register row C1) |
| 4 | Health Checks | ❌ N/A | Spans and metrics observe operations; they provide no health status |
| 5 | Validation | ❌ N/A | No input |
| 6 | Resilience | ❌ N/A | Adds no external call or retry |
| 7 | Distributed Locks | ❌ N/A | Acquires no lock |
| 8 | Transactions | ❌ N/A | Observes the unit of work, never changes it |
| 9 | Idempotency | ❌ N/A | Recording twice is harmless |
| 10 | Multi-Tenancy | ❌ N/A | There is no Dapper-specific tenant code: the tenant tag is one shared helper and its tests are #721 S3 (after #2676); this plan's integration tests assert only that the tag is absent when multi-tenancy is off |
| 11 | Module Isolation | ❌ N/A | No module data |
| 12 | Audit Trail | ❌ N/A | No personal data reaches telemetry |

---

## Prerequisites & Dependencies

### Required Prerequisites

| Prerequisite | Status | Notes |
|-------------|--------|-------|
| #721 S1, S2 and S4 merged | Open (plan on its own branch) | S1 and S2 provide the instruments the tests assert; S4 sets the integration target this plan raises |

### Recommended (Not Blocking)

| Dependency | Issue | Notes |
|-----------|-------|-------|
| Tenant helper | #2676 | Tenant assertions wait for #721 S3 |
| Observability guide | #906 | Documents the instruments |

---

## Next Steps

1. Link this plan from #722 and ask the maintainer to answer Design Choices 1 and 2 when work starts.
2. Open the pull request after #721 S4. Order in the family: #721, then this plan.
3. Maintainer action: amend the acceptance criteria of #722 that name SQLite, bulk operations and an `ActivitySource` for Dapper to the outcome of Design Choices 1 and 2 (this plan never comments on an issue).
