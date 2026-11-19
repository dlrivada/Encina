# Implementation Plan: `AddEncinaExternalService` — services from other repositories in one Aspire AppHost

> **Issue**: [#424](https://github.com/dlrivada/Encina/issues/424)
> **Type**: Feature
> **Complexity**: Medium (8 phases, 5 pull requests of 13, 9, 11, 9 and 6 files)
> **Estimated Scope**: ~600 lines of production code + ~1,500 lines of tests
> **Family**: Aspire (aspire-2): #424 external services, #425 hot reload, #382 service discovery and configuration, #387 Dapr, #391 sidecar. The aspire-1 plans own the shared pieces this plan reuses: #416 (the hosting package, `EncinaResource`, `WithEncina`, the `Encina__Aspire__*` environment contract), #417 (`Encina.Aspire.ServiceDefaults`, the reader of that contract), #440 (naming rules, parity tests, quickstart) and #607 (`Encina.ServiceDiscovery`). **This plan owns the `ExternalServices` folder of the hosting package and the `Encina__Aspire__ExternalServices__*` keys.**

---

## Summary

The issue asks for `AddEncinaExternalService`, so that a service that lives in another repository can be part of the AppHost model with service discovery, a shared message broker, a development stub, dashboard visibility and a contract. State of `origin/main` (`098caa920`; #416, #417, #419, #421 and #440 are merged), verified by reading and grep, and of `Aspire.Hosting` 13.6.5 (the version pinned by `Directory.Packages.props`, #3491; the issue cites Aspire 9.x), read in its `Aspire.Hosting.xml`:

| Item of the issue | Today | Owner of what remains |
|-------------------|-------|-----------------------|
| `AddEncinaExternalService()` | Missing. Aspire 13.6.5 already ships `AddExternalService(name, string)`, `(name, Uri)` and `(name, IResourceBuilder<ParameterResource>)`, `WithHttpHealthCheck(path, statusCode)` for an `ExternalServiceResource`, and `WithReference(externalService)` (service-discovery variables with the resource name as the service name) | **This plan** wraps them and adds the Encina declaration |
| Endpoint configuration (static, environment-based, dynamic) | Aspire: a literal URL, a `Uri` or a parameter (a parameter is how the endpoint changes per environment) | Aspire's; nothing to build |
| Health check integration | Aspire: `WithHttpHealthCheck` | **This plan** exposes `HealthCheckPath` and calls it |
| "Custom resource type for dashboard visibility" and "dashboard view of external dependencies" | `ExternalServiceResource` is already a dashboard resource of its own kind | Aspire's; this plan adds the relationship to the Encina resource only |
| Service discovery (Kubernetes, Consul, DNS) | Application side: #607 (core Kubernetes DNS provider, `Encina.ServiceDiscovery.Consul`); Aspire's own discovery ships with the template | Design Choice 2 |
| Shared message broker, topic routing | `Encina.Messaging` has no environment binding (`MessagingConfiguration` is set in code); an AppHost cannot reference application types (#416) | Design Choice 3 (a declaration by resource name) |
| Schema registry for message contracts | Nothing in `src/` (a grep for `SchemaRegistry` finds only the module schema registry of the database isolation feature) | Design Choice 7 and an issue file |
| Correlation ID propagation across repos | ADR-054 decides propagation: W3C trace context, no baggage (D4); no Encina code propagates a custom correlation header | Design Choice 7 (nothing to build) |
| Stub or mock of an external service | Nothing; `Encina.Testing.WireMock` exists for tests, not for the AppHost | Design Choice 4 |
| Contract-first (`ImplementsContract<T>()`) | Nothing; the AppHost does not load application types | Design Choice 5 |

Differences from the issue text found while reading the code and the Aspire package: (1) the sketch `config.UseMessageBroker(connectionString)` would put a connection string through Encina; Aspire already carries connection strings (`WithReference(resource)`), so this plan declares the broker by resource name and never writes a connection string. (2) `builder.Environment.IsDevelopment()` is available (`IDistributedApplicationBuilder.Environment`), but the run or publish distinction that matters here is `builder.ExecutionContext.IsRunMode`; the stub exists in run mode only. (3) `UseKubernetesServiceDiscovery(...)`, `UseConsulServiceDiscovery(...)` and `UseDnsServiceDiscovery(...)` sketched as three methods become one option, `Discovery` with a name (Design Choice 2).

**Delivered**: in `Encina.Aspire.Hosting` (#416) the folder `ExternalServices/`: `EncinaExternalServiceOptions` (`HealthCheckPath`, `Discovery`, `DiscoveryName`, `SharesMessageBroker`, `OutboxTopic`, `ImplementsContract`, `UseStub`), `EncinaExternalServiceAnnotation`, `AddEncinaExternalService(this IDistributedApplicationBuilder, string name, string url | IResourceBuilder<ParameterResource> urlParameter, Action<EncinaExternalServiceOptions>? configure = null)` returning `IResourceBuilder<ExternalServiceResource>`, and `WithEncinaExternalService<T>(this IResourceBuilder<T> consumer, IResourceBuilder<ExternalServiceResource> external)` for `T : IResourceWithEnvironment`; the stub container of run mode; in `Encina.Aspire.ServiceDefaults` (#417) a reader of `Encina:Aspire:ExternalServices:*`, a read-only catalog of the declared services and a start check that fails closed; log messages 850-853.

**Not in scope**: schema registry and custom correlation headers (Design Choice 7), the discovery providers themselves (#607), message transports, the Azure and Kubernetes publishers (#421).

**Affected packages**: `Encina.Aspire.Hosting` (#416, new folder), `Encina.Aspire.ServiceDefaults` (#417, new folder), `Encina` (core: one EventId range and its PublicAPI line).

**Provider category**: none. The Cloud and Transport categories of AGENTS.md section 5 are not touched: the plan models endpoints and declares a broker by name; it implements no transport, store or lock, so the provider matrices do not apply.

**Scope check (SPEC-000 section 6, DEC-009)**: everything here runs locally (the AppHost model needs no container runtime; the stub uses a container image, the same as any Aspire container resource), so the item is in 1.0 scope. The Kubernetes and Consul declarations name backends that #607 tests against local services; nothing here needs a cloud service.

---

## Design Choices

<details>
<summary><strong>1. The resource: wrap Aspire's external service or define an own resource type (Dilemma D1)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `AddEncinaExternalService` calls Aspire's `AddExternalService`, returns its `IResourceBuilder<ExternalServiceResource>` and stores the Encina declaration as an `EncinaExternalServiceAnnotation`** | Aspire's health check, dashboard kind, `WithReference` and publishers work unchanged; no new resource kind to maintain; the user can chain every Aspire extension | The Encina declaration lives in an annotation, so the dashboard shows nothing Encina-specific on the row |
| **B) `EncinaExternalServiceResource : Resource, IResourceWithServiceDiscovery` (an own type)** | Can publish its own dashboard state and properties (broker, topic, contract) | Aspire's `WithHttpHealthCheck` and `WithReference(external)` take `ExternalServiceResource`, so each would be re-implemented; publishers do not know the type |
| **C) A and a relationship to the `EncinaResource` of #416 so the dashboard groups them** | The dependency is visible without a new type | One more call; needs #416's `EncinaResource` |

### Chosen Option: **A — wrap, with the relationship of C when an `EncinaResource` exists** (recommended, pending the maintainer)

### Rationale

A wins because the whole value of Aspire's external service is that the model, the health check and the publishers already understand it; B wins only if the maintainer wants Encina properties as first-class dashboard columns and accepts re-implementing the extensions. The relationship (`WithRelationship(encina, "Encina")`, present in `Aspire.Hosting` 13.6.5 as #416 verified) is added only when the consumer already has `WithEncina(encina)`, so the call order does not matter and nothing fails when there is no `EncinaResource`.

**Class:** Dilemma

</details>

<details>
<summary><strong>2. Service discovery: what the AppHost declares for Kubernetes, Consul and DNS (Dilemma D2)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) One option, `Discovery` (`Endpoint` default, `Kubernetes`, `Consul`, `Dns`) plus `DiscoveryName`; written as `Encina__Aspire__ExternalServices__<name>__Discovery` and `__DiscoveryName`; the application's #607 providers (or the service defaults) read it** | The AppHost documents how production finds the service; the same declaration serves the Aspire endpoint in run mode and the provider in publish mode | The application must have the matching #607 provider registered; a start check (Design Choice 6) catches the mismatch |
| **B) The AppHost sets only Aspire's service-discovery variables per mode (a literal URL in run mode, a cluster DNS or DNS URL in publish mode); Consul is not expressible** | No contract with the application; Aspire-native | The issue's Consul acceptance item is dropped; Kubernetes and DNS are only URLs the user could write already |
| **C) The AppHost takes a reference to `Encina.ServiceDiscovery` and registers providers itself** | Strongest integration | An AppHost does not load application code (#416); a provider registered in the AppHost does nothing in the application |

### Chosen Option: **A — a declaration read by the application** (recommended, pending the maintainer)

### Rationale

A wins when the same solution runs against local endpoints in development and against Kubernetes or Consul in production and the team wants one place to say so; B wins for teams that use only URLs and want no application-side contract. C is rejected by the architecture: the AppHost builds a resource model and never runs application code. The names of the three kinds equal the provider names of #607 (Kubernetes DNS provider in core, `Encina.ServiceDiscovery.Consul`); `Dns` maps to Microsoft's DNS resolver that the Aspire template already registers, so it needs no Encina provider.

**Class:** Dilemma

</details>

<details>
<summary><strong>3. The shared broker: a declaration by resource name; Encina never writes a connection string</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `SharesMessageBroker(IResourceBuilder<IResourceWithConnectionString> broker)` records the broker's resource name; the consumer gets the connection string only through its own `WithReference(broker)` (Aspire's path)** | No secret passes through Encina; the dashboard and the manifest show the real relationship | The user writes `WithReference(broker)` on each consumer, as for any Aspire resource |
| **B) Encina calls `WithReference(broker)` on the consumer for the user** | One call less | Secrets reach a consumer that did not ask for them; hides a dependency |
| **C) A connection string as a plain string in the options (the issue sketch)** | Closest to the issue | A string option holding a connection string needs `[JsonIgnore]` and a `ToString()` override (AGENTS.md section 3) and would be written into an environment variable in clear text |

