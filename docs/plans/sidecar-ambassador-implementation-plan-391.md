# Implementation Plan: `Encina.Sidecar` — a sidecar and ambassador forwarding host with Encina cross-cutting behavior

> **Issue**: [#391](https://github.com/dlrivada/Encina/issues/391)
> **Type**: Feature
> **Complexity**: High (10 phases, 6 pull requests of 14, 15, 14, 12, 9 and 7 files)
> **Estimated Scope**: ~900 lines of production code + ~2,400 lines of tests
> **Family**: Aspire (aspire-2): #424, #425, #382, #387. Related and merged on `main` as plans: #1233 (`Encina.Http`: outbound client, client certificates per client and tenant, auth providers), #607 (service discovery), #421 (the processor model for deployment publishers), #383 (BFF, in-process aggregation, no YARP), #454 (health probes). **This plan owns the `Encina.Sidecar` and `Encina.Sidecar.Host` projects, the `Yarp.ReverseProxy` pin and EventIds 890-899.**

---

## Summary

The issue asks for `ISidecarProxy`, `IAmbassadorProxy`, an `EncinaSidecarHost` background service with an HTTP listener, health check integration, a Kubernetes example, Docker image configuration and tests. State of `origin/main` (`098caa920`), verified by reading and grep, and of the NuGet feed (read 2026-11-20):

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

A wins for the issue's own key benefit, "language agnostic: works with any app language"; B wins only when the proxied application is an Encina application that wants its handlers invoked by the sidecar, which is what `Encina.AspNetCore` endpoints already do inside the application. YARP is used through `IHttpForwarder` only (no route configuration model, no config file), so its surface in Encina stays small.

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
<summary><strong>4. Retries only for safe methods</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) The resilience middleware retries only `GET`, `HEAD`, `OPTIONS` and `TRACE` (and a method the operator lists as idempotent) on connection failures and 502, 503 and 504 answers; a circuit breaker and a timeout apply to every method; a cancellation is never retried and never a `Left`** | A `POST` is never replayed behind the caller's back (double payment); the issue's `EnableRetry` and `EnableCircuitBreaker` are delivered safely | `POST` failures reach the client |
| **B) Retry everything** | Simple | Duplicates non-idempotent operations |

### Chosen Option: **A — retry idempotent methods only**

### Rationale

RFC 9110 section 9.2.2 defines which methods are idempotent; replaying others is not safe without an idempotency key, which a pass-through proxy cannot know.

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
| **A) The ambassador's outbound handler is built on `Encina.Http` (#1233): client certificates per (client, tenant), auth providers, rate limiting; this plan adds routing and the allowlist only, and the mTLS bullet is delivered by consuming #1233** | One place for certificates and credentials (private keys, passwords, rotation); no secret handling here | The ambassador's mTLS depends on #1233 being merged; `EnableMTLS` is a switch over #1233's options |
| **B) Own client certificate options in `AmbassadorOptions`** | Self-contained | A second implementation of certificates, with key and password options that need `[JsonIgnore]` and `ToString()` hiding; duplicates #1233 |

### Chosen Option: **A — consume `Encina.Http`** (recommended, pending the maintainer)

### Rationale

A wins because #1233 already designs per-tenant certificate handlers and secrets reading; B wins only if the sidecar must ship before #1233. The pull request that needs #1233 (pull request 4) waits for it.

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
| **A) Off by default; when `TenantHeader` is set, a request without a valid value (printable ASCII, at most 128 characters) is denied with `encina.sidecar.tenant_required` (`ErrorCategory.Forbidden`); the value is forwarded unchanged and never logged; a tenant tag appears on telemetry only when this gate is on (ADR-054 D4); the ambassador forwards only header names on an allowlist** | Fail closed when tenancy is configured; pay-for-what-you-use | The gate trusts the network position: the sidecar sits behind the pod boundary, documented |
| **B) On by default** | Safer by default | Breaks every non-tenant deployment |

### Chosen Option: **A — opt-in, deny when configured and missing**

### Rationale

A follows the repository's tenancy rule (a configured gate denies when context is missing; features are opt-in); no tenant-qualified id is built here, so the reserved `default` segment and injectivity do not arise.

**Class:** Settled
**Settled by:** AGENTS.md section 1 (every feature opt-in) and section 3 (gates fail closed); ADR-054 D4

