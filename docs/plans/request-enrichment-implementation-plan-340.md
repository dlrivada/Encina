# Implementation Plan: Request Pre/Post Enrichment Pipeline (Context Attributes, Custom Enrichers, Overwrite-Always, Fail-Closed Sources)

> **Issue**: [#340](https://github.com/dlrivada/Encina/issues/340)
> **Type**: Feature (pipeline, developer experience)
> **Related**: #2184 (pipeline stage contract, `PipelineStage.InputPreparation` and `HandlerPreparation`), #2676 (tenant tag helper, ADR-054 D4), ADR-054 (telemetry rules), ADR-062 (`ErrorCategory`), #346 (multi-tenancy, open) and #357 (closed), #351 (audit trail pipeline behavior, closed: the issue's "#394" is a closed security-core issue, not audit), #316 (event enrichment, a different feature), #3089 (compliance never optional, fail closed)
> **Complexity**: Low to Medium (one behavior, six attributes, two interfaces, one cached plan per request type; the care is in what a client-supplied value may do)
> **Estimated Scope**: ~700 lines of production code + ~1,100 lines of tests, in five pull requests of at most 15 files

---

## Summary

### What exists today (read on 2026-11-23, main `ce352001f`)

- `IRequestContext` (`src/Encina/Abstractions/IRequestContext.cs`) is immutable (`With*` methods) and carries `CorrelationId`, `CausationId`, `Identity` (a `RequestIdentity`), `IdempotencyKey`, `TenantId`, `Timestamp` and `Metadata`. `UserId` is an extension property that returns `Identity.UserId` (`src/Encina/Identity/RequestContextIdentityExtensions.cs`), so there is no separate user id that could disagree with the identity the gates authorized. The module name is `context.GetModuleName()` (`src/Encina/Modules/RequestContextModuleExtensions.cs`). Identities are created by entry points only (`EncinaContextMiddleware`, identity scopes), never by application code.
- `IPipelineBehavior<TRequest, TResponse>.Handle(request, context, nextStep, ct)` has a `nextStep` callback **without a request parameter**: the request instance a behavior sees is the instance the handler receives. A behavior can therefore enrich a request only by mutating that instance in place; it cannot hand a modified copy to the next step.
- `IRequestPreProcessor<in TRequest>` and `IRequestPostProcessor<TRequest, TResponse>` already exist (`src/Encina/Abstractions/`), return `ValueTask<Either<EncinaError, Unit>>`, and are registered with `EncinaConfiguration.AddRequestPreProcessor<T>()` / `AddRequestPostProcessor<T>()`, with an optional `PipelineStage`. The post-processor receives the response, so it can mutate it.
- `PipelineStage` (`src/Encina/Pipeline/Ordering/PipelineStage.cs`) already names `InputPreparation = 800` ("prepares the incoming payload before it is validated") and `HandlerPreparation = 1600` ("injecting secrets, final enrichment"). Stage placement is declared but **not yet applied** (phase note of #2184): until the order resolver lands, behaviors run in registration order.
- Nothing populates request properties from context. `Encina.Marten` has `IEventMetadataEnricher` and the issue family has #316 for events; both are unrelated to request enrichment (the grep for `Enrich` in `src/Encina` finds only the pre-processor remark "request context that can be enriched").
- `EncinaContextMiddleware` (`src/Encina.AspNetCore/`) resolves tenant and identity. The tenant header is trusted only when `EncinaAspNetCoreOptions.TrustTenantIdHeader` is set; `IRequestContext.TenantId` is therefore the only tenant value a request may rely on. Core has no `HttpContext`, so headers need an AspNetCore-side source.
- The issue's sketches use `DateTime.UtcNow` (forbidden in production code: time comes from `TimeProvider`, `AGENTS.md` section 3), a `set`-able `Guid CorrelationId` (the context's correlation id is a `string`), `void Enrich(...)` interfaces (operations return `Either`, section 3) and `config.UseRequestEnrichment(...)` (no such method style exists: registrations are `AddEncina*` extensions on `IServiceCollection`).

### What this plan delivers

- **Attributes** (core `Encina`, folder `src/Encina/Enrichment/`): `[EnrichFrom(ContextProperty)]` with `ContextProperty` = `UserId`, `TenantId`, `CorrelationId`, `IdempotencyKey`, `ModuleName`; `[EnrichFromClaim("type")]`; `[EnrichFromHeader("name")]`; `[EnrichWithTimestamp]`; `[EnrichWithGuid]`.
- **Interfaces**: `IRequestEnricher<TRequest>` and `IResponseEnricher<TResponse>`, both returning `ValueTask<Either<EncinaError, Unit>>`; `IEnrichmentHeaderSource` (implemented in `Encina.AspNetCore` over `IHttpContextAccessor`).
- **One behavior** `RequestEnrichmentBehavior<TRequest, TResponse>` at `PipelineStage.InputPreparation` that builds, once per request type, a plan of compiled property setters (`RequestEnrichmentPlan`, internal), applies it, runs the custom enrichers in registration order, calls `nextStep`, and on `Right` runs the response enrichers.
- **Semantics**: every attributed property is **overwritten** (never "only when empty"), a missing source fails the request closed with a `Left` (`encina.enrichment.identity_required`, `encina.enrichment.context_missing`), an unsupported property shape fails closed (`encina.enrichment.invalid_shape`), and nothing reads `DateTime.UtcNow`: timestamps come from `IRequestContext.Timestamp`.
- **Opt-in registration** `services.AddEncinaRequestEnrichment(Action<RequestEnrichmentOptions>? configure = null)` (`AddEncina*` convention, `TryAdd*`); no behavior is registered unless the application calls it (pay-for-what-you-use, `AGENTS.md` section 1).
- **Observability**: a `Meter("Encina.Enrichment")` counter and `[LoggerMessage]` events in a new block (EventId claim below); no user id, tenant id, claim value or header value reaches a tag or a log.
- **Not delivered**: the optional compile-time source generator (the issue marks it optional; ADR-005 also rejects generators for dispatch; recorded as a follow-up issue file).
- Affected packages: `Encina` (core), `Encina.AspNetCore`. Provider category: none (provider-independent).

---

## Design Choices

<details>
<summary><strong>1. Package placement and what lives where</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Attributes, interfaces, plan and behavior in core `Encina`; the header source in `Encina.AspNetCore`** | Matches the issue's "Affected Packages" (core and AspNetCore); request types can carry the attributes without referencing ASP.NET; works in background jobs and message handlers | Core grows by one folder (`Enrichment/`) |
| **B) A new `Encina.Enrichment` package** | Core stays smaller | A new project, csproj, README and manifest for ~15 small files; the issue does not ask for a package |
| **C) Everything in `Encina.AspNetCore`** | Header and claim access is direct | Request types would reference ASP.NET Core; the issue's own alternative 3 (HTTP-only middleware) is rejected because background and message flows need it |

