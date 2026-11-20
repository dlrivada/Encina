# Implementation Plan: OpenTelemetry for the Ten Message Transports (Publish and Receive Spans, W3C Propagation, Messaging Metrics)

> **Issue**: [#725](https://github.com/dlrivada/Encina/issues/725)
> **Type**: Feature (observability, in the 0.14.0 scope of ADR-054 D5)
> **Related**: ADR-054 (D2 parent or link rule, D3 names, D4 tenant tag, D5 scope, settled point 5 all 10 transports), #177 (messaging attribute constants, owns `MessagingSemanticConventions`), #132 (owns `IContextPropagator` and `TraceContextConsumer`), #186 (`RequestTraceContext`), #2385 (outbox transport adapters), #1609 (consumers of the publish-only transports), #2676 (tenant helper), #1164, #906, ADR-062
> **Complexity**: High (10 packages with ten different publisher interfaces and header models)
> **Estimated Scope**: ~420 lines of production code + ~1,300 lines of tests, in five pull requests of at most 15 files

---

## Summary

### What exists today (read on 2026-11-20, main `0336d31c7`)

- None of the ten transports emits a span or an instrument, and none propagates a trace context (ADR-054 Context; `traceparent` does not occur in `src`).
- **There is no shared transport abstraction**: `IMessageTransport` named in `AGENTS.md` does not exist. Each package has its own publisher interface with its own return type: `IRabbitMQMessagePublisher` (`PublishAsync`, `SendToQueueAsync`, `RequestAsync`), `IKafkaMessagePublisher` (`ProduceAsync`, `ProduceBatchAsync`, `ProduceWithHeadersAsync`), `INATSMessagePublisher`, `IAzureServiceBusMessagePublisher`, `IAmazonSQSMessagePublisher`, `IRedisPubSubMessagePublisher`, `IMQTTMessagePublisher`, `IInMemoryMessageBus`; `Encina.gRPC` (`GrpcEncinaService`) and `Encina.GraphQL` (`IGraphQLMediatorBridge`) are server bridges, not publishers.
- **Only some transports can consume**: MQTT (`MqttSubscription<T>`, `MqttPatternSubscription<T>`) and Redis.PubSub (`SubscribeAsync` with `channelQueue.OnMessage`) have subscriptions; InMemory has an in-process bus with subscriber delegates; RabbitMQ, Kafka, NATS, Azure Service Bus and Amazon SQS are publish-only (#1609, open). The issue's Acknowledge, Reject and queue depth have no code to instrument on `main`.
- Header carriers differ (the matrix of the #132 plan): RabbitMQ `BasicProperties.Headers`, Kafka `Headers` (`byte[]`), NATS headers, MQTT user properties (`.WithUserProperty(` is already used in `MQTTMessagePublisher.cs`), Azure Service Bus application properties, SQS message attributes; Redis.PubSub serializes `RedisMessageWrapper<TMessage>` JSON, an envelope that already exists; InMemory has no wire.
- Package references: RabbitMQ, Kafka, NATS, MQTT, Azure Service Bus, SQS and gRPC reference `Encina` and `Encina.Messaging`; **Redis.PubSub and InMemory reference only `Encina`**; GraphQL references `Encina` and `Encina.DomainModeling`. So a shared helper must live in core.
- `InternalsVisibleTo`: RabbitMQ, Kafka, Azure Service Bus, InMemory and gRPC have none; MQTT, NATS, SQS, Redis.PubSub have `Encina.UnitTests` only; GraphQL has UnitTests and IntegrationTests. No transport lets `Encina.GuardTests` see an internal type, so every type this plan needs to guard-test is public.
- The sibling plans define the pieces this plan uses: #177 the constants class `MessagingSemanticConventions` (destination, partition, client id and body size keys are "set by the transports, #725"), #132 `IContextPropagator` with caller-supplied setters and getters and `TraceContextConsumer` (child for `Send`, link for `Publish`, scheduled and batch), #2385 the outbox adapters that wrap each publisher.

### What this plan delivers

- A public helper in core, `TransportTelemetry` (`Encina.Diagnostics`): one instance per transport owns that package's `ActivitySource` and `Meter` (named after the package, ADR-054 D3: `Encina.RabbitMQ`, `Encina.Kafka`, ...), starts producer and consumer spans, records the three OpenTelemetry messaging instruments and calls the propagator of #132; it is the only new type outside the transports.
- Each publisher gets a `private static readonly TransportTelemetry` and wraps its publish operations: span `{operation} {destination-kind}` with `messaging.system`, `messaging.operation.name`, `messaging.destination.name` where the transport knows it (never a message id or body), `error.type` on failure; injection of `traceparent` and `tracestate` into the transport's native carrier.
- Consumer spans where code exists: MQTT, Redis.PubSub and InMemory deliveries (parent or link per ADR-054 D2); gRPC and GraphQL handlers get a child span of the ambient request activity (ASP.NET Core already extracted the HTTP trace headers); they emit no `messaging.client.consumed.messages` count (register row C6).
- Instruments per transport meter (the eight publisher transports): `messaging.client.operation.duration` (histogram, `s`), `messaging.client.sent.messages` and `messaging.client.consumed.messages` (counters, consumed only where a delivery exists: MQTT, Redis.PubSub, InMemory), names pinned in the constants class of #177. gRPC and GraphQL get child spans only.
- The tenant tag through the helper of #2676.
- Not delivered, each in a Design Choice: queue depth, acknowledge and reject (no code to observe), consumer spans of the five publish-only transports (#1609).
- Shared pieces and owner: the helper is this plan's; constants #177's; propagation #132's; adapters #2385's.
- Affected packages: `Encina` (core), the 10 transports. Provider category: Messaging transport (10 existing, 6 planned, `AGENTS.md` section 5); the six planned transports adopt `TransportTelemetry` in their own plans.

---

## Design Choices

<details>
<summary><strong>1. Where the shared transport telemetry lives</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) A public `TransportTelemetry` class in core `Encina.Diagnostics`, one instance per transport** | Reachable from all 10 packages (Redis.PubSub and InMemory reference only `Encina`); no `InternalsVisibleTo` for 10 assemblies; one place for the rules | Public API in core |
| **B) In `Encina.Messaging`** | Natural home | Redis.PubSub and InMemory would need a new project reference |
| **C) Duplicate the helper in each transport** | No shared API | 10 copies of span, metric and propagation code; drift |
| **D) Decorators in `Encina.OpenTelemetry` over each publisher interface** | Outside the transports | Eight different interfaces and return types, and the OpenTelemetry package would reference all transports |

