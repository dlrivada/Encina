# Implementation Plan: `Encina.Dapr` — sidecar client, service invocation and state-store backed Outbox, Inbox and Saga stores

> **Issue**: [#387](https://github.com/dlrivada/Encina/issues/387)
> **Type**: Feature
> **Complexity**: High (11 phases, 9 pull requests of 13, 14, 13, 15, 15, 10, 10, 12 and 7 files)
> **Estimated Scope**: ~1,300 lines of production code + ~3,000 lines of tests
> **Family**: Aspire (aspire-2): #424, #425, #382, #391. Related: #242 (Dapr PubSub transport, `status:proposal`, same package name `Encina.Dapr`), #388 (Orleans virtual actors), #607 and #382 (service discovery), #416 (the hosting package). The closed duplicates #420 (CloudEvents and Aspire AppHost extensions) and #450 (secrets by attribute, cross-service handler invocation) were carried into this issue. **This plan owns the `Encina.Dapr` and `Encina.Dapr.StateStore` packages and EventIds 878-889.**

---

## Summary

The issue asks for five packages (core, state store, pub/sub, workflows, actors), state-store backed Outbox, Inbox and Saga stores, pub/sub as a transport, service invocation, optional workflows, tracing, and (from the duplicates) CloudEvents, an Aspire AppHost extension, secrets and locks. State of `origin/main` (`098caa920`), verified by reading and grep, and of the NuGet feed (read 2026-11-20):

| Item of the issue | Today | Owner of what remains |
|-------------------|-------|-----------------------|
| `Encina.Dapr` and every Dapr class of the sketches (`DaprOutboxStore`, `DaprMessagePublisher`, `IServiceInvoker`, `IMessagePublisher`) | Missing. `Directory.Packages.props` pins `Dapr.Client` and `Dapr.AspNetCore` 1.16.1 but no project references them (grep); NuGet's current stable is 1.19.1. The sketched `IMessagePublisher` and `IServiceInvoker` do not exist: transports expose `I{Transport}MessagePublisher` (for example `INATSMessagePublisher`), stores are `IOutboxStore`, `IInboxStore`, `ISagaStore` (`src/Encina.Messaging/`) | **This plan** for core, state stores and invocation |
| `Encina.Dapr.PubSub` and `IDaprMessagePublisher` | #242 proposes `Encina.Dapr` for Pub/Sub with `IDaprMessagePublisher`, `IDaprMessageSubscriber`; AGENTS.md section 5 lists Dapr among the planned transports | Design Choice 1 |
| Outbox, Inbox, Saga on the Dapr state API | Missing; the stores have staged writes and `SaveChangesAsync` (`IOutboxStore.cs`, `ISagaStore.cs`) | **This plan** (Design Choices 2 and 3) |
| Service invocation | Missing | **This plan** (Design Choice 4, Settled) |
| Workflows, actors, bindings, secrets by attribute | Missing; `Dapr.Workflow` and `Dapr.Actors` exist on NuGet (1.19.1); `Encina.Security.Secrets` has its own providers | Design Choice 5 and an issue file |
| Distributed lock building block | The lock set is closed: InMemory, Redis, SqlServer, PostgreSQL, MySQL, Azure Blob, DynamoDB (AGENTS.md section 5, SPEC-000 DEC-009); not carried by the duplicates | Design Choice 6 (not delivered) |
| State store as cache backend | The caching set is the eight providers of AGENTS.md section 5 | Design Choice 6 (not delivered) |
| Aspire AppHost extension (from #420) | `Aspire.Hosting.Dapr` stops at 9.1.0 (not updated for Aspire 13); the AppHost integration is `CommunityToolkit.Aspire.Hosting.Dapr` (third party, 13.0.0 stable) | Design Choice 7 |
| OpenTelemetry tracing | `DaprClient` and the sidecar propagate W3C trace context; Encina adds spans (ADR-054) | **This plan** |

Differences from the issue text: (1) the sketched `DaprOutboxStore.AddAsync` writes one key per message and ignores the contract's `GetPendingMessagesAsync` (a query), `MarkAsExhaustedAsync` and `RequeueExhaustedAsync`; the real stores need the Dapr **query API**, whose availability depends on the component (Design Choice 2). (2) A Dapr state transaction is atomic inside one state store component only; the plan never claims atomicity with business data held elsewhere, and the orchestrators never call `SaveChangesAsync` themselves (the commit owner is `SagaOrchestrator.CommitAsync` of #720). (3) The `config.DaprHttpEndpoint = "http://localhost:3500"` default is loopback: it needs the loopback opt-in of `EndpointPolicy` (Design Choice 8).

**Delivered**: `Encina.Dapr` (`EncinaDaprOptions`, validator, `AddEncinaDapr`, `DaprSidecarHealthCheck`, `IDaprServiceInvoker`, error codes, log messages); `Encina.Dapr.StateStore` (`OutboxStoreDapr`, `InboxStoreDapr`, `SagaStoreDapr`, key builder, start validation of the component, `AddEncinaOutboxDapr`, `AddEncinaInboxDapr`, `AddEncinaSagaDapr`); a declaration-only `WithEncinaDapr` in `Encina.Aspire.Hosting` (Design Choice 7, if confirmed); EventIds 878-886.

**Not in scope**: Pub/Sub (Design Choice 1), workflows, actors, bindings and secrets (Design Choice 5), locks and a cache backend (Design Choice 6), a Dapr scheduler.

**Affected packages**: `Encina.Dapr` (new), `Encina.Dapr.StateStore` (new), `Encina.Aspire.Hosting` (#416, small edit), `Encina` (core: two EventId ranges and PublicAPI lines).

**Provider category**: Dapr is not one of the 10 database providers of AGENTS.md section 5: the state-store stores are a provider of the messaging stores through the shared `IOutboxStore`, `IInboxStore` and `ISagaStore` contracts (provider coherence: the contracts and options stay in `Encina.Messaging`; switching to Dapr is a DI registration change). The database matrix therefore does not apply; the contract tests of the stores run against the Dapr stores as one more implementation.

**Scope check (SPEC-000 section 6, DEC-009)**: Dapr runs locally (`daprd` and a Redis state component in containers), AGENTS.md lists Dapr among the six planned transports of 1.0, and nothing here needs a cloud service: in 1.0 scope.

---

## Design Choices

<details>
<summary><strong>1. Package layout and the Pub/Sub boundary with #242 (Dilemma D1)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `Encina.Dapr` (client, options, health, invocation) and `Encina.Dapr.StateStore` here; the Pub/Sub transport is #242's, built on `Encina.Dapr` as `Encina.Dapr.PubSub`** | A transport follows the transport category's contract (send, subscription, DLQ, metadata) and is planned with the other transports; one owner for each package | #387's "Pub/Sub as transport" acceptance item is satisfied by #242, not here |
| **B) This plan also delivers `Encina.Dapr.PubSub` and #242 is closed as a duplicate** | One plan covers the issue's list | Plans a transport outside the transport family's contract work; #242 has no plan |
| **C) One package `Encina.Dapr` with everything** | One dependency | Pulls state, invocation and Pub/Sub for any use of one; no pay-for-what-you-use |

### Chosen Option: **A — core and state store here, Pub/Sub through #242** (recommended, pending the maintainer)

### Rationale

A wins because AGENTS.md section 5 asks every transport to implement the same four capabilities, which the transport plans define together; B wins if the maintainer prefers one Dapr plan and retires #242; C only for a very small footprint. Under every option the shared `AddEncinaDapr` registration, options and health check of this plan are the foundation #242 needs.

**Class:** Dilemma

</details>

<details>
<summary><strong>2. State stores and the query API (Dilemma D2)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) All three stores (Outbox, Inbox, Saga) use key-value reads and writes plus the Dapr state **query API** for the "pending", "stuck", "expired" and "exhausted" lookups; registration validates at start that the configured component is queryable (and transactional) and fails closed when it is not** | The full contract of each store; the issue's acceptance item delivered | The query API is limited to some components and its maturity must be re-read at Phase 1; users pick a queryable component |
| **B) Encina maintains its own index keys (a status list per store) with ETag concurrency** | Works on any component | A hot index key becomes a contention point; a second source of truth to repair |
| **C) Only Saga and Inbox now (key-addressable), Outbox later** | Smaller first delivery | The issue's Outbox store is deferred, and the Saga store still needs a stuck-saga query |

