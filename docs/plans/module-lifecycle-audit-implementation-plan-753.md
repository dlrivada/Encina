# Implementation Plan: Audit trail for module activation, deactivation and configuration changes

> **Issue**: [#753](https://github.com/dlrivada/Encina/issues/753)
> **Type**: Feature
> **Complexity**: Medium (7 phases, no store column, 2 pull requests; it starts after #360 and #752)
> **Estimated Scope**: ~520 lines of production code + ~1,000 lines of tests, manifest and documentation (about 2,450 lines over 2 pull requests of 15 files each; the split table is the authority)
> **Family**: modular monolith (#360 owns the lifecycle and the module state, #352 the module context, #747 and #761 the module dimension of messaging rows). Shared piece owned elsewhere: the configuration-change sink of #752

---

## Summary

Record when a module is activated or deactivated and when its configuration changes, in the existing audit trail, so that after an incident an operator can say which module changed state, when, from which state to which, and under which actor. The three events of the issue are `ModuleActivated`, `ModuleDeactivated` and `ModuleConfigUpdated`.

**State of `origin/main` (`ec129cae5`), verified by reading and grep (corrects the issue text).**

- The issue names `IAuditStore` and "module lifecycle services". Today the audit stores are `IOperationAuditStore` and its siblings (ADR-036); the only lifecycle service is `ModuleLifecycleHostedService` (`src/Encina/Modules/ModuleLifecycleHostedService.cs`). It calls `OnStartAsync` and `OnStopAsync` of the modules that implement `IModuleLifecycle`, is registered only `if (registry.GetLifecycleModules().Count > 0)` (`ModuleServiceCollectionExtensions.cs`, line 84), and logs with hand-written `logger.LogInformation` calls. There is no `ModuleActivated`, state or audit concept in the module code.
- **There is no runtime activation or deactivation operation.** A module "activates" when the host starts and "deactivates" when it stops. No API toggles a module while the host runs, and the issue does not ask for one ("emit from module lifecycle services"). The plan therefore audits the transitions that exist (host-driven) and defers a runtime toggle (Design Choice 1).
- Module configuration today is read once at registration (`IModule.ConfigureServices(IServiceCollection)`); nothing watches it. The plan of #360 (merged plan, not yet code) adds `AddEncinaModules(services, IConfiguration, configure)` and a configuration section `Modules:{Name}` per module (`GetModuleSection`), and a `ModuleLifecycleState` with `IModuleStateProvider` (`Registered`, `Configured`, `Initializing`, `Running`, `Stopping`, `Stopped`, `Failed`) that is updated by the rewritten hosted service, which is registered whenever `AddEncinaModules` is used.
- The audit port for configuration changes is the sink of the plan of #752: `IConfigurationChangeAuditSink.RecordAsync(ConfigurationChangeRecord)` in `Encina/Auditing/`, a null sink registered with `TryAddSingleton`, and an adapter in `Encina.Security.Audit` (a singleton that opens a scope per call and writes an `OperationAuditEntry` through `IOperationAuditStore`). `Encina` does not reference `Encina.Security.Audit` (`src/Encina/Encina.csproj` has no such reference, while `Encina.Security.Audit.csproj` references `Encina`), so core code can only call the sink.
- The actor precedent is `ABACPolicySeedingHostedService`, which runs under the built-in service identity `service:encina.abac.policy-seeding` and records it as the actor of every change (`src/Encina.Security.ABAC/ABACPolicySeedingHostedService.cs`, line 40).
- `EventIdRanges` has `ModuleIsolation = (1900, 1999)`; the plan of #360 reserves `ModulesLifecycle = (7600, 7619)`; this plan takes a range from its own block (Event ID Allocation).

**Not in scope**: a runtime operation that activates or deactivates a module while the host runs (follow-up issue file `artifacts/issues/module-runtime-administration.md`); recording configuration values (Design Choice 5); audit of module data access (read audit is ADR-036's `IReadAuditStore`); module identity on audit rows (#1636).

**Affected packages**: `Encina` (auditor, configuration watcher, hosted-service call sites, log class, EventIds), `Encina.Security.Audit` (no change: the adapter of #752 serves these records). **Provider category**: none (the events use the existing `IOperationAuditStore` through the #752 adapter; no store column, so the 10-provider matrix does not apply).

---

## Design Choices

<details>
<summary><strong>1. What "activation" and "deactivation" mean today (Dilemma D1)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Audit the transitions that exist: `ModuleActivated` when a module reaches `Running` (before = `Configured`, after = `Running`) or `Failed` while initializing (the same event with `Succeeded = false`), `ModuleDeactivated` when it reaches `Stopped` (before = `Running`, after = `Stopped`); read from the state machine of #360; runtime toggling is deferred to an issue file** | Matches the issue ("emit from module lifecycle services"); no new feature; every module is covered, including plain `IModule` classes | One pair of entries per module per host start and stop (noise on restarts) |
| **B) First add a runtime administration API (`IModuleAdministration.ActivateAsync`/`DeactivateAsync`) and audit that** | A real operator action with a human actor | A new feature that needs design (state, handler gating, in-flight requests) and exceeds the issue and one pull request |
| **C) Audit only changes between hosts (compare with the previous run's state)** | Less noise | Needs persisted state of the previous run; the module system has no store |

### Chosen Option: **A — host-driven transitions now, runtime toggling deferred** (recommended, pending the maintainer)

### Rationale

A wins while module state changes only with the host, which is what the code does. B wins if the reference application needs operators to switch a module off at run time. C wins for an application that restarts often and wants only real changes in the trail. The restart noise is bounded: two entries per module and host lifetime, written once.

**Class:** Dilemma

</details>

<details>
<summary><strong>2. Which port records the events (Dilemma D2)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Reuse `IConfigurationChangeAuditSink` and `ConfigurationChangeRecord` of #752 with `SubjectType = "Module"`, `SubjectId` = the module name and `EventName` = the issue's event names** | One abstraction, one adapter and one `OperationAuditEntry` mapping for every administrative change; no new reference | This plan cannot be implemented before #752's sink exists (prerequisite) |
| **B) A module-specific sink `IModuleAuditSink` in `Encina`** | Independent of #752; typed to module events | A second port with the same adapter work in `Encina.Security.Audit`; two ways to record an administrative change |
| **C) `Encina` references `Encina.Security.Audit` and writes `OperationAuditEntry` directly** | No port | A project reference cycle (`Encina.Security.Audit` references `Encina`); breaks pay-for-what-you-use |

### Chosen Option: **A — the sink of #752** (recommended, pending the maintainer)

### Rationale

A wins when the maintainer confirms #752's recommendation (its Design Choice 4). B wins if #752 is rejected or lands much later and the module trail is needed first. C is excluded by the project graph. If #752's choice changes, this plan follows it: the auditor of Phase 2 depends only on a method that takes a record and returns `Either<EncinaError, Unit>`.

**Class:** Dilemma

</details>

<details>
<summary><strong>3. When an audit write fails (Dilemma D3)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) By event: `ModuleActivated` is written before the module's `OnInitializeAsync` runs and a `Left` from the sink fails the module start (the host does not start with an unaudited activation); `ModuleDeactivated` is written after the stop and never blocks shutdown: a `Left` is retried (bounded, delay from the injected `TimeProvider`) and then reported by error code and a counter; `ModuleConfigUpdated` is written after the fact and is retried the same way (the configuration provider already changed)** | Fail closed where the change can still be stopped; never blocks a safe shutdown; never reports success while a record is missing | An unaudited shutdown is possible when the store stays down past the retries |
| **B) Every write is best effort: log and continue** | Never blocks anything | Swallows an error in infrastructure; a host can start with an unaudited activation |
| **C) Every write blocks, including shutdown and reload** | Strictest | An audit outage can keep a host from stopping cleanly and a reload callback cannot fail the provider's change |

