# Implementation Plan: `Encina.Sidecar` — a sidecar and ambassador forwarding host with Encina cross-cutting behavior

> **Issue**: [#391](https://github.com/dlrivada/Encina/issues/391)
> **Type**: Feature
> **Complexity**: High (10 phases, 7 pull requests of 15, 15, 12, 12, 13, 6 and 8 files)
> **Estimated Scope**: ~900 lines of production code + ~2,400 lines of tests
> **Family**: Aspire (aspire-2): #424, #425, #382, #387. Related and merged on `main` as plans: #1233 (`Encina.Http`: outbound client, client certificates per client and tenant, auth providers), #607 (service discovery), #421 (the processor model for deployment publishers), #383 (BFF, in-process aggregation, no YARP), #454 (health probes). **This plan owns the `Encina.Sidecar` and `Encina.Sidecar.Host` projects, the `Yarp.ReverseProxy` pin and EventIds 890-899.**

---

## Summary

The issue asks for `ISidecarProxy`, `IAmbassadorProxy`, an `EncinaSidecarHost` background service with an HTTP listener, health check integration, a Kubernetes example, Docker image configuration and tests. State of `origin/main` (`da9dd5e3e`), verified by reading and grep, and of the NuGet feed (read 2026-11-20):

| Item of the issue | Today | Owner of what remains |
|-------------------|-------|-----------------------|
| `Encina.Sidecar`, `ISidecarProxy`, `IAmbassadorProxy`, `SidecarOptions`, `AmbassadorOptions` | Missing. `Directory.Packages.props` has no `Yarp.ReverseProxy` (NuGet stable: 2.3.0) | **This plan** |
| `ProxyAsync<TRequest, TResponse>` "with the Encina pipeline" | Pipeline behaviors run on typed requests (`IRequest<TResponse>` through `IEncina.Send`); an arbitrary HTTP request of a non-Encina application has no such type | Design Choice 1 |
| `EncinaSidecarHost : BackgroundService` over an `_httpListener` | Missing; `System.Net.HttpListener` is not what ASP.NET Core hosts use (Kestrel) | Design Choice 2 |
| Resilience, observability, health, validation of the proxied traffic | Pieces exist: `Microsoft.Extensions.Http.Resilience` 10.10.0 and Polly 8.6.6 pinned; `IEncinaHealthCheck`; `Encina.OpenTelemetry` | **This plan** wires them |
| mTLS, TLS termination, outbound credentials of the ambassador | #1233 plans per (client, tenant) client certificates and auth providers | Design Choice 6 |
| Kubernetes deployment example, Docker image configuration | Nothing; the .NET SDK can publish a container image from a project | Design Choice 7 |
| Pod or Compose co-location through Aspire | #421 owns the processor model for the publishers; nothing for sidecars | Issue file |
| "Unit tests with more than 85 percent coverage" | AGENTS.md section 9: per-flag targets per file | This plan's targets |

Differences from the issue text: (1) the sketched `EncinaSidecarHost` calls `_encina.SendAsync(request, ct)` and `_httpListener.RespondAsync`; `IEncina` has `Send`, and a hosted service that owns its own listener duplicates what Kestrel does with streaming, TLS and limits. (2) The sketched `ISidecarProxy.CheckTargetHealthAsync()` is called on the same type that forwards; here health is a registered `IEncinaHealthCheck`. (3) The sketched `Dictionary<string, string> Headers` on `AmbassadorOptions` would let configuration inject arbitrary headers into every outbound call; headers are an allowlist of names the ambassador forwards, never values taken from configuration. (4) `encina/sidecar:latest` is a mutable tag; images are tagged by version.

**Delivered**: `Encina.Sidecar` (library: `SidecarOptions`, `AmbassadorOptions`, `ISidecarProxy`, `IAmbassadorProxy`, a forwarder on YARP's `IHttpForwarder`, request limits, resilience for idempotent methods, an optional tenant gate, a combined health check, diagnostics, `AddEncinaSidecar` and `MapEncinaSidecar`); `Encina.Sidecar.Host` (an executable project that runs the library, with container publish settings); documentation with the Kubernetes and Compose examples; EventIds 890-894.

**Not in scope**: response caching (deferred in the issue), body validation (not applicable), mTLS and outbound credentials (#1233), a registry publish pipeline of the image, Aspire co-location, a service mesh.

**Affected packages**: `Encina.Sidecar` (new), `Encina.Sidecar.Host` (new, not packed), `Encina` (core: one EventId range and its PublicAPI line), `Encina.OpenTelemetry` (one subscription line).

**Provider category**: none of the AGENTS.md section 5 categories apart from the Cloud one in spirit (containers and Kubernetes run locally); nothing here is provider-dependent, so no provider matrix applies.

**Scope check (SPEC-000 section 6, DEC-009)**: the sidecar runs in a local container and in a local cluster; no cloud service is needed, so it is in 1.0 scope.

---

## Design Choices

<details>
<summary><strong>1. What the sidecar is: an HTTP forwarder with cross-cutting behavior, or a typed proxy over `IEncina` (Dilemma D1)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) A reverse proxy: `ISidecarProxy.ForwardAsync(HttpContext, CancellationToken)` returns `Either<EncinaError, Unit>` and forwards through YARP's `IHttpForwarder` (streaming, hop-by-hop headers, trace context handled by the maintained library) after the Encina middleware ran (limits, tenant gate, resilience, telemetry)** | Language-agnostic as the issue wants; no typed request needed; a mature forwarder instead of custom stream code | A new dependency (`Yarp.ReverseProxy`); the "Encina pipeline" is a middleware pipeline, not `IPipelineBehavior` |
| **B) The sketch: `ProxyAsync<TRequest, TResponse>` mapping the HTTP body to a typed Encina request and running `IEncina.Send`** | Reuses every behavior (validation, authorization) | Requires a mapping from arbitrary HTTP to typed requests per application; the proxied application is not necessarily .NET or Encina-based |
| **C) A and B** | Both audiences | Two designs to maintain; B has no defined mapping |

### Chosen Option: **A — a forwarder with middleware** (recommended, pending the maintainer)

### Rationale

A wins for the issue's own key benefit, "language agnostic: works with any app language"; under A the issue's typed `ProxyAsync<TRequest, TResponse>` and `ForwardAsync<TRequest, TResponse>` shapes become the `HttpContext` shapes below (they return under option B), and the sketched `ISidecarProxy.RegisterAsync(SidecarRegistration)` is not part of the proxy: registering the instance in discovery is `AddEncinaServiceRegistration` of #382. B wins only when the proxied application is an Encina application that wants its handlers invoked by the sidecar, which is what `Encina.AspNetCore` endpoints already do inside the application. YARP is used through `IHttpForwarder` only (no route configuration model, no config file), so its surface in Encina stays small.

