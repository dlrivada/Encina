# Implementation Plan: `Encina.Messaging` Outbox-to-transport publisher so integration events reach a broker (ADR-028)

> **Issue**: [#2385](https://github.com/dlrivada/Encina/issues/2385)
> **Type**: Feature
> **Complexity**: High (9 phases, 10 transports, ~55 files in 6 pull requests)
> **Estimated Scope**: ~1,300-1,700 lines of production code + ~1,800-2,300 lines of tests

---

## Summary

ADR-028 says integration events leave the process only through the Outbox, but the Outbox has no route to a broker. `OutboxProcessorBase.PublishAsync` calls `encina.Publish(typedNotification, cancellationToken)`, which dispatches in memory; when no handler is registered the dispatcher returns `Right` (`Encina.NotificationDispatcher`, "zero handlers is not an error"), the row is marked processed and the integration event reached nobody. The transports (RabbitMQ, Azure Service Bus, Amazon SQS, Kafka, NATS, Redis.PubSub, MQTT, InMemory, gRPC, GraphQL) only publish and have no reference to the Outbox. The consumer side is #1609, per-handler idempotency is #1220; nothing covers this bridge. The maintainer settled it on 2026-10-17: `IOutboxTransportPublisher` per transport, the processor resolves it per route, a `Left` goes to retry then dead letter, and a startup error when an Outbox has integration-event types and no publisher.

What it implements:

- `IOutboxTransportPublisher` in `Encina.Messaging` (with the `OutboxRoute` it receives) and one adapter per transport package that wraps the transport's existing publisher; the outbox id travels as the broker message id so consumers deduplicate (RabbitMQ, SQS and the other publishers already take a `messageId`).
- A route decision in the processor: a row whose stored type is an `IIntegrationEvent` goes to the transport publisher; every other row (domain events) keeps the in-process `IEncina.Publish`. A processor can override the decision: the Marten processor of #2383 always dispatches in process.
- `[OutboxRoute]` metadata (transport name, destination) with an options default, and metadata propagation (correlation id, tenant id) into transport headers.
- Fail-closed startup validation: an Outbox with registered integration-event types and no `IOutboxTransportPublisher` fails at start-up (cross-checked with the integration-event mappers registered in DI).

Standards: ADR-028 and ADR-050 (modules may require modules, enforced by startup validation); AGENTS.md section 3 (Outbox: reliable at-least-once; a `Left` is never reported as success); AGENTS.md section 5 (transport category: every transport supports send/publish, error handling and DLQ, metadata propagation).

Estimated scope: about 55 files (core 14, transport adapters 20, tests 16, documentation and metadata 5), shipped as 6 pull requests (see "Pull request split"). Affected packages: `Encina.Messaging`, `Encina.DomainModeling` (one base-record change), `Encina.OpenTelemetry`, `Encina.RabbitMQ`, `Encina.AzureServiceBus`, `Encina.AmazonSQS`, `Encina.Kafka`, `Encina.NATS`, `Encina.Redis.PubSub`, `Encina.MQTT`, `Encina.InMemory`, `Encina.gRPC`, `Encina.GraphQL`.

Provider category: Messaging transports (10). The Outbox processor is shared by the 10 database stores and is unchanged per database provider; the ten transports are the provider axis here (AGENTS.md section 5).

---

## Design Choices

<details>
<summary><strong>1. Contract — one publisher abstraction per transport, resolved per route</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `IOutboxTransportPublisher` implemented by an adapter in each transport package; the processor resolves it by route** | One contract; processors stay generic; DLQ semantics shared; the transport coherence rule is met | 10 adapters to build and test |
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
| **A) Rows whose stored type implements `IIntegrationEvent` go to the transport; all others stay in process** | Matches ADR-028 (domain events in process, integration events through the broker) without a new marker | The row's type must resolve at dispatch time (it already must, for deserialization) |
| **B) A flag column on the outbox row** | Explicit per message | Schema change on 10 providers for something the type already says |
| **C) Everything goes to the transport when one is registered** | Simple | Domain events would leave the process, which ADR-028 forbids |

