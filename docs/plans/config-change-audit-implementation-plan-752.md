# Implementation Plan: Audit trail for sharding and tenancy configuration changes

> **Issue**: [#752](https://github.com/dlrivada/Encina/issues/752)
> **Type**: Feature
> **Complexity**: Medium-High (8 phases, no new store column, six pull requests; the tenant events need a prerequisite larger than the audit feature)
> **Estimated Scope**: ~18 production files and ~22 test files, plus the prerequisite
> **Related plans** (by issue number): #751 (fail-closed administrative audit precedent), #1257 (the SPEC-002 tenant work that consumes the tenant lifecycle), #798 (Security.Audit EventIds 5013-5015), #711 (owns `Tenancy` EventIds 1805-1810)

---

## Summary

Record who changed the sharding topology or a tenant's configuration, when, and what the previous value was, in the existing audit trail, so that after an incident the change can be reconstructed. The five events of the issue are `ShardingConfigChanged`, `TenantCreated`, `TenantUpdated`, `TenantDeactivated` and `TenantShardMigrated`.

**State of the code on `d1793713d` (current `origin/main`; confirms the issue's "State today" and adds what the issue did not find).**

- **Sharding.** `ShardTopology` and `IShardTopologyProvider.GetTopology()` are read-only; `DefaultShardTopologyProvider` holds one immutable instance. The only runtime topology change is the resharding workflow: `ReshardingOrchestrator.ExecuteAsync` calls `IReshardingServices.SwapTopologyAsync` at the cutover phase and `RollbackAsync` calls it again to restore the old topology (`src/Encina/Sharding/Resharding/ReshardingOrchestrator.cs:201-213`). Startup configuration (`ShardingOptions`) is code or configuration, not a runtime change. `ShardInfo` carries `ConnectionString`, `ReplicaConnectionStrings` and `IsActive`.
- **Tenancy.** `ITenantStore` has `GetTenantAsync`, `GetAllTenantsAsync`, `ExistsAsync` only (`InMemoryTenantStore` adds `RegisterTenant`, `RemoveTenant`, `Clear` as concrete methods, not on the port). `TenantInfo` is `(TenantId, Name, Strategy, ConnectionString, SchemaName)`: it has no active flag and no shard assignment. Therefore **`TenantDeactivated` and `TenantShardMigrated` have neither an operation nor a data model today**, `TenantCreated` and `TenantUpdated` have no operation, and #596 (persistent `ITenantStore`, open) does not add a write side either.
- **Audit.** Three purpose-named stores (ADR-036). `IOperationAuditStore` and `OperationAuditEntry` (`Action`, `EntityType`, `EntityId`, `Outcome`, `TenantId`, `Metadata`, `TimestampUtc`) are the general write-audit sink; ABAC policy administration already writes administrative changes through it with the actor and tenant. `Encina` and `Encina.Tenancy` do not reference `Encina.Security.Audit` (which references `Encina`), so core code cannot call the store directly.
- The issue names `IAuditStore`; today that is `IOperationAuditStore`.

**Scope.** (1) A small sink abstraction and a record type in `Encina`, an adapter in `Encina.Security.Audit` that writes an `OperationAuditEntry`; (2) `ShardingConfigChanged` emitted around `SwapTopologyAsync` for execute and rollback; (3) the tenant events once a tenant lifecycle operation exists (prerequisite, Design Choice 5); (4) secrets never recorded, failure of the audit write fails the operation, timestamps from `TimeProvider`.

**Out of scope.** The tenant lifecycle administration service and the active flag (prerequisite issue file `tenant-lifecycle-administration`); tenant shard assignment and migration (issue file `tenant-shard-assignment`); audit of other configuration (messaging, caching); the administration surface of #2972.

**Affected packages**: `Encina` (abstraction, sharding emission), `Encina.Security.Audit` (adapter), `Encina.Tenancy` (lifecycle events, after the prerequisite). **Provider category**: none (the events use the existing `IOperationAuditStore`; no store column, so the 10-provider matrix does not apply).

---

## Design Choices

<details>
<summary><strong>1. Which audit store records the events: `IOperationAuditStore`</strong></summary>

**Class:** Settled
**Settled by:** ADR-036 (three purpose-named stores; the operation store is the general write-audit sink) and the issue's Settled choice 1.

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `IOperationAuditStore` with `OperationAuditEntry`** | Already used by the pipeline, ABAC policy administration and Secrets; 10 providers, Marten and in-memory exist | The entry is request-shaped, so before/after values go to `Metadata` |
| **B) The entity-change store (`IAuditLogStore`, #1778)** | Built for before/after of entities | Configuration changes are administrative operations, not entity changes; the store is not yet implemented |
| **C) `IReadAuditStore`** | n/a | Reads, not changes |

### Chosen Option: **A — `IOperationAuditStore`**

### Rationale

- A configuration change is an administrative operation of a subject (a topology or a tenant), which is the shape of the operation store.

</details>

<details>
<summary><strong>2. Secrets are never recorded: a connection string is "changed", never a value</strong></summary>

**Class:** Settled
**Settled by:** `AGENTS.md` section 3 (secrets never reach logs, serialization or plaintext storage; `EncinaError.Message` rule) and the issue's Settled choice 2.

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) A change set API with `Changed(name)` for secrets and `Value(name, value)` for non-secrets; shard ids, counts, strategy names and flags are values; the guards are a contract test that scans every entry written by a full resharding run for a connection string, an architecture test that fails when a `Value(...)` argument is derived from a member named `*ConnectionString*`, `*Secret*`, `*Password*` or `*Key*`, and review (`Value(string, string)` accepts any string, so the shape alone is not a compile-time guarantee)** | Audit shows that it changed; two automated guards | The old value of a connection string cannot be recovered from the trail (by design) |
| **B) Record values and mask with a regex** | Looks complete | Masking by pattern leaks (#1173, #1259 class) |
| **C) Hash the secret** | Detects equality | A hash of a connection string is still derived from a secret |