**Class:** Dilemma

</details>

<details>
<summary><strong>2. The listener: Kestrel through the library, not an own `HttpListener` (Dilemma D2)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Kestrel is the listener: `MapEncinaSidecar()` maps a catch-all endpoint; the host project is a normal ASP.NET Core app; there is no `EncinaSidecarHost` class** | Streaming, HTTP/2, TLS, limits and graceful shutdown from the platform | The issue's `EncinaSidecarHost` and "HTTP listener" bullets are met by Kestrel, not by new classes |
| **B) `EncinaSidecarHost : BackgroundService` with its own `HttpListener` or an embedded `WebApplication`** | Matches the sketch literally | `HttpListener` lacks the features above; an embedded application inside a hosted service duplicates the host |

### Chosen Option: **A — Kestrel, no `EncinaSidecarHost`** (recommended, pending the maintainer)

### Rationale

A wins because the process is an HTTP server and the platform's server is the maintained one; B wins only if the maintainer wants the class name for documentation reasons. Under B the same forwarder is called from the hosted service instead of an endpoint.

**Class:** Dilemma

</details>

<details>
<summary><strong>3. Target selection: configured only, never from a request</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) The target (`TargetHost`, `TargetPort`) and every ambassador route come from options only; the host is validated with `EndpointValidator` (link-local, metadata and unspecified hosts always rejected); a request can never choose a host, a port or a scheme; an unknown ambassador service is denied (`encina.sidecar.route_not_allowed`, `ErrorCategory.Forbidden`)** | A proxy that follows a header or path to any host is a server-side request forgery vector; this one cannot | The operator lists services up front |
| **B) Accept a target from a header or the path** | Flexible | Open proxy |

### Chosen Option: **A — configured targets, deny by default**

### Rationale

AGENTS.md section 3 requires gates to fail closed when context is missing or a lookup fails, and `EndpointPolicy` rejects link-local, cloud metadata and unspecified hosts with no opt-out. The rule holds under every option of the Dilemmas below because it concerns only where requests may go.

**Class:** Settled
**Settled by:** AGENTS.md section 3 (security gates fail closed); `src/Encina/Validation/EndpointPolicy.cs` and `EndpointValidator.cs`

</details>

<details>
<summary><strong>4. A non-idempotent method is never replayed</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) The sidecar never replays a request whose method is not idempotent (RFC 9110: `POST` and `PATCH` are not); a cancellation is never retried and never a `Left`** | A `POST` is never replayed behind the caller's back (double payment) | `POST` failures reach the client |
| **B) Replay any method on failure** | Simple | Duplicates non-idempotent operations |

### Chosen Option: **A — no replay of non-idempotent methods**

### Rationale

RFC 9110 section 9.2.2 defines which methods are idempotent; replaying others is not safe without an idempotency key, which a pass-through proxy cannot know. Which idempotent requests are retried, when, with what breaker and which defaults is the mechanism of Design Choice 10, not part of this entry.

**Class:** Settled
**Settled by:** RFC 9110 section 9.2.2 (idempotent methods); AGENTS.md section 3 (errors are never swallowed: a failed non-idempotent call is reported, not replayed)

</details>

<details>
<summary><strong>5. Loopback default for the target (Dilemma D5)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `AllowLocalEndpoints` defaults to true: the sidecar's own target is the application in the same pod, reached at `127.0.0.1` by definition (`EndpointPolicy` documents loopback as "for local development and sidecars only"); the ambassador routes still need a non-loopback host unless the operator sets the flag per route** | Works with the issue's defaults (`localhost:8080`) | A loopback target is accepted without an explicit decision |
| **B) Default false, as the NATS options do** | Explicit | The documented default configuration fails validation |

### Chosen Option: **A — loopback allowed for the sidecar's own target** (recommended, pending the maintainer)

### Rationale

A wins because a pod-local target is the sidecar pattern; B wins where reviewers want every loopback target to be a conscious choice. Both keep the always-rejected host kinds.

**Class:** Dilemma

</details>

<details>
<summary><strong>6. mTLS and outbound credentials of the ambassador (Dilemma D6)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) The ambassador's outbound handler is built on `Encina.Http` (#1233): client certificates per (client, tenant), auth providers, rate limiting; this plan adds routing and the allowlist only, and the mTLS bullet is delivered by consuming #1233** | One place for certificates and credentials (private keys, passwords, rotation); no secret handling here | The ambassador's mTLS depends on #1233's code being merged (only its plan is on `main`); `AmbassadorOptions.HttpClientName` names the `Encina.Http` client whose certificate and credentials apply, and the issue's `EnableMTLS` switch is not a property here: mTLS is on or off in that client's own configuration |
| **B) Own client certificate options in `AmbassadorOptions`** | Self-contained | A second implementation of certificates, with key and password options that need `[JsonIgnore]` and `ToString()` hiding; duplicates #1233 |

### Chosen Option: **A — consume `Encina.Http`** (recommended, pending the maintainer)

### Rationale

A wins because #1233 already designs per-tenant certificate handlers and secrets reading; B wins only if the sidecar must ship before #1233. The pull request that needs #1233 (pull request 5) waits for it.

**Class:** Dilemma

</details>

<details>
<summary><strong>7. Docker image and Kubernetes example (Dilemma D7)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) The host project carries .NET SDK container publish settings (`PublishContainer`, a version tag, no `latest`); the docs carry a Kubernetes pod example and a Compose example; pushing the image to a registry is the release pipeline's job (an issue file)** | The image is built the repository's way with no Dockerfile; examples are documentation, tested by a manifest-lint step in the docs pull request | The image is not published by this plan |
| **B) Also a workflow that builds and pushes** | One pass | `.github/workflows/*` belongs to the orchestrator; a registry and credentials are not decided |
| **C) A hand-written Dockerfile** | Familiar | A second build path next to the SDK's |

### Chosen Option: **A — SDK container settings and examples, no publish workflow** (recommended, pending the maintainer)

### Rationale

A wins because it delivers "Docker image configuration" and "Kubernetes deployment example" without deciding where images live; B wins if the maintainer wants the image on a registry in this issue; C if a Dockerfile is preferred. The base image is the SDK default for .NET 10 (Ubuntu based), one source, one tag.

