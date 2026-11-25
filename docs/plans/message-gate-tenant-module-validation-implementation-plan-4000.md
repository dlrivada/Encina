# Implementation Plan: Tenant-Aware and Module-Scoped Validators for the Notification, CDC and Saga Validation Gates

> **Issue**: [#4000](https://github.com/dlrivada/Encina/issues/4000)
> **Type**: Feature (validation, multi-tenancy, messaging, CDC)
> **Related**: #3982 (owns the shared pieces: `ValidatorScope`, `ValidatorScopeKinds`, `ValidatorScopeDecision`, `ValidatorScopeSelector`, `AddEncinaValidatorScoping()`; this plan consumes them and opens its pull requests after its S1), #729 (notification gate, owns `ValidationOrchestrator.EvaluateMessageAsync` and the `encina.tenant_id` tag on `Encina.Publish`), #730 (CDC gate, owns `ICdcEventValidator<TEntity>` and the dispatcher stage), #732 (saga gate, owns `ISagaStepValidator<TData>` and the runner stage), #230 (owns `IRequestValidator<T>` and the runner), #738 (saga `TenantId`), #726 (central validation instrumentation), ADR-054, ADR-062 (merged: error category contract)
> **Complexity**: Medium-high (three gates in three packages, one new CDC contract, no persistence change of its own)
> **Estimated Scope**: ~900 lines of production code + ~3,050 lines of tests + ~420 lines of documentation (~4,420 lines in total, as the Estimated File Count table), in seven pull requests of at most 15 files and 1,000 lines each (S1 is delivered as S1a and S1b)
> **Note on #729, #730, #732, #230, #3982**: every statement about their types (`NotificationValidationGate`, `ICdcEventValidator<TEntity>`, `ISagaStepValidator<TData>`, `RequestValidatorRunner`, `ValidatorScopeSelector`) follows their plans (plan pull requests, not code on `main`); each is re-verified against `main` when the pull request that uses it starts

---

## Summary

### What exists today (read on 2026-11-24, main `278c164f1`)

- The three gates are planned, not built: #729 validates a notification once before the fan-out with the registered `IValidationProvider`; #730 adds `ICdcEventValidator<TEntity>` and a stage in `CdcDispatcher`; #732 adds `ISagaStepValidator<TData>` and a stage in `SagaRunner`. None of the three types exists in `src` (`git grep -n -E "ICdcEventValidator|ISagaStepValidator|NotificationValidationGate" -- src` is empty).
- Where the tenant comes from, per gate, in today's code:
  - Notification: the dispatcher receives the ambient `IRequestContext`, whose `TenantId` is a `string?` (`src/Encina/Abstractions/IRequestContext.cs:125`); #729 already tags `Encina.Publish` with `encina.tenant_id` only when a tenant is present (its register row C7).
  - CDC: `ChangeEvent(TableName, Operation, Before, After, Metadata)` and `ChangeMetadata(Position, CapturedAtUtc, TransactionId, SourceDatabase, SourceSchema)` carry no tenant (`src/Encina.Cdc/ChangeMetadata.cs`); #730 creates an anonymous `RequestContext.Create()` per event (its register row C6). `CdcDispatcher` is a singleton that resolves handlers from the root provider (`src/Encina.Cdc/ServiceCollectionExtensions.cs:80`, `CdcDispatcher.cs:126`); `CdcConfiguration` offers `AddHandler`, `WithTableMapping`, `WithOptions`, `WithMessagingBridge` and others (`CdcConfiguration.cs:70-253`) and nothing about tenants.
  - Saga: `SagaRunner.RunAsync` uses `IRequestContextAccessor.RequestContext` or, outside a dispatch, an anonymous `RequestContext.Create()`; its comment states that a caller needing a tenant outside a dispatch "must set `IRequestContextAccessor.RequestContext` themselves" (`src/Encina.Messaging/Sagas/LowCeremony/SagaRunner.cs:69-75`). `ISagaState` has no tenant today; #738 adds it, filled at `StartAsync` from the ambient context.
- The three validation providers reach the tenant differently: `FluentValidationProvider` copies `TenantId` into `RootContextData` (`FluentValidationProvider.cs:60-62`), `DataAnnotationsValidationProvider` into `ValidationContext.Items["TenantId"]` (`DataAnnotationsValidationProvider.cs:39-41`), and `MiniValidationProvider` does not read the context at all (`git grep -n TenantId -- src/Encina.MiniValidator` is empty). A tenant-specific rule written in a provider's native style therefore works under two of the three.
- The module of running code is ambient: `IModuleExecutionContext.CurrentModule` is an `AsyncLocal` string, `null` when no module executes, registered scoped by the database provider packages only (`src/Encina/Modules/Isolation/ModuleExecutionContext.cs:19-22`; `src/Encina.EntityFrameworkCore/ServiceCollectionExtensions.cs:304`). There is no message-to-module mapping (`IModule` has `Name` only). A CDC processor and a background saga run outside any module scope, so the ambient value is `null` there.
- #3982 plans the shared selection pieces (a public `ValidatorScopeSelector` that never skips a validator whose declared scope value is missing); this issue's three gates are the consumers it names.

### What this plan delivers

Tenant-aware and module-scoped validation for the three gates, built on the pieces of #3982, in six pull requests:

1. **CDC tenant source (S1a contract and registration, S1b stage and errors)**: `ICdcTenantResolver` (`string? Resolve(ChangeEvent)`), registered per entity with `CdcConfiguration.WithTenantResolver<TEntity, TResolver>()`, and `WithTenant<TEntity>(string tenantId)` as its constant form. The dispatcher stage builds the validation context with the resolved tenant (`RequestContext.Create().WithTenantId(tenant)`); a tenant that is required and cannot be resolved rejects the event (fail closed), unless the registration opts out explicitly with `TenantRequirement.Optional`, which is logged.
2. **CDC scoping (S2)**: `IScopedCdcEventValidator<TEntity>` (`ScopeKinds`, `AppliesTo(ValidatorScope)`) and `CdcConfiguration.WithEntityModule<TEntity>(string moduleName)`; the stage calls the public selector of #3982 with the declared module.
3. **Saga scoping (S3)**: `IScopedSagaStepValidator<TData>`, `SagaDefinition<TData>.WithModule(string)` and the runner call to the selector.
4. **Saga tenant outside a dispatch (S4, conditional on Design Choice 4 option B and on #738)**: the step context carries the tenant of the saga state when the ambient context has none.
5. **Notification scoping (S5)**: the gate also runs scoped `IRequestValidator<TNotification>` validators through the runner of #230, so notifications get tenant and module scoping like requests; the provider-native path is unchanged.
6. **Documentation (S6)**.

The tenant tag convention on telemetry (ADR-054 D4) is satisfied by #729 for notifications; for CDC and saga it is added by #726 where the central instrumentation creates activities (register row C8). Nothing changes for an application that registers no resolver, no scoped validator and no module.

- Affected packages: `Encina` (core: the notification gate change), `Encina.Cdc`, `Encina.Messaging`. The three validation provider packages need no change (a documented limit for `MiniValidationProvider`, Design Choice 3).
- Provider category: Validation (3) (`AGENTS.md` section 5): each gate is proven under FluentValidation, DataAnnotations and MiniValidator; Database (10) for the saga persisted outcome only where S4 adds a scenario to the existing provider-neutral scenarios class. The CDC connectors (SqlServer, PostgreSQL, MySql, MongoDb, Debezium) are untouched because the stage sits after the connector.
- Shared pieces: owned by #3982 (consumed here). Owned by this plan: `ICdcTenantResolver`, `TenantRequirement`, `IScopedCdcEventValidator<TEntity>`, `IScopedSagaStepValidator<TData>`, the codes `encina.cdc.tenant_unresolved` and `encina.cdc.tenant_resolution_failed`.

---

## Design Choices

<details>
<summary><strong>1. Where a CDC event gets its tenant (the issue's own Dilemma)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) A resolver per entity (`ICdcTenantResolver.Resolve(ChangeEvent)`, registered with `WithTenantResolver<TEntity, TResolver>()`), receiving the whole event including `Metadata.SourceDatabase`** | One connector serves a shared database with a tenant column; the same resolver serves database-per-tenant by mapping `SourceDatabase` to a tenant, so the constant form is a degenerate resolver | The resolver reads payload fields, so a malformed row cannot be resolved (handled fail closed, register row C2) |
| **B) One configured tenant per connector, as the issue states (`CdcConfiguration.WithTenant(id)`)** | Nothing is read from the payload; fits database-per-tenant deployments | The dispatcher does not know the connector (`ICdcDispatcher.DispatchAsync` takes the event only, `ICdcDispatcher.cs:23`; #636, open, adds a dispatch context), so it needs that change first; one connector per tenant in a shared-schema database |

### Chosen Option: **A — Resolver per entity, with `WithTenant<TEntity>` as its constant form** (recommended, pending the maintainer)

### Rationale

- The issue recommends A with B as the degenerate resolver; the event carries `SourceDatabase`, so one contract covers both deployments without a per-connector registration, and `WithTenant<TEntity>(string)` is the constant resolver (a convenience of A, not a third option).
- B wins when every deployment is database-per-tenant with one tenant per process and a payload read is unwelcome.

**Class:** Dilemma

</details>

<details>
<summary><strong>2. Where the module of a CDC event or a saga comes from</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) A declaration at registration: `CdcConfiguration.WithEntityModule<TEntity>(string)` and `SagaDefinition<TData>.WithModule(string)`; the gate passes it to the selector as the scope's module, falling back to the ambient `CurrentModule` when none is declared** | Works where no module scope is ever set (a CDC processor, a background saga); the module is a property of the registration, visible in code | Two new registration members; a mismatch between the declaration and the module's own name is possible |
| **B) The ambient `IModuleExecutionContext.CurrentModule` only** | No new members; one source of truth for modules | Always `null` in a CDC processor and a background saga, so a module-scoped validator would run for every event (fail closed) and module scoping would be inert there |

### Chosen Option: **A — Declared module with ambient fallback** (recommended, pending the maintainer)

### Rationale

- The issue asks for validators that run only for the module that owns the message; only a declaration can say that for a message that never passes through a module scope.
- B wins when the application always sets the module scope around every message, including its background work, and wants no extra registration.

**Class:** Dilemma

</details>

<details>
<summary><strong>3. How notifications get tenant and module scoping</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) The gate (#729) also runs `RequestValidatorRunner` (#230) for the notification, so a scoped `IRequestValidator<TNotification>` applies; `AddEncinaNotificationValidation` registers the runner and the selector** | One provider-neutral way for all three providers, including `MiniValidationProvider`, which cannot see the tenant; the same scoped contract as requests | The notification gate now runs async validators too; depends on #230 and #3982 |
| **B) No runner: tenant rules for notifications are provider-native only (FluentValidation `RootContextData["TenantId"]`, DataAnnotations `Items["TenantId"]`), module scoping is not available for notifications** | Smallest change; nothing new in the gate | MiniValidator users have no way to write a tenant rule for a notification; no module scoping; the three providers differ |

