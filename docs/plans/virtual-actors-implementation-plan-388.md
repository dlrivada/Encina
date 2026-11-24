# Implementation Plan: Virtual Actors (Encina.Actors Contracts and Encina.Orleans Integration, Opt-In Identity, Tenant-Qualified Grain Keys)

> **Issue**: [#388](https://github.com/dlrivada/Encina/issues/388)
> **Type**: Feature (new packages: `Encina.Actors`, `Encina.Orleans`)
> **Related**: #459 (Encina.Orleans handlers as grains, sagas and reminders: builds on this plan's package), #330 (actor-based event sourcing, a different pattern), #420 (Dapr building blocks, closed, planned for the 1.0 transports by `AGENTS.md` section 5: the `IActorSystem` contract must not depend on Orleans), #1257 (default-tenant rule, reserved `default` segment), ADR-054 (telemetry), ADR-062 (`ErrorCategory`), ADR-064 (the result union that will replace LanguageExt `Either`, #3662), SPEC-002 REQ-015 (restored identity), #2184 (stage contract)
> **Complexity**: High (two new packages, a wire contract for `Either`, an identity trust boundary and a tenant-qualified key)
> **Estimated Scope**: ~1,150 lines of production code + ~1,600 lines of tests, in six pull requests of at most 15 files

---

## Summary

### What exists today (read on 2026-11-23, main `38204119d`)

- There is no `Encina.Actors`, no `Encina.Orleans`, no Orleans package in `Directory.Packages.props` (the only related entries are `Dapr.Client` and `Dapr.AspNetCore`), no project in `Encina.slnx` and no manifest. `ROADMAP.md` lists `Encina.Actors` and `Encina.Orleans` as planned packages; #459 (open, no plan on main) also plans `Encina.Orleans` for handlers as grains, sagas, reminders and streams, so the two issues share one package (Design Choice 1 names the owner).
- The latest stable `Microsoft.Orleans.Server` on nuget.org on 2026-11-23 is 10.4.2 (the 10.x line targets .NET 10); the implementing pull request pins the `Microsoft.Orleans.*` versions in `Directory.Packages.props` after confirming `net10.0` support of each package it uses.
- `IEncina` is dispatched inside request scopes: identities are created only by Encina entry points (`src/Encina/Identity/RequestIdentity.cs`: "Application code cannot mint an authenticated identity"). The one existing way to run work under a persisted actor is `IRequestContextScopeFactory.RunRestoredAsync(persisted, PersistedIdentitySource.Internal | External, work, configuredTenantId, ct)` (`src/Encina/Identity/`): `Internal` restores actor and tenant, `External` runs anonymous with the dispatcher's trusted tenant. A grain that calls `IEncina.Send` with no scope reads the anonymous identity, so every gate that needs an authenticated caller denies.
- `EncinaError` is a `readonly record struct` with a `Message` and an optional `Exception` (`src/Encina/Errors/EncinaError.cs`); ADR-064 (accepted) replaces LanguageExt `Either` as the ROP carrier when #3662 is implemented. A wire contract written against `Either<EncinaError, T>` would break at that change.
- The issue's sketches cannot be built as written for Orleans: grain interfaces may not declare properties (`ActorId Id { get; }`, `TState State { get; }`), `GetAllActorsAsync` has no Orleans equivalent (only activated grains are visible, virtual actors are not enumerable), `EncinaGrain` takes `IEncina` by field but no constructor, `AddEncinaOrleans` would duplicate `UseOrleans` (cluster, service id, storage) which belongs to the application's silo, and `Task<Either<EncinaError, T>>` needs a serializer.
- Module and tenant context: `MultiTenancyMarker` signals multi-tenancy; `IRequestContext.TenantId` is the trusted tenant.

### What this plan delivers

- **`Encina.Actors`** (contracts, no Orleans reference): `ActorId` (a validated `Type` and `Key`; `ActorId.Create` returns `Either`), the marker `IEncinaActor`, `IActorSystem` (`Either<EncinaError, TActor> GetActor<TActor>(ActorId id) where TActor : IEncinaActor`) and `ActorErrorCodes`.
- **`Encina.Orleans`**: `ISiloBuilder.AddEncinaOrleans(Action<EncinaOrleansOptions>? configure = null)` (registers `IActorSystem`, the call filters and the failure codec into the application's own silo; cluster, service id and storage stay the application's `UseOrleans` code), `EncinaGrain<TState>` (`Grain` with `IPersistentState<TState>`, `protected Task<Either<EncinaError, TResponse>> SendAsync<TRequest, TResponse>(...)` running `IEncina.Send` inside the right identity scope, `protected Task SaveStateAsync()`), `OrleansActorSystem`, `ActorGrainKey` (tenant-qualified, injective), the incoming and outgoing identity call filters and a wire contract for failures (`ActorFailure`).
- **Identity and tenant trust** are opt-in and fail closed. A grain key is chosen by the caller, so the tenant inside it is wire data, never proof (Design Choice 6). By default every grain call runs anonymous; with multi-tenancy on, the incoming filter denies the call (`encina.actors.tenant_not_verified`) unless the tenant of the key is verified by an application-registered `IActorTenantVerifier` or by the explicit, logged opt-in `EncinaOrleansOptions.TrustGrainKeyTenant = true` (a declared private-gateway assumption). `TrustCallerIdentity = true` (logged once at startup) restores the caller identity carried in the Orleans request context with `PersistedIdentitySource.Internal` and, because it declares the same network trust, also satisfies the tenant check, provided the restored tenant equals the key tenant.
- **Observability and logging** in a new EventId block (claim below), ADR-054 compliant.
- **Not delivered**: an in-process actor runtime (`AddEncinaActors`, `UseInMemoryStorage()`) and `GetAllActorsAsync`: each recorded as a follow-up issue file or a register row; handlers as grains, saga grains, reminders and streams belong to #459.
- Affected packages: `Encina.Actors` (new), `Encina.Orleans` (new). Provider category: none of the Encina categories. Durable state uses Orleans grain storage providers chosen by the application; Encina ships no database actor store, so the 10-provider database rule does not apply (Design Choice 5).

---

## Design Choices

<details>
<summary><strong>1. Scope of the two packages and who owns `Encina.Orleans`</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `Encina.Actors` holds only runtime-neutral contracts; `Encina.Orleans` holds the integration; this plan creates both packages and owns the `Encina.Orleans` skeleton that #459 later extends; the in-process runtime is a follow-up issue** | The contracts stay usable by a Dapr actors adapter later; no second runtime to design now; one clear owner of the shared package | The issue's standalone `AddEncinaActors` and `UseInMemoryStorage()` are not delivered here |
| **B) A + an in-process actor runtime (mailboxes, activation, in-memory state)** | Tests and demos without a silo; the issue's standalone sketch is met | A second actor runtime to design, test and keep equal to Orleans semantics: roughly three more pull requests |
| **C) Orleans only, no `Encina.Actors`** | One package | The `IEncinaActor` and `ActorId` the issue asks for would be Orleans-bound; blocks a Dapr actors adapter |

