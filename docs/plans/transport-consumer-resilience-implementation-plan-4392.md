# Implementation Plan: Consumer-side resilience for message transports

> **Issue**: [#4392](https://github.com/dlrivada/Encina/issues/4392)
> **Type**: Feature (follow-up of #742)
> **Complexity**: Very high (7 phases, 8 transports, 12 pull requests)
> **Estimated Scope**: ~2,300 lines of production code + ~3,400 lines of tests (about 5,700 changed lines in total, twelve pull requests)

---

## Summary

Give the transports a consumer-side resilience story: a circuit break around message consumption that pauses delivery where the broker allows it, and retry only of the broker operations that deserve it, consistent across the transport category. #742 covers only the outbound operations because the transport packages have no consumer abstraction to wrap. This plan adds that abstraction in `Encina.Messaging`, one implementation per transport (all eight), and one decorator in `Encina.Resilience` that uses the provider-keyed executor of #742. Immediate retry of a handler stays with the resilience pipeline of the request type (ADR-055 R4); delayed retry and the dead letter stay in `Encina.Messaging` Recoverability.

What exists today (read in `origin/main` at `0a7ce2ae1`):

- No consumer abstraction: `git grep -E "BasicConsume|ServiceBusProcessor|ReceiveMessageAsync|IMessageConsumer|IMessageTransport" -- src` returns no match; the only `IConsumer<` is the Debezium connector (`src/Encina.Cdc.Debezium/Kafka/DebeziumKafkaConnector.cs:42`). Subscription exists on two publishers only: `IMQTTMessagePublisher.SubscribeAsync<TMessage>(Func<TMessage, ValueTask> handler, string topic, MqttQualityOfService? qos, CancellationToken)` (`src/Encina.MQTT/IMQTTMessagePublisher.cs:39`) and `IRedisPubSubMessagePublisher.SubscribeAsync<TMessage>(Func<TMessage, ValueTask> handler, string? channel, CancellationToken)` (`src/Encina.Redis.PubSub/IRedisPubSubMessagePublisher.cs:34`), plus `IInMemoryMessageBus.Subscribe<TMessage>(Func<TMessage, ValueTask>)` returning `IDisposable` (`src/Encina.InMemory/IInMemoryMessageBus.cs:40`). Their handlers return no result, so a failing handler is invisible to a wrapper.
- Deduplication on the consumer side already keys on the message id: `InboxPipelineBehavior` uses `IRequestContext.IdempotencyKey` as the deduplication key (`src/Encina.Messaging/Inbox/InboxPipelineBehavior.cs:57-108`). The immediate-retry loop of Recoverability is `ExecuteWithImmediateRetriesAsync` (`src/Encina.Messaging/Recoverability/RecoverabilityPipelineBehavior.cs:325`); ADR-055 R4 and the amended ADR-050 move the immediate retry to the resilience pipeline of the request type and forbid several retry layers on one request.
- The plans of the planned transports #239, #241 and #242 list a separate consumer or subscriber interface per package (with a handler returning `ValueTask<Either<EncinaError, Unit>>` in the Pulsar and ActiveMQ cases) as the issue's alternative, and #240 recommends one interface with both publish and subscribe members (its choice, pending the maintainer); this plan's choice 1 is the category-wide answer they wait for.
- #742 (plan on `main`, not implemented): `IProviderResilience.ExecuteAsync`, `ProviderResilienceOptions`, the abstract contract suite, `AddEncinaProviderResilience`; its choice 4 defers consumers to this issue. `Encina.Resilience` does not exist in `src/` (#4380 plans it).

What this plan delivers: `IMessageConsumer`, `IConsumerSubscription`, `ConsumedMessageContext`, `ConsumerHandler<TMessage>` and `ConsumerState` in `Encina.Messaging`; one consumer implementation per transport (RabbitMQ, Kafka, NATS, MQTT, AzureServiceBus, AmazonSQS, Redis.PubSub, InMemory); `ResilientMessageConsumer` with its subscription wrapper, options, validator and registration in `Encina.Resilience`; a shared contract suite over the eight; log messages and the pause instrument; and the tests.

- **Affected packages**: `Encina.Messaging` (abstraction), `Encina.Resilience` (planned; decorator), and the eight transport packages (`Encina.RabbitMQ`, `Encina.Kafka`, `Encina.NATS`, `Encina.MQTT`, `Encina.AzureServiceBus`, `Encina.AmazonSQS`, `Encina.Redis.PubSub`, `Encina.InMemory`). Ownership is stated here and in #4393 in the same words: this plan owns the abstraction, the `ResilientMessageConsumer` decorator and the consumers of these eight transports; #4393 owns, for each of the six planned transports (GoogleCloudPubSub, AmazonEventBridge, Pulsar, Redis.Streams, ActiveMQ, Dapr.PubSub), the publisher decorator and the adoption of `IMessageConsumer` with its `AddEncina<Transport>ConsumerResilience()` registration, as a second pull request per transport that waits for PR-1 to PR-4 of this plan.
- **Provider category**: Transports (AGENTS.md section 5): every transport supports the decorator, proved by one shared contract suite.
- **Estimated size**: 12 pull requests of at most 15 files and 1,500 lines (split in "Implementation Phases").
- **Prerequisites**: #742 implementation, #4380, #4382, described in "Prerequisites & Dependencies".

### Shared Pieces and Ownership (family resilience follow-ups: #4391, #4392, #4393, #4394, #4395)

| Shared piece | Owner | Used by |
|--------------|-------|---------|
| `Encina.Resilience` project, registry, options front-ends | #4380 (plan, by number) | this plan |
| `RequestHandlerCallback<TResponse>(CancellationToken)` and the deadline | #4382 (plan, by number) | handlers dispatched by consumers |
| `IProviderResilience`, `ProviderResilienceOptions`, the abstract contract suite, `ResilienceProvider` range 970-984 | #742 (plan on `main`, by number) | this plan |
| `IMessageConsumer`, `IConsumerSubscription`, `ConsumedMessageContext`, `ConsumerHandler<TMessage>`, `ConsumerState` | **This plan (#4392)** | #4393 (owns the six planned transports' consumer adoption PRs), #239, #240, #241, #242 |
| `ResilientMessageConsumer`, `ConsumerResilienceOptions` | **This plan (#4392)** | none |
| One publisher decorator and one consumer adoption per planned transport; the ADR-055 R7 amendment (covers both directions) | #4393 (plan, by number) | the consumer adoption PRs wait for this plan's PR-1 to PR-4 |
| Telemetry names | #728 (plan on `main`; choices 1 and 8 pending) and #725 (transport telemetry) | Phase 5 |

---

## Design Choices

<details>
<summary><strong>1. The consumer abstraction: one contract implemented by every transport, handler callbacks only, or each SDK's own retry</strong></summary>

AGENTS.md section 5 requires a feature that touches the transport category to be consistent across every provider; Inbox consumption is part of that matrix.

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `IMessageConsumer` in `Encina.Messaging`, implemented by all eight transports (registered by provider key), decorated once by `Encina.Resilience`** | One decorator and one contract suite; the six planned transports adopt it; handler results (`Either`) become visible | Eight new consumer implementations; new public API in `Encina.Messaging` |
| **B) Wrap the handler callbacks of `SubscribeAsync` on MQTT and Redis.PubSub only** | Small now | Two transports only, so the category is inconsistent; the handler returns no result, so failures are invisible; a breaker around a push callback cannot pause the broker |
| **C) Leave consumption to each SDK's retry** | No code | Behavior differs per SDK; ADR-029 classification is not applied |

### Chosen Option: **A - One abstraction, decorated once** (recommended, pending the maintainer)

### Rationale

- A wins for category coherence; B wins if only the two transports that already subscribe matter before 1.0; C wins if the maintainer accepts per-SDK behavior and a documented table. Registration uses keyed services by provider key (`AddKeyedSingleton<IMessageConsumer, KafkaMessageConsumer>("kafka")`), so several transports coexist in one container; each transport has `AddEncina<Transport>Consumer()` and `AddEncina<Transport>ConsumerResilience()` (order-independent, the shape of #742 choice 8).
- Under B or C choices 2 to 5 are re-planned for callbacks or dropped.

- **Class:** Dilemma

</details>

<details>
<summary><strong>2. Delivery model: a push handler delegate, or a pull stream</strong></summary>

The transports differ: RabbitMQ, ASB, MQTT, Redis.PubSub and NATS deliver by callback; Kafka and SQS are poll loops that each SDK wraps; the existing `SubscribeAsync` methods are push with a `Func<TMessage, ValueTask>` handler, and the planned Pulsar and ActiveMQ alternatives return `ValueTask<Either<EncinaError, Unit>>`.

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Push: `SubscribeAsync<TMessage>(string source, ConsumerHandler<TMessage> handler, CancellationToken)` returns `Either<EncinaError, IConsumerSubscription>`; the delegate returns `ValueTask<Either<EncinaError, Unit>>` (Right acknowledges, Left asks the transport for its native negative acknowledgement)** | Matches the two existing subscribers; the handler result is visible; poll loops stay inside the SDK adapters | A poll-based transport buffers inside its adapter |
| **B) Pull: `IAsyncEnumerable<ConsumedMessage<TMessage>>` with explicit `AckAsync` and `NackAsync`** | Pausing is natural (stop pulling); backpressure is explicit | Push brokers need an internal buffer; a bigger contract change for MQTT and Redis.PubSub; two ack calls per message |

### Chosen Option: **A - Push handler delegate** (recommended, pending the maintainer)

### Rationale

- A wins when most transports are push and the handler is the unit of work; B wins when backpressure and batching dominate (Kafka-first applications). `ConsumedMessageContext` carries the provider key, the source name, the broker message id (null when the broker has none), the delivery attempt when the broker reports it, and the headers (so the dispatching code can restore the idempotency key, trace context and tenant metadata); it carries no message body.

- **Class:** Dilemma

</details>

<details>
<summary><strong>3. What the decorator wraps: broker operations with retry and breaker, or handler observation only</strong></summary>

ADR-055 R7 says decorators cover operations ("publish, consume, call, shard query") and connection recovery stays inside each SDK. On the consumer side the operations are the broker-facing ones (receive or poll, acknowledge, negative-acknowledge, commit an offset, resume) and the handler invocation.

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) The acknowledgement, negative acknowledgement, commit and resume operations run through `IProviderResilience.ExecuteAsync` with retry and the breaker (retry only errors classified retryable, ADR-029); the handler invocation runs through the same breaker with retry off, so its transient failures are counted** | A lost acknowledgement is retried; one breaker per provider sees broker faults and handler failures; no second retry on the handler | Four operation kinds to map per transport; needs a way to run an operation through the executor without retry (the mechanism #742 uses for request/reply, its choice 11) |
| **B) Only observe: handler outcomes feed the breaker through the executor with retry off; broker operations are left to the SDK** | Smaller; fewer operation kinds | Broker operation failures are not retried or counted; behavior of ack failures differs per SDK |

### Chosen Option: **A - Broker operations and handler outcomes** (recommended, pending the maintainer)

### Rationale

- A wins when a lost acknowledgement or a failed commit matters (duplicate processing, an offset that never advances); B wins when each SDK's own handling of those operations is judged enough. The retry-off mechanism is whichever form the #742 implementation gives (an operation kind or a per-operation setting); this plan adds none of its own.

- **Class:** Dilemma

</details>

<details>
<summary><strong>4. What an open breaker does to delivery</strong></summary>

When the breaker opens because the handler keeps failing with transient errors or the broker keeps failing, continuing to receive messages only feeds a failing downstream.

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Pause delivery where the transport can (`IConsumerSubscription.SupportsPause`, `PauseAsync`, `ResumeAsync`), for the break duration, then resume and let the next message act as the probe; where it cannot pause, the decorator does not invoke the handler and returns a transient `Left` so the transport applies its negative acknowledgement** | The broker holds the messages; no load on the failing dependency | A transport whose protocol has no negative acknowledgement at the chosen quality of service (MQTT QoS 0) can lose the message: the per-provider table records it |
| **B) Never pause or shed; the breaker only records and logs** | No message is refused | A failing dependency keeps receiving traffic; the breaker has no effect on consumption |

