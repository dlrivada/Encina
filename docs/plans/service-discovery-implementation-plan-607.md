# Implementation Plan: `Encina.ServiceDiscovery` — `IServiceDiscoveryProvider` (Consul, Kubernetes DNS, Eureka, .NET Aspire)

> **Issue**: [#607](https://github.com/dlrivada/Encina/issues/607)
> **Type**: Feature
> **Complexity**: High (9 phases, 4 backends, 3 new provider packages)
> **Estimated Scope**: ~2,000 lines of production code + ~2,800 lines of tests, in about 10 pull requests

---

## Summary

Define the service-discovery abstraction `IServiceDiscoveryProvider` in a new `Encina.ServiceDiscovery` package and implement it for Consul, Kubernetes DNS, Eureka and .NET Aspire, with a cache, an endpoint selector and an `HttpClient` integration that resolves a service name per request. Nothing of it exists in `src/` today: the only Aspire-related project is `Encina.Aspire.Testing` (a testing helper), and `Directory.Packages.props` has no `Consul`, `Steeltoe` or `Microsoft.Extensions.ServiceDiscovery` entry.

SPEC-000 section 6 excludes cloud services without a free local emulator; all four backends run locally (Consul and Eureka in Docker, Kubernetes DNS behind a resolver seam, Aspire as a library), so all four are in 1.0 scope.

**Packages**: `Encina.ServiceDiscovery` (contract, cache, selector, Kubernetes DNS provider, `HttpClient` integration), `Encina.ServiceDiscovery.Consul`, `Encina.ServiceDiscovery.Eureka`, `Encina.ServiceDiscovery.Aspire` (each with its own dependency, so applications pay only for the backend they use).

**Provider category**: none of the AGENTS.md section 5 categories; the backends below are this feature's own provider set, and every feature must be consistent across them.

| Backend | Resolve | Watch | Register and health | Package |
|---|:-:|:-:|:-:|---|
| Consul | ✅ | ✅ (blocking query) | ✅ | `.Consul` |
| Eureka | ✅ | ✅ (polling the registry delta) | ✅ | `.Eureka` |
| Kubernetes DNS | ✅ | ✅ (polling with TTL) | not applicable | core |
| .NET Aspire | ✅ | ✅ (change token of the library) | not applicable | `.Aspire` |

The register and health column assumes Design Choice 1 option C (pending the maintainer).

**EventIds**: this plan takes 7340-7349 from the block 7300-7349 assigned to the feature-flag/service-discovery family (feature flags use 7300-7339).

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
- Under A or B only the interface split of Phase 1 changes; the backends are unchanged.

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
- Under B, Phases 3 (own `HttpClient` handler) and 6 (adapter) shrink to using the library's `AddServiceDiscovery`.

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
<summary><strong>4. Contract shape — Either results, <code>AtUtc</code> timestamps, one resolve method</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `Task<Either<EncinaError, T>>`; `ResolveAsync` returns all healthy endpoints and the selector picks one; `InstanceHealth.LastCheckAtUtc`** | Failure is explicit; the cache and selector are provider-independent | The caller or the `HttpClient` handler selects |
| **B) Bare `Task` results and a `ResolveAsync` returning one endpoint** | Simpler call site | Hides failure; moves load balancing into every provider |

### Chosen Option: **A — Either, all endpoints, selector outside the provider**

### Rationale

- AGENTS.md section 3 (Railway Oriented Programming) and section 4 (`AtUtc` suffix); the issue already rewrote its sketch this way.
- Time comes from `TimeProvider` (cache expiry, watch polling).

**Class:** Settled
**Settled by:** AGENTS.md section 3 (ROP, `TimeProvider`) and section 4 (naming); ADR-001; issue #607 body (interface definition)

</details>

