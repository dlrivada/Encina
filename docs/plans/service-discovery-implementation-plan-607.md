# Implementation Plan: `Encina.ServiceDiscovery` — `IServiceDiscoveryProvider` (Consul, Kubernetes DNS, Eureka, .NET Aspire)

> **Issue**: [#607](https://github.com/dlrivada/Encina/issues/607)
> **Type**: Feature
> **Complexity**: High (16 pull requests, 4 backends, 3 new provider packages)
> **Estimated Scope**: about 9,400 changed lines in 16 pull requests, tests, manifests and PublicAPI files included (table "Pull request split" under Research; each pull request is at most 15 files and 1,500 lines)

---

## Summary

Define the service-discovery abstraction `IServiceDiscoveryProvider` in a new `Encina.ServiceDiscovery` package and implement it for Consul, Kubernetes DNS, Eureka and .NET Aspire, with a cache, an endpoint selector and an `HttpClient` integration that resolves a service name per request. Nothing of it exists in `src/` today: the only Aspire-related project is `Encina.Aspire.Testing` (a testing helper), and `Directory.Packages.props` has no `Consul`, `Steeltoe` or `Microsoft.Extensions.ServiceDiscovery` entry.

SPEC-000 section 6 excludes cloud services without a free local emulator; all four backends run locally (Consul and Eureka in Docker, Kubernetes DNS behind a resolver seam, Aspire as a library), so all four are in 1.0 scope.

**Packages**: `Encina.ServiceDiscovery` (contract, cache, selector, Kubernetes DNS provider, `HttpClient` integration), `Encina.ServiceDiscovery.Consul`, `Encina.ServiceDiscovery.Eureka`, `Encina.ServiceDiscovery.Aspire` (each with its own dependency, so applications pay only for the backend they use; Design Choice 7 records the layout as a Dilemma).

**Provider category**: none of the AGENTS.md section 5 categories; the backends below are this feature's own provider set, and every feature must be consistent across them.

| Backend | Resolve | Watch | Register and health | Package |
|---|:-:|:-:|:-:|---|
| Consul | ✅ | ✅ (blocking query) | ✅ | `.Consul` |
| Eureka | ✅ | ✅ (polling the registry delta) | ✅ | `.Eureka` |
| Kubernetes DNS | ✅ | ✅ (polling) | not applicable | core |
| .NET Aspire | ✅ | ✅ (change token of the library) | not applicable | `.Aspire` |

The register and health column assumes Design Choice 1 option C (pending the maintainer).

**EventIds**: this plan takes 7340-7349 from the block 7300-7349 assigned to the feature-flag/service-discovery family (the feature-flag plans use 7300-7339); each package registers its own range in the pull request that creates it.

**Error contract**: ADR 062 (error category contract, merged in PR #3599) is on `main`. Every error code is created with the `EncinaErrors.Create(code, category, message, ...)` overload (`src/Encina/Results/EncinaErrors.cs`):

| Code | `ErrorCategory` | Meaning |
|---|---|---|
| `encina.servicediscovery.not_found` | `NotFound` | The backend does not know the service |
| `encina.servicediscovery.no_healthy_endpoint` | `Unavailable` | The service is known and has no healthy instance (Design Choice 6) |
| `encina.servicediscovery.provider_failed` | `UpstreamFailure` | The backend failed (after the resilience layer of Design Choice 10) |
| `encina.servicediscovery.invalid_registration` | `Validation` | A `ServiceRegistration` breaks its validation |

---

## Design Choices

<details>
<summary><strong>1. Scope of the interface — one wide interface, a narrow one, or two</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `IServiceDiscoveryProvider` with resolve and watch only; registration and health left to the platform** | What most callers need; Kubernetes and Aspire register nothing | Consul and Eureka self-registration and health need another path |
| **B) One interface with register, deregister, resolve, health and watch (the issue sketch)** | One interface for everything | Kubernetes DNS and Aspire can neither register nor report instance health, so they return a "not supported" `Left` at runtime |
| **C) Two interfaces: `IServiceDiscoveryProvider` (resolve, watch) and `IServiceRegistry` (register, deregister, health), the latter implemented only by Consul and Eureka** | A capability is visible at compile time; no runtime "not supported" | One more interface and registration |

### Chosen Option: **C — Two interfaces (recommended, pending the maintainer)**

### Rationale

- A wins when applications run on platforms that register services (Kubernetes); B wins when every application registers itself with Consul and one type is preferred; C wins when both kinds of backend are used and a missing capability should not be a runtime surprise.
- The Provider Implementation Matrix of the issue already marks register as "Not supported" for Kubernetes DNS and Aspire, and neither of them has per-instance health; C removes that runtime case.
- Under A or B only the interface split of Phases 1a and 1b changes; the backends are unchanged.

**Class:** Dilemma

</details>

<details>
<summary><strong>2. Own abstraction or reuse of <code>Microsoft.Extensions.ServiceDiscovery</code></strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) An Encina abstraction (Either results, watch, health) with an adapter to the Microsoft library as the Aspire provider** | One Encina model across Consul, Eureka, Kubernetes and Aspire; `Either` results; cache and selector shared | One more layer and an own `HttpClient` handler |
| **B) Use the Microsoft library directly and add only the providers it lacks** | Less code; the library is the .NET standard for `HttpClient` | Encina does not own the interface; the library has no registration, health or `Either`, so Consul registration and health would sit outside it |

### Chosen Option: **A — Encina abstraction with an Aspire adapter (recommended, pending the maintainer)**

### Rationale

- A wins when applications mix providers (Consul in some environments, Kubernetes in others) and want one model; B wins when the Microsoft model is accepted as the standard and only gaps are filled.
- Under B, Phase 4 (own `HttpClient` handler) and Phases 7a-7b (adapter) shrink to using the library's `AddServiceDiscovery`.

**Class:** Dilemma

</details>

<details>
<summary><strong>3. Relation to #382 (Service Discovery and Configuration Management)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) #607 is the single discovery contract; #382 is reduced to configuration management (watch of configuration keys)** | One contract per concern; matches the later, more specific issue | #382's text (`IServiceDiscovery`, `ResolveAsync` returning one endpoint) must be edited |
| **B) #382's `IServiceDiscovery` is the contract and #607 supplies the providers** | #382 stays as written | Two overlapping interfaces until one is retired; #382 has no `Either` results |

### Chosen Option: **A — #607 owns discovery (recommended, pending the maintainer)**

### Rationale

- A wins when the later issue is the detailed one (it has providers, results and a test matrix); B wins if #382's single-endpoint `ResolveAsync` shape is preferred.
- This plan builds nothing for configuration management; that stays with #382.

