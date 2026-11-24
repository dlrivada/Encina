# Implementation Plan: Encina.ActiveMQ, an Apache ActiveMQ Artemis message transport over AMQP 1.0

> **Issue**: [#241](https://github.com/dlrivada/Encina/issues/241)
> **Type**: Feature
> **Verified against**: origin/main `0336d31c7` (2026-11-20)
> **Complexity**: High (7 phases, 7 pull requests of about 14, 15, 9, 12, 9, 3 and 7 files)
> **Estimated Scope**: about 950 lines of production code and about 1,900 lines of tests
> **Family**: transport-new-1 (#237, #238, #239, #240, #241). Sibling family transport-new-2 (#242, #610, #611, #612, #310) runs later; "Shared pieces" below says which plan owns what.

---

## Summary

The issue adds Apache ActiveMQ Artemis as a transport over AMQP 1.0 with `Apache.NMS.AMQP`: queue and topic destinations, durable subscriptions, message selectors, transactions, a dead-letter queue, scheduled delivery and message priority. State of `origin/main`:

- Transports are publisher-only except MQTT and Redis.PubSub (`SubscribeAsync`); #1609 tracks the gap. There is no `IMessageTransport` in `src/` (verified by grep); each transport has its own `I<Name>MessagePublisher` with a `messageId` parameter, validated options and an `AddEncina<Name>` method (`src/Encina.NATS`, `src/Encina.MQTT`).
- `Encina.MQTT` shows the credentials shape: `Username` and `Password` marked `[JsonIgnore]` with a `ToString()` override. `Encina.NATS` shows endpoint validation (`EndpointValidator`, `AllowLocalEndpoints` with a startup warning).
- Verified on 2026-11-20 by downloading the packages from NuGet: **Apache.NMS.AMQP 2.4.0** (netstandard2.0; depends on AMQPNetLite.Core 2.4.3 and Apache.NMS 2.2.0) and **Apache.NMS 2.2.0** (its assembly has the async API: `CreateProducerAsync`, `SendAsync`, `ReceiveAsync`, `CreateDurableConsumerAsync`, `StartAsync`, `RecoverAsync`, plus `DeliveryDelay`, `NMSPriority` and `IndividualAcknowledge`); **AMQPNetLite.Core 2.5.5** (Apache-2.0); **Testcontainers.ActiveMq 4.15.0** (the version the repository uses for its other Testcontainers modules; its assembly contains `ArtemisBuilder`). None of them is in `Directory.Packages.props` yet.

Delivered: package `Encina.ActiveMQ` with `IActiveMQMessagePublisher` (publish and subscribe, Design Choice 3), `EncinaActiveMQOptions`, `AddEncinaActiveMQ`, a health check, an `ActivitySource` and `Meter`, log messages in the block `ActiveMQ` 40670-40679, an `ArtemisFixture`, unit, guard, contract and integration tests, and documentation. Delivery is at-least-once; consumers deduplicate on the message id carried as a message property.

**Provider category**: Messaging transports (10 existing, 6 planned; AGENTS.md section 5): send/publish, subscription management, error handling with DLQ, metadata propagation.

**Shared pieces (family)**: the shared transport conformance test base is owned by the Redis.Streams plan (#240, its Design Choice 8); this plan subclasses it in its sixth pull request. The family follow-up issue (adoption of the batch #252, priority #251 and outbox-adapter #2385 contracts, tenant and module headers) is `artifacts/issues/new-transports-tenant-and-shared-contracts.md`, written once by the #240 plan.

---

## Design Choices

<details>
<summary><strong>1. Package placement</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) New package `Encina.ActiveMQ`** | Pay-for-what-you-use; own options, health check, EventId block | One more package |
| **B) Fold into `Encina.RabbitMQ`** | One AMQP package | RabbitMQ uses AMQP 0.9.1, a different protocol, client and options |

### Chosen Option: **A, a new package**

### Rationale

The issue names `Encina.ActiveMQ` (its alternatives section rejects reusing RabbitMQ for AMQP 1.0) and AGENTS.md section 5 lists ActiveMQ as a planned 1.0 transport.

**Class:** Settled
**Settled by:** AGENTS.md section 5 (transports table) and #241

</details>

<details>
<summary><strong>2. Client library (Dilemma D2)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Apache.NMS.AMQP 2.4.0 on Apache.NMS 2.2.0** | The issue's choice; a JMS-like model (durable subscriptions, selectors, delivery delay, priority, individual acknowledge) and an async API | netstandard2.0 library with a large surface; sessions are not thread safe, so Encina owns session handling |
| **B) AMQPNetLite.Core 2.5.5 directly** | Fewer layers; full control of links, outcomes and delivery count | Encina would build durable subscriptions, selectors and redelivery handling itself |

### Chosen Option: **A, Apache.NMS.AMQP** (recommended, pending the maintainer)

### Rationale

A wins when the JMS feature set of the issue should come from the client; B wins when a thinner dependency and exact control of AMQP outcomes matter more than speed of delivery. Under either option the public API of Encina is the same.

**Class:** Dilemma

</details>

