# Implementation Plan: `Encina.BFF` — Backends for Frontends and response aggregation

> **Issue**: [#383](https://github.com/dlrivada/Encina/issues/383)
> **Type**: Feature
> **Complexity**: High (7 phases, new package, no database provider, about 57 files, five pull requests)
> **Estimated Scope**: ~2,000-2,600 lines of production code + ~3,500-4,500 lines of tests

---

## Summary

Add the Backends for Frontends (BFF) pattern to Encina: a request for one client type (web, mobile, IoT) is answered by calling several backend services in parallel and composing one response, all inside the Encina pipeline. The recommended shape (Dilemma D1, option A) is **in-process aggregation**: each backend call is an Encina request dispatched through `IEncina` (typically an `Encina.Refit` request), so validation, caching, resilience, tenancy and authorization apply to every step with no new code, and a new package `Encina.BFF` supplies the fan-out engine, the aggregation contracts, route declaration and endpoint mapping. A reverse-proxy gateway (YARP) stays outside the first delivery unless the maintainer answers D1 with option B.

Facts checked against `origin/main` (`18ec8dd19`):

- There is no `Encina.BFF`, `Encina.BFF.YARP` or `Encina.BFF.Aggregation` project, no `IBffRequestAdapter`, `IResponseAggregator<T>`, `BffRoute` or `AggregateFrom` in `src/`, and `Directory.Packages.props` has no `Yarp.ReverseProxy`.
- `src/Encina.Refit` is the only HTTP client integration: `RestApiRequestHandler<TRequest,TApiClient,TResponse>` turns an HTTP call into `Either<EncinaError,T>` with the outbound taxonomy (`OutboundErrorCodes`: `Timeout`, `RateLimited`, `ServerError`, `NetworkError`, `ClientError`, `UnexpectedResponse`, with transient and ambiguous flags) and records `Encina.Outbound.Send` and `encina.outbound.requests_total`.
- The resilience behaviors exist as pipeline behaviors (`StandardResiliencePipelineBehavior`, `CircuitBreakerPipelineBehavior`, `BulkheadPipelineBehavior` in `Encina.Polly` and `Encina.Extensions.Resilience`), and `QueryCachingPipelineBehavior` with `[Cache]` caches query responses through `ICacheProvider` (`Encina.Caching`).
- `Encina.AspNetCore` provides `AuthorizationPipelineBehavior`, `EncinaContextMiddleware`, `ProblemDetailsExtensions` and an endpoint-mapping precedent (`DPIAEndpointExtensions`); `Encina.Tenancy.AspNetCore` resolves the tenant; `Encina.Testing.WireMock` exists for stub backends.
- Overlap and scope: #199 "BFF Pattern Support (Backend for Frontend)" (open, milestone v0.18.0) proposes the same pattern with `[BFF(ClientType)]` and an `AggregationBuilder`; this issue is in milestone v0.14.0 while the roadmap lists BFF under v0.16.0 (`ROADMAP.md:84`, the milestone table row, and the bullet at `:270`), this issue at `ROADMAP.md:505` and #199 at `ROADMAP.md:663`. The maintainer reconciles them (Dilemma D4). SPEC-000 section 6 is the complete list of 1.0 exclusions and does not list BFF or gateway features.

**Standards**: Backends for Frontends and Gateway Aggregation patterns, RFC 9110 (502, 504), RFC 9457.
**Affected packages**: new `Encina.BFF`; `Encina` (core) for the `EventIdRanges.BFF` registration only; `Encina.AspNetCore` is referenced, not changed.
**Provider category**: none. No database provider is touched; caching of aggregated responses goes through `ICacheProvider` and works with all 8 cache providers without caching-specific code.

---

## Design Choices

<details>
<summary><strong>1. In-process aggregation or reverse-proxy gateway (Dilemma D1)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A — Aggregation as Encina requests**: each backend call is a request (an `Encina.Refit` request or any handler) dispatched through `IEncina`; the BFF fans out, collects and composes | Validation, caching, resilience, tenancy, authorization and telemetry apply to each step through the existing pipeline; one routing model; no new dependency; results are `Either<EncinaError,T>` | Every composed route needs a small aggregator; pure pass-through traffic (header and path rewriting, WebSockets, load balancing) is not served |
| **B — YARP as the gateway** with aggregation steps attached to routes | Header and path transformation, load balancing, health-based routing and WebSocket support for mostly pass-through traffic | A second routing model next to Encina requests; a new dependency (`Yarp.ReverseProxy`); steps run outside the Encina pipeline unless bridged; the issue itself mixes both models (`[AggregateFrom]` on a query record while `ForwardAsync` forwards raw requests) |
| **C — Both in the first delivery** | Covers both audiences | Doubles the surface and the test matrix; the two models need a bridge decided before either is used |

### Chosen Option: **A — Aggregation as Encina requests** (recommended, pending the maintainer)

### Rationale

A wins when each BFF response is a composed model with business rules, which is the case the issue's example shows (`GetOrderDetailsQuery` over order, inventory and payment services), and it reuses what Encina already is. B wins for gateways that mostly proxy and transform traffic; an application that needs one can configure YARP directly, and an `Encina.BFF.YARP` bridge can follow when a real consumer exists. A leaves the YARP acceptance item of the issue out of this delivery; that is the maintainer's call.

**Class:** Dilemma

</details>

<details>
<summary><strong>2. When one backend of an aggregation fails (Dilemma D2)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A — All-or-nothing**: the first failing step fails the aggregate (`Left`) | Simple and consistent; no partial answer can be wrong | One slow or broken service takes the whole screen down |
| **B — Partial**: a failing step is replaced by an absent value and the aggregate succeeds | Availability for dashboards and mobile screens | A partial answer can be wrong for payments or orders; the client must know what is missing |
| **C — All-or-nothing by default, per-step opt-in** (`Optional()` on a step); only an **availability** failure of an optional step is skipped (ADR-062 categories `Unavailable`, `Timeout`, `UpstreamFailure`, `RateLimited`); every other category fails the aggregate; a skipped step is logged, counted and reported in the response metadata the aggregator receives | Correct by default; availability only where the author states it and only for failures that mean "the backend is not there"; the decision is explicit in code | One more concept in the builder |

### Chosen Option: **C — All-or-nothing by default, `Optional()` per step, skipping availability failures only** (recommended, pending the maintainer)

### Rationale

C wins because the right answer differs per step of the same route (a payment step is mandatory, a recommendations step is optional) and a global switch cannot express that. A wins where no step may ever be missing; B wins for read-only dashboards where something is always better than a spinner. The aggregator receives a `ServiceResponse` per step that says whether the step succeeded, so a response model can mark what is absent. An optional step never hides a denial or a caller cancellation: a backend `Unauthenticated` or `Forbidden` (R6), a `Cancelled` (R5), a `Validation` or `InvalidRequest`, and an `Internal` failure fail the aggregate even on an `Optional()` step, so a partial answer can never be the way a denial fails open (AGENTS.md section 3); the property test covers every category.

**Class:** Dilemma

</details>

<details>
<summary><strong>3. Package split (Dilemma D3)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A — One package `Encina.BFF`** (abstractions, aggregation engine, route attributes, endpoint mapping) | No package without a consumer; one README; matches D1 option A, which has no YARP part | The package references `Encina.AspNetCore` for endpoint mapping, so a non-HTTP host that wants only the engine takes that reference too |
| **B — Three packages as listed** (`Encina.BFF`, `Encina.BFF.YARP`, `Encina.BFF.Aggregation`) | Aggregation usable without YARP | `Encina.BFF.YARP` has no content under D1 option A; the split adds packaging cost without a consumer |
| **C — Two packages**: `Encina.BFF` (abstractions and engine, HTTP-free) and `Encina.BFF.AspNetCore` (route attributes and endpoint mapping) | The engine is usable from workers and serverless hosts; follows the `Encina.Tenancy` / `Encina.Tenancy.AspNetCore` pattern | Two projects to maintain for a first delivery |

### Chosen Option: **A — One package `Encina.BFF`** (recommended, pending the maintainer)

### Rationale

A wins while the only host is ASP.NET Core and no consumer of an HTTP-free engine exists. C wins as soon as a worker or Azure Functions host composes backend calls; the engine (`Abstractions/` and `Aggregation/`) lives in folders with no ASP.NET references, enforced by an architecture test (S4), so the split is a move, not a redesign. B wins only if the maintainer keeps YARP in the first delivery (D1 option B or C).

**Class:** Dilemma

</details>

<details>
<summary><strong>4. Relationship with #199 and the milestone (Dilemma D4)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A — One delivery under #383** (status approved, priority critical); #199 is closed as a duplicate after the maintainer reads this plan | One owner and one API; the plan already covers #199's aggregation builder and parallel fetch | #199's `[BFF(ClientType)]` handler style is dropped; the milestone of #383 must be moved |
| **B — Split by half**: #199 owns in-process aggregation, #383 narrows to the YARP gateway layer | Each issue keeps a distinct scope | Under D1 option A #383 has nothing left to do before 1.0; the two APIs must be designed together anyway |
| **C — Keep both as they are** | No decision now | Two competing designs of the same pattern; the roadmap lists BFF three times with three milestones |

### Chosen Option: **A — One delivery under #383** (recommended, pending the maintainer)

### Rationale

A wins because the issues describe one pattern and only one of them is approved and prioritized. B wins if the maintainer wants the YARP layer as a separate later feature. C wins only as a deliberate pause. The milestone (v0.14.0 in the issue, v0.16.0 and v0.18.0 on the roadmap) is the maintainer's to set once A, B or C is chosen.

**Class:** Dilemma

</details>

<details>
<summary><strong>5. How an aggregation is declared (Dilemma D5)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A — Attributes only**, as in the issue: `[BffRoute("/api/v1/orders/{orderId}")]` and `[AggregateFrom("OrderService", "InventoryService", "PaymentService")]` | Declarative, matches the issue's acceptance item | Service names are strings with no binding to a request type or a client; nothing says how a step is built from the route values or composed |
| **B — `[BffRoute]` for the route only, plus a typed aggregator class** registered with `AddAggregator<T>()` (the issue's own configuration example) whose fluent builder declares the steps | The route is declarative; the steps are typed, refactorable and unit-testable; composition is code | Two places to read (the attribute and the aggregator) |
| **C — Fluent only**, no attributes (`MapBff("web").Get("/orders/{id}", ...)`) | One place; no reflection scan | Does not meet the issue's "declarative routing attributes" acceptance item |

### Chosen Option: **B — `[BffRoute]` plus a typed aggregator** (recommended, pending the maintainer)

### Rationale

B wins because a string service name cannot be checked by the compiler or tested in isolation, while a typed step can. A wins only if the maintainer wants the shortest possible declaration and accepts an implicit convention for building steps; C wins for teams that avoid reflection-based discovery. `[AggregateFrom]` is dropped under B and the issue's acceptance list is updated.

**Class:** Dilemma

</details>

<details>
<summary><strong>6. Execution model: parallel fan-out, time from <code>TimeProvider</code>, errors as <code>Either</code> (Dilemma D6)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A — Independent steps run in parallel with `Task.WhenAll`; each step has a timeout from `TimeProvider.CreateCancellationTokenSource`; the first mandatory failure cancels the others; the failing step's `EncinaError` is returned with the step name in its details** | Fan-out is the point of aggregation; deterministic in tests with `FakeTimeProvider`; follows Railway Oriented Programming | Dependent steps (step B needs A's result) are not expressible in the first delivery and are written as a nested request |
| **B — Sequential steps** | Simple | Latency adds up; defeats the pattern |
| **C — A dependency graph executor** | Handles dependent steps | A scheduler to design and test; not needed by the issue |

### Chosen Option: **A — Parallel fan-out with `TimeProvider` timeouts** (recommended, pending the maintainer)

### Rationale

Rules that bind every option: time comes from `TimeProvider`, never from the system clock (AGENTS.md section 3); operations return `Either<EncinaError,T>` (ADR-001, ADR-006); a `Left` from `IEncina.Send` fails the aggregate or, for an `Optional()` step, is skipped visibly and only under the D2 category rule (AGENTS.md section 3, errors are never swallowed). Cancellation of the caller's token cancels every step and propagates as cancellation (`Cancelled`, R5), not as a server error. The execution shape itself is a choice: sequential (B) and graph (C) execution satisfy those rules too, so it is the maintainer's; A wins because fan-out is the purpose of the pattern, B for the smallest executor, C when steps depend on each other.

**Deterministic failure.** With `Task.WhenAll` the error returned for a failed aggregate is that of the first mandatory step to fail by completion order, recorded with an atomic first-failure latch (`Interlocked.CompareExchange`); that failure cancels a linked token source, and the sibling cancellations it causes are discarded, never reported as errors. A cancellation of the caller's own token is told apart (the caller's token is checked first) and propagates as `Cancelled`. The returned error keeps the inner category: it is built with `EncinaErrors.Create("encina.bff.step_failed", innerError.GetCategory(), ...)` and the inner code and step name in its details, so a wrapper never turns a `Forbidden` into a 500.

