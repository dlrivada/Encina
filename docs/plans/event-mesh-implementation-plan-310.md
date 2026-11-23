# Implementation Plan: Encina.EventMesh, an event mesh and event gateway across bounded contexts and transports

> **Issue**: [#310](https://github.com/dlrivada/Encina/issues/310)
> **Type**: Feature
> **Verified against**: origin/main `ce352001f` (2026-11-23)
> **Complexity**: High (9 phases, 9 pull requests of about 13, 15, 11, 14, 14, 14, 15, 7 and 9 files)
> **Estimated Scope**: about 1,900 lines of production code and about 3,000 lines of tests
> **Family**: transport-new-2 (#242 Dapr, #610 ZeroMQ, #611 Azure Event Hubs, #612 Amazon Kinesis, #310 Event Mesh). This issue is a pattern, not a transport: it sits on top of the transports and is planned last in the family; "Shared pieces" below says which plan owns what.

---

## Summary

The issue asks for an event mesh and an event gateway: core abstractions `IEventMesh` and `IEventGateway` with `MeshTarget`, `MeshSubscription` and `MeshEvent`, a local mesh for single-process scenarios, gateway routing with transformation and enrichment, cross-transport routing (Kafka to RabbitMQ to Azure Service Bus), dead letter handling at mesh level, topology discovery and health monitoring, multi-region replication, CloudEvents support and a Dapr pub/sub integration, in three packages (`Encina.EventMesh`, `Encina.EventMesh.CloudEvents`, `Encina.EventMesh.Dapr`). State of `origin/main`:

- None of the three packages exists, and no mesh or gateway code exists (grep of `src` for `EventMesh` and `IEventGateway` finds nothing). Apache EventMesh, which the issue lists as a reference, has no .NET SDK: NuGet's only matches are `FaasNet.EventMesh.*` 0.0.5, third-party packages with fewer than 1,000 downloads, so the issue is Encina's own pattern and depends on no EventMesh SDK.
- There is no `IMessageTransport` in `src/` (grep). Each transport has its own publisher interface and shape: `IRabbitMQMessagePublisher`, `INATSMessagePublisher`, `IAmazonSQSMessagePublisher`, `IInMemoryMessageBus` (`PublishAsync`, `Subscribe(handler)`), `IMQTTMessagePublisher` and `IRedisPubSubMessagePublisher` (publish and `SubscribeAsync`); the others publish only (#1609). The outbox bridge of #2385 (plan merged, code not) defines `IOutboxTransportPublisher.PublishAsync(IOutboxMessage, object payload, OutboxRoute, ct)` per transport, which is row-based and does not fit a gateway that forwards events that were never outbox rows. So "cross-transport routing" needs its own egress and ingress port (Design Choice 7).
- Reusable pieces on `origin/main`: `IContentRouter` (`src/Encina.Messaging/ContentRouter/`) routes typed messages to typed handlers through a `BuiltContentRouterDefinition<TMessage, TResult>`, so it is generic over a message type, while a gateway sees untyped envelopes (Design Choice 6); `IDeadLetterStore.AddAsync(IDeadLetterMessage)` with `IDeadLetterMessageFactory` (`src/Encina.Messaging/DeadLetter/`); `EncinaHealthCheck`; `IEncinaHealthCheck`.
- CloudEvents: no CloudEvents code exists in the library packages (grep finds it only in `Encina.Cdc.Debezium`, which is Debezium's own event format). #260 (CloudEvents format support, open, no plan) proposes `ICloudEvent`, `ICloudEventFactory` and `ICloudEventSerializer`; #238 (EventBridge, plan in review) recommends the CNCF library behind an internal formatter. Verified on 2026-11-23 from NuGet: `CloudNative.CloudEvents` 2.9.0 and `CloudNative.CloudEvents.SystemTextJson` 2.9.0, both Apache-2.0 and net10.0 (Design Choice 9).
- Scope: SPEC-000 REQ-030 and DEC-009 put EIP part 2, the new transports, "locally runnable Aspire and cloud-native integration" in 1.0 and exclude only AI/LLM and cloud services without a free emulator; the mesh needs no cloud service. The issue sits in the milestone v0.19.0 and is planned, not skipped.

Delivered: `Encina.EventMesh` (contracts, pattern matcher, `LocalEventMesh`, `EventGateway` with a route table, typed transformation, loop prevention, an egress and ingress port with a delegate adapter, forwarding with dead lettering, a static topology view, health check, `ActivitySource` and `Meter`, log messages in the new range `EventMesh` 40080-40099) and `Encina.EventMesh.CloudEvents` (mapping between `MeshEvent` and CloudEvents 1.0 in structured JSON mode); unit, guard, property, contract and integration tests (the integration test forwards between the in-process bus and Redis.PubSub on the shared Redis collection); documentation. Not delivered here: the per-transport adapter packages, `Encina.EventMesh.Dapr` (Design Choices 2 and 7), dynamic topology discovery and multi-region replication (Design Choices 10 and 11).

**Provider category**: not a provider-dependent feature in the database sense. It touches the Transports category only through the port of Design Choice 7: the port is the same for every transport, and each transport adapter is a follow-up that keeps the "consistent across every provider of the category" rule (issue file `artifacts/issues/event-mesh-transport-adapters.md`).

**Shared pieces (family)**: the EventId block `40000-40099` of the Transports zone is shared by the five plans (Event ID Allocation). The Dapr adapter depends on `Encina.Dapr.PubSub` of #242 and `Encina.Dapr` of #387. The follow-up issue for tenant and module headers, audit, caching, leader election and resilience of the mesh is `artifacts/issues/event-mesh-deferred-integrations.md`.

---

## Design Choices

<details>
<summary><strong>1. Package layout of the first release</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `Encina.EventMesh` (contracts, local mesh, gateway, port, topology, health) and `Encina.EventMesh.CloudEvents`; the Dapr package is Design Choice 2** | The names the issue gives; CloudEvents is opt-in and carries its third-party dependency alone | Two packages to document |
| **B) One package with CloudEvents inside** | One dependency | Every mesh user takes `CloudNative.CloudEvents` |

### Chosen Option: **A, two packages now**

### Rationale

The issue lists `Encina.EventMesh` and `Encina.EventMesh.CloudEvents` as separate packages, every feature is opt-in (AGENTS.md section 1) and SPEC-000 REQ-030 puts the item in 1.0.

**Class:** Settled
**Settled by:** #310 (package names), AGENTS.md section 1 (pay-for-what-you-use) and SPEC-000 REQ-030

</details>

<details>
<summary><strong>2. The Dapr package (Dilemma D2)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Not in this plan: `Encina.EventMesh.Dapr` is a follow-up issue that starts after #242 pull request 3 (it is a transport adapter over `IDaprMessagePublisher`, Design Choice 7)** | No dependency on two unmerged plans; one adapter pattern for every transport | The issue's third package is not delivered here |
| **B) Deliver it here, after #387 and #242 are merged** | The three packages of the issue ship together | This plan waits for two other plans; it cannot be implemented first |

### Chosen Option: **A, a follow-up issue** (recommended, pending the maintainer)

### Rationale

A wins because the Dapr package is one transport adapter and the adapters are follow-ups (Design Choice 7); B wins if the maintainer wants the mesh released with the Dapr integration the issue names. The issue file is `artifacts/issues/event-mesh-transport-adapters.md`.

**Class:** Dilemma

</details>

<details>
<summary><strong>3. Return types: railway-oriented operations</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Every operation returns `ValueTask<Either<EncinaError, T>>`; `PublishToMeshAsync` returns `Unit`, `SubscribeAsync` returns `Either<EncinaError, IAsyncEnumerable<MeshEvent>>` (creation can fail, the stream ends on cancellation), `GetTopologyAsync` returns `Either<EncinaError, MeshTopology>`; cancellation propagates, never a `Left`** | The rule of the library; the sketch's `IEventGateway` already returns `Either` | Differs from the sketch of `IEventMesh`, which returns bare `ValueTask` and `IAsyncEnumerable` |
| **B) The sketch of `IEventMesh` as written (exceptions for failure)** | Shorter signatures | Exceptions for business failure |

### Chosen Option: **A, `Either` everywhere**

### Rationale

AGENTS.md section 3 requires operations to return `Either<EncinaError, T>` and never to use exceptions for business logic; only A complies. The issue's own `IEventGateway` already follows it.

**Class:** Settled
**Settled by:** AGENTS.md section 3 (Railway Oriented Programming) and ADR-001

</details>

<details>
<summary><strong>4. Delivery inside the local mesh (Dilemma D4)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) A bounded `Channel<MeshEvent>` per subscription (`SubscriptionOptions.Capacity`, default 1,000) with `Wait` when full: `PublishToMeshAsync` awaits a slow subscriber, honoring the caller's cancellation** | Backpressure; no loss; memory is bounded | One slow subscriber slows publishers |
| **B) An unbounded queue per subscription; publishers never wait** | Publishers are never slowed | Unbounded memory behind a stuck subscriber |
| **C) Bounded with `DropOldest`** | Never blocks, bounded | Silent loss, against "errors are never swallowed" for anything that matters |