<details>
<summary><strong>3. Public interface and handler contract (Dilemma D3)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) One `IActiveMQMessagePublisher` with `PublishAsync` and `SubscribeAsync(Func<TMessage, ValueTask> handler, ...)` returning `IAsyncDisposable`; an exception means failure** | Same shape as `IMQTTMessagePublisher` and `IRedisPubSubMessagePublisher`; the shape #1609 asks of the other transports | The name says "publisher" for a consuming member; failure travels as an exception |
| **B) The issue's `IActiveMQMessagePublisher` and `IActiveMQMessageConsumer`, with a handler returning `ValueTask<Either<EncinaError, Unit>>`** | Separate concerns; a `Left` from `IEncina.Send` maps to a redelivery | Differs from the two existing subscribers |

### Chosen Option: **A, the existing shape** (recommended, pending the maintainer)

### Rationale

Under either option a failure is never reported as success (AGENTS.md section 3): the message is not acknowledged, the broker redelivers it, and after the broker's maximum delivery attempts it goes to the dead-letter address (Design Choice 4). A wins for coherence; B wins when handlers wrap `IEncina.Send`. The same question is open in #237, #239 and #240 and must get the same answer.

**Class:** Dilemma

</details>

<details>
<summary><strong>4. Dead-letter queue: broker-native or counted by the client (Dilemma D4)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Broker-native: Artemis address settings (`dead-letter-address`, `max-delivery-attempts`) move a message after N failed deliveries; the transport leaves failed messages unacknowledged, exposes the delivery count to the handler path for logging and documents the required address settings** | Nothing to duplicate; atomic move inside the broker; the broker's own tooling shows the DLQ | Encina cannot set the policy from the client, so a mis-configured broker has no DLQ; the health check cannot verify it |
| **B) Client-counted: the transport reads the delivery count, and after `MaxDeliveryAttempts` publishes to a configured dead-letter address and acknowledges** | Works the same on any AMQP 1.0 broker; policy lives in Encina options | Not atomic (publish then acknowledge can duplicate); two policies when the broker also has one |

### Chosen Option: **A, broker-native** (recommended, pending the maintainer)

### Rationale

A wins when operations own the broker configuration, as is usual for Artemis; B wins for deployments where the application must guarantee a DLQ regardless of broker settings. The integration test configures the address setting on the container in either case.

**Class:** Dilemma

</details>

<details>
<summary><strong>5. Features beyond the acceptance criteria (Dilemma D5)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Include message selectors (an option string on the subscription) and scheduled delivery (a `deliverAt` parameter, from `TimeProvider`); priority is left to the shared priority contract of #251; transactions are not provided** | Selectors and scheduling are cheap in the client; priority has one design for all transports; the issue's own matrix says transactions are ack-based | Priority and transactions are absent from this package for now |
| **B) Include priority and local transactions too** | Complete against the issue's feature list | A second priority design next to #251; transacted sessions complicate the consumer loop |
| **C) Only the acceptance criteria** | Smallest | Selectors and scheduling are listed as features and tasks of the issue |

### Chosen Option: **A, selectors and scheduled delivery** (recommended, pending the maintainer)

### Rationale

Selectors, DLQ, scheduled delivery, priority and transactions appear under Features but not in the acceptance criteria. A wins for coherence with the transport family; B wins for an application that needs broker priority before #251 lands. The selector is configuration, never message data; it is validated for length and logged by length only.

**Class:** Dilemma

</details>

<details>
<summary><strong>6. Connection and session lifetime (Dilemma D6)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) A singleton connection holder created lazily on first use with the NMS async API and disposed by the container; the holder owns the one publish session, its producers and the semaphore that serializes publishes across all scopes (the scoped publisher only calls the holder); one session per subscription, owned and disposed by its subscription handle** | NMS sessions are not thread safe, and the single owner is what serializes them; no blocking call at registration | A semaphore on the publish path |
| **B) A session per publish call** | Simplest | Session creation cost per message |

### Chosen Option: **A, lazy singleton connection, serialized publish session, session per subscription** (recommended, pending the maintainer)

### Rationale

Both options satisfy AGENTS.md section 3 (registration completeness, loops that catch per iteration), so the choice is not settled by it. A wins for sustained publishing, where a session per message would dominate the cost; B wins for rare publishing, where a semaphore on a shared session is needless state. Under A the singleton holder takes no scoped service and the scoped publisher takes the holder (the shape of `src/Encina.NATS/ServiceCollectionExtensions.cs`); the consume loop catches per iteration and cancellation ends it.

**Class:** Dilemma

</details>

<details>
<summary><strong>7. Destination type in the options</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Own enum `ActiveMQDestinationType` (Queue, Topic)** | The public API does not expose a third-party type | A mapping |
| **B) Expose the client's destination enum** | No mapping | The client package becomes part of the public API |

### Chosen Option: **A, an Encina enum**

### Rationale

`EncinaMQTTOptions` exposes `MqttQualityOfService`, not the MQTTnet enum.

**Class:** Trivial
**Settled by:** `src/Encina.MQTT/EncinaMQTTOptions.cs` (`MqttQualityOfService`)

</details>

<details>
<summary><strong>8. Credentials and endpoint safety</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `Username` and `Password` are `[JsonIgnore]` with a `ToString()` override that prints neither; `BrokerUri` is validated with `EndpointValidator` (schemes `amqp`, `amqps`) with an `AllowLocalEndpoints` flag that logs a warning** | Follows the rule for secrets and the NATS precedent | The default `localhost` endpoint is rejected unless the flag is set |
| **B) Plain properties** | Simplest | Violates AGENTS.md section 3 |

