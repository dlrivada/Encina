# Implementation Plan: Encina.GoogleCloudPubSub, a Google Cloud Pub/Sub message transport

> **Issue**: [#237](https://github.com/dlrivada/Encina/issues/237)
> **Type**: Feature
> **Verified against**: origin/main `0336d31c7` (2026-11-20)
> **Complexity**: High (7 phases, 7 pull requests of about 13, 12, 10, 9, 8, 3 and 8 files)
> **Estimated Scope**: about 1,000 lines of production code and about 2,000 lines of tests
> **Family**: transport-new-1 (#237, #238, #239, #240, #241). Sibling family transport-new-2 (#242, #610, #611, #612, #310) runs later; "Shared pieces" below says which plan owns what.

---

## Summary

The issue adds Google Cloud Pub/Sub as a transport: publisher, consumer (subscriber), message ordering with ordering keys, dead-letter topics, message filtering, exactly-once delivery, push and pull subscriptions, a health check, OpenTelemetry and integration tests with the emulator. State of `origin/main`:

- Scope: SPEC-000 section 6.1 keeps cloud items that have a free local emulator inside 1.0 and names transport #237 with the Pub/Sub emulator (SPEC-000 line 171, DEC-009); the Google Cloud Functions gap (#205, REQ-028) is a different feature. The issue is therefore planned, not skipped.
- Transports are publisher-only except MQTT and Redis.PubSub (`SubscribeAsync`); #1609 tracks the gap. There is no `IMessageTransport` in `src/` (verified by grep); each transport has its own `I<Name>MessagePublisher` with a `messageId` parameter, validated options and `AddEncina<Name>` (`src/Encina.AmazonSQS` is the cloud template: region and queue options, ambient credentials, `AllowLocalEndpoints` for emulators, a health check).
- Verified on 2026-11-20 by downloading the packages from NuGet: **Google.Cloud.PubSub.V1 3.40.0** (Apache-2.0; netstandard2.0; depends on Google.Api.Gax.Grpc 4.13.x and Google.Cloud.Iam.V1; its assembly has `PublisherClient`, `SubscriberClient`, `EnableMessageOrdering`, `OrderingKey`, `DeadLetterPolicy`, `EnableExactlyOnceDelivery`, `Filter`, `EmulatorDetection` and `AckResponse`) and **Testcontainers.PubSub 4.15.0** (the repository's Testcontainers version; targets net10.0). Neither is in `Directory.Packages.props` yet; `Google.Protobuf` 3.36.1 and `Google.Cloud.SecretManager.V1` 2.7.0 already are, so pull request 1 confirms by restore that the Gax dependency range resolves without a downgrade warning.

Delivered: package `Encina.GoogleCloudPubSub` with `IGoogleCloudPubSubMessagePublisher` (publish and subscribe, Design Choice 2), `EncinaGoogleCloudPubSubOptions`, `AddEncinaGoogleCloudPubSub`, an optional resource provisioner, a health check, an `ActivitySource` and `Meter`, log messages in the block `GoogleCloudPubSub` 1080-1089, a `PubSubEmulatorFixture`, unit, guard, contract and integration tests, and documentation. Delivery is at-least-once at the Encina level (Pub/Sub exactly-once delivery narrows duplicates inside one region, it does not replace the inbox); consumers deduplicate on the message id carried as an attribute.

**Provider category**: Messaging transports (10 existing, 6 planned; AGENTS.md section 5): send/publish, subscription management, error handling with DLQ, metadata propagation. The cloud-tied ones are tested with local emulators.

**Shared pieces (family)**: the shared transport conformance test base is owned by the Redis.Streams plan (#240, its Design Choice 8); this plan subclasses it in its sixth pull request. The family follow-up issue (adoption of the batch #252, priority #251 and outbox-adapter #2385 contracts, tenant and module headers) is `artifacts/issues/new-transports-tenant-and-shared-contracts.md`, written once by the #240 plan.

---

## Design Choices

<details>
<summary><strong>1. Package placement</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) New package `Encina.GoogleCloudPubSub`** | Pay-for-what-you-use; own options, health check, EventId block | One more package |
| **B) Fold into another cloud transport package** | None | No package shares a client or credentials with Google |

### Chosen Option: **A, a new package**

### Rationale

The issue names `Encina.GoogleCloudPubSub` and AGENTS.md section 5 lists GoogleCloudPubSub as a planned 1.0 transport.

**Class:** Settled
**Settled by:** AGENTS.md section 5 (transports table), SPEC-000 DEC-009 and #237

</details>

<details>
<summary><strong>2. Public interface and handler contract (Dilemma D2)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) One `IGoogleCloudPubSubMessagePublisher` with `PublishAsync` and `SubscribeAsync(Func<TMessage, ValueTask> handler, ...)` returning `IAsyncDisposable`; an exception means failure** | Same shape as `IMQTTMessagePublisher` and `IRedisPubSubMessagePublisher`; the shape #1609 asks of the other transports | The name says "publisher" for a consuming member; failure travels as an exception |
| **B) The issue's publisher interface plus a consumer interface, with a handler returning `ValueTask<Either<EncinaError, Unit>>`** | Separate concerns; a `Left` from `IEncina.Send` maps to a nack | Differs from the two existing subscribers |

