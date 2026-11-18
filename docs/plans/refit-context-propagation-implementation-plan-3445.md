# Implementation Plan: Opt-in propagation of correlation id, tenant and idempotency key by the `Encina.Refit` client

> **Issue**: [#3445](https://github.com/dlrivada/Encina/issues/3445)
> **Type**: Feature
> **Complexity**: Medium (6 phases, no provider matrix, 2 pull requests)
> **Estimated Scope**: ~520 lines of production code + ~1,000 lines of tests, manifests and documentation (about 1,500 lines in total over 2 pull requests of 15 and 9 files; the split table is the authority)
> **Family**: modules-3 (this plan has no module dimension; it ships with the family because the issue was routed there). Related by number: #3438 (the inbound idempotency header default), #1164 (persisted request context), #3440 (inbox outcome)

---

## Summary

Add an optional `DelegatingHandler` to the `Encina.Refit` client pipeline that copies three facts of the ambient request context onto the outgoing HTTP request, each behind its own switch: the correlation id (`IRequestContext.CorrelationId`), the tenant (`IRequestContext.TenantId`, off by default) and an idempotency key for `POST` and `PATCH` (`IRequestContext.IdempotencyKey` when present, otherwise generated). Nothing is added unless an application calls the new extension for a client.

**State of `origin/main` (`d523eda92`), verified by reading and grep.**

- `Encina.Refit` has three overloads of `AddEncinaRefitClient<TApiClient>` (`src/Encina.Refit/ServiceCollectionExtensions.cs`), each returning the `IHttpClientBuilder` through the private `WithMethodCapture`, which does `services.TryAddTransient<OutboundMethodCaptureHandler>()` and `AddHttpMessageHandler<OutboundMethodCaptureHandler>()` (line 134). A delegating handler already exists in the package, so the pipeline mechanism is proven; the new handler is a sibling.
- The ambient context is read in exactly one place, `RestApiRequestHandler.AmbientContext()` (line 136), through the optional `IRequestContextAccessor? _contextAccessor`, and only for logs and telemetry (`OutboundTelemetry.Start`/`Complete`, tag `encina.tenant_id`). Nothing in the package writes a header.
- `IRequestContextAccessor` is registered by `RequestIdentityServiceCollectionExtensions` (`TryAddSingleton<IRequestContextAccessor, RequestContextAccessor>()`, line 49) and is a singleton, so a transient `DelegatingHandler` can take it without a lifetime problem.
- The idempotency key is not inherited by nested dispatches: `RequestContext.ForNestedDispatch` sets `IdempotencyKey = null` (`src/Encina/Core/RequestContext.cs`, line 219). A `RestApiRequest` sent from inside another handler therefore sees no key; only an entry-point dispatch (or a dispatch with an explicit context) carries one.
- The inbound header names are `EncinaAspNetCoreOptions.CorrelationIdHeader = "X-Correlation-ID"`, `TenantIdHeader = "X-Tenant-ID"` and `IdempotencyKeyHeader = "X-Idempotency-Key"` (`src/Encina.AspNetCore/EncinaAspNetCoreOptions.cs`, lines 17, 24, 50). `Encina.Refit` does not reference `Encina.AspNetCore`, so the outbound defaults are declared again in this package.
- `RestApiRequestHandler` classifies a failed `POST` or `PATCH` that may have been processed as `outbound.ambiguous` and tells retry layers not to retry it unless the request opts in (class remarks). That policy is untouched here; using the key to allow such retries is out of scope (follow-up issue file `artifacts/issues/refit-ambiguous-retry-with-idempotency-key.md`).
- `Encina.Refit` EventIds 4650-4656 are used (`RestApiRequestHandler`, range `Refit = (4650, 4699)` in `src/Encina/Diagnostics/EventIdRanges.cs`); 4657-4699 are free.
- Tests today: `tests/Encina.UnitTests/Refit/` (5 files), `tests/Encina.GuardTests/Web/Refit/RestApiRequestHandlerGuardsTests.cs`, `tests/Encina.IntegrationTests/Web/Refit/` (3 files, WireMock), `tests/Encina.PropertyTests/Web/Refit/` (2 files). The manifest `.github/coverage-manifest/Encina.Refit.json` has per-file targets for three of its six files.

**Not in scope**: propagating W3C `traceparent`/`tracestate` (the .NET HTTP stack does it when an `Activity` is current; ADR-054 D4 has Encina propagate only those two as trace context); OpenTelemetry baggage (ADR-054 D4 forbids it); module identity over HTTP (the issue's function 11 is not applicable); the ambiguous-failure retry policy (issue file above); the inbound header default decision (#3438).

**Affected packages**: `Encina.Refit` (handler, options, extension, telemetry), `Encina` (one new `OutboundErrorCodes` constant). **Provider category**: none.

---

## Design Choices

<details>
<summary><strong>1. Where the opt-in lives (Dilemma D1)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) A per-client extension `IHttpClientBuilder.AddEncinaContextPropagation(Action<ContextPropagationOptions>? configure = null)`, chained after `AddEncinaRefitClient`; options are named options keyed by the client name** | Each client chooses its own switches, so the tenant can go to an internal service and never to a third party; the three `AddEncinaRefitClient` overloads do not change | One more call per client |
| **B) A flag on each of the three `AddEncinaRefitClient` overloads** | One call | Three overload signatures change and every client of the application gets the same ceremony; the options object has no natural home |
| **C) Global options (`services.AddEncinaRefitContextPropagation(...)`) that apply to every Encina Refit client** | One switch for the application | Forwarding the tenant to every client, including third parties, by one line; contradicts the issue's "tenant is a separate switch, off by default" intent |

