# Implementation Plan: Message Batching, Bulk Publish Support (IBatchPublisher across the message transports)

> **Issue**: [#252](https://github.com/dlrivada/Encina/issues/252)
> **Type**: Feature
> **Complexity**: High (8 phases, 9 pull requests of 13, 13, 10, 10, 10, 10, 11, 11 and 13 files)
> **Estimated Scope**: about 900 lines of production code and about 2,600 lines of tests, split over nine pull requests (one per transport, after the shared contract)
> **Family**: msg-batching (#121, #252, #302, #339, #349). This plan is independent of the request-batching plans (#302, #349, #339); #121's batch notification handler is a typical caller of the contract defined here. #2385 (outbox-to-transport publisher) will call it. Ordering by partition key and FIFO group ids are #469 and #255.

---

## Summary

The issue asks for an `IBatchPublisher` contract with partial-failure reporting and per-transport batch implementations for Kafka, SQS, Azure Service Bus and RabbitMQ. State of `origin/main` (`b50b8c981`), verified by reading the publishers:

| Transport | Publisher interface | Batch support today | Defect or gap |
|-----------|---------------------|---------------------|---------------|
| Kafka | `IKafkaMessagePublisher` | `ProduceBatchAsync(messages, topic)` | Produces one message at a time and awaits each `ProduceAsync`, so the client never batches; stops at the first failure |
| AmazonSQS | `IAmazonSQSMessagePublisher` | `SendBatchAsync(messages, queueUrl)` | One `SendMessageBatch` call: more than 10 messages or 256 KB fails the whole call; `response.Failed` entries are only logged (`Log.BatchPartiallyFailed`) and the caller gets only the successful ids, so a failed entry is swallowed; a FIFO queue cannot be used (no group id) |
| AzureServiceBus | `IAzureServiceBusMessagePublisher` | None (`SendMessageAsync` per message) | No `ServiceBusMessageBatch` |
| RabbitMQ | `IRabbitMQMessagePublisher` | None (`BasicPublishAsync` per message) | `EncinaRabbitMQOptions.UsePublisherConfirms` (default `true`) is copied in `ServiceCollectionExtensions.cs:37` but the singleton channel is created at `:59` with a plain `CreateChannelAsync()` and the option is read nowhere else (`git grep UsePublisherConfirms`), so confirmations are off today; nothing acknowledges a batch. The single-publish defect is reported as an issue file, not fixed here |
| NATS, MQTT, Redis.PubSub, InMemory | `INATSMessagePublisher`, `IMQTTMessagePublisher`, `IRedisPubSubMessagePublisher`, `IInMemoryMessageBus` | None | `AGENTS.md` section 5 requires a transport feature to be consistent across every transport: these four join the issue's four |
| gRPC, GraphQL | `GrpcMediatorService`, `GraphQLMediatorBridge` | n/a | Request bridges, not message publishers: no `Publisher` type exists in `src/Encina.gRPC` or `src/Encina.GraphQL` (verified by grep); not applicable |

Delivered: in `Encina.Messaging` the contract `IBatchPublisher`, `BatchPublishOptions`, `BatchPublishResult`, `BatchItemFailure` and the shared error codes; in each of the eight transports the implementation (native where the broker has one: Kafka pipelined produce, SQS chunked batches with per-entry results, Azure Service Bus `ServiceBusMessageBatch`; confirmations for RabbitMQ; pipelined or bounded-concurrency publishing elsewhere); the two partial methods `ProduceBatchAsync` and `SendBatchAsync` are replaced by the contract (no alias); a shared contract test base that every transport subclasses; log messages inside each transport's existing EventId range.

**Transport matrix (first draft by the local model from the facts above, reviewed against the publishers)**:

| Transport | Publisher interface today | Batch method today | Planned batch mechanism | Per-call limit |
|---|---|---|---|---|
| Kafka | `IKafkaMessagePublisher` | `ProduceBatchAsync` (produces one message at a time and awaits each) | produce all messages with delivery callbacks, then flush the producer | none by count; the producer batches by its own batch size and linger settings |
| AmazonSQS | `IAmazonSQSMessagePublisher` | `SendBatchAsync` (one SendMessageBatch call, partial failures only logged, more than 10 messages not chunked) | chunked SendMessageBatch calls with per-entry results | 10 entries and 256 KB per call |
| AzureServiceBus | `IAzureServiceBusMessagePublisher` | none | ServiceBusMessageBatch built with TryAddMessage, one send per full batch | bounded by the batch size in bytes, not by a message count |
| RabbitMQ | `IRabbitMQMessagePublisher` | none | publish with confirmations and await all confirmations together | none by count |
| NATS | `INATSMessagePublisher` | none | concurrent JetStream publishes awaiting each acknowledgement, core publish pipelined | none by count |
| MQTT | `IMQTTMessagePublisher` | none | publishes in order with bounded concurrency | broker in-flight window |
| Redis.PubSub | `IRedisPubSubMessagePublisher` | none | concurrent PublishAsync calls on the multiplexer (it pipelines) | none by count |
| InMemory | `IInMemoryMessageBus` | none | loop over the bus in one call | none |

**Provider category**: Messaging transports (10 existing plus 6 planned). Eight publishers are implemented here; gRPC and GraphQL are not publishers. The six planned transports (#237, #238, #239, #241 and the Redis.Streams and Dapr transports) implement the contract when they land, by subclassing the shared contract test; an issue file records it (Phase 7).

---

## Design Choices

<details>
<summary><strong>1. How the capability is exposed (Dilemma D1)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `IBatchPublisher` is a capability interface: every transport's own publisher interface inherits it (`IKafkaMessagePublisher : IBatchPublisher`), and callers inject the transport interface they already use** | No ambiguity when several transports are registered; the shared contract test works over all of them; no router | `IBatchPublisher` is never resolved from DI itself |
| **B) Also register each transport's batch capability as a keyed service of `IBatchPublisher`** | A caller can pick a transport by key | Keys are strings with no registry; a second way to reach the same object |
| **C) One registered `IBatchPublisher` that points to the single configured transport** | The shape of the issue | Fails or picks arbitrarily when two transports are registered, which is the normal case for an outbox plus a command queue |

### Chosen Option: **A, a capability inherited by each transport interface** (recommended, pending the maintainer)

### Rationale

A wins when applications register more than one transport, because the existing publisher interfaces are already per transport; C wins only for single-transport applications that want a transport-neutral call site (a later router can be added without changing A). The interface lives in `Encina.Messaging`, where the abstractions of providers live; `Encina.Redis.PubSub` and `Encina.InMemory` gain a project reference to it.

**Class:** Dilemma

</details>

<details>
<summary><strong>2. The result shape (Dilemma D2)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) The issue's counts plus failures: `BatchPublishResult(int SuccessCount, int FailureCount, IReadOnlyList<BatchItemFailure> Failures)` and `BatchItemFailure(int Index, EncinaError Error)`** | Matches the issue; small; the caller learns which message failed and why | Per-message receipts (Kafka partition and offset, SQS message id) are not returned by the batch call |
| **B) A per-message list: `Items: IReadOnlyList<Either<EncinaError, PublishReceipt>>` aligned with the input** | Callers that need ids or offsets (event sourcing, correlation) get them | A richer type than the issue asks for; a receipt type that must cover broker differences |