### Chosen Option: **A — the query API with a start check** (recommended, pending the maintainer)

### Rationale

A wins when users choose a queryable component (PostgreSQL, MongoDB, Redis with search) and want the stores' full contract; B wins on a component without query support and light load; C if the maintainer wants the smallest first step. Phase 1 re-reads the query API's status in the Dapr documentation for the pinned version before any store code; if it is still alpha the Dilemma is re-asked, because shipping an alpha dependency in the 1.0 contract is the risk.

**Class:** Dilemma

</details>

<details>
<summary><strong>3. Store semantics: staging, commit, keys and tenants</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) The stores stage writes in a per-scope list and apply them in `SaveChangesAsync` through one `ExecuteStateTransactionAsync` (all-or-nothing inside the component); keys are `encina:{store}:{tenantSegment}:{id}` where every part is percent-encoded for `%` and `:`; with multi-tenancy on, a missing tenant denies (`encina.dapr.tenant_required`, `ErrorCategory.Forbidden`); without it the segment is the reserved `default`, and a real tenant named `default` is rejected; optimistic concurrency uses ETags and a mismatch is `encina.dapr.state_conflict` (`ErrorCategory.Conflict`)** | The staged contract of the interfaces; injective keys (no two tenant and id pairs map to one key); fail closed | A transaction needs a transactional component (checked at start) |
| **B) Write immediately on `AddAsync`** | Simpler | Breaks the staged contract the orchestrators rely on |
| **C) Keys without tenant** | Shorter | Tenants collide and leak across each other |

### Chosen Option: **A — staged transaction, injective tenant-qualified keys, ETags**

### Rationale

The staged shape is the contract of `IOutboxStore.SaveChangesAsync` and `ISagaStore.SaveChangesAsync`; the key rule follows the repository's tenancy rules (a missing context denies, the `default` segment is reserved, ids stay injective). The same rule holds under every option of Design Choices 1, 2 and 5 because they change which stores exist, not how a key is built. The transaction is atomic only inside the state store component; it is never described as atomic with business data stored elsewhere.

**Class:** Settled
**Settled by:** AGENTS.md section 3 (compliance and security gates fail closed; Errors never swallowed in background infrastructure) and section 6 item 10 (multi-tenancy); `src/Encina.Messaging/Outbox/IOutboxStore.cs` and `Sagas/ISagaStore.cs` (staged writes and `SaveChangesAsync`)

</details>

<details>
<summary><strong>4. Service invocation: an allowlist of application ids and methods</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `IDaprServiceInvoker.InvokeAsync<TRequest, TResponse>(string appId, string method, TRequest request, CancellationToken)` returns `Either<EncinaError, TResponse>`; `appId` must be in `EncinaDaprOptions.AllowedAppIds` (`encina.dapr.app_id_not_allowed`, `ErrorCategory.Forbidden`); `method` must match a safe pattern (letters, digits, `-`, `_`, `/`, no `..`; `encina.dapr.invalid_method`, `ErrorCategory.Validation`); a sidecar failure is `encina.dapr.invocation_failed` (`ErrorCategory.UpstreamFailure`) or `encina.dapr.sidecar_unavailable` (`ErrorCategory.Unavailable`); cancellation propagates and is never a `Left`** | An application id or a method taken from untrusted data cannot reach an arbitrary service; the same fail-closed rule as the repository's endpoints | The caller lists the services up front |
| **B) Any `appId` and method string** | The sketch | A confused deputy: request data could target any service in the cluster |

### Chosen Option: **A — allowlist, validated method, categorized errors**

### Rationale