### Chosen Option: **A, protected credentials** (the endpoint rule is Design Choice 11)

### Rationale

AGENTS.md section 3 requires `[JsonIgnore]` and a `ToString()` override for an options class with a password; option B violates it. This entry settles only the protection of `Username` and `Password`.

**Class:** Settled
**Settled by:** AGENTS.md section 3 (options with secrets) and `src/Encina.MQTT/EncinaMQTTOptions.cs`

</details>

<details>
<summary><strong>9. OpenTelemetry: per-package source or the shared design of #725 (Dilemma D9)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `ActivitySource` and `Meter` named `Encina.ActiveMQ` in the package, registered by name in `Encina.OpenTelemetry`** | Meets the acceptance criterion now; pattern of `Encina.Messaging.Outbox` | The name list in `Encina.OpenTelemetry` is edited by every transport plan |
| **B) Wait for #725** | One design | #725 has no plan on `origin/main` |

### Chosen Option: **A, per-package instrumentation** (recommended, pending the maintainer)

### Rationale

A wins because the criterion is in this issue; B wins if #725 should rename every source at once. Spans link producer and consumer through message properties (ADR-054 D2, no baggage); the tenant tag of ADR-054 D4 arrives with the multi-tenancy follow-up.

**Class:** Dilemma

</details>

<details>
<summary><strong>10. Multi-tenancy and module headers (Dilemma D10)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Defer to an issue: tenant and module properties need #1164 and the route metadata of #2385** | One design for all transports | Not in this plan |
| **B) Stamp a tenant property now** | Early | Needs the reserved `default` tenant segment and an injective encoding no transport defines yet |

### Chosen Option: **A, defer** (recommended, pending the maintainer)

### Rationale

A wins because one design for all transports needs the persisted context of #1164 and the route metadata of #2385, and the issue's own table defers it ("Tenant context in headers"); B wins for a tenant-isolated deployment that needs the header before those land. The follow-up is an issue file.

**Class:** Dilemma

</details>

<details>
<summary><strong>11. Broker endpoint validation (Dilemma D11)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `BrokerUri` is validated with `EndpointValidator` (schemes `amqp`, `amqps`); a loopback address (including the default `amqp://localhost:5672`) is rejected unless `AllowLocalEndpoints` is set, which logs a startup warning; a URI with user info is rejected, so credentials only travel in `Username` and `Password`** | The NATS and SQS precedent; a mis-set URI cannot reach a link-local or metadata address; no secret in a string that is logged | Local development needs the flag |
| **B) Accept loopback by default** | Zero setup locally | A copied default reaches production unnoticed |

### Chosen Option: **A, validated endpoint** (recommended, pending the maintainer)

### Rationale

A wins for the same reason as in `Encina.NATS` and `Encina.AmazonSQS` (SSRF-style misconfiguration is caught at startup); B wins for a team that only ever runs against a local broker. `EndpointValidator` does not check user info today, so the validator of this package adds that rule.

**Class:** Dilemma

</details>

---

## Implementation Phases

### Phase 1: Package, options and DI (pull request 1)

<details>
<summary>Tasks</summary>

1. `src/Encina.ActiveMQ/Encina.ActiveMQ.csproj` (`Apache.NMS.AMQP`, `Microsoft.Extensions.*.Abstractions`, `LanguageExt.Core`, references to `Encina` and `Encina.Messaging`; `InternalsVisibleTo Encina.UnitTests`), `Encina.slnx` entry, `Directory.Packages.props` entries for `Apache.NMS.AMQP` 2.4.0 (and `Apache.NMS` 2.2.0 if it must be pinned).
2. `EncinaActiveMQOptions`: `BrokerUri` (default `amqp://localhost:5672`), `AllowLocalEndpoints`, `Username` and `Password` (`[JsonIgnore]`), `Destination`, `ActiveMQDestinationType`, `ClientId`, `DurableSubscriptionName`, `MessageSelector`, `ProviderHealthCheck`; `ToString()` prints the destination only; internal validator (`EndpointValidator` and the user-info rule of Design Choice 11, selector length, durable name needs a client id).
3. `AddEncinaActiveMQ(this IServiceCollection, Action<EncinaActiveMQOptions>? configure = null)` with eager validation and `ValidateOnStart`; this pull request registers only the options and their validator (the connection holder is created, tested and registered in pull request 2, so this pull request builds alone).
4. Tests: options, validator and DI unit tests (DI with `ValidateOnBuild` and `ValidateScopes`), `ActiveMQGuardTests`; manifest with the package-level `targets` for unit and guard and the per-file targets and one-sentence justifications of the files added (table of Phase 7, rows of pull request 1; `coverage-report.cs --check-justifications` in `ci.yml` fails a manifest without them); `UnitTests.csproj` and `GuardTests.csproj` references. This pull request has 14 files: csproj, options, `ActiveMQDestinationType`, validator, DI, slnx, `Directory.Packages.props`, 2 test csproj, manifest, 3 unit tests, guard test.

</details>

<details>
<summary>Prompt for AI Agents — Phase 1</summary>

