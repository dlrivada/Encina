# Implementation Plan: `Encina.Dapr` — sidecar client, service invocation and state-store backed Outbox, Inbox and Saga stores

> **Issue**: [#387](https://github.com/dlrivada/Encina/issues/387)
> **Type**: Feature
> **Complexity**: High (11 phases, 13 pull requests of 13, 14, 13, 7, 14, 13, 6, 12, 9, 9, 10, 11 and 7 files)
> **Estimated Scope**: ~1,300 lines of production code + ~3,000 lines of tests
> **Family**: Aspire (aspire-2): #424, #425, #382, #391. Related: #242 (Dapr PubSub transport, `status:proposal`, same package name `Encina.Dapr`), #388 (Orleans virtual actors), #607 and #382 (service discovery), #416 (the hosting package). The closed duplicates #420 (CloudEvents and Aspire AppHost extensions) and #450 (secrets by attribute, cross-service handler invocation) were carried into this issue. **This plan owns the `Encina.Dapr` and `Encina.Dapr.StateStore` packages and EventIds 878-889.**

---

## Summary

The issue asks for five packages (core, state store, pub/sub, workflows, actors), state-store backed Outbox, Inbox and Saga stores, pub/sub as a transport, service invocation, optional workflows, tracing, and (from the duplicates) CloudEvents, an Aspire AppHost extension, secrets and locks. State of `origin/main` (`ce352001f`), verified by reading and grep, and of the NuGet feed (read 2026-11-20):

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
| **A) All three stores (Outbox, Inbox, Saga) use key-value reads and writes plus the Dapr state **query API** for the "pending", "stuck", "expired" and "exhausted" lookups; registration validates at start that the configured component is queryable (and transactional) and fails closed when it is not; concurrent writers are detected with ETags and a mismatch is `encina.dapr.state_conflict` (`ErrorCategory.Conflict`)** | The full contract of each store; the issue's acceptance item delivered | The query API is limited to some components and its maturity must be re-read at Phase 1; users pick a queryable component |
| **B) Encina maintains its own index keys (a status list per store) with ETag concurrency** | Works on any component | A hot index key becomes a contention point; a second source of truth to repair |
| **C) Only Saga and Inbox now (key-addressable), Outbox later** | Smaller first delivery | The issue's Outbox store is deferred, and the Saga store still needs a stuck-saga query |

### Chosen Option: **A — the query API with a start check** (recommended, pending the maintainer)

### Rationale

A wins when users choose a queryable component (PostgreSQL, MongoDB, Redis with search) and want the stores' full contract; B wins on a component without query support and light load; C if the maintainer wants the smallest first step. Phase 1 re-reads the query API's status in the Dapr documentation for the pinned version before any store code; if it is still alpha the Dilemma is re-asked, because shipping an alpha dependency in the 1.0 contract is the risk.

**Class:** Dilemma

</details>

<details>
<summary><strong>3. Store semantics: staging, commit and injective keys</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) The stores stage writes in a per-scope list and apply them in `SaveChangesAsync` through one `ExecuteStateTransactionAsync` (all-or-nothing inside the component); every key is built by one key builder and is injective in its parts (the exact format and the escaped characters are Phase 4 details, not part of this choice; the tenant question is Design Choice 11 and the concurrency policy is part of Design Choice 2)** | The staged contract of the interfaces; injective keys (no two store and id pairs map to one key) | A transaction needs a transactional component (checked at start) |
| **B) Write immediately on `AddAsync`** | Simpler | Breaks the staged contract the orchestrators rely on |

### Chosen Option: **A — staged transaction, one injective key builder**

### Rationale

The staged shape is the contract of `IOutboxStore.SaveChangesAsync` and `ISagaStore.SaveChangesAsync`; injective keys are the repository rule that no two logical ids share a stored id. The same rule holds under every option of Design Choices 1, 2 and 5 because they change which stores exist, not the requirement that every key, including the index keys of Design Choice 2 option B, is produced by the one injective key builder. The transaction is atomic only inside the state store component; it is never described as atomic with business data stored elsewhere.

**Class:** Settled
**Settled by:** `src/Encina.Messaging/Outbox/IOutboxStore.cs` and `Sagas/ISagaStore.cs` (staged writes and `SaveChangesAsync`); AGENTS.md section 3 (errors are never swallowed in background infrastructure: a failed transaction is a `Left`)

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
<summary><strong>5. Actors, bindings, secrets and cross-service handler invocation (Dilemma D5)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Not delivered here: each becomes an issue file; actors overlap #388 (virtual actors), bindings are scheduled-message triggers outside the scheduling pattern's current API, secrets belong to `Encina.Security.Secrets`, cross-service handler invocation needs a remote protocol (the same open question as `SendToService` in #382)** | The plan delivers what can be built correctly now; no half features | These items from the duplicates (#420, #450) stay open as issues |
| **B) Deliver secrets now (a Dapr provider for `Encina.Security.Secrets`)** | Closer to the issue list | Needs a provider contract decision that the secrets category owns |
| **C) Deliver all of them** | Full list | Far beyond one plan |

### Chosen Option: **A — defer each with an issue file** (recommended, pending the maintainer)

### Rationale