Dapr resolves an application id to an address inside the cluster, so an unchecked id is a server-side request forgery vector; AGENTS.md section 3 requires gates to fail closed, and the allowlist denies when it is empty. This holds whatever the Dilemmas decide, because it concerns only the invoker.

**Class:** Settled
**Settled by:** AGENTS.md section 3 (gates fail closed; `EncinaError.Message` never in logs; ROP); ADR-062 (error categories)

</details>

<details>
<summary><strong>5. Workflows, actors, bindings, secrets and cross-service handler invocation (Dilemma D5)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Not delivered here: each becomes an issue file; workflows are the issue's optional item, actors overlap #388 (virtual actors), bindings are scheduled-message triggers outside the scheduling pattern's current API, secrets belong to `Encina.Security.Secrets`, cross-service handler invocation needs a remote protocol (the same open question as `SendToService` in #382)** | The plan delivers what can be built correctly now; no half features | Several acceptance bullets (workflows, secrets, handler invocation) stay open as issues |
| **B) Deliver workflows (`Encina.Dapr.Workflows`) and secrets now** | Closer to the issue list | Doubles the plan; `Dapr.Workflow` is a new dependency with its own runtime model; secrets need a provider contract decision |
| **C) Deliver all of them** | Full list | Far beyond one plan |

### Chosen Option: **A — defer each with an issue file** (recommended, pending the maintainer)

### Rationale

A wins because each omitted bullet is its own feature with its own contract questions (and #388 may own actors); B wins if the maintainer wants workflows as the Saga backend soon; C is a program, not a plan. The issue file of this run covers workflows, actors, bindings and secrets together.

**Class:** Dilemma

</details>

<details>
<summary><strong>6. Locks and a cache backend are not delivered</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) No Dapr `IDistributedLockProvider` and no Dapr cache provider** | Keeps the closed sets of AGENTS.md section 5 (seven lock providers, eight cache providers) | The issue's matrix marks both as included; this plan corrects the matrix |
| **B) Add them** | Matches the matrix | Breaks "exactly these 7" and the eight-cache set; each needs contract and integration tests across the category |

### Chosen Option: **A — keep the sets closed**

### Rationale

The lock set is stated as exact by SPEC-000 DEC-009 and AGENTS.md section 5, and the caching set is the eight providers named there; neither the carried duplicates nor the acceptance criteria of #387 ask for them. A maintainer who wants a Dapr lock changes the spec first.

**Class:** Settled
**Settled by:** AGENTS.md section 5 (lock providers: exactly these 7; caching: 8 providers); SPEC-000 DEC-009

</details>

<details>
<summary><strong>7. Aspire AppHost extension (Dilemma D7)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) A declaration only in `Encina.Aspire.Hosting`: `WithEncinaDapr(...)` writes `Encina__Aspire__Dapr__*` (state store name, app id); the user adds the sidecar with the community toolkit's `WithDaprSidecar()`; no third-party package in the hosting package** | No dependency on a third-party AppHost package; the declaration stays in the #416 contract | The user references the toolkit |
| **B) A new package `Encina.Aspire.Hosting.Dapr` referencing `CommunityToolkit.Aspire.Hosting.Dapr`** | One call adds the sidecar and the declaration | A third-party dependency with its own release train |
| **C) Not delivered** | Smallest | The #420 carry-over is dropped |

### Chosen Option: **A — declaration only** (recommended, pending the maintainer)

### Rationale

A wins when the declaration is what Encina needs (the application reads which components and app id to use) and the toolkit stays the user's choice; B wins if the maintainer wants a single call; C if Aspire integration of Dapr is out of this issue. `Aspire.Hosting.Dapr` is not an option: its last release is 9.1.0.

**Class:** Dilemma

</details>

<details>
<summary><strong>8. Endpoints, the API token and the loopback default (Dilemma D8)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `AllowLocalEndpoints` defaults to true: the sidecar is loopback by definition (`EndpointPolicy` documents loopback as "for local development and sidecars"); link-local, metadata and unspecified hosts stay rejected with no opt-out; the API token is `[JsonIgnore]` and `ToString()` prints no value** | Works out of the box with `http://localhost:3500` | A non-sidecar loopback target is accepted |
| **B) Default false, as the NATS options do** | Strictest | The default endpoint fails validation until the user sets the flag |

### Chosen Option: **A — loopback allowed by default for the sidecar** (recommended, pending the maintainer)

### Rationale

A wins because a Dapr sidecar is local by design and a failing default is a poor first experience; B wins where reviewers want every loopback target to be an explicit decision. Both options keep the token rule (AGENTS.md section 3) and the always-rejected host kinds.

**Class:** Dilemma

</details>

---

## Implementation Phases

### Phase 1: Package skeleton and options (pull request 1)

> **Goal**: The `Encina.Dapr` project, its options and validator, with the client pin current.

<details>
<summary>Tasks</summary>

1. Re-read the Dapr documentation for the pinned version (state query API status, transaction support per component) and record it in the pull request; set `Dapr.Client` and `Dapr.AspNetCore` to the current stable in `Directory.Packages.props` when the repository still builds (the pin is 1.16.1; the feed has 1.19.1); `Dapr.AspNetCore` is referenced only if a later phase needs it.
2. `src/Encina.Dapr/Encina.Dapr.csproj` (`PackageId`, XML docs, `Dapr.Client`, `InternalsVisibleTo` for the unit, guard, contract and integration test projects, PublicAPI analyzer), PublicAPI files, the `Encina.slnx` entry, the project references of the test projects, `.github/coverage-manifest/Encina.Dapr.json`.
3. `EncinaDaprOptions.cs` (public sealed): `HttpEndpoint` (default `http://localhost:3500`), `GrpcEndpoint` (default `http://localhost:50001`), `ApiToken` (`string?`, `[JsonIgnore]`, `ToString()` override), `AllowLocalEndpoints` (Design Choice 8), `AllowedAppIds` (`IReadOnlyCollection<string>`, empty by default: the invoker denies), `InvocationTimeout` (default 30 seconds), `ProviderHealthCheck` (`ProviderHealthCheckOptions`, the type the NATS options use).
4. `EncinaDaprOptionsValidator.cs` (internal): endpoints through `EndpointValidator` with the scheme list `http`, `https`; the app ids must match `[a-z0-9-]{1,63}`; a warning once when `AllowLocalEndpoints` is set.
5. Tests: options and validator unit tests, guard tests; PublicAPI and manifest through `mechanical-fixer`.