### Chosen Option: **A - Pause where possible, otherwise refuse by negative acknowledgement** (recommended, pending the maintainer)

### Rationale

- A wins when downstream protection matters more than throughput; B wins when losing a message is worse than overloading a dependency and the transport cannot hold it. The resume delay uses `TimeProvider.CreateTimer`; the timer callback catches every exception, logs it by error code or `ForLogging()` and never throws (AGENTS.md section 3: a timer callback that throws terminates the process with the system clock). A cancelled subscription token stops the timer and is never reported as a failure.

- **Class:** Dilemma

</details>

<details>
<summary><strong>5. The consumer layer never retries the handler</strong></summary>

A handler that dispatches into `IEncina` already runs through the resilience pipeline of its request type, where immediate retry lives (ADR-055 R4). A second retry in the consumer layer would stack two layers on one request.

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) The consumer decorator never retries a handler invocation; it counts the outcome and returns it** | One retry layer per request; the registration conflict check of R4 has nothing to detect here | A handler that does not dispatch through a retried request type gets no immediate retry from this layer (its transport's redelivery and Recoverability apply) |
| **B) The consumer decorator retries the handler with the provider pipeline** | Immediate retry for every handler | Two retry layers on the same request: a registration error by ADR-050 |

### Chosen Option: **A - No handler retry in the consumer layer**