A wins because each omitted item is its own feature with its own contract questions (and #388 may own actors); B wins if the maintainer wants Dapr secrets soon; C is a program, not a plan. Dapr Workflows, an acceptance criterion of #387, has its own Dilemma (Design Choice 9). The issue file of this run covers these items.

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

<details>
<summary><strong>9. Dapr Workflows (acceptance criterion "Dapr Workflows integration (optional)", Dilemma D9)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Not delivered in this plan: an issue file for `Encina.Dapr.Workflows`; the criterion is marked optional in the issue** | The plan stays within the stores and invocation; a second saga model is decided on its own | The optional criterion is open until that issue is done |
| **B) Deliver `Encina.Dapr.Workflows` as a saga backend here** | Durable execution managed by Dapr | `Dapr.Workflow` (1.19.1) is a new dependency with its own runtime model, a second saga model beside `SagaOrchestrator`, far past one plan |

### Chosen Option: **A — separate issue** (recommended, pending the maintainer)

### Rationale

A wins because the issue itself marks workflows optional and the model choice (Dapr workflow versus `SagaOrchestrator`, #720) deserves its own plan; B wins if durable workflow execution is wanted in the first Dapr release.

**Class:** Dilemma

</details>

<details>
<summary><strong>10. The AppHost declaration in run and publish mode (Dilemma D10)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `WithEncinaDapr` writes `Encina__Aspire__Dapr__AppId` and `__StateStore` in run mode and in publish mode (they are names, no secret, no endpoint)** | A published deployment receives the same names, so the application behaves as in development | The manifest carries two more variables |
| **B) Run mode only (the #416 environment contract convention for development-only switches)** | Nothing extra in a manifest | A deployment never gets the app id or store name and must configure them separately |

### Chosen Option: **A — both modes** (recommended, pending the maintainer)

### Rationale

A wins because the values are configuration, not development aids, and names carry no secret; B wins if every deployment configures Dapr names through its own pipeline and the manifest should stay minimal.

**Class:** Dilemma

</details>

<details>
<summary><strong>11. Where a store gets its tenant (Dilemma D11)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) No tenant in this delivery: keys are `encina:{store}:{id}` where the id is the message id, the saga id or, for the Inbox, `IInboxMessage.MessageId` (a string, typically the idempotency key, so two tenants that use the same key would share one Dapr key, exactly as with the primary key of the relational providers), so keys are injective in (store, id) but not per tenant; **with multi-tenancy on the Dapr stores fail closed**: the state-store start check fails the host when the tenancy abstraction `ITenantContext` is registered in the container (confirmed at Phase 4) unless `DaprStateStoreOptions.AllowSharedTenantKeys` is explicitly set (default false; `encina.dapr.state_requires_tenant_optin`, `ErrorCategory.FailedPrecondition`, logged as 884), so the shared-key case is never silent; multi-tenant stores are a deferred item with an issue file, to be planned together with the Messaging tenancy work, because the Outbox and Saga contracts carry no tenant (a grep of `src/Encina.Messaging/Outbox` and `Sagas` finds no tenant member; the Inbox carries one only inside `InboxOrchestrator`) and the processors read pending work across tenants outside any request** | Honest about the contracts; no invented tenant source; a background processor still works | A multi-tenant application cannot partition Dapr stores by tenant yet |
| **B) The scoped store reads `ITenantContext`, denies a write when tenancy is on and no tenant is present, and qualifies keys with the tenant (reserved `default` segment)** | Tenant isolation of the stored data | Processors run in their own scopes with no tenant, so every background read would deny or read across tenants; the query API cannot filter by key prefix |

### Chosen Option: **A — no tenant in keys now, an issue file for tenant-aware stores** (recommended, pending the maintainer)

### Rationale

A wins while the store contracts have no tenant to read; B wins once the Messaging contracts carry a tenant (the multitenancy plans of the messaging family) and the maintainer wants isolation before then, accepting the processor problem. The matrix row for multi-tenancy is deferred accordingly.

**Class:** Dilemma

</details>

<details>
<summary><strong>12. The HTTP verb of service invocation (Dilemma D12)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `IDaprServiceInvoker.InvokeAsync` always uses POST with the request as the body** | One shape for commands and queries, no verb to validate | A target that exposes a GET endpoint cannot be called with it |
| **B) A verb parameter limited to an allowlist (`GET`, `POST`, `PUT`, `DELETE`)** | Covers REST targets | A bigger surface and a verb to validate per call |

### Chosen Option: **A — POST only** (recommended, pending the maintainer)

### Rationale

A wins because Encina sends requests (commands, queries) as bodies; B wins when the targets are REST services that Encina must call with GET.

**Class:** Dilemma

</details>

<details>
<summary><strong>13. CloudEvents (carried from #420, Dilemma D13)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Not delivered here: the CloudEvents envelope is how Dapr pub/sub carries messages, so it belongs to the Pub/Sub transport (`Encina.Dapr.PubSub`, #242 under Design Choice 1 option A)** | One owner for the envelope | The carried bullet waits for #242's plan |
| **B) A CloudEvents helper in `Encina.Dapr` core that #242 reuses** | Shared early | Designs the envelope before the transport that uses it |

### Chosen Option: **A — #242 owns CloudEvents** (recommended, pending the maintainer)

### Rationale

A wins because the envelope is part of publishing and subscribing, which #242 plans; B wins if the maintainer wants the helper before the transport.