</details>

<details>
<summary>Prompt for AI Agents — Phase 1</summary>

```text
CONTEXT:
- Issue #387; plan docs/plans/dapr-integration-implementation-plan-387.md. D1, D2, D5, D7 and D8 are recommended, pending the maintainer: follow the Maintainer Decisions section if it exists.
- Directory.Packages.props pins Dapr.Client and Dapr.AspNetCore 1.16.1; no project references them yet.

TASK:
Create the Encina.Dapr project, options and validator with tests; update the pins when the build stays green.

KEY RULES:
- The ApiToken property is [JsonIgnore] and ToString() prints no value (AGENTS.md section 3).
- Endpoints go through src/Encina/Validation/EndpointValidator.cs; link-local, metadata and unspecified hosts have no opt-out.
- AllowedAppIds is empty by default: deny until configured.

REFERENCE FILES:
- src/Encina.NATS/EncinaNATSOptions.cs, EncinaNATSOptionsValidator.cs, ServiceCollectionExtensions.cs
- src/Encina/Validation/EndpointPolicy.cs
- .github/coverage-manifest/Encina.Aspire.Testing.json (manifest shape)
```

</details>

---

### Phase 2: Registration, health check and logs (pull request 2)

> **Goal**: `AddEncinaDapr` registers the client and a health check; the core range is registered.

<details>
<summary>Tasks</summary>

1. `ServiceCollectionExtensions.AddEncinaDapr(this IServiceCollection, Action<EncinaDaprOptions>? = null)`: options with `ValidateOnStart`, the validator with `TryAddEnumerable`, `DaprClient` as a singleton built from the options (`DaprClientBuilder` with the HTTP and gRPC endpoints and the API token), `TryAdd` so an application's own client wins; the health check registered when `ProviderHealthCheck.Enabled`.
2. `Health/DaprSidecarHealthCheck.cs` (`IEncinaHealthCheck`; `DefaultName` const, `Tags` static array): `DaprClient.CheckHealthAsync`; unhealthy results carry the error code only.
3. `DaprErrors.cs` (internal): the codes of Design Choices 3 and 4 and `encina.dapr.sidecar_unavailable`, each with `EncinaErrors.Create(code, category: ErrorCategory.X, message: <constant text>)` and one category per code.
4. `Diagnostics/DaprLog.cs`: 878 sidecar health probe failed (Warning: error code), 879 invocation failed (Warning: error code), 880 invocation denied (Warning: a fixed reason, not the app id). `EventIdRanges.DaprCore = (878, 883)` registered first, the allocation test map, the `Encina` PublicAPI lines; ids 878-880 packed, 881-883 spare.
5. Tests: unit (health check healthy and unhealthy with a fake client), guard, a DI test with `ValidateOnBuild` and `ValidateScopes`; PublicAPI and manifest edits through `mechanical-fixer`.

</details>

<details>
<summary>Prompt for AI Agents — Phase 2</summary>

```text
CONTEXT:
- Issue #387, Phase 2 (pull request 2). Phase 1 is merged.

TASK:
Write AddEncinaDapr, the sidecar health check, the error holder, the log class and the EventId range, with tests.

KEY RULES:
- Register EventIdRanges.DaprCore first; ids 878-880 packed inside 878-883.
- Singleton services take no scoped dependency; TryAdd so the application's DaprClient wins.
- Error codes carry a named ErrorCategory; logs carry codes, never EncinaError.Message or an app id.

REFERENCE FILES:
- src/Encina.NATS/ServiceCollectionExtensions.cs and Health/NATSHealthCheck.cs
- src/Encina/Diagnostics/EventIdRanges.cs
- tests/Encina.UnitTests/Testing/Architecture/EncinaEventIdAllocationTests.cs
```

</details>

---

### Phase 3: Service invocation (pull request 3)

> **Goal**: `IDaprServiceInvoker` with the allowlist and categorized errors.

<details>
<summary>Tasks</summary>

1. `IDaprServiceInvoker.cs` and `DaprServiceInvoker.cs`: Design Choice 4; the timeout from `InvocationTimeout` is a linked token source and a timeout is `ErrorCategory.Timeout` (`encina.dapr.timeout`), while the caller's cancellation propagates; the HTTP verb is POST; the activity of Phase 9 wraps the call.
2. Registration edit in `AddEncinaDapr`.
3. Tests: unit with a fake sidecar (WireMock through `Encina.Testing.WireMock`): allowed and denied ids, invalid methods, timeout, cancellation, sidecar failure; guard; an integration test against a real `daprd` container in a `[Collection("Dapr")]` fixture (`DaprFixture` and its collection file) with a second container as the target app.
4. PublicAPI, manifest and the integration project reference through `mechanical-fixer`.

</details>

<details>
<summary>Prompt for AI Agents — Phase 3</summary>

```text
CONTEXT:
- Issue #387, Phase 3 (pull request 3). Phase 2 is merged.

TASK:
Write the invoker, its registration and tests including the daprd integration fixture.

KEY RULES:
- Empty AllowedAppIds denies every call; the app id is never logged.
- Cancellation propagates; only the linked timeout becomes a Timeout Left.
- Integration tests use a shared [Collection("Dapr")] fixture and [Trait("Category", "Integration")].

REFERENCE FILES:
- src/Encina.Testing.WireMock
- tests/Encina.IntegrationTests (collection fixtures and Collections.cs)
```

</details>

---

### Phase 4: State-store package skeleton and keys (pull request 4)

> **Goal**: `Encina.Dapr.StateStore`, its options, the key builder and the start validation.

