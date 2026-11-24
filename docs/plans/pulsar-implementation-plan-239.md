# Implementation Plan: Encina.Pulsar, an Apache Pulsar message transport

> **Issue**: [#239](https://github.com/dlrivada/Encina/issues/239)
> **Type**: Feature
> **Verified against**: origin/main `0336d31c7` (2026-11-20)
> **Complexity**: High (7 phases, 7 pull requests of about 14, 15, 14, 12, 9, 3 and 7 files)
> **Estimated Scope**: about 1,000 lines of production code and about 2,000 lines of tests
> **Family**: transport-new-1 (#237, #238, #239, #240, #241). Sibling family transport-new-2 (#242, #610, #611, #612, #310) runs later; "Shared pieces" below says which plan owns what.

---

## Summary

The issue adds Apache Pulsar as a transport: producer with batching and compression, consumers with the four subscription types (Exclusive, Shared, Failover, Key_Shared), schema support, dead-letter topics, delayed delivery, retention and TTL, and Pulsar's tenant/namespace/topic naming. State of `origin/main`:

- Transports are publisher-only except MQTT and Redis.PubSub (`SubscribeAsync`); #1609 tracks the gap. There is no `IMessageTransport` in `src/` (verified by grep): each transport has its own `I<Name>MessagePublisher` with a `messageId` parameter, a validated options class and `AddEncina<Name>`.
- `Encina.NATS` shows the options shape this plan reuses: `Url` validated by `EndpointValidator` (`src/Encina/Validation/EndpointValidator.cs`) with an `AllowLocalEndpoints` flag that logs a warning, `ProviderHealthCheck`, an internal `IValidateOptions` class. Encina has its own public enum precedent for a library enum, `MqttQualityOfService` (`src/Encina.MQTT/EncinaMQTTOptions.cs`).
- Neither Pulsar client is in `Directory.Packages.props`. Checked on 2026-11-20 by downloading both packages from NuGet: **DotPulsar 5.3.4** (Apache-2.0, the Apache client the issue links, targets net10.0, exposes `RedeliveryCount`, `DeliverAt`, key-shared subscriptions, compression; its assembly has no dead-letter policy and no negative acknowledgement) and **Pulsar.Client 3.21.0** (MIT, targets net8.0, F# with an `FSharp.Core` dependency; its assembly has `EnableBatching`, `DeadLetterPolicy`, `NegativeAcknowledge` and `DeliverAt`). Testcontainers has `Testcontainers.Pulsar` 4.15.0, the version the repository already uses for its other modules.

Delivered: package `Encina.Pulsar` with `IPulsarMessagePublisher` (publish and subscribe, Design Choice 3), `EncinaPulsarOptions`, `AddEncinaPulsar`, a health check, an `ActivitySource` and `Meter`, log messages in the block `Pulsar` 40660-40669, a `PulsarFixture` on `Testcontainers.Pulsar`, unit, guard, contract and integration tests, and documentation. Delivery is at-least-once; consumers deduplicate on the message id carried in the message properties.

**Provider category**: Messaging transports (10 existing, 6 planned; AGENTS.md section 5): send/publish, subscription management, error handling with DLQ, metadata propagation.

**Shared pieces (family)**: the shared transport conformance test base is owned by the Redis.Streams plan (#240, its Design Choice 8); this plan subclasses it in its last pull request. The follow-up issue that makes the five new transports adopt the batch (#252), priority (#251) and outbox-adapter (#2385) contracts and the tenant and module headers is `artifacts/issues/new-transports-tenant-and-shared-contracts.md`, written once for the family by the #240 plan.

---

## Design Choices

<details>
<summary><strong>1. Package placement</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) New package `Encina.Pulsar`** | Pay-for-what-you-use; own options, health check and EventId block | One more package |
| **B) Fold into an existing broker package** | None | No existing package shares a protocol or a client |

### Chosen Option: **A, a new package**

### Rationale

The issue names `Encina.Pulsar` and AGENTS.md section 5 lists Pulsar as a planned 1.0 transport.

**Class:** Settled
**Settled by:** AGENTS.md section 5 (transports table) and #239

</details>

<details>
<summary><strong>2. Client library (Dilemma D2)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Pulsar.Client 3.21.0 (MIT, net8.0)** | Has batching, dead-letter policy, negative acknowledgement and delayed delivery natively, so every feature of the issue maps to a client feature | Community maintained, F# runtime dependency, targets net8.0 only (it loads on net10.0) |
| **B) DotPulsar 5.3.4 (Apache-2.0, net10.0)** | The Apache client the issue links; targets net10.0; no F# dependency | No dead-letter policy and no negative acknowledgement: Encina would build redelivery counting and the dead-letter publish itself; no producer batching builder |

### Chosen Option: **A, Pulsar.Client** (recommended, pending the maintainer)

### Rationale

A wins when the acceptance criteria (batching, subscription types, dead-letter topics) should be met by the client rather than by Encina code; B wins when the project prefers the Apache-maintained client and accepts about 150 more lines of redelivery and dead-letter code plus no batching. Everything below is written for A; under B Phase 3 adds the redelivery counter from `RedeliveryCount` and the dead-letter publish.

**Class:** Dilemma

</details>

<details>
<summary><strong>3. Public interface and handler contract (Dilemma D3)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) One `IPulsarMessagePublisher` with `PublishAsync` and `SubscribeAsync(Func<TMessage, ValueTask> handler, ...)` returning `IAsyncDisposable`; an exception means failure** | Same shape as `IMQTTMessagePublisher` and `IRedisPubSubMessagePublisher`; the shape #1609 asks of the other transports | The name says "publisher" for a consuming member; failure travels as an exception |
| **B) The issue's `IPulsarMessagePublisher` and `IPulsarMessageConsumer`, with a handler returning `ValueTask<Either<EncinaError, Unit>>`** | Separate concerns; a `Left` from `IEncina.Send` maps to a nack | Differs from the two existing subscribers |

