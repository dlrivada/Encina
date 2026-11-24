# Implementation Plan: Tenant- and Module-Scoped Selection of Request Validators

> **Issue**: [#3982](https://github.com/dlrivada/Encina/issues/3982)
> **Type**: Feature
> **Related**: #230 (owns `IRequestValidator<T>`, `RequestValidatorRunner`, the options and the range `ValidationAsync` 550-559; a blocking prerequisite of S2), #4000 (the notification, CDC and saga gates consume the shared pieces this plan owns), #227, #228, #231, #232 (their plans defer tenant and module rules here), #3976 (resilience; edits the same runner at another point), #726 (central validation instrumentation: the tenant tag follows there), ADR-054, ADR-062 (merged: error category contract)
> **Complexity**: Medium (one contract and one selector in core, one runner change, no persistence)
> **Estimated Scope**: ~450 lines of production code + ~1,500 lines of tests + ~300 lines of documentation (~2,250 lines in total, as the Estimated File Count table), in three pull requests of at most 13 files and 1,000 lines each
> **Note on #230**: every statement about `IRequestValidator<T>`, the runner and the range 550-559 follows the plan of #230 (a plan pull request, not code on `main`); it is re-verified against `main` when S2 starts

---

## Summary

### What exists today (read on 2026-11-24, main `a450d0489`)

- No request validator contract exists in `src` (`git grep -n -E "IRequestValidator|ValidatorScope" -- src` is empty). #230 plans `IRequestValidator<in TRequest>` and a shared `RequestValidatorRunner` that resolves every registered validator of a request type and runs them in parallel; the three validation providers call that runner, so a change in the runner serves FluentValidation, DataAnnotations and MiniValidator at once.
- The request context carries the tenant: `IRequestContext.TenantId` is a `string?` (`src/Encina/Abstractions/IRequestContext.cs:125`), `null` when the application is not multi-tenant or the tenant cannot be determined. It does not carry a module.
- The module of the running code is ambient, not on the context: `IModuleExecutionContext.CurrentModule` is a `string?` held in an `AsyncLocal` (`src/Encina/Modules/Isolation/ModuleExecutionContext.cs:19-22`), set by `SetModule` or `CreateScope`; it is `null` when no module is executing. `IModuleExecutionContext` is registered scoped by the database provider packages only (`src/Encina.EntityFrameworkCore/ServiceCollectionExtensions.cs:304`, `Encina.ADO.*`, `Encina.Dapper.*`, `Encina.MongoDB`), not by core; core has the module metadata `IModule.Name` and `IModuleRegistry` (`src/Encina/Modules/`), with no message-to-module mapping.
- `ValidationPipelineBehavior<TRequest, TResponse>` and `ValidationOrchestrator` are tenant-agnostic; the providers only copy the tenant into their own context (`FluentValidationProvider` copies `TenantId` into `RootContextData`, #230 plan). Selection is hand-written inside each validator today.
- Not in `src`: a way for a validator to declare the tenants or modules it applies to, any selection step in a runner, any log message for a skipped validator.

### What this plan delivers

An opt-in way for a request validator to declare its scope, applied by the shared runner:

1. **`IScopedRequestValidator<in TRequest> : IRequestValidator<TRequest>`** with `ValidatorScopeKinds ScopeKinds { get; }` (which dimensions the validator is scoped on: `Tenant`, `Module`) and `bool AppliesTo(ValidatorScope scope)`, where `ValidatorScope` carries the `IRequestContext` and the ambient module name. A validator that does not implement the interface runs for everyone, as today.
2. **`ValidatorScopeSelector`**, a public singleton in core `Encina.Validation` that resolves the scope once per request and decides `Run`, `RunScopeMissing`, `RunSelectionFailed` or `Skip`. The runner (#230) calls it before each validator; the gates of #4000 call the same public selector, because they live in other assemblies (`Encina.Cdc`, `Encina.Messaging`) and an internal type would need `InternalsVisibleTo` or a public seam.
3. **Fail-closed selection**: a validator scoped on a dimension whose value is missing in the request (no tenant, no module) is never skipped; a selection that throws runs the validator. Only an explicit `false` for a present value skips.
4. **Logging**: three log messages (skipped, run because the scope value was missing, selection failed) by validator type and scope kind, never a tenant or module value.

Nothing changes for an application whose validators do not implement the interface.

- Affected packages: `Encina` (core) only. `Encina.DataAnnotations`, `Encina.MiniValidator` and `Encina.FluentValidation` need no code change (S2 proves the same verdict under each).
- Provider category: Validation (3) (`AGENTS.md` section 5): selection is in the shared runner; a contract test and one integration test per provider prove it.
- Shared pieces, owned by this plan: `ValidatorScopeKinds`, `ValidatorScope`, `ValidatorScopeDecision`, `IScopedRequestValidator<T>`, `ValidatorScopeSelector` and `AddEncinaValidatorScoping()`. #4000 consumes them (the dependency runs one way: #4000 opens its pull requests after S1 of this plan). The native conditional syntax of each provider (`When` and `Unless` of FluentValidation, `IValidatableObject`) stays as it is: it decides inside one validator, it does not select among validators.

---

## Design Choices

<details>
<summary><strong>1. The selection surface: an interface method, an attribute, or the issue's literal signature</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `IScopedRequestValidator<T>` with `ScopeKinds` and `AppliesTo(ValidatorScope)`, where `ValidatorScope` holds the context and the ambient module** | The rule can use any runtime value (a tenant plan read from the context); the runner learns which dimensions the validator needs, which is what makes fail-closed selection enforceable; testable without a container | The validator class implements two members |
| **B) A declarative attribute `[ValidatorScope(Tenants = "a,b", Modules = "orders")]` read once per validator type** | Nothing to implement; cached per type; a call per request costs a dictionary lookup | Cannot depend on runtime data; tenant lists in attributes are compile-time constants |
| **C) The issue's literal `bool AppliesTo(IRequestContext context)`** | Exactly the shape the issue sketches | The context has no module, so module scoping would need the validator to inject `IModuleExecutionContext` itself; the runner cannot tell which dimension is needed, so a missing value cannot be forced to run |

### Chosen Option: **A — Interface with declared scope kinds** (recommended, pending the maintainer)

### Rationale

- The issue's own Dilemma recommends the interface method; A keeps it and adds the two things the acceptance criteria need: the module (criterion "module-scoped") and a declared dimension (criterion "a missing tenant or module never skips").
- B wins when the scope is a fixed list and the per-request cost must be zero; C wins when only tenants matter and the maintainer wants the smallest interface.

**Class:** Dilemma

</details>

<details>
<summary><strong>2. Where the module of a request comes from</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) The ambient `IModuleExecutionContext.CurrentModule`, read through `IServiceProvider.GetService` (absent registration means unknown module)** | Exists today; set by whatever sets the module (an application behavior, `CreateScope`); no new metadata | Only as accurate as whoever sets the module; core registers no default, so an application without a database provider package has no module unless it registers one |
| **B) A mapping from the request type to its module through `IModuleRegistry`** | The module is a property of the message type, independent of ambient state | There is no message-to-module metadata today (`IModule` has `Name` only); needs a new convention and registration |

