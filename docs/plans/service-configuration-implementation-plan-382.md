# Implementation Plan: Configuration management and the service registration lifecycle (the part of #382 that #607 does not cover)

> **Issue**: [#382](https://github.com/dlrivada/Encina/issues/382)
> **Type**: Feature
> **Complexity**: Medium (8 phases, 6 pull requests of 15, 14, 12, 15, 14 and 7 files)
> **Estimated Scope**: ~900 lines of production code + ~2,300 lines of tests
> **Family**: Aspire (aspire-2): #424, #425, #387, #391. Related and merged on `main`: #607 (`Encina.ServiceDiscovery`: `IServiceDiscoveryProvider`, `IServiceRegistry`, selector, cache, health check, Consul, Eureka, Kubernetes DNS, Aspire providers), #416/#417/#440 (Aspire packages). The closed duplicate #453 was carried into this issue (load balancing strategies, `SendToService`). **This plan owns the new `Encina.Configuration` and `Encina.Configuration.Consul` packages, the registration lifecycle folder of `Encina.ServiceDiscovery` and the EventId ranges 865-877.**

---

## Summary

The issue asks for a service-discovery abstraction (`IServiceDiscovery`), provider packages (Consul, Kubernetes, Aspire), a configuration abstraction (`IConfigurationProvider`), client-side load balancing, health check integration and automatic registration on startup. State of `origin/main` (`098caa920`), verified by reading and grep:

| Item of the issue | Today | Owner of what remains |
|-------------------|-------|-----------------------|
| `IServiceDiscovery` (resolve, resolve all, register, deregister, watch) and the `Consul`, `Kubernetes`, `Aspire` provider packages | The #607 plan (on `main`, pending its own Dilemmas) defines `IServiceDiscoveryProvider` and `IServiceRegistry` and four backends; nothing exists in `src/` yet | **#607** (Design Choice 1 of this plan) |
| Client-side load balancing, health check integration of the registry | #607: `IEndpointSelector` (round robin by default) in core, `ServiceDiscoveryHealthCheck` (its Phase 8) | #607; further strategies are an `IEndpointSelector` question for #607 (Design Choice 1) |
| `IConfigurationProvider` (`GetAsync<T>`, `WatchAsync<T>`) | Missing. The name is already taken by `Microsoft.Extensions.Configuration.IConfigurationProvider`, referenced by every Encina host | **This plan** (Design Choices 2 and 3) |
| Automatic service registration on startup | Missing; #607 gives the `IServiceRegistry` contract only | **This plan** (Design Choice 4) |
| `IEncina.SendToService<TRequest, TResponse>` (carried from #453) | Missing; there is no remote invocation protocol in `src/` (the outbound HTTP pieces are `Encina.Refit`, `RestApiRequestHandler`, and the #1233 plan) | Design Choice 5 and an issue file |
| "Unit tests with more than 85 percent coverage" | AGENTS.md section 9: coverage is per flag against per-file targets, not one percentage | This plan's per-file targets |

Differences from the issue text found while reading: (1) `ResolveAsync` returning one endpoint without `Either` breaks the repository's rules (AGENTS.md section 3: operations return `Either<EncinaError, T>`); the configuration interface here uses `Either`. (2) A watch that yields a plain `ConfigurationChange<T>` could not report a backend failure; elements are `Either<EncinaError, ConfigurationChange<T>>`, the same shape #607 uses for endpoint changes. (3) The sketch's `IDictionary<string, string> Metadata` on a `record` is mutable inside an immutable type; #607 owns that model.

**Delivered**: `Encina.Configuration` (`IEncinaConfigurationProvider`, `ConfigurationChange<T>`, `InMemoryConfigurationProvider`, `ConfigurationBackedProvider` over `Microsoft.Extensions.Configuration.IConfiguration` with change tokens, `AddEncinaConfiguration`); `Encina.Configuration.Consul` (key/value provider with blocking queries, health check, `AddEncinaConfigurationConsul`); in `Encina.ServiceDiscovery` a `Registration/` folder: `ServiceRegistrationOptions`, `ServiceRegistrationHostedService`, `AddEncinaServiceRegistration`; EventIds 865-877.

**Not in scope**: the discovery contract and its backends (#607), `SendToService` (Design Choice 5), a Kubernetes API watcher (Design Choice 3), tenant-scoped and module-scoped registries (deferred in the issue), leader election and registry audit (deferred in the issue; #607 records the audit follow-up).

**Affected packages**: `Encina.Configuration` (new), `Encina.Configuration.Consul` (new), `Encina.ServiceDiscovery` (#607, new folder), `Encina` (core: two EventId ranges and their PublicAPI lines).

**Provider category**: none of the AGENTS.md section 5 categories (it is not a database, cache, transport or lock feature); the backends are this feature's own set (in-memory, `IConfiguration`, Consul), and the shared `IEncinaConfigurationProvider` contract suite runs against every backend.

**Scope check (SPEC-000 section 6, DEC-009)**: Consul runs in a local container, `IConfiguration` needs nothing, so both are in 1.0. Cloud configuration services (Azure App Configuration, AWS AppConfig) are not in the issue and would fall under section 6 (no free local emulator) if asked for.

---

## Design Choices

<details>
<summary><strong>1. Relation to #607 (Dilemma D1, the same question as #607's Design Choice 3)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) #607 owns the discovery contract; #382 delivers configuration management, the registration lifecycle and the omissions recorded here** | One contract per concern; #607 is the detailed plan (providers, `Either`, test matrix) | #382's text (`IServiceDiscovery`, a single `ResolveAsync`) is edited when the issue is updated |
| **B) #382's `IServiceDiscovery` is the contract and #607 supplies the providers** | #382 stays as written | Two overlapping interfaces until one is retired; #382's sketch has no `Either` |

### Chosen Option: **A — #607 owns discovery** (recommended, pending the maintainer)

### Rationale

A wins when the later and more detailed issue is the contract (it has providers, results and tests); B wins if #382's single-endpoint shape is preferred. The two answers must agree: this plan and #607's plan are decided together, and under B only this plan's Phase 1 changes (the registry interface is the one the maintainer picks), while the configuration and lifecycle phases are unchanged.

**Class:** Dilemma

</details>

<details>
<summary><strong>2. The name of the configuration abstraction (Dilemma D2)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `IEncinaConfigurationProvider`** | No collision with `Microsoft.Extensions.Configuration.IConfigurationProvider`; the `IEncina*` prefix exists in the code base (`IEncinaHealthCheck`) | Differs from the issue's name |
| **B) `IConfigurationProvider` as written (namespace `Encina.Configuration`)** | The issue's name | The same simple name as the Microsoft interface: a file with both namespaces needs aliases or fully qualified names; analyzers and readers confuse them |

### Chosen Option: **A — `IEncinaConfigurationProvider`** (recommended, pending the maintainer)

### Rationale

A wins in every host that already uses `IConfiguration` (all of them); B wins only if literal compliance with the issue text matters more than the collision. The shapes (`GetAsync<T>`, `WatchAsync<T>`) are unchanged.

**Class:** Dilemma

</details>

<details>
<summary><strong>3. Backends of the configuration provider (Dilemma D3)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) An `IConfiguration`-backed provider in the core package (it serves Aspire environment variables, Kubernetes ConfigMaps mounted as files or injected as environment, files) plus a Consul key/value package** | Every free local backend the issue names is covered; Kubernetes needs no API client; one remote backend (the issue asks for "at least one", Consul recommended) | A watch over `IConfiguration` is as granular as its change tokens (a reload signals the whole tree) |
| **B) A and a Kubernetes API watcher for ConfigMaps** | Per-key change events from the cluster | A Kubernetes client dependency and a cluster to test against, for what a mounted ConfigMap already delivers |
| **C) Consul only** | Smallest | Aspire and Kubernetes (named in the issue's provider table) get nothing for configuration |

### Chosen Option: **A — `IConfiguration` adapter plus Consul** (recommended, pending the maintainer)

### Rationale

A wins when most deployments are Aspire or Kubernetes with mounted configuration and Consul is the only remote store; B wins when a cluster must push single-key changes without a restart; C when Consul is the only target. The adapter compares values before and after a reload so a watcher sees one change per key that really changed, not a whole-tree signal.

**Class:** Dilemma

</details>

<details>
<summary><strong>4. Registration lifecycle: what a failed registration does (Dilemma D4)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Registration is opt-in (`Enabled` false by default); when enabled, a failed registration at start fails the host start (the failure is not swallowed, AGENTS.md section 3); deregistration at stop is best effort within the stop token** | A service that claims to register never runs unregistered without anyone seeing it | A registry outage prevents the service from starting |
| **B) Retry in the background with backoff, report unhealthy until registered** | The service starts during a registry outage | The service runs but is not discoverable; needs a loop and a health signal; more code |

