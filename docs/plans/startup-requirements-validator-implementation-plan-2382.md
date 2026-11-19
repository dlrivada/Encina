# Implementation Plan: Startup Requirements Validator (`IEncinaModuleRequirements`)

> **Issue**: [#2382](https://github.com/dlrivada/Encina/issues/2382)
> **Type**: Feature (cross-cutting, fail-closed startup gate)
> **Related**: ADR-050 (the closed list of module requirements this validator enforces), #2381 (the ADR issue, closed), #2239 (the decision that allows required modules), #2412 (Anonymization, AntiTampering, Attestation in-memory defaults: first consumers of this mechanism), #1191 (Marten.GDPR key store), #2248 (the interim `DelayedRetryStartupCheck`), #2396 (processor liveness health), #1251 and #2184 (ADR-050 build order), ADR-021, ADR-054, ADR-062, SPEC-002 DEC-006
> **Complexity**: Medium (a small public contract and one validator, but 11 pull requests across 9 packages because each requirement row lives in its own package)
> **Estimated Scope**: ~650 lines of production code in core + ~120 lines per requirement row + ~2,600 lines of tests, in 11 pull requests of at most 15 files and 1,500 lines each (the estimates below stay under 1,000)

---

## Summary

### What exists today (read on 2026-11-19, main `8727c5cc3`)

- Four startup checks, each with its own shape and message, no shared contract: `ABACDecisionAuditStartupCheck` (`src/Encina.Security.ABAC/DecisionAudit/ABACDecisionAuditStartupCheck.cs`, an `IHostedService`; no store is a `Critical` log 9087 and a failed start, an in-memory store is the warning 9086, `BestEffort` is the warning 9084), `DelayedRetryStartupCheck` (`src/Encina.Messaging/Recoverability/DelayedRetryStartupCheck.cs`, `IServiceProviderIsService`, EventId 5457), `CryptoShreddingStartupValidationHostedService` (`src/Encina.Marten.GDPR/`, an `IHostedLifecycleService` that runs in `StartingAsync` so it stops the host before Marten's daemon starts; its `WarnInMemoryKeyStore` only warns) and `OperationAuditMaskingStartupValidator` (`src/Encina.Security.Audit/`). `BreakTheGlassStartupValidator` (`src/Encina.Security.BreakTheGlass/Services/`) is a fifth, outside ADR-050's table.
- The silent in-memory and first-registration-wins defaults named by the issue still exist: `TryAddSingleton<IDistributedLockProvider, MemoryDistributedLockProvider>()` (`src/Encina.Caching.Memory/ServiceCollectionExtensions.cs:67`); in-memory key, token and audit stores (`src/Encina.Compliance.Anonymization/ServiceCollectionExtensions.cs:91-93`); `InMemoryNonceStore` and `InMemoryKeyProvider` (`src/Encina.Security.AntiTampering/ServiceCollectionExtensions.cs:82-83`); `AddEncinaFakes` registers its fakes with `TryAddSingleton` (`src/Encina.Testing.Fakes/ServiceCollectionExtensions.cs:69-138`), so it can block real services registered later; `AddEncinaFluentValidation` uses `TryAddScoped<IValidationProvider, ...>` and `AddDataAnnotationsValidation` and `AddMiniValidation` use `TryAddSingleton<IValidationProvider, ...>` (`src/Encina.DataAnnotations/ServiceCollectionExtensions.cs:67,72`, `src/Encina.MiniValidator/ServiceCollectionExtensions.cs:60,65`), so the first registered provider silently wins.
- Not in `src`: `IEncinaModuleRequirements`, an `EncinaStartupValidator`, a test-only marker, any environment-based rule. `Encina` core already references `Microsoft.Extensions.Hosting.Abstractions` (`src/Encina/Encina.csproj`), so core can host the validator without a new package. The core EventId range `EventIdRanges.Core = (100, 199)` has 100-176 used (`Encina.cs`, `Encina.Stream.cs`, `RequestIdentityLog.cs`, `Sharding/**`) and 177-199 free on main.

### What this plan delivers

The mechanism of ADR-050 "Enforcement mechanism per row", Option A: a declarative `IEncinaModuleRequirements` contract that each `AddEncina*` method registers, and one hosted validator in core that collects every violation and fails the host with a message naming the module, the requirement and ADR-050. It also rejects test-only implementations outside Development unless an explicit, logged opt-out is set (ADR-050 sub-decision 2) and assumes multi-instance unless the application declares otherwise (sub-decision 3). It then delivers the rows whose ADR-050 source is #2382 and that fit one package each: Validation (a registration error for two providers), Distributed locks (the in-memory lock registered by `AddEncinaMemoryCache`), Testing (`Encina.Testing.Fakes` outside Development or Test), and the migration of the three existing checks that ADR-050 and the issue name (ABAC, Recoverability delayed retries, Marten.GDPR key store).

- Affected packages: `Encina` (core), `Encina.Security.ABAC`, `Encina.Messaging`, `Encina.Marten.GDPR`, `Encina.Caching.Memory`, `Encina.Testing.Fakes`, `Encina.FluentValidation`, `Encina.DataAnnotations`, `Encina.MiniValidator`.
- Provider category: none for the mechanism. The Validation category (3 providers, `AGENTS.md` section 5) is touched by the "exactly one provider" row; the Caching category (8) is touched only through `Encina.Caching.Memory`, the only cache that registers a lock.
- Not in this plan, with their owner: the rows of #2412 (Anonymization, AntiTampering, Attestation) use this contract and are delivered by #2412; the rows whose ADR-050 source is #2382 but that touch many packages (Transactions across ADO.NET, Dapper and EF Core; Hangfire and Quartz; the three audit stores; `Encina.DistributedLock.InMemory`; `Encina.Aspire.Testing`) are the follow-up issue file `startup-validator-remaining-rows`.

---

## Design Choices

<details>
<summary><strong>1. Contract shape: one declarative requirements contract and one validator, or a validator per package</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Declarative `IEncinaModuleRequirements` per package plus one `EncinaStartupValidator` (the issue's proposal)** | One failure format; every row is data a docs page can list; a new row is one small class | A new public abstraction |
| **B) A hosted check per package (copy `ABACDecisionAuditStartupCheck`)** | No new abstraction | About 30 diverging checks and messages; nothing lists the rows |
| **C) `ValidateOnStart` of options only** | Standard ASP.NET Core | Cannot express a rule across modules or by environment |

### Chosen Option: **A — Declarative contract plus one validator**

### Rationale

- ADR-050 chose this mechanism for every row marked "Startup validation" and the #2382 comment of 2026-10-17 records it as settled; B and C each fail a stated need (one format and a cross-module, environment-dependent rule).
- A requirement is a function over the resolved container, so a module states what it needs without the validator knowing the module.

**Class:** Settled
**Settled by:** ADR-050 section "Enforcement mechanism per row" (Option A, accepted 2026-10-17) and the comment of 2026-10-17 on #2382; options B and C are the issue's own rejected alternatives

</details>

<details>
<summary><strong>2. Who registers the validator: the application, or every module that has requirements</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) The application calls `AddEncinaStartupValidation` (the issue's snippet); modules only register requirements** | Explicit | An application that forgets it gets no check: the guarantee is false by default, which ADR-050 rule 3 forbids |
| **B) Each `AddEncina*` method that has requirements calls `AddEncinaModuleRequirements<T>()`, which registers the requirements with `TryAddEnumerable` and the validator once with `TryAddEnumerable`; `AddEncinaStartupValidation(options)` only configures the options** | No silent default; idempotent; opt-in for modules without requirements | One more public extension method |