### Chosen Option: **A — A change set that only records that a secret changed**

### Rationale

- `ShardInfo.ConnectionString`, `ReplicaConnectionStrings` and `TenantInfo.ConnectionString` are always recorded through `Changed`.
- `TenantInfo.Name` and `SchemaName` are not secrets but can identify a natural person (a sole practitioner's tenant): their handling is Design Choice 7.

</details>

<details>
<summary><strong>3. Failure of the audit write fails the operation, and the write is ahead of the change</strong></summary>

**Class:** Settled
**Settled by:** `AGENTS.md` section 3 (errors are never swallowed; compliance and security gates fail closed), the issue's Settled choice 3, and the write-ahead pattern of `AuditStoreABACDecisionRecorder` (`src/Encina.Security.ABAC/DecisionAudit/AuditStoreABACDecisionRecorder.cs`, plan #751).

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Record the intent first; a `Left` from the sink on the forward (execute) path returns a `Left` and the change is not applied** | No forward change without a trail | A recorded intent may exist for a change that then fails (the failure is recorded as a second entry) |
| **B) Record after the change** | Trail equals reality | A change that cannot be audited has already happened |
| **C) Log the failure and continue** | Never blocks | Swallows an error in infrastructure |

### Chosen Option: **A — Write-ahead, fail closed**

### Rationale

- For resharding the intent is recorded before `SwapTopologyAsync`, and the outcome (`Success` or `Failure`) is recorded after, as two entries with the same correlation id.
- This entry covers only the **forward path before the swap**. What happens on the compensating path (rollback) and after an irreversible swap is not settled: Design Choice 9.
- A sink `Left` becomes the resharding failure `encina.sharding.audit_unavailable` (`ErrorCategory.Unavailable`, ADR-062: a dependency is unavailable), created with `EncinaErrors.Create`, never carrying the store's `EncinaError.Message`; the adapter's pass-through of the store's own `Left` keeps the store's category; a `GetCategory` test asserts both.

</details>

<details>
<summary><strong>4. How the emitters reach the audit store</strong></summary>

**Class:** Dilemma (recommended, pending the maintainer)

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) The emitters publish `INotification` events through `IEncina.Publish`; a handler in `Encina.Security.Audit` records them** | No new abstraction in the core; every feature stays opt-in | Indirection; the audit write happens after the publish and cannot veto the change; no audit package means no audit, silently |
| **B) A small sink abstraction in `Encina` (`IConfigurationChangeAuditSink`) implemented by an adapter in `Encina.Security.Audit`, called inside the operation** | The write is ahead of the change and can stop it (Design Choice 3); no new project reference | One more abstraction in the core; with no audit package a null sink applies (documented) |
| **C) `Encina` and `Encina.Tenancy` reference `Encina.Security.Audit`** | Direct call | A dependency cycle risk (`Encina.Security.Audit` references `Encina`); breaks pay-for-what-you-use |

### Chosen Option: **B — A sink abstraction in `Encina` with an adapter in `Encina.Security.Audit`** (recommended, pending the maintainer)

### Rationale

- A wins when audit is only an optional add-on and a missed record is acceptable; B wins when the change must not succeed without its audit record, which is what the issue's constraint says.
- Names (proposals): `IConfigurationChangeAuditSink`, `ConfigurationChangeRecord`, `ConfigurationChangeSet` in `src/Encina/Auditing/`; the default is `NullConfigurationChangeAuditSink`, registered with `TryAddSingleton`; `AddEncinaAudit` registers `OperationAuditConfigurationChangeSink` ahead of the null sink.
- **Lifetime (no captive dependency).** `ReshardingOrchestrator` is registered `TryAddSingleton<IReshardingOrchestrator, ReshardingOrchestrator>()` (`ReshardingServiceCollectionExtensions.cs:46`) while the database `IOperationAuditStore` is scoped. The adapter is therefore a **singleton that opens a scope per call** (`IServiceScopeFactory`) and resolves the store (and the optional `IPiiMasker`) inside it, so the orchestrator never captures a scoped service; the DI test builds the "audit + resharding" combination with `ValidateOnBuild` and `ValidateScopes`.

</details>

<details>
<summary><strong>5. Where the tenant lifecycle events come from: write operations on `ITenantStore` or a separate administration service</strong></summary>

**Class:** Dilemma (recommended, pending the maintainer)

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Add `CreateAsync`, `UpdateAsync`, `DeactivateAsync` to `ITenantStore`** | One abstraction | Read-only stores (configuration or claims based, `InMemoryTenantStore` built from `TenancyOptions.Tenants`) must implement or reject writes; every implementer changes |
| **B) A new `ITenantAdministration` service (Encina.Tenancy) that calls the store through a small write port, emits the events through the sink and owns the active flag** | Read-only stores stay valid; the audit lives where the lifecycle lives; the active flag and shard assignment can be added there | A second abstraction; a prerequisite larger than the audit feature |

### Chosen Option: **B — A tenant administration service** (recommended, pending the maintainer)

### Rationale

- A wins when every tenant store supports management; B wins when read-only stores exist, which is the case today (static options, claims).
- The prerequisite also adds `TenantInfo.IsActive` (a breaking change of the record, accepted pre-1.0); it is an issue of its own (`tenant-lifecycle-administration`), and this plan's tenant phase starts after it.
- `TenantShardMigrated` needs a tenant-to-shard assignment that does not exist in `TenantInfo` or in the shard routers; it waits for the second issue file (`tenant-shard-assignment`).

</details>

<details>
<summary><strong>6. Mapping onto `OperationAuditEntry`, and who the actor is</strong></summary>