### Chosen Option: **A, bounded with backpressure** (recommended, pending the maintainer)

### Rationale

A wins as the default of a reliability library; B wins for a fire-and-forget telemetry mesh inside one process; C is listed because it is the usual choice for live dashboards, but it loses events without telling the publisher. The local mesh is in-process and not durable: a restart loses the queued events, which the documentation states (durability is the outbox's job, ADR-028).

**Class:** Dilemma

</details>

<details>
<summary><strong>5. Patterns and consumer groups</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Dot-separated event type names; `*` matches one segment and `#` matches zero or more (the RabbitMQ topic exchange grammar); subscriptions in the same `ConsumerGroup` compete (round robin), subscriptions in different groups each receive a copy (Kafka consumer-group semantics)** | Two well-known conventions; no new grammar | `#` needs a small matcher |
| **B) Regular expressions** | Powerful | Unbounded cost per event, a regex-denial-of-service surface on input that crosses a trust boundary |

### Chosen Option: **A, topic-exchange patterns and consumer groups**

### Rationale

Pattern syntax and group semantics follow the RabbitMQ topic exchange and Kafka consumer groups, which the issue's `MeshSubscription(Pattern, ConsumerGroup)` mirrors. A pattern is validated when the subscription is created (`encina.eventmesh.invalid_pattern`, `Validation`, R3).

**Class:** Trivial
**Settled by:** RabbitMQ topic exchange and Apache Kafka consumer groups (named comparable products) and the `MeshSubscription` record of #310

</details>

<details>
<summary><strong>6. Gateway routing model (Dilemma D6)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) A mesh-owned route table: a `GatewayRoute` has a name, a pattern over the event type, optional predicates over `MeshEventMetadata` and headers, an optional typed transformation, and a target (a node or a transport adapter); the first matching route by priority wins, or all matching routes when `FanOut` is set** | Works on untyped envelopes, which is what arrives from a transport; one small matcher shared with subscriptions | A second routing mechanism beside `IContentRouter` |
| **B) Build on `IContentRouter` with one `BuiltContentRouterDefinition` per event type, registered by the application** | Reuses the EIP implementation | `IContentRouter` is generic over a message type and handlers return typed results, so the gateway would need a deserialization step per registered type before routing; routes by type name would not exist for unknown types |

### Chosen Option: **A, a mesh-owned route table** (recommended, pending the maintainer)

### Rationale

A wins because the gateway receives envelopes whose type is a string, often unknown to this process; B wins when every routed event type is a known .NET type and the team already uses `IContentRouter`. Transformation is a typed delegate registered on the route (`Transform<TSource, TTarget>(Func<TSource, ValueTask<Either<EncinaError, TTarget>>>)`), the shape of the issue's `TransformAndForwardAsync<TSource, TTarget>`.

**Class:** Dilemma

</details>

<details>
<summary><strong>7. The port to transports and where adapters live (Dilemma D7)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) A mesh-owned port `IMeshTransportAdapter` (`PublishAsync(MeshEvent, MeshTarget, ct)` and `SubscribeAsync(pattern, handler, ct)` for transports that subscribe) in `Encina.EventMesh`; this plan ships a delegate-based adapter (`MeshTransportAdapter.Create(publish, subscribe)`) and an in-process adapter; real adapters are one small package each (`Encina.EventMesh.<Transport>`), opened as follow-ups** | Transports do not depend on the mesh; the port is the same for all; the issue's "cross-transport routing" works with any adapter | The adapters of the issue's examples (Kafka, RabbitMQ, Azure Service Bus) are not in this plan |
| **B) Adapters inside every transport package** | Discoverable | Every transport package would reference `Encina.EventMesh`, against pay-for-what-you-use |
| **C) Reuse `IOutboxTransportPublisher` of #2385 as the egress port** | One port for outbox and mesh | It takes an `IOutboxMessage` row; the gateway forwards events that are not rows; ingress is not covered |
| **D) A new `IMessageTransport` over all transports** | The cleanest long-term model | Reshapes ten transports; #742 plan weighs the same option; outside this issue |

### Chosen Option: **A, a mesh-owned port with satellite adapters** (recommended, pending the maintainer)

### Rationale

A wins for the smallest change that delivers the gateway; D wins if the maintainer later decides a transport-wide interface (the port then becomes that interface). Only MQTT, Redis.PubSub and the in-process bus subscribe today, so a transport that only publishes can be an egress target but not an ingress source until #1609 is done. Adding an adapter for a new transport follows the transport category rule (consistent across providers); the follow-up issue lists each transport.

**Class:** Dilemma

</details>

<details>
<summary><strong>8. Forward failure and dead lettering at mesh level (Dilemma D8)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) A failed forward is a `Left` to the caller (an ingress handler fails and the transport redelivers where it can); when `IDeadLetterStore` is registered and the route has `DeadLetterOnFailure`, the event is also written to it through `IDeadLetterMessageFactory` (opt-in), and a failed dead-letter write is itself a `Left`** | Uses the existing dead letter abstraction and its replay tooling; nothing succeeds silently | The store and factory are provider packages; without them there is no dead letter, only the `Left` |
| **B) A mesh-owned in-memory dead-letter list** | No dependency on a store | Lost on restart; a second dead letter mechanism |