### Chosen Option: **A — fail the start** (recommended, pending the maintainer)

### Rationale

A wins when an unregistered instance is useless or harmful (clients cannot find it) and the orchestrator restarts a failed start; B wins for services that must stay up for their own work during a registry outage. Under both, an invalid address is rejected before any call (`EndpointValidator.ValidateHost`: link-local, metadata and unspecified hosts have no opt-out; loopback only with `AllowLocalEndpoints`), the instance id is generated, never taken from a request, and no instance id is logged. A cancellation of the stop token ends deregistration and is neither a `Left` nor an error.

**Class:** Dilemma

</details>

<details>
<summary><strong>5. `SendToService` and further load balancing strategies (Dilemma D5)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Not delivered here: an issue file for `SendToService` once a remote invocation protocol exists (it needs a defined wire format, endpoint mapping and authentication, a decision the transports and #1233 own); other strategies (`Random`, `LeastConnections`, `Weighted`) are `IEndpointSelector` implementations that #607's contract already allows** | The plan stays on what can be built correctly now | Two acceptance bullets carried from #453 stay open as issues |
| **B) A thin `SendToService` over `Encina.Refit`'s `IRestApiRequest` after resolving an endpoint** | Delivers the bullet | Invents a protocol: a request is sent by HTTP to a peer that has no matching endpoint in Encina today |
| **C) Add the strategies to #607's selector in this plan** | Delivers the bullet | Edits #607's package from a plan about configuration |

### Chosen Option: **A — record and open issues** (recommended, pending the maintainer)

### Rationale