### Chosen Option: **A - Core helper** (recommended, pending the maintainer)

### Rationale

- A wins on reach and coherence (ADR-054 settled point 5); B wins if the maintainer prefers adding the two project references to keep the helper next to the messaging stores.

**Class:** Dilemma

</details>

<details>
<summary><strong>2. Scope of operations: what is instrumented</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Every existing publish operation, every existing subscription delivery (MQTT, Redis.PubSub, InMemory) and the gRPC and GraphQL handlers; no acknowledge, reject or queue depth** | Everything that exists is observed; nothing is invented | The issue's Consume, Acknowledge, Reject and queue depth are partly absent |
| **B) Add consumer abstractions so the five publish-only transports can be observed** | Full lifecycle | That is #1609, a large feature of its own |
| **C) Publish only** | Smallest | Leaves existing deliveries dark; breaks ADR-054 D2's consumer rule |

### Chosen Option: **A - What exists** (recommended, pending the maintainer)

### Rationale

- A wins now; B wins once #1609 lands (the consumers of the five transports then call `TransportTelemetry.StartConsume` in their own plans). Queue depth needs a broker query API that no transport exposes; it is #1609's follow-up.

**Class:** Dilemma

</details>

<details>
<summary><strong>3. Trace propagation per carrier</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Inject and extract through the setter and getter of #132 into each native carrier; Redis.PubSub adds `traceparent` and `tracestate` fields to `RedisMessageWrapper<TMessage>`; InMemory passes the context in-process with the delivery** | All 10 coherent (ADR-054 settled point 5); the Redis envelope already exists | The Redis wire JSON gains two optional fields (pre-1.0, no old subscriber to protect) |
| **B) Skip Redis.PubSub and InMemory** | No wire change | Partial subset, ruled out by ADR-054 settled point 5 |
| **C) Always create a new trace with a link** | Simple | Contradicts D2 for `Send` |