<details>
<summary><strong>5. Package structure — core plus one package per backend with a dependency</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Core (contract, cache, selector, Kubernetes DNS, `HttpClient`) plus `.Consul`, `.Eureka`, `.Aspire`** | Kubernetes DNS needs no dependency beyond the BCL; each SDK is isolated | Four packages |
| **B) Everything in one package** | One NuGet | Every user pulls Consul, Steeltoe and the Microsoft library |

### Chosen Option: **A — Core plus one package per dependency**

### Rationale

- Pay-for-what-you-use (AGENTS.md section 1) and the precedent of `Encina.Caching.Redis` and the secrets providers (one package per backend, each with its own EventId range, `EventIdRanges.cs`).
- The issue's "Affected Packages" lists `Encina.ServiceDiscovery`, `.Consul` and `.Eureka`; the Aspire adapter is its own package because it adds `Microsoft.Extensions.ServiceDiscovery`.

**Class:** Settled
**Settled by:** AGENTS.md section 1 (pay-for-what-you-use); `src/Encina/Diagnostics/EventIdRanges.cs` (one range per backend package); issue #607 body (affected packages)

</details>

<details>
<summary><strong>6. Cache and endpoint selection — decorator with Watch-driven invalidation</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `CachedServiceDiscoveryProvider` decorator (in-process, TTL from `TimeProvider`, invalidated by the provider's watch) and `IEndpointSelector` (round robin default)** | One cache and selector for all backends; testable with `FakeTimeProvider` | Per-instance cache: staleness up to the TTL |
| **B) Cache inside each provider** | Backend-specific tuning | Four implementations of the same logic |

### Chosen Option: **A — Decorator and selector in core**

### Rationale

- Provider coherence: behavior shared across backends lives once in core; backends differ only in how they resolve (AGENTS.md section 3).
- The cache key includes the tenant when one exists (function 10).

**Class:** Trivial
**Settled by:** AGENTS.md section 3 (provider coherence); implementation-plan prompt rule 3 (cross-cutting caching in the core, not per provider)

</details>

<details>
<summary><strong>7. Eureka client — the Steeltoe package named in the issue</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `Steeltoe.Discovery.Eureka` as the issue says** | Maintained client; registration, heartbeats, delta fetch | A large dependency; its .NET 10 support is confirmed at the start of Phase 5 |
| **B) A thin `HttpClient` client against the Eureka REST API** | No dependency | Heartbeat, renewal and delta logic to own |

### Chosen Option: **A — Steeltoe client**

### Rationale

- The issue names the package; the pinned version is confirmed to support net10.0 before Phase 5 starts. If it does not, option B becomes a Dilemma for the maintainer at that point.
- Secrets in options (none for Eureka by default; basic-auth credentials, if configured, are `[JsonIgnore]` with a `ToString()` override).

**Class:** Trivial
**Settled by:** Issue #607 body (supported providers table: `Steeltoe.Discovery.Eureka`); AGENTS.md section 3 (secrets in options)

</details>

---

## Implementation Phases

### Phase 1: Core Contract, Records, Errors and EventId Ranges

> **Goal**: The package and the contract everything else uses.

<details>
<summary><strong>Tasks</strong></summary>

1. `src/Encina.ServiceDiscovery/Encina.ServiceDiscovery.csproj` (net10.0; `Encina`, `Microsoft.Extensions.Options`, `Microsoft.Extensions.Logging.Abstractions`).
2. `Abstractions/IServiceDiscoveryProvider.cs` (`ResolveAsync`, `WatchAsync`), `Abstractions/IServiceRegistry.cs` (`RegisterAsync`, `DeregisterAsync`, `GetHealthAsync`) per Design Choice 1.
3. `Model/ServiceEndpoint.cs`, `ServiceRegistration.cs`, `HealthCheckConfig.cs`, `ServiceHealth.cs`, `InstanceHealth.cs` (`LastCheckAtUtc`), `ServiceEndpointChange.cs`, `ServiceEndpointChangeType.cs`.
4. `ServiceDiscoveryErrors.cs` (`encina.servicediscovery.not_found`, `provider_failed`, `invalid_registration`), `ServiceDiscoveryOptions.cs`, `AddEncinaServiceDiscovery`.
5. `EventIdRanges.cs`: `ServiceDiscovery = (7340, 7343)`, `ServiceDiscoveryConsul = (7344, 7345)`, `ServiceDiscoveryEureka = (7346, 7347)`, `ServiceDiscoveryAspire = (7348, 7349)`; `AssemblyRanges` entries in `EncinaEventIdAllocationTests.cs`.
6. `PublicAPI.Unshipped.txt`, `.github/coverage-manifest/Encina.ServiceDiscovery.json`, project in `Encina.slnx`.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 1</strong></summary>

