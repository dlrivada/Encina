# Implementation Plan: Tenant- and Module-Scoped Validation Resources (Locale, Schemas and Validators)

> **Issue**: [#3981](https://github.com/dlrivada/Encina/issues/3981)
> **Type**: Feature (the issue carries `needs-decision`; every open alternative is a Dilemma below, recommended and pending the maintainer)
> **Related**: #233 (localization: owns `IValidationCultureResolver` and the localizer), #235 (schema-first validation: owns `AddEncinaSchema<TRequest>`), #236 (two-phase validation, defers tenant and module validators per phase), #230 (owns `IRequestValidator<T>` and `RequestValidatorRunner`), #726 (central validation instrumentation), #2676 (tenant tag helper), #2442 (typed `ModuleId` on the request context), #3978 (audit of rejected requests, planned apart), ADR-054, ADR-062 (merged: error category contract)
> **Complexity**: Medium (small core primitives, three thin consumers; no persistence)
> **Estimated Scope**: ~2,500 lines in total (~520 production, ~1,600 tests and manifests, ~380 documentation and ADR), in six pull requests of at most 12 files and 900 lines each

---

## Summary

### What exists today (read on 2026-11-24, main `278c164f1`)

- `TenantId` reaches validators only through `IRequestContext.TenantId` (`src/Encina/Abstractions/IRequestContext.cs:125`; `FluentValidationProvider` copies it into `RootContextData`). Nothing under `src/Encina/Validation/` selects a resource by tenant, and validation never reads a module. The module of a request is available as `RequestContextModuleExtensions.GetModuleName` (a metadata key, `src/Encina/Modules/RequestContextModuleExtensions.cs`) and, after #2442, as `IRequestContext.ModuleId`; there is no `IModuleContext` type in `src`.
- None of the consumers exist on `origin/main` yet: `IRequestValidator<T>` and `RequestValidatorRunner` (#230), `IValidationCultureResolver`, `IValidationMessageLocalizer` and `ValidationCultureScope` (#233), `ISchema<T>` and `AddEncinaSchema<TRequest>(schema)` (#235) are all planned. Each of those plans defers the tenant and module part to this issue: #233 row 10 and 11 (tenant locale, module resource sets), #235 register row C9 and rows 10 and 11 (tenant and module schemas), #236 rows 10 and 11 (tenant and module validators per phase), #230 row 10 and 11 (tenant-scoped validator selection).
- #236 has no phase-selection code: its phase 1 is whichever validators run in `ValidationPipelineBehavior`, and phase 2 is handler code. So "validators per phase" reduces to phase 1 validators, which are `IRequestValidator<T>` registrations (#230); a tenant rule in phase 2 stays application code in the handler.
- The orchestrator is a singleton for `AddDataAnnotationsValidation` and `AddMiniValidation` and scoped for `AddEncinaFluentValidation`; anything the selection needs at request time must be immutable and thread-safe.
- Not in `src`: any scope, key or selector type for validation, and any EventId range for it (`EventIdRanges.cs` has none).

### What this plan delivers

One shared way to say "this resource applies to this tenant and module", used by the three features, with the global validation always applying:

1. **Primitives in core `Encina.Validation`** (no dependency on the other plans): `ValidationScope` (tenant, module, read from `IRequestContext`), `ValidationScopeKey` (an injective key, `default` reserved) and `ValidationScopeSelector<TChoice>` (a frozen lookup: most specific first, global default).
2. **Validator and schema filter** (needs #230 and #235): `ScopedRequestValidator<TRequest>` wraps any `IRequestValidator<TRequest>` and runs it only for the scopes it was registered for; `AddRequestValidator<TRequest, TValidator>(scope)` and `AddEncinaSchema<TRequest>(schema, scope)` overloads register it. The global validators always run; a scoped one adds to them.
3. **Locale and resource sets** (needs #233): `ScopedValidationCultureResolver` (a tenant culture, falling back to the ambient resolver) and `ScopedValidationMessageLocalizer` (a module resource set, falling back to the global one for a missing key).
4. **Registration**: `AddEncinaValidationScoping(options => ...)` with `TryAdd*` registrations proven by a `ValidateOnBuild` and `ValidateScopes` test.

An application that registers nothing behaves exactly as before.

- Affected packages: `Encina` (core), `Encina.Validation.Schema` (one overload, from #235). `Encina.FluentValidation`, `Encina.DataAnnotations` and `Encina.MiniValidator` need no code change: the filter sits in the validators the shared runner executes and the culture scope sits in the orchestrator.
- Provider category: Validation (3) (`AGENTS.md` section 5): a contract test proves the three providers select the same validators and the same culture for the same tenant.
- Shared pieces, owned by this plan: `ValidationScope`, `ValidationScopeKey`, `ValidationScopeSelector<TChoice>`, `ScopedRequestValidator<TRequest>`, the error codes `validation.scope_*` and EventIds 12350-12353. It consumes the types owned by #230, #233 and #235 and changes none of them.

---

## Design Choices

<details>
<summary><strong>1. One shared selection concept versus one hook per feature (the issue's Dilemma D1)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) A shared scope, key, selector and filter used by all three features** | One concept and one registration to learn; one place that owns the injective key and the reserved `default` segment; one contract test for the three providers | Needs #230, #233 and #235 to exist; the shared API must serve a lookup (culture) and a filter (validators) |
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

### Register of defaults and semantics

| Id | Choice | Class | Settled by |
|----|--------|-------|------------|
| C5 | The primitives live in core `Encina.Validation` next to `IValidationProvider` | Trivial | The layout of `src/Encina/Validation/` |
| C6 | A missing tenant or module context selects the global choice; the selection always carries the global choice, so no consumer can return a weaker one; the global validation is never skipped | Settled | AGENTS.md section 3 (compliance and security gates fail closed when context is missing); the requirement text of #3981 |
| C7 | The key is `t<len>:<tenant>` or `-` for no tenant, `m<len>:<module>` or `-` for no module, and `k<len>:<key>`, joined with `\|`; every segment is length-prefixed or the single marker, so no two (tenant, module, key) triples share a key (injective). The tenant `default` (ordinal, ignoring case) is reserved for the no-tenant case: `ValidationScopeKey.Create` returns `validation.scope_reserved_tenant` for it, and at request time a context tenant `default` selects the global choice | Settled | The requirement text of #3981 (injective keys, `default` reserved); AGENTS.md section 3 (fail closed) |
| C8 | New error codes, all `ErrorCategory.InvalidRequest` (ADR-062 R3: a blank or reserved id the module refuses is `InvalidRequest`) except the duplicate registration: `validation.scope_invalid_key` (blank tenant, module or key), `validation.scope_reserved_tenant`, and `validation.scope_conflict` (two choices for one key; `Internal`, a configuration entry per ADR-062 R1). None reaches a client: they surface at startup as an `OptionsValidationException` and from `Create` as a `Left` | Settled | ADR-062 R1, R3 |
| C9 | Telemetry: logs carry codes and counts, never a tenant or module id; the tenant tag on the central validation instrumentation follows ADR-054 D4 through the helper of #2676 and is owned by #726, so this plan adds no instrument | Settled | ADR-054 D4; AGENTS.md section 3 |
| C10 | The module is read from the request context (`GetModuleName()` today, `ModuleId` after #2442), never from the ambient `IModuleExecutionContext`, which is a scoped `AsyncLocal` and is not set outside the EF Core behavior | Trivial | `src/Encina/Modules/RequestContextModuleExtensions.cs`; `src/Encina/Modules/Isolation/IModuleExecutionContext.cs`; the plan of #2442 |
| C11 | The selector and the filter are immutable after construction and resolve no scoped service, so they are safe in the singleton orchestrator of two providers | Settled | AGENTS.md section 3 (never a scoped dependency in a singleton; `ValidateScopes`) |
| C12 | The filter registers with `TryAddEnumerable` so registering the same scoped validator twice runs it once | Trivial | The rule D7 of the plan of #230 (same `TryAddEnumerable` convention) |

---

## Implementation Phases

### Phase 1: Scope, Key and Error Codes (pull request S1a)

<details>
<summary><strong>Tasks</strong></summary>

1. **Create** `src/Encina/Validation/Scoping/ValidationScope.cs`: `public readonly record struct ValidationScope(string? TenantId, string? ModuleId)` with `static ValidationScope Global`, `bool IsGlobal`, and `static ValidationScope From(IRequestContext context)` (blank values and the tenant `default` become null; the module comes from `context.GetModuleName()`); `From` rejects a null context with `ArgumentNullException`.
2. **Create** `src/Encina/Validation/Scoping/ValidationScopeKey.cs`: `public static class ValidationScopeKey { public static Either<EncinaError, string> Create(string? tenantId, string? moduleId, string key); }` (register rows C7 and C8).
3. **Create** `src/Encina/Validation/Scoping/ValidationScopeErrors.cs`: the three codes and their `EncinaError` factories with explicit categories (register row C8), constant messages.
4. **Modify** `src/Encina/PublicAPI.Unshipped.txt` (`mechanical-fixer`).
5. **Tests**: new `tests/Encina.UnitTests/Validation/Scoping/ValidationScopeTests.cs` and `ValidationScopeKeyTests.cs` (blank and `default` tenants, the exact key format, the error categories); new `tests/Encina.GuardTests/Core/Validation/ValidationScopeGuardTests.cs` (null context, null or blank key, a null tenant or module accepted as a valid result); new `tests/Encina.PropertyTests/Validation/ValidationScopeKeyPropertyTests.cs` (FsCheck: for any two different (tenant, module, key) triples the keys differ, including tenants and keys that contain `:`, `\|` and digits).
6. **Manifest** `.github/coverage-manifest/Encina.json` through `mechanical-fixer`.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 1</strong></summary>

```text
CONTEXT: Encina (.NET 10, C# 14). Issue #3981: tenant- and module-scoped validation resources. Core validation lives in src/Encina/Validation. This phase has no dependency on other plans.
TASK: Add ValidationScope, ValidationScopeKey and ValidationScopeErrors in src/Encina/Validation/Scoping, with unit, guard and property tests.
KEY RULES: The key is injective (length-prefixed segments, a single marker for absent) and the tenant "default" is reserved (ordinal, ignoring case): Create returns a Left with an explicit ErrorCategory (InvalidRequest for blank or reserved, Internal for a conflict), never an exception for business input. A context tenant "default" or blank means no tenant. Constant error messages: no tenant or module id in a message. Update PublicAPI.Unshipped.txt and the manifest through mechanical-fixer.
REFERENCE FILES: src/Encina/Abstractions/IRequestContext.cs, src/Encina/Modules/RequestContextModuleExtensions.cs, src/Encina/Errors/ErrorCategory.cs, docs/architecture/adr/062-error-category-contract.md, tests/Encina.PropertyTests (an existing FsCheck test for the style).
```

</details>

### Phase 2: Selector, Options, Registration and Log (pull request S1b)

<details>
<summary><strong>Tasks</strong></summary>

1. **Create** `src/Encina/Validation/Scoping/ValidationScopeSelector.cs`: `public sealed class ValidationScopeSelector<TChoice>` built by `ValidationScopeSelectorBuilder<TChoice>` (`Add(string? tenantId, string? moduleId, TChoice choice)` returns `Either<EncinaError, ValidationScopeSelectorBuilder<TChoice>>`; a duplicate scope is `validation.scope_conflict`; `Build(TChoice global)` freezes into a `FrozenDictionary<string, TChoice>`); `ValidationSelection<TChoice> Select(ValidationScope scope)` returns the global choice and the most specific scoped one (register rows C6 and Design Choice 4); nothing is cached and nothing is allocated for a global scope.
2. **Create** `src/Encina/Validation/Scoping/ValidationScopingOptions.cs` (the entry list that the consumers extend with their own methods; validated at startup by `IValidateOptions`: no reserved tenant, no conflict) and `ValidationScopingServiceCollectionExtensions.cs`: `AddEncinaValidationScoping(this IServiceCollection services, Action<ValidationScopingOptions>? configure = null)` registers the options and `ValidateOnStart` with `TryAdd*`.
3. **Create** `src/Encina/Validation/Scoping/Diagnostics/ValidationScopingLog.cs`: `internal static partial class`, `[LoggerMessage]` 12350 (Error: reserved tenant in a registration, code only), 12351 (Debug: a scoped choice selected, the kind of scope, no ids), 12352 (Debug: global fallback), 12353 (Error: registration conflict, code only).
4. **Modify** `src/Encina/Diagnostics/EventIdRanges.cs`: `ValidationScoping = (12350, 12399)` (the first free 50-id slot after `ValidationAudit` 12300-12349 of the plan of #3978, #340's 12200-12249 and the process-manager family's 12250-12299); `src/Encina/PublicAPI.Unshipped.txt`, `tests/Encina.UnitTests/Testing/Architecture/EncinaEventIdAllocationTests.cs` (the `Encina` entry) and `docs/engineering/eventid-block-registry.md` (a `reserved` and a `zone` row) follow, mechanical.
5. **Tests**: new `tests/Encina.UnitTests/Validation/Scoping/ValidationScopeSelectorTests.cs` (precedence, global only, missing context, conflict, a scoped choice never replaces the global one in the selection); new `tests/Encina.UnitTests/Validation/Scoping/ValidationScopingRegistrationTests.cs` (the container builds with `ValidateOnBuild` and `ValidateScopes` in both registration orders; an invalid option fails at startup); new `tests/Encina.GuardTests/Core/Validation/ValidationScopeSelectorGuardTests.cs`.
6. **Manifest** `.github/coverage-manifest/Encina.json` through `mechanical-fixer`.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 2</strong></summary>

```text
CONTEXT: Encina issue #3981, phase 2 (after phase 1 merged). The selector is a frozen lookup the consumers share; it runs inside a singleton orchestrator.
TASK: Add ValidationScopeSelector<TChoice> and its builder, ValidationScopingOptions with a startup validator, AddEncinaValidationScoping, the log class (12350-12353) and register EventIdRanges.ValidationScoping (12350, 12399).
KEY RULES: Select always returns the global choice plus the most specific scoped one; a missing or reserved-tenant context returns global only. No scoped service anywhere. Register the EventId range first, map the assembly in EncinaEventIdAllocationTests, add PublicAPI and registry rows, pack the ids with no gaps. Logs carry codes and counts, never a tenant or module id. TryAdd registrations; prove them with a ValidateOnBuild and ValidateScopes test in both orders.
REFERENCE FILES: src/Encina/Validation/Scoping/ (phase 1), src/Encina/Diagnostics/EventIdRanges.cs, docs/engineering/eventid-block-registry.md, tests/Encina.UnitTests/Testing/Architecture/EncinaEventIdAllocationTests.cs.
```

</details>

### Phase 3: Scoped Validators and Schemas (pull request S2, after #230; S4, after #235)

<details>
<summary><strong>Tasks</strong></summary>

1. **S2 create** `src/Encina/Validation/Scoping/ScopedRequestValidator.cs`: `public sealed class ScopedRequestValidator<TRequest> : IRequestValidator<TRequest>`, constructor `(IRequestValidator<TRequest> inner, ValidationScope scope)`; `ValidateAsync` returns `ValidationResult.Success` when the context scope does not match (tenant equal, ordinal, or the registered tenant is null; module likewise) and the inner result otherwise (register row C6: it only adds rules).
2. **S2 create** `src/Encina/Validation/Scoping/RequestValidatorScopeExtensions.cs`: `AddRequestValidator<TRequest, TValidator>(this IServiceCollection, ValidationScope scope, ServiceLifetime lifetime = ServiceLifetime.Scoped)` with `TryAddEnumerable` (register row C12); `PublicAPI.Unshipped.txt` and the manifest through `mechanical-fixer`.
3. **S2 tests**: new unit tests of the filter, a guard test, and a contract test (the three real providers with `AddEncinaRequestValidators`: the same scoped validator applies for tenant A and not for tenant B, under each provider), plus an integration test through `IEncina.Send` (two tenants and one module, no container).
4. **S4 modify** `src/Encina.Validation.Schema/SchemaServiceCollectionExtensions.cs` (from #235): `AddEncinaSchema<TRequest>(this IServiceCollection, ISchema<TRequest> schema, ValidationScope scope)` registers the schema validator wrapped in `ScopedRequestValidator<TRequest>`; PublicAPI, the manifest `Encina.Validation.Schema.json` and unit and guard tests in the same pull request.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 3</strong></summary>

```text
CONTEXT: Encina issue #3981, phase 3. Needs IRequestValidator<T> and RequestValidatorRunner from #230 (S2) and AddEncinaSchema from #235 (S4); phases 1 and 2 are merged.
TASK: Add ScopedRequestValidator<TRequest>, the scoped AddRequestValidator overload and the scoped AddEncinaSchema overload, with unit, guard, contract and integration tests.
KEY RULES: A scoped validator only adds rules: when the scope does not match it returns Success and the global validators run unchanged. Match tenants ordinally; a context with no tenant never matches a tenant-scoped registration. TryAddEnumerable. Singleton-safe (no scoped service held by the wrapper). The contract test runs the same request under FluentValidation, DataAnnotations and MiniValidator.
REFERENCE FILES: docs/plans for #230 and #235 (by issue number), src/Encina/Validation/Scoping/ (phases 1 and 2), tests/Encina.ContractTests/Core/Validation/ValidationPipelineBehaviorContractTests.cs.
```

</details>

### Phase 4: Tenant Culture and Module Resource Sets (pull request S3, after #233)

<details>
<summary><strong>Tasks</strong></summary>

1. **Create** `src/Encina/Validation/Scoping/ScopedValidationCultureResolver.cs`: `internal sealed class ScopedValidationCultureResolver : IValidationCultureResolver`, constructor `(ValidationScopeSelector<string> cultures, IValidationCultureResolver ambient)`; `Resolve(IRequestContext)` returns the matching tenant culture name resolved through the `SupportedCultures` matching of #233 (a name outside the allow-list is never turned into a `CultureInfo`) or the ambient result.
2. **Create** `src/Encina/Validation/Scoping/ScopedValidationMessageLocalizer.cs`: `internal sealed class ScopedValidationMessageLocalizer : IValidationMessageLocalizer`, constructor `(ValidationScopeSelector<IValidationMessageLocalizer> modules, IValidationMessageLocalizer global)`; a module localizer is asked first and the global one answers a key the module does not define.
3. **Create** `src/Encina/Validation/Scoping/ValidationScopingCultureExtensions.cs`: `UseTenantCulture(this ValidationScopingOptions options, string tenantId, string cultureName)` and `UseModuleResources(this ValidationScopingOptions options, string moduleId, Type resourceType)`; the registration decorates the #233 services with `TryAdd*` ordering that keeps an application replacement.
4. **Tests**: unit tests of both decorators, a guard test, and an integration test in a non-default culture under the three providers (the culture scope of #233 makes the native localization follow the tenant culture); PublicAPI and the manifest through `mechanical-fixer`.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 4</strong></summary>

```text
CONTEXT: Encina issue #3981, phase 4. Needs IValidationCultureResolver, IValidationMessageLocalizer and the culture allow-list of #233, plus phases 1 and 2.
TASK: Add the tenant culture resolver, the module resource-set localizer and their registration methods.
KEY RULES: A culture name from configuration is matched against SupportedCultures before any CultureInfo is created; an unsupported name never throws and falls back to the ambient culture. A missing tenant means the ambient culture. A module that defines no key falls back to the global localizer. No scoped service in the decorators.
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
| `AGENTS.md` section 3 | Gates fail closed when context is missing | A missing tenant or module selects the global choice and skips nothing (register row C6) |
| ADR-054 D4 | Tenant tag only when multi-tenancy is on; no baggage | No instrument here; #726 and #2676 own it (register row C9) |
| ADR-062 R1, R3 | Configuration entries are `Internal`; a blank or reserved id is `InvalidRequest` | Register row C8 |
| OWASP ASVS V5.1.3 | Allow-list validation of input | A culture name is matched against `SupportedCultures` before a `CultureInfo` exists (#233) |

### Existing Encina Infrastructure to Leverage

| Component | Location | Usage in this feature |
|-----------|----------|-----------------------|
| `IRequestContext.TenantId` | `src/Encina/Abstractions/IRequestContext.cs:125` | The tenant of the scope |
| `RequestContextModuleExtensions` | `src/Encina/Modules/RequestContextModuleExtensions.cs` | The module of the scope today |
| `ValidationOrchestrator`, `IValidationProvider` | `src/Encina/Validation/` | Where the culture scope and the validators run (singleton or scoped) |
| `IRequestValidator<T>`, `RequestValidatorRunner` | #230 (planned) | The filter wraps these |
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
| S1a (scope, key, errors) | 9 (3 production, `PublicAPI.Unshipped.txt`, manifest, 4 tests) | Targets: `ValidationScope.cs` unit 95 guard 90 property 0 (one factory and a null check; no parameter that FsCheck adds anything to); `ValidationScopeKey.cs` unit 100 guard 90 property 90 (every branch plus the injectivity property); `ValidationScopeErrors.cs` unit 100 guard 0 (constant factories, no validated parameter) |
| S1b (selector, options, log) | 12 (4 production, 4 registration and registry files, manifest, 3 tests: selector unit, registration unit, guard) | Targets: `ValidationScopeSelector.cs` unit 100 guard 90; `ValidationScopingOptions.cs` unit 95 guard 85; `ValidationScopingServiceCollectionExtensions.cs` unit 95 guard 85; `ValidationScopingLog.cs` unit 90 guard 0 (generated bodies reached through the selector tests) |
| S2 (validators, after #230) | 8 (2 production, PublicAPI, manifest, unit, guard, contract, integration) | `ScopedRequestValidator.cs` unit 100 guard 90 contract 80 integration 80; `RequestValidatorScopeExtensions.cs` unit 95 guard 90 |
| S3 (culture and resources, after #233) | 9 (3 production, PublicAPI, manifest, unit x2, guard, integration) | `ScopedValidationCultureResolver.cs` unit 95 guard 0 (internal, constructor arguments validated in the registration test); `ScopedValidationMessageLocalizer.cs` unit 95 guard 0; `ValidationScopingCultureExtensions.cs` unit 95 guard 90 |
| S4 (schemas, after #235) | 5 (1 production edit, PublicAPI, manifest, unit, guard) | `SchemaServiceCollectionExtensions.cs` keeps its own targets, raised to unit 95 guard 90 |
| S5 (documentation) | 4 (feature page, ADR, `INVENTORY.md`, changelog fragment) | Documentation phase |
| Total | 47 files over six pull requests (9, 12, 8, 9, 5, 4) | At most 15 files and 900 lines each |

---

## Combined AI Agent Prompts

<details>
<summary><strong>Combined prompt for all phases</strong></summary>

```text
PROJECT CONTEXT: Encina (.NET 10, C# 14, pre-1.0). Issue #3981: tenant- and module-scoped validation resources, to be built after #230 (IRequestValidator<T>), #233 (localization) and #235 (schemas) exist. The three validation providers share one ValidationOrchestrator, a singleton under DataAnnotations and MiniValidator and scoped under FluentValidation.
IMPLEMENTATION OVERVIEW: (1) Core primitives in src/Encina/Validation/Scoping: ValidationScope, ValidationScopeKey (injective, "default" reserved), ValidationScopeErrors, ValidationScopeSelector<TChoice> and its builder, options, AddEncinaValidationScoping, log 12350-12353 in the new range ValidationScoping (12350, 12399). (2) ScopedRequestValidator<TRequest> and scoped AddRequestValidator and AddEncinaSchema overloads. (3) ScopedValidationCultureResolver and ScopedValidationMessageLocalizer. (4) Contract, integration and property tests; documentation and ADR.
KEY PATTERNS: The selection always carries the global choice; a scoped validator only adds rules; a missing or reserved tenant selects global and skips nothing. Keys are length-prefixed and injective. Frozen at startup, thread-safe, no scoped dependency. Error codes validation.scope_* with explicit ErrorCategory. Logs carry codes and counts, never a tenant or module id. TryAdd registrations proven with ValidateOnBuild and ValidateScopes. Design Choices 1 to 4 are Dilemmas recommended and pending the maintainer: shared primitives, additive scopes, startup configuration, most-specific-first precedence.
REFERENCE FILES: src/Encina/Validation/ValidationOrchestrator.cs, src/Encina/Abstractions/IRequestContext.cs, src/Encina/Modules/RequestContextModuleExtensions.cs, src/Encina/Diagnostics/EventIdRanges.cs, docs/engineering/eventid-block-registry.md, the plans of #230, #233, #235 and #236 (by issue number).
```

</details>

---

## Cross-Cutting Integration Matrix

| # | Function | Status | Notes |
|---|----------|--------|-------|
| 1 | Caching | ❌ | Selection is an in-memory lookup in a frozen registry; cache only if a benchmark shows a cost. |
| 2 | OpenTelemetry | ⏭️ | #726 instruments the orchestrator centrally; the selection result becomes an attribute there, and the tenant tag comes from the helper of #2676 (ADR-054 D4). |
| 3 | Structured Logging | ✅ | Four `[LoggerMessage]` ids in the new core range `ValidationScoping` (12350-12399): reserved tenant rejected at registration, scoped choice selected, global fallback used, registration conflict; counts and codes only. |
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
| #230 (`IRequestValidator<T>`, runner) | Hard for S2 | The filter wraps its validators |
| #233 (resolver, localizer, `SupportedCultures`) | Hard for S3 | The decorators wrap its services |
| #235 (`AddEncinaSchema`) | Hard for S4 | The scoped overload |
| #2442 (`IRequestContext.ModuleId`) | Soft | `GetModuleName()` works today; the typed member replaces it later |
| #2676, #726 | Soft | The tenant tag and the central instrumentation |

---

## Next Steps

1. Open S1a, then S1b; S2, S3 and S4 open as their prerequisites merge, in any order; S5 last.
2. Tenant-scoped configuration read from a store at request time (Design Choice 3, option B) is a follow-on that reuses the key and the selector: a cache of the lookup and its invalidation are designed in that work, which starts only when an application needs tenants that change at runtime.
3. Tenant-specific phase 2 rules stay application code in the handler (the domain rules of #236); no hook is planned.