**Class:** Dilemma

</details>

<details>
<summary><strong>4. Contract conventions — Either results, <code>AtUtc</code> timestamps, <code>TimeProvider</code></strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `Task<Either<EncinaError, T>>` on every operation; UTC timestamps end in `AtUtc` (`InstanceHealth.LastCheckAtUtc`); time (cache expiry, polling) from `TimeProvider`** | Failure is explicit; the repository naming and time rules | Callers unwrap an `Either` |
| **B) Bare `Task` results and `DateTime` timestamps** | Simpler call site | Hides failure; breaks the naming and time rules |

### Chosen Option: **A — Either, `AtUtc`, `TimeProvider`**

### Rationale

- AGENTS.md section 3 (Railway Oriented Programming, `TimeProvider`) and section 4 (`AtUtc` suffix); the issue already rewrote its sketch this way. The shape of what `ResolveAsync` returns is Design Choice 5.

**Class:** Settled
**Settled by:** AGENTS.md section 3 (ROP, `TimeProvider`) and section 4 (naming); ADR-001; issue #607 body (interface definition)

</details>

<details>
<summary><strong>5. What <code>ResolveAsync</code> returns</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) All healthy endpoints (`IReadOnlyList<ServiceEndpoint>`); the selector picks one outside the provider** | The cache and the selector are provider-independent; the caller can use several endpoints (failover) | The caller or the `HttpClient` handler selects |
| **B) One endpoint per call (`Either<EncinaError, ServiceEndpoint>`, as #382's sketch)** | Simpler call site; wins when callers never need the list | Load balancing moves into every provider; the cache cannot hold the list |

### Chosen Option: **A — All endpoints, selector outside the provider (recommended, pending the maintainer)**

### Rationale

- A wins when several backends share one cache and selector (provider coherence, AGENTS.md section 3); B wins when the simplest call site matters more than shared behavior.

**Class:** Dilemma

</details>

<details>
<summary><strong>6. Resolution of a service with no healthy instance</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `Left` `no_healthy_endpoint` when the service is known and has no healthy instance; `Left` `not_found` when the backend does not know the service** | The `HttpClient` handler can never fall through to a raw name; an empty list is never reported as success (AGENTS.md section 3) | Callers handle two codes |
| **B) `Right` with an empty list** | The caller decides | An empty list can be mistaken for success; the handler needs an extra check |

### Chosen Option: **A — A typed Left (recommended, pending the maintainer)**

### Rationale

- A wins when a gate-like caller must fail closed; B wins when callers treat "none" as a normal state (a scale-to-zero service).

**Class:** Dilemma

</details>

<details>
<summary><strong>7. Package structure</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Core (contract, cache, selector, Kubernetes DNS, `HttpClient`) plus `.Consul`, `.Eureka`, `.Aspire`** | Kubernetes DNS needs no dependency beyond the BCL, so it stays in core; each SDK is isolated | Four packages; the issue's "Affected Packages" lists the first three only (the Aspire adapter adds `Microsoft.Extensions.ServiceDiscovery`) |
| **B) Kubernetes DNS in its own package `.Kubernetes` too** | Every backend is symmetric | A package with no dependency of its own |
| **C) Everything in one package** | One NuGet | Every user pulls Consul, Steeltoe and the Microsoft library, against pay-for-what-you-use (AGENTS.md section 1) |

### Chosen Option: **A — Core plus one package per dependency (recommended, pending the maintainer)**

### Rationale

- A wins when only dependency-carrying backends deserve a package; B wins when symmetry and independent versioning of the Kubernetes provider matter. C is out for the reason in its Cons; precedent for one package and one EventId range per backend: `Encina.Caching.Redis` and the secrets providers (`src/Encina/Diagnostics/EventIdRanges.cs`).

**Class:** Dilemma

</details>

<details>
<summary><strong>8. Cache and endpoint selection — shared decorator with Watch-driven invalidation</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `CachedServiceDiscoveryProvider` decorator (in-process, TTL from `TimeProvider`, invalidated by the provider's watch) and `IEndpointSelector` (round robin default, one counter per service per process)** | One cache and selector for all backends; testable with `FakeTimeProvider` | Per-instance cache: staleness up to the TTL |
| **B) Cache inside each provider** | Backend-specific tuning | Four implementations of the same logic, against provider coherence |

### Chosen Option: **A — Decorator and selector in core**

### Rationale

- Provider coherence: behavior shared across backends lives once in core; backends differ only in how they resolve (AGENTS.md section 3). B would put the same behavior in four places, so it does not comply.
- The cache key is the service name; a service registry is infrastructure, not tenant-owned data (function 10).

**Class:** Settled
**Settled by:** AGENTS.md section 3 (provider coherence)

</details>

<details>
<summary><strong>9. Defaults and registration dependencies</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) The cache is opt-in (`CacheEnabled = false`), `CacheDuration` 30 seconds; Kubernetes `PollInterval` 30 seconds and default domain `cluster.local` (names `service.namespace.svc.<domain>`); Consul asks for passing instances only, Eureka for status `UP` only; `AddEncinaServiceDiscovery` registers `TimeProvider`, `IMemoryCache` (through `TryAdd`) and the options** | Opt-in and fail-closed defaults; every dependency the services resolve is registered (registration completeness) | The numbers are a first guess |
| **B) The cache is on by default** | Faster by default | A cache the application did not ask for; stale endpoints surprise a failover |

### Chosen Option: **A — Opt-in cache and the defaults above (recommended, pending the maintainer)**

### Rationale

- A wins when pay-for-what-you-use and healthy-only resolution matter most; B wins for applications that resolve on a hot path and accept staleness.

**Class:** Dilemma

</details>

<details>
<summary><strong>10. Resilience of the Consul and Eureka calls</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) A Polly `ResiliencePipeline` inside each package (timeout 5 seconds, 2 retries with jitter; `Polly` and `Microsoft.Extensions.Resilience` are already in `Directory.Packages.props`), configured from its options; failures after the pipeline are `Left` `provider_failed`; no shared policy across packages** | Bounded latency and transient-failure recovery for the two networked catalogs | Another options surface per package |
| **B) Rely on the SDK client timeouts only** | No new dependency | Transient failures surface at once; no retry |

### Chosen Option: **A — Per-package pipeline (recommended, pending the maintainer)**

### Rationale

