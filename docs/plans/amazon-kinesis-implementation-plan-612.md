# Implementation Plan: Encina.AmazonKinesis, an Amazon Kinesis Data Streams transport

> **Issue**: [#612](https://github.com/dlrivada/Encina/issues/612)
> **Type**: Feature
> **Verified against**: origin/main `ce352001f` (2026-11-23)
> **Complexity**: High (8 phases, 8 pull requests of about 11, 14, 7, 14, 9, 10, 9 and 7 files)
> **Estimated Scope**: about 1,400 lines of production code and about 2,600 lines of tests
> **Family**: transport-new-2 (#242 Dapr, #610 ZeroMQ, #611 Azure Event Hubs, #612 Amazon Kinesis, #310 Event Mesh). Sibling family transport-new-1 (#237, #238, #239, #240, #241) owns the shared transport conformance base (#240); "Shared pieces" below says which plan owns what.

---

## Summary

The issue asks for an Amazon Kinesis Data Streams transport: a producer with partition keys and `PutRecords` batches, a consumer on the Kinesis Client Library with checkpoints in DynamoDB, optional enhanced fan-out, a health check, OpenTelemetry, `ServiceCollection` extensions, tests against LocalStack and documentation. State of `origin/main`:

- There is no `Encina.AmazonKinesis` project, and `Directory.Packages.props` pins neither `AWSSDK.Kinesis` nor `AWSSDK.DynamoDBv2` (it pins `AWSSDK.SQS` 4.0.2.17, `AWSSDK.SimpleNotificationService` 4.0.2.19, `AWSSDK.SecretsManager`, `AWSSDK.KeyManagementService` and `Testcontainers.LocalStack` 4.15.0). There is no `IMessageTransport` in `src/` (grep), so the sketch in the issue does not match the code: each transport has its own `I<Name>MessagePublisher` with a `messageId` parameter, validated options and an `AddEncina<Name>` method, and `src/Encina.AmazonSQS` is the template (`EncinaAmazonSQSOptions` with `AllowInsecureHttp` and `AllowLocalEndpoints`, `EndpointValidator`, `TryAddSingleton` clients, `TryAddScoped` publisher, `AmazonSQSHealthCheck`). Only MQTT and Redis.PubSub subscribe; #1609 tracks the gap.
- Verified on 2026-11-23 by downloading the packages from NuGet: **AWSSDK.Kinesis 4.1.1** (Apache-2.0; targets net10.0) has `IAmazonKinesis.PutRecordAsync`, `PutRecordsAsync` (`PutRecordsRequestEntry` with `Data`, `PartitionKey`, `ExplicitHashKey`), `ListShardsAsync`, `GetShardIteratorAsync`, `GetRecordsAsync`, `DescribeStreamSummaryAsync`, `RegisterStreamConsumerAsync` and `SubscribeToShardAsync`; **AWSSDK.DynamoDBv2 4.1.0.3** (the lease and checkpoint table). The package `Amazon.Kinesis.ClientLibrary` named in the issue does **not exist** on NuGet (checked); the only .NET package of that name family is the third-party `Kinesis.ClientLibrary.Net` 0.0.10, not published by AWS. The KCL itself is a Java library, so the issue's "KCL consumer" cannot be taken literally (Design Choice 4).
- Scope: SPEC-000 section 6 keeps cloud items with a free local emulator inside 1.0 and names transport #612 tested on LocalStack (SPEC-000 line 171, DEC-009). The repository already runs a pinned LocalStack (`LocalStackFixture`, image `localstack/localstack:4.14.0`, no auth token, #2699 decision D2) with the collection `Cloud-LocalStack`. The issue is planned, not skipped. The fixture today creates SQS and SNS clients only; the first pull request of this plan that needs Kinesis adds the clients, and whether LocalStack supports each Kinesis call used (resharding, `SubscribeToShard`) is recorded by a probe test at the start of pull request 7.
- Kinesis is a log of shards, not a queue: no per-record acknowledgement, no redelivery, no dead letter queue; a consumer reads a shard in order and records a checkpoint (a sequence number). A record of at most 1 MiB is addressed by a partition key that hashes to a shard.

Delivered: package `Encina.AmazonKinesis` with `IAmazonKinesisMessagePublisher` (publish, batch publish and subscribe, Design Choice 2), `EncinaAmazonKinesisOptions` (validated endpoint), `AddEncinaAmazonKinesis`, a consumer with a DynamoDB lease and checkpoint table, in-place retry and dead lettering (Design Choices 4 and 5), a health check, an `ActivitySource` and `Meter`, log messages in the new range `AmazonKinesis` 40060-40079, unit, guard, property, contract and integration tests against LocalStack, and documentation.

**Provider category**: Messaging transports (AGENTS.md section 5). The transport rule requires send/publish, subscription management, error handling with DLQ and metadata propagation; each is covered below. The cloud-tied ones are tested with local emulators.

**Shared pieces (family)**: the shared conformance base is owned by #240 (this plan subclasses it in pull request 7). `LocalStackFixture` is edited additively here and by #238 (EventBridge, transport-new-1) and by the DynamoDB lock plan #210; a conflict is resolved by keeping both additions. The EventId block `40000-40099` of the Transports zone is shared by the five plans (Event ID Allocation). The follow-up issue for tenant and module headers, adoption of #251, #252 and #2385, and the resilience decorators of the new transports is `artifacts/issues/transport-new-2-tenant-module-and-resilience-adoption.md`, owned by the #242 plan of this family.

---

## Design Choices

<details>
<summary><strong>1. Package placement</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) New package `Encina.AmazonKinesis`** | Pay-for-what-you-use; its own options, health check and EventId range; the name the issue gives, next to `Encina.AmazonSQS` | One more package |
| **B) Inside `Encina.AmazonSQS`** | No new package | Mixes a queue service and a log; different clients, failure model and permissions |

### Chosen Option: **A, a new package**

### Rationale

The issue names `Encina.AmazonKinesis`, SPEC-000 DEC-009 names #612 as a 1.0 transport, and every other transport is its own package.

**Class:** Settled
**Settled by:** #612 (package name) and SPEC-000 DEC-009

</details>

<details>
<summary><strong>2. Public interface: one publisher interface or a publisher and a consumer (Dilemma D2)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) One `IAmazonKinesisMessagePublisher` with `PublishAsync`, `PublishBatchAsync` and `SubscribeAsync`, the shape of `IMQTTMessagePublisher` and `IRedisPubSubMessagePublisher`** | Coherent with the two transports that already subscribe and with the siblings (#240 asks the same question and recommends A); #1609 asks the other transports for the same shape | The name says "publisher" for a consuming member |
| **B) Two interfaces, a publisher and a consumer** | Separate concerns; a producer-only application registers no consumer | Different from MQTT and Redis.PubSub; the split would have to be repeated by every transport |