**Class:** Dilemma

</details>

<details>
<summary><strong>8. Tenant gate and headers</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Off by default; when `TenantHeader` is set, a request without a valid value (the syntax and length limit are a default of Design Choice 9, not part of this choice) is denied with `encina.sidecar.tenant_required` (`ErrorCategory.Forbidden`); the value is forwarded unchanged and never logged; a tenant tag appears on telemetry only when this gate is on (ADR-054 D4); the ambassador forwards only header names on an allowlist** | Fail closed when tenancy is configured; pay-for-what-you-use | The gate trusts the network position: the sidecar sits behind the pod boundary, documented |
| **B) On by default** | Safer by default | Breaks every non-tenant deployment |

### Chosen Option: **A — opt-in, deny when configured and missing**

### Rationale

A follows the repository's tenancy rule (a configured gate denies when context is missing; features are opt-in); no tenant-qualified id is built here, so the reserved `default` segment and injectivity do not arise.

**Class:** Settled
**Settled by:** AGENTS.md section 1 (every feature opt-in) and section 3 (gates fail closed); ADR-054 D4

</details>

<details>
<summary><strong>9. Defaults register and the health path (Dilemma D9)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `ListenPort` 8081 (not 3500, which is the default Dapr HTTP endpoint of #387 in the same pod), `TargetHost` `127.0.0.1`, `TargetPort` 8080, `Timeout` 30 seconds, `MaxRequestBodyBytes` 10 MiB, `MaxHeaderCount` 100, tenant header value valid when printable ASCII of at most 128 characters, `HealthPath` `/_encina/sidecar/health` (so the proxied application's own `/health` is never hidden), `EnableHealthCheck` true, `EnableTracing` true, `EnableRetry` true (idempotent methods only, Design Choice 10), `EnableCircuitBreaker` true** | No port clash with Dapr in one pod; every default is written down; the health route cannot shadow the application | 8081 is a convention, not a standard |
| **B) The issue's defaults: 3500 and `/health`** | Matches the issue sketch | Clashes with Dapr's HTTP port when both sidecars run in one pod; hides the application's `/health` |
| **C) No defaults: every value required** | Nothing implicit | A first run needs a page of settings |

### Chosen Option: **A — the register above** (recommended, pending the maintainer)

### Rationale

A wins in clusters that run both sidecars; B wins when the sidecar stands alone and the maintainer prefers the issue's numbers; C when implicit defaults are unwelcome. Each default is validated at start and listed in the README.

**Class:** Dilemma

</details>

<details>
<summary><strong>10. Retry and breaker mechanism and defaults (Dilemma D10)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) A resilience middleware retries `GET`, `HEAD`, `OPTIONS`, `TRACE` (and methods the operator lists in `IdempotentMethods`) only on connection failures before any response byte is received (two retries with jitter); a status answer is never retried because the forwarder streams the response to the caller; a breaker (opens after 5 consecutive failures, stays open 30 seconds) and the timeout apply to every method; `EnableRetry` and `EnableCircuitBreaker` default true** | Simple and safe with a streaming forwarder; the issue's retry and breaker flags are delivered | A 502, 503 or 504 answer of the application reaches the caller unchanged |
| **B) Also retry idempotent requests on 502, 503 and 504 answers, with a buffered request body and a transformer that suppresses the copy of the failed response** | Hides transient application errors | Buffers bodies (memory), needs the transformer, and cannot stream |
| **C) No retry; the breaker and timeout only** | Smallest | The issue's `EnableRetry` is dropped |

### Chosen Option: **A — connection-failure retries for idempotent methods** (recommended, pending the maintainer)

### Rationale

A wins when the application is local to the pod (a connection failure then means it is restarting) and streaming matters; B wins when the application is remote or flaky and the bodies are small; C when the application handles its own retries. Thresholds (2 retries, 5 failures, 30 seconds) and the four flags are written in the options register of Design Choice 9.

**Class:** Dilemma

</details>

---

## Implementation Phases

### Phase 1: Package skeleton, options and errors (pull request 1)

> **Goal**: The `Encina.Sidecar` project, the options, the validator and the error holder.

<details>
<summary>Tasks</summary>

1. `src/Encina.Sidecar/Encina.Sidecar.csproj` (`FrameworkReference Microsoft.AspNetCore.App`, `Yarp.ReverseProxy`, PublicAPI analyzer, `InternalsVisibleTo` for the unit, guard and contract test projects), `PublicAPI.Shipped.txt`, `PublicAPI.Unshipped.txt`, the pin `Yarp.ReverseProxy` 2.3.0 in `Directory.Packages.props`, the `Encina.slnx` entry, the test project references, `.github/coverage-manifest/Encina.Sidecar.json`.
2. `SidecarOptions.cs` (public sealed): `ListenPort`, `TargetHost`, `TargetPort`, `AllowLocalEndpoints` (Design Choice 5), `Timeout`, `EnableRetry` and `EnableCircuitBreaker` (Design Choice 4), `MaxRequestBodyBytes`, `MaxHeaderCount`, `TenantHeader` (`string?`, a valid header token), `HealthPath`, `EnableHealthCheck`, `IdempotentMethods` and the breaker settings of Design Choice 10; the defaults are the register of Design Choices 9 and 10; `ListenPort` is consumed by the host project (Phase 5), which binds Kestrel to it. `AmbassadorOptions.cs`: `Routes` (a read-only list of `AmbassadorRoute(Name, BaseUrl, AllowLocalEndpoints)`), `ForwardHeaderNames` (an allowlist, empty by default), `HttpClientName` (the `Encina.Http` client, Design Choice 6), `EnableTracing` (default true). The project references `Encina` (`EncinaError`, `EndpointValidator`), `Encina.Messaging` (`IEncinaHealthCheck`, the interface every provider health check implements) and `Encina.AspNetCore` (problem details): a dependency decision recorded here, not an accident. The options validator emits no warning for a loopback flag (no EventId exists for it); a loopback target is the documented default of Design Choice 5.
3. `SidecarOptionsValidator.cs` (internal): ports, `EndpointValidator` on the target and each route, unique route names, header token syntax; no warning is emitted for a loopback flag (no EventId exists for it).
4. `SidecarErrors.cs` (internal static): codes with named categories and constant messages: `encina.sidecar.route_not_allowed` (`ErrorCategory.Forbidden`), `encina.sidecar.tenant_required` (`ErrorCategory.Forbidden`), `encina.sidecar.request_rejected` (`ErrorCategory.InvalidRequest`), `encina.sidecar.target_unavailable` (`ErrorCategory.Unavailable`), `encina.sidecar.timeout` (`ErrorCategory.Timeout`), `encina.sidecar.upstream_failed` (`ErrorCategory.UpstreamFailure`).
5. Tests: options and validator unit tests, guard tests; PublicAPI and manifest through `mechanical-fixer`.

