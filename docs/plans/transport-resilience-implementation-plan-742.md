# Implementation Plan: Resilience patterns for message transport providers (decorators from `Encina.Resilience`)

> **Issue**: [#742](https://github.com/dlrivada/Encina/issues/742)
> **Type**: Feature
> **Complexity**: Very high (7 phases, 8 transports, 11 pull requests)
> **Estimated Scope**: ~900 lines of production code + ~2,600 lines of tests (about 5,500 changed lines in total, eleven pull requests)

---

## Summary

Give every message transport retry, a circuit breaker and a timeout around its outbound operations, through one executor in `Encina.Resilience` and one thin decorator per transport, as decided by SPIKE #2691 R7 (option 7a, comments of 2026-10-24 on #742) and written in [ADR-055](../architecture/adr/055-resilience-family.md): a pipeline from the R2 registry keyed by provider name, one options model (R3), one telemetry (R5), and the SDK's own retry disabled wherever Encina retries (one retry layer, ADR-050). The comment of 2026-10-23 on #742 moves it into the 1.0 scope; the issue's `ITransportResiliencePolicy` is replaced by that model.

What exists today (read in `origin/main` at `0336d31c7`):

- **There is no `IMessageTransport`.** ADR-055 R7 says the decorators "wrap `IMessageTransport`", but `git grep "interface IMessageTransport"` over `src` returns nothing. Each transport has its own publisher interface with its own shape: `IKafkaMessagePublisher` (`ProduceAsync`, `ProduceBatchAsync`, `ProduceWithHeadersAsync`), `IRabbitMQMessagePublisher` (`PublishAsync`, `SendToQueueAsync`, `RequestAsync`), `INATSMessagePublisher` (`PublishAsync`, `RequestAsync`, `JetStreamPublishAsync`), `IMQTTMessagePublisher` and `IRedisPubSubMessagePublisher` (`PublishAsync`, `SubscribeAsync`, `SubscribePatternAsync`), `IAzureServiceBusMessagePublisher` (`SendToQueueAsync`, `PublishToTopicAsync`, `ScheduleAsync`, `CancelScheduledAsync`), `IAmazonSQSMessagePublisher` (`SendToQueueAsync`, `PublishToTopicAsync`, `SendBatchAsync`, `SendToFifoQueueAsync`) and `IInMemoryMessageBus` (`PublishAsync`, `EnqueueAsync`, `Subscribe`). Every operation method returns `ValueTask<Either<EncinaError, T>>` and takes a `CancellationToken`; the exceptions are the subscription methods (`SubscribeAsync` and `SubscribePatternAsync` return `ValueTask<IAsyncDisposable>`, `IInMemoryMessageBus.Subscribe` returns `IDisposable`), which this plan does not wrap (choice 4).
- The transports have **no consumer loop**: a search for `BasicConsume`, `ServiceBusProcessor` and `ReceiveMessageAsync` in `src` finds no match, and the only `IConsumer<` is the Debezium connector. MQTT and Redis.PubSub expose `SubscribeAsync` with a handler callback.
- Resilience of any kind: none in the eight packages (`git grep` of `MaxErrorRetry`, `RetryOptions` and `RetryPolicy` over them is empty). `EncinaKafkaOptions` exposes only `Acks`, `EnableIdempotence` and `MessageTimeoutMs`.
- All eight publishers swallow the caller's cancellation into a `Left`: seven have `catch (Exception ex)` with no `OperationCanceledException` branch (`src/Encina.{RabbitMQ,Kafka,MQTT,AzureServiceBus,AmazonSQS,Redis.PubSub,InMemory}`), and NATS has one only as `when (!cancellationToken.IsCancellationRequested)`, which excludes the caller's own cancellation (lines 101, 162 and 211 return a `Left`). A caller's cancellation therefore arrives as a `Left` such as `KAFKA_PRODUCE_FAILED`. This is recorded in the issue file `transport-publishers-cancellation-as-left` (read from code, nothing run).
- `Encina.Resilience` does **not exist in `src/`**, no open issue creates it, and R6 is not on main. The packages of the six planned transports (GoogleCloudPubSub, AmazonEventBridge, Pulsar, Redis.Streams, ActiveMQ, Dapr) are not on main either.

What this plan delivers: `IProviderResilience` and its executor (a provider-keyed pipeline from the registry: timeout, retry, breaker), `ProviderResilienceOptions` (`ForProvider("kafka", ...)`), one public decorator per transport (eight), the registration per transport, six `[LoggerMessage]` definitions, a shared contract suite that runs every decorator against a fault-injecting fake, and per-broker integration tests. #744 (cloud functions) does not use it (its adapters are inbound and call nothing); #745 (gRPC and GraphQL) and #643 (shards) may.

- **Affected packages**: `Encina.Resilience` (planned), the eight transport packages, `Encina` core (`EventIdRanges.cs` only). The issue also names `Encina.Messaging` and `Encina.Polly`: neither gets code (choices 2 and 7).
- **Provider category**: Transports (AGENTS.md section 5). The matrix is in "Research": eight on main, two carved to #745 (choice 3), six planned ones through the issue file `transport-resilience-planned-transports`.
- **Estimated size**: 11 pull requests of at most 15 files and 1,500 lines, split in "Implementation Phases".
- **Prerequisites**: the foundation, R6 and #335's timeout mapper, described in "Prerequisites & Dependencies".

### Shared Pieces and Ownership (family resilience-patterns-1 and resilience-patterns-2)

| Shared piece | Owner | Used by |
|--------------|-------|---------|
| `Encina.Resilience` project, per-type compiler, registry, options front-ends, rejection-mapper hook (maps `BrokenCircuitException` to the circuit-open `Left`), strategy factories for retry and breaker | The foundation issue (issue file `resilience-foundation-encina-resilience.md` of the family resilience-patterns-1) | every plan of both families |
| The `encina.timeout` mapper and `[Timeout]` strategies | #335 (sibling plan, by number) | the executor's timeout step reuses the mapper |
| `IProviderResilience`, the executor, `ProviderResilienceOptions`, the provider-keyed pipelines, the shared contract suite, EventIds 970-984 (`ResilienceProvider`) | **This plan (#742)** | #745 and #643 may reuse; #744 does not (inbound adapters) |
| One decorator per transport and its registration | **This plan (#742)** | the six planned transports (issue file) |
| The `Meter("Encina.Resilience")` holder and instrument names | #728 (plan on main; its choices 1 and 8 pending) | this plan creates no instrument |

---

## Design Choices

<details>
<summary><strong>1. Where the code lives while `Encina.Resilience` does not exist (shared with #136, #137, #138, #139, #141, #145 and #335)</strong></summary>

ADR-055 R7 builds the decorators on the R2 registry, which does not exist yet.

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Wait for the foundation; this plan is written against the registry and the options model** | One implementation, in its final home; nothing that R1 deletes | Blocked on the foundation |
| **B) Build the executor now in `Encina.Polly` with its own Polly pipelines** | Transports get retry earlier | R2 and R7 rejected a pipeline per provider package (7b); the code is deleted by R1; a second options model |
| **C) The first pull request creates a minimal `Encina.Resilience` project** | Unblocks the plan | The first plan to land defines the foundation's shape for every capability |

### Chosen Option: **A — Wait for the foundation** (recommended, pending the maintainer)

### Rationale

- A wins when the 1.0 order of the spike report is kept; B wins if transport resilience must ship first (it is the most critical gap of the issue: "ZERO resilience"); C wins if the foundation should grow out of this capability. The question is asked once for the resilience family (the shared Dilemma 1 of #136).
- Choices 2 to 9 and 11 are written for A.

- **Class:** Dilemma

</details>

<details>
<summary><strong>2. What the decorators wrap: eight publisher interfaces, or a new `IMessageTransport`</strong></summary>

R7 assumes one abstraction; the code has eight unrelated interfaces with different methods and return types. AGENTS.md section 3 says abstractions live in `Encina.Messaging` and providers share them.

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) One decorator per existing publisher interface, all delegating to one `IProviderResilience.ExecuteAsync` call per operation** | No change to public interfaces; the decorator is mechanical; shared logic is in one place | Eight decorator classes; the shared contract suite needs an adapter per transport; a new transport adds one |
| **B) Introduce `IMessageTransport` in `Encina.Messaging`, make the eight publishers implement it, decorate once** | One decorator; the category rule (Send/Publish on every transport) becomes a type | A breaking redesign of eight public interfaces (specific operations such as `JetStreamPublishAsync` or `ScheduleAsync` do not fit a lowest-common interface); outside this issue and its R7 decision |

### Chosen Option: **A — Decorators over the existing interfaces** (recommended, pending the maintainer)

### Rationale

- A wins when the transport-specific operations must stay; B wins if the maintainer wants the transport category to be one type, which is a design for its own issue. The finding that R7's `IMessageTransport` does not exist is recorded here and in the issue file `transport-resilience-planned-transports`.

- **Class:** Dilemma

</details>

<details>
<summary><strong>3. Which of the "10 transports" this plan covers</strong></summary>

The issue lists ten (RabbitMQ, Kafka, NATS, MQTT, AzureServiceBus, AmazonSQS, Redis.PubSub, InMemory, gRPC, GraphQL). #745 owns gRPC and GraphQL and carries a finding of 2026-11-09: neither package makes an outbound call, and the spike #3384 (open, `needs-decision`) decides what their resilience means. Nothing in #742 depends on #745.

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Eight here (seven brokers and InMemory); gRPC and GraphQL stay with #745** | Every transport that has an outbound call is covered; no duplication with #745; not blocked by #3384 | The acceptance wording "all transports" is met for eight; two are answered by #745 |
| **B) All ten here** | Literal | Decorates nothing for two (no outbound call); pre-empts #3384 |
| **C) Wait for #3384** | One answer for all ten | Blocks the seven brokers on a spike that concerns two packages |