```text
CONTEXT:
- Issue #607, Phase 1. Encina is .NET 10 / C# 14 with Railway Oriented Programming; pre-1.0.
TASK:
- Create the Encina.ServiceDiscovery project with the contract, records, errors, options, registration and the four EventId ranges.
KEY RULES:
- Either<EncinaError, T> on every method; UTC timestamps end in AtUtc.
- Register the ranges first (ADR-021) and update the AssemblyRanges map; run the architecture tests.
- XML docs on all public members; PublicAPI.Unshipped.txt; guard and unit tests in the same pull request.
REFERENCE FILES:
- src/Encina.Compliance.Consent/ServiceCollectionExtensions.cs
- src/Encina/Diagnostics/EventIdRanges.cs
- src/Encina/Results/EncinaErrors.cs
```

</details>

---

### Phase 2: Cache, Selector and Kubernetes DNS Provider

> **Goal**: Shared behavior in core and the dependency-free backend.

<details>
<summary><strong>Tasks</strong></summary>

1. `Caching/CachedServiceDiscoveryProvider.cs` (decorator; `IMemoryCache`; TTL from `ServiceDiscoveryOptions.CacheDuration`; invalidated by `WatchAsync` changes; a `Left` is never cached).
2. `Selection/IEndpointSelector.cs`, `RoundRobinEndpointSelector.cs`.
3. `Kubernetes/IDnsResolver.cs` (seam over `Dns.GetHostAddressesAsync`), `KubernetesDnsServiceDiscoveryProvider.cs` (name `service.namespace.svc.<domain>`; port from `KubernetesDnsOptions.Services[name]`, because plain A records carry no port; SRV lookup is a documented non-goal), `KubernetesDnsOptions.cs`, `AddEncinaServiceDiscoveryKubernetes`.
4. Unit tests with `FakeTimeProvider` and a fake `IDnsResolver`.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 2</strong></summary>

```text
CONTEXT:
- Issue #607, Phase 2. Phase 1 is merged.
TASK:
- Implement the cache decorator, the round-robin selector and the Kubernetes DNS provider with its resolver seam.
KEY RULES:
- Time only through TimeProvider; the tenant is part of the cache key.
- Watch for Kubernetes is polling with a TTL; a polling loop catches per iteration and never lets an exception escape (AGENTS.md section 3).
- Registration uses TryAdd* and never overrides the application's own.
REFERENCE FILES:
- src/Encina.ServiceDiscovery/Abstractions/IServiceDiscoveryProvider.cs
- src/Encina.Caching.Memory/ (decorator and registration pattern)
```

</details>

---

### Phase 3: `HttpClient` Integration

> **Goal**: Resolve a service name per request.

<details>
<summary><strong>Tasks</strong></summary>

1. `Http/ServiceDiscoveryHttpMessageHandler.cs`: a `DelegatingHandler` that rewrites `http(s)://{serviceName}/...` to the selected endpoint; a `Left` from resolution becomes an `HttpRequestException` carrying the error code (the one place an exception is required by the `HttpClient` contract).
2. `Http/ServiceDiscoveryHttpClientBuilderExtensions.cs`: `AddEncinaServiceDiscovery(this IHttpClientBuilder)`.
3. Tests with a stub inner handler; no network.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 3</strong></summary>