### Rationale

- B contradicts ADR-050 and ADR-055 R4; A is the only option that complies. It holds under every option of choices 1 to 4 (the decorator, whatever its operations, never retries the handler).
- **Class:** Settled
- **Settled by:** ADR-055 R4; ADR-050 resilience row ("Several retry layers on the same request" is a registration error)

</details>

<details>
<summary><strong>6. A cancellation is never counted as a breaker failure</strong></summary>

The breaker sees handler outcomes and broker operations; a host shutdown or a caller that leaves cancels tokens. What counts among the other outcomes is choice 9.

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) A cancellation (an `OperationCanceledException` of the subscription token, or a `Left` of category `Cancelled`) is never counted and is never turned into a `Left` of this layer: the exception propagates** | A host shutdown never opens the breaker | None |
| **B) Count a cancellation as a failure** | Simple | A shutdown opens the breaker; contradicts ADR-062 R5 |

### Chosen Option: **A - Cancellation never counted**

### Rationale

- B contradicts ADR-062 rule R5; A is the only option that complies. It holds under every option of choices 1 to 4 and 9.
- **Class:** Settled
- **Settled by:** ADR-062 rule R5 (`*.cancelled` is `Cancelled`, never counted as a server fault)

</details>

<details>
<summary><strong>7. Message id and the Inbox</strong></summary>

`InboxPipelineBehavior` deduplicates on `IRequestContext.IdempotencyKey` (`InboxPipelineBehavior.cs:57-108`); a broker message carries an id only when its publisher assigned one or the broker generates one.

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `ConsumedMessageContext.MessageId` carries the broker message id (the id the publisher assigned, null if the broker has none); the dispatching code sets the idempotency key from it; the consumer layer does no deduplication itself** | One deduplication owner (the Inbox); no state in the consumer | A message with no id is not deduplicated |
| **B) The consumer layer keeps a seen-ids window** | Works without an Inbox | A second, in-memory deduplication path that disagrees with the Inbox across restarts and instances |

### Chosen Option: **A - Surface the id; the Inbox deduplicates** (recommended, pending the maintainer)

### Rationale

- A wins because the Inbox is the idempotent-consumption pattern of AGENTS.md section 3 (Inbox for external messages) and survives restarts and several instances; B wins for an application with no Inbox that accepts in-process deduplication only. Under A a message with no id is not deduplicated.

- **Class:** Dilemma

</details>

<details>
<summary><strong>8. Tests per transport</strong></summary>

AGENTS.md section 5 asks every transport to behave the same and section 9 asks integration classes to use shared `[Collection]` fixtures, never a per-class fixture.

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) One abstract contract suite over a fake inner consumer, one derived class per transport, and one integration class per broker on its collection fixture (consume after a broker restart; the breaker pauses and resumes)** | Proves consistency and the real SDK behavior | Containers in CI |
| **B) A contract suite only, with a justification file per transport for the skipped integration class (AGENTS.md section 9: integration tests of a non-database feature are "justify if skipped")** | Fast; no containers beyond the existing broker tests | Pause, resume and acknowledgement behavior of the real SDKs is unproven by a test |

### Chosen Option: **A - Contract and integration per transport** (recommended, pending the maintainer)

### Rationale

- A wins when the per-provider pause and acknowledgement facts must be proven against the real SDK (AGENTS.md section 5 asks every transport to behave the same); B wins when CI time matters and the contract suite is judged enough. Integration classes use shared `[Collection]` fixtures, never a per-class fixture (AGENTS.md section 9).

- **Class:** Dilemma

</details>

<details>
<summary><strong>9. What else counts as a breaker failure on a consumer</strong></summary>

Besides the cancellation rule of choice 6, the breaker needs a rule for handler `Left`s and broker faults. ADR-029 classifies a failure as Transient, Permanent or Unknown for retry and dead-lettering; it does not say how a breaker counts them.

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Count broker faults and handler `Left`s that `IErrorClassifier` classifies Transient; Permanent and Unknown outcomes are not counted** | A business rejection or an unclassified error never opens the breaker | A dependency failing with errors the classifier calls Unknown is never protected |
| **B) Count Transient and Unknown (ADR-029 treats Unknown conservatively); not Permanent** | Protects against unclassified dependency failures | An unclassified bug can open the breaker and pause consumption |
| **C) Count every `Left` that is not a cancellation** | Simple | A validation error opens the breaker |

### Chosen Option: **A - Transient only** (recommended, pending the maintainer)

### Rationale

- A wins when the classifier is well maintained for the application's errors; B wins when most dependency failures arrive as exceptions the default classifier cannot place; C wins only for an application with no business `Left` on the consume path.

- **Class:** Dilemma

</details>

---

## Implementation Phases