- A wins when the catalog is called per request outside the cache; B wins when the cache (Design Choice 8) absorbs almost every call. Same approach as the network providers of the feature-flag family (#604 Design Choice 16).

**Class:** Dilemma

</details>

<details>
<summary><strong>11. Which request hosts the <code>HttpClient</code> handler rewrites</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) An explicit list of service names in the options (`ServiceDiscoveryHttpOptions.Services`); a request whose host is on the list is resolved and rewritten; any other host passes through untouched; a failed resolution never falls through to the raw name** | The application states which names are services; a real host is never rewritten by mistake | The list must be maintained |
| **B) Any host without a dot is treated as a service name** | No list | A single-label real host (`localhost`, an intranet name) is hijacked |

### Chosen Option: **A — Explicit list (recommended, pending the maintainer)**

### Rationale

- A wins when the application knows its services; B wins for dynamic environments where the names are not known at start. A resolution `Left` becomes an `HttpRequestException` carrying the error code, never a pass-through (the one place an exception is required by the `HttpClient` contract).

**Class:** Dilemma

</details>

<details>
<summary><strong>12. How a watch reports failure</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `WatchAsync` returns `IAsyncEnumerable<Either<EncinaError, ServiceEndpointChange>>`; a failure is a `Left` element and the producer retries with a jittered backoff (time from `TimeProvider`); each loop catches per iteration and never lets an exception escape; the cache evicts the service on a `Left` and entries otherwise live until the TTL** | Failures are visible, not swallowed; matches Railway Oriented Programming | Consumers handle an `Either` per element |
| **B) `ServiceEndpointChange` has a `Failed` change type with an error code** | One element type | A failure is mixed into the data type |
| **C) The stream ends on failure and the consumer restarts it** | Simplest producer | A silent end looks like completion; the restart policy sits in every consumer |

### Chosen Option: **A — `Either` elements (recommended, pending the maintainer)**

### Rationale

- A wins when failures must be seen and counted (EventId 7342); C wins when the producers must stay trivial. Background loops catch per iteration, cancellation from the stopping token ends the loop and is not an error, and a failure is handled like any background `Left` (AGENTS.md section 3).

**Class:** Dilemma

</details>

<details>
<summary><strong>13. Eureka client</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `Steeltoe.Discovery.Eureka` as the issue says** | Maintained client; registration, heartbeats, delta fetch | A large new dependency; its .NET 10 support is confirmed at the start of Phase 6a |
| **B) A thin `HttpClient` client against the Eureka REST API** | No dependency | Heartbeat, renewal and delta logic to own |

### Chosen Option: **A — Steeltoe client (recommended, pending the maintainer)**

### Rationale

- A wins when a maintained client is preferred; B wins when the dependency is too heavy or does not support net10.0. If the pinned version does not support net10.0, B is the only buildable option and the maintainer is told at the start of Phase 6a.
- Credentials in options, if configured, are `[JsonIgnore]` with a `ToString()` override (AGENTS.md section 3).

**Class:** Dilemma

</details>

<details>
<summary><strong>14. Kubernetes DNS integration tests</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Unit tests over the `IDnsResolver` seam with a fake resolver, plus a justification file for the integration test type (a real cluster DNS is not available locally)** | No container; fast | The real `Dns` call is covered only by the thin seam |
| **B) An integration test against a CoreDNS container serving a zone file** | Exercises the real resolver | A container fixture and a zone file for one provider |

### Chosen Option: **A — Resolver seam and a justification file (recommended, pending the maintainer)**

### Rationale

- A wins when the provider is a thin mapping over the resolver; B wins when the real name resolution is the risk.

**Class:** Dilemma

</details>

---

## Implementation Phases

Each phase is one pull request: it carries its own unit, guard and (where it applies) contract and integration tests, its coverage-manifest entries (`.github/coverage-manifest/Encina.ServiceDiscovery*.json`), its `PublicAPI.Unshipped.txt` lines, the project references of the test projects it needs and the diagnostics of the code it adds, so no phase uses a type, project or log class that a later one creates. Every new file under `src/` gets per-flag targets in the package manifest, demanding and realistic, each with a one-sentence justification; a target of 0 needs a reason (for example the unit flag of a file that only a live backend can run, reached by the integration flag). The table "Pull request split" under Research lists the files and lines of each phase.

### Phase 1a: Package, Discovery Contract and Errors

> **Goal**: The package and the resolve side of the contract.

<details>
<summary><strong>Tasks</strong></summary>

1. `src/Encina.ServiceDiscovery/Encina.ServiceDiscovery.csproj` (net10.0; `Encina`, `Microsoft.Extensions.Options`, `Microsoft.Extensions.Logging.Abstractions`), `PublicAPI.Shipped.txt`, `PublicAPI.Unshipped.txt`, the project in `Encina.slnx`.
2. `Abstractions/IServiceDiscoveryProvider.cs` (`ResolveAsync`, `WatchAsync`; Design Choices 5 and 12), `Model/ServiceEndpoint.cs`, `Model/ServiceEndpointChange.cs`, `Model/ServiceEndpointChangeType.cs`.
3. `ServiceDiscoveryErrors.cs` with the codes and `ErrorCategory` values of the Summary table (`invalid_registration` is added in Phase 1b).
4. `.github/coverage-manifest/Encina.ServiceDiscovery.json`; project references in `tests/Encina.UnitTests/Encina.UnitTests.csproj` and `tests/Encina.GuardTests/Encina.GuardTests.csproj`; unit tests of the errors and models; guard tests.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 1a</strong></summary>

```text
CONTEXT:
- Issue #607, Phase 1a. Encina is .NET 10 / C# 14 with Railway Oriented Programming; pre-1.0. ADR 062 is merged: every error code has an ErrorCategory.
TASK:
- Create the Encina.ServiceDiscovery project with the discovery contract, the endpoint models, the errors, the manifest and the tests.
KEY RULES:
- Either<EncinaError, T> on every method; UTC timestamps end in AtUtc.
- EncinaErrors.Create(code, category, message), never the two-argument overload.
- No EventIds in this phase; XML docs on all public members; PublicAPI.Unshipped.txt.
- At most 15 files and 1500 lines.
REFERENCE FILES:
- src/Encina/Results/EncinaErrors.cs
- src/Encina/Errors/ErrorCategory.cs
- src/Encina.Caching.Redis/Encina.Caching.Redis.csproj
```

</details>

---

### Phase 1b: Registry Contract and Registration Model

> **Goal**: The register and health side of the contract (Design Choice 1 option C).

<details>
<summary><strong>Tasks</strong></summary>

1. `Abstractions/IServiceRegistry.cs` (`RegisterAsync`, `DeregisterAsync`, `GetHealthAsync`), `Model/ServiceRegistration.cs` (validated: name, port range, health check timings), `Model/HealthCheckConfig.cs`, `Model/ServiceHealth.cs`, `Model/InstanceHealth.cs` (`LastCheckAtUtc`).
2. `ServiceDiscoveryErrors.cs`: add `invalid_registration` (category `Validation`).
3. `PublicAPI.Unshipped.txt`, manifest entries, unit tests of the registration validation, guard tests.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 1b</strong></summary>

