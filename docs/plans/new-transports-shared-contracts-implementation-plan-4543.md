# Implementation Plan: the five new transports adopt the batch, priority and outbox-adapter contracts and carry tenant and module headers

> **Issue**: [#4543](https://github.com/dlrivada/Encina/issues/4543)
> **Type**: Feature
> **Verified against**: origin/main `f9063538e` (2026-11-27)
> **Complexity**: High (7 phases, 25 pull requests of 4 to 12 files each; the plan itself is one file)
> **Estimated Scope**: about 1,100 lines of production code and about 2,800 lines of tests across the five packages and `Encina.Messaging`
> **Family**: transport-new-1 follow-ups (#4543, #4544, #4545). Packages: `Encina.Redis.Streams` (#240), `Encina.Pulsar` (#239), `Encina.ActiveMQ` (#241), `Encina.GoogleCloudPubSub` (#237), `Encina.AmazonEventBridge` (#238).

---

## Summary

The five transports planned in #237 to #241 publish, subscribe, dead letter, report health and emit telemetry, and nothing more. This issue makes them adopt the contracts the ten existing transports receive from the plans of #252 (batch), #251 (priority) and #2385 (outbox adapter), and carry the tenant and module of the persisted request context in message headers (#1164). State of `origin/main` (`f9063538e`):

- None of the five packages is on `main` (`src/` has no `Encina.Redis.Streams`, `Encina.Pulsar`, `Encina.ActiveMQ`, `Encina.GoogleCloudPubSub` or `Encina.AmazonEventBridge`); their plans are, and so are the plans of #251, #252 and #2385. None of the four contracts is on `main` either: `IBatchPublisher`, `TransportPriorityRouter`, `IOutboxTransportPublisher` and `OutboxRoute` do not exist in `src/` (searched with the Grep tool), and #1164 is open without a plan on `main`. This plan therefore waits for them (Prerequisites) and every pull request below builds only after its prerequisites are on `main`.
- Tenancy today: `MultiTenancyMarker` (`src/Encina/Abstractions/MultiTenancyMarker.cs`) is the container signal that multi-tenancy is on, and `DeadLetterManager` (`src/Encina.Messaging/DeadLetter/DeadLetterManager.cs:22-27,40,78`) is the precedent for an optional `MultiTenancyMarker? tenancy = null` constructor parameter and a deny when tenancy is on and no tenant is resolved. `ITenantContext` and `IModuleContext` do not exist (searched); `IRequestContext` carries `TenantId` and no module (`src/Encina/Abstractions/IRequestContext.cs`; `ModuleId` is #2442, messaging entities are #747). The telemetry tag key is `encina.tenant_id` (`src/Encina/Recoverability/RetryMetrics.cs:89`; ADR-054 D4).
- The outbox route of #2385 is `OutboxRoute(Transport, Destination, TenantId, CorrelationId)` with no module member, and its Design Choice 10 ships adapters whose headers are absent until #1164 fills the route. The batch item of #252 is `BatchPublishItem<TMessage>(Message, Key, MessageId)` with no header member (Design Choice 5 below).

Delivered: one shared header contract in `Encina.Messaging` (`TransportContextHeaders`, owned by this plan), and per transport a batch implementation, a priority mapping, an outbox adapter that stamps the tenant header and the ADR-054 D4 tenant tag, and a module header, each with unit, guard, contract and integration tests, then documentation. This plan writes the helper for the five transports; #4363 (open, no plan yet) covers four other new transports and its own plan decides whether to reuse it (Design Choice 9 records the open gap on tenant-scoped destinations). Structured logging stays Included as the issue says: a denial or a failed stamp is returned as a `Left` and recorded by each transport's existing publish-failed log message with its error code, so no new log call is added.

**Provider category**: Messaging transports (10 existing, 6 planned; AGENTS.md section 5): the feature touches provider-specific code, so it is implemented on all five new transports; the cloud-tied ones (GoogleCloudPubSub, AmazonEventBridge) are tested with the emulators of #237 and #238.

**Shared pieces and order**: this plan owns `TransportContextHeaders` (Phase 1) and the forwarding of `BatchPublishItem.Headers` in the eight transports of #252 (Phase 2, pull requests 2b and 2c). #240 owns the conformance base; #252, #251 and #2385 own their contracts and contract test bases; #4392 and #4393 own consumer and publisher resilience (cited, not planned here). Order: Phase 1, then per transport in the family order Redis.Streams, Pulsar, ActiveMQ, GoogleCloudPubSub, EventBridge, contract by contract (Pull request split).

---

## Design Choices

<details>
<summary><strong>1. Where the shared header contract lives and who owns it</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `TransportContextHeaders` in `Encina.Messaging/Transport/`, owned by this plan, used by the five adapters and by the #4363 plan** | One definition of names, rules and errors; the provider-coherence rule | The first plan to land owns it, the other cites it |
| **B) One copy per transport package** | No shared file | Five diverging definitions of the same security rule |

### Chosen Option: **A, one shared helper in `Encina.Messaging`**

### Rationale

AGENTS.md section 3 (provider coherence) puts abstractions shared by providers in `Encina.Messaging`, which the transport packages reference (`src/Encina.AmazonSQS/Encina.AmazonSQS.csproj` does, and the plans of #237, #238, #239 and #241 name the reference in their pull request 1; the #240 plan references only `Encina`, so its batch pull request adds the `Encina.Messaging` project reference to `Encina.Redis.Streams.csproj`, counted in Phase 2).

**Class:** Settled
**Settled by:** AGENTS.md section 3 (provider coherence: abstractions and options live in `Encina.Messaging`) and the #4363 Proposed Fix ("one design for all transports")

</details>

<details>
<summary><strong>2. Header wire names: one name safe on every carrier or one name per carrier (Dilemma D2)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) One name for every carrier made only of lowercase letters and digits: `encinatenantid`, `encinamoduleid`** | Valid as a CloudEvents extension attribute (names are lowercase letters and digits only), as a JMS property identifier (a dash would read as subtraction in a selector, which #241 adds), as a Pub/Sub attribute, a Pulsar property and a Redis stream field; one constant | Less readable than a dashed name |
| **B) `encina-tenant-id` and a per-carrier translation table (`encina_tenant_id` for JMS, `encinatenantid` for CloudEvents)** | Readable default | A translation table to test per carrier; consumers must know the table |

### Chosen Option: **A, lowercase alphanumeric names** (recommended, pending the maintainer)

### Rationale

A wins because two of the five carriers reject the dashed form, so B needs a table to rescue one name; B wins if the maintainer wants the dashed name of #251's `encina-priority` (its T3, also pending) on every carrier. The same constraint applies to #251's `encina-priority` (its T3 recommends the dashed name on every carrier) on ActiveMQ and EventBridge, so D2 and #251 T3 are asked as one decision that names the rule per carrier; the priority pull requests (Phase 3) wait for it. The ten existing transports take their tenant and module names from the same answer: the #2385 adapters have no header-name constants of their own, so the constants of Phase 1 are the single definition, adopted there when they land.

**Class:** Dilemma

</details>

<details>
<summary><strong>3. Missing tenant while multi-tenancy is on (Dilemma D3)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) The adapter returns a `Left` `encina.messaging.transport_tenant_required` (`InvalidRequest`, ADR-062 R3) when `MultiTenancyMarker` is registered and the route has no tenant; no opt-out; with tenancy off nothing is stamped and nothing is denied** | Fails closed (AGENTS.md section 3, gates deny on missing context); the reserved `default` segment is never invented | A platform-level event published with no tenant is refused until an opt-out exists |
| **B) As A plus an explicit `AllowTenantlessMessages` option that is logged at startup** | Covers platform-level events | A second switch to document and test; every caller of it is a hole |