### Chosen Option: **A, one interface with all members** (recommended, pending the maintainer)

### Rationale

A wins when the category is judged by coherence (one call shape for every transport, #1609); B wins when producers and consumers live in different processes and the consumer needs its own lifetime. The same question is open in #237, #239, #240, #241 and in this family and the answer must be the same.

**Class:** Dilemma

</details>

<details>
<summary><strong>3. The handler contract (Dilemma D3)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `Func<TMessage, ValueTask>` as in `SubscribeAsync` of MQTT and Redis.PubSub; an exception means failure and follows the retry and dead-letter rule of Design Choice 5** | Same call shape as the two existing subscribers; order inside a shard is kept | Business failure travels as an exception, against Railway Oriented Programming |
| **B) `Func<TMessage, CancellationToken, ValueTask<Either<EncinaError, Unit>>>`; a `Left` or an exception is a failure with the same rule** | A `Left` from `IEncina.Send` maps straight; the failure carries an error code | A new shape that the existing two subscribers do not have |

### Chosen Option: **A, the existing shape** (recommended, pending the maintainer)

### Rationale

Under either option a failure is never reported as success and a record is never skipped silently (AGENTS.md section 3). A wins for coherence with the two existing subscribers; B wins when handlers are thin wrappers over `IEncina.Send` returning `Either`.

**Class:** Dilemma

</details>

<details>
<summary><strong>4. The consumer: what replaces the KCL (Dilemma D4)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) An Encina consumer on `AWSSDK.Kinesis`: a DynamoDB table of leases and checkpoints (hash key `ShardId`; attributes owner, lease expiry from `TimeProvider`, checkpoint sequence number, parent shard ids) with conditional writes; instances balance shards, take over expired leases, and read a child shard only after its parents are drained (resharding); the table is created only when `CreateLeaseTableIfMissing` is set (opt-in, default false)** | Meets the issue's "consumer with checkpointing" and balances and fails over like the KCL; no extra runtime | Encina owns a lease algorithm and its tests (two pull requests); reads by polling `GetRecords` |
| **B) Own consumer without coordination: each instance reads the shards named in the options (or all shards) and checkpoints in DynamoDB; two instances on one shard process it twice** | Much smaller; no lease algorithm | No balancing and no failover; scaling out is manual and duplicates records |
| **C) The third-party `Kinesis.ClientLibrary.Net` 0.0.10** | No algorithm to write | An unofficial 0.0.x package outside the repository's control; its runtime model is not known to this plan |

### Chosen Option: **A, an Encina lease-based consumer** (recommended, pending the maintainer)

### Rationale

A wins for a reliability library that must scale out and survive an instance loss; B wins for a single-instance consumer, or a first release that wants the smallest surface (pull request 5 of this plan is dropped); C wins only if its maintenance is judged acceptable. Under A the lease renewal and shard sync loops follow AGENTS.md section 3: they catch per iteration, never let an exception escape, and cancellation ends them; time comes from `TimeProvider`. The shard ownership uses the lease table, not `IDistributedLockProvider`: the DynamoDB lock provider (#210) is open and not on `origin/main`.

**Class:** Dilemma

</details>

<details>
<summary><strong>5. Failure handling and the dead-letter destination (Dilemma D5)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) In-place retry up to `MaxHandlerAttempts` with a capped backoff, then the record is published to a dead-letter stream (`DeadLetterStreamName`, opt-in) with its attributes plus `dead-reason` (an error code, never an exception message) and `delivery-attempts`, and the checkpoint advances; with no dead-letter stream configured the shard stops at the failing record (no checkpoint, logged every attempt) instead of skipping it** | Same service and tooling; order inside the shard is kept until dead lettering | A second stream to create and pay for |
| **B) The failed record goes to an SQS queue (`DeadLetterQueueUrl`, opt-in) through `Encina.AmazonSQS`'s client** | A real queue with redrive and its own dead-letter policy | A cross-service permission and a dependency on the SQS package; the record's 1 MiB can exceed what the queue accepts (SQS: 256 KiB) |

### Chosen Option: **A, a dead-letter stream** (recommended, pending the maintainer)

### Rationale

A wins when failed records are reprocessed by the same kind of consumer; B wins when operations already run SQS dead-letter tooling and records are small. If the dead-letter write fails the record is retried again and the checkpoint does not advance (`encina.kinesis.dead_letter_failed`).

**Class:** Dilemma

</details>

<details>
<summary><strong>6. Enhanced fan-out (Dilemma D6)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Not in this plan: the criterion is optional; the first release polls `GetRecords` (shared throughput of 2 MiB/s per shard); enhanced fan-out is a follow-up issue** | Keeps the consumer to one read path; LocalStack coverage of `SubscribeToShard` is uncertain | The optional criterion stays open |
| **B) Add `RegisterStreamConsumer` and `SubscribeToShardAsync` as an option (`UseEnhancedFanOut`)** | Dedicated throughput per consumer, push delivery | A second read path with its own HTTP/2 event stream, re-subscription every five minutes and its own failure handling; tests may be unit-only if LocalStack lacks it |

### Chosen Option: **A, a follow-up issue** (recommended, pending the maintainer)

### Rationale

A wins for a first release; B wins when several consumers read the same stream and shared throughput is the limit. The issue file is `artifacts/issues/amazon-kinesis-enhanced-fan-out.md`.

**Class:** Dilemma

</details>

<details>
<summary><strong>7. Partial failure of a batch put (Dilemma D7)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) A `Left` (`encina.kinesis.batch_failed`, with the failed count in the details) when `FailedRecordCount` is above zero; never a partial `Right`; the caller (the outbox) retries the batch and consumers deduplicate on the message id** | One result type, as `SendBatchAsync` of `IAmazonSQSMessagePublisher`; no silent loss | Records that succeeded are sent again on retry |
| **B) A `Right` with a per-record result list the caller inspects** | The caller retries only the failed records | A new result type; a caller that ignores the list loses records |

### Chosen Option: **A, a `Left` for any failed record** (recommended, pending the maintainer)

### Rationale

A wins for ROP coherence and for outbox retry semantics; B wins when a producer sends very large batches and throttling is routine. The same question is open for EventBridge in #238 (its Design Choice on partial failure).

**Class:** Dilemma

</details>

<details>
<summary><strong>8. The default partition key (Dilemma D8)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) When the caller gives no key, the message id is the partition key** | Records spread evenly across shards; no hot shard | No ordering across records without an explicit key |
| **B) A constant key (`default`)** | Total order for a low-volume stream | Every record lands on one shard: a hot shard and a throughput cap of 1 MiB/s |