### Chosen Option: **B — Modules register the validator through their own requirements**

### Rationale

- ADR-050 rule 3: "Enforcement is by `TryAdd` registration, startup validation or a registration error, never by a silent default". A validator that the application must remember to add is a silent default.
- `AGENTS.md` section 3 (registration completeness): an `AddEncina*` method that adds a service MUST register what it needs, proven by a `ValidateOnBuild` and `ValidateScopes` test; the validator is such a dependency.

**Class:** Settled
**Settled by:** ADR-050 rule 3 and `AGENTS.md` section 3 "Registration completeness"; only B satisfies both

</details>

<details>
<summary><strong>3. How a test-only implementation is marked</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `[TestOnlyImplementation]` attribute on the class** | Not part of the type's public surface; the validator reads the resolved instance's type with no per-module code; works for `internal` types | Reflection per resolved service (once, at startup); a decorator hides the marker (see register row C3) |
| **B) A marker interface `ITestOnlyImplementation`** | Compile-time visible; a type check is cheap | Adds an interface to every in-memory type's public surface; a generic constraint cannot ask for "not test-only" |
| **C) Each requirement lists the in-memory types itself** | No change to the stores | The list rots when a module adds a store; two places to edit |

### Chosen Option: **A — Attribute** (recommended, pending the maintainer)

### Rationale

- A wins when most in-memory types are `internal` or already carry a public contract (the stores of #2412 do): an attribute adds no member to a contract and one rule serves every module. The evaluator learns which service to inspect from the `ServiceType` field of the requirement (Phase 1, register row C8), so no per-module code is needed.
- B wins when the application code itself must branch on "is this test-only" without reflection (for example a diagnostics endpoint); C wins when a module cannot edit the type it requires (a third-party store).
- The marker's name and shape are public API; the attribute is `sealed`, `AttributeUsage(Class, Inherited = false)`.

**Class:** Dilemma

</details>

<details>
<summary><strong>4. What the validator inspects: the resolved implementation or the service descriptors</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) The resolved implementation, from a scope opened at startup** | What the application really gets, including a registration made after `AddEncina*` and the last registration winning; follows ADR-050 literally | Constructs the service (a store opening a connection in its constructor runs at startup); a decorator hides the inner type |
| **B) The `ServiceDescriptor` list** | Constructs nothing | A factory registration hides the implementation type; a descriptor added after the snapshot is unseen |

### Chosen Option: **A — The resolved implementation**

### Rationale

- ADR-050 states "startup validation of the resolved implementation" and "a `TryAdd` default may supply the in-memory implementation in Development, but never counts as satisfying the requirement elsewhere"; B cannot see which registration won.
- The precedent is `ABACDecisionAuditStartupCheck`, which resolves the store from a scope of its own because database stores are scoped (`ABACDecisionAuditStartupCheck.cs:57`).

**Class:** Settled
**Settled by:** ADR-050 "Enforcement mechanism per row" (second row) and `ABACDecisionAuditStartupCheck.cs:57-58`; only A observes the winning registration

</details>

<details>
<summary><strong>5. Which environments are test-only, and how multi-instance is declared</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `EncinaStartupOptions.TestOnlyEnvironments` defaulting to `["Development"]`, a per-entry override carried by the `TestOnlyEnvironments` field of `ModuleRequirement`/`ModuleProhibition` (the Fakes row sets `Development` and `Test`), a `MultiInstanceOnly` flag on the entry for rows that only apply with several instances, `AllowTestOnlyImplementations` as the explicit opt-out, and `Deployment` defaulting to `MultiInstance`** | Matches ADR-050 sub-decisions 2 and 3 and the Fakes row text; the evaluator reads every value from the entry or the options | Four option values and three entry fields |
| **B) A single `IsProduction` boolean** | Simplest | Cannot express "Development or Test" for Fakes, and a custom environment such as `Staging` would count as non-production |

### Chosen Option: **A — Environment set, opt-out flag and deployment default**

### Rationale

- Sub-decision 2: in-memory implementations outside Development are a startup error and the only escape is an explicit, logged opt-out; sub-decision 3: multi-instance is assumed and an application declares single-instance explicitly. The Fakes row says "an environment other than Development or Test".
- An unknown environment (no `IHostEnvironment` registered) is treated as production: the gate denies when context is missing (`AGENTS.md` section 3, fail closed).

**Class:** Settled
**Settled by:** ADR-050 sub-decisions 2 and 3, the "Testing, Aspire testing" row of the closed list, and SPEC-002 DEC-006 (fail closed, the only opt-out is explicit and logged)

</details>

<details>
<summary><strong>6. Two validation providers: registration error or startup error</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) A registration error thrown by each `AddEncina*` validation method when another provider's descriptor is already present; order-independent because whichever runs second throws** | Fails at the earliest point, with the offending call in the stack trace | Needs a shared public helper in core |
| **B) Startup validation only** | One mechanism | The first registered provider has already won silently when the host builds |

### Chosen Option: **A — Registration error, from the helper `EncinaValidationRegistration.EnsureSingleProvider`**

### Rationale

- ADR-050 lists the row as "Registration error for two providers, order-independent"; sub-decision 7 says forbidden combinations are errors, never warnings, and the statically detectable ones fail from an `AddEncina*` method.
- Startup validation stays as the safety net for a registration made outside the helper (the requirement checks that the resolved `IValidationProvider` is the only one).

**Class:** Settled
**Settled by:** ADR-050 closed list row "Validation (3)", "Enforcement mechanism per row" (third row) and sub-decision 7; option B alone leaves the silent win

</details>

<details>
<summary><strong>7. How violations are reported</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) All violations collected, logged once each, and thrown as one `EncinaStartupValidationException` listing module, requirement and ADR** | The operator fixes everything in one run; a typed exception is testable | One new public exception |
| **B) Throw on the first violation with `InvalidOperationException`** | Smallest | One fix per restart |

### Chosen Option: **A — Collect all, one typed exception**

### Rationale

- The issue asks for a message "naming the module, the requirement and the ADR" and "collects every violation and reports them together"; the same shape exists in `CryptoShreddingConfigurationException` (`src/Encina.Marten.GDPR/CryptoShreddingConfigurationException.cs`), which lists every issue at once.
- The exception and the logs carry module and requirement names, the ADR id and the fix text only; no `EncinaError.Message` and no resolved value (`AGENTS.md` section 3).