### Chosen Option: **A, deny with no opt-out** (recommended, pending the maintainer)

### Rationale

Both options fail closed and keep the opt-out explicit and logged as the rule requires, so the rule does not decide; A wins while no tenantless integration event exists, B wins for an application that publishes platform events through the same outbox. The tenant id is copied verbatim (never trimmed, lower-cased or defaulted); whitespace-only counts as no tenant, as in `DeadLetterManager.AmbientTenantId`. No tenant-qualified name or id is built here (headers only; per-tenant destinations are not introduced, #2385 Design Choice 10), so the reserved `default` segment and injective encoding of #4363 do not apply. Cancellation propagates and is never a `Left`.

**Class:** Dilemma

</details>

<details>
<summary><strong>4. Source of the tenant and module values (Dilemma D4)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Explicit only: the adapter stamps from `OutboxRoute` (the persisted context of #1164); a direct publisher call stamps nothing the caller did not put in `headers`** | No ambient state; the values are the persisted ones; #1164 rejects per-scope `IRequestContext` snapshots as stale | A direct publish inside a request carries no tenant unless the caller stamps it |
| **B) The publisher also stamps from `IRequestContextAccessor` on every direct publish** | Direct publishes carry the tenant automatically | Ambient state in a transport; two sources that can disagree on the outbox path |

### Chosen Option: **A, explicit only** (recommended, pending the maintainer)

### Rationale

A wins because ADR-028 sends integration events through the outbox, whose route is the trustworthy source (#2385 Design Choice 10: no other source is trustworthy today); B wins for applications that publish directly from request handlers and want the tenant without a helper call. The tenant tag of ADR-054 D4 reads the same stamped header, so span and header never disagree. Consequence of A that is stated, not hidden: with tenancy on, a direct publish or a batch with no stamped header passes tenantless while the outbox adapter denies, so only the outbox path is fail closed; and the header is unauthenticated metadata (as #251 T8 says of priority): consumers must not use it for authorization or trust it for isolation, and the documentation says so.

**Class:** Dilemma

</details>

<details>
<summary><strong>5. Headers on a batch (Dilemma D5)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `BatchPublishItem<TMessage>` of #252 gains an optional trailing `IReadOnlyDictionary<string,string>? Headers`** | One tenant per item, so a mixed-tenant batch stays correct; the adapter and the batch use the same helper | An additive change to a contract owned by #252; its eight transports must forward the headers or return a `Left`, which is work this plan owns (pull requests 2b and 2c) |
| **B) `BatchPublishOptions` carries one tenant for the whole call** | Smaller change | A batch of several tenants must be split by the caller |
| **C) The batch reads the ambient context** | No contract change | Ambient state; contradicts Design Choice 4 |