**Class:** Dilemma

</details>

<details>
<summary><strong>14. The Dapr.Client version (Dilemma D14)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Raise the pin of `Dapr.Client` from 1.16.1 to 1.19.1, the current stable on NuGet (read 2026-11-20), in `Directory.Packages.props`, in pull request 1; `Dapr.AspNetCore` stays unused and unreferenced** | The first project to reference the SDK starts on the current release and its fixes | A three-minor jump with no project using the pin so far (so no regression surface), but the SDK's behavior must be re-read at Phase 1 |
| **B) Keep 1.16.1** | No change to a pin that Dependabot already manages | The new package starts on an older release |

### Chosen Option: **A — 1.19.1** (recommended, pending the maintainer)

### Rationale

A wins because no project uses the pin yet, so nothing can regress, and the new package should start from the current release; B wins if the maintainer wants dependency bumps left to Dependabot only.

**Class:** Dilemma

</details>

---

## Implementation Phases

### Phase 1: Package skeleton and options (pull request 1)

> **Goal**: The `Encina.Dapr` project, its options and validator, with the client pin current.

<details>
<summary>Tasks</summary>

1. Re-read the Dapr documentation for the pinned version (state query API status, transaction support per component) and record it in the pull request; set `Dapr.Client` to 1.19.1, the current stable on NuGet (the pin is 1.16.1), in `Directory.Packages.props` (Design Choice 14; if the repository does not build with 1.19.1 the pull request is blocked and reported, not downgraded). `Dapr.AspNetCore` is not referenced by any project of this plan (nothing here maps an incoming Dapr route); its unused pin is left to Dependabot. The root namespace of the package is `Encina.Dapr`, which shadows the `Dapr` root namespace inside `Encina.*` code: files `using Dapr.Client;` and never write `Dapr.Client.X` fully qualified, or alias it with `global::`.
2. `src/Encina.Dapr/Encina.Dapr.csproj` (`PackageId`, XML docs, `Dapr.Client`, `InternalsVisibleTo` for the unit, guard, contract and integration test projects, PublicAPI analyzer), PublicAPI files, the `Encina.slnx` entry, the project references of the test projects, `.github/coverage-manifest/Encina.Dapr.json`.
3. `EncinaDaprOptions.cs` (public sealed): `HttpEndpoint` (default `http://localhost:3500`), `GrpcEndpoint` (default `http://localhost:50001`), `ApiToken` (`string?`, `[JsonIgnore]`, `ToString()` override), `AllowLocalEndpoints` (Design Choice 8), `AllowedAppIds` (`IReadOnlyCollection<string>`, empty by default: the invoker denies), `InvocationTimeout` (default 30 seconds), `ProviderHealthCheck` (`ProviderHealthCheckOptions`, the type the NATS options use).
4. `EncinaDaprOptionsValidator.cs` (internal): endpoints through `EndpointValidator` with the scheme list `http`, `https`; each entry of `AllowedAppIds` must satisfy the application-id rule of the Dapr documentation for the pinned version (read and recorded in the pull request at Phase 1; it may allow namespace-qualified ids such as `app.namespace`, so no hand-written pattern is assumed here: non-blank, no whitespace, no `/`, no `..` are the checks this plan fixes); the validator emits no warning for `AllowLocalEndpoints` (it has no EventId, and loopback is the documented default of Design Choice 8). Defaults register (Trivial, the values are listed in the README): `HttpEndpoint` `http://localhost:3500` and `GrpcEndpoint` `http://localhost:50001` (the Dapr sidecar defaults), `InvocationTimeout` 30 seconds, `AllowedAppIds` empty (deny), `Consistency` strong.
5. Tests: options and validator unit tests, guard tests; PublicAPI and manifest through `mechanical-fixer`.

</details>

<details>
<summary>Prompt for AI Agents — Phase 1</summary>

```text
CONTEXT:
- Issue #387; plan docs/plans/dapr-integration-implementation-plan-387.md. D1, D2, D5, D7, D8, D9, D10, D11, D12, D13 and D14 are recommended, pending the maintainer: follow the Maintainer Decisions section if it exists.
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
3. `DaprErrors.cs` (internal, `Encina.Dapr`): only `encina.dapr.sidecar_unavailable` (`ErrorCategory.Unavailable`), the one code the health check of this pull request emits, created with `EncinaErrors.Create(code, category: ErrorCategory.Unavailable, message: <constant text>)`. Each package owns the codes it emits and each code is added in the pull request that emits it: pull request 3 adds the invoker's codes to this file (`app_id_not_allowed` `Forbidden`, `invalid_method` `Validation`, `invocation_failed` `UpstreamFailure`, `timeout` `Timeout`); the state-store package has its own `DaprStateStoreErrors.cs` (Phases 4 to 6), so no internal type is shared between the two packages.
4. `Diagnostics/DaprLog.cs`: only 878 sidecar health probe failed (Warning: error code), emitted by the health check of this pull request. `EventIdRanges.DaprCore = (878, 883)` registered first, the allocation test map, the `Encina` PublicAPI lines, and `docs/engineering/eventid-block-registry.md` in the same pull request, as `EventIdRegistryAgreementTests` requires: the "Registered ranges" table (header `Name | Block | Area | Zone`) gains `| DaprCore | 878-883 | Observability and platform | historical |` (Zone is `historical` for every range below 10000; `reserved-pre-zone` is only a Status of the "Blocks" table); in the "Blocks" table (header `Block | Area | Owner | Status`) the reserved row 878-889 becomes `878-883` with Status `registered` plus `884-889` kept `reserved-pre-zone`, so no reserved row overlaps a registered range. Pull request 4b does the same for `DaprStateStore` 884-889 (its block row becomes `registered`). The five aspire-2 plans edit one shared row (850-899, owner aspire-2): the first to land splits it into one `reserved-pre-zone` row per plan (#424 850-854, #425 855-864, #382 865-877, #387 878-889, #391 890-899, owners `aspire-2: #<n>`); every later pull request changes only its own row, so the edits are disjoint lines and a conflict is resolved by keeping both; recommended merge order #424, #425, #382, #387, #391. Ids 879 invocation failed (Warning: error code) and 880 invocation denied (Warning: a fixed reason, not the app id) are added by pull request 3 together with the invoker that emits them, so no log method is declared before its emitter; 881-883 spare.
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