### Chosen Option: **A, the existing shape** (recommended, pending the maintainer)

### Rationale

Under either option a failure is never reported as success (AGENTS.md section 3): the message is negatively acknowledged, redelivered, and dead lettered after `MaxRedeliveries`. A wins for coherence; B wins when handlers wrap `IEncina.Send`. The same question is open in #237, #240 and #241 and must get the same answer.

**Class:** Dilemma

</details>

<details>
<summary><strong>4. Subscription type in the options</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Own enum `PulsarSubscriptionType` (Exclusive, Shared, Failover, KeyShared) mapped to the client's** | The public API does not expose a third-party type | A mapping to maintain |
| **B) Expose the client's `SubscriptionType`** | No mapping | The client package becomes part of the public API |

### Chosen Option: **A, an Encina enum**

### Rationale

`EncinaMQTTOptions` exposes `MqttQualityOfService`, not the MQTTnet enum.

**Class:** Trivial
**Settled by:** `src/Encina.MQTT/EncinaMQTTOptions.cs` (`MqttQualityOfService`)

</details>

<details>
<summary><strong>5. Dead lettering</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) A dead-letter topic `<topic>-<subscription>-DLQ` after `MaxRedeliveries` failed deliveries; the reason property carries an error code** | Meets the transport rule; stays on the broker | Operators read a second topic |
| **B) No dead lettering (the issue's acceptance criteria omit it)** | Less code | Violates the transport category rule |

### Chosen Option: **A, a dead-letter topic**

### Rationale

The issue lists dead-letter topics under Features and Tasks but not in its acceptance criteria; AGENTS.md section 5 requires every transport to support error handling and DLQ, so the omission is not a choice: option B is excluded. What the citation settles is that a dead-letter path exists; the topic name `<topic>-<subscription>-DLQ` is Pulsar's own default (so existing tooling finds it) and the threshold `MaxRedeliveries` is an option, and both hold under client option A (the client's `DeadLetterPolicy` acts on the redelivery count) and under client option B of D2 (Encina counts `RedeliveryCount` and publishes to the same topic itself).

**Class:** Settled
**Settled by:** AGENTS.md section 5 (every transport MUST support error handling and DLQ; a transport without dead lettering is excluded)

</details>

<details>
<summary><strong>6. Schema support (Dilemma D6)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Opt-in `SchemaMode`: `None` (default, JSON bytes without a broker schema) or `Json` (a JSON schema registered with the broker per message type)** | Meets the criterion; the broker enforces compatibility; default stays simple | One producer and consumer per message type |
| **B) Avro or Protobuf schemas as well** | Compact formats | Needs generated types or attributes; Encina serializes JSON everywhere else |
| **C) The Confluent-style registry of #259** | Shared design | #259 is a Kafka feature with its own plan |

### Chosen Option: **A, JSON schema, opt-in** (recommended, pending the maintainer)

### Rationale

A wins for a first release because it matches Encina's serializer; B wins for teams with Avro estates, and can be added as further `SchemaMode` values. Pulsar's registry lives in the broker, so nothing outside the client is needed.

**Class:** Dilemma

</details>

<details>
<summary><strong>7. Delayed delivery, retention and TTL (Dilemma D7)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Delayed delivery as a `deliverAt` parameter of `PublishAsync` (computed from `TimeProvider`), refused with `PULSAR_DELAY_UNSUPPORTED` on an Exclusive or Failover subscription; retention and TTL are namespace policies the broker admin sets, documented and not provided** | Uses the client's delayed delivery; no admin client in Encina | Retention and TTL are not configurable from Encina |
| **B) Also wrap the admin REST API for retention and TTL** | Complete from code | A second HTTP integration, its own errors and secrets |
| **C) Defer delayed delivery to scheduling (`IScheduledMessageStore`)** | One scheduler | Scheduling carries domain messages, not broker-side delay (AGENTS.md section 3) |