### Chosen Option: **A — per-client builder extension** (recommended, pending the maintainer)

### Rationale

A wins when clients differ in trust, which is the normal case for outbound calls (an internal service versus a payment provider). C wins when every client is an internal service of one organisation and a single switch is wanted. The extension registers one transient `ContextPropagationHandler` per client through `AddHttpMessageHandler(sp => ...)`, binding the client's named options, and fails at registration when the client name has no options (never silently inactive).

**Class:** Dilemma

</details>

<details>
<summary><strong>2. Position in the pipeline and stability across retries (Dilemma D2)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) The handler is outermost by registration order (it is added right after `AddEncinaRefitClient`, before any resilience handler the application chains), and the README says to call the extension before `.AddStandardResilienceHandler()`; the generated key is generated once per logical call because the handler runs once per logical call** | No extra state; `HttpClientFactory` runs handlers in registration order, first added is outermost | An application that registers its resilience handler first gets a new key per attempt, and nothing detects it |
| **B) Option A plus the generated key is stored in `HttpRequestMessage.Options` under a private key and read first, so a request object (or an options-preserving copy) that passes the handler again keeps its key whichever side of the retry handler the extension sits on** | Stable in more orderings; one dictionary lookup | A private options key; a copy that drops options still regenerates, which the integration test with a retry handler on each side measures |
| **C) The handler sits inside the resilience handler and generates per attempt** | None | Violates the issue's constraint that retries reuse the same key |

### Chosen Option: **B — outermost, with the key cached in the request options** (recommended, pending the maintainer)

### Rationale

B wins when the application's handler order is not under this package's control, which it never is. A wins if the maintainer prefers no hidden request state. C never wins: the issue states that retries must reuse the key (cross-cutting row 6 of the issue). The integration test registers a retrying handler once before and once after the extension and asserts the same key on every attempt for the supported order; for the other order it asserts what B guarantees and documents the rest in the README.

**Class:** Dilemma

</details>

<details>
<summary><strong>3. Which idempotency key is sent (Dilemma D3)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) The ambient key verbatim when present; for `POST` and `PATCH` with no ambient key, a new GUID in `N` format** | The issue text word for word; one inbound request gives one downstream key | Two different downstream calls made under one inbound key send the same key with different payloads; a downstream that deduplicates by key replays the first answer for the second call (the class of defect #3438 reports for the inbound side) |
| **B) Derived: `base64url(SHA-256("<ambient key>\n<METHOD>\n<absolute path and query>"))` when an ambient key exists, a new GUID otherwise** | Stable for the same logical downstream call across replays of the same inbound request, distinct for distinct calls; no body buffering | Two calls to the same method and URL with different bodies under one inbound key still collide |
| **C) As B plus a digest of the request body** | Collision only for identical calls | Buffers the content of every `POST` and `PATCH` (memory, streaming content); the digest of personal data in a header is derived from it |
| **D) No generation: send only the ambient key** | Smallest | A `POST` with no ambient key goes without protection, which the issue's "generate if not present" rules out |

### Chosen Option: **B — derived key** (recommended, pending the maintainer)

### Rationale

B wins when a handler calls more than one downstream endpoint under a single inbound key, which is common. A wins when the downstream treats the key as an opaque token and a one-to-one mapping from the inbound key is required (the downstream is the application's own service and the integration test must see the inbound key unchanged). C wins when two calls to one URL with different bodies are expected under one key. The hash input never includes the body, so no personal data is derived into a header (`AGENTS.md` section 3, `EncinaError.Message` rule by analogy: only identifiers cross the boundary).

**Class:** Dilemma

</details>

<details>
<summary><strong>4. Header names and their defaults (Dilemma D4)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Configurable per client; defaults equal to the inbound defaults of `Encina.AspNetCore`: `X-Correlation-ID`, `X-Tenant-ID`, `X-Idempotency-Key`** | An Encina service calling an Encina service works with no configuration; one vocabulary in the product | `X-Idempotency-Key` is not the name of the IETF draft (`Idempotency-Key`) that third-party APIs use |
| **B) Configurable; correlation and tenant as in A, idempotency default `Idempotency-Key` (the IETF `Idempotency-Key` HTTP header field draft)** | Works with third-party APIs that follow the draft | Differs from Encina's own inbound default until #3438 is decided |
| **C) Not configurable** | Smallest options class | A downstream with another convention cannot be called |

### Chosen Option: **A — configurable, defaults of `Encina.AspNetCore`** (recommended, pending the maintainer)

### Rationale

