# Implementation Plan: Priority for Message Transports (publish-side priority across the transport category)

> **Issue**: [#251](https://github.com/dlrivada/Encina/issues/251)
> **Type**: Feature
> **Complexity**: High (9 phases, 8 existing transports plus the reference InMemory bus, about 70 files, delivered in 7 pull requests)
> **Estimated Scope**: about 1,100 lines of production code + about 1,800 lines of tests
> **Family**: message priority and partitioning (#123, #251, #301, #469, #1254). The shared priority design (type, attribute, interface, resolver) is OWNED by #123; this plan applies it to the transport category and decides only what is specific to transports. It starts after #123 merges and is independent of #301, #469 and #1254.

---

## Summary

A message that carries a `MessagePriority` keeps it when it leaves Encina for a broker: natively where the broker has message priority, by routing to a per-priority destination (queue, topic, subject, channel) where it does not, and always as message metadata. A transport never silently ignores the priority (AGENTS.md section 5, Transports row): an unsupported or unmapped priority returns a `Left`.

Facts verified in today's code that reshape the issue:

- **There is no shared transport abstraction.** `IMessageTransport` (named in AGENTS.md) does not exist in `src/`. Each transport has its own publisher interface: `IRabbitMQMessagePublisher`, `IKafkaMessagePublisher`, `IAmazonSQSMessagePublisher`, `IAzureServiceBusMessagePublisher`, `INATSMessagePublisher`, `IRedisPubSubMessagePublisher`, `IMQTTMessagePublisher`, and `IInMemoryMessageBus`. `IPriorityQueueManager` as drawn in the issue would be a second queueing abstraction beside all of them (choice 3).
- **The transports only publish.** The packages have 6 to 10 source files each, with publishers, options, a `Log.cs` and DI registration, and no consumer or subscription loop (only `InMemoryMessageBus.Subscribe`). "Consumed in priority order" is therefore the application's consumer unless Encina adds one (choice 4).
- Publishers already take an optional destination: `RabbitMQMessagePublisher` a `routingKey` and `CreateProperties(messageId)` building `BasicProperties`; `KafkaMessagePublisher.ProduceAsync` a `topic`; `AmazonSQSMessagePublisher` a `queueUrl`; `AzureServiceBusMessagePublisher` builds a `ServiceBusMessage` with `Subject`. This is where priority routing and metadata attach.
- The transports reference `Encina` and `Encina.Messaging`, except `Encina.InMemory`, which references only `Encina` (a new project reference is needed there, or the shared type moves to core; see #123 choice 1).
- `PriorityLevels` and `StarvationTimeout` from the issue's configuration are not needed: the levels come from the shared enum, and no Encina code consumes from a broker.
- The "Native" cells of the issue's table were unverified. This plan marks each broker claim as "to confirm against vendor documentation at implementation".

- **Affected packages**: `Encina.Messaging` (shared routing helper), `Encina.RabbitMQ`, `Encina.Kafka`, `Encina.AmazonSQS`, `Encina.AzureServiceBus`, `Encina.NATS`, `Encina.Redis.PubSub`, `Encina.MQTT`, `Encina.InMemory`.
- **Provider category**: Transports (10 existing, 6 planned; AGENTS.md section 5). `Encina.gRPC` and `Encina.GraphQL` are adapters whose scope is the open spike #3400 and are NOT planned here (see Prerequisites). The six planned 1.0 transports (GoogleCloudPubSub, AmazonEventBridge, Pulsar, Redis.Streams, ActiveMQ, Dapr) add this capability when they land.

---

## Design Choices

<details>
<summary><strong>1. Shared priority type with the outbox and scheduled stores (Dilemma D1, answered once in #123)</strong></summary>

### Options Considered

| Option | Pros | Cons |
| --- | --- | --- |
| **A) The `MessagePriority` of #123 (`Background..Critical`, higher is more urgent) for transports too** | A priority set on a command survives outbox, scheduler and broker; one attribute and one resolver | The five levels must suit brokers; RabbitMQ recommends few levels (the five fit) |
| **B) A transport-only enum** | Independent of the stores | A mapping at every boundary between the outbox row and the publisher |

### Chosen Option: **A — the shared `MessagePriority` of #123** (recommended, pending the maintainer)

### Rationale

- Option A wins when the outbox publishes to the transport, which is the normal path (the processor rehydrates the notification and the publisher reads its priority again from the instance).
- Option B wins only if brokers need more or fewer levels than the stores; the five-level enum already matches the `PriorityLevels = 5` of the issue.
- This choice is the same question as #123 choice 1 and takes the same answer.

**Class:** Dilemma

</details>

<details>
<summary><strong>2. What a transport without native priority does (Dilemma D2)</strong></summary>

### Options Considered

| Option | Pros | Cons |
| --- | --- | --- |
| **A) Route to one destination per priority, from an explicit mapping; metadata is always set; unmapped means `Left`** | Works on every broker; no silent loss of priority; consumers choose high first | More topology to provision and monitor; the mapping is configuration |
| **B) Refuse at start-up with a clear `Left` (priority unavailable on that broker)** | No hidden topology | Priority is unusable on Kafka, SQS, NATS, Redis and MQTT |
| **C) Apply priority only in the outbox before publishing, never at the broker** | One implementation for all transports | Once messages are in the broker nothing orders them; direct (non-outbox) publishers get nothing |