### Chosen Option: **A, delayed delivery now, retention and TTL documented** (recommended, pending the maintainer)

### Rationale

Neither feature is an acceptance criterion of the issue. A wins when the broker already has its policies managed by operations; B wins when applications provision their own namespaces.

**Class:** Dilemma

</details>

<details>
<summary><strong>8. Naming the Pulsar tenant and namespace (Dilemma D8)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Options `PulsarTenant` and `PulsarNamespace` (defaults `public` and `default`, Pulsar's own defaults) forming `persistent://{tenant}/{namespace}/{topic}`** | No confusion with `ITenantContext` of Encina multi-tenancy | Differs from the issue's `Tenant`, `Namespace` |
| **B) The issue's `Tenant` and `Namespace`** | Matches the issue sketch | Reads as the application tenant |

### Chosen Option: **A, prefixed names** (recommended, pending the maintainer)

### Rationale

A wins because Encina's multi-tenancy (`TenantId`, ADR-054) is a different thing from Pulsar's tenant, which is broker infrastructure; B wins for fidelity to the issue. Mapping the Encina tenant to a Pulsar namespace is part of the deferred multi-tenancy follow-up (Design Choice 10).

**Class:** Dilemma

</details>

<details>
<summary><strong>9. OpenTelemetry: per-package source or the shared design of #725 (Dilemma D9)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `ActivitySource` and `Meter` named `Encina.Pulsar` in the package, registered by name in `Encina.OpenTelemetry`** | Meets the acceptance criterion now; pattern of `Encina.Messaging.Outbox` | The name list in `Encina.OpenTelemetry` is edited by every transport plan |
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
| **A) Defer to an issue: Encina tenant and module properties on messages need #1164 and the route metadata of #2385** | One design for all transports | Not in this plan |
| **B) Stamp a tenant property now** | Early | Needs the reserved `default` tenant segment and an injective encoding no transport defines yet |

### Chosen Option: **A, defer** (recommended, pending the maintainer)

### Rationale

A wins because one design for all transports needs the persisted context of #1164 and the route metadata of #2385, and the issue's own table defers it ("Tenant context in headers"); B wins for a tenant-isolated deployment that needs the header before those land. The follow-up is an issue file.

**Class:** Dilemma

</details>

---

## Implementation Phases

### Phase 1: Package, options and DI (pull request 1)

<details>
<summary>Tasks</summary>

1. `src/Encina.Pulsar/Encina.Pulsar.csproj` (`Pulsar.Client`, `Microsoft.Extensions.*.Abstractions`, `LanguageExt.Core`, reference to `Encina` and `Encina.Messaging`; `InternalsVisibleTo Encina.UnitTests`), `Encina.slnx` entry, `Directory.Packages.props` entry for `Pulsar.Client` 3.21.0.
2. `EncinaPulsarOptions`: `ServiceUrl` (default `pulsar://localhost:6650`, validated with `EndpointValidator` for schemes `pulsar`, `pulsar+ssl`, `http`, `https`; `AllowLocalEndpoints` as in NATS), `PulsarTenant`, `PulsarNamespace`, `TopicPrefix`, `SubscriptionName`, `PulsarSubscriptionType`, `MaxRedeliveries`, `EnableBatching`, `AuthToken` (`[JsonIgnore]`), `ToString()` override printing the namespace only, `ProviderHealthCheck`; internal validator (`ServiceUrl` endpoint rules, positive counts, and a rejection of a `ServiceUrl` that carries user info, which `EndpointValidator` does not check today). `PulsarSubscriptionType` is added here; `SchemaMode` and `PulsarCompressionType` come with the consumers in pull request 3, so this pull request has no type its options do not use.
3. `AddEncinaPulsar(this IServiceCollection, Action<EncinaPulsarOptions>? configure = null)`: eager validation like `AddEncinaNATS` plus `ValidateOnStart`; registers only the options and their validator; no client type is registered here, because the client is built lazily by the producer registry of pull request 2, so this pull request builds alone.
4. Tests: options, validator and DI unit tests (DI with `ValidateOnBuild` and `ValidateScopes`), `PulsarGuardTests`; manifest with the package-level `targets` for unit and guard and the per-file targets and one-sentence justifications of the files added (the table of Phase 7, rows of pull request 1; `coverage-report.cs --check-justifications` in `ci.yml` fails a manifest without them); `UnitTests.csproj` and `GuardTests.csproj` references. This pull request has 14 files: csproj, options, `PulsarSubscriptionType`, validator, DI, slnx, `Directory.Packages.props`, 2 test csproj, manifest, 3 unit tests, guard test.

</details>

<details>
<summary>Prompt for AI Agents — Phase 1</summary>