**Class:** Trivial
**Settled by:** `PersistentPolicyAdministrationPoint` (`src/Encina.Security.ABAC/Administration/PersistentPolicyAdministrationPoint.cs`, `PolicyActor` at line 98 and the service identity of startup seeding) and `OperationAuditEntry` (`src/Encina.Security.Audit/OperationAuditEntry.cs`).

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `Action` = the event name, `EntityType` = `ShardTopology` or `Tenant`, `EntityId` = resharding id or tenant id, `Metadata` = the redacted before/after, `UserId` from the request identity, a fixed service identity (`service:encina.sharding.resharding`) when there is none, `TenantId` = the affected tenant, `Outcome` from the result** | No new store or column; same query surface as the rest of the trail | Before/after live in `Metadata` (stored on every provider as text) |
| **B) A new entry type and table** | Typed | A new store on 10 providers and Marten for no new capability |

### Chosen Option: **A — Reuse `OperationAuditEntry`**

### Rationale

- The `PolicyActor` rule is reused: an anonymous caller is refused for a change that requires an actor; background workflows run under a declared service identity.
- **One time source:** `ConfigurationChangeRecord.OccurredAtUtc` is read by the caller from its own `TimeProvider` (the orchestrator already takes one); the adapter reads no time and takes no `TimeProvider`.

</details>

<details>
<summary><strong>7. Tenant `Name` and `SchemaName` in the recorded values</strong></summary>

**Class:** Dilemma (recommended, pending the maintainer)

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Record them as plaintext values in `Metadata`** | Full before/after for reconstruction | A tenant name can identify a natural person (a sole practitioner); the metadata is stored in plaintext on every provider |
| **B) Record them as `Changed(name)` only, like secrets** | No personal data in the trail | The trail shows that a name changed, not from what to what |
| **C) Record them through `IPiiMasker` (the masked value), and as `Changed(name)` when no masker is registered** | Reconstruction without the direct identifier; uses the existing masking port of `Encina.Security.Audit` | The masked value may not distinguish two names; the adapter takes the masker, so masking happens in the adapter, not in the core |

### Chosen Option: **C — Masked values, `Changed` without a masker** (recommended, pending the maintainer)

### Rationale

- C wins when the trail must stay reconstructible without holding a direct identifier; A wins if tenant names are known to be organisation names only; B wins when no tenant text may reach the trail. `IPiiMasker? masker` in the adapter is this and only this: masking `Value(...)` entries marked as personal by the emitter.

</details>

<details>
<summary><strong>8. Resharding enabled with no audit package: the null sink</strong></summary>

**Class:** Dilemma (recommended, pending the maintainer)

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) The host fails to start when resharding is registered and the sink is the null sink, unless an explicit, logged opt-out is set (`ReshardingOptions.AllowUnauditedTopologyChanges`, logged once by code at start)** | "The change must not succeed without its audit record" holds by default; consistent with compliance-never-optional (2026-10-30) | A host without `Encina.Security.Audit` must set the opt-out to reshard |
| **B) A one-time start-up warning only** | Pay-for-what-you-use; no friction | A resharding succeeds with no audit record, contradicting Design Choice 3 |

### Chosen Option: **A — Fail the host unless explicitly opted out** (recommended, pending the maintainer)

### Rationale

- A wins when a topology change is a control-relevant event (the compliance reading); B wins for hosts that run without any audit package and accept it. The start-up check of Phase 5 implements the choice.

</details>

<details>
<summary><strong>9. Failure points of a resharding cutover and its rollback</strong></summary>

**Class:** Dilemma (recommended, pending the maintainer)

`RollbackAsync` restores the previous topology after a failed cutover; if its write-ahead record fails, a rule "a `Left` from the sink stops the change" would leave the system in the broken topology because the audit store is down (a safety action blocked by an audit outage). After a successful swap the change has happened, so a `Left` from the outcome entry cannot "not apply" it.

| Failure point | State machine rule under the recommendation |
|---------------|---------------------------------------------|
| Before the swap (write-ahead of execute fails) | no swap; result `Left(encina.sharding.audit_unavailable)`; phase `Failed` (Design Choice 3, settled) |
| After the swap, outcome entry of execute fails | the swap stands (no automatic rollback); the resharding returns `Left(encina.sharding.audit_unavailable)` and the phase stays `CuttingOver` completed in the state store with an `AuditPending` marker; the next orchestrator cycle retries the outcome entry until it succeeds or an operator intervenes; never reported as success while the entry is missing |
| Rollback, write-ahead fails | the rollback proceeds (a safety action is never blocked by an audit outage); the failed write-ahead is logged by code and the missing record is retried with the same `AuditPending` marker |
| Rollback, outcome entry fails | same as the execute outcome: the topology stands, `Left` returned, `AuditPending` retried |

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) The table above: fail closed before an irreversible forward change, never block the compensating action, retry the missing outcome** | A broken topology is never left in place because of an audit outage; the trail converges | An unaudited rollback can occur for a period; a retry marker in the state store |
| **B) Rollback is also blocked by a failed write-ahead (strict fail closed)** | No unaudited topology change at all | A broken topology stays until the audit store recovers |

### Chosen Option: **A — Block the forward change, never the rollback** (recommended, pending the maintainer)

### Rationale

- A wins when availability of the safety action matters more than the completeness of the trail during an outage; B wins when an unaudited change is never acceptable even to repair the system.

</details>

---

## Implementation Phases

### Phase 1: Core Abstraction & Models

> **Goal**: the sink, the record and the change set in `Encina`.

<details>
<summary><strong>Tasks</strong></summary>

1. **Create `src/Encina/Auditing/IConfigurationChangeAuditSink.cs`**: `ValueTask<Either<EncinaError, Unit>> RecordAsync(ConfigurationChangeRecord record, CancellationToken cancellationToken)`.
2. **Create `ConfigurationChangeRecord.cs`**: `sealed record (string EventName, string SubjectType, string SubjectId, string? TenantId, ConfigurationChangeSet Before, ConfigurationChangeSet After, string? ActorId, string CorrelationId, DateTimeOffset OccurredAtUtc, bool Succeeded)`.
3. **Create `ConfigurationChangeSet.cs`**: immutable builder: `Value(string name, string value)` (non-secret facts only), `Changed(string name)` (a secret changed), `Count(string name, int value)`.
4. **Create `NullConfigurationChangeAuditSink.cs`** and register it with `TryAddSingleton` in the core registration.
5. PublicAPI lines (mechanical-fixer).

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 1</strong></summary>