### Chosen Option: **A, the existing shape** (recommended, pending the maintainer)

### Rationale

Under either option a failure is never reported as success (AGENTS.md section 3): the handler failure nacks the message, Pub/Sub redelivers it, and after the subscription's maximum delivery attempts it goes to the dead-letter topic. A wins for coherence; B wins when handlers wrap `IEncina.Send`. The same question is open in #238 (publish only), #239, #240 and #241 and must get the same answer.

**Class:** Dilemma

</details>

<details>
<summary><strong>3. Subscription kinds: pull only or also push (Dilemma D3)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Streaming pull only, through `SubscriberClient`** | Works in any host and against the emulator; no endpoint to secure; the acceptance criterion is "consumer/subscriber" | Push subscriptions are not covered |
| **B) Also push: a minimal-API endpoint that validates the Google-signed token and dispatches to the handler** | Covers the issue's "push and pull" feature; fits serverless hosts | Needs ASP.NET Core and OIDC token validation (a security gate that must fail closed); the emulator's push support is limited |
| **C) Defer push to its own issue** | Keeps this plan one package | The feature is listed by the issue |

### Chosen Option: **A, pull only; push as a follow-up issue** (recommended, pending the maintainer)

### Rationale

Push subscriptions appear under Features but not in the acceptance criteria or the task list. A wins for a transport package without a web dependency; B wins when the host is Cloud Run or Functions and receiving by HTTP is the deployment model. The follow-up issue file records the design (token validation fails closed, AGENTS.md section 3).

**Class:** Dilemma

</details>

<details>
<summary><strong>4. Message ordering</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Opt-in `EnableMessageOrdering`; `PublishAsync` takes an `orderingKey`; after a failed ordered publish the publisher calls the client's resume for that key and returns a `Left`** | Ordering has a cost (a regional endpoint, a paused key after failure), so it is off by default; the issue's criterion is met | One more option and a resume path |
| **B) Always on** | Simplest | Breaks pay-for-what-you-use; needs a regional endpoint for everyone |

### Chosen Option: **A, opt-in ordering**

### Rationale

Ordering is an acceptance criterion; every feature is opt-in. The ordering key travels as the Pub/Sub ordering key and a validation `Left` (`GOOGLEPUBSUB_ORDERING_DISABLED`, `InvalidRequest`) is returned when a key is given while ordering is off.

**Class:** Settled
**Settled by:** AGENTS.md section 1 (pay-for-what-you-use) and #237 (acceptance criterion: message ordering support)

</details>

<details>
<summary><strong>5. Topics and subscriptions: provisioned elsewhere or by the transport (Dilemma D5)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) The transport never creates resources; infrastructure as code owns topics, subscriptions and the dead-letter policy; the health check verifies they exist** | Least privilege (no admin IAM roles at runtime) | Integration tests need their own provisioning code; a mis-configured subscription has no DLQ |
| **B) Opt-in `AutoCreateResources` (default off): an internal provisioner creates the topic, the dead-letter topic and the subscription with `DeadLetterPolicy`, ordering, filter and exactly-once settings, idempotently** | Gives the emulator tests and small deployments a one-line setup; the DLQ is guaranteed to exist | Needs admin permissions when turned on; one more component to test |

### Chosen Option: **B, opt-in provisioning** (recommended, pending the maintainer)

### Rationale

A wins in production under least privilege; B wins for development, tests and small projects. The two compose: B is off unless asked for. Dead lettering is native (a subscription `DeadLetterPolicy` with `MaxDeliveryAttempts` and a dead-letter topic, as AGENTS.md section 5 requires DLQ support); the integration test proves whether the emulator enforces it (Phase 3 task 4).

**Class:** Dilemma

</details>

<details>
<summary><strong>6. Filtering and exactly-once delivery (Dilemma D6)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Subscription options `SubscriptionFilter` (a filter expression) and `EnableExactlyOnceDelivery`, applied when the provisioner creates the subscription; the consumer uses the acknowledgement result when exactly-once is on and never counts it as the idempotency guarantee** | Both listed features are available; no extra runtime code beyond ack-result handling | Only effective with `AutoCreateResources` or a subscription made elsewhere with the same settings |
| **B) Filtering only** | Smaller | Exactly-once is a feature of the issue |
| **C) Neither; defer to an issue** | Smallest | Both are listed by the issue (filtering is in its task list) |

