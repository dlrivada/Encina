# Implementation Plan: Encina.AzureEventHubs, an Azure Event Hubs streaming transport

> **Issue**: [#611](https://github.com/dlrivada/Encina/issues/611)
> **Type**: Feature
> **Verified against**: origin/main `ce352001f` (2026-11-23)
> **Complexity**: Medium (7 phases, 7 pull requests of about 12, 13, 6, 12, 9, 13 and 8 files)
> **Estimated Scope**: about 900 lines of production code and about 2,000 lines of tests
> **Family**: transport-new-2 (#242 Dapr, #610 ZeroMQ, #611 Azure Event Hubs, #612 Amazon Kinesis, #310 Event Mesh). Sibling family transport-new-1 (#237, #238, #239, #240, #241) owns the shared transport conformance base (#240); "Shared pieces" below says which plan owns what.

---

## Summary

The issue asks for an Azure Event Hubs transport for high-throughput event streaming: a producer with partition keys and batches, a consumer with consumer groups and checkpointing in blob storage, a health check, OpenTelemetry, optional Azure Schema Registry integration, `ServiceCollection` extensions, tests and documentation. State of `origin/main`:

- There is no `Encina.AzureEventHubs` project, and `Directory.Packages.props` pins neither `Azure.Messaging.EventHubs` nor `Azure.Messaging.EventHubs.Processor` (it pins `Azure.Messaging.ServiceBus` 7.20.1, `Azure.Storage.Blobs` 12.30.1, `Azure.Identity` 1.21.0, `Testcontainers.Azurite` and `Testcontainers.ServiceBus` 4.15.0). There is no `IMessageTransport` in `src/` (grep), so the sketch in the issue does not match the code: each transport has its own `I<Name>MessagePublisher` with a `messageId` parameter, validated options and an `AddEncina<Name>` method, and `src/Encina.AzureServiceBus` is the template (options with a `[JsonIgnore]` connection string and a `ToString()` override, `TryAddSingleton` of the client, `TryAddScoped` publisher). Only MQTT and Redis.PubSub subscribe; #1609 tracks the gap.
- Verified on 2026-11-23 by downloading the packages from NuGet: **Azure.Messaging.EventHubs 5.12.2** and **Azure.Messaging.EventHubs.Processor 5.12.2** (both MIT; net8.0 and netstandard2.0, so they run on net10.0). The producer has `EventHubProducerClient.SendAsync(IEnumerable<EventData>, SendEventOptions, ct)`, `CreateBatchAsync(CreateBatchOptions, ct)` and `EventHubBufferedProducerClient`; `EventData` has `MessageId`, `CorrelationId`, `ContentType`, `Properties` and `PartitionKey`; `EventProcessorClient` takes a `BlobContainerClient` (the checkpoint store), a consumer group and a connection string or credential, and raises `ProcessEventAsync` and `ProcessErrorAsync`; the Processor package depends on `Azure.Storage.Blobs`. `Testcontainers.EventHubs` 4.15.0 (the pinned Testcontainers line) is MIT and has `EventHubsBuilder.WithAcceptLicenseAgreement`, `WithAzuriteContainer(INetwork, AzuriteContainer, string)`, `WithConfigurationBuilder(EventHubsServiceConfiguration)` and `EventHubsContainer.GetConnectionString()`.
- Scope: SPEC-000 section 6 keeps cloud items with a free local emulator inside 1.0 and names transport #611 with the Event Hubs emulator (SPEC-000 line 171, DEC-009). The issue is planned, not skipped. Its optional Azure Schema Registry criterion is a different case (Design Choice 7).
- Event Hubs is a log, not a queue: there is no per-message acknowledgement, no redelivery and no dead letter queue; a consumer reads a partition in order and records a checkpoint. A handler that throws does not stop `EventProcessorClient` (the exception goes to `ProcessErrorAsync` and the next event is delivered), so failure handling is Encina's job (Design Choice 3).

Delivered: package `Encina.AzureEventHubs` with `IAzureEventHubsMessagePublisher` (publish, batch publish and subscribe, Design Choice 2), `EncinaAzureEventHubsOptions` (validated, `[JsonIgnore]` connection strings), `AddEncinaAzureEventHubs`, a consumer that checkpoints in blob storage and dead-letters (Design Choices 3 and 4), a health check, an `ActivitySource` and `Meter`, log messages in the new range `AzureEventHubs` 40040-40059, unit, guard, property, contract and integration tests against the official emulator, and documentation.

**Provider category**: Messaging transports (AGENTS.md section 5). The transport rule requires send/publish, subscription management, error handling with DLQ and metadata propagation; each is covered below. The cloud-tied ones are tested with local emulators.

**Shared pieces (family)**: the shared conformance base is owned by #240 (this plan subclasses it in pull request 6). The EventId block `40000-40099` of the Transports zone is shared by the five plans (Event ID Allocation). The follow-up issue for tenant and module headers, adoption of #251, #252 and #2385, and the resilience decorators of the new transports is `artifacts/issues/transport-new-2-tenant-module-and-resilience-adoption.md`, owned by the #242 plan of this family.

---

## Design Choices

<details>
<summary><strong>1. Package placement</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) New package `Encina.AzureEventHubs`** | Pay-for-what-you-use; its own options, health check and EventId range; the name the issue gives | One more package |
| **B) Inside `Encina.AzureServiceBus`** | No new package | Mixes a queue broker and a log; different clients, different failure model |

### Chosen Option: **A, a new package**

### Rationale

The issue names `Encina.AzureEventHubs`, SPEC-000 DEC-009 names #611 as a 1.0 transport, and every other transport is its own package.

**Class:** Settled
**Settled by:** #611 (package name) and SPEC-000 DEC-009

</details>