### Chosen Option: **A — Ambient `CurrentModule`** (recommended, pending the maintainer)

### Rationale

- A uses only what exists on `main`; B would add module metadata that the module-isolation work (#747 names module scoping for another feature) may define differently.
- B wins when a module must be known for a request that runs outside any module scope, such as a background consumer.

**Class:** Dilemma

</details>

<details>
<summary><strong>3. Synchronous or asynchronous selection</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Synchronous `bool AppliesTo(ValidatorScope)`** | No I/O in the selection step, so it cannot hang or need its own timeout; cheap; the parallel runner calls it before fan-out | A rule that needs a lookup (a tenant plan in a store) must read it in the validator body instead |
| **B) `ValueTask<bool> AppliesToAsync(...)`** | Supports a lookup in selection | The selection step becomes an I/O call that needs the #230 timeout and failure mapping of its own; a skipped validator may cost a round trip |

### Chosen Option: **A — Synchronous** (recommended, pending the maintainer)

### Rationale

- The issue motivates "a tenant plan" as runtime data; the plan is expected on the context or in an options object. A lookup belongs in the validator, which already has timeout, parallelism and failure mapping (#230).
- B wins when the plan data is only in a store and a validator body would run in vain for tenants it does not concern.

**Class:** Dilemma

</details>

<details>
<summary><strong>4. How tenant ids in a scope are compared</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Ordinal, case-sensitive; the helper in `ValidatorScope` compares with `StringComparison.Ordinal`; a null tenant never equals any value, including the reserved `default` segment** | A tenant id is an opaque identifier; no culture surprises; injective (two different ids never match one scope entry) | A validator listing `Acme` does not match `acme` |
| **B) Ordinal ignore-case** | Forgiving of casing in configuration | Two ids that differ only in case would be selected by one entry; tenant stores may treat them as distinct tenants |