### Chosen Option: **A - Core plus an AspNetCore header source**

### Rationale

- The issue's "Affected Packages" checks exactly `Encina` (core) and `Encina.AspNetCore`; only A satisfies it. Claims need no HTTP: `RequestIdentity.Principal` (`src/Encina/Identity/RequestIdentity.cs`) carries them, so `[EnrichFromClaim]` works in core; only headers need the AspNetCore source.

**Class:** Settled
**Settled by:** #340 "Affected Packages" (Encina core, Encina.AspNetCore); `AGENTS.md` section 1 (pay-for-what-you-use)

</details>

<details>
<summary><strong>2. Which pipeline stage the behavior occupies</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `PipelineStage.InputPreparation` (800)** | Runs before validation, so a validator can require an enriched value and a client-supplied value is overwritten before any validator or later gate reads it; sits after authentication, authorization and compliance, which decide from the context identity | A gate in an earlier stage that reads a request property (rather than the context) still sees the client's raw value |
| **B) `PipelineStage.HandlerPreparation` (1600)** | The stage text names "final enrichment"; validators see the unenriched request | A validator that requires `TenantId` or `CreatedBy` fails on a request the enricher would have filled |
| **C) Outermost (`Observability` or earlier)** | Every step sees enriched values | Enrichment would run for requests later rejected by admission, authentication or authorization, doing work for a denied caller |

### Chosen Option: **A - InputPreparation** (recommended, pending the maintainer)

### Rationale

- A wins when handlers have validators over enriched properties, which is the common case (a `[Required] TenantId`); B wins when the same properties are validated as client input first and enriched only afterwards; C wins if a gate must read enriched properties, which this plan avoids by keeping gates on the context. Placement is declared with `[PipelineStage(PipelineStage.InputPreparation)]` on the behavior; until #2184 applies stages, the registration order of `AddEncinaRequestEnrichment` decides, and the DI test asserts the declared stage through `IEncinaPipelineInspector` (`src/Encina/Pipeline/Ordering/`).

**Class:** Dilemma

</details>

<details>
<summary><strong>3. How a request is changed: overwrite-always, in place, closed on bad shapes</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Overwrite every attributed property in place through a compiled setter; a property with no `set` or `init` accessor, or of an incompatible type, fails the request closed (`encina.enrichment.invalid_shape`, `ErrorCategory.Internal`) and is logged once per type** | A client can never smuggle a `CreatedBy` or `TenantId` past the enricher (over-posting); one rule, one test; no copy is possible anyway because `nextStep` takes no request | The request class must expose a setter or `init`; a record positional property without `{ get; init; }` is rejected |
| **B) Fill only when the property holds its default value** | Lets a client or an offline caller supply `RequestedAtUtc` | A client can supply `CreatedBy` and `TenantId` itself; the enriched value is no longer authoritative |
| **C) Produce a modified copy (`with`) and pass it on** | Immutable requests | Impossible: `RequestHandlerCallback<TResponse>` has no request parameter, so the handler would still receive the original |

### Chosen Option: **A - Overwrite always** (recommended, pending the maintainer)

### Rationale

- A wins for identity-bound values (user, tenant, claim, correlation): the value must come from the trusted context. B wins for a property that is legitimately client-supplied with a default; the attribute then is not used on it (a plain property with no attribute stays untouched, so B's scenario needs no option). C is ruled out by the callback signature. `init` setters are called through a compiled delegate on the setter method, which the runtime allows; the unit test proves it on a `record` with `{ get; init; }`.

**Class:** Dilemma

</details>

<details>
<summary><strong>4. What happens when a source is missing</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Required by default: a missing user or claim (anonymous caller) returns `Left` `encina.enrichment.identity_required` (`ErrorCategory.Unauthenticated`); a missing tenant while `MultiTenancyMarker` is registered, a missing module or a missing header returns `Left` `encina.enrichment.context_missing` (`ErrorCategory.FailedPrecondition`); `Required = false` on an attribute leaves the property `null` and succeeds** | Fails closed like every other gate (`AGENTS.md` section 3); an unauthenticated call never reaches a handler with `CreatedBy = null` | A request type used by anonymous endpoints must say `Required = false` |
| **B) Optional by default: leave the property `null`** | No friction | A store that trusts the property writes a row with no owner or tenant; the failure appears later and elsewhere |