### Chosen Option: **A - Native carriers** (recommended, pending the maintainer)

### Rationale

- A wins on the settled coherence point; C wins only for a transport whose carrier cannot hold two strings. The Kafka header value is the UTF-8 bytes of the string; MQTT uses a user property (MQTT 3.1.1 connections have none and then take the Redis-style envelope only if #132's matrix says so: the helper reports "no carrier" and the span is then unlinked).

**Class:** Dilemma

</details>

<details>
<summary><strong>4. What attributes a transport span carries</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `messaging.system`, `messaging.operation.name`, `messaging.destination.name` where the transport has a name (queue, topic, subject, channel), `messaging.batch.message_count` for batches, `error.type`; never the message id, body, headers or key; the destination is emitted only for a configured or type-derived name, not for a caller string that may contain an id** | Follows the conventions and ADR-054 D6.3 spirit; bounded | A caller-supplied destination such as a per-tenant queue is omitted |
| **B) Emit every name the caller passes** | More detail | Per-tenant or per-entity names are unbounded and may carry personal data |

### Chosen Option: **A - Bounded attributes** (recommended, pending the maintainer)

### Rationale

- A wins unless the maintainer accepts unbounded destination cardinality; the keys come from `MessagingSemanticConventions` (#177).

**Class:** Dilemma

</details>

<details>
<summary><strong>5. Cost, time, errors and the tenant tag</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `HasListeners()` for spans, `Instrument.Enabled` for metrics, `TimeProvider` injected into `TransportTelemetry`, `error.type` is the `EncinaError` code, publishers return the `Either` unchanged, tenant from the helper of #2676** | Zero cost without a listener; no behavior change | One constructor argument per publisher static |
| **B) Always measure** | Simpler | Pays per message |

### Chosen Option: **A - Guarded and pass-through**

### Rationale

- ADR-054 settled points 2 and 7 and D4; `AGENTS.md` section 3 (a `Left` is never reported as success).

**Class:** Trivial
**Settled by:** ADR-054 settled points 2 and 7 and D4; `AGENTS.md` section 3

</details>

### Register of defaults and semantics