### Chosen Option: **A — Ordinal** (recommended, pending the maintainer)

### Rationale

- The reserved `default` tenant segment marks the no-tenant partition of tenant-qualified ids in the multi-tenancy plans (#711, #738); a null tenant is therefore never matched to any listed value, and a listed `default` never matches a real tenant.
- B wins when tenant ids come from a case-insensitive source such as a host name.

**Class:** Dilemma

</details>

<details>
<summary><strong>5. Package placement</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Core `Encina.Validation`, next to the runner** | The runner (#230) is in core; the gates of #4000 reference core already | Core grows by about six files |
| **B) A new package `Encina.Validation.Scoping`** | Core untouched | The runner must call the selector, so the seam is in core anyway; a package for six small types |

### Chosen Option: **A — Core `Encina.Validation`**

### Rationale

- The runner, the orchestrator and `ValidationResult` are in `src/Encina/Validation/`; the issue ticks `Encina (core)` only.

**Class:** Trivial
**Settled by:** the existing layout (`src/Encina/Validation/`) and the issue's "Affected Packages" list (`Encina (core)`)

</details>

### Register of defaults and semantics

| Id | Choice | Class | Basis |
|----|--------|-------|-------|
| C1 | A validator scoped on a dimension whose value is missing (no tenant, no module) is never skipped: the selector returns `RunScopeMissing` without calling `AppliesTo` | Settled | `AGENTS.md` section 3 (compliance and security gates fail closed: when context is missing the gate denies, here by running the rule); holds under every option of Design Choices 1 to 4 because the runner enforces it from the declared kinds |
| C2 | A selection that throws (`AppliesTo` raises) runs the validator (`RunSelectionFailed`) and logs the exception type through `ForLogging()`; a faulty rule can only add validation, never remove it | Settled | `AGENTS.md` section 3 (errors are never swallowed in background infrastructure; gates fail closed; exception logging through `ForLogging()`) |
| C3 | Selection lives in the shared runner and the public selector, not in the providers | Settled | `AGENTS.md` section 5 (Validation: the three providers integrate with the same orchestrator and behavior and stay consistent); the issue's Alternative 2 |
| C4 | The scope (context, module) is resolved once per request before the parallel fan-out; validators receive the same `ValidatorScope` instance, so the `AsyncLocal` module value is read on the request's own logical thread, never inside a parallel worker | Settled | `src/Encina/Modules/Isolation/ModuleExecutionContext.cs:19` (`AsyncLocal`); #230 Phase 1 (the runner runs validators with `Parallel.ForEachAsync`) |
| C5 | A skipped validator contributes no errors and no timeout; it is not a validator success or failure in any count | Trivial | `RequestValidatorRunner` returns the merged errors of the validators it ran (#230 Phase 1); a validator that does not run has none |
| C6 | Logs carry the code, request type name, validator type name and scope kind only, never a tenant id, module name or request value; the ADR-054 D4 tenant tag on telemetry is #726's, because the tag is added where a metric is emitted | Settled | `AGENTS.md` section 3 (`EncinaError.Message` and request data never reach logs); ADR-054 D4; #726 is open and instruments the validation pipeline centrally |
| C7 | EventIds 553-555 inside the range `ValidationAsync` (550-559) that #230 registers in its S2 and the registry reserves in the block 550-599: 553 validator skipped (debug), 554 validator ran because a scope value was missing (debug), 555 selection failed (error). Packed, no new range | Trivial | `AGENTS.md` section 7 (ids inside a package's existing or reserved range, packed); `docs/engineering/eventid-block-registry.md` row 550-599; #230 uses 550-552 and keeps 553-559 free |
| C8 | Tenant offboarding does not matter to selection: the selector never consults a tenant store, so an offboarded tenant id on a request is matched like any other id and a scoped validator for it still runs or is skipped by its own rule | Trivial | the selector reads `IRequestContext.TenantId` only (`IRequestContext.cs:125`) and calls no store; tenant lifecycle is `Encina.Tenancy`'s |
| C9 | `AddEncinaValidatorScoping()` registers `ValidatorScopeSelector` with `TryAddSingleton`; `AddEncinaRequestValidators` (#230) calls it, and the gates of #4000 call it too, so the selector exists wherever a consumer is registered | Settled | `AGENTS.md` section 3 (registration completeness: an `AddEncina*` method registers every dependency it resolves, proven by a `ValidateOnBuild` and `ValidateScopes` DI test); the selector holds no scoped state (it reads the scoped `IServiceProvider` passed in) |
| C10 | No new error code: a skipped validator is not an error and a selection failure runs the validator, so no `Left` is created and no ADR-062 category is needed | Trivial | ADR-062 applies to created `EncinaError` codes; this feature creates none |

---

## Implementation Phases

### Phase 1: Contract, Selector and Registration

> **Goal**: The public types and the selector, tested alone; the runner does not use them yet (pull request S1).

<details>
<summary><strong>Tasks</strong></summary>

1. **Create** in `src/Encina/Validation/`: `ValidatorScopeKinds.cs` (`[Flags] public enum ValidatorScopeKinds { None = 0, Tenant = 1, Module = 2 }`), `ValidatorScopeDecision.cs` (`public enum ValidatorScopeDecision { Run, RunScopeMissing, RunSelectionFailed, Skip }`), `ValidatorScope.cs` (`public sealed record ValidatorScope(IRequestContext Context, string? ModuleName)` with `string? TenantId => Context.TenantId`, `bool HasTenant`, `bool HasModule` (a null or whitespace value is missing), `bool IsTenant(string tenantId)` and `bool IsModule(string moduleName)` ordinal comparisons that return false for a missing value; constructor guards `Context`), `IScopedRequestValidator.cs` (`public interface IScopedRequestValidator<in TRequest> : IRequestValidator<TRequest> { ValidatorScopeKinds ScopeKinds { get; } bool AppliesTo(ValidatorScope scope); }`).
2. **Create** `ValidatorScopeSelector.cs` (`public sealed class ValidatorScopeSelector`; `ValidatorScope CreateScope(IRequestContext context, IServiceProvider serviceProvider)` reads `serviceProvider.GetService<IModuleExecutionContext>()?.CurrentModule` (Design Choice 2, option A); `ValidatorScopeDecision Decide(Type validatorType, ValidatorScopeKinds declared, Func<ValidatorScope, bool> appliesTo, ValidatorScope scope)`: a validator that declares `None` is not scoped on any dimension, so the selector returns `Run` and never calls `AppliesTo`; a declared dimension whose value is missing returns `RunScopeMissing` (register row C1); an exception from `appliesTo` returns `RunSelectionFailed` (C2; the selector logs it in S2, the exception never leaves `Decide`); otherwise `Skip` or `Run` from the delegate).
3. **Create** `ValidatorScopeServiceCollectionExtensions.cs`: `AddEncinaValidatorScoping(this IServiceCollection)` with `TryAddSingleton<ValidatorScopeSelector>()` (register row C9).
4. **Modify** `src/Encina/PublicAPI.Unshipped.txt` and `.github/coverage-manifest/Encina.json` (through `mechanical-fixer`).
5. Tests (S1): `ValidatorScopeSelectorTests` (every decision: declared tenant present and true or false, present and the rule throws, missing tenant, declared module with and without `IModuleExecutionContext` registered, both kinds with one missing, `None`), `ValidatorScopeTests` (`IsTenant` and `IsModule` ordinal, a null tenant never equals `default`, whitespace is missing), `ValidatorScopeSelectorGuardTests` (null context, null service provider, null delegate; the `ValidatorScope` constructor), `ValidatorScopeProperties` (FsCheck: for any tenant and module combination a declared-and-missing dimension never yields `Skip`, and the decision is deterministic for the same input), and `ValidatorScopingRegistrationTests` (DI test with `ValidateOnBuild` and `ValidateScopes` resolving the selector and showing a second `AddEncinaValidatorScoping()` call adds nothing).

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 1</strong></summary>

```text
CONTEXT:
Encina (.NET 10, C# 14, pre-1.0, no backward compatibility). Issue #3982 adds tenant- and module-scoped selection of
request validators. Read docs/plans/validator-scoped-selection-implementation-plan-3982.md (Design Choices 1-5, register
rows C1-C10) and docs/plans/async-cross-field-validation-implementation-plan-230.md for the contract it extends.

TASK:
1. Create ValidatorScopeKinds, ValidatorScopeDecision, ValidatorScope, IScopedRequestValidator<T>, the public
   ValidatorScopeSelector and AddEncinaValidatorScoping under src/Encina/Validation/.
2. The selector never skips a validator whose declared scope value is missing and runs a validator whose selection throws.

KEY RULES:
- Fail closed: missing context runs the validator; exceptions logged through ForLogging(), never their message.
- Public seam (not internal): the gates of #4000 live in other assemblies. XML docs on every public member.
- PublicAPI.Unshipped.txt and the manifest through mechanical-fixer; tests execute real selector code.

REFERENCE FILES:
- src/Encina/Abstractions/IRequestContext.cs (:125), src/Encina/Modules/Isolation/IModuleExecutionContext.cs
- src/Encina/Validation/IValidationProvider.cs, ValidationOrchestrator.cs
```

</details>

---

### Phase 2: Runner Integration and Logging

> **Goal**: The shared runner selects validators and logs the decision (pull request S2; needs #230 S1 and S2).

<details>
<summary><strong>Tasks</strong></summary>

1. **Modify** `src/Encina/Validation/RequestValidatorRunner.cs` (#230): an optional trailing constructor parameter `ValidatorScopeSelector? selector = null` (the #230 tests that build the runner with `new` compile unchanged; `null` means no selection, every validator runs). Before the parallel fan-out the runner creates the scope once (register row C4); per validator that is an `IScopedRequestValidator<TRequest>` it calls `Decide`; `Skip` removes the validator, every other decision runs it.
2. **Modify** `src/Encina/Validation/ValidationAsyncLog.cs` (#230 S2): `[LoggerMessage]` 553 skipped, 554 ran because a scope value was missing, 555 selection failed (error, exception through `ForLogging()`), each by validator type and `ValidatorScopeKinds` only. **Modify** `ValidatorScopeSelector.cs`: an optional trailing constructor parameter `ILogger<ValidatorScopeSelector>? logger = null` and the three log calls inside `Decide`, so every consumer (the runner here, the gates of #4000 in other assemblies) logs the decision the same way and no gate needs a log range of its own for it.
3. **Modify** `RequestValidatorServiceCollectionExtensions.cs` (#230): `AddEncinaRequestValidators` calls `AddEncinaValidatorScoping()`.
4. **Modify** `.github/coverage-manifest/Encina.json` (through `mechanical-fixer`).
5. Tests (S2): `ScopedValidatorSelectionTests` (a scoped validator is skipped for a tenant it does not apply to and runs for one it does; a tenant-scoped validator runs when the request has no tenant; a module-scoped one runs when no module is set and is skipped when another module is current; a throwing `AppliesTo` runs the validator; unscoped validators are untouched; a skipped validator adds no errors and does not time out), a log test for 553 to 555 (EventIds and no tenant, module or request value in the rendered messages), and an addition to the #230 runner DI test that resolves the runner with the selector under `ValidateOnBuild` and `ValidateScopes`.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 2</strong></summary>

```text
CONTEXT:
Issue #3982, plan docs/plans/validator-scoped-selection-implementation-plan-3982.md, phase 2 of 3. Depends on S1 and on
#230 S1-S2 (the runner and ValidationAsyncLog).

TASK:
1. Give RequestValidatorRunner an optional ValidatorScopeSelector; create the scope once before the fan-out and call
   Decide per IScopedRequestValidator<TRequest>; Skip removes the validator.
2. Add log methods 553-555 to ValidationAsyncLog (range ValidationAsync 550-559), call them from the selector (optional
   ILogger), and register AddEncinaValidatorScoping from AddEncinaRequestValidators.

KEY RULES:
- Existing tests that construct the runner with new compile unchanged (optional parameter).
- No tenant, module or request value in a log message; exceptions through ForLogging().

REFERENCE FILES:
- src/Encina/Validation/RequestValidatorRunner.cs and ValidationAsyncLog.cs (from #230)
- tests/Encina.UnitTests/Testing/Architecture/EncinaEventIdAllocationTests.cs (no edit expected: the range exists)
```

</details>

---

### Phase 3: Provider Equivalence, Integration and Documentation

> **Goal**: The three providers are proven to give the same verdict and the feature is documented (pull request S3).

<details>
<summary><strong>Tasks</strong></summary>

1. **Create** `tests/Encina.ContractTests/Validation/ScopedValidatorSelectionContractTests.cs`: one scoped validator set (a tenant-scoped rule, a module-scoped rule, an unscoped rule) run under the real DataAnnotations, MiniValidator and FluentValidation providers with a registered `IScopedRequestValidator<T>`, same `ValidationResult` expected under each.
2. **Create** `tests/Encina.IntegrationTests/Validation/{DataAnnotations,FluentValidation,MiniValidator}/ScopedValidatorPipelineTests.cs`: a request through `IEncina.Send` with the real pipeline behavior and a tenant set on the request context; the stricter rule fires for its tenant only, a request without a tenant is validated by it. Not a database feature: no container.
3. **Modify** `docs/features/async-and-cross-field-validation.md` (owned by #230 S6; a section "Scoping a validator to tenants and modules", through `docs-writer`): the interface, the three decisions, why a missing value runs the rule, how a tenant plan lives on the context, the module source and its limits (Design Choice 2).
4. **Create** `changelog.d/3982-validator-scoped-selection.added.md` (through `mechanical-fixer`).
5. **Create** the justification files `tests/Encina.LoadTests/Validation/ValidatorScopedSelection.md` and `tests/Encina.BenchmarkTests/Validation/ValidatorScopedSelection.md` (`AGENTS.md` section 9 format): one interface check per validator and one `AsyncLocal` read per request, negligible next to validation.
6. Build verification: `dotnet build --configuration Release` with zero warnings; `dotnet test` of the touched projects; per-flag coverage of the touched files against the targets of the Estimated File Count section.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 3</strong></summary>

```text
CONTEXT:
Issue #3982, plan docs/plans/validator-scoped-selection-implementation-plan-3982.md, phase 3 of 3. Depends on S2.

TASK:
1. Write the equivalence contract test (three real providers) and the three integration tests through IEncina.Send.
2. Document the feature (docs-writer), add the changelog fragment (mechanical-fixer) and the two justification files.

KEY RULES:
- Tests execute real provider code, no reflection-only tests; Shouldly through Encina.Testing.Shouldly.
- Never type coverage figures by hand: cite cov: markers (SPEC-001). Link ADRs and sibling plans by number only.

REFERENCE FILES:
- .claude/skills/encina-docs/SKILL.md, changelog.d/README.md
- tests/Encina.IntegrationTests/Validation/ (the #230 S5 tests)
```

</details>

---

## Research

### Relevant Standards & Specifications

| Source | Provision | Use in this feature |
|--------|-----------|---------------------|
| `AGENTS.md` section 3 | Compliance and security gates fail closed; errors never swallowed; exceptions through `ForLogging()` | Register rows C1, C2, C6 |
| `AGENTS.md` section 5 | Validation category: the three providers stay consistent | Selection in the shared runner (row C3) |
| ADR-054 D4 | Tenant tag only when a tenant is present | Why the tag is #726's (row C6) |
| ADR-062 | Error category contract | No new code (row C10) |
| `docs/engineering/eventid-block-registry.md`, ADR-021 | Block 550-599 reserved for the validation family | EventIds 553-555 (row C7) |

### Existing Encina Infrastructure to Leverage

| Component | Location | Usage in this feature |
|-----------|----------|-----------------------|
| `IRequestContext.TenantId` | `src/Encina/Abstractions/IRequestContext.cs:125` | The tenant dimension |
| `IModuleExecutionContext.CurrentModule` | `src/Encina/Modules/Isolation/IModuleExecutionContext.cs:80`, `ModuleExecutionContext.cs:22` | The module dimension (ambient, `null` when none) |
| `IRequestValidator<T>`, `RequestValidatorRunner`, `ValidationAsyncLog` (#230) | `src/Encina/Validation/` (after #230) | Extended in S2 |
| `ValidationPipelineBehavior`, `ValidationOrchestrator` | `src/Encina/Validation/` | Unchanged; the runner is called by the providers |
| `TryAddSingleton` registration with a `ValidateOnBuild` DI test | `src/Encina.EntityFrameworkCore/ServiceCollectionExtensions.cs` (the registration style) | Row C9 |

### Event ID Allocation

| Package | Range | Notes |
|---------|-------|-------|
| `Encina` core (`ValidationAsync`, registered by #230 S2, inside the block 550-599 reserved for the validation family) | 553-555 | 550-552 used by #230; 553-555 used by this plan; 556-559 free |

### Estimated File Count

Every count includes tests, coverage manifest edits, PublicAPI files, the documentation page and the changelog fragment. No pull request uses a type created by a later one; each enters its per-file manifest targets with its own tests. The edited-tests column is the #230 runner DI test S2 extends (one file); the runner's constructor parameter is optional, so no call site changes (re-counted with `git grep -c "new RequestValidatorRunner(" -- tests` when S2 starts).

| PR | Content | New src | Changed src | New tests | Edited tests | Manifest | Other | Files | Lines (est.) |
|----|---------|---------|-------------|-----------|--------------|----------|-------|-------|--------------|
| S1 | Contract, selector, registration | 6 (`ValidatorScopeKinds`, `ValidatorScopeDecision`, `ValidatorScope`, `IScopedRequestValidator`, `ValidatorScopeSelector`, `ValidatorScopeServiceCollectionExtensions`) | 1 (PublicAPI) | 5 (selector unit, scope unit, guard, property, DI) | 0 | 1 | 0 | 13 | ~750 |
| S2 | Runner integration, logs | 0 | 4 (`RequestValidatorRunner`, `ValidationAsyncLog`, `ValidatorScopeSelector`, `RequestValidatorServiceCollectionExtensions`) | 2 (selection unit, log) | 1 (the #230 runner DI test) | 1 | 0 | 8 | ~600 |
| S3 | Equivalence, integration, docs | 0 | 0 | 4 (contract, 3 integration) | 0 | 0 | 4 (feature page, fragment, 2 justification `.md`) | 8 | ~500 |
| **Total** | | **6** | **5** | **11** | **1** | **2** | **4** | **29** | **~1,850** |

The columns sum to the total (6 + 5 + 11 + 1 + 2 + 4 = 29) and the rows sum to 29 files (13 + 8 + 8) and about 1,850 lines (750 + 600 + 500), within the header's estimate. S1 is at 13 files; none exceeds 1,500 lines. S1 comes first; S2 needs S1 and #230 S2; S3 needs S2.

Per-file coverage targets (per flag, with a one-sentence justification), written into `.github/coverage-manifest/Encina.json` by the pull request that adds the file:

| File (entered by) | Unit | Guard | Property | Contract | Justification |
|------|------|-------|----------|----------|---------------|
| `ValidatorScopeSelector.cs` (S1; the S2 logging change keeps the same targets, reached by the S2 log test) | 95 | 85 | 60 | 0 | Every decision branch is reachable with fakes; the guard tests reach the argument checks; the S1 property covers the never-skip-when-missing invariant; no contract test targets the selector itself |
| `ValidatorScope.cs` (S1) | 95 | 80 | 0 | 0 | Small comparison members covered by the scope unit tests; the constructor guard by the guard test; a property adds nothing over the ordinal cases |
| `ValidatorScopeServiceCollectionExtensions.cs` (S1) | 90 | 90 | 0 | 0 | One `TryAdd`, covered by the S1 DI test; the null check by the guard test |
| `ValidatorScopeKinds.cs`, `ValidatorScopeDecision.cs`, `IScopedRequestValidator.cs` (S1) | 0 | 0 | 0 | 0 | Enums and an interface with no executable code and no parameters; behavior is covered through the selector and the runner |
| `RequestValidatorRunner.cs` (S2, changed methods) | 90 | 85 | 0 | 40 | The new branches (skip, run, scope missing, selection failed) are covered by the S2 selection tests; the guard tests of #230 reach the optional parameter; the S3 contract test runs the main path |
| `ValidationAsyncLog.cs` (S2, changed) | 90 | 0 | 0 | 0 | Source-generated methods exercised by the S2 log test; no parameters to guard |

Integration targets are not entered per file: the S3 tests run through `IEncina.Send` and reach the runner's main path, and the package default for the integration flag stays.

---

## Combined AI Agent Prompts

<details>
<summary><strong>Full combined prompt for all phases</strong></summary>

```text
You are implementing issue #3982 - tenant- and module-scoped selection of request validators.

PROJECT CONTEXT:
- Encina is a .NET 10 / C# 14 library; pre-1.0, no backward compatibility, best solution always.
- Validation follows the orchestrator pattern (AGENTS.md sections 3 and 5): the shared runner of #230 serves the three
  providers; selection lives in the runner so the three behave the same.
- Gates fail closed: a missing tenant or module never removes a rule.

IMPLEMENTATION OVERVIEW:
1. Core: ValidatorScopeKinds, ValidatorScopeDecision, ValidatorScope, IScopedRequestValidator<T>, a public
   ValidatorScopeSelector and AddEncinaValidatorScoping (S1).
2. The runner creates the scope once and calls the selector per scoped validator; the selector logs 553-555 (S2).
3. Contract test over the three providers, three integration tests through IEncina.Send, docs, changelog (S3).
Three pull requests (S1 to S3), see the Estimated File Count table. #4000 reuses the public selector in its gates.

KEY PATTERNS:
- Optional trailing constructor parameters keep existing tests compiling; DI test with ValidateOnBuild and ValidateScopes.
- Codes and type names only in logs; ForLogging() for exceptions; never a tenant or module value in a log.

REFERENCE FILES:
- docs/plans/validator-scoped-selection-implementation-plan-3982.md, docs/plans/async-cross-field-validation-implementation-plan-230.md
- src/Encina/Abstractions/IRequestContext.cs, src/Encina/Modules/Isolation/
```

</details>

---

## Cross-Cutting Integration Matrix

| # | Function | Status | Notes |
|---|----------|--------|-------|
| 1 | Caching | ❌ N/A | A per-request decision; a per-type cache of declared kinds is not needed because `ScopeKinds` is a property read |
| 2 | OpenTelemetry | ⏭️ #726 | #726 (open) instruments the validation pipeline centrally; the ADR-054 D4 tenant tag is added where its metric is emitted (register row C6) |
| 3 | Structured Logging | ✅ Phase 2 | EventIds 553-555 in `ValidationAsync`; validator type and scope kind only |
| 4 | Health Checks | ❌ N/A | No checkable dependency |
| 5 | Validation | ✅ All phases | This is validation; the three providers behave the same (S3 contract test) |
| 6 | Resilience | ❌ N/A | In-process; selection is synchronous and cannot hang (Design Choice 3) |
| 7 | Distributed Locks | ❌ N/A | No shared state |
| 8 | Transactions | ❌ N/A | Validation precedes the handler |
| 9 | Idempotency | ❌ N/A | Selection is deterministic for the same context and module |
| 10 | Multi-Tenancy | ✅ Phases 1, 2 | `TenantId` selection, fail closed when absent, ordinal comparison, the reserved `default` segment never matched by a null tenant (Design Choice 4) |
| 11 | Module Isolation | ✅ Phases 1, 2 | Module selection from the ambient `IModuleExecutionContext` (Design Choice 2); a message-type mapping is the alternative option |
| 12 | Audit Trail | ⏭️ Deferred | Auditing of requests rejected by scoped rules belongs to the validation-failure audit family (#232 owns the security audit of violations); no state changes here |

---

## Prerequisites & Dependencies

### Required Prerequisites

| Prerequisite | Status | Notes |
|-------------|--------|-------|
| `IRequestValidator<T>`, `RequestValidatorRunner`, `ValidationAsyncLog`, the range `ValidationAsync` (#230) | Open (plan only) | Blocking for S2 only; S1 compiles against `IRequestValidator<T>` and so waits for #230 S1 |
| `IModuleExecutionContext`, `IRequestContext.TenantId` | ✅ On `main` | Module and tenant sources |

### Recommended (Not Blocking)

| Dependency | Issue | Notes |
|-----------|-------|-------|
| Central validation instrumentation | #726 (open) | Tenant tag and counters |
| Resilience for async validators | #3976 (open) | Also edits `RequestValidatorRunner.cs`; merge order, no shared type |
| Tenant-aware validators for notification, CDC and saga gates | #4000 (open) | Consumes the public selector of S1 |

---

## Next Steps

1. Link this plan from #3982 and ask the maintainer to answer Design Choices 1 to 4 when work starts.
2. Implement S1 to S3 as the pull requests of the Estimated File Count split once #230 S1 and S2 are merged, `Fixes #3982` on S3.
3. Re-verify against `main` when S1 starts: the `IRequestValidator<T>` signature and the runner constructor from #230, and that `IModuleExecutionContext` is still registered only by the provider packages.