A wins because both bullets belong to other owners (the transports and #1233 for the protocol, #607 for the selector) and building them here would duplicate or pre-empt them; B wins if the maintainer wants a convenience method before the protocol exists; C if one plan should own all strategies. The issue files of this run carry `SendToService` and the extra strategies.

**Class:** Dilemma

</details>

<details>
<summary><strong>6. Results and errors of the configuration contract</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `ValueTask<Either<EncinaError, T>>` for `GetAsync<T>` (a missing key is a `Left` with category `NotFound`); `IAsyncEnumerable<Either<EncinaError, ConfigurationChange<T>>>` for `WatchAsync<T>`; every code is created with `EncinaErrors.Create(code, category: ErrorCategory.X, message: <constant text>)`; cancellation ends the enumeration or propagates `OperationCanceledException`, never a `Left`** | Failure is explicit and categorized; the same shape as #607 | Callers unwrap an `Either` |
| **B) `Option<T>` for a missing key and exceptions for failures** | A missing key is not an error | Exceptions for business flow break AGENTS.md section 3 |

### Chosen Option: **A — `Either` with named categories**

### Rationale

AGENTS.md section 3 (ROP) and ADR-062 fix the shape; the codes are `encina.configuration.not_found` (`ErrorCategory.NotFound`), `encina.configuration.invalid_key` (`ErrorCategory.Validation`), `encina.configuration.invalid_value` (`ErrorCategory.Validation`: the stored text cannot be bound to `T`), `encina.configuration.provider_failed` (`ErrorCategory.UpstreamFailure`), and for the lifecycle `encina.servicediscovery.registration_invalid_address` (`ErrorCategory.Validation`) and `encina.servicediscovery.registration_requires_registry` (`ErrorCategory.FailedPrecondition`). A code has one category (the completeness scan fails otherwise); messages are constant text with no key, value or address in them.

**Class:** Settled
**Settled by:** AGENTS.md section 3 (Railway Oriented Programming, `EncinaError.Message` rule); ADR-001; ADR-062 (error category contract)

</details>

<details>
<summary><strong>7. Keys, values and secrets</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Keys are validated (non-blank, at most 512 characters, no control characters, no `..` segment); values are bound with `System.Text.Json`; a value never appears in a log, tag or `EncinaError.Message`; the Consul options carry a token that is `[JsonIgnore]` with a `ToString()` override; the Consul address goes through `EndpointValidator`** | A configuration value can hold a secret and must not leak; the token cannot be printed | Operators of secrets use the secrets providers of `Encina.Security.Secrets`, not this feature |
| **B) No validation, values logged for diagnostics** | Easier debugging | A value read from a store can be a connection string or a key |

### Chosen Option: **A — validate keys, never log values, hide the token**

### Rationale

AGENTS.md section 3 requires `[JsonIgnore]` and a `ToString()` override for an options class with a token, and forbids `EncinaError.Message` in logs; Consul tokens are credentials, and values may carry secrets.

**Class:** Settled
**Settled by:** AGENTS.md section 3 (options with a token or key; `EncinaError.Message` never reaches logs); `src/Encina/Validation/EndpointPolicy.cs` (link-local, metadata and unspecified hosts have no opt-out)

</details>

---

## Implementation Phases

### Phase 1: Core contract and in-memory provider (pull request 1)

> **Goal**: The `Encina.Configuration` project with the contract, the models, the errors and an in-memory provider.

<details>
<summary>Tasks</summary>

1. `src/Encina.Configuration/Encina.Configuration.csproj` (`PackageId`, XML docs, `Microsoft.Extensions.Configuration.Abstractions` reference, `InternalsVisibleTo` for the unit, guard and contract test projects, PublicAPI analyzer), `PublicAPI.Shipped.txt`, `PublicAPI.Unshipped.txt`, the entry in `Encina.slnx`.
2. `IEncinaConfigurationProvider.cs` (`GetAsync<T>(string key, CancellationToken)` and `WatchAsync<T>(string key, CancellationToken)`; Design Choice 6), `ConfigurationChange.cs` (record `ConfigurationChange<T>(string Key, T? Value, ConfigurationChangeType Type, DateTimeOffset ObservedAtUtc)`), `ConfigurationChangeType.cs` (`Added`, `Updated`, `Removed`).
3. `ConfigurationErrors.cs` (internal static): the four codes of Design Choice 6 with their categories (named `category:` argument, constant messages); `ConfigurationKey.cs` (internal static): `Validate(string?)`.
4. `InMemoryConfigurationProvider.cs`: a dictionary of JSON strings, `SetAsync`/`RemoveAsync` for tests, change streams through `System.Threading.Channels` per watcher; time from `TimeProvider`.
5. Tests: unit (get, missing key, invalid key, invalid value, watch of add, update and remove, cancellation ends the stream and is not an error), guard (null and blank arguments of the public members; a nullable `TimeProvider` accepted), manifest entries and PublicAPI through `mechanical-fixer`; the project references of `Encina.UnitTests`, `Encina.GuardTests` and `Encina.ContractTests`.

</details>

<details>
<summary>Prompt for AI Agents — Phase 1</summary>

```text
CONTEXT:
- Issue #382; plan docs/plans/service-configuration-implementation-plan-382.md. D1 to D5 are recommended, pending the maintainer: follow the Maintainer Decisions section if it exists.
- #607's package Encina.ServiceDiscovery is the sibling for the Either watch shape.

TASK:
Create the Encina.Configuration project with the contract, models, errors, key validation and the in-memory provider, with tests.

KEY RULES:
- EncinaErrors.Create(code, category: ErrorCategory.X, message: <constant>) with a named category; one category per code.
- Cancellation ends a watch or propagates OperationCanceledException; never a Left.
- Time from TimeProvider; UTC timestamps end in AtUtc.
- No key or value in a message, log or tag.

REFERENCE FILES:
- src/Encina/Results/EncinaErrors.cs, docs/architecture/adr/062-error-category-contract.md
- docs/plans/service-discovery-implementation-plan-607.md
- src/Encina.Messaging/Health/IEncinaHealthCheck.cs (naming of the IEncina prefix)
```