### Chosen Option: **A — fail closed before the activation, never block a stop** (recommended, pending the maintainer)

### Rationale

A wins when availability of the shutdown path matters more than completeness of the trail during an outage (the reasoning of #752's Design Choice 9 for rollback). B wins when the trail is advisory. C wins when an unaudited change is never acceptable. A `System.Threading.Timer` or `TimeProvider.CreateTimer` retry callback catches its own failures and never throws (`AGENTS.md` section 3, #2774); the failure is logged by error code through `ForLogging()` (exception type and stack trace, never its message). The new error code `encina.modules.audit_unavailable` has ADR-062 category `Unavailable` and carries no store message.

**Class:** Dilemma

</details>

<details>
<summary><strong>4. How the audit is switched on (Dilemma D4)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Automatic when a real sink is registered (`AddEncinaAudit` replaces the null sink of #752); an explicit, logged opt-out `config.WithoutLifecycleAudit()` on `ModuleConfiguration`** | The audit package is the opt-in; a forgotten flag cannot leave module changes unaudited | Registering audit adds entries the application did not name one by one |
| **B) An explicit `config.AuditLifecycle()` flag, off by default** | Strict opt-in per feature | A forgotten flag means no trail; one more switch beside `AddEncinaAudit` |

### Chosen Option: **A — automatic with a real sink, logged opt-out** (recommended, pending the maintainer)

### Rationale

A wins when audit is a control that should not depend on a second switch (the compliance-never-optional stance of the maintainer's 2026-10-30 decision: an opt-out is explicit and logged). B wins when module audit is a diagnostic aid only. With the null sink nothing is written and the module system behaves as today (one Debug log, EventId 7724, per host start).

**Class:** Dilemma

</details>

<details>
<summary><strong>5. What a configuration change records (Dilemma D5)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Key paths only: the entry lists which keys under `Modules:{Name}` were added, removed or changed (`Changed(name)`), never a value; the change is found by comparing a per-key SHA-256 digest kept in memory** | A connection string or a secret in a module section can never reach the trail | The trail shows that a setting changed, not from what to what |
| **B) Key paths plus the values of an allow-list per module (`ModuleLifecycleAuditOptions.AuditedKeys`)** | Reconstructible for chosen non-secret settings (a feature switch, a limit) | Every allow-listed key is a decision someone must get right; a mis-listed secret leaks |
| **C) All values, masked by pattern** | Full picture | Masking by pattern leaks (#1173 and #1259 class) |

### Chosen Option: **A — key paths only** (recommended, pending the maintainer)

### Rationale

A wins because the code cannot know which keys are secrets (`AGENTS.md` section 3: secrets never reach plaintext storage). B wins when operators need the old and new value of a few known switches and accept the allow-list as a reviewed list. C wins nowhere. The digest is `SHA-256` over the UTF-8 value, kept per key in the watcher and never written; it is not a secret store (a low-entropy value could be guessed from a digest in memory, which never leaves the process).

**Class:** Dilemma

</details>

<details>
<summary><strong>6. Who is the actor</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) The built-in service identity `service:encina.modules.lifecycle` for host-driven transitions and configuration reloads, declared by `AddEncinaModules` and opened with `RunAsServiceAsync` around the audited work (the precedent of policy seeding); `Metadata["trigger"]` = `host_start`, `host_stop` or `configuration_reload`** | Same actor model as every other background administrative change; no anonymous entry | A reload has no human behind it, so the actor says "the module system" and the trigger says why |
| **B) Leave the actor empty** | Nothing to declare | An audit entry without attribution contradicts the acceptance criterion "who/when for all changes" |