```text
CONTEXT: Encina.NATS is the template for endpoint-validated options and AddEncina<Name>. Pulsar.Client 3.21.0 (D2 recommended) is not yet in Directory.Packages.props.
TASK: Create the Encina.Pulsar package skeleton with options, validator and DI, plus their unit and guard tests.
KEY RULES: AuthToken is [JsonIgnore] with a ToString() override that never prints it; EndpointValidator with AllowLocalEndpoints (warning logged once); TryAdd registrations only; DI test with ValidateOnBuild and ValidateScopes; own enum for the subscription type; no Obsolete.
REFERENCE FILES: src/Encina.NATS/EncinaNATSOptions.cs, src/Encina.NATS/EncinaNATSOptionsValidator.cs, src/Encina.NATS/ServiceCollectionExtensions.cs, src/Encina.MQTT/EncinaMQTTOptions.cs.
```

</details>

### Phase 2: Publishing (pull request 2)

<details>
<summary>Tasks</summary>

1. `IPulsarMessagePublisher.PublishAsync<TMessage>(TMessage message, string? topic = null, string? messageId = null, IReadOnlyDictionary<string,string>? properties = null, string? key = null, DateTimeOffset? deliverAt = null, CancellationToken cancellationToken = default) where TMessage : class` returning `ValueTask<Either<EncinaError, Unit>>`; `PulsarMessagePublisher` (scoped) that takes a singleton `PulsarProducerRegistry` owning one producer per topic and message type (created lazily, disposed by the container with the client), because a cache inside a scoped publisher would never reuse producers and would leak them; the message id travels as the `message-id` property (a new one only when null or empty) and `deliverAt` is the delayed delivery of D7.
2. Errors (ADR-062), in a new `PulsarErrorCodes.cs`: `PULSAR_PUBLISH_FAILED` `Internal` (R4), `PULSAR_TOPIC_REQUIRED` `InvalidRequest` (R3), `PULSAR_DELAY_UNSUPPORTED` `InvalidRequest`; cancellation propagates; constant messages; exception to `ForLogging()`.
3. `Log.cs` 40660-40662; `EventIdRanges.Pulsar = (40660, 40669)`; `AssemblyRanges` entry; `docs/engineering/eventid-block-registry.md` (through `docs-writer`): the registered-ranges row `| Pulsar | 40660-40669 | Transports | zone |` and the Blocks row `| 40660-40669 | Transports | transport-new-1 (#239) | registered |` (see "EventId allocation" in Research; no code range below 10000 is added, as `EventIdRegistryAgreementTests` rule 5 requires); and `src/Encina/PublicAPI.Unshipped.txt` (through `mechanical-fixer`) gets the line `static readonly Encina.Diagnostics.EventIdRanges.Pulsar -> (int Min, int Max)`, because the range is a public field of `Encina`. The other four transport plans take the neighbouring slices, so their pull requests are merged one at a time (a new commit on a branch when a slot moves, never a rebase of a pushed branch).
4. DI: `AddEncinaPulsar` gains `TryAddSingleton<PulsarProducerRegistry>` and `TryAddScoped<IPulsarMessagePublisher, PulsarMessagePublisher>` here (registration completeness, AGENTS.md section 3), with the resolution test under `ValidateOnBuild` and `ValidateScopes` in `ServiceCollectionExtensionsTests`.
5. The registry (`PulsarProducerRegistry`) also builds and owns the Pulsar client, lazily and asynchronously on first use; it is the only place that creates it, so there is no separate factory type.
6. Tests: `PulsarMessagePublisherTests` (including the message-id rule), `PulsarProducerRegistryTests` (unit), guard edit, manifest edit with the targets and justifications of the files added. This pull request has 15 files: interface, publisher, producer registry, error codes, `Log`, `EventIdRanges.cs`, `src/Encina/PublicAPI.Unshipped.txt`, allocation test, registry doc, DI edit, DI test edit, 2 unit tests, guard edit, manifest.

</details>

<details>
<summary>Prompt for AI Agents — Phase 2</summary>

```text
CONTEXT: Package skeleton exists. Publishing follows the messageId convention of the other publishers.
TASK: Implement PublishAsync with cached producers, message id, key, optional delay and batching/compression from options, with unit and guard tests.
KEY RULES: Named category: argument on every EncinaErrors call; constant messages; ids and payloads never logged; TimeProvider for deliverAt arithmetic; EventIds only 40660-40669 registered first in EventIdRanges.cs; cancellation propagates.
REFERENCE FILES: src/Encina.NATS/NATSMessagePublisher.cs, src/Encina.Kafka/KafkaMessagePublisher.cs, src/Encina/Diagnostics/EventIdRanges.cs, tests/Encina.UnitTests/NATS/Publishing/NATSPublisherMessageIdTests.cs.
```

</details>

### Phase 3: Subscriptions, redelivery and dead letters (pull request 3)

<details>
<summary>Tasks</summary>