</details>

<details>
<summary><strong>7. Client identification and the reuse of Encina behaviors</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A — The client type comes from the BFF mapping** (`MapBff("mobile")` builds a route group per client type); caching, resilience, validation and authorization come from the existing pipeline behaviors on each step and on the composed request | A client cannot choose its own BFF profile; no new cross-cutting code; same behaviors, same options as everywhere else | A new client type is a new mapped group |
| **B — The client type comes from a request header** (`X-Client-Type`) | One route for all clients | Any caller can claim any profile; the same reason `TrustTenantIdHeader` defaults to false |
| **C — The BFF implements its own cache and circuit breaker** | Self-contained | Duplicates `QueryCachingPipelineBehavior` and the Polly behaviors; two configurations to keep consistent |

### Chosen Option: **A — Client type from the mapping, existing behaviors reused**

### Rationale

Authorization and fault isolation must not depend on a value the caller supplies. `EncinaAspNetCoreOptions.TrustTenantIdHeader` is the precedent for refusing a client-chosen identity hint by default, and the existing behaviors already implement caching, circuit breaking and validation for any request. This holds for every answer of D1 and D5: any route mapping builds the client type from the mapped group, never from a header.

**Class:** Trivial
**Settled by:** `src/Encina.AspNetCore/EncinaAspNetCoreOptions.cs` (`TrustTenantIdHeader`), `src/Encina.Caching` (`QueryCachingPipelineBehavior`), `src/Encina.Polly` (resilience behaviors), AGENTS.md sections 1 and 3