### Chosen Option: **A — per-priority destinations from an explicit mapping, fail closed** (recommended, pending the maintainer)

### Rationale

- Option A wins on Kafka and SQS, where the issue itself says "separate topics or queues by priority", and keeps the category consistent as AGENTS.md section 5 requires.
- Option B wins when topology provisioning is owned by operations; Option C wins when the outbox is the only producer. Both stay possible: an unmapped priority returns a `Left`, and `TransportPriorityOptions.Enabled = false` (the default) leaves publishing exactly as today.
- Destinations are never derived by string surgery on an SQS queue URL or a Kafka cluster topic; the mapping is configuration, validated at start-up.

**Class:** Dilemma

</details>

<details>
<summary><strong>3. Is a separate `IPriorityQueueManager` needed (Dilemma D3)</strong></summary>

### Options Considered

| Option | Pros | Cons |
| --- | --- | --- |
| **A) Yes, as proposed (`EnqueueAsync` / `DequeueAsync`)** | An explicit API for code that enqueues directly | A second queueing abstraction beside eight publisher interfaces; a `DequeueAsync` needs a consumer that no transport package has |
| **B) No: each publisher resolves the priority from the message with `MessagePriorityResolver` (#123) and applies it through a shared `TransportPriorityRouter`** | No new public abstraction; existing publisher signatures unchanged; the rehydrated outbox notification keeps its priority | Priority is implicit (attribute or interface), with an optional explicit argument only if needed |

### Chosen Option: **B — priority as publish metadata, no queue manager** (recommended, pending the maintainer)

### Rationale

- Option B wins when all publishing goes through the existing publisher interfaces, which is the code today.
- Option A wins if consumers outside the outbox must enqueue by priority directly and Encina also provides the consuming side; that is the consumer helper of choice 4, option B.

**Class:** Dilemma

</details>

<details>
<summary><strong>4. Starvation prevention at the broker (Dilemma D4, linked to #123 choice 3)</strong></summary>

### Options Considered

| Option | Pros | Cons |
| --- | --- | --- |
| **A) Not built here: Encina does not consume from brokers; the outbox side is covered by the aged-first rule of #123; documentation tells consumers of per-priority destinations how to avoid starvation** | No new consumer abstraction; scope matches the code | A consumer of per-priority queues can starve low priority if it ignores the guidance |
| **B) A consumer helper in `Encina.Messaging` that drains per-priority destinations with age-based promotion (the same rule as #123)** | One tested policy for every transport | A new consumer abstraction across 8 transports, which does not exist today; larger than this issue |
| **C) Strict priority, no prevention** | Simplest | Low priority can wait forever |

### Chosen Option: **A — not built here; document and revisit with a consumer abstraction** (recommended, pending the maintainer)

### Rationale