<details>
<summary><strong>2. Public interface: one publisher interface or a publisher and a consumer (Dilemma D2)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) One `IAzureEventHubsMessagePublisher` with `PublishAsync`, `PublishBatchAsync` and `SubscribeAsync`, the shape of `IMQTTMessagePublisher` and `IRedisPubSubMessagePublisher`** | Coherent with the two transports that already subscribe and with the siblings (#240 asks the same question and recommends A); #1609 asks the other transports for the same shape | The name says "publisher" for a consuming member |
| **B) Two interfaces, a publisher and a consumer** | Separate concerns; a producer-only application registers no processor | Different from MQTT and Redis.PubSub; the split would have to be repeated by every transport |

### Chosen Option: **A, one interface with all members** (recommended, pending the maintainer)

### Rationale

A wins when the category is judged by coherence (one call shape for every transport, #1609); B wins when producers and consumers live in different processes and the consumer needs its own lifetime. The same question is open in #237, #239, #240, #241 and in this family and the answer must be the same.

**Class:** Dilemma

</details>

<details>
<summary><strong>3. The handler contract and failure semantics (Dilemma D3)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `Func<TMessage, ValueTask>` as in `SubscribeAsync` of MQTT and Redis.PubSub; an exception means failure: the event is retried in place up to `MaxHandlerAttempts` with a capped backoff, then dead-lettered (Design Choice 4); the checkpoint advances only after success or dead lettering** | Same call shape as the two existing subscribers; order inside a partition is kept | Business failure travels as an exception, against Railway Oriented Programming |
| **B) `Func<TMessage, CancellationToken, ValueTask<Either<EncinaError, Unit>>>`; a `Left` or an exception is a failure with the same retry and dead-letter rule** | A `Left` from `IEncina.Send` maps straight; the failure carries an error code | A new shape that the existing two subscribers do not have |

### Chosen Option: **A, the existing shape** (recommended, pending the maintainer)

### Rationale

Under either option a failure is never reported as success and an event is never skipped silently (AGENTS.md section 3): without a dead-letter destination an event that keeps failing stops its partition (no checkpoint, logged every attempt) rather than being lost. A wins for coherence with the two existing subscribers; B wins when handlers are thin wrappers over `IEncina.Send` returning `Either`.

**Class:** Dilemma

</details>

<details>
<summary><strong>4. Dead-letter destination (Dilemma D4)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) A dead-letter event hub in the same namespace (`DeadLetterEventHubName`, opt-in): the failed event is published with its original properties plus `dead-reason` (an error code, never an exception message) and `delivery-attempts`, then the checkpoint advances** | Same service, same tooling and consumers; ordering of the main stream is not blocked | A second event hub to create and monitor; namespace tiers limit the number of hubs |
| **B) A blob container next to the checkpoints (`<container>-dead`): the failed event is written as a blob** | No second event hub; the payload leaves the stream | Operators read blobs, not events; a new storage layout |

### Chosen Option: **A, a dead-letter event hub** (recommended, pending the maintainer)

### Rationale

A wins when failed events are reprocessed by the same kind of consumer; B wins when the namespace tier is at its hub limit or operations prefer blob tooling. Both are opt-in; with neither configured, Design Choice 3's partition stall applies. If the dead-letter write fails the event is retried again and the checkpoint does not advance (`encina.eventhubs.dead_letter_failed`).

**Class:** Dilemma

</details>

<details>
<summary><strong>5. Checkpoint store and cadence (Dilemma D5)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Azure Blob storage through the SDK's `EventProcessorClient`, a checkpoint after every handled event (`CheckpointEveryEvents` = 1)** | Matches the issue ("checkpointing with blob storage"); the SDK also balances partitions across instances through the same container; the smallest replay window | One blob write per event lowers throughput |
| **B) The same store with a checkpoint every N events or every interval (`CheckpointEveryEvents` > 1, validated)** | Higher throughput | After a crash up to N events are processed again |

### Chosen Option: **A, blob store, checkpoint per event by default** (recommended, pending the maintainer)

### Rationale

A wins for the default of a reliability library; B wins for telemetry-style streams where replaying a few events is harmless. Delivery is at-least-once either way, so consumers deduplicate on the message id (the inbox, AGENTS.md section 3). The blob container holds checkpoints and ownership records, so no distributed lock is needed for partition ownership.

**Class:** Dilemma

</details>

<details>
<summary><strong>6. Batch publishing</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `PublishBatchAsync<TMessage>(IReadOnlyList<TMessage> messages, string? partitionKey = null, ...)` over `CreateBatchAsync` and `EventDataBatch`, returning `Either<EncinaError, IReadOnlyList<string>>` (the message ids), like `SendBatchAsync` of `IAmazonSQSMessagePublisher`; a message too large for any batch is `encina.eventhubs.message_too_large`** | Meets the acceptance criterion "Batch operations"; Event Hubs batches natively | The shared batch contract of #252 may reshape it later |
| **B) Wait for the batch contract of #252** | One shape for 16 transports | The criterion would stay open |

### Chosen Option: **A, a native batch method**

### Rationale

The issue's acceptance criteria list "Batch operations"; only A delivers it, and `SendBatchAsync` of `src/Encina.AmazonSQS/IAmazonSQSMessagePublisher.cs` is the precedent. #252 adoption is in the shared issue file.

**Class:** Settled
**Settled by:** #611 acceptance criteria (batch operations) and the precedent `SendBatchAsync` in `src/Encina.AmazonSQS/IAmazonSQSMessagePublisher.cs`

</details>

<details>
<summary><strong>7. Azure Schema Registry (the optional criterion)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Not in 1.0: the registry is an Azure cloud service with no free local emulator; a follow-up when it can be tested** | Complies with the 1.0 scope and the testing rule | The optional criterion stays open |
| **B) Implement it with `Azure.Data.Schemaregistry`** | Delivers the optional criterion | Cannot be tested without a paid Azure account, which SPEC-000 excludes |

### Chosen Option: **A, excluded from 1.0**

### Rationale