<details>
<summary>Tasks</summary>

1. `src/Encina.Dapr.StateStore/Encina.Dapr.StateStore.csproj` (references `Encina.Dapr` and `Encina.Messaging`), PublicAPI files, the `Encina.slnx` entry, test project references, manifest.
2. `DaprStateStoreOptions.cs`: `StoreName` (required), `Consistency` (default strong), `TtlSeconds` (`int?`), validated.
3. `DaprStateKey.cs` (internal static): `Build(string store, string? tenantId, bool tenancyEnabled, string id)` returning `Either<EncinaError, string>`: percent-encodes `%` and `:` in each part, rejects the tenant `default` when tenancy is on, denies a missing tenant when tenancy is on (`encina.dapr.tenant_required`, `ErrorCategory.Forbidden`); a unit test proves injectivity over generated pairs.
4. `DaprStateStoreStartCheck.cs` (internal): calls the Dapr metadata endpoint, requires the named component to exist and, per the stores registered, to support transactions and queries (`encina.dapr.state_query_unsupported`, `ErrorCategory.FailedPrecondition`); a failure stops the host with the code only.
5. Tests: unit (keys, options, check with a fake client), guard, a property test (FsCheck, `Encina.Testing.FsCheck`) for key injectivity and round trip; PublicAPI and manifest through `mechanical-fixer`.

</details>

<details>
<summary>Prompt for AI Agents — Phase 4</summary>

```text
CONTEXT:
- Issue #387, Phase 4 (pull request 4). D2 is recommended, pending the maintainer.

TASK:
Create the state-store project, options, key builder, start check and tests.

KEY RULES:
- Keys are injective and tenant-qualified; the reserved default segment is never a real tenant; a missing tenant denies.
- Errors carry a named ErrorCategory; the start check throws with the code only.
- No tenant id or key in logs.

REFERENCE FILES:
- src/Encina.Messaging/Outbox/IOutboxStore.cs, Inbox/IInboxStore.cs, Sagas/ISagaStore.cs
- src/Encina.Tenancy
- docs/plans (tenant key conventions of the multitenancy plans)
```

</details>

---

### Phase 5: The Saga store (pull request 5)

> **Goal**: `SagaStoreDapr` with ETags and the query API, plus the registration and EventIds.

<details>
<summary>Tasks</summary>

1. `SagaStoreDapr.cs` (`ISagaStore`): `GetAsync` reads by key; `AddAsync`/`UpdateAsync` stage; `GetStuckSagasAsync`/`GetExpiredSagasAsync` use the query API; `SaveChangesAsync` applies the staged operations in one state transaction with ETags; cancellation propagates.
2. The state entity (`DaprSagaState : ISagaState`) and its factory as the provider implementations do (verify on the ADO saga store); `AddEncinaSagaDapr()` with `TryAdd*`, called before `AddEncinaMessaging`'s default; the start check of Phase 4 runs when a Dapr store is registered.
3. `Diagnostics/DaprStateStoreLog.cs`: 884 component validation failed (Error: code), 885 state transaction failed (Error: code), 886 ETag conflict (Debug). `EventIdRanges.DaprStateStore = (884, 889)` registered first; ids 884-886 packed, 887-889 spare; the allocation map and `Encina` PublicAPI lines.
4. Tests: unit with a fake client; the shared `ISagaStore` contract suite runs against it; an integration test on the `Dapr` collection with a queryable component (PostgreSQL or MongoDB state component); guard; DI test; manifests and PublicAPI through `mechanical-fixer`.

</details>

<details>
<summary>Prompt for AI Agents — Phase 5</summary>

```text
CONTEXT:
- Issue #387, Phase 5 (pull request 5). Phase 4 is merged.

TASK:
Write SagaStoreDapr, its entity, registration, log class and range, with tests and the contract suite.

KEY RULES:
- Register EventIdRanges.DaprStateStore first; ids 884-886 packed.
- Never claim atomicity beyond the state store; the orchestrators never call SaveChangesAsync, the commit owner does.
- A satellite registration uses TryAdd* and wins over the in-memory default in any order.

REFERENCE FILES:
- src/Encina.ADO.PostgreSQL (saga store, entity and factory)
- src/Encina.Messaging/Sagas/ISagaStore.cs
- src/Encina/Diagnostics/EventIdRanges.cs
```

</details>

---

### Phase 6: The Inbox and Outbox stores (pull requests 6 and 7)

> **Goal**: `InboxStoreDapr` and `OutboxStoreDapr` with the same rules.

<details>
<summary>Tasks</summary>

1. `InboxStoreDapr.cs` (`IInboxStore`): first-write-wins through an ETag-checked create for duplicate detection; expiry through the component's TTL metadata when `TtlSeconds` is set and the query API otherwise; `AddEncinaInboxDapr()`.
2. `OutboxStoreDapr.cs` (`IOutboxStore`): `GetPendingMessagesAsync` and the exhausted lookups through the query API; `MarkAsProcessedAsync`, `MarkAsFailedAsync`, `MarkAsExhaustedAsync`, `RequeueExhaustedAsync`; `AddEncinaOutboxDapr()`; entities and factories as the providers do.
3. Tests per store as in Phase 5 (unit, contract suite, integration, guard, DI); manifests and PublicAPI through `mechanical-fixer`. Pull request 6 is the Inbox, pull request 7 the Outbox, so each stays within 15 files.

</details>

<details>
<summary>Prompt for AI Agents — Phase 6</summary>

```text
CONTEXT:
- Issue #387, Phase 6 (pull requests 6 and 7). The Saga store and the key builder exist.

TASK:
Write InboxStoreDapr (pull request 6) then OutboxStoreDapr (pull request 7), each with registration and tests.

KEY RULES:
- Reuse the key builder, the error holder and the log class; new EventIds only inside the existing range.
- A duplicate message is a normal result, not an error; a transaction failure is a Left.

REFERENCE FILES:
- src/Encina.Messaging/Inbox/IInboxStore.cs, Outbox/IOutboxStore.cs
- src/Encina.ADO.PostgreSQL (inbox and outbox stores)
```

