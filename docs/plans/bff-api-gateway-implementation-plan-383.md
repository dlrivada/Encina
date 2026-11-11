# Implementation Plan: `Encina.BFF` — Backends for Frontends and response aggregation

> **Issue**: [#383](https://github.com/dlrivada/Encina/issues/383)
> **Type**: Feature
> **Complexity**: High (7 phases, new package, no database provider, about 57 files, five pull requests)
> **Estimated Scope**: ~2,000-2,600 lines of production code + ~3,500-4,500 lines of tests

---

## Summary

Add the Backends for Frontends (BFF) pattern to Encina: a request for one client type (web, mobile, IoT) is answered by calling several backend services in parallel and composing one response, all inside the Encina pipeline. The recommended shape (Dilemma D1, option A) is **in-process aggregation**: each backend call is an Encina request dispatched through `IEncina` (typically an `Encina.Refit` request), so validation, caching, resilience, tenancy and authorization apply to every step with no new code, and a new package `Encina.BFF` supplies the fan-out engine, the aggregation contracts, route declaration and endpoint mapping. A reverse-proxy gateway (YARP) stays outside the first delivery unless the maintainer answers D1 with option B.

Facts checked against `origin/main`:

- There is no `Encina.BFF`, `Encina.BFF.YARP` or `Encina.BFF.Aggregation` project, no `IBffRequestAdapter`, `IResponseAggregator<T>`, `BffRoute` or `AggregateFrom` in `src/`, and `Directory.Packages.props` has no `Yarp.ReverseProxy`.
- `src/Encina.Refit` is the only HTTP client integration: `RestApiRequestHandler<TRequest,TApiClient,TResponse>` turns an HTTP call into `Either<EncinaError,T>` with the outbound taxonomy (`OutboundErrorCodes`: `Timeout`, `RateLimited`, `ServerError`, `NetworkError`, `ClientError`, `UnexpectedResponse`, with transient and ambiguous flags) and records `Encina.Outbound.Send` and `encina.outbound.requests_total`.
- The resilience behaviors exist as pipeline behaviors (`StandardResiliencePipelineBehavior`, `CircuitBreakerPipelineBehavior`, `BulkheadPipelineBehavior` in `Encina.Polly` and `Encina.Extensions.Resilience`), and `QueryCachingPipelineBehavior` with `[Cache]` caches query responses through `ICacheProvider` (`Encina.Caching`).
- `Encina.AspNetCore` provides `AuthorizationPipelineBehavior`, `EncinaContextMiddleware`, `ProblemDetailsExtensions` and an endpoint-mapping precedent (`DPIAEndpointExtensions`); `Encina.Tenancy.AspNetCore` resolves the tenant; `Encina.Testing.WireMock` exists for stub backends.
- Overlap and scope: #199 "BFF Pattern Support (Backend for Frontend)" (open, milestone v0.18.0) proposes the same pattern with `[BFF(ClientType)]` and an `AggregationBuilder`; this issue is in milestone v0.14.0 while `ROADMAP.md:84` lists BFF under v0.16.0, `ROADMAP.md:505` this issue and `ROADMAP.md:663` #199. The maintainer reconciles them (Dilemma D4). SPEC-000 section 6 is the complete list of 1.0 exclusions and does not list BFF or gateway features.

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
| **C — All-or-nothing by default, per-step opt-in** (`Optional()` on a step); a skipped optional step is logged, counted and reported in the response metadata the aggregator receives | Correct by default; availability only where the author states it; the decision is explicit in code | One more concept in the builder |

### Chosen Option: **C — All-or-nothing by default, `Optional()` per step** (recommended, pending the maintainer)

### Rationale

C wins because the right answer differs per step of the same route (a payment step is mandatory, a recommendations step is optional) and a global switch cannot express that. A wins where no step may ever be missing; B wins for read-only dashboards where something is always better than a spinner. The aggregator receives a `ServiceResponse` per step that says whether the step succeeded, so a response model can mark what is absent.

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

A wins while the only host is ASP.NET Core and no consumer of an HTTP-free engine exists. C wins as soon as a worker or Azure Functions host composes backend calls; the engine lives in its own folder so the split is a move, not a redesign. B wins only if the maintainer keeps YARP in the first delivery (D1 option B or C).

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
<summary><strong>6. Execution model: parallel fan-out, time from <code>TimeProvider</code>, errors as <code>Either</code></strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A — Independent steps run in parallel with `Task.WhenAll`; each step has a timeout from `TimeProvider.CreateCancellationTokenSource`; the first mandatory failure cancels the others; the failing step's `EncinaError` is returned with the step name in its details** | Fan-out is the point of aggregation; deterministic in tests with `FakeTimeProvider`; follows Railway Oriented Programming | Dependent steps (step B needs A's result) are not expressible in the first delivery and are written as a nested request |
| **B — Sequential steps** | Simple | Latency adds up; defeats the pattern |
| **C — A dependency graph executor** | Handles dependent steps | A scheduler to design and test; not needed by the issue |