1. `IDaprServiceInvoker.cs` and `DaprServiceInvoker.cs`: Design Choice 4; the timeout from `InvocationTimeout` is a linked token source and a timeout is `ErrorCategory.Timeout` (`encina.dapr.timeout`), while the caller's cancellation propagates; the HTTP verb is POST (Design Choice 12); the activity of Phase 9 wraps the call in pull request 3b. The invoker emits 879 and 880, added to `DaprLog` here (an edit of the PR 2 file).
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
2. `DaprStateStoreOptions.cs`: `StoreName` (required), `Consistency` (default strong, registered); there is no TTL here: `DaprInboxStoreOptions.InboxTtlSeconds` (`int?`, null by default, an Inbox-only option, validated positive) is the only expiry setting, because a TTL on Outbox or Saga entries would silently delete unprocessed messages or running sagas (data loss); Outbox and Saga entries are removed only by their stores' own purge operations.
3. `DaprStateKey.cs` (internal static): `Build(string store, string id)` returning the key `encina:{store}:{id}` with `%`, `:` and `|` percent-encoded in each part (Dapr reserves `||` in state keys, to be confirmed against the pinned SDK documentation at Phase 1; escaping `|` makes the builder safe either way); a unit and a property test prove injectivity over generated pairs. There is no tenant in the key (Design Choice 11). `DaprStateStoreErrors.cs` (internal, `Encina.Dapr.StateStore`) is created in pull request 4b with `encina.dapr.state_query_unsupported` (`ErrorCategory.FailedPrecondition`), the first code something emits, and gains `encina.dapr.state_conflict` (`ErrorCategory.Conflict`) in pull request 5, so a code is never declared before its emitter (this pull request declares no error code). `DaprStateStoreOptions`, `DaprInboxStoreOptions` and the key builder are the production files of this pull request.
4. (pull request 4b) `DaprStateStoreStartCheck.cs` (internal) and the log: calls the Dapr metadata endpoint, requires the named component to exist and, per the stores registered, to support transactions and queries (`encina.dapr.state_query_unsupported`, `ErrorCategory.FailedPrecondition`); it also fails closed when multi-tenancy is registered and `AllowSharedTenantKeys` is not set (Design Choice 11, code `encina.dapr.state_requires_tenant_optin`); a failure logs 884 state-store start check failed (Error: code) and stops the host with the code only. The check does not register itself: pull request 4c adds the public `AddEncinaDaprStateStore(this IServiceCollection, Action<DaprStateStoreOptions>?)` in the state-store `ServiceCollectionExtensions.cs` (registers `DaprStateStoreOptions` with validation on start, the start check as a hosted service with `TryAddEnumerable`, and is called by `AddEncinaSagaDapr`, `AddEncinaInboxDapr` and `AddEncinaOutboxDapr`), with its PublicAPI lines, guard test and DI test (`ValidateOnBuild`, `ValidateScopes`). `DaprStateStoreLog.cs` with 884, `EventIdRanges.DaprStateStore = (884, 889)` registered first, the allocation map, the `Encina` PublicAPI lines and `Encina.json` manifest entry are in pull request 4b, so the start check and its log land together; the queryable state component files for the Docker fixture (a component YAML mounted by `DaprFixture`) are also in 4b, with the integration case of the check.
5. Tests: unit (keys, options in pull request 4; the check with a fake client in 4b), guard, a property test (FsCheck, `Encina.Testing.FsCheck`) for key injectivity and round trip; PublicAPI and manifest through `mechanical-fixer`.

</details>

<details>
<summary>Prompt for AI Agents — Phase 4</summary>

```text
CONTEXT:
- Issue #387, Phase 4 (pull request 4). D2 is recommended, pending the maintainer.

TASK:
Create the state-store project, options, key builder, start check and tests.

KEY RULES:
- Keys are injective (one key builder, %, : and | escaped); there is no tenant in the key (Design Choice 11, recommended, pending the maintainer).
- Errors carry a named ErrorCategory; the start check throws with the code only; each package owns and declares only the codes it emits.
- No id or key in logs.

REFERENCE FILES:
- src/Encina.Messaging/Outbox/IOutboxStore.cs, Inbox/IInboxStore.cs, Sagas/ISagaStore.cs
```