### Chosen Option: **A — The `IIntegrationEvent` marker decides**

### Rationale

- ADR-028: "Every transport package consumes integration events from the Outbox relay, never domain events", and the event-sourced processor of #2383 overrides the decision to always dispatch in process.
- **Class:** Settled
- **Settled by:** ADR-028 and the plan of #2383 (`docs/plans/es-reliable-delivery-implementation-plan-2383.md`, the Marten processor never calls a transport publisher)

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
| **A) A startup validator inspects the registered `IDomainEventToIntegrationEventMapper<,>` services and the assemblies configured for message types (#1221); an Outbox with any integration-event type and no `IOutboxTransportPublisher` fails at start-up** | Fails before the first message, as the issue asks; no reflection beyond the DI descriptors | Types that are neither mapped nor registered are not seen |
| **B) Fail on the first integration-event row (a `Left` that retries and dead-letters)** | Sees every type | The first deployment error shows up as dead letters, hours later |
| **C) A health check only** | Visible to operations | Does not prevent a running, silently failing application |

### Chosen Option: **A — Startup validation from registrations, with B as the safety net (recommended, pending the maintainer)**

### Rationale

- C wins only where operations prefers degraded health to a refusal to start; B is kept as the runtime net for types the validator cannot see (the row fails visibly with `outbox.no_transport_publisher`, never `Right`).
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
| **A) `IntegrationEvent` (the base record) also implements `INotification`, so the existing `OutboxWriter` and `TransactionalOutboxPipelineBehavior` store it unchanged** | No second write path; one change in one file | The base record references the core `INotification` |
| **B) Make the outbox store `object` instead of `INotification`** | No marker needed | Changes `IHasNotifications`, `OutboxWriter` and the processor signatures across the package |

### Chosen Option: **A — The base record implements `INotification`**

### Rationale

- `OutboxWriter.WriteAsync` takes `IReadOnlyList<INotification>` and `IIntegrationEvent` is not an `INotification` today, so an integration event cannot currently be stored; the smallest change that closes the gap is the marker on the base record (the plan verifies `Encina.DomainModeling` already references the core).
- **Class:** Trivial
- **Settled by:** ADR-028 (integration events are published only through the Outbox) and `src/Encina.Messaging/Outbox/OutboxWriter.cs` (the writer accepts `INotification`)

</details>

---

## Implementation Phases

### Phase 1: Core Models & Contracts

<details>
<summary><strong>Tasks</strong></summary>

1. `src/Encina.Messaging/Outbox/IOutboxTransportPublisher.cs` (new): `string TransportName { get; }` and `ValueTask<Either<EncinaError, Unit>> PublishAsync(IOutboxMessage message, object payload, OutboxRoute route, CancellationToken cancellationToken)`.
2. `src/Encina.Messaging/Outbox/OutboxRoute.cs` (new): `readonly record struct OutboxRoute(string Transport, string Destination, string? TenantId, string? CorrelationId)`; `OutboxRouteAttribute` (new): `[OutboxRoute("transport", "destination")]`.
3. `OutboxErrorCodes`: `NoTransportPublisher = "outbox.no_transport_publisher"`, `TransportPublishFailed = "outbox.transport_publish_failed"`.
4. `src/Encina.DomainModeling/IIntegrationEvent.cs`: the `IntegrationEvent` base record also implements `INotification` (Design Choice 7); verify the project reference first.
5. `PublicAPI.Unshipped.txt` of `Encina.Messaging` and `Encina.DomainModeling` (via `mechanical-fixer`).

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 1</strong></summary>

