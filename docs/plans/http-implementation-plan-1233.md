# Implementation Plan: `Encina.Http` — Outbound HTTP Client with Auth Providers, Client Certificates, Rate Limiting and Coded Errors

> **Issue**: [#1233](https://github.com/dlrivada/Encina/issues/1233) (SPEC-002 tracking id **P-31**, priority P0)
> **Type**: Feature
> **Complexity**: High (new package, 8 pull requests)
> **Estimated Scope**: ~1,400-1,900 lines of production code + ~2,500-3,200 lines of tests
> **Specification**: [SPEC-002](../specifications/SPEC-002-eu-regulatory-readiness.md) REQ-039 (AC-039; scenarios S9, S10), REQ-034 (coded outbound errors), REQ-061 (AC-043, tenant awareness), REQ-062 (AC-044, instrumentation)
> **Decisions**: [ADR-055](../architecture/adr/055-resilience-family.md) (resilience family: `Encina.Http` is built on Microsoft.Extensions.Http.Resilience and partitions per client by tenant), [ADR-057](../architecture/adr/057-verifactu-boundary.md) D9 (no AEAT endpoint, Verifactu preset or RRSIF type), [ADR-029](../architecture/adr/029-recoverability-error-classification.md), [ADR-054](../architecture/adr/054-observability-conventions-and-trace-propagation.md) D3 and D4 (meter names, tenant tag), [ADR-062](../architecture/adr/062-error-category-contract.md) (error category set where the error is created)
> **Parent EPIC**: [#1186](https://github.com/dlrivada/Encina/issues/1186) — EU regulatory readiness (SPEC-002)
> **Depends on**: [#1223](https://github.com/dlrivada/Encina/issues/1223) (P-21, outbound error taxonomy; closed, merged); [#1234](https://github.com/dlrivada/Encina/issues/1234) (P-32, `Encina.Security.OAuth`; open) and [#1246](https://github.com/dlrivada/Encina/issues/1246) (P-44, X.509 retrieval; open) only through interfaces this plan defines
> **Related**: [#1257](https://github.com/dlrivada/Encina/issues/1257) (per-tenant credentials, multi-tenancy of the SPEC-002 capabilities), [#2414](https://github.com/dlrivada/Encina/issues/2414) (rate limiters spike), [#469](https://github.com/dlrivada/Encina/issues/469) (cross-host pacing), [#2278](https://github.com/dlrivada/Encina/issues/2278)

---

## Summary

Applications call tax, messaging, calendar and payment APIs with API keys, OAuth tokens or client certificates, each with its own rate limits and server-directed waits. Without a shared client each application re-implements credentials, mTLS, pacing, error mapping and log redaction. `Encina.Refit` already maps failures to the coded taxonomy (`OutboundErrorCodes`, `OutboundErrorMetadataKeys`, `RetryAfterParser` in core), but nothing supplies the handler stack underneath. This plan adds the new package **`Encina.Http`**: a composable `DelegatingHandler` stack registered per named client, built on Microsoft.Extensions.Http.Resilience and usable by Refit clients.

1. **Outbound auth providers.** `IOutboundAuthProvider` applies credentials to a request. Shipped: API key and static bearer read from `ISecretReader` (`Encina.Security.Secrets`). Hooks defined here and implemented by their owners: `IAccessTokenSource` (OAuth tokens, #1234) and `IClientCertificateSource` (X.509, #1246).
2. **Client certificates and mTLS.** A certificate-bearing primary handler **per (client, tenant)**, so one tenant's certificate is never presented on another tenant's connection.
3. **Per-client, per-tenant rate limiter** over `System.Threading.RateLimiting.PartitionedRateLimiter`, plus a partition pause when a 429 or 503 carries `Retry-After` (parsed with the existing `RetryAfterParser`). The limiter and the auth handler run **per attempt**, inside the resilience pipeline, so a retried 429 or 5xx is paced and re-authenticated (Design Choice 4).
4. **Coded errors.** Handler-level rejections (rate limit, missing tenant, auth failure, certificate failure) use the existing `OutboundErrorCodes` plus two new codes, each created with an `ErrorCategory` (ADR-062), reach `Either` consumers through a typed `OutboundRejectedException` that `RestApiRequestHandler` maps, and never carry bodies or credentials.
5. **Redacting logging and OpenTelemetry.** Logs and spans carry method, host, status class and client name only (the framework's own `IHttpClientFactory` loggers are removed); metrics tags are bounded, and the tenant id is a span attribute and a metric tag when multi-tenancy is on (ADR-054 D4).
6. **Boundary.** `Encina.Http` has no AEAT endpoint, no Verifactu preset and no RRSIF type, and its README says so (ADR-057 D9).

**Standards**: SPEC-002 REQ-039, REQ-034, REQ-061, REQ-062; RFC 9110 section 10.2.3 (`Retry-After`); mutual TLS (RFC 8446 section 4.4.2, client certificate authentication); ADR-055; ADR-057.

**Estimated scope**: about 22 new production files in one new package, 3 modified files in `Encina` and `Encina.Refit`, about 28 test files, 5 documentation files; 8 pull requests (see Implementation Phases).

**Affected packages**: new `Encina.Http`; `Encina` (two codes and one exception type next to `OutboundErrorCodes`); `Encina.Refit` (maps the new exception); `Encina.Security.Secrets` (read only); `Encina.OpenTelemetry` (registers the new source and meter names, if the package lists sources).

**Provider category**: None (provider-independent). The distributed-lock and database matrices do not apply; rate limiting is per process (see the matrix note on #469).

**What the code does today** (`origin/main` at `c49b987e9`):

- No `Encina.Http` package exists under `src/`; the family registry assigns `6300-6349` to `Encina.Http` (`src/Encina/Diagnostics/EventIdRanges.cs` has no entry yet; `Refit` is `4650-4699`).
- Core `Encina.Recoverability` already has `OutboundErrorCodes` (`encina.outbound.client_error`, `server_error`, `rate_limited`, `timeout`, `network_error`, `unexpected_response`, `unexpected`), `OutboundErrorMetadataKeys` (`http.status`, `http.retry_after`, `transient`, `outbound.ambiguous`), `RetryAfterParser.Parse(string?/RetryConditionHeaderValue?, TimeProvider)` and `IErrorClassifier` (closed #1223, P-21).
- `Encina.Refit` (`RestApiRequestHandler<TRequest, TApiClient, TResponse>`, `AddEncinaRefitClient<TApiClient>` returning `IHttpClientBuilder`, `Diagnostics/OutboundTelemetry.cs` on the `Encina` source and meter) maps `ApiException` and network failures to those codes without the response body.
- `ISecretReader` (`src/Encina.Security.Secrets/Abstractions/ISecretReader.cs`) returns `ValueTask<Either<EncinaError, string>>`. `Microsoft.Extensions.Http` 10.0.12 and `Microsoft.Extensions.Http.Resilience` 10.10.0 are in `Directory.Packages.props`; `Encina.Extensions.Resilience` references the latter today.
- `Encina.Resilience`, the shared partitioned rate-limiting core of #2414 and the shared `Retry-After` delay function named by ADR-055 **do not exist yet**; the spike #2414 is open.
- `AGENTS.md` section 5 names `Extensions.Http.Resilience` among the resilience packages, which does not exist (the P-30 note in the issue); this plan uses the real package names.
- Neither #1234 nor #1246 is built, and neither is blocked by an open maintainer decision; this plan defines the two interfaces they implement.

---

## Design Choices

<details>
<summary><strong>1. Rate limiter: build a thin partitioned limiter in <code>Encina.Http</code> now, or wait for the Encina.Resilience core</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) A thin internal limiter in `Encina.Http` now:** one `PartitionedRateLimiter<HttpRequestMessage>` keyed by (client, tenant) plus a per-partition pause gate for `Retry-After`, behind an internal interface so the Encina.Resilience core can replace it | Ships P-31 (P0) without waiting for an unplanned package; the requirement (REQ-039) is met; the surface is about 150 lines | Two rate limiters exist until #2414's implementation lands; a later swap costs one class |
| **B) Wait for the `Encina.Resilience` rate-limiting core** (ADR-055 R1) and build the HTTP partitioning on it | One limiter, per ADR-055 | `Encina.Resilience` does not exist and has no plan; P-31 is blocked on a package that replaces two others |

### Chosen Option: **A — thin limiter now, replaceable** (recommended, pending the maintainer)

### Rationale

- B wins if the maintainer schedules the Encina.Resilience core before this package; A wins when P-31 must land first, which the P0 priority and the dependency state (core not planned) suggest.
- ADR-055 line 30 already fixes the behavior this limiter implements (per client, by tenant, `Retry-After` pauses the partition, missing tenant denied), so the later swap changes the engine, not the contract.

**Class:** Dilemma

</details>

<details>
<summary><strong>2. Per-tenant credential naming — define the convention here, or wait for #1257</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Define one internal convention now:** the secret name is `{client}/{tenant}/{purpose}` where **each segment is percent-encoded injectively** (`%` becomes `%25`, `/` becomes `%2F`), so a tenant id such as `b/c` can never shift a segment; the no-tenant segment is the reserved token `default` (tenancy off, or the explicit `AllowMissingTenant` opt-out), and **when tenancy is on a tenant id equal to `default` is refused** with the coded `tenant_required` rejection (the reserved-`default` rule of the claim-check storage plan: `default` is never a tenant id, so no real tenant shares the no-tenant partition); composed by one class `OutboundSecretNames`, resolved through `ISecretReader`; a missing secret for a tenant is a coded `Left`, never a fallback to another tenant. The limiter partition key and the handler-cache key are structured keys (client, tenant-or-none, purpose) and are never built by concatenating strings | AC-043 is testable in this package; a tenant cannot address another tenant's or client's secret, and a tenant called `default` cannot reach the tenant-less secret or partition; the convention is in one place and #1257 can adjust it | If #1257 chooses another layout (a per-tenant secret store, a vault path prefix) the class changes; a deployment with a real tenant named `default` must rename it |
| **B) Defer all per-tenant credentials to #1257** | One decision owner | `Encina.Http` would ship single-tenant credentials only, contradicting AC-043 in this issue |

### Chosen Option: **A — one convention class now** (recommended, pending the maintainer)

### Rationale

- B wins if #1257 is going to land before this package; A wins when this package ships first. The acceptance criterion AC-043 of this very issue requires per-tenant resolution.
- Tests: a property test proves `Compose` is injective over (client, tenant, purpose) triples including `/`, `%` and `default`; a two-tenant test with a tenant named `default` proves it is refused when tenancy is on and that the tenant-less opt-out never resolves a tenant's secret.

**Class:** Dilemma

</details>

<details>
<summary><strong>3. Surface: handlers and builder extensions only, or also an <code>Either</code>-returning client</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Handlers and `IHttpClientBuilder` extensions; rejections are a typed `OutboundRejectedException` that `RestApiRequestHandler` maps to the coded `Left`** | Fits the .NET `HttpClient` model and the issue ("the handler stack Refit clients can use"); smallest API | A caller using a raw `HttpClient` sees the exception, not an `Either` |
| **B) Also ship `IEncinaHttpClient.SendAsync(name, request)` returning `Either<EncinaError, HttpResponseMessage>`** | ROP all the way for raw-client users | A second client API to design, document and keep in step; not asked for |

### Chosen Option: **A — handlers only** (recommended, pending the maintainer)

### Rationale

- A wins while every consumer goes through Refit or Encina requests; B wins if raw `HttpClient` consumers appear. B can be added later on top of the same handlers.
- The exception exists only inside the HTTP pipeline contract; ADR-001 applies at the Encina boundary, where it is converted.

**Class:** Dilemma

</details>

<details>
<summary><strong>4. Compose with Microsoft.Extensions.Http.Resilience; do not replace it</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Compose:** Encina handlers (tenant guard, rate limit, auth, redaction) are added to the named client's pipeline **after** the resilience handler, so they run on every attempt; retry, circuit breaker and timeout stay in `AddStandardResilienceHandler` or `Encina.Resilience` later | No second retry layer (ADR-055 conflict check); a retried 429 or 5xx acquires a permit and re-runs auth, and the `Retry-After` gate is fed by each attempt's response | The order of handlers matters and must be documented and tested |
| **B) Own retry and circuit breaker inside `Encina.Http`** | One package | Stacks two retries on one request, the conflict ADR-055 forbids |

### Chosen Option: **A — compose, with the Encina handlers per attempt**

### Rationale

- The order, from the outside in, is: resilience handler (retry, breaker, timeout), then tenant guard, rate limiter, auth, redacting logging closest to the transport. With the limiter outside the retry, every retry would be sent without a permit, an expired bearer would be reused across attempts and the `Retry-After` gate would see only the final response, which defeats REQ-039; so the per-attempt placement is required, not optional.
- Who adds the resilience handler is Dilemma 9. A tenant-guard rejection is an `OutboundRejectedException`, which the standard retry predicates do not handle, so it is not retried (a unit test proves it, and a second one proves a retried 429 acquires a new permit and waits or is rejected on the second attempt).

**Class:** Settled
**Settled by:** ADR-055 (Encina.Http is built on Microsoft.Extensions.Http.Resilience; one retry layer), SPEC-002 REQ-039 (a per-client limiter honouring `Retry-After`, which only holds per attempt), the 2026-10-17 comment on issue #1233 ("composed, not replaced")

</details>

<details>
<summary><strong>5. mTLS: one primary handler per (client, tenant)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) A cache of primary handlers (`SocketsHttpHandler` with `SslOptions.ClientCertificates`) keyed by (client, tenant), each with its own connection pool, lifetime-bounded and evicted before the certificate expires** | A tenant's certificate is presented only on its own connections; satisfies AC-043 | More handler instances; the cache needs a lifetime and eviction |
| **B) One handler with a certificate-selection callback** | One connection pool | The callback runs at connection time while the pool is keyed by endpoint, so a connection opened with tenant A's certificate may serve tenant B's request |
| **C) One named client per tenant** (the client name includes the tenant, registered at runtime or by a tenant list) | Each tenant has its own factory handler chain and pool with no custom cache | Clients must be registered ahead of use or on demand, the named-client set grows with the tenant count, and options are repeated per tenant |