### Chosen Option: **A, the issue's shape** (recommended, pending the maintainer)

### Rationale

A wins when callers need only success or failure per message; B wins when callers must record where each message landed. The failure error is the transport's `EncinaError` (callers must never log its message, `AGENTS.md` section 3); a batch reports `AllSucceeded` as computed from the counts.

**Class:** Dilemma

</details>

<details>
<summary><strong>3. One signature for every transport</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `ValueTask<Either<EncinaError, BatchPublishResult>> PublishBatchAsync<TMessage>(IReadOnlyList<TMessage> messages, BatchPublishOptions? options = null, CancellationToken cancellationToken = default) where TMessage : class`** | Same shape as the existing publisher methods (`ValueTask`, `Either`, `where TMessage : class`); a list has a count and an index | The issue's `IEnumerable<T> where T : INotification` is not used |
| **B) The issue's `IEnumerable<T> where T : INotification`** | Matches the issue text | Every existing publisher accepts any class (Kafka and SQS publish plain DTOs), so a notification constraint would exclude them; an `IEnumerable` has no stable index for failures |

### Chosen Option: **A, the existing convention**

### Rationale

`IKafkaMessagePublisher`, `IAmazonSQSMessagePublisher`, `IAzureServiceBusMessagePublisher` and `IRabbitMQMessagePublisher` all use `ValueTask`, `Either<EncinaError, ...>` and `where TMessage : class`.

**Class:** Trivial
**Settled by:** the four existing publisher interfaces, for example `src/Encina.Kafka/IKafkaMessagePublisher.cs` (`ProduceAsync<TMessage> ... where TMessage : class`).

</details>

<details>
<summary><strong>4. Partial failure is a `Right` with failures; nothing is swallowed</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) After the pre-flight checks pass, return `Right(BatchPublishResult)` whose `Failures` list every message that was not accepted; `Left` only for what prevents the attempt (missing destination, a null element, an unsupported destination such as an SQS FIFO queue)** | The caller sees exactly which messages failed; no failure is dropped (today SQS drops them into a log line) | A caller that ignores `FailureCount` thinks the call succeeded; the XML docs and the README say to check it |
| **B) `Left` when any message fails** | A failure is a `Left` | The partial information the issue requires is lost |

### Chosen Option: **A, partial results**

### Rationale

#252's acceptance requires "`BatchPublishResult` with partial failure info", and `AGENTS.md` section 3 requires a failed operation to fail visibly, which the explicit `FailureCount` and the per-message error do. A cancelled token propagates `OperationCanceledException` and is never a failure entry.

**Class:** Settled
**Settled by:** #252 acceptance criteria ("`BatchPublishResult` with partial failure info") and `AGENTS.md` section 3 (errors are never swallowed).

</details>

<details>
<summary><strong>5. The two partial methods are replaced, not kept</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Remove `IKafkaMessagePublisher.ProduceBatchAsync` and `IAmazonSQSMessagePublisher.SendBatchAsync`; their callers (only their own tests: `git grep` finds no other caller in `src`) move to `PublishBatchAsync`** | One batch method per transport; the swallowing SQS behavior disappears | The two tests files per transport change |
| **B) Keep them next to the new method** | Nothing to update | Two batch APIs with different failure semantics per transport |

### Chosen Option: **A, replace**

### Rationale

`AGENTS.md` section 3 forbids compatibility aliases and layers.

**Class:** Settled
**Settled by:** `AGENTS.md` section 3 (no compatibility aliases or layers; pre-1.0, section 1).

</details>

<details>
<summary><strong>6. Linger and the maximum delay (Dilemma D6)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) The explicit list API has no delay: `BatchPublishOptions` carries `MaxBatchSize`, `PartitionByType` and `Destination`; the issue's `MaxDelay` is not added; Kafka's own linger and batch size are exposed as `LingerMs` and `BatchSizeBytes` on `EncinaKafkaOptions`** | A call publishes what it is given and returns; the broker-native linger stays in the broker client | "Automatic batching with max delay" of the issue's feature list is not provided for transports without a native linger |
| **B) Also add a decorator that buffers single publishes and flushes by size or delay, built on #302's collector** | Single `PublishAsync` callers batch without code changes | A second buffering layer in front of each transport, with its own durability gap (a crash loses buffered messages), and a dependency on `Encina.Batching` |