### Chosen Option: **A — a declaration by resource name**

### Rationale

Aspire's convention is that connection strings travel only through `WithReference`; Encina declares which broker the external service shares and where its outbox topic is (`OutboxTopic`), so the application's start check can require the outbox pattern. The environment keys carry a resource name and a topic, never a credential.

**Class:** Trivial
**Settled by:** `Aspire.Hosting` 13.6.5 (`Aspire.Hosting.xml`, `ResourceBuilderExtensions.WithReference` carries connection strings); AGENTS.md section 3 (options with a connection string)

</details>

<details>
<summary><strong>4. Development stub (Dilemma D4)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `UseStub(mappingsDirectory)` adds, in run mode only, a WireMock container resource named `<name>-stub` that serves the JSON mappings of that directory, and the consumer's service-discovery variable for `<name>` points at it instead of the real URL** | Real HTTP on a real port; the mappings are plain files shared with `Encina.Testing.WireMock` tests; nothing changes in publish mode | A container image to pin; a container runtime is needed in run mode when a stub is used |
| **B) No stub in the AppHost: document pointing the endpoint parameter at a local stub the developer starts** | No image, no code | The acceptance item "development stub/mock support" is dropped |
| **C) An in-process stub (a Kestrel mini app inside the AppHost)** | No container | Runs application-like code in the AppHost, against #416; reimplements WireMock |

### Chosen Option: **A — a WireMock container in run mode** (recommended, pending the maintainer)

### Rationale

A wins when a team wants to develop one service while the other repositories' services are absent, which is the whole point of the issue's "gradual adoption"; B wins for teams that already run their own stubs. The image tag is one constant with the source of its choice written next to it (the repository rule of one source per container default). In publish mode `UseStub` has no effect, so no stub ever reaches a deployment.

**Class:** Dilemma

</details>