</details>

<details>
<summary><strong>8. Default authorization of a BFF route group</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A — `MapBff` applies `RequireAuthorization()` to the group by default; `AllowAnonymous()` is an explicit opt-out per group that is logged at startup (EventId 6850 `RouteMapped` carries the flag)** | Fails closed: an unannotated aggregated query is never anonymous by accident | One call to opt out for public routes |
| **B — Inherit whatever the host configured (nothing by default)** | No new behavior | `AuthorizationPipelineBehavior` acts only on requests that carry attributes, so an unannotated query exposes aggregated backend data anonymously |

### Chosen Option: **A — authorization required by default**

### Rationale

AGENTS.md section 3 requires security gates to fail closed; only A satisfies it, because B leaves a missing authorization annotation as an open door. The requirement is the ASP.NET Core authorization of the group (authentication scheme and policy are the application's), not a new auth system; the existing `AuthorizationPipelineBehavior` still evaluates attributes on the composed query and on each step request.

**Class:** Settled
**Settled by:** AGENTS.md section 3 (security gates fail closed; the only opt-out is explicit and logged), SPEC-002 DEC-006

</details>

<details>
<summary><strong>9. Credentials sent to backends and cookie-based clients (Dilemma D9)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A — No credential is forwarded by default**: the backend identity is whatever the named client's `HttpClient` handler adds (a client-credentials or token-exchange handler configured by the application); `BffOptions` holds no secret | Cannot leak the caller's token to a backend that was not meant to see it; one rule for every named backend | The application writes a handler per backend that needs the user's identity |
| **B — Opt-in per backend: `ForwardCallerToken()` copies the incoming `Authorization` header** (never default, never global) | Convenient for backends that accept the same token | Easy to enable for a backend that should not see it; audience and scope mismatch; needs a documented warning |
| **C — Forward the incoming `Authorization` header to every named backend** | Zero configuration | Leaks the caller's credential to every backend: not acceptable |

### Chosen Option: **A — no credential forwarded unless a named-client handler is configured** (recommended, pending the maintainer)

### Rationale

C is excluded by the fail-closed principle (AGENTS.md section 3), A and B both satisfy it, so the choice is the maintainer's; A is the smaller surface and B can be added later per backend. For a cookie-session BFF, anti-forgery and CORS are the host's configuration (`MapBff` returns a normal `RouteGroupBuilder` and sets neither); the guide states this position and the deferred issue `bff-cookie-csrf-cors.md` (Next Steps) tracks a first-class answer.

**Class:** Dilemma

</details>

---

## Implementation Phases

### Phase 1: Package, core contracts and errors

<details>
<summary><strong>Tasks</strong></summary>

1. `src/Encina.BFF/Encina.BFF.csproj` (references `Encina`, the ASP.NET Core framework, `Microsoft.CodeAnalysis.PublicApiAnalyzers`; `InternalsVisibleTo` for the five test projects like `Encina.AspNetCore`), added to `Encina.slnx`; `PublicAPI.Shipped.txt`, `PublicAPI.Unshipped.txt`, `README.md`.
2. `Abstractions/BffContext.cs`: `ClientType`, `TenantId`, `CorrelationId` taken from `IRequestContext`. With tenancy on and no tenant resolved no context is built: the endpoint returns `Left` (`encina.bff.tenant_required`, `InvalidRequest`) before any backend call (fail closed).
3. `Abstractions/ServiceResponse.cs`: step name, `Either<EncinaError, object>`, `Elapsed`, `Skipped`.
4. `Abstractions/IBffRequestAdapter.cs`: the issue's `ForwardAsync<TRequest,TResponse>(TRequest, BffContext, CancellationToken)` returning `Either<EncinaError,TResponse>`; the default implementation (`Adapters/DefaultBffRequestAdapter.cs`, lands in S3 with its tests) dispatches through `IEncina.Send` with the step timeout.
5. `Abstractions/IResponseAggregator.cs`: `IResponseAggregator<TResponse>.AggregateAsync(IReadOnlyList<ServiceResponse>, CancellationToken)` as in the issue.
6. `BffErrors.cs`: codes with their ADR-062 `ErrorCategory`, each built through the categorized `EncinaErrors.Create` (the package is outside the pending list, so the completeness scan applies from the first commit; there is no status-table entry to add): `encina.bff.step_failed` keeps the **inner error's category** (`innerError.GetCategory()`, so a `Forbidden` stays 403 and an outbound `UpstreamFailure` stays 502; R7), `encina.bff.step_timeout` is `Timeout` (504), `encina.bff.aggregation_failed` (the composition of the aggregator failed) is `Internal` (R4), `encina.bff.tenant_required` (tenancy on and no tenant resolved) is `InvalidRequest` (R3), `encina.bff.route_unmapped` is `Internal`; details carry step name and inner code only, never the inner `Message`.
7. `src/Encina/Diagnostics/EventIdRanges.cs`: `BFF = (6850, 6879)`; `tests/Encina.UnitTests/Testing/Architecture/EncinaEventIdAllocationTests.cs`: `["Encina.BFF"] = [nameof(EventIdRanges.BFF)]` (these two edits, the core `PublicAPI.Unshipped.txt` line and the `Encina.json` manifest row land in S2 with the log class that uses the range, not in S1).

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 1</strong></summary>

```text
CONTEXT: Issue #383 adds the BFF pattern. Dilemmas D1 (in-process aggregation), D2 (all-or-nothing with per-step Optional), D3 (one package), D4 (single delivery under #383), D5 ([BffRoute] plus a typed aggregator), D6 (parallel fan-out) and D9 (no credential forwarded by default) are recommended, pending the maintainer. Every error code is categorized (ADR-062).
TASK: Create the Encina.BFF project, the contracts (BffContext, ServiceResponse, IBffRequestAdapter, IResponseAggregator<T>), BffErrors, and register EventIdRanges.BFF (6850-6879) first.
KEY RULES: new package gets README, PublicAPI files and a coverage manifest; Either<EncinaError,T> everywhere; no exception for business logic; no [Obsolete]; sealed where possible; XML docs with examples.
REFERENCE FILES: src/Encina.Refit/Encina.Refit.csproj, src/Encina.Refit/Abstractions/IRestApiRequest.cs, src/Encina/Diagnostics/EventIdRanges.cs, src/Encina.AspNetCore/Encina.AspNetCore.csproj
```

</details>

### Phase 2: Aggregation engine

<details>
<summary><strong>Tasks</strong></summary>

1. `Aggregation/AggregationBuilder.cs`: `AggregationBuilder<TRequest,TResponse>` with `Fetch<TBackendRequest,TBackendResponse>(string name, Func<TRequest, TBackendRequest> build)`, `.Optional()`, `.Timeout(TimeSpan)`; produces an immutable list of steps.
2. `Aggregation/AggregationStep.cs` and `Aggregation/ParallelAggregationExecutor.cs`: starts all steps with `Task.WhenAll`, each with `TimeProvider.CreateCancellationTokenSource(timeout)` linked to the caller's token; a mandatory failure cancels the siblings and returns the error of the first mandatory step to fail by completion order (atomic first-failure latch; sibling cancellations caused by it are discarded; the caller's own cancellation is checked first and propagates as `Cancelled`), built with the inner category kept (Design Choice 6); an optional failure is recorded as a skipped `ServiceResponse` only when its category is `Unavailable`, `Timeout`, `UpstreamFailure` or `RateLimited` (D2); any other category on an optional step (`Unauthenticated`, `Forbidden`, `Cancelled`, `Validation`, `InvalidRequest`, `Internal`) fails the aggregate. A step timeout is `encina.bff.step_timeout` (`Timeout`).
3. `Aggregation/BffAggregator.cs`: `abstract class BffAggregator<TRequest,TResponse> : IRequestHandler<TRequest,TResponse>` with `Configure(AggregationBuilder<TRequest,TResponse>)` and `Compose(TRequest, IReadOnlyList<ServiceResponse>)`.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 2</strong></summary>