### Chosen Option: **A, `Left` plus the existing dead letter store** (recommended, pending the maintainer)

### Rationale

A wins because it reuses `IDeadLetterStore.AddAsync` and the replay tooling, and never reports success on a failure (AGENTS.md section 3); B wins for a test or single-process setup with no database. The dead-letter reason is an error code, never an exception message.

**Class:** Dilemma

</details>

<details>
<summary><strong>9. CloudEvents support (Dilemma D9)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `Encina.EventMesh.CloudEvents` maps `MeshEvent` and `MeshEventMetadata` to a CloudEvents 1.0 event in structured JSON mode with `CloudNative.CloudEvents` and its `SystemTextJson` formatter (both Apache-2.0, net10.0); the library types stay internal; the public surface is the mapper and its options; the broader `ICloudEvent` API of #260 is not created here** | Delivers the acceptance criterion now; one library, the one #238 recommends | If #260 is later built as a general feature, the two packages must share the formatter |
| **B) Wait for #260 and depend on its `ICloudEvent` types** | One CloudEvents model for the library | #260 has no plan; the mesh criterion would stay open |

### Chosen Option: **A, a mapper over the CNCF library** (recommended, pending the maintainer)

### Rationale

A wins because the acceptance criterion is in this issue and #260 is unplanned; B wins if the maintainer wants one CloudEvents model first. Binary content mode is not delivered (transports differ in header support); a non-JSON `datacontenttype` is rejected with `encina.eventmesh.cloudevents.unsupported_content` (`InvalidRequest`, R3).

**Class:** Dilemma

</details>

<details>
<summary><strong>10. Topology discovery (Dilemma D10)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) A static view: `GetTopologyAsync` lists the nodes, regions, routes, subscriptions and adapters this process knows from its options and registrations, with each adapter's health from its registered health check** | Truthful about what it knows; testable; nothing external | No discovery of other processes' nodes |
| **B) Dynamic discovery through a registry (a service-discovery backend, #607, or a Dapr metadata call)** | Sees the real mesh | Needs a backend and a protocol this plan cannot test; #607 has no plan on `origin/main` |

### Chosen Option: **A, a static view** (recommended, pending the maintainer)

### Rationale

A wins for a first release; B wins for a large mesh where nodes come and go. Dynamic discovery is a follow-up in `artifacts/issues/event-mesh-deferred-integrations.md`.

**Class:** Dilemma

</details>

<details>
<summary><strong>11. Multi-region replication (Dilemma D11)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Not a separate mechanism: a region is an attribute of `MeshTarget`, and a route with `FanOut` forwards to several targets, which replicates an event to nodes in other regions; ordering, conflict handling and catch-up are not provided** | Meets the common case with the routing already planned | Not replication in the log-shipping sense |
| **B) A replication engine with ordering and offsets per region pair** | Real replication | Needs durable offsets, a store and failure semantics this plan does not design; large |

### Chosen Option: **A, replication as fan-out to regional targets** (recommended, pending the maintainer)

### Rationale

A wins for event distribution where each region consumes independently; B wins when regions must converge on one ordered history. B is a follow-up in `artifacts/issues/event-mesh-deferred-integrations.md`.

**Class:** Dilemma

</details>

<details>
<summary><strong>12. Unroutable events and cycles (Dilemma D12)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Fail closed: an event with no matching route is rejected (`encina.eventmesh.route_not_found`, `Internal`, R1, a configuration entry is missing) and dead lettered when Design Choice 8 applies; every forward appends the node id to the event's hop list and an event that reaches `MaxHops` (default 8) or revisits a node is rejected (`encina.eventmesh.hop_limit_exceeded`, `InvalidRequest`, R3)** | Nothing is dropped silently; a misrouted or looping event is visible; cycles cannot amplify | A new route must be added before a new event type flows |
| **B) Drop an unroutable event with a counter, and loop guard only by hop count** | A forgiving mesh | A silent loss until someone reads a counter |

### Chosen Option: **A, reject unroutable events and bound hops** (recommended, pending the maintainer)

### Rationale

A wins for a library whose first rule is that errors are not swallowed (AGENTS.md section 3) and whose gateways sit at trust boundaries; B wins for a best-effort broadcast mesh where most events have no consumer. A loud rejection is the default under either answer; a route with `Discard = true` can opt out explicitly and logs.

**Class:** Dilemma

</details>

<details>
<summary><strong>13. OpenTelemetry: per-package source or the shared transport design of #725 (Dilemma D13)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `ActivitySource` and `Meter` named `Encina.EventMesh`, registered by name in `Encina.OpenTelemetry`; spans for publish, route, transform, forward and receive, linked across hops through the W3C `traceparent` carried in the event metadata; counters tagged by route and outcome** | Meets the issue's cross-cutting row now; the same pattern as `Encina.Messaging.Outbox` | The name list in `Encina.OpenTelemetry` is edited by every plan |
| **B) Wait for the shared transport instrumentation of #725** | One design | The mesh is not a transport; #725's plan is merged but its code is not |

### Chosen Option: **A, per-package instrumentation** (recommended, pending the maintainer)

### Rationale

A wins because the row is integrated in the issue; B wins if the maintainer wants the mesh spans named by #725's convention. Spans link, never baggage (ADR-054 D2); no tenant tag is emitted until Design Choice 14 is done (ADR-054 D4).

**Class:** Dilemma

</details>

<details>
<summary><strong>14. Deferred integrations (Dilemma D14)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Defer to one issue file: tenant-scoped routes and tenant and module metadata (#1164, #2385), audit of cross-boundary flows, topology caching, leader election for gateway nodes, resilience decorators on the egress adapters, schema validation (#602)** | One design for each, with its prerequisite; the first release stays small | The issue's matrix rows stay open |
| **B) Add a tenant segment to subscription patterns now** | Early isolation | Needs the reserved `default` tenant segment and an injective encoding that no component has defined |

### Chosen Option: **A, defer to a follow-up issue** (recommended, pending the maintainer)

### Rationale

A wins because each deferred item needs a contract another issue defines first; B wins for a tenant-isolated deployment that needs it before those land. The issue file is `artifacts/issues/event-mesh-deferred-integrations.md`.

**Class:** Dilemma

</details>

---

## Implementation Phases

### Phase 1: Package, options and DI (pull request 1)

<details>
<summary>Tasks</summary>