### Chosen Option: **A — Parallel fan-out with `TimeProvider` timeouts**

### Rationale

Time comes from `TimeProvider`, never from the system clock (AGENTS.md section 3); operations return `Either<EncinaError,T>` (ADR-001, ADR-006); a `Left` from `IEncina.Send` fails the aggregate or, for an `Optional()` step, is skipped visibly (AGENTS.md section 3, errors are never swallowed). Cancellation of the caller's token cancels every step and propagates as cancellation, not as an error.

**Class:** Settled
**Settled by:** AGENTS.md section 3 (`TimeProvider`, errors are never swallowed), ADR-001, ADR-006

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

Authorization and fault isolation must not depend on a value the caller supplies. `EncinaAspNetCoreOptions.TrustTenantIdHeader` is the precedent for refusing a client-chosen identity hint by default, and the existing behaviors already implement caching, circuit breaking and validation for any request.

**Class:** Trivial
**Settled by:** `src/Encina.AspNetCore/EncinaAspNetCoreOptions.cs` (`TrustTenantIdHeader`), `src/Encina.Caching` (`QueryCachingPipelineBehavior`), `src/Encina.Polly` (resilience behaviors), AGENTS.md sections 1 and 3

</details>

---

## Implementation Phases

### Phase 1: Package, core contracts and errors

<details>
<summary><strong>Tasks</strong></summary>

1. `src/Encina.BFF/Encina.BFF.csproj` (references `Encina`, the ASP.NET Core framework, `Microsoft.CodeAnalysis.PublicApiAnalyzers`; `InternalsVisibleTo` for the five test projects like `Encina.AspNetCore`), added to `Encina.slnx`; `PublicAPI.Shipped.txt`, `PublicAPI.Unshipped.txt`, `README.md`.
2. `Abstractions/BffContext.cs`: `ClientType`, `TenantId`, `CorrelationId` taken from `IRequestContext`.
3. `Abstractions/ServiceResponse.cs`: step name, `Either<EncinaError, object>`, `Elapsed`, `Skipped`.
4. `Abstractions/IBffRequestAdapter.cs`: the issue's `ForwardAsync<TRequest,TResponse>(TRequest, BffContext, CancellationToken)` returning `Either<EncinaError,TResponse>`; the default implementation dispatches through `IEncina.Send` with the step timeout.
5. `Abstractions/IResponseAggregator.cs`: `IResponseAggregator<TResponse>.AggregateAsync(IReadOnlyList<ServiceResponse>, CancellationToken)` as in the issue.
6. `BffErrors.cs`: codes `encina.bff.step_failed`, `encina.bff.step_timeout`, `encina.bff.aggregation_failed`; details carry step name and inner code only.
7. `src/Encina/Diagnostics/EventIdRanges.cs`: `BFF = (6850, 6879)`; `tests/Encina.UnitTests/Testing/Architecture/EncinaEventIdAllocationTests.cs`: `["Encina.BFF"] = [nameof(EventIdRanges.BFF)]`.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 1</strong></summary>

```text
CONTEXT: Issue #383 adds the BFF pattern. Dilemmas D1 (in-process aggregation), D2 (all-or-nothing with per-step Optional), D3 (one package), D4 (single delivery under #383) and D5 ([BffRoute] plus a typed aggregator) are recommended, pending the maintainer.
TASK: Create the Encina.BFF project, the contracts (BffContext, ServiceResponse, IBffRequestAdapter, IResponseAggregator<T>), BffErrors, and register EventIdRanges.BFF (6850-6879) first.
KEY RULES: new package gets README, PublicAPI files and a coverage manifest; Either<EncinaError,T> everywhere; no exception for business logic; no [Obsolete]; sealed where possible; XML docs with examples.
REFERENCE FILES: src/Encina.Refit/Encina.Refit.csproj, src/Encina.Refit/Abstractions/IRestApiRequest.cs, src/Encina/Diagnostics/EventIdRanges.cs, src/Encina.AspNetCore/Encina.AspNetCore.csproj
```

</details>

### Phase 2: Aggregation engine

<details>
<summary><strong>Tasks</strong></summary>