A wins while the inbound default is what Encina services exchange; B wins when most clients are third-party APIs. The inbound default itself is the open point of #3438 (`needs-decision`); this plan does not wait for it because the name is a property, and if #3438 renames the inbound default the outbound default changes in the same one-line edit. Header names are validated at registration as HTTP tokens (letters, digits and the characters of RFC 9110 section 5.6.2), so a name can never carry a control character.

**Class:** Dilemma

</details>

<details>
<summary><strong>5. A header the caller already set (Dilemma D5)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) The explicit header wins: the handler adds a header only when the request has none (a Refit `[Headers("Idempotency-Key: ...")]` or a `[Header]` parameter is the caller's statement)** | No surprise for code that sets its own key; a caller can opt out per call | A wrong explicit value is sent as it is |
| **B) The context always overwrites** | The context is the single truth | Silently discards an explicit per-call value |

### Chosen Option: **A — the explicit header wins** (recommended, pending the maintainer)

### Rationale

A wins when a few calls need their own key (a downstream that mints keys per business operation). B wins when the application wants the context to be the only source and treats an explicit header as a defect. The counter of Design Choice 7 records `outcome=already_present` so the case is visible.

**Class:** Dilemma

</details>

<details>
<summary><strong>6. Missing or invalid context (Dilemma D6)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) The tenant switch is fail closed: with `PropagateTenantId` on, a missing or invalid ambient tenant fails the call before it is sent with `OutboundErrorCodes.ContextMissing` (`ErrorCategory.FailedPrecondition`, not transient); the correlation id and the key never fail a call (an absent or invalid value is skipped and counted)** | A tenant-aware downstream never receives a request without a tenant and treats it as its default tenant; matches the fail-closed rule for context-dependent gates | Background work with no tenant cannot use a tenant-propagating client; the handler throws an internal exception that `RestApiRequestHandler` maps to the error |
| **B) Everything is best effort: a missing or invalid value is skipped and counted** | Never blocks a call | The downstream may serve the request under its default tenant, which is cross-tenant exposure |

### Chosen Option: **A — tenant fail closed, the rest best effort** (recommended, pending the maintainer)

### Rationale

A wins when the downstream is multi-tenant and a request without a tenant is a defect (`AGENTS.md` section 3: gates fail closed when context is missing). B wins when the downstream is single-tenant and the switch is only a convenience. Values are validated before use: a value with a control character (RFC 9110 section 5.5), or longer than the option `MaxValueLength` (default 256), is invalid; its text is never logged, only the header name and `outcome=skipped_invalid` (`AGENTS.md` section 3). The tenant segment rules (reserved `default` segment, injective tenant-qualified ids) do not apply: the handler sends the tenant id as given and builds no tenant-qualified identifier.

**Class:** Dilemma

</details>

<details>
<summary><strong>7. Telemetry names</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Extend `OutboundTelemetry` with one counter `encina.outbound.context_propagation` (no `_total` suffix, ADR-054 rule 4; tags `header` = `correlation`/`tenant`/`idempotency`, `outcome` = `added`/`already_present`/`skipped_invalid`/`skipped_missing`) on the existing `Encina` meter, and no new span; the `Encina.Outbound.Send` activity keeps its tags** | One meter and one naming family; bounded tag values; the issue's optional span `encina.refit.context_propagation` is replaced by this counter (a deviation from the issue text) because it adds nothing the existing client span lacks | The counter has no tenant tag (the header is a closed set, the tenant is not a dimension of this counter) |
| **B) A new span `encina.refit.context_propagation` as sketched in the issue** | Matches the issue sketch | A span per call for three header writes; another source name |

### Chosen Option: **A — one counter on the existing meter**

### Rationale

Meter and activity source of the package are `"Encina"` (`src/Encina.Refit/Diagnostics/OutboundTelemetry.cs`); the existing tags never carry a URL, a message or a data-subject identifier, and neither does the new counter. The tenant is a tag only on the existing request counter, where `OutboundTelemetry` already adds `encina.tenant_id` when a tenant exists.

**Class:** Trivial
**Settled by:** `src/Encina.Refit/Diagnostics/OutboundTelemetry.cs` (the `Encina` source and meter, tags without URL or message) and [ADR-054](../architecture/adr/054-observability-conventions-and-trace-propagation.md) rule 4 of its decision list (instrument names are lowercase and dot-separated with no `_total` suffix on counters, which gives the name `encina.outbound.context_propagation`) and D4 (tenant tag only where a tenant exists).

</details>

<details>
<summary><strong>8. Default state and the tenant switch</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Off unless the extension is called; once called, correlation id and idempotency key are on and the tenant is off until `PropagateTenantId = true`** | Pay-for-what-you-use; sending a tenant id to a third party needs a deliberate line | Two steps to propagate the tenant |
| **B) Everything on once the extension is called** | One step | A tenant id reaches a third party by default |

### Chosen Option: **A — opt-in, tenant separately off**

### Rationale

