# Implementation Plan: `Encina.Messaging` Outbox-to-transport publisher so integration events reach a broker (ADR-028)

> **Issue**: [#2385](https://github.com/dlrivada/Encina/issues/2385)
> **Type**: Feature
> **Verified against**: origin/main `c49b987e9` (2026-11-12)
> **Complexity**: High (8 phases, 10 transports, ~100 files in 9 pull requests)
> **Estimated Scope**: ~1,300-1,700 lines of production code + ~1,800-2,300 lines of tests

---

## Summary

ADR-028 says integration events leave the process only through the Outbox, but the Outbox has no route to a broker. `OutboxProcessorBase.PublishAsync` calls `encina.Publish(typedNotification, cancellationToken)`, which dispatches in memory; when no handler is registered the dispatcher returns `Right` (`Encina.NotificationDispatcher`, "zero handlers is not an error"), the row is marked processed and the integration event reached nobody. The transports (RabbitMQ, Azure Service Bus, Amazon SQS, Kafka, NATS, Redis.PubSub, MQTT, InMemory, gRPC, GraphQL) only publish and have no reference to the Outbox. The consumer side is #1609, per-handler idempotency is #1220; nothing covers this bridge. The maintainer settled it on 2026-10-17: `IOutboxTransportPublisher` per transport, the processor resolves it per route, a `Left` goes to retry then dead letter, and a startup error when an Outbox has integration-event types and no publisher.

What it implements:

- `IOutboxTransportPublisher` in `Encina.Messaging` (with the `OutboxRoute` it receives) and one adapter per transport package that wraps the transport's existing publisher; the outbox id travels as the broker message id so consumers deduplicate (verified: the RabbitMQ publisher takes a `messageId` parameter; Kafka has none and carries the id in a header, the in-memory bus has none, Design Choice 9).
- A route decision in the processor: a row whose stored type is an `IIntegrationEvent` goes to the transport publisher; every other row (domain events) keeps the in-process `IEncina.Publish`. `Encina.Messaging` references only `Encina`, so the marker must be visible to it: Design Choice 3, a Dilemma. A processor can override the decision: the Marten processor of #2383 always dispatches in process.
- `[OutboxRoute]` metadata (transport name, destination) with an options default, and metadata propagation (correlation id, tenant id) into transport headers when the route carries them; the values come from the persisted context of #1164 (Design Choice 10), so until it lands the headers are absent.
- Fail-closed startup validation: an Outbox with registered integration-event types and no `IOutboxTransportPublisher` fails at start-up (cross-checked with the integration-event types contributed to a core type source, Design Choice 5).

Standards: ADR-028 and ADR-050 (modules may require modules, enforced by startup validation); AGENTS.md section 3 (Outbox: reliable at-least-once; a `Left` is never reported as success); AGENTS.md section 5 (transport category: every transport supports send/publish, error handling and DLQ, metadata propagation).

Estimated scope: about 100 files (production 40, tests 40, documentation and metadata 20), shipped as 9 pull requests (see "Pull request split"). Affected packages: `Encina` (the marker, Design Choice 3), `Encina.Messaging`, `Encina.DomainModeling`, `Encina.OpenTelemetry`, `Encina.RabbitMQ`, `Encina.AzureServiceBus`, `Encina.AmazonSQS`, `Encina.Kafka`, `Encina.NATS`, `Encina.Redis.PubSub`, `Encina.MQTT`, `Encina.InMemory`, `Encina.gRPC`, `Encina.GraphQL`.

Provider category: Messaging transports (10). The Outbox processor is shared by the 10 database stores and is unchanged per database provider; the ten transports are the provider axis here (AGENTS.md section 5); eight of them have an outbound publish operation to wrap and the other two are server bridges (Design Choice 8).

---

## Design Choices

<details>
<summary><strong>1. Contract — one publisher abstraction per transport, resolved per route</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `IOutboxTransportPublisher` implemented by an adapter in each transport package; the processor resolves it by route** | One contract; processors stay generic; DLQ semantics shared; the transport coherence rule is met | 8 adapters to build and test (10 under Design Choice 8 B) |
| **B) Bridge handlers written by the application** | No new abstraction | Every application reimplements it, and a missing handler silently succeeds today |
| **C) Publish from the handler directly** | Simple | Dual write, the problem the Outbox exists to prevent |

### Chosen Option: **A — `IOutboxTransportPublisher` per transport**

### Rationale

- The maintainer comment of 2026-10-17 settled it by elimination: B fails open (zero handlers returns `Right`) and C is the dual write; the transport category rule requires coherence across all transports.
- **Class:** Settled
- **Settled by:** #2385 (maintainer comment of 2026-10-17: settled by elimination), ADR-028 and AGENTS.md section 5 (transport category coherence)

</details>

<details>
<summary><strong>2. Method signature — abstraction types, not a provider message class</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `ValueTask<Either<EncinaError, Unit>> PublishAsync(IOutboxMessage message, object payload, OutboxRoute route, CancellationToken)`; the processor already deserialized the payload** | Provider-agnostic (`IOutboxMessage` is the abstraction of every database store); the adapter serializes with its own serializer and puts `message.Id` in the broker message id | The payload is deserialized once for the adapter |
| **B) The issue sketch, `PublishAsync(OutboxMessage message, ct)` with a concrete class** | Shortest | No single `OutboxMessage` class exists: each provider has its own (`Encina.ADO.SqlServer.Outbox.OutboxMessage`, EF, Mongo...), so a concrete type would mix provider code into the abstraction |

### Chosen Option: **A — `IOutboxMessage`, payload and route**

### Rationale