```text
CONTEXT: Phase 1 delivered the contracts. Backend calls are Encina requests dispatched through IEncina (Encina.Refit requests in practice).
TASK: Implement the aggregation builder, the parallel executor and the BffAggregator base class.
KEY RULES: time only from the injected TimeProvider; the caller's cancellation propagates as cancellation, not as an error; the first mandatory failure cancels siblings; an Optional step failure is never silent (logged and counted in Phase 5); no shared mutable state between steps; the executor allocates one task per step and no more.
REFERENCE FILES: src/Encina.Refit/Handlers/RestApiRequestHandler.cs, src/Encina/Abstractions/IEncina.cs, src/Encina.Polly (resilience behaviors)
```

</details>

### Phase 3: Route declaration and endpoint mapping

<details>
<summary><strong>Tasks</strong></summary>

1. `Routing/BffRouteAttribute.cs`: `[AttributeUsage(AttributeTargets.Class)] sealed class BffRouteAttribute(string route)`; optional `Method` (default `GET`).
2. `Routing/BffRouteDiscovery.cs`: scans the assemblies passed to `MapBffRoutes` once at startup and returns the routes; a query record with a `[BffRoute]` but no registered aggregator is a startup error, not a runtime 404 (fail closed).
3. `Routing/BffEndpointExtensions.cs`: `MapBff(this IEndpointRouteBuilder, string clientType)` returns a `RouteGroupBuilder` that applies `RequireAuthorization()` by default (Design Choice 8); `AllowAnonymous()` is the explicit opt-out and is logged at startup. Its endpoints bind **only the route values declared in the template** to the query's constructor parameters (no query-string or body binding, so no over-posting), send the query through `IEncina` (so `ValidationPipelineBehavior` runs on the composed request), and render `Left` with `ToProblemDetails`; the status comes from the error's `ErrorCategory` (ADR-062), with no internal error codes beyond what #3426 decides. No credential is forwarded to a backend unless a named-client handler is configured (D9).

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 3</strong></summary>

```text
CONTEXT: Aggregators exist (Phase 2). Routes are declared with [BffRoute] on the query record; the client type comes from the mapped group, never from a header.
TASK: Implement BffRouteAttribute, BffRouteDiscovery and MapBff.
KEY RULES: discovery runs once at startup; a route without an aggregator fails startup; the group requires authorization by default and AllowAnonymous is explicit and logged; only declared route values bind; no backend credential is forwarded by default; render errors through ToProblemDetails (status from the ErrorCategory); no Reflection.Emit.
REFERENCE FILES: src/Encina.AspNetCore/DPIAEndpointExtensions.cs, src/Encina.AspNetCore/ProblemDetailsExtensions.cs, src/Encina.AspNetCore/EncinaAspNetCoreOptions.cs
```