</details>

---

### Phase 5: The Saga store (pull request 5)

> **Goal**: `SagaStoreDapr` with ETags and the query API, plus the registration and EventIds.

<details>
<summary>Tasks</summary>

1. `SagaStoreDapr.cs` (`ISagaStore`): `GetAsync` reads by key; `AddAsync`/`UpdateAsync` stage; `GetStuckSagasAsync`/`GetExpiredSagasAsync` use the query API; `SaveChangesAsync` applies the staged operations in one state transaction with ETags; cancellation propagates.
2. The state entity (`DaprSagaState : ISagaState`) and its factory as the provider implementations do (verify on the ADO saga store); `AddEncinaSagaDapr()` registers `ISagaStore` as scoped with `TryAddScoped` (the store holds a per-scope staging list and takes the singleton `DaprClient`). `Encina.Messaging` registers no default store for `IOutboxStore`, `IInboxStore` or `ISagaStore` (no `InMemory*Store` class and no `TryAdd` of the three interfaces under `src/Encina.Messaging`; a store comes from a provider package), so the order against a default cannot matter; against an application's own registration `TryAdd` never overrides it, and an application registration added after the Dapr call replaces it as in any container. The DI test of this pull request builds the provider in both orders (Dapr call before and after an application-registered store) and with only the Dapr call, with `ValidateOnBuild` and `ValidateScopes`. The start check of Phase 4 runs when a Dapr store is registered.
3. `Diagnostics/DaprStateStoreLog.cs` (the file created in pull request 4b with 884) gains 885 state transaction failed (Error: code) and 886 ETag conflict (Debug), both emitted by `SagaStoreDapr`; the range `DaprStateStore = (884, 889)` is already registered; ids 884-886 packed, 887-889 spare. The saga entity and its factory are two files of this pull request (counted in its row).
4. Tests: unit with a fake client (every `ISagaStore` operation); no behavioral contract suite for `ISagaStore` exists on `main` (`tests/Encina.ContractTests` has only the reflection checks of `EntityFrameworkCore/StoreContractTests.cs` and `SagaContractTests.cs`), so there is no Contract target for the Dapr stores and the suites are an issue file of this run; an integration test on the `Dapr` collection with a queryable component (PostgreSQL or MongoDB state component); guard; DI test; manifests and PublicAPI through `mechanical-fixer`.

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

1. `InboxStoreDapr.cs` (`IInboxStore`): first-write-wins through an ETag-checked create for duplicate detection; expiry through the component's TTL metadata when `InboxTtlSeconds` is set (never for the other stores) and the query API otherwise; `AddEncinaInboxDapr()`.
2. `OutboxStoreDapr.cs` (`IOutboxStore`): `GetPendingMessagesAsync` and the exhausted lookups through the query API; `MarkAsProcessedAsync`, `MarkAsFailedAsync`, `MarkAsExhaustedAsync`, `RequeueExhaustedAsync`; `AddEncinaOutboxDapr()`; entities and factories as the providers do.
3. Tests per store as in Phase 5 (unit, integration, guard, DI with the two-order cases; no contract suite, see Phase 5); manifests and PublicAPI through `mechanical-fixer`. Pull request 6 is the Inbox, pull request 7 the Outbox, so each stays within 15 files.

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