- AGENTS.md section 5 forbids provider-specific code in abstractions; the issue's sketch name `OutboxMessage` is the entity name of each provider, not a shared type.
- **Class:** Settled
- **Settled by:** AGENTS.md section 5 (provider coherence: abstractions in `Encina.Messaging`, never provider-specific code) and `src/Encina.Messaging/Outbox/IOutboxMessage.cs`

</details>

<details>
<summary><strong>3. Which rows go to a transport</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) The marker `IIntegrationEvent` moves to the core `Encina` package (same namespace, `Encina.DomainModeling` keeps the base record, the mapper and everything else); `Encina.Messaging` tests `typeof(IIntegrationEvent).IsAssignableFrom(type)`** | Messaging gets no new project reference; the direction stays abstraction (core) to implementation; domain assemblies keep their references | A public type changes assembly (pre-1.0, acceptable) |
| **B) `Encina.Messaging` adds a project reference to `Encina.DomainModeling`** | Marker stays where it is | The messaging abstraction package now depends on a modeling package (DomainModeling does not reference Messaging today, so no cycle, but every messaging user gets the dependency) |
| **C) A flag or route column on the outbox row** | Explicit per message; no type test | Schema change on 10 providers for something the type already says |
| **D) Everything goes to the transport when one is registered** | Simple | Domain events would leave the process, which ADR-028 forbids |

### Chosen Option: **A — The marker lives in the core, and the `IIntegrationEvent` type decides (recommended, pending the maintainer)**

### Rationale

- Verified: `Encina.Messaging.csproj` references only `Encina.csproj`, while `IIntegrationEvent` and `IDomainEventToIntegrationEventMapper<,>` live in `Encina.DomainModeling`; Messaging cannot name either as written. ADR-028 forbids only option D; options B and C also comply with it, so the choice is the maintainer's. The event-sourced processor of #2383 overrides the decision to always dispatch in process (a plan on a branch, not code on main).
- Under A the startup validator cannot name the mapper either: it reads a small core abstraction, `IIntegrationEventTypeSource` (`IReadOnlyCollection<Type> IntegrationEventTypes`), which `Encina.DomainModeling` implements for the mappers registered through it (Design Choice 5).
- **Class:** Dilemma

</details>

<details>
<summary><strong>4. How the route (transport and destination) is chosen</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `[OutboxRoute("rabbitmq", "orders.v1")]` on the integration event type, with `OutboxOptions.DefaultTransport` and a destination convention (the registry name of #1221, else the type name) when omitted** | The route sits with the contract; one default covers the common single-broker case | A type-level attribute cannot vary per environment |
| **B) A route table in `OutboxOptions` (type to transport and destination)** | Configurable per environment; no attribute | Distance between the event and its route; a table to maintain |
| **C) Convention only** | Zero configuration | No way to choose a transport when several are registered |

### Chosen Option: **A — Attribute with an options default and a table override (recommended, pending the maintainer)**

### Rationale

- B wins for deployments that publish the same event to different brokers per environment; A wins where the event contract owns its destination (the typical integration event). Both can coexist: the options table overrides the attribute.
- **Class:** Dilemma

</details>

<details>
<summary><strong>5. Startup validation — from registered mappers, or lazily at first dispatch</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) A startup validator reads the `IIntegrationEventTypeSource` services (Design Choice 3: contributed by the registered mappers; #1221's message-type assemblies contribute another source when #1221 lands, until then the validator sees mapper registrations only); an Outbox with any integration-event type and no `IOutboxTransportPublisher` fails at start-up** | Fails before the first message, as the issue asks; no reflection beyond the DI descriptors | Types that are neither mapped nor listed are not seen until #1221 lands |
| **B) Fail on the first integration-event row (a `Left` that retries and dead-letters)** | Sees every type | The first deployment error shows up as dead letters, hours later |
| **C) A health check only** | Visible to operations | Does not prevent a running, silently failing application |

### Chosen Option: **A — Startup validation from registrations, with B as the safety net (recommended, pending the maintainer)**

### Rationale

- C wins only where operations prefers degraded health to a refusal to start; B is kept as the runtime net for types the validator cannot see, including an integration event type that is neither mapped nor listed (the row fails visibly with `outbox.no_transport_publisher`, never `Right`). #1221 is not a prerequisite; its source only widens what the validator sees.
- **Class:** Dilemma

</details>

<details>
<summary><strong>6. Failure semantics and message identity</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) A `Left` (or an exception) from the adapter is a failed delivery: the existing retry, backoff and dead-letter path of `OutboxBatchProcessor`; the outbox id is the broker message id** | No new retry code; at-least-once with consumer-side deduplication; consistent with the existing messageId parameters | Duplicates are possible after a crash between the broker ack and `MarkAsProcessedAsync` (the at-least-once contract) |
| **B) The adapter retries by itself before returning** | Fewer outbox retries | A second retry layer that hides failures and multiplies delay |

### Chosen Option: **A — The existing outbox failure path**

### Rationale

- AGENTS.md section 3: a `Left` from a processor, adapter or store fails that operation (retry, dead letter), never reports success; the Outbox is documented as at-least-once.
- **Class:** Settled
- **Settled by:** AGENTS.md section 3 (errors are never swallowed in background infrastructure; Outbox is reliable at-least-once) and #2385 (maintainer comment of 2026-10-17)

</details>