**Class:** Trivial
**Settled by:** `CryptoShreddingStartupValidationHostedService.cs` (all issues reported once, event 8459, one exception, event 8470) and the issue text

</details>

<details>
<summary><strong>8. When the validator runs, and whether a requirement can only warn</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `IHostedLifecycleService.StartingAsync`, so it runs before any hosted service starts; requirements carry a severity (`Error` throws, `Warning` is logged)** | No processor starts before the check; keeps the two existing warnings (ABAC best-effort) without making them errors | Two severities |
| **B) `IHostedService.StartAsync`, errors only** | Matches the issue's wording | A processor registered earlier can start working before the check throws |

### Chosen Option: **A — `StartingAsync`, with `Error` and `Warning` severities**

### Rationale

- `CryptoShreddingStartupValidationHostedService.cs:56-59` documents why: `StartingAsync` runs before every `StartAsync`, so a misconfiguration stops the host before a daemon processes work. `IHostedLifecycleService` is an `IHostedService`, so ADR-050's "an `IHostedService` that throws" still holds.
- ADR-050 sub-decision 7 makes forbidden combinations errors; the warnings that exist today (`ABACDecisionAuditStartupCheck`: `BestEffort`) are not requirements and keep a place without becoming errors.

**Class:** Trivial
**Settled by:** `CryptoShreddingStartupValidationHostedService.cs:56-59` and ABAC's warning 9084 (`ABACDecisionAuditStartupCheck.cs:72-75`)

</details>

<details>
<summary><strong>9. Health check and telemetry for the validation outcome</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Defer both to a follow-up issue (a startup span, the `encina.startup.requirement_violations` counter and a health entry)** | An error stops the host, so the only live outcomes are warnings and opt-outs; no new meter in core before the ADR-054 meter renames land | The issue lists the health and telemetry items |
| **B) Add them now** | Matches the issue text | A meter and a span that an aborted host cannot export; `IEncinaHealthCheck` lives in `Encina.Messaging`, not in core, so a core health check needs an abstraction move |

### Chosen Option: **A — Defer** (recommended, pending the maintainer)

### Rationale

- A wins while violations abort the host (the exported signal would be the success path only); B wins if the maintainer wants the opt-out and warning counts on a dashboard from the first release, in which case core takes `Meter` `Encina.Startup`, instrument `encina.startup.requirement_violations` (no `_total` suffix, ADR-054 settled point 4) and tag `module`; no tenant tag is emitted because the validator runs before any tenant exists (ADR-054 D4).
- The follow-up issue file carries the full scope.

**Class:** Dilemma

</details>

<details>
<summary><strong>10. Build order of ADR-050 sub-decision 8</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Start S1 and S2 now and treat #2184 (stages) and #1251 (row claiming) as not blocking, because they precede the rows that depend on them, not the mechanism** | The mechanism and the rows of this plan do not use stages or claiming; work starts now | Reads ADR-050 sub-decision 8 ("#2184 and #1251 first, then the requirements validator #2382") more loosely than its words |
| **B) List #2184 and #1251 as prerequisites and open S1 only after both close** | Follows the ADR literally | Both are open; the whole plan waits for work it does not use |

### Chosen Option: **A — Mechanism now, ordering as an ADR reading** (recommended, pending the maintainer)

### Rationale

- A wins because no row of this plan needs a stage contract or row claiming; B wins if the maintainer wants the ADR order kept to the letter. Both issues are open on 2026-11-19.

**Class:** Dilemma

</details>

### Register of defaults and semantics