SPEC-000 DEC-009 lists "the cloud halves split from #727, #604, #451, #602, #259 and #421" among the deliverables tied to a cloud service with no free local emulator and puts them in the milestone "Post-1.0: Cloud services without a local emulator"; the issue links #602 (`ISchemaRegistryProvider`) for exactly this. The criterion is optional in the issue.

**Class:** Settled
**Settled by:** SPEC-000 DEC-009 (cloud services without a free local emulator are out of 1.0) and #611 (the criterion is optional)

</details>

<details>
<summary><strong>8. Credentials (Dilemma D8)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Connection strings only, as `Encina.AzureServiceBus`: `ConnectionString` for the namespace and `CheckpointStorageConnectionString` for the blob account, both `[JsonIgnore]` with a `ToString()` override that prints neither; a missing value fails at startup** | One pattern across Azure transports; the emulator is addressed by connection string | Secrets in configuration; no managed identity |
| **B) Also `FullyQualifiedNamespace` and `CheckpointStorageUri` with a `TokenCredential` (the pinned `Azure.Identity`, `DefaultAzureCredential` by default)** | Passwordless production deployments | Two credential paths to validate, test and document; the emulator needs the connection string path anyway |

### Chosen Option: **A, connection strings** (recommended, pending the maintainer)

### Rationale

A wins for parity with `Encina.AzureServiceBus` and the smallest surface; B wins for deployments that forbid shared keys. Under either option a connection string is a secret: `[JsonIgnore]`, `ToString()` overridden, never logged (AGENTS.md section 3).

**Class:** Dilemma

</details>

<details>
<summary><strong>9. Envelope, partitioning and metadata</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `EventData` body = UTF-8 JSON; `MessageId` = the caller's `messageId` (a new GUID only when null or empty), `ContentType` `application/json`, `Properties` carry `type`, `traceparent` and one `h:<name>` entry per caller header; the optional `partitionKey` goes in `SendEventOptions.PartitionKey`, no partition id is exposed** | The same message-id rule as every other publisher; native fields | Property names are limited to what Event Hubs accepts |
| **B) One JSON envelope in the body** | Simple | Double serialization; invisible to Event Hubs tooling |

### Chosen Option: **A, native `EventData` fields**

### Rationale

Every existing publisher takes `messageId` and generates one only when null or empty (`src/Encina.NATS/INATSMessagePublisher.cs`, `src/Encina.AmazonSQS/IAmazonSQSMessagePublisher.cs`); the outbox passes its row id there so consumers deduplicate. Partition keys are the issue's "partitioned publishing"; tenant and module properties are not stamped here (Design Choice 12).

**Class:** Trivial
**Settled by:** the `messageId` convention of `src/Encina.NATS/INATSMessagePublisher.cs` and `src/Encina.AmazonSQS/IAmazonSQSMessagePublisher.cs`, and #611 ("partition key support")

</details>

<details>
<summary><strong>10. The emulator terms in the test fixture (Dilemma D10)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) The fixture accepts the emulator's licence terms with `WithAcceptLicenseAgreement(true)`, pins the image tag, and `docs/testing/integration-tests.md` lists the terms, as done for the Service Bus emulator in #2699** | Tests run for every contributor and in CI; one precedent | Accepting terms on behalf of every contributor who runs the tests |
| **B) The fixture accepts only when an environment variable is set (an explicit per-machine opt-in); the integration tests report themselves unavailable otherwise** | No implicit acceptance | Tests silently not run for contributors; CI needs the variable; against "tested against the emulator" if left unset |

### Chosen Option: **A, accept in the fixture and document** (recommended, pending the maintainer)

### Rationale

A wins for the same reason the maintainer chose it for the Service Bus emulator (`ServiceBusEmulatorFixture`, #2699, 2026-10-27); B wins if the maintainer reads the Event Hubs emulator terms differently from the Service Bus ones. The terms are a different document, so the plan does not treat the earlier answer as covering them.

**Class:** Dilemma

</details>

<details>
<summary><strong>11. OpenTelemetry: per-package source or the shared transport design of #725 (Dilemma D11)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `ActivitySource` and `Meter` named `Encina.AzureEventHubs` in the package, registered by name in `Encina.OpenTelemetry`; the W3C `traceparent` travels in the event properties** | Meets the issue's cross-cutting row now; the same pattern as `Encina.Messaging.Outbox` | The name list in `Encina.OpenTelemetry` is edited by every transport plan |
| **B) Wait for the shared transport instrumentation of #725** | One design for all transports | #725's plan is merged but its code is not; the row would stay open |

### Chosen Option: **A, per-package instrumentation** (recommended, pending the maintainer)

### Rationale

A wins because the issue's matrix marks OpenTelemetry as integrated; B wins if #725 should rename every source at once. Spans link producer and consumer (ADR-054 D2) with no baggage; no tenant tag is emitted until Design Choice 12 is done (ADR-054 D4).

**Class:** Dilemma

</details>

<details>
<summary><strong>12. Multi-tenancy and module headers (Dilemma D12)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Defer to an issue: tenant-aware partition routing and tenant and module properties need the persisted request context (#1164) and the route metadata (#2385)** | No invented scheme; one design for all transports | Not in this plan |
| **B) Use the tenant id as the partition key now** | Early per-tenant ordering | Needs the reserved `default` tenant segment and an injective encoding no transport has defined; hot tenants make hot partitions |

### Chosen Option: **A, defer to a follow-up issue** (recommended, pending the maintainer)

### Rationale

A wins because one design for all transports needs #1164 and #2385; B wins for a deployment that needs per-tenant ordering before those land. The issue file is `artifacts/issues/transport-new-2-tenant-module-and-resilience-adoption.md`.

**Class:** Dilemma

</details>

---

## Implementation Phases

### Phase 1: Package, options and DI (pull request 1)

<details>
<summary>Tasks</summary>