<details>
<summary><strong>7. How an integration event is stored in the Outbox</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) The interface `IIntegrationEvent` itself extends `INotification` (with the marker in the core, Design Choice 3 A), so every integration event, including a type that implements the interface directly without the base record, is storable by the existing `OutboxWriter` and `TransactionalOutboxPipelineBehavior`** | No second write path; no integration event is unstorable by construction | Every integration event is also an `INotification`, so an in-process `INotificationHandler<TIntegrationEvent>` would be resolvable by an ordinary `Publish` (the outbox path routes it to the transport instead) |
| **B) Only the base record `IntegrationEvent` implements `INotification`** | Smaller change | A type implementing `IIntegrationEvent` directly (the interface is public and the base record is optional) still cannot be stored, and fails only at the call site |
| **C) Make the outbox store `object` instead of `INotification`** | No marker needed | Changes `IHasNotifications`, `OutboxWriter` and the processor signatures across the package |

### Chosen Option: **A — The interface extends `INotification` (recommended, pending the maintainer)**

### Rationale

- `OutboxWriter.WriteAsync` takes `IReadOnlyList<INotification>` and `IIntegrationEvent` is not an `INotification` today, so an integration event cannot currently be stored. A closes the gap for every implementer; B wins if the maintainer wants integration events to stay outside the notification hierarchy.
- **Class:** Dilemma

</details>

<details>
<summary><strong>8. gRPC and GraphQL — transports without an outbound publish operation</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `Encina.gRPC` and `Encina.GraphQL` get no outbox adapter; the transport rule is recorded as "not applicable to a request/response server bridge" for the outbound publish side, in the plan, the documentation and the contract suite's list of adapters (eight adapters)** | No invented client capability; nothing unbuildable | The category rule ("every transport") has two documented exceptions |
| **B) Build an outbound client adapter for each (a gRPC client call, a GraphQL mutation client)** | Rule met literally | Verified: neither package has an outbound client (`GrpcMediatorService` and `IGraphQLEncinaBridge` serve inbound calls; no `GrpcChannel` or `HttpClient` use); a new capability and a new dependency per package |

### Chosen Option: **A — No adapter for the two server bridges (recommended, pending the maintainer)**

### Rationale

- AGENTS.md section 5 requires every transport to support send and publish; these two packages expose Encina to callers and have no publish operation to wrap. B wins if the maintainer wants integration events delivered to a remote gRPC or GraphQL endpoint. Pull request E exists only if B is chosen.
- **Class:** Dilemma

</details>

<details>
<summary><strong>9. Carrying the outbox id on transports whose publisher has no message-id parameter</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Per transport: use the message-id parameter where the publisher has one (RabbitMQ, and the others verified at pick-up); Kafka carries it in the header `message-id` through `ProduceWithHeadersAsync`; the in-process bus carries none** | Uses each transport's native mechanism | The contract assertion is per transport |
| **B) Wrap every payload in an Encina envelope carrying the id** | One mechanism | Changes the wire format of every transport |

### Chosen Option: **A — Native mechanism per transport**

### Rationale

- Verified on origin/main: `IRabbitMQMessagePublisher.PublishAsync` takes `string? messageId`; `IKafkaMessagePublisher` has `ProduceWithHeadersAsync` and no message-id parameter; `IInMemoryMessageBus.PublishAsync` has none. The in-memory bus has no broker acknowledgement and no redelivery, so there is nothing to deduplicate: the contract test for "message id equals the outbox id" skips InMemory with that stated reason. B changes the wire format for no gain.
- **Class:** Trivial

</details>

<details>
<summary><strong>10. Where the correlation and tenant values of the route come from</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Ship the adapters now; `OutboxRoute.TenantId` and `CorrelationId` stay `null` and the headers are absent until the persisted context of #1164 lands, which fills them** | No blocking dependency; the adapters already write the headers when present | AGENTS.md section 5 "metadata propagation" is unmet on all adapters until #1164 lands, and the documentation says so |
| **B) Make #1164 a hard prerequisite of this issue** | Metadata is complete at first release | #1164 (open) delays the whole bridge |

### Chosen Option: **A — Ship now, headers when the context exists (recommended, pending the maintainer)**

### Rationale

- Verified: `IOutboxMessage` has no tenant or correlation member and the processor's scope has no ambient request context, so there is no trustworthy source today (a tenant id read from anywhere else would be a security risk). B wins if metadata propagation is an acceptance criterion for the first release.
- Under ADR-054 D4 the tenant header is written only when multi-tenancy is on and the route carries a tenant.
- **Class:** Dilemma

</details>

---

## Implementation Phases

### Phase 1: Core Models & Contracts

<details>
<summary><strong>Tasks</strong></summary>

1. `src/Encina.Messaging/Outbox/IOutboxTransportPublisher.cs` (new): `string TransportName { get; }` and `ValueTask<Either<EncinaError, Unit>> PublishAsync(IOutboxMessage message, object payload, OutboxRoute route, CancellationToken cancellationToken)`.
2. `src/Encina.Messaging/Outbox/OutboxRoute.cs` (new): `readonly record struct OutboxRoute(string Transport, string Destination, string? TenantId, string? CorrelationId)`; `OutboxRouteAttribute` (new): `[OutboxRoute("transport", "destination")]`.
3. `OutboxErrorCodes`: `NoTransportPublisher = "outbox.no_transport_publisher"` (`ErrorCategory.FailedPrecondition`), `TransportPublishFailed = "outbox.transport_publish_failed"` (`ErrorCategory.UpstreamFailure`), both created with `EncinaErrors.Create(code, ErrorCategory, ...)` (ADR-062); the contract suite asserts `GetCategory()` for every adapter, and that a `Left` or exception from an adapter carries the transport error code only, never broker response bodies, in `details`.
4. The marker (Design Choice 3 A): `IIntegrationEvent` moves from `src/Encina.DomainModeling/IIntegrationEvent.cs` to `src/Encina/Abstractions/` (namespace kept) and extends `INotification` (Design Choice 7 A); `IntegrationEvent` and the mapper stay in `Encina.DomainModeling` (so `IIntegrationEvent.cs` in DomainModeling is split, not deleted). The XML doc of the moved interface has a `cref` to `IDomainEventToIntegrationEventMapper{TDomain,TIntegration}`, a DomainModeling type that the core cannot reference (CS1574, and the build has zero warnings): in the moved file it becomes plain text (`<c>IDomainEventToIntegrationEventMapper</c>`). New core `IIntegrationEventTypeSource` (Design Choice 3), implemented by `Encina.DomainModeling` for registered mappers, is created in PR V together with the validator that reads it.
5. `PublicAPI.Unshipped.txt` of `Encina`, `Encina.Messaging` and `Encina.DomainModeling` (via `mechanical-fixer`; the moved type is removed from DomainModeling's file and added to the core's).

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 1</strong></summary>