```text
CONTEXT:
- Issue #607, Phase 1b. Phase 1a is merged. Under Design Choice 1 options A or B this phase changes shape.
TASK:
- Add IServiceRegistry, the registration and health models, the invalid_registration error and their tests.
KEY RULES:
- Validation returns Either with detailed errors, never an exception.
- Timestamps end in AtUtc.
REFERENCE FILES:
- src/Encina.ServiceDiscovery/Abstractions/IServiceDiscoveryProvider.cs
- src/Encina/Results/EncinaErrors.cs
```

</details>

---

### Phase 2a: Options, Registration, Selector and Core Diagnostics

> **Goal**: The options, `AddEncinaServiceDiscovery`, the selector and the core diagnostics, created before any code that logs.

<details>
<summary><strong>Tasks</strong></summary>

1. `ServiceDiscoveryOptions.cs` (`CacheEnabled`, `CacheDuration`; defaults of Design Choice 9; validated on start) and `ServiceCollectionExtensions.AddEncinaServiceDiscovery` (registers the options and `TimeProvider` with `TryAdd`, and `IMemoryCache` with `TryAdd` only when `CacheEnabled` is true, so an application that does not enable the cache gets no cache registration).
2. `Selection/IEndpointSelector.cs`, `Selection/RoundRobinEndpointSelector.cs` (one counter per service per process).
3. `src/Encina/Diagnostics/EventIdRanges.cs`: `ServiceDiscovery = (7340, 7343)`; the `AssemblyRanges` entry in `EncinaEventIdAllocationTests.cs`; the `Encina` `PublicAPI.Unshipped.txt` line; the per-file entry for `src/Encina/Diagnostics/EventIdRanges.cs` in `.github/coverage-manifest/Encina.json` (unit flag at its target through the allocation tests; guard flag 0 because the file has no executable code with parameters), entered unconditionally here and reused by Phases 5b, 6b and 7b.
4. `Diagnostics/ServiceDiscoveryDiagnostics.cs`: `ActivitySource` and `Meter` `Encina.ServiceDiscovery`; counter `encina.servicediscovery.resolutions` (tags `provider`, `outcome`), span `encina.servicediscovery.resolve` (tags `service.name`, `provider`). `Diagnostics/ServiceDiscoveryLogMessages.cs`: `[LoggerMessage]` EventIds 7340 resolve failed, 7341 cache entry evicted, 7342 watch failed, 7343 no healthy endpoint. No endpoint addresses or metadata values in tags or logs.
5. Tests: selector, options and DI (`ValidateOnBuild`, `ValidateScopes`), diagnostics, guard tests; manifest entries and PublicAPI.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 2a</strong></summary>

```text
CONTEXT:
- Issue #607, Phase 2a. Phases 1a-1b are merged.
TASK:
- Add the options, the registration, the round-robin selector, the EventId range, the diagnostics and log messages, with tests.
KEY RULES:
- Register ServiceDiscovery in EventIdRanges.cs first (ADR-021); EventIds sequential inside 7340-7343; [LoggerMessage] source generator.
- Never log EncinaError.Message or endpoint addresses; log the error code.
- AddEncinaServiceDiscovery registers every dependency it resolves; DI test with ValidateOnBuild and ValidateScopes.
- At most 15 files and 1500 lines.
REFERENCE FILES:
- src/Encina.Compliance.Consent/ServiceCollectionExtensions.cs
- src/Encina.Compliance.Consent/Diagnostics/ConsentDiagnostics.cs
- src/Encina/Diagnostics/EventIdRanges.cs
```

</details>

---

### Phase 2b: Cache Decorator

> **Goal**: `CachedServiceDiscoveryProvider` (Design Choices 8, 9 and 12).

<details>
<summary><strong>Tasks</strong></summary>

1. `Caching/CachedServiceDiscoveryProvider.cs` (decorator; `IMemoryCache`; TTL from `ServiceDiscoveryOptions.CacheDuration`; a `Left` is never cached; one watch loop per cached service started on first resolve and stopped at disposal, catching per iteration; a `Left` element evicts the service and logs 7341/7342); registered when `CacheEnabled`, in any registration order, without overriding the application's own provider.
2. Tests with `FakeTimeProvider`: hit, miss, expiry, eviction on a watch change and on a `Left`, loop never throws, DI test for cache on and off; guard tests; manifest entries and PublicAPI.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 2b</strong></summary>

```text
CONTEXT:
- Issue #607, Phase 2b. Phase 2a is merged (log messages 7340-7343 exist).
TASK:
- Implement the cache decorator with watch-driven invalidation and its tests.
KEY RULES:
- Time only through TimeProvider; the cache key is the service name.
- The watch loop catches per iteration, never lets an exception escape; cancellation ends it (AGENTS.md section 3).
- Registration uses TryAdd* and never overrides the application's own.
REFERENCE FILES:
- src/Encina.ServiceDiscovery/Abstractions/IServiceDiscoveryProvider.cs
- src/Encina.Caching.Memory/ (decorator and registration pattern)
```

</details>

---

### Phase 3: Kubernetes DNS Provider

> **Goal**: The dependency-free backend.

<details>
<summary><strong>Tasks</strong></summary>

1. `Kubernetes/IDnsResolver.cs` (seam over `Dns.GetHostAddressesAsync`), `Kubernetes/KubernetesDnsServiceDiscoveryProvider.cs` (name `service.namespace.svc.<domain>`; port from `KubernetesDnsOptions.Services[name]`, because plain A records carry no port; SRV lookup is a documented non-goal; watch by polling every `PollInterval` with `Either` elements, Design Choice 12), `Kubernetes/KubernetesDnsOptions.cs`, `AddEncinaServiceDiscoveryKubernetes`.
2. Tests: provider with a fake `IDnsResolver` and `FakeTimeProvider`, DI test, guard tests; the integration justification file `tests/Encina.IntegrationTests/ServiceDiscovery/KubernetesDns.md` (Design Choice 14); manifest entries and PublicAPI.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 3</strong></summary>

```text
CONTEXT:
- Issue #607, Phase 3. Phase 2a is merged. Design Choice 14 is pending the maintainer: under B a CoreDNS fixture and an integration test replace the justification file.
TASK:
- Implement the resolver seam, the Kubernetes DNS provider, its options and registration, with tests.
KEY RULES:
- Polling loop catches per iteration and never lets an exception escape (AGENTS.md section 3).
- Registration uses TryAdd* and never overrides the application's own.
- No contract-test justification file; the .md is for the integration type only.
REFERENCE FILES:
- src/Encina.ServiceDiscovery/Abstractions/IServiceDiscoveryProvider.cs
- src/Encina.Caching.Memory/ServiceCollectionExtensions.cs
```