### Chosen Option: **A — a declared service identity**

### Rationale

`ABACPolicySeedingHostedService` is the precedent: startup work runs under `service:encina.abac.policy-seeding`, which its registration declares, and the policy administration point records that actor.

**Class:** Trivial
**Settled by:** `src/Encina.Security.ABAC/ABACPolicySeedingHostedService.cs` (line 40, `ServiceIdentityName`) and `src/Encina.Security.ABAC/Administration/PersistentPolicyAdministrationPoint.cs` (the actor rule and the service identity of startup seeding).

</details>

<details>
<summary><strong>7. Telemetry names</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) One counter `encina.modules.lifecycle_audit_total` with tags `event` (`activated`, `deactivated`, `config_updated`), `outcome` (`recorded`, `failed`, `skipped_no_sink`) and `module.name` (the registered set, bounded); no tenant tag (modules are process-wide)** | Same tag vocabulary as module spans; bounded | A new counter |
| **B) Reuse the module dispatch counter of #747** | Fewer instruments | A different meaning (message dispatch) |

### Chosen Option: **A — a dedicated counter**

### Rationale

`module.name` is the existing module tag (`ModuleActivitySource`: `activity?.SetTag("module.name", moduleName)`); a tenant tag would be wrong because the lifecycle has no tenant (ADR-054 D4: the tenant tag only where a tenant exists).

**Class:** Trivial
**Settled by:** `src/Encina/Modules/Diagnostics/ModuleActivitySource.cs` (tag `module.name`) and [ADR-054](../architecture/adr/054-observability-conventions-and-trace-propagation.md) D4.

</details>

---

## Implementation Phases

### Phase 1: Prerequisites and shared types (pull request 1)

> **Goal**: confirm the pieces this plan consumes exist, and add the options.

<details>
<summary>Tasks</summary>

1. Verify at work start that #360's `IModuleStateProvider`, `ModuleLifecycleState` and the rewritten `ModuleLifecycleHostedService` and #752's `IConfigurationChangeAuditSink`, `ConfigurationChangeRecord`, `ConfigurationChangeSet` and `NullConfigurationChangeAuditSink` are on `main`; if not, the pull request waits (see Prerequisites).
2. `src/Encina/Modules/ModuleConfiguration.cs`: `WithoutLifecycleAudit()` (sets an internal flag, logged once at start by code, D4); `AuditRetryCount` (default 3) and `AuditRetryDelay` (default 2 seconds) with `ArgumentOutOfRangeException` for values below 1 and below zero (D3).
3. `src/Encina/Modules/ModuleErrorCodes.cs` gains `AuditUnavailable = "encina.modules.audit_unavailable"` (ADR-062 `Unavailable`, set where the error is created with the categorized overload).

</details>

<details>
<summary>Prompt for AI Agents — Phase 1</summary>

```text
CONTEXT:
- Issue #753; plan docs/plans/module-lifecycle-audit-implementation-plan-753.md. D1-D5 are recommended, pending the maintainer: follow the Maintainer Decisions section if it exists.
- Encina is pre-1.0: no compatibility overloads, no [Obsolete].

TASK:
Add the options and the error code; do not add the sink or the state provider, they belong to #752 and #360.

KEY RULES:
- Validate option values at registration time with ArgumentOutOfRangeException.
- XML docs on every public member; zero warnings; PublicAPI.Unshipped.txt updated.

REFERENCE FILES:
- src/Encina/Modules/ModuleConfiguration.cs
- src/Encina/Modules/ModuleServiceCollectionExtensions.cs
```