```text
CONTEXT:
- Issue #607, Phase 3. Applies only if Design Choice 2 is decided for option A (own abstraction); under B this phase uses the Microsoft library's AddServiceDiscovery instead.
TASK:
- Implement the delegating handler and the IHttpClientBuilder extension.
KEY RULES:
- Request URIs without a registered service name pass through untouched.
- Never log the full URI query or headers; log the service name and error code.
REFERENCE FILES:
- src/Encina.ServiceDiscovery/Selection/RoundRobinEndpointSelector.cs
- src/Encina.Compliance.Consent/ServiceCollectionExtensions.cs
```

</details>

---

### Phase 4: Consul Provider and Registry

> **Goal**: `Encina.ServiceDiscovery.Consul`.

<details>
<summary><strong>Tasks</strong></summary>

1. `Directory.Packages.props`: pin `Consul`; project `src/Encina.ServiceDiscovery.Consul/`.
2. `ConsulServiceDiscoveryProvider.cs` (healthy instances only; `WatchAsync` by blocking query with the index; a failure ends the iteration with a `Left`-backed error event, not an exception), `ConsulServiceRegistry.cs` (registration with the check from `HealthCheckConfig`, instance health), `ConsulServiceDiscoveryOptions.cs` (ACL token `[JsonIgnore]`, `ToString()` override), `AddEncinaServiceDiscoveryConsul`, log messages 7344-7345.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 4</strong></summary>

```text
CONTEXT:
- Issue #607, Phase 4. Consul is tested in Docker with a Testcontainers fixture.
TASK:
- Create the Consul package with provider, registry, options, registration and log messages.
KEY RULES:
- The ACL token is a secret: [JsonIgnore] and a ToString() override.
- A Consul error becomes Left provider_failed; never an empty list reported as success.
- EventIds only inside 7344-7345.
REFERENCE FILES:
- src/Encina.ServiceDiscovery/Abstractions/IServiceDiscoveryProvider.cs
- tests/Encina.TestInfrastructure/Fixtures/RedisFixture.cs
```

</details>

---

### Phase 5: Eureka Provider and Registry

> **Goal**: `Encina.ServiceDiscovery.Eureka`.

<details>
<summary><strong>Tasks</strong></summary>

1. Confirm the pinned `Steeltoe.Discovery.Eureka` supports net10.0 (Design Choice 7); pin it in `Directory.Packages.props`; project `src/Encina.ServiceDiscovery.Eureka/`.
2. `EurekaServiceDiscoveryProvider.cs`, `EurekaServiceRegistry.cs` (instance id, heartbeat owned by the client, instance health), `EurekaServiceDiscoveryOptions.cs`, registration, log messages 7346-7347.
3. Eureka container fixture; the image tag is the vendor's published tag, defined in one place.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 5</strong></summary>

```text
CONTEXT:
- Issue #607, Phase 5.
TASK:
- Create the Eureka package with provider, registry, options, registration, log messages and the container fixture.
KEY RULES:
- Credentials, if any, are [JsonIgnore] with a ToString() override.
- Registry delta polling catches per iteration and never throws out of the loop.
REFERENCE FILES:
- src/Encina.ServiceDiscovery.Consul/ (sibling package)
- tests/Encina.TestInfrastructure/Fixtures/RabbitMqFixture.cs
```

</details>

---

### Phase 6: .NET Aspire Adapter

> **Goal**: `Encina.ServiceDiscovery.Aspire` over `Microsoft.Extensions.ServiceDiscovery`.

<details>
<summary><strong>Tasks</strong></summary>