### Chosen Option: **A — handler per (client, tenant)** (recommended, pending the maintainer)

### Rationale

- A and C both satisfy AC-043; A wins because the tenant set is dynamic and the named-client registry is not, C wins when the tenant list is small and static (no custom handler cache to write).
- The factory builds one handler chain per named client, so the primary handler of A is a **dispatching `HttpMessageHandler`** that picks the tenant's `SocketsHttpHandler` per request from a singleton `TenantHandlerCache` owned by the DI container. The factory rotates and disposes the chain every `HandlerLifetime` (default 2 minutes); the dispatching handler must not dispose the shared cache or the per-tenant handlers still serving in-flight requests, and eviction disposes a tenant handler only after its in-flight requests finish. The cache is bounded (`MaxCachedTenants`, Dilemma 10) and evicts least-recently-used, so a stream of hostile tenant ids cannot grow handlers without limit.
- Phase 3 proves it with a test: two tenants with different certificates against one Kestrel server (`RequireCertificate`) must each be seen with their own certificate, including when requests alternate over a pooled connection.
- Without a tenant, the reserved no-tenant segment applies (Design Choice 2); a certificate that cannot be resolved is a coded `Left` (`encina.outbound.auth_failed`), never an unauthenticated request.

**Class:** Dilemma

</details>

<details>
<summary><strong>6. OAuth and certificate sources are interfaces defined here, implemented by their owners</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Define `IAccessTokenSource` and `IClientCertificateSource` in `Encina.Http`; `Encina.Security.OAuth` (#1234) and `Encina.Security.Secrets` (#1246) implement them** | `Encina.Http` ships and is tested with fakes; no dependency on unbuilt packages; dependency direction is satellite to base | Two interfaces to keep stable; the owners must honour them |
| **B) Wait for #1234 and #1246** | Interfaces designed by their owners | Blocks a P0 on two very-high and medium issues |

### Chosen Option: **A — interfaces here** (recommended, pending the maintainer)

### Rationale