<details>
<summary><strong>5. Contract-first (Dilemma D5)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `ImplementsContract<T>()` and `ImplementsContract(string typeName)` record the type's full name and assembly name; the application's start check resolves each name among the loaded types and fails when a declared contract is not loaded** | A drift between the AppHost declaration and the application is a visible start failure; the AppHost needs only the (Encina-free) contract assembly | Name resolution at start; a contract assembly that is not yet loaded when the check runs must be forced by the check (resolve through the application's own assemblies list) |
| **B) Not delivered here; an issue file for contract verification** | No name resolution | The acceptance item "contract-first support" is dropped from this issue |
| **C) Verify through a generated contract test in `Encina.Aspire.Testing`** | Runs in CI, not at every start | Does not catch drift in a running solution |

### Chosen Option: **A — a declaration with a start check** (recommended, pending the maintainer)

### Rationale

A wins when the contract library is a shared NuGet package and drift is the risk the issue names; B wins if the maintainer prefers contract checks only in tests (then phases 4 to 6 lose the check, not the declaration); C is complementary and is planned in #440's conformance tests, not here. The check never loads an assembly from a path and never instantiates a type: it matches a name against `AppDomain.CurrentDomain.GetAssemblies()`, so an environment variable cannot cause code to load.

**Class:** Dilemma

</details>

<details>
<summary><strong>6. Fail closed: what the start check and the AppHost refuse</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) The AppHost rejects at model-build time a URL that is not absolute `http` or `https`, carries user-info (`user:pass@`), or names a link-local, cloud metadata or unspecified host; the application's start check fails the host when a declaration is malformed, names a broker without the outbox pattern declared, or names a contract that is not loaded** | A credential in a URL can never reach an environment variable or the dashboard; drift is a start failure, not a silent no-op (AGENTS.md section 3: gates fail closed) | A little code duplicated from `EndpointValidator` (the AppHost does not reference core) |
| **B) Accept anything the user writes and log a warning** | No code | A URL with user-info is shown in the dashboard and written into every consumer's environment |

### Chosen Option: **A — reject at build time, fail at start**

### Rationale

`Encina.Validation.EndpointValidator` and `EndpointPolicy` (`src/Encina/Validation/`) already refuse link-local, cloud metadata and unspecified hosts with no opt-out; the AppHost package keeps no reference to core, so it reimplements the three checks for URLs in 30 lines and a contract test pins the host classification of both implementations against the same sample hosts. Loopback and private hosts stay allowed in the AppHost: local endpoints are the main use of run mode.

**Class:** Settled
**Settled by:** AGENTS.md section 3 (security gates fail closed; secrets never printed or stored in plaintext); `src/Encina/Validation/EndpointPolicy.cs` (link-local, metadata and unspecified hosts have no opt-out)

</details>

<details>
<summary><strong>7. Features of the issue this plan does not deliver (Dilemma D7)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Schema registry and cross-repo correlation are recorded as omissions: correlation follows ADR-054 (W3C trace context, no baggage, so nothing to build); the schema registry becomes its own issue file (a registry integration needs a message-serialization decision that this issue does not own)** | The plan stays one pull-request family; the omission is written, not silent | The issue's "Messaging Integration" bullets are only partly delivered |
| **B) Build a message schema registry abstraction here** | Delivers the bullet | A new feature with its own providers and plan, far beyond an AppHost wrapper |
| **C) Add a custom correlation header** | Matches the issue text | Contradicts ADR-054 D4 (no baggage) and duplicates `traceparent` |

### Chosen Option: **A — record the omissions and open an issue** (recommended, pending the maintainer)

### Rationale

A wins because the two bullets are not part of the issue's acceptance criteria (they sit in its Features list) and one of them is already decided by ADR-054; B wins only if the maintainer wants the registry now. The issue file `artifacts/issues/schema-registry-integration.md` of this run carries the registry.

**Class:** Dilemma

</details>

---

## Implementation Phases

### Phase 1: Options, annotation and validation (pull request 1)

> **Goal**: `EncinaExternalServiceOptions`, the annotation, the discovery enum and the URL validation exist and are tested.

<details>
<summary>Tasks</summary>