### Chosen Option: **A - Required by default** (recommended, pending the maintainer)

### Rationale

- A wins for compliance-bearing handlers (audit owner, tenant column); B wins for public read endpoints that merely log a user when there is one, which can say `Required = false` explicitly. With multi-tenancy off (no `MultiTenancyMarker`), a missing tenant is not an error: the property is set to `null`, and the reserved `default` tenant segment is never invented. Neither code carries `EncinaError.Message` text of the identity (`AGENTS.md` section 3).

**Class:** Dilemma

</details>

<details>
<summary><strong>5. Custom enrichers and response enrichers: new interfaces or the existing processors</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) New `IRequestEnricher<TRequest>` and `IResponseEnricher<TResponse>`, run by the one enrichment behavior in registration order, `Left` aborts** | Satisfies the issue's acceptance list; the response enricher is keyed by response type, not request type; one place orders and measures all enrichment | Overlaps in shape with `IRequestPreProcessor` and `IRequestPostProcessor` |
| **B) No new interfaces: document `IRequestPreProcessor<T>` at `InputPreparation` and `IRequestPostProcessor<TRequest, TResponse>` for enrichment** | No duplicate surface | The issue's acceptance criteria name the two interfaces; a response enricher cannot be keyed by response type alone |

### Chosen Option: **A - New interfaces** (recommended, pending the maintainer)

### Rationale

- A wins while the acceptance criteria stand; B wins if the maintainer prefers the smallest surface and rewrites the criteria (the attributes and the behavior are unchanged either way). Both enrichers are resolved per request from the scope (`IEnumerable<IRequestEnricher<TRequest>>`), so scoped enrichers are allowed and nothing scoped is injected into a singleton.

**Class:** Dilemma

</details>

<details>
<summary><strong>6. Which headers `[EnrichFromHeader]` may read</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) An explicit allow-list in `RequestEnrichmentOptions.AllowedHeaders`; a header not on the list fails the plan (`invalid_shape`); the tenant, correlation and idempotency headers are refused because those values come from `ContextProperty` (the middleware validated them)** | A header is client-controlled text; the application states which ones it accepts; `TrustTenantIdHeader` cannot be bypassed through the attribute | One more option to configure |
| **B) Any header** | Zero configuration | `[EnrichFromHeader("X-Tenant-ID")]` would copy an unvalidated tenant header into a request, bypassing `TrustTenantIdHeader` and the 403 ambiguity checks of `EncinaContextMiddleware` |

### Chosen Option: **A - Allow-list, reserved headers refused**

### Rationale

- `EncinaContextMiddleware` refuses ambiguous tenant identity with 403; an attribute that reads the raw header would reopen exactly that hole. The reserved names are read from `EncinaAspNetCoreOptions` (`TenantIdHeader`, `CorrelationIdHeader`, `IdempotencyKeyHeader`) at registration.

**Class:** Settled
**Settled by:** `src/Encina.AspNetCore/EncinaContextMiddleware.cs` (403 on conflicting tenant, `TrustTenantIdHeader`); `AGENTS.md` section 3 (gates fail closed)

</details>

### Register of defaults and semantics