</details>

<details>
<summary>Prompt for AI Agents — Phase 1</summary>

```text
CONTEXT:
- Issue #391; plan docs/plans/sidecar-ambassador-implementation-plan-391.md. D1, D2, D5, D6, D7, D9 and D10 are recommended, pending the maintainer: follow the Maintainer Decisions section if it exists.
- Directory.Packages.props has no Yarp.ReverseProxy; add 2.3.0 (NuGet stable) and build.

TASK:
Create the Encina.Sidecar project, options, validator and the error holder with tests.

KEY RULES:
- Targets come from options only and go through EndpointValidator; defaults deny (empty routes, empty header allowlist).
- Every error: EncinaErrors.Create(code, category: ErrorCategory.X, message: <constant>); one category per code.
- No header value, tenant value or URL in any message.

REFERENCE FILES:
- src/Encina.NATS (options, validator, registration)
- src/Encina/Validation/EndpointValidator.cs, EndpointPolicy.cs
- .github/coverage-manifest/Encina.Aspire.Testing.json
```

</details>

---

### Phase 2: The forwarder and registration (pull request 2)

> **Goal**: `ISidecarProxy` forwards through YARP; `AddEncinaSidecar` and `MapEncinaSidecar` register and map it.

<details>
<summary>Tasks</summary>