1. `src/Encina.AzureEventHubs/Encina.AzureEventHubs.csproj` (net10.0; packages `Azure.Messaging.EventHubs` and `Azure.Messaging.EventHubs.Processor` (new pins 5.12.2 in `Directory.Packages.props`), `Azure.Storage.Blobs` (already pinned), `Microsoft.Extensions.*.Abstractions`, `LanguageExt.Core`; project references to `Encina` and `Encina.Messaging`; `InternalsVisibleTo Encina.UnitTests`) and the `Encina.slnx` entry. Transport packages carry no `PublicAPI.*.txt` today (none in AzureServiceBus, AmazonSQS, NATS), so none is added.
2. `EncinaAzureEventHubsOptions` (namespace `Encina.AzureEventHubs`, public sealed): `ConnectionString` and `CheckpointStorageConnectionString` (`[JsonIgnore]`; `ToString()` prints the event hub name only), `EventHubName`, `ConsumerGroup` (default `$Default`, the Event Hubs default group), `CheckpointContainerName`, `CheckpointEveryEvents` (default 1), `MaxHandlerAttempts`, `DeadLetterEventHubName` (null: none), `ProviderHealthCheck`. Internal `EncinaAzureEventHubsOptionsValidator : IValidateOptions<>`: non-blank connection strings and names, `CheckpointEveryEvents` and `MaxHandlerAttempts` positive, the dead-letter name different from `EventHubName`.
3. `ServiceCollectionExtensions.AddEncinaAzureEventHubs(this IServiceCollection, Action<EncinaAzureEventHubsOptions>? configure = null)`: options with `ValidateOnStart`, the validator through `TryAddEnumerable`; a DI test that builds the provider with `ValidateOnBuild` and `ValidateScopes`.
4. Tests: `EncinaAzureEventHubsOptionsTests`, `ServiceCollectionExtensionsTests` (unit), `AzureEventHubsGuardTests` (guard: null `services`, null `configure` accepted); manifest `.github/coverage-manifest/Encina.AzureEventHubs.json`; `UnitTests.csproj` and `GuardTests.csproj` ProjectReference edits.

</details>

<details>
<summary>Prompt for AI Agents — Phase 1</summary>

```text
CONTEXT: Encina.AzureServiceBus is the template (options with a JsonIgnore connection string and a ToString override, DI). Azure.Messaging.EventHubs and its Processor 5.12.2 are MIT and net8.0-compatible.
TASK: Create the Encina.AzureEventHubs package skeleton with validated options and AddEncinaAzureEventHubs, and their unit and guard tests.
KEY RULES: both connection strings are [JsonIgnore] with a ToString() override that never prints them; validation fails at startup with OptionsValidationException; TryAdd registrations only; a DI test with ValidateOnBuild and ValidateScopes; no Obsolete, no compatibility code.
REFERENCE FILES: src/Encina.AzureServiceBus/*.cs, src/Encina.AmazonSQS/EncinaAmazonSQSOptionsValidator.cs, .github/coverage-manifest/Encina.AzureServiceBus.json.
```

</details>

### Phase 2: Publishing, envelope and the EventId range (pull request 2)

<details>
<summary>Tasks</summary>

1. `IAzureEventHubsMessagePublisher.PublishAsync<TMessage>(TMessage message, string? partitionKey = null, string? eventHubName = null, string? messageId = null, IReadOnlyDictionary<string,string>? headers = null, CancellationToken cancellationToken = default) where TMessage : class` returning `ValueTask<Either<EncinaError, string>>` (the message id). `AzureEventHubsMessagePublisher` (scoped; `EventHubProducerClient` from the container, `IOptions`, `ILogger`); the producer is a singleton registered with `TryAddSingleton` from the options.
2. Errors (ADR-062), in `AzureEventHubsErrorCodes.cs` (one constants class, so later pull requests add their codes there), each created with the named argument `category:` and a constant message: `encina.eventhubs.publish_failed` `Internal` (R4), `encina.eventhubs.publish_timeout` `Timeout` (R7, the SDK's service-timeout reason), `encina.eventhubs.publish_throttled` `Unavailable` (R7, the SDK's service-busy and quota reasons), `encina.eventhubs.event_hub_not_configured` `Internal` (R1). Cancellation propagates as `OperationCanceledException`, never a `Left` (R5). The exception goes to `ForLogging()`, never its message.
3. `Log.cs` 40040-40042 (publishing, published, publish failed; no message ids, no payload, no connection string). `EventIdRanges.AzureEventHubs = (40040, 40059)`; the `AssemblyRanges` entry in `EncinaEventIdAllocationTests`; `docs/engineering/eventid-block-registry.md` (through `docs-writer`): the row `| AzureEventHubs | 40040-40059 | Transports | zone |` in the registered ranges table and this plan's row of the Blocks table set to `registered` (the five rows of the family block `40000-40099` are added by whichever plan lands first: `40000-40019` #242, `40020-40039` #610, `40040-40059` #611, `40060-40079` #612, `40080-40099` #310, owner `transport-new-2: #<n>`; a conflict is resolved by keeping both rows, never by a rebase of a pushed branch).
4. DI: `AddEncinaAzureEventHubs` gains `TryAddSingleton` of the producer and `TryAddScoped<IAzureEventHubsMessagePublisher, AzureEventHubsMessagePublisher>()` in this pull request (registration completeness, AGENTS.md section 3); the DI test resolves the publisher with `ValidateOnBuild` and `ValidateScopes`.
5. Tests: `AzureEventHubsMessagePublisherTests` and `AzureEventHubsMessageIdTests` (unit, over a substituted `EventHubProducerClient`, which has a protected constructor for mocking), guard edit; manifest edit with the unit and guard targets (contract and integration targets are entered in pull request 6).

</details>

<details>
<summary>Prompt for AI Agents — Phase 2</summary>