1. Pin `Microsoft.Extensions.ServiceDiscovery`; project `src/Encina.ServiceDiscovery.Aspire/`.
2. `AspireServiceDiscoveryProvider.cs` (resolve through the library's resolver; watch through its change token; the library exposes no per-instance health, so this package implements no `IServiceRegistry`), `AddEncinaServiceDiscoveryAspire`, log messages 7348-7349.
3. Tests with the library's configuration-based resolver (`services__name__http__0` style configuration); no AppHost required.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 6</strong></summary>

```text
CONTEXT:
- Issue #607, Phase 6. Existing Encina.Aspire.Testing is a testing helper and is not changed.
TASK:
- Create the Aspire adapter package.
KEY RULES:
- Resolve and watch only; this package does not implement IServiceRegistry (Design Choice 1).
- Either results; library exceptions become provider_failed with the exception type only.
REFERENCE FILES:
- src/Encina.Aspire.Testing/EncinaTestContext.cs
- src/Encina.ServiceDiscovery/Abstractions/IServiceDiscoveryProvider.cs
```

</details>

---

### Phase 7: Observability and Health Check

> **Goal**: Diagnostics for the core and the health check.

<details>
<summary><strong>Tasks</strong></summary>

1. `Diagnostics/ServiceDiscoveryDiagnostics.cs`: `ActivitySource` and `Meter` `Encina.ServiceDiscovery`; counter `encina.servicediscovery.resolutions` (tags `provider`, `outcome`), span `encina.servicediscovery.resolve` (tags `service.name`, `provider`).
2. `Diagnostics/ServiceDiscoveryLogMessages.cs`: `[LoggerMessage]` EventIds 7340-7343 (resolve failed, cache hit, watch failed, no healthy endpoint).
3. `Health/ServiceDiscoveryHealthCheck.cs` (`DefaultName` const, static `Tags` array, resolves the provider through `IServiceProvider.CreateScope()`).
4. No endpoint addresses or metadata values in tags or logs; the tenant id is a metric tag.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 7</strong></summary>

```text
CONTEXT:
- Issue #607, Phase 7. The ranges are registered in Phase 1.
TASK:
- Add the diagnostics, the log messages and the health check.
KEY RULES:
- EventIds sequential inside 7340-7343; [LoggerMessage] source generator.
- Never log EncinaError.Message; log the error code.
REFERENCE FILES:
- src/Encina.Compliance.Consent/ConsentHealthCheck.cs
- src/Encina.Compliance.Consent/ConsentDiagnostics.cs
```

</details>

---

### Phase 8: Testing

> **Goal**: Every applicable test type for the four backends.

<details>
<summary><strong>Tasks</strong></summary>

1. Unit: cache, selector, Kubernetes provider (fake resolver), handler, each provider's mapping; DI tests with `ValidateOnBuild` and `ValidateScopes` for every `AddEncinaServiceDiscovery*`.
2. Guard: every public method with parameters.
3. Contract: one suite over `IServiceDiscoveryProvider` run against every backend, one over `IServiceRegistry` for Consul and Eureka.
4. Integration (`[Collection]` fixtures): Consul and Eureka containers; Kubernetes DNS against a CoreDNS container serving a zone file, or a `.md` justification if the container is not practical; Aspire needs no container.
5. Property: not required (lookups are simple); load: justification `.md`; benchmark (optional): resolve through the cache.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 8</strong></summary>

```text
CONTEXT:
- Issue #607, Phase 8; targets in .github/coverage-manifest/Encina.ServiceDiscovery*.json.
TASK:
- Write the tests listed in Phase 8 Tasks and reach every flag's per-file target.
KEY RULES:
- Tests execute real package code; shared [Collection] fixtures; ClearAllDataAsync-style reset in InitializeAsync.
- CRAP <= 10 on changed methods.
REFERENCE FILES:
- docs/testing/integration-tests.md
- docs/en/guides/TESTING.md
```

</details>

---

### Phase 9: Documentation and Finalization

> **Goal**: Document the feature and the decisions.

<details>
<summary><strong>Tasks</strong></summary>

1. ADR for the interface scope and the relation to the Microsoft library (Design Choices 1-3); ADR number assigned when the ADR pull request opens.
2. Package READMEs (four), a guide under `docs/features/`, `changelog.d/607-service-discovery.added.md`, `docs/INVENTORY.md`, ROADMAP entry.
3. `dotnet build --configuration Release` with 0 warnings; tests pass; per-flag coverage at target.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 9</strong></summary>

```text
CONTEXT:
- Issue #607, Phase 9; documentation goes through docs-writer.
TASK:
- Write the ADR, the READMEs, the guide, the fragment and the INVENTORY/ROADMAP updates.
KEY RULES:
- Never edit CHANGELOG.md; coverage figures only through covref markers.
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
| AGENTS.md section 3 | ROP, `TimeProvider`, secrets in options, loops never throw | Design Choices 4 and 7; Phases 2, 4, 5 |
| AGENTS.md section 5 | Provider coherence across a feature's backends | Contract suite over all backends (Phase 8) |
| Consul HTTP API, Eureka REST protocol, Kubernetes DNS specification | Health-aware catalog, registry, service names | Provider behavior |
| `Microsoft.Extensions.ServiceDiscovery` | Resolver and change tokens | Aspire adapter |

### Existing Encina Infrastructure to Leverage

| Component | Location | Usage in this feature |
|-----------|----------|----------------------|
| `EncinaErrors.Create`, `Either` | `src/Encina/Results/EncinaErrors.cs:25` | Errors |
| `EventIdRanges` | `src/Encina/Diagnostics/EventIdRanges.cs` | Ranges 7340-7349 |
| Satellite registration pattern | `src/Encina.Compliance.Consent/ServiceCollectionExtensions.cs` | `AddEncinaServiceDiscovery*` |
| Health check pattern | `src/Encina.Compliance.Consent/ConsentHealthCheck.cs` | `ServiceDiscoveryHealthCheck` |
| Testcontainers fixtures | `tests/Encina.TestInfrastructure/Fixtures/` | Consul, Eureka, CoreDNS fixtures |
| `Encina.Aspire.Testing` | `src/Encina.Aspire.Testing/` | Not changed; a testing helper only |
| Related issues | #382 (open, service discovery and configuration management), #453 (closed) | Design Choice 3 |

### Event ID Allocation

| Package | Range | Notes |
|---------|-------|-------|
| **`Encina.ServiceDiscovery`** | **7340-7343** | New |
| **`Encina.ServiceDiscovery.Consul`** | **7344-7345** | New |
| **`Encina.ServiceDiscovery.Eureka`** | **7346-7347** | New |
| **`Encina.ServiceDiscovery.Aspire`** | **7348-7349** | New |
| `Encina.FeatureFlags*` | 7300-7339 | The feature-flag plans (#604, #300, #345, #451) |

### Estimated File Count

| Category | Files | Notes |
|----------|-------|-------|
| Core (Phases 1-3, 7) | ~34 | Contract, records, cache, selector, Kubernetes DNS, handler, diagnostics, health |
| Consul, Eureka, Aspire (Phases 4-6) | ~30 | About 10 each |
| Tests (Phase 8) | ~40 | Unit, guard, contract, integration for Consul and Eureka |
| Documentation (Phase 9) | ~8 | ADR, 4 READMEs, guide, fragment, INVENTORY |
| **Total** | **~112** | In about 10 pull requests of at most 15 files and 1500 lines |

---

## Combined AI Agent Prompts

<details>
<summary><strong>Full combined prompt for all phases</strong></summary>

```text
You are implementing issue #607: Encina.ServiceDiscovery (Consul, Kubernetes DNS, Eureka, .NET Aspire).

PROJECT CONTEXT:
- Encina is a .NET 10 / C# 14 library with Railway Oriented Programming; pre-1.0, no compatibility code.
- Provider category: none of the AGENTS.md categories; the four backends must behave consistently.
- Read the plan's Design Choices first: Dilemmas 1-3 are pending the maintainer.

IMPLEMENTATION OVERVIEW:
Phase 1: contract, records, errors, EventId ranges 7340-7349. Phase 2: cache, selector, Kubernetes DNS.
Phase 3: HttpClient handler. Phase 4: Consul. Phase 5: Eureka. Phase 6: Aspire adapter.
Phase 7: diagnostics and health check. Phase 8: tests. Phase 9: documentation and ADR.

KEY PATTERNS:
- Either<EncinaError, T> everywhere; time from TimeProvider; secrets with [JsonIgnore] and ToString().
- Satellite DI AddEncinaServiceDiscovery{Provider} with TryAdd*; one range per package.
- Loops (watch, polling) catch per iteration and never throw out; cancellation ends them.
- Each phase is one pull request of at most 15 files and 1500 lines.

REFERENCE FILES:
- docs/plans/service-discovery-implementation-plan-607.md (this plan)
- src/Encina.Compliance.Consent/ServiceCollectionExtensions.cs
- src/Encina.Compliance.Consent/ConsentHealthCheck.cs
```

</details>

---

## Cross-Cutting Integration Matrix

| # | Function | Status | Notes |
|---|----------|--------|-------|
| 1 | Caching | ✅ Phase 2 | `CachedServiceDiscoveryProvider` decorator (Design Choice 6) |
| 2 | OpenTelemetry | ✅ Phase 7 | `ActivitySource` and `Meter` `Encina.ServiceDiscovery`; resolution counter and span |
| 3 | Structured Logging | ✅ Phases 4-7 | EventIds 7340-7349 across four ranges |
| 4 | Health Checks | ✅ Phase 7 | `ServiceDiscoveryHealthCheck` (resolves a configured probe service) |
| 5 | Validation | ✅ Phase 1 | `ServiceRegistration` validated (name, port range, health check timings); options validated on start |
| 6 | Resilience | ✅ Phases 4-5 | Consul and Eureka calls go through Polly retry and timeout; failure returns `provider_failed` |
| 7 | Distributed Locks | ❌ N/A | Read-only lookups; registration is single-instance |
| 8 | Transactions | ❌ N/A | No transactional state |
| 9 | Idempotency | ❌ N/A | Lookups are idempotent; re-registering the same instance id is an upsert |
| 10 | Multi-Tenancy | ⏭️ Deferred | Tenant-scoped registry: issue file `service-discovery-tenant-scope` in `artifacts/issues/`; the tenant id is already part of the cache key and a metric tag |
| 11 | Module Isolation | ❌ N/A | Discovery is cross-cutting |
| 12 | Audit Trail | ⏭️ Deferred | Registration audit: issue file `service-discovery-registration-audit` in `artifacts/issues/` |

---

## Prerequisites & Dependencies

### Required Prerequisites

| Prerequisite | Status | Notes |
|-------------|--------|-------|
| `Either`, `EncinaErrors`, `TimeProvider` abstractions | Done | In `src/Encina` |
| Testcontainers fixtures pattern | Done | `tests/Encina.TestInfrastructure/Fixtures/` |

### Recommended (Not Blocking)

| Dependency | Issue | Notes |
|-----------|-------|-------|
| Service discovery and configuration management | #382 | Design Choice 3; configuration management stays there |
| Closed predecessor | #453 | Superseded by #607 |
| Aspire AppHost integration | #416 | Can use the Aspire provider later |

---

## Next Steps

1. Review this plan; the Dilemmas (1, 2 and 3) are asked when work on the issue starts.
2. Post the plan link on #607 and on #382.
3. Phase 1 can start now; Phases 4-6 are independent of each other.
4. Each phase is one pull request of at most 15 files and 1500 lines; the last carries `Fixes #607`.