| Id | Choice | Class | Basis |
|----|--------|-------|-------|
| C1 | No new `EncinaError` code (publisher results pass through), so no ADR-062 category here; no logging and no EventId: the block 6380-6399 is unused (a malformed incoming trace context is logged by the API of #132) | Settled | ADR-062; `AGENTS.md` section 7 |
| C2 | A telemetry failure never changes a publish result or a delivery: spans, metrics and propagation are best effort and never throw | Settled | `AGENTS.md` section 3 |
| C3 | The consumer relation is fixed per operation (`Send` child, `Publish`, scheduled and batch link), identical on all transports, no switch | Settled | ADR-054 D2 |
| C4 | No tenant-qualified id is built; the tenant is a tag only; with multi-tenancy off no tenant tag | Settled | ADR-054 D4 |
| C5 | Body size is emitted only where the publisher already holds the serialized bytes (no re-serialization for telemetry) | Dilemma (recommended, pending the maintainer) | cost on the hot path against #177's `messaging.message.body.size` |
| C6 | gRPC and GraphQL get child spans but no `messaging.client.consumed.messages` count: a server request is not a consumed message, and the two packages are server bridges, not publishers; the alternative is a count tagged `messaging.system` = `grpc` or `graphql` | Dilemma (recommended, pending the maintainer) | the scenario that flips it: operators who want one request-rate panel across all ten transports and accept a semantic stretch of the OpenTelemetry metric |

---

## Implementation Phases

### Phase 1: Core `TransportTelemetry` (pull request T1, after #177 and #132)

<details>
<summary><strong>Tasks</strong></summary>

1. **Create** `src/Encina/Diagnostics/TransportTelemetry.cs` (`public sealed class`): constructor `(string packageName, string messagingSystem, TimeProvider? timeProvider = null)` creating `ActivitySource(packageName, "1.0")` and `Meter(packageName, "1.0")` with the three instruments; `Activity? StartPublish(string operation, string? destination)`, `Activity? StartConsume(...)` (applies the D2 rule through `TraceContextConsumer` of #132), `StartMeasurement()`, `Complete<T>(Activity?, in Measurement, Either<EncinaError, T>)`, `Failed(Activity?, in Measurement, Exception)` and `Inject(Activity?, setter)`. Everything is a no-op when nothing listens.
   `Measurement` is a public `readonly struct` (nested in `TransportTelemetry`, an opaque timestamp holder with no public members besides the implicit default constructor), because it appears in the public signatures of `Complete` and `Failed`.
2. **Modify** `src/Encina.OpenTelemetry/ServiceCollectionExtensions.cs`: subscribe the ten package sources and meters in `WithEncina` (until #1787 subscribes by wildcard); edit `tests/Encina.UnitTests/OpenTelemetry/ServiceCollectionExtensionsTests.cs` so a `WithEncina` test asserts the ten sources and ten meters are subscribed (otherwise the new lines are uncovered for the crap-gate).
3. `src/Encina/PublicAPI.Unshipped.txt` (about 12 lines: the class, its constructor, six methods, the nested `Measurement` struct and its default constructor), `.github/coverage-manifest/Encina.json`, `.github/coverage-manifest/Encina.OpenTelemetry.json` and `changelog.d/725-transport-telemetry.added.md` through `mechanical-fixer`.
4. Tests: new `tests/Encina.UnitTests/Diagnostics/TransportTelemetryTests.cs` (listeners see the span kind, tags, the three instruments, error type; nothing happens without a listener; a fake `TimeProvider` gives exact durations; no body, id or message text in any tag); new `tests/Encina.GuardTests/Diagnostics/TransportTelemetryGuardTests.cs` (null or whitespace package name and system; null `timeProvider` valid; a `default(Measurement)` passed to `Complete` and `Failed` is a valid-result case, which guards the members of the public struct); new `tests/Encina.ContractTests/Diagnostics/TransportTelemetryContractTests.cs` (instrument names equal the constants of #177 for every `messaging.system` value).

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 1</strong></summary>

```text
CONTEXT:
Encina (.NET 10, C# 14, pre-1.0). Issue #725, plan docs/plans/transport-opentelemetry-implementation-plan-725.md, phase 1.
#177 merged (MessagingSemanticConventions in core) and #132 merged (IContextPropagator, TraceContextConsumer). The ten transports
have no shared interface; Redis.PubSub and InMemory reference only Encina, so the helper is public in core.

TASK:
1. Create TransportTelemetry (public sealed, Encina.Diagnostics) owning a package ActivitySource and Meter, the instruments
   messaging.client.operation.duration (s), messaging.client.sent.messages, messaging.client.consumed.messages, span start and
   completion helpers and propagation injection.
2. Subscribe the ten package sources and meters in WithEncina.
3. Unit, guard and contract tests; PublicAPI, manifests and changelog through mechanical-fixer.

KEY RULES:
- Zero cost without a listener; time through TimeProvider (optional last parameter, valid-result test for null).
- Never emit message ids, bodies, headers, keys, EncinaError.Message or exception messages; never throw from telemetry.

REFERENCE FILES:
- the plans of #177 (messaging semantic conventions) and #132 (trace context propagation)
- docs/architecture/adr/054-observability-conventions-and-trace-propagation.md
```

</details>

---

### Phase 2: Brokered Publishers, Group 1 (pull request T2)

<details>
<summary><strong>Tasks</strong></summary>

1. **Modify** `RabbitMQMessagePublisher.cs`, `KafkaMessagePublisher.cs`, `NATSMessagePublisher.cs` and `AzureServiceBusMessagePublisher.cs`: a `private static readonly TransportTelemetry` (`Encina.RabbitMQ`, `Encina.Kafka`, `Encina.NATS`, `Encina.AzureServiceBus`); every public publish method starts the span, injects the context into the native carrier (`BasicProperties.Headers`, Kafka `Headers`, NATS headers, `ApplicationProperties`), measures and completes with the unchanged `Either`; `ProduceBatchAsync` and Service Bus scheduling set `messaging.batch.message_count`.
2. Tests: new `<Transport>PublisherTelemetryTests.cs` under `tests/Encina.UnitTests/Messaging/<Transport>/` for each (with the existing mocking of the client: the span, the injected header and the instruments; a failing client gives `error.type` and the `Left` is returned). RabbitMQ, Kafka and Service Bus have no `InternalsVisibleTo`, so tests drive the public publisher only. The four manifests (`Encina.RabbitMQ.json`, `Encina.Kafka.json`, `Encina.NATS.json`, `Encina.AzureServiceBus.json`) and the changelog through `mechanical-fixer`.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 2</strong></summary>

```text
CONTEXT:
Issue #725 plan docs/plans/transport-opentelemetry-implementation-plan-725.md, phase 2. TransportTelemetry exists in core.

TASK:
1. Wrap the publish operations of the RabbitMQ, Kafka, NATS and Azure Service Bus publishers with TransportTelemetry and inject
   traceparent and tracestate into each native carrier.
2. One unit test file per transport; manifests and changelog through mechanical-fixer.

KEY RULES:
- Return the same Either as before; never throw from telemetry; no message id, body, header values or per-tenant names in tags.
- RabbitMQ, Kafka, Azure Service Bus have no InternalsVisibleTo: test only through public types.

REFERENCE FILES:
- src/Encina.RabbitMQ/RabbitMQMessagePublisher.cs, src/Encina.Kafka/KafkaMessagePublisher.cs, src/Encina.NATS/NATSMessagePublisher.cs
- src/Encina.AzureServiceBus/AzureServiceBusMessagePublisher.cs
```

</details>

---

### Phase 3: Group 2 and the Consumers That Exist (pull request T3)

<details>
<summary><strong>Tasks</strong></summary>

1. **Modify** `AmazonSQSMessagePublisher.cs` (message attributes), `MQTTMessagePublisher.cs` (user property; consumer span in `MqttSubscription<T>.OnMessageReceived` and `MqttPatternSubscription<T>.OnMessageReceived`), `RedisPubSubMessagePublisher.cs` (two optional fields in `RedisMessageWrapper<TMessage>`; consumer span in the subscription delivery) and `InMemoryMessageBus.cs` (publish span, consumer span per subscriber delivery, context carried with the queued item).
2. Tests: one new telemetry test file per transport under `tests/Encina.UnitTests/Messaging/<Transport>/` (SQS, MQTT and Redis.PubSub have `InternalsVisibleTo` for UnitTests; InMemory has none, so its test uses the public bus); a `Send`-style delivery is a child span and a `Publish` delivery carries an `ActivityLink` (ADR-054 D2). Four manifests and the changelog through `mechanical-fixer`.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 3</strong></summary>

```text
CONTEXT:
Issue #725 plan docs/plans/transport-opentelemetry-implementation-plan-725.md, phase 3. Phases 1 and 2 merged.

TASK:
1. Instrument SQS, MQTT, Redis.PubSub and InMemory publishers and the deliveries of MQTT, Redis.PubSub and InMemory subscribers.
2. Add the traceparent and tracestate fields to RedisMessageWrapper; keep them optional.
3. Tests for span relation (child for Send, link for Publish), carrier injection and instruments.

KEY RULES:
- Telemetry never changes delivery or publish results; handler exceptions propagate as before.
- No message text, ids, channels or topics with ids in tags.

REFERENCE FILES:
- src/Encina.MQTT/MQTTMessagePublisher.cs, src/Encina.Redis.PubSub/RedisPubSubMessagePublisher.cs, src/Encina.InMemory/InMemoryMessageBus.cs
```

</details>

---

### Phase 4: gRPC, GraphQL and the Coherence Test (pull request T4)

<details>
<summary><strong>Tasks</strong></summary>

1. **Modify** `src/Encina.gRPC/GrpcMediatorService.cs` (class `GrpcEncinaService`) and `src/Encina.GraphQL/GraphQLMediatorBridge.cs`: a child span per `SendAsync`, `PublishAsync`, `StreamAsync` and query, mutation and subscription, as a child of the ambient activity (no header extraction: ASP.NET Core instrumentation does it); no `messaging.client.consumed.messages` count (register row C6), only the spans.
2. **Modify** `tests/Encina.ContractTests/Encina.ContractTests.csproj`: add ProjectReferences to `Encina.RabbitMQ`, `Encina.Kafka`, `Encina.NATS`, `Encina.MQTT`, `Encina.InMemory`, `Encina.gRPC` and `Encina.GraphQL` (the project already references `Encina.AmazonSQS`, `Encina.AzureServiceBus` and `Encina.Redis.PubSub`); counted in T4.
3. Tests: new `GrpcTelemetryTests.cs` and `GraphQLTelemetryTests.cs` (public service only; GraphQL has UnitTests IVT); new `tests/Encina.ContractTests/Messaging/TransportTelemetryCoherenceTests.cs` (every publisher transport's publish emits the same instrument names, the `messaging.system` value set and `error.type` rules; gRPC and GraphQL are checked for the span only); manifests and changelog through `mechanical-fixer`.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 4</strong></summary>

```text
CONTEXT:
Issue #725 plan docs/plans/transport-opentelemetry-implementation-plan-725.md, phase 4. Phases 1-3 merged.

TASK:
1. Child spans (no message count, register row C6) in the gRPC service and the GraphQL bridge.
2. A contract test across the transports for instrument names and tag keys; add the seven missing ProjectReferences to the
   ContractTests csproj.

KEY RULES:
- No trace header extraction in these two packages (the host does it); never emit query text, variables or payloads.

REFERENCE FILES:
- src/Encina.gRPC/GrpcMediatorService.cs, src/Encina.GraphQL/GraphQLMediatorBridge.cs
```

</details>

---

### Phase 5: Tenant Tag and Broker Proof (pull request T5, tenant part after #2676)

<details>
<summary><strong>Tasks</strong></summary>

1. **Modify** `TransportTelemetry.cs`: the tenant tag through the helper of #2676 on spans and instruments, only when multi-tenancy is on.
2. **Create** integration tests with the existing fixtures (`RabbitMqFixture`, `KafkaFixture`, `NatsFixture`, `MqttFixture`, `RedisFixture` in `tests/Encina.TestInfrastructure/Fixtures/`, each in its shared collection): `tests/Encina.IntegrationTests/Observability/Transports/RabbitMqTelemetryIntegrationTests.cs`, `KafkaTelemetryIntegrationTests.cs`, `MqttTelemetryIntegrationTests.cs` and `RedisPubSubTelemetryIntegrationTests.cs`: a real publish carries `traceparent` in the broker message and the span and instruments are emitted; MQTT and Redis.PubSub also prove the consumer span as a child. The emulator-based transports (Service Bus, SQS) and NATS are proved by the unit tests of T2 and T3 until their fixtures are used in this folder.
3. Edit `TransportTelemetryTests.cs` for tenant on, off and blank; manifest and changelog through `mechanical-fixer`; README section through `docs-writer`.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 5</strong></summary>

```text
CONTEXT:
Issue #725 plan docs/plans/transport-opentelemetry-implementation-plan-725.md, phase 5. #2676 merged.

TASK:
1. Tenant tag in TransportTelemetry. 2. Integration tests on real RabbitMQ, Kafka, MQTT and Redis with the shared fixtures.

KEY RULES:
- Shared collection fixtures only; no Thread.Sleep; assert the broker message really carries traceparent.

REFERENCE FILES:
- tests/Encina.TestInfrastructure/Fixtures/RabbitMqFixture.cs, KafkaFixture.cs, MqttFixture.cs, RedisFixture.cs
```

</details>

---

## Research

### Relevant Standards & Specifications

| Source | Provision | Use in this feature |
|--------|-----------|---------------------|
| ADR-054 | D2, D3, D4, D5, settled points 2, 3, 5, 7 | Rules and scope |
| W3C Trace Context | `traceparent`, `tracestate` | Wire format |
| OpenTelemetry messaging conventions | Spans, `messaging.client.operation.duration`, `.sent.messages`, `.consumed.messages` | Instruments and attributes |
| `AGENTS.md` section 5 | Transport category consistency | All 10 |

### Existing Encina Infrastructure to Leverage

| Component | Location | Usage in this feature |
|-----------|----------|-----------------------|
| `MessagingSemanticConventions`, `IContextPropagator`, `TraceContextConsumer` | #177 and #132 (not on main) | Keys and propagation |
| Publishers and subscriptions | `src/Encina.<Transport>/` | Instrumented |
| Broker fixtures | `tests/Encina.TestInfrastructure/Fixtures/` | T5 |
| `WithEncina()` | `src/Encina.OpenTelemetry/ServiceCollectionExtensions.cs` | Subscription |

### Event ID Allocation

| Package | Range | Notes |
|---------|-------|-------|
| none | none | No logging code (register row C1); transports keep their existing ranges |

### Estimated File Count

| PR | Content | New src | Changed src | New tests | Edited tests | Manifest | Other | Files | Lines (est.) |
|----|---------|---------|-------------|-----------|--------------|----------|-------|-------|--------------|
| T1 | Phase 1 | 1 | 2 (`ServiceCollectionExtensions.cs` of OpenTelemetry, PublicAPI) | 3 | 1 (OpenTelemetry `ServiceCollectionExtensionsTests.cs`) | 2 | 1 (changelog) | 10 | ~650 |
| T2 | Phase 2 | 0 | 4 | 4 | 0 | 4 | 1 | 13 | ~800 |
| T3 | Phase 3 | 0 | 4 | 4 | 0 | 4 | 1 | 13 | ~850 |
| T4 | Phase 4 | 0 | 2 | 3 | 0 | 2 | 2 (changelog, ContractTests csproj) | 9 | ~500 |
| T5 | Phase 5 | 0 | 1 | 4 | 1 | 1 | 2 (changelog, README) | 9 | ~620 |
| **Total** | | **1** | **13** | **18** | **2** | **13** | **7** | **54** | **~3,420** |

Columns sum: 1 + 13 + 18 + 2 + 13 + 7 = 54 = 10 + 13 + 13 + 9 + 9. No interface changes: the publisher interfaces are untouched, so no fake or test-double changes (`new XPublisher(` call sites keep their constructors). `PublicAPI.Unshipped.txt` changes only for the core class and its nested public `Measurement` struct (T1, about 12 lines); the `RedisMessageWrapper` fields are internal to the package.

Per-file coverage targets:

| File | Unit | Guard | Contract | Property | Integration | Justification |
|------|------|-------|----------|----------|-------------|---------------|
| `Diagnostics/TransportTelemetry.cs` (core) | 92 | 60 | 30 | 0 | 30 | Span, metric, error and no-listener paths are listener-driven; guard covers constructor arguments with valid-result null time provider; contract pins instrument names; integration runs it on four real brokers |
| `RabbitMQMessagePublisher.cs` | 95 | 20 | 0 | 0 | 0 | Existing targets kept (read from `Encina.RabbitMQ.json`); the new span wrapper is driven by the new telemetry unit tests on every publish method |
| `KafkaMessagePublisher.cs` | 95 | 15 | 0 | 0 | 0 | Existing targets kept; produce and batch paths are driven against a substitute producer |
| `NATSMessagePublisher.cs` | 95 | 15 | 0 | 0 | 30 | Existing targets kept; publish, request and JetStream paths are unit-driven, JetStream deduplication keeps its integration test |
| `AzureServiceBusMessagePublisher.cs` | 90 | 15 | 90 | 0 | 90 | Existing targets kept; send, schedule and cancel are driven by the unit tests and the emulator tests |
| `AmazonSQSMessagePublisher.cs` | 95 | 10 | 75 | 0 | 85 | Existing targets kept; the message-attribute injection is driven by the SQS unit tests |
| `MQTTMessagePublisher.cs` | 85 | 10 | 0 | 0 | 0 | Existing targets kept; the publish wrapper and the delivery span are unit-driven (the subscription wrappers need a real client, as the manifest says) |
| `RedisPubSubMessagePublisher.cs` | 70 | 20 | 0 | 0 | 85 | Existing targets kept; publish is unit-driven, the delivery span lines run in the Redis integration tests (T5) |
| `InMemoryMessageBus.cs` (the manifest entry today says "Interface, no implementation" with no target; T3 replaces it) | 80 | 0 | 0 | 0 | 0 | New per-file target: publish and delivery spans are driven by the telemetry unit tests; guard 0 because `Encina.GuardTests` has no ProjectReference to `Encina.InMemory` |
| `GrpcMediatorService.cs` | 80 | 15 | 0 | 0 | 0 | No per-file entry today (package targets unit 55, guard 15); new unit target 80 because the child-span lines are driven by the new unit test, guard kept at the package value |
| `GraphQLMediatorBridge.cs` | 80 | 15 | 0 | 0 | 0 | Same as gRPC: package targets unit 55, guard 15 today; the span lines are driven by the new unit test |

---

## Combined AI Agent Prompts

<details>
<summary><strong>Full combined prompt for all phases</strong></summary>

```text
You are implementing issue #725 - OpenTelemetry for the ten message transports - in five pull requests, after #177 and #132.

PROJECT CONTEXT:
- Encina .NET 10 / C# 14, pre-1.0, no compatibility layers. Edit with Edit/Write only; PublicAPI, manifests and changelog through
  mechanical-fixer; README through docs-writer.
- There is no shared transport interface; the helper is public in core because Redis.PubSub and InMemory reference only Encina.
- ADR-054: W3C only, no baggage, D2 child-or-link rule, names from the constants class of #177, zero cost without a listener.

IMPLEMENTATION OVERVIEW:
1. T1: TransportTelemetry (core). 2. T2: RabbitMQ, Kafka, NATS, Service Bus. 3. T3: SQS, MQTT, Redis.PubSub, InMemory plus the
   deliveries that exist. 4. T4: gRPC, GraphQL and the coherence contract test. 5. T5: tenant tag and broker integration proof.

KEY PATTERNS:
- A private static readonly TransportTelemetry per publisher; return the same Either; never throw from telemetry.
- Never emit ids, bodies, header values, query text or per-tenant names. Acknowledge, reject and queue depth are out of scope (#1609).

REFERENCE FILES:
- src/Encina.<Transport>/ publishers, and the plans of #177, #132 and #2385
```

</details>

---

## Cross-Cutting Integration Matrix

| # | Function | Status | Notes |
|---|----------|--------|-------|
| 1 | Caching | ❌ N/A | Instrumentation observes messaging; it adds no cached reads |
| 2 | OpenTelemetry | ✅ Phases 1-5 | This is the feature for message transports and W3C propagation |
| 3 | Structured Logging | ❌ N/A | A malformed incoming trace context is logged by the API of #132, not here (register row C1) |
| 4 | Health Checks | ❌ N/A | Observes operations; provides no health status |
| 5 | Validation | ❌ N/A | No input |
| 6 | Resilience | ❌ N/A | Spans observe the publish call; retries stay in the transports and the outbox |
| 7 | Distributed Locks | ❌ N/A | Acquires no lock |
| 8 | Transactions | ❌ N/A | Never commits or rolls back |
| 9 | Idempotency | ❌ N/A | A redelivered message gets a new consumer span linked to the producer |
| 10 | Multi-Tenancy | ✅ Phase 5 | Tenant tag only when multi-tenancy is on, through the helper of #2676 (ADR-054 D4) |
| 11 | Module Isolation | ❌ N/A | No module data |
| 12 | Audit Trail | ❌ N/A | No payload or personal data is emitted |

---

## Prerequisites & Dependencies

### Required Prerequisites

| Prerequisite | Status | Notes |
|-------------|--------|-------|
| #177 constants class | Open (plan merged) | T1 uses its keys |
| #132 propagation API | Open (plan merged) | T1 uses `IContextPropagator` and `TraceContextConsumer` |
| #2676 tenant helper | Open | Only T5's tenant part waits |

### Recommended (Not Blocking)

| Dependency | Issue | Notes |
|-----------|-------|-------|
| Outbox transport adapters | #2385 | Wrap the instrumented publishers; no change needed |
| Consumers for the five publish-only transports | #1609 | Call `StartConsume` in their own plans |
| Envelope with trace context | #1164 | Stores carry the same two strings; unrelated to the wire |
| Subscription by wildcard | #1787 | Explicit `AddSource` and `AddMeter` meanwhile |
| Planned transports (6) | their own plans | Adopt `TransportTelemetry` |

---

## Next Steps

1. Link this plan from #725 and ask the maintainer to answer Design Choices 1 to 4 and register rows C5 and C6 when work starts.
2. Implement T1 to T4; T5 after #2676. Order in the family: #721, #722, #723, #724, #725 (this one last: it waits for #177 and #132).
3. Maintainer action: amend the acceptance criteria of #725 that name Acknowledge, Reject, queue depth and "unit tests per transport" for the five publish-only transports to the outcome of Design Choice 2 (this plan never comments on an issue).