1. `src/Encina.EventMesh/Encina.EventMesh.csproj` (net10.0; packages `Microsoft.Extensions.*.Abstractions`, `LanguageExt.Core`; project references to `Encina` and `Encina.Messaging`; `InternalsVisibleTo Encina.UnitTests`) and the `Encina.slnx` entry. The transports of this family carry no `PublicAPI.*.txt` today; this package is not a transport and its public surface is large, so it follows `Encina.Messaging`: `PublicAPI.Shipped.txt` and `PublicAPI.Unshipped.txt` are created empty here and the later pull requests add their lines, through `mechanical-fixer`.
2. `EncinaEventMeshOptions` (namespace `Encina.EventMesh`, public sealed): `NodeId` (default the machine name), `Region`, `MaxHops` (default 8), `DefaultSubscriptionCapacity` (default 1,000), `ProviderHealthCheck`; it holds no secret. Internal validator: non-blank `NodeId`, `NodeId` without `.`, `*` or `#`, positive `MaxHops` and capacity.
3. `ServiceCollectionExtensions.AddEncinaEventMesh(this IServiceCollection, Action<EncinaEventMeshOptions>? configure = null)`: options with `ValidateOnStart`, the validator through `TryAddEnumerable`; a DI test that builds the provider with `ValidateOnBuild` and `ValidateScopes`.
4. Tests: `EncinaEventMeshOptionsTests`, `ServiceCollectionExtensionsTests` (unit), `EventMeshGuardTests` (guard); manifest `.github/coverage-manifest/Encina.EventMesh.json`; `UnitTests.csproj` and `GuardTests.csproj` ProjectReference edits.

</details>

<details>
<summary>Prompt for AI Agents — Phase 1</summary>

```text
CONTEXT: Encina.Messaging is the nearest package family (options, validator, AddEncina*). The mesh is a pattern package, not a transport.
TASK: Create the Encina.EventMesh package skeleton with validated options and AddEncinaEventMesh, and their unit and guard tests.
KEY RULES: validation fails at startup with OptionsValidationException; TryAdd registrations only; a DI test with ValidateOnBuild and ValidateScopes; no Obsolete, no compatibility code; no private application names.
REFERENCE FILES: src/Encina.Messaging/MessagingServiceCollectionExtensions.cs, src/Encina.AmazonSQS/EncinaAmazonSQSOptionsValidator.cs, .github/coverage-manifest/Encina.Messaging.json.
```

</details>

### Phase 2: Mesh contracts, pattern matcher and errors (pull request 2)

<details>
<summary>Tasks</summary>