1. `Aggregation/AggregationBuilder.cs`: `AggregationBuilder<TRequest,TResponse>` with `Fetch<TBackendRequest,TBackendResponse>(string name, Func<TRequest, TBackendRequest> build)`, `.Optional()`, `.Timeout(TimeSpan)`; produces an immutable list of steps.
2. `Aggregation/AggregationStep.cs` and `Aggregation/ParallelAggregationExecutor.cs`: starts all steps with `Task.WhenAll`, each with `TimeProvider.CreateCancellationTokenSource(timeout)` linked to the caller's token; a mandatory failure cancels the siblings and returns the first failing step's error wrapped with the step name; an optional failure is recorded as a skipped `ServiceResponse`.
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
3. `Routing/BffEndpointExtensions.cs`: `MapBff(this IEndpointRouteBuilder, string clientType)` returns a `RouteGroupBuilder` whose endpoints bind route values to the query's constructor parameters, send the query through `IEncina`, and render `Left` with `ToProblemDetails` (no internal error codes beyond what #3426 decides).

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 3</strong></summary>

```text
CONTEXT: Aggregators exist (Phase 2). Routes are declared with [BffRoute] on the query record; the client type comes from the mapped group, never from a header.
TASK: Implement BffRouteAttribute, BffRouteDiscovery and MapBff.
KEY RULES: discovery runs once at startup; a route without an aggregator fails startup; authorization stays with the existing behavior (the group applies the endpoint's authorization policy); render errors through ToProblemDetails; no Reflection.Emit.
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

1. `Diagnostics/BffLog.cs` (`[LoggerMessage]`, range `EventIdRanges.BFF`): `6850 RouteMapped`, `6851 AggregationStarted`, `6852 StepCompleted`, `6853 StepFailed`, `6854 StepTimedOut`, `6855 OptionalStepSkipped`, `6856 AggregationCompleted`, `6857 AggregationFailed`, `6858 BackendProbeFailed`; arguments are route template, client type, step name and error code only.
2. `Diagnostics/BffDiagnostics.cs`: `ActivitySource` and `Meter` named `Encina.BFF`; counters `encina.bff.aggregations_total` and `encina.bff.steps_total` (tags `client_type`, `step`, `outcome`), histogram `encina.bff.step_duration` in seconds; one span per aggregation and per step.
3. `Health/BffBackendHealthCheck.cs`: `IEncinaHealthCheck` with `DefaultName` and static `Tags`, scoped resolution through `IServiceProvider.CreateScope()`, one probe per configured backend through `IHttpClientFactory`; reports the backend name only.
4. Tenancy: `TenantId` flows from the request identity into `BffContext` and every step; choosing a tenant-specific backend address stays with the named client's `HttpClient` configuration. Caching, resilience and validation: tests prove that `[Cache]` on the composed query and the Polly behaviors on a step request apply with no BFF code.

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
4. **Property** (FsCheck): the composed result is independent of step completion order; N steps with k optional failures yield exactly the non-failed responses; the aggregate fails if and only if a mandatory step fails.
5. **Integration**: ASP.NET Core test host with `Encina.Testing.WireMock` stub backends: success, one mandatory backend 500 (aggregate fails), one optional backend 500 (aggregate succeeds with the step skipped), backend timeout, tenant propagated, `[Cache]` second call served from `ICacheProvider` (in-memory provider).
6. **Load**: concurrent aggregations against stub backends (`tests/Encina.LoadTests`), run with `DOTNET_JitObjectStackAllocationConditionalEscape=0` locally. **Benchmark**: aggregation hot path with in-memory fake steps (`BenchmarkSwitcher.FromAssembly(...).Run(args, config)`, results to `artifacts/performance/`).
7. Per-flag targets for a new `.github/coverage-manifest/Encina.BFF.json` (written by `mechanical-fixer`): executor and builder unit 95, guard 90, property 85; endpoint extensions unit 90, guard 85; options validator unit 95; log and diagnostics classes with the usual source-generated justification; each target with a one-sentence justification.

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
2. `src/Encina.BFF/README.md`; guide `docs/en/guides/backends-for-frontends.md` (aggregation, optional steps, caching a composed query, why YARP is not part of it) written by `docs-writer`.
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
| RFC 9110 sections 15.6.3 and 15.6.5 | 502 Bad Gateway, 504 Gateway Timeout | Upstream failure semantics, applied by the status table of #3419 |
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
| Production, `Encina.BFF` (new) | 22 | csproj, abstractions 4, errors 1, aggregation 4, routing 3, options and DI 3, diagnostics, log and health 3, `PublicAPI` 2, README |
| Production, other packages (modified) | 3 | `EventIdRanges.cs`, `EncinaEventIdAllocationTests.cs`, `Encina.slnx` |
| Tests | 24 | Unit about 10, guard 2, contract 2, property 2, integration 3, load 1, benchmark 1, justification files 3 |
| Documentation and records | 7 | Guide, ADR, changelog fragment, coverage manifest, INVENTORY row, ROADMAP edit, README edits |

Total about 56 files and more than 5,000 lines: well above one worker pull request. Proposed sub-issues, each within 15 files and 1,500 lines: **S1** Phase 1 (project, contracts, errors, `EventIdRanges.BFF`, manifest; about 12 files); **S2** Phase 2 (engine, tests of all types except integration; about 14 files); **S3** Phases 3 and 4 (routing, endpoint mapping, DI, options, tests; about 15 files); **S4** Phase 5 and the integration, load and benchmark tests (logging, diagnostics, health, WireMock tests; about 13 files); **S5** Phase 7 (documentation, ADR, records; docs-writer). Each sub-issue is opened after the maintainer answers the Dilemmas.

---

## Combined AI Agent Prompts

<details>
<summary><strong>Full combined prompt for all phases</strong></summary>

```text
PROJECT CONTEXT: Encina is a pre-1.0 .NET 10 / C# 14 library with Railway Oriented Programming (Either<EncinaError,T>). Issue #383 adds the BFF pattern as the new package Encina.BFF. Dilemmas D1 (in-process aggregation), D2 (all-or-nothing, per-step Optional), D3 (one package), D4 (single delivery under #383, #199 closed as duplicate) and D5 ([BffRoute] plus a typed aggregator) are recommended and pending the maintainer: do not start before they are answered, and work only in the sub-issue you were briefed for.
IMPLEMENTATION OVERVIEW: Phase 1 package, contracts, errors, EventIdRanges.BFF. Phase 2 aggregation builder and parallel executor. Phase 3 [BffRoute], discovery, MapBff. Phase 4 options, validator, AddEncinaBFF. Phase 5 logging (6850-6858), diagnostics, backend health check, proof that caching/resilience/validation/tenancy apply. Phase 6 tests in 7 types. Phase 7 documentation and ADR.
KEY PATTERNS: backend calls are Encina requests through IEncina; time from TimeProvider; first mandatory failure fails the aggregate with the step name; Optional steps are skipped visibly; client type from the mapping, never from a header; opt-in registration with TryAdd proven by a ValidateOnBuild/ValidateScopes test; no EncinaError.Message, payloads or user ids in logs, traces or health results; CRAP <= 10; Shouldly via Encina.Testing.Shouldly; PublicAPI, changelog and manifest through mechanical-fixer; documentation through docs-writer.
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
| 9 | Idempotency | ⏭️ | Deduplication of forwarded writes does not apply to aggregated reads; deferred with the issue file `bff-write-passthrough-idempotency.md` written with this plan. |
| 10 | Multi-Tenancy | ✅ | `TenantId` flows from the request identity into `BffContext` and every step; the tenant-specific address of a backend stays in the named client's configuration (Phase 5). |
| 11 | Module Isolation | ❌ | The gateway aggregates across modules by design. |
| 12 | Audit Trail | ⏭️ | Audit of gateway requests (who asked what of which backends) is deferred with the issue file `bff-gateway-request-audit.md` written with this plan. |