- Option A wins while transports are publisher-only (verified above).
- Option B wins when Encina adds consumption to the transport category; it is then a separate feature with its own plan.
- The InMemory reference bus does have delivery inside Encina; it applies the same aged-first rule there (Phase 2), which gives the tested reference policy.

**Class:** Dilemma

</details>

<details>
<summary><strong>5. Consistent behavior across the category and opt-in</strong></summary>

### Options Considered

| Option | Pros | Cons |
| --- | --- | --- |
| **A) Every transport implements the same contract: priority metadata always when enabled, native priority or per-priority destination per capability, `Left` for unsupported or unmapped; `TransportPriorityOptions.Enabled = false` by default on every transport** | Same behavior and test on all of them; nothing changes unless enabled | A capability table must be maintained per transport |
| **B) Only RabbitMQ and Azure Service Bus now** | Smaller | Violates the category rule and leaves Kafka and SQS (where the issue says "separate topics") out |

### Chosen Option: **A — one contract for all transports, off by default**

### Rationale

- A feature touching transports must be consistent across the category; every messaging capability is optional and disabled by default.
- The shared `TransportPriorityRouter` (in `Encina.Messaging`) takes the resolved priority, the transport's capability (`Native`, `Destination`, `MetadataOnly`) and the mapping, and returns the destination and metadata or a `Left`. Transports differ only in how they attach them.

**Class:** Settled
**Settled by:** AGENTS.md section 5 (Transports row: a feature touching them must be consistent across every provider of the category; Send/Publish, metadata propagation) and section 3 (every messaging pattern optional and disabled by default; ADR-050).

</details>

<details>
<summary><strong>6. Configuration shape: one options object per transport, no level count</strong></summary>

### Options Considered

| Option | Pros | Cons |
| --- | --- | --- |
| **A) `TransportPriorityOptions` (`Enabled`, `Destinations`) as a property `Priority` of each transport options class (`EncinaRabbitMQOptions.Priority`, and so on), validated at start-up** | Same shape everywhere; options live with the transport as they do today | One property added to 8 options classes |
| **B) One global `AddEncinaMessaging(options => options.UsePriorityQueues ...)` as the issue sketches** | One switch | `AddEncinaMessaging` with that shape does not exist; messaging options are per pattern and transport options are per transport |

### Chosen Option: **A — a `Priority` options object per transport**

### Rationale

- Transport options classes are the existing configuration surface (`EncinaRabbitMQOptions`, `EncinaKafkaOptions`, ...).
- `PriorityLevels` is dropped (derived from the enum) and `StarvationTimeout` is dropped with choice 4.

**Class:** Trivial
**Settled by:** existing per-transport options classes such as `src/Encina.RabbitMQ/EncinaRabbitMQOptions.cs` and `src/Encina.AmazonSQS/EncinaAmazonSQSOptionsValidator.cs`, and AGENTS.md section 3 (options validated at start-up).

</details>

---

## Implementation Phases

Seven pull requests of at most 15 files and 1500 lines: PR-A phase 1, PR-B phase 2 (InMemory), PR-C RabbitMQ, PR-D Kafka and NATS, PR-E SQS and Azure Service Bus, PR-F Redis.PubSub and MQTT, PR-G observability follow-ups, docs and ADR. Integration tests ship with each transport PR.

### Phase 1: Shared Routing Core in `Encina.Messaging`

<details>
<summary><strong>Tasks</strong></summary>

New folder `src/Encina.Messaging/Transport/`:

1. `TransportPriorityOptions.cs`: `Enabled` (false), `Destinations` (`IDictionary<MessagePriority, string>`), validation (every level mapped when the transport has `Destination` capability).
2. `TransportPriorityCapability.cs`: `Native`, `Destination`, `MetadataOnly`.
3. `TransportPriorityRouter.cs`: `Either<EncinaError, TransportPriorityRoute> Route(object message, TransportPriorityCapability capability, TransportPriorityOptions options, string defaultDestination)`; uses `MessagePriorityResolver` (#123); error codes `transport.priority_unmapped` and `transport.priority_unsupported`.
4. `TransportPriorityMetrics.cs`: `encina.transport.priority.published` (counter, tags `transport`, `priority`) on the shared `"Encina"` meter.
5. `PublicAPI.Unshipped.txt` through `mechanical-fixer`.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 1</strong></summary>

```text
CONTEXT: Plan docs/plans/transport-priority-implementation-plan-251.md. #123 merged MessagePriority and MessagePriorityResolver in src/Encina.Messaging/Priority. There is no IMessageTransport in the code: every transport has its own publisher interface and options class.
TASK: Implement Phase 1 Tasks 1 to 4 as a transport-agnostic helper that returns the destination and metadata for a message, or a Left.
KEY RULES: Either<EncinaError, T> (ADR-001, ADR-006); never silently ignore a priority; no provider-specific code in Encina.Messaging (AGENTS.md section 3, provider coherence); XML docs; metric tags carry the transport name and priority only, never message content.
REFERENCE FILES: src/Encina.Messaging/Priority/MessagePriorityResolver.cs (from #123), src/Encina.AmazonSQS/EncinaAmazonSQSOptionsValidator.cs, src/Encina.Messaging/Diagnostics/OutboxProcessorMetrics.cs.
```

</details>

### Phase 2: `Encina.InMemory` Reference Behavior

<details>
<summary><strong>Tasks</strong></summary>

1. `src/Encina.InMemory/Encina.InMemory.csproj`: project reference to `Encina.Messaging` (or core placement of the enum, per #123 choice 1).
2. `src/Encina.InMemory/EncinaInMemoryOptions.cs`: `Priority` options; `InMemoryMessageBus`: when enabled, `PublishAsync` places the message on a priority queue drained by a background pump in aged-first order, `TimeProvider` for the age; when disabled, delivery is exactly as today.
3. `Log.cs` events inside the existing `InMemory` range 4450-4499.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 2</strong></summary>

```text
CONTEXT: InMemoryMessageBus dispatches to subscribers directly (PublishAsync, Subscribe). It is the reference implementation the contract tests of the category compare against.
TASK: Implement Phase 2 Tasks 1 to 3. The priority-ordered pump is opt-in.
KEY RULES: time from TimeProvider; the pump catches per iteration and never lets an exception escape; a Left from a handler is not swallowed; EventIds inside the existing InMemory range 4450-4499, packed.
REFERENCE FILES: src/Encina.InMemory/InMemoryMessageBus.cs, EncinaInMemoryOptions.cs, Log.cs, src/Encina/Diagnostics/EventIdRanges.cs.
```

</details>

### Phase 3: RabbitMQ (native priority)

<details>
<summary><strong>Tasks</strong></summary>

1. `EncinaRabbitMQOptions.Priority` (capability `Native`).
2. `RabbitMQMessagePublisher.CreateProperties`: set `BasicProperties.Priority` from the route (value 0 to 4) and the metadata header `encina-priority`.
3. A small helper that returns the queue argument `x-max-priority = 4` for applications that declare their queues; the publisher declares no queues today (queue declaration is the consumer's).
4. Log events in the existing `RabbitMQ` range 4100-4149; vendor behavior (a queue needs `x-max-priority`; larger number delivered first) confirmed against RabbitMQ documentation at implementation.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 3</strong></summary>

```text
CONTEXT: RabbitMQMessagePublisher builds BasicProperties in CreateProperties(messageId) and publishes to the exchange in EncinaRabbitMQOptions.ExchangeName with a routing key defaulting to the message type name.
TASK: Implement Phase 3 Tasks 1 to 4 using TransportPriorityRouter (Phase 1).
KEY RULES: async with CancellationToken; password-bearing options keep [JsonIgnore] and ToString (AGENTS.md section 3); the publisher returns Left, never throws, for an unmapped priority; metadata only when enabled.
REFERENCE FILES: src/Encina.RabbitMQ/RabbitMQMessagePublisher.cs, EncinaRabbitMQOptions.cs, Log.cs.
```

</details>

### Phase 4: Kafka and NATS (destination per priority)

<details>
<summary><strong>Tasks</strong></summary>

1. `EncinaKafkaOptions.Priority` and `KafkaMessagePublisher.ProduceAsync`: topic from the mapping, record header `encina-priority`.
2. `EncinaNATSOptions.Priority` and `NATSMessagePublisher`: subject from the mapping, header `Encina-Priority`.
3. Log events in the existing `Kafka` (4150-4199) and `NATS` (4200-4249) ranges.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 4</strong></summary>

```text
CONTEXT: KafkaMessagePublisher.ProduceAsync takes an optional topic (default EncinaKafkaOptions.DefaultEventTopic); NATSMessagePublisher publishes to a subject.
TASK: Implement Phase 4 Tasks 1 to 3. Kafka and core NATS have no per-message priority (confirm against vendor documentation), so both use the Destination capability.
KEY RULES: unmapped priority is a Left, never a fallback to the default topic; topics and subjects are never created by the publisher; no message content in logs.
REFERENCE FILES: src/Encina.Kafka/KafkaMessagePublisher.cs, EncinaKafkaOptions.cs, src/Encina.NATS/NATSMessagePublisher.cs, EncinaNATSOptionsValidator.cs.
```

</details>

### Phase 5: Amazon SQS and Azure Service Bus

<details>
<summary><strong>Tasks</strong></summary>

1. `EncinaAmazonSQSOptions.Priority` (capability `Destination`, mapping of priority to queue URL) and `AmazonSQSMessagePublisher`: `queueUrl` from the mapping, message attribute `encina-priority`; validator extended.
2. `EncinaAzureServiceBusOptions.Priority` and `AzureServiceBusMessagePublisher`: `ServiceBusMessage.ApplicationProperties["encina-priority"]` always; capability `Destination` (queue or topic per priority) because the message class has no priority property (confirm against vendor documentation), so the issue's "Native" cell is corrected.
3. Log events in the existing `AmazonSQS` (4350-4399) and `AzureServiceBus` (4300-4349) ranges.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 5</strong></summary>

```text
CONTEXT: AmazonSQSMessagePublisher takes an optional queueUrl; AzureServiceBusMessagePublisher builds ServiceBusMessage with Subject and sends through a sender per destination.
TASK: Implement Phase 5 Tasks 1 to 3.
KEY RULES: queue URLs and entity names come only from validated configuration; unmapped priority is a Left; cloud services are tested with local emulators (LocalStack for SQS, the Service Bus emulator) per SPEC-000 DEC-009; connection strings stay [JsonIgnore].
REFERENCE FILES: src/Encina.AmazonSQS/AmazonSQSMessagePublisher.cs, EncinaAmazonSQSOptionsValidator.cs, src/Encina.AzureServiceBus/AzureServiceBusMessagePublisher.cs.
```

</details>

### Phase 6: Redis.PubSub and MQTT

<details>
<summary><strong>Tasks</strong></summary>

1. `EncinaRedisPubSubOptions.Priority` and `RedisPubSubMessagePublisher`: channel from the mapping (Redis Pub/Sub has no headers, so the channel name is the only carrier; capability `Destination`).
2. `EncinaMQTTOptions.Priority` and `MQTTMessagePublisher`: topic from the mapping and an MQTT 5 user property `encina-priority` (confirm the client library and broker version support it).
3. Log events in the existing `RedisPubSub` (4400-4449) and `MQTT` (4250-4299) ranges.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 6</strong></summary>

```text
CONTEXT: Redis Pub/Sub and MQTT publishers publish to a channel or topic given by options or an argument.
TASK: Implement Phase 6 Tasks 1 to 3.
KEY RULES: same contract as Phases 3 to 5; on MQTT fall back to MetadataOnly only if the maintainer-approved capability table says so, never silently; no message content in logs.
REFERENCE FILES: src/Encina.Redis.PubSub/RedisPubSubMessagePublisher.cs, src/Encina.MQTT/MQTTMessagePublisher.cs, the Log.cs of each.
```

</details>

### Phase 7: Cross-Cutting Integration and Observability

<details>
<summary><strong>Tasks</strong></summary>

1. `src/Encina/Diagnostics/EventIdRanges.cs`: register `MessagingTransportPriority = (6725, 6734)` for the shared helper and add it to the `Encina.Messaging` entry in `EncinaEventIdAllocationTests`; per-transport events stay in each transport's own range.
2. `src/Encina.Messaging/Diagnostics/TransportPriorityLogMessages.cs` (`[LoggerMessage]`, 6725 to 6728): route resolved (transport, priority and destination name only), unmapped priority rejected (code), unsupported capability rejected (code), options validated at start-up.
3. Span attribute `encina.transport.priority` on each publisher's existing publish activity where one exists.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 7</strong></summary>

```text
CONTEXT: ADR-021: register the range first (6725-6734 is assigned to this plan by the batch registry). Per-transport LoggerMessage events live in each transport's Log.cs inside its registered range.
TASK: Implement Phase 7 Tasks 1 to 3, packing EventIds 6725-6728 sequentially.
KEY RULES: [LoggerMessage] source generator; no message content, no EncinaError.Message in logs or tags; run the EventId architecture tests.
REFERENCE FILES: src/Encina/Diagnostics/EventIdRanges.cs, tests/Encina.UnitTests/Testing/Architecture/EncinaEventIdAllocationTests.cs, src/Encina.RabbitMQ/Log.cs.
```

</details>

### Phase 8: Testing

<details>
<summary><strong>Tasks</strong></summary>

1. Unit: router per capability (native, destination, metadata only), unmapped and unsupported priorities, options validation, InMemory pump order with `FakeTimeProvider`.
2. Guard: all new public members.
3. Contract: one publisher contract run against every transport publisher (or its test double at the client boundary): same priority in gives the same destination and metadata out, `Left` for unmapped, unchanged behavior when disabled.
4. Property (FsCheck): router output is a function of (priority, mapping) only; InMemory delivery has no starvation beyond the threshold.
5. Integration against real brokers or local emulators through the shared fixtures (RabbitMQ, Kafka, NATS, Redis, MQTT broker, LocalStack SQS, Service Bus emulator); native RabbitMQ priority delivery order is observed.
6. Load and benchmark: `.md` justifications (routing is an in-memory lookup); the InMemory pump is the only concurrent code and is covered by a property test.
7. Coverage: per-file targets in each transport manifest under `.github/coverage-manifest/` (`mechanical-fixer`); CRAP at most 10.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 8</strong></summary>

```text
CONTEXT: AGENTS.md section 9: per-flag coverage, tests execute real package code, Shouldly through Encina.Testing.Shouldly, Testcontainers fixtures, contract tests must instantiate real implementations.
TASK: Write the tests of Phase 8 Tasks 1 to 5 and the justification files of Task 6; report per-flag coverage against each manifest.
KEY RULES: deterministic (FakeTimeProvider, no Thread.Sleep); every transport of the category; cloud transports against local emulators.
REFERENCE FILES: tests/Encina.UnitTests (existing transport tests), tests/Encina.IntegrationTests (transport collections), .github/coverage-manifest/Encina.RabbitMQ.json.
```

</details>

### Phase 9: Documentation and Finalization

<details>
<summary><strong>Tasks</strong></summary>

1. XML docs; `PublicAPI.Unshipped.txt` per package; `changelog.d/251-transport-priority.added.md` (`mechanical-fixer`).
2. `docs-writer`: transport priority guide with the capability table (the verified one), consumer guidance against starvation (choice 4), package READMEs; the ADR of the shared priority design (#123) gets a transport section; ADR number assigned when the ADR pull request opens.
3. Update #251 to the corrected scope (no `IPriorityQueueManager`, no `PriorityLevels`, no `StarvationTimeout`).
4. `dotnet build --configuration Release` with zero warnings; `dotnet test`; every flag at its target.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 9</strong></summary>

```text
CONTEXT: documentation, changelog fragments and PublicAPI lines have path owners (docs-writer, mechanical-fixer).
TASK: Finish Phase 9 Tasks 1 to 4 after Phases 1 to 8 are merged.
KEY RULES: English only; coverage figures only through covref markers; write "the reference application", never a private client name; the capability table lists only what was confirmed against vendor documentation.
REFERENCE FILES: changelog.d/README.md, docs/architecture/adr/index.md, docs/plans/dsr-implementation-plan-404.md.
```

</details>

---

## Research

### Relevant Standards & Specifications

| Standard or source | Relevance |
| --- | --- |
| Azure Architecture Center, Priority Queue pattern | Order work by priority; separate queues per priority where the broker has none |
| RabbitMQ priority queues | Native: `x-max-priority` queue argument and the message `priority` property (confirm at implementation) |
| Azure Service Bus, Kafka, SQS, NATS, MQTT 5 documentation | No per-message priority property is expected; confirm each before relying on it |
| SPEC-000 DEC-009, SPEC-000 section 6 | The 10 existing and 6 planned transports are in 1.0; cloud ones are tested with local emulators |
| AGENTS.md sections 3, 5, 6, 7 | Category consistency, opt-in, cross-cutting check, EventIds |
| ADR-001, ADR-006, ADR-018, ADR-021, ADR-050 | ROP, cross-cutting check, EventIds, module dependencies |

Capability table (draft, every "to confirm" cell is checked against vendor documentation by the implementing worker):

| Transport | Capability | Carrier |
| --- | --- | --- |
| RabbitMQ | Native (to confirm) | `BasicProperties.Priority`, queue `x-max-priority` |
| AzureServiceBus | Destination (no priority property, to confirm) | queue or topic per priority, `ApplicationProperties` |
| AmazonSQS | Destination | queue URL per priority, message attribute |
| Kafka | Destination | topic per priority, record header |
| NATS | Destination | subject per priority, header |
| Redis.PubSub | Destination | channel per priority (no headers) |
| MQTT | Destination | topic per priority, MQTT 5 user property (to confirm) |
| InMemory | Reference ordering | priority queue and pump |
| gRPC, GraphQL | Not planned here | see #3400 |

### Existing Encina Infrastructure to Leverage

| Component | Location | Usage in this feature |
| --- | --- | --- |
| Per-transport publishers and options | `src/Encina.RabbitMQ`, `Encina.Kafka`, `Encina.AmazonSQS`, `Encina.AzureServiceBus`, `Encina.NATS`, `Encina.Redis.PubSub`, `Encina.MQTT` | Attach route and metadata |
| `InMemoryMessageBus` | `src/Encina.InMemory/InMemoryMessageBus.cs` | Reference priority behavior |
| `MessagePriority`, `MessagePriorityResolver` (from #123) | `src/Encina.Messaging/Priority/` | Shared type and resolution |
| Shared `"Encina"` meter | `src/Encina.Messaging/Diagnostics/OutboxProcessorMetrics.cs` pattern | `encina.transport.priority.published` |
| Transport `Log.cs` files and registered ranges | each transport, `src/Encina/Diagnostics/EventIdRanges.cs` (4100-4499) | Per-transport events |
| `EncinaAmazonSQSOptionsValidator`, `EncinaNATSOptionsValidator` | transport packages | Validation pattern for the new options |

### Event ID Allocation

| Package | Range | Notes |
| --- | --- | --- |
| `Encina.Messaging` (`EventIdRanges.MessagingTransportPriority`, new) | 6725-6734 | From the family block 6700-6749; this plan uses 6725-6728. Siblings: #123 6700-6714, #301 6715-6724, #469 6735-6747, #1254 6748-6749 |
| Each transport package | existing ranges 4100-4499 | Ids chosen at implementation from the free slots of each transport's `Log.cs` |

### Estimated File Count

| Category | Files | Notes |
| --- | --- | --- |
| Shared core in `Encina.Messaging` | 8 | Options, capability, router, metrics, logs, `EventIdRanges.cs`, PublicAPI |
| Transports (8 packages) | 32 | Options, publisher, `Log.cs`, validator or DI per package |
| Tests | 24 | Unit, guard, contract, property, integration per transport |
| Docs, changelog, PublicAPI | 14 | Guide, READMEs, fragment, PublicAPI per package |

---

## Combined AI Agent Prompts

<details>
<summary><strong>Combined Prompt — All Phases</strong></summary>

```text
PROJECT CONTEXT: Encina, pre-1.0, .NET 10 / C# 14. Issue #251, plan docs/plans/transport-priority-implementation-plan-251.md. It applies the shared priority design of #123 to the transport category; #123 must be merged first. There is no IMessageTransport in the code: each transport has its own publisher interface, and transports only publish. Read AGENTS.md first.

IMPLEMENTATION OVERVIEW: (1) TransportPriorityOptions, capability, router and metrics in Encina.Messaging. (2) InMemory reference ordering. (3) RabbitMQ native. (4) Kafka and NATS destinations. (5) SQS and Azure Service Bus. (6) Redis.PubSub and MQTT. (7) EventIds 6725-6728 and spans. (8) Tests against real brokers and emulators. (9) Docs. Seven pull requests of at most 15 files and 1500 lines.

KEY PATTERNS: Either<EncinaError, T>; an unmapped or unsupported priority is a Left, never ignored; off by default on every transport; priority resolved from the message by MessagePriorityResolver; destinations only from validated configuration; metadata header encina-priority; consistent behavior across the category; [LoggerMessage] with registered ranges; shared meter; secrets stay [JsonIgnore] with ToString overrides.

REFERENCE FILES: src/Encina.RabbitMQ/RabbitMQMessagePublisher.cs, src/Encina.Kafka/KafkaMessagePublisher.cs, src/Encina.AmazonSQS/AmazonSQSMessagePublisher.cs, src/Encina.AzureServiceBus/AzureServiceBusMessagePublisher.cs, src/Encina.InMemory/InMemoryMessageBus.cs, src/Encina/Diagnostics/EventIdRanges.cs, docs/plans/dsr-implementation-plan-404.md.
```

</details>

---

## Cross-Cutting Integration Matrix

| # | Function | Status | Notes |
|---|----------|--------|-------|
| 1 | Caching | ❌ | Priority routing does not read cacheable data |
| 2 | OpenTelemetry | ✅ | `encina.transport.priority.published` counter and the `encina.transport.priority` span attribute (Phase 7) |
| 3 | Structured Logging | ✅ | `[LoggerMessage]` 6725-6728 plus per-transport events in their registered ranges (Phase 7) |
| 4 | Health Checks | ❌ | Each transport already has a connectivity health check; per-priority queue depth is broker-side monitoring and needs no Encina dependency |
| 5 | Validation | ✅ | `TransportPriorityOptions` validated at start-up (every level mapped for the Destination capability); priority value validated by the resolver (Phases 1 to 6) |
| 6 | Resilience | ❌ | No new external call: a publish to a per-priority destination fails with the same `Left` as today and follows the application's existing policy |
| 7 | Distributed Locks | ❌ | Brokers distribute messages to consumers; no shared state is updated by Encina |
| 8 | Transactions | ❌ | Publishing to a broker is not transactional with the database; atomic publish is the outbox, #123 |
| 9 | Idempotency | ❌ | Duplicates are handled by the outbox and inbox, issues #735, #148 and #1220 |
| 10 | Multi-Tenancy | ❌ | Priority carries no tenant data; tenant-aware destinations are a separate concern of the tenancy plans |
| 11 | Module Isolation | ❌ | No module scoping is introduced; `ModuleId` on messaging entities is #747 |
| 12 | Audit Trail | ❌ | No compliance-relevant data is created |

---

## Prerequisites & Dependencies

- #123 merged: `MessagePriority`, `IPrioritizedMessage`, `MessagePriorityResolver`, and the answer to the shared-type question (this plan's choice 1 is the same question).
- #3400 (what `Encina.gRPC` and `Encina.GraphQL` are for 1.0) decides whether those two adapters need this capability; they are not planned here, and the plan must be amended once the spike is decided.
- The six planned 1.0 transports add a row to the capability table in their own plans.
- Local emulators for the cloud transports (LocalStack, Service Bus emulator) as in SPEC-000 DEC-009.

## Next Steps

1. Maintainer answers choices 1 to 4 when work starts (choice 1 repeats #123 choice 1); answers go to `## Maintainer Decisions`.
2. Orchestrator updates #251 to the corrected scope after the answers.
3. Worker briefs per pull request (PR-A to PR-G) once #123 merges; the capability table is verified by the implementing worker per transport.