```text
CONTEXT: Encina pre-1.0, .NET 10, no compatibility layers. Issue #2385 adds the Outbox-to-broker route of
ADR-028. IOutboxMessage is the provider-agnostic row abstraction; each database provider has its own
OutboxMessage class, so no abstraction may name one.
TASK: Create IOutboxTransportPublisher, OutboxRoute, OutboxRouteAttribute and the two error codes; make the
IntegrationEvent base record also an INotification.
KEY RULES: Either<EncinaError,T> returns; XML docs with examples; PublicAPI.Unshipped.txt; no provider-specific
code in Encina.Messaging.
REFERENCE FILES: src/Encina.Messaging/Outbox/IOutboxMessage.cs, src/Encina.DomainModeling/IIntegrationEvent.cs,
src/Encina.RabbitMQ/IRabbitMQMessagePublisher.cs
```

</details>

### Phase 2: Processor and Route Resolution

<details>
<summary><strong>Tasks</strong></summary>

1. `OutboxBatchProcessor`: the publish callback receives the resolved type; `OutboxProcessorBase.PublishAsync` asks a new virtual `ResolveDispatch(Type notificationType)` (default: transport when the type implements `IIntegrationEvent`, otherwise in process). The Marten processor of #2383 overrides it to in-process.
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
2. `OutboxTransportStartupValidator` (hosted-service start check or `IValidateOptions`): fails when the Outbox is on, integration-event types exist (registered mappers or configured message-type assemblies) and no `IOutboxTransportPublisher` is registered; also fails when a configured transport name has no publisher.
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

### Phase 4: Transport Adapters — 10 transports

<details>
<summary><strong>Tasks</strong></summary>

One adapter per transport package, wrapping the transport's existing publisher, each with `TransportName` equal to a documented constant, header propagation (correlation id, tenant id) and the outbox id as the broker message id:

1. `Encina.RabbitMQ`: `RabbitMQOutboxTransportPublisher` over `IRabbitMQMessagePublisher.PublishAsync(message, routingKey, messageId, ct)`.
2. `Encina.Kafka`: over `IKafkaMessagePublisher.ProduceWithHeadersAsync` (topic from the route, key from the destination convention, headers carry the metadata).
3. `Encina.NATS`, `Encina.MQTT`, `Encina.Redis.PubSub`, `Encina.AzureServiceBus`, `Encina.AmazonSQS`: over their `I*MessagePublisher`.
4. `Encina.InMemory`: over `IInMemoryMessageBus.PublishAsync` (the test transport).
5. `Encina.gRPC` and `Encina.GraphQL`: request/response bridges rather than brokers; the adapter maps the route to the remote notification call (read `GrpcMediatorService` and `IGraphQLEncinaBridge` first); if a transport has no outbound publish operation, the plan's Next Steps records it as a gap to confirm with the maintainer before the pull request.
6. Each adapter maps a transport failure to `outbox.transport_publish_failed` (the transport error code is kept as a detail, never the message) and uses the transport's own `[LoggerMessage]` range (RabbitMQ 4100-4149, Kafka 4150-4199, NATS 4200-4249, MQTT 4250-4299, AzureServiceBus 4300-4349, AmazonSQS 4350-4399, RedisPubSub 4400-4449, InMemory 4450-4499, gRPC 4500-4549, GraphQL 4550-4599).

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
3. Multi-tenancy (function 10): the tenant id travels as a transport header; per-tenant destinations are not introduced.
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