- The issue text names the sources ("OAuth from P-32, client certificate from P-44") and treats them as hooks; the implementing issues add a one-method adapter each. B (wait for #1234 and #1246) is also valid; A wins because it unblocks a P0, B wins if the owners should design the interfaces. `IAccessTokenSource`, `IClientCertificateSource`, `IOutboundAuthProvider` and `OutboundCallContext` are public API, so their names and shapes are the maintainer's to confirm.

**Class:** Dilemma

</details>

<details>
<summary><strong>7. Missing tenant is denied; the opt-out is explicit and logged</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) A call without tenant context is a coded `Left` (`encina.outbound.tenant_required`, created with `ErrorCategory.InvalidRequest`, ADR-062 R3) unless the client sets `AllowMissingTenant` (explicit), in which case the reserved no-tenant segment applies and the opt-out is logged once per client. "Tenancy is on" is the `MultiTenancyMarker` registration, the trigger `DeadLetterManager` uses (`_tenancy is not null`, `DeadLetterManager.cs:155`), and the tenant is read from `IRequestContextAccessor` exactly as `DeadLetterManager.AmbientTenantId` does (line 100), so the sibling stores and this package agree; with tenancy off no tenant is required and no tenant tag is emitted** | Fail closed; no shared anonymous partition; one trigger and one source across the plans | A background job without a tenant must opt out explicitly |
| **B) Route to a shared anonymous partition** | Never blocks | Two tenants' calls share one pacing and credential partition |

### Chosen Option: **A — deny unless explicit opt-out**

### Rationale

- A shared anonymous partition would let two tenants' calls share one pacing and credential partition, which AC-043 forbids; the opt-out is the only way a tenant-less job proceeds, and it is visible in the log.

**Class:** Settled
**Settled by:** ADR-055 (a call without tenant context is denied unless it opts out explicitly and the opt-out is logged; never a shared anonymous partition; #2414 D3), `AGENTS.md` section 3 (gates fail closed; the only opt-out is explicit and logged), SPEC-002 REQ-061

</details>

<details>
<summary><strong>8. Redaction and telemetry: nothing from the request or response body, bounded tags, the tenant per ADR-054 D4</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Logs, spans and metrics carry client name, HTTP method, `server.address`, status class and the error code; never URL path or query, headers, bodies, credentials or exception messages; the tenant id is a span attribute and a metric tag (`encina.tenant_id`) on `encina.http.requests` and `encina.http.throttled` when multi-tenancy is on, and no tenant tag when it is off (ADR-054 D4)** | Safe by construction; follows ADR-054 D4 and AC-044; a test asserts it | Less detail when debugging; operators use the correlation id; tenant cardinality (documented in the observability guide, #906) |
| **B) Log the full URL and status** | Easier debugging | URLs can carry identifiers and tokens in the query string |

### Chosen Option: **A — redacting by construction**

### Rationale

- The handler order puts redacting logging closest to the transport so Encina's own lower handlers cannot log first. That does not stop the framework: `AddHttpClient` adds `IHttpClientFactory` logging handlers that log the request URI. The builder (Phase 1) therefore calls `RemoveAllLoggers()` on the `IHttpClientBuilder` and the redacting handler is the only logger; a test with an in-memory exporter and a log capture asserts that no credential, body, path or query appears, including in the factory's categories.
- The meter is named `Encina.Http` and instruments carry no `_total` suffix (ADR-054 D3 and point 4).

**Class:** Settled
**Settled by:** `AGENTS.md` section 3 (`EncinaError.Message` never reaches logs or tags; options with secrets hide them), ADR-054 D3 and D4 (package-named meter; tenant as span attribute and metric tag when tenancy is on), SPEC-002 REQ-062 and AC-044 (no payloads or direct identifiers in activity attributes, metric tags or logs), issue #1233 comment (logs and errors never carry bodies or credentials)

</details>

<details>
<summary><strong>9. Who adds the resilience handler: the Encina extension, or the application with order validation</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `AddEncinaHttpClient` / `AddEncinaHttpHandlers` add `AddStandardResilienceHandler` themselves (configurable through an `Action<HttpStandardResilienceOptions>`), then the Encina handlers** | The order of Design Choice 4 is guaranteed by construction; one retry layer; nothing to forget | An application with its own resilience handler must express it through the delegate (or not use the extension) |
| **B) The application adds the resilience handler; `AddEncinaHttpHandlers` must be called after it and validates the order when the chain is built (fails with a coded `InvalidOperationException` naming the client)** | Full control for applications and for `Encina.Resilience` later | Order depends on call order (`AddHttpMessageHandler` order is call order); the validation must recognise a resilience handler it did not add |

### Chosen Option: **A — the extension owns the order** (recommended, pending the maintainer)

### Rationale

- A removes a class of misconfiguration; B wins if applications routinely bring their own resilience handler or when `Encina.Resilience` replaces the standard one (the extension then changes in one place).

**Class:** Dilemma

</details>

<details>
<summary><strong>10. Defaults and placement that are visible to users</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `MaxQueueWait` 0 (reject at once with `rate_limited`), `HonourRetryAfter` true, `MaxRetryAfter` 5 minutes, `MaxCachedTenants` 256 with least-recently-used eviction, and `OutboundRejectedException` plus the two new codes in core `Encina` next to `OutboundErrorCodes`** | Predictable latency (no hidden queueing); the server's wait is honoured but a hostile or mistaken server cannot pause a partition for hours; bounded memory; `Encina.Refit` maps the exception without referencing `Encina.Http` | Bursts above the limit fail instead of waiting; new public API in core `Encina` |
| **B) A short queue (for example 2 s), no `Retry-After` cap or a smaller one, a larger cache bound, and the exception and codes in `Encina.Http` with `Encina.Refit` referencing it** | Smooths bursts; fewer rejections | Holds requests; an unbounded pause; more memory; `Encina.Refit` gains a dependency on the new package |

### Chosen Option: **A** (recommended, pending the maintainer)

### Rationale

- None of these values is fixed by a rule; each changes behaviour for users (queueing, pause length, memory) or the public API of another package (core `Encina`). `AllowMissingTenant = false` is not in this list: it is settled by the fail-closed rule (Design Choice 7).

**Class:** Dilemma

</details>

---

## Implementation Phases