### Chosen Option: **A — Eight here** (recommended, pending the maintainer)

### Rationale

- A wins on independence; C wins if the maintainer wants #742 and #745 to ship together. The six planned transports of AGENTS.md section 5 are not on main; their decorators are the issue file `transport-resilience-planned-transports`, so the category stays consistent when they land.

- **Class:** Dilemma

</details>

<details>
<summary><strong>4. Which operations are wrapped: outbound only, or consumers too</strong></summary>

The acceptance criterion "circuit breaker for broker connectivity" and the issue's "Consumer circuit break" assume consumers; the packages have none (publish and request/reply, plus MQTT and Redis.PubSub handler subscriptions).

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Outbound operations (publish, send, request/reply, schedule) now; consumer-side resilience when a consumer abstraction exists, as the issue file `transport-consumer-resilience`** | Wraps what exists; no design invented for a missing consumer | The "consumer circuit break" part of the issue is deferred to an issue |
| **B) Also wrap the handler callbacks of `SubscribeAsync` (MQTT, Redis.PubSub)** | Some consumer coverage now | A breaker around a push callback cannot pause the broker's delivery; two transports only, so the category is inconsistent |

### Chosen Option: **A — Outbound now, consumers by issue** (recommended, pending the maintainer)

### Rationale

- A wins on coherence across the category; B wins if the maintainer wants a partial consumer story before a consumer design exists. Connection recovery stays inside each SDK and is documented per provider (ADR-055 R7, a settled sentence of the decision).

- **Class:** Dilemma

</details>

<details>
<summary><strong>5. Retrying a publish without duplicating it</strong></summary>

A publish whose answer was lost may already have been delivered. The publish-style methods have a `messageId` parameter that becomes the message-id header so consumers can deduplicate (Inbox, AGENTS.md section 3), and the implementations generate a new id when it is null or empty (`KafkaMessagePublisher`, `RabbitMQMessagePublisher`). Five operations have none: RabbitMQ and NATS `RequestAsync`, `IInMemoryMessageBus.PublishAsync` and `EnqueueAsync`, and Azure Service Bus `CancelScheduledAsync`; `ProduceBatchAsync` carries an id per tuple and `AmazonSQSMessagePublisher.SendBatchAsync` takes no id and builds entries with `Id = index` and no MessageId attribute. Those are choices 11 and 12; this choice covers the publish-style methods that have the parameter.

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) The decorator assigns the message id once, before the first attempt, when the caller passed none; every attempt carries the same id; retry only errors classified retryable (ADR-029)** | Every retry is deduplicable by the consumer; callers need no change | A consumer without an Inbox still sees an at-least-once duplicate (the documented Outbox contract) |
| **B) Retry only calls that already carry a caller-supplied id; others fail on the first error** | No duplicate can arise from an unidentified message | Most direct publishes carry no id and get no retry, which leaves the "zero resilience" gap |