### Chosen Option: **A — The gate also runs the runner** (recommended, pending the maintainer)

### Rationale

- `AGENTS.md` section 5 asks the three providers to behave the same; only the provider-neutral contract gives `MiniValidationProvider` a tenant, because that provider ignores the context. A runner that is not registered would silently skip scoped validators, so `AddEncinaNotificationValidation` always registers it (register row C5).
- B wins when notifications are expected to carry no scoped rules in 1.0 and the smallest gate is preferred.

**Class:** Dilemma

</details>

<details>
<summary><strong>4. The tenant of a saga step that runs outside a dispatch</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Keep the documented contract of `SagaRunner`: outside a dispatch the context is anonymous unless the caller sets the accessor; a tenant-required step validator rejects (fail closed); tests and docs say so** | No dependency on #738; no change to `SagaRunner` beyond the selector call | A resumed or background saga run loses its tenant unless every caller sets the accessor |
| **B) When the ambient context has no tenant and the saga state has one (#738), the step context is `requestContext.WithTenantId(state.TenantId)`** | A background run validates under the tenant the saga started with, without caller discipline | Needs #738 implemented first (S4 waits); the runner reads the state it already holds; a state tenant differing from an ambient tenant must be handled (register row C6) |

### Chosen Option: **B — Derive from the saga state once #738 is implemented** (recommended, pending the maintainer)

### Rationale

- Saga steps routinely resume in a background job; a tenant-required rule that rejects every resumed step is a defect the caller cannot always avoid. B is delivered last (S4) so S3 does not wait for #738.
- A wins when #738 is not planned for 1.0 or when the maintainer wants the runner to stay free of state reads.

**Class:** Dilemma

</details>

### Register of defaults and semantics

| Id | Choice | Class | Basis |
|----|--------|-------|-------|
| C1 | A CDC entity with a registered resolver has a required tenant by default: when the stage ends with no usable tenant (usable is defined by C2), it rejects the event with `encina.cdc.tenant_unresolved`, created with `ErrorCategory.InvalidRequest` (ADR-062 rule R3: "a missing header, key, id or tenant is `InvalidRequest`"), before any validator runs; `TenantRequirement.Optional` is the explicit opt-out, set at registration and logged (EventId 662) | Settled | `AGENTS.md` section 3 (compliance and security gates fail closed: when context is missing the gate denies; the only opt-out is explicit and logged) and ADR-062 R3 (read in `docs/architecture/adr/062-error-category-contract.md`: the missing tenant case names `InvalidRequest`); holds under both options of Design Choice 1 (a constant tenant is always present, so it never reaches the rejection) |
| C2 | A usable tenant is a non-blank string of at most 128 characters that is not the value `default`; a resolved value outside that is treated as unresolved (C1). The value `default` is excluded because the sibling multi-tenancy plans use it as the reserved segment of the no-tenant partition in tenant-qualified ids, a convention no ADR or SPEC defines; if #738's pending Design Choice picks a fixed default tenant constant (its option B), `default` could be a legitimate stored tenant and this exclusion would have to be dropped. A resolver that throws is a `Left` `encina.cdc.tenant_resolution_failed` (`ErrorCategory.Internal`, rule R4, constant message), retried by the CDC failure path of #2343 and #730, never replaced by a guessed tenant; the exception is logged by type through `ForLogging()` (EventId 663). This plan composes no tenant-qualified id and no key by tenant (the resolver result goes to `WithTenantId`, nothing is concatenated), so injectivity does not arise here | Dilemma (recommended, pending the maintainer) | public codes, a numeric limit and a convention; option B: accept `default` and any non-blank value up to the limit, so a data defect and a legitimate default tenant are not confused; B wins when #738 adopts a default tenant constant |
| C3 | An unresolved-tenant rejection is non-retryable (a deterministic verdict, like `encina.cdc.validation_failed`): the processor branch of #730 dead-letters it at once with the code as the reason. Option B: retry it up to `MaxRetries` like any other `Left` (#2343) before dead-lettering | Dilemma (recommended, pending the maintainer): dead-letter at once | both end in the dead letter queue; retrying wins when the tenant is filled in by a later change to the same row that the retry could see |
| C4 | The stage never invents a tenant and never reuses one from another event: the context is built per event from the resolver result, and a context without a tenant is an anonymous context | Settled | `AGENTS.md` section 3 (compliance and security gates fail closed: missing context is not replaced by a guessed one) |
| C5 | Every gate registration registers what it resolves, with `TryAdd*`: `CdcConfiguration` registration paths and the saga registration call `AddEncinaValidatorScoping()`; under option A of Design Choice 3 `AddEncinaNotificationValidation` also calls `AddEncinaRequestValidators()` so a scoped notification validator is never silently ignored; each is proven by a DI test per package built with `ValidateOnBuild` and `ValidateScopes` | Settled | `AGENTS.md` section 3 (registration completeness: an `AddEncina*` method registers every dependency it resolves) |
| C6 | Under Design Choice 4 option B, when the ambient context and the saga state both carry a tenant and they differ, the runner (which holds both: the ambient context it resolves and the state it loaded) rejects the step. Option A: the existing `saga.not_found` semantics of #738's recommended Design Choice 1 (no new code, no leak of the saga's existence); option B: a code of this plan (`saga.step_tenant_mismatch`, category `Forbidden`, constant message); option C: the ambient tenant wins. Whether the pair is ever visible to the runner (#738 enforces the mismatch as a store predicate) is verified when S4 starts; S4 stays blocked on #738's Design Choice 1 | Dilemma (recommended, pending the maintainer): A | A matches the recommended store behavior and leaks nothing; B wins when operators must tell a wrong-tenant step from a missing saga; C wins when the ambient tenant is trusted over stored data (fails open, so only with a logged opt-out) |
| C7 | The singleton CDC dispatcher never holds a scoped service: it resolves the selector (a singleton) and the resolver (registered with the lifetime the application gives it, resolved from a scope created per validation by `IServiceScopeFactory`) | Settled | `AGENTS.md` section 3 (never inject a scoped service into a singleton; registration completeness); `src/Encina.Cdc/ServiceCollectionExtensions.cs:80` (singleton dispatcher) |
| C8 | Telemetry: the notification activity gets the tenant tag from #729 (its row C7), tag only when a tenant is present; for CDC and saga this plan creates no activity or instrument, the tag follows with #726's central instrumentation; no instrument is added | Trivial | ADR-054 D4; #729 register row C7; `AGENTS.md` section 6 allows "Defer (open an issue and reference it)": #726 is open |
| C9 | Tenant id in log entries: log entries carry whether a tenant was present, not its value (a boolean `HasTenant`). Option B: log the tenant id as a structured property when present | Dilemma (recommended, pending the maintainer): presence only | logs outlive and travel further than traces and a tenant id can identify a single practitioner; ADR-054 D4 covers telemetry tags, not logs; the id wins when operators must triage a single tenant from logs alone |
| C11 | An offboarded tenant needs no handling: neither the resolver contract nor the selector consults a tenant store, so rows of an offboarded tenant are validated like any other; tenant lifecycle stays with `Encina.Tenancy` | Trivial | the resolver and the selector read the event or context only; no `ITenantStore` call exists in the design |
| C12 | EventIds 662 (tenant optional and unresolved, warning) and 663 (resolver failed, error) inside `ValidationCdc` (660-669), registered by #730 S1; the decision logs of scoped selection are the selector's 553-555 (#3982), so no gate needs more. Packed, no new range | Trivial | `AGENTS.md` section 7 (ids inside a registered range, packed); #730 uses 660-661 and keeps 662-669 free |
| C13 | The scoped contracts are separate interfaces (`IScopedCdcEventValidator<TEntity> : ICdcEventValidator<TEntity>`, `IScopedSagaStepValidator<TData> : ISagaStepValidator<TData>`), so an unscoped validator is untouched and an unscoped gate registration behaves as in #730 and #732 | Trivial | `AGENTS.md` section 1 (pay for what you use: every feature opt-in); the scoped interfaces mirror the surface #3982 recommends and adapt if its Design Choice 1 is answered differently (Prerequisites) |

---

## Implementation Phases

### Phase 1: CDC Tenant Source

> **Goal**: A CDC event reaches its validators with a tenant when one is configured and is rejected when a required tenant is missing, in two pull requests: S1a (the contract, the registration members and their DI proof; needs #730 S1) and S1b (the stage, the processor branch, the codes and the log; needs S1a and #730 S1 to S3).

<details>
<summary><strong>Tasks</strong></summary>

S1a:

1. **Create** `src/Encina.Cdc/Validation/ICdcTenantResolver.cs` (`public interface ICdcTenantResolver { string? Resolve(ChangeEvent changeEvent); }`, XML docs with an example reading `Metadata.SourceDatabase`), `src/Encina.Cdc/Validation/TenantRequirement.cs` (`public enum TenantRequirement { Required = 0, Optional = 1 }`) and `src/Encina.Cdc/Validation/ConstantCdcTenantResolver.cs` (`internal sealed`, returns a fixed id; behind `WithTenant`).
2. **Modify** `src/Encina.Cdc/CdcConfiguration.cs`: `WithTenantResolver<TEntity, TResolver>(TenantRequirement requirement = TenantRequirement.Required) where TResolver : class, ICdcTenantResolver` and `WithTenant<TEntity>(string tenantId)` (a non-blank id; whether `default` is accepted follows register row C2: `ArgumentException` at registration otherwise). The resolver type and requirement are stored in the `CdcConfiguration` per entity type. **Modify** `src/Encina.Cdc/ServiceCollectionExtensions.cs` (where the registrations of the configuration are applied, `:86-90` for handlers): register each stored resolver as a transient service. **Modify** `src/Encina.Cdc/PublicAPI.Unshipped.txt` and `.github/coverage-manifest/Encina.Cdc.json` (through `mechanical-fixer`). Tests (S1a): `CdcConfigurationTenantGuardTests` (null and blank arguments of `WithTenant` and `WithTenantResolver`), `CdcTenantRegistrationTests` (DI test with `ValidateOnBuild` and `ValidateScopes` resolving a registered resolver and the dispatcher), `CdcTenantResolverContractTests` (a resolver returns the same tenant for the same event and never throws on a malformed payload object, run over a fixture resolver and `ConstantCdcTenantResolver`). S1a is 10 files: 3 new src, 3 changed (configuration, registration extension, PublicAPI), 1 manifest, 3 tests.

S1b:

3. **Modify** the dispatcher stage of #730 (`src/Encina.Cdc/Processing/CdcDispatcher.cs` or the stage class #730 creates): resolve the tenant per event with the rules of register rows C1, C2 and C4 and build the context with `RequestContext.Create().WithTenantId(tenant)`; resolver resolved from the per-validation scope (register row C7).
4. **Modify** `src/Encina.Cdc/Processing/CdcProcessor.cs`: the code `encina.cdc.tenant_unresolved` joins `encina.cdc.validation_failed` in the dead-letter-at-once branch of #730 (register row C3).
5. **Modify** `src/Encina.Cdc/Errors/CdcErrorCodes.cs` and `CdcErrors.cs`: the two codes with `ErrorCategory.InvalidRequest` (ADR-062 rule R3, register row C1) and `ErrorCategory.Internal` through `EncinaErrors.Create` and `EncinaErrors.FromException` with a constant message (details: table, operation only). **Modify** `src/Encina.Cdc/Validation/CdcValidationLog.cs` (#730): 662 and 663 (by table, operation, `HasTenant`; exception through `ForLogging()`; register rows C9, C12).
6. **Modify** `src/Encina.Cdc/PublicAPI.Unshipped.txt` (the two public code constants) and `.github/coverage-manifest/Encina.Cdc.json` (through `mechanical-fixer`).
7. Tests (S1b): `CdcTenantStageTests` (resolved tenant reaches the validator context; null, blank, over-length and, per register row C2, `default` are unresolved; required plus unresolved rejects with the `InvalidRequest` `Left`; optional plus unresolved runs anonymously and logs 662; a resolver that throws gives the `Internal` `Left`; two events resolve independently; the rendered logs carry no tenant value), `CdcErrorsTenantTests` (codes, categories, constant message), `CdcErrorsTenantGuardTests` in `Encina.GuardTests` (null table name and null exception of the two factories). S1b is 10 files: 0 new src, 6 changed (stage, processor, error codes, errors, log, PublicAPI), 1 manifest, 3 tests.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 1</strong></summary>

```text
CONTEXT:
Encina (.NET 10, C# 14, pre-1.0). Issue #4000, plan docs/plans/message-gate-tenant-module-validation-implementation-plan-4000.md,
phase 1 of 6 (pull requests S1a then S1b). The CDC validation stage is the one #730 creates (read docs/plans/cdc-event-validation-implementation-plan-730.md).

TASK:
1. Create ICdcTenantResolver and the internal constant resolver; add CdcConfiguration.WithTenantResolver and WithTenant.
2. In the dispatcher stage build the validation context with the resolved tenant; a required tenant that cannot be
   resolved rejects the event (encina.cdc.tenant_unresolved, ErrorCategory.InvalidRequest); a throwing resolver is
   encina.cdc.tenant_resolution_failed (Internal). Dead-letter the first at once in CdcProcessor.
3. Log 662 and 663 in the ValidationCdc range; PublicAPI and manifest through mechanical-fixer.

KEY RULES:
- Fail closed; the only opt-out is TenantRequirement.Optional, explicit and logged. Never guess a tenant.
- The dispatcher is a singleton: resolve the resolver from a per-validation scope; no scoped service in a singleton.
- Logs and errors carry codes, table, operation and HasTenant only; exceptions through ForLogging().

REFERENCE FILES:
- src/Encina.Cdc/CdcConfiguration.cs, Processing/CdcDispatcher.cs, Processing/CdcProcessor.cs, ChangeMetadata.cs
- src/Encina/Abstractions/IRequestContext.cs (:125, WithTenantId)
```

</details>

---

### Phase 2: CDC Scoped Validators and Module Declaration

> **Goal**: CDC validators can be scoped to tenants and modules (pull request S2; needs S1a, S1b and #3982 S1).

<details>
<summary><strong>Tasks</strong></summary>

1. **Create** `src/Encina.Cdc/Validation/IScopedCdcEventValidator.cs` (`public interface IScopedCdcEventValidator<TEntity> : ICdcEventValidator<TEntity> { ValidatorScopeKinds ScopeKinds { get; } bool AppliesTo(ValidatorScope scope); }`).
2. **Modify** `CdcConfiguration.cs`: `WithEntityModule<TEntity>(string moduleName)` (non-blank). **Modify** the dispatcher stage: per scoped validator, `ValidatorScopeSelector.Decide` with the scope `new ValidatorScope(context, declaredModule ?? ambientModule)`; the ambient module is read from `IModuleExecutionContext` resolved in the per-validation scope (the singleton dispatcher holds none; the interface is registered only by the database provider packages, so without one the ambient module is `null`); `Skip` removes the validator. **Modify** `src/Encina.Cdc/ServiceCollectionExtensions.cs`: the registration path calls `AddEncinaValidatorScoping()` (register row C5).
3. **Modify** `src/Encina.Cdc/PublicAPI.Unshipped.txt` and `.github/coverage-manifest/Encina.Cdc.json` (through `mechanical-fixer`; the dispatcher stage enters its Integration target here, reached by the provider integration test below).
4. Tests (S2): `CdcScopedValidatorTests` (a tenant-scoped validator runs for its tenant and is skipped for another; a module-scoped one is skipped for a different declared module and runs when no module is declared and none is ambient; a tenant-scoped validator runs when no tenant resolves and `Optional` is set, and rejects through its own rule; unscoped validators untouched), `CdcScopedValidatorGuardTests`, `CdcScopingRegistrationTests` (DI test: dispatcher and selector built with `ValidateOnBuild` and `ValidateScopes`), and `CdcTenantValidationProviderTests` (integration, no container: the same tenant-specific rule through the provider adapter of #730 under FluentValidation and DataAnnotations, and through a scoped validator under MiniValidator, per Design Choice 3).

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 2</strong></summary>

```text
CONTEXT:
Issue #4000, plan docs/plans/message-gate-tenant-module-validation-implementation-plan-4000.md, phase 2 of 6. Depends on S1
and on #3982 S1 (ValidatorScope, ValidatorScopeSelector, AddEncinaValidatorScoping; read its plan).

TASK:
1. Create IScopedCdcEventValidator<TEntity>; add CdcConfiguration.WithEntityModule<TEntity>(string).
2. In the dispatcher stage call the public selector per scoped validator with the declared module (ambient fallback).

KEY RULES:
- Skip only on an explicit false for a present value; a missing value or a throwing rule runs the validator.
- The selector is public (another assembly): do not use internals. Never a tenant or module value in a log.

REFERENCE FILES:
- src/Encina.Cdc/Processing/CdcDispatcher.cs, docs/plans/validator-scoped-selection-implementation-plan-3982.md
```

</details>

---

### Phase 3: Saga Scoped Step Validators

> **Goal**: Saga step validators can be scoped to tenants and modules (pull request S3; needs #732 S1 to S3 and #3982 S1).

<details>
<summary><strong>Tasks</strong></summary>

1. **Create** `src/Encina.Messaging/Sagas/Validation/IScopedSagaStepValidator.cs` (`public interface IScopedSagaStepValidator<in TData> : ISagaStepValidator<TData> where TData : class { ValidatorScopeKinds ScopeKinds { get; } bool AppliesTo(ValidatorScope scope); }`).
2. **Modify** `src/Encina.Messaging/Sagas/LowCeremony/SagaDefinition.cs`: `SagaDefinition<TData>.WithModule(string moduleName)` and `BuiltSagaDefinition<TData>.ModuleName` (`string?`, null by default).
3. **Modify** the runner stage of #732 (`SagaRunner.cs` or its stage class): per scoped validator of the step, `Decide` with `new ValidatorScope(requestContext, definition.ModuleName ?? ambientModule)`; `SagaRunner` gets an optional trailing `ValidatorScopeSelector? selector = null` constructor parameter (the `new SagaRunner(` sites, 15 lines in 6 test files on `origin/main` per `git grep -c "new SagaRunner(" -- src tests`, re-counted when S3 starts, compile unchanged; `null` means no selection); the ambient module is read from the runner's scoped `IModuleExecutionContext` (`null` without a database provider package); **modify** `MessagingServiceCollectionExtensions.cs`: the saga registration calls `AddEncinaValidatorScoping()` (register row C5).
4. **Modify** `src/Encina.Messaging/PublicAPI.Unshipped.txt` and `.github/coverage-manifest/Encina.Messaging.json` (through `mechanical-fixer`).
5. Tests (S3): `SagaScopedStepValidatorTests` (tenant and module scoping per step, a tenant-required step rejects under an anonymous context and the saga takes the existing compensation path), `SagaDefinitionModuleTests` (`WithModule`, null default, a blank module is an `ArgumentException`), `SagaScopedValidatorGuardTests` (the new interface needs no guard; the `WithModule` argument check, and the null selector as a valid case), `SagaScopingRegistrationTests` (DI test with `ValidateOnBuild` and `ValidateScopes` resolving `SagaRunner` and the selector).

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 3</strong></summary>

```text
CONTEXT:
Issue #4000, plan docs/plans/message-gate-tenant-module-validation-implementation-plan-4000.md, phase 3 of 6. Depends on
#732 S1-S3 (the runner stage) and #3982 S1.

TASK:
1. Create IScopedSagaStepValidator<TData>; add SagaDefinition<TData>.WithModule and BuiltSagaDefinition<TData>.ModuleName.
2. Call the public selector per scoped validator in the runner stage; register the selector from the saga registration.

KEY RULES:
- Optional trailing constructor parameter keeps the 14 existing SagaRunner construction sites compiling.
- A rejected step uses the existing step-failure path (compensation, persisted code only); no new saga error code.

REFERENCE FILES:
- src/Encina.Messaging/Sagas/LowCeremony/SagaDefinition.cs, SagaRunner.cs (:60-90)
- docs/plans/saga-step-validation-implementation-plan-732.md
```

</details>

---

### Phase 4: Saga Tenant From the Saga State (conditional)

> **Goal**: A background saga run validates under the tenant the saga started with (pull request S4; only when Design Choice 4 option B stands, and after #738 is implemented).

<details>
<summary><strong>Tasks</strong></summary>

1. **Modify** the runner (`SagaRunner.cs`): when the ambient context has no tenant and `ISagaState.TenantId` (#738) is set, the step context passed to validators and to `step.Execute` is `requestContext.WithTenantId(state.TenantId)`; when both are set and differ, the step is rejected as register row C6 (a Dilemma, option A recommended: `saga.not_found` semantics) says; when neither is set the context is unchanged.
2. **Modify** `.github/coverage-manifest/Encina.Messaging.json` (through `mechanical-fixer`): the Integration 50 target of `SagaRunner.cs` is entered here, by the scenario test below, only when this pull request is built.
3. Tests (S4): `SagaRunnerStateTenantTests` (state-only tenant reaches the validator; ambient-only unchanged; both equal unchanged; both different rejected; neither unchanged; a fake `ISagaState` with a tenant), an addition to the provider-neutral saga scenarios class of #732 S3 so the ten database providers run the scenario "resumed saga validates under the stored tenant" (the class runs on all ten stores through their existing integration collections), and a property test (FsCheck: for any pair of ambient and state tenants the effective tenant is deterministic and never an invented value).

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 4</strong></summary>

```text
CONTEXT:
Issue #4000, plan docs/plans/message-gate-tenant-module-validation-implementation-plan-4000.md, phase 4 of 6. Depends on S3
and on #738 (ISagaState.TenantId); skip this phase when the maintainer chose option A of Design Choice 4.

TASK:
1. Make SagaRunner enrich the step context with the saga state's tenant when the ambient context has none.
2. Add the unit, property and provider-neutral scenario tests.

KEY RULES:
- Never invent a tenant; a conflicting pair is rejected through #738's handling. No new saga error code here.

REFERENCE FILES:
- docs/plans/saga-tenant-implementation-plan-738.md, src/Encina.Messaging/Sagas/LowCeremony/SagaRunner.cs
```

</details>

---

### Phase 5: Notification Gate Scoping

> **Goal**: Scoped request validators apply to notifications (pull request S5; needs #729 S1 to S3, #230 S1 and S2, #3982 S2).

<details>
<summary><strong>Tasks</strong></summary>

1. **Modify** the notification gate of #729 (`src/Encina/Validation/NotificationValidationGate.cs`): the gate receives `object notification` (`INotificationValidationGate.ShouldDispatchAsync(object ...)`), while the runner entry of #230 is generic (`RunAsync<TRequest>`), so this pull request adds a non-generic entry to `RequestValidatorRunner` (`RunAsync(Type requestType, object request, ...)`, a delegate per runtime type cached in a static generic holder, as the behaviors of the pipeline cache per type); the gate takes `RequestValidatorRunner` as a required constructor dependency, never an optional `GetService`, so a missing runner is a registration error found by `ValidateOnBuild` and scoped validators are never silently skipped (register row C5); after the provider validation it runs the runner for the runtime notification type with the gate's context and merges the result with the provider's; a failure maps through #729's `EvaluateMessageAsync` result handling and codes (no new code). **Modify** `src/Encina/Validation/RequestValidatorRunner.cs` (#230) for the entry.
2. **Modify** `AddEncinaNotificationValidation` (#729): `TryAdd` the runner and the selector by calling `AddEncinaRequestValidators()` (register row C5).
3. **Modify** `src/Encina/PublicAPI.Unshipped.txt` (the non-generic runner entry) and `.github/coverage-manifest/Encina.json` (through `mechanical-fixer`; the gate enters its Integration target here, reached by the three integration tests below).
4. Tests (S5): `NotificationScopedValidatorTests` (a tenant-scoped notification validator applies per tenant through `IEncina.Publish`; a module-scoped one applies per module; scoped validators do not run for an application that did not call `AddEncinaNotificationValidation`; the non-generic runner entry equals the generic one), `RequestValidatorRunnerGuardTests` edit (a null request type and a null request of the new entry), `NotificationValidationScopingRegistrationTests` (DI test with `ValidateOnBuild` and `ValidateScopes`), and three integration tests, one per provider (`tests/Encina.IntegrationTests/Validation/{DataAnnotations,FluentValidation,MiniValidator}/NotificationTenantValidationTests.cs`): the same tenant-specific rule behaves differently per tenant; under MiniValidator the rule is a scoped validator (Design Choice 3).

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 5</strong></summary>

```text
CONTEXT:
Issue #4000, plan docs/plans/message-gate-tenant-module-validation-implementation-plan-4000.md, phase 5 of 6. Depends on
#729 S1-S3, #230 S1-S2 and #3982 S2.

TASK:
1. Make the notification gate also run the shared RequestValidatorRunner; register runner and selector from
   AddEncinaNotificationValidation.
2. Write the unit, DI and three provider integration tests.

KEY RULES:
- Registration completeness: no scoped validator may be silently ignored for lack of a registration.
- No tenant or module value in logs; the tenant tag on Encina.Publish stays #729's.

REFERENCE FILES:
- docs/plans/notification-validation-implementation-plan-729.md, docs/plans/async-cross-field-validation-implementation-plan-230.md
```

</details>

---

### Phase 6: Documentation and Finalization

> **Goal**: Usage, READMEs, changelog and justification files (pull request S6).

<details>
<summary><strong>Tasks</strong></summary>

1. **Modify** the feature page of the message gates created by #729 (name verified when S6 starts; through `docs-writer`): a section "Tenant-aware and module-scoped validators" for the three gates, the CDC resolver and `TenantRequirement`, the module declaration, the MiniValidator limit and the scoped-validator path, and the saga contract about the ambient tenant.
2. **Modify** `src/Encina.Cdc/README.md` and `src/Encina.Messaging/README.md` (through `docs-writer`).
3. **Create** `changelog.d/4000-message-gate-tenant-module-validation.added.md` (through `mechanical-fixer`).
4. **Create** the justification files `tests/Encina.LoadTests/Validation/MessageGateScoping.md` and `tests/Encina.BenchmarkTests/Validation/MessageGateScoping.md` (`AGENTS.md` section 9 format): one selector call per scoped validator and one resolver call per event, negligible next to validation and the CDC read.
5. Build verification: `dotnet build --configuration Release` with zero warnings; `dotnet test` of the touched projects; per-flag coverage of the touched files against the targets of the Estimated File Count section.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 6</strong></summary>

```text
CONTEXT:
Issue #4000, plan docs/plans/message-gate-tenant-module-validation-implementation-plan-4000.md, phase 6 of 6.

TASK:
1. Document the feature (docs-writer): tenant source, scoping, module declaration, the provider limits.
2. Add the changelog fragment (mechanical-fixer) and the two justification files; run build and per-flag coverage.

KEY RULES:
- Never type coverage figures by hand: cite cov: markers (SPEC-001). Link ADRs and sibling plans by number only.

REFERENCE FILES:
- .claude/skills/encina-docs/SKILL.md, changelog.d/README.md
```

</details>

---

## Research

### Relevant Standards & Specifications

| Source | Provision | Use in this feature |
|--------|-----------|---------------------|
| `AGENTS.md` section 3 | Gates fail closed, the only opt-out explicit and logged; no scoped service in a singleton; errors never swallowed | Register rows C1, C3, C4, C5, C7 |
| `AGENTS.md` section 5 | Validation category: the three providers behave the same | Design Choice 3 |
| `AGENTS.md` section 6, rows 10 and 11 | Multi-tenancy and module isolation evaluated for every feature | The subject of this issue |
| ADR-054 D4 | Tenant tag only when a tenant is present | Register row C8 |
| ADR-062 | Error category contract: R3 (field rules are `Validation`; a missing tenant is `InvalidRequest`), R4 (infrastructure failures) | Codes of register rows C1 and C2 |

### Existing Encina Infrastructure to Leverage

| Component | Location | Usage in this feature |
|-----------|----------|-----------------------|
| `IRequestContext.TenantId`, `WithTenantId` | `src/Encina/Abstractions/IRequestContext.cs:125,185` | The tenant carried to every validator |
| `ChangeEvent`, `ChangeMetadata` | `src/Encina.Cdc/ChangeEvent.cs`, `ChangeMetadata.cs` | Input of the resolver (`SourceDatabase`, payload) |
| `CdcConfiguration`, `CdcDispatcher`, `CdcProcessor` | `src/Encina.Cdc/` | Registration, stage and dead-letter branch |
| `SagaRunner`, `SagaDefinition<TData>` | `src/Encina.Messaging/Sagas/LowCeremony/` | Context fallback (`SagaRunner.cs:69-75`) and the module declaration |
| `IModuleExecutionContext.CurrentModule` | `src/Encina/Modules/Isolation/` | Ambient module fallback |
| `ValidatorScope`, `ValidatorScopeSelector`, `AddEncinaValidatorScoping` (#3982) | `src/Encina/Validation/` (after #3982) | Selection in all three gates |
| `NotificationValidationGate`, `ICdcEventValidator<T>`, `ISagaStepValidator<T>` (#729, #730, #732) | their plans | The gates this plan extends |
| `FluentValidationProvider`, `DataAnnotationsValidationProvider`, `MiniValidationProvider` | `src/Encina.FluentValidation`, `src/Encina.DataAnnotations`, `src/Encina.MiniValidator` | Tenant reach per provider (`:60-62`, `:39-41`, none) |

### Event ID Allocation

| Package | Range | Notes |
|---------|-------|-------|
| `Encina.Cdc` (`ValidationCdc`, registered by #730 S1, inside the block 650-699 reserved for the validation family) | 662-663 | 660-661 used by #730; 662-663 used by this plan; 664-669 free. The scoping decision logs are #3982's 553-555 in `ValidationAsync`; the notification and saga gates need no id of their own |

### Estimated File Count

Every count includes tests, coverage manifest edits, PublicAPI files, READMEs and the changelog fragment. No pull request uses a type created by a later one; each enters its per-file manifest targets with its own tests and registers what it introduces with a DI test. Caller counts of changed constructors are re-counted with `git grep` when the pull request starts (the new parameters are optional).

| PR | Content | New src | Changed src | New tests | Edited tests | Manifest | Other | Files | Lines (est.) |
|----|---------|---------|-------------|-----------|--------------|----------|-------|-------|--------------|
| S1a | CDC tenant contract and registration | 3 (`ICdcTenantResolver`, `TenantRequirement`, `ConstantCdcTenantResolver`) | 3 (`CdcConfiguration`, `Cdc/ServiceCollectionExtensions`, PublicAPI) | 3 (guard, DI, resolver contract) | 0 | 1 | 0 | 10 | ~600 |
| S1b | CDC tenant stage, codes, log | 0 | 6 (dispatcher stage, `CdcProcessor`, `CdcErrorCodes`, `CdcErrors`, `CdcValidationLog`, PublicAPI) | 3 (stage, errors, errors guard) | 0 | 1 | 0 | 10 | ~500 |
| S2 | CDC scoped validators and module | 1 (`IScopedCdcEventValidator`) | 4 (`CdcConfiguration`, dispatcher stage, `Cdc/ServiceCollectionExtensions`, PublicAPI) | 4 (scoped, guard, DI, provider integration) | 0 | 1 | 0 | 10 | ~800 |
| S3 | Saga scoped validators and module | 1 (`IScopedSagaStepValidator`) | 4 (`SagaDefinition`, `SagaRunner`, `MessagingServiceCollectionExtensions`, PublicAPI) | 4 (scoped, definition, guard, DI) | 0 | 1 | 0 | 10 | ~850 |
| S4 | Saga tenant from state (conditional) | 0 | 1 (`SagaRunner`) | 2 (unit, property) | 1 (the saga scenarios class) | 1 | 0 | 5 | ~450 |
| S5 | Notification gate scoping | 0 | 4 (`NotificationValidationGate`, its registration extension, `RequestValidatorRunner`, PublicAPI) | 5 (scoped unit, DI, 3 integration) | 1 (`RequestValidatorRunnerGuardTests`) | 1 | 0 | 11 | ~800 |
| S6 | Documentation | 0 | 2 (the two READMEs) | 0 | 0 | 0 | 4 (feature page, fragment, 2 justification `.md`) | 6 | ~420 |
| **Total** | | **5** | **24** | **21** | **2** | **6** | **4** | **62** | **~4,420** |

The columns sum to the total (5 + 24 + 21 + 2 + 6 + 4 = 62) and the rows sum to 62 files (10 + 10 + 10 + 10 + 5 + 11 + 6) and about 4,420 lines (600 + 500 + 800 + 850 + 450 + 800 + 420). The largest pull request is S5 at 11 files; none exceeds 1,000 lines. S1a comes first; S1b needs S1a; S2 needs S1b and #3982 S1; S3 needs #732 S1 to S3 and #3982 S1; S4 needs S3 and #738; S5 needs #729, #230 and #3982 S2; S6 needs the rest.

Per-file coverage targets (per flag, with a one-sentence justification), written into the package manifests by the pull request that adds or changes the file:

| File (entered by) | Unit | Guard | Contract | Integration | Justification |
|------|------|-------|----------|-------------|---------------|
| `ICdcTenantResolver.cs`, `TenantRequirement.cs` (S1a) | 0 | 0 | 0 | 0 | An interface with no executable code; behavior is covered through the implementations |
| `ConstantCdcTenantResolver.cs` (S1a) | 90 | 0 | 40 | 0 | One return, covered by the S1a contract test and the S1b stage tests; guard 0 because the type is internal without `InternalsVisibleTo` for `Encina.GuardTests` |
| `CdcConfiguration.cs` (S1a, S2: changed members) | 85 | 20 | 0 | 0 | The new registration members are reached by the S1a DI test and unit tests; guard 20 because the S1a guard test reaches only the argument checks of two methods in a large configuration class (measured evidence for the rule: the `Encina.Cdc` manifest guard target is 15 and `OutboxCdcHandler.cs` measures 15.3 %) |
| `Cdc/ServiceCollectionExtensions.cs` (S1a, S2: changed registrations) | 85 | 15 | 0 | 0 | The new resolver and scoping registrations are reached by the S1a and S2 DI tests; guard 15 because the existing file is large and its guard tests own the argument checks, the S1a guard file adds none for it |
| `CdcValidationLog.cs` (S1b, changed) | 90 | 0 | 0 | 0 | The two new log methods are exercised by the stage tests; guard 0 because source-generated log methods have no guarded parameters |
| The dispatcher stage (S1b enters Unit 90; S2 enters Integration 50) | 90 | 0 | 0 | 50 | Every tenant branch (resolved, unresolved required, optional, throws) is reachable with fakes in S1b and every selector decision in S2; guard 0 because the stage is internal without `InternalsVisibleTo` for the guard project; the Integration 50 is written by S2, whose provider integration test reaches the main path |
| `CdcProcessor.cs` (S1b, changed branch) | 85 | 0 | 0 | 0 | One new dead-letter condition, reached by a stage-to-processor unit case; the processor's guards are #730's |
| `CdcErrors.cs` (S1b, changed) | 95 | 30 | 0 | 0 | Two factories reached by the S1b errors tests; guard 30 because the S1b guard file reaches the argument checks of the two new factories in a file that holds many other factories |
| `CdcErrorCodes.cs` (S1b, changed) | 100 | 0 | 0 | 0 | Two constants, covered through the factories; no parameters to guard |
| `IScopedCdcEventValidator.cs`, `IScopedSagaStepValidator.cs` (S2, S3) | 0 | 0 | 0 | 0 | Interfaces with no executable code; behavior is covered through the stages |
| `SagaDefinition.cs` (S3, changed members) | 90 | 25 | 0 | 0 | `WithModule` and `ModuleName` are reached by the S3 definition tests; guard 25 because the S3 guard file reaches only the `WithModule` argument check of a file that is mostly the definition builder |
| `MessagingServiceCollectionExtensions.cs` (S3, changed registration) | 85 | 0 | 0 | 0 | The saga registration call is reached by the S3 DI test; guard 0 because the change is one call in a large file whose argument checks belong to the package's existing guard tests |
| `SagaRunner.cs` (S3: Unit 90, Guard 15; S4, built only under option B of Design Choice 4: Integration 50) | 90 | 15 | 0 | 0 | The selector branches are reached with fakes in S3; guard 15 because the S3 guard file reaches only the optional constructor parameter of a file that is saga flow; the Integration 50 is entered only by S4, whose scenario reaches the main path on the ten stores, so it does not exist when option A stands |
| `RequestValidatorRunner.cs` (S5, the new non-generic entry; its Unit and Guard targets here extend #230's and #3982's rows) | 90 | 20 | 0 | 0 | The entry's branches (cached delegate, equality with the generic entry) are reached by the S5 unit tests; guard 20 because the edited `RequestValidatorRunnerGuardTests` reaches the null checks of the new entry only |
| `NotificationValidationGate.cs` (S5, changed method) | 90 | 0 | 0 | 50 | The runner branch is reached by the S5 unit tests; guard 0 because the gate is internal (#729) without `InternalsVisibleTo` for the guard project (the non-generic runner entry it calls is public and guard-tested in S5); the S5 integration tests reach the main path |

Property targets are entered only for the S4 saga tenant rule (`SagaRunner.cs` property 40, from the S4 property test: for any pair of tenants the effective tenant is deterministic and never invented); the other files carry no property target.

---

## Combined AI Agent Prompts

<details>
<summary><strong>Full combined prompt for all phases</strong></summary>

```text
You are implementing issue #4000 - tenant-aware and module-scoped validators for the notification, CDC and saga validation gates.

PROJECT CONTEXT:
- Encina is a .NET 10 / C# 14 library; pre-1.0, no backward compatibility, best solution always.
- Validation follows the orchestrator pattern (AGENTS.md sections 3 and 5). The gates are #729 (notification), #730
  (CDC) and #732 (saga); the shared selection pieces are owned by #3982.
- Gates fail closed: a missing tenant or module never removes a rule; the only opt-out is explicit and logged.

IMPLEMENTATION OVERVIEW:
1. CDC tenant source: ICdcTenantResolver, WithTenantResolver, WithTenant, TenantRequirement (S1a); the stage and two error codes (S1b).
2. CDC scoped validators and WithEntityModule (S2); saga scoped validators and WithModule (S3).
3. Saga tenant from the saga state once #738 is implemented (S4, conditional on a Dilemma).
4. The notification gate also runs the shared runner so scoped validators apply to notifications (S5).
5. Docs, changelog, justification files (S6).
Seven pull requests (S1a, S1b, S2 to S6), see the Estimated File Count table.

KEY PATTERNS:
- The CDC dispatcher is a singleton: resolve scoped services from a per-validation scope; no scoped service in a singleton.
- Optional trailing constructor parameters keep existing construction sites compiling; DI test with ValidateOnBuild
  and ValidateScopes for every registration this plan adds.
- Codes, table, operation and HasTenant only in logs and errors; never a tenant id, module name or payload; ForLogging().

REFERENCE FILES:
- docs/plans/message-gate-tenant-module-validation-implementation-plan-4000.md
- docs/plans/validator-scoped-selection-implementation-plan-3982.md, docs/plans/notification-validation-implementation-plan-729.md,
  docs/plans/cdc-event-validation-implementation-plan-730.md, docs/plans/saga-step-validation-implementation-plan-732.md
```

</details>

---

## Cross-Cutting Integration Matrix

| # | Function | Status | Notes |
|---|----------|--------|-------|
| 1 | Caching | ❌ N/A | Validation is per message; a resolver result is not cached (a cache would risk serving one event's tenant to another) |
| 2 | OpenTelemetry | ⏭️ #726 | #729 tags `Encina.Publish` with `encina.tenant_id` only when present; CDC and saga tags follow with #726's central instrumentation (open); this plan adds no instrument (register row C8) |
| 3 | Structured Logging | ✅ Phase 1 | EventIds 662-663 in `ValidationCdc`; scoping decisions logged by #3982's selector (553-555); presence of a tenant only (register row C9) |
| 4 | Health Checks | ❌ N/A | No checkable dependency |
| 5 | Validation | ✅ All phases | This is the validation feature; each gate is proven under the three providers |
| 6 | Resilience | ❌ N/A | No external call; a resolver that throws follows the CDC retry path of #2343 |
| 7 | Distributed Locks | ❌ N/A | No shared state |
| 8 | Transactions | ❌ N/A | Validation runs before the work and writes nothing |
| 9 | Idempotency | ❌ N/A | Validation and tenant resolution are repeatable for the same event |
| 10 | Multi-Tenancy | ✅ All phases | The subject of the issue: resolver, fail-closed required tenant, saga tenant from state, tenant-scoped validators; the reserved `default` segment is never a usable tenant (register row C2) |
| 11 | Module Isolation | ✅ Phases 2, 3, 5 | Declared or ambient module passed to the selector (Design Choice 2) |
| 12 | Audit Trail | ❌ N/A | Auditing of rejected messages belongs to the audit family; a rejection is a `Left` or a dead letter entry that already records the code |

---

## Prerequisites & Dependencies

### Required Prerequisites

| Prerequisite | Status | Notes |
|-------------|--------|-------|
| `ValidatorScope`, `ValidatorScopeSelector`, `AddEncinaValidatorScoping` (#3982) | Open (plan only) | Blocking for S2, S3 and S5; if #3982 Design Choice 1 is answered differently, those pull requests are re-planned |
| CDC validation stage and `ICdcEventValidator<TEntity>` (#730), and #2343 (a `Left` is retried or dead-lettered) | Open | Blocking for S1a, S1b and S2 (S1a needs #730 S1; S1b needs #730 S1 to S3). Transitive chain: `IScopedRequestValidator<T>` extends `IRequestValidator<T>`, so #3982 S1 itself waits for #230 S1, and the scoping decision logs 553-555 exist only after #3982 S2, so S2 and S3 of this plan log no scoping decision until then |
| Saga step validation stage and `ISagaStepValidator<TData>` (#732) | Open | Blocking for S3 |
| Notification gate and `EvaluateMessageAsync` (#729); `IRequestValidator<T>` and the runner (#230) | Open | Blocking for S5 |
| `ISagaState.TenantId` (#738) | Open | Blocking for S4 only |

### Recommended (Not Blocking)

| Dependency | Issue | Notes |
|-----------|-------|-------|
| Central validation instrumentation | #726 (open) | CDC and saga tenant tags |
| Dispatch context for `ICdcDispatcher` | #636 (open) | Would let a resolver see the connector id; not required here (`SourceDatabase` is on the event) |

---

## Next Steps

1. Link this plan from #4000 and ask the maintainer to answer Design Choices 1 to 4 and the Dilemma rows C2, C6 and C9 when work starts; remove `needs-decision` from the issue after the answers are recorded.
2. Implement S1a, S1b and S2 to S6 as the pull requests of the Estimated File Count split in the order the table gives, after the prerequisites of each; S4 only when Design Choice 4 option B stands and #738 is implemented; `Fixes #4000` on S6.
3. Re-verify against `main` when each pull request starts: the stage and context types of #730 and #732, the gate of #729, the selector of #3982, and the 14 `SagaRunner` construction sites.