The issue records both as settled. The tenant id is a personal-data-adjacent identifier of the application's customer and data minimisation (GDPR Art. 5(1)(c) and Art. 25, the measure of ADR-054 D4 for baggage) is the reason it is never forwarded by default. ADR-054 D4 ("Encina propagates and persists only `traceparent` and `tracestate`") governs OpenTelemetry baggage and the trace context that Encina itself propagates and stores; it does not govern application headers (`X-Tenant-ID`, `X-Correlation-ID`, `X-Idempotency-Key`) that a client opts into per call chain. The authority for sending those headers is the issue's Settled decisions 1 and 2 (opt-in, and the tenant switch off by default), which keep the tenant out unless the application writes `PropagateTenantId = true`.

**Class:** Settled
**Settled by:** `AGENTS.md` section 1 (pay-for-what-you-use, every feature opt-in), [ADR-054](../architecture/adr/054-observability-conventions-and-trace-propagation.md) D4 (data minimisation, GDPR Art. 5(1)(c) and Art. 25) and the Settled choices 1 and 2 of #3445.

</details>

---

## Implementation Phases

### Phase 1: Options, error code and exception (pull request 1)

> **Goal**: the types that every later phase uses.

<details>
<summary>Tasks</summary>

1. `src/Encina.Refit/ContextPropagationOptions.cs` (public sealed): `string CorrelationIdHeader = "X-Correlation-ID"`, `string TenantIdHeader = "X-Tenant-ID"`, `string IdempotencyKeyHeader = "X-Idempotency-Key"`, `bool PropagateCorrelationId = true`, `bool PropagateTenantId = false`, `bool PropagateIdempotencyKey = true`, `int MaxValueLength = 256`; the methods that get a key are fixed to `POST` and `PATCH` (a list option would be a second switch with no demand). `Validate()` throws `ArgumentException` for a blank or non-token header name and `ArgumentOutOfRangeException` for `MaxValueLength` below 1 (registration time, never per request).
2. `src/Encina/Recoverability/OutboundErrorCodes.cs`: `public const string ContextMissing = "encina.outbound.context_missing";` with XML docs (the tenant switch is on and the ambient tenant is missing or invalid). ADR-062 category of the code: `FailedPrecondition`, set where the error is created with the categorized `EncinaErrors.Create` overload (Phase 2).
3. `src/Encina.Refit/Handlers/ContextPropagationException.cs` (internal sealed): carries only the header name; its message is fixed text.
4. `PublicAPI.Unshipped.txt` of `Encina` (one line) and `Encina.Refit` (options type, members, extension), manifest entries (through `mechanical-fixer`).

</details>

<details>
<summary>Prompt for AI Agents — Phase 1</summary>

```text
CONTEXT:
- Issue #3445; plan docs/plans/refit-context-propagation-implementation-plan-3445.md. D1-D6 are recommended, pending the maintainer: follow the Maintainer Decisions section if it exists.
- Encina is pre-1.0: no compatibility overloads, no [Obsolete].

TASK:
Add ContextPropagationOptions with Validate(), OutboundErrorCodes.ContextMissing and the internal exception, with unit and guard tests.

KEY RULES:
- Header names are validated as HTTP tokens at registration, never per request.
- XML docs on every public member; zero warnings; PublicAPI.Unshipped.txt updated.
- The error code is created with the ADR-062 categorized overload (FailedPrecondition).

REFERENCE FILES:
- src/Encina.Refit/ServiceCollectionExtensions.cs
- src/Encina/Recoverability/OutboundErrorCodes.cs
- src/Encina.AspNetCore/EncinaAspNetCoreOptions.cs
```

</details>

---

### Phase 2: The handler (pull request 1)

> **Goal**: copy the context onto the request, once per logical call.

<details>
<summary>Tasks</summary>

1. `src/Encina.Refit/Handlers/ContextPropagationHandler.cs` (internal sealed, `DelegatingHandler`): constructor `(IRequestContextAccessor? accessor, ContextPropagationOptions options, ILogger<ContextPropagationHandler> logger)` (the handler reads no time, so it takes no `TimeProvider`); `SendAsync` reads `accessor?.RequestContext` once, then: (a) correlation id: add `options.CorrelationIdHeader` when enabled, the value is non-blank, valid and the request has no such header; (b) tenant: when `PropagateTenantId`, a missing or invalid tenant throws `ContextPropagationException` (D6); otherwise the same add rule; (c) idempotency key: only for `HttpMethod.Post` and `HttpMethod.Patch` when enabled: reuse the key cached in `request.Options` (D2), else derive it from the ambient key (D3) or generate a GUID, add it and cache it.
2. `src/Encina.Refit/Handlers/RestApiRequestHandler.cs`: one new `catch (ContextPropagationException)` before the general catch that returns `Failure(OutboundErrorCodes.ContextMissing, ErrorCategory.FailedPrecondition, "Required request context is missing", transient: false)` (a categorized variant of the private `Failure`), and counts it through `OutboundTelemetry`; the error carries no tenant value.
3. Tests: unit (each header on and off, existing header kept, invalid value skipped, tenant missing throws, derived key stable for equal inputs and different for different method or path, a `GET` gets no key, no accessor means nothing added and no failure unless the tenant switch is on); the property test `tests/Encina.PropertyTests/Web/Refit/ContextPropagationPropertyTests.cs` (the derived key is deterministic, 43 characters of base64url, and differs when the ambient key, method or path differs) ships in pull request 2.