```text
CONTEXT: Encina.NATS and Encina.MQTT are the templates for validated options, credentials and AddEncina<Name>. Apache.NMS.AMQP 2.4.0 (D2 recommended) is not yet in Directory.Packages.props.
TASK: Create the Encina.ActiveMQ skeleton with options, validator and the DI of the options (the connection holder belongs to pull request 2), plus their unit and guard tests.
KEY RULES: Username and Password are [JsonIgnore] with a ToString() override that prints neither; EndpointValidator with AllowLocalEndpoints (warning once); TryAdd registrations only; DI test with ValidateOnBuild and ValidateScopes; own enum for the destination type; no blocking connection at registration.
REFERENCE FILES: src/Encina.MQTT/EncinaMQTTOptions.cs, src/Encina.NATS/EncinaNATSOptionsValidator.cs, src/Encina.NATS/ServiceCollectionExtensions.cs.
```

</details>

### Phase 2: Publishing (pull request 2)

<details>
<summary>Tasks</summary>

1. `IActiveMQMessagePublisher.PublishAsync<TMessage>(TMessage message, string? destination = null, string? messageId = null, IReadOnlyDictionary<string,string>? properties = null, DateTimeOffset? deliverAt = null, CancellationToken cancellationToken = default) where TMessage : class` returning `ValueTask<Either<EncinaError, Unit>>`; `ActiveMQConnectionHolder` (singleton, Design Choice 6: owns the lazy connection, the publish session, its producers and the semaphore; disposed by the container) and `ActiveMQMessagePublisher` (scoped; the holder, options, logger, `TimeProvider`): JSON text message, `message-id` property (a new id only when null or empty), the destination as queue or topic, delivery delay computed from `deliverAt` and `TimeProvider`.
2. Errors (ADR-062), in a new `ActiveMQErrorCodes.cs`: `ACTIVEMQ_PUBLISH_FAILED` `Internal` (R4), `ACTIVEMQ_DESTINATION_REQUIRED` `InvalidRequest` (R3); cancellation propagates; constant messages; exception to `ForLogging()`.
3. `Log.cs` 40670-40672; `EventIdRanges.ActiveMQ = (40670, 40679)`; `AssemblyRanges` entry; `docs/engineering/eventid-block-registry.md` (through `docs-writer`): the registered-ranges row `| ActiveMQ | 40670-40679 | Transports | zone |` and the Blocks row `| 40670-40679 | Transports | transport-new-1 (#241) | registered |` (see "EventId allocation" in Research; no code range below 10000 is added, as `EventIdRegistryAgreementTests` rule 5 requires); and `src/Encina/PublicAPI.Unshipped.txt` (through `mechanical-fixer`) gets the line `static readonly Encina.Diagnostics.EventIdRanges.ActiveMQ -> (int Min, int Max)`, because the range is a public field of `Encina`. The other four transport plans take the neighbouring slices, so their pull requests are merged one at a time (a new commit on a branch when a slot moves, never a rebase of a pushed branch).
4. DI: `AddEncinaActiveMQ` gains `TryAddSingleton<ActiveMQConnectionHolder>` and `TryAddScoped<IActiveMQMessagePublisher, ActiveMQMessagePublisher>` here (registration completeness, AGENTS.md section 3), with the resolution test under `ValidateOnBuild` and `ValidateScopes` in `ServiceCollectionExtensionsTests`.
5. Tests: `ActiveMQMessagePublisherTests` (including the message-id rule), `ActiveMQConnectionHolderTests` (unit), guard edit, manifest edit with the targets and justifications of the files added. This pull request has 15 files: interface, publisher, connection holder, error codes, `Log`, `EventIdRanges.cs`, `src/Encina/PublicAPI.Unshipped.txt`, allocation test, registry doc, DI edit, DI test edit, 2 unit tests, guard edit, manifest.

</details>

<details>
<summary>Prompt for AI Agents — Phase 2</summary>

```text
CONTEXT: The package skeleton exists. Publishing follows the messageId convention of the other publishers.
TASK: Implement PublishAsync over NMS async APIs with queue or topic destinations, message id, properties and optional delay, with unit and guard tests.
KEY RULES: Named category: argument on every EncinaErrors call; constant messages; ids and payloads never logged; TimeProvider for delay arithmetic; EventIds only 40670-40679 registered first in EventIdRanges.cs; cancellation propagates; sessions are never shared across threads.
REFERENCE FILES: src/Encina.NATS/NATSMessagePublisher.cs, src/Encina.MQTT/MQTTMessagePublisher.cs, src/Encina/Diagnostics/EventIdRanges.cs, tests/Encina.UnitTests/MQTT/Publishing/MQTTPublisherMessageIdTests.cs.
```

</details>

### Phase 3: Subscriptions, durable subscriptions and redelivery (pull request 3)

<details>
<summary>Tasks</summary>