```text
CONTEXT: The package skeleton exists. Publishing follows Encina.AzureServiceBus and Encina.AmazonSQS: a messageId parameter, generated only when null or empty.
TASK: Implement IAzureEventHubsMessagePublisher.PublishAsync with partition key support, the error codes, the log messages 40040-40042, the EventId range AzureEventHubs, the producer and publisher DI registrations, with unit and guard tests.
KEY RULES: EncinaErrors.Create with the named category: argument (ADR-062); constant messages, never ex.Message; ids, payloads and connection strings never logged; cancellation propagates; EventIds only 40040-40059, registered first in EventIdRanges.cs and in the registry (Zone zone); no tenant tag.
REFERENCE FILES: src/Encina.AzureServiceBus/AzureServiceBusMessagePublisher.cs, src/Encina/Diagnostics/EventIdRanges.cs, tests/Encina.UnitTests/Testing/Architecture/EncinaEventIdAllocationTests.cs, docs/engineering/eventid-block-registry.md.
```

</details>

### Phase 3: Batch publishing (pull request 3)

<details>
<summary>Tasks</summary>

1. `PublishBatchAsync<TMessage>(IReadOnlyList<TMessage> messages, string? partitionKey = null, string? eventHubName = null, CancellationToken cancellationToken = default)` returning `ValueTask<Either<EncinaError, IReadOnlyList<string>>>`: one or more `EventDataBatch` objects through `CreateBatchAsync` (the partition key set on the batch options), `TryAdd` per event, a new batch when one is full; a single event larger than an empty batch is `encina.eventhubs.message_too_large` `Validation` (R3, a field rule). A failed send of a batch returns a `Left` and no ids (never a partial `Right`).
2. `Log.cs` 40043-40044 (publishing batch, batch published; counts only).
3. Tests: `AzureEventHubsBatchTests` (unit: split across batches, oversize event, failure), guard edit, manifest edit.

</details>

<details>
<summary>Prompt for AI Agents — Phase 3</summary>

```text
CONTEXT: Single publish exists. Event Hubs batches are size-bounded EventDataBatch objects; TryAdd returns false when full.
TASK: Implement PublishBatchAsync with batch splitting, the oversize error and the two log messages, with unit tests.
KEY RULES: a failed batch is a Left, never a partial Right; the oversize code is Validation (R3); cancellation propagates; counts only in logs; dispose each EventDataBatch.
REFERENCE FILES: src/Encina.AmazonSQS/AmazonSQSMessagePublisher.cs (SendBatchAsync), AGENTS.md section 3.
```

</details>

### Phase 4: Consumer, checkpoints and dead lettering (pull request 4)

<details>
<summary>Tasks</summary>

1. `SubscribeAsync<TMessage>(Func<TMessage, ValueTask> handler, string? eventHubName = null, CancellationToken cancellationToken = default)` returning `ValueTask<IAsyncDisposable>`: an internal `IEventHubsProcessorHost` seam wraps `EventProcessorClient` (the SDK type is concrete; its constructor with a `BlobContainerClient` is the production path) so unit tests substitute it; `ProcessEventAsync` runs the handler with the retry and dead-letter rule of Design Choices 3 and 4, then `UpdateCheckpointAsync` every `CheckpointEveryEvents` events (Design Choice 5); `ProcessErrorAsync` logs the error code and exception type and never rethrows. The checkpoint container is created if missing. Cancellation stops the processor (`StopProcessingAsync`) and disposes the subscription.
2. Codes added to `AzureEventHubsErrorCodes.cs`: `encina.eventhubs.subscribe_failed` `Internal` (R4), `encina.eventhubs.checkpoint_failed` `Internal` (R4), `encina.eventhubs.dead_letter_failed` `Internal` (R4), `encina.eventhubs.deserialize_failed` `Internal` (R4, an undeserializable event is dead-lettered at once).
3. `Log.cs` 40045-40052 (subscription started, partition claimed, handler failed with attempt, dead lettered, dead letter failed, checkpoint failed, processor error, subscription ended; partition ids are allowed, event ids and payloads are not).
4. DI: the processor host seam registration (the processor itself is created per subscription); the DI test subscribes with a substituted seam.
5. Tests: `AzureEventHubsConsumerTests`, `AzureEventHubsDeadLetterTests`, `AzureEventHubsCheckpointTests` (unit over the seam), guard edit, manifest edit, options edit if a field is missing.

</details>

<details>
<summary>Prompt for AI Agents — Phase 4</summary>

```text
CONTEXT: Publishing exists. EventProcessorClient balances partitions through a blob container, delivers events in order per partition and continues after a handler exception.
TASK: Implement SubscribeAsync over EventProcessorClient behind an internal seam: in-place retry with capped backoff, dead lettering to the configured event hub, checkpointing every N events, error logging, with unit tests.
KEY RULES: an event is never skipped silently: after the attempts it is dead-lettered or the partition stops with no checkpoint; the checkpoint advances only after success or a successful dead-letter write; ProcessErrorAsync never throws; cancellation propagates and stops the processor; dead-reason is an error code, never an exception message; connection strings, event ids and payloads never logged.
REFERENCE FILES: src/Encina.AzureServiceBus/AzureServiceBusMessagePublisher.cs, src/Encina.Redis.PubSub/RedisPubSubMessagePublisher.cs, AGENTS.md section 3.
```

</details>

### Phase 5: Health check and observability (pull request 5)

<details>
<summary>Tasks</summary>