</details>

---

### Phase 2: The `IConfiguration` adapter, registration and logs (pull request 2)

> **Goal**: Aspire, Kubernetes ConfigMaps and files serve the contract; `AddEncinaConfiguration` registers it.

<details>
<summary>Tasks</summary>

1. `ConfigurationBackedProvider.cs`: reads `IConfiguration` (a section or a flat key), binds with `System.Text.Json` over the key's text or subtree, and watches with `IConfiguration.GetReloadToken()` (re-registering after each signal); on a signal it compares the previous and new text per watched key and yields one change when it differs.
2. `EncinaConfigurationOptions.cs` (`KeyPrefix` `string?`, validated) and `ServiceCollectionExtensions.AddEncinaConfiguration(this IServiceCollection, Action<EncinaConfigurationOptions>? = null)`: registers the options with validation on start, `TimeProvider` with `TryAdd`, the provider with `TryAddSingleton<IEncinaConfigurationProvider, ConfigurationBackedProvider>` (a satellite provider registered before wins, as the satellite DI rule says).
3. `Diagnostics/ConfigurationLog.cs` (`[LoggerMessage]`): 865 watch ended after a failure (Warning: exception through `ForLogging()`), 866 value could not be bound (Warning: the code only), 867 reload signal handled (Debug: changed key count). `EventIdRanges.ConfigurationCore = (865, 869)` registered first, the `AssemblyRanges` entry of `EncinaEventIdAllocationTests`, the `Encina` PublicAPI lines; ids 865-867 are packed, 868-869 spare.
4. Tests: unit (a reload with one changed key yields one change, a reload with none yields none), guard, a DI test with `ValidateOnBuild` and `ValidateScopes`, a contract suite `IEncinaConfigurationProviderContractTests` that runs against the in-memory and the backed provider; manifests and PublicAPI through `mechanical-fixer`.

</details>

<details>
<summary>Prompt for AI Agents — Phase 2</summary>

```text
CONTEXT:
- Issue #382, Phase 2 (pull request 2). Phase 1 is merged. D3 is recommended, pending the maintainer.

TASK:
Write the IConfiguration-backed provider, its options and registration, the log class and the range, with tests and the contract suite.

KEY RULES:
- Register EventIdRanges.ConfigurationCore first; ids 865-867 packed inside 865-869.
- The provider is a singleton; it holds no scoped service.
- Log codes and counts only; no key, no value, no EncinaError.Message.
- The contract suite executes real providers.

REFERENCE FILES:
- src/Encina/Diagnostics/EventIdRanges.cs
- src/Encina.Compliance.Consent/ServiceCollectionExtensions.cs (satellite registration pattern)
- tests/Encina.UnitTests/Testing/Architecture/EncinaEventIdAllocationTests.cs
```

</details>

---

### Phase 3: Consul package skeleton (pull request 3)

> **Goal**: The `Encina.Configuration.Consul` project, its options and the pinned client.

<details>
<summary>Tasks</summary>

1. `src/Encina.Configuration.Consul/Encina.Configuration.Consul.csproj`, PublicAPI files, the `Encina.slnx` entry, the project references of the test projects. The `Consul` client package is the one #607 pins in `Directory.Packages.props` (#607 pull request 5a); if this pull request lands first it pins the same package and version, and #607 reuses it.
2. `ConsulConfigurationOptions.cs`: `Address` (validated with `EndpointValidator` and `EndpointPolicy.ForHttps(allowInsecureHttp, allowLocalEndpoints)`, a local Consul agent is loopback so `AllowLocalEndpoints` is explicit), `Token` (`[JsonIgnore]`, `ToString()` override printing no value), `Datacenter`, `KeyPrefix`, `WatchWaitTime` (default 5 minutes, the Consul blocking-query wait), `ProviderHealthCheck`.
3. `ConsulConfigurationOptionsValidator.cs` (internal): the endpoint and ranges; a warning once when `AllowLocalEndpoints` is set, as the NATS validator does.
4. Tests: options and validator unit tests, guard tests; the shared `ConsulFixture` and collection of #607 are reused.

</details>

<details>
<summary>Prompt for AI Agents — Phase 3</summary>

```text
CONTEXT:
- Issue #382, Phase 3 (pull request 3). #607 pull request 5a (Consul pin and fixture) is merged or lands first.

TASK:
Create the Consul configuration project skeleton, options and validator with tests.

KEY RULES:
- The token property is [JsonIgnore] and ToString() prints no secret (AGENTS.md section 3).
- The address goes through EndpointValidator; link-local, metadata and unspecified hosts always fail.
- Do not pin a second Consul version.

REFERENCE FILES:
- src/Encina.NATS/EncinaNATSOptions.cs, src/Encina.NATS/EncinaNATSOptionsValidator.cs
- src/Encina/Validation/EndpointPolicy.cs
- docs/plans/service-discovery-implementation-plan-607.md (Phase 5a)
```

</details>

---

### Phase 4: The Consul provider and its health check (pull request 4)

> **Goal**: Key/value reads and watches against Consul, with a health check and logs.