### Chosen Option: **A, both as subscription settings** (recommended, pending the maintainer)

### Rationale

Neither is an acceptance criterion. A wins because both are subscription properties, not code paths; C wins if the emulator does not support them and the team prefers no untested option. The Phase 3 emulator probe decides which settings get an integration test and which only a unit test.

**Class:** Dilemma

</details>

<details>
<summary><strong>7. Credentials and endpoint</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Application Default Credentials only, no secret in the options; `ProjectId`, `TopicId`, `SubscriptionId`, an optional `EmulatorHost` validated with `EndpointValidator.ValidateHost` and `AllowLocalEndpoints` with a startup warning** | Nothing to leak; the same ambient credential model as SQS | A credential file or key cannot be set from options |
| **B) A credentials JSON or key file path in the options** | Explicit | A secret in options (needs `[JsonIgnore]` and a `ToString()` override) |

### Chosen Option: **A, ambient credentials and a validated emulator host**

### Rationale

`EncinaAmazonSQSOptions` has a region and no credentials, and `AllowInsecureHttp` and `AllowLocalEndpoints` for emulators; the Google client resolves Application Default Credentials itself, so there is nothing to protect.

**Class:** Trivial
**Settled by:** `src/Encina.AmazonSQS/EncinaAmazonSQSOptions.cs` and `src/Encina.AmazonSQS/EncinaAmazonSQSOptionsValidator.cs`

</details>

<details>
<summary><strong>8. Envelope and metadata propagation</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Data is the JSON payload; attributes carry `message-id` (a new id only when null or empty), `message-type` and the caller's headers; a header that breaks the service limits (key length, value length, attribute count) returns a validation `Left` before sending** | Native attributes visible to filters; the same message-id rule as the other publishers | Limits must be mirrored in the validator |
| **B) Everything in one JSON envelope** | One field | Filters cannot see the headers |

### Chosen Option: **A, attributes**

### Rationale

Every publisher takes `messageId` and generates one only when null or empty (`INATSMessagePublisher`, `IRedisPubSubMessagePublisher`); filtering needs attributes. Codes: `GOOGLEPUBSUB_ATTRIBUTE_INVALID` `Validation` (R3).

**Class:** Trivial
**Settled by:** the `messageId` convention of `src/Encina.NATS/INATSMessagePublisher.cs` and `src/Encina.Redis.PubSub/IRedisPubSubMessagePublisher.cs`

</details>

<details>
<summary><strong>9. OpenTelemetry: per-package source or the shared design of #725 (Dilemma D9)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `ActivitySource` and `Meter` named `Encina.GoogleCloudPubSub` in the package, registered by name in `Encina.OpenTelemetry`** | Meets the acceptance criterion now; pattern of `Encina.Messaging.Outbox` | The name list in `Encina.OpenTelemetry` is edited by every transport plan |
| **B) Wait for #725** | One design | #725 has no plan on `origin/main` |

### Chosen Option: **A, per-package instrumentation** (recommended, pending the maintainer)

### Rationale

A wins because the criterion is in this issue; B wins if #725 should rename every source at once. Spans link producer and consumer through message attributes (ADR-054 D2, no baggage); the tenant tag of ADR-054 D4 arrives with the multi-tenancy follow-up.

**Class:** Dilemma

</details>

<details>
<summary><strong>10. Multi-tenancy and module headers (Dilemma D10)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Defer to an issue: tenant and module attributes need #1164 and the route metadata of #2385** | One design for all transports | Not in this plan |
| **B) Stamp a tenant attribute now** | Early | Needs the reserved `default` tenant segment and an injective encoding no transport defines yet |

### Chosen Option: **A, defer** (recommended, pending the maintainer)

### Rationale

A wins because one design for all transports needs the persisted context of #1164 and the route metadata of #2385, and the issue's own table defers it ("Tenant context in headers"); B wins for a tenant-isolated deployment that needs the attribute before those land. The follow-up is an issue file.

**Class:** Dilemma

</details>

---

## Implementation Phases

### Phase 1: Package, options and DI (pull request 1)

<details>
<summary>Tasks</summary>