</details>

---

### Phase 4: `HttpClient` Integration

> **Goal**: Resolve a service name per request (Design Choice 11).

<details>
<summary><strong>Tasks</strong></summary>

1. `Http/ServiceDiscoveryHttpOptions.cs` (the `Services` list), `Http/ServiceDiscoveryHttpMessageHandler.cs`: a `DelegatingHandler` that rewrites `http(s)://{serviceName}/...` for the listed names to the selected endpoint; a `Left` becomes an `HttpRequestException` carrying the error code.
2. `Http/ServiceDiscoveryHttpClientBuilderExtensions.cs`: `AddEncinaServiceDiscovery(this IHttpClientBuilder)`.
3. Tests with a stub inner handler (no network): rewrite, pass-through of unlisted hosts, no fall-through on a `Left`; DI test; guard tests; manifest entries and PublicAPI.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 4</strong></summary>

```text
CONTEXT:
- Issue #607, Phase 4. Applies only if Design Choice 2 is decided for option A (own abstraction); under B this phase uses the Microsoft library's AddServiceDiscovery instead.
TASK:
- Implement the delegating handler, its options and the IHttpClientBuilder extension, with tests.
KEY RULES:
- Hosts not on the list pass through untouched; a failed resolution never falls through to DNS of the raw name.
- Never log the full URI query or headers; log the service name and error code.
REFERENCE FILES:
- src/Encina.ServiceDiscovery/Selection/RoundRobinEndpointSelector.cs
- src/Encina.Compliance.Consent/ServiceCollectionExtensions.cs
```

</details>

---

### Phase 5a: Consul Package Skeleton and Options

> **Goal**: The `Encina.ServiceDiscovery.Consul` project and its options.

<details>
<summary><strong>Tasks</strong></summary>

1. `Directory.Packages.props`: pin `Consul`; project `src/Encina.ServiceDiscovery.Consul/` with `PublicAPI.Shipped.txt`, `PublicAPI.Unshipped.txt` and the entry in `Encina.slnx`.
2. `ConsulServiceDiscoveryOptions.cs` (ACL token `[JsonIgnore]`, `ToString()` override; Polly timeout and retry settings of Design Choice 10; validated on start).
3. Tests: project references in the unit, guard, contract and integration test projects; options tests (redacted `ToString()`); a guard test; `.github/coverage-manifest/Encina.ServiceDiscovery.Consul.json`.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 5a</strong></summary>

```text
CONTEXT:
- Issue #607, Phase 5a. Phase 1a is merged.
TASK:
- Create the Consul project, its options, the pinned package, the manifest and the test project references.
KEY RULES:
- The ACL token is a secret: [JsonIgnore] and a ToString() override.
- At most 15 files and 1500 lines.
REFERENCE FILES:
- src/Encina.Caching.Redis/ (package and options layout)
- Directory.Packages.props
```

</details>

---

### Phase 5b: Consul Provider

> **Goal**: `ConsulServiceDiscoveryProvider`.

<details>
<summary><strong>Tasks</strong></summary>

1. `ConsulServiceDiscoveryProvider.cs` (healthy instances only; `WatchAsync` by blocking query with the index, `Either` elements, Design Choice 12; Polly pipeline of Design Choice 10), `AddEncinaServiceDiscoveryConsul`.
2. `EventIdRanges.cs`: `ServiceDiscoveryConsul = (7344, 7345)`, the `AssemblyRanges` entry and the `Encina` PublicAPI line; `Diagnostics/ConsulLogMessages.cs` (7344 watch failed, 7345 request failed after the pipeline).
3. Phase 5a adds `ConsulFixture` (Testcontainers) in `tests/Encina.TestInfrastructure/Fixtures/` and its collection definition, so the fixture is in place before the integration tests of this phase use it; the image tag is the vendor's published tag, defined once.
4. Tests: provider unit tests, DI test (`ValidateOnBuild`, `ValidateScopes`), guard tests, the `IServiceDiscoveryProvider` contract suite, an integration test (`[Collection]`); manifest entries.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 5b</strong></summary>

```text
CONTEXT:
- Issue #607, Phase 5b. Phase 5a is merged. Consul is tested in Docker with a Testcontainers fixture.
TASK:
- Create the Consul provider, its registration, EventId range, log messages, fixture and tests.
KEY RULES:
- A Consul error becomes Left provider_failed; never an empty list reported as success (an empty healthy set is Left no_healthy_endpoint, Design Choice 6).
- Register ServiceDiscoveryConsul first; EventIds only inside 7344-7345.
- At most 15 files and 1500 lines.
REFERENCE FILES:
- src/Encina.ServiceDiscovery/Abstractions/IServiceDiscoveryProvider.cs
- tests/Encina.TestInfrastructure/Fixtures/RedisFixture.cs
```

</details>

---

### Phase 5c: Consul Registry

> **Goal**: `ConsulServiceRegistry` (Design Choice 1 option C).

<details>
<summary><strong>Tasks</strong></summary>

1. `ConsulServiceRegistry.cs` (registration with the check from `HealthCheckConfig`, deregistration, instance health), registration in `AddEncinaServiceDiscoveryConsul`.
2. Tests: registry unit tests, guard tests, the `IServiceRegistry` contract suite, an integration test on the `ConsulFixture`; manifest entries and PublicAPI.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 5c</strong></summary>

```text
CONTEXT:
- Issue #607, Phase 5c. Phase 5b is merged.
TASK:
- Add the Consul registry with its tests.
KEY RULES:
- Re-registering the same instance id is an upsert; a Consul error becomes Left provider_failed.
- Shared [Collection] fixture; never dispose it from a test.
REFERENCE FILES:
- src/Encina.ServiceDiscovery.Consul/ConsulServiceDiscoveryProvider.cs
- src/Encina.ServiceDiscovery/Abstractions/IServiceRegistry.cs
```

</details>

---

### Phase 6a: Eureka Package Skeleton and Options

> **Goal**: The `Encina.ServiceDiscovery.Eureka` project and its options (Design Choice 13).

<details>
<summary><strong>Tasks</strong></summary>