### Chosen Option: **A — Assign the id once** (recommended, pending the maintainer)

### Rationale

- A wins for the usual Outbox and event publishing; B wins for a transport whose consumers cannot deduplicate and where a duplicate is harmful. The decision uses `IErrorClassifier` and never retries a `Permanent` error; `InMemory` has no id parameter and no ambiguity (the call is in-process).

- **Class:** Dilemma

</details>

<details>
<summary><strong>6. Who owns the retry per provider when the SDK also retries</strong></summary>

ADR-055 R7 and ADR-050 allow one retry layer: "the SDK's own retry is disabled wherever Encina retries". Kafka's idempotent producer (`EnableIdempotence`, default true in `EncinaKafkaOptions`) depends on the client's own retries; the other SDKs differ.

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Per provider, one owner: Encina retries where the SDK retry can be turned off without losing a delivery guarantee; where it cannot (Kafka's idempotent producer) the SDK keeps its retries and Encina applies the breaker and the timeout only; a provider PR verifies the SDK default and records it in the options defaults and the docs** | One layer per provider; no guarantee lost | The retry behavior differs for Kafka; each provider PR must verify its SDK |
| **B) Encina retries everywhere and the SDK retry is disabled everywhere, including Kafka (idempotence off)** | Identical behavior on all eight | Loses the idempotent-producer guarantee and changes a default of `EncinaKafkaOptions` |

### Chosen Option: **A — One owner per provider** (recommended, pending the maintainer)

### Rationale

- A wins when delivery guarantees matter more than identical behavior; B wins if uniformity is the category goal and Kafka can rely on consumer deduplication. The SDK facts in "Research" are claims to verify in each provider PR, not facts of this plan.

- **Class:** Dilemma

</details>

<details>
<summary><strong>7. "Dead letter routing when retries are exhausted"</strong></summary>

`Encina.Messaging` has a dead-letter store and orchestrator (`src/Encina.Messaging/DeadLetter/`); the Outbox processor already retries and dead-letters a message whose publish keeps failing. The transports have no store.

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) The executor returns the last attempt's `Left` unchanged; the caller (the Outbox processor, or the application) decides what exhaustion means; the broker's own dead-letter feature (SQS redrive, Service Bus DLQ) stays configuration of the broker** | No transport depends on `Encina.Messaging` stores; no second dead-letter path next to Recoverability (ADR-055 R4) | A direct publish that fails is not dead-lettered by this feature |
| **B) The decorator writes an exhausted message to `IDeadLetterStore`** | Meets the criterion literally for direct publishes | Eight packages depend on a store and a tenant/module context they do not have; two dead-letter paths |

### Chosen Option: **A — Return the `Left`; the caller dead-letters** (recommended, pending the maintainer)

### Rationale

- A wins on layering and R4's single retry owner; B wins if direct publishes outside the Outbox need a dead letter record. The acceptance criterion is met through the Outbox path that already exists.

- **Class:** Dilemma

</details>

<details>
<summary><strong>8. Registration of the decorators</strong></summary>

A transport registers its publisher scoped (`services.TryAddScoped<IKafkaMessagePublisher, KafkaMessagePublisher>()`). Applications call `AddEncinaKafka` and the resilience call in any order.

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) One opt-in extension per transport (`AddEncinaKafkaResilience()`); the transport registers the inner type as the concrete class and the interface through a factory that wraps it when a marker singleton is present, so the order of the two calls does not matter** | Explicit per provider; works in any order; nothing changes for an application that does not call it | Eight extensions; each `AddEncina<Transport>` registration is modified |
| **B) One `AddEncinaTransportResilience()` in `Encina.Resilience` that wraps every transport registered by a descriptor each `AddEncina<Transport>` always adds** | One call for an application with several transports | Every transport always registers a descriptor (a small cost for everyone); the descriptor contract is a new public type |

### Chosen Option: **A — One extension per transport, order-independent** (recommended, pending the maintainer)

### Rationale

- A wins when an application uses one or two transports and wants each decision visible; B wins for applications with many transports. Under either, the executor and the options are shared (`AddEncinaResilience(o => o.ForProvider("kafka", ...))`); a DI test builds the provider with `ValidateOnBuild` and `ValidateScopes` for each transport (AGENTS.md section 3).

- **Class:** Dilemma

</details>

<details>
<summary><strong>9. What a caller's cancellation becomes</strong></summary>

Seven publishers turn a cancellation into a `Left` (see the Summary). The executor must not retry it nor count it against the breaker.

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) The executor checks the caller's token: when it is cancelled the executor stops, counts nothing against the breaker and throws `OperationCanceledException` (cancellation propagates)** | Idiomatic .NET; the caller's shutdown is never reported as a broker failure | A caller that never expects an exception from a `ValueTask<Either<...>>` must handle it |
| **B) Return `Left` with `EncinaErrorCodes.RequestCancelled` (category `Cancelled`, ADR-062 R5)** | No exception | The same cancellation then travels as a value that a retry layer further out may mistake for a failure |

### Chosen Option: **A — Propagate** (recommended, pending the maintainer)

### Rationale

- A wins when the caller owns the token (host shutdown); B wins for strict ROP callers. The underlying defect (publishers hiding the cancellation in a `Left`) is the issue file `transport-publishers-cancellation-as-left`; the executor relies on the token, not on the shape of the `Left`.

- **Class:** Dilemma

</details>

<details>
<summary><strong>10. Circuit-breaker state and its key</strong></summary>

ADR-055 settled points: breaker state lives in a per-container singleton, never in a static field; R7: the pipeline is keyed by provider name.

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) One breaker per provider name per container, in the singleton pipeline registry; the key is a fixed lowercase constant of the decorator's own package, never derived from caller input (the constants of the transports decorated by this plan are in "Research")** | Broker connectivity is a property of the broker; the key is injective (one distinct constant per decorator, no tenant or user input) | One broker outage opens the breaker for every topic of that provider |
| **B) One breaker per provider and destination (topic, queue)** | A bad topic does not stop the others | Unbounded keys from caller input; a registry that grows without limit |

### Chosen Option: **A — Per provider name**

### Rationale