### Chosen Option: **A, item-level headers** (recommended, pending the maintainer)

### Rationale

A wins because tenancy is a property of a message; B wins if every batch is one tenant (a per-tenant outbox processor). A lands in `Encina.Messaging` as the first pull request after #252's contract pull request, before the five batch pull requests; if #252's pull request 1 has not merged, the member is added there instead.

**Class:** Dilemma

</details>

<details>
<summary><strong>6. Native mechanism or one generic implementation of batch and priority (Dilemma D6)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Each transport uses its broker's native mechanism, to be confirmed against vendor documentation at implementation as #251 does: `XADD` pipelined in one batch on Redis.Streams; the producer's send batching on Pulsar (client per #239 D2); pipelined sends on one session on ActiveMQ; the publisher client's batching on Pub/Sub; `PutEvents` of up to ten entries on EventBridge; priority as a destination per priority on Redis.Streams, Pulsar, Pub/Sub and EventBridge, and as `NMSPriority` on ActiveMQ (#241 verified it in `Apache.NMS` 2.2.0)** | Uses what each broker is good at | Five code paths |
| **B) One generic loop of single publishes with bounded concurrency in a shared helper** | One tested implementation | Gives up native batching and priority |

### Chosen Option: **A, native mechanisms** (recommended, pending the maintainer)

### Rationale

A wins for throughput; B wins if the maintainer prefers one tested loop. The partial-failure rule of #252 holds under both: a failed item is a `BatchItemFailure` with the code `<TRANSPORT>_BATCH_ITEM_FAILED` (`Internal`, ADR-062 R4), never success; a failure of the whole call is a `Left` `<TRANSPORT>_BATCH_FAILED` (`Internal` R4, or `Unavailable` R7 when the broker is unreachable); an unmapped or unsupported priority is a `Left` from the router of #251, never ignored.

**Class:** Dilemma

</details>

<details>
<summary><strong>7. Priority on EventBridge (Dilemma D7)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Capability `Destination`: an event bus per priority from the validated mapping; the bus name goes into `EventBusName` of the put** | Same capability model as Pub/Sub and Pulsar; rules per bus can route by priority | Five buses to provision (the provisioner of #238 creates a non-default bus) |
| **B) No priority support: with the feature enabled the publisher returns a `Left` `EVENTBRIDGE_PRIORITY_UNSUPPORTED` (`InvalidRequest`)** | Honest; no extra buses | The transport has no priority |

### Chosen Option: **A, a bus per priority** (recommended, pending the maintainer)

### Rationale

A wins when priority matters for routing; B wins for an application with one bus. Both comply with the rule that a transport never silently ignores priority (AGENTS.md section 5, via #251).

**Class:** Dilemma

</details>

<details>
<summary><strong>8. Tenant tag on telemetry</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Tag `encina.tenant_id` on the publish spans and counters of each transport only when `MultiTenancyMarker` is registered and a tenant header was stamped** | Follows ADR-054 D4; no tag when tenancy is off | Cardinality grows with tenants (documented in the observability guide, #906) |
| **B) Never tag** | No cardinality | Violates ADR-054 D4 |
| **C) Always tag, `none` without a tenant** | Uniform | Emits a tenant tag with tenancy off |

### Chosen Option: **A, tag only when tenancy is on**

### Rationale

ADR-054 D4 states the tenant id is a span attribute and a metric tag in every package when multi-tenancy is enabled and no tenant tag is emitted when it is off; only A satisfies both clauses.

**Class:** Settled
**Settled by:** ADR-054 D4 (`docs/architecture/adr/054-observability-conventions-and-trace-propagation.md` line 50) and SPEC-002 AC-044

</details>

<details>
<summary><strong>9. Tenant-scoped destinations for the nine new transports (Dilemma D9)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Headers only, as the issue says; tenant-scoped destinations (stream keys, topics, partition keys) are not built** | No key scheme invented; no tenant-qualified id exists, so the reserved `default` segment and injectivity do not arise | The tenant-scoped stream keys that #240 D10 and #239 D8 defer to this family have no owner; #4363 item 1 (scoped topics and partition keys for Dapr, ZeroMQ, Event Hubs, Kinesis) is planned separately |
| **B) Also scoped destinations for all nine transports, with the reserved `default` segment and an injective encoding defined once in `Encina.Messaging`** | One design for nine transports; isolation by destination | A key scheme to invent and test on nine brokers, which this issue does not ask for; a hash-tag rule for Redis.Streams |