</details>

<details>
<summary>Prompt for AI Agents — Phase 2</summary>

```text
CONTEXT:
- Issue #3445. The handler is added by the extension of Phase 3; IRequestContextAccessor is a singleton (RequestIdentityServiceCollectionExtensions.cs:49), so a transient handler may take it.

TASK:
Implement ContextPropagationHandler, the catch in RestApiRequestHandler and their tests.

KEY RULES:
- Read the accessor once per call; never log a header value, only the header name and the outcome.
- A Left is returned, never an exception to the caller: the catch maps the internal exception to the categorized error.
- No synchronous blocking; propagate the CancellationToken.

REFERENCE FILES:
- src/Encina.Refit/Handlers/OutboundMethodCapture.cs
- src/Encina.Refit/Handlers/RestApiRequestHandler.cs
- src/Encina/Core/RequestContext.cs
```

</details>

---

### Phase 3: Registration (pull request 1)

> **Goal**: the per-client opt-in.

<details>
<summary>Tasks</summary>

1. `src/Encina.Refit/HttpClientBuilderExtensions.cs` (public static): `AddEncinaContextPropagation(this IHttpClientBuilder builder, Action<ContextPropagationOptions>? configure = null)`: `ArgumentNullException.ThrowIfNull(builder)`; `builder.Services.AddOptions<ContextPropagationOptions>(builder.Name).Configure(configure ?? (_ => { }))` followed by `Validate()` through `PostConfigure`/`Validate` with `ValidateOnStart`; `builder.AddHttpMessageHandler(sp => new ContextPropagationHandler(sp.GetService<IRequestContextAccessor>(), sp.GetRequiredService<IOptionsMonitor<ContextPropagationOptions>>().Get(builder.Name), sp.GetRequiredService<ILoggerFactory>().CreateLogger<ContextPropagationHandler>()))`; returns the builder.
2. DI test `tests/Encina.UnitTests/Refit/ContextPropagationRegistrationTests.cs`: builds the provider with `ValidateOnBuild` and `ValidateScopes`, resolves the client for two names with different options, with and without `AddEncina()`, and asserts an invalid header name fails at start.
3. Guard tests `tests/Encina.GuardTests/Web/Refit/ContextPropagationGuardTests.cs`: null builder; `configure` null is valid (a valid-result test); handler constructor null `options` and `logger`; `Validate()` blank and non-token header names.

</details>

<details>
<summary>Prompt for AI Agents — Phase 3</summary>

```text
CONTEXT:
- Issue #3445. AddEncinaRefitClient returns IHttpClientBuilder; the extension chains on it (D1). Registration completeness (AGENTS.md section 3): every option type the handler resolves is registered and proven by a DI test with ValidateOnBuild and ValidateScopes.

TASK:
Write the builder extension, the options registration and the DI and guard tests.

KEY RULES:
- Named options keyed by builder.Name; fail at start on invalid options.
- A nullable parameter that accepts null gets a valid-result test, not an exception test.

REFERENCE FILES:
- src/Encina.Refit/ServiceCollectionExtensions.cs
- tests/Encina.UnitTests/Refit/ServiceCollectionExtensionsTests.cs
- tests/Encina.GuardTests/Web/Refit/RestApiRequestHandlerGuardsTests.cs
```

</details>

---

### Phase 4: Cross-Cutting Integration (pull request 1)

> **Goal**: the functions marked Include in the matrix.

<details>
<summary>Tasks</summary>

1. Observability: `OutboundTelemetry.ContextPropagated(string header, string outcome)` adds the counter `encina.outbound.context_propagation` of Design Choice 7 (bounded tag values; `header` and `outcome` are closed sets, no value is ever a tag).
2. Logging: three `[LoggerMessage]` methods on the handler (partial class), EventIds 4657 header added (Debug, header name only), 4658 value skipped as invalid (Debug, header name only), 4659 tenant missing for a tenant-propagating client (Warning, header name only), all inside the existing `Refit = (4650, 4699)` range and packed after 4656; no new range is needed.
3. Multi-tenancy (function 10): the tenant switch of Design Choices 6 and 8.
4. Idempotency (function 9): the key of Design Choice 3; the retry-with-key policy is the follow-up issue file.

</details>

<details>
<summary>Prompt for AI Agents — Phase 4</summary>

```text
CONTEXT:
- Issue #3445; EventIds 4657-4659 inside the existing Refit range (4650-4699), packed with no gap after 4656.

TASK:
Add the counter and the three log messages to the handler and test them with a MeterListener and a fake logger.

KEY RULES:
- Logs and tags carry the header name and the outcome only; never a header value, a URL or an EncinaError.Message.
- ADR-054 D4: no baggage; the tenant only as the existing encina.tenant_id attribute where a tenant exists.

REFERENCE FILES:
- src/Encina.Refit/Diagnostics/OutboundTelemetry.cs
- src/Encina/Diagnostics/EventIdRanges.cs
```

</details>

---

### Phase 5: Testing (pull requests 1 and 2)