</details>

### Phase 4: Configuration and DI

<details>
<summary><strong>Tasks</strong></summary>

1. `BffOptions.cs`: default step timeout, `Backends` (name to named-client name and optional health path).
2. `BffOptionsValidator.cs`: `IValidateOptions<BffOptions>` (timeout positive, names unique, health paths relative).
3. `ServiceCollectionExtensions.AddEncinaBFF(this IServiceCollection, Action<BffOptions>?)` and `AddAggregator<TAggregator>()`: options, validator, `IBffRequestAdapter` default, `TimeProvider` (TryAddSingleton to `TimeProvider.System`), aggregator registrations; all `TryAdd`.
4. DI test with `ValidateOnBuild` and `ValidateScopes`.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 4</strong></summary>

```text
CONTEXT: The package is opt-in (AGENTS.md section 1): nothing is registered unless AddEncinaBFF is called.
TASK: Implement BffOptions, its validator and the registration methods with a DI completeness test.
KEY RULES: every registration is TryAdd; the application's own registrations win in any order; no secrets in options; validate at startup; document each option with an example.
REFERENCE FILES: src/Encina.Security/ServiceCollectionExtensions.cs, src/Encina.Refit/ServiceCollectionExtensions.cs, src/Encina/Identity/RequestIdentityOptionsValidator.cs
```

</details>

### Phase 5: Cross-cutting integration and observability

<details>
<summary><strong>Tasks</strong></summary>