```text
CONTEXT: Encina pre-1.0, .NET 10, no compatibility layers. Issue #2385 adds the Outbox-to-broker route of
ADR-028. IOutboxMessage is the provider-agnostic row abstraction; each database provider has its own
OutboxMessage class, so no abstraction may name one.
TASK: Create IOutboxTransportPublisher, OutboxRoute, OutboxRouteAttribute and the two error codes (with
ErrorCategory); move IIntegrationEvent to the core and make it an INotification (Dilemmas 3 and 7, pending the
maintainer). IIntegrationEventTypeSource is created later, in PR V.
KEY RULES: Either<EncinaError,T> returns; XML docs with examples; PublicAPI.Unshipped.txt; no provider-specific
code in Encina.Messaging.
REFERENCE FILES: src/Encina.Messaging/Outbox/IOutboxMessage.cs, src/Encina.DomainModeling/IIntegrationEvent.cs,
src/Encina.RabbitMQ/IRabbitMQMessagePublisher.cs
```

</details>

### Phase 2: Processor and Route Resolution

<details>
<summary><strong>Tasks</strong></summary>

1. `OutboxBatchProcessor`: the publish callback receives the resolved type. `OutboxProcessorBase.PublishAsync` is `private static` today and receives only the callback (verified, lines 136 and 170), so it becomes an instance method that asks a new protected virtual `ResolveDispatch(Type notificationType)` (default: transport when the type implements `IIntegrationEvent`, otherwise in process). Effects: every provider's `OutboxProcessor` subclass compiles unchanged (the new member is virtual with a default), `PublishAsync` can no longer be static, and the Marten processor of #2383 overrides `ResolveDispatch` to in-process (it must subclass `OutboxProcessorBase`). The same method is edited by #1220 (context building), #1221 and #1251: the later pull request rebases, and the change from static to instance is made by whichever of #1220 and this plan lands first.
2. `OutboxRouteResolver` (internal): attribute, then options table, then default transport and the destination convention; `Left(outbox.no_transport_publisher)` when the transport name has no registered publisher.
3. Transport dispatch: publisher resolved by `TransportName` from `IEnumerable<IOutboxTransportPublisher>`; a `Left` or exception is a failed delivery through the existing path (Design Choice 6); the outbox id is passed on as the broker message id.
4. Keep CRAP at 10 or less: dispatch decision, route resolution and publication are separate small methods.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 2</strong></summary>

```text
CONTEXT: OutboxProcessorBase.PublishAsync currently calls encina.Publish; OutboxBatchProcessor treats any Left
as a failed delivery (retry, then exhausted and dead letter). Plan #1220 changes the same method (it builds a
request context with the outbox id); coordinate the edits.
TASK: Add the dispatch decision, the route resolver and the transport dispatch described in the plan.
KEY RULES: a Left never becomes success; no payload or EncinaError.Message in logs or persisted failure text
(error codes only); TimeProvider is untouched here; the default for a non-integration-event row is unchanged.
REFERENCE FILES: src/Encina.Messaging/Outbox/OutboxProcessorBase.cs, src/Encina.Messaging/Outbox/OutboxBatchProcessor.cs,
docs/plans/es-reliable-delivery-implementation-plan-2383.md, docs/plans/outbox-dispatch-idempotency-implementation-plan-1220.md
```

</details>

### Phase 3: Configuration, DI & Startup Validation

<details>
<summary><strong>Tasks</strong></summary>