| Id | Choice | Class | Basis |
|----|--------|-------|-------|
| C1 | Timestamps: `[EnrichWithTimestamp]` sets `IRequestContext.Timestamp` (`DateTimeOffset`, or `.UtcDateTime` for a `DateTime` property); production code never reads `DateTime.UtcNow`; the issue's sketch is corrected | Settled | `AGENTS.md` section 3 (time from `TimeProvider`); `IRequestContext.Timestamp` docs |
| C2 | `[EnrichWithGuid]` sets `Guid.NewGuid()` (a random id, no time component) | Trivial | #340 text (`[EnrichWithGuid]` gives `Guid.NewGuid()`) |
| C3 | `ContextProperty.UserId` is `context.UserId` (the identity's user id), `CorrelationId` is the context's `string`, so the property type must be `string`; a `Guid` target is `invalid_shape` | Settled | `src/Encina/Identity/RequestContextIdentityExtensions.cs`; `IRequestContext.CorrelationId` |
| C4 | New error codes use the `encina.enrichment.*` prefix: `identity_required` is `Unauthenticated`, `context_missing` is `FailedPrecondition`, `invalid_shape` is `Internal` (ADR-062 categories) | Settled | ADR-062; `src/Encina/Errors/ErrorCategory.cs` |
| C5 | Telemetry: tenant tag only through the helper of #2676 when multi-tenancy is on (PR E3 adds it when #2676 has merged); no user id, claim value, header value or `EncinaError.Message` in any tag or log; instrument names lowercase and dotted with no `_total` | Settled | ADR-054 D4 and settled points 2 and 4; `AGENTS.md` section 3 |
| C6 | No tenant-qualified id is built here, so the reserved `default` tenant segment and injectivity do not apply | Settled | ADR-054 D4 |
| C7 | Plans are cached per request type in a `ConcurrentDictionary<Type, RequestEnrichmentPlan>` (static per-generic-type caching, as `ConsentRequiredPipelineBehavior` does); the custom enrichers are resolved per request | Trivial | `src/Encina.Compliance.Consent/ConsentRequiredPipelineBehavior.cs` (attribute cache); `AGENTS.md` section 3 |
| C8 | The optional source generator is out of scope; the compiled-setter plan is the runtime path | Settled | Issue acceptance ("optional"); ADR-005 (no generators for dispatch) |

---

## Implementation Phases

### Delivery order and size

Five pull requests, each building and passing alone with its own tests; counts include tests, PublicAPI, manifests, README and changelog fragment (table in Research).

| PR | Phases | Content | Files |
|----|--------|---------|-------|
| E1 | 1 | Attributes, `ContextProperty`, error codes, `RequestEnrichmentPlan` | 12 |
| E2 | 2 | Interfaces, options, behavior, registration | 13 |
| E3 | 3 | Diagnostics, logging, EventId range | 10 |
| E4 | 4 | AspNetCore header source | 9 |
| E5 | 5, 6 | Pipeline integration and property proof, documentation | 6 |

### Phase 1: Attributes, Error Codes and the Cached Plan (pull request E1)

<details>
<summary><strong>Tasks</strong></summary>

1. **Create** in `src/Encina/Enrichment/`: `ContextProperty.cs` (`public enum`: `UserId`, `TenantId`, `CorrelationId`, `IdempotencyKey`, `ModuleName`); `EnrichFromAttribute.cs`, `EnrichFromClaimAttribute.cs`, `EnrichFromHeaderAttribute.cs`, `EnrichWithTimestampAttribute.cs`, `EnrichWithGuidAttribute.cs` (each `[AttributeUsage(AttributeTargets.Property)]`, `sealed`, with `bool Required { get; init; } = true` on the three source attributes; XML docs name the overwrite-always rule).
2. **Create** `EnrichmentErrorCodes.cs` (`public static class`, constants `encina.enrichment.identity_required`, `context_missing`, `invalid_shape`) and `RequestEnrichmentPlan.cs` (`internal sealed`): `static RequestEnrichmentPlan For(Type requestType)` reads the attributes of the type's public instance properties, validates the shape (setter or `init` present, type compatible with the source: `string` for user, tenant, claim, header, correlation, idempotency key and module; `DateTime` or `DateTimeOffset` for timestamps; `Guid` or `string` for guid), and compiles one setter delegate per property; `ApplyAsync(object request, IRequestContext context, IEnrichmentHeaderSource? headers)` is added in E2 (E1 builds and tests the plan without applying it).
3. **Modify** `src/Encina/PublicAPI.Unshipped.txt` (one line per public member) and `.github/coverage-manifest/Encina.json` (per-file entries for the new files with the targets of the table in Research), both through `mechanical-fixer`.
4. Tests: new `tests/Encina.UnitTests/Core/Enrichment/RequestEnrichmentPlanTests.cs` (plan per shape: `set`, `init`, record, no setter rejected, wrong type rejected, `Guid` target for a string property rejected, two attributes on one property rejected, cache returns the same plan); new `tests/Encina.GuardTests/Core/Enrichment/RequestEnrichmentPlanGuardTests.cs` (null type; attribute constructors reject null or whitespace names).

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 1</strong></summary>

```text
CONTEXT:
Encina (.NET 10, C# 14, pre-1.0). Issue #340, plan docs/plans/request-enrichment-implementation-plan-340.md, phase 1.
Core `Encina` already has IRequestContext (immutable), RequestIdentity, PipelineStage. Request instances are mutated in place
because IPipelineBehavior's nextStep has no request parameter.

TASK:
1. Create the five attributes, ContextProperty and EnrichmentErrorCodes in src/Encina/Enrichment/.
2. Create the internal RequestEnrichmentPlan that validates shapes and compiles setters (init setters included).
3. PublicAPI and manifest entries through mechanical-fixer. Unit and guard tests with real request types.

KEY RULES:
- Attributes validate their name arguments (null or whitespace throws ArgumentException/ArgumentNullException).
- An unsupported shape is an error value later (invalid_shape), never a silent skip.
- No DateTime.UtcNow anywhere; no new IDs outside the EventId claim of the plan.

REFERENCE FILES:
- src/Encina/Abstractions/IRequestContext.cs, src/Encina/Identity/RequestContextIdentityExtensions.cs
- src/Encina.Compliance.Consent/ConsentRequiredPipelineBehavior.cs (attribute and property caching)
```

</details>

---

### Phase 2: Interfaces, Options, Behavior and Registration (pull request E2)

<details>
<summary><strong>Tasks</strong></summary>

1. **Create** in `src/Encina/Enrichment/`: `IRequestEnricher.cs` (`ValueTask<Either<EncinaError, Unit>> Enrich(TRequest request, IRequestContext context, CancellationToken cancellationToken)`), `IResponseEnricher.cs` (same shape over `TResponse response`), `IEnrichmentHeaderSource.cs` (`bool TryGetHeader(string name, out string? value)`; no implementation in core, so a request type that uses `[EnrichFromHeader]` with none registered fails closed `context_missing`), `RequestEnrichmentOptions.cs` (`IReadOnlyCollection<string> AllowedHeaders`, `bool EnableAttributeBasedEnrichment = true`, `Add...` registration helpers for enrichers), `RequestEnrichmentBehavior.cs` and `EnrichmentServiceCollectionExtensions.cs` (`AddEncinaRequestEnrichment`).
2. `RequestEnrichmentBehavior<TRequest, TResponse>` (`[PipelineStage(PipelineStage.InputPreparation)]`, `IPipelineBehavior<TRequest, TResponse> where TRequest : IRequest<TResponse>`): constructor `(IOptions<RequestEnrichmentOptions> options, IEnumerable<IRequestEnricher<TRequest>> requestEnrichers, IEnumerable<IResponseEnricher<TResponse>> responseEnrichers, IServiceProvider services)` (the header source is resolved with `GetService`, never required); `Handle` applies the plan (overwrite always), runs request enrichers (a `Left` aborts before `nextStep`), calls `nextStep`, and on `Right` runs response enrichers (a `Left` fails the request, the handler result is not returned: errors are never swallowed).
3. `AddEncinaRequestEnrichment` registers the options (`AddOptions`, validator for blank header names) and the behavior with `services.TryAddEnumerable(ServiceDescriptor.Scoped(typeof(IPipelineBehavior<,>), typeof(RequestEnrichmentBehavior<,>)))`, the idiom of `AddEncinaAuthorization` (`src/Encina.AspNetCore/ServiceCollectionExtensions.cs` line 207); it registers nothing else. Header-source registration stays with `Encina.AspNetCore` (E4).
4. **Modify** `RequestEnrichmentPlan.cs` (add `ApplyAsync`), `src/Encina/PublicAPI.Unshipped.txt`, `.github/coverage-manifest/Encina.json`, and add `changelog.d/340-request-enrichment.added.md` (the three through `mechanical-fixer`).
5. Tests: new `RequestEnrichmentBehaviorTests.cs` (each attribute with a real `IEncina` built by `AddEncina`; overwrite of a client value; anonymous caller fails `identity_required`; multi-tenancy marker with no tenant fails `context_missing`, without the marker the tenant is `null` and the call succeeds; `Required = false`; enricher `Left` aborts; response enricher after `Right` only; timestamp equals `context.Timestamp` under `FakeTimeProvider`); new `RequestEnrichmentRegistrationTests.cs` (provider built with `ValidateOnBuild` and `ValidateScopes`; the behavior is absent without the call; stage is `InputPreparation` through `IEncinaPipelineInspector`); new `RequestEnrichmentBehaviorGuardTests.cs` (null options, null enrichers, null request, null context, null `nextStep`).

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 2</strong></summary>

```text
CONTEXT:
Issue #340 plan docs/plans/request-enrichment-implementation-plan-340.md, phase 2. Phase 1 merged (attributes, plan).

TASK:
1. IRequestEnricher, IResponseEnricher, IEnrichmentHeaderSource, RequestEnrichmentOptions.
2. RequestEnrichmentBehavior at PipelineStage.InputPreparation; overwrite always; Left on a missing source.
3. AddEncinaRequestEnrichment (TryAdd*), PublicAPI, manifest and changelog through mechanical-fixer.
4. Unit, DI and guard tests as listed in the plan.

KEY RULES:
- Operations return Either<EncinaError, T>; never throw for business failures; EncinaError.Message is never logged or tagged.
- Never inject a scoped service into a singleton; resolve enrichers per request from the scope.
- Missing context fails closed; the tenant is never invented ("default") when multi-tenancy is off: it stays null.

REFERENCE FILES:
- src/Encina/Abstractions/IPipelineBehavior.cs, IRequestPreProcessor.cs, src/Encina/Pipeline/Steps/PreProcessorStep.cs
- src/Encina/Core/ServiceCollectionExtensions.cs, src/Encina/Core/EncinaConfiguration.cs
```

</details>

---

### Phase 3: Observability (pull request E3)

<details>
<summary><strong>Tasks</strong></summary>

1. **Create** `src/Encina/Enrichment/EnrichmentDiagnostics.cs` (`internal static`; `Meter("Encina.Enrichment", "1.0")`; `Counter<long>` `encina.enrichment.applications` with tags `encina.enrichment.outcome` (`applied`, `denied`, `invalid_shape`) and `encina.request.type`; no tenant, user, claim or header value; the tenant tag is added through the helper of #2676 once it has merged) and `EnrichmentLog.cs` (`[LoggerMessage]` partial class, EventIds from the claim below: plan built, shape rejected, source missing; arguments are the request type name and the error code only).
2. **Modify** `RequestEnrichmentBehavior.cs` to record the outcome with `Instrument.Enabled` guards; **modify** `src/Encina/Diagnostics/EventIdRanges.cs` (register `RequestEnrichment`), `docs/engineering/eventid-block-registry.md` (the row of the claim becomes `registered`), `tests/Encina.UnitTests/Testing/Architecture/EncinaEventIdAllocationTests.cs` (the `["Encina"]` entry gains `nameof(EventIdRanges.RequestEnrichment)`), `src/Encina/PublicAPI.Unshipped.txt` (the new field) and `.github/coverage-manifest/Encina.json`.
3. Tests: new `EnrichmentDiagnosticsTests.cs` (a `MeterListener` sees the counter and the three outcomes; no tag value equals a user id or tenant id used in the test), edit `RequestEnrichmentBehaviorTests.cs` for the log events through a fake logger; run `EventIdRegistryAgreementTests` and `EncinaEventIdAllocationTests`.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 3</strong></summary>

```text
CONTEXT:
Issue #340 plan docs/plans/request-enrichment-implementation-plan-340.md, phase 3. Phases 1 and 2 merged.

TASK:
1. EnrichmentDiagnostics (Meter Encina.Enrichment) and EnrichmentLog ([LoggerMessage]) inside the claimed EventId block.
2. Register the range in EventIdRanges.cs, the registry row and the AssemblyRanges map of the allocation test.
3. Tests for instruments, tags and log events.

KEY RULES:
- ADR-021: register the range before using any EventId; pack ids sequentially.
- ADR-054: lowercase dotted names, no _total, no user id, tenant id, claim or header value in a tag or log.

REFERENCE FILES:
- src/Encina/Diagnostics/EventIdRanges.cs, docs/engineering/eventid-block-registry.md
- tests/Encina.UnitTests/Testing/Architecture/EncinaEventIdAllocationTests.cs
```

</details>

---

### Phase 4: The AspNetCore Header Source (pull request E4)

<details>
<summary><strong>Tasks</strong></summary>

1. **Create** `src/Encina.AspNetCore/Enrichment/HttpContextEnrichmentHeaderSource.cs` (`public sealed`, `IEnrichmentHeaderSource`, constructor `(IHttpContextAccessor accessor)`; returns `false` when there is no `HttpContext` or the header is absent; a header with several values is refused (`false`) so one name never maps to two values).
2. **Modify** `src/Encina.AspNetCore/ServiceCollectionExtensions.cs` (`AddEncinaAspNetCore` registers `AddHttpContextAccessor` if absent and `TryAddSingleton<IEnrichmentHeaderSource, HttpContextEnrichmentHeaderSource>()`; the reserved header names come from `EncinaAspNetCoreOptions` for the allow-list check), `src/Encina.AspNetCore/PublicAPI.Unshipped.txt`, `.github/coverage-manifest/Encina.AspNetCore.json`, `src/Encina.AspNetCore/README.md` (through `docs-writer`), `changelog.d/340-request-enrichment-headers.added.md` (through `mechanical-fixer`).
3. Tests: new `tests/Encina.UnitTests/AspNetCore/HttpContextEnrichmentHeaderSourceTests.cs` (present, absent, multi-value, no `HttpContext`; reserved header refused); new `tests/Encina.GuardTests/AspNetCore/HttpContextEnrichmentHeaderSourceGuardTests.cs` (null accessor, null or blank name); new `tests/Encina.UnitTests/AspNetCore/AspNetCoreEnrichmentRegistrationTests.cs` builds the provider with `ValidateOnBuild` and `ValidateScopes` and resolves the source. `Encina.AspNetCore.csproj` has no `InternalsVisibleTo` entry for `Encina.GuardTests`, so the source class is `public sealed` and needs no csproj edit.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 4</strong></summary>

```text
CONTEXT:
Issue #340 plan docs/plans/request-enrichment-implementation-plan-340.md, phase 4. Phase 2 merged (IEnrichmentHeaderSource).

TASK:
1. HttpContextEnrichmentHeaderSource over IHttpContextAccessor.
2. Register it in AddEncinaAspNetCore with TryAddSingleton; PublicAPI, manifest, README (docs-writer), changelog (mechanical-fixer).
3. Unit, guard and DI tests.

KEY RULES:
- A header is client-controlled: never log or tag its value; the tenant, correlation and idempotency headers are never read here.
- No HttpContext (a background job) returns false so the behavior fails closed.

REFERENCE FILES:
- src/Encina.AspNetCore/EncinaContextMiddleware.cs, EncinaAspNetCoreOptions.cs, ServiceCollectionExtensions.cs
```

</details>

---

### Phase 5: Pipeline Proof and Property Tests (pull request E5)

<details>
<summary><strong>Tasks</strong></summary>

1. **Create** `tests/Encina.IntegrationTests/Core/RequestEnrichmentPipelineIntegrationTests.cs` (the real `AddEncina` plus `AddEncinaRequestEnrichment` with a validator behavior: the validator sees the enriched value; a client-supplied `TenantId` is overwritten; the response enricher output reaches the caller; no database or container is used, so no collection fixture applies) and `tests/Encina.PropertyTests/Core/RequestEnrichmentPropertyTests.cs` (FsCheck: for any client-supplied string values the attributed properties equal the context values after enrichment, and enrichment is idempotent).
2. Manifest `Encina.json` through `mechanical-fixer`.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 5</strong></summary>

```text
CONTEXT:
Issue #340 plan docs/plans/request-enrichment-implementation-plan-340.md, phases 5 and 6. Phases 1 to 4 merged.

TASK:
1. Integration test over the real pipeline and a FsCheck property test for overwrite-always and idempotence.
2. Manifest entries through mechanical-fixer.

KEY RULES:
- Tests execute real package code (no reflection-only asserts); Shouldly through Encina.Testing.Shouldly; FakeTimeProvider for time.

REFERENCE FILES:
- tests/Encina.UnitTests/Core/AddEncinaServiceGraphTests.cs
```

</details>

---

### Phase 6: Documentation and Finalization (inside pull request E5)

<details>
<summary><strong>Tasks</strong></summary>

1. `docs/features/request-enrichment.md` (usage guide: attributes, overwrite-always, required sources, header allow-list, custom enrichers, stage placement) through `docs-writer`; a line in `docs/INVENTORY.md` and `ROADMAP.md` (the entry "Request Enrichment #340") through `docs-writer`.
2. XML docs with examples on every public member (done in each phase); `changelog.d` fragments are added by E2 and E4.
3. Build verification (`dotnet build --configuration Release`, zero warnings); every coverage flag reaches the per-file targets below; no ADR (no new architecture decision: the stage contract is ADR-owned by #2184).

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 6</strong></summary>

```text
CONTEXT:
Issue #340 plan docs/plans/request-enrichment-implementation-plan-340.md, phase 6.

TASK:
1. Ask docs-writer for docs/features/request-enrichment.md and the INVENTORY and ROADMAP lines.
2. Run the build and the tests; report measured per-file coverage against the plan's targets.

KEY RULES:
- Documentation never types coverage figures by hand (covref markers).

REFERENCE FILES:
- docs/features/ (existing guides), .claude/skills/encina-docs/SKILL.md
```

</details>

---

## Research

### Relevant Standards & Specifications

| Source | Provision | Use in this feature |
|--------|-----------|---------------------|
| `AGENTS.md` section 3 | Time from `TimeProvider`; ROP; gates fail closed; `EncinaError.Message` never reaches telemetry | Timestamp source, `Either` interfaces, missing-source behavior, tags |
| ADR-054 | Settled points 2, 4, 7, 8; D4 | Names, privacy, tenant tag through #2676 |
| ADR-062 | `ErrorCategory` contract | Categories of the three new codes |
| ADR-021 | EventId ranges registered first | Claim and registration in E3 |
| #2184 | Stage contract (`InputPreparation`, `HandlerPreparation`) | Behavior placement |
| Wolverine middleware (`wolverinefx.net`) | Context propagation through middleware | The inspiration cited by the issue |

### Existing Encina Infrastructure to Leverage

| Component | Location | Usage in this feature |
|-----------|----------|-----------------------|
| `IRequestContext`, `RequestIdentity` | `src/Encina/Abstractions/IRequestContext.cs`, `src/Encina/Identity/` | Sources of user, tenant, claims, timestamp, correlation |
| `PipelineStage`, `PipelineStageAttribute`, `IEncinaPipelineInspector` | `src/Encina/Pipeline/Ordering/` | Stage declaration and its test |
| `EncinaConfiguration.AddPipelineBehavior`, `AddRequestPreProcessor` | `src/Encina/Core/EncinaConfiguration.cs` | Registration pattern |
| `MultiTenancyMarker` | `src/Encina/Abstractions/MultiTenancyMarker.cs` | Decides whether a missing tenant is an error |
| `ConsentRequiredPipelineBehavior` | `src/Encina.Compliance.Consent/` | Attribute and property cache pattern |
| `EncinaContextMiddleware`, `EncinaAspNetCoreOptions` | `src/Encina.AspNetCore/` | Tenant trust and reserved header names |

### Event ID Allocation

| Package | Range | Notes |
|---------|-------|-------|
| `Encina` (core) | claim `10000-10049` named `RequestEnrichment` (area Core and messaging, first 50-id slot of the 10000 block) | Reserved by this plan; E3 registers it in `EventIdRanges.cs` and the registry (`reserved` to `registered`); packed from 10000, no gaps; three events used, 47 left |
| `Encina.AspNetCore` | none | The header source logs nothing |

### Estimated File Count

| PR | Content | New src | Changed src | New tests | Edited tests | Manifest | Other | Files | Lines (est.) |
|----|---------|---------|-------------|-----------|--------------|----------|-------|-------|--------------|
| E1 | Phase 1 | 8 | 1 (`PublicAPI.Unshipped.txt`) | 2 | 0 | 1 | 0 | 12 | ~650 |
| E2 | Phase 2 | 6 | 2 (`RequestEnrichmentPlan.cs`, `PublicAPI.Unshipped.txt`) | 3 | 0 | 1 | 1 (changelog) | 13 | ~900 |
| E3 | Phase 3 | 2 | 4 (`RequestEnrichmentBehavior.cs`, `EventIdRanges.cs`, `PublicAPI.Unshipped.txt`, registry doc) | 1 | 2 (behavior tests, allocation test) | 1 | 0 | 10 | ~450 |
| E4 | Phase 4 | 1 | 3 (`ServiceCollectionExtensions.cs`, `PublicAPI.Unshipped.txt`, README) | 3 | 0 | 1 | 1 (changelog) | 9 | ~420 |
| E5 | Phases 5, 6 | 0 | 0 | 2 | 0 | 1 | 3 (guide, `INVENTORY.md`, `ROADMAP.md`) | 6 | ~380 |
| **Total** | | **17** | **10** | **11** | **2** | **5** | **5** | **50** | **~2,800** |

Columns sum: 17 + 10 + 11 + 2 + 5 + 5 = 50 = 12 + 13 + 10 + 9 + 6. Test doubles and implementers: the plan changes no interface that has implementers (`IRequestEnricher`, `IResponseEnricher` and `IEnrichmentHeaderSource` are new; `IEnrichmentHeaderSource` has one implementer, in E4); the only `new X(` site of the behavior is the DI registration.

Per-file coverage targets (no per-file entry exists for the new files; the package targets of `Encina.json` are unit 70, guard 20, contract 15, property 80, integration 85 and the plan adds per-file entries above them):

| File | Unit | Guard | Contract | Property | Integration | Justification |
|------|------|-------|----------|----------|-------------|---------------|
| `Enrichment/RequestEnrichmentPlan.cs` | 90 | 40 | 0 | 60 | 0 | Every shape, source kind and rejection is table-driven by unit tests; guard covers the null type; the property test drives the setter paths; no contract beyond the behavior's |
| `Enrichment/RequestEnrichmentBehavior.cs` | 90 | 60 | 0 | 40 | 50 | All outcomes and both enricher kinds are unit tested; guard covers constructor and `Handle` arguments; the property and integration tests run the real pipeline |
| `Enrichment/EnrichmentServiceCollectionExtensions.cs` | 90 | 50 | 0 | 0 | 0 | Registration and its null checks are driven by the DI and guard tests |
| `Enrichment/EnrichmentDiagnostics.cs` | 80 | 0 | 0 | 0 | 0 | Instruments reached by the listener test; internal static with no validated parameter |
| `Enrichment/*Attribute.cs` (five files) | 90 | 70 | 0 | 0 | 0 | Constructors and the `Required` default are executed by unit and guard tests |
| `Encina.AspNetCore/Enrichment/HttpContextEnrichmentHeaderSource.cs` | 90 | 60 | 0 | 0 | 0 | Present, absent, multi-value and no-context paths are unit tested; guard covers the constructor and name |

---

## Combined AI Agent Prompts

<details>
<summary><strong>Full combined prompt for all phases</strong></summary>

```text
You are implementing issue #340 - request pre/post enrichment - in five small pull requests (E1 to E5). E3 waits for nothing, but its
tenant tag waits for #2676; stage placement is applied only when #2184 lands, so the behavior is placed by registration order until then.

PROJECT CONTEXT:
- Encina .NET 10 / C# 14, pre-1.0, no [Obsolete], no compatibility layers. Edit with Edit/Write only; PublicAPI, manifest, changelog
  through mechanical-fixer; docs and READMEs through docs-writer.
- Requests are mutated in place (nextStep has no request parameter). Every attributed property is overwritten; a missing source is a Left.

IMPLEMENTATION OVERVIEW:
1. E1: attributes, ContextProperty, EnrichmentErrorCodes, RequestEnrichmentPlan.
2. E2: IRequestEnricher, IResponseEnricher, IEnrichmentHeaderSource, options, behavior at InputPreparation, AddEncinaRequestEnrichment.
3. E3: Meter and [LoggerMessage] in the claimed EventId block, registered in EventIdRanges.cs and the registry.
4. E4: HttpContextEnrichmentHeaderSource in Encina.AspNetCore, header allow-list.
5. E5: integration and property tests, the guide.

KEY PATTERNS:
- Errors: encina.enrichment.identity_required (Unauthenticated), context_missing (FailedPrecondition), invalid_shape (Internal).
- Time from IRequestContext.Timestamp; never DateTime.UtcNow. No raw tenant, user, claim or header value in tags or logs.
- Guard tests for every new public constructor and method parameter; null optional parameters get valid-result tests.

REFERENCE FILES:
- src/Encina/Abstractions/IPipelineBehavior.cs, IRequestContext.cs, src/Encina/Pipeline/Ordering/PipelineStage.cs
- src/Encina.AspNetCore/EncinaContextMiddleware.cs
```

</details>

---

## Cross-Cutting Integration Matrix

| # | Function | Status | Notes |
|---|----------|--------|-------|
| 1 | Caching | ❌ N/A | Enrichment is per request; only the compiled plan is cached (an internal type cache, not a data cache) |
| 2 | OpenTelemetry | ✅ Phase 3 | `Meter("Encina.Enrichment")` counter by outcome; tenant tag through #2676 when multi-tenancy is on; no user, claim or header value |
| 3 | Structured Logging | ✅ Phase 3 | `[LoggerMessage]` events in the claimed block: request type name and error code only |
| 4 | Health Checks | ❌ N/A | In-process pipeline behavior with no dependency to check |
| 5 | Validation | ❌ N/A | The behavior runs before the validation stage on purpose (Design Choice 2); it adds no validator, and the existing validators already see the enriched request, so the issue's deferral needs no new issue |
| 6 | Resilience | ❌ N/A | No external call; a custom enricher that calls out owns its own resilience |
| 7 | Distributed Locks | ❌ N/A | Stateless |
| 8 | Transactions | ❌ N/A | Runs in the stage before the transaction stage and never commits |
| 9 | Idempotency | ❌ N/A | `ContextProperty.IdempotencyKey` only reads the key; the inbox stage is untouched |
| 10 | Multi-Tenancy | ✅ Phase 2 | `ContextProperty.TenantId` from the context only; required when `MultiTenancyMarker` is registered; never invented as `default` |
| 11 | Module Isolation | ✅ Phase 2 | `ContextProperty.ModuleName` from `GetModuleName()`; missing module is `context_missing` when required |
| 12 | Audit Trail | ❌ N/A | No state change; enrichment never records user or tenant values, and the audit behavior (#351, closed) reads the same context |

---

## Prerequisites & Dependencies

### Required Prerequisites

| Prerequisite | Status | Notes |
|-------------|--------|-------|
| None blocking | - | E1 to E5 build on `main` today; the stage is applied when #2184 lands (registration order until then) |

### Recommended (Not Blocking)

| Dependency | Issue | Notes |
|-----------|-------|-------|
| Pipeline stage contract applied | #2184 | Makes the declared `InputPreparation` placement effective |
| Tenant tag helper | #2676 | E3 adds the tenant tag when it has merged |
| Follow-up: compile-time source generator | issue file `artifacts/issues/request-enrichment-source-generator.md` | The issue's optional criterion; to open when the maintainer wants it |

---

## Next Steps

1. Link this plan from #340 and ask the maintainer to answer Design Choices 2 to 5 when work starts.
2. Implement E1, then E2, E3 and E4 (E3 and E4 are independent after E2), then E5.
3. Maintainer action: amend the acceptance criteria of #340 that name `DateTime.UtcNow`, a `Guid CorrelationId` and `config.UseRequestEnrichment` to the outcome of this plan (this plan never comments on an issue).