---

## Prerequisites & Dependencies

| Prerequisite | State | Effect on this plan |
|---|---|---|
| Answers to D1 to D5 | Pending the maintainer | The phases assume the recommended options; D1 option B or C adds a YARP package, `Directory.Packages.props` entry and a bridge phase |
| #3419 (error-code to status mapping) | Open | `encina.bff.*` codes need entries in the status table (for example 502 and 504 families); the BFF passes the first failing step's error through |
| #3426 (which error codes a client may see) | Open, `needs-decision` | Decides whether the inner outbound code is exposed to clients; the BFF details follow that decision |
| #3400 (what `Encina.gRPC` and `Encina.GraphQL` are for) | Open, `needs-decision` | GraphQL Federation (the issue's alternative 3) is a scope question for that spike; not a prerequisite |
| #199 | Open | Overlapping issue, reconciled by D4 |

---

## Next Steps

1. The maintainer answers D1 to D5 when work on #383 starts; the answers go into `## Maintainer Decisions`.
2. Open the sub-issues S1 to S5 (each within 15 files and 1,500 lines) and move the milestone as D4 decides.
3. Open the two deferred issues from `artifacts/issues/bff-write-passthrough-idempotency.md` and `artifacts/issues/bff-gateway-request-audit.md`.
4. Reconcile `ROADMAP.md:84`, `:505` and `:663` once D4 is answered.