1. `src/Encina.GoogleCloudPubSub/Encina.GoogleCloudPubSub.csproj` (`Google.Cloud.PubSub.V1`, `Microsoft.Extensions.*.Abstractions`, `LanguageExt.Core`, references to `Encina` and `Encina.Messaging`; `InternalsVisibleTo Encina.UnitTests`), `Encina.slnx` entry, `Directory.Packages.props` entry for `Google.Cloud.PubSub.V1` 3.40.0; the restore must show no downgrade warning for Gax or Protobuf.
2. `EncinaGoogleCloudPubSubOptions`: `ProjectId`, `TopicId`, `SubscriptionId`, `EmulatorHost`, `AllowLocalEndpoints`, `EnableMessageOrdering`, `AutoCreateResources`, `DeadLetterTopicId`, `MaxDeliveryAttempts` (5 to 100, the service range), `SubscriptionFilter`, `EnableExactlyOnceDelivery`, `ProviderHealthCheck`; `ToString()` prints project and topic only; internal validator (ids non-empty and well formed, attempts range, dead-letter topic required when attempts are set).
3. `AddEncinaGoogleCloudPubSub(this IServiceCollection, Action<EncinaGoogleCloudPubSubOptions>? configure = null)` with eager validation and `ValidateOnStart`; a client factory holder with `TryAddSingleton`.
4. Tests: options, validator and DI unit tests (DI with `ValidateOnBuild` and `ValidateScopes`), `GoogleCloudPubSubGuardTests`; manifest; `UnitTests.csproj` and `GuardTests.csproj` references.

</details>

<details>
<summary>Prompt for AI Agents — Phase 1</summary>

```text
CONTEXT: Encina.AmazonSQS is the cloud template (ambient credentials, AllowLocalEndpoints, validator). Google.Cloud.PubSub.V1 3.40.0 is not yet in Directory.Packages.props.
TASK: Create the Encina.GoogleCloudPubSub skeleton with options, validator, client holder and DI, plus unit and guard tests.
KEY RULES: No secret in options; EndpointValidator for EmulatorHost with AllowLocalEndpoints (warning once); TryAdd registrations only; DI test with ValidateOnBuild and ValidateScopes; clients are created lazily, never at registration; no Obsolete.
REFERENCE FILES: src/Encina.AmazonSQS/EncinaAmazonSQSOptions.cs, src/Encina.AmazonSQS/EncinaAmazonSQSOptionsValidator.cs, src/Encina.AmazonSQS/ServiceCollectionExtensions.cs.
```

</details>

### Phase 2: Publishing and ordering (pull request 2)

<details>
<summary>Tasks</summary>

1. `IGoogleCloudPubSubMessagePublisher.PublishAsync<TMessage>(TMessage message, string? topicId = null, string? messageId = null, IReadOnlyDictionary<string,string>? attributes = null, string? orderingKey = null, CancellationToken cancellationToken = default) where TMessage : class` returning `ValueTask<Either<EncinaError, string>>` (the service message id); `GoogleCloudPubSubMessagePublisher` (scoped; client holder, options, logger): JSON data, attributes of Design Choice 8, ordering key of Design Choice 4 with resume after a failed ordered publish.
2. Errors (ADR-062): `GOOGLEPUBSUB_PUBLISH_FAILED` `Internal` (R4), `GOOGLEPUBSUB_TOPIC_REQUIRED` `InvalidRequest` (R3), `GOOGLEPUBSUB_ATTRIBUTE_INVALID` `Validation` (R3), `GOOGLEPUBSUB_ORDERING_DISABLED` `InvalidRequest`; cancellation propagates; constant messages; exception to `ForLogging()`.
3. `Log.cs` 1080-1082; `EventIdRanges.GoogleCloudPubSub = (1080, 1089)`; `AssemblyRanges` entry; `docs/engineering/eventid-block-registry.md` (through `docs-writer`): the row `| GoogleCloudPubSub | 1080-1089 | Transports | historical |` in the registered ranges table and (Zone is `historical` for every range below 10000; `reserved-pre-zone` is only a status of the Blocks table), and the split of the Blocks row `1050-1099` so that `1080-1089` has status `registered` and the free slices keep `reserved-pre-zone`, with no reserved block overlapping a registered range and the `registered` row matching its range exactly, as `EventIdRegistryAgreementTests` requires. The other four transport plans edit neighbouring rows of the same two tables, so the pull requests are merged one at a time and a later one re-splits the remainder row (a new commit on its branch, never a rebase of a pushed branch).
4. Tests: `GoogleCloudPubSubMessagePublisherTests`, `GoogleCloudPubSubMessageIdTests`, guard edit, manifest edit.

</details>

<details>
<summary>Prompt for AI Agents — Phase 2</summary>