1. `src/Encina.Aspire.Hosting/Dapr/EncinaDaprHostingOptions.cs` (`AppId`, `StateStoreName`), `WithEncinaDapr<T>(this IResourceBuilder<T>, Action<EncinaDaprHostingOptions>)` writing `Encina__Aspire__Dapr__AppId` and `__StateStore` (no secret, no endpoint), in run mode and in publish mode (Design Choice 10); the key helper in `EncinaAspireEnvironment` (#416, edited). The hosting package takes no reference to any Dapr or Encina package.
2. Application side, inside the Dapr packages (not in `Encina.Aspire.ServiceDefaults`, which stays free of Dapr references, pay-for-what-you-use): an internal `DaprAspireDeclarationOptionsSetup : IConfigureOptions<EncinaDaprOptions>` in `Encina.Dapr` reads `Encina:Aspire:Dapr:AppId` (it adds that id to `AllowedAppIds` only if the application configured none and the value passes the validator; it never widens a list the application set), and an `IConfigureOptions<DaprStateStoreOptions>` in `Encina.Dapr.StateStore` reads `Encina:Aspire:Dapr:StateStore` when `StoreName` is blank. An invalid value is an options validation failure at start (`encina.dapr.invalid_declaration`, `ErrorCategory.Validation`, constant message, no value echoed). Both setups are registered by `AddEncinaDapr` and the store registrations, with a DI test (`ValidateOnBuild`, `ValidateScopes`).
3. Tests: unit over the Aspire model, unit for both setups, guard, the parity case of #440's `EnvironmentContractTests` (the writer into the Dapr readers; `Encina.ContractTests` references the Dapr packages); PublicAPI and manifests through `mechanical-fixer`.

</details>

<details>
<summary>Prompt for AI Agents — Phase 7</summary>

```text
CONTEXT:
- Issue #387, Phase 7 (pull request 8). The plans of #416 and #440 are merged; their code is merged before this pull request (it is not on main today). D7 and D10 are recommended, pending the maintainer.

TASK:
Write the declaration-only WithEncinaDapr and its reader, with tests.

KEY RULES:
- The hosting package takes no reference to any Encina or Dapr package.
- Written in run and publish mode (Design Choice 10); no endpoint, token or connection string is written.
- No reference from Encina.Aspire.ServiceDefaults to any Dapr package: the Dapr packages read the declaration themselves.
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
2. `Encina.OpenTelemetry` subscribes the new source and meter (one-line edit of its `WithEncina` list and `AddEncinaInstrumentation`), recorded in pull request 3b, which also edits the `Encina.OpenTelemetry` manifest entry and adds a unit test that builds a tracer and meter provider and asserts both are subscribed.

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

1. Unit, guard, property (key injectivity), integration (daprd and a queryable component), DI tests per pull request. Load and benchmark: not applicable to the first delivery (the stores are I/O bound to the sidecar); stated in the pull request.
2. **Per-file targets** (new files; each reached by its own pull request):

   | File (pull request) | Unit | Guard | Contract | Property | Integration | Justification |
   |------|-----:|------:|---------:|---------:|------------:|---------------|
   | `EncinaDaprOptions.cs`, validator (1) | 95 | 90 | 0 | 0 | 0 | Every rule and the token hiding asserted |
   | `ServiceCollectionExtensions.cs` (2) | 95 | 90 | 0 | 0 | 0 | Registration, guards and the DI test |
   | `DaprSidecarHealthCheck.cs` (2) | 95 | 85 | 0 | 0 | 0 | Healthy, unhealthy and exception branches |
   | `DaprErrors.cs`, `DaprLog.cs` (2, with 879 and 880 added in 3) | 100 | 0 | 0 | 0 | 0 | Constants and generated methods, each called by an emitter of the pull request that declares it (878 by the health check, 879 and 880 by the invoker); no guarded parameter |
   | `EventIdRanges.cs` (2, 4b, edits) | 95 (the manifest's current unit target, kept) | 0 | 0 | 0 | 0 | Two new fields read by the allocation architecture test; no target lowered |
   | `Encina.OpenTelemetry` `ServiceCollectionExtensions.cs` (3b, edit) | 90 (current, kept) | 35 (current, kept) | 0 | 0 | 0 | Two subscription lines; the new unit test asserts both subscriptions; the manifest's current unit 90 and guard 35 are kept, not lowered |
   | `EncinaAspireEnvironment.cs` (8b, edit) | 100 | existing kept | 90 | 0 | 0 | Two new keys, written in both modes and read by the parity case |
   | `DaprAspireDeclarationOptionsSetup.cs` and the state-store setup (8a; Contract raised in 8b) | 95 | 0 | 0 in 8a, 90 from 8b | 0 | 0 | Internal setups run by the unit tests of 8a; the parity case and the contract project references that reach them are in 8b, which raises the Contract target; no public parameter for guard tests |
   | The state-store `ServiceCollectionExtensions.cs` (`AddEncinaDaprStateStore`, 4c) | 95 | 90 | 0 | 0 | 0 | Registration of the options and the check, guards and the DI test |
   | `DaprStateStoreLog.cs` (4b, 5) | 90 | 0 | 0 | 0 | 0 | 884 called by the start check (4b), 885 and 886 by the saga store (5) |
   | `DaprServiceInvoker.cs` (3) | 92 | 85 | 0 | 0 | 70 | A fake sidecar covers every branch; `daprd` covers the real path |
   | `DaprStateKey.cs` (4) | 100 | 0 | 0 | 90 | 0 | Pure function; the property test proves injectivity; internal static |
   | `DaprStateStoreStartCheck.cs` (4b) | 92 | 0 | 0 | 0 | 60 | Fake metadata for branches; one integration case |
   | `SagaStoreDapr.cs` (5) | 90 | 85 | 0 | 0 | 70 | A fake client for every operation; a queryable component in Docker; Contract 0 because no behavioral suite for `ISagaStore` exists on `main` (an issue file creates the three suites, after which the Dapr stores join and the target is raised) |
   | `InboxStoreDapr.cs` (6) | 90 | 85 | 0 | 0 | 70 | Same, for `IInboxStore` |
   | `OutboxStoreDapr.cs` (7) | 90 | 85 | 0 | 0 | 70 | Same, for `IOutboxStore` |
   | `DaprDiagnostics.cs` (3b) | 90 | 0 | 0 | 0 | 0 | Listener-based unit tests of tags |
   | `IDaprServiceInvoker.cs` (3) | 0 | 0 | 0 | 0 | 0 | An interface with no executable code; covered through `DaprServiceInvoker` |
   | `DaprStateStoreOptions.cs`, `DaprInboxStoreOptions.cs` (4) | 95 | 90 | 0 | 0 | 0 | Defaults and validation asserted; the Inbox-only TTL validated positive |
   | `DaprSagaState.cs` and its factory (5), the Inbox and Outbox entities (6, 7) | 90 | 0 | 0 | 0 | 0 | Entities and factories exercised by the store tests; no guarded public parameter |
   | The state-store registration classes (`AddEncinaSagaDapr` 5, `AddEncinaInboxDapr` 6, `AddEncinaOutboxDapr` 7) | 95 | 90 | 0 | 0 | 0 | Registration, guards and the two-order DI tests |
   | `EncinaDaprHostingOptions.cs` and the `WithEncinaDapr` extension (8b) | 95 | 90 | 90 | 0 | 0 | Options defaults and the writer on the real Aspire model; the parity case reads them into the Dapr setups |
   | `DaprStateStoreErrors.cs` (4b, edited in 5) | 100 | 0 | 0 | 0 | 0 | Constants, each code created by a factory the emitters call in the same pull request |

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
- Tests execute real code; the stores are tested with a fake client and against a real component; never lower a manifest target.
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

1. XML docs with `<example>`; README of the two packages and a guide (through `docs-writer`): endpoints and the token, the allowlist, the queryable-component requirement, the atomicity limit, what is not delivered (the multi-tenancy fail-closed rule of Design Choice 11 included); `docs/INVENTORY.md`; the issue files of this run, opened by the orchestrator with this plan: `dapr-workflows-actors-bindings-secrets` (workflows, actors, bindings, secrets, handler invocation), `messaging-store-contract-suites` (the three behavioral store suites) and `dapr-tenant-aware-stores` (tenant-qualified keys); `changelog.d/387-dapr-integration.added.md` (through `mechanical-fixer`); an ADR for the store semantics if the maintainer wants it (number assigned when the ADR pull request opens).
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
| 2 | Extension class, health check, `DaprErrors`, `DaprLog`, `EventIdRanges.cs` edit, `docs/engineering/eventid-block-registry.md` edit, `Encina` PublicAPI, `Encina.json`, allocation map edit, Dapr PublicAPI edit and manifest edit, unit, guard and DI test files | 14 | 1,000 |
| 3 | `IDaprServiceInvoker`, `DaprServiceInvoker`, extension edit, `DaprErrors` edit (the four invoker codes), `DaprLog` edit (879, 880), Dapr PublicAPI edit and manifest edit, unit, guard, `DaprFixture`, collection file, integration test, integration csproj edit | 13 | 1,050 |
| 3b | `DaprDiagnostics`, invoker edit (spans and counters), `Encina.OpenTelemetry` `ServiceCollectionExtensions.cs` edit, `Encina.OpenTelemetry.json` manifest edit, subscription unit test, diagnostics unit test, Dapr manifest edit | 7 | 500 |
| 4 | State-store project file, two PublicAPI files, `Encina.slnx` edit, `DaprStateStoreOptions`, `DaprInboxStoreOptions`, `DaprStateKey`, manifest, three test project references, unit, guard and property test files | 14 | 1,000 |
| 4b | `DaprStateStoreStartCheck`, `DaprStateStoreLog` (884), `DaprStateStoreErrors` (`state_query_unsupported`, `state_requires_tenant_optin`), `EventIdRanges.cs` edit, `docs/engineering/eventid-block-registry.md` edit, `Encina` PublicAPI, `Encina.json`, state-store manifest edit, allocation map edit, queryable component file for the fixture, `DaprFixture` edit, unit test, integration test (the check constructed directly against the fixture) | 13 | 1,000 |
| 4c | The state-store `ServiceCollectionExtensions.cs` (`AddEncinaDaprStateStore`, public), state-store PublicAPI edit, state-store manifest edit, unit test (options validation, check registration), guard test, DI test | 6 | 450 |
| 5 | `SagaStoreDapr`, saga entity, saga factory, `AddEncinaSagaDapr` (edit of the state-store `ServiceCollectionExtensions.cs`), `DaprStateStoreLog` edit (885, 886), `DaprStateStoreErrors` edit (`state_conflict`), PublicAPI and manifest edits, unit, guard, DI (two orders) and integration tests (the registration calls `AddEncinaDaprStateStore` of 4c) | 12 | 1,100 |
| 6 | `InboxStoreDapr`, entity, `AddEncinaInboxDapr` (edit of the registration file), PublicAPI, manifest, unit, guard, DI (two orders) and integration tests | 9 | 800 |
| 7 | `OutboxStoreDapr`, entity, `AddEncinaOutboxDapr` (edit of the registration file), PublicAPI, manifest, unit, guard, DI (two orders) and integration tests | 9 | 900 |
| 8a | The Dapr side of the declaration: `DaprAspireDeclarationOptionsSetup` (`Encina.Dapr`), the state-store setup, `AddEncinaDapr` registration edit (`Encina.Dapr/ServiceCollectionExtensions.cs`), state-store registration edit, `DaprErrors` edit and `DaprStateStoreErrors` edit (`encina.dapr.invalid_declaration`, `ErrorCategory.Validation`, one per package), setups unit test, DI test edit, two Dapr manifest edits (the setups' Contract target is 0 here and raised in 8b) | 10 | 700 |
| 8b | The hosting side: `EncinaDaprHostingOptions`, `WithEncinaDapr` extension, `EncinaAspireEnvironment` edit, Hosting PublicAPI and manifest edits, hosting unit test, hosting guard test, `EnvironmentContractTests` edit (the writer into the Dapr setups), `Encina.ContractTests.csproj` edit (references to the Dapr packages), two Dapr manifest edits (raising the setups' Contract target from 0 to 90) | 11 | 700 |
| 9 | Two READMEs, guide, inventory line, changelog fragment, two justification files (load, benchmark) | 7 | 380 |

PR 1 and PR 2 are sequential; PR 3 needs PR 2; PR 3b needs PR 3; PR 4 needs PR 2; PR 4b needs PR 4 and PR 3 (the fixture); PR 4c needs PR 4b; PR 5 needs PR 4c; PRs 6 and 7 need PR 5; PR 8a needs PRs 2 and 4b; PR 8b needs PR 8a and the code of #416 and #440 (their plans are merged; `src/Encina.Aspire.Hosting` is not on `main` yet); PR 9 is last. `Encina.Aspire.ServiceDefaults` (#417) is not touched. Interfaces are not changed, so no implementer or test double is touched; no behavioral contract suite exists for the three store interfaces on `main` (only reflection checks under `tests/Encina.ContractTests/EntityFrameworkCore`), so none is counted here and an issue file of this run asks for the suites. Every row is counted by file name; a pull request that measures over 15 files or 1,500 lines when briefed is split again.

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
| Edits of shared files | 6 | `Directory.Packages.props`, `EventIdRanges.cs`, `Encina.OpenTelemetry`, `EncinaAspireEnvironment` (`Encina.Aspire.ServiceDefaults` is not touched) |
| Public API and manifests | 14 | Three packages' PublicAPI and manifests, `Encina` PublicAPI, `Encina.json` |
| Tests and project files | 24 | Unit, guard, DI, contract, property, integration, fixture, csproj references, allocation map |
| Documentation | 7 | Two READMEs, guide, inventory, fragment, two justification files |
| **Total** | **by the split table: 13 + 14 + 13 + 7 + 14 + 13 + 6 + 12 + 9 + 9 + 10 + 11 + 7 = 138 file touches over 13 pull requests** | The split table is the authority |

---

## Combined AI Agent Prompts

<details>
<summary><strong>Full combined prompt for all phases</strong></summary>

```text
PROJECT CONTEXT:
- Encina is a pre-1.0 .NET 10 / C# 14 library; Either<EncinaError, T>; no [Obsolete], no compatibility layers.
- Issue #387: Dapr integration. Plan: docs/plans/dapr-integration-implementation-plan-387.md; D1, D2, D5, D7, D8, D9, D10, D11, D12, D13 and D14 are recommended, pending the maintainer.

IMPLEMENTATION OVERVIEW (9 pull requests, each builds alone):
1-3. Encina.Dapr: options, registration, health, invoker, diagnostics (Phases 1-3, 9).
4-7. Encina.Dapr.StateStore: keys, start check, Saga, Inbox, Outbox stores (Phases 4-6).
8a/8b. The Aspire declaration: the Dapr-side setups, then the hosting side (Phase 7). 9. Documentation (Phase 11).

KEY PATTERNS:
- Staged writes applied in one state transaction; ETags; one injective key builder; no tenant in the key (Design Choice 11).
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
| 3 | Structured Logging | ✅ | Phases 2 to 5: EventIds 878-880 and 884-886 (884 is the state-store start check, including the multi-tenancy fail-closed case) |
| 4 | Health Checks | ✅ | Phase 2: `DaprSidecarHealthCheck`; Phase 4 start check of the component |
| 5 | Validation | ✅ | Options, app ids, methods, keys |
| 6 | Resilience | ✅ | An invocation timeout; retries and circuit breaking stay the sidecar's resiliency policy, configured in Dapr |
| 7 | Distributed Locks | ❌ | Not applicable: no Dapr lock provider (Design Choice 6, AGENTS.md section 5 closed set) |
| 8 | Transactions | ✅ | Phases 5 to 6: one state transaction per `SaveChangesAsync`, atomic inside the state store only |
| 9 | Idempotency | ✅ | Phase 6: the Inbox store is the deduplication store |
| 10 | Multi-Tenancy | ⏭️ | Deferred (Design Choice 11): the Outbox and Saga contracts carry no tenant, so no tenant-qualified key can be built honestly; an issue file of this run asks for tenant-aware Dapr stores once the Messaging contracts carry one |
| 11 | Module Isolation | ❌ | Not applicable: a sidecar is per service, as the issue states |
| 12 | Audit Trail | ⏭️ | Deferred as in the issue: invocation audit; the issue file of this run for Dapr follow-ups lists it |

---

## Prerequisites & Dependencies

- The code of #416 and #440 (only their plans are merged; `src/Encina.Aspire.Hosting` is not on `main` yet) for pull request 8; #720's `SagaOrchestrator.CommitAsync` is the commit owner the stores rely on; the maintainer's answer to Design Choice 1 (and #242's plan) before pull request 3 if #242 is to build on `Encina.Dapr`.
- `Dapr.Client` is already pinned; `CommunityToolkit.Aspire.Hosting.Dapr` is not added (Design Choice 7 option A).
- Issue file of this run: Dapr workflows, actors, bindings, secrets and handler invocation (Design Choice 5), covering the Dapr audit item.

---

## Next Steps

1. The maintainer answers D1, D2, D5, D7, D8, D9, D10, D11, D12, D13 and D14 when work on #387 starts; the answers go into `## Maintainer Decisions` and the markers are removed. If Phase 1 finds the query API still alpha, D2 is asked again before pull request 4.
2. Pull requests 1 to 3, then 4 to 7, then 8 and 9.