</details>

---

## Implementation Phases

### Phase 1: Package skeleton, options and errors (pull request 1)

> **Goal**: The `Encina.Sidecar` project, the options, the validator and the error holder.

<details>
<summary>Tasks</summary>

1. `src/Encina.Sidecar/Encina.Sidecar.csproj` (`FrameworkReference Microsoft.AspNetCore.App`, `Yarp.ReverseProxy`, PublicAPI analyzer, `InternalsVisibleTo` for the unit, guard and contract test projects), `PublicAPI.Shipped.txt`, `PublicAPI.Unshipped.txt`, the pin `Yarp.ReverseProxy` 2.3.0 in `Directory.Packages.props`, the `Encina.slnx` entry, the test project references, `.github/coverage-manifest/Encina.Sidecar.json`.
2. `SidecarOptions.cs` (public sealed): `ListenPort` (default 3500, 1-65535), `TargetHost` (default `127.0.0.1`), `TargetPort` (default 8080), `AllowLocalEndpoints` (Design Choice 5), `Timeout` (default 30 seconds), `EnableRetry` and `EnableCircuitBreaker` (default true for safe methods only, Design Choice 4), `MaxRequestBodyBytes` (default 10 MiB), `MaxHeaderCount` (default 100), `TenantHeader` (`string?`, a valid header token), `EnableHealthCheck` (default true). `AmbassadorOptions.cs`: `Routes` (a read-only list of `AmbassadorRoute(Name, BaseUrl, AllowLocalEndpoints)`), `ForwardHeaderNames` (an allowlist, empty by default), `EnableTracing` (default true).
3. `SidecarOptionsValidator.cs` (internal): ports, `EndpointValidator` on the target and each route, unique route names, header token syntax; a warning once when a loopback flag is set.
4. `SidecarErrors.cs` (internal static): codes with named categories and constant messages: `encina.sidecar.route_not_allowed` (`ErrorCategory.Forbidden`), `encina.sidecar.tenant_required` (`ErrorCategory.Forbidden`), `encina.sidecar.request_rejected` (`ErrorCategory.InvalidRequest`), `encina.sidecar.target_unavailable` (`ErrorCategory.Unavailable`), `encina.sidecar.timeout` (`ErrorCategory.Timeout`), `encina.sidecar.upstream_failed` (`ErrorCategory.UpstreamFailure`).
5. Tests: options and validator unit tests, guard tests; PublicAPI and manifest through `mechanical-fixer`.

</details>

<details>
<summary>Prompt for AI Agents — Phase 1</summary>

```text
CONTEXT:
- Issue #391; plan docs/plans/sidecar-ambassador-implementation-plan-391.md. D1, D2, D5, D6 and D7 are recommended, pending the maintainer: follow the Maintainer Decisions section if it exists.
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

### Phase 2: The forwarder, limits and registration (pull request 2)

> **Goal**: `ISidecarProxy` forwards through YARP after the limits; `AddEncinaSidecar` and `MapEncinaSidecar` register and map it.

<details>
<summary>Tasks</summary>

1. `ISidecarProxy.cs` and `SidecarProxy.cs`: `ForwardAsync(HttpContext context, CancellationToken cancellationToken)` returns `Either<EncinaError, Unit>`; builds the destination from the options only, calls `IHttpForwarder.SendAsync` with a `SocketsHttpHandler`-based invoker (no automatic redirect following, no cookies, `UseProxy = false`), maps a `ForwarderError` to `target_unavailable`, `timeout` or `upstream_failed`; the caller's cancellation is not a `Left`.
2. `Middleware/SidecarLimitsMiddleware.cs`: body size and header count; rejects with `request_rejected` before forwarding.
3. `ServiceCollectionExtensions.AddEncinaSidecar(this IServiceCollection, Action<SidecarOptions>? = null)` (options with `ValidateOnStart`, `TryAdd` registrations, `AddHttpForwarder()`), and `SidecarEndpointRouteBuilderExtensions.MapEncinaSidecar(this IEndpointRouteBuilder)` (a catch-all endpoint after the health endpoint); the extension maps `Either` to status codes (`Forbidden` 403, `InvalidRequest` 400, `Unavailable` 503, `Timeout` 504, `UpstreamFailure` 502) with a problem body that carries the code only, reusing the `Encina.AspNetCore` mapping if one exists (verify at this phase).
4. `Diagnostics/SidecarLog.cs`: 890 forward failed (Warning: error code), 891 request rejected (Debug: error code), 892 sidecar started (Information: listen and target ports as integers). `EventIdRanges.Sidecar = (890, 899)` registered first, the allocation map, the `Encina` PublicAPI lines; ids 890-892 packed now; 893-894 are used in Phase 3.
5. Tests: unit with `Microsoft.AspNetCore.TestHost` and a fake target (a real YARP forwarder over two in-process servers): streaming body, headers preserved, hop-by-hop headers dropped, size limit, unreachable target; guard; a DI test with `ValidateOnBuild` and `ValidateScopes`; PublicAPI and manifest through `mechanical-fixer`.

</details>

<details>
<summary>Prompt for AI Agents — Phase 2</summary>

```text
CONTEXT:
- Issue #391, Phase 2 (pull request 2). Phase 1 is merged. D1 and D2 are recommended, pending the maintainer.