```text
CONTEXT: The package skeleton exists. Publishing follows the messageId convention of the other publishers.
TASK: Implement PublishAsync with attributes validation, message id, ordering keys and the resume after a failed ordered publish, with unit and guard tests.
KEY RULES: Named category: argument on every EncinaErrors call; constant messages, never ex.Message; ids and payloads never logged; EventIds only 1080-1089 registered first in EventIdRanges.cs; cancellation propagates; the SDK client sits behind a small internal interface so unit tests can substitute it.
REFERENCE FILES: src/Encina.AmazonSQS/AmazonSQSMessagePublisher.cs, src/Encina.NATS/NATSMessagePublisher.cs, src/Encina/Diagnostics/EventIdRanges.cs, tests/Encina.UnitTests/NATS/Publishing/NATSPublisherMessageIdTests.cs.
```

</details>

### Phase 3: Subscriptions, provisioning and dead letters (pull request 3)

<details>
<summary>Tasks</summary>

1. `SubscribeAsync<TMessage>(Func<TMessage, ValueTask> handler, string? subscriptionId = null, CancellationToken cancellationToken = default)`: a streaming-pull `SubscriberClient` per subscription; success acks, failure nacks; the delivery attempt is read from the message for logging; exceptions are caught per message and never escape; cancellation stops the client.
2. Internal `GoogleCloudPubSubResourceProvisioner` (only when `AutoCreateResources`): creates topic, dead-letter topic and subscription idempotently (an already-exists answer is success); codes `GOOGLEPUBSUB_SUBSCRIBE_FAILED` and `GOOGLEPUBSUB_PROVISION_FAILED`, both `Internal` (R4).
3. `Log.cs` 1083-1087 (subscription started, handler failed, nacked with attempt count, resources provisioned, provisioning failed).
4. Emulator probe (first task of the integration pull request): a Testcontainers test that creates a subscription with the dead-letter policy, the filter and exactly-once and records which the emulator accepts and enforces; the outcome decides which settings get an integration assertion and which a unit test only.
5. Tests: `GoogleCloudPubSubConsumerTests`, `GoogleCloudPubSubProvisionerTests`, guard edit, manifest edit.

</details>

<details>
<summary>Prompt for AI Agents — Phase 3</summary>

```text
CONTEXT: Publishing exists; consumers must ack, nack and rely on the subscription's dead-letter policy.
TASK: Implement SubscribeAsync over streaming pull and the opt-in resource provisioner.
KEY RULES: A handler failure is never reported as success (nack); no exception escapes the message callback; cancellation stops the subscriber; provisioning is idempotent and off by default; the emulator host replaces credentials only when EmulatorHost is set.
REFERENCE FILES: src/Encina.Redis.PubSub/RedisPubSubMessagePublisher.cs, src/Encina.AmazonSQS/AmazonSQSMessagePublisher.cs, AGENTS.md section 3.
```

</details>

### Phase 4: Health check and observability (pull request 4)

<details>
<summary>Tasks</summary>

1. `Health/GoogleCloudPubSubHealthCheck : EncinaHealthCheck` (`DefaultName = "encina-googlecloudpubsub"`, tags `encina`, `messaging`, `googlecloudpubsub`, `ready`; reads the topic through the admin client within `ProviderHealthCheck.Timeout`; text names the exception type only), registered when enabled.
2. `Diagnostics/GoogleCloudPubSubActivitySource` and `GoogleCloudPubSubMetrics`: spans publish, consume, ack, nack, consumer-to-producer link through message attributes; counters `googlecloudpubsub.published`, `.consumed`, `.nacked` with tags `topic`, `outcome`; `Encina.OpenTelemetry` gains `AddSource`/`AddMeter` lines.
3. Tests: `GoogleCloudPubSubHealthCheckTests`, `GoogleCloudPubSubDiagnosticsTests`; DI edit; manifest edit.

</details>

<details>
<summary>Prompt for AI Agents — Phase 4</summary>

```text
CONTEXT: Publish and consume exist. The issue requires a health check and traces and metrics.
TASK: Add the health check and instrumentation and register the names in Encina.OpenTelemetry.
KEY RULES: No exception message in health results or tags; links not baggage (ADR-054 D2); zero cost without a listener.
REFERENCE FILES: src/Encina.AmazonSQS/Health/AmazonSQSHealthCheck.cs, src/Encina.Messaging/Diagnostics/OutboxActivitySource.cs, src/Encina.OpenTelemetry/ServiceCollectionExtensions.cs.
```

</details>

### Phase 5: Cross-cutting integration

<details>
<summary>Tasks</summary>

1. Verify each row of the matrix below against the code; deferred rows point to the family issue file and to the push-subscription issue file.
2. Confirm every code has its ADR-062 category and no log call carries a message id, payload or `EncinaError.Message`.

</details>

<details>
<summary>Prompt for AI Agents — Phase 5</summary>