1. `SubscribeAsync<TMessage>(Func<TMessage, ValueTask> handler, string? destination = null, CancellationToken cancellationToken = default)`: one session per subscription in individual-acknowledge mode; a queue consumer, a topic consumer or a durable topic consumer (`DurableSubscriptionName` with `ClientId`); optional `MessageSelector`; success acknowledges, failure leaves the message unacknowledged and requests redelivery (D4 option A); the receive loop catches per iteration and never lets an exception escape; cancellation ends it.
2. Codes, added to `ActiveMQErrorCodes.cs`: `ACTIVEMQ_SUBSCRIBE_FAILED` `Internal` (R4), `ACTIVEMQ_DESERIALIZE_FAILED` `Internal` (R4). An undeserializable message is settled through the client's reject path; whether Artemis moves a rejected AMQP delivery to the dead-letter address at once or counts it toward `max-delivery-attempts` is not verified at plan time, so the code does not depend on the answer and the dead-letter integration scenario of pull request 5 includes an undeserializable message and asserts the observed behavior, which the feature page then states.
3. Acceptance mapping for the dead-letter queue (D4 option A): the criterion is met by the broker's address settings, which the transport cannot set or verify from the client; the feature page documents the required `dead-letter-address` and `max-delivery-attempts`, and the integration test configures them on the container.
4. `Log.cs` 40673-40677 (subscription started, handler failed, redelivery requested, rejected, loop iteration failed).
5. Tests: `ActiveMQConsumerLoopTests`, `ActiveMQDurableSubscriptionTests`, `ActiveMQSelectorTests`, guard edit, manifest edit with the targets and justifications of the files changed. This pull request has 9 files: interface edit, publisher edit, error codes edit, `Log` edit, 3 unit tests, guard edit, manifest.

</details>

<details>
<summary>Prompt for AI Agents — Phase 3</summary>

```text
CONTEXT: Publishing exists; consumers must acknowledge, redeliver and leave dead lettering to the broker (D4 option A).
TASK: Implement SubscribeAsync for queues, topics and durable topic subscriptions with selectors and the receive loop.
KEY RULES: A handler failure is never reported as success; the loop catches per iteration and never throws; OperationCanceledException from the stopping token ends the loop; the selector is configuration and is logged by length only; one session per subscription.
REFERENCE FILES: src/Encina.MQTT/MQTTMessagePublisher.cs, src/Encina.Redis.PubSub/RedisPubSubMessagePublisher.cs, AGENTS.md section 3.
```

</details>

### Phase 4: Health check and observability (pull request 4)

<details>
<summary>Tasks</summary>

1. `Health/ActiveMQHealthCheck : EncinaHealthCheck` (`DefaultName = "encina-activemq"`, tags `encina`, `messaging`, `activemq`, `ready`; opens or reuses the connection within `ProviderHealthCheck.Timeout`; text names the exception type only), registered when enabled.
2. `Diagnostics/ActiveMQActivitySource` and `ActiveMQMetrics`: spans publish, consume, acknowledge, reject, consumer-to-producer link through message properties; counters `activemq.published`, `.consumed`, `.rejected` with tags `destination`, `outcome`; `Encina.OpenTelemetry` gains `AddSource`/`AddMeter` lines.
3. The publisher and the consume loop (pull requests 2 and 3) are the emitters, so the publisher file is edited here to start and stop the spans and increment the counters. Tests: `ActiveMQHealthCheckTests`, `ActiveMQDiagnosticsTests`, an edit of `ActiveMQMessagePublisherTests`, guard edit (health check constructor); DI edit (health check only); manifest edit with the targets and justifications of the files added or changed. The `Encina.OpenTelemetry` edit (the new `AddSource`/`AddMeter` lines in `src/Encina.OpenTelemetry/ServiceCollectionExtensions.cs`) is tested by a new unit test `ActiveMQOpenTelemetryRegistrationTests` that builds the tracer and meter providers with `AddEncinaOpenTelemetry` and asserts the package source and meter names are subscribed; that file's existing per-file targets in `.github/coverage-manifest/Encina.OpenTelemetry.json` (unit 90, guard 35) are not lowered and need no new entry. This pull request has 12 files: health check, 2 diagnostics, publisher edit, DI edit, `Encina.OpenTelemetry` edit, 4 unit tests (health, diagnostics, publisher edit, OpenTelemetry registration), guard edit, manifest.

</details>

<details>
<summary>Prompt for AI Agents — Phase 4</summary>

```text
CONTEXT: Publish and consume exist. The issue requires a health check and traces and metrics.
TASK: Add the health check and instrumentation and register the names in Encina.OpenTelemetry.
KEY RULES: No exception message in health results or tags; links not baggage (ADR-054 D2); zero cost without a listener.
REFERENCE FILES: src/Encina.NATS/Health/NATSHealthCheck.cs, src/Encina.Messaging/Diagnostics/OutboxActivitySource.cs, src/Encina.OpenTelemetry/ServiceCollectionExtensions.cs.
```

</details>

### Phase 5: Cross-cutting integration

<details>
<summary>Tasks</summary>

1. Verify each row of the matrix below against the code; deferred rows point to the family issue file.
2. Confirm every code has its ADR-062 category and no log call carries a message id, payload, selector text or `EncinaError.Message`.

</details>

<details>
<summary>Prompt for AI Agents — Phase 5</summary>

```text
CONTEXT: ActiveMQ publishes, consumes, reports health and emits telemetry.
TASK: Verify the cross-cutting matrix against the code.
KEY RULES: AGENTS.md sections 3, 6 and 7; ADR-062 categories.
REFERENCE FILES: AGENTS.md section 6, docs/architecture/adr/062-error-category-contract.md.
```

</details>

### Phase 6: Fixture, integration and contract tests (pull requests 5 and 6)