1. `ISidecarProxy.cs` and `SidecarProxy.cs`: `ForwardAsync(HttpContext context, CancellationToken cancellationToken)` returns `Either<EncinaError, Unit>`; builds the destination from the options only, calls `IHttpForwarder.SendAsync` with a `SocketsHttpHandler`-based invoker (no automatic redirect following, no cookies, `UseProxy = false`), maps a `ForwarderError` to `target_unavailable`, `timeout` or `upstream_failed`; the caller's cancellation is not a `Left`.
2. `ServiceCollectionExtensions.AddEncinaSidecar(this IServiceCollection, Action<SidecarOptions>? = null)` (options with `ValidateOnStart`, `TryAdd` registrations, `AddHttpForwarder()`), and `SidecarEndpointRouteBuilderExtensions.MapEncinaSidecar(this IEndpointRouteBuilder)` (a catch-all endpoint after the health endpoint); the extension maps `Either` to status codes (`Forbidden` 403, `InvalidRequest` 400, `Unavailable` 503, `Timeout` 504, `UpstreamFailure` 502) with a problem body that carries the code only, through `ProblemDetailsExtensions.ToProblemDetails` of `Encina.AspNetCore` (which the package therefore references), and the health endpoint is added in Phase 3.
3. `Diagnostics/SidecarLog.cs`: only 890 forward failed (Warning: error code), emitted by `SidecarProxy`. `EventIdRanges.Sidecar = (890, 899)` registered first, the allocation map, the `Encina` PublicAPI lines, and `docs/engineering/eventid-block-registry.md` as `EventIdRegistryAgreementTests` requires: the "Registered ranges" table (header `Name | Block | Area | Zone`) gains `| Sidecar | 890-899 | Observability and platform | historical |` (Zone is `historical` for every range below 10000; `reserved-pre-zone` is only a Status of the "Blocks" table), and in the "Blocks" table (header `Block | Area | Owner | Status`) the reserved row 890-899 (or the shared 850-899 row, if this pull request lands first) becomes a row with Status `registered`, the rest of 850-899 staying `reserved-pre-zone`. The five aspire-2 plans edit that one shared row: the first to land splits it into one `reserved-pre-zone` row per plan (#424 850-854, #425 855-864, #382 865-877, #387 878-889, #391 890-899, owners `aspire-2: #<n>`); every later pull request changes only its own row, so the edits are disjoint lines; recommended merge order #424, #425, #382, #387, #391. `MapEncinaSidecar` (`SidecarEndpointRouteBuilderExtensions`) is created in pull request 3 with the health endpoint, so pull request 2 stays at 15 files; the proxy is usable without mapping, and its tests call it directly. Each later pull request adds the ids its own classes emit (Phases 3 to 5: 891-896, packed in order), so no log method is declared before its emitter and every target of `SidecarLog.cs` is reachable in the pull request that sets it. There is no "sidecar started" message: with Kestrel as the listener (Design Choice 2) the host already logs it.
4. Tests: unit with `Microsoft.AspNetCore.TestHost` and a fake target (a real YARP forwarder over two in-process servers): streaming body, headers preserved, hop-by-hop headers dropped, unreachable target; guard; a DI test with `ValidateOnBuild` and `ValidateScopes`; a contract suite `ISidecarProxyContractTests` (instantiates the real `SidecarProxy`, pins the error-to-category mapping), so the contract target of `SidecarProxy.cs` is reached in this pull request; PublicAPI and manifest through `mechanical-fixer`.

</details>

<details>
<summary>Prompt for AI Agents — Phase 2</summary>

```text
CONTEXT:
- Issue #391, Phase 2 (pull request 2). Phase 1 is merged. D1 and D2 are recommended, pending the maintainer.

TASK:
Write the proxy, the registration and mapping, the first log message (890) and the EventId range, with tests and the contract suite.

KEY RULES:
- Register EventIdRanges.Sidecar first; EventIds packed from 890.
- The destination is built from options only; a request never selects a host, port or scheme.
- Cancellation is never a Left; log codes and integers only, no URL, header or body.
- Singletons take no scoped dependency.

REFERENCE FILES:
- src/Encina.AspNetCore (error to status mapping, endpoint extensions)
- src/Encina/Diagnostics/EventIdRanges.cs
- tests/Encina.UnitTests/Testing/Architecture/EncinaEventIdAllocationTests.cs
```

</details>

---

### Phase 3: Limits, tenant gate and health (pull request 3), then resilience and telemetry (pull request 4)

> **Goal**: Request limits, the optional tenant gate and the combined health check (pull request 3); safe retries, the breaker and diagnostics (pull request 4).

<details>
<summary>Tasks</summary>

1. Pull request 3: `Middleware/SidecarLimitsMiddleware.cs`: body size and header count; rejects with `request_rejected` before forwarding (log 891 request rejected, Debug: error code).
2. Pull request 3: `Middleware/SidecarTenantMiddleware.cs`: Design Choice 8 (log 892 request denied by the tenant gate, Debug: no value).
3. Pull request 3: `Health/SidecarHealthCheck.cs` (`IEncinaHealthCheck`, from `Encina.Messaging`, which the package references because every provider health check implements that interface; `DefaultName` const, `Tags` static array): the target's reachability and the sidecar's own state (log 893 target health probe failed, Warning: code). `MapEncinaSidecar` maps the health endpoint at `SidecarOptions.HealthPath` (default `/_encina/sidecar/health`, so the proxied application's own `/health` is never hidden) before the catch-all. `SidecarLog`, the extension class, PublicAPI and manifest are edited (counted as modified).
4. Pull request 4: `Middleware/SidecarResilienceMiddleware.cs`: a Polly pipeline built once per options instance: a timeout from `Timeout`, a circuit breaker, and a retry only for safe methods and only for **connection failures before any response byte is received** (the forwarder streams the upstream response into `HttpContext.Response`, so a status-based retry would need a buffered body and a transformer that suppresses the copy; the plan does not retry on 502, 503 and 504 answers, which reach the caller unchanged); a request whose body was already read is never retried (log 894 circuit opened, Warning).
5. Pull request 4: `Diagnostics/SidecarDiagnostics.cs`: `ActivitySource` and `Meter` `Encina.Sidecar`, span `encina.sidecar.forward`, counter `encina.sidecar.requests` with tags `outcome` and `method`, where `method` is normalized to the nine standard HTTP methods plus `other` (the client controls the method token, so it is never used raw); the tenant tag only when the gate is on, and only after the value passed validation (the header is unauthenticated, so a spoofed value can raise tag cardinality: the limit is documented and the tag stays off by default). `Encina.OpenTelemetry` subscribes the source and the meter (one edit of `src/Encina.OpenTelemetry/ServiceCollectionExtensions.cs`, whose manifest entry gains a unit test that builds a tracer and meter provider and asserts both are subscribed).
6. Tests: unit (limits, tenant allow and deny, health branches in pull request 3; retry on GET before a response but not on POST, no retry on a 503 answer, breaker opens, tags and method normalization in pull request 4), guard; manifests and PublicAPI through `mechanical-fixer`.

</details>

<details>
<summary>Prompt for AI Agents — Phase 3</summary>

```text
CONTEXT:
- Issue #391, Phase 3 (pull requests 3 and 4). Phase 2 is merged. D4 and D8 are Settled in the plan.

TASK:
Pull request 3: limits and tenant middleware, the health check. Pull request 4: the resilience middleware, the diagnostics and the OpenTelemetry subscription. Each with tests; each adds only the SidecarLog ids its own classes emit.

KEY RULES:
- Retry only idempotent methods, only before any response byte, never on a status answer; never retry a cancellation.
- The metric method tag is normalized to the standard methods plus other.
- With the tenant gate on, a missing or invalid header denies; never log the value; tenant tag only when the gate is on.
- Health check: DefaultName const, Tags static array.

REFERENCE FILES:
- src/Encina.Extensions.Resilience, src/Encina.Polly
- src/Encina.OpenTelemetry/ServiceCollectionExtensions.cs
- src/Encina.NATS/Health/NATSHealthCheck.cs
```

</details>

---

### Phase 4: The ambassador (pull request 5)

> **Goal**: `IAmbassadorProxy` forwards to a named, allowlisted service through #1233's outbound client.

<details>
<summary>Tasks</summary>

1. `IAmbassadorProxy.cs` and `AmbassadorProxy.cs`: `ForwardAsync(HttpContext context, string targetService, CancellationToken)`: looks the service up in `AmbassadorOptions.Routes` (deny when absent), forwards only the allowlisted header names, uses the named `HttpClient` that `Encina.Http` configures per client (client certificates, auth providers, rate limits; Design Choice 6), and resolves a route without a fixed `BaseUrl` through `IServiceDiscoveryProvider` of #607 when registered. **One source for the destination**: the route's `BaseUrl` or the discovery result, never the named client's own `BaseAddress` (the named client contributes only its handler: certificate, credentials, rate limit). **Every destination is validated before use**: the route `BaseUrl` at start, and each discovered endpoint on every resolution, with `EndpointValidator.ValidateHost` under a per-route `EndpointPolicy` (link-local, metadata and unspecified hosts always rejected; loopback and private hosts only when the route sets `AllowLocalEndpoints`), because a registry entry or a hostname that resolves to a metadata address must not be reachable (`route_not_allowed`, `ErrorCategory.Forbidden`, log 895 destination rejected: no host in the message). The ambassador's outbound handler is built with `AllowAutoRedirect = false` (a redirect could leave the validated destination); the check that the named client was configured so fails the start (`ValidateOnStart`). Log 896 ambassador forward failed (Warning: error code); both ids are added by this pull request, which edits `SidecarLog`.
2. Registration: `AddEncinaAmbassador(this IServiceCollection, Action<AmbassadorOptions>)` (the parameter is not nullable; an empty route list is a start error).
3. Tests: unit with fake routes and a fake outbound handler, guard, DI; contract: the `ISidecarProxy` and `IAmbassadorProxy` suites check the same error mapping; PublicAPI and manifests through `mechanical-fixer`.

</details>

<details>
<summary>Prompt for AI Agents — Phase 4</summary>

```text
CONTEXT:
- Issue #391, Phase 4 (pull request 5). The code of #1233 and #607 (only their plans are on main today) is merged before this pull request. D6 is recommended, pending the maintainer.

TASK:
Write the ambassador, its registration and tests.

KEY RULES:
- Unknown service: deny with route_not_allowed; the request never picks a URL; forward only allowlisted header names.
- Outbound credentials and certificates come from Encina.Http, never from this package's options.
- Validate every route and every discovered endpoint with EndpointValidator; no redirects; the destination is never the named client's BaseAddress.
- Cancellation is never a Left.

REFERENCE FILES:
- docs/plans/http-implementation-plan-1233.md, docs/plans/service-discovery-implementation-plan-607.md
- src/Encina.Sidecar (Phases 1-3)
```

</details>

---

### Phase 5: The host project and the image (pull request 6)

> **Goal**: A runnable sidecar process and its container settings.

<details>
<summary>Tasks</summary>

1. `src/Encina.Sidecar.Host/Encina.Sidecar.Host.csproj` (`OutputType Exe`, `IsPackable false`, `Microsoft.NET.Sdk.Web`, container publish properties: repository name, tag from the project version, no `latest`), `Program.cs` (minimal hosting: `AddEncinaSidecar` bound from the `Encina:Sidecar` configuration section and environment variables, `ConfigureKestrel` listening on `SidecarOptions.ListenPort` (the only consumer of that option), `MapEncinaSidecar`), the `Encina.slnx` entry.
2. Environment names documented for the container: `Encina__Sidecar__TargetPort`, `Encina__Sidecar__TargetHost`, and so on, bound by `IConfiguration` (no custom parser).
3. Tests: a unit-level test (`WebApplicationFactory`-style over `TestHost`) that builds the host with in-memory configuration and forwards one request; it needs `Program` visible: `public partial class Program` is declared at the end of `Program.cs` (the standard pattern), so no `InternalsVisibleTo` is needed. The documentation pull request validates the Kubernetes and Compose examples.
4. The host project is production code with a `Program.cs`; its coverage target is stated in Phase 9.

</details>

<details>
<summary>Prompt for AI Agents — Phase 5</summary>

```text
CONTEXT:
- Issue #391, Phase 5 (pull request 6). Pull requests 1 to 4 are merged. D7 is recommended, pending the maintainer.

TASK:
Create the host project with container publish settings and a minimal Program.cs, with a host-level test.

KEY RULES:
- Configuration comes from IConfiguration; no secret is baked into the image; the image tag is the version, never latest.
- Do not touch .github/workflows/*; publishing is the release pipeline's job (issue file).

REFERENCE FILES:
- src/Encina.Cli (an executable project in this repository)
- Directory.Build.props
```

</details>

---

### Phase 6: Cross-Cutting Integration

> **Goal**: Evaluate the 12 functions.

<details>
<summary>Tasks</summary>

1. Telemetry, logging, health, resilience, validation and tenancy are wired in Phases 2 to 4; caching is deferred as in the issue; idempotency is handled by refusing to retry unsafe methods.
2. Fail closed: unknown routes, missing tenant when gated, oversized requests, an invalid target at start.

</details>

<details>
<summary>Prompt for AI Agents — Phase 6</summary>

```text
CONTEXT:
- Issue #391; Phases 1-5 exist.

TASK:
Check the matrix against the code and fix gaps; add no feature.

KEY RULES:
- Gates fail closed; no values in logs; no scoped service in a singleton.

REFERENCE FILES:
- AGENTS.md section 6
- src/Encina.Sidecar
```

</details>

---

### Phase 7: Observability

> **Goal**: Logs, a span and a counter.

<details>
<summary>Tasks</summary>

1. Confirm ids 890-896 are packed inside 890-899 (897-899 spare), that the architecture tests pass and that the source and meter are subscribed in `Encina.OpenTelemetry`; no tag carries a URL, header value, route target or tenant value (tenant only when the gate is on, ADR-054 D4).

</details>

<details>
<summary>Prompt for AI Agents — Phase 7</summary>

```text
CONTEXT:
- Issue #391, Phase 7. Phases 2 and 3 created the logs and diagnostics.

TASK:
Run the allocation and uniqueness architecture tests and fix any gap.

KEY RULES:
- No id outside 890-899; no sparse ids.

REFERENCE FILES:
- src/Encina/Diagnostics/EventIdRanges.cs
- tests/Encina.UnitTests/Testing/Architecture/EncinaEventIdAllocationTests.cs
```

</details>

---

### Phase 8: Testing

> **Goal**: Every flag reaches its target in the pull request that sets it.

<details>
<summary>Tasks</summary>

1. Unit, guard, DI and contract tests per pull request; property: not applicable (no complex domain logic); integration: the host test of Phase 5 plus a container smoke test is not added (a container runtime is not required for unit coverage); load and benchmark: justified by `.md` files in the documentation pull request (the sidecar is a thin forwarder; throughput is YARP's).
2. **Per-file targets** (new files; each reached by its own pull request):

   | File (pull request) | Unit | Guard | Contract | Property | Integration | Justification |
   |------|-----:|------:|---------:|---------:|------------:|---------------|
   | `SidecarOptions.cs`, `AmbassadorOptions.cs`, validator (1) | 95 | 90 | 0 | 0 | 0 | Every rule and default asserted |
   | `SidecarErrors.cs` (1) | 100 | 0 | 0 | 0 | 0 | Constants; internal, no guarded parameter |
   | `SidecarProxy.cs` (2) | 92 | 85 | 85 | 0 | 0 | Real YARP over in-process servers; the contract suite pins the error mapping |
   | `SidecarLimitsMiddleware.cs` (3) | 95 | 85 | 0 | 0 | 0 | Each limit and the pass-through |
   | `ServiceCollectionExtensions.cs` (2) | 95 | 90 | 0 | 0 | 0 | Registration, guards and the DI test |
   | `SidecarEndpointRouteBuilderExtensions.cs` (3) | 95 | 90 | 0 | 0 | 0 | Created in pull request 3 with the health endpoint: the mapping, the status mapping through `ToProblemDetails` and the guards, run over `TestHost` |
   | `ISidecarProxy.cs` (2), `IAmbassadorProxy.cs` (5) | 0 | 0 | 0 | 0 | 0 | Interfaces with no executable code; covered through `SidecarProxy` and `AmbassadorProxy` and their contract suites |
   | `AmbassadorRoute.cs` (1, record) | 0 | 0 | 0 | 0 | 0 | A record with no executable logic; read by the validator and ambassador tests (generated record members are not coverable lines) |
   | `SidecarResilienceMiddleware.cs` (4) | 90 | 85 | 0 | 0 | 0 | Retry, no retry, breaker and timeout with a fake time source |
   | `SidecarTenantMiddleware.cs` (3) | 95 | 85 | 0 | 0 | 0 | Allow, deny, invalid value |
   | `SidecarHealthCheck.cs` (3) | 95 | 85 | 0 | 0 | 0 | Healthy, unreachable, exception |
   | `SidecarDiagnostics.cs` (4), `SidecarLog.cs` (2 to 5) | 90 | 0 | 0 | 0 | 0 | Listener-based tests of tags; generated methods called by the tests |
   | `AmbassadorProxy.cs` (5) | 92 | 85 | 85 | 0 | 0 | Fake routes and outbound handler; the shared suite |
   | `Encina.OpenTelemetry` `ServiceCollectionExtensions.cs` (4, edit) | 90 (current, kept) | 35 (current, kept) | 0 | 0 | 0 | Two subscription lines added; the new unit test asserts both subscriptions; the manifest's current unit 90 and guard 35 are kept, not lowered |
   | `EventIdRanges.cs` (2, edit) | 95 (current, kept) | 0 | 0 | 0 | 0 | One new field read by the allocation architecture test |
   | `Program.cs` of the host (6) | 70 | 0 | 0 | 0 | 0 | Top-level statements run by the host-level test; no guarded parameter |

3. Run `crap-gate` locally (every touched method CRAP <= 10) and `coverage-report.cs --check-justifications`; never lower a manifest target.

</details>

<details>
<summary>Prompt for AI Agents — Phase 8</summary>

```text
CONTEXT:
- Issue #391, Phase 8. Each pull request carries the tests that reach its targets.

TASK:
Measure per-flag coverage, close gaps, run crap-gate and the justification check.

KEY RULES:
- Tests execute real code (real YARP over TestHost); guard tests in tests/Encina.GuardTests; contract suites instantiate real proxies.
- Never swallow an exception thrown by the code under test.

REFERENCE FILES:
- .github/coverage-manifest/Encina.json
- docs/testing/coverage-measurement-methodology.md
```

</details>

---

### Phase 9: Documentation & Finalization (pull request 7)

> **Goal**: A user can run the sidecar in Kubernetes and Compose.

<details>
<summary>Tasks</summary>

1. XML docs with `<example>`; README of `Encina.Sidecar` and `Encina.Sidecar.Host`; a guide with the Kubernetes pod example and the Compose example (through `docs-writer`; the manifests are linted in the documentation pull request); `docs/INVENTORY.md`; `changelog.d/391-sidecar.added.md` (through `mechanical-fixer`); load and benchmark justification files.
2. Issue files of this run: the image publish pipeline and the Aspire co-location (`WithEncinaSidecar` reusing #421's processor model).

</details>

<details>
<summary>Prompt for AI Agents — Phase 9</summary>

```text
CONTEXT:
- Issue #391, Phase 9 (pull request 7). Code merged.

TASK:
Write the READMEs, the guide with the two deployment examples, the inventory lines and the changelog fragment.

KEY RULES:
- No hand-typed coverage figures; English only; no private application names; image tags are versions, never latest.

REFERENCE FILES:
- .claude/skills/encina-docs/SKILL.md
- changelog.d/README.md
```

</details>

---

### Phase 10: Pull request split

> **Goal**: Each pull request builds and passes alone.

<details>
<summary>Tasks</summary>

1. The split below is the authority; a pull request that measures over 15 files or 1,500 lines when briefed is split again (pull request 2 is the closest).

</details>

<details>
<summary>Prompt for AI Agents — Phase 10</summary>

```text
CONTEXT:
- Issue #391, Phase 10.

TASK:
Before briefing each pull request, count its files and lines with git diff --shortstat and split if over the limits.

KEY RULES:
- Count tests, manifests, PublicAPI, csproj references, allocation map, DI tests and fixtures.

REFERENCE FILES:
- AGENTS.md section 11
```

</details>

---

### Proposed split into pull requests (limit 15 files and 1500 lines each)

| PR | Content | Files | Approx. lines |
|----|---------|------:|--------------:|
| 1 | `Directory.Packages.props` edit, project file, two PublicAPI files, `Encina.slnx` edit, `SidecarOptions`, `AmbassadorOptions`, validator, `SidecarErrors`, manifest, three test project references (the project file carries the `Encina`, `Encina.Messaging` and `Encina.AspNetCore` references), unit and guard test files, counted file by file: props, project, two PublicAPI, slnx, three options and validator files (`SidecarOptions`, `AmbassadorOptions`, validator), `SidecarErrors`, manifest, three csproj edits, unit, guard | 15 | 1,000 |
| 2 | `ISidecarProxy`, `SidecarProxy`, extension class (`AddEncinaSidecar`; the endpoint mapping moves to pull request 3 to stay within 15 files), `SidecarLog`, `EventIdRanges.cs` edit, `docs/engineering/eventid-block-registry.md` edit (row for `Sidecar` 890-899 in "Registered ranges" and the split of the reserved 850-899 row, required by `EventIdRegistryAgreementTests`), `Encina` PublicAPI, `Encina.json`, allocation map edit, Sidecar PublicAPI edit, Sidecar manifest edit, one unit test file, guard test, DI test, `ISidecarProxyContractTests` | 15 | 1,300 |
| 3 | `SidecarLimitsMiddleware`, `SidecarTenantMiddleware`, `SidecarHealthCheck`, `SidecarEndpointRouteBuilderExtensions` (`MapEncinaSidecar`: health endpoint and catch-all), extension class edit (registrations), `SidecarLog` edit (891-893), Sidecar PublicAPI and manifest edits, unit tests (limits and tenant in one file, health in one), guard test edit, DI test edit | 12 | 950 |
| 4 | `SidecarResilienceMiddleware`, `SidecarDiagnostics`, extension class edit (registration), `Encina.OpenTelemetry` `ServiceCollectionExtensions.cs` edit, `Encina.OpenTelemetry.json` manifest edit, subscription unit test, `SidecarLog` edit (894), Sidecar PublicAPI and manifest edits, unit tests (2), guard test edit | 12 | 1,100 |
| 5 | `IAmbassadorProxy`, `AmbassadorProxy`, extension class edit (`AddEncinaAmbassador`), `SidecarLog` edit (895-896), project file edit (references to `Encina.Http` and `Encina.ServiceDiscovery`), Sidecar PublicAPI and manifest edits, unit, guard, DI and contract tests, two test project reference edits | 13 | 1,100 |
| 6 | Host csproj, `Program.cs`, `Encina.slnx` edit, host-level test, host manifest, test project reference (no public API, so no PublicAPI file) | 6 | 450 |
| 7 | Two READMEs, guide page, inventory line, changelog fragment, two justification files, one more doc file for the examples | 8 | 420 |

PR 1 needs nothing; PR 2 needs PR 1; PR 3 needs PR 2; PR 4 needs PR 3; PR 5 needs PR 2 and the code of #1233 and #607 (only their plans are on `main` today); PR 6 needs PR 4; PR 7 is last. No interface of another package is changed, so no implementer or test double is touched. Every pull request is counted from the file names of its row, not estimated; PR 1 and PR 2 sit exactly at 15.

---

## Research

### Relevant Standards & Specifications

| Reference | What it defines | Use in this feature |
|-----------|-----------------|---------------------|
| `AGENTS.md` sections 1, 3, 6 | Opt-in, fail closed, errors by code, 12 functions | Design Choices 3 and 8 |
| ADR-021, ADR-054 (D4), ADR-062 | EventIds, tenant tag only when tenancy is on, error categories | Phases 1 to 3 and 7 |
| RFC 9110 section 9.2.2 | Idempotent methods | Design Choice 4 |
| SPEC-000 section 6, DEC-009 | Local-container features are in 1.0 | Scope |
| YARP `IHttpForwarder` | Forwarding, header transforms, streaming | Phase 2 |

### Existing Encina Infrastructure to Leverage

| Component | Location | Usage in this feature |
|-----------|----------|-----------------------|
| `EndpointValidator`, `EndpointPolicy` | `src/Encina/Validation/` | Target and route validation |
| `Encina.Http` | #1233 | Ambassador outbound client, certificates, auth |
| `IServiceDiscoveryProvider` | #607 | Routes without a fixed address |
| `Microsoft.Extensions.Http.Resilience`, Polly | `Directory.Packages.props` | Retry, breaker, timeout |
| `IEncinaHealthCheck` | `src/Encina.Messaging/Health/` | Health check |
| `Encina.OpenTelemetry` source and meter lists | `src/Encina.OpenTelemetry/ServiceCollectionExtensions.cs` | Subscription |
| `Encina.Cli` | `src/Encina.Cli` | An executable project to copy conventions from |

### Event ID Allocation

| Package | Range | Notes |
|---------|-------|-------|
| `Encina.Sidecar` (`Sidecar`, new) | 890-899, ids 890-896 used (890 PR 2; 891-893 PR 3; 894 PR 4; 895-896 PR 5) | From the block 850-899 (aspire-2); registered in `EventIdRanges.cs` and the allocation test map before use |

### Estimated File Count

| Category | Files | Notes |
|----------|------:|-------|
| Production code | 16 | Library (14), host (2) |
| Edits of shared files | 5 | `Directory.Packages.props`, `Encina.slnx`, `EventIdRanges.cs`, `Encina.OpenTelemetry`, project references |
| Public API and manifests | 6 | `Encina.Sidecar` PublicAPI and manifest, `Encina` PublicAPI, `Encina.json`, host manifest |
| Tests and project files | 16 | Unit, guard, DI, contract, host test, allocation map, csproj references |
| Documentation | 7 | Two READMEs, guide, inventory, fragment, two justification files |
| **Total** | **by the split table: 15 + 15 + 12 + 12 + 13 + 6 + 8 = 81 file touches over 7 pull requests** | The split table is the authority |

---

## Combined AI Agent Prompts

<details>
<summary><strong>Full combined prompt for all phases</strong></summary>

```text
PROJECT CONTEXT:
- Encina is a pre-1.0 .NET 10 / C# 14 library; Either<EncinaError, T>; no [Obsolete], no compatibility layers.
- Issue #391: sidecar and ambassador. Plan: docs/plans/sidecar-ambassador-implementation-plan-391.md; D1, D2, D5, D6, D7, D9 and D10 are recommended, pending the maintainer.

IMPLEMENTATION OVERVIEW (7 pull requests, each builds alone):
1. Encina.Sidecar skeleton, options, validator, errors (Phase 1).
2. The YARP forwarder, registration, log 890 and the range, with the contract suite (Phase 2).
3. Limits, tenant gate, health check (Phase 3).
4. Resilience, diagnostics, OpenTelemetry subscription (Phase 3).
5. The ambassador on Encina.Http and discovery (Phase 4).
6. The host project and container settings (Phase 5). 7. Documentation (Phase 9).

KEY PATTERNS:
- Targets from options only, validated with EndpointValidator; a request never chooses a host; unknown routes denied.
- Retry only idempotent methods; cancellation is never a Left.
- EncinaErrors.Create(code, category: ErrorCategory.X, message: <constant>); no URL, header or tenant value in logs or tags.
- Tenant gate opt-in and fail closed; tenant tag only when it is on.
- EventIds registered first: Sidecar 890-899.

REFERENCE FILES:
- src/Encina.NATS, src/Encina/Validation/EndpointValidator.cs
- docs/plans/http-implementation-plan-1233.md, docs/plans/service-discovery-implementation-plan-607.md
- src/Encina/Diagnostics/EventIdRanges.cs
- .github/coverage-manifest/Encina.json
```

</details>

---

## Cross-Cutting Integration Matrix

| # | Function | Status | Notes |
|---|----------|--------|-------|
| 1 | Caching | ⏭️ | Deferred as in the issue (sidecar response caching); the issue file of this run for sidecar follow-ups lists it |
| 2 | OpenTelemetry | ✅ | Phase 3: `Encina.Sidecar` source and meter; W3C context forwarded by YARP; tenant tag only when the gate is on (ADR-054 D4) |
| 3 | Structured Logging | ✅ | Phases 2 to 4: EventIds 890-896 |
| 4 | Health Checks | ✅ | Phase 3: `SidecarHealthCheck` (target and self); probe endpoints of Kubernetes are #454's |
| 5 | Validation | ✅ | Options, targets and request limits; body validation is not applicable (infrastructure pattern, as the issue says) |
| 6 | Resilience | ✅ | Phase 3: timeout, breaker, retry for idempotent methods only |
| 7 | Distributed Locks | ❌ | Not applicable: a stateless proxy |
| 8 | Transactions | ❌ | Not applicable: no store |
| 9 | Idempotency | ✅ | Design Choice 4: unsafe methods are never replayed (the proxy adds no deduplication) |
| 10 | Multi-Tenancy | ✅ | Design Choice 8: an opt-in gate that denies when configured and missing; no tenant-qualified id is built |
| 11 | Module Isolation | ❌ | Not applicable: per service by design, as the issue states |
| 12 | Audit Trail | ⏭️ | Deferred as in the issue (proxy request audit); the issue file of this run for sidecar follow-ups lists it |

---

## Prerequisites & Dependencies

- Pull requests 1 to 4 and 6 need nothing outside the repository; pull request 5 needs the code of #1233 and #607 merged (only their plans are on `main` today).
- `Yarp.ReverseProxy` 2.3.0 is added to `Directory.Packages.props` (the #383 plan does not use YARP).
- Issue files of this run: the image publish pipeline (registry and tags), Aspire co-location through #421's processor model, and the deferred caching and audit items.

---

## Next Steps

1. The maintainer answers D1, D2, D5, D6, D7, D9 and D10 when work on #391 starts; the answers go into `## Maintainer Decisions` and the markers are removed.
2. Pull requests 1 to 3 in order; 4 after #1233 and #607; 5; 6 last.