1. `Diagnostics/BffLog.cs` (`[LoggerMessage]`, range `EventIdRanges.BFF`): `6850 RouteMapped` (also carries the anonymous opt-out flag of Design Choice 8), `6851 AggregationStarted`, `6852 StepCompleted`, `6853 StepFailed`, `6854 StepTimedOut`, `6855 OptionalStepSkipped`, `6856 AggregationCompleted`, `6857 AggregationFailed`, `6858 BackendProbeFailed`; arguments are route template, client type, step name and error code only.
2. `Diagnostics/BffDiagnostics.cs`: `ActivitySource` and `Meter` named `Encina.BFF`; counters `encina.bff.aggregations` and `encina.bff.steps` (no `_total` suffix, ADR-054 decision 4; tags `client_type`, `step`, `outcome`), histogram `encina.bff.step_duration` in seconds; one span per aggregation and per step. Per ADR-054 D4 the tenant id is a span attribute and a metric tag only when multi-tenancy is on, and no tenant tag is emitted when it is off (the cardinality risk is documented by #906); logs never carry the tenant id.
3. `Health/BffBackendHealthCheck.cs`: `IEncinaHealthCheck` with `DefaultName` and static `Tags`, scoped resolution through `IServiceProvider.CreateScope()`, one probe per configured backend through `IHttpClientFactory`; reports the backend name only.
4. Tenancy: `TenantId` flows from the request identity into `BffContext` and every step; with tenancy on and no tenant resolved the request is denied with `encina.bff.tenant_required` before any backend call (never a default tenant); choosing a tenant-specific backend address stays with the named client's `HttpClient` configuration. Caching, resilience and validation: tests prove that `[Cache]` on the composed query and the Polly behaviors on a step request apply with no BFF code.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 5</strong></summary>

```text
CONTEXT: The engine, routing and DI exist. EventIdRanges.BFF (6850-6879) was registered in Phase 1.
TASK: Add logging, diagnostics and the backend health check; add tests that prove caching, resilience, validation and tenancy apply to aggregated requests without BFF-specific code.
KEY RULES: sequential EventIds with no gaps; never log EncinaError.Message, request payloads or user ids; counters are Counter<long> with tags; health check results carry the backend name and no exception message; scoped resolution in the health check.
REFERENCE FILES: src/Encina.Refit/Diagnostics/OutboundTelemetry.cs, src/Encina.Security/Health/SecurityHealthCheck.cs, src/Encina.AspNetCore/EncinaContextMiddlewareLog.cs, src/Encina/Diagnostics/EventIdRanges.cs
```

</details>

### Phase 6: Testing — 7 test types

<details>
<summary><strong>Tasks</strong></summary>

1. **Unit**: builder, executor (parallelism, ordering of results, mandatory and optional failure, timeout with `FakeTimeProvider`, cancellation), adapter, route discovery (missing aggregator), endpoint binding, options validator, error factory.
2. **Guard**: every public method with parameters.
3. **Contract**: `IBffRequestAdapter` and `IResponseAggregator<T>` on the real implementations.
4. **Property** (FsCheck): the composed result is independent of step completion order; N steps with k optional failures yield exactly the non-failed responses; the aggregate fails if and only if a mandatory step fails; **for every `ErrorCategory`**, an optional step with an availability category (`Unavailable`, `Timeout`, `UpstreamFailure`, `RateLimited`) is skipped and any other category (including `Unauthenticated`, `Forbidden`, `Cancelled`) fails the aggregate; the returned error keeps the inner category; the returned error is that of the first mandatory failure by completion order. An architecture test asserts that `Abstractions/` and `Aggregation/` reference no `Microsoft.AspNetCore` namespace (the D3 option C split stays a move).
5. **Integration**: ASP.NET Core test host with `Encina.Testing.WireMock` stub backends: success, one mandatory backend 500 (aggregate fails), one optional backend 503 (aggregate succeeds with the step skipped), one optional backend 403 (aggregate fails, never skipped), backend timeout, tenant propagated, tenancy on with no tenant (denied before any backend call), an anonymous call to a `MapBff` group (401) and to an `AllowAnonymous()` group (200, startup log shows the opt-out), the caller's `Authorization` header absent on the backend request, `[Cache]` second call served from `ICacheProvider` (in-memory provider).
6. **Load**: concurrent aggregations against stub backends (`tests/Encina.LoadTests`), run with `DOTNET_JitObjectStackAllocationConditionalEscape=0` locally. **Benchmark**: aggregation hot path with in-memory fake steps (`BenchmarkSwitcher.FromAssembly(...).Run(args, config)`, results to `artifacts/performance/`).
7. Per-file targets for a new `.github/coverage-manifest/Encina.BFF.json` (written by `mechanical-fixer`; `-` = flag not applicable; each sub-issue reports the measured values of its own files; the integration column of S1 to S4 files is met by S5):

| File | Unit | Guard | Contract | Property | Integration | Justification |
|------|:----:|:-----:|:--------:|:--------:|:-----------:|---------------|
| `Abstractions/BffContext.cs` | 95 | 90 | - | - | 80 | A small value holder with null guards; the integration host proves the tenant and correlation flow. |
| `Abstractions/ServiceResponse.cs` | 95 | 90 | - | 85 | - | Pure data and accessors; property tests cover the skipped/failed/succeeded invariants. |
| `Abstractions/IBffRequestAdapter.cs`, `IResponseAggregator.cs` | 0 | 0 | 90 | - | - | Interfaces with no executable code; the contract tests run the real implementations. |
| `BffErrors.cs` | 95 | 90 | - | 85 | - | Error factories with a fixed code and category each; property tests cover the preserved inner category. |
| `Aggregation/AggregationBuilder.cs` | 95 | 90 | - | 85 | - | Pure builder logic reachable without a host. |
| `Aggregation/AggregationStep.cs` | 90 | 85 | - | - | - | An immutable step record plus a thin constructor. |
| `Aggregation/ParallelAggregationExecutor.cs` | 95 | 90 | - | 90 | 80 | The core of the pattern: parallelism, first-failure latch, timeouts with `FakeTimeProvider`; the property tests cover order independence and the category rule. |
| `Aggregation/BffAggregator.cs` | 90 | 85 | 85 | - | 80 | Template base class over the executor. |
| `Adapters/DefaultBffRequestAdapter.cs` | 90 | 85 | 90 | - | 80 | Dispatches through `IEncina` with the step timeout; a mocked `IEncina` covers every branch. |
| `Routing/BffRouteAttribute.cs` | 100 | 0 | - | - | - | A constructor with one string argument validated in the discovery, no other logic. |
| `Routing/BffRouteDiscovery.cs` | 90 | 85 | - | - | 75 | Assembly scan with a startup failure for a route without an aggregator. |
| `Routing/BffEndpointExtensions.cs` | 90 | 85 | - | - | 80 | Default authorization, route-value binding, error rendering; the host test proves 401, 200 and the opt-out. |
| `BffOptions.cs` | 90 | 0 | - | - | 70 | Properties only, no parameters, so guard tests cannot reach it. |
| `BffOptionsValidator.cs` | 95 | 90 | - | - | 70 | Few branches (timeout, unique names, relative paths). |
| `ServiceCollectionExtensions.cs` | 90 | 90 | - | - | 80 | Registration only, proven by the `ValidateOnBuild`/`ValidateScopes` DI test. |
| `Diagnostics/BffDiagnostics.cs` | 90 | 85 | - | - | - | Counters, histogram and spans with a tenant tag only when tenancy is on; testable with a meter listener. |
| `Diagnostics/BffLog.cs` | 0 | 0 | - | - | - | Source-generated `[LoggerMessage]` partial methods with no hand-written code; reached through the executor tests. |
| `Health/BffBackendHealthCheck.cs` | 90 | 85 | - | - | 75 | One probe per backend through a fake `IHttpClientFactory`; the host test proves the healthy path. |

The CRAP table of the changed methods (comp, cov, CRAP <= 10) is reported with each sub-issue.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 6</strong></summary>

```text
CONTEXT: The package is implemented. Tests must execute real code (AGENTS.md section 9) and prove the fail-closed and failure paths.
TASK: Write the seven test types and measure per-flag coverage of every file against the manifest.
KEY RULES: Shouldly via Encina.Testing.Shouldly; Encina.Testing.WireMock for backends; FakeTimeProvider for timeouts; no Thread.Sleep; builders for test data; integration tests use shared [Collection] fixtures when a database is involved (none is needed here); run the CRAP gate table locally.
REFERENCE FILES: tests/Encina.UnitTests/Refit/, src/Encina.Testing.WireMock, tests/Encina.LoadTests, tests/Encina.BenchmarkTests, .github/coverage-manifest/Encina.Refit.json
```

</details>

### Phase 7: Documentation and finalization

<details>
<summary><strong>Tasks</strong></summary>

1. XML docs with examples on every public API; `PublicAPI.Unshipped.txt` complete (`mechanical-fixer`).
2. `src/Encina.BFF/README.md`; guide `docs/en/guides/backends-for-frontends.md` (aggregation, optional steps and the categories they may skip, caching a composed query, default authorization and `AllowAnonymous`, no credential forwarding and the cookie-session CSRF/CORS position, why YARP is not part of it) written by `docs-writer`.
3. ADR in `docs/architecture/adr/` for D1 (ADR number assigned when the ADR pull request opens); changelog fragment `changelog.d/383-bff-aggregation.added.md`; `ROADMAP.md:505` and `:663` reconciled according to D4; `docs/INVENTORY.md` row; the closing of #199 is the maintainer's action.
4. Build and test verification: `dotnet build --configuration Release` with 0 warnings; every flag reaches its manifest target.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 7</strong></summary>

```text
CONTEXT: Encina.BFF is implemented and tested; documentation and records remain.
TASK: Write the README and guide through docs-writer; add the ADR, changelog fragment, PublicAPI lines, manifest and INVENTORY row (the manifest, changelog and PublicAPI through mechanical-fixer).
KEY RULES: documentation in English; never type coverage figures by hand (covref markers); the guide states the all-or-nothing default and the Optional step; the ADR records D1 and its alternatives.
REFERENCE FILES: src/Encina.Refit/README.md, docs/architecture/adr/index.md, changelog.d/README.md, ROADMAP.md
```

</details>

---

## Research

### Relevant Standards & Specifications

| Standard | Reference | Relevance to this feature |
|---|---|---|
| Backends for Frontends pattern | Sam Newman (2015); Azure Architecture Center, "Backends for Frontends pattern" | One backend per client type |
| Gateway Aggregation pattern | Azure Architecture Center | One request fans out to several services |
| YARP reverse proxy | Microsoft, package `Yarp.ReverseProxy` | Route, cluster and transform model; relevant only if D1 selects the gateway |
| RFC 9110 sections 15.6.3 and 15.6.5 | 502 Bad Gateway, 504 Gateway Timeout | Upstream failure semantics, applied by the ADR-062 category table (`UpstreamFailure` 502, `Timeout` 504) |
| RFC 9457 | Problem Details for HTTP APIs | Body of an aggregation failure |
| ADR-001, ADR-006 | `docs/architecture/adr/` | `Either<EncinaError,T>` for operations |

### Existing Encina Infrastructure to Leverage

| Component | Location | Usage in this feature |
|---|---|---|
| `RestApiRequestHandler`, `IRestApiRequest` | `src/Encina.Refit/Handlers/RestApiRequestHandler.cs`, `Abstractions/IRestApiRequest.cs` | Typed backend calls; the backend step of an aggregation |
| `OutboundErrorCodes`, `OutboundTelemetry` | `src/Encina.Refit/Diagnostics/OutboundTelemetry.cs` | Transient and ambiguous classification; the `Encina.Outbound.Send` activity |
| `IEncina.Send`, `IPipelineBehavior` | `src/Encina/Abstractions/IEncina.cs`, `IPipelineBehavior.cs` | Each step is dispatched as an Encina request |
| `QueryCachingPipelineBehavior`, `CacheAttribute` | `src/Encina.Caching` | Caches composed query responses through `ICacheProvider` with no new code |
| `StandardResiliencePipelineBehavior`, `CircuitBreakerPipelineBehavior`, `BulkheadPipelineBehavior` | `src/Encina.Polly`, `src/Encina.Extensions.Resilience` | Fault isolation per backend |
| `ValidationPipelineBehavior` | `src/Encina/Validation/ValidationPipelineBehavior.cs` | Request validation at the BFF edge |
| `AuthorizationPipelineBehavior`, `ProblemDetailsExtensions`, `EncinaContextMiddleware` | `src/Encina.AspNetCore` | Client-specific authorization, error rendering, identity and tenant binding |
| `DPIAEndpointExtensions` | `src/Encina.AspNetCore/DPIAEndpointExtensions.cs` | Pattern for `MapGroup` endpoint extensions |
| `IEncinaHealthCheck` | `src/Encina.Messaging/Health/IEncinaHealthCheck.cs` | Backend probes |
| `Encina.Testing.WireMock` | `src/Encina.Testing.WireMock` | Stub backends in tests |

### Event ID Allocation

| Package | Range | Notes |
|---|---|---|
| `Encina.BFF` | 6850-6879 | New range `EventIdRanges.BFF`, registered first in `src/Encina/Diagnostics/EventIdRanges.cs`; 6850-6858 used by this plan, 6859-6879 free; assembly mapped in `AssemblyRanges` of `EncinaEventIdAllocationTests` |
| `Encina.BFF.YARP` | 6880-6899 | Held only if D1 or D3 keeps a YARP package; otherwise returned to the registry |

### Estimated File Count

| Category | Files | Notes |
|---|---|---|
| Production, `Encina.BFF` (new) | 22 | csproj, abstractions 4, errors 1, aggregation 4 (with the adapter), routing 3, options and DI 3, diagnostics, log and health 3, `PublicAPI` 2, README |
| Production, other packages (modified) | 4 | `EventIdRanges.cs`, `EncinaEventIdAllocationTests.cs`, core `PublicAPI.Unshipped.txt`, `Encina.slnx` |
| Test-project edits | 7 | `<ProjectReference>` to `Encina.BFF` in `Encina.UnitTests`, `Encina.GuardTests`, `Encina.ContractTests`, `Encina.PropertyTests`, `Encina.IntegrationTests`, `Encina.LoadTests`, `Encina.BenchmarkTests` |
| Tests | 24 | Unit about 10, guard 2, contract 2, property 2, integration 3, load 1, benchmark 1, justification files 3 |
| Documentation and records | 7 | Guide, ADR, changelog fragment, coverage manifests (`Encina.BFF.json`, `Encina.json` row), INVENTORY row, ROADMAP edit |

Total about 64 files and more than 5,000 lines: well above one worker pull request. Proposed sub-issues; each counts its code, tests, `PublicAPI` lines, manifest rows, log entries and the `<ProjectReference>` edits it needs, builds and passes alone, and stays within 15 files and 1,500 lines (the targets of a file ship with the file):

| Sub-issue | Content | Files |
|-----------|---------|-------|
| **S1** | Project, `Encina.slnx`, `BffContext`, `ServiceResponse`, `IBffRequestAdapter`, `IResponseAggregator`, `BffErrors` (categorized), `PublicAPI.Shipped/Unshipped.txt`, manifest; `Encina.UnitTests` and `Encina.GuardTests` project references; errors, response and context unit tests and a guard test | 15 |
| **S2** | `EventIdRanges.BFF`, `AssemblyRanges` entry, core `PublicAPI.Unshipped.txt`, `Encina.json` row, `BffLog`, `AggregationBuilder`, `AggregationStep`, `ParallelAggregationExecutor`, `BffDiagnostics`, package `PublicAPI` and manifest edits; builder, executor and diagnostics unit tests and a guard test | 15 |
| **S3** | `DefaultBffRequestAdapter`, `BffAggregator`, `BffRouteAttribute`, `BffRouteDiscovery`, `BffEndpointExtensions`, `BffOptions` and validator, `ServiceCollectionExtensions`, `PublicAPI`, manifest; adapter and aggregator, routing, options/DI unit tests and a guard test | 14 |
| **S4** | `BffBackendHealthCheck`, `PublicAPI`, manifest; `Encina.PropertyTests` and `Encina.ContractTests` project references; property tests, contract tests, the architecture test, health unit and guard tests | 11 |
| **S5** | Integration tests (host helper, mandatory/optional/tenancy/authorization cases, cache case), `Encina.IntegrationTests` reference, load test and `Encina.LoadTests` reference, benchmark and `Encina.BenchmarkTests` reference, justification `.md` files | about 12 |
| **S6** | Documentation: README, guide, ADR, changelog fragment, INVENTORY row, ROADMAP edit (`docs-writer`) | about 6 |

The integration-flag targets of S1 to S4 files are met by S5; those sub-issues report their other flags and state the integration flag as pending S5. Each sub-issue is opened after the maintainer answers the Dilemmas.

---

## Combined AI Agent Prompts

<details>
<summary><strong>Full combined prompt for all phases</strong></summary>

```text
PROJECT CONTEXT: Encina is a pre-1.0 .NET 10 / C# 14 library with Railway Oriented Programming (Either<EncinaError,T>). Issue #383 adds the BFF pattern as the new package Encina.BFF. Dilemmas D1 (in-process aggregation), D2 (all-or-nothing, per-step Optional), D3 (one package), D4 (single delivery under #383, #199 closed as duplicate), D5 ([BffRoute] plus a typed aggregator), D6 (parallel fan-out) and D9 (no credential forwarded by default) are recommended and pending the maintainer: do not start before they are answered, and work only in the sub-issue you were briefed for (S1 to S6). Every new error code passes an ADR-062 category; routes require authorization by default; Optional steps skip availability failures only; tenancy on with no tenant denies.
IMPLEMENTATION OVERVIEW: Phase 1 package, contracts, errors, EventIdRanges.BFF. Phase 2 aggregation builder and parallel executor. Phase 3 [BffRoute], discovery, MapBff. Phase 4 options, validator, AddEncinaBFF. Phase 5 logging (6850-6858), diagnostics, backend health check, proof that caching/resilience/validation/tenancy apply. Phase 6 tests in 7 types. Phase 7 documentation and ADR.
KEY PATTERNS: backend calls are Encina requests through IEncina; time from TimeProvider; first mandatory failure (by completion order) fails the aggregate with the step name and the inner category; Optional steps are skipped visibly and only for availability categories; client type from the mapping, never from a header; opt-in registration with TryAdd proven by a ValidateOnBuild/ValidateScopes test; no EncinaError.Message, payloads or user ids in logs, traces or health results; CRAP <= 10; Shouldly via Encina.Testing.Shouldly; PublicAPI, changelog and manifest through mechanical-fixer; documentation through docs-writer.
REFERENCE FILES: src/Encina.Refit/Handlers/RestApiRequestHandler.cs, src/Encina.Refit/Diagnostics/OutboundTelemetry.cs, src/Encina.AspNetCore/DPIAEndpointExtensions.cs, src/Encina.AspNetCore/ProblemDetailsExtensions.cs, src/Encina.Caching (QueryCachingPipelineBehavior), src/Encina/Diagnostics/EventIdRanges.cs, .github/coverage-manifest/Encina.Refit.json
```

</details>

---

## Cross-Cutting Integration Matrix

| # | Function | Status | Notes |
|---|----------|--------|-------|
| 1 | Caching | ✅ | A composed query marked `[Cache]` is cached by `QueryCachingPipelineBehavior` through `ICacheProvider`, which works with all 8 cache providers; no BFF code, proven by a test (Phase 5). |
| 2 | OpenTelemetry | ✅ | `Encina.BFF` activity source and meter: aggregation and step counters, step duration histogram, one span per aggregation and step (Phase 5). |
| 3 | Structured Logging | ✅ | `[LoggerMessage]` in `BffLog` with EventIds 6850-6858 in the range registered first in Phase 1. |
| 4 | Health Checks | ✅ | `BffBackendHealthCheck` probes the configured backends through `IHttpClientFactory` (Phase 5). |
| 5 | Validation | ✅ | The existing `ValidationPipelineBehavior` validates the composed request and each step request; `BffOptions` are validated at startup (Phase 4). |
| 6 | Resilience | ✅ | Circuit breaker, bulkhead and retry come from the Polly behaviors on each step request; the per-step timeout comes from `TimeProvider` (Phases 2 and 5). |
| 7 | Distributed Locks | ❌ | Stateless routing and composition; no shared state to coordinate. |
| 8 | Transactions | ❌ | The BFF composes reads from several services; a distributed write belongs to a saga, not to a gateway. |
| 9 | Idempotency | ⏭️ | Deduplication of forwarded writes does not apply to aggregated reads; deferred with the issue file `artifacts/issues/bff-write-passthrough-idempotency.md` (git-ignored; the orchestrator opens it). |
| 10 | Multi-Tenancy | ✅ | `TenantId` flows from the request identity into `BffContext` and every step; with tenancy on and no tenant the request is denied (`encina.bff.tenant_required`); telemetry carries the tenant as an attribute and tag only when tenancy is on (ADR-054 D4); the tenant-specific address of a backend stays in the named client's configuration (Phase 5). |
| 11 | Module Isolation | ❌ | The gateway aggregates across modules by design. |
| 12 | Audit Trail | ⏭️ | Audit of gateway requests (who asked what of which backends) is deferred with the issue file `artifacts/issues/bff-gateway-request-audit.md` (git-ignored; the orchestrator opens it). |

---

## Prerequisites & Dependencies

| Prerequisite | State | Effect on this plan |
|---|---|---|
| Answers to D1 to D6 and D9 | Pending the maintainer | The phases assume the recommended options; D1 option B or C adds a YARP package, `Directory.Packages.props` entry and a bridge phase |
| ADR-062 (error category contract, accepted 2026-11-11) and its implementation #3545 | ADR accepted | Replaces the status table of #3419: every `encina.bff.*` code is categorized at the call site (Phase 1 task 6), the categorized `EncinaErrors.Create` overload must be on main before S1, and `step_failed` keeps the inner category |
| #3426 (which error codes a client may see) | Open, `needs-decision` | Decides whether the inner outbound code is exposed to clients; the BFF details follow that decision |
| #3400 (what `Encina.gRPC` and `Encina.GraphQL` are for) | Open, `needs-decision` | GraphQL Federation (the issue's alternative 3) is a scope question for that spike; not a prerequisite |
| #199 | Open | Overlapping issue, reconciled by D4 |

---

## Next Steps

1. The maintainer answers D1 to D6 and D9 when work on #383 starts; the answers go into `## Maintainer Decisions`.
2. Open the sub-issues S1 to S6 (each within 15 files and 1,500 lines) and move the milestone as D4 decides.
3. Open the three deferred issues from `artifacts/issues/bff-write-passthrough-idempotency.md`, `artifacts/issues/bff-gateway-request-audit.md` and `artifacts/issues/bff-cookie-csrf-cors.md` before the plan is briefed.
4. Reconcile `ROADMAP.md:84` (milestone table), `:270`, `:505` and `:663` once D4 is answered.