> **Goal**: per-flag coverage at the manifest targets.

<details>
<summary>Tasks</summary>

1. **Unit, Guard**: pull request 1 (Phases 1-4). **Property**: pull request 2 (file budget of pull request 1: 15 files).
2. **Integration** (pull request 2): `tests/Encina.IntegrationTests/Web/Refit/ContextPropagationIntegrationTests.cs` on the existing WireMock fixture: a `POST` carries the correlation id and a key, a `GET` carries no key, the tenant header appears only with the switch, a missing tenant with the switch returns `Left(encina.outbound.context_missing)` without a request reaching WireMock, a retrying handler registered after the extension sees one key across attempts, an explicit `[Headers]` value is kept. Real HTTP through `HttpClient`; no database is involved, so the 10-provider rule does not apply.
3. **Contract**: not applicable (no new abstraction with several implementers). **Load** and **Benchmark**: justification files `tests/Encina.LoadTests/Web/Refit/ContextPropagation.md` (the folder is created by the file; `tests/Encina.LoadTests` has no Refit folder today) and `tests/Encina.BenchmarkTests/Encina.Refit.Benchmarks/ContextPropagation.md` (three header writes and one SHA-256 over a short string per `POST`; the hot path of the package is the HTTP call).
4. **Per-file targets** (the unit and guard targets are written into `.github/coverage-manifest/Encina.Refit.json` and `Encina.json` by pull request 1; the property and integration targets of the table are written by the manifest edit of pull request 2, because each flag reaches its target only with its own test files; each has a one-sentence justification; baselines of edited files are measured at work start with `dotnet test tests\Encina.<Flag>Tests --collect "XPlat Code Coverage"` and `.github/scripts/coverage-report.cs`; a target is the measured value or higher):

   | File | Unit | Guard | Contract | Property | Integration | Justification |
   |---|---|---|---|---|---|---|
   | `Handlers/ContextPropagationHandler.cs` | 95 | 70 | 0 | 80 | 85 | Every branch is a unit test against a stub inner handler; guard reaches the constructor checks; the property test proves the derived key; the WireMock scenarios run the real pipeline; contract 0 because there is no abstraction |
   | `ContextPropagationOptions.cs` | 95 | 90 | 0 | 0 | 0 | Defaults and `Validate()` are pure; guard covers blank and non-token names; no property or integration flag adds lines |
   | `HttpClientBuilderExtensions.cs` | 90 | 90 | 0 | 0 | 60 | Registration asserted through the client builder; the WireMock fixture registers real clients |
   | `Handlers/ContextPropagationException.cs` | 100 | 0 | 0 | 0 | 0 | One constructor; guard 0 because the type is internal without `InternalsVisibleTo` for `Encina.GuardTests` and its only parameter is a fixed header name |
   | `Handlers/RestApiRequestHandler.cs` (edit) | 95 | 5 | 0 | 70 | 85 | Existing targets are kept; the new catch is covered by a unit test and the WireMock scenario |
   | `Diagnostics/OutboundTelemetry.cs` (edit) | 95 | 0 | 0 | 60 | 60 | Existing targets are kept; the counter is asserted with a `MeterListener` |
   | `Encina/Recoverability/OutboundErrorCodes.cs` (edit) | 100 | 0 | 0 | 0 | 0 | A constant; asserted with the category test of ADR-062 |

</details>

<details>
<summary>Prompt for AI Agents — Phase 5</summary>

```text
CONTEXT:
- Issue #3445. Tests travel with the pull request that adds the code they cover; the integration test is pull request 2.

TASK:
Write the tests of Phase 5 and the manifest targets with their justifications.

KEY RULES:
- Tests execute real code; Shouldly through Encina.Testing.Shouldly; WireMock through Encina.Testing.WireMock.
- Never swallow an exception thrown by the code under test; no Thread.Sleep.
- Targets never go down.

REFERENCE FILES:
- tests/Encina.IntegrationTests/Web/Refit/RestApiRequestHandlerIntegrationTests.cs
- tests/Encina.UnitTests/Refit/RestApiRequestHandlerMethodCaptureTests.cs
- .github/coverage-manifest/Encina.Refit.json
```

</details>

---

### Phase 6: Documentation & Finalization (pull request 2)

<details>
<summary>Tasks</summary>

1. XML docs on every new public member (done with each phase); `PublicAPI.Unshipped.txt` of `Encina` and `Encina.Refit` (through `mechanical-fixer`).
2. `src/Encina.Refit/README.md` section "Propagating the request context" (through `docs-writer`): the three switches, the handler order rule of D2, the tenant warning, a code sample; the configuration guide gains the same section; `docs/INVENTORY.md` line.
3. `changelog.d/3445-refit-context-propagation.added.md` (through `mechanical-fixer`).
4. `dotnet build --configuration Release` with 0 warnings; `dotnet test` with every flag at its target; CRAP of every changed method at most 10.

</details>

<details>
<summary>Prompt for AI Agents — Phase 6</summary>