1. `IEventMesh` (Design Choice 3): `PublishToMeshAsync<TEvent>(TEvent @event, MeshTarget target, MeshPublishOptions? options = null, CancellationToken ct = default)`, `SubscribeAsync(MeshSubscription subscription, CancellationToken ct = default)`, `GetTopologyAsync(CancellationToken ct = default)`. Records, one file each: `MeshTarget(string NodeId, string? Region = null)`, `MeshSubscription(string Pattern, string ConsumerGroup, SubscriptionOptions? Options = null)`, `SubscriptionOptions(Capacity)`, `MeshPublishOptions` (renamed from the issue's `PublishOptions`: `Dapr.Messaging.PublishOptions` exists and the Dapr adapter imports both), `MeshEvent(object Payload, MeshEventMetadata Metadata, string SourceNode)`, `MeshEventMetadata` (event id, type name, `OccurredAtUtc`, `traceparent`, hop list, headers), `MeshTopology` and `MeshNode`. Constructors validate their arguments (non-blank ids, a valid pattern).
2. `MeshPattern` (internal): the matcher of Design Choice 5 (no regex, linear in the segments, no allocation per match).
3. `EventMeshErrorCodes.cs` (ADR-062, each created with the named argument `category:` and a constant message): `encina.eventmesh.invalid_pattern` `Validation` (R3), `encina.eventmesh.node_not_found` `Internal` (R1, a topology entry), `encina.eventmesh.duplicate_subscription` `AlreadyExists` (R2).
4. The `PublicAPI.Unshipped.txt` lines of the contracts above (through `mechanical-fixer`; the lines are decided from the signatures).
5. Tests: `MeshPatternTests` (unit: wildcards, anchors, empty segments), `MeshModelsTests` (unit: argument validation), guard edit, manifest edit.

</details>

<details>
<summary>Prompt for AI Agents — Phase 2</summary>

```text
CONTEXT: The package skeleton exists. The mesh contracts follow the issue sketch with Either return types.
TASK: Write IEventMesh, the records, the pattern matcher and the error codes, with their unit and guard tests.
KEY RULES: every operation returns Either<EncinaError, T> and cancellation propagates; EncinaErrors.Create with the named category: argument; constant messages; no regular expressions in the matcher; constructors validate arguments (guard tests cover each); PublicAPI lines through mechanical-fixer.
REFERENCE FILES: src/Encina.Messaging/ContentRouter/IContentRouter.cs, src/Encina.Messaging/ContentRouter/RouteDefinition.cs, src/Encina/Results/EncinaErrors.cs.
```

</details>

### Phase 3: Local mesh and the EventId range (pull request 3)

<details>
<summary>Tasks</summary>

1. `LocalEventMesh : IEventMesh` (internal sealed, singleton; it stamps `OccurredAtUtc` from `TimeProvider`, registered with `TryAddSingleton(TimeProvider.System)` in this pull request): a subscription registry; per subscription a bounded `Channel<MeshEvent>` (Design Choice 4); competing consumers inside a group (round robin) and one copy per group; `PublishToMeshAsync` to the local node delivers in process, to another node id returns `encina.eventmesh.node_not_found` unless a gateway adapter owns it (pull request 5); `SubscribeAsync` returns the channel reader as `IAsyncEnumerable<MeshEvent>` and removes the subscription when enumeration ends or the token is cancelled. Zero subscribers is `Right` with a counter (as the notification dispatcher does).
2. `Log.cs` 40080-40083 (event published, no subscriber, subscription added, subscription removed; no event ids, no payloads). `EventIdRanges.EventMesh = (40080, 40099)`; the `AssemblyRanges` entry in `EncinaEventIdAllocationTests`; `docs/engineering/eventid-block-registry.md` (through `docs-writer`): the row `| EventMesh | 40080-40099 | Transports | zone |` in the registered ranges table and this plan's row of the Blocks table set to `registered` (the five rows of the family block `40000-40099` are added by whichever plan lands first: `40000-40019` #242, `40020-40039` #610, `40040-40059` #611, `40060-40079` #612, `40080-40099` #310, owner `transport-new-2: #<n>`; a conflict is resolved by keeping both rows, never by a rebase of a pushed branch).
3. DI: `AddEncinaEventMesh` registers `IEventMesh` as `LocalEventMesh` (`TryAddSingleton`); the DI test resolves it with `ValidateOnBuild` and `ValidateScopes`.
4. Tests: `LocalEventMeshTests` (unit: fan-out to groups, competing consumers, backpressure with a bounded capacity, cancellation, unsubscribe on disposal), `LocalEventMeshConcurrencyTests` (unit: concurrent publishers and subscribers), guard edit, manifest edit.

</details>

<details>
<summary>Prompt for AI Agents — Phase 3</summary>

```text
CONTEXT: Contracts and the matcher exist. The local mesh is the in-process implementation of IEventMesh.
TASK: Implement LocalEventMesh with bounded channels per subscription, consumer groups and unsubscribe on disposal, the log messages 40080-40083, the EventId range EventMesh and the DI registration, with unit tests.
KEY RULES: a slow subscriber applies backpressure to the publisher and the publisher's cancellation propagates, never a Left; no lock is held while awaiting; EventIds only 40080-40099, registered first in EventIdRanges.cs and in the registry (Zone zone); ids and payloads never logged.
REFERENCE FILES: src/Encina.InMemory/InMemoryMessageBus.cs, src/Encina/Diagnostics/EventIdRanges.cs, docs/engineering/eventid-block-registry.md.
```

</details>

### Phase 4: Gateway routing and transformation (pull request 4)

<details>
<summary>Tasks</summary>

1. `IEventGateway.RouteAsync(IncomingEvent @event, ct)` and `TransformAndForwardAsync<TSource, TTarget>(TSource source, GatewayRoute route, ct)`, both returning `ValueTask<Either<EncinaError, Unit>>`; `IncomingEvent` (the envelope as it arrives: type name, raw payload as bytes or object, metadata, origin adapter); `GatewayRoute` and `GatewayRouteBuilder` (name, pattern, predicates over metadata and headers, `Priority`, `FanOut`, `Discard`, the typed transformation, the target); routes are registered with `AddEncinaEventMeshRoute(...)` in the DI extensions, not in the options.
2. `EventGateway` (singleton): selects the matching routes (Design Choice 6), applies the transformation with the exception caught and mapped to `encina.eventmesh.transform_failed` (`Internal`, R4), enforces `MaxHops` and the visited-node rule (Design Choice 12; `encina.eventmesh.hop_limit_exceeded` `InvalidRequest`, R3), returns `encina.eventmesh.route_not_found` (`Internal`, R1) when nothing matches and `Discard` is not set. Forwarding to a local subscription uses `LocalEventMesh`; forwarding to an adapter is pull request 5, so this pull request's gateway delivers locally only.
3. `Log.cs` 40084-40088 (route matched, no route, transform failed, hop limit exceeded, event discarded). DI: `TryAddSingleton<IEventGateway, EventGateway>()`; the DI test resolves it.
4. Tests: `EventGatewayRoutingTests`, `GatewayRouteBuilderTests`, `EventGatewayTransformTests` (unit: priority, fan-out, discard, transform failure, loop and hop limit), guard edit, manifest edit.

</details>

<details>
<summary>Prompt for AI Agents — Phase 4</summary>

```text
CONTEXT: The local mesh exists. The gateway routes untyped envelopes by pattern and metadata and applies typed transformations.
TASK: Implement IEventGateway, the route model and builder, EventGateway routing with transformation, hop and cycle control and the log messages, with unit tests.
KEY RULES: fail closed: no route is a Left unless Discard is set; a transformation exception is caught and mapped to an error code, never rethrown and never success; cancellation propagates; error codes use named ADR-062 categories and constant messages; ids and payloads never logged.
REFERENCE FILES: src/Encina.Messaging/ContentRouter/*.cs, src/Encina.Messaging/RoutingSlip/*.cs, src/Encina/Results/EncinaErrors.cs.
```

</details>

### Phase 5: Transport port, adapters and forwarding (pull request 5)

<details>
<summary>Tasks</summary>

1. `IMeshTransportAdapter` (`Name`, `PublishAsync(MeshEvent, MeshTarget, ct)` returning `Either<EncinaError, Unit>`, `SubscribeAsync(pattern, Func<IncomingEvent, ValueTask<Either<EncinaError, Unit>>>, ct)` returning `Either<EncinaError, IAsyncDisposable>`, and `CanSubscribe`); `MeshTransportAdapter.Create(...)` (delegate-based, the way an application adapts any publisher today); `InProcessMeshTransportAdapter` over `IInMemoryMessageBus` is not shipped in `Encina.EventMesh` (it would reference `Encina.InMemory`), the tests build it with `Create`.
2. The gateway's forward step: for a route whose target is an adapter, call `PublishAsync`; a failure is `encina.eventmesh.forward_failed` (`Internal`, R4); when `IDeadLetterStore` and `IDeadLetterMessageFactory` are registered and the route has `DeadLetterOnFailure`, the event is written there and the result stays a `Left` (Design Choice 8); a failed dead-letter write is `encina.eventmesh.dead_letter_failed` (`Internal`, R4). The ingress side: an adapter subscription feeds `IEventGateway.RouteAsync`, and a `Left` goes back to the adapter so the transport redelivers where it can.
3. A hosted service starts the ingress subscriptions and stops them with the host; each loop catches per iteration and never lets an exception escape, the stopping token ends it (AGENTS.md section 3). `Log.cs` 40089-40093 (forwarded, forward failed, dead lettered, dead letter failed, ingress iteration failed).
4. DI: adapters register through `AddEncinaEventMeshAdapter(...)`; the hosted service is registered in `AddEncinaEventMesh`; the DI test builds the provider with an adapter and `ValidateOnBuild`.
5. Tests: `MeshTransportAdapterTests`, `EventGatewayForwardingTests` (unit with substituted adapter and dead letter store), `EventMeshIngressTests`, guard edit, manifest edit.

</details>

<details>
<summary>Prompt for AI Agents — Phase 5</summary>

```text
CONTEXT: The gateway routes locally. Transports differ in shape: only the in-process bus, MQTT and Redis.PubSub subscribe today.
TASK: Implement the transport port, the delegate adapter, forwarding with dead lettering, the ingress hosted service and their log messages, with unit tests.
KEY RULES: a failed forward is a Left and never success; the dead letter write is opt-in and its failure is also a Left; the ingress loop never lets an exception escape and cancellation ends it; no event ids or payloads in logs; the dead-letter reason is an error code, never an exception message.
REFERENCE FILES: src/Encina.InMemory/IInMemoryMessageBus.cs, src/Encina.Redis.PubSub/IRedisPubSubMessagePublisher.cs, src/Encina.Messaging/DeadLetter/IDeadLetterStore.cs, src/Encina.Messaging/DeadLetter/IDeadLetterMessageFactory.cs.
```

</details>

### Phase 6: Topology, health check and observability (pull request 6)

<details>
<summary>Tasks</summary>

1. `GetTopologyAsync` on `LocalEventMesh`: the static view of Design Choice 10 (this node, configured nodes and regions, routes, subscriptions by pattern and group, adapters and whether each can subscribe).
2. `Health/EventMeshHealthCheck : EncinaHealthCheck` (`DefaultName = "encina-event-mesh"`, tags `encina`, `messaging`, `event-mesh`, `ready`): unhealthy when an ingress subscription has ended abnormally or an adapter reports unhealthy; the result text names the check and the exception type only. Registered when `ProviderHealthCheck.Enabled`.
3. `Diagnostics/EventMeshActivitySource` and `EventMeshMetrics`: spans publish, route, transform, forward, receive with a link from consumer to producer through the `traceparent` of the event metadata; counters `event_mesh.published`, `.routed`, `.forwarded`, `.rejected`, `.dead_lettered` with tags `route`, `outcome` (route names, never event ids); no tenant tag. `Encina.OpenTelemetry` gains `AddSource`/`AddMeter` lines. `Log.cs` 40094-40096 (topology requested, adapter unhealthy, ingress subscription ended).
4. Tests: `EventMeshTopologyTests`, `EventMeshHealthCheckTests`, `EventMeshDiagnosticsTests` (unit), guard edit, manifest edit; the `Encina.OpenTelemetry` registration test is extended (its manifest targets stay at today's unit 90 and guard 35).

</details>

<details>
<summary>Prompt for AI Agents — Phase 6</summary>

```text
CONTEXT: Routing and forwarding exist. The issue requires topology discovery, health monitoring and traces.
TASK: Implement the static topology, the health check, the ActivitySource and Meter and register their names in Encina.OpenTelemetry.
KEY RULES: health results and tags never contain an exception message; spans link, never baggage (ADR-054 D2); metric tags are route names, never event ids; zero cost when no listener; no manifest target is lowered.
REFERENCE FILES: src/Encina.Messaging/Health/EncinaHealthCheck.cs, src/Encina.Messaging/Diagnostics/OutboxActivitySource.cs, src/Encina.OpenTelemetry/ServiceCollectionExtensions.cs.
```

</details>

### Phase 7: CloudEvents package (pull request 7)

<details>
<summary>Tasks</summary>

1. `src/Encina.EventMesh.CloudEvents/Encina.EventMesh.CloudEvents.csproj` (packages `CloudNative.CloudEvents` and `CloudNative.CloudEvents.SystemTextJson`, new pins 2.9.0 in `Directory.Packages.props`; project reference to `Encina.EventMesh`; `InternalsVisibleTo Encina.UnitTests`), the `Encina.slnx` entry, `EncinaEventMeshCloudEventsOptions` (default `Source` URI, `DataContentType` `application/json`), its validator, `CloudEventsMeshFormatter` (public: `ToCloudEvent(MeshEvent)` and `FromCloudEvent(...)`, both `Either`), `ServiceCollectionExtensions.AddEncinaEventMeshCloudEvents` (registers the formatter the gateway uses to read and write envelopes on adapters that carry CloudEvents), the PublicAPI files (through `mechanical-fixer`) and the manifest `.github/coverage-manifest/Encina.EventMesh.CloudEvents.json`.
2. Errors (ADR-062): `encina.eventmesh.cloudevents.invalid_event` `Validation` (R3), `encina.eventmesh.cloudevents.unsupported_content` `InvalidRequest` (R3). The mapping: CloudEvents `id` = event id, `type` = event type name, `source` = `Source` option or the node id, `time` = `OccurredAtUtc`, `traceparent` extension, headers as extensions with the prefix `encina`.
3. Tests: `CloudEventsMeshFormatterTests` (unit: round trip, required attributes, unsupported content), `CloudEventsOptionsTests`, `ServiceCollectionExtensionsTests`, guard test; `UnitTests.csproj` and `GuardTests.csproj` ProjectReference edits.

</details>

<details>
<summary>Prompt for AI Agents — Phase 7</summary>

```text
CONTEXT: The mesh exists and uses an internal envelope. CloudNative.CloudEvents 2.9.0 and its SystemTextJson formatter are Apache-2.0.
TASK: Create Encina.EventMesh.CloudEvents with the mapper between MeshEvent and CloudEvents 1.0 in structured JSON mode, its options and DI, with tests.
KEY RULES: the library types stay internal; Either for every operation; named ADR-062 categories and constant messages; payloads and ids never logged; no new log ids unless the package logs; PublicAPI through mechanical-fixer.
REFERENCE FILES: src/Encina.Cdc.Debezium/DebeziumEventMapper.cs, src/Encina.AmazonSQS/ServiceCollectionExtensions.cs, .github/coverage-manifest/Encina.AmazonSQS.json.
```

</details>

### Phase 8: Contract, property and integration tests (pull request 8)

<details>
<summary>Tasks</summary>

1. `tests/Encina.ContractTests/EventMesh/EventMeshContractTests.cs`: the `IEventMesh` and `IEventGateway` contracts run against the real `LocalEventMesh` and `EventGateway` (publish to subscribers, `Left` for an unknown node, no route is a `Left`, cancellation propagates); `ContractTests.csproj` reference.
2. `tests/Encina.PropertyTests/EventMesh/MeshPatternPropertyTests.cs` (FsCheck: a pattern equals its own type name, `#` matches every type, the matcher agrees with a reference implementation, no input crashes it) and the `PropertyTests.csproj` reference.
3. `tests/Encina.IntegrationTests/EventMesh/EventMeshCrossTransportIntegrationTests.cs` in the shared `RedisCollection` of the integration project (`[Collection(RedisCollection.Name)]`, as `RedisPubSubMessagePublisherIntegrationTests` does; `[Trait("Category","Integration")]`): the gateway forwards an event from the in-process bus to Redis.PubSub and back through two adapters built with `MeshTransportAdapter.Create`, with transformation and a CloudEvents round trip; `IntegrationTests.csproj` reference. Adapters for MQTT, Kafka, RabbitMQ and Azure Service Bus are the follow-up issue.
4. One manifest edit entering the contract, property and integration targets (the table of Phase 9).

</details>

<details>
<summary>Prompt for AI Agents — Phase 8</summary>

```text
CONTEXT: The mesh, gateway, adapters and CloudEvents package exist. Redis.PubSub and the in-process bus both subscribe, so they are the two real transports of the cross-transport test.
TASK: Write the contract, property and integration tests.
KEY RULES: real Redis through the existing RedisCollection and RedisFixture, never a per-class container; cleanup in InitializeAsync; no Thread.Sleep (bounded waits on TaskCompletionSource); Shouldly through Encina.Testing.Shouldly; contract tests execute the real mesh and gateway.
REFERENCE FILES: tests/Encina.IntegrationTests/Messaging/RedisPubSub/RedisPubSubMessagePublisherIntegrationTests.cs, tests/Encina.IntegrationTests/Infrastructure/Caching/Fixtures/RedisFixture.cs, tests/Encina.ContractTests/Messaging/RedisPubSub/*.cs.
```

</details>

### Phase 9: Documentation and finalization (pull request 9)

<details>
<summary>Tasks</summary>

Per-flag targets (entered in `.github/coverage-manifest/Encina.EventMesh.json` and `Encina.EventMesh.CloudEvents.json` in the pull request that adds the tests of each flag: unit and guard with the file in pull requests 1 to 7, contract, property and integration in pull request 8; measured with the commands of AGENTS.md section 9):

| File | unit | guard | contract | property | integration |
|------|:---:|:---:|:---:|:---:|:---:|
| `LocalEventMesh.cs` | 90 | 15 | 60 | 0 | 50 |
| `EventGateway.cs` | 90 | 15 | 60 | 0 | 60 |
| `MeshPattern.cs` | 100 | 0 | 0 | 90 | 0 |
| `MeshTransportAdapter.cs`, ingress hosted service | 85 | 15 | 0 | 0 | 60 |
| Contract records (`MeshTarget.cs`, `MeshSubscription.cs`, ...) | 95 | 70 | 0 | 0 | 0 |
| `EncinaEventMeshOptions.cs`, `...OptionsValidator.cs` | 100 | 0 | 0 | 0 | 0 |
| `ServiceCollectionExtensions.cs` | 85 | 55 | 0 | 0 | 0 |
| `Health/EventMeshHealthCheck.cs`, `Diagnostics/*.cs` | 90 | 40 | 0 | 0 | 0 |
| `CloudEventsMeshFormatter.cs` | 90 | 20 | 0 | 0 | 60 |
| Interfaces, `Log.cs` | 0 | 0 | 0 | 0 | 0 |

| File and flag | Justification |
|---------------|---------------|
| Local mesh and gateway unit 90, contract 60 | Unit tests drive every branch in process; the contract tests run the public contract against the real classes and leave transformation and dead-letter branches to unit tests. |
| Gateway and adapter integration 60 | The integration test crosses the real bus and Redis.PubSub, leaving failure branches that need a broken transport. |
| Pattern matcher unit 100, property 90 | Pure function; the property test covers the invariants and leaves argument-validation branches to unit tests. |
| Contract records guard 70 | Constructors validate their arguments; guard tests reach each check, the remainder is property accessors. |
| Options, validator, DI, health check, diagnostics | Pure data, registration and instrumentation, reached by unit tests; guard tests reach argument checks only. |
| Interfaces and `Log.cs` 0 | No executable lines (interfaces) or generated delegates; guard tests cannot reach either. |

1. `src/Encina.EventMesh/README.md`, `src/Encina.EventMesh.CloudEvents/README.md`, `docs/features/event-mesh.md` (with examples), the Event Mesh section of `docs/messaging/transports.md` and `docs/INVENTORY.md` through `docs-writer`; changelog fragment `changelog.d/310-event-mesh.added.md` through `mechanical-fixer`.
2. Load and benchmark: justification files `tests/Encina.LoadTests/EventMesh/EventMesh.md` and `tests/Encina.BenchmarkTests/EventMesh/EventMesh.md` for the first release (the local mesh and matcher are in-process; a benchmark of the matcher can follow the first measurement).
3. `dotnet build --configuration Release` with zero warnings; every flag at its target; CRAP of every new method at most 10.

</details>

<details>
<summary>Prompt for AI Agents — Phase 9</summary>

```text
CONTEXT: The packages are implemented and tested; documentation and the justification files remain.
TASK: Enter the manifest justifications, write the two justification files, and hand the READMEs, feature page, transports page, INVENTORY and changelog fragment to the owning specialists.
KEY RULES: targets never go down; no hand-typed coverage figures in docs; never edit CHANGELOG.md; docs through docs-writer, fragments through mechanical-fixer; documentation says what is not delivered (adapters, Dapr, dynamic discovery, replication).
REFERENCE FILES: tests/Encina.LoadTests/Messaging/RedisPubSub/RedisPubSub.md, .github/coverage-manifest/Encina.Messaging.json, changelog.d/README.md.
```

</details>

---

## Research

### Relevant Standards & Specifications

| Standard / Spec | Relevance | Notes |
|-----------------|-----------|-------|
| `AGENTS.md` sections 1, 3, 5, 6, 7, 9 | Pay-for-what-you-use, ROP, background loops, transport category, 12 functions, EventIds, coverage | Phases 1 to 9 |
| SPEC-000 REQ-030 and DEC-009 | The mesh is in 1.0 (EIP part 2, new transports); it needs no cloud service | Scope |
| ADR-062, ADR-054 D2 and D4, ADR-028 | Error categories; trace links and tenant tags; integration events leave the process through the outbox | Phases 2 to 6 |
| CloudEvents 1.0 (CNCF) | Event envelope and structured JSON mode | Design Choice 9 |
| RabbitMQ topic exchange, Kafka consumer groups | Pattern grammar and group semantics | Design Choice 5 |

### Existing Encina Infrastructure to Leverage

| Component | Location | Usage in this feature |
|-----------|----------|-----------------------|
| `IContentRouter`, `RouteDefinition` | `src/Encina.Messaging/ContentRouter/` | Considered and not reused (typed); same style of builder and result |
| `IDeadLetterStore`, `IDeadLetterMessageFactory` | `src/Encina.Messaging/DeadLetter/` | Opt-in dead lettering of failed forwards |
| `IInMemoryMessageBus`, `IRedisPubSubMessagePublisher` | `src/Encina.InMemory/`, `src/Encina.Redis.PubSub/` | The two subscribing transports of the integration test |
| `EncinaHealthCheck`, `ProviderHealthCheckOptions` | `src/Encina.Messaging/Health/` | Health check base and options |
| `OutboxActivitySource` | `src/Encina.Messaging/Diagnostics/OutboxActivitySource.cs` | Pattern for the source and meter |
| `RedisCollection`, `RedisFixture` | `tests/Encina.IntegrationTests/Infrastructure/Caching/Fixtures/RedisFixture.cs` | Shared Redis container used by `RedisPubSubMessagePublisherIntegrationTests` |
| `EventIdRanges` and the registry | `src/Encina/Diagnostics/EventIdRanges.cs`, `docs/engineering/eventid-block-registry.md` | New field `EventMesh`; zone rows |

### Event ID Allocation

| Package | Range | Notes |
|---------|-------|-------|
| `EventMesh` (new) | 40080-40099 | Last range of the block `40000-40099` of the Transports zone taken by transport-new-2 (`DaprPubSub` 40000-40019, `ZeroMQ` 40020-40039, `AzureEventHubs` 40040-40059, `AmazonKinesis` 40060-40079); ids 40080-40096 used (40080-40083 local mesh, 40084-40088 gateway, 40089-40093 forwarding, 40094-40096 topology and health), 40097-40099 spare. The block `9750-9799` of the batch snapshot is registered as `SecurityBreakTheGlass` on `origin/main` (status `superseded` in the registry). Verified free by a search of the registry and of every `docs/plan-*` branch for `40000`. |

### Estimated File Count

| Category | Files | Notes |
|----------|------:|-------|
| Production code | 32 | Mesh: csproj, options, validator, DI, 8 contract files (`IEventMesh` and 7 records), matcher, error codes, `LocalEventMesh`, `Log`, `IEventGateway`, `IncomingEvent`, `GatewayRoute`, `GatewayRouteBuilder`, `EventGateway`, adapter port, adapter factory, ingress hosted service, health check, 2 diagnostics; CloudEvents: csproj, options, validator, formatter, DI |
| Tests | 22 | New unit, guard, contract, property and integration files of the nine pull requests (guard edits and manifest edits are counted per pull request) |
| Shared edits | 13 | `Encina.slnx` (twice), `Directory.Packages.props`, 7 test `.csproj` edits (`UnitTests` and `GuardTests` twice, `ContractTests`, `PropertyTests`, `IntegrationTests`), `EventIdRanges.cs`, allocation test, `Encina.OpenTelemetry` |
| Documentation | 9 | 2 READMEs, feature page, transports page, INVENTORY, registry rows, changelog, 2 justification files |
| **Pull request 1** | **13** | csproj, 2 PublicAPI files, options, validator, DI, `Encina.slnx`, `UnitTests.csproj`, `GuardTests.csproj`, manifest, options test, DI test, guard test; about 450 lines |
| **Pull request 2** | **15** | `IEventMesh`, 7 record files (`MeshTarget`, `MeshSubscription`, `SubscriptionOptions`, `MeshPublishOptions`, `MeshEvent`, `MeshEventMetadata`, `MeshTopology` with `MeshNode`), `MeshPattern`, error codes, `PublicAPI.Unshipped.txt` edit, 2 unit tests, guard edit, manifest edit; about 700 lines |
| **Pull request 3** | **11** | `LocalEventMesh` (internal), `Log`, `EventIdRanges.cs`, allocation test, registry, DI edit, DI test edit, 2 unit tests, guard edit, manifest edit; about 700 lines |
| **Pull request 4** | **14** | `IEventGateway`, `IncomingEvent`, `GatewayRoute`, `GatewayRouteBuilder`, `EventGateway`, error codes edit, `Log` edit, DI edit, DI test edit, `PublicAPI.Unshipped.txt` edit, 3 unit tests, guard edit, manifest edit; about 900 lines |
| **Pull request 5** | **14** | adapter port, adapter factory, ingress hosted service, gateway edit, error codes edit, `Log` edit, DI edit, DI test edit, `PublicAPI.Unshipped.txt` edit, 3 unit tests, guard edit, manifest edit; about 800 lines |
| **Pull request 6** | **14** | `LocalEventMesh` edit (topology), health check, 2 diagnostics, `Log` edit, DI edit, DI test edit, `Encina.OpenTelemetry` edit, `PublicAPI.Unshipped.txt` edit, 3 unit tests, guard edit, manifest edit; about 650 lines |
| **Pull request 7** | **15** | csproj, 2 PublicAPI files, options, validator, formatter (the error codes are internal constants inside it), DI, `Encina.slnx`, `Directory.Packages.props`, `UnitTests.csproj`, `GuardTests.csproj`, manifest, 2 unit tests, guard test; about 700 lines |
| **Pull request 8** | **7** | contract test, property test, integration test, `ContractTests.csproj`, `PropertyTests.csproj`, `IntegrationTests.csproj`, manifest; about 700 lines |
| **Pull request 9** | **9** | 2 READMEs, feature page, transports page, INVENTORY, changelog fragment, 2 justification files, 2 manifests (justifications); about 450 lines |

Recount by file name, including PublicAPI files, manifests, DI tests, project references, `Encina.slnx`, `Directory.Packages.props` and the registry: no pull request exceeds 15 files or 1,500 lines. Pull request 2 and pull request 7 are at the limit; if the implementation finds one more file in either, that pull request splits at the records and the options respectively.

Implementers and callers: `IEventMesh`, `IEventGateway` and `IMeshTransportAdapter` are new, so they have no implementers or `new X(` sites to update; the shared edits are additive. Pull request 3 registers `TimeProvider` and the local mesh, pull request 4 the gateway, pull request 5 the ingress hosted service, pull request 6 the health check, pull request 7 the CloudEvents formatter. Each pull request builds alone in order 1 to 9 (each needs the previous one).

---

## Combined AI Agent Prompts

<details>
<summary>Combined prompt for all phases</summary>

```text
PROJECT CONTEXT: Encina is a pre-1.0 .NET 10 / C# 14 library; no backward compatibility, zero warnings, ROP (Either<EncinaError, T>). Transports each have their own publisher interface; Encina.EventMesh adds a mesh and a gateway on top of them through a port that satellite adapters implement.

IMPLEMENTATION OVERVIEW: PR1 package, options, DI; PR2 contracts, pattern matcher and errors; PR3 local mesh with bounded channels and the EventId range EventMesh 40080-40099; PR4 gateway routing and transformation with hop control; PR5 transport port, delegate adapter, forwarding with dead lettering and ingress; PR6 topology, health check and telemetry; PR7 CloudEvents package; PR8 contract, property and integration tests (in process bus and Redis.PubSub); PR9 documentation. Dilemmas D2, D4, D6, D7, D8, D9, D10, D11, D12, D13 and D14 are implemented as the maintainer answers, the recommended options until then.

KEY PATTERNS: fail closed (no route is a Left); never success on a failure; loops catch per iteration; backpressure, never silent loss; named ADR-062 category on every error; constant error messages; ids and payloads never logged; targets never go down; CRAP <= 10.

REFERENCE FILES: src/Encina.Messaging/ContentRouter, src/Encina.Messaging/DeadLetter, src/Encina.InMemory, src/Encina.Redis.PubSub, src/Encina/Diagnostics/EventIdRanges.cs.
```

</details>

---

## Cross-Cutting Integration Matrix

| # | Function | Status | Notes |
|---|----------|--------|-------|
| 1 | Caching | ⏭️ | Caching the topology and the route table is a follow-up (issue file `event-mesh-deferred-integrations`); the first release reads from memory. |
| 2 | OpenTelemetry | ✅ | Per-package `ActivitySource` and `Meter` (Design Choice 13), spans linked across hops. |
| 3 | Structured Logging | ✅ | `[LoggerMessage]` in `Log.cs`, EventIds 40080-40096 of the range 40080-40099; error codes and exception types only. |
| 4 | Health Checks | ✅ | `EventMeshHealthCheck` covers ingress subscriptions and adapters. |
| 5 | Validation | ✅ | Options, patterns and the event envelope are validated; schema validation at the gateway depends on #602 and is in the deferred issue file. |
| 6 | Resilience | ⏭️ | `Encina.Resilience` does not exist on `origin/main`; a breaker on each hop is a decorator of the egress adapters, recorded in the deferred issue file with #742's model. |
| 7 | Distributed Locks | ⏭️ | Leader election for gateway nodes is in the deferred issue file; the first release has no singleton work across nodes. |
| 8 | Transactions | ❌ | Event routing; durability and atomic publication are the outbox's job (ADR-028). |
| 9 | Idempotency | ✅ | The event id is the message id on every hop; the hop list prevents loops; shared deduplication stays with the inbox (a gateway may redeliver). |
| 10 | Multi-Tenancy | ⏭️ | Tenant-scoped routes and metadata need #1164 and #2385; issue file `event-mesh-deferred-integrations`. |
| 11 | Module Isolation | ⏭️ | Same issue file. |
| 12 | Audit Trail | ⏭️ | Auditing cross-boundary flows needs the operation audit store contract (`IOperationAuditStore`, today `IAuditLogStore`, #1778); issue file `event-mesh-deferred-integrations`. |

---

## Prerequisites & Dependencies

- None blocking pull request 1; pull requests 2 to 9 follow in order. Dead lettering uses the existing `IDeadLetterStore` and factory when an application registers them.
- **Related, not blocking**: #260 (CloudEvents format support, open, no plan), #318 (event streaming abstractions, open), #1609 (the transports that only publish become ingress sources when they gain subscribe), #2385 (outbox bridge; the mesh port is separate), #742 and #725 (resilience and telemetry of transports), #602 (schema registry), #1164 and #1778 (request context and audit store naming).
- **Sibling plans**: #242 (the Dapr adapter of the mesh builds on its `IDaprMessagePublisher`), #610, #611 and #612 of this family and the five plans of transport-new-1 (each transport becomes an adapter in a follow-up).

---

## Next Steps

1. Ask the maintainer D2, D4, D6, D7, D8, D9, D10, D11, D12, D13 and D14 when work on #310 starts; record the answers in `## Maintainer Decisions`.
2. Pull requests 1 to 3: package (11), contracts (15), local mesh (12).
3. Pull requests 4 to 6: gateway (14), transport port and forwarding (11), topology, health and telemetry (12).
4. Pull requests 7 to 9: CloudEvents (12), contract, property and integration tests (7), documentation (8).