1. Confirm the pinned `Steeltoe.Discovery.Eureka` supports net10.0; pin it in `Directory.Packages.props`; project `src/Encina.ServiceDiscovery.Eureka/` with `PublicAPI.Shipped.txt`, `PublicAPI.Unshipped.txt` and the entry in `Encina.slnx`.
2. `EurekaServiceDiscoveryOptions.cs` (credentials `[JsonIgnore]` with a `ToString()` override; Polly settings; validated on start).
3. Tests: project references in the unit, guard, contract and integration test projects; options tests; a guard test; `.github/coverage-manifest/Encina.ServiceDiscovery.Eureka.json`.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 6a</strong></summary>

```text
CONTEXT:
- Issue #607, Phase 6a. Design Choice 13 is pending the maintainer.
TASK:
- Create the Eureka project, its options, the pinned package, the manifest and the test project references.
KEY RULES:
- Credentials, if any, are [JsonIgnore] with a ToString() override.
- At most 15 files and 1500 lines.
REFERENCE FILES:
- src/Encina.ServiceDiscovery.Consul/ (sibling package)
- Directory.Packages.props
```

</details>

---

### Phase 6b: Eureka Provider

> **Goal**: `EurekaServiceDiscoveryProvider`.

<details>
<summary><strong>Tasks</strong></summary>