```text
CONTEXT:
- Encina core must not reference Encina.Security.Audit. Design Choices 2 (no secret values), 3 (fail closed), 4B (sink in core).

TASK:
Create the sink interface, the record, the change set builder and the null sink; unit tests for the builder (a secret can only be recorded as Changed).

KEY RULES:
- ROP: Either<EncinaError, Unit>; no exceptions for business logic.
- Timestamps are DateTimeOffset named ...AtUtc, taken from TimeProvider by the caller.
- XML docs on every public member; zero warnings.

REFERENCE FILES:
- src/Encina.Security.Audit/Abstractions/IOperationAuditStore.cs
- src/Encina/Abstractions/MultiTenancyMarker.cs (style of a core marker type)
```

</details>

### Phase 2: The Audit Adapter & Registration

> **Goal**: `Encina.Security.Audit` writes the record as an `OperationAuditEntry`.

<details>
<summary><strong>Tasks</strong></summary>

1. **Create `src/Encina.Security.Audit/OperationAuditConfigurationChangeSink.cs`** (`internal sealed`, **singleton**, Design Choice 4): constructor `(IServiceScopeFactory scopes, ILogger<...>)`; per call it opens a scope and resolves `IOperationAuditStore` and `IPiiMasker?` in it (the masker masks the values the emitter marks personal, Design Choice 7; no `TimeProvider`, time comes from the record); maps the record per Design Choice 6 (`Metadata["before"]`, `Metadata["after"]` as the change sets' name/value dictionaries, `Metadata["event"]`); returns the store's `Left` unchanged.
2. **Modify `ServiceCollectionExtensions.cs`** (`AddEncinaAudit`): register the adapter as a singleton. Registration order: the null sink of core is replaced because `AddEncinaAudit` removes a null-sink descriptor first (small helper next to `OperationAuditStoreRegistration`), so the adapter wins in any registration order (`AGENTS.md` section 3).
3. **DI test** with `ValidateOnBuild` and `ValidateScopes`, both orders, and the "audit + resharding" combination (a resolved singleton orchestrator with the singleton adapter and a scoped store).
4. Counter and log messages in Phase 6.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 2</strong></summary>

```text
CONTEXT:
- Phase 1 merged. IOperationAuditStore is scoped for the database providers and singleton for the in-memory default; the adapter is a singleton that opens a scope per call, because the orchestrator that consumes it is a singleton.

TASK:
Implement the adapter and its registration; test with a fake IOperationAuditStore that fails, succeeds and records.

KEY RULES:
- Registration completeness: provider before and after AddEncinaAudit gives the same result.
- A store failure is returned, never swallowed; logs carry the error code only.

REFERENCE FILES:
- src/Encina.Security.Audit/OperationAuditStoreRegistration.cs
- src/Encina.Security.ABAC/DecisionAudit/AuditStoreABACDecisionRecorder.cs
- tests/Encina.UnitTests/ADO/PostgreSQL/OperationAuditStoreRegistrationTests.cs
```

</details>

### Phase 3: Sharding Emission

> **Goal**: `ShardingConfigChanged` around the topology swap, for execute and rollback.

<details>
<summary><strong>Tasks</strong></summary>

1. **Modify `ReshardingOrchestrator.cs`**: inject `IConfigurationChangeAuditSink` (constructor, null-checked); before `SwapTopologyAsync` in the cutover phase build the record: `Before` and `After` from the two topologies as shard ids, active flags and replica counts, with `Changed("connection_strings")` for any shard whose connection strings differ; call `RecordAsync`; a `Left` fails the resharding (phase `Failed`) before the swap with `encina.sharding.audit_unavailable`. After the swap record the outcome entry. `RollbackAsync` does the same with `EventName = "ShardingConfigChanged"` and a `rollback` fact. The failure points follow the table of Design Choice 9 (recommended): execute write-ahead failure blocks the swap; a failed rollback write-ahead or a failed outcome entry never undoes or blocks the topology and leaves an `AuditPending` marker in the resharding state store that the next cycle retries; each point has a unit test with a fake sink.
2. Actor: the identity of the request when there is one; otherwise `service:encina.sharding.resharding`.
3. **Modify `ReshardingServiceCollectionExtensions.cs`** to keep the sink resolvable (the null sink makes it optional).
4. **Log messages** for the sharding side: the ids follow the highest id in use in `Core = (100, 199)` computed at implementation time over the whole range (ids 121-161 are already used in `src/Encina/Sharding` alone, so the plan numbers none); packed sequentially, codes only: write-ahead failed, outcome failed, rollback audit failed, `AuditPending` retried.
5. **Error code** `encina.sharding.audit_unavailable` in `ReshardingErrorCodes.cs` (`ErrorCategory.Unavailable`, Design Choice 3) with its `GetCategory` unit test.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 3</strong></summary>

```text
CONTEXT:
- ReshardingOrchestrator is internal sealed with a TimeProvider; SwapTopologyAsync is on IReshardingServices. ShardInfo has ConnectionString and ReplicaConnectionStrings that must never be recorded.

TASK:
Emit the write-ahead record before the swap and the outcome record after, for execute and rollback; unit tests with a fake sink and fake IReshardingServices (sink fails -> no swap, result Failed; swap fails -> outcome Failure recorded).

KEY RULES:
- No secret value in any record (ConfigurationChangeSet.Changed only).
- Time from the injected TimeProvider.
- A Left from the sink is returned as the resharding failure; never swallowed.

REFERENCE FILES:
- src/Encina/Sharding/Resharding/ReshardingOrchestrator.cs (lines 85-213)
- src/Encina/Sharding/Resharding/IReshardingServices.cs
- src/Encina/Sharding/ShardInfo.cs
```

</details>

### Phase 4: Tenancy Lifecycle Events (after the prerequisite)

> **Goal**: `TenantCreated`, `TenantUpdated`, `TenantDeactivated` emitted by the tenant administration service. Starts only after the issue `tenant-lifecycle-administration` is merged.

<details>
<summary><strong>Tasks</strong></summary>

1. In the administration service (Design Choice 5B), build and record the write-ahead record before calling the store, per operation: Created (`After`: name, strategy, schema name, `Changed("connection_string")` when set), Updated (`Before` and `After` of the changed non-secret fields), Deactivated (`IsActive` true to false).
2. `TenantId` of the entry is the affected tenant, not the caller's; `UserId` is the actor.
3. `TenantShardMigrated` is not emitted until `tenant-shard-assignment` exists; the event name and its record shape are reserved in the sink documentation.
4. **Log messages** in a new class `TenancyConfigurationAuditLog` with the new range `TenancyConfigurationAudit = (6370, 6379)` (ids 6370-6374: tenant created, updated, deactivated audited, audit write failed, audit retry), claimed from `artifacts/decisions/plan-batch/eventid-blocks.md` because `Tenancy` ids 1805-1810 belong to #711; register the range in `EventIdRanges.cs` first and map it to the `Encina.Tenancy` entry of `AssemblyRanges` in `EncinaEventIdAllocationTests`.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 4</strong></summary>

```text
CONTEXT:
- The administration service and TenantInfo.IsActive come from the prerequisite issue. The sink is IConfigurationChangeAuditSink.

TASK:
Emit the three events from the service operations; tests with a fake store and a fake sink (sink failure -> no store change).

KEY RULES:
- ConnectionString is recorded as Changed only.
- The affected tenant id is the entry's TenantId; the caller is the actor.

REFERENCE FILES:
- src/Encina.Tenancy/Abstractions/ITenantStore.cs
- src/Encina.Tenancy/Diagnostics/TenancyLog.cs
```

</details>

### Phase 5: Configuration & DI

> **Goal**: confirm opt-in behavior and the null sink.

<details>
<summary><strong>Tasks</strong></summary>

1. No option is added: auditing is on when `AddEncinaAudit` is registered; without it the null sink applies (documented as "configuration changes are not audited").
2. A start-up check (`IHostedService`, following `ABACDecisionAuditStartupCheck`), per Design Choice 8 (recommended A): fails host start when resharding is registered and the sink is the null sink, unless `ReshardingOptions.AllowUnauditedTopologyChanges` is set, in which case it logs once by code that topology changes are unaudited; with option B it logs a warning once instead. Multi-tenancy alone (no tenant administration yet) does not trigger it.
3. DI tests for the combinations (audit only, resharding only, both, resharding with the opt-out).

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 5</strong></summary>

```text
CONTEXT:
- Pay-for-what-you-use: audit is opt-in. A warning, not an error, when the null sink is in use with resharding.

TASK:
Add the start-up check and the DI tests.

KEY RULES:
- Hosted-service loops never let an exception escape; the check runs once.
- Logs carry no tenant or connection data.

REFERENCE FILES:
- src/Encina.Security.ABAC/DecisionAudit/ABACDecisionAuditStartupCheck.cs
```

</details>

### Phase 6: Cross-Cutting Integration and Observability

> **Goal**: counter, log messages, ranges.

<details>
<summary><strong>Tasks</strong></summary>

1. **Counter** in `OperationAuditMeter` (`src/Encina.Security.Audit/Diagnostics/OperationAuditMeter.cs`): `encina.audit.config_changes` (`Counter<long>`, tags `event` and `outcome`); the issue proposed `encina.audit.config_change_count`; ADR-054 (lowercase, dot separated, no `_total`) is followed and the name is fixed here.
2. **Log messages** for the adapter: Security.Audit ids 5016-5018 (the next ids after the last one of #798, 5015: recorded (debug), store failed, masker unavailable); three messages, packed, no gap.
3. **Tenant tag**: through the shared helper of #2676 when it exists (ADR-054 D4); the entry's `TenantId` is already persisted.
4. No span: events are recorded inside existing operations.
5. The new range `TenancyConfigurationAudit = (6370, 6379)` is registered and mapped in the pull request that adds the tenant events (Phase 4); the sharding ids sit in the existing `Core` range, already mapped.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 6</strong></summary>

```text
CONTEXT:
- ADR-021: ranges are registered before use; ids sequential.

TASK:
Add the counter and the log messages; check EventIdRanges.cs for the highest used id in each range first.

KEY RULES:
- Never log EncinaError.Message, connection strings or tenant secrets.
- Instrument names follow ADR-054 settled point 4.

REFERENCE FILES:
- src/Encina/Diagnostics/EventIdRanges.cs
- src/Encina.Security.Audit/Diagnostics/OperationAuditMeter.cs
```

</details>

### Phase 7: Testing — 7 Test Types

> **Goal**: per-flag targets; secrets and failure semantics proven.

<details>
<summary><strong>Tasks</strong></summary>

| Test type | Scope | Location |
|-----------|-------|----------|
| Unit | change set builder, record mapping, adapter (store success and failure), resharding emission (sink success, sink failure, swap failure, rollback), tenant events | `tests/Encina.UnitTests/Sharding/`, `.../Security/Audit/`, `.../Tenancy/` |
| Guard | new constructors and public methods | `tests/Encina.GuardTests/` |
| Contract | `IConfigurationChangeAuditSink` contract against the adapter and the null sink; a record never contains the connection string, scanned over all entries written by a full resharding run | `tests/Encina.ContractTests/Security/Audit/` |
| Property | justification `.md`: the before/after capture is a simple mapping covered by unit tests | `tests/Encina.PropertyTests/Tenancy/ConfigurationAudit.md` |
| Integration | justification `.md`: no store column; the existing `IOperationAuditStore` integration tests cover persistence | `tests/Encina.IntegrationTests/Tenancy/ConfigurationAudit.md` |
| Load | justification `.md`: configuration changes are rare | `tests/Encina.LoadTests/Tenancy/ConfigurationAudit.md` |
| Benchmark | justification `.md`: no hot path | `tests/Encina.BenchmarkTests/Tenancy/ConfigurationAudit.md` |

**Per-file targets** (proposed; measured values replace them, never lower):

"Baseline" is the per-file target in `.github/coverage-manifest/` on `d1793713d`; "none" means no per-file entry (the package default applies).

| File | Baseline | Flags | Justification |
|------|----------|-------|---------------|
| `Encina/Auditing/ConfigurationChangeSet.cs` (new, PR 1a) | new | unit 100, guard 95 | pure builder. |
| `Encina/Auditing/ConfigurationChangeRecord.cs` (new, PR 1a) | new | unit 95, guard 95 | record with init members. |
| `Encina/Auditing/IConfigurationChangeAuditSink.cs` (new, PR 1a) | new | unit 0 | an interface, no executable line. |
| `Encina/Auditing/NullConfigurationChangeAuditSink.cs` (new, PR 1a) | new | unit 100, guard 100 | one method returning success. |
| `Encina/Core/ServiceCollectionExtensions.cs` (PR 1a) | unit 96, guard 86, contract 92 | unchanged | one registration line in a covered method. |
| `Security.Audit/OperationAuditConfigurationChangeSink.cs` (new, PR 1b) | new | unit 98, guard 90, contract 90 | mapping, scope-per-call and failure paths. |
| `Security.Audit/ServiceCollectionExtensions.cs` and `OperationAuditStoreRegistration.cs` (PR 1b) | unit 92, guard 82; unit 95 | unchanged | one registration and one helper branch, covered by the order tests. |
| `Security.Audit/Diagnostics/OperationAuditMeter.cs` (PR 1b) | unit 100, guard 0 | unchanged | one counter read through a meter listener. |
| `Security.Audit/Diagnostics/ConfigurationChangeAuditLog.cs` (new, PR 1b) | new | unit 90 | generated logging methods exercised with a fake logger. |
| `Encina/Sharding/Resharding/ReshardingOrchestrator.cs` (PR 2) | none | unit 90, guard 80 | the emission points and every failure branch of Design Choice 9 are unit-tested with a fake sink. |
| `Encina/Sharding/Resharding/ReshardingServiceCollectionExtensions.cs` (PR 2) | none | unit 80 | one registration check, proven by the DI test. |
| `Encina/Sharding/Resharding/ReshardingErrorCodes.cs` (PR 2) | none | unit 90 | the new code asserted with its category. |
| `Encina/Sharding/Resharding/ShardTopologyChangeSetMapper.cs` (new, PR 2) | new | unit 100, guard 90 | the pure topology-to-change-set mapping (CRAP extraction). |
| `Encina/Sharding/Resharding/ReshardingAuditStartupCheck.cs` (new, PR 1c) | new | unit 95 | the three outcomes (audit present, null sink with opt-out, null sink without) are unit-tested. |
| `Encina.Tenancy/Diagnostics/TenancyConfigurationAuditLog.cs` (new, PR 3) | new | unit 90 | generated logging methods. |
| Tenant administration service (prerequisite) | owned by the prerequisite | unit 95, guard 90 | the audit calls add branches; the prerequisite issue sets its own targets. |

CRAP gate: extract the topology diff (`ShardTopology` to change set) into its own small method to keep every method at or below 10.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 7</strong></summary>

```text
CONTEXT:
- Per-flag coverage (AGENTS.md section 9); tests execute real code; Shouldly via Encina.Testing.Shouldly.

TASK:
Write the tests above and the four justification .md files (template of AGENTS.md section 9). Update the manifests (Encina, Encina.Tenancy, Encina.Security.Audit) through mechanical-fixer.

KEY RULES:
- No Thread.Sleep; FakeTimeProvider for time; test data with builders.
- A test never swallows an exception thrown by the code it drives.

REFERENCE FILES:
- tests/Encina.UnitTests/Sharding (existing resharding tests)
- tests/Encina.ContractTests/Security/Audit/OperationAuditStoreContractTests.cs
```

</details>

### Phase 8: Documentation & Finalization

> **Goal**: documents, ADR, fragments.

<details>
<summary><strong>Tasks</strong></summary>

1. XML docs on all new public API; `README.md` of `Encina.Tenancy` and `Encina.Security.Audit`; configuration guide (docs-writer).
2. ADR in `docs/architecture/adr/`: "Configuration change audit through a core sink" once Design Choices 4, 5, 8 and 9 are answered (ADR number assigned when the ADR pull request opens).
3. `changelog.d/752-config-change-audit.added.md` (mechanical-fixer); `PublicAPI.Unshipped.txt` for `Encina` and `Encina.Security.Audit`.
4. `docs/INVENTORY.md` entry; build with 0 warnings; every coverage flag at its target.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 8</strong></summary>

```text
CONTEXT:
- Code phases merged; documentation remains.

TASK:
Delegate pages to docs-writer, fragments and PublicAPI to mechanical-fixer; verify the build.

KEY RULES:
- Never edit CHANGELOG.md; no hand-typed coverage figures in docs.

REFERENCE FILES:
- changelog.d/README.md
- src/Encina.Tenancy/README.md
```

</details>

---

## Research

### Relevant Standards & Specifications

| Source | Requirement | Use in this feature |
|--------|-------------|---------------------|
| ISO/IEC 27001:2022 Annex A 8.32 (change management) and SOC 2 CC8.1 | administrative changes recorded with actor, time and before/after | motivation named in the issue |
| ADR-036 | purpose-named audit stores | Design Choice 1 |
| ADR-054 settled point 4 and D4 | metric naming; tenant tag only when multi-tenancy is on | Phase 6 |
| `AGENTS.md` section 3 | no secrets in logs or storage; no swallowed errors; fail closed | Design Choices 2 and 3 |
| SPEC-002 REQ-061 (audit part) and REQ-007, REQ-008 | the audit records the tenant | `TenantId` of the entry |

### Existing Encina Infrastructure to Leverage

| Component | Location | Usage in this feature |
| :--- | :--- | :--- |
| IOperationAuditStore and OperationAuditEntry (Action, EntityType, EntityId, Metadata, TenantId) | src/Encina.Security.Audit/Abstractions/IOperationAuditStore.cs, src/Encina.Security.Audit/OperationAuditEntry.cs | the sink adapter writes the configuration change records here |
| OperationAuditFailureMode and OperationAuditErrors | src/Encina.Security.Audit/OperationAuditFailureMode.cs, src/Encina.Security.Audit/OperationAuditErrors.cs | failure semantics of the audit write |
| PersistentPolicyAdministrationPoint (PolicyActor, write-ahead audit of administrative changes) | src/Encina.Security.ABAC/Administration/PersistentPolicyAdministrationPoint.cs | reference for actor attribution and fail-closed administrative audit |
| AuditStoreABACDecisionRecorder | src/Encina.Security.ABAC/DecisionAudit/AuditStoreABACDecisionRecorder.cs | write-ahead recorder pattern through IOperationAuditStore |
| ReshardingOrchestrator | src/Encina/Sharding/Resharding/ReshardingOrchestrator.cs | ExecuteAsync and RollbackAsync call SwapTopologyAsync, the only runtime topology change |
| IReshardingServices.SwapTopologyAsync | src/Encina/Sharding/Resharding/IReshardingServices.cs | emission point of ShardingConfigChanged |
| ShardTopology and ShardInfo (ShardId, ConnectionString, ReplicaConnectionStrings, IsActive) | src/Encina/Sharding/ShardTopology.cs, src/Encina/Sharding/ShardInfo.cs | before/after summaries; connection strings are never recorded |
| ITenantStore and InMemoryTenantStore (read-only) | src/Encina.Tenancy/Abstractions/ITenantStore.cs, src/Encina.Tenancy/Stores/InMemoryTenantStore.cs | no write side today; the lifecycle prerequisite |
| TenantInfo (TenantId, Name, Strategy, ConnectionString, SchemaName; no IsActive, no shard) | src/Encina.Tenancy/Abstractions/TenantInfo.cs | shape the Created/Updated events summarise |
| TenancyMetrics, TenancyActivitySource, TenancyLog (EventIds 1800-1804) | src/Encina.Tenancy/Diagnostics/ | existing diagnostics of the tenancy package |
| OperationAuditMeter | src/Encina.Security.Audit/Diagnostics/OperationAuditMeter.cs | home of the new counter |
| EventIdRanges (Tenancy 1800-1899, SecurityAudit 5000-5099, Core 100-199) | src/Encina/Diagnostics/EventIdRanges.cs | ranges to extend |
| #751 (write-ahead decision audit) | src/Encina.Security.ABAC/DecisionAudit/ | pattern for fail-closed audit of administrative operations |
| Coverage manifests | .github/coverage-manifest/Encina.json, .github/coverage-manifest/Encina.Tenancy.json, .github/coverage-manifest/Encina.Security.Audit.json | per-file targets |

### Event ID Allocation

| Package | Range | Notes |
|---------|-------|-------|
| `Encina.Security.Audit` | 5016-5018 | inside `SecurityAudit = (5000, 5099)`; #798 uses 5013-5015 |
| `Encina.Tenancy` (configuration audit) | 6370-6374 inside the new `TenancyConfigurationAudit = (6370, 6379)` | claimed from `eventid-blocks.md` (free 6370-6399); `Tenancy` 1805-1810 belong to #711, none used here; the range is registered first and mapped to the `Encina.Tenancy` assembly |
| `Encina` (sharding) | first free ids in `Core = (100, 199)` after the highest in use | ids 121-161 are used in `src/Encina/Sharding` alone; compute over the whole range at implementation time, register nothing new |

### Estimated File Count

| Category | Files | Notes |
| :--- | :--- | :--- |
| Production, core abstraction and records (Encina) | 5 | new: sink interface, record, change set builder, null sink; modified: core registration |
| Production, adapter and DI (Security.Audit) | 5 | new 2 (adapter, log class), modified 3 |
| Production, sharding emission and start-up check (Encina) | 6 | modified: orchestrator, registration, error codes; new: mapper, start-up check, options edit |
| Production, tenancy lifecycle events (Encina.Tenancy), after the prerequisite | 6 | modified 3, new 3 (including the log class and the range) |
| Tests (unit, guard, contract, justification .md) | 24 | new |
| Documentation (READMEs, configuration guide, ADR, changelog fragments, PublicAPI, manifests) | 10 | docs-writer and mechanical-fixer |
| Total | about 56 | six pull requests (the tenant part after the prerequisite) |

---

## Combined AI Agent Prompts

<details>
<summary><strong>Combined prompt — all phases</strong></summary>

```text
PROJECT CONTEXT:
- Encina, .NET 10 / C# 14, pre-1.0, ROP. Issue #752: audit sharding and tenancy configuration changes through the existing IOperationAuditStore. Encina and Encina.Tenancy do not reference Encina.Security.Audit.
- Today only resharding changes the topology at runtime (SwapTopologyAsync); ITenantStore has no write side; TenantInfo has no active flag and no shard assignment.

IMPLEMENTATION OVERVIEW:
1. Core: IConfigurationChangeAuditSink, ConfigurationChangeRecord, ConfigurationChangeSet (secrets only as Changed), NullConfigurationChangeAuditSink.
2. Security.Audit: OperationAuditConfigurationChangeSink writing OperationAuditEntry (Action = event name, EntityType = ShardTopology or Tenant, Metadata = redacted before/after); registered so that it wins in any order.
3. ReshardingOrchestrator: write-ahead record before SwapTopologyAsync, outcome record after; execute and rollback; a sink Left before the swap fails the resharding with encina.sharding.audit_unavailable (Unavailable); the rollback and the post-swap outcome follow Design Choice 9.
4. After the tenant administration prerequisite: Created, Updated, Deactivated events; TenantShardMigrated after tenant-shard-assignment.
5. Counter encina.audit.config_changes, log messages in registered ranges, start-up warning for the null sink.
6. Tests of 7 types (4 justified), docs, ADR, fragments via specialists.

KEY PATTERNS:
- Write-ahead, fail closed; no secret value ever; actor from the request identity or a service identity; time from TimeProvider.
- Registration order never decides which sink wins.

REFERENCE FILES:
- src/Encina/Sharding/Resharding/ReshardingOrchestrator.cs
- src/Encina.Security.ABAC/DecisionAudit/AuditStoreABACDecisionRecorder.cs
- src/Encina.Security.ABAC/Administration/PersistentPolicyAdministrationPoint.cs
- src/Encina.Tenancy/Abstractions/ITenantStore.cs
```

</details>

---

## Cross-Cutting Integration Matrix

| # | Function | Status | Notes |
|---|----------|--------|-------|
| 1 | Caching | ❌ | Audit records are write-only here. |
| 2 | OpenTelemetry | ✅ | Counter `encina.audit.config_changes`; tenant tag through the #2676 helper when it exists. |
| 3 | Structured Logging | ✅ | Security.Audit 5016-5018, the new `TenancyConfigurationAudit` range 6370-6379 (ids 6370-6374), first free ids in the `Core` range for sharding; codes only. |
| 4 | Health Checks | ❌ | No new checkable dependency; a one-time start-up warning covers the null sink. |
| 5 | Validation | ❌ | No new input; the records are produced internally. |
| 6 | Resilience | ❌ | Failure semantics are fail closed (Design Choice 3), not a retry feature. |
| 7 | Distributed Locks | ❌ | No shared mutable state is introduced; concurrent reshardings are already serialised by the state store. |
| 8 | Transactions | ⏭️ | Defer: the audit write is not in the change's transaction (the plan says so: write-ahead plus an outcome entry, Design Choice 9). Whether change and audit can commit atomically through a unit of work is its own question, a separate line in the prerequisite issue file `tenant-lifecycle-administration`; its issue number is added here when `open-issue` has created it (until then this Defer carries no number). |
| 9 | Idempotency | ❌ | Each change is a distinct event. |
| 10 | Multi-Tenancy | ✅ | The entry records the affected tenant (`TenantId`). |
| 11 | Module Isolation | ❌ | No module scoping of configuration changes. |
| 12 | Audit Trail | ✅ | This is the audit feature (ADR-036 operation store). |

---

## Prerequisites & Dependencies

- **For Phases 1-3 and 5-8 (sharding)**: none open. They can start after the maintainer answers Design Choices 4, 8 and 9.
- **For Phase 4 (tenant events)**: issue files `tenant-lifecycle-administration` (administration service, write port, `TenantInfo.IsActive`) and, for `TenantShardMigrated`, `tenant-shard-assignment`; their issue numbers are listed here once `open-issue` has created them. #596 (persistent `ITenantStore`) supplies the persistence the service writes through.
- **Soft**: #2676 (tenant tag helper), #2972 (administration surface and platform-operator permission), #749 and #750 (sibling audit trails for messaging and sagas that can reuse the sink pattern), #798 (Security.Audit EventIds 5013-5015; this plan takes 5016-5018), #1257 (the tenant lifecycle consumer).

---

## Next Steps

1. Maintainer answers Design Choices 4, 5, 7, 8 and 9 when work on the issue starts (recorded in `## Maintainer Decisions`).
2. Open the two prerequisite issues from the files in `artifacts/issues/`.
3. Worker pull requests. Counts include tests, manifest entries, PublicAPI lines, fragments and justification files; each builds alone, carries its own tests and stays within 15 files and 1500 lines (recount at start):
   - **PR 1a** (core sink): `IConfigurationChangeAuditSink`, `ConfigurationChangeRecord`, `ConfigurationChangeSet`, `NullConfigurationChangeAuditSink`, `Core/ServiceCollectionExtensions.cs` (5); unit tests for the builder, the record and the null sink, and a DI test (4); `Encina.json` manifest, `PublicAPI.Unshipped.txt` (2): 11 files.
   - **PR 1b** (adapter): `OperationAuditConfigurationChangeSink`, `ServiceCollectionExtensions`, the null-sink replacement helper in `OperationAuditStoreRegistration`, `OperationAuditMeter`, `ConfigurationChangeAuditLog` (5); adapter unit, guard and contract tests, DI test with the audit + resharding combination (4); `Encina.Security.Audit.json` manifest, `PublicAPI.Unshipped.txt`, changelog fragment (3): 12 files.
   - **PR 1c** (start-up policy, Design Choice 8): `ReshardingAuditStartupCheck`, `ReshardingOptions`, `ReshardingServiceCollectionExtensions` (3); unit and DI tests (2); manifest, PublicAPI (2): 7 files.
   - **PR 2** (resharding emission): `ReshardingOrchestrator`, `ShardTopologyChangeSetMapper`, `ReshardingErrorCodes`, the state-store `AuditPending` marker (the resharding state type and its store interface implementers, counted at start) (4); unit tests for the orchestrator failure points, the mapper and the error category (3); the four justification `.md` files (4); manifest, PublicAPI (2): 13 files.
   - **PR 3**: Phase 4 (tenant events, the range registration, `TenancyConfigurationAuditLog`) after the prerequisite, with its tests.
   - **PR 4**: documentation, the ADR and the READMEs.
4. Link this plan from #752.