<details>
<summary>Tasks</summary>

1. Pull request 5 (9 files, with a health check scenario): `Directory.Packages.props` entry `Testcontainers.ActiveMq` 4.15.0, `tests/Encina.TestInfrastructure/Fixtures/ArtemisFixture.cs` (the module's `ArtemisBuilder`, image pinned in the fixture to the latest stable Artemis release, since Apache Artemis publishes no designated LTS line; broker settings with `max-delivery-attempts` small so the dead-letter test is fast), `ArtemisCollection.cs`, `TestInfrastructure.csproj` and `IntegrationTests.csproj` references, `tests/Encina.IntegrationTests/MessageBrokers/ActiveMQ/ActiveMQIntegrationTests.cs`, `ActiveMQDeadLetterIntegrationTests.cs` and `ActiveMQHealthCheckIntegrationTests.cs` (`[Trait("Category","Integration")]`, `[Trait("Transport","ActiveMQ")]`): queue and topic, durable subscription across a reconnect, selector, scheduled delivery, redelivery then dead-letter address (with an undeserializable message), one healthy check; manifest with the integration targets and justifications of the publisher, the connection holder and the health check.
2. Pull request 6 (3 files): `tests/Encina.ContractTests/MessageBrokers/ActiveMQ/ActiveMQConformanceTests.cs` subclassing the shared conformance base (owned by #240), `ContractTests.csproj` reference, manifest edit; it lands after #240's contract pull request.

</details>

<details>
<summary>Prompt for AI Agents — Phase 6</summary>

```text
CONTEXT: Integration tests run against a real Artemis container through a shared collection.
TASK: Add the fixture, the collection, the integration tests and the conformance subclass.
KEY RULES: One shared container per collection, never per class; no Thread.Sleep (bounded waits on TaskCompletionSource); Shouldly through Encina.Testing.Shouldly; no secrets outside the fixture.
REFERENCE FILES: tests/Encina.TestInfrastructure/Fixtures/RabbitMqFixture.cs, tests/Encina.IntegrationTests/MessageBrokers/NATS/NATSJetStreamDeduplicationIntegrationTests.cs, tests/Encina.IntegrationTests/MessageBrokers/AmazonSQS/LocalStackCollection.cs.
```

</details>

### Phase 7: Documentation and finalization (pull request 7)

<details>
<summary>Tasks</summary>

Per-flag targets are entered in `.github/coverage-manifest/Encina.ActiveMQ.json` in the pull request that adds the tests of each flag, each with its one-sentence justification and with the package-level `targets` of that flag in the same pull request (`coverage-report.cs --check-justifications`, run by `ci.yml`, fails a manifest without them). The numbers are planned ceilings; each pull request replaces one with the measured value floored to a multiple of 5 when that is lower (never lowering a target already entered).

| Pull request | File | unit | guard | contract | property | integration | Justification |
|---|---|:---:|:---:|:---:|:---:|:---:|---|
| 1 | `EncinaActiveMQOptions.cs`, `EncinaActiveMQOptionsValidator.cs` | 100 | 0 | 0 | 0 | 0 | Pure data and validation; guard tests have no argument to check. |
| 1 | `ServiceCollectionExtensions.cs` | 85 | 10 | 0 | 0 | 0 | Unit tests build the provider for each option combination; guard tests reach only the null check of `services`. |
| 1 | `ActiveMQDestinationType.cs` | 0 | 0 | 0 | 0 | 0 | An enum has no executable lines. |
| 2 | `ActiveMQMessagePublisher.cs` | 85 | 20 | 0 | 0 | 0 | Unit tests substitute the NMS interfaces for each publish branch; guard tests reach only argument checks. |
| 2 | `ActiveMQConnectionHolder.cs` | 80 | 0 | 0 | 0 | 0 | A substituted connection factory reaches lazy creation, serialization and disposal; it has no public argument surface beyond the publisher. |
| 2 | `ActiveMQErrorCodes.cs`, `IActiveMQMessagePublisher.cs`, `Log.cs` | 0 | 0 | 0 | 0 | 0 | Constants, an interface and generated delegates have no executable lines. |
| 3 | `ActiveMQMessagePublisher.cs` (consume part) | 85 | 20 | 0 | 0 | 0 | The receive loop, redelivery and durable and selector paths are unit tested over substituted sessions. |
| 4 | `Health/ActiveMQHealthCheck.cs` | 90 | 40 | 0 | 0 | 0 | A substituted holder reaches the healthy and unhealthy branches; guard tests reach the constructor checks. |
| 4 | `Diagnostics/*.cs` | 80 | 0 | 0 | 0 | 0 | Counters and spans are exercised through publisher and consumer unit tests; the static initializers are the remainder. |
| 5 | `ActiveMQMessagePublisher.cs`, `ActiveMQConnectionHolder.cs` | 85 | 20 | 0 | 0 | 65 | Integration tests run real publish, durable subscribe, selector, scheduling and redelivery on Artemis, leaving connection-failure branches. |
| 5 | `Health/ActiveMQHealthCheck.cs` | 90 | 40 | 0 | 0 | 50 | One real connection check; the failing branch needs a broken broker. |
| 6 | `ActiveMQMessagePublisher.cs` | 85 | 20 | 50 | 0 | 65 | The conformance base runs the common publish and subscribe scenarios, not selectors or scheduling. |

1. `src/Encina.ActiveMQ/README.md`, `docs/features/activemq.md` (including the required broker address settings of D4), the ActiveMQ section of `docs/messaging/transports.md` and `docs/INVENTORY.md` through `docs-writer`; changelog fragment `changelog.d/241-activemq.added.md` through `mechanical-fixer`. Transport packages carry no `PublicAPI.*.txt` today, so none is added.
2. Load and benchmark justification files under `tests/Encina.LoadTests/Messaging/ActiveMQ/` and `tests/Encina.BenchmarkTests/Messaging/ActiveMQ/` (throughput belongs to the broker).
3. `dotnet build --configuration Release` with zero warnings; every flag at its target; CRAP of every new method at most 10.

</details>

<details>
<summary>Prompt for AI Agents — Phase 7</summary>

```text
CONTEXT: The package is implemented and tested; documentation remains.
TASK: Write the two justification files (the manifest targets and their justifications were entered in pull requests 1 to 6), hand README, feature page, transports page, INVENTORY and the changelog fragment to the owning specialists.
KEY RULES: Targets never go down; no hand-typed coverage figures in docs; never edit CHANGELOG.md.
REFERENCE FILES: tests/Encina.LoadTests/Messaging/RedisPubSub/RedisPubSub.md, .github/coverage-manifest/Encina.NATS.json, changelog.d/README.md.
```

</details>

---

## Research

### Relevant Standards & Specifications

| Standard / Spec | Relevance | Notes |
|-----------------|-----------|-------|
| `AGENTS.md` sections 3, 5, 6, 7, 9 | Secrets in options, background loops, transport category, 12 functions, EventIds, coverage | All phases |
| SPEC-000 DEC-009 | ActiveMQ is one of the six planned transports in 1.0; tested against a container | Scope |
| ADR-062, ADR-054 D2 and D4 | Error categories; trace links and tenant tags | Phases 2 to 4 |
| AMQP 1.0, Artemis address settings (dead-letter address, max delivery attempts), durable subscriptions, selectors (named by the issue) | Native mechanisms | Design Choices 4 and 5 |

### Existing Encina Infrastructure to Leverage

| Component | Location | Usage in this feature |
|-----------|----------|-----------------------|
| `Encina.MQTT`, `Encina.NATS` | `src/Encina.MQTT/`, `src/Encina.NATS/` | Templates for credentials, endpoint validation, DI, health check |
| `EndpointValidator` | `src/Encina/Validation/EndpointValidator.cs` | Broker URI validation |
| `EncinaHealthCheck`, `ProviderHealthCheckOptions` | `src/Encina.Messaging/Health/` | Health check base |
| `RabbitMqFixture` and collections | `tests/Encina.TestInfrastructure/Fixtures/RabbitMqFixture.cs` | Pattern for `ArtemisFixture` |
| `EventIdRanges` | `src/Encina/Diagnostics/EventIdRanges.cs` | New field `ActiveMQ` |

### Event ID Allocation

| Package | Range | Notes |
|---------|-------|-------|
| `ActiveMQ` (new) | 40670-40679 | 40670-40672 publish, 40673-40677 consume, 40678-40679 left for health and connection faults; third slice of the family |

**EventId allocation.** AGENTS.md section 7 allows new blocks only in the 10000+ zone and `EventIdRegistryAgreementTests` rule 5 fails any new range below 10000, so the family does not use the old 1050-1099 reservation. The Transports blocks 40000-40549 are reserved by #4007 for the existing transports (RabbitMQ 40000-40049 through SignalR 40500-40549) and 40550-40649 are claimed by the transport-new-2 plans (#242, #610, #611, #612, #310, 20 ids each, in their open pull requests), so the five plans of transport-new-1 take contiguous 10-id slices from 40650: `RedisStreams` 40650-40659 (#240), `Pulsar` 40660-40669 (#239), `ActiveMQ` 40670-40679 (this plan), `GoogleCloudPubSub` 40680-40689 (#237), `AmazonEventBridge` 40690-40699 (#238). Each plan's registering pull request adds its own registered-ranges row (Zone `zone`) and its own Blocks row with status `registered`, so no `reserved` row overlaps a registered range. If a slot has been taken by then, the registering pull request takes the next free slot and the ids of the plan shift by the same offset.

### Estimated File Count

| Category | Files | Notes |
|----------|------:|-------|
| Production code | 10 | csproj, options, validator, enum, interface, publisher, connection holder, `Log`, DI, health check (diagnostics add 2) |
| Tests | 17 | options, validator, DI, publisher, message id, consumer loop, durable, selector, health, diagnostics, guard, fixture, collection, 2 integration, conformance subclass, manifest |
| Shared edits | 9 | `Encina.slnx`, `Directory.Packages.props` (twice), 4 test `.csproj`, `EventIdRanges.cs`, allocation test, `Encina.OpenTelemetry` |
| Documentation | 8 | README, feature page, transports page, INVENTORY, registry row, changelog, 2 justification files |
| **Pull request 1** | **14** | csproj, options, enum, validator, DI (options only), slnx, `Directory.Packages.props`, 2 test csproj, manifest (package targets, targets and justifications), 3 unit tests, guard; about 600 lines |
| **Pull request 2** | **15** | interface, publisher, connection holder, `ActiveMQErrorCodes`, `Log`, `EventIdRanges.cs`, `PublicAPI.Unshipped.txt`, allocation test, registry (registered row and Blocks row), DI edit (holder and scoped publisher), DI test edit, 2 unit tests, guard edit, manifest edit; about 800 lines |
| **Pull request 3** | **9** | interface edit, publisher edit, error codes edit, `Log` edit, 3 unit tests, guard edit, manifest edit; about 700 lines |
| **Pull request 4** | **12** | health check, 2 diagnostics, publisher edit (spans and counters), DI edit (health check only), `Encina.OpenTelemetry` edit, 4 unit tests (health, diagnostics, publisher edit, OpenTelemetry registration), guard edit, manifest edit; about 650 lines |
| **Pull request 5** | **9** | `Directory.Packages.props`, `TestInfrastructure.csproj`, fixture, collection, `IntegrationTests.csproj`, 3 integration classes, manifest; about 700 lines |
| **Pull request 6** | **3** | conformance subclass, `ContractTests.csproj`, manifest; about 120 lines |
| **Pull request 7** | **7** | README, feature page, transports page, INVENTORY, changelog fragment, 2 justification files; about 350 lines |

`IActiveMQMessagePublisher` is new, so there are no implementers or `new X(` sites to update. Pull requests 1 to 5 and 7 build alone in order (each needs the previous one); pull request 6 needs the conformance base of #240.

---

## Combined AI Agent Prompts

<details>
<summary>Combined prompt for all phases</summary>

```text
PROJECT CONTEXT: Encina is a pre-1.0 .NET 10 / C# 14 library; no backward compatibility, zero warnings, ROP (Either<EncinaError, T>). Transports are publisher-only except MQTT and Redis.PubSub; Encina.ActiveMQ adds an Artemis transport over AMQP 1.0.

IMPLEMENTATION OVERVIEW: PR1 package, options with protected credentials, validator, DI of the options, client package entry; PR2 connection holder, publish with message id, properties, delay and the EventId block 40670-40679; PR3 subscriptions, durable topic subscriptions, selectors and redelivery with broker-native dead lettering; PR4 health check and telemetry; PR5 Artemis fixture and integration tests; PR6 conformance subclass; PR7 documentation. Dilemmas D2, D3, D4, D5, D6, D9, D10 and D11 are implemented as the maintainer answers, the recommended options until then.

KEY PATTERNS: at-least-once, never success on a failure; background loop catches per iteration; named ADR-062 category on every error; constant error messages; ids, payloads and selectors never logged; secrets [JsonIgnore] with ToString override; TimeProvider; sessions never shared across threads; targets never go down; CRAP <= 10.

REFERENCE FILES: src/Encina.MQTT, src/Encina.NATS, src/Encina.Messaging/Health, tests/Encina.TestInfrastructure/Fixtures/RabbitMqFixture.cs, src/Encina/Diagnostics/EventIdRanges.cs.
```

</details>

---

## Cross-Cutting Integration Matrix

| # | Function | Status | Notes |
|---|----------|--------|-------|
| 1 | Caching | ❌ | A transport does not cache messages. |
| 2 | OpenTelemetry | ✅ | Per-package `ActivitySource` and `Meter` (Design Choice 9). |
| 3 | Structured Logging | ✅ | `[LoggerMessage]` in `Log.cs`, EventIds 40670-40679; error codes and exception types only. |
| 4 | Health Checks | ✅ | `ActiveMQHealthCheck` checks the broker connection. |
| 5 | Validation | ❌ | Options are validated at startup; payload validation belongs to the consuming handler's pipeline. |
| 6 | Resilience | ⏭️ | Retry and circuit breaking for transports is #742 (open); broker redelivery is built in. |
| 7 | Distributed Locks | ❌ | Queue consumers compete; the broker gives each message to one consumer. |
| 8 | Transactions | ❌ | Acknowledgement based; local transacted sessions are not provided (Design Choice 5). |
| 9 | Idempotency | ✅ | The message id is carried as a property in every message; processing idempotency stays with the inbox. |
| 10 | Multi-Tenancy | ⏭️ | Tenant properties need #1164 and #2385; family issue file. |
| 11 | Module Isolation | ⏭️ | Same issue file. |
| 12 | Audit Trail | ❌ | Transport is infrastructure; audit belongs to the outbox and inbox. |

---

## Prerequisites & Dependencies

- None blocking pull request 1. Pull requests 2 to 5 and 7 follow in order; pull request 6 waits for the conformance base of #240.
- **Related, not blocking**: #1609 (subscribe shape for the other transports), #251 (message priority, which owns the priority design for all transports), #252 and #2385 (contracts the new transports adopt through the family issue file), #725, #742, #1164.
- **Sibling plans**: #237, #238, #239 and #240; transport-new-2 reuses the same conformance base.

---

## Next Steps

1. Ask the maintainer D2, D3, D4, D5, D6, D9, D10 and D11 when work on #241 starts; record the answers in `## Maintainer Decisions`.
2. Pull request 1: package skeleton (14 files).
3. Pull requests 2 to 4: publish (15), subscribe (9), health and telemetry (12).
4. Pull requests 5 to 7: fixture and integration tests (9), conformance subclass (3), documentation (7).