- B does not comply: it keys by caller-supplied values, which grows the registry without a bound and breaks the injectivity of keys; A is the only option that satisfies the singleton rule with a bounded key. It holds under every option of choices 1 to 9 and 11 (it concerns how a key is formed, not the package, the wrapped interface, the set of transports or which operations retry; a transport added by choice 3 option B would add its own constant). A breaker is not tenant-keyed: broker connectivity is shared, so the reserved `default` tenant segment does not apply.
- **Class:** Settled
- **Settled by:** ADR-055 settled points ("circuit-breaker state lives in a per-container singleton, never in a static field") and R7 ("keyed by provider name or shard id")

</details>

<details>
<summary><strong>11. Request/reply, batch and id-less operations</strong></summary>

Request/reply (RabbitMQ and NATS `RequestAsync`) executes work on the other side and returns a response; a Kafka batch (`ProduceBatchAsync`, an id per tuple) can be delivered in part (the SQS batch is choice 12); `InMemory` publishing has no id and an in-process failure is a subscriber exception.

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Request/reply is not retried (breaker and timeout only); a Kafka batch is retried whole with each tuple's id assigned once, so the consumer deduplicates the elements already delivered; `InMemory` is not retried (a retry would re-run subscribers; breaker and timeout apply); `CancelScheduledAsync` is retried (cancelling twice is idempotent)** | No duplicated work on the other side of a request; partial batches converge | Request/reply gets no retry; a batch may redeliver elements the consumer must deduplicate |
| **B) All of them retried like a publish** | One rule | A retried request repeats side effects; an in-process retry runs handlers twice |

### Chosen Option: **A — Per operation kind** (recommended, pending the maintainer)

### Rationale

- A wins on safety (ADR-029: a command is retried only when idempotent; a request is a command); B wins if every request/reply in the application is idempotent, which an option `RetryRequestReply` (default false) can express per provider.

- **Class:** Dilemma

</details>

<details>
<summary><strong>12. The SQS batch (`SendBatchAsync`)</strong></summary>

`IAmazonSQSMessagePublisher.SendBatchAsync(IEnumerable<TMessage>, string? queueUrl, CancellationToken)` has no id parameter, and the implementation builds batch entries with `Id = index` and no MessageId attribute (`AmazonSQSMessagePublisher.cs`, read from code). A decorator cannot give a retry a stable id, so a whole-batch retry would deliver the already accepted entries again with nothing to deduplicate on.

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `SendBatchAsync` is not retried (breaker and timeout apply); the caller or the Outbox re-sends the batch** | No interface change; no undeduplicable duplicates | A transient SQS failure of a batch is not retried by this feature |
| **B) Add an optional per-message id parameter to `SendBatchAsync` and carry it as a message attribute, then retry the whole batch like Kafka's** | Same behavior as Kafka; consumers deduplicate | A change to a public interface and its implementation, outside "no interface changes"; one more file set in PR-8 |

### Chosen Option: **A — No batch retry for SQS** (recommended, pending the maintainer)

### Rationale

- A wins for this issue's size and its no-interface-change scope; B wins if SQS batches are the main publishing path and the maintainer accepts the interface change (pre-1.0, a breaking change is acceptable).

- **Class:** Dilemma

</details>

---

## Implementation Phases

Eleven pull requests, in order: **PR-1** Phases 1 and 2 (options, executor, registration, with their unit, guard, DI and property tests); **PR-2** Phase 5 (EventIds, logs and the span event; it modifies the executor and its tests); **PR-3 to PR-10** one transport each (Phase 3), in the order Kafka, RabbitMQ, NATS, MQTT, AzureServiceBus, AmazonSQS, Redis.PubSub, InMemory; PR-3 also adds the abstract contract suite; **PR-11** Phase 7 (documentation). Every PR needs the foundation, R6 and the #335 mapper merged (choice 1 option A); under B or C the split is recounted.

### Phase 1: Core Models and Contracts

<details>
<summary>Tasks</summary>

1. `src/Encina.Resilience/Providers/ProviderResilienceOptions.cs`: per provider `RetryOptions` (`MaxAttempts` default 3, base delay with Polly's exponential jitter, D5), `BreakerOptions` (`FailureThreshold` default 5, `BreakDuration` default 30 s, from the issue's defaults), `TimeSpan? Timeout`, `bool RetryEnabled` (a provider PR sets false for Kafka under choice 6 A); `ForProvider(string name, Action<ProviderPolicy> configure)`; validation at startup (positive values, known name format); every default is the issue's own and is opt-in: nothing applies unless `AddEncina<Transport>Resilience()` is called (AGENTS.md section 1).
2. `ProviderResilienceOptionsValidator.cs` (internal).
3. `IProviderResilience.cs` (public): `ValueTask<Either<EncinaError, T>> ExecuteAsync<T>(string provider, string operation, Func<CancellationToken, ValueTask<Either<EncinaError, T>>> action, CancellationToken cancellationToken)`; parameters validated (`ArgumentException.ThrowIfNullOrWhiteSpace`, `ArgumentNullException.ThrowIfNull`).

</details>

<details>
<summary>Prompt for AI Agents — Phase 1</summary>

```text
CONTEXT: Encina issue #742, plan docs/plans/transport-resilience-implementation-plan-742.md. Encina.Resilience exists (foundation merged), with the registry, options model, retry and breaker strategy factories, and the rejection-mapper hook; R6 and the #335 timeout mapper landed.
TASK: Create ProviderResilienceOptions, its validator and IProviderResilience.
KEY RULES: nothing applies unless a transport opts in; invalid options fail at startup; guards on every public parameter; no exception for business flow; XML docs with examples.
REFERENCE FILES: src/Encina.Security.Secrets (ResilientSecretReaderDecorator, the R7 precedent), docs/architecture/adr/055-resilience-family.md
```

</details>

### Phase 2: The Executor and its Registration

<details>
<summary>Tasks</summary>

1. `Providers/ProviderResilience.cs` (internal sealed, singleton): resolves the provider's pipeline from the registry (key = provider name), builds it on first use from `ProviderResilienceOptions` with the foundation's retry and breaker factories and the #335 timeout step, in the fixed order (total timeout, retry, breaker); runs `action`; a `Left` is classified by `IErrorClassifier`, only retryable ones are retried; a cancelled caller token ends the run and throws `OperationCanceledException` without counting a breaker failure (choice 9); an open circuit returns the foundation's coded `Left`; delays come from Polly's delay types and `TimeProvider`.
2. `Providers/ProviderResilienceServiceCollectionExtensions.cs` (public): `AddEncinaProviderResilience(this IServiceCollection services)` registers the executor and the options validation with `TryAdd*`; guarded; the DI test builds with `ValidateOnBuild` and `ValidateScopes`.
3. `PublicAPI.Unshipped.txt` entries (`mechanical-fixer`).