TASK:
Write the proxy, the limits middleware, the registration and mapping, the first log messages and the EventId range, with tests.

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

### Phase 3: Resilience, tenant gate, health and telemetry (pull request 3)

> **Goal**: Safe retries, the optional tenant gate, the combined health check and diagnostics.

<details>
<summary>Tasks</summary>

1. `Middleware/SidecarResilienceMiddleware.cs`: a Polly pipeline (timeout from `Timeout`, retry for safe methods on connection failure and 502, 503, 504 with jitter, a circuit breaker) built once per options instance; a request body that cannot be replayed is never retried.
2. `Middleware/SidecarTenantMiddleware.cs`: Design Choice 8.
3. `Health/SidecarHealthCheck.cs` (`IEncinaHealthCheck`; `DefaultName` const, `Tags` static array): the target's reachability and the sidecar's own state; `MapEncinaSidecar` maps `/health` before the catch-all.
4. `Diagnostics/SidecarDiagnostics.cs`: `ActivitySource` and `Meter` `Encina.Sidecar`, span `encina.sidecar.forward`, counter `encina.sidecar.requests` (tags `outcome`, `method`); the tenant tag only when the gate is on. `Encina.OpenTelemetry` subscribes the source and the meter (one edit).
5. `SidecarLog` gains 893 target health probe failed (Warning: code) and 894 request denied by the tenant gate (Debug: no value); these are emitted by this pull request's classes, so the pull request edits the Phase 2 log class (counted as modified).
6. Tests: unit (retry on GET but not on POST, breaker opens, tenant gate allows and denies, health branches, tags), guard; manifests and PublicAPI through `mechanical-fixer`.

</details>

<details>
<summary>Prompt for AI Agents — Phase 3</summary>

```text
CONTEXT:
- Issue #391, Phase 3 (pull request 3). Phase 2 is merged. D4 and D8 are Settled in the plan.

TASK:
Write the resilience and tenant middleware, the health check, the diagnostics and their tests.

KEY RULES:
- Retry only idempotent methods and only a replayable body; never retry a cancellation.
- With the tenant gate on, a missing or invalid header denies; never log the value; tenant tag only when the gate is on.
- Health check: DefaultName const, Tags static array.

REFERENCE FILES:
- src/Encina.Extensions.Resilience, src/Encina.Polly
- src/Encina.OpenTelemetry/ServiceCollectionExtensions.cs
- src/Encina.NATS/Health/NATSHealthCheck.cs
```

</details>

---

### Phase 4: The ambassador (pull request 4)

> **Goal**: `IAmbassadorProxy` forwards to a named, allowlisted service through #1233's outbound client.

<details>
<summary>Tasks</summary>

1. `IAmbassadorProxy.cs` and `AmbassadorProxy.cs`: `ForwardAsync(HttpContext context, string targetService, CancellationToken)`: looks the service up in `AmbassadorOptions.Routes` (deny when absent), forwards only the allowlisted header names, uses the named `HttpClient` that `Encina.Http` configures per client (client certificates, auth providers, rate limits; Design Choice 6), and resolves a route without a fixed `BaseUrl` through `IServiceDiscoveryProvider` of #607 when registered.
2. Registration: `AddEncinaAmbassador(this IServiceCollection, Action<AmbassadorOptions>)` (the parameter is not nullable; an empty route list is a start error).
3. Tests: unit with fake routes and a fake outbound handler, guard, DI; contract: the `ISidecarProxy` and `IAmbassadorProxy` suites check the same error mapping; PublicAPI and manifests through `mechanical-fixer`.