</details>

---

### Phase 7: Aspire declaration (pull request 8)

> **Goal**: `WithEncinaDapr` declares the Dapr settings in the AppHost (if Design Choice 7 option A is confirmed).

<details>
<summary>Tasks</summary>

1. `src/Encina.Aspire.Hosting/Dapr/EncinaDaprOptions`-like `EncinaDaprHostingOptions` (`AppId`, `StateStoreName`), `WithEncinaDapr<T>(this IResourceBuilder<T>, Action<EncinaDaprHostingOptions>)` writing `Encina__Aspire__Dapr__AppId` and `__StateStore` (no secret, no endpoint); the key helper in `EncinaAspireEnvironment` (#416, edited).
2. Application side in `Encina.Aspire.ServiceDefaults` (#417, edited): the reader supplies the declared names as defaults to `AddEncinaDapr` only when the application did not set them; an invalid declaration is a `Left` with `encina.aspire.defaults.invalid_declaration` of #417.
3. Tests: unit over the Aspire model, guard, the parity case of #440's `EnvironmentContractTests`; PublicAPI and manifests through `mechanical-fixer`.

</details>

<details>
<summary>Prompt for AI Agents — Phase 7</summary>

```text
CONTEXT:
- Issue #387, Phase 7 (pull request 8). #416, #417 and #440 are merged. D7 is recommended, pending the maintainer.

TASK:
Write the declaration-only WithEncinaDapr and its reader, with tests.

KEY RULES:
- The hosting package takes no reference to any Encina or Dapr package.
- Written in run mode only; no endpoint, token or connection string is written.
- Naming: WithEncina* on a resource (#440).

REFERENCE FILES:
- src/Encina.Aspire.Hosting (#416), src/Encina.Aspire.ServiceDefaults (#417)
- tests/Encina.ContractTests/Aspire/EnvironmentContractTests.cs
```

</details>

---

### Phase 8: Cross-Cutting Integration

> **Goal**: Evaluate the 12 functions and wire the integrated ones.

<details>
<summary>Tasks</summary>

1. Health: Phase 2; validation: options and invoker inputs; transactions: Phases 5 to 6; idempotency: the Inbox store; multi-tenancy: Design Choice 3; resilience: the invocation timeout (retries are the sidecar's resiliency policy, configured in Dapr, not duplicated here).
2. Caching and locks are not delivered (Design Choice 6); audit and module scoping per the matrix.

</details>

<details>
<summary>Prompt for AI Agents — Phase 8</summary>

```text
CONTEXT:
- Issue #387; Phases 1-7 exist.

TASK:
Check the matrix against the code and fix gaps; add no feature.

KEY RULES:
- Gates fail closed; no ids or values in logs; no scoped service in a singleton.

REFERENCE FILES:
- AGENTS.md section 6
- src/Encina.NATS
```

</details>

---

### Phase 9: Observability

> **Goal**: Spans and counters for invocation and state operations.

<details>
<summary>Tasks</summary>

1. `Diagnostics/DaprDiagnostics.cs` (`Encina.Dapr`): an `ActivitySource` and a `Meter`; span `encina.dapr.invoke` and counter `encina.dapr.invocations` (tags `outcome`), span `encina.dapr.state.transaction` and counter `encina.dapr.state.operations` (tags `store`, `outcome`). Tags never carry an app id, a key or a tenant value; when multi-tenancy is on the tenant is a span attribute and a metric tag as ADR-054 D4 requires, and nothing is emitted when it is off.
2. `Encina.OpenTelemetry` subscribes the new source and meter (one-line edit of its `WithEncina` list and `AddEncinaInstrumentation`), recorded in the pull request that adds the diagnostics (pull request 3).

</details>

<details>
<summary>Prompt for AI Agents — Phase 9</summary>

```text
CONTEXT:
- Issue #387, Phase 9. The invoker and stores exist.

TASK:
Add the ActivitySource and Meter, wrap the invoker and the state transaction, subscribe them in Encina.OpenTelemetry.

KEY RULES:
- No app id, key or tenant value as a tag unless multi-tenancy is on (ADR-054 D4); never an EncinaError.Message.
- Counters are Counter<long> with dimensional tags.

REFERENCE FILES:
- src/Encina.OpenTelemetry/ServiceCollectionExtensions.cs
- docs/architecture/adr/054-observability-conventions-and-trace-propagation.md
```

</details>

---

### Phase 10: Testing

> **Goal**: Every flag reaches its target in the pull request that sets it.

<details>
<summary>Tasks</summary>

1. Unit, guard, contract (the store suites), property (key injectivity), integration (daprd and a queryable component), DI tests per pull request. Load and benchmark: not applicable to the first delivery (the stores are I/O bound to the sidecar); stated in the pull request.
2. **Per-file targets** (new files; each reached by its own pull request):

   | File (pull request) | Unit | Guard | Contract | Property | Integration | Justification |
   |------|-----:|------:|---------:|---------:|------------:|---------------|
   | `EncinaDaprOptions.cs`, validator (1) | 95 | 90 | 0 | 0 | 0 | Every rule and the token hiding asserted |
   | `ServiceCollectionExtensions.cs` (2) | 95 | 90 | 0 | 0 | 0 | Registration, guards and the DI test |
   | `DaprSidecarHealthCheck.cs` (2) | 95 | 85 | 0 | 0 | 0 | Healthy, unhealthy and exception branches |
   | `DaprErrors.cs`, `DaprLog.cs` (2) | 100 | 0 | 0 | 0 | 0 | Constants and generated methods called by the tests; no guarded parameter |
   | `DaprServiceInvoker.cs` (3) | 92 | 85 | 0 | 0 | 70 | A fake sidecar covers every branch; `daprd` covers the real path |
   | `DaprStateKey.cs` (4) | 100 | 0 | 0 | 90 | 0 | Pure function; the property test proves injectivity; internal static |
   | `DaprStateStoreStartCheck.cs` (4) | 92 | 0 | 0 | 0 | 60 | Fake metadata for branches; one integration case |
   | `SagaStoreDapr.cs` (5) | 90 | 85 | 90 | 0 | 70 | Fake client plus the contract suite; a queryable component in Docker |
   | `InboxStoreDapr.cs` (6) | 90 | 85 | 90 | 0 | 70 | Same |
   | `OutboxStoreDapr.cs` (7) | 90 | 85 | 90 | 0 | 70 | Same |
   | `DaprDiagnostics.cs` (3) | 90 | 0 | 0 | 0 | 0 | Listener-based unit tests of tags |

3. Run `crap-gate` locally (every touched method CRAP <= 10: the store methods are split into small private methods) and `coverage-report.cs --check-justifications`.

</details>

<details>
<summary>Prompt for AI Agents — Phase 10</summary>

```text
CONTEXT:
- Issue #387, Phase 10. Each pull request carries the tests that reach its targets.

TASK:
Measure per-flag coverage, close the gaps, run crap-gate and the justification check.

KEY RULES:
- Tests execute real code; contract suites instantiate the real stores; never lower a manifest target.
- Integration tests use [Collection("Dapr")] shared fixtures.

REFERENCE FILES:
- .github/coverage-manifest/Encina.json
- docs/testing/integration-tests.md
```

</details>

---

### Phase 11: Documentation & Finalization (pull request 9)

> **Goal**: Users can set up Dapr with Encina from a README.

<details>
<summary>Tasks</summary>

1. XML docs with `<example>`; README of the two packages and a guide (through `docs-writer`): endpoints and the token, the allowlist, the queryable-component requirement, the atomicity limit, what is not delivered; `docs/INVENTORY.md`; `changelog.d/387-dapr-integration.added.md` (through `mechanical-fixer`); an ADR for the store semantics if the maintainer wants it (number assigned when the ADR pull request opens).
2. Issue files of this run: workflows, actors, bindings and secrets (Design Choice 5).

</details>

<details>
<summary>Prompt for AI Agents — Phase 11</summary>

```text
CONTEXT:
- Issue #387, Phase 11 (pull request 9). Code merged.

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
| 1 | `Directory.Packages.props` edit, project file, two PublicAPI files, `Encina.slnx` edit, options, validator, manifest, three test project reference edits, unit and guard test files | 13 | 900 |
| 2 | Extension class, health check, `DaprErrors`, `DaprLog`, `EventIdRanges.cs` edit, `Encina` PublicAPI, `Encina.json`, allocation map edit, Dapr PublicAPI edit and manifest edit, unit, guard and DI test files | 14 | 1,000 |
| 3 | Invoker and interface, `DaprDiagnostics`, extension edit, `Encina.OpenTelemetry` list edit, PublicAPI, manifest, unit, guard, `DaprFixture`, collection file, integration test, integration csproj edit | 13 | 1,100 |
| 4 | State-store project file, two PublicAPI files, `Encina.slnx` edit, options, key builder, start check, manifest, three test project references, unit, guard and property test files | 15 | 1,000 |
| 5 | `SagaStoreDapr`, saga entity, registration, `DaprStateStoreLog`, `EventIdRanges.cs` edit, `Encina` PublicAPI, `Encina.json`, allocation map edit, PublicAPI and manifest edits, unit, guard, DI, contract and integration tests | 15 | 1,300 |
| 6 | `InboxStoreDapr`, entity, registration, PublicAPI, manifest, unit, guard, DI, contract and integration tests | 10 | 900 |
| 7 | `OutboxStoreDapr`, entity, registration, PublicAPI, manifest, unit, guard, DI, contract and integration tests | 10 | 1,000 |
| 8 | Hosting options and extension, `EncinaAspireEnvironment` edit, ServiceDefaults reader and extension edits, Hosting and ServiceDefaults manifests and PublicAPI, unit, guard tests, `EnvironmentContractTests` edit | 12 | 700 |
| 9 | Two READMEs, guide, inventory line, changelog fragment, two justification files (load, benchmark) | 7 | 380 |

PR 1 and PR 2 are sequential; PR 3 needs PR 2; PR 4 needs PR 2; PR 5 needs PR 4; PRs 6 and 7 need PR 5; PR 8 needs #416, #417, #440 (merged) and PR 2; PR 9 is last. Interfaces are not changed, so no implementer or test double is touched; the contract suites of the stores already exist and gain one more implementer each. A pull request that measures over 15 files or 1,500 lines when briefed is split again; PRs 4 and 5 are the closest.

---

## Research

### Relevant Standards & Specifications

| Reference | What it defines | Use in this feature |
|-----------|-----------------|---------------------|
| `AGENTS.md` sections 3, 5, 6 | Fail closed, secrets, locks and caches closed sets, 12 functions, provider coherence | Design Choices 3, 4, 6 and 8 |
| ADR-001, ADR-062 | Either results and error categories | Error codes |
| ADR-021 | EventId ranges registered first | `DaprCore` 878-883, `DaprStateStore` 884-889 |
| ADR-054 (D4) | W3C trace context; tenant tag only where multi-tenancy is on | Phase 9 |
| SPEC-000 section 6, DEC-009 | Local-emulator rule; lock set closed | Scope, Design Choice 6 |
| Dapr state API (transactions, ETags, query, TTL) and service invocation | The building blocks | Phases 3 to 6; status re-read at Phase 1 |

### Existing Encina Infrastructure to Leverage

| Component | Location | Usage in this feature |
|-----------|----------|-----------------------|
| `IOutboxStore`, `IInboxStore`, `ISagaStore` | `src/Encina.Messaging/Outbox/`, `Inbox/`, `Sagas/` | The contracts the Dapr stores implement |
| NATS options, validator, publisher, health check, registration | `src/Encina.NATS/` | The provider pattern copied |
| `EndpointValidator`, `EndpointPolicy` | `src/Encina/Validation/` | Endpoint rules |
| `IEncinaHealthCheck` | `src/Encina.Messaging/Health/` | Sidecar health |
| `EncinaErrors.Create(code, category, message)` | `src/Encina/Results/EncinaErrors.cs` | Errors |
| `Encina.Testing.WireMock`, `Encina.Testing.Testcontainers`, `Encina.Testing.FsCheck` | `src/Encina.Testing.*` | Test doubles, containers, properties |
| `Dapr.Client` pin | `Directory.Packages.props` | The SDK |

### Event ID Allocation

| Package | Range | Notes |
|---------|-------|-------|
| `Encina.Dapr` (`DaprCore`, new) | 878-883, ids 878-880 used | From the block 850-899 (aspire-2); registered in `EventIdRanges.cs` and the allocation test map before use |
| `Encina.Dapr.StateStore` (`DaprStateStore`, new) | 884-889, ids 884-886 used | Same block; #242 takes its own range from the transport family's block |

### Estimated File Count

| Category | Files | Notes |
|----------|------:|-------|
| Production code | 22 | Core (9), state store (10), Aspire declaration (3) |
| Edits of shared files | 6 | `Directory.Packages.props`, `EventIdRanges.cs`, `Encina.OpenTelemetry`, `EncinaAspireEnvironment`, two ServiceDefaults files |
| Public API and manifests | 14 | Three packages' PublicAPI and manifests, `Encina` PublicAPI, `Encina.json` |
| Tests and project files | 24 | Unit, guard, DI, contract, property, integration, fixture, csproj references, allocation map |
| Documentation | 7 | Two READMEs, guide, inventory, fragment, two justification files |
| **Total** | **by the split table: 13 + 14 + 13 + 15 + 15 + 10 + 10 + 12 + 7 = 109 file touches over 9 pull requests** | The split table is the authority |

---

## Combined AI Agent Prompts

<details>
<summary><strong>Full combined prompt for all phases</strong></summary>

```text
PROJECT CONTEXT:
- Encina is a pre-1.0 .NET 10 / C# 14 library; Either<EncinaError, T>; no [Obsolete], no compatibility layers.
- Issue #387: Dapr integration. Plan: docs/plans/dapr-integration-implementation-plan-387.md; D1, D2, D5, D7 and D8 are recommended, pending the maintainer.

IMPLEMENTATION OVERVIEW (9 pull requests, each builds alone):
1-3. Encina.Dapr: options, registration, health, invoker, diagnostics (Phases 1-3, 9).
4-7. Encina.Dapr.StateStore: keys, start check, Saga, Inbox, Outbox stores (Phases 4-6).
8. The Aspire declaration (Phase 7). 9. Documentation (Phase 11).

KEY PATTERNS:
- Staged writes applied in one state transaction; ETags; injective tenant-qualified keys; a missing tenant denies.
- Invocation only to an allowlisted app id and a validated method; cancellation propagates, never a Left.
- Options with a token: [JsonIgnore] and ToString(); endpoints through EndpointValidator.
- EncinaErrors.Create(code, category: ErrorCategory.X, message: <constant>); one category per code; no ids or values in logs.
- EventIds registered first: DaprCore 878-883, DaprStateStore 884-889.

REFERENCE FILES:
- src/Encina.NATS, src/Encina.Messaging/Outbox/IOutboxStore.cs, Inbox/IInboxStore.cs, Sagas/ISagaStore.cs
- src/Encina/Validation/EndpointValidator.cs
- src/Encina/Diagnostics/EventIdRanges.cs
- .github/coverage-manifest/Encina.json
```

</details>

---

## Cross-Cutting Integration Matrix

| # | Function | Status | Notes |
|---|----------|--------|-------|
| 1 | Caching | ❌ | Not applicable: the caching provider set is closed at eight (Design Choice 6); the issue's "state store as cache backend" is not delivered |
| 2 | OpenTelemetry | ✅ | Phase 9: `Encina.Dapr` source and meter; W3C context by the SDK; tenant tag only when tenancy is on |
| 3 | Structured Logging | ✅ | Phases 2 and 5: EventIds 878-880 and 884-886 |
| 4 | Health Checks | ✅ | Phase 2: `DaprSidecarHealthCheck`; Phase 4 start check of the component |
| 5 | Validation | ✅ | Options, app ids, methods, keys |
| 6 | Resilience | ✅ | An invocation timeout; retries and circuit breaking stay the sidecar's resiliency policy, configured in Dapr |
| 7 | Distributed Locks | ❌ | Not applicable: no Dapr lock provider (Design Choice 6, AGENTS.md section 5 closed set) |
| 8 | Transactions | ✅ | Phases 5 to 6: one state transaction per `SaveChangesAsync`, atomic inside the state store only |
| 9 | Idempotency | ✅ | Phase 6: the Inbox store is the deduplication store |
| 10 | Multi-Tenancy | ✅ | Design Choice 3: tenant-qualified injective keys, reserved `default`, missing tenant denies |
| 11 | Module Isolation | ❌ | Not applicable: a sidecar is per service, as the issue states |
| 12 | Audit Trail | ⏭️ | Deferred as in the issue: invocation audit; the issue file of this run for Dapr follow-ups lists it |

---

## Prerequisites & Dependencies

- #416, #417 and #440 (merged) for pull request 8; #720's `SagaOrchestrator.CommitAsync` is the commit owner the stores rely on; the maintainer's answer to Design Choice 1 (and #242's plan) before pull request 3 if #242 is to build on `Encina.Dapr`.
- `Dapr.Client` is already pinned; `CommunityToolkit.Aspire.Hosting.Dapr` is not added (Design Choice 7 option A).
- Issue file of this run: Dapr workflows, actors, bindings, secrets and handler invocation (Design Choice 5), covering the Dapr audit item.

---

## Next Steps

1. The maintainer answers D1, D2, D5, D7 and D8 when work on #387 starts; the answers go into `## Maintainer Decisions` and the markers are removed. If Phase 1 finds the query API still alpha, D2 is asked again before pull request 4.
2. Pull requests 1 to 3, then 4 to 7, then 8 and 9.