### Chosen Option: **A - Contracts plus Orleans, runtime later** (recommended, pending the maintainer)

### Rationale

- A wins now because Orleans already is the runtime the issue's title names and TestingHost gives silo-less-looking tests. B wins if the maintainer wants actors without Orleans before 1.0; the follow-up issue file `artifacts/issues/actors-in-process-runtime.md` records it. C wins only if no second actor backend is ever planned. This plan (G2) is the single owner that creates the `Encina.Orleans` csproj, its package references and its `Encina.slnx` entry; #459 has no plan on main and only extends the package after G3 merges, never creating it. #459 uses the `Orleans` EventId range after this plan's G4 registers it.

**Class:** Dilemma

</details>

<details>
<summary><strong>2. How Orleans is configured: into the application's silo, or by Encina</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `ISiloBuilder.AddEncinaOrleans(...)` adds Encina's services and filters; the application keeps `UseOrleans`, clustering, `ClusterOptions` and storage** | No second owner of cluster and storage configuration; works with Aspire (`#459`) and with every Orleans storage package; filters are registered the Orleans way | The application writes two calls (`UseOrleans` and `AddEncinaOrleans`) |
| **B) `services.AddEncinaOrleans(config => { config.ClusterId...; config.UseDevelopmentStorage(); })` as the issue sketches** | One call | Duplicates `UseOrleans`, `ClusterOptions` and storage selection; two ways to configure one silo; Encina would own a subset of Orleans options |

### Chosen Option: **A - Into the application's silo** (recommended, pending the maintainer)

### Rationale

- A wins for any real deployment (clustering and storage differ per environment and are Orleans packages); B wins for a quick-start sample, which the guide covers with a four-line `UseOrleans` snippet. A client-side counterpart `IClientBuilder.AddEncinaOrleansClient()` registers the outgoing filter and the codec for applications that call grains from outside the cluster.

**Class:** Dilemma

</details>

<details>
<summary><strong>3. Whose identity a grain call runs under</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Default: the grain runs anonymous with the tenant verified under Design Choice 6 (`PersistedIdentitySource.External`); `TrustCallerIdentity = true` (logged at startup) makes the incoming filter restore the caller identity from the Orleans request context (`PersistedIdentitySource.Internal`)** | Fails closed: an unconfigured cluster denies every gated request; the trust decision (the silo and gateway ports are private) is explicit and logged, like the other opt-outs of `AGENTS.md` section 3 | Every gated handler fails until the application opts in |
| **B) Always restore the caller identity** | Works out of the box | Orleans cannot tell a silo from a client: any caller able to reach a gateway could set the request-context key and act as any user |
| **C) Always run anonymous, never restore** | Simplest and safest | Handlers behind authorization are unusable inside grains; the application would pass identity data as plain command fields, which the gates do not read |
| **D) The grain runs under a declared service identity (`AddEncinaServiceIdentity`)** | Gates see an authenticated service | Every call is attributed to the service, not to the user; audit loses the actor |

### Chosen Option: **A - Anonymous by default, opt-in to restore** (recommended, pending the maintainer)

### Rationale

- A wins in deployments where the gateway is reachable by more than the application (the default posture); the opt-in then covers a private cluster with mTLS. B wins only when the network is the sole boundary and the maintainer accepts that; C wins if gates never run inside grains; D wins for jobs that act as a service, which `RunAsServiceAsync` already supports for that handler. The tenant is wire data too (the key and the request context are both chosen by the caller): it reaches `configuredTenantId` only after the verification of Design Choice 6.

**Class:** Dilemma

</details>

<details>
<summary><strong>4. The wire contract for `Either<EncinaError, T>`</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) A codec for the carrier plus an `ActorFailure` DTO (code, ADR-062 category, message, string metadata, exception type name; the exception object is dropped); the grain interface methods return `Task<Either<EncinaError, T>>`** | Grain interfaces read like Encina handlers; the wire shape does not depend on the carrier type that ADR-064 replaces, so only the codec's local mapping changes at #3662 | One generalized codec, written and tested |
| **B) Grain methods return a plain `ActorResult<T>` DTO** | No codec for a third-party type | Every grain converts at both ends; the ROP type is not what handlers see |
| **C) Throw on `Left` and let Orleans transport exceptions** | Nothing to serialize | Breaks ROP (`AGENTS.md` section 3): business failures become exceptions |

### Chosen Option: **A - Codec and `ActorFailure`** (recommended, pending the maintainer)

### Rationale

- A wins for handler-shaped grains; B wins if the maintainer prefers no custom codec and accepts conversion code in every grain. The message crosses the wire inside the cluster's trust boundary, but it is never logged or tagged on either side (the logger and the span take the code and the exception type only).

**Class:** Dilemma

</details>

<details>
<summary><strong>5. Where actor state is persisted</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Orleans grain storage (`IPersistentState<TState>`), the storage provider named in `EncinaOrleansOptions.StorageName` and configured by the application with Orleans packages** | No Encina store to keep in step on 10 providers; every Orleans storage package works; matches the issue ("UseDevelopmentStorage, Azure, Redis, etc.") | Durable state is outside the Encina provider matrix, so coherence is by documentation |
| **B) An Encina `IActorStateStore` implemented on the 10 database providers** | One database for everything | New store on 10 providers (`AGENTS.md` section 5) duplicating Orleans storage; no demand recorded |

### Chosen Option: **A - Orleans grain storage** (recommended, pending the maintainer)

### Rationale

- A wins unless the maintainer wants actor state under Encina's tenancy and audit stores; B then needs its own plan across the 10 providers. `SaveStateAsync` writes the state with `WriteStateAsync`; it is a separate write from the handler's own commit, so the plan never describes the two as atomic: a `Left` from `IEncina.Send` leaves the state unwritten, and a successful send followed by a failed write returns the write failure.