1. `Health/AzureEventHubsHealthCheck : EncinaHealthCheck` (`DefaultName = "encina-azure-event-hubs"`, tags `encina`, `messaging`, `azure-event-hubs`, `ready`): `GetEventHubPropertiesAsync` on the producer bounded by `ProviderHealthCheck.Timeout`; the result text names the check and the exception type only (never the connection string). Registered like `AzureServiceBusHealthCheck` when `ProviderHealthCheck.Enabled`.
2. `Diagnostics/AzureEventHubsActivitySource` and `AzureEventHubsMetrics`: spans publish, process, checkpoint, dead-letter with a link from consumer to producer through the `traceparent` property; counters `azure_event_hubs.published`, `.processed`, `.dead_lettered`, `.checkpoints` with tags `event_hub`, `outcome`; no tenant tag. `Encina.OpenTelemetry` gains `AddSource`/`AddMeter` lines.
3. Tests: `AzureEventHubsHealthCheckTests`, `AzureEventHubsDiagnosticsTests` (unit), guard edit, manifest edit; the `Encina.OpenTelemetry` registration test is extended (its manifest targets stay at today's unit 90 and guard 35).

</details>

<details>
<summary>Prompt for AI Agents — Phase 5</summary>

```text
CONTEXT: Publish, batch and consume exist. The issue requires a health check and traces and metrics.
TASK: Add the health check, the ActivitySource and Meter, and register the source and meter names in Encina.OpenTelemetry.
KEY RULES: health results and tags never contain an exception message or a connection string; spans link, never baggage (ADR-054 D2); zero cost when no listener; no manifest target is lowered.
REFERENCE FILES: src/Encina.AzureServiceBus/Health/AzureServiceBusHealthCheck.cs, src/Encina.Messaging/Diagnostics/OutboxActivitySource.cs, src/Encina.OpenTelemetry/ServiceCollectionExtensions.cs.
```

</details>

### Phase 6: Emulator, contract, property and integration tests (pull request 6)

<details>
<summary>Tasks</summary>

1. `tests/Encina.TestInfrastructure/Fixtures/EventHubsEmulatorFixture.cs` (Testcontainers.EventHubs with its own Azurite container on the emulator's docker network, because the existing `AzuriteFixture` container is created without a network; the pinned image tag chosen at implementation time as the newest explicit tag, never `latest`; the licence acceptance of Design Choice 10; the emulator configuration declares the event hubs and consumer groups the tests use, including a dead-letter hub; the same Azurite serves the checkpoint blobs), `TestInfrastructure.csproj` references (`Testcontainers.EventHubs`, `Azure.Messaging.EventHubs`), `Directory.Packages.props` (`Testcontainers.EventHubs` 4.15.0), and an `EventHubsEmulatorCollection` class in each of `tests/Encina.IntegrationTests/MessageBrokers/AzureEventHubs/` and `tests/Encina.ContractTests/MessageBrokers/AzureEventHubs/` (per assembly, as `ServiceBusEmulatorCollection` is; no existing collection can be reused, grep of `CollectionDefinition` finds none for Event Hubs).
2. `tests/Encina.ContractTests/MessageBrokers/AzureEventHubs/AzureEventHubsConformanceTests.cs`, a subclass of `TransportConformanceContractBase` of #240 (publish with message id, subscribe delivers, handler failure retried then dead-lettered, cancellation); `ContractTests.csproj` reference.
3. `tests/Encina.IntegrationTests/MessageBrokers/AzureEventHubs/AzureEventHubsIntegrationTests.cs` (`[Collection]`, `[Trait("Category","Integration")]`, `[Trait("Transport","AzureEventHubs")]`): partition-key ordering, two consumer groups, batch publish, restart resumes from the checkpoint, dead-letter hub content; `IntegrationTests.csproj` reference.
4. `tests/Encina.PropertyTests/Messaging/AzureEventHubs/AzureEventHubsEnvelopePropertyTests.cs` (FsCheck round trip of message id, properties and body) and `PropertyTests.csproj` reference.
5. One manifest edit entering the contract, property and integration targets (the table of Phase 7); `docs/testing/integration-tests.md` gains the emulator's terms (through `docs-writer`).

</details>

<details>
<summary>Prompt for AI Agents — Phase 6</summary>

```text
CONTEXT: The conformance base of #240 is merged; the official Event Hubs emulator runs in a container with its Azurite sidecar.
TASK: Write the emulator fixture, the per-assembly collections, the conformance subclass, the integration tests and the property test.
KEY RULES: one shared collection fixture, never a per-class container; the emulator needs its own Azurite on its docker network; pin image tags; no Thread.Sleep (bounded waits on TaskCompletionSource); Shouldly through Encina.Testing.Shouldly; ClearAllDataAsync-style cleanup in InitializeAsync (new consumer group or hub names per test class).
REFERENCE FILES: tests/Encina.TestInfrastructure/Fixtures/ServiceBusEmulatorFixture.cs, tests/Encina.TestInfrastructure/Fixtures/AzuriteFixture.cs, tests/Encina.IntegrationTests/MessageBrokers/AzureServiceBus/ServiceBusEmulatorCollection.cs.
```

</details>

### Phase 7: Documentation and finalization (pull request 7)

<details>
<summary>Tasks</summary>

Per-flag targets (entered in `.github/coverage-manifest/Encina.AzureEventHubs.json` in the pull request that adds the tests of each flag: unit and guard with the file in pull requests 1 to 5, contract, property and integration in pull request 6; measured with the commands of AGENTS.md section 9):

| File | unit | guard | contract | property | integration |
|------|:---:|:---:|:---:|:---:|:---:|
| `AzureEventHubsMessagePublisher.cs` | 85 | 20 | 50 | 20 | 70 |
| `AzureEventHubsConsumer.cs` (the processor host and handler pipeline) | 85 | 15 | 40 | 0 | 65 |
| `EncinaAzureEventHubsOptions.cs`, `...OptionsValidator.cs` | 100 | 0 | 0 | 0 | 0 |
| `ServiceCollectionExtensions.cs` | 85 | 55 | 0 | 0 | 0 |
| `Health/AzureEventHubsHealthCheck.cs` | 90 | 40 | 0 | 0 | 50 |
| `Diagnostics/*.cs` | 90 | 0 | 0 | 0 | 0 |
| `IAzureEventHubsMessagePublisher.cs`, `Log.cs` | 0 | 0 | 0 | 0 | 0 |

| File and flag | Justification |
|---------------|---------------|
| Publisher unit 85, integration 70 | Unit tests substitute the producer for every branch; integration tests publish, batch and read back through the emulator, leaving the service-busy and timeout branches that the emulator does not raise. |
| Consumer unit 85, contract 40, integration 65 | Unit tests drive retry, dead letter and checkpoint over the seam; integration tests run partitions, restart and the dead-letter hub on the emulator; ownership-loss and error-callback branches need injected faults. |
| Publisher guard 20, contract 50, property 20 | Guard tests reach only the argument checks; the conformance base runs the publish and subscribe scenarios; the property test covers the envelope fields only. |
| Options and validator unit 100 | Pure data and validation. |
| Health check integration 50 | One real properties call; the failure branch needs a stopped emulator. |
| Interface, `Log.cs` 0 | No executable lines (interface) or generated delegates; guard tests cannot reach either. |

1. `src/Encina.AzureEventHubs/README.md`, `docs/features/azure-event-hubs.md`, the Event Hubs section of `docs/messaging/transports.md` and `docs/INVENTORY.md` through `docs-writer`; changelog fragment `changelog.d/611-azure-event-hubs.added.md` through `mechanical-fixer`.
2. Load and benchmark: justification files `tests/Encina.LoadTests/Messaging/AzureEventHubs/AzureEventHubs.md` and `tests/Encina.BenchmarkTests/Messaging/AzureEventHubs/AzureEventHubs.md` (throughput is the service's; the Redis.PubSub precedent has the same two files).
3. `dotnet build --configuration Release` with zero warnings; every flag at its target; CRAP of every new method at most 10.

</details>

<details>
<summary>Prompt for AI Agents — Phase 7</summary>

```text
CONTEXT: The package is implemented and tested; documentation and the justification files remain.
TASK: Enter the manifest justifications, write the two justification files, and hand the README, feature page, transports page, INVENTORY and changelog fragment to the owning specialists.
KEY RULES: targets never go down; no hand-typed coverage figures in docs; never edit CHANGELOG.md; docs through docs-writer, fragments through mechanical-fixer.
REFERENCE FILES: tests/Encina.LoadTests/Messaging/RedisPubSub/RedisPubSub.md, .github/coverage-manifest/Encina.AzureServiceBus.json, changelog.d/README.md.
```

</details>

---

## Research

### Relevant Standards & Specifications

| Standard / Spec | Relevance | Notes |
|-----------------|-----------|-------|
| `AGENTS.md` sections 3, 5, 6, 7, 9 | Secrets in options, background loops, transport category, 12 functions, EventIds, coverage | Phases 1 to 7 |
| SPEC-000 DEC-009 | #611 is a 1.0 transport tested with the Event Hubs emulator; cloud services without an emulator are out | Scope, Design Choice 7 |
| ADR-062, ADR-054 D2 and D4 | Error categories; trace links and tenant tags | Phases 2, 4 and 5 |
| Azure Event Hubs concepts (partitions, consumer groups, checkpoints) | Delivery model and failure handling | Design Choices 3 to 5 |

### Existing Encina Infrastructure to Leverage

| Component | Location | Usage in this feature |
|-----------|----------|-----------------------|
| `Encina.AzureServiceBus` | `src/Encina.AzureServiceBus/` | Template for options, DI, publisher and health check |
| `EncinaHealthCheck`, `ProviderHealthCheckOptions` | `src/Encina.Messaging/Health/` | Health check base and options |
| `ServiceBusEmulatorFixture`, `AzuriteFixture` | `tests/Encina.TestInfrastructure/Fixtures/` | Pattern for an emulator fixture with a docker network (`AzuriteFixture` itself has no network and is not reused) |
| `OutboxActivitySource` | `src/Encina.Messaging/Diagnostics/OutboxActivitySource.cs` | Pattern for the source and meter |
| `EventIdRanges` and the registry | `src/Encina/Diagnostics/EventIdRanges.cs`, `docs/engineering/eventid-block-registry.md` | New field `AzureEventHubs`; zone rows |

### Event ID Allocation

| Package | Range | Notes |
|---------|-------|-------|
| `AzureEventHubs` (new) | 40040-40059 | Third range of the block `40000-40099` of the Transports zone taken by transport-new-2 (`DaprPubSub` 40000-40019 is #242's, `ZeroMQ` 40020-40039 is #610's); ids 40040-40052 used (40040-40042 publish, 40043-40044 batch, 40045-40052 consume), 40053-40059 spare. The block `9750-9799` of the batch snapshot is registered as `SecurityBreakTheGlass` on `origin/main` (status `superseded` in the registry). Verified free by a search of the registry and of every `docs/plan-*` branch for `40000`. |

### Estimated File Count

| Category | Files | Notes |
|----------|------:|-------|
| Production code | 13 | csproj, options, validator, interface, publisher, consumer host, processor seam, error codes, `Log`, DI, health check, 2 diagnostics |
| Tests | 13 | options, DI, publisher, message id, batch, consumer, dead letter, checkpoint, health, diagnostics, guard, property, conformance subclass, integration, fixture |
| Shared edits | 10 | `Encina.slnx`, `Directory.Packages.props` (twice), 6 test `.csproj` files, `EventIdRanges.cs`, allocation test, `Encina.OpenTelemetry` |
| Documentation | 9 | README, feature page, transports page, INVENTORY, registry rows, integration-tests page, changelog, 2 justification files |
| **Pull request 1** | **12** | csproj, options, validator, DI, `Encina.slnx`, `Directory.Packages.props`, `UnitTests.csproj`, `GuardTests.csproj`, manifest, options test, DI test, guard test; about 450 lines |
| **Pull request 2** | **13** | interface, publisher, error codes, `Log`, `EventIdRanges.cs`, allocation test, registry, DI edit, DI test edit, 2 unit tests, guard edit, manifest edit; about 700 lines |
| **Pull request 3** | **6** | interface edit, publisher edit, `Log` edit, unit test, guard edit, manifest edit; about 350 lines |
| **Pull request 4** | **12** | interface edit, consumer host, processor seam, error codes edit, `Log` edit, DI edit, DI test edit, 3 unit tests, guard edit, manifest edit; about 900 lines |
| **Pull request 5** | **9** | health check, 2 diagnostics, DI edit, `Encina.OpenTelemetry` edit, 2 unit tests, guard edit, manifest edit; about 500 lines |
| **Pull request 6** | **13** | emulator fixture, `TestInfrastructure.csproj`, `Directory.Packages.props`, 2 collection classes, conformance subclass, integration test, property test, `ContractTests.csproj`, `IntegrationTests.csproj`, `PropertyTests.csproj`, integration-tests page, manifest; about 900 lines |
| **Pull request 7** | **8** | README, feature page, transports page, INVENTORY, changelog fragment, 2 justification files, manifest justifications; about 350 lines |

Implementers and callers: `IAzureEventHubsMessagePublisher` is new, so it has no implementers or `new X(` sites to update; the shared edits are additive. Pull request 1 registers options and their validator, pull request 2 the producer and the scoped publisher, pull request 4 the processor seam, pull request 5 the health check. Each pull request builds alone in order 1 to 7; pull request 6 also needs #240 pull request 5 (the conformance base).

---

## Combined AI Agent Prompts

<details>
<summary>Combined prompt for all phases</summary>

```text
PROJECT CONTEXT: Encina is a pre-1.0 .NET 10 / C# 14 library; no backward compatibility, zero warnings, ROP (Either<EncinaError, T>). Transports are publisher-only except MQTT and Redis.PubSub; Encina.AzureEventHubs adds a streaming transport on Azure.Messaging.EventHubs, tested with the official emulator.

IMPLEMENTATION OVERVIEW: PR1 package, options, DI; PR2 publish with partition keys and the EventId range AzureEventHubs 40040-40059; PR3 batch publishing; PR4 processor consumer with blob checkpoints, retry and dead lettering; PR5 health check and telemetry; PR6 emulator fixture, conformance subclass, property and integration tests; PR7 documentation and justification files. Dilemmas D2, D3, D4, D5, D8, D10, D11 and D12 are implemented as the maintainer answers, the recommended options until then.

KEY PATTERNS: at-least-once, never success on a failure and never a silent skip; handler failures retry in place then dead-letter or stall the partition; named ADR-062 category on every error; constant error messages; ids, payloads and connection strings never logged; connection strings [JsonIgnore] with ToString override; targets never go down; CRAP <= 10.

REFERENCE FILES: src/Encina.AzureServiceBus, src/Encina.Messaging/Health, tests/Encina.TestInfrastructure/Fixtures/ServiceBusEmulatorFixture.cs, src/Encina/Diagnostics/EventIdRanges.cs.
```

</details>

---

## Cross-Cutting Integration Matrix

| # | Function | Status | Notes |
|---|----------|--------|-------|
| 1 | Caching | ❌ | A transport does not cache messages. |
| 2 | OpenTelemetry | ✅ | Per-package `ActivitySource` and `Meter` (Design Choice 11); the shared design of #725 may rename them. |
| 3 | Structured Logging | ✅ | `[LoggerMessage]` in `Log.cs`, EventIds 40040-40052 of the range 40040-40059; error codes and exception types only. |
| 4 | Health Checks | ✅ | `AzureEventHubsHealthCheck` reads the event hub properties. |
| 5 | Validation | ❌ | Options are validated at startup; payload validation belongs to the validation behaviors of the consuming handler. |
| 6 | Resilience | ⏭️ | The SDK retries transient faults by itself (its own retry options); one retry layer per ADR-050 and #742 decide whether Encina adds a decorator, recorded in the issue file `transport-new-2-tenant-module-and-resilience-adoption`. Handler failures retry in place (Design Choice 3). |
| 7 | Distributed Locks | ❌ | The processor balances partitions through blob ownership records; no lock provider is involved. |
| 8 | Transactions | ❌ | The checkpoint is the only commit point; no unit of work is involved. |
| 9 | Idempotency | ✅ | The message id travels as `EventData.MessageId`; processing idempotency stays with the inbox (replay after a crash is possible). |
| 10 | Multi-Tenancy | ⏭️ | Tenant-aware partition routing and properties need #1164 and #2385; issue file `transport-new-2-tenant-module-and-resilience-adoption`. |
| 11 | Module Isolation | ⏭️ | Same issue file. |
| 12 | Audit Trail | ❌ | Transport is infrastructure; audit belongs to the outbox and inbox. |

---

## Prerequisites & Dependencies

- None blocking pull request 1. Pull requests 2 to 7 follow in order; pull request 6 needs #240 pull request 5 (the conformance base).
- **Related, not blocking**: #1609 (the other transports gain subscribe with the shape chosen in D2 and D3), #251, #252 and #2385 (contracts the new transports adopt through the issue file), #725 and #742 (telemetry and resilience of all transports), #1164, #602 (schema registry abstraction; the Azure half is out of 1.0).
- **Sibling plans**: #242, #610, #612 and #310 of this family and the five plans of transport-new-1 answer the same D2, D3 and D11 questions, with the same recommended answers.

---

## Next Steps

1. Ask the maintainer D2, D3, D4, D5, D8, D10, D11 and D12 when work on #611 starts; record the answers in `## Maintainer Decisions`.
2. Pull request 1: package skeleton (12 files).
3. Pull requests 2 to 5: publish (13), batch (6), consume (12), health and telemetry (9).
4. Pull requests 6 and 7: emulator, contract, property and integration tests (13), documentation (8).