Twelve pull requests, in order: **PR-1** Phase 1 (the contracts in `Encina.Messaging`); **PR-2** Phase 2 and the log messages of Phase 5 (decorator, subscription wrapper, options, validator, registration and the five log definitions in `Encina.Resilience`, because the resume-timer callback must log its failure; it needs the #742 executor); **PR-3** the paused-subscriptions gauge of Phase 5 (it modifies the PR-2 decorator and subscription wrapper and their tests); **PR-4** the abstract contract suite, the fake consumer and the InMemory consumer (Phase 3, the transport that needs no infrastructure); **PR-5 to PR-11** one transport each, in the order RabbitMQ, Kafka, NATS, MQTT, AzureServiceBus, AmazonSQS, Redis.PubSub (Phase 3); **PR-12** Phase 7 (documentation). Every PR needs the foundation and #4382 merged; PR-2 onward needs the #742 implementation.

### Phase 1: Core Models and Contracts

<details>
<summary>Tasks</summary>

1. `src/Encina.Messaging/Consumers/IMessageConsumer.cs`: `string Provider { get; }`; `ValueTask<Either<EncinaError, IConsumerSubscription>> SubscribeAsync<TMessage>(string source, ConsumerHandler<TMessage> handler, CancellationToken cancellationToken) where TMessage : class`.
2. `ConsumerHandler.cs`: `public delegate ValueTask<Either<EncinaError, Unit>> ConsumerHandler<in TMessage>(TMessage message, ConsumedMessageContext context, CancellationToken cancellationToken) where TMessage : class;`.
3. `ConsumedMessageContext.cs` (sealed record: `Provider`, `Source`, `MessageId`, `DeliveryAttempt`, `Headers`; a factory with `ArgumentException.ThrowIfNullOrWhiteSpace` on `Provider` and `Source` and `ArgumentNullException.ThrowIfNull` on `Headers`).
4. `IConsumerSubscription.cs` (`ConsumerState State { get; }`, `bool SupportsPause { get; }`, `ValueTask<Either<EncinaError, Unit>> PauseAsync(CancellationToken)`, `ResumeAsync(CancellationToken)`, `IAsyncDisposable`) and `ConsumerState.cs` (`Active`, `Paused`, `Stopped`).
5. New error codes (every one has a named ADR-062 category and a constant message; no `EncinaError.Message` reaches a log): `encina.consumer.pause_unsupported`, returned by `PauseAsync` and `ResumeAsync` of a subscription whose `SupportsPause` is false (callers check the flag first; category `Internal`, ADR-062 R4: a capability gap of a provider is never 501, and R2 forbids `FailedPrecondition` before #3420); and in each provider PR two codes in the style of the publisher code `MQTT_PUBLISH_FAILED` (`src/Encina.MQTT/MQTTMessagePublisher.cs:101`), both with a constant message that names neither the source nor the broker address: `<TRANSPORT>_SUBSCRIBE_FAILED` (for example `KAFKA_SUBSCRIBE_FAILED`), returned by `SubscribeAsync` when the broker cannot be reached or times out, category `Unavailable` (the ADR-062 row "503, retry later": a dependency outage the retry classifier treats as transient); and `<TRANSPORT>_SUBSCRIBE_REJECTED` (for example `KAFKA_SUBSCRIBE_REJECTED`), returned when the broker refuses the subscription because of the application's own configuration or credentials (a wrong topic, a refused credential, a missing permission), category `Internal` (ADR-062 R4: a provider or deployment fault is `Internal`, and retrying cannot fix it). The one-per-transport constants live in the transport package.
6. `ConsumerErrorCodes.cs` (public static class in `Encina.Messaging/Consumers/`, holding `PauseUnsupported = "encina.consumer.pause_unsupported"`, the code of task 5; the per-transport codes are constants of each transport package).
7. Tests in the same PR: unit and guard tests of `ConsumedMessageContext`, and a unit test that `ConsumerErrorCodes.PauseUnsupported` has the documented text; `PublicAPI.Unshipped.txt` and the manifest (`mechanical-fixer`).

</details>

<details>
<summary>Prompt for AI Agents — Phase 1</summary>

```text
CONTEXT: Encina issue #4392, plan docs/plans/transport-consumer-resilience-implementation-plan-4392.md. The maintainer has answered choices 1 and 2.
TASK: Create the consumer contracts in Encina.Messaging with unit and guard tests.
KEY RULES: Either<EncinaError, T> on every operation; no message body in the context; guards on the factory; XML docs with <example>; PublicAPI lines through mechanical-fixer; the abstraction lives in Encina.Messaging and has no provider-specific code.
REFERENCE FILES: src/Encina.MQTT/IMQTTMessagePublisher.cs, src/Encina.Messaging/Inbox/InboxPipelineBehavior.cs, docs/architecture/adr/062-error-category-contract.md
```

</details>

### Phase 2: Default Implementation and Decorator

<details>
<summary>Tasks</summary>

1. `src/Encina.Resilience/Consumers/ConsumerResilienceOptions.cs`: per provider `PauseOnOpenCircuit` (default true), the break duration read from the provider's breaker options of #742 (no second value), `HandlerTimeout` (null: none); validated at startup (`ConsumerResilienceOptionsValidator.cs`, internal).
2. `ResilientMessageConsumer.cs` (public sealed, implements `IMessageConsumer`; constructor `(IMessageConsumer inner, IProviderResilience resilience, IOptions<ConsumerResilienceOptions> options, TimeProvider timeProvider, ILogger<ResilientMessageConsumer> logger)`, all null-guarded): `SubscribeAsync` wraps the handler (choices 3 to 6): each invocation goes through `ExecuteAsync(provider, "consume", ...)` with retry off; an open circuit makes the decorator pause the subscription (choice 4) or return a transient `Left` without invoking the handler; the acknowledgement, negative acknowledgement, commit and resume operations that the transport exposes to the decorator run through the executor with retry.
3. `ResilientConsumerSubscription.cs` (internal sealed): wraps `IConsumerSubscription`; owns the resume timer (`TimeProvider.CreateTimer`, callback catches everything, logs it with `ConsumerResumeTimerFailed` from the log class that ships in this same PR (Phase 5 task 1), and never throws); disposal stops the timer first.
4. `ConsumerResilienceServiceCollectionExtensions.cs` (public static): `AddEncinaConsumerResilience(this IServiceCollection services)` registers the options and validator with `TryAdd*`; `ArgumentNullException.ThrowIfNull(services)`; the DI test builds the provider with `ValidateOnBuild` and `ValidateScopes`.

</details>

<details>
<summary>Prompt for AI Agents — Phase 2</summary>

```text
CONTEXT: Phase 1 merged; the #742 executor is merged.
TASK: Implement the options, the decorator, the subscription wrapper and the registration.
KEY RULES: the decorator never retries the handler; a cancellation propagates and is never counted or returned as a Left; the counting rule of choice 9 as the maintainer answers it (recommended: transient failures by IErrorClassifier only); the resume timer callback catches every exception and never throws; time only through TimeProvider; no scoped service in a singleton; no tenant key.
REFERENCE FILES: docs/plans/transport-resilience-implementation-plan-742.md, src/Encina/Recoverability/IErrorClassifier.cs, docs/en/guides/TESTING.md (timer callbacks must not throw)
```

</details>

### Phase 3: Provider Implementations (the Consumers)

<details>
<summary>Tasks</summary>

For each of the eight transports (PR-4 for InMemory, PR-5 to PR-11 for the others), in its package:

1. `<Transport>MessageConsumer.cs` (public sealed, implements `IMessageConsumer`; `Provider` is the fixed lowercase key of the package: `rabbitmq`, `kafka`, `nats`, `mqtt`, `azureservicebus`, `amazonsqs`, `redispubsub`, `inmemory`): subscribes through the SDK, builds a `ConsumedMessageContext` per delivery (headers, id, attempt), calls the handler, acknowledges on `Right` and applies the SDK's negative acknowledgement on `Left`; `SupportsPause` and the pause mechanism are verified per SDK by the author of the provider PR before it is opened (the checklist in "Research"; nothing about an SDK is assumed here); an `OperationCanceledException` of the subscription token ends the delivery without a `Left`.
2. `ServiceCollectionExtensions.cs` (modified): `AddEncina<Transport>Consumer()` (keyed registration) and `AddEncina<Transport>ConsumerResilience()` (order-independent wrap by the decorator when the marker is present).
2b. `Encina.<Transport>.csproj` (modified): a `ProjectReference` to `Encina.Resilience` for the resilience extension, unless the #742 decorator PR of the same transport already added it. This edge makes every transport package that offers the extension take Polly transitively, the shape #742 choice 8 option A accepts; counted in each transport PR below.
3. SDK facts the author of the provider PR answers before opening it (a prerequisite, not an open item of this plan): whether the SDK redelivers by itself and how to turn that off where Encina retries (ADR-050), the pause mechanism, the exception for a closed broker. Connection recovery stays inside each SDK and is documented per provider (ADR-055 R7).
4. Tests in the same PR: the consumer unit test, the guard test, the DI test, a class derived from the abstract contract suite (PR-4 adds the suite with the InMemory consumer) and the integration class on the broker's collection fixture (the existing ones per #742 Phase 6 task 5; a missing fixture for RabbitMQ, Kafka or MQTT is added in that PR and counted).

</details>

<details>
<summary>Prompt for AI Agents — Phase 3</summary>

```text
CONTEXT: Phases 1 and 2 merged. Transport in this PR: <name>.
TASK: Add the consumer, its registrations and tests for the transport, and verify its SDK redelivery and pause behavior.
KEY RULES: the consumer has a public constructor taking only public types; Right acknowledges and Left asks for the native negative acknowledgement; no message body in logs; cancellation ends delivery without a Left; keyed registration by the fixed provider key; registration order-independent and proven with ValidateOnBuild and ValidateScopes; nothing changes for an application that does not call the consumer extension; record only SDK facts the PR verified.
REFERENCE FILES: src/Encina.Kafka/ServiceCollectionExtensions.cs, tests/Encina.IntegrationTests/MessageBrokers/, docs/plans/transport-resilience-implementation-plan-742.md (Phase 3)
```

</details>

### Phase 4: Cross-Cutting Integration

<details>
<summary>Tasks</summary>

1. Idempotency (function 9): choice 7; the dispatching code sets `IdempotencyKey` from `MessageId`.
2. Multi-tenancy (function 10): the decorator takes no tenant and keys nothing by it (the breaker is per provider: broker connectivity is shared, so no tenant-qualified key and no reserved `default` tenant segment exist); tenant metadata travels in `Headers` for the dispatching code; what happens to a message that arrives without tenant metadata under multi-tenancy is a gap this plan does not close: no component in `src/` denies an inbound message for a missing tenant (the issue says "follows the transport's existing rule", and no transport consumes today; the only mention of a tenancy gate in the pipeline code is a documentation example, `src/Encina/Pipeline/Ordering/PipelineStage.cs:24`). The tenant and module headers of inbound messages are designed by #1164 and #2385 and carried out for the new transports by #4363; this layer neither reads nor enforces them, and the `Headers` of the context are how that future code reads them.
3. Health checks: the existing transport checks stay; no breaker-state check is planned (the breaker is per process).

</details>

<details>
<summary>Prompt for AI Agents — Phase 4</summary>

```text
CONTEXT: Phases 1 to 3 done.
TASK: Verify the cross-cutting rows of the plan and add only what they need.
KEY RULES: no tenant key; no new health check; headers carry metadata, the body never reaches logs.
REFERENCE FILES: docs/architecture/adr/054-observability-conventions-and-trace-propagation.md
```

</details>

### Phase 5: Observability

<details>
<summary>Tasks</summary>

1. `Diagnostics/ConsumerResilienceLogMessages.cs`: `ConsumerPaused` (Warning: provider, source), `ConsumerResumed` (Information), `ConsumerHandlerRefused` (Warning: provider, source; handler not invoked), `ConsumerSubscriptionFailed` (Error: error code only), `ConsumerResumeTimerFailed` (Error: exception through `ForLogging()`), with EventIds 976 to 980 in that order, inside the range `ResilienceProvider` (970-984) that the #742 plan registers in its PR-2 (970-975 used, 976-984 spare); a search of the registry rows 950-999 and the sibling plans #4391 and #4395 finds no other claim on 976-980. They are valid once the #742 range exists in `src/Encina/Diagnostics/EventIdRanges.cs`; this plan creates no range. The log class ships in PR-2 with the decorator, because the resume-timer callback logs its failure.
2. Instruments (PR-3): Polly's `resilience.polly.*` metrics cover the strategies (ADR-055 R5); Encina adds only the number of paused subscriptions (an observable gauge), named by the convention #728 settles; tenant tag only with multi-tenancy (ADR-054 D4); no activation switch.

</details>

<details>
<summary>Prompt for AI Agents — Phase 5</summary>

```text
CONTEXT: Phases 1 to 4 done.
TASK: Add the log messages and the paused-subscriptions gauge.
KEY RULES: [LoggerMessage] source generator; ids packed without gaps inside a range that is registered first (ADR-021); never message bodies, ids or EncinaError.Message in logs; exceptions through ForLogging(); names from #728.
REFERENCE FILES: src/Encina/Diagnostics/EventIdRanges.cs, docs/engineering/eventid-block-registry.md
```

</details>

### Phase 6: Testing

<details>
<summary>Tasks</summary>

1. Unit (`tests/Encina.UnitTests/Resilience/Consumers/` and per transport; `FakeTimeProvider`): the decorator pauses after the threshold and resumes after the break; a transport without pause gets the refusal path; a cancellation is never counted and, under the recommended counting rule of choice 9, neither is a Permanent or Unknown handler `Left`; the handler is never retried; the timer callback survives a throwing resume (it logs and does not throw, per the fake-clock rule of the testing guide); the consumer of each transport acknowledges on `Right` and negatively acknowledges on `Left`.
2. Guard (`tests/Encina.GuardTests/`): `ResilientMessageConsumer` constructor nulls, `SubscribeAsync` null or blank `source` and null `handler`, `AddEncinaConsumerResilience` null `services`, each transport consumer constructor and extension methods; a nullable parameter that accepts null has a valid-result test, not an exception test.
3. Contract (`tests/Encina.ContractTests/Messaging/`): the abstract `MessageConsumerContractTests` and a generic `FakeMessageConsumer` (PR-4) and one derived class per transport: each decorated consumer, driven by a handler that fails transiently, pauses or refuses, resumes and returns handler results. No per-transport fake is needed: the failure comes from the handler, and broker faults are covered by the integration classes. The contract and integration rows of `ResilientMessageConsumer.cs` are written in PR-4 and in the first broker PR (PR-5) respectively, the PRs that add those tests.
4. Integration (`tests/Encina.IntegrationTests/MessageBrokers/<Transport>/`, collection fixtures, `[Trait("Category", "Integration")]`): per broker, consumption resumes after a broker restart and the breaker pauses while the dependency fails; InMemory has no infrastructure, so its integration target is 0 with that reason.
5. Property, load, benchmark: property tests of the pause state machine are not planned (a handful of states, covered by unit tests); load and benchmark justification files (the decorator adds one executor call per message).

</details>

<details>
<summary>Prompt for AI Agents — Phase 6</summary>

```text
CONTEXT: Phases 1 to 5 done.
TASK: Write the Phase 6 tests.
KEY RULES: tests execute real package code; FakeTimeProvider, never Thread.Sleep; collection fixtures only (never IClassFixture, never new fixtures per class); Shouldly through Encina.Testing.Shouldly; a test never swallows an exception thrown by the code it drives.
REFERENCE FILES: tests/Encina.IntegrationTests/MessageBrokers/, docs/en/guides/TESTING.md
```

</details>

### Phase 7: Documentation and Finalization

<details>
<summary>Tasks</summary>

1. `docs/features/transport-consumer-resilience.md` and the `Encina.Resilience` and `Encina.Messaging` README sections (`docs-writer`): the abstraction, the pause table per provider (only what the PRs verified), at-least-once and the Inbox, what is not covered (gRPC and GraphQL, #745).
2. `changelog.d/4392-transport-consumer-resilience.added.md` (and one fragment per provider PR), `PublicAPI.Unshipped.txt`, manifest rows (`mechanical-fixer`).
3. Verification: `dotnet build` 0 warnings; filtered tests; EventId architecture tests; `--check-justifications`.

</details>

<details>
<summary>Prompt for AI Agents — Phase 7</summary>

```text
CONTEXT: Phases 1 to 6 green.
TASK: Documentation and verification.
KEY RULES: never edit CHANGELOG.md; documentation by docs-writer, metadata by mechanical-fixer; the per-provider table contains only what the PRs verified; no hand-typed coverage figures (cite covref markers).
REFERENCE FILES: changelog.d/README.md, .claude/skills/encina-docs/SKILL.md
```

</details>

---

## Research

### Relevant Standards and Specifications

| Source | Relevance |
|--------|-----------|
| ADR-055 R4, R5, R7 | One retry owner; telemetry; decorators over operations; connection recovery stays in the SDK |
| ADR-050 (resilience row), ADR-029 | One retry layer; classification of failures |
| ADR-062 R2, R5 | Categories; cancellation is never a fault |
| ADR-054 D4 | Tenant tag only with multi-tenancy |
| AGENTS.md section 3 (timers never throw, Inbox, `EncinaError.Message`), section 5 (transports), section 9 (collection fixtures) | Rules applied in choices 4, 7 and 8 |
| MassTransit retry and circuit-breaker filters, NServiceBus recoverability | Comparable products named by the issue |

### SDK facts: a stated prerequisite of each provider pull request

This plan states no SDK fact: the eight consumers do not exist, and what each SDK offers is read from the SDK version the package pins. Before opening the pull request of a transport, its author answers, from that SDK, five questions and records the answers in the package README and the page of #742: does the SDK redeliver by itself and how is that turned off where Encina retries (ADR-050); how does it pause delivery (or does it have no way, which sets `SupportsPause` to false); what is its negative acknowledgement and what does the broker do with a message never acknowledged; which exception does a closed broker raise; and, for MQTT, what a quality-of-service 0 subscription loses when the handler is refused. The answers decide the `SupportsPause` value and the refusal path of choice 4 for that transport; they are inputs to the pull request, not open items of the plan.

### Existing Encina Infrastructure to Leverage

| Component | Location | Usage |
|-----------|----------|-------|
| `IMQTTMessagePublisher.SubscribeAsync`, `IRedisPubSubMessagePublisher.SubscribeAsync`, `IInMemoryMessageBus.Subscribe` | `src/Encina.MQTT/`, `src/Encina.Redis.PubSub/`, `src/Encina.InMemory/` | The existing push shape (choice 2) |
| `InboxPipelineBehavior`, `IdempotencyKey` | `src/Encina.Messaging/Inbox/InboxPipelineBehavior.cs:57-108` | Deduplication (choice 7) |
| `RecoverabilityPipelineBehavior` | `src/Encina.Messaging/Recoverability/RecoverabilityPipelineBehavior.cs` | Delayed retry and dead letter stay |
| `IErrorClassifier` | `src/Encina/Recoverability/IErrorClassifier.cs` | Choice 9 |
| `IProviderResilience`, `ProviderResilienceOptions`, the abstract contract suite | #742 plan | The decorator's engine |
| `ResilientSecretReaderDecorator` | `src/Encina.Security.Secrets/` | The R7 precedent |

### Event ID Allocation

| Package | Range | Notes |
|---------|-------|-------|
| `Encina.Resilience` (`ResilienceProvider`, registered by #742 PR-2) | 976-980 | Five ids from the spare 976-984 of the #742 range; no registry row or sibling plan (#4391 uses 955-957, #4395 uses 917-919) claims them; no new range is created here |

### Estimated File Count

| PR | Files | Content |
|----|-------|---------|
| PR-1 | 11 | `IMessageConsumer.cs`, `ConsumerHandler.cs`, `ConsumedMessageContext.cs`, `IConsumerSubscription.cs`, `ConsumerState.cs`, `ConsumerErrorCodes.cs`, `ConsumedMessageContextTests.cs`, `ConsumedMessageContextGuardTests.cs`, `ConsumerErrorCodesTests.cs`, `PublicAPI.Unshipped.txt`, manifest rows |
| PR-2 | 14 | `ConsumerResilienceOptions.cs`, `ConsumerResilienceOptionsValidator.cs`, `ResilientMessageConsumer.cs`, `ResilientConsumerSubscription.cs`, `ConsumerResilienceServiceCollectionExtensions.cs`, `Diagnostics/ConsumerResilienceLogMessages.cs`, decorator unit test, subscription unit test (including the timer-failure log), validator unit test, DI test, guard test, log messages test, `PublicAPI.Unshipped.txt`, manifest rows; the `EventIdRanges.cs`, registry and allocation-test edits belong to #742 PR-2, which registers the range |
| PR-3 | 7 | the gauge class and its test, `ResilientMessageConsumer.cs` and `ResilientConsumerSubscription.cs` (modified, they record), their two tests (modified), manifest rows (the gauge class has no public API, so no PublicAPI file) |
| PR-4 | 12 | `InMemoryMessageConsumer.cs`, `ServiceCollectionExtensions.cs` (modified), `MessageConsumerContractTests.cs` (abstract), `FakeMessageConsumer.cs`, the InMemory derived class, consumer unit, guard and DI tests, `PublicAPI.Unshipped.txt`, manifest rows (including the contract row of `ResilientMessageConsumer.cs`), changelog fragment, `Encina.InMemory.csproj` (`ProjectReference` to `Encina.Resilience`) |
| PR-5 to PR-11 (each) | 12 | `<Transport>MessageConsumer.cs`, `ServiceCollectionExtensions.cs` and `Encina.<Transport>.csproj` (modified; the `ProjectReference` to `Encina.Resilience`), the consumer unit, guard and DI tests, the derived contract class, the integration class, `PublicAPI.Unshipped.txt`, manifest rows, README line, changelog fragment; plus a collection fixture and its collection definition for RabbitMQ, Kafka and MQTT if absent (14). PR-5 also writes the integration row of `ResilientMessageConsumer.cs` in the same manifest edit |
| PR-12 | 5 | feature page, two README sections, two justification `.md` (load, benchmark) |

Each PR is within 15 files and 1,500 lines. Implementers and doubles: `IMessageConsumer` is new; its implementers are the eight consumers and the test fakes of the contract suite. No existing interface changes and no `new` site of an existing class changes.

Per-file targets (new files have no row on `origin/main`; the eight transport `ServiceCollectionExtensions.cs` rows are quoted from their manifests):

| File | Unit | Guard | Contract | Property | Integration | Justification |
|------|------|-------|----------|----------|-------------|---------------|
| `IMessageConsumer.cs`, `ConsumerHandler.cs`, `IConsumerSubscription.cs`, `ConsumerState.cs` | 0 | 0 | 0 | 0 | 0 | Interfaces, a delegate and an enum: no executable code |
| `ConsumedMessageContext.cs` | 95 | 40 | 0 | 0 | 0 | One factory with three argument checks, a large share of its lines |
| `ConsumerResilienceOptions.cs`, `ConsumerResilienceOptionsValidator.cs` | 95 | 15 | 0 | 0 | 0 | Property bag and one test per rule; the guard flag reaches the validator's null check |
| `ConsumerErrorCodes.cs` | 90 | 0 | 0 | 0 | 0 | One constant, read by its unit test and the decorator tests |
| `ResilientMessageConsumer.cs` | 90 (PR-2) | 20 (PR-2) | 80 (row written in PR-4) | 0 | 50 (row written in PR-5) | Every branch of choices 3 to 6 and 9 with a fake clock and fake consumer in PR-2; guard reaches the constructor and `SubscribeAsync` checks in PR-2; the contract row is set by the PR that adds the suite (PR-4) and the integration row by the first PR that adds a broker class (PR-5) |
| `ResilientConsumerSubscription.cs` | 90 | 15 | 0 | 0 | 0 | Pause, resume, timer failure and disposal by unit test in PR-2; internal, constructed by the decorator, the guard flag reaches its constructor checks through the PR-2 guard tests |
| The paused-subscriptions gauge class (PR-3) | 85 | 0 | 0 | 0 | 0 | Listener-based unit test in PR-3; no public parameters |
| `ConsumerResilienceServiceCollectionExtensions.cs` | 85 | 40 | 0 | 0 | 0 | The null check is a large share of a short method; the DI test reaches the registrations |
| `ConsumerResilienceLogMessages.cs` | 85 | 0 | 0 | 0 | 0 | Generated code; log assertions with a fake logger (PR-2) |
| `<Transport>MessageConsumer.cs` (eight) | 90 | 20 | 70 | 0 | 60 (InMemory 0) | The SDK callbacks are driven by fakes in unit tests and by the broker in integration; guard reaches constructor checks; InMemory has no infrastructure |
| `ServiceCollectionExtensions.cs` of the transports (modified) | RabbitMQ 95, Kafka 95, NATS 85, Redis.PubSub 80, AzureServiceBus 95, AmazonSQS 85 (new row; package unit 55), MQTT 85 (new row), InMemory 85 (new row) | RabbitMQ 45, Kafka 38, NATS 55, Redis.PubSub 4, AzureServiceBus 95, AmazonSQS 30 (new row), MQTT 30 (new row), InMemory 30 (new row) | 0 | 0 | AzureServiceBus 90, Redis.PubSub 95, AmazonSQS 55, others 0 | Quoted from the manifests on `origin/main`, kept and never lowered: RabbitMQ unit 95 and guard 45; Kafka 95 and 38; NATS 85 and 55; Redis.PubSub unit 80, guard 4, integration 95; AzureServiceBus unit 95, guard 95, integration 90; AmazonSQS integration 55 (its row has no unit or guard value; package targets unit 55, guard 15). MQTT and InMemory have no per-file row (package targets: unit 55, guard 15), so rows are added for them and for the AmazonSQS unit and guard flags with the numbers shown |

CRAP of each added method is at most 10 (the decorator splits choices 3 to 6 and 9 into one small method each).

---

## Combined AI Agent Prompts

<details>
<summary>Combined prompt for all phases</summary>

```text
PROJECT CONTEXT: Encina (pre-1.0, .NET 10, C# 14). Issue #4392: consumer-side resilience for the 8 transports (ADR-055 R4, R7; ADR-050). AGENTS.md rules apply. The #742 executor, #4380 and #4382 are merged first; if missing, stop and report.

IMPLEMENTATION OVERVIEW: (1) IMessageConsumer and its contract types in Encina.Messaging; (2) ResilientMessageConsumer, subscription wrapper, options and registration in Encina.Resilience; (3) one consumer per transport with keyed registration and an order-independent resilience extension; (4) log messages and a paused-subscriptions gauge; (5) unit, guard, DI, contract (abstract suite plus one class per transport) and integration tests; (6) documentation and fragments.

KEY PATTERNS: Right acknowledges and Left asks for the native negative acknowledgement; the consumer layer never retries the handler; the counting rule of choice 9; cancellation propagates, is never counted and is never a Left; timer callbacks never throw; no message bodies or ids in logs; no tenant key; the Inbox deduplicates on the message id; specialists for PublicAPI, changelog, manifest and docs.

REFERENCE FILES: docs/plans/transport-resilience-implementation-plan-742.md, docs/architecture/adr/055-resilience-family.md, src/Encina.Messaging/Inbox/InboxPipelineBehavior.cs
```

</details>

---

## Cross-Cutting Integration Matrix

| # | Function | Status | Notes |
|---|----------|--------|-------|
| 1 | Caching | ❌ | Not applicable |
| 2 | OpenTelemetry | ✅ | Polly's instruments through R5, a paused-subscriptions gauge, tenant tag only with multi-tenancy; names from #728 |
| 3 | Structured Logging | ✅ | Five definitions, EventIds 976-980 of the #742 range |
| 4 | Health Checks | ⏭️ | Per-transport health checks exist or are planned separately (the transport plans); no breaker-state check here |
| 5 | Validation | ✅ | Options validated at startup |
| 6 | Resilience | ✅ | The feature |
| 7 | Distributed Locks | ❌ | Not applicable: the breaker is per process |
| 8 | Transactions | ❌ | Not applicable |
| 9 | Idempotency | ✅ | The Inbox deduplicates on the message id surfaced by the context (choice 7) |
| 10 | Multi-Tenancy | ✅ | Tenant tag only with multi-tenancy; headers carry tenant metadata for the dispatching code; no component of `src/` denies a tenant-less inbound message today (a gap owned by #1164, #2385 and #4363, see Phase 4); no tenant-qualified key here |
| 11 | Module Isolation | ❌ | Not applicable |
| 12 | Audit Trail | ❌ | Not applicable |

---

## Prerequisites & Dependencies

| Prerequisite | State | Effect |
|--------------|-------|--------|
| #742 implementation (executor, contract suite, `ResilienceProvider` range 970-984, registered by its PR-2) | Plan on `main`; not implemented | Blocks PR-2 onward; the executor must be able to run an operation without retry (its choice 11 needs the same) |
| #4380 (`Encina.Resilience`) and #4382 (token and deadline) | Plans on `main`; not implemented | Block #742, hence this plan |
| The maintainer's answers to choices 1 and 2 | Pending | Block PR-1 |
| #239, #240, #241, #242 (planned transports' consumer surfaces) | Plans on `main`; none implemented | #4393 owns their consumer adoption and consumer resilience registration (its consumer PRs wait for this plan's PR-1 to PR-4) |
| #728 and #725 (telemetry names and transport telemetry) | Plans on `main`; choices pending | Gauge name and tags |
| Integration fixtures for RabbitMQ, Kafka, MQTT | Counted in the provider PR if absent | #742 Phase 6 task 5 says the same |

---

## Next Steps

1. Commit this plan, open its PR and link it from #4392.
2. When #4392 is picked up, re-check the choices against today's code and ask the maintainer about the Dilemmas (choices 1 to 4, 7, 8 and 9).
3. Brief workers for PR-1 to PR-12 in order, after #4380, #4382 and the #742 implementation are merged.