**Class:** Dilemma

</details>

<details>
<summary><strong>6. Where the tenant of a grain call is verified</strong></summary>

### Options Considered

A grain key such as `acme/Player/1` is chosen by whoever calls `GetGrain`; the Orleans request context is chosen by the caller too. Orleans cannot tell a silo from a client, so neither carries proof of a tenant.

| Option | Pros | Cons |
|--------|------|------|
| **A) Trusted binding by declaration: with multi-tenancy on, the incoming filter denies (`encina.actors.tenant_not_verified`, `ErrorCategory.Forbidden`) unless `EncinaOrleansOptions.TrustGrainKeyTenant = true` (logged at startup, states that the silo and gateway ports are reachable only by the application)** | No new type; fails closed by default; the assumption is explicit and logged | A forged key is accepted whenever the opt-in is on and the gateway is not private; no per-call proof |
| **B) A + an application-registered `IActorTenantVerifier` (`ValueTask<Either<EncinaError, Unit>> VerifyAsync(string keyTenantId, CancellationToken)`) that the incoming filter calls for every call; the application checks the key tenant against what it authenticated (its own gateway, mTLS client certificate or token); with no verifier and no opt-in the call is denied** | A real per-call check is possible without Encina owning client authentication; the opt-in remains the trivial case | One public interface and a small amount of filter logic; the application must write the verifier |
| **C) Signed grain keys: `OrleansActorSystem` appends an HMAC of the key made with a cluster secret in options (`[JsonIgnore]`, `ToString` override); the incoming filter verifies it** | Verified inside the silo with no network assumption | Secret distribution and rotation to every silo and trusted client; the key format and `ActorGrainKey` injectivity gain a signature part; a larger design |

### Chosen Option: **B - Verifier or declared trust, denied otherwise** (recommended, pending the maintainer)

### Rationale

- B wins because it keeps A's one-line opt-in for private clusters and gives public-gateway deployments a real check; A alone wins if the maintainer wants no new public interface before 1.0; C wins if the cluster must be safe with no network assumption, and then needs its own plan for key management. In every option the caller's tenant carried in the request context is never used to approve a key tenant (it is as forgeable as the key). `TrustCallerIdentity = true` restores the caller identity and declares the same network trust, so it satisfies the check only when the restored tenant equals the key tenant; otherwise the call is denied. With multi-tenancy off the key tenant is the reserved `default` and nothing needs verifying. The denial covers ungated handlers and the grain's persisted state, because it happens before `EncinaGrain` runs.

**Class:** Dilemma

</details>

### Register of defaults and semantics