</details>

<details>
<summary>Prompt for AI Agents — Phase 4</summary>

```text
CONTEXT:
- Issue #391, Phase 4 (pull request 4). #1233 and #607 are merged before this pull request. D6 is recommended, pending the maintainer.

TASK:
Write the ambassador, its registration and tests.

KEY RULES:
- Unknown service: deny with route_not_allowed; the request never picks a URL; forward only allowlisted header names.
- Outbound credentials and certificates come from Encina.Http, never from this package's options.
- Cancellation is never a Left.

REFERENCE FILES:
- docs/plans/http-implementation-plan-1233.md, docs/plans/service-discovery-implementation-plan-607.md
- src/Encina.Sidecar (Phases 1-3)
```

</details>

---

### Phase 5: The host project and the image (pull request 5)

> **Goal**: A runnable sidecar process and its container settings.

<details>
<summary>Tasks</summary>

1. `src/Encina.Sidecar.Host/Encina.Sidecar.Host.csproj` (`OutputType Exe`, `IsPackable false`, `Microsoft.NET.Sdk.Web`, container publish properties: repository name, tag from the project version, no `latest`), `Program.cs` (minimal hosting: `AddEncinaSidecar` bound from the `Encina:Sidecar` configuration section and environment variables, `MapEncinaSidecar`), the `Encina.slnx` entry.
2. Environment names documented for the container: `Encina__Sidecar__TargetPort`, `Encina__Sidecar__TargetHost`, and so on, bound by `IConfiguration` (no custom parser).
3. Tests: a unit-level test that builds the host with in-memory configuration and forwards one request; the documentation pull request validates the Kubernetes and Compose examples.
4. The host project is production code with a `Program.cs`; its coverage target is stated in Phase 9.

</details>

<details>
<summary>Prompt for AI Agents — Phase 5</summary>