1. `OutboxOptions`: `DefaultTransport` (string?) and `Routes` (type to route table), validated at start-up.
2. `OutboxTransportStartupValidator` (hosted-service start check or `IValidateOptions`): fails when the Outbox is on, `IIntegrationEventTypeSource` services list integration-event types (registered mappers; #1221's message-type assemblies once they exist) and no `IOutboxTransportPublisher` is registered; also fails when a configured transport name has no publisher. It lands in its own pull request after every adapter pull request (see the split), so no earlier pull request leaves an application that registers integration-event mappers unable to start.
3. Each transport's `AddEncina{Transport}` registers its adapter with `TryAddEnumerable` (several transports may coexist); a DI test per transport with `ValidateOnBuild` and `ValidateScopes` in both registration orders.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 3</strong></summary>

```text
CONTEXT: AGENTS.md section 3, registration completeness; ADR-050 startup validation of required modules.
TASK: Add the options, the validator and the adapter registrations with their DI tests.
KEY RULES: fail closed (an Outbox that can lose integration events does not start); the application's own
registration wins; opt-in preserved (nothing changes when no integration event type exists).
REFERENCE FILES: src/Encina.Messaging/MessagingServiceCollectionExtensions.cs, src/Encina.RabbitMQ/ServiceCollectionExtensions.cs,
docs/architecture/adr/050-modules-may-require-modules.md
```

</details>

### Phase 4: Transport Adapters — 8 transports (10 under Design Choice 8 B)

<details>
<summary><strong>Tasks</strong></summary>

One adapter per transport package, wrapping the transport's existing publisher, each with `TransportName` equal to a documented constant, header propagation (correlation id, tenant id) and the outbox id as the broker message id:

1. `Encina.RabbitMQ`: `RabbitMQOutboxTransportPublisher` over `IRabbitMQMessagePublisher.PublishAsync(message, routingKey, messageId, ct)`.
2. `Encina.Kafka`: over `IKafkaMessagePublisher.ProduceWithHeadersAsync` (topic from the route, key from the destination convention; there is no message-id parameter, so the outbox id travels in the header `message-id`, Design Choice 9; headers also carry the metadata when present).
3. `Encina.NATS`, `Encina.MQTT`, `Encina.Redis.PubSub`, `Encina.AzureServiceBus`, `Encina.AmazonSQS`: over their `I*MessagePublisher`; whether each takes a message-id parameter is checked at pick-up (Design Choice 9: native mechanism per transport).
4. `Encina.InMemory`: over `IInMemoryMessageBus.PublishAsync` (the test transport; no message-id parameter, Design Choice 9).
5. `Encina.gRPC` and `Encina.GraphQL`: server-side bridges with no outbound publish operation (verified); adapters only if the maintainer picks Design Choice 8 B, in which case pull request E exists.
6. Each adapter maps a transport failure to `outbox.transport_publish_failed` (the transport error code is kept as a detail, never the message or a response body) and uses the transport's own `[LoggerMessage]` range (RabbitMQ 4100-4149, Kafka 4150-4199, NATS 4200-4249, MQTT 4250-4299, AzureServiceBus 4300-4349, AmazonSQS 4350-4399, RedisPubSub 4400-4449, InMemory 4450-4499, gRPC 4500-4549, GraphQL 4550-4599).

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 4</strong></summary>

```text
CONTEXT: AGENTS.md section 5, transport category: every transport supports send/publish, error handling and DLQ,
metadata propagation. The existing publishers already accept a message id for deduplication.
TASK: Implement the adapters listed in the Tasks block, one pull request per group of transports.
KEY RULES: identical contract on every transport; metadata propagation into headers; no payload or tenant data in
logs; EventIds only inside the transport's own registered range, packed after its highest id in use.
REFERENCE FILES: src/Encina.RabbitMQ/RabbitMQMessagePublisher.cs, src/Encina.Kafka/IKafkaMessagePublisher.cs,
src/Encina.InMemory/IInMemoryMessageBus.cs, src/Encina.gRPC/GrpcMediatorService.cs, src/Encina/Diagnostics/EventIdRanges.cs
```

</details>

### Phase 5: Cross-Cutting Integration

<details>
<summary><strong>Tasks</strong></summary>

1. Resilience (function 6): retry and backoff are the outbox's; a Polly policy around the broker call is configured per transport package where it exists (`Encina.Polly`), not added here.
2. Health checks (function 4): each transport already registers its health check (for example `RabbitMQHealthCheck`); this phase only adds a test that the outbox with a transport registered reports the transport check.
3. Multi-tenancy (function 10): the tenant id travels as a transport header when the route carries one (Design Choice 10; ADR-054 D4: only when multi-tenancy is on); per-tenant destinations are not introduced.
4. Idempotency (function 9): the broker message id is the outbox id (consumer-side deduplication is #1609 and the Inbox).

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 5</strong></summary>

```text
CONTEXT: Phases 1-4 built the route and the adapters.
TASK: Verify the matrix rows marked included are implemented and add the health-check and tenant-header tests.
KEY RULES: AGENTS.md section 6; no scope growth; deferred rows keep their issue.
REFERENCE FILES: AGENTS.md section 6, src/Encina.RabbitMQ/RabbitMQHealthCheck.cs
```

</details>

### Phase 6: Observability

<details>
<summary><strong>Tasks</strong></summary>

1. Counter `encina.outbox.transport_published_total` (tags `transport`, `outcome`) and a span `encina.outbox.transport_publish` with messaging semantic attributes (`messaging.system`, `messaging.destination.name`, `messaging.message.id`), no payload; `Encina.OpenTelemetry` adds the source. The tenant is a span attribute (`encina.tenant_id`) and a counter tag (`tenant`) **only when multi-tenancy is enabled and the route carries a tenant (ADR-054 D4)**; with multi-tenancy off no tenant attribute or tag is emitted.
2. `OutboxTransportLog` (`[LoggerMessage]`) with EventIds 5817-5822 inside the family block 5800-5849 (`EventIdRanges.MessagingInboxIdempotency`, registered by plan #3440 or by the first plan of the family to land), packed after #1221's 5811-5816: 5817 route resolved (Debug), 5818 no publisher (Error), 5819 publish failed (Warning, error code), 5820 startup validation failed, 5821 publish succeeded (Debug), 5822 dispatch kept in process (Debug).
3. Transport adapters log through their own registered ranges (Phase 4).

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 6</strong></summary>

```text
CONTEXT: ADR-021; the family block 5800-5849 is registered once (#3440 5800-5804, #1220 5805-5810, #1221
5811-5816); this plan uses 5817-5822, with no gaps.
TASK: Add the counter, the span and OutboxTransportLog; re-check on the branch that 5817-5822 are free.
KEY RULES: bounded tag dimensions; EncinaError.Message never in logs; exceptions through ForLogging(); packed ids.
REFERENCE FILES: src/Encina/Diagnostics/EventIdRanges.cs, src/Encina.Messaging/Diagnostics/MessagePurgeMetrics.cs,
src/Encina.OpenTelemetry/ServiceCollectionExtensions.cs
```

</details>

### Phase 7: Testing — 7 Test Types

<details>
<summary><strong>Tasks</strong></summary>

1. Unit: route resolution (attribute, table, default, missing), dispatch decision (integration event versus domain event), failure paths (Left, exception, no publisher), startup validator, each adapter with a mocked transport publisher (NSubstitute).
2. Guard: every new public member.
3. Contract: one contract suite run against every adapter (8, or 10 under Design Choice 8 B): success, failure to `outbox.transport_publish_failed` with `GetCategory()` equal to `UpstreamFailure` and no broker response body in `details`, message id equals the outbox id (InMemory excepted, Design Choice 9), headers carry correlation and tenant when the route has them.
4. Property: for any mix of integration and domain rows, no domain row reaches a transport publisher and no integration row is marked processed without a publisher call.
5. Integration: real brokers through Testcontainers where a container exists (RabbitMQ, Kafka, NATS, Redis, MQTT), local emulators for the cloud transports (Azure Service Bus emulator, LocalStack for SQS), InMemory directly; an end-to-end scenario per transport: an integration event written with a database store (one scenario also across the 10 database providers with the InMemory transport) reaches the broker; a `Left` retries then dead-letters; a processor that always dispatches in process (Marten) never calls a transport.
6. Load and Benchmark: `.md` justifications (the broker round trip dominates).
7. Manifest targets per new or touched `src/` file (floors for the worker, never lowered; `.github/coverage-manifest/{Package}.json`):

| File | Unit | Guard | Contract | Property | Integration | Justification |
|------|-----:|------:|---------:|---------:|------------:|---------------|
| `OutboxRouteResolver.cs`, `OutboxProcessorBase.cs` (dispatch decision) | 95 | 100 | 0 | 90 | 0 | Pure decision logic; every branch (attribute, table, default, missing publisher, domain versus integration row) is a unit case and the routing invariant is a property. |
| `OutboxTransportStartupValidator.cs`, `OutboxOptions.cs` additions | 95 | 100 | 0 | 0 | 0 | Validation rules over DI descriptors; each failure mode is a unit case. |
| `*OutboxTransportPublisher.cs` (8 adapters) | 90 | 100 | 100 | 0 | 90 | The adapter is a thin mapping over a mocked publisher in unit tests; the shared contract suite pins identical behavior and the integration scenario proves delivery to the real broker or emulator. |
| `IIntegrationEvent.cs` (moved), `IIntegrationEventTypeSource` implementation | 95 | 100 | 100 | 0 | 0 | Marker and a type list; the contract keeps the shape. |

CRAP table of every changed method before each pull request.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 7</strong></summary>

```text
CONTEXT: Per-flag coverage; tests run real package code; Shouldly via Encina.Testing.Shouldly; NSubstitute for
mocks; integration tests use shared [Collection] fixtures and Encina.Testing.Testcontainers.
TASK: Implement the listed tests and the justification files; set manifest targets.
KEY RULES: no Thread.Sleep; the contract suite runs against real adapters; targets never go down; every
transport covered.
REFERENCE FILES: tests/Encina.IntegrationTests/Messaging/Outbox, existing transport integration tests under
tests/Encina.IntegrationTests, .github/coverage-manifest/Encina.Messaging.json
```

</details>

### Phase 8: Documentation & Finalization

<details>
<summary><strong>Tasks</strong></summary>

1. XML docs with examples; `changelog.d/2385-outbox-transport-publisher.added.md` (via `mechanical-fixer`).
2. `docs/messaging/transports.md` and `docs/features/transactional-outbox.md`: the route, the attribute, the options, the startup error, what the Outbox now does with integration events (by `docs-writer`); correct any public text that says the Outbox publishes to a broker before this ships (#3385).
3. ADR for the decided Dilemmas (3, 4, 5, 7, 8 and 10) once decided (ADR number assigned when the ADR pull request opens); `docs/INVENTORY.md`; PublicAPI files of every touched package.
4. Build 0 warnings; tests at manifest targets.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 8</strong></summary>

```text
CONTEXT: Documentation follows the encina-docs skill; coverage figures are cited, never typed.
TASK: Finish XML docs, changelog, docs/messaging/transports.md, the outbox page, the ADR and INVENTORY.
KEY RULES: English only; do not name a private client application; PublicAPI files complete.
REFERENCE FILES: docs/messaging/transports.md, docs/features/transactional-outbox.md, .claude/skills/encina-docs/SKILL.md
```

</details>

---

## Research

### Relevant Standards & Specifications

| Standard | Requirement | How this feature meets it |
|----------|-------------|---------------------------|
| ADR-028 | Integration events leave the process only through the Outbox | Phases 1-4 |
| ADR-050 | Required modules enforced by startup validation | Phase 3 validator |
| AGENTS.md section 3 | Outbox at-least-once; a `Left` never reported as success | Design Choice 6 |
| AGENTS.md section 5 | Transport coherence | every adapter with one contract suite (8, or 10 under Design Choice 8 B) |
| Transactional outbox pattern (comparable products: MassTransit, NServiceBus, CAP, Wolverine) | A relay publishes stored events to the broker, with the message id for consumer deduplication | Phase 2 and Phase 4 |

### Existing Encina Infrastructure to Leverage

| Component | Location | Usage in this feature |
|-----------|----------|-----------------------|
| OutboxProcessorBase / OutboxBatchProcessor | src/Encina.Messaging/Outbox/ | dispatch decision; retry and dead-letter path unchanged |
| IOutboxMessage | src/Encina.Messaging/Outbox/IOutboxMessage.cs | provider-agnostic row |
| IIntegrationEvent / IDomainEventToIntegrationEventMapper | src/Encina.DomainModeling/ | marker and validator input |
| Transport publishers | src/Encina.{RabbitMQ,Kafka,NATS,MQTT,Redis.PubSub,AzureServiceBus,AmazonSQS,InMemory}/ | wrapped by the adapters (they accept a message id) |
| gRPC / GraphQL bridges | src/Encina.gRPC/GrpcMediatorService.cs, src/Encina.GraphQL/IGraphQLMediatorBridge.cs | adapters to confirm |
| Transport health checks | for example src/Encina.RabbitMQ/RabbitMQHealthCheck.cs | reported with the outbox |
| EventIdRanges transports | src/Encina/Diagnostics/EventIdRanges.cs (4100-4599) | adapter logging |
| Marten processor of #2383 | docs/plans/es-reliable-delivery-implementation-plan-2383.md | overrides the dispatch decision |
| Dead letter integration | src/Encina.Messaging/DeadLetter | exhausted transport failures |

### Event ID Allocation

| Package | Range | Notes |
|---------|-------|-------|
| `Encina.Messaging` | 5817-5822 inside `MessagingInboxIdempotency` (5800-5849) | Family block assigned 2026-11-11; packed order #3440 5800-5804, #1220 5805-5810, #1221 5811-5816, this plan 5817-5822, no gaps; the first plan of the family to land registers the block |
| Transport packages | existing ranges 4100-4599 | Packed after each package's highest id in use; no new range |

### Estimated File Count

| Category | Files | Notes |
|----------|-------|-------|
| Core abstractions, resolver, validator, options, observability | 18 | contract, route, attribute, error codes, resolver, validator, options, processor edits, metrics, log, moved marker, type source and its DomainModeling implementation, instrumented source |
| Transport adapters and registrations | 16 | 8 adapters, 8 registration edits (10 and 20 under Design Choice 8 B) |
| Tests | 40 | unit, guard, contract suite, property, integration scenarios per transport, DI tests, health tests, 2 justification files, existing tests touched by the moved marker |
| Documentation and metadata | 26 | changelog, transports and outbox pages, ADR, INVENTORY, one manifest and one `PublicAPI.Unshipped.txt` per touched package |
| **Total** | **100** | Nine pull requests; the per-PR counts below add up to more because a file touched by two pull requests (a manifest or `PublicAPI.Unshipped.txt`) is counted in each |

### Pull request split

Counts include tests, coverage manifests and `PublicAPI.Unshipped.txt`. Each pull request builds alone and leaves existing applications starting: the startup validator, which can refuse to start, lands only after every adapter exists.

| PR | Content | Files (approx.) |
|----|---------|-----------------|
| A1 | Contracts and marker: `IOutboxTransportPublisher`, `OutboxRoute` and `OutboxRouteAttribute` (2 files), `OutboxErrorCodes` edit, the moved `IIntegrationEvent` (a new file in `src/Encina/Abstractions/` plus the edit of `src/Encina.DomainModeling/IIntegrationEvent.cs`, which also holds the base record `IntegrationEvent` and is therefore split, not deleted); PublicAPI of Encina, Messaging and DomainModeling; 3 manifests; 2 unit and guard test files. `IIntegrationEventTypeSource` is not in A1: it is only needed by the validator and lands in V | 14 |
| A2 | Processor: `ResolveDispatch`, route resolver, options, transport dispatch, metrics, `OutboxTransportLog`, `EventIdRanges` and `EncinaEventIdAllocationTests` edits; unit and property tests; existing processor tests that change; manifest | 14 |
| B1 | RabbitMQ and Kafka adapters, registrations, integration tests, DI tests, manifests, PublicAPI, plus the contract suite (the abstract suite and its RabbitMQ and Kafka derived classes, so the first adapters merge with the suite that pins `GetCategory()`, the no-response-body assertion and identical behavior) | 14 |
| B2 | NATS and MQTT, same content; each adds its derived class to the contract suite | 12 |
| B3 | Redis.PubSub and InMemory, same content; each adds its derived class to the contract suite | 12 |
| C | AzureServiceBus and AmazonSQS (emulator-based integration tests, fixtures) | 12 |
| E | gRPC and GraphQL adapters, only if Design Choice 8 B is picked | 8 |
| V | `OutboxTransportStartupValidator`, options validation, `IIntegrationEventTypeSource` (core) and its `Encina.DomainModeling` implementation, their tests, PublicAPI and manifests of Encina and DomainModeling, health-check tests, DI tests | 13 |
| F | Documentation, ADR, justifications, changelog | 6 |

A1 first; A2 depends on A1; B1, B2, B3, C (and E) depend on A2 and may land in any order; V depends on all adapters; F last. The Marten override of #2383 depends on A2.

---

## Combined AI Agent Prompts

<details>
<summary><strong>Full combined prompt for all phases</strong></summary>

```text
PROJECT CONTEXT:
Encina is pre-1.0, .NET 10 / C# 14, nullable enabled, no backward compatibility. Issue #2385 gives the Outbox a
route to brokers (ADR-028): IOutboxTransportPublisher per transport, resolved per route; integration events go to
the transport, domain events stay in process; a Left retries then dead-letters; an Outbox with integration-event
types and no publisher fails at start-up. Eight transports have an outbound publish operation; gRPC and GraphQL
are server bridges (Design Choice 8, pending the maintainer).

IMPLEMENTATION OVERVIEW:
1. IOutboxTransportPublisher, OutboxRoute, OutboxRouteAttribute, error codes with ErrorCategory;
   IIntegrationEvent in the core, extending INotification (both pending the maintainer).
2. Processor dispatch decision (IIntegrationEvent marker, PublishAsync becomes an instance method), route
   resolver, transport dispatch with the outbox id as broker message id; the Marten processor overrides to in-process.
3. OutboxOptions (DefaultTransport, Routes), startup validator, adapter registrations with TryAddEnumerable.
4. Eight adapters wrapping the existing publishers (gRPC and GraphQL only under Design Choice 8 B).
5. Counter, span, OutboxTransportLog EventIds 5817-5822 in the family block 5800-5849.
6. Tests on all flags with a contract suite over every adapter; docs, ADR and changelog.

KEY PATTERNS:
- ROP; a Left never becomes success; error codes (never messages) in logs and persisted text.
- Provider coherence: IOutboxMessage in abstractions; identical contract on every transport.
- Registered EventId ranges first; bounded tag dimensions; CRAP <= 10; targets never go down; PRs of at most 15 files.

REFERENCE FILES:
docs/plans/outbox-transport-publisher-implementation-plan-2385.md, src/Encina.Messaging/Outbox/OutboxProcessorBase.cs,
src/Encina.Messaging/Outbox/OutboxBatchProcessor.cs, src/Encina.Messaging/Outbox/IOutboxMessage.cs,
src/Encina.DomainModeling/IIntegrationEvent.cs, src/Encina.RabbitMQ/IRabbitMQMessagePublisher.cs,
src/Encina.Kafka/IKafkaMessagePublisher.cs, src/Encina.InMemory/IInMemoryMessageBus.cs,
docs/architecture/adr/028-domain-events-versus-integration-events.md, src/Encina/Diagnostics/EventIdRanges.cs
```

</details>

---

## Cross-Cutting Integration Matrix

| # | Function | Status | Notes |
|---|----------|--------|-------|
| 1 | Caching | ❌ | A write path to a broker; nothing to cache |
| 2 | OpenTelemetry | ✅ | Counter and publish span with messaging semantic attributes, no payload; tenant attribute and tag only when multi-tenancy is on (ADR-054 D4, Phase 6) |
| 3 | Structured Logging | ✅ | `OutboxTransportLog` EventIds 5817-5822 and each transport's own range (Phases 4 and 6) |
| 4 | Health Checks | ✅ | Each transport's existing health check is reported with the outbox; the startup validator covers the missing-publisher case (Phase 5) |
| 5 | Validation | ✅ | Startup validation of routes, transport names and the missing publisher (Phase 3) |
| 6 | Resilience | ❌ | Not applicable here: retry, backoff and dead letter are the outbox's own (Design Choice 6, at-least-once), and the adapters wrap publishers whose resilience each transport package already owns; this plan adds no new external call policy |
| 7 | Distributed Locks | ❌ | Multi-instance processing is the row-claiming concern of #1251; this plan adds no shared state |
| 8 | Transactions | ❌ | Publication happens after the commit, in the processor; the atomic part is the existing outbox write |
| 9 | Idempotency | ✅ | The outbox id is the broker message id for consumer-side deduplication (Phase 2) |
| 10 | Multi-Tenancy | ✅ | The tenant id travels as a transport header when the route carries one (source: #1164, Design Choice 10); no per-tenant destination (Phase 5) |
| 11 | Module Isolation | ❌ | `ModuleId` in messaging stays with #747 |
| 12 | Audit Trail | ❌ | Publication is operational; compliance events are audited at their source |

---

## Prerequisites & Dependencies

| Issue | Relation | Notes |
|-------|----------|-------|
| [#1220](https://github.com/dlrivada/Encina/issues/1220), [#1221](https://github.com/dlrivada/Encina/issues/1221), [#1251](https://github.com/dlrivada/Encina/issues/1251) | Share `OutboxProcessorBase` | #1220 (per-handler idempotency, context building), #1221 (stored type names) and #1251 (row claiming) edit the same class; rebase order is the order of landing, and the later pull request rebases onto the `PublishAsync` static-to-instance change |
| [#1221](https://github.com/dlrivada/Encina/issues/1221) | Soft dependency | The route destination convention and the validator's type source use the registry name and assemblies when #1221 exists; until then the type name and mapper registrations are used |
| [#1164](https://github.com/dlrivada/Encina/issues/1164) | Source of the route metadata | Persisted request context; header values for tenant and correlation come from it (Design Choice 10); absent until it lands |
| [#2383](https://github.com/dlrivada/Encina/issues/2383) | Consumer of the hook | The Marten processor overrides the dispatch decision to in-process |
| [#1609](https://github.com/dlrivada/Encina/issues/1609) | Consumer side | Transport consumers and Inbox deduplication |
| [#3379](https://github.com/dlrivada/Encina/issues/3379) | Ordering | The maintainer's Dilemma 3 orders this issue (v0.18.0) against the consumer side (v0.19.0) |
| [#3385](https://github.com/dlrivada/Encina/issues/3385) | Wording | Public text must not claim the Outbox publishes to a broker until this ships |
| [#3440](https://github.com/dlrivada/Encina/issues/3440) | Shares the EventId block | Family block 5800-5849 |

---

## Next Steps

1. At pick-up, re-check the Design Choices against today's code, ask the maintainer Dilemmas 3, 4, 5, 7, 8 and 10 and record the answers in `## Maintainer Decisions`; run `check-plan.ps1` without `-Draft`.
2. Design Choice 8: the absence of an outbound client in `Encina.gRPC` and `Encina.GraphQL` was verified on 2026-11-12; re-check it at pick-up.
3. `Encina.Messaging.csproj` references only `Encina.csproj` (checked 2026-11-12); Design Choice 3 decides how the marker becomes visible to it.
4. Re-check that 5817-5822 are free and that the family block is registered before PR A2; coordinate the `OutboxProcessorBase` edit with the plans of #1220 and #1251.
5. Measure per-flag coverage and the CRAP table for every touched file before each pull request.