```text
CONTEXT: Pub/Sub publishes, consumes, reports health and emits telemetry.
TASK: Verify the cross-cutting matrix against the code.
KEY RULES: AGENTS.md sections 3, 6 and 7; ADR-062 categories.
REFERENCE FILES: AGENTS.md section 6, docs/architecture/adr/062-error-category-contract.md.
```

</details>

### Phase 6: Emulator fixture, integration and contract tests (pull requests 5 and 6)

<details>
<summary>Tasks</summary>

1. Pull request 5 (8 files): `Directory.Packages.props` entry `Testcontainers.PubSub` 4.15.0, `tests/Encina.TestInfrastructure/Fixtures/PubSubEmulatorFixture.cs` (the module's builder; the emulator image tag is pinned explicitly in the fixture, since Google publishes no LTS line, to a current stable Cloud SDK emulators tag), `PubSubEmulatorCollection.cs`, `TestInfrastructure.csproj` and `IntegrationTests.csproj` references, `tests/Encina.IntegrationTests/MessageBrokers/GoogleCloudPubSub/GoogleCloudPubSubIntegrationTests.cs` and `GoogleCloudPubSubFeatureProbeTests.cs` (`[Trait("Category","Integration")]`, `[Trait("Transport","GoogleCloudPubSub")]`): publish and subscribe, ordering, dead letter, filter and exactly-once as far as the probe shows the emulator supports them; manifest.
2. Pull request 6 (3 files): `tests/Encina.ContractTests/MessageBrokers/GoogleCloudPubSub/GoogleCloudPubSubConformanceTests.cs` subclassing the shared conformance base (owned by #240), `ContractTests.csproj` reference, manifest edit; it lands after #240's contract pull request.

</details>

<details>
<summary>Prompt for AI Agents — Phase 6</summary>

```text
CONTEXT: SPEC-000 requires the Pub/Sub transport to be tested against the free local emulator through a shared collection.
TASK: Add the fixture, the collection, the integration and probe tests and the conformance subclass.
KEY RULES: One shared emulator container per collection, never per class; no Thread.Sleep (bounded waits on TaskCompletionSource); Shouldly through Encina.Testing.Shouldly; a feature the emulator does not enforce is asserted at the unit level and named in the test, never skipped silently.
REFERENCE FILES: tests/Encina.TestInfrastructure/Fixtures/LocalStackFixture.cs, tests/Encina.IntegrationTests/MessageBrokers/AmazonSQS/AmazonSQSPublisherIntegrationTests.cs, tests/Encina.IntegrationTests/MessageBrokers/AmazonSQS/LocalStackCollection.cs.
```

</details>

### Phase 7: Documentation and finalization (pull request 7)

<details>
<summary>Tasks</summary>

Per-flag targets (entered in `.github/coverage-manifest/Encina.GoogleCloudPubSub.json` in the pull request that adds the tests of each flag: unit and guard targets with the file in pull requests 1 to 4, the integration targets in pull request 5 and the contract targets in pull request 6, because the publisher is added in pull request 2 but those tests land later):

| File | unit | guard | contract | property | integration |
|------|:---:|:---:|:---:|:---:|:---:|
| `GoogleCloudPubSubMessagePublisher.cs` | 85 | 20 | 50 | 0 | 65 |
| `GoogleCloudPubSubResourceProvisioner.cs` | 85 | 0 | 0 | 0 | 60 |
| `EncinaGoogleCloudPubSubOptions.cs`, `...OptionsValidator.cs` | 100 | 0 | 0 | 0 | 0 |
| `ServiceCollectionExtensions.cs` | 85 | 55 | 0 | 0 | 0 |
| `Health/GoogleCloudPubSubHealthCheck.cs` | 90 | 40 | 0 | 0 | 50 |
| `Diagnostics/*.cs` | 90 | 0 | 0 | 0 | 0 |
| `IGoogleCloudPubSubMessagePublisher.cs`, `Log.cs` | 0 | 0 | 0 | 0 | 0 |

| File and flag | Justification |
|---------------|---------------|
| Publisher unit 85, integration 65 | Unit tests substitute the client seam for each branch; the integration class runs real publish, subscribe, ordering and dead letter on the emulator, leaving connection-failure branches. |
| Provisioner integration 60 | The emulator accepts creation calls; settings it ignores are unit tested only. |
| Publisher guard 20, contract 50 | Guard tests reach only argument checks; the conformance base runs the common scenarios, not ordering or filters. |
| Options and validator unit 100 | Pure data and validation. |
| Health check integration 50 | One real topic read; the failing branch needs a broken service. |
| Interface and `Log.cs` 0 | No executable lines or generated delegates. |

1. `src/Encina.GoogleCloudPubSub/README.md`, `docs/features/google-cloud-pubsub.md`, the Pub/Sub section of `docs/messaging/transports.md` and `docs/INVENTORY.md` through `docs-writer`; changelog fragment `changelog.d/237-google-cloud-pubsub.added.md` through `mechanical-fixer`. Transport packages carry no `PublicAPI.*.txt` today, so none is added.
2. Load and benchmark justification files under `tests/Encina.LoadTests/Messaging/GoogleCloudPubSub/` and `tests/Encina.BenchmarkTests/Messaging/GoogleCloudPubSub/` (throughput belongs to the service).
3. Issue file `artifacts/issues/google-cloud-pubsub-push-subscriptions.md` (push endpoint, D3 option A).
4. `dotnet build --configuration Release` with zero warnings; every flag at its target; CRAP of every new method at most 10.

</details>

<details>
<summary>Prompt for AI Agents — Phase 7</summary>

```text
CONTEXT: The package is implemented and tested; documentation remains.
TASK: Enter the manifest targets with one-sentence justifications, write the two justification files, hand README, feature page, transports page, INVENTORY and the changelog fragment to the owning specialists.
KEY RULES: Targets never go down; no hand-typed coverage figures in docs; never edit CHANGELOG.md.
REFERENCE FILES: tests/Encina.LoadTests/Messaging/RedisPubSub/RedisPubSub.md, .github/coverage-manifest/Encina.AmazonSQS.json, changelog.d/README.md.
```

</details>

---

## Research

### Relevant Standards & Specifications

| Standard / Spec | Relevance | Notes |
|-----------------|-----------|-------|
| `AGENTS.md` sections 1, 3, 5, 6, 7, 9 | Pay-for-what-you-use, background callbacks, transport category, 12 functions, EventIds, coverage | All phases |
| SPEC-000 section 6.1 and DEC-009 | The Pub/Sub transport is in 1.0 and is tested with the emulator | Scope |
| ADR-062, ADR-054 D2 and D4 | Error categories; trace links and tenant tags | Phases 2 to 4 |
| Pub/Sub ordering keys, dead-letter policy, filters, exactly-once, attribute limits (named by the issue) | Native mechanisms | Design Choices 4 to 8 |

### Existing Encina Infrastructure to Leverage

| Component | Location | Usage in this feature |
|-----------|----------|-----------------------|
| `Encina.AmazonSQS` | `src/Encina.AmazonSQS/` | Template for cloud options, validator, DI, health check |
| `EndpointValidator` | `src/Encina/Validation/EndpointValidator.cs` | Emulator host validation |
| `EncinaHealthCheck`, `ProviderHealthCheckOptions` | `src/Encina.Messaging/Health/` | Health check base |
| `LocalStackFixture`, `LocalStackCollection` | `tests/Encina.TestInfrastructure/Fixtures/LocalStackFixture.cs` | Pattern for the emulator fixture and collection |
| `EventIdRanges` | `src/Encina/Diagnostics/EventIdRanges.cs` | New field `GoogleCloudPubSub` |

### Event ID Allocation

| Package | Range | Notes |
|---------|-------|-------|
| `GoogleCloudPubSub` (new) | 1080-1089 | Fourth of five packages of block 1050-1099 (`reserved-pre-zone`, transport-new-1); 1080-1082 publish, 1083-1087 consume and provisioning, 1088-1089 left for health and loop faults; verified free by repo-wide grep |

### Estimated File Count

| Category | Files | Notes |
|----------|------:|-------|
| Production code | 12 | csproj, options, validator, interface, publisher, client holder, provisioner, `Log`, DI, health check, 2 diagnostics |
| Tests | 18 | options, validator, DI, publisher, message id, consumer, provisioner, health, diagnostics, guard, fixture, collection, 2 integration, conformance subclass, manifest |
| Shared edits | 9 | `Encina.slnx`, `Directory.Packages.props` (twice), 4 test `.csproj`, `EventIdRanges.cs`, allocation test, `Encina.OpenTelemetry` |
| Documentation | 9 | README, feature page, transports page, INVENTORY, registry row, changelog, 2 justification files, push issue file (git-ignored) |
| **Pull request 1** | **13** | csproj, options, validator, DI, slnx, `Directory.Packages.props`, 2 test csproj, manifest, 3 unit tests, guard; about 600 lines |
| **Pull request 2** | **12** | interface, publisher, client holder, `Log`, `EventIdRanges.cs`, allocation test, registry row, 2 unit tests, guard edit, manifest edit, DI edit; about 650 lines |
| **Pull request 3** | **10** | interface edit, publisher edit, provisioner, `Log` edit, 2 unit tests, guard edit, manifest edit, options edit, DI edit; about 800 lines |
| **Pull request 4** | **9** | health check, 2 diagnostics, DI edit, `Encina.OpenTelemetry` edit, 2 unit tests, manifest edit; about 500 lines |
| **Pull request 5** | **8** | `Directory.Packages.props`, `TestInfrastructure.csproj`, fixture, collection, `IntegrationTests.csproj`, 2 integration classes, manifest; about 650 lines |
| **Pull request 6** | **3** | conformance subclass, `ContractTests.csproj`, manifest; about 120 lines |
| **Pull request 7** | **8** | README, feature page, transports page, INVENTORY, changelog fragment, 2 justification files, manifest justifications; about 350 lines |

`IGoogleCloudPubSubMessagePublisher` is new, so there are no implementers or `new X(` sites to update. Pull requests 1 to 5 and 7 build alone in order (each needs the previous one); pull request 6 needs the conformance base of #240.

---

## Combined AI Agent Prompts

<details>
<summary>Combined prompt for all phases</summary>

```text
PROJECT CONTEXT: Encina is a pre-1.0 .NET 10 / C# 14 library; no backward compatibility, zero warnings, ROP (Either<EncinaError, T>). Transports are publisher-only except MQTT and Redis.PubSub; Encina.GoogleCloudPubSub adds Google Cloud Pub/Sub, tested with the local emulator (SPEC-000 DEC-009).

IMPLEMENTATION OVERVIEW: PR1 package, options, client holder, DI, client package entry; PR2 publish with attributes, message id, ordering keys and the EventId block 1080-1089; PR3 streaming-pull subscriptions, ack and nack, opt-in resource provisioner with dead-letter policy, filter and exactly-once settings; PR4 health check and telemetry; PR5 emulator fixture, integration tests and the feature probe; PR6 conformance subclass; PR7 documentation. Dilemmas D2, D3, D5, D6 and D9 are implemented as the maintainer answers, the recommended options until then.

KEY PATTERNS: at-least-once, never success on a failure; no exception escapes a callback; named ADR-062 category on every error; constant error messages; ids and payloads never logged; no secret in options; TimeProvider; targets never go down; CRAP <= 10.

REFERENCE FILES: src/Encina.AmazonSQS, src/Encina.NATS, src/Encina.Messaging/Health, tests/Encina.TestInfrastructure/Fixtures/LocalStackFixture.cs, src/Encina/Diagnostics/EventIdRanges.cs.
```

</details>

---

## Cross-Cutting Integration Matrix

| # | Function | Status | Notes |
|---|----------|--------|-------|
| 1 | Caching | ❌ | A transport does not cache messages. |
| 2 | OpenTelemetry | ✅ | Per-package `ActivitySource` and `Meter` (Design Choice 9). |
| 3 | Structured Logging | ✅ | `[LoggerMessage]` in `Log.cs`, EventIds 1080-1089; error codes and exception types only. |
| 4 | Health Checks | ✅ | `GoogleCloudPubSubHealthCheck` reads the topic. |
| 5 | Validation | ✅ | Options and attribute limits are validated before sending; payload validation belongs to the consuming handler's pipeline. |
| 6 | Resilience | ⏭️ | Retry and circuit breaking for transports is #742 (open); the client's own retry settings apply and redelivery is built in. |
| 7 | Distributed Locks | ❌ | A subscription gives each message to one subscriber of the group. |
| 8 | Transactions | ❌ | Acknowledgement based. |
| 9 | Idempotency | ✅ | The message id is carried as an attribute; exactly-once delivery is an opt-in subscription setting; processing idempotency stays with the inbox. |
| 10 | Multi-Tenancy | ⏭️ | Tenant attributes need #1164 and #2385; family issue file. |
| 11 | Module Isolation | ⏭️ | Same issue file. |
| 12 | Audit Trail | ❌ | Transport is infrastructure; audit belongs to the outbox and inbox. |

---

## Prerequisites & Dependencies

- None blocking pull request 1. Pull requests 2 to 5 and 7 follow in order; pull request 6 waits for the conformance base of #240.
- **Related, not blocking**: #1609 (subscribe shape for the other transports), #251, #252 and #2385 (contracts the new transports adopt through the family issue file), #260 (CloudEvents), #725, #742, #1164; the push-subscription issue file of Phase 7.
- **Sibling plans**: #238, #239, #240 and #241; transport-new-2 reuses the same conformance base.

---

## Next Steps

1. Ask the maintainer D2, D3, D5, D6, D9 and D10 when work on #237 starts; record the answers in `## Maintainer Decisions`.
2. Pull request 1: package skeleton (13 files).
3. Pull requests 2 to 4: publish (12), subscribe and provisioning (10), health and telemetry (9).
4. Pull requests 5 to 7: emulator fixture and integration tests (8), conformance subclass (3), documentation (8).