### Chosen Option: **A, the message id** (recommended, pending the maintainer)

### Rationale

A wins for throughput, the reason to use Kinesis; B wins for a low-volume stream whose consumers need one global order. The issue's `PublishAsync(message, partitionKey)` makes the key a required argument; this plan makes it optional so the `messageId` convention of the other publishers applies.

**Class:** Dilemma

</details>

<details>
<summary><strong>9. Endpoint, region and credentials</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Ambient AWS credentials and `Region`, as `Encina.AmazonSQS`; an optional `ServiceUrl` for emulators validated by `EndpointValidator` (https, no loopback, link-local or metadata address) unless `AllowInsecureHttp` or `AllowLocalEndpoints` is set (the documentation states the relaxations; the validator itself does not log)** | No secret in options; the same safety rule as the SQS package | The emulator needs two flags |
| **B) Keys in the options** | Simple | A secret in configuration |

### Chosen Option: **A, ambient credentials and a validated endpoint**

### Rationale

`src/Encina.AmazonSQS/EncinaAmazonSQSOptions.cs` and `EncinaAmazonSQSOptionsValidator.cs` (the `EndpointPolicy.ForHttps(allowInsecureHttp, allowLocalEndpoints)` call) are the convention for the AWS transports. The options hold no secret.

**Class:** Trivial
**Settled by:** `src/Encina.AmazonSQS/EncinaAmazonSQSOptions.cs` and `src/Encina.AmazonSQS/EncinaAmazonSQSOptionsValidator.cs`

</details>

<details>
<summary><strong>10. Envelope and metadata propagation</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) The record data is one UTF-8 JSON envelope `{ "id", "type", "traceparent", "headers", "payload" }` (Kinesis records have no attributes), the partition key per Design Choice 8; a new message id only when the caller gives none** | The only place Kinesis offers for metadata; the same message-id rule as the other publishers | The envelope is part of the 1 MiB limit; a consumer outside Encina must know the envelope |
| **B) The bare payload with metadata in a side channel** | Smaller records | There is no side channel in Kinesis |

### Chosen Option: **A, a JSON envelope in the record data**

### Rationale

Every existing publisher takes `messageId` and generates one only when null or empty (`src/Encina.NATS/INATSMessagePublisher.cs`, `src/Encina.AmazonSQS/IAmazonSQSMessagePublisher.cs`); the outbox passes its row id there so consumers deduplicate. A record above the limit is `encina.kinesis.record_too_large` (`Validation`, R3) before AWS is called. Tenant and module headers are not stamped here (Design Choice 13).

**Class:** Trivial
**Settled by:** the `messageId` convention of `src/Encina.NATS/INATSMessagePublisher.cs` and `src/Encina.AmazonSQS/IAmazonSQSMessagePublisher.cs`

</details>

<details>
<summary><strong>11. Integration test infrastructure</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) The shared `LocalStackFixture` (collection `Cloud-LocalStack`, image pinned to 4.14.0, no token), extended with Kinesis and DynamoDB clients; the collection classes that already exist per assembly are reused** | One container for all AWS emulator tests; the acceptance criterion names LocalStack | A probe test is needed for the calls LocalStack may not support |
| **B) A new per-class LocalStack container** | Isolated | Forbidden by the shared-fixture rule; slower |

### Chosen Option: **A, the shared LocalStack collection**

### Rationale

The issue lists "Integration tests (LocalStack)", SPEC-000 DEC-009 says the cloud transports are tested against their local emulator, and AGENTS.md section 9 forbids a per-class fixture for integration tests. `LocalStackCollection` exists in `tests/Encina.IntegrationTests/MessageBrokers/AmazonSQS/LocalStackCollection.cs` and in the ContractTests project (grep of `CollectionDefinition`); test classes of this package use `[Collection(LocalStackFixture.CollectionName)]` and need no new collection class.

**Class:** Settled
**Settled by:** #612 acceptance criteria, SPEC-000 DEC-009 and AGENTS.md section 9 (shared collection fixtures)

</details>

<details>
<summary><strong>12. OpenTelemetry: per-package source or the shared transport design of #725 (Dilemma D12)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `ActivitySource` and `Meter` named `Encina.AmazonKinesis` in the package, registered by name in `Encina.OpenTelemetry`; the W3C `traceparent` travels in the record envelope** | Meets the issue's cross-cutting row now; the same pattern as `Encina.Messaging.Outbox` | The name list in `Encina.OpenTelemetry` is edited by every transport plan |
| **B) Wait for the shared transport instrumentation of #725** | One design for all transports | #725's plan is merged but its code is not; the row would stay open |

### Chosen Option: **A, per-package instrumentation** (recommended, pending the maintainer)

### Rationale

A wins because the issue's matrix marks OpenTelemetry as integrated; B wins if #725 should rename every source at once. Spans link producer and consumer (ADR-054 D2) with no baggage; no tenant tag is emitted until Design Choice 13 is done (ADR-054 D4).

**Class:** Dilemma

</details>

<details>
<summary><strong>13. Multi-tenancy and module headers (Dilemma D13)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Defer to an issue: tenant-aware shard routing and tenant and module envelope entries need the persisted request context (#1164) and the route metadata (#2385)** | No invented scheme; one design for all transports | Not in this plan |
| **B) Use the tenant id as the partition key now** | Early per-tenant ordering | Needs the reserved `default` tenant segment and an injective encoding no transport has defined; hot tenants make hot shards |

### Chosen Option: **A, defer to a follow-up issue** (recommended, pending the maintainer)

### Rationale

A wins because one design for all transports needs #1164 and #2385; B wins for a deployment that needs per-tenant ordering before those land. The issue file is `artifacts/issues/transport-new-2-tenant-module-and-resilience-adoption.md`.

**Class:** Dilemma

</details>

---

## Implementation Phases

### Phase 1: Package, options, endpoint validation and DI (pull request 1)

<details>
<summary>Tasks</summary>