1. `SubscribeAsync<TMessage>(Func<TMessage, ValueTask> handler, string? topic = null, CancellationToken cancellationToken = default)`: one consumer per subscription with the configured subscription type, a dead-letter policy (`MaxRedeliveries`, dead-letter topic of D5) and a receive loop that catches per iteration; success acknowledges, failure negatively acknowledges; the loop never lets an exception escape and cancellation ends it.
2. Codes, added to `PulsarErrorCodes.cs`: `PULSAR_SUBSCRIBE_FAILED` `Internal` (R4), `PULSAR_DESERIALIZE_FAILED` `Internal` (R4). A message that cannot be deserialized is negatively acknowledged and goes to the dead-letter topic when the client's dead-letter policy fires after `MaxRedeliveries` (not at once: the policy acts on the redelivery count, and a hand-written immediate dead letter would be extra publish-and-acknowledge code); under D2 option B the same rule is Encina code.
3. `SchemaMode` (`None`, `Json`, D6) and `PulsarCompressionType` (Encina enums, D4's rule) with their options and validator rules land here with the consumers, because the schema applies to producers and consumers alike; the publisher edit applies them to cached producers.
4. `Log.cs` 40663-40667 (subscription started, handler failed, redelivery requested, dead lettered, loop iteration failed).
5. Tests: `PulsarConsumerLoopTests`, `PulsarDeadLetterTests`, `PulsarSubscriptionTypeTests`, `PulsarSchemaAndCompressionTests`, guard edit, manifest edit with the targets and justifications of the files changed. This pull request has 14 files: publisher interface edit, publisher edit, `SchemaMode`, `PulsarCompressionType`, options edit, validator edit, `Log` edit, error codes edit, 4 unit tests, guard edit, manifest.

</details>

<details>
<summary>Prompt for AI Agents — Phase 3</summary>

```text
CONTEXT: Publishing exists; consumers must acknowledge, redeliver and dead letter.
TASK: Implement SubscribeAsync, the receive loop, negative acknowledgement, the dead-letter policy and the subscription types.
KEY RULES: A handler failure is never reported as success; the loop catches per iteration and never throws; OperationCanceledException from the stopping token ends the loop; dead-letter reason is an error code, never an exception message; delayed delivery needs Shared or KeyShared.
REFERENCE FILES: src/Encina.MQTT/MQTTMessagePublisher.cs, src/Encina.Redis.PubSub/RedisPubSubMessagePublisher.cs, AGENTS.md section 3.
```

</details>

### Phase 4: Health check and observability (pull request 4)

<details>
<summary>Tasks</summary>

1. `Health/PulsarHealthCheck : EncinaHealthCheck` (`DefaultName = "encina-pulsar"`, tags `encina`, `messaging`, `pulsar`, `ready`; checks the client connection state within `ProviderHealthCheck.Timeout`; text names the exception type only), registered when enabled.
2. `Diagnostics/PulsarActivitySource` and `PulsarMetrics`: spans publish, consume, acknowledge, dead-letter, consumer-to-producer link through message properties; counters `pulsar.published`, `.consumed`, `.dead_lettered` with tags `topic`, `outcome`; `Encina.OpenTelemetry` gains `AddSource`/`AddMeter` lines.
3. The publisher (pull requests 2 and 3) is the emitter, so its file is edited here to start and stop the spans and increment the counters. Tests: `PulsarHealthCheckTests`, `PulsarDiagnosticsTests`, an edit of `PulsarMessagePublisherTests`, guard edit (health check constructor); DI edit (health check only); manifest edit with the targets and justifications of the files added or changed. This pull request has 11 files: health check, 2 diagnostics, publisher edit, DI edit, `Encina.OpenTelemetry` edit, 4 unit tests (health, diagnostics, publisher edit, OpenTelemetry registration), guard edit, manifest. The `Encina.OpenTelemetry` edit (the new `AddSource`/`AddMeter` lines in `src/Encina.OpenTelemetry/ServiceCollectionExtensions.cs`) is tested by a new unit test `PulsarOpenTelemetryRegistrationTests` that builds the tracer and meter providers with `AddEncinaOpenTelemetry` and asserts the package source and meter names are subscribed; that file's existing per-file targets in `.github/coverage-manifest/Encina.OpenTelemetry.json` (unit 90, guard 35) are not lowered and need no new entry. This pull request therefore has 12 files.

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
2. Confirm every code has its ADR-062 category and no log call carries a message id, payload or `EncinaError.Message`.

</details>

<details>
<summary>Prompt for AI Agents — Phase 5</summary>

```text
CONTEXT: Pulsar publishes, consumes, dead letters, reports health and emits telemetry.
TASK: Verify the cross-cutting matrix against the code.
KEY RULES: AGENTS.md sections 3, 6 and 7; ADR-062 categories.
REFERENCE FILES: AGENTS.md section 6, docs/architecture/adr/062-error-category-contract.md.
```

</details>

### Phase 6: Fixture, integration and contract tests (pull requests 5 and 6)

<details>
<summary>Tasks</summary>

1. Pull request 5: `Directory.Packages.props` entry `Testcontainers.Pulsar` 4.15.0, `tests/Encina.TestInfrastructure/Fixtures/PulsarFixture.cs` (image pinned to the Pulsar 4.0 LTS line of the Apache release policy, one container for the collection), `PulsarCollection.cs`, `tests/Encina.IntegrationTests/MessageBrokers/Pulsar/PulsarIntegrationTests.cs`, `PulsarDeadLetterIntegrationTests.cs` and `PulsarHealthCheckIntegrationTests.cs` (one healthy check; `[Trait("Category","Integration")]`, `[Trait("Transport","Pulsar")]`): publish and subscribe, Shared and Key_Shared, dead letter after `MaxRedeliveries`, delayed delivery, schema `Json`; csproj references; the manifest edit enters the integration targets and justifications of the publisher, the producer registry and the health check. Pull request 5 has 9 files: package entry, `TestInfrastructure.csproj`, fixture, collection, `IntegrationTests.csproj`, three integration classes, manifest.
2. Pull request 6 (3 files): `tests/Encina.ContractTests/MessageBrokers/Pulsar/PulsarConformanceTests.cs` subclassing the shared conformance base (owned by #240), `ContractTests.csproj` reference, manifest edit; it lands after #240's contract pull request.

</details>

<details>
<summary>Prompt for AI Agents — Phase 6</summary>

```text
CONTEXT: Integration tests run against a real Pulsar standalone container through a shared collection.
TASK: Add the fixture, the collection, the integration tests and the conformance subclass.
KEY RULES: One shared container per collection, never per class; no Thread.Sleep (bounded waits on TaskCompletionSource); Shouldly through Encina.Testing.Shouldly; the pinned image is the vendor LTS line.
REFERENCE FILES: tests/Encina.TestInfrastructure/Fixtures/NatsFixture.cs, tests/Encina.IntegrationTests/MessageBrokers/NATS/NATSJetStreamDeduplicationIntegrationTests.cs, tests/Encina.IntegrationTests/MessageBrokers/AmazonSQS/LocalStackCollection.cs.
```

</details>

### Phase 7: Documentation and finalization (pull request 7)

<details>
<summary>Tasks</summary>

Per-flag targets are entered in `.github/coverage-manifest/Encina.Pulsar.json` in the pull request that adds the tests of each flag, each with its one-sentence justification and with the package-level `targets` of that flag in the same pull request (`coverage-report.cs --check-justifications`, run by `ci.yml`, fails a manifest without them). The numbers are planned ceilings; each pull request replaces one with the measured value floored to a multiple of 5 when that is lower (never lowering a target already entered).

| Pull request | File | unit | guard | contract | property | integration | Justification |
|---|---|:---:|:---:|:---:|:---:|:---:|---|
| 1 | `EncinaPulsarOptions.cs`, `EncinaPulsarOptionsValidator.cs` | 100 | 0 | 0 | 0 | 0 | Pure data and validation; guard tests have no argument to check. |
| 1 | `ServiceCollectionExtensions.cs` | 85 | 10 | 0 | 0 | 0 | Unit tests build the provider for each option combination; guard tests reach only the null check of `services`. |
| 1 | `PulsarSubscriptionType.cs` | 0 | 0 | 0 | 0 | 0 | An enum has no executable lines. |
| 2 | `PulsarMessagePublisher.cs` | 85 | 20 | 0 | 0 | 0 | Unit tests substitute the client interfaces for each publish branch; guard tests reach only argument checks. |
| 2 | `PulsarProducerRegistry.cs` | 85 | 0 | 0 | 0 | 0 | The registry is exercised through substituted producer factories; it has no public argument surface beyond the publisher. |
| 2 | `PulsarErrorCodes.cs`, `IPulsarMessagePublisher.cs`, `Log.cs` | 0 | 0 | 0 | 0 | 0 | Constants, an interface and generated delegates have no executable lines. |
| 3 | `PulsarMessagePublisher.cs` (consume part), `SchemaMode.cs`, `PulsarCompressionType.cs` | 85 | 20 | 0 | 0 | 0 | The receive loop, negative acknowledgement and dead-letter policy are unit tested over substituted consumers; the enums have no executable lines. |
| 4 | `Health/PulsarHealthCheck.cs` | 90 | 40 | 0 | 0 | 0 | A substituted client reaches the healthy and unhealthy branches; guard tests reach the constructor checks. |
| 4 | `Diagnostics/*.cs` | 80 | 0 | 0 | 0 | 0 | Counters and spans are exercised through publisher and consumer unit tests; the static initializers are the remainder. |
| 5 | `PulsarMessagePublisher.cs`, `PulsarProducerRegistry.cs` | 85 | 20 | 0 | 0 | 65 | Integration tests run real publish, subscribe, dead letter, delay and schema on a container, leaving connection-failure branches. |
| 5 | `Health/PulsarHealthCheck.cs` | 90 | 40 | 0 | 0 | 50 | One real connection check; the failing branch needs a broken broker. |
| 6 | `PulsarMessagePublisher.cs` | 85 | 20 | 50 | 0 | 65 | The conformance base runs the common publish and subscribe scenarios, not delay or schema. |

1. `src/Encina.Pulsar/README.md`, `docs/features/pulsar.md`, the Pulsar section of `docs/messaging/transports.md` and `docs/INVENTORY.md` through `docs-writer`; changelog fragment `changelog.d/239-pulsar.added.md` through `mechanical-fixer`. Transport packages carry no `PublicAPI.*.txt` today (none in NATS, Kafka, Redis.PubSub), so none is added.
2. Load and benchmark justification files under `tests/Encina.LoadTests/Messaging/Pulsar/` and `tests/Encina.BenchmarkTests/Messaging/Pulsar/` (throughput belongs to the broker).
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
| `AGENTS.md` sections 3, 5, 6, 7, 9 | Background loops, transport category, 12 functions, EventIds, coverage | All phases |
| SPEC-000 DEC-009 | Pulsar is one of the six planned transports in 1.0; tested against a container | Scope |
| ADR-062, ADR-054 D2 and D4 | Error categories; trace links and tenant tags | Phases 2 to 4 |
| Apache Pulsar subscription types, dead-letter topic, delayed delivery, schema (named by the issue) | Native mechanisms | Design Choices 4 to 7 |

### Existing Encina Infrastructure to Leverage

| Component | Location | Usage in this feature |
|-----------|----------|-----------------------|
| `Encina.NATS` | `src/Encina.NATS/` | Template for endpoint-validated options, DI, health check |
| `EndpointValidator` | `src/Encina/Validation/EndpointValidator.cs` | Service URL validation |
| `EncinaHealthCheck`, `ProviderHealthCheckOptions` | `src/Encina.Messaging/Health/` | Health check base |
| `NatsFixture` and collections | `tests/Encina.TestInfrastructure/Fixtures/NatsFixture.cs` | Pattern for `PulsarFixture` |
| `OutboxActivitySource` | `src/Encina.Messaging/Diagnostics/OutboxActivitySource.cs` | Pattern for the source and meter |
| `EventIdRanges` | `src/Encina/Diagnostics/EventIdRanges.cs` | New field `Pulsar` |

### Event ID Allocation

| Package | Range | Notes |
|---------|-------|-------|
| `Pulsar` (new) | 40660-40669 | 40660-40662 publish, 40663-40667 consume, 40668-40669 left for the health check and loop faults; second slice of the family |

**EventId allocation.** AGENTS.md section 7 allows new blocks only in the 10000+ zone and `EventIdRegistryAgreementTests` rule 5 fails any new range below 10000, so the family does not use the old 1050-1099 reservation. The Transports blocks 40000-40549 are reserved by #4007 for the existing transports (RabbitMQ 40000-40049 through SignalR 40500-40549) and 40550-40649 are claimed by the transport-new-2 plans (#242, #610, #611, #612, #310, 20 ids each, in their open pull requests), so the five plans of transport-new-1 take contiguous 10-id slices from 40650: `RedisStreams` 40650-40659 (#240), `Pulsar` 40660-40669 (this plan), `ActiveMQ` 40670-40679 (#241), `GoogleCloudPubSub` 40680-40689 (#237), `AmazonEventBridge` 40690-40699 (#238). Each plan's registering pull request adds its own registered-ranges row (Zone `zone`) and its own Blocks row with status `registered`, so no `reserved` row overlaps a registered range. If a slot has been taken by then, the registering pull request takes the next free slot and the ids of the plan shift by the same offset.

### Estimated File Count

| Category | Files | Notes |
|----------|------:|-------|
| Production code | 11 | csproj, options, validator, interface, publisher, `Log`, DI, health check, 2 diagnostics, `PulsarSubscriptionType` |
| Tests | 18 | options, validator, DI, publisher, message id, consumer loop, dead letter, subscription type, health, diagnostics, guard, fixture, collection, 2 integration, conformance subclass, manifest |
| Shared edits | 9 | `Encina.slnx`, `Directory.Packages.props` (twice), 4 test `.csproj`, `EventIdRanges.cs`, allocation test, `Encina.OpenTelemetry` |
| Documentation | 8 | README, feature page, transports page, INVENTORY, registry row, changelog, 2 justification files |
| **Pull request 1** | **14** | csproj, options, enum, validator, DI, slnx, `Directory.Packages.props`, 2 test csproj, manifest, 3 unit tests, guard; about 600 lines |
| **Pull request 2** | **15** | interface, publisher, producer registry, `PulsarErrorCodes`, `Log`, `EventIdRanges.cs`, `PublicAPI.Unshipped.txt`, allocation test, registry (registered row and Blocks row), DI edit (registry and scoped publisher), DI test edit, 2 unit tests, guard edit, manifest edit; about 800 lines |
| **Pull request 3** | **14** | interface edit, publisher edit, `SchemaMode`, `PulsarCompressionType`, options edit, validator edit, `Log` edit, error codes edit, 4 unit tests, guard edit, manifest; about 900 lines |
| **Pull request 4** | **12** | health check, 2 diagnostics, publisher edit (spans and counters), DI edit (health check only), `Encina.OpenTelemetry` edit, 4 unit tests (health, diagnostics, publisher edit, OpenTelemetry registration), guard edit, manifest; about 650 lines |
| **Pull request 5** | **9** | `Directory.Packages.props`, `TestInfrastructure.csproj`, fixture, collection, `IntegrationTests.csproj`, 3 integration classes, manifest; about 700 lines |
| **Pull request 6** | **3** | conformance subclass, `ContractTests.csproj`, manifest; about 120 lines |
| **Pull request 7** | **7** | README, feature page, transports page, INVENTORY, changelog fragment, 2 justification files; about 350 lines |

`IPulsarMessagePublisher` is new, so there are no implementers or `new X(` sites to update. Pull requests 1 to 5 and 7 build alone in order (each needs the previous one); pull request 6 needs the conformance base of #240.

---

## Combined AI Agent Prompts

<details>
<summary>Combined prompt for all phases</summary>

```text
PROJECT CONTEXT: Encina is a pre-1.0 .NET 10 / C# 14 library; no backward compatibility, zero warnings, ROP (Either<EncinaError, T>). Transports are publisher-only except MQTT and Redis.PubSub; Encina.Pulsar adds an Apache Pulsar transport.

IMPLEMENTATION OVERVIEW: PR1 package, options, DI, client package entry; PR2 publish with message id, key, delay and the EventId block 40660-40669; PR3 subscriptions, negative acknowledgement and dead-letter topic; PR4 health check and telemetry; PR5 Pulsar fixture and integration tests; PR6 conformance subclass; PR7 documentation. Dilemmas D2, D3, D6, D7, D8, D9 and D10 are implemented as the maintainer answers, the recommended options until then.

KEY PATTERNS: at-least-once, never success on a failure; background loop catches per iteration; named ADR-062 category on every error; constant error messages; ids and payloads never logged; secrets [JsonIgnore] with ToString override; TimeProvider; targets never go down; CRAP <= 10.

REFERENCE FILES: src/Encina.NATS, src/Encina.MQTT, src/Encina.Messaging/Health, tests/Encina.TestInfrastructure/Fixtures/NatsFixture.cs, src/Encina/Diagnostics/EventIdRanges.cs.
```

</details>

---

## Cross-Cutting Integration Matrix

| # | Function | Status | Notes |
|---|----------|--------|-------|
| 1 | Caching | ❌ | A transport does not cache messages. |
| 2 | OpenTelemetry | ✅ | Per-package `ActivitySource` and `Meter` (Design Choice 9). |
| 3 | Structured Logging | ✅ | `[LoggerMessage]` in `Log.cs`, EventIds 40660-40669; error codes and exception types only. |
| 4 | Health Checks | ✅ | `PulsarHealthCheck` checks the client connection. |
| 5 | Validation | ❌ | Options are validated at startup; payload validation belongs to the consuming handler's pipeline. |
| 6 | Resilience | ⏭️ | Retry and circuit breaking for transports is #742 (open); redelivery of failed messages is built in. |
| 7 | Distributed Locks | ❌ | Shared subscriptions give each message to one consumer. |
| 8 | Transactions | ❌ | Acknowledgement based; Pulsar transactions are not used. |
| 9 | Idempotency | ✅ | The message id is carried as a property in every message; processing idempotency stays with the inbox. |
| 10 | Multi-Tenancy | ⏭️ | Encina tenant properties and namespace mapping need #1164 and #2385; family issue file. |
| 11 | Module Isolation | ⏭️ | Same issue file. |
| 12 | Audit Trail | ❌ | Transport is infrastructure; audit belongs to the outbox and inbox. |

---

## Prerequisites & Dependencies

- None blocking pull request 1. Pull requests 2 to 5 and 7 follow in order; pull request 6 waits for the conformance base of #240.
- **Related, not blocking**: #1609 (subscribe shape for the other transports), #251, #252 and #2385 (contracts the new transports adopt through the family issue file), #259 (schema registry of Kafka, not used here), #725, #742, #1164.
- **Sibling plans**: #237, #238, #240 and #241; transport-new-2 reuses the same conformance base.

---

## Next Steps

1. Ask the maintainer D2, D3, D6, D7, D8, D9 and D10 when work on #239 starts; record the answers in `## Maintainer Decisions`.
2. Pull request 1: package skeleton (14 files).
3. Pull requests 2 to 4: publish (15), subscribe and dead letter (14), health and telemetry (12).
4. Pull requests 5 to 7: fixture and integration tests (9), conformance subclass (3), documentation (7).