```text
CONTEXT:
- Issue #391, Phase 5 (pull request 5). Pull requests 1 to 3 are merged. D7 is recommended, pending the maintainer.

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

1. Confirm ids 890-894 are packed inside 890-899 (895-899 spare), that the architecture tests pass and that the source and meter are subscribed in `Encina.OpenTelemetry`; no tag carries a URL, header value, route target or tenant value (tenant only when the gate is on, ADR-054 D4).

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
   | `SidecarLimitsMiddleware.cs` (2) | 95 | 85 | 0 | 0 | 0 | Each limit and the pass-through |
   | `ServiceCollectionExtensions.cs`, `SidecarEndpointRouteBuilderExtensions.cs` (2) | 95 | 90 | 0 | 0 | 0 | Registration, mapping and the DI test |
   | `SidecarResilienceMiddleware.cs` (3) | 90 | 85 | 0 | 0 | 0 | Retry, no retry, breaker and timeout with a fake time source |
   | `SidecarTenantMiddleware.cs` (3) | 95 | 85 | 0 | 0 | 0 | Allow, deny, invalid value |
   | `SidecarHealthCheck.cs` (3) | 95 | 85 | 0 | 0 | 0 | Healthy, unreachable, exception |
   | `SidecarDiagnostics.cs`, `SidecarLog.cs` (2, 3) | 90 | 0 | 0 | 0 | 0 | Listener-based tests of tags; generated methods called by the tests |
   | `AmbassadorProxy.cs` (4) | 92 | 85 | 85 | 0 | 0 | Fake routes and outbound handler; the shared suite |
   | `Program.cs` of the host (5) | 70 | 0 | 0 | 0 | 0 | Top-level statements run by the host-level test; no guarded parameter |

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

### Phase 9: Documentation & Finalization (pull request 6)

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
- Issue #391, Phase 9 (pull request 6). Code merged.

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
| 1 | `Directory.Packages.props` edit, project file, two PublicAPI files, `Encina.slnx` edit, `SidecarOptions`, `AmbassadorOptions`, validator, `SidecarErrors`, manifest, three test project references, unit and guard test files | 14 | 1,000 |
| 2 | `ISidecarProxy`, `SidecarProxy`, `SidecarLimitsMiddleware`, extension class, endpoint extension, `SidecarLog`, `EventIdRanges.cs` edit, `Encina` PublicAPI, `Encina.json`, allocation map edit, Sidecar PublicAPI edit and manifest edit, unit (2), guard and DI tests | 15 | 1,400 |
| 3 | Resilience and tenant middleware, health check, diagnostics, `SidecarLog` edit, `Encina.OpenTelemetry` edit, PublicAPI and manifest edits, unit (2), guard and DI test edits | 14 | 1,200 |
| 4 | `IAmbassadorProxy`, `AmbassadorProxy`, registration edit, PublicAPI and manifest edits, unit, guard, DI and contract tests, project references to `Encina.Http` and `Encina.ServiceDiscovery` | 12 | 1,000 |
| 5 | Host csproj, `Program.cs`, `Encina.slnx` edit, host-level test, manifest, test project reference, `PublicAPI` is not needed (no public API) | 9 | 450 |
| 6 | Two READMEs, guide page, inventory line, changelog fragment, two justification files, one more doc file for the examples | 7 | 420 |

PR 1 needs nothing; PR 2 needs PR 1; PR 3 needs PR 2; PR 4 needs PR 2 and #1233 and #607 (their packages, merged); PR 5 needs PR 3; PR 6 is last. No interface of another package is changed, so no implementer or test double is touched. Project references added in PR 4 count as edits of three csproj files in its row.

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
| `Encina.Sidecar` (`Sidecar`, new) | 890-899, ids 890-894 used | From the block 850-899 (aspire-2); registered in `EventIdRanges.cs` and the allocation test map before use |

### Estimated File Count

| Category | Files | Notes |
|----------|------:|-------|
| Production code | 16 | Library (14), host (2) |
| Edits of shared files | 5 | `Directory.Packages.props`, `Encina.slnx`, `EventIdRanges.cs`, `Encina.OpenTelemetry`, project references |
| Public API and manifests | 6 | `Encina.Sidecar` PublicAPI and manifest, `Encina` PublicAPI, `Encina.json`, host manifest |
| Tests and project files | 16 | Unit, guard, DI, contract, host test, allocation map, csproj references |
| Documentation | 7 | Two READMEs, guide, inventory, fragment, two justification files |
| **Total** | **by the split table: 14 + 15 + 14 + 12 + 9 + 7 = 71 file touches over 6 pull requests** | The split table is the authority |

---

## Combined AI Agent Prompts

<details>
<summary><strong>Full combined prompt for all phases</strong></summary>

```text
PROJECT CONTEXT:
- Encina is a pre-1.0 .NET 10 / C# 14 library; Either<EncinaError, T>; no [Obsolete], no compatibility layers.
- Issue #391: sidecar and ambassador. Plan: docs/plans/sidecar-ambassador-implementation-plan-391.md; D1, D2, D5, D6 and D7 are recommended, pending the maintainer.

IMPLEMENTATION OVERVIEW (6 pull requests, each builds alone):
1. Encina.Sidecar skeleton, options, validator, errors (Phase 1).
2. The YARP forwarder, limits, registration, first logs and range (Phase 2).
3. Resilience, tenant gate, health, diagnostics (Phase 3).
4. The ambassador on Encina.Http and discovery (Phase 4).
5. The host project and container settings (Phase 5). 6. Documentation (Phase 9).

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
| 3 | Structured Logging | ✅ | Phases 2 and 3: EventIds 890-894 |
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

- Pull requests 1 to 3 and 5 need nothing outside the repository; pull request 4 needs the packages of #1233 and #607 merged.
- `Yarp.ReverseProxy` 2.3.0 is added to `Directory.Packages.props` (the #383 plan does not use YARP).
- Issue files of this run: the image publish pipeline (registry and tags), Aspire co-location through #421's processor model, and the deferred caching and audit items.

---

## Next Steps

1. The maintainer answers D1, D2, D5, D6 and D7 when work on #391 starts; the answers go into `## Maintainer Decisions` and the markers are removed.
2. Pull requests 1 to 3 in order; 4 after #1233 and #607; 5; 6 last.