```text
CONTEXT:
- Issue #3445 is implemented and tested.

TASK:
Write the documentation set through the specialists that own them.

KEY RULES:
- Pages go through docs-writer; the changelog fragment and PublicAPI lines through mechanical-fixer; never edit CHANGELOG.md; coverage figures are cited with covref markers, never typed.

REFERENCE FILES:
- changelog.d/README.md
- src/Encina.Refit/README.md
- .claude/skills/encina-docs/SKILL.md
```

</details>

---

### Proposed split into pull requests (limit 15 files and 1500 lines each)

| PR | Content | Files | Approx. lines |
|----|---------|------:|--------------:|
| 1 | Options, exception, handler, builder extension, `RestApiRequestHandler` catch, `OutboundTelemetry` counter, `OutboundErrorCodes`, `PublicAPI.Unshipped.txt` x2, manifests x2 (`Encina.json`, `Encina.Refit.json`; unit and guard targets), unit tests of the handler, DI test, guard tests, the existing `RestApiRequestHandlerOutboundTests.cs` (edited) | 15 | 1,150 |
| 2 | Property test, integration test, README, changelog fragment, configuration guide page, `docs/INVENTORY.md` line, 2 justification files, manifest (`Encina.Refit.json`, property and integration targets) | 9 | 550 |

PR 1 builds and passes alone (the extension is public but nothing calls it until an application does); PR 2 only adds tests and documentation. The recount: PR 1 = 7 production files (handler, options, exception, builder extension, `RestApiRequestHandler`, `OutboundTelemetry`, `OutboundErrorCodes`) + 2 PublicAPI + 2 manifests + 3 test files (handler unit, DI, guard) + 1 edited test = 15; PR 2 = property test, integration test, README, changelog fragment, configuration guide, `INVENTORY.md`, 2 justification files, manifest = 9 (the sum of the rows is the authority; the property test already sits in PR 2, so there is no further fallback: a PR 1 that measures over 15 files or 1,500 lines moves the `RestApiRequestHandlerOutboundTests.cs` edit to PR 2).

---

## Research

### Relevant Standards & Specifications

| Reference | What it defines | Use in this feature |
|-----------|-----------------|---------------------|
| `AGENTS.md` sections 1 and 3 | Pay-for-what-you-use; fail closed on missing context; `EncinaError.Message` never reaches logs | Opt-in, tenant fail closed, no values in logs |
| [ADR-054](../architecture/adr/054-observability-conventions-and-trace-propagation.md) D4 | Only `traceparent` and `tracestate` are propagated, no baggage; tenant tag only with multi-tenancy | The handler adds plain headers, never baggage; the .NET stack propagates the trace |
| [ADR-062](../architecture/adr/062-error-category-contract.md) | Every error code has an `ErrorCategory` where it is created | `encina.outbound.context_missing` is `FailedPrecondition` |
| RFC 9110 sections 5.5 and 5.6.2 | Field value and token syntax | Header name and value validation |
| IETF draft "The Idempotency-Key HTTP Header Field" | `Idempotency-Key` for `POST` and `PATCH` | D4 default alternative |
| W3C Trace Context | `traceparent` | Propagated by `HttpClient`, not by this handler |
| GDPR Art. 5(1)(c) and Art. 25 | Data minimisation | Tenant off by default |

### Existing Encina Infrastructure to Leverage

| Component | Location | Usage in this feature |
|-----------|----------|-----------------------|
| `OutboundMethodCaptureHandler`, `WithMethodCapture` | `src/Encina.Refit/Handlers/OutboundMethodCapture.cs`, `ServiceCollectionExtensions.cs` | The pattern of a delegating handler on the client pipeline |
| `RestApiRequestHandler.AmbientContext()` | `src/Encina.Refit/Handlers/RestApiRequestHandler.cs` | The way the ambient context is read |
| `IRequestContextAccessor` (singleton) | `src/Encina/Identity/RequestIdentityServiceCollectionExtensions.cs` | Ambient context source |
| `OutboundTelemetry` | `src/Encina.Refit/Diagnostics/OutboundTelemetry.cs` | Meter and tag vocabulary |
| `OutboundErrorCodes`, `EncinaErrors.Create` (categorized overload) | `src/Encina/Recoverability/OutboundErrorCodes.cs`, `src/Encina/Errors/` | New error code and category |
| `EncinaAspNetCoreOptions` header defaults | `src/Encina.AspNetCore/EncinaAspNetCoreOptions.cs` | Default header names |
| `RequestContext.ForNestedDispatch` | `src/Encina/Core/RequestContext.cs` | Why a nested request has no ambient key |
| WireMock fixtures | `tests/Encina.IntegrationTests/Web/Refit/`, `Encina.Testing.WireMock` | Integration tests |

### Event ID Allocation

| Package | Range | Notes |
|---------|-------|-------|
| `Encina.Refit` | 4657-4659 inside the existing `Refit = (4650, 4699)` | 4650-4656 are used by `RestApiRequestHandler`; ids packed with no gap; 4660-4699 stay free; the allocation test map already lists `Encina.Refit` |

### Estimated File Count

