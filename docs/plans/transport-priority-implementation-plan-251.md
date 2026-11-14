# Implementation Plan: Priority for Message Transports (publish-side priority across the transport category)

> **Issue**: [#251](https://github.com/dlrivada/Encina/issues/251)
> **Type**: Feature
> **Complexity**: High (9 phases, 7 existing transports plus the reference InMemory bus, 107 file changes, delivered in 10 pull requests of at most 15 files each, see "Delivery order and size")
> **Estimated Scope**: about 1,100 lines of production code + about 1,800 lines of tests
> **Family**: message priority and partitioning (#123, #251, #301, #469, #1254). The shared priority design (type, attribute, interface, resolver) is OWNED by #123, and the type `MessagePriority` is created by PR-A of #123; this plan applies it to the transport category and decides only what is specific to transports. It starts after PR-A of #123 has merged (it never creates the type) and is independent of #301, #469 and #1254.

---

## Summary

A message that carries a `MessagePriority` keeps it when it leaves Encina for a broker: natively where the broker has message priority, by routing to a per-priority destination (queue, topic, subject, channel) where it does not, and always as message metadata. A transport never silently ignores the priority (AGENTS.md section 5, Transports row): an unsupported or unmapped priority returns a `Left`.

Facts verified in today's code that reshape the issue:

- **There is no shared transport abstraction.** `IMessageTransport` (named in AGENTS.md) does not exist in `src/`. Each transport has its own publisher interface: `IRabbitMQMessagePublisher`, `IKafkaMessagePublisher`, `IAmazonSQSMessagePublisher`, `IAzureServiceBusMessagePublisher`, `INATSMessagePublisher`, `IRedisPubSubMessagePublisher`, `IMQTTMessagePublisher`, and `IInMemoryMessageBus`. `IPriorityQueueManager` as drawn in the issue would be a second queueing abstraction beside all of them (choice 3).
- **The transports only publish.** The packages have 6 to 10 source files each, with publishers, options, a `Log.cs` and DI registration, and no consumer or subscription loop (only `InMemoryMessageBus.Subscribe`). "Consumed in priority order" is therefore the application's consumer unless Encina adds one (choice 4).
- Publishers already take an optional destination: `RabbitMQMessagePublisher` a `routingKey` and `CreateProperties(messageId)` building `BasicProperties`; `KafkaMessagePublisher.ProduceAsync` a `topic`; `AmazonSQSMessagePublisher` a `queueUrl`; `AzureServiceBusMessagePublisher` builds a `ServiceBusMessage` with `Subject`. This is where priority routing and metadata attach.
- RabbitMQ, Kafka, AmazonSQS, AzureServiceBus, NATS and MQTT reference `Encina` and `Encina.Messaging`; `Encina.InMemory` and `Encina.Redis.PubSub` reference only `Encina` (verified in their `.csproj` files). Both need a new project reference to `Encina.Messaging`, or the shared types would have to move to core; this is register entry T1.
- `InMemoryMessageBus` already has a queued path: `PublishAsync` delivers inline to subscribers, while `EnqueueAsync` writes to a bounded or unbounded `Channel` drained by `WorkerCount` background workers started in the constructor (`BoundedCapacity`, `FullMode`, `UseUnboundedChannel` in `EncinaInMemoryOptions`); a handler failure on that path is logged by `Log.ErrorProcessingQueuedMessage`, there is no publisher to return a `Left` to.
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
- The InMemory reference bus does have delivery inside Encina, on its queued path (`EnqueueAsync`); the starvation rule of #123 applies there only if register entry T2 is answered A (Phase 2), which would give the tested reference policy.

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

- A feature touching transports must be consistent across the category, and every messaging capability is optional and disabled by default; this excludes option B (two transports only).
- The shared `TransportPriorityRouter` (in `Encina.Messaging`) takes the resolved priority, the transport's capability (`Native` or `Destination`) and the mapping, and returns the destination and metadata or a `Left`. Transports differ only in how they attach them.
- What is NOT settled by that rule is kept out of this entry: the `Left` for an unmapped priority is choice 2 option A, the wire contract is entry T3, and the capability set is entry T6.

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

### Decision register (decisions not covered by choices 1 to 6)

Class is Trivial, Settled (verifiable citation excludes every other option), Dilemma (the maintainer decides; recommendation only) or Defer.

| Id | Decision | Class | Text |
| --- | --- | --- | --- |
| T1 | `Encina.InMemory` and `Encina.Redis.PubSub` get a project reference to `Encina.Messaging` | Dilemma (recommended: A, pending the maintainer) | A) add the references (keeps the shared types where #123 puts them); B) move `MessagePriority`, the resolver and the router to core `Encina` (no new references, but it changes the placement decided in #123 choice 1 and every transport then depends on a larger core). Transports that never use priority pay one extra assembly reference under A |
| T2 | InMemory ordering applies to the queued path (`EnqueueAsync`) only, with `InMemoryPriorityOptions` (`Enabled` false, `StarvationThreshold` default 5 minutes as #123 choice 3); queue bound, `FullMode`, shutdown (cancellation of the existing token) and the logging of a handler failure on the queued path stay as today; `PublishAsync` (inline delivery) is unchanged | Dilemma (recommended: A, pending the maintainer) | B) InMemory only carries the metadata and does not order, so no reference policy exists for the category. A wins when the contract tests need an in-process reference of the aged-first rule; B wins when the smallest change is wanted |
| T3 | Wire contract: one metadata name `encina-priority` on every carrier (RabbitMQ header, Kafka record header, SQS message attribute, Service Bus application property, NATS header, MQTT 5 user property), value is the enum name | Dilemma (recommended: A, pending the maintainer) | B) per-carrier conventions (for example `Encina-Priority` on NATS) or the numeric value. A wins on one documented name; B wins where a carrier has its own naming convention. The name is public once released |
| T4 | RabbitMQ `BasicProperties.Priority` is the numeric value of the #123 enum (`Background` 0 to `Critical` 4) and the queue argument is `x-max-priority = 4` | Trivial | Derived from the enum values of #123 choice 1; if the maintainer changes the levels there, this changes with them |
| T5 | A message with no resolvable priority is not an error: `MessagePriorityResolver` returns `Normal` (#123 choice 2), so every message has a priority when the feature is enabled; the `Destination` capability requires a mapping for all five levels, validated at start-up | Trivial | Consequence of the resolver contract; an undefined enum value is a `Left` (`messaging.invalid_priority`, #123 R4) |
| T6 | Capabilities are `Native` and `Destination` only; the `MetadataOnly` capability of the first draft is dropped | Trivial | No transport of the table needs it; a transport added later brings it in its own plan |
| T7 | Telemetry carries the transport name and the priority only: no destination name in metric tags (unbounded cardinality) and none in logs; the per-publish "route resolved" log is dropped (hot path); logs record an error code, the transport and the priority | Trivial | AGENTS.md section 3 (no message content or error message in logs); bounded tag cardinality |
| T8 | The priority carried in a header is unauthenticated metadata: consumers must not use it for authorization; the guide says so | Trivial | A documentation obligation of Phase 9 |
| T9 | Tenant-aware destinations (a mapping per tenant) | Defer | Follow-up issue to be opened (draft: `artifacts/decisions/plan-batch/followups/transport-priority-tenant-destinations.md`) |

---

## Implementation Phases

### Delivery order and size

Nothing here changes an existing interface: each transport gains an options property and publisher logic behind `Enabled = false`, so every pull request leaves main building. No pull request uses a type created by a later one: the router, options and log class come first (A), then InMemory (B), then one transport per pull request. All pull requests have at most 15 files and 1500 lines; counts include tests, coverage manifests, PublicAPI files and READMEs. Prerequisite: PR-A of #123 has merged.

| PR | Content (phases) | src | Tests | PublicAPI | Manifests | Docs | Total |
| --- | --- | ---: | ---: | ---: | ---: | ---: | ---: |
| A | `TransportPriorityOptions`, `TransportPriorityCapability`, `TransportPriorityRouter`, `TransportPriorityMetrics`, `TransportPriorityLogMessages`, `EventIdRanges.cs`; router and options unit tests, one guard file, the EventId allocation test (Phase 1, Phase 7) | 6 | 4 | 2 | 2 | 0 | 14 |
| B | InMemory: csproj reference, options, `InMemoryMessageBus` queued path, `Log.cs`; unit, options and guard tests, a property test (no starvation beyond the threshold); README (Phase 2) | 4 | 4 | 1 | 1 | 1 | 11 |
| C | RabbitMQ: options, publisher, `Log.cs`, the `x-max-priority` helper; unit, guard, new publisher integration class, its fixture, the contract test file (created here) (Phase 3) | 4 | 5 | 1 | 1 | 1 | 12 |
| D | Kafka: options, publisher, `Log.cs`; unit, guard, new integration class and fixture, contract test (Phase 4) | 3 | 5 | 1 | 1 | 1 | 11 |
| E | NATS: options, validator, publisher, `Log.cs`; unit, guard, new integration class and fixture, contract test (Phase 4) | 4 | 5 | 1 | 1 | 1 | 12 |
| F | Amazon SQS: options, validator, publisher, `Log.cs`; unit, guard, the existing publisher integration class on LocalStack, contract test (Phase 5) | 4 | 4 | 1 | 1 | 1 | 11 |
| G | Azure Service Bus: options, publisher, `Log.cs`; unit, guard, the existing publisher integration class on the emulator, contract test (Phase 5) | 3 | 4 | 1 | 1 | 1 | 10 |
| H | Redis.PubSub: csproj reference, options, publisher, `Log.cs`; unit, guard, the existing publisher integration class, contract test (Phase 6) | 4 | 4 | 1 | 1 | 1 | 11 |
| I | MQTT: options, publisher, `Log.cs`; unit, guard, new integration class and fixture, contract test (Phase 6) | 3 | 5 | 1 | 1 | 1 | 11 |
| J | Guide with the verified capability table, INVENTORY, ADR section, changelog fragment (Phase 9; `docs-writer`, `mechanical-fixer`) | 0 | 0 | 0 | 0 | 4 | 4 |
| **Total** | 10 pull requests | **35** | **40** | **10** | **10** | **12** | **107** |

Sum: 14 + 11 + 12 + 11 + 12 + 11 + 10 + 11 + 11 + 4 = 107; by column 35 + 40 + 10 + 10 + 12 = 107. Where a broker has only a health-check integration test today (RabbitMQ, Kafka, NATS, MQTT), the publisher integration class is new in that pull request, with the collection fixture of its health-check test where one is shared and a new fixture file (counted) otherwise. SQS (LocalStack collection), Service Bus (emulator collection) and Redis.PubSub already have publisher integration classes.

**Tests and coverage targets ship with each pull request.** Each enters per-file targets (with a one-sentence justification, written by `mechanical-fixer`) in its package manifest and reports measured per-flag coverage. New files: `TransportPriorityRouter.cs` (unit 95, guard 85: unit tests per capability, unmapped and unsupported priorities; guard tests for null arguments), `TransportPriorityOptions.cs` (unit 95, guard 60), `TransportPriorityCapability.cs` (0, 0: an enum has no executable lines), `TransportPriorityMetrics.cs` (unit 80, guard 0: instruments created once, no parameter checks), `TransportPriorityLogMessages.cs` (unit 70, guard 0: source-generated, asserted through a fake logger). Existing targets of every touched publisher never go down.

### Phase 1: Shared Routing Core in `Encina.Messaging`

<details>
<summary><strong>Tasks</strong></summary>

New folder `src/Encina.Messaging/Transport/`:

Pull request A.

1. `TransportPriorityOptions.cs`: `Enabled` (false), `Destinations` (`IDictionary<MessagePriority, string>`), validation (all five levels mapped when the transport has `Destination` capability, register entry T5).
2. `TransportPriorityCapability.cs`: `Native`, `Destination` (register entry T6).
3. `TransportPriorityRouter.cs`: `Either<EncinaError, TransportPriorityRoute> Route(object message, TransportPriorityCapability capability, TransportPriorityOptions options, string defaultDestination)` (the `TransportPriorityRoute` record lives in the same file); uses `MessagePriorityResolver` (#123, created by its PR-A); error codes `transport.priority_unmapped` and `transport.priority_unsupported`, both built with the category overload of `EncinaErrors.Create` (ADR 062, merged): `ErrorCategory.Internal` for unmapped (a configuration fault caught at start-up validation, so reaching it at run time is a wiring bug) and `ErrorCategory.FailedPrecondition` for unsupported. The metadata name is a constant (register entry T3).
4. `TransportPriorityMetrics.cs`: `encina.transport.priority.published` (counter, tags `transport`, `priority`) on the shared `"Encina"` meter.
5. `TransportPriorityLogMessages.cs` and the range in `EventIdRanges.cs` (Phase 7), created here because the router is the first code that logs.
6. `PublicAPI.Unshipped.txt` and manifest entries through `mechanical-fixer`.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 1</strong></summary>

```text
CONTEXT: Plan docs/plans/transport-priority-implementation-plan-251.md. PR-A of #123 merged MessagePriority and MessagePriorityResolver in src/Encina.Messaging/Priority; never create them here. There is no IMessageTransport in the code: every transport has its own publisher interface and options class.
TASK: Implement Phase 1 Tasks 1 to 5 as a transport-agnostic helper that returns the destination and metadata for a message, or a Left.
KEY RULES: Either<EncinaError, T> (ADR-001, ADR-006); never silently ignore a priority; no provider-specific code in Encina.Messaging (AGENTS.md section 3, provider coherence); XML docs; metric tags carry the transport name and priority only, never message content.
REFERENCE FILES: src/Encina.Messaging/Priority/MessagePriorityResolver.cs (from #123), src/Encina.AmazonSQS/EncinaAmazonSQSOptionsValidator.cs, src/Encina.Messaging/Diagnostics/OutboxProcessorMetrics.cs.
```

</details>

### Phase 2: `Encina.InMemory` Reference Behavior

<details>
<summary><strong>Tasks</strong></summary>

Pull request B; the whole phase is conditional on register entries T1 and T2.

1. `src/Encina.InMemory/Encina.InMemory.csproj`: project reference to `Encina.Messaging` (T1, option A).
2. `src/Encina.InMemory/EncinaInMemoryOptions.cs`: `Priority` options (`Enabled` false, `StarvationThreshold` 5 minutes); `InMemoryMessageBus`: when enabled, `EnqueueAsync` stores messages in a priority-ordered structure drained by the existing `WorkerCount` workers in the order of #123 choice 3 (aged first, then `Priority` descending, then arrival), `TimeProvider` for the age and the existing `BoundedCapacity`/`FullMode`/cancellation semantics unchanged; `PublishAsync` and the disabled mode behave exactly as today (T2). A handler failure on the queued path is logged as today; no `Left` is returned because no publisher waits for it.
3. `Log.cs` events inside the existing `InMemory` range 4450-4499, packed after the last used id, written by this pull request only if it logs something new.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 2</strong></summary>

```text
CONTEXT: InMemoryMessageBus dispatches to subscribers directly (PublishAsync, Subscribe). It is the reference implementation the contract tests of the category compare against.
TASK: Implement Phase 2 Tasks 1 to 3. The priority-ordered queued path is opt-in (register entry T2).
KEY RULES: time from TimeProvider; the existing workers keep catching per iteration and never let an exception escape (cancellation is not an error); EventIds inside the existing InMemory range 4450-4499, packed; only the queued path (EnqueueAsync) is ordered.
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
2. `EncinaNATSOptions.Priority` and `NATSMessagePublisher`: subject from the mapping, header `encina-priority` (one name on every carrier, register entry T3).
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
KEY RULES: same contract as Phases 3 to 5; on MQTT, if the client or broker has no MQTT 5 user property, the publisher returns a Left (no silent fallback; register entry T6 has no MetadataOnly capability); Redis.PubSub gets its project reference to Encina.Messaging in this pull request (T1); no message content in logs.
REFERENCE FILES: src/Encina.Redis.PubSub/RedisPubSubMessagePublisher.cs, src/Encina.MQTT/MQTTMessagePublisher.cs, the Log.cs of each.
```

</details>

### Phase 7: Cross-Cutting Integration and Observability

<details>
<summary><strong>Tasks</strong></summary>

Registered in pull request A (the first that logs, so the range and the log class exist before any publisher uses them).

1. `src/Encina/Diagnostics/EventIdRanges.cs`: register `MessagingTransportPriority = (6712, 6714)`, exactly the 3 ids used, packed after the 6708-6711 of #301 (itself after the 6700-6707 of #123); add it to the `Encina.Messaging` entry in `EncinaEventIdAllocationTests`; per-transport events stay in each transport's own range.
2. `src/Encina.Messaging/Diagnostics/TransportPriorityLogMessages.cs` (`[LoggerMessage]`, 6712 to 6714): unmapped priority rejected (code, transport, priority), unsupported capability rejected (code, transport), priority routing enabled at start-up (transport name). No per-publish "route resolved" event (hot path, register entry T7).
3. Span attribute `encina.transport.priority` on each publisher's existing publish activity where one exists, added in that transport's pull request.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 7</strong></summary>

```text
CONTEXT: ADR-021: register the range first (the family block is 6700-6749 in the batch registry; this plan registers 6712-6714). Per-transport LoggerMessage events live in each transport's Log.cs inside its registered range.
TASK: Implement Phase 7 Tasks 1 to 3, packing EventIds 6712-6714 sequentially.
KEY RULES: [LoggerMessage] source generator; no message content, no EncinaError.Message in logs or tags; run the EventId architecture tests.
REFERENCE FILES: src/Encina/Diagnostics/EventIdRanges.cs, tests/Encina.UnitTests/Testing/Architecture/EncinaEventIdAllocationTests.cs, src/Encina.RabbitMQ/Log.cs.
```

</details>

### Phase 8: Testing

<details>
<summary><strong>Tasks</strong></summary>

Tests ship with the pull request that owns the code (see the delivery table); this phase lists the kinds.

1. Unit: router per capability (native, destination), unmapped and unsupported priorities, options validation (A); InMemory queued-path order with `FakeTimeProvider` (B).
2. Guard (AGENTS.md section 9, #3798): every new public constructor or method with a validated parameter gets its argument tests in the pull request that adds it (router and options in A, each transport's options and publisher in its own pull request).
3. Contract: one publisher contract file, created in C and extended by D to I, instantiates the REAL publisher of each transport with a test double only at the broker client boundary (never a double of the publisher): same priority in gives the same destination and metadata out, `Left` for unmapped, unchanged behavior when disabled. It compares routing output only; ordering parity between InMemory and a broker is not claimed.
4. Property (FsCheck): InMemory delivery has no starvation beyond the threshold (B); router output as a function of (priority, mapping) only is a unit-level property in A.
5. Integration against real brokers or local emulators through shared collection fixtures; per transport: SQS (LocalStack collection), Service Bus (emulator collection) and Redis.PubSub already have publisher integration classes, RabbitMQ, Kafka, NATS and MQTT get a new class (and a fixture file when their health-check test does not share one); native RabbitMQ priority delivery order is observed.
6. Load and benchmark: `.md` justifications (routing is an in-memory lookup); the InMemory queued path is the only concurrent code and is covered by the property test.
7. Coverage: per-file targets in each transport manifest under `.github/coverage-manifest/` entered in the same pull request as the tests (`mechanical-fixer`); CRAP at most 10 on changed methods.

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
2. `docs-writer`: transport priority guide with the capability table (the verified one), consumer guidance against starvation (choice 4), a security note that the priority header is unauthenticated metadata and consumers must never use it for authorization (register entry T8), package READMEs (written in each transport pull request); the ADR of the shared priority design (#123) gets a transport section; if #123 does not open that ADR before pull request J, the transport section becomes its own ADR; the ADR number is assigned when the ADR pull request opens.
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
| InMemory | Reference ordering (entry T2) | priority order on the `EnqueueAsync` queued path |
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
| `Encina.Messaging` (`EventIdRanges.MessagingTransportPriority`, new) | 6712-6714 | From the family block 6700-6749; exactly the 3 ids used, packed after #123 (6700-6707) and #301 (6708-6711), which register their own ranges; #469 and #1254 use the rest of the block |
| Each transport package | existing ranges 4100-4499 | Ids chosen at implementation from the free slots of each transport's `Log.cs` |

### Estimated File Count

The authoritative count is the "Delivery order and size" table (107 file changes in 10 pull requests).

| Category | File changes | Notes |
| --- | ---: | --- |
| Source | 35 | Router core 6, InMemory 4, seven transports 25 (options, validator where it exists, publisher, `Log.cs`, csproj reference for Redis.PubSub) |
| Tests | 40 | Unit, guard, contract, property, integration and new fixtures |
| PublicAPI | 10 | Two in A, one per package after |
| Manifests | 10 | Same |
| Documentation | 12 | Eight package READMEs, guide, INVENTORY, ADR section, changelog fragment |
| Total | 107 | Equals the delivery table |

---

## Combined AI Agent Prompts

<details>
<summary><strong>Combined Prompt — All Phases</strong></summary>

```text
PROJECT CONTEXT: Encina, pre-1.0, .NET 10 / C# 14. Issue #251, plan docs/plans/transport-priority-implementation-plan-251.md. It applies the shared priority design of #123 to the transport category; #123 must be merged first. There is no IMessageTransport in the code: each transport has its own publisher interface, and transports only publish. Read AGENTS.md first.

IMPLEMENTATION OVERVIEW: (1) TransportPriorityOptions, capability, router and metrics in Encina.Messaging. (2) InMemory reference ordering. (3) RabbitMQ native. (4) Kafka and NATS destinations. (5) SQS and Azure Service Bus. (6) Redis.PubSub and MQTT. (7) EventIds 6712-6714 and spans. (8) Tests against real brokers and emulators. (9) Docs. Ten pull requests of at most 15 files and 1500 lines (A router, B InMemory, C to I one transport each, J docs), each with its own tests and manifest targets. PR-A of #123 (the type) merges first.

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
| 3 | Structured Logging | ✅ | `[LoggerMessage]` 6712-6714 plus per-transport events in their registered ranges (Phase 7) |
| 4 | Health Checks | ❌ | Each transport already has a connectivity health check; per-priority queue depth is broker-side monitoring and needs no Encina dependency |
| 5 | Validation | ✅ | `TransportPriorityOptions` validated at start-up (every level mapped for the Destination capability); priority value validated by the resolver (Phases 1 to 6) |
| 6 | Resilience | ❌ | No new external call: a publish to a per-priority destination fails with the same `Left` as today and follows the application's existing policy |
| 7 | Distributed Locks | ❌ | Brokers distribute messages to consumers; no shared state is updated by Encina |
| 8 | Transactions | ❌ | Publishing to a broker is not transactional with the database; atomic publish is the outbox, #123 |
| 9 | Idempotency | ❌ | Duplicates are handled by the outbox and inbox, issues #735, #148 and #1220 |
| 10 | Multi-Tenancy | ❌ | Priority carries no tenant data; follow-up issue to be opened for tenant-aware destinations (draft: `artifacts/decisions/plan-batch/followups/transport-priority-tenant-destinations.md`, register entry T9) |
| 11 | Module Isolation | ❌ | No module scoping is introduced; `ModuleId` on messaging entities is #747 |
| 12 | Audit Trail | ❌ | No compliance-relevant data is created |

---

## Prerequisites & Dependencies

- PR-A of #123 merged: `MessagePriority`, `IPrioritizedMessage`, `MessagePriorityResolver`, the shared error code, and the answer to the shared-type question (this plan's choice 1 is the same question). No pull request of this plan starts before it, and none creates those types.
- #3400 (what `Encina.gRPC` and `Encina.GraphQL` are for 1.0) decides whether those two adapters need this capability; they are not planned here, so the issue's acceptance criterion "every transport of the category behaves consistently" is NOT met until the plan is amended once the spike is decided; the issue update (Next Steps 2) records this explicitly.
- The six planned 1.0 transports add a row to the capability table in their own plans.
- Local emulators for the cloud transports (LocalStack, Service Bus emulator) as in SPEC-000 DEC-009.

## Next Steps

1. Maintainer answers choices 1 to 4 and entries T1, T2, T3 when work starts (choice 1 repeats #123 choice 1); answers go to `## Maintainer Decisions`.
2. Orchestrator updates #251 to the corrected scope after the answers: no `IPriorityQueueManager`, `PriorityLevels` or `StarvationTimeout`; RabbitMQ queue declaration with `x-max-priority` stays with the application (Encina publishes the property and offers the argument as a helper); Azure Service Bus is `Destination`, not `Native`; gRPC and GraphQL are outside (see Prerequisites); cross-cutting rows 4 and 6 are Not applicable with one sentence each.
3. Worker briefs per pull request (PR-A to PR-J) once PR-A of #123 merges; the capability table is verified by the implementing worker per transport.

## Maintainer Decisions

None yet. Pending: choices 1 to 4 and entries T1, T2, T3.