</details>

<details>
<summary>Prompt for AI Agents — Phase 2</summary>

```text
CONTEXT: Phase 1 done.
TASK: Implement the provider executor and its registration.
KEY RULES: the registry is a per-container singleton, never a static field; classification through IErrorClassifier (ADR-029); cancellation propagates and is never counted as a failure; time only through TimeProvider; no scoped service captured by the singleton.
REFERENCE FILES: src/Encina/Recoverability/IErrorClassifier.cs, src/Encina/Recoverability/OutboundRetryRules.cs, src/Encina.Polly/Behaviors/RetryPipelineBehavior.cs
```

</details>

### Phase 3: Provider Implementations (the Decorators)

<details>
<summary>Tasks</summary>

For each of the eight transports (PR-3 to PR-10), in its package:

1. `Resilient<Transport>MessagePublisher.cs` (public sealed, implements the package's publisher interface; constructor `(I<Transport>MessagePublisher inner, IProviderResilience resilience)`, both public types, both null-guarded): every operation method calls `resilience.ExecuteAsync("<provider>", "<operation>", ct => inner.Method(..., messageId ?? generated, ct), cancellationToken)` and passes the id assigned once (choice 5) per the operation kinds of choice 11; the subscription methods and accessors (`SubscribeAsync`, `SubscribePatternAsync`, `Subscribe`, `PendingCount`, `SubscriberCount`, `IMQTTMessagePublisher.IsConnected`) pass through unwrapped (choice 4); `InMemory`'s decorator wraps `PublishAsync` and `EnqueueAsync` without retry. Per-provider policy defaults are set by the transport's own registration: `AddEncinaKafkaResilience()` configures `ProviderResilienceOptions` with `ForProvider("kafka", p => p.RetryEnabled = false)` before the application's configuration (choice 6 A), and the InMemory registration does the same for `inmemory`; an application's own `ForProvider` call overrides it.
2. `ServiceCollectionExtensions.cs` (modified): `AddEncina<Transport>Resilience()` per choice 8; the inner registration becomes the concrete class plus a factory.
3. `Encina.<Transport>.csproj`: `ProjectReference` to `Encina.Resilience`; `PublicAPI.Unshipped.txt`, the package manifest and one line in the package `README.md` (`mechanical-fixer`, `docs-writer`).
4. Tests in the same PR: the decorator unit test, the DI test (`ValidateOnBuild`, `ValidateScopes`), the guard test, a derived class of the abstract contract suite (`tests/Encina.ContractTests/Messaging/`, with a `ProjectReference` edit of `Encina.ContractTests.csproj`) and the broker integration test (Phase 6 task 5).
4. Provider facts to verify in each PR and record in the options defaults and the docs: whether the SDK retries by default and how to turn it off (choice 6); which exceptions the SDK raises for a closed broker.

</details>

<details>
<summary>Prompt for AI Agents — Phase 3</summary>

```text
CONTEXT: Phases 1 and 2 merged. Transport in this PR: <name>.
TASK: Add the decorator and its registration for the transport, and verify its SDK retry behavior.
KEY RULES: the decorator has a public constructor taking only public types; the message id is assigned once before the first attempt; every method of the interface is wrapped except pure accessors; registration is order-independent and proven with ValidateOnBuild and ValidateScopes; nothing changes for an application that does not call the resilience extension.
REFERENCE FILES: src/Encina.Kafka/IKafkaMessagePublisher.cs, src/Encina.Kafka/ServiceCollectionExtensions.cs, src/Encina.Security.Secrets
```

</details>

### Phase 4: Cross-Cutting Integration

<details>
<summary>Tasks</summary>

1. Tenant and module: the executor takes no tenant or module and keys nothing by them (choice 10); telemetry tenant tag only when multi-tenancy is on (ADR-054 D4).
2. Health checks: the existing transport health checks stay; a breaker state check is not planned here (the breaker is per process); noted in the docs.
3. The deferred pieces are the issue files `transport-consumer-resilience`, `transport-resilience-planned-transports` and `transport-publishers-cancellation-as-left`; gRPC and GraphQL are #745 (open).

</details>

<details>
<summary>Prompt for AI Agents — Phase 4</summary>

```text
CONTEXT: Phases 1 to 3 merged.
TASK: Verify the cross-cutting rows of the plan; add only what they need.
KEY RULES: no tenant or module key; no new health check.
REFERENCE FILES: docs/architecture/adr/054-observability-conventions-and-trace-propagation.md
```

</details>

### Phase 5: Observability

<details>
<summary>Tasks</summary>

1. `src/Encina/Diagnostics/EventIdRanges.cs`: register `ResilienceProvider = (970, 984)`; core `PublicAPI.Unshipped.txt`; `docs/engineering/eventid-block-registry.md` (read by `EventIdRegistryAgreementTests`): in the "Registered ranges" table add `ResilienceProvider | 970-984 | Observability and platform | historical` (the test requires Zone `historical` for every registered range below 10000; `reserved-pre-zone` is only a status of the Blocks table); in the Blocks table add `970-984 ... registered` and keep every other part of the 950-999 row `reserved-pre-zone`. The three plans of this family split the same row by number: 950-959 is #335, 960-969 is #145, 970-984 is #742 and 985-999 stays `reserved-pre-zone`; whichever pull request merges later rebases its registry edit on the earlier one (a merge-order note for the orchestrator); `EncinaEventIdAllocationTests.cs`: create the `Encina.Resilience` entry of `AssemblyRanges` if absent, otherwise add the range to it (an entry for an assembly with no EventIds fails `AssemblyRanges_HasNoStaleEntries`, so the entry exists only once some plan registers ids). The block 950-999 stays valid after the 10000+ zone of AGENTS.md section 7 (it is `reserved-pre-zone` in `docs/engineering/eventid-block-registry.md`).
2. `Providers/ProviderResilienceLogMessages.cs`: 970 `ProviderRetryExhausted` (Warning: provider, operation, attempts), 971 `ProviderCircuitOpened` (Warning), 972 `ProviderCircuitClosed` (Information), 973 `ProviderCallRejectedByOpenCircuit` (Warning), 974 `ProviderRetryAttempt` (Debug), 975 `ProviderTimedOut` (Warning); 976 to 984 spare. Never `EncinaError.Message`, message bodies or ids.
3. `ProviderResilience.cs` (modified): emits the logs from Polly's `OnRetry`, `OnOpened`, `OnClosed` callbacks; spans and Polly's instruments follow R5 and #728 (no instrument of our own); the provider name is a tag.

</details>

<details>
<summary>Prompt for AI Agents — Phase 5</summary>

```text
CONTEXT: Phases 1 and 2 merged.
TASK: Register the EventId range, add the log messages and emit them from the executor.
KEY RULES: register the range in EventIdRanges.cs first (ADR-021), pack ids 970-975 without gaps; [LoggerMessage] source generator; no messages, bodies or ids in logs; exceptions through ForLogging().
REFERENCE FILES: src/Encina/Diagnostics/EventIdRanges.cs, tests/Encina.UnitTests/Testing/Architecture/EncinaEventIdAllocationTests.cs
```

</details>

### Phase 6: Testing

<details>
<summary>Tasks</summary>

1. Unit (`tests/Encina.UnitTests/Resilience/Providers/` and per transport): executor retries a transient `Left` and not a permanent one, stops at `MaxAttempts`, opens the breaker after the threshold, half-opens after the break with a fake clock, times out, never counts a cancelled token, throws on cancellation; each decorator forwards arguments, assigns the id once and keeps it across attempts, returns the last `Left`.
2. Guard (`tests/Encina.GuardTests/`): the executor's `ExecuteAsync` (null or blank `provider`, null `action`), `AddEncinaProviderResilience` null `services`, and for each of the eight decorators the constructor `(inner, resilience)` with a null of each (and no nullable parameter).
3. Contract (`tests/Encina.ContractTests/Messaging/`): the abstract suite lands in PR-3 and each transport PR adds one derived class (PR-3 to PR-10): each decorator, against a fake inner that fails transiently, retries (where choice 11 says so), opens the breaker and returns the `Left`; runs real code.
4. Property (`tests/Encina.PropertyTests/Resilience/`, PR-1): FsCheck, for any failure sequence the number of attempts never exceeds `MaxAttempts` and the id is constant across attempts.
5. Integration (in each transport PR, `tests/Encina.IntegrationTests/MessageBrokers/<Transport>/`, shared collection fixtures of the existing broker tests (the ASB emulator, LocalStack and the NATS collection exist; the Redis one is `RedisFixture` under `Infrastructure/Caching`; a missing collection fixture for RabbitMQ, Kafka or MQTT is added in that PR and counted), `[Trait("Category", "Integration")]`): per broker (RabbitMQ, Kafka, NATS, MQTT, AzureServiceBus emulator, AmazonSQS LocalStack, Redis.PubSub) a publish succeeds after a broker restart and the breaker opens while the container is stopped; PR-3 edits `Encina.IntegrationTests.csproj` to add a `ProjectReference` to `Encina.Resilience` (counted). `InMemory` has no infrastructure: its integration target is 0 with that reason.
6. Load and benchmark: justification files (the executor is a Polly pipeline call); a benchmark is not planned because the added cost is one registry lookup per call.

</details>

<details>
<summary>Prompt for AI Agents — Phase 6</summary>

```text
CONTEXT: Phases 1 to 5 done.
TASK: Write the Phase 6 tests.
KEY RULES: tests execute real code; FakeTimeProvider, never Thread.Sleep; collection fixtures only (never IClassFixture, never new fixtures); ClearAllDataAsync is not needed for brokers, use the existing broker fixtures; Shouldly through Encina.Testing.Shouldly.
REFERENCE FILES: tests/Encina.IntegrationTests/MessageBrokers/, .github/coverage-manifest/Encina.Kafka.json
```

</details>

### Phase 7: Documentation and Finalization

<details>
<summary>Tasks</summary>

1. `docs/features/transport-resilience.md` and the `Encina.Resilience` README section (`docs-writer`): the options, the per-provider SDK retry table (as verified), at-least-once and the message id, what is not covered (consumers, gRPC, GraphQL), connection recovery per SDK; each transport README line lands in its own transport PR (Phase 3 task 3).
2. `changelog.d/742-transport-resilience.added.md`, the `Encina.Resilience` manifest rows for the load and benchmark justifications (`mechanical-fixer`); each transport's `PublicAPI.Unshipped.txt` and manifest land in its own PR.
3. Verification: `dotnet build` 0 warnings; tests filtered to `Resilience.Providers`; EventId architecture tests; `--check-justifications`.

</details>

<details>
<summary>Prompt for AI Agents — Phase 7</summary>

```text
CONTEXT: Phases 1 to 6 green.
TASK: Documentation and verification.
KEY RULES: never edit CHANGELOG.md; docs by docs-writer, metadata by mechanical-fixer; no hand-typed coverage figures; the SDK retry table contains only what the provider PRs verified.
REFERENCE FILES: changelog.d/README.md, .claude/skills/encina-docs/SKILL.md
```

</details>

---

## Research

### Relevant Standards and Specifications

| Source | Relevance |
|--------|-----------|
| ADR-055 R2, R3, R5, R6, R7 | One pipeline registry, keyed by provider; telemetry; decorators |
| ADR-050 (resilience row), ADR-029 | One retry layer per request; only retryable errors |
| ADR-062 | Categories of the returned errors (the last attempt's own `Left`; the timeout and open-circuit codes belong to #335 and the foundation) |
| AGENTS.md section 5 (Transports) | Send/Publish, error handling, metadata on every transport |
| Outbox and Inbox contracts (AGENTS.md section 3) | At-least-once and idempotent consumption: why the id is assigned once |

### Existing Encina Infrastructure to Leverage

| Component | Location | Usage |
|-----------|----------|-------|
| Publisher interfaces and implementations | `src/Encina.{Kafka,RabbitMQ,NATS,MQTT,AzureServiceBus,AmazonSQS,Redis.PubSub,InMemory}/` | Decorated |
| `ResilientSecretReaderDecorator` | `src/Encina.Security.Secrets/` | The R7 precedent |
| `IErrorClassifier`, `OutboundRetryRules` | `src/Encina/Recoverability/` | Retry classification |
| Existing broker integration tests and collections | `tests/Encina.IntegrationTests/MessageBrokers/` | The per-transport PRs |
| `DeadLetter/` store and orchestrator | `src/Encina.Messaging/DeadLetter/` | Not used by the transports (choice 7) |

Transport matrix (AGENTS.md section 5; 10 existing and 6 planned):

| Transport | In this plan | Operations wrapped | SDK retry (claim, verify in the PR) |
|-----------|--------------|--------------------|-------------------------------------|
| RabbitMQ | PR-4 | `PublishAsync`, `SendToQueueAsync`, `RequestAsync` | Client has connection recovery, no publish retry |
| Kafka | PR-3 | `ProduceAsync`, `ProduceBatchAsync`, `ProduceWithHeadersAsync` | Idempotent producer retries inside `MessageTimeoutMs`; keeps its retries (choice 6 A) |
| NATS | PR-5 | `PublishAsync`, `RequestAsync`, `JetStreamPublishAsync` | Reconnect only |
| MQTT | PR-6 | `PublishAsync` | None on publish |
| AzureServiceBus | PR-7 | `SendToQueueAsync`, `PublishToTopicAsync`, `ScheduleAsync`, `CancelScheduledAsync` | Client retry options on by default; disable when Encina retries |
| AmazonSQS | PR-8 | `SendToQueueAsync`, `PublishToTopicAsync`, `SendBatchAsync` (no retry, choice 12), `SendToFifoQueueAsync` | AWS SDK retries by default; disable when Encina retries |
| Redis.PubSub | PR-9 | `PublishAsync` | None on publish |
| InMemory | PR-10 | `PublishAsync`, `EnqueueAsync` | Not applicable |
| gRPC, GraphQL | No: #745 (open; spike #3384) | none | none |
| GoogleCloudPubSub, AmazonEventBridge, Pulsar, Redis.Streams, ActiveMQ, Dapr (planned) | No: issue file `transport-resilience-planned-transports` | when each package lands | per package |

Provider-name constants (the breaker and pipeline keys of choice 10, one `const string` in each decorator; fixed lowercase, never derived from input): `kafka`, `rabbitmq`, `nats`, `mqtt`, `azureservicebus`, `amazonsqs`, `redispubsub`, `inmemory`. They are also the names accepted by `ForProvider(...)`.

### Event ID Allocation

| Package | Range | Notes |
|---------|-------|-------|
| `Encina.Resilience` (`ResilienceProvider`) | 970-984 | New range from the block 950-999; 970-975 used, 976-984 spare; #744 uses none of them (its ids are 4711-4712 and 4759-4760 in its own ranges); verified free by repository-wide search on `0336d31c7`; the PR that registers it also edits `docs/engineering/eventid-block-registry.md` (PR-2) |

### Estimated File Count

| PR | Files | Content |
|----|-------|---------|
| PR-1 | 15 | 5 production (`ProviderResilienceOptions`, validator, `IProviderResilience`, `ProviderResilience`, `ProviderResilienceServiceCollectionExtensions`), 5 tests (executor, options and validator, DI, guard, property), `Encina.PropertyTests.csproj` and `Encina.GuardTests.csproj` (`ProjectReference` to `Encina.Resilience`, each only if missing), `PublicAPI.Unshipped.txt`, manifest rows |
| PR-2 | 10 | `ProviderResilienceLogMessages`, `EventIdRanges.cs`, `docs/engineering/eventid-block-registry.md` (the registry edit of Phase 5 task 1: `ResilienceProvider | 970-984 | Observability and platform | historical` and the Blocks row split; `EventIdRegistryAgreementTests` fails otherwise), core `PublicAPI.Unshipped.txt`, `EncinaEventIdAllocationTests.cs`, `ProviderResilience.cs` (modified), executor test (modified), Resilience `PublicAPI.Unshipped.txt`, manifest |
| PR-3 (Kafka) | 15 | decorator, `ServiceCollectionExtensions.cs` (modified), `Encina.Kafka.csproj`, package `PublicAPI.Unshipped.txt`, manifest, `README.md`, unit, DI and guard tests, derived contract class, integration test, a new `Collections.cs` with the Kafka `CollectionDefinition` (the existing `KafkaHealthCheckIntegrationTests` use `IClassFixture<KafkaFixture>` and no `CollectionDefinition` exists for Kafka, RabbitMQ or MQTT), the abstract contract suite, `Encina.ContractTests.csproj` and `Encina.IntegrationTests.csproj` (one `ProjectReference` each) |
| PR-4 (RabbitMQ), PR-6 (MQTT) | 13 each | the 11 per-transport files (decorator, registration, `.csproj`, `PublicAPI`, manifest, `README.md`, unit, DI, guard, contract class, integration test), `Encina.ContractTests.csproj`, and the new collection definition of its broker |
| PR-5 (NATS), PR-7 (AzureServiceBus), PR-8 (AmazonSQS), PR-9 (Redis.PubSub) | 12 each | the same files, using the existing collections (NATS, Service Bus emulator, LocalStack, `RedisFixture`) |
| PR-10 (InMemory) | 11 | the 12 files without the integration test (no infrastructure) |
| PR-11 | 6 | feature page, Resilience README section, changelog fragment, 2 justification `.md` (load, benchmark), `Encina.Resilience` manifest rows |

Each PR is within 15 files and 1,500 lines. Implementers of changed interfaces: none (no interface changes); `new X(` sites of the registrations are the eight `ServiceCollectionExtensions.cs` files, listed above.

Per-file targets (new files; baseline: none exists on `origin/main`; existing manifest targets of the eight packages are not lowered; each target is set by the PR that adds the tests reaching it):

| File | Unit | Guard | Contract | Property | Integration | Justification |
|------|------|-------|----------|----------|-------------|---------------|
| `ProviderResilience.cs` (PR-1) | 90 | 100 | 0 | 85 | 0 | Retry, breaker, timeout, cancellation driven with a fake clock; the property test of PR-1 on attempts; guard test on `ExecuteAsync` arguments |
| `ProviderResilienceOptions.cs`, `ProviderResilienceOptionsValidator.cs` (PR-1) | 95 | 0 | 0 | 0 | 0 | Property bags and rules; internal validator reached through `InternalsVisibleTo` |
| `ProviderResilienceServiceCollectionExtensions.cs` (PR-1) | 90 | 100 | 0 | 0 | 0 | Guarded public method plus the DI test |
| `ProviderResilienceLogMessages.cs` (PR-2) | 85 | 0 | 0 | 0 | 0 | Log assertions with a fake logger |
| `Resilient<Transport>MessagePublisher.cs` (each in its own PR, PR-3 to PR-10) | 90 | 100 | 85 | 0 | 70 (InMemory 0) | Forwarding and id assignment in the unit tests; guard tests send null to the public constructor; the derived contract class and the broker integration test of the same PR reach the contract and integration rows; InMemory has no infrastructure |
| Kafka `ServiceCollectionExtensions.cs` (PR-3, modified; floors unit 95 / guard 38) | 95 | 38 | 0 | 0 | 70 | The DI test reaches the new `AddEncinaKafkaResilience`; the broker integration test registers through it |
| RabbitMQ `ServiceCollectionExtensions.cs` (PR-4; floors unit 95 / guard 45) | 95 | 45 | 0 | 0 | 70 | Same |
| NATS `ServiceCollectionExtensions.cs` (PR-5; floors unit 85 / guard 55) | 90 | 55 | 0 | 0 | 70 | Same; unit 90 is above the 85 floor |
| MQTT `ServiceCollectionExtensions.cs` (PR-6; no per-file floor on main) | 90 | 85 | 0 | 0 | 70 | New targets, with the DI and guard tests of the PR |
| AzureServiceBus `ServiceCollectionExtensions.cs` (PR-7; floors unit 95 / guard 95 / integration 90) | 95 | 95 | 0 | 0 | 90 | The emulator integration test registers the decorator through the new extension, so the 90 floor holds |
| AmazonSQS `ServiceCollectionExtensions.cs` (PR-8; floor integration 55) | 90 | 85 | 0 | 0 | 55 | The LocalStack integration test registers through the new extension; the floor is kept |
| Redis.PubSub `ServiceCollectionExtensions.cs` (PR-9; floors unit 80 / guard 4 / integration 95) | 90 | 4 | 0 | 0 | 95 | The Redis integration test registers through the new extension, so the 95 floor holds |
| InMemory `ServiceCollectionExtensions.cs` (PR-10; no per-file floor on main) | 90 | 85 | 0 | 0 | 0 | New targets; no infrastructure, so integration 0 |

The floors above were read from `.github/coverage-manifest/Encina.<Package>.json` on `origin/main` and are never lowered; the new-code numbers are set by the DI and guard tests of the same PR.

CRAP of each added method is at most 10 (the executor splits pipeline construction, classification and logging into small methods).

---

## Combined AI Agent Prompts

<details>
<summary>Combined prompt for all phases</summary>

```text
PROJECT CONTEXT: Encina (pre-1.0, .NET 10, C# 14). Issue #742: resilience for message transports (ADR-055 R7, option 7a). AGENTS.md rules apply. The foundation, R6 and the #335 timeout mapper are merged first; if missing, stop and report.

IMPLEMENTATION OVERVIEW: (1) ProviderResilienceOptions and IProviderResilience; (2) the executor with a provider-keyed pipeline (timeout, retry, breaker), cancellation propagating; (3) EventIds 970-975 and logs; (4) one public decorator per transport (Kafka, RabbitMQ, NATS, MQTT, AzureServiceBus, AmazonSQS, Redis.PubSub, InMemory) with an order-independent opt-in registration; (5) a shared contract suite and per-broker integration tests; (6) docs and fragments.

KEY PATTERNS: one retry owner per provider (verify the SDK); the message id assigned once; only retryable errors; breaker state in a per-container singleton keyed by provider name; decorator constructors take public types only; opt-in; specialists for PublicAPI, changelog, manifest and docs.

REFERENCE FILES: docs/architecture/adr/055-resilience-family.md, src/Encina.Kafka/IKafkaMessagePublisher.cs, src/Encina/Recoverability/IErrorClassifier.cs, src/Encina.Security.Secrets
```

</details>

---

## Cross-Cutting Integration Matrix

| # | Function | Status | Notes |
|---|----------|--------|-------|
| 1 | Caching | ❌ | Outbound operations are not cacheable |
| 2 | OpenTelemetry | ✅ | Polly's instruments and span events (R5, #728); provider name tag; tenant tag only with multi-tenancy |
| 3 | Structured Logging | ✅ | EventIds 970-975 (PR-2) |
| 4 | Health Checks | ❌ | Existing transport health checks stay; the breaker is per process |
| 5 | Validation | ✅ | Options validated at startup; executor arguments guarded |
| 6 | Resilience | ✅ | This is the capability: retry, breaker and timeout per provider |
| 7 | Distributed Locks | ❌ | No shared state |
| 8 | Transactions | ❌ | The Outbox owns atomicity; a retried publish is outside the transaction |
| 9 | Idempotency | ✅ | The message id assigned once makes retries deduplicable (choice 5) |
| 10 | Multi-Tenancy | ❌ | The breaker keys on the broker, not the tenant (choice 10); the reserved `default` segment does not apply; tenant metadata propagation is another issue family |
| 11 | Module Isolation | ❌ | Module-independent |
| 12 | Audit Trail | ❌ | Infrastructure concern |

---

## Prerequisites & Dependencies

| Prerequisite | State | Effect |
|--------------|-------|--------|
| The foundation: `Encina.Resilience`, registry, options model, retry and breaker factories, rejection-mapper hook | No issue exists: issue file `resilience-foundation-encina-resilience.md` (family resilience-patterns-1, to be opened by the orchestrator) | Blocks implementation (choice 1); the hook must also map `BrokenCircuitException` to the circuit-open `Left` |
| R6 | No issue exists: issue file `resilience-r6-pipeline-token-and-deadline.md` | The executor's timeout reaches the broker call through the token |
| #335 timeout mapper | Sibling plan, by number | The executor's timeout step |
| #745 (open) and spike #3384 (open, `needs-decision`) | Open | Own gRPC and GraphQL (choice 3) |
| #643 (open) | Open | May reuse the executor for shards |
| #728 telemetry plan | On main, choices pending | Names only |
| Issue files `transport-consumer-resilience`, `transport-resilience-planned-transports`, `transport-publishers-cancellation-as-left` | Written with this plan, to be opened by the orchestrator | Deferred pieces |

---

## Next Steps

1. Commit this plan, open its PR and link it from #742.
2. When #742 is picked up, re-check the choices against today's code and ask the maintainer about the Dilemmas (choices 1 to 9, 11 and 12).
3. Brief workers for PR-1 to PR-11 in order, after the foundation, R6 and the #335 mapper are merged.