| Id | Choice | Class | Basis |
|----|--------|-------|-------|
| C1 | No new `EncinaError` code is created: a violation is a startup exception, as in the four existing checks, so there is no ADR-062 `ErrorCategory` to assign | Settled | ADR-062 contract (a category is chosen where an `EncinaError` is created; none is created); the four checks cited in Summary |
| C2 | A requirement that throws or whose predicate throws is reported as a violation of its module (`EventId` 181), never skipped; an `OperationCanceledException` from the stopping token propagates | Settled | `AGENTS.md` section 3 (gates fail closed; cancellation is not an error to swallow, ADR-052) |
| C3 | A decorator around a test-only store hides the marker, and the validator does not unwrap decorators, so a decorated in-memory store is not detected; the XML docs and the `docs/features` page say so, as the remarks of `ABACDecisionAuditStartupCheck` already do | Trivial | `ABACDecisionAuditStartupCheck.cs:25-26` (the same documented limitation) |
| C4 | The validator and every requirement take `IServiceScopeFactory`/`IServiceProvider` at start, never a scoped service in a singleton constructor | Settled | `AGENTS.md` section 3 "Registration completeness" |
| C5 | A tenant-qualified id is never produced: the validator's keys are module names and requirement ids (strings of the code), so no tenant/key pair maps to one id and the reserved `default` tenant segment does not apply | Settled | the validator has no tenant input; ADR-054 D4 (no tenant tag when nothing tenant-scoped is emitted) |
| C6 | Public names: `IEncinaModuleRequirements`, `ModuleRequirement`, `ModuleProhibition`, `StartupViolation`, `StartupSeverity`, `EncinaStartupOptions`, `EncinaDeployment`, `EncinaStartupValidationException`, `TestOnlyImplementationAttribute`, `AddEncinaStartupValidation`, `AddEncinaModuleRequirements<T>` in namespace `Encina.Startup` | Dilemma (recommended, pending the maintainer) | public surface; alternatives are the namespace `Encina` or a shorter `Requirement` family; recorded here because every later row depends on the names |
| C7 | The `Module` string of a requirements class is the name of the ADR-050 row ("Distributed locks (5)" is `DistributedLocks`) so the docs table and the failure message use the same word | Trivial | ADR-050 closed list column "Module" |
| C8 | The evaluator reads every rule from the entry, never from a module: `ServiceType` (the service whose resolved implementation is checked for `[TestOnlyImplementation]`), `MultiInstanceOnly` (skipped when `Deployment` is `SingleInstance`) and `TestOnlyEnvironments` (the entry's own environment set, overriding the options default). All three are optional parameters after the required ones | Trivial | follows from Design Choices 3 and 5; without these fields the evaluator would see only opaque predicates |

---

## Implementation Phases

### Phase 1: Core Model and Evaluator (pull request S1)

> **Goal**: The public contract and the pure evaluation logic, with no host and no DI.

<details>
<summary><strong>Tasks</strong></summary>

1. **Create** in `src/Encina/Startup/`: `IEncinaModuleRequirements.cs` (`string Module { get; }`, `string Adr { get; }`, `IReadOnlyCollection<ModuleRequirement> Requires { get; }`, `IReadOnlyCollection<ModuleProhibition> Forbids { get; }`); `ModuleRequirement.cs` (`sealed record ModuleRequirement(string Id, string Description, string Fix, Func<IServiceProvider, bool> IsSatisfied, Func<IServiceProvider, bool>? AppliesWhen = null, StartupSeverity Severity = StartupSeverity.Error, Type? ServiceType = null, bool MultiInstanceOnly = false, IReadOnlyCollection<string>? TestOnlyEnvironments = null)`; optional parameters after required ones, CS1737; the last three are the fields the evaluator rules below read, register row C8); `ModuleProhibition.cs` (same shape, true means violated); `StartupViolation.cs` (`sealed record StartupViolation(string Module, string Id, string Description, string Fix, string Adr, StartupSeverity Severity)`) with `StartupSeverity` in the same file; `EncinaStartupOptions.cs` (also declares the `EncinaDeployment` enum: `EncinaDeployment Deployment = MultiInstance`, `bool AllowTestOnlyImplementations`, `IReadOnlyCollection<string> TestOnlyEnvironments` default `["Development"]`); `TestOnlyImplementationAttribute.cs`; `StartupRequirementEvaluator.cs` (internal static: `Evaluate(IEnumerable<IEncinaModuleRequirements>, IServiceProvider, EncinaStartupOptions, string? environmentName)` returns `IReadOnlyList<StartupViolation>`).
2. The evaluator applies, in order, to every requirement and prohibition: its `AppliesWhen`; `MultiInstanceOnly` (an entry flagged true is skipped when `Deployment` is `SingleInstance`); the effective environment set (the entry's `TestOnlyEnvironments` when not null, else the options value); the test-only rule (when `ServiceType` is not null, the service is resolved and, if the resolved implementation's type carries the attribute, that is a violation unless the environment is in the effective set or `AllowTestOnlyImplementations` is set; an unknown environment, `null`, denies); the predicate (`IsSatisfied`, or `IsViolated` for a prohibition without `ServiceType`; an entry without `ServiceType` and with `TestOnlyEnvironments` is evaluated only outside the effective set); a predicate or resolution that throws becomes a violation (register row C2).
3. **Modify** `src/Encina/PublicAPI.Unshipped.txt` and `.github/coverage-manifest/Encina.json`, and add `changelog.d/2382-startup-requirements-model.added.md`, through `mechanical-fixer` (targets in Research).
4. Tests (new files): `tests/Encina.UnitTests/Startup/StartupRequirementEvaluatorTests.cs` (each field of C8: a `ServiceType` whose implementation carries the attribute is a violation outside the environment set and inside it when the opt-out is set; `MultiInstanceOnly` skipped for `SingleInstance`; an entry-level environment set overrides the options one); `tests/Encina.GuardTests/Startup/StartupGuardTests.cs` (null module, empty id, null predicate, null options collections); `tests/Encina.PropertyTests/Startup/StartupEvaluatorProperties.cs` (FsCheck: the violation set does not depend on the order of the requirements classes or of the requirements inside one); `tests/Encina.ContractTests/Startup/ModuleRequirementsContractTests.cs` (moved here from S2 so the S1 contract targets are met by S1: a sample implementation keeps the contract, non-empty module and ADR id, unique requirement ids, the records keep their members including the three fields of C8).

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 1</strong></summary>

```text
CONTEXT:
Encina (.NET 10, C# 14, pre-1.0). Issue #2382, plan docs/plans/startup-requirements-validator-implementation-plan-2382.md,
phase 1. ADR-050 (docs/architecture/adr/050-modules-may-require-modules.md) defines the closed list of module
requirements; this phase adds only the model and the pure evaluator in the Encina core package.

TASK:
1. Create the types listed in the plan under src/Encina/Startup/ (namespace Encina.Startup), with XML docs and examples.
2. StartupRequirementEvaluator.Evaluate returns every violation, never throws for a predicate failure (a failing
   predicate is a violation), applies AppliesWhen, Deployment and the test-only rule with an unknown environment denied.
   ModuleRequirement and ModuleProhibition carry ServiceType, MultiInstanceOnly and TestOnlyEnvironments (optional, last);
   the evaluator reads them: no rule may depend on anything the record does not hold.
3. Add PublicAPI entries, the per-file coverage targets and the changelog fragment through mechanical-fixer.
4. Write unit, guard, property and contract tests that execute the real evaluator and records.

KEY RULES:
- Optional parameters after required ones. No EncinaError code is created. No logging in this phase.
- Fail closed: missing environment denies. No scoped service in a singleton.
- Edit source with the Edit/Write tools only; PublicAPI and manifest through mechanical-fixer.

REFERENCE FILES:
- docs/architecture/adr/050-modules-may-require-modules.md
- src/Encina.Security.ABAC/DecisionAudit/ABACDecisionAuditStartupCheck.cs
- src/Encina.Marten.GDPR/CryptoShreddingConfigurationException.cs
```

</details>

---

### Phase 2: Hosted Validator, Registration and Logging (pull request S2)

> **Goal**: The host fails before any hosted service starts when a requirement is violated.

<details>
<summary><strong>Tasks</strong></summary>

1. **Create** `src/Encina/Startup/EncinaStartupValidator.cs` (`internal sealed class : IHostedLifecycleService`; constructor `(IEnumerable<IEncinaModuleRequirements> requirements, IServiceScopeFactory scopeFactory, IOptions<EncinaStartupOptions> options, IServiceProvider environmentLookup, ILogger<EncinaStartupValidator> logger)`; `StartingAsync` opens one scope, reads `IHostEnvironment` with `GetService` (absent means unknown environment), calls the evaluator, logs and throws).
2. **Create** `src/Encina/Startup/EncinaStartupValidationException.cs` (`public sealed`, `IReadOnlyList<StartupViolation> Violations`; the message lists `Module`, `Id`, `Fix` and `Adr` of each violation, nothing resolved from the container).
3. **Create** `src/Encina/Startup/StartupValidationLog.cs` (`[LoggerMessage]` with EventIds 177-183 of `EventIdRanges.Core`; XML text "Event IDs: 177-183 (see EventIdRanges.Core)"): 177 passed (Debug), 178 requirement violated (Error), 179 test-only implementation allowed by the opt-out (Warning, one per implementation), 180 environment unresolved, denied (Error), 181 a requirement threw (Error, exception type through `ForLogging()`), 182 single-instance declared (Information), 183 requirement warning (Warning).
4. **Create** `src/Encina/Startup/EncinaStartupServiceCollectionExtensions.cs`: `AddEncinaStartupValidation(this IServiceCollection, Action<EncinaStartupOptions>? configure = null)` and `AddEncinaModuleRequirements<TRequirements>(this IServiceCollection)` (registers the requirements with `TryAddEnumerable`, the validator with `TryAddEnumerable<IHostedService, EncinaStartupValidator>`, and the options with `AddOptions`; both idempotent).
5. **Modify** `PublicAPI.Unshipped.txt` (the new public types), `.github/coverage-manifest/Encina.json` (targets in Research) and add `changelog.d/2382-startup-requirements-validator.added.md`, all through `mechanical-fixer`.
6. Tests: `tests/Encina.UnitTests/Startup/EncinaStartupValidatorTests.cs` (a real `HostBuilder`: a violation stops `StartAsync`, a warning does not, the opt-out starts the host and logs 179, no `IHostEnvironment` denies, the validator runs before a hosted service registered earlier); `tests/Encina.UnitTests/Startup/EncinaStartupRegistrationTests.cs` (builds the provider with `ValidateOnBuild` and `ValidateScopes` and resolves the validator and the options; calling `AddEncinaModuleRequirements` twice registers one validator); `tests/Encina.GuardTests/Startup/EncinaStartupRegistrationGuardTests.cs` (null `services` for `AddEncinaStartupValidation` and `AddEncinaModuleRequirements<T>`; the validator constructor with a null requirements collection, scope factory, options, environment lookup and logger; the `EncinaStartupValidationException` constructor with a null violations list; a null `configure` is valid and gets a test of the valid result). The contract test of the requirements records is in S1. Justification files for the non-database test types: `tests/Encina.IntegrationTests/Startup/EncinaStartupValidator.md`, `tests/Encina.LoadTests/Startup/EncinaStartupValidator.md`, `tests/Encina.BenchmarkTests/Startup/EncinaStartupValidator.md` (startup-only, no database, no concurrency).

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 2</strong></summary>

```text
CONTEXT:
Issue #2382 plan docs/plans/startup-requirements-validator-implementation-plan-2382.md, phase 2. Phase 1 already
merged: IEncinaModuleRequirements, ModuleRequirement, StartupRequirementEvaluator in src/Encina/Startup/.

TASK:
1. Create EncinaStartupValidator (IHostedLifecycleService, StartingAsync), EncinaStartupValidationException,
   StartupValidationLog (EventIds 177-183 of EventIdRanges.Core, no gaps) and the two registration methods.
2. The validator resolves everything from a scope it opens, reports every violation (log 178, then one exception),
   logs warnings (183) without throwing, logs the opt-out per implementation (179), denies an unknown environment (180).
3. Tests with a real HostBuilder, a DI test with ValidateOnBuild and ValidateScopes, a contract test, and the three
   justification .md files.

KEY RULES:
- Never log EncinaError.Message or an exception message: use ForLogging() and codes.
- Idempotent registration; the application's own options registration wins.
- PublicAPI, the coverage manifest and the changelog fragment go through mechanical-fixer.
- Cancellation from the stopping token propagates; every other failure of a requirement is a violation.

REFERENCE FILES:
- src/Encina.Marten.GDPR/CryptoShreddingStartupValidationHostedService.cs
- src/Encina/Diagnostics/EventIdRanges.cs, src/Encina/Diagnostics/RedactedException.cs
- src/Encina.Security.ABAC/DecisionAudit/ABACDecisionAuditStartupCheck.cs
```

</details>

---

### Phase 3: Migrate the Existing Checks (pull requests M1, M2, M3)

> **Goal**: One failure format for ABAC, Recoverability delayed retries and the Marten.GDPR key store.

<details>
<summary><strong>Tasks</strong></summary>

1. **M1, `Encina.Security.ABAC`**: create `src/Encina.Security.ABAC/DecisionAudit/ABACModuleRequirements.cs` (requirement "an `IOperationAuditStore` is registered" applying when `ABACOptions.DecisionAudit.Enabled`; prohibition "the resolved store is `InMemoryOperationAuditStore`" (a direct type test, as the check does today at `ABACDecisionAuditStartupCheck.cs:67`; the generic marker for the audit stores belongs to the remaining-rows issue, so this pull request touches no second package); warning "`FailureMode` is `BestEffort`"); delete `ABACDecisionAuditStartupCheck.cs`; remove the three log methods 9084, 9086 and 9087 from `ABACLogMessages.cs` (their ids stay unassigned); edit `ServiceCollectionExtensions.cs` to call `AddEncinaModuleRequirements`. Tests: replace `ABACDecisionAuditStartupCheckTests.cs` and `ABACDecisionAuditStartupCheckGuardTests.cs` with `ABACModuleRequirementsTests.cs`, edit `ABACDecisionAuditServiceGraphTests.cs`. Documentation through `docs-writer` (these pages name the removed check and ids): `docs/features/abac/reference/decision-audit.md` (the startup-check table, lines 190-197), `docs/features/abac/reference/observability.md` (the 9084, 9086 and 9087 rows), `docs/features/abac/reference/configuration.md` (lines 47 and 78) and `src/Encina.Security.ABAC/README.md` (line 210).
2. **M2, `Encina.Messaging`**: create `src/Encina.Messaging/Recoverability/RecoverabilityModuleRequirements.cs` (store and `IDelayedRetryMessageFactory` present when `EnableDelayedRetries`; `IEncina` present for the processor); delete `DelayedRetryStartupCheck.cs` and its log class (5457), keep the registration-time `DescribeMissing` call (a statically detectable case, `RecoverabilityServiceCollectionExtensions.cs:141-147`) by moving the helper into the requirements class; edit `RecoverabilityServiceCollectionExtensions.cs:113,122`. Tests edited: `DelayedRetryRegistrationTests.cs`, `MessagingServiceCollectionExtensionsRecoverabilityTests.cs`, `MessagingServiceCollectionExtensionsCoreEquivalenceTests.cs`, `ServiceCollectionExtensionsRecoverabilityRoutersTests.cs` (the EF Core one). Documentation through `docs-writer`: `src/Encina.EntityFrameworkCore/README.md` (line 427 names `DelayedRetryStartupCheck`).
3. **M3, `Encina.Marten.GDPR`**: create `CryptoShreddingModuleRequirements.cs` (prohibition: the resolved `ISubjectKeyProvider` is `InMemorySubjectKeyProvider` outside the test-only environments, marked with the attribute); remove `WarnInMemoryKeyStore` and its log method from the hosted service (the serializer, contract-modifier and `[CryptoShredded]` classification stay there: they validate the configuration of the module, not a dependency); edit `ServiceCollectionExtensions.cs`. Tests: `CryptoShreddingStartupValidationHostedServiceTests.cs` and `StartupValidationHost.cs` edited, one new requirements test. The in-memory key store changes from a warning to a startup error outside Development (ADR-050 row "Crypto-shredding", #1191).
4. Each pull request edits its package manifest and adds a changelog fragment (`changelog.d/2382-<slug>.changed.md`) through `mechanical-fixer`; none changes a `PublicAPI` file because the deleted types are `internal`. `docs/architecture/module-dependency-matrix.md` (lines 58 and 69 name the interim checks) and the ADR-050 text that names them are updated once, in D1.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 3</strong></summary>

```text
CONTEXT:
Issue #2382 plan docs/plans/startup-requirements-validator-implementation-plan-2382.md, phase 3 (one pull request per
package: ABAC, Messaging, Marten.GDPR). Phases 1 and 2 are merged: AddEncinaModuleRequirements<T>() exists.

TASK:
1. Replace the package's startup check by an IEncinaModuleRequirements class with the same conditions.
2. Keep behavior: ABAC without a store and an in-memory store fail; BestEffort stays a warning; delayed retries without a
   store or factory fail; the Marten.GDPR in-memory key provider outside Development fails.
3. Move the tests to the new class; delete the old hosted checks and their log methods without reassigning EventIds.

KEY RULES:
- One package per pull request, at most 15 files. Never swallow an error; a failing requirement is a violation.
- Mark in-memory implementations with [TestOnlyImplementation]; never rely on a scoped service in a singleton.
- Manifest and changelog through mechanical-fixer.

REFERENCE FILES:
- src/Encina.Security.ABAC/DecisionAudit/ABACDecisionAuditStartupCheck.cs
- src/Encina.Messaging/Recoverability/DelayedRetryStartupCheck.cs, RecoverabilityServiceCollectionExtensions.cs
- src/Encina.Marten.GDPR/CryptoShreddingStartupValidationHostedService.cs (WarnInMemoryKeyStore)
```

</details>

---

### Phase 4: Rows of the Closed List (pull requests V1, then V2 and V3, L1, T1)

> **Goal**: The rows whose source is #2382 and that fit one package.

<details>
<summary><strong>Tasks</strong></summary>

1. **V1, V2, V3 (Validation)**: V1 creates `src/Encina/Validation/EncinaValidationRegistration.cs` (public static `EnsureSingleProvider(this IServiceCollection services, Type providerType)`: throws `InvalidOperationException` naming both providers and ADR-050 when an `IValidationProvider` descriptor of another implementation type exists; idempotent for the same type; it also registers the internal `ValidationModuleRequirements`, created in the same pull request in `src/Encina/Validation/`, through `AddEncinaModuleRequirements`: the safety net with two requirements, "exactly one `IValidationProvider` is resolved" and, as ADR-050's Validation row requires, "an `IValidationProvider` is registered when validators exist", where validators exist means an `IValidationProvider` or `ValidationOrchestrator` descriptor is present or `ValidationPipelineBehavior<,>` is registered, so a pipeline with no provider fails startup) and calls it from `AddEncinaFluentValidation` (both overloads, `src/Encina.FluentValidation/ServiceCollectionExtensions.cs:62,104`); V2 and V3 call it from `AddDataAnnotationsValidation` (`src/Encina.DataAnnotations/ServiceCollectionExtensions.cs:67`, `TryAdd` at :72) and `AddMiniValidation` (`src/Encina.MiniValidator/ServiceCollectionExtensions.cs:60`, `TryAdd` at :65). **Ordering**: V2 and V3 open only after V1 merges, because the helper is created in V1. Tests per package: registration-order tests in both orders, a DI test, guard tests for the helper (V1), and a startup test with a pipeline behavior registered and no provider (V1).
2. **L1 (Locks)**: `src/Encina.Caching.Memory/MemoryCacheModuleRequirements.cs`: one requirement with `ServiceType = typeof(IDistributedLockProvider)` and `MultiInstanceOnly = true`; the resolved `MemoryDistributedLockProvider` is marked `[TestOnlyImplementation]`, so it is a violation when `Deployment` is `MultiInstance` and the environment is not in the effective set; an application with one instance declares `Deployment = SingleInstance`. Edit `ServiceCollectionExtensions.cs:67` area and the README of the package through `docs-writer`. The package has no `InternalsVisibleTo` today, so L1 also edits `src/Encina.Caching.Memory/Encina.Caching.Memory.csproj` to add `InternalsVisibleTo` for `Encina.UnitTests` (counted below), because the requirements class is `internal` and tested in `Encina.UnitTests`.
3. **T1 (Testing)**: `src/Encina.Testing.Fakes/FakesModuleRequirements.cs`: a prohibition "`AddEncinaFakes` was called" (the registration records a marker service) with `TestOnlyEnvironments = ["Development", "Test"]`, so it is evaluated, and violated, only outside those two environments (register row C8). The package has no `InternalsVisibleTo` either: T1 edits `src/Encina.Testing.Fakes/Encina.Testing.Fakes.csproj` to add it for `Encina.UnitTests` (counted below).
4. Each pull request adds its manifest entry and a `changelog.d/2382-<slug>.added.md` fragment through `mechanical-fixer`; `PublicAPI.Unshipped.txt` lines only where a public type is added (V1 in core; L1 and T1 requirements classes are `internal`).

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 4</strong></summary>

```text
CONTEXT:
Issue #2382 plan docs/plans/startup-requirements-validator-implementation-plan-2382.md, phase 4: the ADR-050 rows with
source #2382 that fit one package: Validation (3 providers, exactly one), Distributed locks (in-memory lock of
AddEncinaMemoryCache), Testing (Encina.Testing.Fakes outside Development or Test).

TASK:
1. V1: add EncinaValidationRegistration.EnsureSingleProvider in core and call it from AddEncinaFluentValidation;
   V2 and V3 (after V1) call it from AddDataAnnotationsValidation and AddMiniValidation. Whichever call runs second throws.
2. L1 and T1: add one internal IEncinaModuleRequirements class each and register it with AddEncinaModuleRequirements.
3. Tests: both registration orders, DI with ValidateOnBuild and ValidateScopes, a startup test with a real host.

KEY RULES:
- Validation providers stay consistent across all 3 packages (AGENTS.md section 5).
- A single-instance application declares it through EncinaStartupOptions.Deployment; never a silent default.
- PublicAPI, manifests and changelog fragments through mechanical-fixer; the package README through docs-writer.

REFERENCE FILES:
- src/Encina.FluentValidation/ServiceCollectionExtensions.cs, src/Encina.DataAnnotations/ServiceCollectionExtensions.cs,
  src/Encina.MiniValidator/ServiceCollectionExtensions.cs
- src/Encina.Caching.Memory/ServiceCollectionExtensions.cs, src/Encina.Testing.Fakes/ServiceCollectionExtensions.cs
```

</details>

---

### Phase 5: Documentation and Finalization (pull request D1)

> **Goal**: The page that lists the rows, generated from what is registered, and the ADR-050 status note.

<details>
<summary><strong>Tasks</strong></summary>

1. `docs/features/startup-requirements.md` through `docs-writer`: how to declare single-instance, how to opt out (and that the opt-out is logged), how a module adds requirements, the decorator limitation (register row C3).
2. `docs/architecture/adr/050-modules-may-require-modules.md` through `docs-writer`: a dated amendment note naming the rows that are enforced by this validator and linking the follow-up issue.
3. `docs/architecture/module-dependency-matrix.md` through `docs-writer`: the Delayed retries and ABAC rows (lines 58 and 69) name interim checks that M1 and M2 removed.
4. `ROADMAP.md` is not edited (no milestone changes); `docs/INVENTORY.md` is edited only if it lists core namespaces (checked when the pull request opens).

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 5</strong></summary>

```text
CONTEXT:
Issue #2382 plan docs/plans/startup-requirements-validator-implementation-plan-2382.md, phase 5, documentation only.

TASK:
1. Write docs/features/startup-requirements.md (one Diataxis quadrant: how-to) and the ADR-050 amendment note.
2. Verify every identifier exists in src/ and every link resolves.

KEY RULES:
- Documentation never types coverage figures by hand. Use the encina-docs skill; no emojis.

REFERENCE FILES:
- docs/architecture/adr/050-modules-may-require-modules.md, docs/architecture/module-dependency-matrix.md
```

</details>

---

## Research

### Relevant Standards & Specifications

| Source | Provision | Use in this feature |
|--------|-----------|---------------------|
| ADR-050 | Closed list per module; sub-decisions 2 (in-memory outside Development is an error, explicit logged opt-out), 3 (multi-instance assumed), 7 (forbidden combinations are errors) | The rows and defaults of the validator |
| SPEC-002 DEC-006 | Gates fail closed; the only opt-out is explicit and logged | Unknown environment denies; opt-out logged (179) |
| GDPR art. 25 and art. 32 | Protection by default; in-memory key and nonce stores lose data on restart | Why the safe state is the default (ADR-050 forcing reasons) |
| ADR-054 | Settled point 4 (no `_total`), D3 (one meter and source per package), D4 (tenant tag only when multi-tenancy is on) | Deferred telemetry shape (Design Choice 9) |
| ADR-062 | `ErrorCategory` contract | No `EncinaError` is created (register row C1) |
| ADR-021 | EventId ranges registered before use | Core ids 177-183, inside the existing `Core` range |

### Existing Encina Infrastructure to Leverage

| Component | Location | Usage in this feature |
|-----------|----------|-----------------------|
| `ABACDecisionAuditStartupCheck` | `src/Encina.Security.ABAC/DecisionAudit/ABACDecisionAuditStartupCheck.cs` | The model for scope-per-check resolution; migrated in M1 |
| `DelayedRetryStartupCheck` | `src/Encina.Messaging/Recoverability/DelayedRetryStartupCheck.cs` | Migrated in M2; its `DescribeMissing` stays at registration time |
| `CryptoShreddingStartupValidationHostedService` | `src/Encina.Marten.GDPR/` | `StartingAsync` precedent and the multi-issue exception; key store warning migrated in M3 |
| `CryptoShreddingConfigurationException` | `src/Encina.Marten.GDPR/CryptoShreddingConfigurationException.cs` | Shape of the typed exception |
| `RedactedException.ForLogging` | `src/Encina/Diagnostics/RedactedException.cs:150` | Logging a thrown requirement without its message |
| `EventIdRanges.Core` | `src/Encina/Diagnostics/EventIdRanges.cs` | Ids 177-183 |
| `IHostEnvironment`, `IHostedLifecycleService` | `Microsoft.Extensions.Hosting.Abstractions`, referenced by `src/Encina/Encina.csproj` | Environment and start ordering |
| `ValidationOrchestrator`, `IValidationProvider` | `src/Encina/Validation/` | The row "exactly one provider" |

### Event ID Allocation

| Package | Range | Notes |
|---------|-------|-------|
| `Encina` (core) | 177-183 (inside `EventIdRanges.Core` 100-199) | 177 passed, 178 violated, 179 test-only allowed by opt-out, 180 environment unresolved, 181 requirement threw, 182 single-instance declared, 183 warning; no new range is registered, and no id of the block 7950-7999 is used. Ids 184-199 stay free for other plans |
| `Encina.Security.ABAC` | none new | 9084, 9086 and 9087 are retired by M1, never reassigned |
| `Encina.Messaging` | none new | 5457 is retired by M2 |

### Estimated File Count

Every count includes tests, the coverage manifest, PublicAPI, changelog fragment and justification files; each pull request builds and passes alone.

| PR | Content | New src | Changed src | New tests | Edited tests | Manifest | Other | Files | Lines (est.) |
|----|---------|---------|-------------|-----------|--------------|----------|-------|-------|--------------|
| S1 | Core model and evaluator | 7 | 1 (PublicAPI) | 4 (unit, guard, property, contract) | 0 | 1 | 1 (changelog) | 14 | ~1,000 |
| S2 | Validator, registration, log | 4 | 1 (PublicAPI) | 3 (2 unit, 1 guard) | 0 | 1 | 4 (changelog, 3 `.md` justifications) | 13 | ~850 |
| M1 | ABAC migration | 1 | 3 (`ServiceCollectionExtensions.cs`, `ABACLogMessages.cs` and the deleted `ABACDecisionAuditStartupCheck.cs`) | 1 | 3 (2 replaced, 1 edited) | 1 | 5 (changelog, `decision-audit.md`, `observability.md`, `configuration.md`, ABAC README) | 14 | ~600 |
| M2 | Messaging migration | 1 | 2 (the extensions class and the deleted `DelayedRetryStartupCheck.cs`) | 1 | 4 | 1 | 2 (changelog, `Encina.EntityFrameworkCore/README.md`) | 11 | ~480 |
| M3 | Marten.GDPR migration | 1 | 4 (hosted service, log class, extensions, `InMemorySubjectKeyProvider.cs` attribute) | 1 | 2 | 1 | 1 | 10 | ~400 |
| V1 | Validation helper and FluentValidation | 2 | 2 (`ServiceCollectionExtensions.cs`, core PublicAPI) | 3 | 0 | 2 | 1 | 10 | ~450 |
| V2 | DataAnnotations | 0 | 1 | 2 | 0 | 1 | 1 | 5 | ~200 |
| V3 | MiniValidator | 0 | 1 | 2 | 0 | 1 | 1 | 5 | ~200 |
| L1 | Caching.Memory lock row | 1 | 4 (extensions class, attribute on `MemoryDistributedLockProvider.cs`, README, `Encina.Caching.Memory.csproj` for `InternalsVisibleTo`) | 2 | 0 | 1 | 1 | 9 | ~320 |
| T1 | Testing.Fakes row | 1 | 2 (extensions class, `Encina.Testing.Fakes.csproj` for `InternalsVisibleTo`) | 2 | 0 | 1 | 1 | 7 | ~270 |
| D1 | Documentation | 0 | 0 | 0 | 0 | 0 | 3 (feature page, ADR note, `module-dependency-matrix.md`) | 3 | ~250 |
| **Total** | | **18** | **21** | **21** | **9** | **11** | **21** | **101** | **~5,000** |

The 11 rows sum to the total (14 + 13 + 14 + 11 + 10 + 10 + 5 + 5 + 9 + 7 + 3 = 101 files). Every pull request is at most 15 files and 1,500 lines (the largest is S1 at about 1,000). The S1 pull request adds public types with no consumer in the same pull request; they are executed by its own unit, guard, property and contract tests, so the crap-gate and the per-flag targets are met by S1 alone.

Per-file coverage targets (per flag, one sentence each), written to `.github/coverage-manifest/Encina.json` by the pull request that adds the file:

| File | Unit | Guard | Contract | Property | Justification |
|------|------|-------|----------|----------|---------------|
| `Startup/StartupRequirementEvaluator.cs` | 95 | 70 | 0 | 80 | Every branch (applies-when, deployment, environment, opt-out, throwing predicate) is reachable with plain delegates; guard reaches the argument checks only; order independence is a property; no interface contract applies to a static class |
| `Startup/EncinaStartupValidator.cs` | 90 | 85 | 0 | 0 | A real `HostBuilder` drives start, violation, warning, opt-out and missing-environment paths; guard reaches the constructor checks |
| `Startup/EncinaStartupServiceCollectionExtensions.cs` | 90 | 90 | 0 | 0 | The DI test and null checks reach every line; no contract or property applies to registration |
| `Startup/ModuleRequirement.cs`, `ModuleProhibition.cs`, `StartupViolation.cs`, `EncinaStartupOptions.cs` | 85 | 80 | 80 | 0 | Records and options with validated members; the contract test executes the real records |
| `Startup/EncinaStartupValidationException.cs`, `StartupValidationLog.cs`, `TestOnlyImplementationAttribute.cs` | 80 | 0 | 0 | 0 | Exception and generated log code are executed by the validator tests; no parameters worth a guard test (attribute has none, the exception takes a validated list covered by the S2 unit test) |
| Requirement classes of M1-M3, V1 (`ValidationModuleRequirements`, `EncinaValidationRegistration`), L1 (`MemoryCacheModuleRequirements`) and T1 (the Fakes requirements class) | 90 | 70 | 0 | 0 | Each is a list of entries evaluated by the S1 evaluator; the migration, registration-order and startup tests execute every entry in both pass and fail directions; guard reaches the public `EnsureSingleProvider` argument checks only (V1), the other classes are `internal` with no validated parameters; no contract or property applies to a fixed list of rows |

---

## Combined AI Agent Prompts

<details>
<summary><strong>Full combined prompt for all phases</strong></summary>

```text
You are implementing issue #2382 - the startup requirements validator of ADR-050 - in pull requests of at most 15 files.

PROJECT CONTEXT:
- Encina is a .NET 10 / C# 14 library; pre-1.0, no backward compatibility, best solution always; PowerShell and C# scripts
  only; edit source only with the Edit/Write tools.
- ADR-050 is the closed list of what each module requires. Gates fail closed (SPEC-002 DEC-006): a missing environment
  denies; the only opt-out is explicit and logged.

IMPLEMENTATION OVERVIEW:
1. S1: Encina.Startup model (IEncinaModuleRequirements, ModuleRequirement, ModuleProhibition, StartupViolation, options,
   TestOnlyImplementationAttribute) and the pure StartupRequirementEvaluator.
2. S2: EncinaStartupValidator (IHostedLifecycleService, StartingAsync), EncinaStartupValidationException, log events
   177-183 of EventIdRanges.Core, AddEncinaStartupValidation and AddEncinaModuleRequirements<T> (idempotent TryAdd).
3. M1-M3: migrate the ABAC, delayed-retry and Marten.GDPR key-store checks; V1-V3: one validation provider only (a
   registration error, order independent); L1: in-memory lock of AddEncinaMemoryCache; T1: Encina.Testing.Fakes.
4. D1: the docs page and the ADR-050 amendment note.

KEY PATTERNS:
- Resolve implementations from a scope opened at startup; never inject a scoped service into a singleton.
- Never log EncinaError.Message or an exception message; ForLogging() and codes only.
- One package per pull request; manifest, PublicAPI and changelog through mechanical-fixer; documentation through docs-writer.
- Guard tests for every new public constructor and method with validated parameters; a DI test with ValidateOnBuild and
  ValidateScopes for each registration method.

REFERENCE FILES:
- docs/architecture/adr/050-modules-may-require-modules.md
- src/Encina.Security.ABAC/DecisionAudit/ABACDecisionAuditStartupCheck.cs
- src/Encina.Marten.GDPR/CryptoShreddingStartupValidationHostedService.cs
- docs/plans/startup-requirements-validator-implementation-plan-2382.md
```

</details>

---

## Cross-Cutting Integration Matrix

| # | Function | Status | Notes |
|---|----------|--------|-------|
| 1 | Caching | ❌ N/A | No reads; the validator runs once at startup |
| 2 | OpenTelemetry | ⏭️ Deferred | Startup span and `encina.startup.requirement_violations` counter; Design Choice 9; follow-up issue file `startup-validator-observability-and-health` |
| 3 | Structured Logging | ✅ Phase 2 | `[LoggerMessage]` events 177-183 of `EventIdRanges.Core` |
| 4 | Health Checks | ⏭️ Deferred | An error aborts the host; the outcome as a health entry is in the same follow-up issue file (`IEncinaHealthCheck` is in `Encina.Messaging`, not core) |
| 5 | Validation | ✅ Phase 4 | The row "exactly one validation provider" across the 3 providers |
| 6 | Resilience | ❌ N/A | No external call |
| 7 | Distributed Locks | ✅ Phase 4 | Row L1 checks that the resolved lock provider is not the in-memory one on several instances; the validator takes no lock |
| 8 | Transactions | ⏭️ Deferred | The Transactions row (ADO.NET, Dapper, EF Core accessor, several providers) is the follow-up issue file `startup-validator-remaining-rows` |
| 9 | Idempotency | ❌ N/A | A pure read at startup |
| 10 | Multi-Tenancy | ✅ Phase 1 | `AppliesWhen` lets a module condition a requirement on multi-tenancy (ADR-050 "Conditional: multi-tenancy"); the validator emits no tenant-qualified id (register row C5) |
| 11 | Module Isolation | ❌ N/A | Requirements are per `AddEncina*` call, not per module context |
| 12 | Audit Trail | ✅ Phase 3 | ABAC's requirement that an `IOperationAuditStore` is registered and not in-memory (the audit-store rows of other modules follow in the remaining-rows issue) |

---

## Prerequisites & Dependencies

### Required Prerequisites

| Prerequisite | Status | Notes |
|-------------|--------|-------|
| ADR-050 accepted, #2381 closed | ✅ On `main` | The closed list is the source of every row |
| `Microsoft.Extensions.Hosting.Abstractions` in core | ✅ On `main` | `src/Encina/Encina.csproj` |

### Recommended (Not Blocking)

| Dependency | Issue | Notes |
|-----------|-------|-------|
| In-memory defaults of Anonymization, AntiTampering, Attestation use this contract | #2412 | Opens after S2; a second consumer of `[TestOnlyImplementation]` |
| Processor liveness health | #2396 | Pairs with the deferred health entry |
| Marten.GDPR persistent key store | #1191 | M3 turns the warning into an error; the persistent store is #1191 |
| Stage-order contract and row claiming | #2184, #1251 | ADR-050 sub-decision 8 puts them before #2382; whether that blocks S1 is Design Choice 10 (recommended: not blocking, pending the maintainer) |

---

## Next Steps

1. Link this plan from #2382 and ask the maintainer to answer Design Choices 3 and 9 and register row C6 when work starts.
2. Open S1, then S2; M1, M2, M3, V1, L1 and T1 are independent of each other after S2 and can run in parallel lanes; V2 and V3 open only after V1 merges, because V1 creates the helper they call. Whether S1 may start before #2184 and #1251 close is Design Choice 10 (pending the maintainer).
3. Open the follow-up issue files when S2 opens: `startup-validator-observability-and-health` and `startup-validator-remaining-rows` (drafts in `artifacts/issues/` of the planning worktree).