Eight pull requests, each at most 15 files and 1500 changed lines counting tests, `PublicAPI.*.txt`, `.github/coverage-manifest/*.json` and fragments (`AGENTS.md` section 11), each building green on its own and each carrying the contract, property and integration tests of the `src/` files it adds (AGENTS.md section 9: a file's per-flag targets are met by the PR that adds it):

| PR | Content | Own tests |
|----|---------|-----------|
| 1 | Phase 1: scaffold, options, validator, builder (adds the resilience handler, removes the framework loggers), README, solution entry | unit, guard, DI test |
| 2 | Core: `OutboundErrorCodes.AuthFailed` / `TenantRequired`, `OutboundRejectedException`, the `RestApiRequestHandler` mapping with its `ErrorCategory` per code, classifier tests (Phase 2 task 8, Phase 5 tasks 2-3), and the `Http` EventId range registration with its `AssemblyRanges` mapping | unit, guard, contract of the mapping |
| 3 | Phase 2 tasks 1-6: auth providers, `OutboundCallContext`, `OutboundSecretNames` | unit, guard, contract (every provider), property (name injectivity) |
| 4 | Phase 2 task 7 and Phase 4 task 4: `AuthHandler`, `TenantGuardHandler`, builder wiring | unit, guard, integration (two-tenant secrets, tenant named `default`) |
| 5 | Phase 3: `IClientCertificateSource`, `TenantHandlerCache`, dispatching primary handler | unit, guard, integration (Kestrel `RequireCertificate`) |
| 6 | Phase 4 tasks 1-3 and 5: rate limiter, `RetryAfterGate`, `RateLimitHandler` | unit, guard, property (rate bound), integration (WireMock S9/S10, retried 429) |
| 7 | Phase 5 tasks 1 and 4 and Phase 6: redacting handler, diagnostics, the remaining log message, health check | unit, guard, property (redaction), integration (exporter and log capture) |
| 8 | Phase 7 justification files and Phase 8: guide, ADR, index, fragment, INVENTORY | docs only |

The counts per PR are in Estimated File Count.

### Phase 1: Package Scaffold, Options and Registration

> **Goal**: An empty but registered package with the validated options and the builder entry point.

<details>
<summary><strong>Tasks</strong></summary>

#### New project `src/Encina.Http/`

1. **`Encina.Http.csproj`** — `net10.0`; references `Encina`, `Encina.Security.Secrets`; packages `Microsoft.Extensions.Http`, `Microsoft.Extensions.Http.Resilience`, `Microsoft.Extensions.Options`, `Microsoft.CodeAnalysis.PublicApiAnalyzers`; `InternalsVisibleTo` the test projects; `PublicAPI.Shipped.txt` / `PublicAPI.Unshipped.txt`; add to `Encina.slnx`
2. **`Options/EncinaHttpClientOptions.cs`** — per client: `double PermitsPerSecond` (0 = no limit), `int Burst`, `TimeSpan MaxQueueWait` (default 0: reject at once), `bool HonourRetryAfter` (default true), `bool AllowMissingTenant` (default false), `ClientCertificateName`, `AuthPurpose`; no secret value property (names only)
3. **`Options/EncinaHttpClientOptionsValidator.cs`** — `IValidateOptions` per named client (named options): permits and burst consistent, wait non-negative
4. **`HttpClientBuilderExtensions.cs`** — `AddEncinaHttpClient(this IServiceCollection, string name, Action<EncinaHttpClientBuilder> configure)` returning `IHttpClientBuilder`, and `AddEncinaHttpHandlers(this IHttpClientBuilder, Action<HttpStandardResilienceOptions>? configureResilience = null, ...)` for an existing client (used with Refit's `AddEncinaRefitClient`); both add `AddStandardResilienceHandler` first (Dilemma 9, option A) and then the Encina handlers, so the order of Design Choice 4 holds by construction, and both call `RemoveAllLoggers()` so the redacting handler is the only logger (Design Choice 8); PR 1 ships only `AddEncinaHttpClient`, `AddEncinaHttpHandlers`, the resilience setup and `RemoveAllLoggers()`; `EncinaHttpClientBuilder` gains one method per later pull request: `UseApiKey`, `UseBearer`, `UseAccessTokenSource` (PR 4, with the auth handler), `UseClientCertificate` (PR 5), `RateLimit(permitsPerSecond)` (PR 6)
5. **Diagnostics and the EventId range** — none in this phase; the range is registered in PR 2 and each log message ships with the PR that emits it (Phase 6 task 1)
6. **`README.md`** — package README stating plainly that `Encina.Http` has no AEAT endpoint, Verifactu preset or RRSIF type (ADR-057 D9)

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 1</strong></summary>

```
You are implementing Phase 1 of issue #1233: the Encina.Http package scaffold.

CONTEXT:
- Encina.Http is a new satellite built on Microsoft.Extensions.Http.Resilience (ADR-055); it composes with it and
  does not own retry or circuit breaking. Named-client options per client name.
- Refit clients are registered with AddEncinaRefitClient<T>() which returns IHttpClientBuilder; the handlers must
  attach to that builder.

TASK:
Create the project, options, validator, builder extensions and README as listed in the Phase 1 tasks, add the
project to Encina.slnx and the manifest stub .github/coverage-manifest/Encina.Http.json (through mechanical-fixer).
Add unit and guard tests and a DI test with ValidateOnBuild and ValidateScopes for two named clients.

KEY RULES:
- No secret value in any options property; options hold names only.
- ArgumentNullException guards; XML docs on every public member; zero warnings; PublicAPI files complete.
- The README says Encina.Http has no AEAT endpoint, no Verifactu preset and no RRSIF type; no private client
  application is named ("the reference application").

REFERENCE FILES:
- src/Encina.Refit/ServiceCollectionExtensions.cs
- src/Encina.Extensions.Resilience/Encina.Extensions.Resilience.csproj
- docs/architecture/adr/055-resilience-family.md
- docs/architecture/adr/057-verifactu-boundary.md
```

</details>

### Phase 2: Auth Providers and the Two Hooks

> **Goal**: Credentials from secrets, per tenant, fail closed.

<details>
<summary><strong>Tasks</strong></summary>

#### `src/Encina.Http/Auth/`

1. **`IOutboundAuthProvider.cs`** — `ValueTask<Either<EncinaError, Unit>> ApplyAsync(HttpRequestMessage request, OutboundCallContext context, CancellationToken ct)`; `OutboundCallContext` = client name, tenant id (nullable), purpose
2. **`ApiKeyAuthProvider.cs`** — header name configurable; the key from `ISecretReader` by `OutboundSecretNames.Compose(client, tenant, purpose)` (injective, Design Choice 2); the value is set on the request and never logged. The auth handler runs per attempt (Design Choice 4), so an expired token is re-read on each retry
3. **`BearerAuthProvider.cs`** — static bearer from a secret
4. **`IAccessTokenSource.cs`** (OAuth hook, implemented by #1234) and **`AccessTokenAuthProvider.cs`** — `ValueTask<Either<EncinaError, string>> GetAccessTokenAsync(OutboundCallContext, CancellationToken)`; sets `Authorization: Bearer`
5. **`IClientCertificateSource.cs`** (certificate hook, implemented by #1246) — `ValueTask<Either<EncinaError, X509Certificate2>> GetCertificateAsync(OutboundCallContext, CancellationToken)`
6. **`OutboundSecretNames.cs`** (internal) — the one convention class of Dilemma 2: segments percent-encoded, reserved no-tenant token `default`, a tenant id equal to `default` refused when tenancy is on
7. **`AuthHandler.cs`** — `DelegatingHandler` running the provider; a `Left` becomes `OutboundRejectedException` with `encina.outbound.auth_failed`; a missing secret or tenant never falls back
8. **Core** (`src/Encina/Recoverability/`): `OutboundErrorCodes.AuthFailed = "encina.outbound.auth_failed"` and `TenantRequired = "encina.outbound.tenant_required"`; `OutboundRejectedException` (public sealed; `Code`, metadata dictionary; no message text from remote systems) — `PublicAPI.Unshipped.txt` of `Encina` and `Encina.Http`. **Error categories (ADR-062, set where the `EncinaError` is created, in the `RestApiRequestHandler` mapping of `OutboundRejectedException`)**: `tenant_required` is `InvalidRequest` (R3); `auth_failed` is `Internal` (R1 and R4: a missing secret or certificate is a server-side lookup failure, not something the caller sent); `rate_limited` raised by the local limiter keeps the category `Unavailable` that ADR-062 R7 gives the outbound `rate_limited` code (the completeness scan fails on one code created with two categories) and carries the wait as a `TimeSpan` under `EncinaErrorMetadataKeys.RetryAfter` alongside the outbound `http.retry_after`, so hosts write `Retry-After` from the former. A unit test per code asserts `GetCategory`

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 2</strong></summary>

```
You are implementing Phase 2 of issue #1233: outbound auth providers.

CONTEXT:
- ISecretReader returns ValueTask<Either<EncinaError, string>>. OAuth tokens (#1234) and X.509 certificates (#1246)
  are not built: this phase defines the interfaces they will implement and tests with fakes.
- Credentials resolve per tenant (SPEC-002 AC-043); a missing tenant or secret fails closed with a coded error.
- Handlers cannot return Either: they throw OutboundRejectedException(code, metadata), converted at the Encina
  boundary (Phase 5).

TASK:
Implement the Phase 2 tasks and unit tests: API key and bearer applied per tenant, two tenants get their own secret,
missing secret fails, missing tenant fails unless AllowMissingTenant, the token source called per request, no secret
in any log or exception.

KEY RULES:
- Never log or put into an error the secret value, the secret name composed with tenant, or an exception message.
- Time from TimeProvider where used; options hold names only.
- New public API in core goes through PublicAPI.Unshipped.txt (mechanical-fixer).

REFERENCE FILES:
- src/Encina.Security.Secrets/Abstractions/ISecretReader.cs
- src/Encina/Recoverability/OutboundErrorCodes.cs
- src/Encina/Recoverability/OutboundErrorMetadataKeys.cs
- src/Encina.Refit/Handlers/RestApiRequestHandler.cs
```

</details>

### Phase 3: Client Certificates and mTLS

> **Goal**: A tenant's certificate is only ever presented on its own connections.

<details>
<summary><strong>Tasks</strong></summary>

#### `src/Encina.Http/Tls/`

1. **`TenantHandlerCache.cs`** (internal, a singleton owned by the DI container, never by a factory-built chain) — keyed by a structured (client, tenant-or-none) key: builds a `SocketsHttpHandler` with `SslOptions.ClientCertificates` from `IClientCertificateSource`, bounded `PooledConnectionLifetime`, evicted before the certificate's `NotAfter` (read through the injected `TimeProvider`) and disposed only after its in-flight requests finish, bounded by `MaxCachedTenants` with least-recently-used eviction (Dilemma 10), concurrency-safe creation, no certificate private material in logs
2. **`ClientCertificateHandlerFactory.cs`** — plugs a **dispatching `HttpMessageHandler`** in as the primary handler of the named client through `ConfigurePrimaryHttpMessageHandler`: it picks the tenant's `SocketsHttpHandler` from the cache per request and never disposes the cache or the tenant handlers when the factory rotates the chain every `HandlerLifetime` (its `Dispose` is a no-op for them); a test rotates the chain while a tenant request is in flight
3. A certificate that cannot be resolved, is expired or not yet valid is `encina.outbound.auth_failed`; the request is not sent
4. Server-certificate validation stays the platform default; a callback that accepts everything is never installed

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 3</strong></summary>

```
You are implementing Phase 3 of issue #1233: mTLS with a handler per (client, tenant).

CONTEXT:
- Design choice 5: a SocketsHttpHandler pool is keyed by endpoint, so one handler with a certificate callback could
  reuse a connection opened with another tenant's certificate; use one primary handler per (client, tenant).
- Certificates come from IClientCertificateSource (hook for #1246); tests use a fake source and generated
  self-signed certificates.

TASK:
Implement the cache and factory as listed. Tests: unit (cache creation, eviction before NotAfter on a
FakeTimeProvider, concurrent creation yields one handler, unresolved certificate fails closed); integration with a
Kestrel test server that requires a client certificate: two tenants with different certificates alternate requests
and the server sees each tenant's own certificate; an expired certificate is not sent.

KEY RULES:
- Never accept all server certificates; never log certificate content, thumbprint secrets or exception messages.
- Dispose evicted handlers without breaking in-flight requests.

REFERENCE FILES:
- docs/plans/http-implementation-plan-1233.md (design choice 5)
- src/Encina.Security.Secrets/Abstractions/ISecretReader.cs
- tests/Encina.IntegrationTests/Web/Refit/
```

</details>

### Phase 4: Rate Limiter and the Retry-After Gate

> **Goal**: Per-client, per-tenant pacing that honours the server's wait.

<details>
<summary><strong>Tasks</strong></summary>

#### `src/Encina.Http/RateLimiting/`

1. **`OutboundRateLimiter.cs`** (internal) — a `PartitionedRateLimiter<OutboundCallContext>` with a token-bucket limiter per (client, tenant) partition (a structured key, never a concatenated string) from `EncinaHttpClientOptions` (permits per second, burst, queue wait); a limiter is created lazily and disposed with the cache
2. **`RetryAfterGate.cs`** (internal) — per partition, `PauseUntilUtc`; after a response with status 429 or 503 and a valid `Retry-After` (`RetryAfterParser.Parse(header, TimeProvider)`), the partition is paused and later calls wait or are rejected per `MaxQueueWait`; a pause is capped by `MaxRetryAfter` (default in Dilemma 10) to bound a hostile server
3. **`RateLimitHandler.cs`** — `DelegatingHandler` placed after the resilience handler, so it runs on **every attempt**: acquire a lease (a retried 429 waits or is rejected on its second attempt); rejection throws `OutboundRejectedException(OutboundErrorCodes.RateLimited, retry-after metadata)`; the response of the inner handler feeds the gate on each attempt, so retries cannot hit the server before the pause ends
4. Tenant guard (`TenantGuardHandler`, PR 4): no tenant and no opt-out throws `OutboundRejectedException(TenantRequired)` before any partition is touched; the tenant comes from `IRequestContextAccessor` and "tenancy on" is the `MultiTenancyMarker` (Design Choice 7); a tenant id equal to `default` under tenancy is refused (Design Choice 2)
5. The internal interface hides the engine so the Encina.Resilience core can replace it (Dilemma 1)

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 4</strong></summary>

```
You are implementing Phase 4 of issue #1233: the outbound rate limiter.

CONTEXT:
- ADR-055: Encina.Http partitions per client by tenant; a 429 or 503 with Retry-After pauses that partition; a call
  without tenant is denied unless an explicit, logged opt-out.
- RetryAfterParser (core) parses delta-seconds and HTTP-date against a TimeProvider.

TASK:
Implement the limiter, gate and handler as listed. Unit tests on a FakeTimeProvider: permits never exceeded, burst,
queue wait, two tenants do not share a partition (including a tenant named `default` versus the tenant-less opt-out),
Retry-After pauses only the partition that received it, an absurd Retry-After is capped, no tenant denied, opt-out
logged once, and a 429 retried by the standard resilience handler acquires a new permit on its second attempt (waits or
is rejected) instead of bypassing the limiter. Property test (FsCheck): over any sequence of calls
the granted permits in any window never exceed the configured rate plus burst.

KEY RULES:
- Time only from TimeProvider; no Task.Delay without it; no Thread.Sleep in tests.
- Tags and logs carry client name and outcome only, plus `encina.tenant_id` as a span attribute and metric tag when
  tenancy is on (ADR-054 D4); the tenant id is never a log argument.

REFERENCE FILES:
- src/Encina/Recoverability/RetryAfterParser.cs
- src/Encina/Recoverability/OutboundErrorCodes.cs
- docs/architecture/adr/055-resilience-family.md
```

</details>

### Phase 5: Coded Errors, Redacting Logging and the Refit Mapping

> **Goal**: Every failure reaches Encina callers as a coded `Left`, and nothing sensitive is logged.

<details>
<summary><strong>Tasks</strong></summary>

1. **`Diagnostics/RedactingLoggingHandler.cs`** — the handler closest to the transport (the builder removed the framework's URI-logging handlers with `RemoveAllLoggers()` in Phase 1) logging method, `server.address`, status class, elapsed time and the error code; never path, query, headers or bodies
2. **`src/Encina.Refit/Handlers/RestApiRequestHandler.cs`** (modify) — catch `OutboundRejectedException` and return `Left` created with the `ErrorCategory` of its code (Phase 2 task 8) and its metadata (`http.retry_after` and `EncinaErrorMetadataKeys.RetryAfter` as a `TimeSpan`, `transient` = false for tenant and auth failures, true for rate limited); the message is fixed text. `RestApiRequestHandler.cs` is also the file the ADR-062 producer slice for `Encina.Refit` edits (`tests/Encina.UnitTests/Core/ErrorCategoryPending.txt` lists `src/Encina.Refit/` under `P-Refit`, and `ErrorCategoryCompletenessStaticScanTests` scans it). The new mapping uses the categorized `EncinaErrors.Create(code, ErrorCategory, ...)` overload, so it never adds an uncategorized site; PR 2 leaves the `P-Refit` pending line untouched (the Refit slice owns categorizing the existing `ApiException` and network sites and deleting that line). Merge order is free: whichever pull request lands second rebases on the other's edit of this file, and the PR 2 brief names the check
3. **`src/Encina/Recoverability/IErrorClassifier` mapping** — verify that `rate_limited` stays transient and `auth_failed` / `tenant_required` are not retried; add classifier tests (no change to the classifier unless a test shows a gap)
4. Tests: unit and property (no secret string, header value, body, path or query ever appears in any captured log, including the `IHttpClientFactory` categories, exception message, metric tag or activity attribute for generated inputs)

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 5</strong></summary>

```
You are implementing Phase 5 of issue #1233: coded errors and redaction.

CONTEXT:
- RestApiRequestHandler already maps ApiException and network failures to OutboundErrorCodes with http.status,
  http.retry_after and transient metadata and never attaches the response body. Handlers in Encina.Http throw
  OutboundRejectedException (Phase 2).

TASK:
Add the redacting logging handler and the Refit mapping of OutboundRejectedException; add the classifier tests. The
property test generates secrets, URLs with query strings and bodies and asserts none appears in captured output.

KEY RULES:
- EncinaError.Message never reaches logs, tags or health results: error code only; exceptions through ForLogging().
- Do not change existing Refit behavior for ApiException or network failures.

REFERENCE FILES:
- src/Encina.Refit/Handlers/RestApiRequestHandler.cs
- src/Encina/Recoverability/IErrorClassifier.cs
- src/Encina/Recoverability/OutboundErrorMetadataKeys.cs
```

</details>

### Phase 6: Cross-Cutting Integration and Observability

> **Goal**: Tracing, metrics, logging ids, tenant propagation and the optional health check.

<details>
<summary><strong>Tasks</strong></summary>

1. **Range and log messages** — the range `Http = (6300, 6349)` is registered in `src/Encina/Diagnostics/EventIdRanges.cs` and `Encina.Http` is mapped in `EncinaEventIdAllocationTests` in **PR 2** (before any message exists; 6250-6299 is the `DistributedLock` range of the lock plans). Each message ships in the pull request whose handler emits it, in `Diagnostics/HttpLogMessages.cs`, with ids packed sequentially in landing order (so the opt-out of PR 4 is logged from the pull request that introduces it): PR 4 6300 auth failed by code (Warning), 6301 tenant required, denied (Warning), 6302 tenant opt-out in use (Warning, once per client); PR 5 6303 certificate unresolved or expired by code (Warning), 6304 tenant handler evicted (Debug); PR 6 6305 rate limited (Debug), 6306 partition paused by Retry-After (Information); PR 7 6307 request completed (Debug, redacted). Any other id waits for the next free slot (6308 upward) and is registered with the pull request that needs it
2. **`Diagnostics/HttpDiagnostics.cs`** — `ActivitySource` `Encina.Http` (version `1.0`), `Meter` `Encina.Http` (ADR-054 D3), counters `encina.http.requests` (tags client, status class, outcome) and `encina.http.throttled` (tags client, reason), each with the tag `encina.tenant_id` when the `MultiTenancyMarker` is registered and no tenant tag when it is not (ADR-054 D4), and histogram `encina.http.duration`; span `encina.http.send` with `encina.tenant_id` (when tenancy is on), `http.request.method`, `server.address`; tenant id from `IRequestContextAccessor`
3. **`Health/OutboundClientHealthCheck.cs`** — optional per named client (`IEncinaHealthCheck`), registered only for a client with a configured base address (no base address, no probe): reachability of that address through a dedicated probe `HttpClient` built **without** the Encina handler stack, so the probe sends no credentials, takes no limiter permit, needs no tenant (the tenant guard is not in its pipeline) and counts nothing against any partition; short timeout; reports host only
4. Tenancy: no tenant is required and no tenant tag is emitted when the `MultiTenancyMarker` is absent (the reserved no-tenant segment of Design Choice 2 applies); with `Encina.Tenancy`, the tenant id comes from `IRequestContextAccessor`, the same trigger and source as `DeadLetterManager`
5. Validation: options validator (Phase 1); resilience: composition order documented and tested with `AddStandardResilienceHandler` (one retry layer)

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 6</strong></summary>

```
You are implementing Phase 6 of issue #1233: observability, tenancy and health of Encina.Http.

CONTEXT:
- AGENTS.md section 7: register the range first in EventIdRanges.cs, add the assembly to EncinaEventIdAllocationTests,
  pack ids sequentially. SPEC-002 REQ-062: tenant as an attribute, no payloads or direct identifiers.
- Health checks: DefaultName const, static Tags array; the check sends no credentials.

TASK:
Register the range, write the log messages, diagnostics and health check as listed and wire them into the handlers.
Add the in-memory exporter test of AC-044 (main operations emit their activity and metric with the tenant as an
attribute) and the health check test.

KEY RULES:
- Metric tags: client name, status class, outcome, reason, plus encina.tenant_id only when multi-tenancy is on
  (ADR-054 D4); never path or host names beyond the configured server.address.
- A test asserts that no tag, attribute or log carries a payload, response body or direct identifier.

REFERENCE FILES:
- src/Encina/Diagnostics/EventIdRanges.cs
- src/Encina.Refit/Diagnostics/OutboundTelemetry.cs
- src/Encina.Tenancy/Diagnostics/TenancyActivitySource.cs
- tests/Encina.UnitTests/Testing/Architecture/EncinaEventIdAllocationTests.cs
```

</details>

### Phase 7: Testing

> **Goal**: Every flag reaches its own target; mTLS and pacing run against real servers.

<details>
<summary><strong>Tasks</strong></summary>

1. **Unit** (`tests/Encina.UnitTests/Http/`) — phases 1 to 6
2. **Guard** (`tests/Encina.GuardTests/Http/`) — every public constructor and method
3. **Contract** (`tests/Encina.ContractTests/Http/`) — the `IOutboundAuthProvider` contract over every provider shipped (API key, bearer, access token): applies the credential, fails closed without tenant or secret, never logs the value
4. **Property** (`tests/Encina.PropertyTests/Http/`) — the rate limiter bound and the redaction property
5. **Integration** (`tests/Encina.IntegrationTests/Http/`) — WireMock via `Encina.Testing.WireMock` (scenarios S9 and S10: pacing, a server-directed wait honoured, a rejection) and a Kestrel server with `RequireCertificate` for mTLS; two-tenant isolation tests (tenant A never uses, reads or paces on tenant B's credentials, certificate or partition) per AC-043
6. **Load and benchmark** — justification files (the handlers are thin and per-process; a rate-limiter benchmark only if the maintainer wants a number)
7. **Coverage manifest** — new `Encina.Http.json` (created in PR 1, extended in each PR) with per-file targets and one-sentence justifications (`mechanical-fixer`). The handlers and the limiter are pure logic over `FakeTimeProvider`, so unit targets are demanding:

   | File | Unit | Guard | Contract | Property | Integration | Justification |
   |------|-----:|------:|---------:|---------:|------------:|---------------|
   | `OutboundSecretNames.cs` | 100 | 0 | 0 | 100 | 0 | One pure function; the injectivity property is its safety net; internal |
   | `ApiKeyAuthProvider.cs`, `BearerAuthProvider.cs`, `AccessTokenAuthProvider.cs` | 95 | 100 | 90 | 0 | 80 | Small classes over `ISecretReader` fakes; the provider contract runs over each; two-tenant integration |
   | `AuthHandler.cs`, `TenantGuardHandler.cs` | 95 | 100 | 0 | 0 | 80 | Branches are success, `Left`, no tenant, opt-out; the two-tenant test runs through the real chain |
   | `RateLimitHandler.cs`, `OutboundRateLimiter.cs`, `RetryAfterGate.cs` | 95 | 100 | 0 | 90 | 80 | Pure logic over `FakeTimeProvider`; property proves the rate bound; WireMock proves S9/S10 and the retried 429 |
   | `TenantHandlerCache.cs`, `ClientCertificateHandlerFactory.cs` | 90 | 100 | 0 | 0 | 85 | Creation, eviction and rotation on a fake clock; Kestrel `RequireCertificate` proves the isolation |
   | `RedactingLoggingHandler.cs`, `HttpDiagnostics.cs` | 90 | 100 | 0 | 90 | 70 | Redaction property over generated secrets, URLs and bodies; exporter test for the instruments |
   | `OutboundClientHealthCheck.cs` | 95 | 100 | 0 | 0 | 0 | Healthy, unhealthy, no base address |
   | `EncinaHttpClientOptions.cs`, validator, builder extensions | 90 | 100 | 0 | 0 | 0 | Property bag, three validation rules and registration branches |

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 7</strong></summary>

```
You are implementing Phase 7 of issue #1233: tests of Encina.Http.

CONTEXT:
- Tests execute real package code; assertions use Shouldly through Encina.Testing.Shouldly; time from
  FakeTimeProvider; WireMock through Encina.Testing.WireMock; shared fixtures, no per-test servers where a fixture
  exists.
- Coverage is per flag against .github/coverage-manifest/Encina.Http.json.

TASK:
Write the tests of the Phase 7 tasks, measure each flag with
`dotnet test tests\Encina.<Flag>Tests --collect "XPlat Code Coverage"` and report the per-file table.

KEY RULES:
- No Thread.Sleep; a test never swallows an exception thrown by the code under test.
- The two-tenant isolation tests are mandatory (AC-043); a justification .md is never allowed for them.

REFERENCE FILES:
- src/Encina.Testing.WireMock/EncinaWireMockFixture.cs
- tests/Encina.IntegrationTests/Web/Refit/
- tests/Encina.UnitTests/Testing/Architecture/EncinaEventIdAllocationTests.cs
```

</details>

### Phase 8: Documentation and Finalization

> **Goal**: Operators can configure a client and understand what it does and does not do.

<details>
<summary><strong>Tasks</strong></summary>

1. XML documentation on every public API (`<example>` on the builder extensions)
2. `docs-writer`: package README (including the ADR-057 D9 boundary statement), a configuration guide (named client, auth, mTLS, rate limit, handler order with Microsoft.Extensions.Http.Resilience), tenant behavior and the opt-out
3. ADR in `docs/architecture/adr/` (number assigned when the ADR pull request opens) for the handler per (client, tenant) and the handler order; `mechanical-fixer` adds it to the ADR index
4. `changelog.d/1233-encina-http.added.md` (`mechanical-fixer`); `PublicAPI.Unshipped.txt` complete in `Encina`, `Encina.Http`; `docs/INVENTORY.md` for the new package; `ROADMAP.md` if the milestone text changes; the `AGENTS.md` section 5 Resilience row names `Extensions.Http.Resilience`, which does not exist (the P-30 note): that wording is the maintainer's and is changed in its own change, not in this pull request
5. `dotnet build --configuration Release` with 0 warnings; `dotnet test`; every flag at its target; CRAP <= 10 on changed methods; the pull request records the ADR-018 evaluation of the 12 functions

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 8</strong></summary>

```
You are finishing issue #1233: documentation and verification.

CONTEXT:
- Documentation goes through docs-writer and the encina-docs skill; the changelog fragment, PublicAPI lines, ADR index
  and the coverage manifest go through mechanical-fixer.

TASK:
Complete the Phase 8 tasks and report the per-flag figures of the touched files against their targets.

KEY RULES:
- State that Encina.Http has no AEAT endpoint, no Verifactu preset and no RRSIF type, in the README.
- English only; no private client application named; no hand-typed coverage figures.
- Zero warnings; no [Obsolete]; no compatibility layer.

REFERENCE FILES:
- .claude/skills/encina-docs/SKILL.md
- docs/architecture/adr/057-verifactu-boundary.md
- changelog.d/README.md
```

</details>

---

## Research

### Relevant Standards and Specifications

| Source | Provision | Relevance |
|--------|-----------|-----------|
| SPEC-002 REQ-039, AC-039 | Outbound auth, certificates and mTLS, per-client limiter honouring Retry-After, coded errors, redacting logs, OpenTelemetry | The requirement this plan implements; scenarios S9, S10 |
| SPEC-002 REQ-034 | Outbound errors coded with `http.status`, `http.retry_after`, `transient`; never the body | Reuses the existing taxonomy |
| SPEC-002 REQ-061, AC-043 | Credentials, certificates and rate limits resolve per tenant; missing configuration fails closed | Handler per tenant, per-tenant secrets |
| SPEC-002 REQ-062, AC-044 | Tracing, metrics, `[LoggerMessage]` ids in a registered range, health check, no payloads in telemetry | Phase 6 |
| ADR-055 | `Encina.Http` over Microsoft.Extensions.Http.Resilience; partition per client by tenant; Retry-After pauses the partition; no tenant denied | Design choices 4, 1, 7 |
| ADR-057 D9 | No AEAT endpoint, Verifactu preset or RRSIF type | README boundary statement |
| ADR-029 | Error classification for retry | `rate_limited` transient; auth and tenant failures not retried |
| RFC 9110 section 10.2.3 | `Retry-After` as delta-seconds or HTTP-date | Parsed by `RetryAfterParser` |
| RFC 8446 section 4.4.2 | TLS 1.3 client certificate authentication | mTLS handler |

### Existing Encina Infrastructure to Leverage

| Component | Location | Usage in this feature |
|-----------|----------|----------------------|
| `OutboundErrorCodes`, `OutboundErrorMetadataKeys` | `src/Encina/Recoverability/OutboundErrorCodes.cs`, `OutboundErrorMetadataKeys.cs` | Existing codes and metadata; two codes added |
| `RetryAfterParser` | `src/Encina/Recoverability/RetryAfterParser.cs` | Parses `Retry-After` against a `TimeProvider` |
| `IErrorClassifier`, `OutboundRetryRules` | `src/Encina/Recoverability/` | Retry classification of the new codes |
| `RestApiRequestHandler` | `src/Encina.Refit/Handlers/RestApiRequestHandler.cs` | Maps `OutboundRejectedException` to a coded `Left` |
| `AddEncinaRefitClient` | `src/Encina.Refit/ServiceCollectionExtensions.cs` | Returns the `IHttpClientBuilder` the handlers attach to |
| `OutboundTelemetry` | `src/Encina.Refit/Diagnostics/OutboundTelemetry.cs` | Pattern for outbound activity and metric names |
| `ISecretReader` | `src/Encina.Security.Secrets/Abstractions/ISecretReader.cs` | Source of API keys and bearer secrets |
| `IRequestContextAccessor` | registered by `Encina.Tenancy` (`src/Encina.Tenancy/ServiceCollectionExtensions.cs`) | Ambient tenant id |
| Microsoft.Extensions.Http(.Resilience) | `Directory.Packages.props` (10.0.12 / 10.10.0) | `IHttpClientBuilder`, `AddStandardResilienceHandler` |
| `EncinaWireMockFixture` | `src/Encina.Testing.WireMock/EncinaWireMockFixture.cs` | Integration tests for pacing and errors |
| `EncinaEventIdAllocationTests` | `tests/Encina.UnitTests/Testing/Architecture/EncinaEventIdAllocationTests.cs` | Assembly-to-range map |

### Event ID Allocation

| Package | Range | Notes |
|---------|-------|-------|
| `Encina.Http` (new `Http` entry) | 6300-6349 | Registered first in `EventIdRanges.cs` (ADR-021) by PR 2, before any message; 6300-6307 allocated by this plan, 6308-6349 left to the family; 6250-6299 belongs to the `DistributedLock` range of the lock plans (#741, #717) |
| `Encina.Refit` | 4650-4699 | Existing; unchanged |

### Estimated File Count

| Category | Files | Notes |
|----------|------:|-------|
| Production (new, `Encina.Http`) | 24 | Options (2), builder (2), auth (8), TLS (3), rate limiting (3), tenant guard (1), diagnostics and logging (3), health (1), csproj (1) |
| Production (modified) | 4 | `OutboundErrorCodes.cs`, new `OutboundRejectedException.cs` (new), `RestApiRequestHandler.cs`, `EventIdRanges.cs` |
| PublicAPI, manifests, solution | 7 | `PublicAPI.Unshipped.txt` in `Encina` and `Encina.Http`, `PublicAPI.Shipped.txt`, `Encina.Http.json`, `Encina.json`, `Encina.Refit.json`, `Encina.slnx` |
| Unit and guard tests | 14 | |
| Contract, property tests | 5 | |
| Integration tests | 5 | WireMock, Kestrel mTLS, two-tenant isolation |
| Justification files | 2 | Load, benchmark |
| Documentation and changelog | 5 | README, guide, ADR, fragment, INVENTORY entry |
| **Total** | **~66** | Across 8 pull requests |

| PR | Files (production + PublicAPI + manifests + tests) | Lines (estimate) |
|----|-----|-----:|
| 1: scaffold | 13 (csproj, slnx, options, validator, 2 builder files, README, 2 PublicAPI files, `Encina.Http.json`, unit, DI/unit, guard) | ~900 |
| 2: core codes, Refit mapping, EventId range | 13 (2 core files modified, `OutboundRejectedException`, `RestApiRequestHandler`, `EventIdRanges.cs`, EventId map test, `PublicAPI.Unshipped.txt` in `Encina`, 2 manifests, unit (3 test files), guard) | ~800 |
| 3: auth providers | 13 (7 production, `PublicAPI.Unshipped.txt`, manifest, unit, guard, contract, property) | ~1100 |
| 4: auth and tenant handlers | 10 (`AuthHandler`, `TenantGuardHandler`, builder mod, manifest, `PublicAPI.Unshipped.txt`, 2 unit, guard, integration, the tenant resolver, `HttpLogMessages.cs` with 6300-6302) | ~950 |
| 5: mTLS | 11 (3 production, builder mod, `PublicAPI.Unshipped.txt`, manifest, unit, guard, integration with the Kestrel fixture, log messages edit with 6303-6304) | ~1050 |
| 6: rate limiting | 11 (3 production, builder mod, manifest, unit, property, guard, WireMock integration, log messages edit with 6305-6306) | ~1050 |
| 7: observability | 12 (3 production, log messages edit with 6307, 2 `PublicAPI.Unshipped.txt`, manifest, 2 unit, guard, property, integration) | ~1200 |
| 8: documentation | 7 (guide, ADR, index, fragment, INVENTORY, 2 justification files) | ~500 |

---

## Combined AI Agent Prompts

<details>
<summary><strong>Full combined prompt for all phases</strong></summary>

```
You are implementing issue #1233 (SPEC-002 P-31): the new package Encina.Http, an outbound HTTP handler stack.

PROJECT CONTEXT:
- .NET 10 / C# 14, nullable enabled, pre-1.0: best solution first, no compatibility layers, no [Obsolete].
- Built on Microsoft.Extensions.Http.Resilience (ADR-055): compose, never own retry or circuit breaking.
- Errors: core OutboundErrorCodes/OutboundErrorMetadataKeys/RetryAfterParser already exist (P-21, closed). Handlers
  cannot return Either: they throw OutboundRejectedException, mapped to a coded Left by RestApiRequestHandler.
- Tenant awareness (REQ-061): credentials, certificates and rate limits per tenant; missing tenant denied unless an
  explicit, logged opt-out; the reserved no-tenant segment `default` when tenancy is off (a tenant id `default` is
  refused when tenancy is on); tenancy on = the MultiTenancyMarker, tenant from IRequestContextAccessor.

IMPLEMENTATION OVERVIEW:
1. Package scaffold, named-client options and validator, AddEncinaHttpClient / AddEncinaHttpHandlers, README stating
   no AEAT endpoint, no Verifactu preset, no RRSIF type (ADR-057 D9).
2. Auth: IOutboundAuthProvider, API key and bearer from ISecretReader (secret name {client}/{tenant}/{purpose} with each segment percent-encoded),
   IAccessTokenSource (hook for #1234), IClientCertificateSource (hook for #1246), AuthHandler; new core codes
   auth_failed and tenant_required; OutboundRejectedException.
3. mTLS: a SocketsHttpHandler per (client, tenant) with a lifetime-bounded, certificate-expiry-aware cache; test with
   a Kestrel server requiring client certificates.
4. Rate limiting: PartitionedRateLimiter per (client, tenant), Retry-After gate pausing the partition, capped.
5. Redacting logging handler closest to the transport; Refit mapping of OutboundRejectedException.
6. Observability: EventId range Http = 6300-6349 registered first, ActivitySource and Meter Encina.Http, optional
   health check; tenant id as span attribute and metric tag when tenancy is on (ADR-054 D4). Encina handlers run per
   attempt after the standard resilience handler; error codes carry an ErrorCategory (ADR-062).
7. Tests: unit, guard, contract (auth provider contract), property (rate bound, redaction), integration (WireMock S9/S10,
   Kestrel mTLS, two-tenant isolation); docs, ADR, changelog, PublicAPI, manifest through the specialists.

KEY PATTERNS:
- Never log or tag credentials, bodies, URLs with query, headers, certificate content or exception messages.
- Time only from TimeProvider; handlers dispose evicted resources safely.
- Eight pull requests of at most 15 files each, each carrying the tests of its own files.

REFERENCE FILES:
- docs/plans/http-implementation-plan-1233.md
- docs/architecture/adr/055-resilience-family.md
- docs/architecture/adr/057-verifactu-boundary.md
- src/Encina.Refit/Handlers/RestApiRequestHandler.cs
- src/Encina/Recoverability/OutboundErrorCodes.cs
- src/Encina.Security.Secrets/Abstractions/ISecretReader.cs
```

</details>

---

## Cross-Cutting Integration Matrix

| # | Function | Status | Notes |
|---|----------|--------|-------|
| 1 | Caching | ❌ | No read path benefits from caching; the handler cache of Phase 3 is connection state, not data caching |
| 2 | OpenTelemetry | ✅ | Phase 6: `ActivitySource` and `Meter` named `Encina.Http`; tenant id as span attribute and metric tag when tenancy is on (ADR-054 D4); no payloads |
| 3 | Structured Logging | ✅ | Phase 6: `[LoggerMessage]` 6300-6307 in the new `Http` range registered first (ADR-021) |
| 4 | Health Checks | ✅ | Phase 6: optional per-client reachability check, no credentials sent |
| 5 | Validation | ✅ | Phase 1: named-client options validated at start-up |
| 6 | Resilience | ✅ | Phases 1 and 4: composes with Microsoft.Extensions.Http.Resilience; Retry-After-aware pacing; circuit breaking stays in the resilience packages |
| 7 | Distributed Locks | ❌ | Rate limiting is per process; cross-host pacing is the ordered-partition work of [#469](https://github.com/dlrivada/Encina/issues/469), not a lock |
| 8 | Transactions | ❌ | Single-operation calls; no atomic multi-operation need |
| 9 | Idempotency | ❌ | Not a message or request entry point; non-idempotent retry rules already live in `OutboundRetryRules` |
| 10 | Multi-Tenancy | ✅ | Phases 2 to 4 and 6: per-tenant credentials, certificates and pacing; fail closed without tenant (AC-043); the cross-capability layout follows [#1257](https://github.com/dlrivada/Encina/issues/1257) |
| 11 | Module Isolation | ❌ | SPEC-002 requires no module scoping here; `ModuleId` in messaging stays with #747 |
| 12 | Audit Trail | ❌ | Outbound calls are traced and metered, not audited; callers audit business operations |

---

## Prerequisites & Dependencies

| Item | State | Effect on this plan |
|------|-------|---------------------|
| [#1223](https://github.com/dlrivada/Encina/issues/1223) — outbound error taxonomy (P-21) | Closed, merged | Codes, metadata keys, `RetryAfterParser` and the classifier are reused |
| [#1234](https://github.com/dlrivada/Encina/issues/1234) — `Encina.Security.OAuth` (P-32) | Open | Implements `IAccessTokenSource` later; not a prerequisite (fakes in tests) |
| [#1246](https://github.com/dlrivada/Encina/issues/1246) — X.509 retrieval (P-44) | Open | Implements `IClientCertificateSource` later; not a prerequisite |
| [#1257](https://github.com/dlrivada/Encina/issues/1257) — multi-tenancy of the SPEC-002 capabilities | Open | May refine the secret naming (Dilemma 2); the convention sits in one class |
| [#2414](https://github.com/dlrivada/Encina/issues/2414) and the planned `Encina.Resilience` core | Open / unbuilt | Dilemma 1; the internal limiter interface allows the swap |
| [#2278](https://github.com/dlrivada/Encina/issues/2278) — rate limiting and circuit breaker independent | Open | Unaffected; they stay independent |

---

## Next Steps

1. Link this plan from issue #1233 and start the PR 1 brief (Phase 1); the other PRs follow in order.
2. When work on the issue starts, ask the maintainer to answer Dilemmas 1, 2, 3, 5, 6, 9 and 10 (limiter now or after the Encina.Resilience core; the per-tenant secret naming; handlers only or an `Either` client; handler per tenant or a client per tenant; interfaces defined here or by their owners; who adds the resilience handler; the visible defaults), record the answers in a `## Maintainer Decisions` section placed last, and have the adversarial-reviewer verify the Settled choices.
3. One commit per phase; the last commit of PR 8 references `Fixes #1233`.