| Id | Choice | Class | Basis |
|----|--------|-------|-------|
| C1 | `IEncinaActor` is a marker with no members; `Id` and `State` properties of the issue are dropped (a grain interface cannot declare properties, so `IEncinaActor<TState>` would have nothing remotable); the grain exposes `State` to its own code only | Trivial | Orleans grain interface rules (methods return `Task`/`ValueTask`); the XML doc of `IEncinaActor` cites the Orleans documentation page on grain interfaces (the issue's References), not a repository rule |
| C2 | `GetAllActorsAsync` is not part of `IActorSystem`: only activated grains are visible and virtual actors have no enumeration | Trivial | Orleans virtual actor model; the XML doc of `IActorSystem` cites the Orleans Overview page named in the issue's References |
| C3 | `ActorId` is a `readonly struct` with `Create(type, key)` returning `Either`; the type must match `[A-Za-z][A-Za-z0-9_.-]{0,63}` and the key must be non-empty and at most 256 characters; `default(ActorId)` is rejected by every consumer. The issue's `record struct` primary constructor cannot validate and throws are not business-logic errors | Settled | `AGENTS.md` section 3 (ROP, no exceptions for business logic) |
| C4 | Grain key: `{tenant}/{type}/{key}` with each component percent-encoded (`Uri.EscapeDataString`), so `/` cannot occur inside a component and two (tenant, type, key) triples never map to one key; the tenant segment is the escaped tenant id when `MultiTenancyMarker` is registered and the reserved literal `default` when it is not; a tenant whose id equals `default` is refused when multi-tenancy is on | Settled | #1257 (default-tenant rule, reserved `default` segment); `AGENTS.md` section 3 (gates fail closed) |
| C5 | With multi-tenancy on and no tenant in the caller's context, `GetActor` returns `Left` `encina.actors.tenant_required` (`ErrorCategory.FailedPrecondition`) and never invents `default` | Settled | `AGENTS.md` section 3 (missing context denies); ADR-062 |
| C6 | Error codes (prefix `encina.actors.`): `invalid_actor_id` (`Validation`), `tenant_required` (`FailedPrecondition`), `reserved_tenant` (`Validation`), `identity_not_trusted` (`Unauthenticated`, a restored identity was requested but `TrustCallerIdentity` is off), `tenant_not_verified` (`Forbidden`, multi-tenancy is on and neither the verifier nor an opt-in verified the tenant of the key) | Settled | ADR-062; `src/Encina/Errors/ErrorCategory.cs` |
| C7 | Time comes from `TimeProvider` (the grain activation timestamps in logs use none); no `DateTime.UtcNow` | Settled | `AGENTS.md` section 3 |
| C8 | Telemetry: `ActivitySource` and `Meter` named `Encina.Orleans` (owner: this plan); tags carry the grain type name and outcome only; never the actor key, tenant id, user id or message; tenant tag through the #2676 helper when multi-tenancy is on; names lowercase dotted, no `_total` | Settled | ADR-054 settled points 2, 4, 8 and D4 |
| C9 | `SendAsync` never swallows a `Left`: it returns it to the grain method, which returns it to the caller | Settled | `AGENTS.md` section 3 (errors never swallowed) |
| C10 | The in-process runtime is the follow-up issue file `artifacts/issues/actors-in-process-runtime.md`; durable state on the 10 providers is not planned (Design Choice 5) | Settled | `AGENTS.md` section 11 (every identified problem is recorded) |
| C11 | The tenant in a grain key and in the Orleans request context is wire data; it is never accepted without the verification of Design Choice 6 (denied `tenant_not_verified` otherwise) | Dilemma | Design Choice 6 (options A, B, C); `AGENTS.md` section 3 (gates fail closed) |

---

## Implementation Phases

### Delivery order and size

Six pull requests; each builds and passes alone with its own tests (counts include csproj, `Directory.Packages.props`, `Encina.slnx`, PublicAPI, manifests, test-project references and README; table in Research).

| PR | Phase | Content | Files |
|----|-------|---------|-------|
| G1 | 1 | `Encina.Actors` package: `ActorId`, `IEncinaActor`, `IActorSystem`, error codes; the `reserved` EventId row | 14 |
| G2 | 2 | `Encina.Orleans` skeleton: options, `IActorTenantVerifier`, `ActorGrainKey`, silo-builder registration | 15 |
| G3 | 3 | `EncinaGrain<TState>`, identity and tenant-verification filters, codec, `OrleansActorSystem`, TestingHost integration | 14 |
| G4 | 4 | Observability and EventIds | 12 |
| G5 | 5 | Property, contract proof (own TestCluster fixture) and examples in tests | 7 |
| G6 | 6 | READMEs, guide, ADR, INVENTORY, ROADMAP, changelog | 7 |

### Phase 1: `Encina.Actors` Contracts (pull request G1)

<details>
<summary><strong>Tasks</strong></summary>

1. **Create** `src/Encina.Actors/Encina.Actors.csproj` (references `Encina` only; `Microsoft.CodeAnalysis.PublicApiAnalyzers`; `InternalsVisibleTo` for UnitTests, GuardTests, ContractTests, PropertyTests), `PublicAPI.Shipped.txt` (empty header) and `PublicAPI.Unshipped.txt`; **modify** `Encina.slnx` (project entry), `tests/Encina.UnitTests/Encina.UnitTests.csproj` and `tests/Encina.GuardTests/Encina.GuardTests.csproj` (ProjectReference).
2. **Create** `ActorId.cs` (`readonly struct`, `Type`, `Key`, `Create`, `ToString` returning `Type/Key` without any tenant, `IEquatable<ActorId>`), `IEncinaActor.cs` (marker), `IActorSystem.cs` (`Either<EncinaError, TActor> GetActor<TActor>(ActorId id) where TActor : IEncinaActor`; XML example), `ActorErrorCodes.cs` (the five codes of register row C6); **modify** `docs/engineering/eventid-block-registry.md` (new row to the "Block | Area | Owner | Status" table, sorted after the existing rows: `| 80000-80049 | Cloud and integration | #388 plan: Orleans (Encina.Orleans, ids 80000-80003 used) Note: assigned 2026-11-23 | reserved |`; no 10000+ row exists on main, and if an earlier registry pull request already carries this row, G1 drops the edit).
3. **Create** `.github/coverage-manifest/Encina.Actors.json` and the `PublicAPI` lines through `mechanical-fixer`.
4. Tests: new `tests/Encina.UnitTests/Actors/ActorIdTests.cs` (valid, separators, length limits, default struct, equality) and `tests/Encina.GuardTests/Actors/ActorIdGuardTests.cs` (null or whitespace type and key through `Create`).

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 1</strong></summary>

```text
CONTEXT:
Encina (.NET 10, C# 14, pre-1.0). Issue #388, plan docs/plans/virtual-actors-implementation-plan-388.md, phase 1.
New package Encina.Actors holds runtime-neutral contracts only; it must not reference Orleans.

TASK:
1. Create the csproj, ActorId, IEncinaActor, IActorSystem, ActorErrorCodes; add the project to Encina.slnx and the two test csproj references.
2. PublicAPI files and the manifest through mechanical-fixer.
3. Unit and guard tests for ActorId.

KEY RULES:
- ActorId.Create returns Either<EncinaError, ActorId>; no exceptions for invalid input; default(ActorId) is invalid.
- Error codes use the encina.actors. prefix with ADR-062 categories (invalid_actor_id Validation, tenant_required FailedPrecondition,
  reserved_tenant Validation, identity_not_trusted Unauthenticated, tenant_not_verified Forbidden).
- Add the reserved row 80000-80049 Orleans to docs/engineering/eventid-block-registry.md (the table parsed by EventIdRegistryAgreementTests).
- XML docs with examples on every public member; zero warnings.

REFERENCE FILES:
- src/Encina/Errors/EncinaErrorCodes.cs, src/Encina/Errors/ErrorCategory.cs
- src/Encina.DistributedLock/Encina.DistributedLock.csproj (small package layout)
```

</details>

---

### Phase 2: `Encina.Orleans` Skeleton (pull request G2)

<details>
<summary><strong>Tasks</strong></summary>

1. **Create** `src/Encina.Orleans/Encina.Orleans.csproj` (references `Encina`, `Encina.Actors`; `Microsoft.Orleans.Sdk` and `Microsoft.Orleans.Server` through `Directory.Packages.props`; `InternalsVisibleTo` for UnitTests, GuardTests, ContractTests, PropertyTests, IntegrationTests), `PublicAPI.Shipped.txt`, `PublicAPI.Unshipped.txt`; **modify** `Directory.Packages.props` (the `Microsoft.Orleans.*` versions, one line per package), `Encina.slnx`, and the two test csproj files (UnitTests and GuardTests references).
2. **Create** `EncinaOrleansOptions.cs` (`StateName`, `StorageName`, `TrustCallerIdentity = false`, `TrustGrainKeyTenant = false`; `ToString` prints no values that name a secret: there are none), `IActorTenantVerifier.cs` (public: `ValueTask<Either<EncinaError, Unit>> VerifyAsync(string keyTenantId, CancellationToken cancellationToken)`; the application implements it; optional in DI), `ActorGrainKey.cs` (`internal static`; `Encode(ActorId, string? tenantId, bool multiTenant)` and `TryDecode(string, out ...)` as register rows C4 and C5) and `SiloBuilderExtensions.cs` (`AddEncinaOrleans(this ISiloBuilder, Action<EncinaOrleansOptions>? configure = null)`: registers options, `IActorSystem` as `OrleansActorSystem` (scoped lifetimes are not injected into singletons: the system takes `IGrainFactory` and `IRequestContextAccessor`), `TryAdd*` only).
3. `Encina.Orleans.json` manifest and `PublicAPI` lines through `mechanical-fixer`.
4. Tests: new `tests/Encina.UnitTests/Orleans/ActorGrainKeyTests.cs` (encode and decode round trip, `/` and `%` inside components, tenant `default` refused, multi-tenancy off uses `default`), `tests/Encina.GuardTests/Orleans/SiloBuilderExtensionsGuardTests.cs` (null builder; null `configure` is valid and yields the defaults), `tests/Encina.UnitTests/Orleans/AddEncinaOrleansRegistrationTests.cs` (a host built with `UseOrleans` and `AddEncinaOrleans` builds with `ValidateOnBuild` and `ValidateScopes` without starting the silo).

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 2</strong></summary>

```text
CONTEXT:
Issue #388 plan docs/plans/virtual-actors-implementation-plan-388.md, phase 2. G1 merged (Encina.Actors).

TASK:
1. Create the Encina.Orleans project (this plan is its only creator), package versions, options (TrustCallerIdentity and
   TrustGrainKeyTenant, both false), IActorTenantVerifier, ActorGrainKey and AddEncinaOrleans(ISiloBuilder).
2. Manifest and PublicAPI through mechanical-fixer. Unit, guard and registration tests.

KEY RULES:
- The application keeps UseOrleans, clustering and storage; Encina only adds its services and filters (TryAdd*).
- ActorGrainKey is injective (percent-encode each component, join with '/'); reserved tenant segment "default"; never invent it
  when multi-tenancy is on.
- Never inject a scoped service into a singleton.

REFERENCE FILES:
- src/Encina/Abstractions/MultiTenancyMarker.cs, src/Encina/Abstractions/IRequestContextAccessor.cs
- docs/plans/virtual-actors-implementation-plan-388.md (register rows C4 and C5)
```

</details>

---

### Phase 3: Grain Base, Identity Filters, Codec and Actor System (pull request G3)

<details>
<summary><strong>Tasks</strong></summary>

1. **Create** in `src/Encina.Orleans/`: `EncinaGrain.cs` (`public abstract class EncinaGrain<TState> : Grain where TState : class, new()`; constructor `(IPersistentState<TState> state, IEncina encina, IRequestContextScopeFactory scopes)`; `protected TState State`, `protected Task SaveStateAsync()`, `protected Task<Either<EncinaError, TResponse>> SendAsync<TRequest, TResponse>(TRequest request, CancellationToken cancellationToken = default) where TRequest : IRequest<TResponse>` which opens the scope per Design Choice 3 and calls `IEncina.Send`), `OrleansActorSystem.cs` (`IActorSystem`; builds the key with `ActorGrainKey` from the caller's `IRequestContextAccessor`, then `IGrainFactory.GetGrain(typeof(TActor), key)`), `IncomingIdentityCallFilter.cs` and `OutgoingIdentityCallFilter.cs` (write and read one Orleans request-context entry holding a `PersistedRequestIdentity`; read only when `TrustCallerIdentity`; otherwise the entry is ignored, never trusted; before the grain runs the incoming filter verifies the key tenant per Design Choice 6: multi-tenancy off needs nothing, otherwise `IActorTenantVerifier`, or `TrustGrainKeyTenant`, or `TrustCallerIdentity` with a restored tenant equal to the key tenant, else a `Left` `encina.actors.tenant_not_verified`; the caller's tenant carried in the request context is never used to approve a key tenant), and `EitherActorCodec.cs` (the generalized codec for `Either<EncinaError, T>` and `EncinaError`, with the `ActorFailure` DTO of Design Choice 4 declared in the same file, `[GenerateSerializer]`).
2. **Modify** `SiloBuilderExtensions.cs` (add the filters, the codec and the client counterpart `IClientBuilder.AddEncinaOrleansClient()`), `Directory.Packages.props` (`Microsoft.Orleans.TestingHost`), `tests/Encina.IntegrationTests/Encina.IntegrationTests.csproj` (ProjectReference and package), `PublicAPI.Unshipped.txt` and the manifest.
3. Tests: new `tests/Encina.IntegrationTests/Orleans/EncinaGrainIntegrationTests.cs` in a shared `[Collection("Orleans-TestCluster")]` fixture (`OrleansTestCluster.cs` holds the collection definition and the fixture: one in-process `TestCluster` for the family; a `PlayerGrain` that sends a command: `Right` round trip, `Left` round trip with code and category, state written after success only, anonymous by default so a gated handler is denied, restored identity with `TrustCallerIdentity`, a forged key (`GetGrain(type, "other-tenant/Player/1")` straight from the grain factory with multi-tenancy on) is denied `tenant_not_verified` by default and for a rejecting `IActorTenantVerifier`, accepted only with the verifier or `TrustGrainKeyTenant`, two tenants never share a grain), new `tests/Encina.UnitTests/Orleans/IdentityCallFilterTests.cs` and `tests/Encina.GuardTests/Orleans/EncinaGrainGuardTests.cs` (null state, null encina, null scopes, null request).

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 3</strong></summary>

```text
CONTEXT:
Issue #388 plan docs/plans/virtual-actors-implementation-plan-388.md, phase 3. G1 and G2 merged.

TASK:
1. EncinaGrain<TState>, OrleansActorSystem, the incoming and outgoing identity call filters, ActorFailure and the Either codec.
2. TestingHost integration tests in the shared Orleans-TestCluster collection; unit and guard tests.

KEY RULES:
- Default: anonymous (PersistedIdentitySource.External); with multi-tenancy on the incoming filter denies tenant_not_verified unless
  IActorTenantVerifier or TrustGrainKeyTenant verified the key tenant; TrustCallerIdentity restores the caller identity with
  PersistedIdentitySource.Internal (tenant must equal the key tenant) and is logged once at startup.
- Never trust a tenant from the wire (grain key or request context) without that verification. A Left is returned, never swallowed; EncinaError.Message is never logged or tagged.
- State is written with WriteStateAsync after a successful send only; do not call this atomic with the handler's commit.

REFERENCE FILES:
- src/Encina/Identity/IRequestContextScopeFactory.cs, PersistedRequestIdentity.cs, PersistedIdentitySource.cs
- src/Encina/Errors/EncinaError.cs
```

</details>

---

### Phase 4: Observability (pull request G4)

<details>
<summary><strong>Tasks</strong></summary>

1. **Create** `src/Encina.Orleans/Diagnostics/OrleansDiagnostics.cs` (`ActivitySource` and `Meter` named `Encina.Orleans`; spans `encina.orleans.grain_call`; counter `encina.orleans.grain.calls` with tags `encina.orleans.grain_type` and `encina.orleans.outcome` (`ok`, `failed`, `denied`); histogram `encina.orleans.grain.call.duration` in seconds) and `Diagnostics/OrleansLog.cs` (`[LoggerMessage]`: identity trust enabled at startup, call failed by code, state write failed by exception type).
2. **Modify** the grain and the actor system to record (the filters stay as G3 wrote them); **modify** `src/Encina/Diagnostics/EventIdRanges.cs` (field `Orleans`), `docs/engineering/eventid-block-registry.md` (the row added by G1 changes from `reserved` to `registered`, and the `Name | Block | Area | Zone` row for `Orleans` is added), `tests/Encina.UnitTests/Testing/Architecture/EncinaEventIdAllocationTests.cs` (the map gains `["Encina.Orleans"]`), the `PublicAPI.Unshipped.txt` files of `Encina` (the new field) and `Encina.Orleans`, and `.github/coverage-manifest/Encina.Orleans.json`.
3. Tests: new `OrleansDiagnosticsTests.cs` (listener sees spans and instruments; no actor key, tenant or user id in any tag), edit `EncinaGrainIntegrationTests.cs`.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 4</strong></summary>

```text
CONTEXT:
Issue #388 plan docs/plans/virtual-actors-implementation-plan-388.md, phase 4. G3 merged.

TASK:
1. OrleansDiagnostics and OrleansLog inside the claimed EventId block; register the range, the registry row and the allocation map.
2. Tests for spans, instruments and log events.

KEY RULES:
- ADR-021 and ADR-054: names lowercase dotted, no _total, seconds with unit s; no actor key, tenant id, user id or message in a tag or log.
- Instrumentation costs nothing without a listener.

REFERENCE FILES:
- src/Encina/Diagnostics/EventIdRanges.cs, docs/engineering/eventid-block-registry.md
```

</details>

---

### Phase 5: Property, Contract Proof and Examples (pull request G5)

<details>
<summary><strong>Tasks</strong></summary>

1. **Create** `tests/Encina.PropertyTests/Orleans/ActorGrainKeyPropertyTests.cs` (the internal `ActorGrainKey` is reached through the `InternalsVisibleTo` entry for PropertyTests in the G2 csproj) (FsCheck: encode then decode returns the triple; two distinct triples never encode to one key) and `tests/Encina.ContractTests/Actors/ActorSystemContractTests.cs` (`OrleansActorSystem` over a TestCluster honors the `IActorSystem` contract: invalid id is `Left`, missing tenant is `Left` with multi-tenancy on) and `tests/Encina.ContractTests/Actors/OrleansContractCluster.cs` (its own `[Collection("Orleans-Contract")]` definition, `TestCluster` fixture and the grain interface and implementation the contract test calls: xUnit collection definitions are per assembly, so the `Orleans-TestCluster` collection of `Encina.IntegrationTests` cannot be reused); **modify** `tests/Encina.PropertyTests/Encina.PropertyTests.csproj` (ProjectReference to `Encina.Orleans`) and `tests/Encina.ContractTests/Encina.ContractTests.csproj` (ProjectReference to `Encina.Orleans` plus the `Microsoft.Orleans.TestingHost` and `Microsoft.Orleans.Sdk` package references, versions already pinned in `Directory.Packages.props` by G2 and G3), and the manifests `Encina.Orleans.json` and `Encina.Actors.json`.
2. The examples the issue asks for (Player, Device) are the `PlayerGrain` and `DeviceGrain` of the integration tests and the guide.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 5</strong></summary>

```text
CONTEXT:
Issue #388 plan docs/plans/virtual-actors-implementation-plan-388.md, phase 5. G1 to G4 merged.

TASK:
1. FsCheck property tests for ActorGrainKey and a contract test for IActorSystem over the real Orleans implementation, with its own
   TestCluster collection and fixture in Encina.ContractTests (never reuse the IntegrationTests collection).
2. Project references and manifest entries (mechanical-fixer).

KEY RULES:
- Tests execute real code; contract and property tests instantiate real implementations; Shouldly via Encina.Testing.Shouldly.

REFERENCE FILES:
- tests/Encina.UnitTests/Orleans/ActorGrainKeyTests.cs
```

</details>

---

### Phase 6: Documentation and Finalization (pull request G6)

<details>
<summary><strong>Tasks</strong></summary>

1. Through `docs-writer`: `src/Encina.Actors/README.md`, `src/Encina.Orleans/README.md`, `docs/features/virtual-actors.md` (guide: contracts, silo setup, identity trust opt-in, tenant-qualified keys, state persistence choices, examples), the ADR (Orleans integration decisions: the number is assigned when the ADR pull request opens), `docs/INVENTORY.md` and `ROADMAP.md` lines.
2. `changelog.d/388-virtual-actors.added.md` through `mechanical-fixer`.
3. Build with zero warnings; every coverage flag reaches the targets below.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 6</strong></summary>

```text
CONTEXT:
Issue #388 plan docs/plans/virtual-actors-implementation-plan-388.md, phase 6. All code merged.

TASK:
1. Ask docs-writer for the two READMEs, the guide, the ADR draft, INVENTORY and ROADMAP lines; the fragment through mechanical-fixer.
2. Report measured per-file coverage against the plan.

KEY RULES:
- Documentation never types coverage figures by hand (covref markers); public text never names a private client application.

REFERENCE FILES:
- .claude/skills/encina-docs/SKILL.md, docs/features/
```

</details>

---

## Research

### Relevant Standards & Specifications

| Source | Provision | Use in this feature |
|--------|-----------|---------------------|
| Microsoft Orleans documentation | Grain interfaces, `IPersistentState<T>`, call filters, serialization | Contract shape, persistence, filters, codec |
| `AGENTS.md` sections 3 and 5 | ROP; fail closed; 10-provider database rule applies to provider-dependent stores | Either on every method; denies; state outside Encina's stores |
| SPEC-002 REQ-015 | Deferred messages restore their originating actor | `RunRestoredAsync` sources (`Internal`, `External`) |
| ADR-054 | Telemetry names, privacy, tenant tag | Phase 4 |
| ADR-062, ADR-064 | `ErrorCategory`; result union replacing `Either` | Error codes; wire DTO independent of the carrier |
| ADR-021 | EventId ranges | Claim below |

### Existing Encina Infrastructure to Leverage

| Component | Location | Usage in this feature |
|-----------|----------|-----------------------|
| `IRequestContextScopeFactory.RunRestoredAsync` | `src/Encina/Identity/` | Identity scope inside a grain |
| `PersistedRequestIdentity`, `PersistedIdentitySource` | `src/Encina/Identity/` | Identity carried across a call |
| `MultiTenancyMarker`, `IRequestContextAccessor` | `src/Encina/Abstractions/` | Tenant-qualified keys |
| `EncinaError`, `ErrorCategory` | `src/Encina/Errors/` | Wire DTO and error codes |
| `EncinaEventIdAllocationTests` | `tests/Encina.UnitTests/Testing/Architecture/` | Allocation map entry |
| Collection fixtures pattern | `tests/Encina.IntegrationTests/**/Collections.cs` | One shared TestCluster collection |

### Event ID Allocation

| Package | Range | Notes |
|---------|-------|-------|
| `Encina.Orleans` | claim `80000-80049` named `Orleans` (area Cloud and integration, first 50-id slot of the 80000 block) | Reserved by this plan; G4 registers it; packed from 80000, four events used (80000-80003: identity trust at startup, tenant trust mode at startup, call failed by code, state write failed); the `reserved` row is added by G1 (see Phase 1) and G4 changes it to `registered`; #459 uses the same range after this plan |
| `Encina.Actors` | none | Contracts only, no logging |

### Estimated File Count

| PR | Content | New src | Changed src | New tests | Edited tests | Manifest | Other | Files | Lines (est.) |
|----|---------|---------|-------------|-----------|--------------|----------|-------|-------|--------------|
| G1 | Phase 1 | 7 (csproj, 4 types, 2 PublicAPI) | 0 | 2 | 2 (`Encina.UnitTests.csproj`, `Encina.GuardTests.csproj`) | 1 | 2 (`Encina.slnx`, the `reserved` row in `docs/engineering/eventid-block-registry.md`) | 14 | ~700 |
| G2 | Phase 2 | 7 (csproj, 2 PublicAPI, 4 types: options, `IActorTenantVerifier`, `ActorGrainKey`, `SiloBuilderExtensions`) | 0 | 3 | 2 (the same two test csproj files) | 1 | 2 (`Directory.Packages.props`, `Encina.slnx`) | 15 | ~800 |
| G3 | Phase 3 | 5 (grain, actor system, 2 filters, codec with `ActorFailure`) | 2 (`SiloBuilderExtensions.cs`, `PublicAPI.Unshipped.txt`) | 4 (integration, cluster collection and fixture, filter unit, grain guard) | 1 (`Encina.IntegrationTests.csproj`) | 1 | 1 (`Directory.Packages.props`) | 14 | ~1,450 (limit 1,500: if the measured diff is above it, the tenant-verification filter tests move to G4's slot and G4 grows to 13 files) |
| G4 | Phase 4 | 2 (diagnostics, log) | 5 (grain, actor system, `EventIdRanges.cs`, `PublicAPI.Unshipped.txt` of `Encina` with one line per new `EventIdRanges` field (`static readonly Encina.Diagnostics.EventIdRanges.Orleans -> (int Min, int Max)`, the one field of this plan), `PublicAPI.Unshipped.txt` of `Encina.Orleans`) | 1 | 2 (integration test, allocation test) | 1 | 1 (registry doc: the row changes from `reserved` to `registered` and gains the `Name | Block | Area | Zone` row) | 12 | ~450 |
| G5 | Phase 5 | 0 | 0 | 3 (property, contract, `OrleansContractCluster.cs` with its own collection, fixture and grain types) | 2 (`Encina.PropertyTests.csproj`, `Encina.ContractTests.csproj`) | 2 | 0 | 7 | ~420 |
| G6 | Phase 6 | 0 | 0 | 0 | 0 | 0 | 7 (2 READMEs, guide, ADR, `INVENTORY.md`, `ROADMAP.md`, changelog) | 7 | ~500 |
| **Total** | | **21** | **7** | **13** | **9** | **6** | **13** | **69** | **~4,300** |

Columns sum: 21 + 7 + 13 + 9 + 6 + 13 = 69 = 14 + 15 + 14 + 12 + 7 + 7. The grain-key tests use `InternalsVisibleTo` entries written inside the new csproj of G2, so no csproj edit is added. No existing interface changes, so there are no test doubles to update; `IActorSystem` has one implementer (`OrleansActorSystem`) and `IEncinaActor` is a marker. The orchestrator recounts these with a grep on the branch when it writes each brief.

Per-file coverage targets (no manifest exists for the new packages; the implementing PRs create `Encina.Actors.json` and `Encina.Orleans.json` with these per-file targets):

| File | Unit | Guard | Contract | Property | Integration | Justification |
|------|------|-------|----------|----------|-------------|---------------|
| `Encina.Actors/ActorId.cs` | 95 | 70 | 0 | 0 | 0 | Table-driven validation and equality; guard covers the null and whitespace arguments of `Create` |
| `Encina.Actors/ActorErrorCodes.cs` | 0 | 0 | 0 | 0 | 0 | Constants only, no executable code |
| `Encina.Orleans/ActorGrainKey.cs` | 95 | 40 | 0 | 80 | 0 | Encode and decode branches are unit tested; the property test proves injectivity and round trip |
| `Encina.Orleans/SiloBuilderExtensions.cs` | 85 | 60 | 0 | 0 | 40 | Registration and null checks by unit, guard and registration tests; the silo path by the TestCluster |
| `Encina.Orleans/EncinaGrain.cs` | 50 | 50 | 0 | 0 | 80 | Behavior needs a running grain, covered by the TestCluster tests; guard covers the constructor |
| `Encina.Orleans/OrleansActorSystem.cs` | 70 | 60 | 60 | 0 | 60 | Key building and error paths unit tested; the contract test and the cluster cover the real grain factory |
| `Encina.Orleans/IActorTenantVerifier.cs` | 0 | 0 | 0 | 0 | 0 | Interface with no executable code; its behavior is covered through the incoming filter rows |
| `Encina.Orleans/IncomingIdentityCallFilter.cs`, `OutgoingIdentityCallFilter.cs` | 85 | 50 | 0 | 0 | 60 | Trust on and off, missing and malformed entries and every tenant-verification branch (verifier accepts, rejects, absent; opt-in; forged key) are unit tested; the cluster proves the wiring and the forged-key denial |
| `Encina.Orleans/EitherActorCodec.cs`, `ActorFailure.cs` | 80 | 0 | 0 | 0 | 80 | Codec branches exercised by round trips in the cluster and unit tests; DTO has no validated parameter |
| `Encina.Orleans/Diagnostics/OrleansDiagnostics.cs` | 80 | 0 | 0 | 0 | 0 | Instruments reached by the listener test; internal static, no validated parameter |

---

## Combined AI Agent Prompts

<details>
<summary><strong>Full combined prompt for all phases</strong></summary>

```text
You are implementing issue #388 - virtual actors (Encina.Actors contracts and the Encina.Orleans integration) - in six small pull
requests (G1 to G6), in order. #459 later extends Encina.Orleans; do not implement handler-as-grain mapping, saga grains or reminders.

PROJECT CONTEXT:
- Encina .NET 10 / C# 14, pre-1.0, no [Obsolete], no compatibility layers. Edit with Edit/Write only; PublicAPI, manifests and
  changelog through mechanical-fixer; READMEs, the guide and the ADR through docs-writer.
- The application keeps UseOrleans, clustering and storage; Encina.Orleans adds services and filters to its silo.
- Default identity inside a grain is anonymous; a multi-tenant call is denied unless IActorTenantVerifier or the logged opt-in TrustGrainKeyTenant verified the key tenant (a key is chosen by the caller); TrustCallerIdentity is an explicit, logged opt-in.

IMPLEMENTATION OVERVIEW:
1. G1: Encina.Actors (ActorId, IEncinaActor marker, IActorSystem, error codes).
2. G2: Encina.Orleans skeleton (options, ActorGrainKey, AddEncinaOrleans on ISiloBuilder, package versions).
3. G3: EncinaGrain<TState>, identity filters, ActorFailure and the Either codec, OrleansActorSystem, TestingHost tests.
4. G4: Encina.Orleans ActivitySource, Meter and [LoggerMessage] in the claimed EventId block.
5. G5: property and contract proof. 6. G6: documentation.

KEY PATTERNS:
- ROP everywhere (Either); never swallow a Left; EncinaError.Message never reaches a log or tag.
- Grain keys are tenant-qualified and injective; the reserved "default" segment is never invented with multi-tenancy on.
- Guard tests for every public constructor and method parameter; a null optional parameter gets a valid-result test.
- Durable state is Orleans grain storage; never call the state write and the handler commit atomic.

REFERENCE FILES:
- src/Encina/Identity/IRequestContextScopeFactory.cs, src/Encina/Errors/EncinaError.cs, src/Encina/Diagnostics/EventIdRanges.cs
- the plan of #459 once it exists (shared Encina.Orleans package)
```

</details>

---

## Cross-Cutting Integration Matrix

| # | Function | Status | Notes |
|---|----------|--------|-------|
| 1 | Caching | ⏭️ | Grain state is its own cache (an activated grain keeps its state in memory); an Encina cache decorator over grain reads is deferred until a use case exists (the issue also defers it), no issue opened because no consumer is named |
| 2 | OpenTelemetry | ✅ Phase 4 | `Encina.Orleans` source and meter; tenant tag through #2676; never the actor key |
| 3 | Structured Logging | ✅ Phase 4 | `[LoggerMessage]` in the claimed block; code and exception type only |
| 4 | Health Checks | ⏭️ | Silo health is Orleans' `IHealthCheckParticipant` and `AddHealthChecks`; an `IEncinaHealthCheck` over cluster membership is deferred to #459, which owns Aspire and cluster concerns |
| 5 | Validation | ✅ Phase 1 | `ActorId.Create` validates type and key; grain request validation runs through the normal pipeline inside `IEncina.Send` |
| 6 | Resilience | ❌ N/A | Orleans owns retries and reactivation; Encina adds no second retry layer around grain calls |
| 7 | Distributed Locks | ❌ N/A | Grains are single-threaded by design; no shared-state lock is needed |
| 8 | Transactions | ❌ N/A | State writes are Orleans storage writes; `TransactionPipelineBehavior` still applies to the handler inside `Send`; Orleans transactional grains are not used |
| 9 | Idempotency | ❌ N/A | Grain calls are at-most-once without Orleans' own dedup; a duplicate-safe command uses the inbox behavior inside `Send`; no new mechanism here |
| 10 | Multi-Tenancy | ✅ Phases 2-3 | Tenant-qualified injective grain keys; fail closed without a tenant; a tenant from the grain key or request context is wire data and is denied unless verified (Design Choice 6) |
| 11 | Module Isolation | ❌ N/A | Grains are not module-scoped in this plan; module-scoped silos are a deployment concern recorded in the guide |
| 12 | Audit Trail | ⏭️ | Gated handlers inside `Send` audit through the existing behaviors under the restored identity (opt-in); a grain-level audit of calls is not added |

---

## Prerequisites & Dependencies

### Required Prerequisites

| Prerequisite | Status | Notes |
|-------------|--------|-------|
| Orleans 10.x package versions on nuget.org | Available (latest stable seen on 2026-11-23: 10.4.2) | Pinned in G2; confirm `net10.0` per package |
| `RunRestoredAsync` identity scopes | Merged | `src/Encina/Identity/` |

### Recommended (Not Blocking)

| Dependency | Issue | Notes |
|-----------|-------|-------|
| Tenant tag helper | #2676 | G4 adds the tenant tag when it has merged |
| Result union replacing `Either` | #3662 (ADR-064) | The codec's local mapping changes; the wire DTO does not |
| Handlers as grains, saga grains, reminders | #459 | Builds on `Encina.Orleans` after G3 |
| In-process actor runtime | issue file `artifacts/issues/actors-in-process-runtime.md` | Design Choice 1, option B |

---

## Next Steps

1. Link this plan from #388 and ask the maintainer to answer Design Choices 1 to 6 when work starts; tell #459's author that this plan owns the `Encina.Orleans` project (Design Choice 1).
2. Implement G1 to G6 in order (G4 and G5 are independent after G3).
3. Maintainer action: amend the acceptance criteria of #388 that name `IEncinaActor.Id`, `IEncinaActor<TState>`, `GetAllActorsAsync`, `AddEncinaActors` and ">85% coverage" to the outcome of this plan (this plan never comments on an issue).