### Chosen Option: **A, headers only** (recommended, pending the maintainer)

### Rationale

A wins because the issue's scope is headers; B wins for a tenant-isolated deployment that needs destination isolation. Under A the known gap is recorded here: #240 D10 and #239 D8 point to "the family issue" for scoped destinations and this plan does not deliver them; under B they become a Phase 8 of this plan and a joint design with #4363.

**Class:** Dilemma

</details>

---

## Implementation Phases

### Phase 1: Shared header contract (pull request 1)

<details>
<summary>Tasks</summary>

1. `src/Encina.Messaging/Transport/TransportContextHeaders.cs` (static class): constants `TenantId` and `ModuleId` (names per D2), `Stamp(IReadOnlyDictionary<string,string>? headers, string? tenantId, string? moduleId, bool multiTenancyEnabled)` returning `Either<EncinaError, IReadOnlyDictionary<string,string>>` (a new dictionary, the caller's input is never mutated; a caller header that already uses a reserved name is overwritten by the stamped value only when the value is the same, else `Left`), `TryReadTenantId(IReadOnlyDictionary<string,string>?)` and `TryReadModuleId`. Rules: tenancy on and no tenant is `Left` (D3); tenancy off stamps no tenant; module is stamped only when present and never denied (no module-isolation marker exists on `main`, searched).
2. `TransportContextErrorCodes.cs`: `encina.messaging.transport_tenant_required` `InvalidRequest` (R3) and `encina.messaging.transport_header_conflict` `Validation` (R3, a reserved header supplied with a different value); constants, categorized at the call site; no log call (the transport's own publish-failed log records the code).
3. `src/Encina.Messaging/PublicAPI.Unshipped.txt` (through `mechanical-fixer`), the `Encina.Messaging.json` manifest rows (through `mechanical-fixer`), tests: `TransportContextHeadersTests` (unit), `TransportContextHeadersGuardTests` (guard: all parameters are nullable, so each null is a valid-result test, plus the empty-string and whitespace cases), `TransportContextHeadersPropertyTests` (FsCheck: stamping never mutates the input, tenant read-back equals the input verbatim). This pull request has 7 files and about 400 lines; it builds alone.

</details>

<details>
<summary>Prompt for AI Agents — Phase 1</summary>

```text
CONTEXT: Encina.Messaging/Transport is where #252 puts IBatchPublisher. MultiTenancyMarker (src/Encina/Abstractions/MultiTenancyMarker.cs) is the tenancy-on signal; ITenantContext does not exist.
TASK: Add TransportContextHeaders and its error codes with unit, guard and property tests.
KEY RULES: named ADR-062 category on every EncinaErrors call (missing tenant InvalidRequest R3); the tenant id is copied verbatim, never defaulted to "default"; never mutate the input dictionary; constant error messages; no tenant value in any exception message.
REFERENCE FILES: src/Encina.Messaging/DeadLetter/DeadLetterManager.cs, src/Encina.Messaging/DeadLetter/DeadLetterErrorCodes.cs, docs/architecture/adr/062-error-category-contract.md.
```

</details>

### Phase 2: Batch adoption (pull request 2, then pull requests 3 to 7, one per transport)

<details>
<summary>Tasks</summary>

1. Pull request 2 (in `Encina.Messaging`, only when D5 is answered A): add `Headers` to `BatchPublishItem<TMessage>`, with the `PublicAPI.Unshipped.txt` line, the unit and guard test edits and the `Encina.Messaging.json` row; pull request 2 has 5 files (`BatchPublishItem.cs`, `PublicAPI.Unshipped.txt`, a unit test edit, a guard test edit, the manifest).
1b. Pull requests 2b (Kafka, AmazonSQS, AzureServiceBus, RabbitMQ) and 2c (NATS, MQTT, Redis.PubSub, InMemory), a declared sweep of the same change over the eight publishers of #252 (no new logic): each `PublishBatchAsync` forwards a non-empty `Headers` of an item into the broker's native header carrier, or returns `<T>_BATCH_HEADERS_UNSUPPORTED` (`InvalidRequest`, R3) where the carrier has none (Redis.PubSub and InMemory have no headers, as #251 notes for Redis.PubSub), so a header is never dropped silently. Each pull request is 4 publisher edits, 4 unit test edits and 4 manifest edits (12 files, about 500 lines). They need #252's transport pull requests on `main`; if those have not merged, the forwarding is added to them instead.
2. Per transport (pull requests 3 to 7 in the order Redis.Streams, Pulsar, ActiveMQ, GoogleCloudPubSub, EventBridge): `I<T>MessagePublisher : IBatchPublisher`; `PublishBatchAsync` with the native mechanism of D6, chunking to the broker limit (EventBridge ten entries and the 256 KB put limit, Pub/Sub 1,000 messages and 10 MB per publish request, to confirm against vendor documentation), per-item results as `BatchItemFailure`, item headers stamped through `TransportContextHeaders` when present; `<TRANSPORT>_BATCH_ITEM_FAILED` `Internal` (R4) added to the package's `*ErrorCodes.cs`. A cancellation propagates as `OperationCanceledException`, never a `Left`, and is not a failure item.
3. Implementers of the changed interface, counted by search at pick-up: the publisher class, the `Resilient<T>MessagePublisher` decorator and the hand-written `<T>FakePublisher` test double of #4393 (both implement the package publisher interface and break with the new member), and NSubstitute doubles (no edit needed). Order: #4393's pull request for the transport lands first; this pull request then edits the decorator, the fake and the decorator test (3 more files, so 11 files, 12 for Redis.Streams); if #4393 is not on `main` yet, #4393 implements the member when it lands after this pull request. The package's DI registration is unchanged (the publisher is already registered); `Encina.Redis.Streams.csproj` gains the `Encina.Messaging` project reference here.
4. Tests per transport (7 to 8 files each): `<T>PublisherBatchTests` (unit), guard edit, `<T>BatchContractTests` (a subclass of the batch contract base of #252), an integration class in the shared collection of the package (`RedisCollection`, the `PulsarFixture` collection of #239, `ArtemisFixture` of #241, the `PubSubEmulator` collection of #237, `Cloud-LocalStack` of #238), the package manifest edit, and the interface and publisher edits.

</details>

<details>
<summary>Prompt for AI Agents — Phase 2</summary>

```text
CONTEXT: IBatchPublisher and its contract test base (#252) are on main; the transport package and its publisher (its plan) are on main.
TASK: Make the transport's publisher interface inherit IBatchPublisher and implement PublishBatchAsync natively, with unit, guard, contract and integration tests.
KEY RULES: partial failure is a BatchItemFailure, never success; cancellation propagates and is never a Left or a failure item; a Left from the broker for an item is Internal (R4); message ids are never logged; shared integration collections, no per-class containers, no Thread.Sleep.
REFERENCE FILES: docs/plans for #252 and the transport's own plan, src/Encina.Messaging/Transport/IBatchPublisher.cs, tests/Encina.ContractTests/MessageBrokers/.
```

</details>

### Phase 3: Priority adoption (pull requests 8 to 12, one per transport)

<details>
<summary>Tasks</summary>

1. Per transport: `Priority` (`TransportPriorityOptions` of #251) as a property of the package options and its validator rule (every `Destination` mapping is well formed for the broker: on Redis.Streams a stream key that satisfies the key rule of #240 D11 if it is answered A); the publisher calls `TransportPriorityRouter.Route(message, capability, options, defaultDestination)`, attaches the destination and the priority metadata under the wire name of D2, and returns the router's `Left` for an unmapped level. Capabilities (to confirm against vendor documentation at implementation, as #251 states for its own table): Redis.Streams, Pulsar and Pub/Sub `Destination`; ActiveMQ `Native` through `NMSPriority`; EventBridge `Destination` through `EventBusName` (D7).
2. Tests per transport (about 8 files): options and validator unit tests, publisher unit tests, a contract subclass of the priority contract of #251, an integration test that publishes at two priorities and reads the destination or property from the broker, package manifest edit.
3. No new log message: a router `Left` is returned and recorded by the existing publish-failed log with its code; no destination name enters a metric tag (#251 T7).

</details>

<details>
<summary>Prompt for AI Agents — Phase 3</summary>

```text
CONTEXT: TransportPriorityRouter, TransportPriorityOptions and TransportPriorityCapability (#251) are on main; the transport package is on main.
TASK: Add the Priority option and its validation, route every publish through the router and attach destination and metadata.
KEY RULES: an unmapped priority is a Left, never ignored; capability claims are confirmed against vendor documentation before coding; the metadata name follows D2; no destination name in metric tags or logs.
REFERENCE FILES: docs/plans for #251, src/Encina.Messaging/Priority/, the transport's options and publisher.
```

</details>

### Phase 4: Outbox adapters with tenant headers and the tenant tag (pull requests 13 to 17, one per transport)

<details>
<summary>Tasks</summary>

1. Per transport: `<T>OutboxTransportPublisher : IOutboxTransportPublisher` (public sealed, scoped, public constructor `(I<T>MessagePublisher publisher, IOptions<...> options, MultiTenancyMarker? tenancy = null)`), with `TransportName` a documented constant (`redis-streams`, `pulsar`, `activemq`, `googlecloudpubsub`, `amazoneventbridge`); `PublishAsync(IOutboxMessage, object payload, OutboxRoute, CancellationToken)` calls `TransportContextHeaders.Stamp` from `route.TenantId`, passes the outbox id as the broker message id (the `messageId` parameter every package keeps from #240 to #241; on EventBridge the CloudEvents `id`), and maps a transport `Left` to the failed delivery of #2385 with the transport code kept as a detail, never the message. Registered with `TryAddEnumerable(ServiceDescriptor.Scoped<IOutboxTransportPublisher, <T>OutboxTransportPublisher>())` inside `AddEncina<T>` (several transports coexist), with a DI test under `ValidateOnBuild` and `ValidateScopes` in both registration orders.
2. Tenant tag: the package's `*Diagnostics` publish span and counter add `encina.tenant_id` through one private method when the header was stamped and tenancy is on (Design Choice 8); the unit test asserts the tag is absent when the marker is not registered.
3. Tests per transport (about 10 files): adapter unit test with a substituted publisher, a guard test for the adapter constructor (null `publisher`, null `options`; the nullable `tenancy` accepted), the derived class of the outbox contract suite of #2385, an integration test that publishes a stamped message and reads `encinatenantid` from the broker with the broker's own client (consumer-side context is #4392), DI test edit, diagnostics test edit, manifest edit. Transport packages carry no `PublicAPI.*.txt` today (none in NATS, Redis.PubSub, AmazonSQS, Kafka), so none is added.
4. Order after #2385 contracts and #1164 are on `main`; with tenancy on and no tenant in the route the adapter returns the Phase 1 `Left` (D3).

</details>

<details>
<summary>Prompt for AI Agents — Phase 4</summary>

```text
CONTEXT: IOutboxTransportPublisher, OutboxRoute and the outbox contract suite (#2385) and the persisted context (#1164) are on main.
TASK: Add the transport's outbox adapter, its registration, the tenant header stamping and the tenant tag, with unit, guard, contract and integration tests.
KEY RULES: the adapter is registered with TryAddEnumerable; the outbox id is the broker message id; tenancy on and no tenant denies (InvalidRequest R3); the tenant id is never defaulted; no tenant value in logs or error messages; a transport Left fails the delivery, never success.
REFERENCE FILES: docs/plans for #2385, src/Encina.Messaging/Transport/TransportContextHeaders.cs, the transport's publisher and ServiceCollectionExtensions.cs.
```

</details>

### Phase 5: Module headers (pull requests 18 to 22, one per transport)

<details>
<summary>Tasks</summary>

1. Starts only when `OutboxRoute` carries a module (#2442 puts `ModuleId` on `IRequestContext`, #747 on the messaging entities, #1164 persists it); until then the helper accepts a `moduleId` parameter and the adapters pass `null`.
2. Per transport (4 files): the adapter passes `route.ModuleId` to `Stamp`, the adapter unit test and the integration test gain the module read-back, the package manifest edit. A module is stamped only when present and never denied.

</details>

<details>
<summary>Prompt for AI Agents — Phase 5</summary>

```text
CONTEXT: OutboxRoute carries a ModuleId after #2442, #747 and #1164.
TASK: Pass the route's module to TransportContextHeaders.Stamp in the transport's adapter and extend its tests.
KEY RULES: no module is defaulted; absence is not an error; no new log message.
REFERENCE FILES: the transport's adapter, tests/Encina.UnitTests for the adapter.
```

</details>

### Phase 6: Cross-cutting integration and observability

<details>
<summary>Tasks</summary>

1. Verify the matrix below against the merged code; the tenant tag is delivered in Phase 4, no new `ActivitySource`, `Meter` or log message is created (the five packages keep their ranges 40650-40699).
2. Confirm every new error code has its ADR-062 category (`encina.messaging.transport_tenant_required` `InvalidRequest`, `encina.messaging.transport_header_conflict` `Validation`, the five `<TRANSPORT>_BATCH_ITEM_FAILED` `Internal`) and that no log call or tag carries a payload, a message id or `EncinaError.Message`.

</details>

<details>
<summary>Prompt for AI Agents — Phase 6</summary>

```text
CONTEXT: The five transports adopt the contracts and stamp tenant and module headers.
TASK: Verify the cross-cutting matrix against the code and fix any gap.
KEY RULES: AGENTS.md sections 3, 5, 6, 7; ADR-054 D4; ADR-062 categories.
REFERENCE FILES: AGENTS.md section 6, docs/architecture/adr/054-observability-conventions-and-trace-propagation.md.
```

</details>

### Phase 7: Documentation and finalization (pull request 23)

<details>
<summary>Tasks</summary>

Per-file targets, entered in each package manifest by the pull request that adds the tests (never lowered; a target needs its test file in the same pull request; rows of the five packages are created by their own plans, so "unchanged" below quotes those plans' planned values):

| Pull request | File | unit | guard | contract | property | integration | Justification |
|---|---|:---:|:---:|:---:|:---:|:---:|---|
| 1 | `TransportContextHeaders.cs` | 95 | 30 | 0 | 60 | 0 | Pure functions reached by unit and property tests; guard tests reach the nullable-argument cases. |
| 1 | `TransportContextErrorCodes.cs` | 0 | 0 | 0 | 0 | 0 | Constants have no executable lines. |
| 2 | `BatchPublishItem.cs` | 0 | 0 | 0 | 0 | 0 | A positional record with no executable logic; exercised through the batch tests. |
| 2b, 2c | the eight existing publishers (edited) | measured baseline or higher | measured baseline or higher | unchanged | 0 | unchanged | The sweep adds one forwarding branch per publisher with its unit test; no target is lowered (rows are owned by #252). |
| 3 to 7 | `<T>MessagePublisher.cs` (batch) | 85 | 20 | 50 | 0 | 70 for Redis.Streams, 65 for the other four | Unit values are the package plans' own; the batch contract subclass lands in the same pull request; integration runs a real batch. |
| 8 to 12 | `<T>OptionsValidator.cs` (the validator file of each package) | 100 | 0 | 0 | 0 | 0 | Pure validation, an internal type. |
| 8 to 12 | `<T>MessagePublisher.cs` (priority edit) | 85 | 20 | 50 | 0 | 70 for Redis.Streams, 65 for the other four | The router call adds branches covered by unit tests in the same pull request; the priority contract subclass and the integration test cover the rest; values never lowered. |
| 13 to 17 | `<T>OutboxTransportPublisher.cs` | 90 | 30 | 60 | 0 | 60 | The unit test substitutes the publisher; guard reaches the two constructor checks; the contract suite and one broker round trip cover the rest. |
| 13 to 17 | `ServiceCollectionExtensions.cs` | 85 | 10 | 0 | 0 | 0 | The package plan's own values; the DI test adds one registration. |

1. Five package READMEs, `docs/messaging/transports.md` and `docs/features/transactional-outbox.md` rows through `docs-writer`; changelog fragment `changelog.d/4543-new-transports-shared-contracts.added.md` through `mechanical-fixer` (8 files in all).
2. `dotnet build --configuration Release` with zero warnings; every flag at its target; CRAP of every new method at most 10.

</details>

<details>
<summary>Prompt for AI Agents — Phase 7</summary>

```text
CONTEXT: The contracts are adopted by all five transports.
TASK: Hand the READMEs, the transports page, the outbox feature page and the changelog fragment to docs-writer and mechanical-fixer; re-measure the per-flag targets.
KEY RULES: targets never go down; no hand-typed coverage figures in docs; never edit CHANGELOG.md.
REFERENCE FILES: changelog.d/README.md, docs/messaging/transports.md.
```

</details>

---

## Research

### Relevant Standards & Specifications

| Standard / Spec | Relevance | Notes |
|-----------------|-----------|-------|
| `AGENTS.md` sections 3, 5, 6, 9 | Provider coherence, transport category, 12 functions, coverage | All phases |
| ADR-054 D4 | Tenant attribute and tag only when multi-tenancy is on | Design Choice 8 |
| ADR-062 R3, R4 | Missing tenant `InvalidRequest`; provider failures `Internal` | Error codes |
| ADR-028 | Integration events leave the process through the outbox | Design Choice 4 |
| SPEC-000 DEC-009 | The five transports are in 1.0 scope; cloud ones use emulators | Scope |
| CloudEvents 1.0 attribute naming | Extension names are lowercase letters and digits | Design Choice 2 |

### Existing Encina Infrastructure to Leverage

| Component | Location | Usage in this feature |
|-----------|----------|-----------------------|
| `MultiTenancyMarker` | `src/Encina/Abstractions/MultiTenancyMarker.cs` | Tenancy-on signal for the adapters |
| `DeadLetterManager` | `src/Encina.Messaging/DeadLetter/DeadLetterManager.cs` | Optional-marker constructor precedent |
| `RetryMetrics` | `src/Encina/Recoverability/RetryMetrics.cs:89` | Tag key `encina.tenant_id` |
| Plans of #251, #252, #2385 | `docs/plans/` | Contracts adopted (not on `main` as code) |
| Plans of #237 to #241 | `docs/plans/` | Package shapes, ranges, fixtures |

### Event ID Allocation

| Package | Range | Notes |
|---------|-------|-------|
| none new | none | No new log call (Phase 6). The last two ids of each family block (`Pulsar` 40668-40669, `ActiveMQ` 40678-40679, `GoogleCloudPubSub` 40688-40689, `AmazonEventBridge` 40698-40699) are kept by #239, #241, #237 and #238 for their own health and faults, so they are not allocated here; any id #4544 or #4545 needs from them is proposed in those plans and otherwise to be assigned by the orchestrator. |

### Estimated File Count

| Category | Files | Notes |
|----------|------:|-------|
| Production code | 24 | helper and codes (2), 5 batch edits, 5 options and validator edits, 5 adapters, 5 adapter edits for modules, 2 `Encina.Messaging` edits |
| Tests | 70 | per transport: batch (4), priority (3), adapter (5), module (2); Phase 1 (3) |
| Manifests, DI, PublicAPI | 35 | package manifests and DI edits per pull request |
| Documentation | 8 | five READMEs, transports page, outbox page, changelog fragment |
| **Pull request 1** | **7** | about 400 lines |
| **Pull requests 2b and 2c (sweep)** | **12** | each about 500 lines |
| **Pull requests 3 to 7 (batch)** | **11** (12 for Redis.Streams: the `Encina.Messaging` project reference) | each about 500 lines |
| **Pull requests 8 to 12 (priority)** | **8** | each about 400 lines |
| **Pull requests 13 to 17 (adapter)** | **10** | each about 600 lines |
| **Pull requests 18 to 22 (module)** | **4** | each about 120 lines |
| **Pull request 23** | **8** | about 300 lines; pull request 2 (D5) has 5 files |

Every pull request is at most 15 files and 1500 lines and builds alone once its prerequisites are on `main`.

---

## Combined AI Agent Prompts

<details>
<summary>Combined prompt for all phases</summary>

```text
PROJECT CONTEXT: Encina is a pre-1.0 .NET 10 / C# 14 library; no backward compatibility, zero warnings, ROP (Either<EncinaError, T>). The five new transports (#237 to #241) are on main; the batch (#252), priority (#251) and outbox-adapter (#2385) contracts and the persisted context (#1164) are prerequisites.

IMPLEMENTATION OVERVIEW: PR1 TransportContextHeaders; PR2 batch item headers (if D5 A); PR3-7 batch; PR8-12 priority; PR13-17 outbox adapter, tenant header and tenant tag; PR18-22 module header; PR23 documentation. Dilemmas D2 to D7 and D9 are implemented as the maintainer answers, the recommended options until then.

KEY PATTERNS: tenancy on and no tenant denies (InvalidRequest R3); tenant id verbatim, never defaulted; MultiTenancyMarker as an optional constructor parameter; TryAddEnumerable for adapters; cancellation never a Left; named ADR-062 category; targets never go down; CRAP <= 10.

REFERENCE FILES: docs/plans for #237-#241, #251, #252, #2385, src/Encina.Messaging/DeadLetter, src/Encina/Abstractions/MultiTenancyMarker.cs.
```

</details>

---

## Cross-Cutting Integration Matrix

| # | Function | Status | Notes |
|---|----------|--------|-------|
| 1 | Caching | ❌ | Transports do not cache messages. |
| 2 | OpenTelemetry | ✅ | Tenant tag `encina.tenant_id` only when tenancy is on (Design Choice 8, Phase 4). |
| 3 | Structured Logging | ❌ | No new log call; failures are `Left` values recorded by each transport's existing publish-failed message with the code. |
| 4 | Health Checks | ❌ | No new dependency. |
| 5 | Validation | ✅ | Priority mapping and batch options are validated at startup (Phase 3). |
| 6 | Resilience | ⏭️ | Publisher decorators are #4393 (open) and #742; the batch member added here must be implemented by `Resilient<T>MessagePublisher` (Phase 2 task 3). |
| 7 | Distributed Locks | ❌ | No shared state. |
| 8 | Transactions | ❌ | Acknowledgement based. |
| 9 | Idempotency | ✅ | The outbox id travels as the broker message id (Phase 4). |
| 10 | Multi-Tenancy | ✅ | Tenant header, denial when missing, tenant tag (Design Choices 3, 4, 8). |
| 11 | Module Isolation | ✅ | Module header in Phase 5 after #2442 and #747. |
| 12 | Audit Trail | ❌ | Infrastructure; audit stays with the outbox. |

---

## Prerequisites & Dependencies

- **Blocking**: the five packages through their publisher pull requests (#237 to #241, pull request 2 of each); `IBatchPublisher` and its contract base (#252 pull request 1); `TransportPriorityRouter` (#251 first pull request); `IOutboxTransportPublisher`, `OutboxRoute` and the contract suite (#2385 first pull requests); #1164 for the route values; #2442 and #747 for Phase 5.
- **Related, not blocking**: #4363 (the four other new transports reuse `TransportContextHeaders`), #4392 and #4393 (consumer and publisher resilience), #742, #725, #1609, #906.
- **Sibling plans**: #4544 and #4545 depend on #237 and #238 only.

---

## Next Steps

1. Ask the maintainer D2 (together with #251 T3) to D7 and D9 when work on #4543 starts; record the answers in `## Maintainer Decisions`.
2. Pull request 1: shared header contract (7 files).
3. Pull requests 3 to 17: batch, priority and adapter per transport in the family order, as the prerequisites land.
4. Pull requests 18 to 23: module headers, then documentation.