1. `src/Encina.AmazonKinesis/Encina.AmazonKinesis.csproj` (net10.0; packages `AWSSDK.Kinesis` 4.1.1 and `AWSSDK.DynamoDBv2` 4.1.0.3 (new pins in `Directory.Packages.props`, `Directory.Packages.props` has no `AWSSDK.Core` pin, so the transitive core version is whatever the SQS pins already resolve together with these two; the pull request records the restore result), `Microsoft.Extensions.*.Abstractions`, `LanguageExt.Core`; project references to `Encina` and `Encina.Messaging`; `InternalsVisibleTo Encina.UnitTests`) and the `Encina.slnx` entry. Transport packages carry no `PublicAPI.*.txt` today (none in AmazonSQS, NATS, Redis.PubSub), so none is added.
2. `EncinaAmazonKinesisOptions` (namespace `Encina.AmazonKinesis`, public sealed): `Region` (default `us-east-1`), `ServiceUrl`, `AllowInsecureHttp`, `AllowLocalEndpoints`, `StreamName`, `DeadLetterStreamName` (null: none), `ConsumerName`, `LeaseTableName`, `CreateLeaseTableIfMissing` (default false), `MaxHandlerAttempts`, `MaxRecordsPerRead`, `LeaseDuration`, `ProviderHealthCheck`. It holds no secret (ambient credentials); `ToString()` prints the stream name only. Internal `EncinaAmazonKinesisOptionsValidator : IValidateOptions<>`: the endpoint rule of Design Choice 9 (it does not log: the package has no log messages until pull request 2, where the range is registered), non-blank names, positive counts and durations.
3. `ServiceCollectionExtensions.AddEncinaAmazonKinesis(this IServiceCollection, Action<EncinaAmazonKinesisOptions>? configure = null)`: options with `ValidateOnStart`, the validator through `TryAddEnumerable`, `TryAddSingleton<IAmazonKinesis>` from the options (an application's own client wins); `IAmazonDynamoDB` is registered only in pull request 4 with the consumer, so a publish-only application takes no DynamoDB client (AGENTS.md section 1); a DI test that builds the provider with `ValidateOnBuild` and `ValidateScopes`.
4. Tests: `AmazonKinesisOptionsAndRegistrationTests` (unit: options, validator, DI), `AmazonKinesisGuardTests` (guard: null `services`, null `configure` accepted); manifest `.github/coverage-manifest/Encina.AmazonKinesis.json` with the unit and guard targets of these files and, in the same file, their one-sentence justifications at file and package level (`coverage-report.cs --check-justifications` fails a target without one); every later pull request adds the targets and justifications of the files and flags it introduces; `UnitTests.csproj` and `GuardTests.csproj` ProjectReference edits.

</details>

<details>
<summary>Prompt for AI Agents — Phase 1</summary>

```text
CONTEXT: Encina.AmazonSQS is the template (options, validator with the endpoint policy, DI). AWSSDK.Kinesis 4.1.1 and AWSSDK.DynamoDBv2 4.1.0.3 are Apache-2.0. Amazon.Kinesis.ClientLibrary does not exist on NuGet.
TASK: Create the Encina.AmazonKinesis package skeleton with validated options and AddEncinaAmazonKinesis, and their unit and guard tests.
KEY RULES: no secret in options (ambient credentials); the endpoint validator never prints the URL; TryAdd registrations only; a DI test with ValidateOnBuild and ValidateScopes; no Obsolete, no compatibility code.
REFERENCE FILES: src/Encina.AmazonSQS/*.cs, src/Encina/Validation/EndpointValidator.cs, .github/coverage-manifest/Encina.AmazonSQS.json, docs/engineering/eventid-block-registry.md.
```

</details>

### Phase 2: Publishing and the envelope (pull request 2)

<details>
<summary>Tasks</summary>

1. `IAmazonKinesisMessagePublisher.PublishAsync<TMessage>(TMessage message, string? partitionKey = null, string? streamName = null, string? messageId = null, IReadOnlyDictionary<string,string>? headers = null, CancellationToken cancellationToken = default) where TMessage : class` returning `ValueTask<Either<EncinaError, string>>` (the message id). `AmazonKinesisMessagePublisher` (scoped; `IAmazonKinesis`, `IOptions`, `ILogger`): `PutRecordAsync` with the envelope of Design Choice 10 and the key of Design Choice 8.
2. Errors (ADR-062), in `AmazonKinesisErrorCodes.cs` (one constants class, so later pull requests add their codes there), each created with the named argument `category:` and a constant message: `encina.kinesis.publish_failed` `Internal` (R4), `encina.kinesis.publish_throttled` `RateLimited` (`ProvisionedThroughputExceededException` is a rate limit; ADR-062 R7 maps `rate_limited` to the outbound table, and `ErrorCategory.RateLimited` exists), `encina.kinesis.record_too_large` `Validation` (R3), `encina.kinesis.stream_not_configured` `Internal` (R1). Cancellation propagates as `OperationCanceledException`, never a `Left` (R5). The exception goes to `ForLogging()`, never its message.
3. `Log.cs` 40060-40062 (publishing, published, publish failed; no message ids, no payload, no partition keys). `EventIdRanges.AmazonKinesis = (40060, 40079)`; the line `static readonly Encina.Diagnostics.EventIdRanges.AmazonKinesis -> (int Min, int Max)` in `src/Encina/PublicAPI.Unshipped.txt` (through `mechanical-fixer`); the `AssemblyRanges` entry in `EncinaEventIdAllocationTests`; `docs/engineering/eventid-block-registry.md` (through `docs-writer`): the row `| AmazonKinesis | 40060-40079 | Transports | zone |` in the registered ranges table and the Blocks row `| 40060-40079 | Transports | transport-new-2: #612 | registered |` (exact bounds, as `EventIdRegistryAgreementTests` rules 2 and 3 require; the other four plans of the family add their own disjoint rows, so a conflict is resolved by keeping both, never by a rebase of a pushed branch). DI: `TryAddScoped<IAmazonKinesisMessagePublisher, AmazonKinesisMessagePublisher>()` (registration completeness, AGENTS.md section 3); the DI test resolves it with `ValidateOnBuild` and `ValidateScopes`.
4. Tests: `AmazonKinesisMessagePublisherTests` and `AmazonKinesisEnvelopeTests` (unit over a substituted `IAmazonKinesis`), registration test edit, guard edit; manifest edit with the unit and guard targets (contract and integration targets are entered in pull request 7).

</details>

<details>
<summary>Prompt for AI Agents — Phase 2</summary>

```text
CONTEXT: The package skeleton exists. Publishing follows Encina.AmazonSQS: a messageId parameter, generated only when null or empty. Kinesis records have no attributes, so metadata lives in a JSON envelope in the data.
TASK: Implement IAmazonKinesisMessagePublisher.PublishAsync with the envelope, the partition key default, the size check, the error codes class, the log messages 40060-40062 and the EventId range AmazonKinesis, with unit and guard tests.
KEY RULES: EncinaErrors.Create with the named category: argument (ADR-062); constant messages, never ex.Message; ids, payloads and partition keys never logged; cancellation propagates; the size is checked before AWS is called.
REFERENCE FILES: src/Encina.AmazonSQS/AmazonSQSMessagePublisher.cs, src/Encina/Diagnostics/EventIdRanges.cs.
```

</details>

### Phase 3: Batch publishing (pull request 3)

<details>
<summary>Tasks</summary>

1. `PublishBatchAsync<TMessage>(IReadOnlyList<TMessage> messages, string? streamName = null, CancellationToken cancellationToken = default)` returning `ValueTask<Either<EncinaError, IReadOnlyList<string>>>` over `PutRecordsAsync` in chunks of at most 500 records and 5 MiB; any `FailedRecordCount` above zero is a `Left` (Design Choice 7), with the failed count in the details and no ids.
2. Code added to `AmazonKinesisErrorCodes.cs`: `encina.kinesis.batch_failed` `Internal` (R4). `Log.cs` 40063-40064 (publishing batch, batch published; counts only).
3. Tests: `AmazonKinesisBatchTests` (unit: chunking, partial failure, oversize record), guard edit, manifest edit.

</details>

<details>
<summary>Prompt for AI Agents — Phase 3</summary>

```text
CONTEXT: Single publish exists. PutRecords accepts up to 500 records and 5 MiB and reports failures per record.
TASK: Implement PublishBatchAsync with chunking and the Left on any failed record, with unit tests.
KEY RULES: never a partial Right; cancellation propagates; counts only in logs; the error code has an ADR-062 category.
REFERENCE FILES: src/Encina.AmazonSQS/AmazonSQSMessagePublisher.cs (SendBatchAsync), AGENTS.md section 3.
```

</details>

### Phase 4: Shard reader, checkpoints and dead lettering (pull request 4)

<details>
<summary>Tasks</summary>

1. `SubscribeAsync<TMessage>(Func<TMessage, ValueTask> handler, string? streamName = null, CancellationToken cancellationToken = default)` returning `ValueTask<IAsyncDisposable>`. Internal pieces: `KinesisShardReader` (`ListShardsAsync`, `GetShardIteratorAsync` from the checkpoint or `TRIM_HORIZON`, a `GetRecordsAsync` loop that backs off when empty or throttled), `IKinesisCheckpointStore` with `DynamoDbKinesisCheckpointStore` (conditional `PutItem` and `UpdateItem` on the lease table; the table is created only when `CreateLeaseTableIfMissing` is set), and the consumer host that runs the handler with the in-place retry and dead lettering of Design Choice 5. In this pull request one instance reads every shard (no coordination yet; pull request 5 adds leases), and a checkpoint advances only after success or a successful dead-letter write. The read loops catch per iteration; the stopping token ends them.
2. Codes added to `AmazonKinesisErrorCodes.cs`: `encina.kinesis.subscribe_failed`, `encina.kinesis.checkpoint_failed`, `encina.kinesis.dead_letter_failed`, `encina.kinesis.deserialize_failed` (an undeserializable record is dead-lettered at once), all `Internal` (R4).
3. `Log.cs` 40065-40071 (subscription started, shard read started, handler failed with attempt, dead lettered, dead letter failed, checkpoint failed, read iteration failed). DI: `TryAddSingleton<IAmazonDynamoDB>` from the options (an application's own client wins) and `TryAddSingleton<IKinesisCheckpointStore, DynamoDbKinesisCheckpointStore>()`; the DI test subscribes with substituted clients.
4. Tests: `KinesisShardReaderTests`, `KinesisCheckpointStoreTests`, `KinesisConsumerTests` (unit over substituted `IAmazonKinesis` and `IAmazonDynamoDB`), registration test edit, guard edit, manifest edit.

</details>

<details>
<summary>Prompt for AI Agents — Phase 4</summary>

```text
CONTEXT: Publishing and batches exist. There is no KCL for .NET: Encina reads shards with GetRecords and keeps checkpoints in a DynamoDB table.
TASK: Implement SubscribeAsync with the shard reader, the DynamoDB checkpoint store, in-place retry, dead lettering to a stream and the log messages, with unit tests.
KEY RULES: a record is never skipped silently: after the attempts it is dead-lettered or the shard stops with no checkpoint; the checkpoint advances only after success or a successful dead-letter write; loops catch per iteration and never let an exception escape; cancellation propagates; dead-reason is an error code, never an exception message; ids and payloads never logged; time through TimeProvider.
REFERENCE FILES: src/Encina.AmazonSQS/AmazonSQSMessagePublisher.cs, src/Encina.Redis.PubSub/RedisPubSubMessagePublisher.cs, AGENTS.md section 3.
```

</details>

### Phase 5: Leases, balancing and resharding (pull request 5)

<details>
<summary>Tasks</summary>

1. `KinesisLeaseManager` (internal): acquire a shard lease with a conditional write (owner, `LeaseDuration`, expiry computed from `TimeProvider`), renew it from a `PeriodicTimer` loop, take over an expired lease, release on dispose, and fair-share balancing with an explicit rebalance. Instances are counted from heartbeat items: each instance writes an item `INSTANCE#<id>` with an expiry in the same table on every renewal tick, so an instance that holds no lease yet is already counted; the share is `ceil(shards / live instances)`. An instance acquires leases only while it holds fewer than its share, and a lease above its share is released: the renewal loop stops renewing the surplus, its reader finishes the batch in flight, checkpoints and releases it (a conditional delete of the owner field), so another instance can take it on its next tick. A test on a fake clock starts one instance, then a second, and asserts that the shards converge to an even split within a bounded number of lease durations and that no shard is read by two instances at once. A lost lease (a conditional write fails) stops that shard's reader without a checkpoint and is logged, never returned to a caller (`encina.kinesis.lease_lost`, `Conflict`, R2).
2. `KinesisShardSync` (internal): a periodic `ListShardsAsync` that creates lease items for new shards and reads a child shard only after every parent shard is closed and fully checkpointed (resharding order).
3. The consumer host of pull request 4 now reads only the shards it holds. Codes added: `encina.kinesis.lease_lost` `Conflict` (R2), `encina.kinesis.shard_sync_failed` `Internal` (R4). `Log.cs` 40072-40076 (lease acquired, lease lost, shard sync iteration failed, shard split observed, lease released). All loops catch per iteration and never let an exception escape (AGENTS.md section 3).
4. Tests: `KinesisLeaseManagerTests` (unit with `FakeTimeProvider`: acquire, renew, expiry takeover, heartbeat counting, fair share and surplus release when a second instance starts, lost lease), `KinesisShardSyncTests` (parent before child), guard edit, manifest edit.

</details>

<details>
<summary>Prompt for AI Agents — Phase 5</summary>

```text
CONTEXT: The single-instance consumer exists. Several instances of one application must share the shards and survive each other's loss.
TASK: Implement the DynamoDB lease manager, fair-share balancing and shard sync with parent-before-child ordering, with unit tests on a fake clock.
KEY RULES: time only through TimeProvider; renewal and sync loops catch per iteration and never let an exception escape; a lost lease stops the reader without a checkpoint; conditional writes only (no read-then-write races); no IDistributedLockProvider.
REFERENCE FILES: src/Encina.Messaging/Scheduling (loops), docs/en/guides/TESTING.md (timer callbacks must not throw), AGENTS.md section 3.
```

</details>

### Phase 6: Health check and observability (pull request 6)

<details>
<summary>Tasks</summary>

1. `Health/AmazonKinesisHealthCheck : EncinaHealthCheck` (`DefaultName = "encina-amazon-kinesis"`, tags `encina`, `messaging`, `amazon-kinesis`, `ready`): `DescribeStreamSummaryAsync` bounded by `ProviderHealthCheck.Timeout`, healthy when the stream is `ACTIVE`; the result text names the check and the exception type only. Registered like `AmazonSQSHealthCheck` when `ProviderHealthCheck.Enabled`.
2. `Diagnostics/AmazonKinesisActivitySource` and `AmazonKinesisMetrics`: spans publish, process, checkpoint, dead-letter with a link from consumer to producer through the envelope `traceparent`; counters `amazon_kinesis.published`, `.processed`, `.dead_lettered`, `.leases_lost` with tags `stream`, `outcome`; no tenant tag. `Encina.OpenTelemetry` gains `AddSource`/`AddMeter` lines.
3. Tests: `AmazonKinesisHealthCheckTests`, `AmazonKinesisDiagnosticsTests` (unit), guard edit, manifest edit; the `Encina.OpenTelemetry` registration test is extended and counted as a file (its manifest targets and justifications stay at today's unit 90 and guard 35).

</details>

<details>
<summary>Prompt for AI Agents — Phase 6</summary>

```text
CONTEXT: Publish, batch and consume exist. The issue requires a health check and traces and metrics.
TASK: Add the health check, the ActivitySource and Meter, and register the source and meter names in Encina.OpenTelemetry.
KEY RULES: health results and tags never contain an exception message; spans link, never baggage (ADR-054 D2); zero cost when no listener; no manifest target is lowered.
REFERENCE FILES: src/Encina.AmazonSQS/Health/AmazonSQSHealthCheck.cs, src/Encina.Messaging/Diagnostics/OutboxActivitySource.cs, src/Encina.OpenTelemetry/ServiceCollectionExtensions.cs.
```

</details>

### Phase 7: LocalStack, contract, property and integration tests (pull request 7)

<details>
<summary>Tasks</summary>

1. `LocalStackFixture` gains `IAmazonKinesis` and `IAmazonDynamoDB` clients and creates the test stream and lease table names (additive; `ClearAllDataAsync` also deletes the Kinesis streams and the DynamoDB tables the tests create); `TestInfrastructure.csproj` references `AWSSDK.Kinesis` and `AWSSDK.DynamoDBv2`. The pull request starts with a probe test that records what LocalStack supports (`ListShards`, `GetRecords`, `SplitShard`, `SubscribeToShard`, conditional DynamoDB writes); a feature LocalStack lacks is covered at unit level and listed in the pull request.
2. `tests/Encina.ContractTests/MessageBrokers/AmazonKinesis/AmazonKinesisConformanceTests.cs`, a subclass of `TransportConformanceContractBase` of #240 (publish with message id, subscribe delivers, handler failure retried then dead-lettered, cancellation) in `[Collection(LocalStackFixture.CollectionName)]`; `ContractTests.csproj` reference.
3. `tests/Encina.IntegrationTests/MessageBrokers/AmazonKinesis/AmazonKinesisIntegrationTests.cs` (`[Trait("Category","Integration")]`, `[Trait("Transport","AmazonKinesis")]`): partition-key ordering, batch publish, two consumers sharing shards through the lease table, takeover after an expired lease, restart resumes from the checkpoint, dead-letter stream content; `IntegrationTests.csproj` reference.
4. `tests/Encina.PropertyTests/Messaging/AmazonKinesis/AmazonKinesisEnvelopePropertyTests.cs` (FsCheck round trip of the envelope and the size limit) and `PropertyTests.csproj` reference.
5. One manifest edit entering the contract, property and integration targets of the table of Phase 8 together with their one-sentence justifications, the package-level contract and integration targets and justifications included (`--check-justifications`).

</details>

<details>
<summary>Prompt for AI Agents — Phase 7</summary>

```text
CONTEXT: The conformance base of #240 is merged; LocalStack runs in the shared Cloud-LocalStack collection and the repository's pinned image needs no token.
TASK: Extend the LocalStack fixture with Kinesis and DynamoDB clients, write the probe, the conformance subclass, the integration tests and the property test.
KEY RULES: reuse the shared collection and its pinned image, never a per-class container; ClearAllDataAsync-style cleanup in InitializeAsync (unique stream and table names per test class); no Thread.Sleep (bounded waits on TaskCompletionSource); Shouldly through Encina.Testing.Shouldly; a feature LocalStack lacks is asserted at unit level and listed in the PR.
REFERENCE FILES: tests/Encina.TestInfrastructure/Fixtures/LocalStackFixture.cs, tests/Encina.IntegrationTests/MessageBrokers/AmazonSQS/*.cs, tests/Encina.ContractTests/MessageBrokers/AmazonSQS/*.cs.
```

</details>

### Phase 8: Documentation and finalization (pull request 8)

<details>
<summary>Tasks</summary>

Per-flag targets, each written with its one-sentence justification in `.github/coverage-manifest/Encina.AmazonKinesis.json` in the same pull request that adds the tests of the flag (unit and guard with the file in pull requests 1 to 6, contract, property and integration in pull request 7; this pull request adds no target); the table is the final state, measured with the commands of AGENTS.md section 9:

| File | unit | guard | contract | property | integration |
|------|:---:|:---:|:---:|:---:|:---:|
| `AmazonKinesisMessagePublisher.cs` | 85 | 20 | 50 | 20 | 70 |
| `KinesisShardReader.cs`, consumer host | 85 | 15 | 40 | 0 | 65 |
| `DynamoDbKinesisCheckpointStore.cs` | 85 | 0 | 0 | 0 | 70 |
| `KinesisLeaseManager.cs`, `KinesisShardSync.cs` | 90 | 0 | 0 | 0 | 60 |
| `EncinaAmazonKinesisOptions.cs`, `...OptionsValidator.cs` | 100 | 0 | 0 | 0 | 0 |
| `ServiceCollectionExtensions.cs` | 85 | 55 | 0 | 0 | 0 |
| `Health/AmazonKinesisHealthCheck.cs` | 90 | 40 | 0 | 0 | 50 |
| `Diagnostics/*.cs` | 90 | 0 | 0 | 0 | 0 |
| `IAmazonKinesisMessagePublisher.cs`, `Log.cs` | 0 | 0 | 0 | 0 | 0 |

| File and flag | Justification |
|---------------|---------------|
| Publisher unit 85, integration 70 | Unit tests substitute `IAmazonKinesis` for every branch; integration tests put, batch and read back on LocalStack, leaving throttling and service-error branches the emulator does not raise. |
| Reader and consumer unit 85, integration 65 | Unit tests drive retry, dead letter and checkpoint over substituted clients; integration tests run real shards, restart and the dead-letter stream; iterator-expiry and throttling branches need injected faults. |
| Checkpoint store integration 70 | Real conditional writes on LocalStack's DynamoDB; the failure branches need a failing table. |
| Lease manager unit 90, integration 60 | A fake clock reaches acquire, renew, expiry and fair share; the integration test covers two real consumers, leaving race windows only a fault injector reaches. |
| Publisher guard 20, contract 50, property 20 | Guard tests reach only the argument checks; the conformance base runs the publish and subscribe scenarios; the property test covers the envelope only. |
| Options and validator unit 100 | Pure data and validation. |
| Health check integration 50 | One real stream summary; the failure branch needs a missing stream. |
| Interface, `Log.cs` 0 | No executable lines (interface) or generated delegates; guard tests cannot reach either. |

1. `src/Encina.AmazonKinesis/README.md`, `docs/features/amazon-kinesis.md`, the Kinesis section of `docs/messaging/transports.md` and `docs/INVENTORY.md` through `docs-writer`; changelog fragment `changelog.d/612-amazon-kinesis.added.md` through `mechanical-fixer`.
2. Load and benchmark: justification files `tests/Encina.LoadTests/Messaging/AmazonKinesis/AmazonKinesis.md` and `tests/Encina.BenchmarkTests/Messaging/AmazonKinesis/AmazonKinesis.md` (throughput is the service's; the Redis.PubSub precedent has the same two files).
3. `dotnet build --configuration Release` with zero warnings; every flag at its target; CRAP of every new method at most 10.

</details>

<details>
<summary>Prompt for AI Agents — Phase 8</summary>

```text
CONTEXT: The package is implemented and tested; documentation and the justification files remain.
TASK: Check that every manifest target already has its justification (run coverage-report.cs --check-justifications), write the two justification files, and hand the README, feature page, transports page, INVENTORY and changelog fragment to the owning specialists.
KEY RULES: targets never go down; no hand-typed coverage figures in docs; never edit CHANGELOG.md; docs through docs-writer, fragments through mechanical-fixer.
REFERENCE FILES: tests/Encina.LoadTests/Messaging/RedisPubSub/RedisPubSub.md, .github/coverage-manifest/Encina.AmazonSQS.json, changelog.d/README.md.
```

</details>

---

## Research

### Relevant Standards & Specifications

| Standard / Spec | Relevance | Notes |
|-----------------|-----------|-------|
| `AGENTS.md` sections 3, 5, 6, 7, 9 | Background loops, transport category, 12 functions, EventIds, coverage | Phases 1 to 8 |
| SPEC-000 DEC-009 | #612 is a 1.0 transport tested with LocalStack | Scope |
| ADR-062, ADR-054 D2 and D4 | Error categories; trace links and tenant tags | Phases 2, 4 and 6 |
| Amazon Kinesis Data Streams concepts (shards, partition keys, iterators, resharding, leases as in the KCL) | Delivery model, ordering and the consumer algorithm | Design Choices 4 to 8 |

### Existing Encina Infrastructure to Leverage

| Component | Location | Usage in this feature |
|-----------|----------|-----------------------|
| `Encina.AmazonSQS` | `src/Encina.AmazonSQS/` | Template for options, endpoint validator, DI, publisher and health check |
| `EndpointValidator` | `src/Encina/Validation/EndpointValidator.cs` | Endpoint safety for the emulator URL |
| `EncinaHealthCheck`, `ProviderHealthCheckOptions` | `src/Encina.Messaging/Health/` | Health check base and options |
| `LocalStackFixture`, `LocalStackCollection` | `tests/Encina.TestInfrastructure/Fixtures/LocalStackFixture.cs`, `tests/Encina.IntegrationTests/MessageBrokers/AmazonSQS/LocalStackCollection.cs` | Shared emulator (extended with Kinesis and DynamoDB clients) |
| `OutboxActivitySource` | `src/Encina.Messaging/Diagnostics/OutboxActivitySource.cs` | Pattern for the source and meter |
| `EventIdRanges` and the registry | `src/Encina/Diagnostics/EventIdRanges.cs`, `docs/engineering/eventid-block-registry.md` | New field `AmazonKinesis`; zone rows |

### Event ID Allocation

| Package | Range | Notes |
|---------|-------|-------|
| `AmazonKinesis` (new) | 40060-40079 | Fourth range of the block `40000-40099` of the Transports zone taken by transport-new-2 (`DaprPubSub` 40000-40019 is #242's, `ZeroMQ` 40020-40039 is #610's, `AzureEventHubs` 40040-40059 is #611's); ids 40060-40076 used (40060-40062 publish, 40063-40064 batch, 40065-40071 consume, 40072-40076 leases), 40077-40079 spare. The block `9750-9799` of the batch snapshot is registered as `SecurityBreakTheGlass` on `origin/main` (status `superseded` in the registry). Verified free by a search of the registry and of every `docs/plan-*` branch for `40000`. |

### Estimated File Count

| Category | Files | Notes |
|----------|------:|-------|
| Production code | 17 | csproj, options, validator, interface, publisher, error codes, `Log`, DI, shard reader, checkpoint store (interface and DynamoDB class), consumer host, lease manager, shard sync, health check, 2 diagnostics |
| Tests | 15 | options and registration, publisher, envelope, batch, reader, checkpoint, consumer, lease, shard sync, health, diagnostics, guard, property, conformance subclass, integration |
| Shared edits | 11 | `Encina.slnx`, `Directory.Packages.props`, `LocalStackFixture`, `TestInfrastructure.csproj`, 5 test `.csproj` files, `EventIdRanges.cs`, allocation test, `Encina.OpenTelemetry` |
| Documentation | 8 | README, feature page, transports page, INVENTORY, registry rows, changelog, 2 justification files |
| **Pull request 1** | **11** | csproj, options, validator, DI, `Encina.slnx`, `Directory.Packages.props`, `UnitTests.csproj`, `GuardTests.csproj`, manifest, one unit test file, guard test; about 450 lines |
| **Pull request 2** | **14** | interface, publisher, error codes, `Log`, `EventIdRanges.cs`, `src/Encina/PublicAPI.Unshipped.txt` edit, allocation test, registry, DI edit, 2 unit tests, registration test edit, guard edit, manifest edit; about 750 lines |
| **Pull request 3** | **7** | interface edit, publisher edit, error codes edit, `Log` edit, unit test, guard edit, manifest edit; about 350 lines |
| **Pull request 4** | **14** | interface edit, reader, checkpoint store interface, DynamoDB checkpoint store, consumer host, error codes edit, `Log` edit, DI edit, registration test edit, 3 unit tests, guard edit, manifest edit; about 1,100 lines |
| **Pull request 5** | **9** | lease manager, shard sync, consumer host edit, error codes edit, `Log` edit, 2 unit tests, guard edit, manifest edit; about 800 lines |
| **Pull request 6** | **10** | health check, 2 diagnostics, DI edit, `Encina.OpenTelemetry` edit, `Encina.OpenTelemetry` registration test edit, 2 unit tests, guard edit, manifest edit; about 500 lines |
| **Pull request 7** | **9** | `LocalStackFixture` edit, `TestInfrastructure.csproj`, conformance subclass, integration test, property test, `ContractTests.csproj`, `IntegrationTests.csproj`, `PropertyTests.csproj`, manifest; about 900 lines |
| **Pull request 8** | **7** | README, feature page, transports page, INVENTORY, changelog fragment, 2 justification files; about 350 lines |

Implementers and callers: `IAmazonKinesisMessagePublisher` is new, so it has no implementers or `new X(` sites to update; `LocalStackFixture` is edited additively (callers use its existing members). Pull request 1 registers options, validator and the two AWS clients, pull request 2 the scoped publisher, pull request 4 the checkpoint store, pull request 6 the health check. Each pull request builds alone in order 1 to 8; pull request 7 also needs #240 pull request 5 (the conformance base). If the maintainer chooses option B of Design Choice 4, pull request 5 is dropped.

---

## Combined AI Agent Prompts

<details>
<summary>Combined prompt for all phases</summary>

```text
PROJECT CONTEXT: Encina is a pre-1.0 .NET 10 / C# 14 library; no backward compatibility, zero warnings, ROP (Either<EncinaError, T>). Transports are publisher-only except MQTT and Redis.PubSub; Encina.AmazonKinesis adds a streaming transport on AWSSDK.Kinesis with an Encina lease-based consumer (the KCL is a Java library), tested on LocalStack.

IMPLEMENTATION OVERVIEW: PR1 package, options, endpoint validation, DI; PR2 publish with envelope and the EventId range AmazonKinesis 40060-40079; PR3 PutRecords batches; PR4 shard reader, DynamoDB checkpoints, retry and dead-letter stream; PR5 leases, balancing and resharding; PR6 health check and telemetry; PR7 LocalStack fixture, conformance subclass, property and integration tests; PR8 documentation. Dilemmas D2, D3, D4, D5, D6, D7, D8, D12 and D13 are implemented as the maintainer answers, the recommended options until then.

KEY PATTERNS: at-least-once, never success on a failure and never a silent skip; loops catch per iteration; time through TimeProvider; named ADR-062 category on every error; constant error messages; ids, payloads and partition keys never logged; no secret in options; targets never go down; CRAP <= 10.

REFERENCE FILES: src/Encina.AmazonSQS, src/Encina.Messaging/Health, tests/Encina.TestInfrastructure/Fixtures/LocalStackFixture.cs, src/Encina/Diagnostics/EventIdRanges.cs.
```

</details>

---

## Cross-Cutting Integration Matrix

| # | Function | Status | Notes |
|---|----------|--------|-------|
| 1 | Caching | ❌ | A transport does not cache messages. |
| 2 | OpenTelemetry | ✅ | Per-package `ActivitySource` and `Meter` (Design Choice 12); the shared design of #725 may rename them. |
| 3 | Structured Logging | ✅ | `[LoggerMessage]` in `Log.cs`, EventIds 40060-40076 of the range 40060-40079; error codes and exception types only. |
| 4 | Health Checks | ✅ | `AmazonKinesisHealthCheck` describes the stream. |
| 5 | Validation | ❌ | Options are validated at startup; payload validation belongs to the validation behaviors of the consuming handler. |
| 6 | Resilience | ⏭️ | The AWS SDK retries transient faults by itself; one retry layer per ADR-050, and #742 decides whether Encina adds a decorator, recorded in the issue file `transport-new-2-tenant-module-and-resilience-adoption`. Handler failures retry in place (Design Choice 5). |
| 7 | Distributed Locks | ✅ | Shard ownership is the DynamoDB lease table with conditional writes (Design Choice 4); the DynamoDB lock provider (#210) is open and is not used. |
| 8 | Transactions | ❌ | The checkpoint is the only commit point; no unit of work is involved. |
| 9 | Idempotency | ✅ | The message id travels in the envelope; processing idempotency stays with the inbox (replay after a crash or a lease loss is possible). |
| 10 | Multi-Tenancy | ⏭️ | Tenant-aware shard routing and envelope entries need #1164 and #2385; issue file `transport-new-2-tenant-module-and-resilience-adoption`. |
| 11 | Module Isolation | ⏭️ | Same issue file. |
| 12 | Audit Trail | ❌ | Transport is infrastructure; audit belongs to the outbox and inbox. |

---

## Prerequisites & Dependencies

- None blocking pull request 1. Pull requests 2 to 8 follow in order; pull request 7 needs #240 pull request 5 (the conformance base).
- **Related, not blocking**: #1609 (the other transports gain subscribe with the shape chosen in D2 and D3), #251, #252 and #2385 (contracts the new transports adopt through the issue files), #725 and #742 (telemetry and resilience of all transports), #1164, #210 (the DynamoDB lock provider, open), #238 (EventBridge, which also edits `LocalStackFixture`).
- **Sibling plans**: #242, #610, #611 and #310 of this family and the five plans of transport-new-1 answer the same D2, D3 and D12 questions, with the same recommended answers.

---

## Next Steps

1. Ask the maintainer D2, D3, D4, D5, D6, D7, D8, D12 and D13 when work on #612 starts; record the answers in `## Maintainer Decisions`.
2. Pull request 1: package skeleton (11 files).
3. Pull requests 2 to 6: publish and the EventId range (14), batch (7), reader and checkpoints (14), leases (9), health and telemetry (10).
4. Pull requests 7 and 8: LocalStack contract, property and integration tests (9), documentation (8).