<details>
<summary>Tasks</summary>

1. `ConsulConfigurationProvider.cs`: `GetAsync<T>` reads one key (404 is `not_found`); `WatchAsync<T>` runs blocking queries with the index and yields one `Either` per observed change; a request failure after the resilience pipeline is `provider_failed` (`ErrorCategory.UpstreamFailure`); the pipeline is the one #607 uses for Consul (timeout and retry from `Microsoft.Extensions.Http.Resilience`/Polly, already pinned), so the retry never turns a cancellation into a `Left`.
2. `ConsulConfigurationHealthCheck.cs` (`IEncinaHealthCheck`; `DefaultName` const, `Tags` static array, as the other provider checks) probing the agent leader endpoint; registered when `ProviderHealthCheck.Enabled`.
3. `ServiceCollectionExtensions.AddEncinaConfigurationConsul(this IServiceCollection, Action<ConsulConfigurationOptions>?)`: registers options, validator, the Consul client factory and the provider with `TryAddSingleton` called before `AddEncinaConfiguration` (a database or remote provider wins over the `IConfiguration` default in any order).
4. `Diagnostics/ConsulConfigurationLog.cs`: 870 watch request failed (Warning), 871 request failed after the pipeline (Error), 872 blocking query index reset (Debug). `EventIdRanges.ConfigurationConsul = (870, 873)` registered first; ids 870-872 packed, 873 spare; the allocation test map and the `Encina` PublicAPI lines.
5. Tests: unit (a fake Consul HTTP handler), guard, DI, the contract suite of Phase 2 run against this provider, an integration test on the `ConsulFixture` collection (`[Collection("Consul")]`, `[Trait("Category", "Integration")]`); manifests and PublicAPI through `mechanical-fixer`.

</details>

<details>
<summary>Prompt for AI Agents — Phase 4</summary>

```text
CONTEXT:
- Issue #382, Phase 4 (pull request 4). Phases 1 to 3 are merged.

TASK:
Write the Consul provider, its health check, the registration, the log class and range, with tests.

KEY RULES:
- Cancellation propagates; it is never a Left and never retried.
- Register EventIdRanges.ConfigurationConsul first; ids 870-872 packed.
- Health check: DefaultName const, Tags static array, scoped resolution through IServiceProvider.CreateScope() if a scoped service is needed.
- No key, value or token in any log, tag or message.

REFERENCE FILES:
- src/Encina.NATS (options, validator, health check, registration)
- docs/plans/service-discovery-implementation-plan-607.md (Consul provider phases)
- src/Encina/Diagnostics/EventIdRanges.cs
```

</details>

---

### Phase 5: Registration lifecycle in `Encina.ServiceDiscovery` (pull request 5)

> **Goal**: An opt-in hosted service registers the instance at start and deregisters at stop.

<details>
<summary>Tasks</summary>