1. Counter `encina.outbox.transport_published_total` (tags `transport`, `outcome`) and a span `encina.outbox.transport_publish` with messaging semantic attributes (`messaging.system`, `messaging.destination.name`, `messaging.message.id`), no payload; `Encina.OpenTelemetry` adds the source.
2. `OutboxTransportLog` (`[LoggerMessage]`) with EventIds 5835-5840 inside the family block 5800-5849 (`EventIdRanges.MessagingInboxIdempotency`, registered by plan #3440 or by the first plan of the family to land): route resolved (Debug), no publisher (Error), publish failed (Warning, error code), startup validation failed.
3. Transport adapters log through their own registered ranges (Phase 4).

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 6</strong></summary>

```text
CONTEXT: ADR-021; the family block 5800-5849 is registered once (#3440 5800-5804, #1220 5805-5810, #1221
5820-5825); this plan uses 5835-5840.
TASK: Add the counter, the span and OutboxTransportLog; re-check on the branch that 5835-5840 are free.
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
3. Contract: one contract suite run against all 10 adapters (success, failure to `outbox.transport_publish_failed`, message id equals the outbox id, headers carry correlation and tenant).
4. Property: for any mix of integration and domain rows, no domain row reaches a transport publisher and no integration row is marked processed without a publisher call.
5. Integration: real brokers through Testcontainers where a container exists (RabbitMQ, Kafka, NATS, Redis, MQTT), local emulators for the cloud transports (Azure Service Bus emulator, LocalStack for SQS), InMemory directly; an end-to-end scenario per transport: an integration event written with a database store (one scenario also across the 10 database providers with the InMemory transport) reaches the broker; a `Left` retries then dead-letters; a processor that always dispatches in process (Marten) never calls a transport.
6. Load and Benchmark: `.md` justifications (the broker round trip dominates).
7. Manifest targets with justifications for every touched file; CRAP table.

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
3. ADR for Design Choices 4 and 5 once decided (next free number at implementation time); `docs/INVENTORY.md`; PublicAPI files of every touched package.
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
| AGENTS.md section 5 | Transport coherence | 10 adapters with one contract suite |
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
| `Encina.Messaging` | 5835-5840 inside `MessagingInboxIdempotency` (5800-5849) | Family block assigned 2026-11-11; used so far: #3440 5800-5804, #1220 5805-5810, #1221 5820-5825; the first plan of the family to land registers the block |
| Transport packages | existing ranges 4100-4599 | Packed after each package's highest id in use; no new range |

### Estimated File Count

| Category | Files | Notes |
|----------|-------|-------|
| Core abstractions, resolver, validator, options, observability | 14 | contract, route, attribute, error codes, resolver, validator, options, processor edits, metrics, log, DomainModeling edit, instrumented source |
| Transport adapters and registrations | 20 | 10 adapters, 10 registration edits |
| Tests | 16 | unit, guard, contract suite, property, integration scenarios per transport group, DI tests, 2 justification files |
| Documentation and metadata | 5 | changelog, transports and outbox pages, ADR, INVENTORY, manifests |
| **Total** | **55** | Six pull requests |

### Pull request split

| PR | Content | Files (approx.) |
|----|---------|-----------------|
| A | Phases 1, 2, 3 core, observability core, unit, guard and property tests | 15 |
| B | RabbitMQ, Kafka, NATS adapters with registrations and integration tests | 12 |
| C | Redis.PubSub, MQTT, InMemory adapters with registrations and tests; the contract suite | 12 |
| D | AzureServiceBus and AmazonSQS adapters (emulator-based tests) | 8 |
| E | gRPC and GraphQL adapters (after the Next Steps confirmation) | 6 |
| F | Documentation, ADR, justifications, changelog | 6 |

A is the prerequisite of B to F; the Marten override of #2383 depends on A.

---

## Combined AI Agent Prompts

<details>
<summary><strong>Full combined prompt for all phases</strong></summary>

```text
PROJECT CONTEXT:
Encina is pre-1.0, .NET 10 / C# 14, nullable enabled, no backward compatibility. Issue #2385 gives the Outbox a
route to brokers (ADR-028): IOutboxTransportPublisher per transport, resolved per route; integration events go to
the transport, domain events stay in process; a Left retries then dead-letters; an Outbox with integration-event
types and no publisher fails at start-up. Ten transports are in scope.

IMPLEMENTATION OVERVIEW:
1. IOutboxTransportPublisher, OutboxRoute, OutboxRouteAttribute, error codes; IntegrationEvent also INotification.
2. Processor dispatch decision (IIntegrationEvent marker), route resolver, transport dispatch with the outbox id
   as broker message id; the Marten processor overrides to in-process.
3. OutboxOptions (DefaultTransport, Routes), startup validator, adapter registrations with TryAddEnumerable.
4. Ten adapters wrapping the existing publishers; gRPC and GraphQL confirmed before their pull request.
5. Counter, span, OutboxTransportLog EventIds 5835-5840 in the family block 5800-5849.
6. Tests on all flags with a contract suite over the 10 adapters; docs, ADR and changelog.

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
| 2 | OpenTelemetry | ✅ | Counter and publish span with messaging semantic attributes, no payload (Phase 6) |
| 3 | Structured Logging | ✅ | `OutboxTransportLog` EventIds 5835-5840 and each transport's own range (Phases 4 and 6) |
| 4 | Health Checks | ✅ | Each transport's existing health check is reported with the outbox; the startup validator covers the missing-publisher case (Phase 5) |
| 5 | Validation | ✅ | Startup validation of routes, transport names and the missing publisher (Phase 3) |
| 6 | Resilience | ⏭️ | The outbox retry and dead letter apply; a Polly policy around the broker call belongs to the transport packages (`Encina.Polly` integration); no issue yet, to be opened if a transport lacks one |
| 7 | Distributed Locks | ❌ | Multi-instance processing is the row-claiming concern of #1251; this plan adds no shared state |
| 8 | Transactions | ❌ | Publication happens after the commit, in the processor; the atomic part is the existing outbox write |
| 9 | Idempotency | ✅ | The outbox id is the broker message id for consumer-side deduplication (Phase 2) |
| 10 | Multi-Tenancy | ✅ | The tenant id travels as a transport header; no per-tenant destination (Phase 5) |
| 11 | Module Isolation | ❌ | `ModuleId` in messaging stays with #747 |
| 12 | Audit Trail | ❌ | Publication is operational; compliance events are audited at their source |

---

## Prerequisites & Dependencies

| Issue | Relation | Notes |
|-------|----------|-------|
| [#1220](https://github.com/dlrivada/Encina/issues/1220) | Shares `OutboxProcessorBase` | Per-handler idempotency for in-process dispatch; both edit `PublishAsync`, the later pull request rebases |
| [#1221](https://github.com/dlrivada/Encina/issues/1221) | Shares the stored type name | The route destination convention and the reader use the registry name when present |
| [#2383](https://github.com/dlrivada/Encina/issues/2383) | Consumer of the hook | The Marten processor overrides the dispatch decision to in-process |
| [#1609](https://github.com/dlrivada/Encina/issues/1609) | Consumer side | Transport consumers and Inbox deduplication |
| [#3379](https://github.com/dlrivada/Encina/issues/3379) | Ordering | The maintainer's Dilemma 3 orders this issue (v0.18.0) against the consumer side (v0.19.0) |
| [#3385](https://github.com/dlrivada/Encina/issues/3385) | Wording | Public text must not claim the Outbox publishes to a broker until this ships |
| [#3440](https://github.com/dlrivada/Encina/issues/3440) | Shares the EventId block | Family block 5800-5849 |

---

## Next Steps

1. At pick-up, re-check the Design Choices against today's code, ask the maintainer Dilemmas 4 and 5 and record the answers in `## Maintainer Decisions`; run `check-plan.ps1` without `-Draft`.
2. Before PR E, read `GrpcMediatorService` and `IGraphQLEncinaBridge` to confirm that each has an outbound publish operation; if not, record the gap and ask the maintainer how the transport category rule applies to a request/response bridge.
3. `Encina.DomainModeling` already references `Encina.csproj` (checked 2026-11-11), so Design Choice 7 needs no new project reference; confirm that again before PR A.
4. Re-check that 5835-5840 are free and that the family block is registered before PR A; coordinate the `OutboxProcessorBase` edit with the plan of #1220.
5. Measure per-flag coverage and the CRAP table for every touched file before each pull request.