1. `EurekaServiceDiscoveryProvider.cs` (instances with status `UP` only; registry delta polling with `Either` elements; Polly pipeline), `AddEncinaServiceDiscoveryEureka`.
2. `EventIdRanges.cs`: `ServiceDiscoveryEureka = (7346, 7347)`, the `AssemblyRanges` entry and the `Encina` PublicAPI line; `Diagnostics/EurekaLogMessages.cs` (7346 delta poll failed, 7347 request failed after the pipeline).
3. Phase 6a adds `EurekaFixture` (Testcontainers; the image tag is the vendor's published tag, defined once) and its collection definition, before the integration tests of this phase use it.
4. Tests: provider unit tests, DI test, guard tests, the provider contract suite, an integration test; manifest entries.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 6b</strong></summary>

```text
CONTEXT:
- Issue #607, Phase 6b. Phase 6a is merged.
TASK:
- Create the Eureka provider, registration, EventId range, log messages, fixture and tests.
KEY RULES:
- Registry delta polling catches per iteration and never throws out of the loop.
- Register ServiceDiscoveryEureka first; EventIds only inside 7346-7347.
- At most 15 files and 1500 lines.
REFERENCE FILES:
- src/Encina.ServiceDiscovery.Consul/ (sibling package)
- tests/Encina.TestInfrastructure/Fixtures/RabbitMqFixture.cs
```

</details>

---

### Phase 6c: Eureka Registry

> **Goal**: `EurekaServiceRegistry`.

<details>
<summary><strong>Tasks</strong></summary>

1. `EurekaServiceRegistry.cs` (instance id, heartbeat owned by the client, instance health), registration in `AddEncinaServiceDiscoveryEureka`.
2. Tests: registry unit tests, guard tests, the `IServiceRegistry` contract suite, an integration test on the `EurekaFixture`; manifest entries and PublicAPI.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 6c</strong></summary>

```text
CONTEXT:
- Issue #607, Phase 6c. Phase 6b is merged.
TASK:
- Add the Eureka registry with its tests.
KEY RULES:
- Re-registering the same instance id is an upsert; errors become Left provider_failed.
REFERENCE FILES:
- src/Encina.ServiceDiscovery.Eureka/EurekaServiceDiscoveryProvider.cs
- src/Encina.ServiceDiscovery.Consul/ConsulServiceRegistry.cs
```

</details>

---

### Phase 7a: Aspire Package Skeleton

> **Goal**: The `Encina.ServiceDiscovery.Aspire` project and the pinned library.

<details>
<summary><strong>Tasks</strong></summary>

1. `Directory.Packages.props`: pin `Microsoft.Extensions.ServiceDiscovery`; project `src/Encina.ServiceDiscovery.Aspire/` with `PublicAPI.Shipped.txt`, `PublicAPI.Unshipped.txt` and the entry in `Encina.slnx`.
2. `.github/coverage-manifest/Encina.ServiceDiscovery.Aspire.json` and the project references in the unit, guard, contract and integration test projects.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 7a</strong></summary>

```text
CONTEXT:
- Issue #607, Phase 7a. Existing Encina.Aspire.Testing is a testing helper and is not changed.
TASK:
- Create the Aspire project skeleton, the pinned package, the manifest and the test project references.
KEY RULES:
- Package version only in Directory.Packages.props.
REFERENCE FILES:
- src/Encina.Aspire.Testing/EncinaTestContext.cs
- src/Encina.ServiceDiscovery.Consul/ (sibling package)
```

</details>

---

### Phase 7b: Aspire Adapter

> **Goal**: `AspireServiceDiscoveryProvider` over `Microsoft.Extensions.ServiceDiscovery`.

<details>
<summary><strong>Tasks</strong></summary>

1. `AspireServiceDiscoveryProvider.cs` (resolve through the library's resolver; watch through its change token with `Either` elements; the library exposes no per-instance health, so this package implements no `IServiceRegistry`), `AddEncinaServiceDiscoveryAspire`.
2. `EventIdRanges.cs`: `ServiceDiscoveryAspire = (7348, 7349)`, the `AssemblyRanges` entry and the `Encina` PublicAPI line; `Diagnostics/AspireLogMessages.cs` (7348 resolver failed, 7349 change token failed).
3. Tests with the library's configuration-based resolver (`services__name__http__0` style configuration); no AppHost required: provider unit tests, DI test, guard tests, the provider contract suite; manifest entries.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 7b</strong></summary>

```text
CONTEXT:
- Issue #607, Phase 7b. Phase 7a is merged.
TASK:
- Create the Aspire adapter, registration, EventId range, log messages and tests.
KEY RULES:
- Resolve and watch only; this package does not implement IServiceRegistry (Design Choice 1).
- Either results; library exceptions become provider_failed with the exception attached (logged through ForLogging, never the message).
- Register ServiceDiscoveryAspire first; EventIds only inside 7348-7349.
REFERENCE FILES:
- src/Encina.Aspire.Testing/EncinaTestContext.cs
- src/Encina.ServiceDiscovery/Abstractions/IServiceDiscoveryProvider.cs
```

</details>

---

### Phase 8: Health Check

> **Goal**: An opt-in health check for the core.

<details>
<summary><strong>Tasks</strong></summary>

1. `Health/ServiceDiscoveryHealthCheck.cs` (`DefaultName` const, static `Tags` array, resolves the provider through `IServiceProvider.CreateScope()` and a configured probe service; the result carries the error code, never the message), opt-in registration in `ServiceCollectionExtensions`.
2. Tests: health check unit tests, DI test, guard tests; manifest entries and PublicAPI.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 8</strong></summary>

```text
CONTEXT:
- Issue #607, Phase 8. The core and at least one backend are merged.
TASK:
- Add the health check, its opt-in registration and tests.
KEY RULES:
- Never put EncinaError.Message or endpoint addresses in the health result; use the error code.
REFERENCE FILES:
- src/Encina.Compliance.Consent/Health/ConsentHealthCheck.cs
- src/Encina.Messaging/Health/IEncinaHealthCheck.cs
```

</details>

---

### Phase 9: Documentation, Justifications and Finalization

> **Goal**: Document the feature and the decisions, and justify the test types that are not applicable.

<details>
<summary><strong>Tasks</strong></summary>

1. ADR for the interface scope and the relation to the Microsoft library (Design Choices 1-3); ADR number assigned when the ADR pull request opens.
2. Package READMEs (four), a guide under `docs/features/`, `changelog.d/607-service-discovery.added.md`, `docs/INVENTORY.md`, ROADMAP entry.
3. Justification files: `tests/Encina.LoadTests/ServiceDiscovery/ServiceDiscovery.md` (lookups, no concurrent shared state beyond the cache), `tests/Encina.PropertyTests/ServiceDiscovery/ServiceDiscovery.md` (lookups are simple mappings) and `tests/Encina.BenchmarkTests/ServiceDiscovery/ServiceDiscovery.md` (the cached resolve path is a dictionary read; no benchmark until a hot path is measured).
4. `dotnet build --configuration Release` with 0 warnings; tests pass; per-flag coverage of every new file at its target.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 9</strong></summary>

```text
CONTEXT:
- Issue #607, Phase 9; documentation goes through docs-writer, the fragment through mechanical-fixer.
TASK:
- Write the ADR, the READMEs, the guide, the fragment, the INVENTORY/ROADMAP updates and the justification files.
KEY RULES:
- Never edit CHANGELOG.md; coverage figures only through covref markers.
- No contract-test justification file.
- This pull request carries Fixes #607.
REFERENCE FILES:
- docs/architecture/adr/index.md
- changelog.d/README.md
```

</details>

---

## Research

### Relevant Standards & Specifications

| Source | Provision | What it asks of this feature |
|--------|-----------|------------------------------|
| SPEC-000 section 6, DEC-009 | Cloud services without a free local emulator are outside 1.0 | All four backends run locally, so all are in scope |
| AGENTS.md section 3 | ROP, `TimeProvider`, secrets in options, loops never throw, provider coherence | Design Choices 4, 8, 12 and 13 |
| AGENTS.md section 5 | Provider coherence across a feature's backends | Contract suite over all backends |
| ADR 062 | Error category contract (merged in PR #3599) | Every error code has a category |
| Consul HTTP API, Eureka REST protocol, Kubernetes DNS specification | Health-aware catalog, registry, service names | Provider behavior |
| `Microsoft.Extensions.ServiceDiscovery` | Resolver and change tokens | Aspire adapter |

### Existing Encina Infrastructure to Leverage

| Component | Location | Usage in this feature |
|-----------|----------|----------------------|
| `EncinaErrors.Create` (with `ErrorCategory`), `Either` | `src/Encina/Results/EncinaErrors.cs` | Errors |
| `EventIdRanges` | `src/Encina/Diagnostics/EventIdRanges.cs` | Ranges 7340-7349 |
| Satellite registration pattern | `src/Encina.Compliance.Consent/ServiceCollectionExtensions.cs` | `AddEncinaServiceDiscovery*` |
| Health check pattern | `src/Encina.Compliance.Consent/Health/ConsentHealthCheck.cs` | `ServiceDiscoveryHealthCheck` |
| Diagnostics pattern | `src/Encina.Compliance.Consent/Diagnostics/ConsentDiagnostics.cs` | `ServiceDiscoveryDiagnostics` |
| Testcontainers fixtures | `tests/Encina.TestInfrastructure/Fixtures/` | Consul and Eureka fixtures |
| `Encina.Aspire.Testing` | `src/Encina.Aspire.Testing/` | Not changed; a testing helper only |
| Related issues | #382 (open, service discovery and configuration management), #453 (closed) | Design Choice 3 |

### Event ID Allocation

| Package | Range | Notes |
|---------|-------|-------|
| **`Encina.ServiceDiscovery`** | **7340-7343** | New, registered in Phase 2a |
| **`Encina.ServiceDiscovery.Consul`** | **7344-7345** | New, registered in Phase 5b |
| **`Encina.ServiceDiscovery.Eureka`** | **7346-7347** | New, registered in Phase 6b |
| **`Encina.ServiceDiscovery.Aspire`** | **7348-7349** | New, registered in Phase 7b |
| `Encina.FeatureFlags*` | 7300-7339 | The feature-flag plans (#604, #300, #345, #451) |

### Estimated File Count

| Category | Files | Notes |
|----------|------:|-------|
| Core (Phases 1a-4, 8) | 70 | Contract, models, options, selector, diagnostics, cache, Kubernetes DNS, handler, health check, with their tests |
| Consul (Phases 5a-5c) | 36 | Package, provider, registry, fixture, tests |
| Eureka (Phases 6a-6c) | 36 | Package, provider, registry, fixture, tests |
| Aspire (Phases 7a-7b) | 22 | Package, adapter, tests |
| Documentation (Phase 9) | 12 | ADR, 4 READMEs, guide, fragment, INVENTORY, ROADMAP, 3 justification files |
| **Total** | **176** | 16 pull requests of at most 15 files and 1500 lines (next table) |

#### Pull Request Split

Files include tests, manifests, `PublicAPI` files, `Directory.Packages.props`, the solution entry, test project references and the changelog fragment; lines are estimates re-measured when each pull request opens. Every pull request is at most 15 files and 1500 lines.

| Phase | Content | Files | Lines |
|-------|---------|------:|------:|
| 1a | Project, PublicAPI (2), solution entry, discovery contract, endpoint models (3), errors, manifest, 2 test project references, unit and guard tests | 14 | 600 |
| 1b | Registry contract, registration models (4), errors edit, PublicAPI, manifest, unit and guard tests | 10 | 450 |
| 2a | Options, registration, selector (2), EventId range and allocation test, diagnostics, log messages, PublicAPI (2), manifest, `Encina` manifest entry for `EventIdRanges.cs`, selector tests, options-DI-and-diagnostics tests, guard tests | 15 | 800 |
| 2b | Cache decorator, registration edit, PublicAPI, manifest, cache, DI edit, guard tests | 7 | 500 |
| 3 | Resolver seam, Kubernetes provider, options, registration edit, PublicAPI, manifest, provider tests, DI tests, guard tests, integration justification file | 10 | 600 |
| 4 | Handler options, handler, builder extension, PublicAPI, manifest, handler, DI and guard tests | 8 | 500 |
| 5a | Consul pin, project, PublicAPI (2), solution entry, options, manifest, 4 test project references, options and guard tests, `ConsulFixture` and collection (2) | 15 | 450 |
| 5b | Consul provider, registration, log messages, EventId range and allocation test (2), PublicAPI (2), manifest, unit, DI, guard, contract and integration tests | 13 | 1,000 |
| 5c | Consul registry, registration edit, PublicAPI, manifest, unit, guard, contract and integration tests | 8 | 600 |
| 6a | Eureka pin, project, PublicAPI (2), solution entry, options, manifest, 4 test project references, options and guard tests, `EurekaFixture` and collection (2) | 15 | 450 |
| 6b | Eureka provider, registration, log messages, EventId range and allocation test (2), PublicAPI (2), manifest, unit, DI, guard, contract and integration tests | 13 | 1,000 |
| 6c | Eureka registry, registration edit, PublicAPI, manifest, unit, guard, contract and integration tests | 8 | 600 |
| 7a | Aspire pin, project, PublicAPI (2), solution entry, manifest, 4 test project references | 10 | 300 |
| 7b | Aspire provider, registration, log messages, EventId range and allocation test (2), PublicAPI (2), manifest, unit, DI, guard and contract tests | 12 | 700 |
| 8 | Health check, registration edit, PublicAPI, manifest, unit and guard tests | 6 | 350 |
| 9 | ADR, 4 READMEs, guide, fragment, INVENTORY, ROADMAP, 3 justification files | 12 | 500 |
| **Total** | 16 pull requests | **176** | **9,400** |

---

## Combined AI Agent Prompts

<details>
<summary><strong>Full combined prompt for all phases</strong></summary>

```text
You are implementing issue #607: Encina.ServiceDiscovery (Consul, Kubernetes DNS, Eureka, .NET Aspire).

PROJECT CONTEXT:
- Encina is a .NET 10 / C# 14 library with Railway Oriented Programming; pre-1.0, no compatibility code. ADR 062 (error categories) is merged.
- Provider category: none of the AGENTS.md categories; the four backends must behave consistently.
- Read the plan's Design Choices first: the Dilemmas are pending the maintainer.

IMPLEMENTATION OVERVIEW:
Phases 1a-1b: contract, records, errors. Phase 2a: options, selector, diagnostics, EventIds 7340-7343. Phase 2b: cache.
Phase 3: Kubernetes DNS. Phase 4: HttpClient handler. Phases 5a-5c: Consul. Phases 6a-6c: Eureka.
Phases 7a-7b: Aspire adapter. Phase 8: health check. Phase 9: documentation and ADR.

KEY PATTERNS:
- Either<EncinaError, T> everywhere; every error code has an ErrorCategory; time from TimeProvider; secrets with [JsonIgnore] and ToString().
- Satellite DI AddEncinaServiceDiscovery{Provider} with TryAdd*; one range per package, registered in the pull request that creates it.
- Loops (watch, polling) catch per iteration and never throw out; cancellation ends them; failures are Left elements.
- Each phase is one pull request of at most 15 files and 1500 lines with its own tests and manifest entries.

REFERENCE FILES:
- docs/plans/service-discovery-implementation-plan-607.md (this plan)
- src/Encina.Compliance.Consent/ServiceCollectionExtensions.cs
- src/Encina.Compliance.Consent/Health/ConsentHealthCheck.cs
```

</details>

---

## Cross-Cutting Integration Matrix

| # | Function | Status | Notes |
|---|----------|--------|-------|
| 1 | Caching | ✅ Phase 2b | `CachedServiceDiscoveryProvider` decorator, opt-in (Design Choices 8 and 9) |
| 2 | OpenTelemetry | ✅ Phase 2a | `ActivitySource` and `Meter` `Encina.ServiceDiscovery`; resolution counter and span |
| 3 | Structured Logging | ✅ Phases 2a, 5b, 6b, 7b | EventIds 7340-7349 across four ranges |
| 4 | Health Checks | ✅ Phase 8 | `ServiceDiscoveryHealthCheck` (resolves a configured probe service) |
| 5 | Validation | ✅ Phases 1b, 2a | `ServiceRegistration` validated (name, port range, health check timings); options validated on start |
| 6 | Resilience | ✅ Phases 5b, 6b | Consul and Eureka calls go through a Polly timeout and retry pipeline (Design Choice 10); failure returns `provider_failed` |
| 7 | Distributed Locks | ❌ N/A | Read-only lookups; registration is single-instance |
| 8 | Transactions | ❌ N/A | No transactional state |
| 9 | Idempotency | ❌ N/A | Lookups are idempotent; re-registering the same instance id is an upsert |
| 10 | Multi-Tenancy | ❌ N/A | A service registry is infrastructure shared by all tenants, not tenant-owned data; a tenant-scoped registry has no use case in the issue (the issue's table marked it Defer; the change is recorded on the issue when the plan link is posted) |
| 11 | Module Isolation | ❌ N/A | Discovery is cross-cutting |
| 12 | Audit Trail | ⏭️ Deferred | Registration audit: follow-up issue to be opened (draft: artifacts/decisions/plan-batch/followups/service-discovery-registration-audit.md) |

---

## Prerequisites & Dependencies

### Required Prerequisites

| Prerequisite | Status | Notes |
|-------------|--------|-------|
| `Either`, `EncinaErrors` (with `ErrorCategory`, ADR 062), `TimeProvider` abstractions | Done | In `src/Encina`; ADR 062 merged in PR #3599 |
| Testcontainers fixtures pattern | Done | `tests/Encina.TestInfrastructure/Fixtures/` |

### Recommended (Not Blocking)

| Dependency | Issue | Notes |
|-----------|-------|-------|
| Service discovery and configuration management | #382 | Design Choice 3; configuration management stays there |
| Closed predecessor | #453 | Superseded by #607 |
| Aspire AppHost integration | #416 | Can use the Aspire provider later |

---

## Next Steps

1. Review this plan; the Dilemmas (1, 2, 3, 5, 6, 7, 9, 10, 11, 12, 13 and 14) are asked when work on the issue starts.
2. Post the plan link on #607 and on #382, and record there that multi-tenancy changed from Defer to not applicable.
3. Phases 1a-1b start now; Phases 5a-5c, 6a-6c and 7a-7b are independent of each other once Phase 2a is merged.
4. Each phase is one pull request of at most 15 files and 1500 lines; the last (Phase 9) carries `Fixes #607`.