1. `src/Encina.ServiceDiscovery/Registration/ServiceRegistrationOptions.cs`: `Enabled` (false), `ServiceName`, `Address`, `Port`, `Metadata` (read-only copy), `HealthCheck` (the `HealthCheckConfig` of #607), `AllowLocalEndpoints` (false); validated on start; `InstanceId` is generated (`{service}-{random}` with a GUID segment), never configured.
2. `ServiceRegistrationHostedService.cs` (internal sealed): `StartAsync` validates the address with `EndpointValidator.ValidateHost`, requires an `IServiceRegistry` (`registration_requires_registry`, `ErrorCategory.FailedPrecondition`), registers through it; a `Left` makes `StartAsync` throw `InvalidOperationException` with the code only (Design Choice 4 option A); `StopAsync` deregisters, logs and never throws on a `Left`; the stop token's cancellation is neither logged as an error nor turned into a `Left`.
3. `ServiceCollectionExtensions.AddEncinaServiceRegistration(this IServiceCollection, Action<ServiceRegistrationOptions>)`: registers the options and `AddHostedService`; registering without a configure action is not allowed (the service name is required), so the parameter is not nullable.
4. `Registration/Diagnostics/ServiceRegistrationLog.cs`: 874 registered (Information: service name), 875 registration failed (Error: error code), 876 deregistered (Information), 877 deregistration failed (Warning: error code); no instance id and no address in any message. `EventIdRanges.ServiceDiscoveryRegistration = (874, 877)` registered first (the assembly `Encina.ServiceDiscovery` then maps to two ranges, the other being #607's 7340-7343), the allocation map, the `Encina` PublicAPI lines.
5. Tests: unit with a fake `IServiceRegistry` (success, a `Left` at start throws, a `Left` at stop does not, invalid address, missing registry, cancellation), guard, a DI test; an integration case on the Consul registry fixture of #607 if that pull request has landed; manifests and PublicAPI through `mechanical-fixer`.

</details>

<details>
<summary>Prompt for AI Agents — Phase 5</summary>

```text
CONTEXT:
- Issue #382, Phase 5 (pull request 5). #607 pull requests 1a and 1b (IServiceDiscoveryProvider, IServiceRegistry, models) are merged. D1 and D4 are recommended, pending the maintainer.

TASK:
Write the registration options, hosted service, extension and logs for Encina.ServiceDiscovery, with tests.

KEY RULES:
- Opt-in, off by default; a failed registration at start fails the start; a failed deregistration does not.
- EndpointValidator.ValidateHost on the address; never log the instance id or the address.
- Cancellation is never a Left. Register EventIdRanges.ServiceDiscoveryRegistration first.
- The hosted service is a singleton: take IServiceRegistry (a singleton in #607) by constructor; if it is scoped, use IServiceScopeFactory.

REFERENCE FILES:
- docs/plans/service-discovery-implementation-plan-607.md (Phases 1b and 5c)
- src/Encina/Validation/EndpointValidator.cs
- src/Encina.Messaging/Outbox/OutboxProcessorBase.cs
```

</details>

---

### Phase 6: Cross-Cutting Integration

> **Goal**: Evaluate and wire the 12 functions.

<details>
<summary>Tasks</summary>

1. Caching: not added (a cache of configuration would hide reloads, Design Choice 3). Resilience: the Consul pipeline (Phase 4). Health checks: `ConsulConfigurationHealthCheck` (Phase 4); the registry health stays #607's. Validation: options, keys, addresses. Logging: Phases 2, 4, 5. Idempotency: a repeated registration of the same generated id is an upsert in #607's registry; configuration reads are idempotent.
2. Fail closed: an invalid key, address or token-less production Consul (a warning only, because Consul without ACLs is legitimate in development) is handled per the options; a missing registry or invalid address stops the start.
3. Tenancy: no tenant-qualified key is created; a registry is shared infrastructure (as #607 records) and configuration keys are global; the reserved `default` tenant segment does not arise. No tenant tag is emitted (ADR-054 D4: tenant tags only where multi-tenancy is on).

</details>

<details>
<summary>Prompt for AI Agents — Phase 6</summary>

```text
CONTEXT:
- Issue #382; Phases 1-5 exist.

TASK:
Check the matrix against the code and fix gaps; add no feature.

KEY RULES:
- Gates fail closed; no value or id in logs.

REFERENCE FILES:
- AGENTS.md section 6
- src/Encina.NATS
```

</details>

---

### Phase 7: Observability and Testing

> **Goal**: Logs and the per-flag targets.

<details>
<summary>Tasks</summary>

1. Observability: structured logs of Phases 2, 4 and 5 (ids 865-867, 870-872, 874-877). No `ActivitySource` or `Meter` for configuration reads (they are in-process or one cheap call, and the issue marks OpenTelemetry as included only for service discovery, which is #607's `Encina.ServiceDiscovery` source); a span `encina.configuration.watch` is deferred with the Consul provider's first measured need and recorded as a follow-up in the PR text.
2. Tests: unit and guard in each pull request; contract suite (Phases 2 and 4); integration on Consul (Phases 4 and 5); property: not applicable (no complex logic); load and benchmark: not applicable, stated in the pull request.
3. **Per-file targets** (new files; each reached by its own pull request):

   | File (pull request) | Unit | Guard | Contract | Property | Integration | Justification |
   |------|-----:|------:|---------:|---------:|------------:|---------------|
   | `IEncinaConfigurationProvider.cs`, `ConfigurationChange.cs` (1) | 0 | 0 | 0 | 0 | 0 | Interface and records with no executable logic; covered through the providers |
   | `ConfigurationKey.cs`, `ConfigurationErrors.cs` (1) | 100 | 0 | 0 | 0 | 0 | Pure functions and constants, every rule and code asserted; internal, no guarded public parameter |
   | `InMemoryConfigurationProvider.cs` (1) | 95 | 90 | 0 | 0 | 0 | Real dictionary and channels; guards for public members |
   | `ConfigurationBackedProvider.cs` (2) | 92 | 85 | 90 | 0 | 0 | Real `ConfigurationBuilder` with in-memory sources and reload; the contract suite runs it |
   | `EncinaConfigurationOptions.cs`, `ServiceCollectionExtensions.cs` (2) | 95 | 90 | 0 | 0 | 0 | Validation and DI test with `ValidateOnBuild` |
   | `ConfigurationLog.cs` (2) | 90 | 0 | 0 | 0 | 0 | Generated methods called by the provider tests |
   | `ConsulConfigurationOptions.cs`, validator (3) | 95 | 90 | 0 | 0 | 0 | Every rule and the token hiding asserted |
   | `ConsulConfigurationProvider.cs` (4) | 90 | 85 | 90 | 0 | 70 | A fake HTTP handler for the branches; the contract suite and a Consul container for the real path |
   | `ConsulConfigurationHealthCheck.cs` (4) | 95 | 85 | 0 | 0 | 0 | Healthy, unhealthy and exception branches |
   | `ServiceRegistrationOptions.cs`, `ServiceRegistrationHostedService.cs` (5) | 92 | 85 | 0 | 0 | 60 | Fake registry for every branch; the integration case needs the Consul registry of #607 |
   | `ServiceRegistrationLog.cs`, `ConsulConfigurationLog.cs` (4, 5) | 90 | 0 | 0 | 0 | 0 | Generated methods called by the provider and hosted service tests |

4. Run `crap-gate` locally (every touched method CRAP <= 10; the watch loops are split into small methods) and `coverage-report.cs --check-justifications`.

</details>

<details>
<summary>Prompt for AI Agents — Phase 7</summary>

```text
CONTEXT:
- Issue #382, Phase 7. Each pull request carries the tests that reach its own targets.

TASK:
Measure per-flag coverage, close gaps, run crap-gate and the justification check.

KEY RULES:
- Tests execute real code; never swallow an exception thrown by code under test.
- Guard tests in tests/Encina.GuardTests; never lower a manifest target.
- Integration tests use a [Collection] fixture and [Trait("Category", "Integration")].

REFERENCE FILES:
- .github/coverage-manifest/Encina.json
- docs/testing/coverage-measurement-methodology.md
```

</details>

---

### Phase 8: Documentation & Finalization (pull request 6)

> **Goal**: Users can configure and register from a README.

<details>
<summary>Tasks</summary>

1. XML docs with `<example>`; README of `Encina.Configuration` and `Encina.Configuration.Consul` and a section in the `Encina.ServiceDiscovery` README (through `docs-writer`); a guide page; `docs/INVENTORY.md` lines; `changelog.d/382-service-configuration.added.md` (through `mechanical-fixer`); load and benchmark justification files.
2. The issue files for `SendToService` and the extra balancing strategies are opened by the orchestrator with this plan.

</details>

<details>
<summary>Prompt for AI Agents — Phase 8</summary>

```text
CONTEXT:
- Issue #382, Phase 8 (pull request 6). Code merged.

TASK:
Write READMEs, the guide, inventory lines and the changelog fragment.

KEY RULES:
- No hand-typed coverage figures; English only; no private application names.

REFERENCE FILES:
- .claude/skills/encina-docs/SKILL.md
- changelog.d/README.md
```

</details>

---

### Proposed split into pull requests (limit 15 files and 1500 lines each)

| PR | Content | Files | Approx. lines |
|----|---------|------:|--------------:|
| 1 | Project file, two PublicAPI files, `Encina.slnx` edit, contract interface, `ConfigurationChange` (with the type enum), `ConfigurationErrors`, `ConfigurationKey`, `InMemoryConfigurationProvider`, manifest, test project references (one csproj edit each for Unit, Guard, Contract), unit and guard test files | 15 | 1,200 |
| 2 | Backed provider, options, extension class, `ConfigurationLog`, `EventIdRanges.cs` edit, `Encina` PublicAPI, `Encina.json`, allocation map edit, Configuration PublicAPI edit and manifest edit, unit, guard, DI and contract-suite test files | 14 | 1,200 |
| 3 | Consul project file, two PublicAPI files, `Encina.slnx` edit, options, validator, manifest, test project references (Unit, Guard, Integration csproj edits), options and validator unit tests, guard test | 13 | 800 |
| 4 | Provider, health check, extension class, `ConsulConfigurationLog`, `EventIdRanges.cs` edit, `Encina` PublicAPI, `Encina.json`, allocation map edit, Consul PublicAPI edit and manifest edit, unit, guard and DI tests, contract-suite edit, integration test | 15 | 1,300 |
| 5 | `ServiceRegistrationOptions`, hosted service, extension class, `ServiceRegistrationLog`, `EventIdRanges.cs` edit, `Encina` PublicAPI, `Encina.json`, allocation map edit, ServiceDiscovery PublicAPI edit and manifest edit, unit, guard and DI tests | 13 | 1,000 |
| 6 | Three READMEs (two new, one section), guide page, inventory lines, changelog fragment, two justification files | 8 | 420 |

PR 1 and PR 2 are independent of #607 (they need only `Encina`); PR 3 needs the `Consul` pin and fixture of #607 pull request 5a (or pins the same version first); PR 4 needs PR 2 and PR 3; PR 5 needs #607 pull requests 1a, 1b and 2a; PR 6 is last. No interface is changed, so no implementer or test double is touched. A pull request that measures over 15 files or 1,500 lines when briefed is split again; PRs 1 and 4 are the closest to the limit.

---

## Research

### Relevant Standards & Specifications

| Reference | What it defines | Use in this feature |
|-----------|-----------------|---------------------|
| `AGENTS.md` sections 3 and 6 | ROP, secrets in options, errors by code, 12 functions | Design Choices 6 and 7 |
| ADR-001 and ADR-062 | Either results and the error category contract | Error codes and categories |
| ADR-021 | EventId ranges registered first | 865-877 |
| ADR-054 (D4) | Tenant tag only where multi-tenancy is on | No tenant data |
| SPEC-000 section 6 and DEC-009 | Cloud services without a free emulator are out; local backends are in | Scope |
| Consul KV HTTP API (blocking queries with `index` and `wait`) | The watch mechanism | Phase 4 |

### Existing Encina Infrastructure to Leverage

| Component | Location | Usage in this feature |
|-----------|----------|-----------------------|
| `IServiceDiscoveryProvider`, `IServiceRegistry`, `ServiceRegistration`, `HealthCheckConfig` | #607 (`Encina.ServiceDiscovery`) | The registration lifecycle |
| `EndpointValidator`, `EndpointPolicy` | `src/Encina/Validation/` | Addresses |
| NATS options, validator, health check, registration | `src/Encina.NATS/` | The provider pattern copied |
| `IEncinaHealthCheck` | `src/Encina.Messaging/Health/IEncinaHealthCheck.cs` | Consul health check |
| `EncinaErrors.Create(code, category, message)` | `src/Encina/Results/EncinaErrors.cs` | Errors |
| Consul client pin, `ConsulFixture` | #607 pull request 5a | The Consul package and tests |
| Secrets providers | `src/Encina.Security.Secrets*` | Where secrets belong (not here) |

### Event ID Allocation

| Package | Range | Notes |
|---------|-------|-------|
| `Encina.Configuration` (`ConfigurationCore`, new) | 865-869, ids 865-867 used | From the block 850-899 (aspire-2) |
| `Encina.Configuration.Consul` (`ConfigurationConsul`, new) | 870-873, ids 870-872 used | Same block |
| `Encina.ServiceDiscovery` (`ServiceDiscoveryRegistration`, new second range) | 874-877, all used | Same block; #607 keeps 7340-7343 for the same assembly. Registered before use in `EventIdRanges.cs` and the allocation test map |

### Estimated File Count

| Category | Files | Notes |
|----------|------:|-------|
| Production code | 17 | Configuration (7 plus provider files), Consul (6), registration folder (4) |
| Public API and manifests | 10 | Three packages' PublicAPI and manifests, `Encina` PublicAPI, `Encina.json` |
| Tests and project files | 16 | Unit, guard, DI, contract, integration, csproj references, allocation map |
| Documentation | 8 | Three READMEs, guide, inventory, fragment, two justification files |
| **Total** | **by the split table: 15 + 14 + 13 + 15 + 13 + 8 = 78 file touches over 6 pull requests** | The split table is the authority |

---

## Combined AI Agent Prompts

<details>
<summary><strong>Full combined prompt for all phases</strong></summary>

```text
PROJECT CONTEXT:
- Encina is a pre-1.0 .NET 10 / C# 14 library; Either<EncinaError, T>; no [Obsolete], no compatibility layers.
- Issue #382: configuration management and the registration lifecycle; discovery itself is #607. Plan: docs/plans/service-configuration-implementation-plan-382.md; D1 to D5 are recommended, pending the maintainer.

IMPLEMENTATION OVERVIEW (6 pull requests, each builds alone):
1. Encina.Configuration: contract, models, errors, in-memory provider (Phase 1).
2. The IConfiguration adapter, registration, logs 865-867 (Phase 2).
3. Consul package skeleton, options, validator (Phase 3).
4. Consul provider, health check, logs 870-872 (Phase 4).
5. Registration lifecycle in Encina.ServiceDiscovery, logs 874-877 (Phase 5).
6. Documentation (Phase 8).

KEY PATTERNS:
- EncinaErrors.Create(code, category: ErrorCategory.X, message: <constant>); one category per code; no key, value or address in messages.
- Cancellation is never a Left. Watches yield Either elements.
- Options with a token: [JsonIgnore] and ToString() override; addresses through EndpointValidator.
- Registration is opt-in; a failed registration at start fails the start.
- EventIds registered first in EventIdRanges.cs; packed.

REFERENCE FILES:
- docs/plans/service-discovery-implementation-plan-607.md
- src/Encina.NATS, src/Encina/Validation/EndpointValidator.cs
- src/Encina/Diagnostics/EventIdRanges.cs
- .github/coverage-manifest/Encina.json
```

</details>

---

## Cross-Cutting Integration Matrix

| # | Function | Status | Notes |
|---|----------|--------|-------|
| 1 | Caching | ❌ | Not applicable: caching configuration would hide reloads; discovery caching is #607's |
| 2 | OpenTelemetry | ⏭️ | Deferred: configuration reads need no span now; discovery tracing is #607's `Encina.ServiceDiscovery` source; a watch span is a follow-up named in the pull request when measured need appears (an issue is opened if the maintainer wants it earlier) |
| 3 | Structured Logging | ✅ | Phases 2, 4, 5: EventIds 865-867, 870-872, 874-877 |
| 4 | Health Checks | ✅ | Phase 4: `ConsulConfigurationHealthCheck`; the registry health is #607's |
| 5 | Validation | ✅ | Keys, options, addresses (Design Choice 7) |
| 6 | Resilience | ✅ | Phase 4: the Consul pipeline (timeout, retry) shared with #607 |
| 7 | Distributed Locks | ❌ | Not applicable: leader election was deferred by the issue itself; registration is per instance |
| 8 | Transactions | ❌ | Not applicable: no transactional state |
| 9 | Idempotency | ✅ | Phase 5: a repeated registration is an upsert; configuration reads and watches are idempotent |
| 10 | Multi-Tenancy | ❌ | Not applicable: global keys and shared registry (deferred by the issue as per-tenant registries); no tenant-qualified key exists, so the reserved `default` segment and injectivity do not arise |
| 11 | Module Isolation | ❌ | Not applicable: per-module endpoints were deferred by the issue |
| 12 | Audit Trail | ⏭️ | Deferred as in the issue: registration audit is already the deferred item of #607's matrix (its follow-up issue file); this plan adds no second one |

---

## Prerequisites & Dependencies

- **#607** pull requests 1a, 1b and 2a (the contracts, the options and the diagnostics folder) before pull request 5; pull request 5a (the `Consul` pin and fixture) before pull request 3. #607's own Dilemmas are pending; pull request 5 follows whichever `IServiceRegistry` shape the maintainer picks (Design Choice 1 of #607).
- No new package outside Microsoft's configuration abstractions and the Consul client.
- Issue files of this run: `SendToService` (Design Choice 5) and the extra load balancing strategies.

---

## Next Steps

1. The maintainer answers D1, D2, D3, D4 and D5 when work on #382 starts, together with #607's Design Choices 1 and 3; the answers go into `## Maintainer Decisions` and the markers are removed.
2. Pull requests 1 and 2 first (independent of #607); pull request 3 after #607 5a; pull request 4; pull request 5 after #607 2a; pull request 6 last.