1. `src/Encina.Aspire.Hosting/ExternalServices/EncinaExternalServiceDiscovery.cs` (public enum): `Endpoint` (0), `Kubernetes`, `Consul`, `Dns`.
2. `ExternalServices/EncinaExternalServiceOptions.cs` (public sealed): `HealthCheckPath` (`string?`, default `"/health"`, validated like #416's path: starts with `/`, no whitespace), `HealthCheckStatusCode` (`int?`, 100 to 599), `Discovery`, `DiscoveryName` (required when `Discovery` is not `Endpoint`, a valid DNS label list), `OutboxTopic` (`string?`), methods `SharesMessageBroker(IResourceBuilder<IResourceWithConnectionString> broker)` (guard; stores the resource name), `ImplementsContract<T>()`, `ImplementsContract(string typeName)` (a non-blank `Namespace.Type, Assembly` text) and `UseStub(string mappingsDirectory)` (Phase 3). An internal `Validate()` returns the first violation as text; a violation throws `ArgumentException` at the call that built it (builder-time programming error, the convention of #416; there is no `Either` in an AppHost).
3. `ExternalServices/EncinaExternalServiceAnnotation.cs` (public sealed, `IResourceAnnotation`): the validated snapshot of the options (immutable collections).
4. `ExternalServices/ExternalServiceUrlValidator.cs` (internal static): `Validate(string? url)` and `Validate(Uri uri)`: absolute, scheme `http` or `https`, no user-info, no link-local (169.254.0.0/16, fe80::/10), cloud metadata (169.254.169.254, `metadata.google.internal`) or unspecified host (0.0.0.0, ::).
5. `EncinaAspireEnvironment.cs` (the #416 file, edited): `ExternalServiceKey(string serviceName, string field)` returning `Encina__Aspire__ExternalServices__{serviceName}__{field}`; constants `Discovery`, `DiscoveryName`, `Broker`, `OutboxTopic`, `Contracts`, `Stub`.
6. Tests: unit for the validator (each rule, loopback and private allowed), for `Validate()` and for the annotation; guard tests for the public methods with parameters; `.github/coverage-manifest/Encina.Aspire.Hosting.json` entries and `PublicAPI.Unshipped.txt` through `mechanical-fixer`.

</details>

<details>
<summary>Prompt for AI Agents — Phase 1</summary>

```text
CONTEXT:
- Issue #424; plan docs/plans/aspire-external-service-implementation-plan-424.md. D1, D2, D4, D5 and D7 (the Dilemmas, numbered like the Design Choices) are recommended, pending the maintainer: follow the Maintainer Decisions section if it exists. The package Encina.Aspire.Hosting exists (#416 merged).
- Aspire.Hosting 13.6.5: AddExternalService, ExternalServiceResource, WithHttpHealthCheck and WithReference(external) exist; confirm each signature in Aspire.Hosting.xml before coding.

TASK:
Add the discovery enum, the options, the annotation, the URL validator and the environment key helper with their tests.

KEY RULES:
- The Encina.Aspire.Hosting project takes no reference to any Encina package (#416); the validator duplicates three host rules from src/Encina/Validation/EndpointValidator.cs and a contract test compares both on the same sample hosts.
- A credential never appears: no connection string, no URL with user-info, no secret in any property; ToString of the options prints no URL.
- Throw ArgumentException/ArgumentNullException with the parameter name at the call that gets the bad value.

REFERENCE FILES:
- src/Encina.Aspire.Hosting (created by #416)
- src/Encina/Validation/EndpointValidator.cs, src/Encina/Validation/EndpointPolicy.cs
- src/Encina.Aspire.Testing/DistributedApplicationTestingBuilderExtensions.cs
```

</details>

---

### Phase 2: The two entry points (pull request 1)

> **Goal**: `AddEncinaExternalService` and `WithEncinaExternalService` build the model and write the environment.

<details>
<summary>Tasks</summary>

1. `ExternalServices/EncinaExternalServiceBuilderExtensions.cs`: `AddEncinaExternalService(this IDistributedApplicationBuilder builder, string name, string url, Action<EncinaExternalServiceOptions>? configure = null)` and the overload taking `IResourceBuilder<ParameterResource> urlParameter` (a parameter is not validated at build time: its value arrives at run time, so the start check of Phase 5 validates the injected URL): guards, runs `configure`, validates, calls `AddExternalService`, `WithHttpHealthCheck(path, status)` when `HealthCheckPath` is not null, adds the annotation. A second call with the same name throws `InvalidOperationException` (Aspire already refuses a duplicate resource name; the message names the resource).
2. `ExternalServices/EncinaExternalServiceResourceBuilderExtensions.cs`: `WithEncinaExternalService<T>(this IResourceBuilder<T> consumer, IResourceBuilder<ExternalServiceResource> external) where T : IResourceWithEnvironment`: throws `InvalidOperationException` when the external resource has no `EncinaExternalServiceAnnotation` (it was not created by `AddEncinaExternalService`); calls `consumer.WithReference(external)` (Aspire's variables), then writes the keys of Phase 1 (`Discovery`, `DiscoveryName`, `Broker`, `OutboxTopic`, `Contracts__0`..`Contracts__n`) with `WithEnvironment`; when the consumer already carries a `WithEncina(encina)` annotation of #416 it adds `WithRelationship(encina, "Encina")`. A consumer that references two external services gets two key sets (the name is in the key).
3. The service name is the resource name; Aspire resource names are `[A-Za-z0-9-]`, which is a legal configuration segment, so the key is injective per service name (two services never write the same key). There is no tenant in the AppHost; the reserved `default` tenant segment does not apply here.
4. Tests: unit over `DistributedApplication.CreateBuilder()` (the model, no container runtime): the health check annotation, the keys per option, two services, a missing annotation, the parameter overload; guard tests; the manifest and PublicAPI lines through `mechanical-fixer`.

</details>

<details>
<summary>Prompt for AI Agents — Phase 2</summary>

```text
CONTEXT:
- Issue #424, Phase 2; pull request 1 continues. The options, annotation and validator of Phase 1 exist.
- Follow the naming rule of #440: AddEncina* on the builder, WithEncina* on a resource.

TASK:
Write AddEncinaExternalService (two overloads) and WithEncinaExternalService with their tests, over the real Aspire model.

KEY RULES:
- Read the environment values from the annotation only; never from the options object after the call.
- Environment keys come only from EncinaAspireEnvironment; the application reader of Phase 4 reads the same keys.
- No connection string and no URL is written by Encina; Aspire writes the endpoint through WithReference.

REFERENCE FILES:
- src/Encina.Aspire.Hosting/EncinaResourceBuilderExtensions.cs (#416)
- src/Encina.Aspire.Testing/DistributedApplicationTestingBuilderExtensions.cs (second-call guard convention)
- Aspire.Hosting 13.6.5 Aspire.Hosting.xml (ExternalServiceBuilderExtensions)
```

</details>

---

### Phase 3: The development stub (pull request 2)

> **Goal**: `UseStub` adds a WireMock container in run mode and points the consumers at it.

<details>
<summary>Tasks</summary>

1. `ExternalServices/EncinaExternalServiceStub.cs` (internal): the constants `Image` (`wiremock/wiremock`) and `ImageTag` (one constant, with the source of the choice in a comment; verify the current stable tag at Phase 3), the container port, the mappings mount path.
2. `EncinaExternalServiceOptions.UseStub(string mappingsDirectory)`: stores a full path; throws when the directory does not exist at build time (a missing mappings directory is a mistake, not a silent empty stub).
3. In `AddEncinaExternalService`: when the stub is declared and `builder.ExecutionContext.IsRunMode` is true, `AddContainer("<name>-stub", Image, ImageTag)` with a bind mount of the directory (read-only) and an HTTP endpoint; the resource carries `ExcludeFromManifest()` so a publish never sees it. In publish mode nothing is added.
4. `WithEncinaExternalService`: when a stub exists, it writes `services__<name>__http__0` to the stub's endpoint expression instead of calling `WithReference(external)` for the URL, so the consumer's discovery name is the same in both worlds. Verify in `Aspire.Hosting.xml` that no `WithReference` overload renames the service; if one exists use it.
5. Tests: unit (a stub adds one container in run mode and none in publish mode, the mount is read-only, a missing directory throws); an integration test under `tests/Encina.IntegrationTests` with `DistributedApplicationTestingBuilder` (through `Encina.Aspire.Testing`) that starts the AppHost with a stub and calls the stub through the consumer's variable (needs Docker; `[Trait("Category", "Integration")]`); guard tests; PublicAPI and manifest lines through `mechanical-fixer`.

</details>

<details>
<summary>Prompt for AI Agents — Phase 3</summary>

```text
CONTEXT:
- Issue #424, Phase 3 (pull request 2). Pull request 1 is merged. D4 is recommended, pending the maintainer: follow the Maintainer Decisions section if it exists.

TASK:
Add UseStub, the WireMock container in run mode only, the consumer redirect, and their tests.

KEY RULES:
- In publish mode no stub resource is created and nothing about it reaches the manifest.
- The image tag is one constant, never "latest".
- The mappings mount is read-only; the directory must exist.

REFERENCE FILES:
- src/Encina.Testing.WireMock (the mapping format used in tests)
- src/Encina.Aspire.Testing/EncinaTestContext.cs
- tests/Encina.IntegrationTests (collection fixtures; Docker-based tests use a [Collection] fixture)
```

</details>

---

### Phase 4: The application-side reader and catalog (pull request 3)

> **Goal**: The application reads the declaration into a read-only catalog.

<details>
<summary>Tasks</summary>

1. `src/Encina.Aspire.ServiceDefaults/ExternalServices/EncinaExternalServiceDeclaration.cs` (public sealed record): `Name`, `Discovery` (an enum of the same four values defined in the application package, because the AppHost and application packages share no type), `DiscoveryName`, `BrokerResourceName`, `OutboxTopic`, `Contracts` (`IReadOnlyList<string>`).
2. `ExternalServices/EncinaExternalServiceReader.cs` (internal static): `Read(IConfiguration)` returns `Either<EncinaError, IReadOnlyList<EncinaExternalServiceDeclaration>>`; every error is created with `EncinaErrors.Create(code, category: ErrorCategory.Validation, message: ...)`: `encina.aspire.externalservice.invalid_declaration` (`ErrorCategory.Validation`, a key that is not a known field, an unknown `Discovery` value, `Discovery` without `DiscoveryName`). The error message never contains a configuration value; it names the key.
3. `ExternalServices/IEncinaExternalServiceCatalog.cs` and `EncinaExternalServiceCatalog.cs`: read-only lookup by name (`TryGet`) and the list; registered as a singleton with `TryAddSingleton` by `AddEncinaDefaults` only when at least one declaration exists (pay for what you use).
4. `EncinaDefaultsExtensions.AddEncinaDefaults` (the #417 file, edited): reads the declarations; a `Left` is thrown as `InvalidOperationException` carrying the error code only.
5. Tests: unit for the reader (each field, none declared, an unknown field, an invalid discovery, no value in any message), guard, a DI test that builds the provider with `ValidateOnBuild` and `ValidateScopes`, and the writer-reader parity case added to `EnvironmentContractTests` (one edit of a #440 file); PublicAPI, manifest through `mechanical-fixer`. The catalog is `internal sealed` (the public interface returns only the public declaration type; no public constructor takes an internal type).

</details>

<details>
<summary>Prompt for AI Agents — Phase 4</summary>

```text
CONTEXT:
- Issue #424, Phase 4 (pull request 3). #417's AddEncinaDefaults and EncinaAspireConfiguration exist. D2 and D5 are recommended, pending the maintainer.

TASK:
Write the declaration type, the reader returning Either, the catalog and its registration, with tests.

KEY RULES:
- Create each error with EncinaErrors.Create(code, category: ErrorCategory.<X>, message: ...): a named category argument, one category per code (the completeness scan fails otherwise).
- No configuration value in any message, log or exception.
- Register with TryAdd*; the catalog is a singleton holding data only; no scoped dependency.

REFERENCE FILES:
- src/Encina.Aspire.ServiceDefaults (created by #417)
- src/Encina/Results/EncinaErrors.cs, docs/architecture/adr/062-error-category-contract.md
```

</details>

---

### Phase 5: Cross-cutting integration — the start check (pull request 3)

> **Goal**: A declaration that does not match the application fails the start.

<details>
<summary>Tasks</summary>

1. `ExternalServices/ExternalServiceStartCheck.cs` (internal sealed, `IHostedService` whose `StartAsync` runs the check): for each declaration: (a) a declared broker requires the outbox pattern declared by #416's `Encina__Aspire__Patterns__Outbox` (`encina.aspire.externalservice.broker_requires_outbox`, `ErrorCategory.FailedPrecondition`); (b) each contract name must match a type among the loaded assemblies (`encina.aspire.externalservice.contract_not_loaded`, `ErrorCategory.FailedPrecondition`); (c) for a parameterized URL, the injected `services__<name>__*` URL goes through `Encina.Validation.EndpointValidator` with `EndpointPolicy.ForHttps(allowInsecureHttp: true, allowLocalEndpoints: true)` (`encina.aspire.externalservice.endpoint_rejected`, `ErrorCategory.Validation`; link-local, metadata and unspecified hosts stay rejected). Any failure throws `InvalidOperationException` with the code and the service name, never a URL or a value (the host stops: the gate fails closed).
2. Missing context denies: a declaration with a name the catalog cannot read is a failure, not a skip.
3. Registered by `AddEncinaDefaults` after the #417 start check, only when a declaration exists.
4. Tests: unit for each failure and for the passing case. The writer-reader parity case of #440's `EnvironmentContractTests` lands with the reader in Phase 4 (pull request 3a), not here.

</details>

<details>
<summary>Prompt for AI Agents — Phase 5</summary>

```text
CONTEXT:
- Issue #424, Phase 5 (pull request 3). The catalog of Phase 4 exists.

TASK:
Write the start check with its three failures and add the parity case to the environment contract test.

KEY RULES:
- Fail closed: any declaration the check cannot verify stops the host.
- Never load an assembly from a path and never instantiate a type: match names against AppDomain.CurrentDomain.GetAssemblies().
- Throw with the error code and the service name only.

REFERENCE FILES:
- src/Encina.Aspire.ServiceDefaults (the #417 start check)
- src/Encina/Validation/EndpointValidator.cs
- tests/Encina.ContractTests/Aspire/EnvironmentContractTests.cs (#440)
```

</details>

---

### Phase 6: Observability

> **Goal**: The start check is visible in structured logs; no tracing is added.

<details>
<summary>Tasks</summary>

1. `ExternalServices/Diagnostics/ExternalServiceLog.cs` (`[LoggerMessage]`, in `Encina.Aspire.ServiceDefaults`): 850 external services loaded (Information: count), 851 invalid declaration (Error: error code, key name), 852 broker declared without the outbox pattern (Error: service name, code), 853 contract not loaded (Error: service name, contract name, code). Registered first: `EventIdRanges.AspireExternalServices = (850, 854)` in `src/Encina/Diagnostics/EventIdRanges.cs`, the `AssemblyRanges` entry of `EncinaEventIdAllocationTests` (the assembly `Encina.Aspire.ServiceDefaults` then maps to two ranges, as #417's 800-819 plus this one), the `Encina` PublicAPI line. Ids 850-853 are packed; 854 is spare.
2. No `ActivitySource` and no `Meter`: the start check runs once and the AppHost has no telemetry; cross-service traces are W3C `traceparent` propagated by `HttpClient` and the transports (ADR-054), with no baggage (D4: no custom correlation header and no tenant tag emitted here, because nothing is measured per tenant).
3. Errors are logged by code, never by `EncinaError.Message`, and an exception through `ForLogging()`.

</details>

<details>
<summary>Prompt for AI Agents — Phase 6</summary>

```text
CONTEXT:
- Issue #424, Phase 6. The start check of Phase 5 exists and logs nothing yet.

TASK:
Register the EventId range first, then write ExternalServiceLog and call it from the start check.

KEY RULES:
- EventIdRanges.AspireExternalServices first; ids 850-853 packed inside 850-854; XML docs naming the range.
- Log codes, names and integers only; no URL, no configuration value, no EncinaError.Message.

REFERENCE FILES:
- src/Encina/Diagnostics/EventIdRanges.cs
- tests/Encina.UnitTests/Testing/Architecture/EncinaEventIdAllocationTests.cs
- artifacts/decisions/plan-batch/eventid-blocks.md (block 850-899, aspire-2)
```

</details>

---

### Phase 7: Testing

> **Goal**: Every flag reaches its target with tests that run the real code.

<details>
<summary>Tasks</summary>

1. **Unit** (`tests/Encina.UnitTests/Aspire/ExternalServices/`): validator, options, builder extensions over a real model, stub (run and publish mode), reader, catalog, start check.
2. **Guard** (`tests/Encina.GuardTests/Aspire/ExternalServices/`): null and blank arguments of the two entry points, of `SharesMessageBroker`, `ImplementsContract`, `UseStub`; a nullable `configure` accepts null and returns a builder.
3. **Contract** (`tests/Encina.ContractTests/Aspire/`): the host classification of `ExternalServiceUrlValidator` equals `EndpointValidator.ClassifyHost` for a table of sample hosts; the writer-reader parity for external services.
4. **Property**: not applicable, no complex logic (the key builder is injective by construction, covered by a unit test over names).
5. **Integration**: the stub test of Phase 3 (Docker); the other behavior needs no external system and is unit-tested over the real model.
6. **Load and benchmark**: not applicable; stated in the pull request (an AppHost model builder runs once).
7. **Per-file targets** (measured at work start with `dotnet test tests\Encina.<Flag>Tests --collect "XPlat Code Coverage"` and `.github/scripts/coverage-report.cs`; new files have no baseline):

   | File | Unit | Guard | Contract | Property | Integration | Justification |
   |------|-----:|------:|---------:|---------:|------------:|---------------|
   | `ExternalServiceUrlValidator.cs` | 100 | 0 | 90 | 0 | 0 | Pure rules, each tested; internal static with no guarded public parameters, the contract test covers the host table |
   | `EncinaExternalServiceOptions.cs` | 95 | 90 | 0 | 0 | 0 | Validation and the three public methods with parameters are reached by unit and guard tests |
   | `EncinaExternalServiceAnnotation.cs` | 100 | 0 | 0 | 0 | 0 | A data holder read in every builder test; no parameterized public member reachable by guard tests |
   | `EncinaExternalServiceBuilderExtensions.cs` | 95 | 90 | 0 | 0 | 0 | Run over the real Aspire model, both overloads and every guard |
   | `EncinaExternalServiceResourceBuilderExtensions.cs` | 95 | 90 | 0 | 0 | 0 | Each key, the missing annotation and the relationship, asserted on the real model |
   | `EncinaExternalServiceStub.cs` | 90 | 0 | 0 | 0 | 70 | Constants and the container wiring, run in both modes by unit tests; the integration test of the same pull request starts the stub with Docker |
   | `EncinaExternalServiceReader.cs` | 100 | 0 | 90 | 0 | 0 | Pure parsing with every branch tested; internal, the parity contract test runs it |
   | `EncinaExternalServiceCatalog.cs` | 100 | 85 | 0 | 0 | 0 | Lookup and list; the constructor guard is reached by guard tests |
   | `ExternalServiceStartCheck.cs` | 95 | 85 | 0 | 0 | 0 | Each failure and the passing case with an in-memory configuration |
   | `ExternalServiceLog.cs` | 90 | 0 | 0 | 0 | 0 | Generated logger methods called by the start check tests; no parameters worth a guard test |

8. Per-flag coverage is reported in the pull request against this table, `crap-gate` is run locally (`CRAP <= 10` on every touched method; the start check has a small switch over three failures, kept below complexity 8), and `coverage-report.cs --check-justifications` exits 0.

</details>

<details>
<summary>Prompt for AI Agents — Phase 7</summary>

```text
CONTEXT:
- Issue #424, Phase 7. The tests of each phase are written with its code; this phase closes the gaps against the per-file targets.

TASK:
Measure per-flag coverage of the files above, close every gap, and run crap-gate and the justification check.

KEY RULES:
- Tests execute the real code over DistributedApplication.CreateBuilder(); no reflection-only test.
- Guard tests live in tests/Encina.GuardTests, never only in UnitTests; a nullable parameter that accepts null gets a valid-result test.
- Integration test uses a shared [Collection] fixture and [Trait("Category", "Integration")].

REFERENCE FILES:
- .github/coverage-manifest/Encina.Aspire.Hosting.json (#416)
- docs/testing/coverage-measurement-methodology.md
- tests/Encina.UnitTests/Testing/Architecture/EncinaEventIdAllocationTests.cs
```

</details>

---

### Phase 8: Documentation & Finalization (pull request 5)

> **Goal**: Users can follow the feature from a README.

<details>
<summary>Tasks</summary>

1. XML documentation with `<example>` on every new public type and member.
2. `src/Encina.Aspire.Hosting/README.md` and `src/Encina.Aspire.ServiceDefaults/README.md` sections (through `docs-writer`): the call, the environment keys table, the stub, the contract check, the limits (declaration not instruction; no connection string; stub in run mode only).
3. `changelog.d/424-aspire-external-service.added.md` (through `mechanical-fixer`).
4. `docs/INVENTORY.md` line; no ADR (the layout and naming decisions are #440's); no ROADMAP change.
5. `PublicAPI.Unshipped.txt` for both packages and `Encina`; build `dotnet build --configuration Release` with 0 warnings; every flag at its target.
6. The follow-up issue file for the schema registry (D7) is opened by the orchestrator with this plan.

</details>

<details>
<summary>Prompt for AI Agents — Phase 8</summary>

```text
CONTEXT:
- Issue #424, Phase 8 (pull request 5). All code of pull requests 1 to 3 is merged.

TASK:
Write the documentation sections, the changelog fragment and the inventory line.

KEY RULES:
- Documentation never types coverage figures by hand; use covref markers or omit them.
- English only; no private application names; "the reference application" if a case study is needed.
- Public docs state Spain/EU only where a regulation is mentioned (none is expected here).

REFERENCE FILES:
- .claude/skills/encina-docs/SKILL.md
- changelog.d/README.md
- src/Encina.Aspire.Hosting/README.md (#416)
```

</details>

---

### Proposed split into pull requests (limit 15 files and 1500 lines each)

| PR | Content | Files | Approx. lines |
|----|---------|------:|--------------:|
| 1 | `EncinaExternalServiceDiscovery`, `EncinaExternalServiceOptions`, `EncinaExternalServiceAnnotation`, `ExternalServiceUrlValidator`, `EncinaExternalServiceBuilderExtensions`, `EncinaExternalServiceResourceBuilderExtensions`, `EncinaAspireEnvironment.cs` edit, hosting `PublicAPI.Unshipped.txt`, hosting manifest, unit tests (2 files), guard tests (1 file), the validator contract test (1 file; `Encina.ContractTests` already references the hosting package through #440) | 13 | 1,200 |
| 2 | `EncinaExternalServiceStub`, options and builder edits, `PublicAPI.Unshipped.txt`, manifest, unit and guard test edits, integration test and its fixture reference (`Encina.IntegrationTests.csproj` edit) | 9 | 650 |
| 3a | Declaration, reader, catalog and interface, `EncinaDefaultsExtensions.cs` edit, ServiceDefaults `PublicAPI.Unshipped.txt` and manifest, unit, guard and DI test files, `EnvironmentContractTests.cs` edit (the writer-reader parity case, so the reader's contract target is reached in the pull request that sets it) | 11 | 1,000 |
| 3b | `ExternalServiceStartCheck`, `ExternalServiceLog`, `EventIdRanges.cs` edit, `Encina` `PublicAPI.Unshipped.txt`, `Encina.json` manifest, ServiceDefaults manifest edit, allocation test map edit, `EncinaDefaultsExtensions.cs` edit (registers the check), unit test | 9 | 800 |
| 4 | Two README sections, `docs/INVENTORY.md` line, changelog fragment, two justification files (load, benchmark) | 6 | 330 |

PR 1 needs #416 pull requests 1 and 2 (the package and `WithEncina`); PR 2 needs PR 1; PR 3a needs #417 and #440 (merged on `main`: `AddEncinaDefaults`, the reader and `EnvironmentContractTests`) and PR 1; PR 3b needs PR 3a; PR 4 is last. No interface is changed, so no implementer is touched. A pull request that measures over 15 files or 1,500 lines when it is briefed is split again. Guard tests, the DI test and the manifests are counted in every row.

---

## Research

### Relevant Standards & Specifications

| Reference | What it defines | Use in this feature |
|-----------|-----------------|---------------------|
| `AGENTS.md` sections 3 and 6 | Fail closed, secrets never printed, errors by code, 12 functions | Design Choice 6, Phases 4 to 6 |
| ADR-021 | EventId ranges registered first | `AspireExternalServices` 850-854 |
| ADR-054 (D4) | W3C trace context, no baggage, tenant tag only when tenancy is on | Why no correlation code is built |
| ADR-062 | Error category contract | Error codes and categories of Phases 4 and 5 |
| SPEC-000 section 6 and DEC-009 | Aspire integration that runs locally is in 1.0 | Scope |
| `Aspire.Hosting` 13.6.5 (`ExternalServiceBuilderExtensions`, `ExternalServiceResource`, `WithReference`, `WithRelationship`) | The AppHost API | Phases 1 to 3 |

### Existing Encina Infrastructure to Leverage

| Component | Location | Usage in this feature |
|-----------|----------|-----------------------|
| `Encina.Aspire.Hosting` (`EncinaResource`, `WithEncina`, `EncinaAspireEnvironment`) | #416 | The package that receives the folder; the key helper |
| `Encina.Aspire.ServiceDefaults` (`AddEncinaDefaults`, `EncinaAspireConfiguration`) | #417 | The reader and the start check |
| `EndpointValidator`, `EndpointPolicy` | `src/Encina/Validation/EndpointValidator.cs`, `EndpointPolicy.cs` | The rules the AppHost duplicates and the start check calls |
| `EncinaErrors.Create(code, category, message)` | `src/Encina/Results/EncinaErrors.cs` | Error creation with a named category |
| `Encina.Testing.WireMock` | `src/Encina.Testing.WireMock` | The mapping format the stub reads |
| `Encina.Aspire.Testing` | `src/Encina.Aspire.Testing` | Integration test of the stub |
| `Encina.ServiceDiscovery` providers | #607 | What `Discovery` names |

### Event ID Allocation

| Package | Range | Notes |
|---------|-------|-------|
| `Encina.Aspire.ServiceDefaults` (`AspireExternalServices`, new) | 850-854, ids 850-853 used | From the assigned block 850-899 (aspire-2, `artifacts/decisions/plan-batch/eventid-blocks.md`); the siblings of the family use 855-864 (#425), 865-877 (#382), 878-889 (#387) and 890-899 (#391); registered in `EventIdRanges.cs` and the allocation test map before use |

### Estimated File Count

| Category | Files | Notes |
|----------|------:|-------|
| Production code | 13 | 6 in the hosting folder (enum, options, annotation, validator, two extension classes) plus the stub; 5 in ServiceDefaults (declaration, reader, catalog and interface, start check, log); `EventIdRanges.cs` edit |
| Edits of sibling files | 3 | `EncinaAspireEnvironment.cs`, `EncinaDefaultsExtensions.cs`, `EnvironmentContractTests.cs` |
| Public API and manifests | 6 | Hosting and ServiceDefaults `PublicAPI.Unshipped.txt` and manifests, `Encina` PublicAPI, `Encina.json` |
| Tests and project files | 11 | Unit (5), guard (2), contract (2), DI (1), integration (1), allocation map edit, one csproj edit |
| Documentation | 4 | Two README sections, inventory line, changelog fragment |
| **Total** | **by the split table: 13 + 9 + 11 + 9 + 6 = 48 file touches over 5 pull requests** | The split table is the authority |

---

## Combined AI Agent Prompts

<details>
<summary><strong>Full combined prompt for all phases</strong></summary>

```text
PROJECT CONTEXT:
- Encina is a pre-1.0 .NET 10 / C# 14 library; Either<EncinaError, T>; no [Obsolete], no compatibility layers.
- Issue #424: AddEncinaExternalService for services in other repositories. Plan: docs/plans/aspire-external-service-implementation-plan-424.md; D1, D2, D4, D5 and D7 are recommended, pending the maintainer.

IMPLEMENTATION OVERVIEW (5 pull requests, each builds alone):
1. Options, annotation, URL validation, AddEncinaExternalService, WithEncinaExternalService (Phases 1-2).
2. The WireMock stub of run mode (Phase 3).
3. The application reader, catalog, start check and log (Phases 4-6).
4. Documentation (Phase 8).

KEY PATTERNS:
- Wrap Aspire's AddExternalService; keep the Encina declaration in an annotation; never write a connection string or a URL with credentials.
- The application reads Encina__Aspire__ExternalServices__<name>__<field> and fails the start when the declaration does not match the code.
- EncinaErrors.Create(code, category: ErrorCategory.X, message: ...) with a named category; codes only in logs and exceptions.
- The hosting project references no Encina package; the validator duplicates three host rules and a contract test compares it with EndpointValidator.

REFERENCE FILES:
- src/Encina.Aspire.Hosting, src/Encina.Aspire.ServiceDefaults (#416, #417)
- src/Encina/Validation/EndpointValidator.cs
- src/Encina/Diagnostics/EventIdRanges.cs
- .github/coverage-manifest/Encina.Aspire.Testing.json (manifest shape)
```

</details>

---

## Cross-Cutting Integration Matrix

| # | Function | Status | Notes |
|---|----------|--------|-------|
| 1 | Caching | ❌ | Not applicable: the declaration is read once at start |
| 2 | OpenTelemetry | ❌ | Not applicable as new instrumentation: cross-repo traces are W3C `traceparent` propagated by `HttpClient` and the transports, with no baggage (ADR-054 D4); the AppHost emits no telemetry; omission recorded in Design Choice 7 |
| 3 | Structured Logging | ✅ | Phase 6: `ExternalServiceLog`, EventIds 850-853 of `AspireExternalServices` |
| 4 | Health Checks | ✅ | Phase 2: Aspire's HTTP health check on the external resource through `HealthCheckPath`; no application-side check (the application does not probe other repositories' services here) |
| 5 | Validation | ✅ | Phase 1 and 5: URL, path, status code and discovery name at build time; the injected URL at start |
| 6 | Resilience | ❌ | Not applicable: the plan makes no call; outbound resilience is #1233 and the service defaults of #417 |
| 7 | Distributed Locks | ❌ | Not applicable: no shared state |
| 8 | Transactions | ❌ | Not applicable: no store |
| 9 | Idempotency | ❌ | Not applicable: the model is rebuilt on every AppHost start and the start check is read-only |
| 10 | Multi-Tenancy | ❌ | Not applicable: the AppHost and the declaration are not tenant-aware; no tenant-qualified id exists, so the reserved `default` segment and injectivity across tenants do not arise (the key is injective per service name) |
| 11 | Module Isolation | ❌ | Not applicable: modules in the AppHost are #423; an external service is not a module |
| 12 | Audit Trail | ❌ | Not applicable: a development-time model; no state change to audit |

---

## Prerequisites & Dependencies

- **#416 pull requests 1 and 2** (the package, `EncinaResource`, `WithEncina`, the environment keys) before pull request 1; **#417 pull requests 1 and 2** (`AddEncinaDefaults`, the reader) before pull request 3a; #440's `EnvironmentContractTests` before pull request 3b (otherwise 3b adds that parity test inside its own contract file).
- #607 supplies the providers `Discovery` names; this plan builds no provider and needs #607 only for its documentation example and for the end-to-end case.
- `Aspire.Hosting` 13.6.5 is pinned (#3491); the plan adds no package. `WireMock` is used through its container image only.
- The issue file for the schema registry is opened by the orchestrator with this plan.

---

## Next Steps

1. The maintainer answers D1, D2, D4, D5 and D7 when work on #424 starts; the answers go into `## Maintainer Decisions` and the markers are removed.
2. Pull request 1 after #416; pull request 2; pull request 3a after #417; pull request 3b; pull request 4 last.