</details>

---

### Phase 2: The lifecycle auditor (pull request 1)

> **Goal**: map state transitions to records and write them.

<details>
<summary>Tasks</summary>

1. `src/Encina/Modules/Auditing/ModuleLifecycleAuditor.cs` (internal sealed): constructor `(IConfigurationChangeAuditSink sink, IRequestContextScopeFactory scopes, TimeProvider timeProvider, ILogger<ModuleLifecycleAuditor> logger)`; `ValueTask<Either<EncinaError, Unit>> RecordActivationAsync(string moduleName, ModuleLifecycleState before, ModuleLifecycleState after, bool succeeded, CancellationToken ct)`, `RecordDeactivationAsync(...)`: build the `ConfigurationChangeRecord` (`EventName` `ModuleActivated` or `ModuleDeactivated`, `SubjectType` `Module`, `SubjectId` = module name, `TenantId` null, `Before`/`After` = `Value("state", name)`, `ActorId` = `service:encina.modules.lifecycle`, `CorrelationId` from the scope's context, `OccurredAtUtc` from `TimeProvider`), run it under `RunAsServiceAsync` (D6).
2. Failure handling per D3: `RecordActivationAsync` returns the `Left` to the hosted service, which fails the module start; the stop and reload paths call `RecordWithRetryAsync`, a bounded retry whose delay comes from `TimeProvider.CreateTimer`/`Task.Delay(delay, timeProvider)` and whose final failure is logged (EventId 7723) and counted, never thrown.
3. `src/Encina/Modules/ModuleLifecycleHostedService.cs` (rewritten by #360): the auditor is an optional constructor parameter (null when the null sink is registered and nothing would be written, D4); calls: before `OnInitializeAsync` (`before` = `Configured`), after it reached `Running` or `Failed`, after `OnStoppedAsync`.
4. Tests: unit (each event maps to the exact record, actor and trigger, state names, `Succeeded = false` on a failed initialize, a `Left` before the activation fails the start, a `Left` on stop is retried the configured number of times with `FakeTimeProvider` and then counted, nothing is thrown out of the retry), guard (constructor nulls; the sink argument of `RecordActivationAsync` blank module name), property (the record for a state pair has `Before != After` and a module name equal to the input).

</details>

<details>
<summary>Prompt for AI Agents — Phase 2</summary>

```text
CONTEXT:
- Issue #753. #360 provides the state tracker and the hosted service; #752 provides the sink. The auditor depends only on the sink method and on IModuleStateProvider.

TASK:
Write ModuleLifecycleAuditor, wire it into the hosted service and test every transition.

KEY RULES:
- A Left before an activation fails the start; a stop is never blocked (D3). A retry callback never throws (AGENTS.md section 3, #2774).
- Time from TimeProvider; EncinaError.Message never logged; exceptions through ForLogging().
- Never claim atomicity with the module's own work: the record is written before OnInitializeAsync and the outcome after.

REFERENCE FILES:
- src/Encina/Modules/ModuleLifecycleHostedService.cs
- src/Encina.Security.ABAC/ABACPolicySeedingHostedService.cs
- docs/plans/config-change-audit-implementation-plan-752.md
```

</details>

---

### Phase 3: Registration (pull request 1)

<details>
<summary>Tasks</summary>

1. `ModuleServiceCollectionExtensions.AddEncinaModules`: `TryAddSingleton<ModuleLifecycleAuditor>()` through a factory that resolves the sink, the scope factory, `TimeProvider` and the logger (the auditor is a singleton, like the hosted service; the sink adapter of #752 is a singleton that opens a scope per call, so no scoped service is captured); declares the service identity `encina.modules.lifecycle` in the identity catalog options; when the sink is the null sink or `WithoutLifecycleAudit()` was called, the auditor is not given to the hosted service.
2. DI test `tests/Encina.UnitTests/Core/Modules/ModuleLifecycleAuditRegistrationTests.cs`: builds the provider with `ValidateOnBuild` and `ValidateScopes` with and without `AddEncinaAudit`, in both registration orders (the real sink wins over the null sink in any order, `AGENTS.md` section 3), and with `WithoutLifecycleAudit()`.

</details>

<details>
<summary>Prompt for AI Agents — Phase 3</summary>

```text
CONTEXT:
- Issue #753. Registration completeness (AGENTS.md section 3): the auditor and every dependency it resolves are registered, proven by a DI test with ValidateOnBuild and ValidateScopes. A database store wins over an in-memory default in any order.

TASK:
Register the auditor, declare the service identity and write the DI test.

KEY RULES:
- Never inject a scoped service into a singleton; the sink adapter opens its own scope.
- The opt-out is explicit and logged once, by error code.

REFERENCE FILES:
- src/Encina/Modules/ModuleServiceCollectionExtensions.cs
- src/Encina.Security.ABAC/ServiceCollectionExtensions.cs
```

</details>

---

### Phase 4: Configuration change detection (pull request 2)

> **Goal**: `ModuleConfigUpdated`.

<details>
<summary>Tasks</summary>

1. `src/Encina/Modules/Auditing/ModuleConfigurationWatcher.cs` (internal sealed, started by the hosted service after the module reached `Running`): for each module, snapshots `IConfiguration.GetSection("Modules:{Name}")` as `path -> SHA-256(value)`; `ChangeToken.OnChange(() => section.GetReloadToken(), OnChanged)`; the callback catches every exception (a throwing callback on the provider's thread would end the process, `AGENTS.md` section 3), computes added, removed and changed paths against the snapshot (ordinal comparison), builds the record with `Changed(path)` entries only (D5) and calls `RecordWithRetryAsync`.
2. A change with no differing key (a reload that changed nothing) writes nothing. `IDisposable` registrations are disposed on `StopAsync`.
3. Tests: unit (reload with an added, a removed and a changed key; no entry for an unchanged reload; no value in any record or log; a failing sink is retried and counted and the callback never throws), property (for any two key/value sets the recorded paths equal the symmetric difference plus the changed values, and never contain a value).

</details>

<details>
<summary>Prompt for AI Agents — Phase 4</summary>

```text
CONTEXT:
- Issue #753. The configuration section is Modules:{Name} (#360, GetModuleSection). IConfiguration comes from DI at run time.

TASK:
Write ModuleConfigurationWatcher and its tests; use an in-memory configuration provider with Reload() in the tests.

KEY RULES:
- Key paths only, never values (D5); the digest never leaves memory.
- The change-token callback never throws; a failure is retried then counted.

REFERENCE FILES:
- src/Encina/Modules/ModuleLifecycleHostedService.cs
- docs/plans/module-lifecycle-implementation-plan-360.md
```

</details>

---

### Phase 5: Cross-Cutting Integration (pull requests 1 and 2)

<details>
<summary>Tasks</summary>

1. Observability: `ModuleLifecycleAuditMetrics` (`Meter` `Encina.Modules.Audit`, counter of Design Choice 7), registered in the OpenTelemetry meter list of `src/Encina.OpenTelemetry/ServiceCollectionExtensions.cs` in pull request 2.
2. Logging: `src/Encina/Modules/Diagnostics/ModuleLifecycleAuditLog.cs` with EventIds 7720 activation recorded (Debug), 7721 deactivation recorded (Debug), 7722 configuration update recorded (Debug, number of keys only), 7723 audit write failed after the retries (Error, error code and `ForLogging()` only), 7724 audit skipped, no sink registered (Debug), 7725 opt-out `WithoutLifecycleAudit()` active (Warning, once). Range `EventIdRanges.ModuleLifecycleAudit = (7720, 7729)` registered first in `src/Encina/Diagnostics/EventIdRanges.cs` and in the `Encina` entry of the allocation map `tests/Encina.UnitTests/Testing/Architecture/EncinaEventIdAllocationTests.cs`.
3. Multi-tenancy (function 10): not applicable, modules are process-wide (#360 records the same).

</details>

<details>
<summary>Prompt for AI Agents — Phase 5</summary>

```text
CONTEXT:
- Issue #753. ADR-021: register the range first, pack ids from 7720 with no gaps (7726-7729 stay free).

TASK:
Add the log class, the range and the metrics and test them.

KEY RULES:
- Logs carry module names, error codes and counts, never EncinaError.Message or configuration values.
- Run the architecture tests that scan EventIds.

REFERENCE FILES:
- src/Encina/Diagnostics/EventIdRanges.cs
- src/Encina/Modules/Diagnostics/ModuleLog.cs
- tests/Encina.UnitTests/Testing/Architecture/EncinaEventIdAllocationTests.cs
```

</details>

---

### Phase 6: Testing (pull requests 1 and 2)

<details>
<summary>Tasks</summary>

1. **Unit, Guard, Property**: as listed in Phases 2 to 5, with each pull request.
2. **Integration** (pull request 2): `tests/Encina.IntegrationTests/Modules/ModuleLifecycleAuditIntegrationTests.cs`: a real `IHost` with `AddEncinaModules`, `AddEncinaAudit` (the adapter of #752 and `InMemoryOperationAuditStore`) and two modules (one with hooks, one plain `IModule`): after `StartAsync` the store holds one `ModuleActivated` entry per module with actor `service:encina.modules.lifecycle`, after a configuration reload a `ModuleConfigUpdated` entry with key paths and no value, after `StopAsync` the `ModuleDeactivated` entries in reverse order, and a failing store fails the start. The sink and store are the application's own, so no provider database is involved and the 10-provider rule does not apply (the adapter's own provider coverage belongs to #752 and ADR-036).
3. **Contract**: `ModuleLifecycleAuditContractTests` checks that every `ModuleLifecycleState` pair the hosted service can produce maps to an event name of the issue (pull request 1). **Load** and **Benchmark**: justification files `tests/Encina.LoadTests/Modules/LifecycleAudit.md` and `tests/Encina.BenchmarkTests/Encina.Benchmarks/LifecycleAudit.md` (a handful of writes per host lifetime; no concurrent path).
4. **Per-file targets** (written into `.github/coverage-manifest/Encina.json` with one-sentence justifications; baselines of edited files are measured at work start with `dotnet test tests\Encina.<Flag>Tests --collect "XPlat Code Coverage"` and `.github/scripts/coverage-report.cs`; a target is the measured value or higher):

   | File | Unit | Guard | Contract | Property | Integration | Justification |
   |---|---|---|---|---|---|---|
   | `Modules/Auditing/ModuleLifecycleAuditor.cs` | 95 | 0 | 80 | 80 | 80 | Every mapping, retry and failure branch runs with a fake sink and `FakeTimeProvider`; guard 0 because the type is internal without `InternalsVisibleTo` for `Encina.GuardTests` (its constructor parameters are required services checked by the unit tests); integration drives it through a real host |
   | `Modules/Auditing/ModuleConfigurationWatcher.cs` | 95 | 0 | 0 | 80 | 80 | Reload scenarios run on an in-memory configuration; guard 0 for the same internal-type reason; the property proves paths without values |
   | `Modules/ModuleConfiguration.cs` (edit) | 90 | 90 | 0 | 0 | 0 | The new options validate their range; guard covers the range checks |
   | `Modules/ModuleLifecycleHostedService.cs` (edit) | 90 | 80 | 0 | 0 | 80 | The audit call sites run in the host test; guard covers the constructor checks |
   | `Modules/Diagnostics/ModuleLifecycleAuditLog.cs`, `ModuleLifecycleAuditMetrics.cs` | 90 | 0 | 0 | 0 | 0 | Source-generated log methods and a counter reached through the auditor tests; no parameterized public member |
   | `Modules/ModuleErrorCodes.cs` (edit) | 100 | 0 | 0 | 0 | 0 | A constant asserted with the category test of ADR-062 |

</details>

<details>
<summary>Prompt for AI Agents — Phase 6</summary>

```text
CONTEXT:
- Issue #753. Tests travel with the pull request that adds the code they cover; the end-to-end host test is pull request 2.

TASK:
Write the tests of Phase 6 and the manifest targets with one-sentence justifications.

KEY RULES:
- Tests execute real code; Shouldly through Encina.Testing.Shouldly; FakeTimeProvider for retries; never swallow an exception thrown by the code under test (AGENTS.md section 9, #2774).
- Targets never go down.

REFERENCE FILES:
- tests/Encina.UnitTests/Core/Modules/ModuleLifecycleHostedServiceTests.cs
- docs/en/guides/TESTING.md
- .github/coverage-manifest/Encina.json
```

</details>

---

### Phase 7: Documentation & Finalization (pull request 2)

<details>
<summary>Tasks</summary>

1. XML docs on every new public member; `PublicAPI.Unshipped.txt` of `Encina` and `Encina.OpenTelemetry` (through `mechanical-fixer`).
2. `docs/features/module-lifecycle.md` (created by #360) gains an "Auditing module changes" section (through `docs-writer`); `docs/INVENTORY.md` line; changelog fragment `changelog.d/753-module-lifecycle-audit.added.md` (through `mechanical-fixer`).
3. `dotnet build --configuration Release` with 0 warnings; `dotnet test` with every flag at its target; CRAP of every changed method at most 10.

</details>

<details>
<summary>Prompt for AI Agents — Phase 7</summary>

```text
CONTEXT:
- Issue #753 is implemented and tested.

TASK:
Write the documentation set through the specialists that own them.

KEY RULES:
- Pages go through docs-writer; fragment and PublicAPI lines through mechanical-fixer; never edit CHANGELOG.md; coverage figures are cited with covref markers, never typed.

REFERENCE FILES:
- changelog.d/README.md
- .claude/skills/encina-docs/SKILL.md
```

</details>

---

### Proposed split into pull requests (limit 15 files and 1500 lines each)

| PR | Content | Files | Approx. lines |
|----|---------|------:|--------------:|
| 1 | `ModuleConfiguration`, `ModuleErrorCodes`, `ModuleLifecycleAuditor`, `ModuleLifecycleAuditLog`, `ModuleLifecycleHostedService` (edit), `ModuleServiceCollectionExtensions` (edit), `EventIdRanges` (edit), allocation test map (edit), `PublicAPI.Unshipped.txt`, `Encina.json` manifest, auditor unit test, DI test, options guard test, contract test, `ModuleLifecycleHostedServiceTests` (edit) | 15 | 1,250 |
| 2 | `ModuleConfigurationWatcher`, `ModuleLifecycleAuditMetrics`, `Encina.OpenTelemetry` meter registration, `ModuleLifecycleHostedService` (edit), `PublicAPI.Unshipped.txt` x2, manifests x2, watcher unit and property tests, host integration test, 2 justification files, `docs/features/module-lifecycle.md`, changelog fragment | 15 | 1,200 |

PR 1 builds and passes alone once #360 and #752 are on `main`; PR 2 follows it. The edits to `ModuleLifecycleHostedService` and `ModuleServiceCollectionExtensions` are the same files #360 rewrites, so this plan's PR 1 is opened after #360's pull requests 3 and 4 (hosted service and state) have merged. The recount: PR 1 = 8 production and registry files (`ModuleConfiguration`, `ModuleErrorCodes`, `ModuleLifecycleAuditor`, `ModuleLifecycleAuditLog`, `ModuleLifecycleHostedService`, `ModuleServiceCollectionExtensions`, `EventIdRanges`, the allocation test map) + 1 PublicAPI + 1 manifest + 5 test files (auditor unit, DI, options guard, contract, the edited `ModuleLifecycleHostedServiceTests.cs`) = 15. PR 2 = 3 production (`ModuleConfigurationWatcher`, `ModuleLifecycleAuditMetrics`, the OpenTelemetry registration) + the hosted-service edit that starts the watcher + 2 PublicAPI + 2 manifests (`Encina.json`, `Encina.OpenTelemetry.json`) + 3 tests (watcher unit, watcher property, host integration) + 2 justification files + 1 guide + 1 changelog fragment = 15.

---

## Research

### Relevant Standards & Specifications

| Reference | What it defines | Use in this feature |
|-----------|-----------------|---------------------|
| `AGENTS.md` section 3 | Errors never swallowed in background infrastructure; callbacks never throw; secrets never in plaintext storage; `EncinaError.Message` never logged | D3, D5, callbacks |
| [ADR-036](../architecture/adr/036-three-audit-stores.md) | Three purpose-named audit stores; the operation store is the write-audit sink | Records go through `IOperationAuditStore` (via #752) |
| [ADR-054](../architecture/adr/054-observability-conventions-and-trace-propagation.md) D4 | Tenant tag only where a tenant exists | No tenant tag |
| [ADR-062](../architecture/adr/062-error-category-contract.md) | Category on every error code | `encina.modules.audit_unavailable` is `Unavailable` |
| ISO/IEC 27001:2022 Annex A 8.15 and 8.32 (logging, change management) | Administrative changes are logged and attributable | The reason a change trail exists |
| SPEC-002 DEC-006 | Compliance gates fail closed | D3 and D4 |

### Existing Encina Infrastructure to Leverage

| Component | Location | Usage in this feature |
|-----------|----------|-----------------------|
| `ModuleLifecycleHostedService` | `src/Encina/Modules/ModuleLifecycleHostedService.cs` | Call sites of the auditor |
| `IModuleRegistry`, `ModuleConfiguration`, `AddEncinaModules` | `src/Encina/Modules/` | Module names, options, registration |
| `ModuleLifecycleState`, `IModuleStateProvider` | #360 (planned, `src/Encina/Modules/ModuleLifecycleState.cs`) | Before and after states |
| `IConfigurationChangeAuditSink`, `ConfigurationChangeRecord`, `ConfigurationChangeSet` | #752 (planned, `src/Encina/Auditing/`) | The port |
| `OperationAuditConfigurationChangeSink` | #752 (planned, `src/Encina.Security.Audit/`) | The adapter that writes `OperationAuditEntry` |
| `ABACPolicySeedingHostedService` | `src/Encina.Security.ABAC/ABACPolicySeedingHostedService.cs` | Service-identity precedent |
| `IRequestContextScopeFactory.RunAsServiceAsync` | `src/Encina/Identity/IRequestContextScopeFactory.cs` | Opens the actor scope |
| `ModuleActivitySource` | `src/Encina/Modules/Diagnostics/ModuleActivitySource.cs` | Tag vocabulary |
| `EventIdRanges`, allocation test | `src/Encina/Diagnostics/EventIdRanges.cs`, `tests/Encina.UnitTests/Testing/Architecture/EncinaEventIdAllocationTests.cs` | Range registration |

### Event ID Allocation

| Package | Range | Notes |
|---------|-------|-------|
| `Encina` (`ModuleLifecycleAudit`, new) | 7720-7729, ids 7720-7725 used | From the assigned block 7700-7749 (registry `artifacts/decisions/plan-batch/eventid-blocks.md`); 7726-7729 free; the allocation test map gains the range under `Encina`. Other plans of the family: #761 7700-7709, #2442 7710-7719 |

### Estimated File Count

| Category | Files | Notes |
|----------|------:|-------|
| Production | 9 | auditor, watcher, log, metrics, options, error code, 2 edits, OpenTelemetry registration |
| EventIds, PublicAPI, manifest | 5 | `EventIdRanges`, allocation map, `PublicAPI.Unshipped.txt` x2, `Encina.json` |
| Tests | 8 | auditor unit, watcher unit and property, DI, guard, contract, host integration, 2 justification files |
| Documentation | 2 | guide section, changelog fragment |
| **Total** | **30 file touches across 2 pull requests (15 + 15)** | The hosted service, `Encina.json` and `PublicAPI.Unshipped.txt` are edited in both pull requests and count in each; the table above groups them by kind |

---

## Combined AI Agent Prompts

<details>
<summary><strong>Full combined prompt for all phases</strong></summary>

```text
PROJECT CONTEXT:
- Encina is a pre-1.0 .NET 10 / C# 14 library; Either<EncinaError, T>; no [Obsolete], no compatibility layers.
- Issue #753: audit ModuleActivated, ModuleDeactivated and ModuleConfigUpdated. Plan: docs/plans/module-lifecycle-audit-implementation-plan-753.md; Dilemmas D1-D5 are recommended, pending the maintainer. Prerequisites: #360 (state tracker, hosted service) and #752 (the configuration-change sink and its adapter).

IMPLEMENTATION OVERVIEW (2 pull requests):
1. Options, error code, ModuleLifecycleAuditor, log class and EventIds, hosted-service call sites, registration, unit, DI and contract tests (Phases 1-3, 5, 6).
2. ModuleConfigurationWatcher, metrics, host integration test, documentation (Phases 4-7).

KEY PATTERNS:
- Records go through IConfigurationChangeAuditSink with SubjectType Module; the actor is service:encina.modules.lifecycle opened with RunAsServiceAsync.
- ModuleActivated is written before OnInitializeAsync and a Left fails the start; stops and reloads are retried with TimeProvider and never block or throw.
- Configuration changes record key paths only, found by per-key SHA-256 digests kept in memory.
- Singletons never capture scoped services; the sink adapter opens its own scope.

REFERENCE FILES:
- src/Encina/Modules/ModuleLifecycleHostedService.cs
- src/Encina/Modules/ModuleServiceCollectionExtensions.cs
- src/Encina.Security.ABAC/ABACPolicySeedingHostedService.cs
- docs/plans/config-change-audit-implementation-plan-752.md
- docs/plans/module-lifecycle-implementation-plan-360.md
```

</details>

---

## Cross-Cutting Integration Matrix

| # | Function | Status | Notes |
|---|----------|--------|-------|
| 1 | Caching | ❌ | Not applicable: lifecycle events are written once and never read on a hot path |
| 2 | OpenTelemetry | ✅ | Phase 5: counter `encina.modules.lifecycle_audit_total` with `module.name`, no tenant tag |
| 3 | Structured Logging | ✅ | Phase 5: `ModuleLifecycleAuditLog`, EventIds 7720-7725, module names and counts only |
| 4 | Health Checks | ❌ | Not applicable: the module health of #364 reads state; this feature adds no checkable dependency |
| 5 | Validation | ✅ | Option ranges validated at registration; a blank module name is rejected by the auditor |
| 6 | Resilience | ✅ | D3: bounded retry of stop and reload records with `TimeProvider`; the audit store call is the external dependency |
| 7 | Distributed Locks | ❌ | Not applicable: the lifecycle is per host; #360 records the same |
| 8 | Transactions | ❌ | Not applicable: the audit record is not part of a business unit of work; the orchestrator never calls `SaveChangesAsync` for it |
| 9 | Idempotency | ❌ | Not applicable: a host start is not a duplicable request; a retried write may produce a duplicate entry, which the entry's correlation id makes recognisable |
| 10 | Multi-Tenancy | ❌ | Not applicable: modules are process-wide code units; the reserved `default` tenant segment and injective tenant-qualified ids do not apply because no tenant-qualified identifier is built |
| 11 | Module Isolation | ✅ | The feature records module state; the module name is the registered name |
| 12 | Audit Trail | ✅ | The feature itself, through the sink of #752 and `IOperationAuditStore` (ADR-036) |

---

## Prerequisites & Dependencies

- **#360** (module lifecycle): `IModuleStateProvider`, the state transitions and the rewritten always-registered `ModuleLifecycleHostedService` (pull requests 3 and 4 of #360) and the `Modules:{Name}` section with the `Microsoft.Extensions.Configuration` package reference of `Encina` (pull request 2). Without them there is no state to read for plain modules.
- **#752** (configuration change audit): the sink, the record, the change set and the adapter (pull requests 1 and 2 of #752). Its Design Choices 4 and 6 are recommended there, pending the maintainer; this plan's D2 follows its answer.
- #1636 (module id on audit rows) is independent: it adds the module to audit queries, while these entries carry the module as `EntityId`.
- #352 (module context producer) is not required: the lifecycle does not run inside a request.

---

## Next Steps

1. The maintainer answers D1 to D5 when work on #753 starts; the answers go into `## Maintainer Decisions` and the markers are removed.
2. Wait for #360 pull requests 3 and 4 and #752 pull requests 1 and 2; then pull request 1, then pull request 2.
3. Open the issue file `artifacts/issues/module-runtime-administration.md` when this plan merges.
