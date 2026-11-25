# Implementation Plan: Tenant- and Module-Scoped Validation Resources (Locale, Schemas and Validators)

> **Issue**: [#3981](https://github.com/dlrivada/Encina/issues/3981)
> **Type**: Feature (the issue carries `needs-decision`; every open alternative is a Dilemma below, recommended and pending the maintainer)
> **Related**: #233 (localization: owns `IValidationCultureResolver` and the localizer), #235 (schema-first validation: owns `AddEncinaSchema<TRequest>`), #236 (two-phase validation, defers tenant and module validators per phase), #230 (owns `IRequestValidator<T>` and `RequestValidatorRunner`), #726 (central validation instrumentation), #2676 (tenant tag helper), #2442 (typed `ModuleId` on the request context), #3978 (audit of rejected requests, planned apart), #3982 (owns the shared validator-applicability pieces: `ValidatorScope`, `ValidatorScopeKinds`, `IScopedRequestValidator<T>`, `ValidatorScopeSelector`, `AddEncinaValidatorScoping()`; this plan consumes them), #4000 (the notification, CDC and saga gates; consumes #3982), #1257 (owns the reserved `default` tenant Dilemma DC2), ADR-054, ADR-062 (merged: error category contract)
> **Complexity**: Medium (small core primitives, three thin consumers; no persistence)
> **Estimated Scope**: ~2,200 lines in total (~420 production, ~1,450 tests and manifests, ~330 documentation and ADR), in six pull requests of at most 12 files and 900 lines each

---

## Summary

### What exists today (read on 2026-11-24, main `278c164f1`; re-checked after the merge of `origin/main` at the same commit)

- `TenantId` reaches validators only through `IRequestContext.TenantId` (`src/Encina/Abstractions/IRequestContext.cs:125`; `FluentValidationProvider` copies it into `RootContextData`). Nothing under `src/Encina/Validation/` selects a resource by tenant, and validation never reads a module. The module of a request is available as `RequestContextModuleExtensions.GetModuleName` (a metadata key, `src/Encina/Modules/RequestContextModuleExtensions.cs`) and, after #2442, as `IRequestContext.ModuleId`; there is no `IModuleContext` type in `src`.
- None of the consumers exist on `origin/main` yet: `IRequestValidator<T>` and `RequestValidatorRunner` (#230), `IValidationCultureResolver`, `IValidationMessageLocalizer` and `ValidationCultureScope` (#233), `ISchema<T>` and `AddEncinaSchema<TRequest>(schema)` (#235) are all planned. Each of those plans defers the tenant and module part to this issue: #233 row 10 and 11 (tenant locale, module resource sets), #235 register row C9 and rows 10 and 11 (tenant and module schemas), #236 rows 10 and 11 (tenant and module validators per phase), #230 row 10 and 11 (tenant-scoped validator selection).
- #236 has no phase-selection code: its phase 1 is whichever validators run in `ValidationPipelineBehavior`, and phase 2 is handler code. So "validators per phase" reduces to phase 1 validators, which are `IRequestValidator<T>` registrations (#230); a tenant rule in phase 2 stays application code in the handler.
- The orchestrator is a singleton for `AddDataAnnotationsValidation` and `AddMiniValidation` and scoped for `AddEncinaFluentValidation`; anything the selection needs at request time must be immutable and thread-safe.
- Not in `src`: any scope, key or selector type for validation, and any EventId range for it (`EventIdRanges.cs` has none).

### What this plan delivers

One shared way to say "this resource applies to this tenant and module", used by the three features, with the global validation always applying. The work is split by ownership between this plan and #3982, which already plans the validator-applicability pieces:

- **#3982 owns "does this validator run for this request"**: `ValidatorScope`, `ValidatorScopeKinds`, `ValidatorScopeDecision`, `IScopedRequestValidator<T>`, `ValidatorScopeSelector` and `AddEncinaValidatorScoping()`. This plan defines none of them and consumes them for validators and schemas (S2 and S4).
- **This plan owns "which value (a culture, a resource set) applies for this tenant and module"**: the key and the lookup of a replaced choice, which #3982 does not provide (it only answers run or skip).

1. **Primitives in core `Encina.Validation`** (no dependency on the other plans): `ValidationScope` (tenant, module, read from `IRequestContext`), `ValidationScopeKey` (an injective key, `default` reserved) and `ValidationScopeSelector<TChoice>` (a frozen lookup of a choice: most specific first, global default). Whether these three types should instead reuse `ValidatorScope` is Design Choice 5.
2. **Validators and schemas** (needs #3982 S1, #230 and #235): no new filter type. A tenant- or module-scoped request validator implements `IScopedRequestValidator<TRequest>` of #3982 and the runner of #230 skips it through `ValidatorScopeSelector`; a scoped schema is an `IScopedRequestValidator<TRequest>` produced by the `AddEncinaSchema<TRequest>(schema, ...)` overload of #235 that this plan adds in S4. The global validators always run; a scoped one adds to them.
3. **Locale and resource sets** (needs #233): `ScopedValidationCultureResolver` (a tenant culture, falling back to the ambient resolver) and `ScopedValidationMessageLocalizer` (a module resource set, falling back to the global one for a missing key).
4. **Registration**: `AddEncinaValidationScoping(options => ...)` with `TryAdd*` registrations proven by a `ValidateOnBuild` and `ValidateScopes` test; it never registers `ValidatorScopeSelector` (that is `AddEncinaValidatorScoping()` of #3982).

An application that registers nothing behaves exactly as before.

- Affected packages: `Encina` (core), `Encina.Validation.Schema` (one overload, from #235). `Encina.FluentValidation`, `Encina.DataAnnotations` and `Encina.MiniValidator` need no code change: the validator selection sits in the shared runner of #230/#3982 and the culture scope sits in the orchestrator.
- Provider category: Validation (3) (`AGENTS.md` section 5): the S3 integration test proves the three providers follow the same tenant culture (the validator selection contract test belongs to #3982).
- Shared pieces, owned by this plan: `ValidationScope`, `ValidationScopeKey`, `ValidationScopeSelector<TChoice>`, `ValidationSelection<TChoice>`, `ValidationScopeSelectorBuilder<TChoice>`, the error codes `validation.scope_*` and EventIds 12350-12353. Owned by #3982 and consumed here: the validator-applicability types listed above. Owned by #230, #233 and #235 and consumed here: their contracts, none changed.
- Dependency order: #3982 S1 (and #230) before S2; S2, #3982 S1 and #235 before S4; #233 before S3; S1a and S1b depend on nothing. #4000 consumes #3982 and does not depend on this plan.

---

## Design Choices

<details>
<summary><strong>1. One shared selection concept versus one hook per feature (the issue's Dilemma D1)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) A shared scope, key and selector of choices (culture, resource set), plus the validator applicability of #3982 for validators and schemas** | One concept and one registration to learn; one place that owns the injective key and the reserved `default` segment | Needs #233, #235 and #3982 to exist for their consumers; two scope types (Design Choice 5) |
| **B) Each feature owns its hook: the localization resolver of #233, a schema registry keyed by tenant in #235, validator selection in #230** | Each ships alone and fits its shape; the first feature does not wait for the others | Three similar APIs to keep equal, three implementations of the key rule, and #233, #235 and #236 each re-decide the reserved segment |

### Chosen Option: **A - shared primitives, thin consumers** (recommended, pending the maintainer)

### Rationale

- The injective key and the reserved `default` segment are the part most likely to drift; A writes them once. A also keeps the consumers small (S2 to S4 are a few files each).
- B wins when the features ship at different times and the first must not wait for the shared pieces: S1a and S1b of this plan have no dependency, so the first feature can adopt the primitives alone, and a feature that cannot wait can ship its own hook and move later.

**Class:** Dilemma

</details>

<details>
<summary><strong>2. A scoped schema or validator: added to the global one, or replacing it</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Added: the global validators always run and the scoped ones run in addition; a scope can only make validation stricter** | Matches "nothing is skipped": a missing or wrong tenant context can never select a weaker rule; the selection type carries the global choice, so a consumer cannot return only the scoped one | A tenant cannot relax a global rule (it must change the global rule or use a condition inside it) |
| **B) Replaced: the scoped validator or schema stands in for the global one of the same request type** | A tenant can have a looser or different schema | A scope that matches by mistake (a tenant id that collides, a stale registration) silently weakens validation; a gate must fail closed |

### Chosen Option: **A - additive** (recommended, pending the maintainer)

### Rationale

- The issue's use cases are all "stricter" (a stricter schema, rules added to the structural phase). A wins for every one of them and cannot fail open. B wins for a platform whose tenants are allowed different contracts for the same request type, and the maintainer accepts that a wrong match then weakens validation.
- This choice applies to validators and schemas; a culture and a resource set are replaced by nature (one culture per request), with the global one as the fallback.

**Class:** Dilemma

</details>

<details>
<summary><strong>3. Where the scoped configuration comes from</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Registrations in code or bound configuration, frozen at startup** | No I/O and no lock on the request path; testable; the registry is immutable and thread-safe in a singleton orchestrator | A new tenant needs a restart or a reload of the application's configuration |
| **B) Read per request from a store (a tenant record with a culture, a table of schemas)** | Tenants change at runtime without a restart | A store read on every request; a new failure mode that must fail closed; a cache and an invalidation to design; needs the tenancy and storage design of its own |

### Chosen Option: **A - frozen at startup** (recommended, pending the maintainer)

### Rationale

- The selection sits on the hot path of every request and runs inside a singleton; A keeps it a dictionary lookup. B wins for a SaaS whose tenants are created and changed by customers at runtime; it is a follow-on that reuses the primitives (the key and the selector stay, the source changes) and is listed in Next Steps.

**Class:** Dilemma

</details>

<details>
<summary><strong>4. Precedence when a choice is registered for a tenant and a module together</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Most specific first: tenant and module, then tenant, then module, then global** | Natural reading of "this tenant in this module"; every step is a plain lookup | A module-wide default loses to a tenant-only choice even where the module owns the resource |
| **B) Module first: module, then tenant and module, then tenant, then global** | The module that owns a resource set decides it | A tenant cannot override a module default without naming the module |

### Chosen Option: **A - most specific first** (recommended, pending the maintainer)

### Rationale

- Only replacement consumers use the order (a culture is per tenant, a resource set is per module, and no consumer registers one choice under both today), so the order is cheap to change. A wins for locale-style choices owned by the tenant; B wins if module teams own their resource sets and tenants must not override them. Additive consumers (validators, schemas) apply every matching scope and ignore the order.

**Class:** Dilemma

</details>

<details>
<summary><strong>5. One scope type with #3982, or a separate one for value selection</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Two types with two jobs: `ValidatorScope` of #3982 answers "does this validator run" and `ValidationScope` of this plan keys "which culture or resource set applies"** | Each plan ships alone; `ValidatorScope` carries the whole `IRequestContext` and an ambient module, which a key does not need; the key is a plain value type that can be a dictionary key | Two similarly named scope types in `Encina.Validation` (the names differ by one suffix) |
| **B) This plan reuses `ValidatorScope` of #3982 and adds only the key and the selector** | One scope concept; one place reads the tenant and the module | S1a and S1b then depend on #3982 S1 and lose their "no dependency" property; `ValidatorScope` holds a reference to the context, which a frozen-lookup key should not retain |

### Chosen Option: **A - two types, one rename pending** (recommended, pending the maintainer)

### Rationale

- A keeps this plan buildable first and keeps the applicability decision (run or skip, with the fail-closed `RunScopeMissing` of #3982) apart from the value lookup (replace, with the global fallback). B wins if the maintainer prefers one scope vocabulary and accepts that #3982 S1 merges first; the cost then is the S1a task 1 (`ValidationScope` is dropped and `ValidationScopeKey.Create` takes the tenant and module strings of a `ValidatorScope`). If A stands the names stay as written in this plan; a rename of `ValidationScope` (for example `ValidationChoiceScope`) to avoid the near-collision with `ValidatorScope` is the maintainer's option and touches only S1a to S4 identifiers.

**Class:** Dilemma

</details>

### Register of defaults and semantics

| Id | Choice | Class | Settled by |
|----|--------|-------|------------|
| C5 | The primitives live in core `Encina.Validation` next to `IValidationProvider` | Trivial | The layout of `src/Encina/Validation/` |
| C6 | Missing context is defined here once, per dimension: a blank or reserved (`default`) context tenant is "no tenant", and an absent or blank module is "no module". `Select` walks the chain of Design Choice 4 and skips each step that needs a dimension the context lacks, so a request with no tenant but a module still reaches the module step, a request with a tenant but no module still reaches the tenant step, and only a request that has neither (or no matching entry) gets the global choice alone. `ValidationScopeSelector<TChoice>.Select` always carries the global choice, so no consumer can return a weaker one for a missing context. Whether a present scoped validator or schema adds to the global one or replaces it is Design Choice 2, not decided here | Settled | AGENTS.md section 3 (compliance and security gates fail closed when context is missing); the requirement text of #3981 |
| C7 | The key is `t<len>:<tenant>` or `-` for no tenant, `m<len>:<module>` or `-` for no module, and `k<len>:<key>`, joined with `\|`; every segment is length-prefixed or the single marker, so no two (tenant, module, key) triples share a key (injective) | Settled | The requirement text of #3981 (injective keys), proven by the property test of S1a |
| C7b | The tenant `default` (ordinal, ignoring case) is reserved for the no-tenant case: `ValidationScopeKey.Create` returns `validation.scope_reserved_tenant` for it, and at request time a context tenant `default` is "no tenant" as defined in C6 (the module step still runs). The reservation follows the Dilemma DC2 of #1257 (a reserved default tenant, recommended and pending the maintainer there); the issue text of #3981 only refers to "the plans of the 1.0 batch" and there is no ADR or SPEC for it. If #1257 settles differently, only `ValidationScopeKey.Create` and `ValidationScope.From` change | Dilemma (recommended, pending the maintainer) | #1257 DC2 |
| C8 | New error codes: `validation.scope_invalid_key` (blank tenant, module or key) and `validation.scope_reserved_tenant` are `ErrorCategory.Validation` (ADR-062 R3: field rules are `Validation`; both are rules on the value of a field the caller or the configuration supplies, not a missing header, key, id or tenant, which R3 makes `InvalidRequest`); `validation.scope_conflict` (two choices registered for one key) is `ErrorCategory.AlreadyExists` (ADR-062 R2: duplicates are `AlreadyExists`; R1 does not apply because the conflicting entry is not a lookup by a caller-supplied id). None reaches a client: they surface at startup as an `OptionsValidationException` and from `Create` as a `Left` | Settled | ADR-062 R2, R3 |
| C9 | Telemetry: logs carry codes and counts, never a tenant or module id; the tenant tag on the central validation instrumentation follows ADR-054 D4 through the helper of #2676 and is owned by #726, so this plan adds no instrument | Settled | ADR-054 D4; AGENTS.md section 3 |
| C10 | The module is read from the request context (`GetModuleName()` today, `ModuleId` after #2442), never from the ambient `IModuleExecutionContext`, which is a scoped `AsyncLocal` and is not set outside the EF Core behavior | Trivial | `src/Encina/Modules/RequestContextModuleExtensions.cs`; `src/Encina/Modules/Isolation/IModuleExecutionContext.cs`; the plan of #2442 |
| C11 | The selector and the wrapper are immutable after construction and resolve no scoped service, so they are safe in the singleton orchestrator of two providers | Settled | AGENTS.md section 3 (never a scoped dependency in a singleton; `ValidateScopes`) |
| C12 | The wrapper needs a per-call scope, so it registers as a factory descriptor (`TryAddEnumerable` rejects those and keys on the implementation type, which would suppress a second scoped validator of one request type). Dedupe identity is `(TRequest, TValidator, ValidationScopeKey, lifetime)` kept in an internal ledger on the collection: identical registrations collapse and run once, distinct validators or scopes each register and run. Singleton-safe: the ledger holds no service | Trivial | `Microsoft.Extensions.DependencyInjection` `TryAddEnumerable` contract (only one option registers a scoped validator); the same registration-twice-runs-once outcome as D7 of the plan of #230 |

---

## Implementation Phases

### Phase 1: Scope, Key and Error Codes (pull request S1a)

<details>
<summary><strong>Tasks</strong></summary>

1. **Create** `src/Encina/Validation/Scoping/ValidationScope.cs`: `public readonly record struct ValidationScope(string? TenantId, string? ModuleId)` with `static ValidationScope Global`, `bool IsGlobal`, and `static ValidationScope From(IRequestContext context)` (blank values and the tenant `default` become null; the module comes from `context.GetModuleName()`); `From` rejects a null context with `ArgumentNullException`.
2. **Create** `src/Encina/Validation/Scoping/ValidationScopeKey.cs`: `public static class ValidationScopeKey { public static Either<EncinaError, string> Create(string? tenantId, string? moduleId, string key); }` (register rows C7 and C8).
3. **Create** `src/Encina/Validation/Scoping/ValidationScopeErrors.cs`: the three codes and their `EncinaError` factories with explicit categories (`Validation`, `Validation`, `AlreadyExists`; register row C8), constant messages.
4. **Modify** `src/Encina/PublicAPI.Unshipped.txt` (`mechanical-fixer`).
5. **Tests**: new `tests/Encina.UnitTests/Validation/Scoping/ValidationScopeTests.cs` and `ValidationScopeKeyTests.cs` (blank and `default` tenants, the exact key format, the error categories); new `tests/Encina.GuardTests/Core/Validation/ValidationScopeGuardTests.cs` (null context, null or blank key, a null tenant or module accepted as a valid result); new `tests/Encina.PropertyTests/Validation/ValidationScopeKeyPropertyTests.cs` (FsCheck: for any two different (tenant, module, key) triples the keys differ, including tenants and keys that contain `:`, `\|` and digits).
6. **Manifest** `.github/coverage-manifest/Encina.json` through `mechanical-fixer`.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 1</strong></summary>

```text
CONTEXT: Encina (.NET 10, C# 14). Issue #3981: tenant- and module-scoped validation resources. Core validation lives in src/Encina/Validation. This phase has no dependency on other plans.
TASK: Add ValidationScope, ValidationScopeKey and ValidationScopeErrors in src/Encina/Validation/Scoping, with unit, guard and property tests.
KEY RULES: The key is injective (length-prefixed segments, a single marker for absent) and the tenant "default" is reserved (ordinal, ignoring case): Create returns a Left with an explicit ErrorCategory (Validation for blank or reserved, AlreadyExists for a conflict), never an exception for business input. A context tenant "default" or blank means no tenant. Constant error messages: no tenant or module id in a message. Update PublicAPI.Unshipped.txt and the manifest through mechanical-fixer.
REFERENCE FILES: src/Encina/Abstractions/IRequestContext.cs, src/Encina/Modules/RequestContextModuleExtensions.cs, src/Encina/Errors/ErrorCategory.cs, docs/architecture/adr/062-error-category-contract.md, tests/Encina.PropertyTests (an existing FsCheck test for the style).
```

</details>

### Phase 2: Selector, Options, Registration and Log (pull request S1b)

<details>
<summary><strong>Tasks</strong></summary>

1. **Create** `src/Encina/Validation/Scoping/ValidationScopeSelector.cs`: `public sealed class ValidationScopeSelector<TChoice>`, built by `ValidationScopeSelectorBuilder<TChoice>` and returning `ValidationSelection<TChoice>` (both public, declared in this same file, so the file counts hold) (`Add(string? tenantId, string? moduleId, TChoice choice)` returns `Either<EncinaError, ValidationScopeSelectorBuilder<TChoice>>`; a duplicate scope is `validation.scope_conflict`; `Build(TChoice global)` freezes into a `FrozenDictionary<string, TChoice>`); `ValidationSelection<TChoice> Select(ValidationScope scope)` returns the global choice and the most specific scoped one (register row C6 and Design Choice 4); nothing is cached and nothing is allocated for a global scope.
2. **Create** `src/Encina/Validation/Scoping/ValidationScopingOptions.cs` (the entry list that the consumers extend with their own methods; validated at startup by `IValidateOptions`: no reserved tenant, no conflict) and `ValidationScopingServiceCollectionExtensions.cs`: `AddEncinaValidationScoping(this IServiceCollection services, Action<ValidationScopingOptions>? configure = null)` registers the options and `ValidateOnStart` with `TryAdd*`.
3. **Create** `src/Encina/Validation/Scoping/Diagnostics/ValidationScopingLog.cs`: `internal static partial class`, `[LoggerMessage]` 12350 (Error: reserved tenant in a registration, code only) and 12351 (Error: registration conflict, code only), both logged by the options validator at startup and reached by the registration test. The selector is a frozen type with no logger; the two per-request Debug ids, 12352 (a scoped choice selected, the kind of scope, no ids) and 12353 (global fallback), are added to this class in S3 and logged by its consumers (the culture resolver and the localizer decorators, which receive an `ILogger`), so they are reached by the S3 tests.
4. **Modify** `src/Encina/Diagnostics/EventIdRanges.cs`: `ValidationScoping = (12350, 12399)` (the first free 50-id slot after `ValidationAudit` 12300-12349 of the plan of #3978, #340's 12200-12249 and the process-manager family's 12250-12299); `src/Encina/PublicAPI.Unshipped.txt`, `tests/Encina.UnitTests/Testing/Architecture/EncinaEventIdAllocationTests.cs` (the `Encina` entry) and `docs/engineering/eventid-block-registry.md` (a `reserved` and a `zone` row) follow, mechanical.
5. **Tests**: new `tests/Encina.UnitTests/Validation/Scoping/ValidationScopeSelectorTests.cs` (precedence, global only, missing context, conflict, a scoped choice never replaces the global one in the selection); new `tests/Encina.UnitTests/Validation/Scoping/ValidationScopingRegistrationTests.cs` (the container builds with `ValidateOnBuild` and `ValidateScopes` in both registration orders; an invalid option fails at startup); new `tests/Encina.GuardTests/Core/Validation/ValidationScopeSelectorGuardTests.cs`.
6. **Manifest** `.github/coverage-manifest/Encina.json` through `mechanical-fixer`.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 2</strong></summary>

```text
CONTEXT: Encina issue #3981, phase 2 (after phase 1 merged). The selector is a frozen lookup the consumers share; it runs inside a singleton orchestrator.
TASK: Add ValidationScopeSelector<TChoice> and its builder, ValidationScopingOptions with a startup validator, AddEncinaValidationScoping, the log class (12350 and 12351 now; 12352 and 12353 arrive with S3) and register EventIdRanges.ValidationScoping (12350, 12399).
KEY RULES: Select always returns the global choice plus the most specific scoped one; "missing context" is defined in register row C6 (a blank or reserved tenant is no tenant, an absent module is no module; each chain step needing a missing dimension is skipped, so a module with no tenant still selects its module choice, and only a request with neither gets global only). No scoped service anywhere. Register the EventId range first, map the assembly in EncinaEventIdAllocationTests, add PublicAPI and registry rows, pack the ids with no gaps. Logs carry codes and counts, never a tenant or module id. TryAdd registrations; prove them with a ValidateOnBuild and ValidateScopes test in both orders.
REFERENCE FILES: src/Encina/Validation/Scoping/ (phase 1), src/Encina/Diagnostics/EventIdRanges.cs, docs/engineering/eventid-block-registry.md, tests/Encina.UnitTests/Testing/Architecture/EncinaEventIdAllocationTests.cs.
```

</details>

### Phase 3: Scoped Validators and Schemas (pull request S2, after #230; S4, after S2 and #235)

<details>
<summary><strong>Tasks</strong></summary>

This phase defines no selection mechanism: the decision "does this validator run for this request" is `ValidatorScopeSelector` of #3982, called by the runner of #230. This phase adds only a registration-time wrapper, so an existing validator (a FluentValidation one, a schema validator) can be scoped by registration without implementing `IScopedRequestValidator<T>` itself.

1. **S2 create** `src/Encina/Validation/Scoping/ScopedRequestValidator.cs`: `public sealed class ScopedRequestValidator<TRequest> : IScopedRequestValidator<TRequest>` (the interface is owned by #3982), constructor `(IRequestValidator<TRequest> inner, ValidationScope scope)`; `ScopeKinds` is `Tenant` and/or `Module` according to the non-null members of `scope`; `AppliesTo(ValidatorScope)` compares with `ValidatorScope.IsTenant` and `IsModule` (ordinal; #3982); `ValidateAsync` delegates to `inner`. The skip or run decision, including `RunScopeMissing` (a context that lacks a declared tenant or module runs the validator, so a missing context never skips a rule), belongs to `ValidatorScopeSelector`; the wrapper does not repeat it.
2. **S2 create** `src/Encina/Validation/Scoping/RequestValidatorScopeExtensions.cs`: `AddRequestValidator<TRequest, TValidator>(this IServiceCollection, ValidationScope scope, ServiceLifetime lifetime = ServiceLifetime.Scoped)` (register row C12). Registration shape: one factory descriptor `ServiceDescriptor.Describe(typeof(IScopedRequestValidator<TRequest>), sp => new ScopedRequestValidator<TRequest>(<the TValidator resolved or created from sp>, scope), lifetime)` added with `services.Add`, never `TryAddEnumerable` (that method throws `ArgumentException` for a factory descriptor and, keyed by implementation type, would drop a second validator of the same request type). A registration ledger instance (an internal singleton held on the collection, found by scanning it, so no service is resolved) records the identity `(TRequest, TValidator, ValidationScopeKey, lifetime)`: an identical registration is skipped and runs once, any other validator or scope for the same request is a distinct registration and runs. It calls no registration of the selector, because `AddEncinaRequestValidators` of #230 already calls `AddEncinaValidatorScoping()` of #3982; `PublicAPI.Unshipped.txt` and the manifest through `mechanical-fixer`.
3. **S2 tests**: unit tests of the wrapper, a guard test, and an integration test through `IEncina.Send` (two tenants and one module, no container) with a registration `ValidateOnBuild` and `ValidateScopes` check. The three-provider contract test of the selection itself is #3982's (Phase 3 there); this plan adds none for validators.
4. **S4 modify** (needs S2 merged: it uses `ScopedRequestValidator<TRequest>`) `src/Encina.Validation.Schema/SchemaServiceCollectionExtensions.cs` (from #235): `AddEncinaSchema<TRequest>(this IServiceCollection, ISchema<TRequest> schema, ValidationScope scope)` registers the schema validator wrapped in `ScopedRequestValidator<TRequest>`; PublicAPI, the manifest `Encina.Validation.Schema.json` and unit and guard tests in the same pull request.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 3</strong></summary>

```text
CONTEXT: Encina issue #3981, phase 3. Needs IRequestValidator<T> and RequestValidatorRunner from #230 (S2), IScopedRequestValidator<T>, ValidatorScope and ValidatorScopeSelector from #3982 S1 (S2 and S4), and AddEncinaSchema from #235 (S4); phases 1 and 2 are merged. Re-read the plans of #3982 and #230 and verify their types on main first.
TASK: Add ScopedRequestValidator<TRequest> (an IScopedRequestValidator<TRequest> wrapper), the scoped AddRequestValidator overload and the scoped AddEncinaSchema overload, with unit, guard and integration tests.
KEY RULES: Do NOT define a second selector, scope-kind or applicability interface: #3982 owns them. The wrapper reports ScopeKinds and AppliesTo; the selector decides (a declared dimension missing from the context runs the validator). Match tenants ordinally. Register through a factory descriptor and the identity ledger of register row C12, never TryAddEnumerable. Singleton-safe (no scoped service held by the wrapper).
REFERENCE FILES: docs/plans for #230, #235 and #3982 (by issue number), src/Encina/Validation/Scoping/ (phases 1 and 2).
```

</details>

### Phase 4: Tenant Culture and Module Resource Sets (pull request S3, after #233)

<details>
<summary><strong>Tasks</strong></summary>

1. **Create** `src/Encina/Validation/Scoping/ScopedValidationCultureResolver.cs`: `internal sealed class ScopedValidationCultureResolver : IValidationCultureResolver`, constructor `(ValidationScopeSelector<string> cultures, IValidationCultureResolver ambient)`; `Resolve(IRequestContext)` returns the matching tenant culture name resolved through the `SupportedCultures` matching of #233 (a name outside the allow-list is never turned into a `CultureInfo`) or the ambient result.
2. **Create** `src/Encina/Validation/Scoping/ScopedValidationMessageLocalizer.cs`: `internal sealed class ScopedValidationMessageLocalizer : IValidationMessageLocalizer`, constructor `(ValidationScopeSelector<IValidationMessageLocalizer> modules, IValidationMessageLocalizer global)`; a module localizer is asked first and the global one answers a key the module does not define.
3. **Create** `src/Encina/Validation/Scoping/ValidationScopingCultureExtensions.cs`: `UseTenantCulture(this ValidationScopingOptions options, string tenantId, string cultureName)` and `UseModuleResources(this ValidationScopingOptions options, string moduleId, Type resourceType)`; the registration decorates the #233 services with `TryAdd*` ordering that keeps an application replacement.
4. **Modify** `src/Encina/Validation/Scoping/Diagnostics/ValidationScopingLog.cs` (S1b): add 12352 (Debug: a scoped choice selected) and 12353 (Debug: global fallback); both decorators receive an `ILogger` and call them (register row C9: kind of scope only, no ids).
5. **Tests**: unit tests of both decorators, a guard test, and an integration test in a non-default culture under the three providers (the culture scope of #233 makes the native localization follow the tenant culture); PublicAPI and the manifest through `mechanical-fixer`.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 4</strong></summary>

```text
CONTEXT: Encina issue #3981, phase 4. Needs IValidationCultureResolver, IValidationMessageLocalizer and the culture allow-list of #233, plus phases 1 and 2.
TASK: Add the tenant culture resolver, the module resource-set localizer and their registration methods.
KEY RULES: A culture name from configuration is matched against SupportedCultures before any CultureInfo is created; an unsupported name never throws and falls back to the ambient culture. "Missing context" is register row C6: no tenant (blank or reserved) means no tenant culture entry applies and the ambient culture is used, while the module step of the localizer still runs for the request's module. A module that defines no key falls back to the global localizer. No scoped service in the decorators.
REFERENCE FILES: docs/plans for #233 (by issue number), src/Encina/Validation/Scoping/ (phases 1 and 2).
```

</details>

### Phase 5: Documentation, ADR and Finalization (pull request S5)

<details>
<summary><strong>Tasks</strong></summary>

1. **Create** `docs/features/validation-scoped-resources.md` through `docs-writer` (how to scope a locale, a schema and a validator; the additive rule; the key and the reserved tenant; what a missing context does).
2. **Create** the ADR of the shared selection decision through `docs-writer` (number assigned when the ADR pull request opens).
3. **Create** `changelog.d/3981-validation-scoped-resources.added.md` through `mechanical-fixer`; **modify** `docs/INVENTORY.md` (one row).
4. Build verification: `dotnet build --configuration Release` with 0 warnings; every flag reaches the targets of "Estimated File Count".

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 5</strong></summary>

```text
CONTEXT: Encina issue #3981, phase 5. Code and tests of phases 1 to 4 are merged.
TASK: Hand the documentation page, the ADR, the INVENTORY row and the changelog fragment to docs-writer and mechanical-fixer; run build, tests and the coverage commands.
KEY RULES: Documentation never types coverage figures by hand (covref markers). Never edit CHANGELOG.md. The ADR number is assigned when its pull request opens. Public text never names a private application.
REFERENCE FILES: docs/features/audit-tracking.md, changelog.d/README.md, docs/architecture/adr/index.md.
```

</details>

---

## Research

### Relevant Standards & Specifications

| Standard | Requirement | How the plan meets it |
|----------|-------------|-----------------------|
| `AGENTS.md` section 3 | Gates fail closed when context is missing | A missing tenant or module context never skips a step it does not need and never returns less than the global choice (register row C6) |
| ADR-054 D4 | Tenant tag only when multi-tenancy is on; no baggage | No instrument here; #726 and #2676 own it (register row C9) |
| ADR-062 R1, R3 | Configuration entries are `Internal`; a field rule on a blank or reserved tenant, module or key is `Validation`; a missing header, key, id or tenant is `InvalidRequest` | Register row C8 |
| OWASP ASVS V5.1.3 | Allow-list validation of input | A culture name is matched against `SupportedCultures` before a `CultureInfo` exists (#233) |

### Existing Encina Infrastructure to Leverage

| Component | Location | Usage in this feature |
|-----------|----------|-----------------------|
| `IRequestContext.TenantId` | `src/Encina/Abstractions/IRequestContext.cs:125` | The tenant of the scope |
| `RequestContextModuleExtensions` | `src/Encina/Modules/RequestContextModuleExtensions.cs` | The module of the scope today |
| `ValidationOrchestrator`, `IValidationProvider` | `src/Encina/Validation/` | Where the culture scope and the validators run (singleton or scoped) |
| `IRequestValidator<T>`, `RequestValidatorRunner` | #230 (planned) | The wrapper wraps these |
| `IScopedRequestValidator<T>`, `ValidatorScope`, `ValidatorScopeSelector`, `AddEncinaValidatorScoping()` | #3982 (planned) | The wrapper implements and the runner uses them |
| `IValidationCultureResolver`, `IValidationMessageLocalizer` | #233 (planned) | The decorators wrap these |
| `AddEncinaSchema<TRequest>` | #235 (planned) | The scoped overload |
| `TenantTelemetry.ValueFor` | #2676 (planned) | The tenant tag, used by #726 |

### Event ID Allocation

| Package | Range | Notes |
|---------|-------|-------|
| `Encina` (core) | `12350-12399` (`ValidationScoping`), ids 12350-12353 used | New range, area Core and messaging, first free 50-id slot after `ValidationAudit` 12300-12349 (the plan of #3978), 12200-12249 (#340) and 12250-12299 (#128, #296, #317, #392); registered in S1b with its registry rows |

### Estimated File Count

| Category | Files | Notes |
|----------|-------|-------|
| S1a (scope, key, errors) | 9 (3 production, `PublicAPI.Unshipped.txt`, manifest, 4 tests) | Guard targets are set from what a guard test can reach: argument checks and the `Left` returns for blank input (the whole file is the coverable base, so a file with one check cannot reach 90). Basis: `Validation/ValidationOrchestrator.cs` in `.github/coverage-manifest/Encina.json` carries a guard target backed by real null checks on two public methods; these files have fewer. Re-measure with the commands of `AGENTS.md` section 9 when S1a opens and set the measured value if lower. Targets: `ValidationScope.cs` unit 95 guard 50 property 0 (one factory and a null check; FsCheck adds nothing); `ValidationScopeKey.cs` unit 100 guard 40 property 90 (every branch plus the injectivity property; guard reaches only the blank-input returns); `ValidationScopeErrors.cs` unit 100 guard 0 (constant factories, no validated parameter) |
| S1b (selector, options, log) | 12 (4 production, 4 registration and registry files, manifest, 3 tests: selector unit, registration unit, guard) | Targets: `ValidationScopeSelector.cs` (selector, builder and selection) unit 100 guard 50 (the builder's null and blank checks); `ValidationScopingOptions.cs` unit 95 guard 40; `ValidationScopingServiceCollectionExtensions.cs` unit 95 guard 60 (null `services`); `ValidationScopingLog.cs` unit 90 guard 0 (generated bodies for 12350 and 12351, reached through the registration test) |
| S2 (validators, after #230 and #3982 S1) | 6 (2 production, PublicAPI, manifest, unit, guard, integration; the contract test belongs to #3982) | `ScopedRequestValidator.cs` unit 100 guard 60 integration 80; `RequestValidatorScopeExtensions.cs` unit 95 guard 60 |
| S3 (culture and resources, after #233) | 10 (3 production, 1 log edit, PublicAPI, manifest, unit x2, guard, integration) | `ScopedValidationCultureResolver.cs` unit 95 guard 0 (internal, constructor arguments validated in the registration test); `ScopedValidationMessageLocalizer.cs` unit 95 guard 0; `ValidationScopingCultureExtensions.cs` unit 95 guard 60; `ValidationScopingLog.cs` (12352 and 12353) unit 95 |
| S4 (schemas, after S2, #235 and #3982 S1) | 5 (1 production edit, PublicAPI, manifest, unit, guard) | `SchemaServiceCollectionExtensions.cs` keeps its own targets, raised to unit 95 guard 60 |
| S5 (documentation) | 4 (feature page, ADR, `INVENTORY.md`, changelog fragment) | Documentation phase |
| Total | 46 files over six pull requests (9, 12, 6, 10, 5, 4) | At most 15 files and 900 lines each |

---

## Combined AI Agent Prompts

<details>
<summary><strong>Combined prompt for all phases</strong></summary>

```text
PROJECT CONTEXT: Encina (.NET 10, C# 14, pre-1.0). Issue #3981: tenant- and module-scoped validation resources, to be built after #230 (IRequestValidator<T>), #233 (localization) and #235 (schemas) exist. The three validation providers share one ValidationOrchestrator, a singleton under DataAnnotations and MiniValidator and scoped under FluentValidation.
IMPLEMENTATION OVERVIEW: (1) Core primitives in src/Encina/Validation/Scoping: ValidationScope, ValidationScopeKey (injective, "default" reserved), ValidationScopeErrors, ValidationScopeSelector<TChoice> and its builder, options, AddEncinaValidationScoping, log 12350-12353 in the new range ValidationScoping (12350, 12399). (2) ScopedRequestValidator<TRequest> (a wrapper implementing IScopedRequestValidator<T> of #3982) and scoped AddRequestValidator and AddEncinaSchema overloads; never redefine ValidatorScope, ValidatorScopeKinds, ValidatorScopeSelector or AddEncinaValidatorScoping, which #3982 owns. (3) ScopedValidationCultureResolver and ScopedValidationMessageLocalizer. (4) Contract, integration and property tests; documentation and ADR.
KEY PATTERNS: The selection always carries the global choice; a scoped validator only adds rules; a missing or reserved tenant selects global and skips nothing. Keys are length-prefixed and injective. Frozen at startup, thread-safe, no scoped dependency. Error codes validation.scope_* with explicit ErrorCategory. Logs carry codes and counts, never a tenant or module id. TryAdd registrations proven with ValidateOnBuild and ValidateScopes. Design Choices 1 to 4 are Dilemmas recommended and pending the maintainer: shared primitives, additive scopes, startup configuration, most-specific-first precedence.
REFERENCE FILES: src/Encina/Validation/ValidationOrchestrator.cs, src/Encina/Abstractions/IRequestContext.cs, src/Encina/Modules/RequestContextModuleExtensions.cs, src/Encina/Diagnostics/EventIdRanges.cs, docs/engineering/eventid-block-registry.md, the plans of #230, #233, #235, #236 and #3982 (by issue number).
```

</details>

---

## Cross-Cutting Integration Matrix

| # | Function | Status | Notes |
|---|----------|--------|-------|
| 1 | Caching | ❌ | Selection is an in-memory lookup in a frozen registry; cache only if a benchmark shows a cost. |
| 2 | OpenTelemetry | ⏭️ | #726 instruments the orchestrator centrally; the selection result becomes an attribute there, and the tenant tag comes from the helper of #2676 (ADR-054 D4). |
| 3 | Structured Logging | ✅ | Four `[LoggerMessage]` ids in the new core range `ValidationScoping` (12350-12399): reserved tenant rejected at registration (12350), registration conflict (12351), scoped choice selected (12352) and global fallback used (12353, the last two added in S3 and logged by the decorators); counts and codes only. |
| 4 | Health Checks | ❌ | In-process configuration, no checkable dependency. |
| 5 | Validation | ✅ | This is a validation feature in core `Encina.Validation`, consumed by the three providers. |
| 6 | Resilience | ❌ | No external call. |
| 7 | Distributed Locks | ❌ | The frozen registry is read-only after startup; no shared mutable state. |
| 8 | Transactions | ❌ | Validation runs before the handler; selection touches no store. |
| 9 | Idempotency | ❌ | Selection is deterministic for a context. |
| 10 | Multi-Tenancy | ✅ | The tenant of the request context selects locale, schema and validators; keys are injective and `default` is reserved. |
| 11 | Module Isolation | ✅ | The module of the request context narrows the selection (tenant and module, tenant, module, global). |
| 12 | Audit Trail | ⏭️ | Auditing of rejected requests is #3978; a selection decision is not an auditable event. |

---

## Prerequisites & Dependencies

| Item | Kind | Why |
|------|------|-----|
| None for S1a and S1b | - | The primitives stand alone and can ship first |
| #3982 S1 (`ValidatorScope`, `IScopedRequestValidator<T>`, `ValidatorScopeSelector`, `AddEncinaValidatorScoping()`) | Hard for S2 and S4 | The wrapper implements its interface; the selection is its selector. Order: #3982 S1, then S2, then S4 (#4000 also consumes #3982 and is independent of this plan) |
| #230 (`IRequestValidator<T>`, runner) | Hard for S2 | The wrapper wraps its validators |
| #233 (resolver, localizer, `SupportedCultures`) | Hard for S3 | The decorators wrap its services |
| #235 (`AddEncinaSchema`) | Hard for S4 | The scoped overload |
| S2 of this plan (`ScopedRequestValidator<TRequest>`) | Hard for S4 | The schema overload wraps the schema validator in it; S4 does not compile before S2 merges |
| #1257 (Dilemma DC2, reserved `default` tenant) | Soft | Register row C7b follows it |
| #2442 (`IRequestContext.ModuleId`) | Soft | `GetModuleName()` works today; the typed member replaces it later |
| #2676, #726 | Soft | The tenant tag and the central instrumentation |

---

## Next Steps

1. Open S1a, then S1b; S2 and S3 open as their prerequisites merge (S2 needs #3982 S1 and #230), in any order; S4 opens after S2 has merged (it also needs #3982 S1 and #235); S5 last. Before S2 starts, re-read the merged plans of #3982 and #230 and reconcile any type that changed.
2. Tenant-scoped configuration read from a store at request time (Design Choice 3, option B) is a follow-on that reuses the key and the selector: a cache of the lookup and its invalidation are designed in that work, which starts only when an application needs tenants that change at runtime.
3. Tenant-specific phase 2 rules stay application code in the handler (the domain rules of #236); no hook is planned.