| Category | Files | Notes |
|----------|------:|-------|
| Production (`Encina.Refit`, `Encina`) | 7 | 3 new in `Encina.Refit` plus the extension, 3 edits |
| PublicAPI and manifests | 4 | `Encina`, `Encina.Refit`; `Encina.json`, `Encina.Refit.json` |
| Tests | 7 | unit handler, DI, guard, 1 edited unit (PR 1); property, integration (PR 2); the 2 justification files count with PR 2 |
| Documentation and changelog | 4 | README, configuration guide, `docs/INVENTORY.md` line, changelog fragment (PR 2) |
| **Total** | **24 across 2 pull requests** (15 and 9) | `Encina.Refit.json` is edited in both pull requests and counts in each |

---

## Combined AI Agent Prompts

<details>
<summary><strong>Full combined prompt for all phases</strong></summary>

```text
PROJECT CONTEXT:
- Encina is a pre-1.0 .NET 10 / C# 14 library; Either<EncinaError, T>; no [Obsolete], no compatibility layers.
- Issue #3445: an opt-in DelegatingHandler in Encina.Refit that propagates the correlation id, the tenant (separate switch, off by default) and an idempotency key (POST and PATCH) to the downstream API. Plan: docs/plans/refit-context-propagation-implementation-plan-3445.md; Dilemmas D1-D6 are recommended, pending the maintainer.

IMPLEMENTATION OVERVIEW (2 pull requests):
1. Options, error code, exception, handler, builder extension, telemetry, logs, unit, DI and guard tests (Phases 1-4, 5 partly).
2. Property test, WireMock integration test, README, configuration guide, changelog, justification files (Phases 5-6).

KEY PATTERNS:
- A per-client extension on IHttpClientBuilder with named options; the handler is outermost by registration order and caches the generated key in HttpRequestMessage.Options.
- The key is derived from the ambient key, the method and the path (SHA-256, base64url) and is never derived from the body.
- The tenant switch fails closed with encina.outbound.context_missing (FailedPrecondition); correlation id and key are best effort.
- Logs and tags carry header names and outcomes, never values; no baggage (ADR-054 D4).

REFERENCE FILES:
- src/Encina.Refit/ServiceCollectionExtensions.cs
- src/Encina.Refit/Handlers/OutboundMethodCapture.cs
- src/Encina.Refit/Handlers/RestApiRequestHandler.cs
- src/Encina.Refit/Diagnostics/OutboundTelemetry.cs
- tests/Encina.IntegrationTests/Web/Refit/RestApiRequestHandlerIntegrationTests.cs
```

</details>

---

## Cross-Cutting Integration Matrix

| # | Function | Status | Notes |
|---|----------|--------|-------|
| 1 | Caching | ❌ | Not applicable: header propagation reads no cacheable data |
| 2 | OpenTelemetry | ✅ | Phase 4: counter `encina.outbound.context_propagation` (no `_total` suffix, ADR-054 rule 4), no new span, no baggage (ADR-054 D4) |
| 3 | Structured Logging | ✅ | Phase 4: EventIds 4657-4659 inside the existing Refit range, header names only |
| 4 | Health Checks | ❌ | Not applicable: no new dependency to check |
| 5 | Validation | ✅ | Header names are validated as tokens at registration; values are validated per call (control characters, length) |
| 6 | Resilience | ✅ | D2: the handler is outermost and the key is cached so retries reuse it; the retry policy for ambiguous failures stays unchanged (follow-up issue file `artifacts/issues/refit-ambiguous-retry-with-idempotency-key.md`) |
| 7 | Distributed Locks | ❌ | Not applicable: no shared mutable state |
| 8 | Transactions | ❌ | Not applicable: an outbound HTTP call is outside the unit of work |
| 9 | Idempotency | ✅ | D3: the outbound key for `POST` and `PATCH`; the inbound deduplication of the downstream is not this package's |
| 10 | Multi-Tenancy | ✅ | D6 and Design Choice 8: tenant header behind its own switch, fail closed when enabled and missing; no tenant-qualified id is built, so the reserved `default` segment and injectivity rules do not apply |
| 11 | Module Isolation | ❌ | Not applicable: the module is not propagated over HTTP (the issue records the same) |
| 12 | Audit Trail | ❌ | Not applicable: the receiving service audits; this client records no state change |

---

## Prerequisites & Dependencies

- None blocks the work. #3438 (inbound idempotency header default, `needs-decision`) decides the name `Idempotency-Key` versus `X-Idempotency-Key` for `Encina.AspNetCore`; D4 keeps the outbound name a property so the plan does not wait for it.
- #1164 (persisted request context) and #2442 (module on the request context) do not interact with this plan: the handler reads only `CorrelationId`, `TenantId` and `IdempotencyKey` of the ambient context.
- ADR-062 contract (`#3545`): the categorized `EncinaErrors.Create` overload exists on `main` (`src/Encina/Errors/ErrorCategory.cs`).

---

## Next Steps

1. The maintainer answers D1 to D6 when work on #3445 starts; the answers go into `## Maintainer Decisions` and the markers are removed.
2. Pull request 1, then pull request 2.
3. Open the issue file `artifacts/issues/refit-ambiguous-retry-with-idempotency-key.md` when this plan merges.