### Chosen Option: **A, no delay in the contract** (recommended, pending the maintainer)

### Rationale

A wins when callers already hold a list, as the outbox does (#121 hands runs of messages to a batch handler); B wins for services that publish many single events and accept the durability trade-off, which belongs in its own issue because a buffered publisher is not an acknowledged publisher. Kafka compression stays a producer setting (`CompressionType` on `EncinaKafkaOptions`, null by default); no other transport has native payload compression, so the issue's "batch compression" is limited to Kafka.

**Class:** Dilemma

</details>

<details>
<summary><strong>7. Chunking, order and the SQS FIFO case</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Chunks are sent one after another in input order, each at most `min(MaxBatchSize, the transport's limit)`; SQS chunks by 10 entries and 256 KB and an Azure Service Bus batch fills by bytes; a batch to an SQS FIFO queue returns `Left` `SQS_FIFO_BATCH_UNSUPPORTED` (category `InvalidRequest`)** | Order is preserved across chunks; each limit is a transport fact handled where it lives; FIFO is not silently broken | FIFO bulk publishing waits for group ids |
| **B) Send chunks concurrently** | Faster | Order across chunks is lost |
| **C) Generate a group id for FIFO batches** | FIFO works | An invented group id defeats the ordering the queue was chosen for; keys come from the partition family (#469, #255) |

### Chosen Option: **A, sequential chunks and an explicit FIFO refusal**

### Rationale

`SendToFifoQueueAsync` in `src/Encina.AmazonSQS/AmazonSQSMessagePublisher.cs` already requires an explicit `messageGroupId`, so a batch without one cannot be valid on a FIFO queue.

**Class:** Settled
**Settled by:** `src/Encina.AmazonSQS/AmazonSQSMessagePublisher.cs` (`SendToFifoQueueAsync` requires a group id) and the AWS `SendMessageBatch` limits named by the issue (10 messages).

</details>

---

## Implementation Phases

### Phase 1: The contract (pull request 1)

<details>
<summary>Tasks</summary>

1. `src/Encina.Messaging/Transport/IBatchPublisher.cs` (D1, D3): `ValueTask<Either<EncinaError, BatchPublishResult>> PublishBatchAsync<TMessage>(IReadOnlyList<TMessage> messages, BatchPublishOptions? options = null, CancellationToken cancellationToken = default) where TMessage : class`; XML docs state the partial-failure rule (D4), the chunk order rule and that cancellation propagates.
2. `BatchPublishOptions` (sealed record: `MaxBatchSize` 100 validated 1 to 10,000, `PartitionByType` false, `Destination` null meaning the transport's default derived as for a single publish; `Validate()` returning `Either<EncinaError, Unit>`), `BatchPublishResult(SuccessCount, FailureCount, Failures)` with `AllSucceeded`, `BatchItemFailure(int Index, EncinaError Error)` (D2), and `BatchPublishErrorCodes` (`messaging.batch_null_item` category `InvalidRequest`, `messaging.batch_options_invalid` category `InvalidRequest`).
3. `tests/Encina.ContractTests/MessageBrokers/BatchPublisherContractBase.cs` (abstract, real publisher instances built by each subclass over a faked SDK client): empty list gives `Right(0, 0)`, all accepted, one rejected message reported with its index and error, a null element gives `Left`, more messages than `MaxBatchSize` are published once each in input order, cancellation throws `OperationCanceledException`, `Destination` overrides the default.
4. Tests: unit `BatchPublishResultTests` and `BatchPublishOptionsTests`, guard `BatchPublishOptionsGuardTests` (`Validate` rules); PublicAPI.Unshipped for `Encina.Messaging` (through `mechanical-fixer`), `Encina.Messaging.json` manifest entries, README edit (through `docs-writer`), changelog fragment `changelog.d/252-bulk-publish.added.md` (through `mechanical-fixer`).

</details>

<details>
<summary>Prompt for AI Agents — Phase 1</summary>

```text
CONTEXT: No transport has a common batch contract. Each transport has its own publisher interface (IKafkaMessagePublisher, IAmazonSQSMessagePublisher, ...) with ValueTask and Either results. D1 (a capability interface inherited by each transport interface), D2 (counts plus failures) and D6 (no delay in the contract) apply as recommended until the maintainer answers.
TASK: Add IBatchPublisher, BatchPublishOptions, BatchPublishResult, BatchItemFailure, the error codes and the shared contract test base with their tests.
KEY RULES: Partial failure is Right with Failures, never swallowed; a null element is Left; cancellation propagates; XML docs with examples; ADR-062 category on every code; never log or return an exception message; the contract test base instantiates real publishers.
REFERENCE FILES: src/Encina.Kafka/IKafkaMessagePublisher.cs, src/Encina.AmazonSQS/IAmazonSQSMessagePublisher.cs, tests/Encina.ContractTests/MessageBrokers/AmazonSQS/AmazonSQSPublisherContractTests.cs, .github/coverage-manifest/Encina.Messaging.json.
```

</details>

### Phase 2: Kafka (pull request 2)

<details>
<summary>Tasks</summary>

1. `IKafkaMessagePublisher : IBatchPublisher`; `ProduceBatchAsync` removed (D5). `KafkaMessagePublisher.PublishBatchAsync`: per chunk, start every `ProduceAsync` without awaiting each (the client batches by its linger and batch size), then await them together; each delivery report maps to a success or a `BatchItemFailure` (`KAFKA_BATCH_ITEM_FAILED`, category `UpstreamFailure`); a thrown `ProduceException` for one message fails that message only; `Destination` replaces the default topic. Key and message id are derived exactly as `ProduceAsync` derives them (the same private helper, no copy).
2. `EncinaKafkaOptions.LingerMs`, `BatchSizeBytes`, `CompressionType` (all nullable, validated, applied to the producer configuration in `ServiceCollectionExtensions`); EventIds 4176 batch published (Debug, count) and 4177 batch partially failed (Warning, counts), inside the existing `Kafka` range 4150-4199 (4156-4175 are claimed by #587, 4176-4199 are free on `origin/main`).
3. Tests: edit `KafkaMessagePublisherBatchTests` and `KafkaGuardTests` for the new method; new `KafkaBatchPublisherContractTests` (subclass of the contract base) and `KafkaPublisherBatchIntegrationTests` (Testcontainers Kafka, shared collection); PublicAPI.Unshipped (removal and additions, through `mechanical-fixer`), `Encina.Kafka.json`, README edit, changelog fragment `changelog.d/252-kafka-batch-publish.changed.md`.

</details>

<details>
<summary>Prompt for AI Agents — Phase 2</summary>

```text
CONTEXT: IBatchPublisher exists in Encina.Messaging. The current Kafka ProduceBatchAsync awaits each message and stops at the first failure.
TASK: Implement PublishBatchAsync for Kafka, remove ProduceBatchAsync, add the producer options and log messages, and update the tests.
KEY RULES: Never await each produce inside the loop; one message's failure never aborts the others; exception type only in logs (ForLogging); EventIds 4176-4177 only inside the Kafka range; database-style async rules do not apply but cancellation must reach the awaits.
REFERENCE FILES: src/Encina.Kafka/KafkaMessagePublisher.cs, src/Encina.Kafka/EncinaKafkaOptions.cs, src/Encina.Kafka/ServiceCollectionExtensions.cs, tests/Encina.UnitTests/Kafka/Publishing/KafkaMessagePublisherBatchTests.cs.
```

</details>

### Phase 3: Amazon SQS (pull request 3)

<details>
<summary>Tasks</summary>

1. `IAmazonSQSMessagePublisher : IBatchPublisher`; `SendBatchAsync` removed. `PublishBatchAsync`: chunks of at most 10 entries and 256 KB (the sum of each body and its attributes, measured before sending); entry ids are the global input index as text, so `Successful` and `Failed` entries map back to input indexes; every `Failed` entry becomes a `BatchItemFailure` (`SQS_BATCH_ENTRY_FAILED`, category `UpstreamFailure`, from the entry's code, never the SDK message); a thrown exception fails every message of that chunk (`SQS_BATCH_SEND_FAILED`, existing code); one message above 256 KB alone is a failure (`SQS_MESSAGE_TOO_LARGE`, `InvalidRequest`); a FIFO queue URL (ends with `.fifo`) returns `Left` `SQS_FIFO_BATCH_UNSUPPORTED` (D7). The existing log messages 4356-4359 are reused (4357 reports the partial failure counts); no new EventId.
2. Tests: edit `AmazonSQSMessagePublisherBatchTests`, `AmazonSQSGuardTests`, `AmazonSQSPublisherContractTests` (subclass the base) and `AmazonSQSPublisherIntegrationTests` (LocalStack); PublicAPI.Unshipped, `Encina.AmazonSQS.json`, README edit, changelog fragment `changelog.d/252-sqs-batch-publish.changed.md` (the swallowed partial failure is now reported).

</details>

<details>
<summary>Prompt for AI Agents — Phase 3</summary>

```text
CONTEXT: SendBatchAsync sends one SendMessageBatch call, does not chunk, and drops failed entries into a log line.
TASK: Replace it with PublishBatchAsync (chunking by 10 entries and 256 KB, per-entry results, FIFO refusal) and update the tests including the LocalStack integration test.
KEY RULES: Entry ids map to input indexes; no failed entry is dropped; only error codes of entries in results, never SDK messages; reuse the existing log messages; no new EventId.
REFERENCE FILES: src/Encina.AmazonSQS/AmazonSQSMessagePublisher.cs, src/Encina.AmazonSQS/Log.cs, tests/Encina.UnitTests/AmazonSQS/Publishing/AmazonSQSMessagePublisherBatchTests.cs, tests/Encina.IntegrationTests/MessageBrokers/AmazonSQS/AmazonSQSPublisherIntegrationTests.cs.
```

</details>

### Phase 4: Azure Service Bus (pull request 4)

<details>
<summary>Tasks</summary>

1. `IAzureServiceBusMessagePublisher : IBatchPublisher`. `PublishBatchAsync`: one `ServiceBusSender` per call (created for `Destination` or the default topic or queue, disposed with `await using`, as the single publish does); build a `ServiceBusMessageBatch` with `CreateMessageBatchAsync` and `TryAddMessage`; when a message does not fit, send the batch and start another; a message that does not fit an empty batch is a failure (`ASB_MESSAGE_TOO_LARGE`, `InvalidRequest`); a failed send fails the messages of that batch (`ASB_BATCH_SEND_FAILED`, `UpstreamFailure`, exception type only). The batch size is bounded by bytes, so `MaxBatchSize` caps the count per batch on top.
2. EventIds 4312 batch sent (Debug), 4313 batch partially failed (Warning), 4314 message too large (Warning), inside the `AzureServiceBus` range 4300-4349 (4312-4349 free).
3. Tests: new `AzureServiceBusMessagePublisherBatchTests`, edit `AzureServiceBusGuardTests`, `AzureServiceBusPublisherContractTests` (subclass) and `AzureServiceBusPublisherIntegrationTests` (emulator); PublicAPI.Unshipped, `Encina.AzureServiceBus.json`, README edit.

</details>

<details>
<summary>Prompt for AI Agents — Phase 4</summary>

```text
CONTEXT: The Azure Service Bus publisher sends one message per call; the SDK offers ServiceBusMessageBatch sized in bytes.
TASK: Implement PublishBatchAsync with ServiceBusMessageBatch and TryAddMessage, the log messages and the tests.
KEY RULES: A message too large for an empty batch is a per-message failure, never an exception; dispose the sender; exception type only in logs; EventIds 4312-4314 only.
REFERENCE FILES: src/Encina.AzureServiceBus/AzureServiceBusMessagePublisher.cs, src/Encina.AzureServiceBus/Log.cs, tests/Encina.ContractTests/MessageBrokers/AzureServiceBus/AzureServiceBusPublisherContractTests.cs.
```

</details>

### Phase 5: RabbitMQ and NATS (pull requests 5 and 6)

<details>
<summary>Tasks</summary>

1. RabbitMQ: `IRabbitMQMessagePublisher : IBatchPublisher`; `PublishBatchAsync` publishes the chunk on a dedicated channel that the batch publisher creates itself from the injected `IConnection` with publisher confirmations enabled (the RabbitMQ.Client 7.2.1 channel options; the exact option names and the channel's concurrency rules are verified against the package at work start), honors `EncinaRabbitMQOptions.UsePublisherConfirms` (when it is `false`, a batch publishes without confirmations and reports success on handoff, stated in the XML docs), and awaits every confirmation; a nack or a returned message fails that message (`RABBITMQ_BATCH_ITEM_FAILED`, `UpstreamFailure`); the existing single publish path and its channel are unchanged. EventIds 4106 batch published, 4107 batch partially failed, 4108 confirmation timed out, inside the `RabbitMQ` range 4100-4149.
2. NATS: `INATSMessagePublisher : IBatchPublisher`; with JetStream registered, the chunk's `JetStreamPublishAsync` calls start together and each acknowledgement is awaited (a missing ack or a duplicate-rejected publish is a per-message failure `NATS_BATCH_ITEM_FAILED`); without JetStream the core publishes are pipelined. EventIds 4210 batch published, 4211 batch partially failed, 4212 JetStream unavailable for the batch, inside the `NATS` range 4200-4249.
3. Tests per transport: new unit batch tests, guard edits, contract subclass, new integration class (Testcontainers RabbitMQ, NATS with JetStream), PublicAPI.Unshipped, manifest, README edit.

</details>

<details>
<summary>Prompt for AI Agents — Phase 5</summary>

```text
CONTEXT: Neither publisher batches today; RabbitMQ publishes without confirmations.
TASK: Implement PublishBatchAsync for RabbitMQ (confirmed publishing on a dedicated channel) and NATS (concurrent JetStream publishes), with log messages and tests, one pull request each.
KEY RULES: The single-message publish path keeps its behavior; every nack, return or missing ack is a per-message failure; exception type only in logs; EventIds only inside each transport's range as listed; verify SDK member names at work start.
REFERENCE FILES: src/Encina.RabbitMQ/RabbitMQMessagePublisher.cs, src/Encina.NATS/NATSMessagePublisher.cs, tests/Encina.IntegrationTests/MessageBrokers/NATS/NATSJetStreamDeduplicationIntegrationTests.cs, tests/Encina.UnitTests/NATS/NATSPublishAckTests.cs.
```

</details>

### Phase 6: MQTT, Redis.PubSub and InMemory (pull requests 7, 8 and 9)

<details>
<summary>Tasks</summary>

1. MQTT: `IMQTTMessagePublisher : IBatchPublisher`; publishes in input order with bounded concurrency (an option `MaxInFlight` default 10, validated above zero) and one `BatchItemFailure` per rejected publish (`MQTT_BATCH_ITEM_FAILED`). EventIds 4256-4258 in the `MQTT` range 4250-4299.
2. Redis.PubSub: `src/Encina.Redis.PubSub/Encina.Redis.PubSub.csproj` gains the `Encina.Messaging` project reference; `IRedisPubSubMessagePublisher : IBatchPublisher`; the chunk's `PublishAsync` calls run together on the multiplexer (it pipelines); a message that fails is reported, a message with no subscriber is not a failure (Redis returns zero receivers). EventIds 4406-4408 in the `RedisPubSub` range 4400-4449 (4400-4405 are used).
3. InMemory: `src/Encina.InMemory/Encina.InMemory.csproj` gains the `Encina.Messaging` project reference; `IInMemoryMessageBus : IBatchPublisher`; the batch loops over the in-process bus and reports a failing subscriber per message. EventIds 4460-4462 in the `InMemory` range 4450-4499.
4. Tests per transport: new unit tests, guard edits (a new guard test file for InMemory), contract subclass, an integration class for MQTT (Testcontainers mosquitto) and Redis.PubSub (Testcontainers Redis); InMemory has no external service, so its contract test runs the real bus and no integration class is added; PublicAPI.Unshipped, manifest, README edit; pull request 9 also adds `docs/features/bulk-publish.md` (through `docs-writer`).

</details>

<details>
<summary>Prompt for AI Agents — Phase 6</summary>

```text
CONTEXT: MQTT, Redis.PubSub and InMemory have no batch support; Redis.PubSub and InMemory do not reference Encina.Messaging yet.
TASK: Implement PublishBatchAsync for the three transports, add the project references, log messages and tests, one pull request each.
KEY RULES: Bounded concurrency for MQTT; order preserved where the broker preserves it; no subscriber is not a failure on Redis; exception type only in logs; EventIds only inside each transport's range as listed; a project reference edit must keep the package restoring and building alone.
REFERENCE FILES: src/Encina.MQTT/MQTTMessagePublisher.cs, src/Encina.Redis.PubSub/RedisPubSubMessagePublisher.cs, src/Encina.InMemory/InMemoryMessageBus.cs, tests/Encina.GuardTests/Messaging/RedisPubSub/RedisPubSubMessagePublisherGuardTests.cs.
```

</details>

### Phase 7: Cross-Cutting Integration and finalization

<details>
<summary>Tasks</summary>

1. Errors (ADR-062): `messaging.batch_null_item` `InvalidRequest`, `messaging.batch_options_invalid` `InvalidRequest`, per transport `<T>_BATCH_ITEM_FAILED` `UpstreamFailure`, `SQS_BATCH_ENTRY_FAILED` `UpstreamFailure`, `SQS_FIFO_BATCH_UNSUPPORTED` `InvalidRequest`, `SQS_MESSAGE_TOO_LARGE` `InvalidRequest`, `ASB_MESSAGE_TOO_LARGE` `InvalidRequest`, `ASB_BATCH_SEND_FAILED` `UpstreamFailure` (each transport keeps its existing code naming style).
2. Logging (function 3): counts and error codes only; the exception type through `ForLogging()`; no `EncinaError.Message`.
3. Issue files: `artifacts/issues/rabbitmq-publisher-confirms-not-applied.md` (the dead `UsePublisherConfirms` option and the confirmation-less single publish, found while reading the publisher) and `artifacts/issues/bulk-publish-planned-transports.md`: the six planned transports implement the contract and subclass the shared contract test when they land (#237, #238, #239, #241, Redis.Streams, Dapr), as `AGENTS.md` section 5 requires consistency across the category.
4. Final verification as in Phase 8.

</details>

<details>
<summary>Prompt for AI Agents — Phase 7</summary>

```text
CONTEXT: Eight transports implement the contract; the planned ones follow later.
TASK: Verify each integration exists in code and tests and that the planned-transports issue file exists.
KEY RULES: Exception type only in logs and results; never log EncinaError.Message; ADR-062 category on every code.
REFERENCE FILES: AGENTS.md sections 3, 5 and 6, docs/architecture/adr/062-error-category-contract.md.
```

</details>

### Phase 8: Testing and documentation

<details>
<summary>Tasks</summary>

Per-flag targets are entered in each package's manifest (`.github/coverage-manifest/Encina.Messaging.json`, `Encina.Kafka.json`, `Encina.AmazonSQS.json`, `Encina.AzureServiceBus.json`, `Encina.RabbitMQ.json`, `Encina.NATS.json`, `Encina.MQTT.json`, `Encina.Redis.PubSub.json`, `Encina.InMemory.json`) in the pull request that edits the file. Baselines of the edited publisher files are measured at work start with `dotnet test tests\Encina.<Flag>Tests --collect "XPlat Code Coverage" --results-directory artifacts\coverage\<Flag>Tests` and `dotnet run --file .github/scripts/coverage-report.cs -- --input artifacts/coverage --output artifacts/coverage-report`; each target is the measured value or higher.

| File | unit | guard | contract | property | integration |
|------|:---:|:---:|:---:|:---:|:---:|
| `BatchPublishOptions.cs`, `BatchPublishResult.cs`, `BatchItemFailure.cs` | 100 | 90 | 0 | 0 | 0 |
| `KafkaMessagePublisher.cs`, `AmazonSQSMessagePublisher.cs` (edited) | measured baseline or higher | measured baseline or higher | 70 | 0 | 60 |
| `AzureServiceBusMessagePublisher.cs`, `RabbitMQMessagePublisher.cs`, `NATSMessagePublisher.cs` (edited) | measured baseline or higher | measured baseline or higher | 70 | 0 | 60 |
| `MQTTMessagePublisher.cs`, `RedisPubSubMessagePublisher.cs` (edited) | measured baseline or higher | measured baseline or higher | 70 | 0 | 60 |
| `InMemoryMessageBus.cs` (edited) | measured baseline or higher | measured baseline or higher | 80 | 0 | 0 |
| `EncinaKafkaOptions.cs` (edited) | 100 | measured baseline or higher | 0 | 0 | 0 |
| `IBatchPublisher.cs` and the transport interfaces (edited) | 0 | 0 | 0 | 0 | 0 |

| File and flag | Justification |
|---------------|---------------|
| Result and options types unit 100, guard 90 | Pure data and validation; the guard tests reach every argument check. |
| Publishers contract 70, integration 60 | The shared contract base runs the real publisher over a faked SDK client for every scenario of Phase 1, and the broker integration tests (emulators and Testcontainers) run the native paths; the remainder are SDK failure branches that need fault injection. |
| `InMemoryMessageBus.cs` contract 80, integration 0 | The contract test runs the real bus end to end; there is no external service for an integration test. |
| Edited publisher unit and guard: baselines | The existing publish paths keep their tests; the new batch tests raise the unit figure and the targets stay at least at the measured baseline. |
| Interfaces 0 | No executable lines. |

1. **Load and Benchmark**: justification files `tests/Encina.LoadTests/Messaging/BulkPublish.md` and `tests/Encina.BenchmarkTests/Messaging/BulkPublish.md` (pull request 9, the last): throughput is a property of each broker and client, measured by the broker's own tools; a benchmark of the faked clients would not support a decision.
2. **Documentation**: `docs/features/bulk-publish.md` (pull request 9) and the README of each package through `docs-writer`; changelog fragments through `mechanical-fixer`; an ADR is not needed (no new cross-package decision beyond the capability interface of D1): ADR number assigned when the ADR pull request opens, if the maintainer asks for one.
3. `dotnet build --configuration Release` with zero warnings; all tests green; every flag at its target; CRAP of every changed method below 10.

</details>

<details>
<summary>Prompt for AI Agents — Phase 8</summary>

```text
CONTEXT: Per-flag targets live in each package's coverage manifest; targets never go down; tests execute real code.
TASK: Write the tests, the two justification files and the manifest entries with one-sentence justifications in the pull request that edits each file; documentation and changelog fragments go through the owning specialists.
KEY RULES: Shouldly through Encina.Testing.Shouldly; shared [Collection] fixtures and Testcontainers or emulators for integration tests, never a per-class fixture; no Thread.Sleep; never edit CHANGELOG.md; no hand-typed coverage figures in docs.
REFERENCE FILES: tests/Encina.IntegrationTests/MessageBrokers, tests/Encina.ContractTests/MessageBrokers, docs/testing/integration-tests.md, changelog.d/README.md, .claude/skills/encina-docs/SKILL.md.
```

</details>

---

## Research

### Relevant Standards & Specifications

| Standard / Spec | Relevance | Notes |
|-----------------|-----------|-------|
| `AGENTS.md` section 5 | A transport feature must be consistent across every provider of the category (10 plus 6 planned) | Provider matrix above |
| `AGENTS.md` section 3 | Errors never swallowed; no aliases; exception messages never in logs | D4, D5 |
| ADR-021, ADR-062 | EventIds in the package's own range; error categories | Phases 2 to 7 |
| Kafka producer batching (`batch.size`, `linger.ms`); SQS `SendMessageBatch`; Azure Service Bus message batches (named by the issue and its references) | Native mechanisms | Provider matrix |

### Existing Encina Infrastructure to Leverage

| Component | Location | Usage in this feature |
|-----------|----------|-----------------------|
| The eight publisher interfaces and classes | `src/Encina.{Kafka,AmazonSQS,AzureServiceBus,RabbitMQ,NATS,MQTT,Redis.PubSub,InMemory}/` | Inherit the contract and implement it |
| Existing batch methods and tests | `IKafkaMessagePublisher.ProduceBatchAsync`, `IAmazonSQSMessagePublisher.SendBatchAsync`, `tests/Encina.UnitTests/{Kafka,AmazonSQS}/Publishing/*BatchTests.cs` | Replaced (D5) |
| Contract tests with emulators | `tests/Encina.ContractTests/MessageBrokers/{AmazonSQS,AzureServiceBus}` | Pattern for the shared contract base |
| Integration test collections | `tests/Encina.IntegrationTests/MessageBrokers/` | Shared fixtures for the new integration classes |
| `EventIdRanges` per transport | `src/Encina/Diagnostics/EventIdRanges.cs` (`Kafka` 4150-4199, `AmazonSQS` 4350-4399, `AzureServiceBus` 4300-4349, `RabbitMQ` 4100-4149, `NATS` 4200-4249, `MQTT` 4250-4299, `RedisPubSub` 4400-4449, `InMemory` 4450-4499) | Ids allocated inside each range |

### Event ID Allocation

| Package | Range | Notes |
|---------|-------|-------|
| `Kafka` (existing 4150-4199) | 4176-4177 | 4156-4175 claimed by #587 |
| `AmazonSQS` (existing 4350-4399) | none new | Reuses 4356-4359 |
| `AzureServiceBus` (existing 4300-4349) | 4312-4314 | Free on `origin/main` |
| `RabbitMQ` (existing 4100-4149) | 4106-4108 | Free on `origin/main` |
| `NATS` (existing 4200-4249) | 4210-4212 | Free on `origin/main` |
| `MQTT` (existing 4250-4299) | 4256-4258 | Free on `origin/main` |
| `RedisPubSub` (existing 4400-4449) | 4406-4408 | 4400-4405 used |
| `InMemory` (existing 4450-4499) | 4460-4462 | 4450-4459 used |

No new range and no block of `artifacts/decisions/plan-batch/eventid-blocks.md` is used; the ids above are inside existing package ranges and are listed here for the registry.

### Estimated File Count

| Category | Files | Notes |
|----------|------:|-------|
| Production code | 33 | contract (4 types plus error codes), 8 interface edits, 8 publisher edits, 7 `Log` edits (SQS has none), `EncinaKafkaOptions`, `EncinaMQTTOptions`, Kafka `ServiceCollectionExtensions`, 2 csproj edits (Redis.PubSub, InMemory) |
| Public API and manifest | 18 | one PublicAPI.Unshipped and one manifest per package, nine packages |
| Tests | 38 | contract base, 2 unit and 1 guard for the contract, 8 transport unit tests, 8 guard edits, 8 contract subclasses, 6 integration classes, edits of the existing Kafka and SQS tests |
| Justification files | 2 | load, benchmark |
| Documentation | 14 | 9 README edits, feature page, 4 changelog fragments |
| **Pull request 1** | **13** | 4 types plus the error codes class, PublicAPI, manifest, contract base, 3 unit and guard tests, README edit, changelog fragment; about 600 lines |
| **Pull request 2** | **13** | Kafka: 5 production (interface, publisher, options, `ServiceCollectionExtensions`, `Log`), PublicAPI, manifest, 4 tests, README, changelog fragment; about 700 lines |
| **Pull request 3** | **10** | SQS: interface, publisher, PublicAPI, manifest, 4 test edits, README, changelog fragment |
| **Pull request 4** | **10** | Azure Service Bus: interface, publisher, Log, PublicAPI, manifest, 4 tests, README |
| **Pull request 5** | **10** | RabbitMQ: interface, publisher, Log, PublicAPI, manifest, 4 tests, README |
| **Pull request 6** | **10** | NATS: same shape as pull request 5 |
| **Pull request 7** | **11** | MQTT: pull request 5's shape plus the options edit (`MaxInFlight`) |
| **Pull request 8** | **11** | Redis.PubSub: pull request 5's shape plus the csproj edit |
| **Pull request 9** | **13** | InMemory: csproj, interface, bus, Log, PublicAPI, manifest, 3 tests (unit, guard, contract), README, feature page, 2 justification files (load, benchmark) |

Callers and implementers, by grep on `origin/main` (2026-11-17): `ProduceBatchAsync` and `SendBatchAsync` are declared in their interfaces and publishers and called only by `KafkaMessagePublisherBatchTests`, `KafkaGuardTests`, `AmazonSQSMessagePublisherBatchTests`, `AmazonSQSGuardTests`, `AmazonSQSPublisherContractTests` and `AmazonSQSPublisherIntegrationTests`; each pull request removes the method and updates those files together. The eight publisher interfaces have one implementer each (the concrete publishers, listed by `git grep ": I<Name>"`); there are no test doubles that implement them in `src` or `tests` (the same grep finds no other class), so adding a base interface breaks nothing. Every pull request adds production code with its own tests and builds alone in the order 1 to 9; each of 2 to 9 needs pull request 1.

---

## Combined AI Agent Prompts

<details>
<summary>Combined prompt for all phases</summary>

```text
PROJECT CONTEXT: Encina is a pre-1.0 .NET 10 / C# 14 library; no backward compatibility, zero warnings, ROP (Either<EncinaError, T>). Each transport has its own publisher interface; Kafka and SQS have partial batch methods (Kafka awaits each message; SQS drops failed entries into a log line); the other six have none.

IMPLEMENTATION OVERVIEW: PR1 IBatchPublisher, BatchPublishOptions, BatchPublishResult, BatchItemFailure and a shared contract test base in Encina.Messaging and ContractTests; PR2 to PR9 one transport each (Kafka pipelined produce with linger and batch size options, SQS chunked batches with per-entry results and a FIFO refusal, Azure Service Bus message batches, RabbitMQ confirmed publishing, NATS JetStream publishes, MQTT bounded concurrency, Redis.PubSub pipelined publishes, InMemory loop), each inheriting the contract in its own publisher interface, replacing the old partial methods and logging inside its existing EventId range. Dilemmas D1, D2 and D6 are implemented as the maintainer answers, the recommended options until then.

KEY PATTERNS: Partial failure is Right with Failures, never swallowed; Left only before the attempt; chunks sequential in input order; cancellation propagates; exception type only in logs and never EncinaError.Message; ADR-062 category on every code; each transport keeps its naming style; targets never go down; CRAP <= 10.

REFERENCE FILES: src/Encina.Kafka/KafkaMessagePublisher.cs, src/Encina.AmazonSQS/AmazonSQSMessagePublisher.cs, src/Encina.AzureServiceBus/AzureServiceBusMessagePublisher.cs, tests/Encina.ContractTests/MessageBrokers, tests/Encina.IntegrationTests/MessageBrokers, src/Encina/Diagnostics/EventIdRanges.cs.
```

</details>

---

## Cross-Cutting Integration Matrix

| # | Function | Status | Notes |
|---|----------|--------|-------|
| 1 | Caching | ❌ | Published messages are not cached. |
| 2 | OpenTelemetry | ⏭️ | Transport instrumentation (spans and metrics for send and receive) is #725; a batch publish will be one span there with a message count, and this plan adds no instrument of its own. |
| 3 | Structured Logging | ✅ | Per-transport log messages inside each existing EventId range (Event ID Allocation table); counts and error codes only. |
| 4 | Health Checks | ❌ | The eight transports already have health checks (for example `KafkaHealthCheck`, `AmazonSQSHealthCheck`); a batch call adds no dependency to check. |
| 5 | Validation | ⏭️ | Payload validation at the boundary is the validation family's work (#729 for notification handlers); a batch publishes what the caller validated, and the options are validated by `Validate()`. |
| 6 | Resilience | ⏭️ | Retry, timeouts and circuit breaking for transports are #742; the contract reports per-message failures so a policy can retry only the failed indexes. |
| 7 | Distributed Locks | ❌ | The consumer handles locking; publishing needs no lock. |
| 8 | Transactions | ❌ | Transports are acknowledgement-based, as the issue states; the "transaction support for batches" feature item is not provided (a broker transaction would be a per-broker feature of its own). |
| 9 | Idempotency | ❌ | The consumer handles idempotency (inbox); the NATS JetStream deduplication of single publishes keeps working for each message of a batch. |
| 10 | Multi-Tenancy | ❌ | The batch publisher adds and reads no tenant; a message carries whatever metadata the single publish adds today (none for tenancy), and stamping outgoing messages with a tenant is the caller's or the outbox's concern (#737 for outbox messages). |
| 11 | Module Isolation | ❌ | Publishing is not module-scoped; no module identifier exists in the transport publishers. |
| 12 | Audit Trail | ❌ | Transport publishing is infrastructure; audit of message flows belongs to the outbox and inbox audit work (#749). |

---

## Prerequisites & Dependencies

- None blocking for pull request 1. Pull requests 2 to 9 depend on pull request 1 and are otherwise independent of each other, so they can be reviewed in any order; the recommended order is Kafka, SQS, Azure Service Bus (the issue's transports), then RabbitMQ, NATS, MQTT, Redis.PubSub and InMemory.
- **Related, not blocking**: #121 (a batch notification handler calls `PublishBatchAsync`), #2385 (outbox-to-transport publisher), #587 (claims Kafka EventIds 4156-4175), #725 (transport telemetry), #742 (transport resilience), #469 and #255 (partition keys and FIFO group ids), and the planned transports #237, #238, #239 and #241.

---

## Next Steps

1. Ask the maintainer D1, D2 and D6 when work on #252 starts; record the answers in `## Maintainer Decisions`.
2. Pull request 1: the contract and the shared contract test base (13 files).
3. Pull requests 2 to 4: Kafka (13 files), SQS (10), Azure Service Bus (10).
4. Pull requests 5 to 9: RabbitMQ (10), NATS (10), MQTT (11), Redis.PubSub (11), InMemory with the feature page and the justification files (13).
