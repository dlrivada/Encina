# Implementation Plan: Encina.Dapr.PubSub, a Dapr pub/sub transport on the sidecar

> **Issue**: [#242](https://github.com/dlrivada/Encina/issues/242)
> **Type**: Feature
> **Verified against**: origin/main `ce352001f` (2026-11-23)
> **Complexity**: Medium (7 phases, 6 pull requests of about 12, 14, 11, 9, 10 and 8 files)
> **Estimated Scope**: about 800 lines of production code and about 1,800 lines of tests
> **Family**: transport-new-2 (#242 Dapr, #610 ZeroMQ, #611 Azure Event Hubs, #612 Amazon Kinesis, #310 Event Mesh). Sibling family transport-new-1 (#237, #238, #239, #240, #241) owns the shared transport conformance base (#240); "Shared pieces" below says which plan owns what.

---

## Summary

The issue asks for Dapr pub/sub as a transport: a publisher, a subscriber with topic routing, service invocation, CloudEvents handling, bulk publish, dead-letter topics, a health check, traces and metrics, and tests against a Dapr sidecar. State of `origin/main`:

- There is no `Encina.Dapr` project: `Directory.Packages.props` pins `Dapr.Client` and `Dapr.AspNetCore` 1.16.1 but no project under `src/` or `tests/` references them (grep of every `.csproj`). The package `Encina.Dapr` (sidecar client, options, health check, service invocation, state-store backed stores) is planned by #387, which leaves pub/sub to this issue.
- There is no `IMessageTransport` in `src/` (grep); each transport has its own `I<Name>MessagePublisher` (`src/Encina.AmazonSQS/IAmazonSQSMessagePublisher.cs`, `src/Encina.NATS/INATSMessagePublisher.cs`) with a `messageId` parameter, validated options and an `AddEncina<Name>` method. Only MQTT and Redis.PubSub subscribe; #1609 tracks the gap.
- Verified on 2026-11-23 by downloading the packages from NuGet: `Dapr.Client` 1.19.1 (Apache-2.0; net10.0) has `DaprClient.PublishEventAsync`, `BulkPublishEventAsync`, `CheckHealthAsync` and `GetMetadataAsync`; `Dapr.Messaging` 1.19.1 (Apache-2.0; net10.0) has `IDaprPublishSubscribeClient` (publish with `PublishOptions`: `Id`, `Source`, `Type`, `Subject`, `TraceParent`, `TraceState`, `Metadata`), `TopicResponseAction` (`Success`, `Retry`, `Drop`), `DeadLetterOptions`, and in its assembly `DaprPublishSubscribeClient`, `DaprSubscriptionOptions`, `MessageHandlingPolicy`, `TopicMessage`, `TopicMessageHandler` and `IDaprSubscription` (the streaming subscription). `Dapr.Messaging` depends on `Grpc.AspNetCore` 2.80.0, so a consumer of the subscribe side takes the ASP.NET shared framework. No Testcontainers module for Dapr exists on NuGet (checked); the sidecar image is `daprio/daprd` (Apache-2.0) run through the already pinned generic `Testcontainers` 4.15.0.
- Scope: the issue is in SPEC-000 section 6 scope. DEC-009 and REQ-030 put the new transports in 1.0, AGENTS.md section 5 lists Dapr among the six planned transports, and Dapr needs no cloud service (a local `daprd` with an in-memory pub/sub component), so the issue is planned, not skipped.

Delivered: package `Encina.Dapr.PubSub` (Design Choice 1) with `IDaprMessagePublisher` (publish and subscribe, Design Choice 3), `EncinaDaprPubSubOptions`, `AddEncinaDaprPubSub`, a health check, an `ActivitySource` and `Meter`, log messages in the new range `DaprPubSub` 40000-40019, unit, guard, property, contract and integration tests (a real `daprd` sidecar), and documentation. Delivery is at-least-once: the sidecar redelivers until the handler answers `Success`, so consumers deduplicate on the message id the CloudEvent carries (the inbox, AGENTS.md section 3).

**Provider category**: Messaging transports (10 existing, 6 planned; AGENTS.md section 5). The transport rule requires send/publish, subscription management, error handling with DLQ and metadata propagation; each is covered below.

**Shared pieces (family)**: `Encina.Dapr` (client registration, options, endpoint and token, health, service invocation) is owned by #387; this plan only adds the pub/sub package on top of it. The shared transport conformance base (`TransportConformanceContractBase`) is owned by #240; this plan subclasses it in pull request 5. The follow-up issue that makes the new transports adopt #251, #252 and #2385 is owned by #240; this family's own follow-up issue file is `artifacts/issues/transport-new-2-tenant-module-and-resilience-adoption.md`. The EventId block `40000-40099` of the Transports zone is shared by the five plans of this family (Event ID Allocation).

---

## Design Choices

<details>
<summary><strong>1. Package placement and ownership with #387 (Dilemma D1)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) A new package `Encina.Dapr.PubSub` that references `Encina.Dapr` from #387; this plan starts after #387 pull requests 1 and 2 are merged** | One owner per package (core and state store in #387, transport here); pay-for-what-you-use: a state-store user takes no streaming gRPC; the sidecar options exist once | #242 pull request 1 waits for #387; the issue names the package `Encina.Dapr` |
| **B) Pub/sub inside `Encina.Dapr`, as the issue says** | One package, no waiting for a second project | Two plans edit one project; every `Encina.Dapr` user takes `Dapr.Messaging` and its `Grpc.AspNetCore` dependency |
| **C) A standalone `Encina.Dapr.PubSub` with its own `DaprClient` registration and endpoint options** | Independent of #387 | Two registrations of the sidecar endpoint and token; two health checks of one sidecar |

### Chosen Option: **A, `Encina.Dapr.PubSub` on top of `Encina.Dapr`** (recommended, pending the maintainer)

### Rationale

A wins when the maintainer keeps #387's boundary (its Design Choice 1 also recommends A, and writes "Pub/Sub through #242"); B wins if one Dapr package and one release train is preferred over footprint; C wins only if #242 must ship before #387. Under every option the endpoint, token and health probe of the sidecar are written once, in `Encina.Dapr`.

**Class:** Dilemma

</details>

<details>
<summary><strong>2. Registration of the Dapr client</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `AddEncinaDaprPubSub` calls `AddEncinaDapr()` itself (its registrations are `TryAdd`, so an application's own client wins)** | One call is enough; no resolution failure at the first publish | The application's `AddEncinaDapr` configuration must come from the same call chain |
| **B) The application must call `AddEncinaDapr` first; a missing client fails at the first publish** | Smaller method | A missing dependency is found at run time, not at startup |

### Chosen Option: **A, the pub/sub registration adds the client registration**

### Rationale

AGENTS.md section 3 requires an `AddEncina*` method to register every option type and dependency it resolves, proven by a DI test that builds the provider with `ValidateOnBuild` and `ValidateScopes`; only A satisfies it.

**Class:** Settled
**Settled by:** AGENTS.md section 3 (registration completeness)

</details>

<details>
<summary><strong>3. Public interface: one publisher interface or a publisher and a subscriber (Dilemma D3)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) One `IDaprMessagePublisher` with `PublishAsync` and `SubscribeAsync`, the shape of `IMQTTMessagePublisher` and `IRedisPubSubMessagePublisher`** | Coherent with the two transports that already subscribe and with the siblings (#240 asks the same question and recommends A); #1609 asks the other transports for the same shape | The name says "publisher" for a consuming member |
| **B) The issue's two interfaces, `IDaprMessagePublisher` and `IDaprMessageSubscriber`** | Separate concerns; a publish-only application registers no subscriber | Different from MQTT and Redis.PubSub; the same split would have to be repeated by the other transports |

### Chosen Option: **A, one interface with both members** (recommended, pending the maintainer)

### Rationale

A wins when the category is judged by coherence (one call shape for every transport, #1609); B wins when publishing and consuming live in different processes and the subscriber needs its own lifetime. The same question is open in #237, #239, #240 and #241 and in this family (#610, #611, #612); the answer must be the same in all. `IDaprServiceInvoker`, which the issue lists with the two interfaces, is not part of this choice (Design Choice 8).

**Class:** Dilemma

</details>

<details>
<summary><strong>4. The handler contract and failure semantics (Dilemma D4)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `Func<TMessage, ValueTask>` as in `SubscribeAsync` of MQTT and Redis.PubSub; a normal return is `TopicResponseAction.Success`, an exception is `Retry`** | Same call shape as the two existing subscribers | Business failure travels as an exception, against Railway Oriented Programming |
| **B) `Func<TMessage, CancellationToken, ValueTask<Either<EncinaError, Unit>>>`; a `Left` of category `Validation` or `InvalidRequest` is `Drop`, any other `Left` or an exception is `Retry`** | A `Left` from `IEncina.Send` maps straight to a response; poison messages are dropped (and dead lettered, Design Choice 6) at once | A new shape that the existing two subscribers do not have; the mapping by category is a new rule |

### Chosen Option: **A, the existing shape** (recommended, pending the maintainer)

### Rationale

Under either option a failure is never reported as success (AGENTS.md section 3) and the sidecar, not Encina, owns the redelivery count. A wins for coherence with the two existing subscribers; B wins when handlers are thin wrappers over `IEncina.Send` returning `Either`. #240 and the other siblings ask the same question and must get the same answer.

**Class:** Dilemma

</details>

<details>
<summary><strong>5. Subscription mechanism (Dilemma D5)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Streaming subscription through `Dapr.Messaging` (`DaprPublishSubscribeClient.SubscribeAsync`): the application opens the stream to the sidecar, no inbound HTTP endpoint; one subscription per topic with the handler registered for it ("topic routing" is topic to handler)** | Fits `SubscribeAsync(handler)`; works in a worker service without a web host; the response action is the handler's return | `Dapr.Messaging` pulls `Grpc.AspNetCore`; no routing rules or bulk subscribe |
| **B) Declarative or programmatic HTTP subscriptions (`/dapr/subscribe`, route rules, `Dapr.AspNetCore`)** | Routing rules by content (`match` expressions), bulk subscribe, the model most Dapr documentation shows | Needs an ASP.NET host mapping endpoints, so a worker service cannot subscribe; the handler shape is an HTTP route, not a callback |

### Chosen Option: **A, streaming subscription** (recommended, pending the maintainer)

### Rationale

A wins for a worker or console host and for the shape `SubscribeAsync(handler)`; B wins for an ASP.NET application that wants routing rules in the sidecar or bulk subscribe. The pull request 3 starts with a probe test (recorded in the pull request) that opens a streaming subscription against the integration sidecar and confirms the pinned SDK's parameter shapes, because only the type names were verified by reading the package.

**Class:** Dilemma

</details>

<details>
<summary><strong>6. Dead lettering (Dilemma D6)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Delegate to Dapr: the subscription sets a dead-letter topic (`DeadLetterOptions.TopicName`), the sidecar publishes there after its retries (its resiliency policy) are exhausted or on `Drop`; the options name the suffix `.dead`** | Uses the sidecar's native mechanism; no delivery counting or retry state in the app | Operators read a second topic; no Encina tooling reads it; retry limits live in Dapr's resiliency files |
| **B) Hand the message to the Encina dead letter store of `Encina.Messaging`** | One place for all dead letters | Couples the transport to a database store (a separate 1.0 workstream); a delivery limit needs Encina's own per-message state, which the sidecar already keeps |

### Chosen Option: **A, the Dapr dead-letter topic** (recommended, pending the maintainer)

### Rationale

A wins for a transport that must work without a database; B wins when operations already monitor the Encina dead letter store and accept dead-lettering on the first failure only. Under A the dead-letter topic is opt-in: `EncinaDaprPubSubOptions.DeadLetterTopicSuffix` is null by default and the subscription sets no dead-letter topic.

**Class:** Dilemma

</details>

<details>
<summary><strong>7. CloudEvents handling (Dilemma D7)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Dapr's native CloudEvents wrapping: the publisher sets the CloudEvent attributes through `PublishOptions` (`Id` = the message id, `Type` = the message type name, `Source` = the application id of `EncinaDaprOptions`, `TraceParent`), the subscriber reads them from the delivered envelope** | The default Dapr behavior; interoperable with every other Dapr application and with #260 once it lands | The envelope is Dapr's, not an Encina format |
| **B) `rawPayload=true`: Encina's own JSON, the message id and type in metadata** | Full control of the bytes | Loses CloudEvents interoperability, which is the reason to use Dapr |

### Chosen Option: **A, native CloudEvents through `PublishOptions`** (recommended, pending the maintainer)

### Rationale

A wins when the other publishers and subscribers on the topic are Dapr applications; B wins when a non-Dapr consumer reads the broker topic directly and cannot unwrap CloudEvents. #260 (CloudEvents format support) is open and has no plan, so this plan does not depend on it.

**Class:** Dilemma

</details>

<details>
<summary><strong>8. Service invocation and workflows: the acceptance criterion that #387 covers (Dilemma D8)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Not built here: `IDaprServiceInvoker` is delivered by #387 (its Design Choice 4, with an allow-list of app ids); workflows are the follow-up of #387's Design Choice on `Encina.Dapr.Workflows`; this plan lists both as covered elsewhere** | One owner and one security design for invocation; this plan stays a transport | The criterion is met by another plan, not this one |
| **B) Also build an invoker in `Encina.Dapr.PubSub`** | One package covers the issue's list | Two invokers with two allow-list designs; invocation is not a transport operation |

### Chosen Option: **A, covered by #387** (recommended, pending the maintainer)

### Rationale

A wins because the invocation design (allow-list, timeouts, error categories) is a security decision #387 already carries; B wins only if #387 is closed or its invoker is dropped. If #387 drops its invoker, the criterion moves to a new issue file, not to this plan.

**Class:** Dilemma

</details>

<details>
<summary><strong>9. Bulk publish (Dilemma D9)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Not in this plan: batch publishing is one contract for every transport (#252), adopted through the shared follow-up of #240** | One batch shape for 16 transports | The issue's feature "bulk publish" is not delivered now |
| **B) `PublishBatchAsync` now over `DaprClient.BulkPublishEventAsync` (verified in `Dapr.Client` 1.19.1, with `BulkPublishResponse` listing failed entries)** | Delivers the feature; Dapr maps it to the broker's bulk API | A Dapr-only batch shape that #252 may replace; partial failure handling to define |

### Chosen Option: **A, adopt the #252 contract later** (recommended, pending the maintainer)

### Rationale

A wins for coherence across transports; B wins when a Dapr user needs bulk publish before #252 is planned. "Bulk publish" is a feature bullet, not an acceptance criterion of the issue.

**Class:** Dilemma

</details>

<details>
<summary><strong>10. Envelope and metadata propagation</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) CloudEvent `id` is the caller's `messageId` (a new GUID only when null or empty), `type` the message type name, `traceparent` stamped from the current activity, caller headers as `PublishOptions.Metadata` entries with the prefix `encina-`** | The same message-id rule as every other publisher; the outbox row id reaches consumers | Metadata keys are limited to what the broker component accepts |
| **B) Everything in one JSON body** | Simple | Double serialization; headers invisible to Dapr tooling |

### Chosen Option: **A, CloudEvent attributes and prefixed metadata**

### Rationale

Every existing publisher takes `messageId` and generates one only when null or empty (`src/Encina.NATS/INATSMessagePublisher.cs`, `src/Encina.AmazonSQS/IAmazonSQSMessagePublisher.cs`); the outbox passes its row id there so consumers deduplicate. Tenant and module headers are not stamped here (Design Choice 13).

**Class:** Trivial
**Settled by:** the `messageId` convention of `src/Encina.NATS/INATSMessagePublisher.cs` and `src/Encina.AmazonSQS/IAmazonSQSMessagePublisher.cs`

</details>

<details>
<summary><strong>11. Integration test infrastructure</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) A shared `DaprSidecarFixture` (generic Testcontainers, `daprd` with an in-memory pub/sub component), one `[Collection]` per test assembly** | Real sidecar, as the issue's last acceptance criterion requires; one container for all test classes | A new fixture; the image is pulled once |
| **B) A fake sidecar (WireMock) for the gRPC and HTTP calls** | No container | Does not test the sidecar's delivery, retry or dead-letter behavior |

### Chosen Option: **A, a real sidecar fixture**

### Rationale

The issue's acceptance criteria list "Integration tests with Dapr sidecar", and AGENTS.md section 9 requires tests that execute real package code against real externals, with shared collection fixtures. There is no reusable Dapr fixture or collection today (grep of `tests` for `CollectionDefinition` finds none for Dapr); the collection classes are per assembly, as for `LocalStackCollection` in `tests/Encina.IntegrationTests/MessageBrokers/AmazonSQS/`.

**Class:** Settled
**Settled by:** #242 acceptance criteria and AGENTS.md section 9 (integration tests, shared collection fixtures)

</details>

<details>
<summary><strong>12. OpenTelemetry: per-package source or the shared transport design of #725 (Dilemma D12)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `ActivitySource` and `Meter` named `Encina.Dapr.PubSub` in the package, registered by name in `Encina.OpenTelemetry`; the W3C `traceparent` travels in the CloudEvent; Dapr's own sidecar spans are Dapr's built-in telemetry** | Meets the criterion "traces/metrics (via Dapr built-in)" now: the sidecar traces itself, the package adds the application side | The name list in `Encina.OpenTelemetry` is edited by every transport plan |
| **B) Wait for the shared transport instrumentation of #725** | One design for all transports | #725's plan is merged but its code is not; the criterion would stay open |

### Chosen Option: **A, per-package instrumentation** (recommended, pending the maintainer)

### Rationale

A wins because the acceptance criterion is in this issue; B wins if #725 should rename every source at once. Spans link producer and consumer (ADR-054 D2) with no baggage. No tenant tag is emitted until the multi-tenancy follow-up (Design Choice 13); ADR-054 D4 then adds it.

**Class:** Dilemma

</details>

<details>
<summary><strong>13. Multi-tenancy and module headers (Dilemma D13)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Defer to an issue: tenant-scoped topic names and tenant and module metadata need the persisted request context (#1164) and the route metadata (#2385)** | No invented topic scheme; one design for all transports | Not in this plan |
| **B) Stamp a tenant segment into the topic name now** | Early isolation | Needs the reserved `default` tenant segment and an injective topic encoding that no transport has defined |

### Chosen Option: **A, defer to a follow-up issue** (recommended, pending the maintainer)

### Rationale

A wins because one design for all transports needs #1164 and #2385; B wins for a tenant-isolated deployment that needs scoped topics before those land. The issue file is `artifacts/issues/transport-new-2-tenant-module-and-resilience-adoption.md`, which also records the resilience decorator for these four transports (the #742 model; #742 itself does not cover the six planned transports).

**Class:** Dilemma

</details>

---

## Implementation Phases

### Phase 1: Package, options and DI (pull request 1)

<details>
<summary>Tasks</summary>

1. `src/Encina.Dapr.PubSub/Encina.Dapr.PubSub.csproj` (net10.0; packages `Dapr.Messaging` (new pin in `Directory.Packages.props`, 1.19.1), `Microsoft.Extensions.*.Abstractions`, `LanguageExt.Core`; project references to `Encina`, `Encina.Messaging` and `Encina.Dapr` (#387); `InternalsVisibleTo Encina.UnitTests`) and the `Encina.slnx` entry. Transport packages carry no `PublicAPI.*.txt` today (none in NATS, AmazonSQS, Redis.PubSub), so none is added.
2. `EncinaDaprPubSubOptions` (namespace `Encina.Dapr.PubSub`, public sealed): `PubSubName` (default `pubsub`, the name of the Dapr documentation's examples), `DefaultTopic`, `DeadLetterTopicSuffix` (null: no dead letter), `MaxQueuedMessages`, `ProviderHealthCheck` (`ProviderHealthCheckOptions`). It holds no secret: the sidecar endpoint and API token are `EncinaDaprOptions` of #387 (`ApiToken` is `[JsonIgnore]` with a `ToString()` override there). `ToString()` prints the pub/sub name only. Internal `EncinaDaprPubSubOptionsValidator : IValidateOptions<>`: non-blank `PubSubName`, topic names without whitespace or control characters, positive `MaxQueuedMessages`.
3. `ServiceCollectionExtensions.AddEncinaDaprPubSub(this IServiceCollection, Action<EncinaDaprPubSubOptions>? configure = null)`: calls `AddEncinaDapr()` (Design Choice 2), options with `ValidateOnStart`, the validator through `TryAddEnumerable`; a DI test that builds the provider with `ValidateOnBuild` and `ValidateScopes`.
4. Tests: `EncinaDaprPubSubOptionsTests`, `ServiceCollectionExtensionsTests` (unit), `DaprPubSubGuardTests` (guard: null `services`, null `configure` accepted); manifest `.github/coverage-manifest/Encina.Dapr.PubSub.json`; `UnitTests.csproj` and `GuardTests.csproj` ProjectReference edits.

</details>

<details>
<summary>Prompt for AI Agents — Phase 1</summary>

```text
CONTEXT: Encina.AmazonSQS and Encina.Redis.PubSub are the templates (options, validator, AddEncina*). Encina.Dapr (#387) provides AddEncinaDapr, EncinaDaprOptions and the DaprClient singleton and is already merged.
TASK: Create the Encina.Dapr.PubSub package skeleton with validated options and AddEncinaDaprPubSub, and their unit and guard tests.
KEY RULES: no secret in these options (the token lives in EncinaDaprOptions); validation fails at startup with OptionsValidationException; TryAdd registrations only; a DI test with ValidateOnBuild and ValidateScopes; the root namespace Encina.Dapr shadows the Dapr namespace, so write using Dapr.Client; and never Dapr.Client.X fully qualified; no Obsolete, no compatibility code.
REFERENCE FILES: src/Encina.AmazonSQS/*.cs, src/Encina.Redis.PubSub/*.cs, src/Encina.NATS/EncinaNATSOptionsValidator.cs, .github/coverage-manifest/Encina.AmazonSQS.json.
```

</details>

### Phase 2: Publishing, envelope and the EventId range (pull request 2)

<details>
<summary>Tasks</summary>

1. `IDaprMessagePublisher.PublishAsync<TMessage>(TMessage message, string? topic = null, string? messageId = null, IReadOnlyDictionary<string,string>? headers = null, CancellationToken cancellationToken = default) where TMessage : class` returning `ValueTask<Either<EncinaError, string>>` (the message id). `DaprMessagePublisher` (scoped; `DaprClient`, `IOptions`, `ILogger`): `PublishEventAsync` with the CloudEvent attributes of Design Choice 7 and the metadata of Design Choice 10; the topic is the argument or `DefaultTopic`.
2. Errors (ADR-062), in `DaprPubSubErrorCodes.cs` (one constants class, so later pull requests add their codes there), each created with the named argument `category:` and a constant message: `encina.dapr.pubsub.publish_failed` category `Internal` (R4, provider failure), `encina.dapr.pubsub.topic_required` `InvalidRequest` (R3). Cancellation propagates as `OperationCanceledException`, never a `Left` (R5). The exception goes to `ForLogging()`, never its message.
3. `Log.cs` 40000-40002 (publishing, published, publish failed; no message ids, no payload). `EventIdRanges.DaprPubSub = (40000, 40019)`; the line `static readonly Encina.Diagnostics.EventIdRanges.DaprPubSub -> (int Min, int Max)` in `src/Encina/PublicAPI.Unshipped.txt` (through `mechanical-fixer`); the `AssemblyRanges` entry in `EncinaEventIdAllocationTests`; `docs/engineering/eventid-block-registry.md` (through `docs-writer`): the row `| DaprPubSub | 40000-40019 | Transports | zone |` in the registered ranges table (Zone `zone` for a range at or above 10000), and in the Blocks table the five `reserved` rows of this family (`40000-40019` registered to this plan, `40020-40039`, `40040-40059`, `40060-40079`, `40080-40099` reserved for #610, #611, #612 and #310, owner `transport-new-2: #<n>`); the status of this plan's row is `registered`, so no reserved row overlaps a registered range as `EventIdRegistryAgreementTests` requires. Whichever of the five plans lands first adds the five rows; the later pull requests change only their own row, so a conflict is resolved by keeping both (a new commit, never a rebase of a pushed branch).
4. DI: `AddEncinaDaprPubSub` gains `services.TryAddScoped<IDaprMessagePublisher, DaprMessagePublisher>()` in this pull request (registration completeness, AGENTS.md section 3); `ServiceCollectionExtensions` tests gain the resolution test with `ValidateOnBuild` and `ValidateScopes`.
5. Tests: `DaprMessagePublisherTests` and `DaprMessageIdTests` (unit, over a substituted `DaprClient`), guard edit; manifest edit with the unit and guard targets of the publisher (contract and integration targets are entered in pull request 5, where those tests land).

</details>

<details>
<summary>Prompt for AI Agents — Phase 2</summary>

```text
CONTEXT: The package skeleton exists. Publishing follows Encina.AmazonSQS and Encina.Redis.PubSub: a messageId parameter, generated only when null or empty. Dapr wraps payloads in CloudEvents; PublishOptions sets the attributes.
TASK: Implement IDaprMessagePublisher.PublishAsync, the CloudEvent attributes and metadata, the error codes class, the scoped DI registration, the log messages 40000-40002 and the EventId range DaprPubSub, with unit and guard tests.
KEY RULES: EncinaErrors.Create with the named category: argument (ADR-062); constant messages, never ex.Message; ids and payloads never logged; cancellation propagates; EventIds only 40000-40019, registered first in EventIdRanges.cs and in the registry (Zone zone); no tenant tag.
REFERENCE FILES: src/Encina.AmazonSQS/AmazonSQSMessagePublisher.cs, src/Encina/Diagnostics/EventIdRanges.cs, tests/Encina.UnitTests/Testing/Architecture/EncinaEventIdAllocationTests.cs, docs/engineering/eventid-block-registry.md.
```

</details>

### Phase 3: Streaming subscription (pull request 3)

<details>
<summary>Tasks</summary>

1. `SubscribeAsync<TMessage>(Func<TMessage, ValueTask> handler, string? topic = null, CancellationToken cancellationToken = default)` returning `ValueTask<IAsyncDisposable>`. An internal `IDaprSubscriptionClient` seam wraps the SDK's concrete `DaprPublishSubscribeClient.SubscribeAsync` (the SDK type is not an interface), so unit tests substitute it. The handler wrapper deserializes the CloudEvent data, runs the handler, answers `TopicResponseAction.Success` on a normal return, `Retry` when the handler throws (Design Choice 4) and `Drop` for an undeserializable message (dead lettered by the sidecar when a dead-letter topic is set, Design Choice 6). The wrapper catches every exception, so nothing escapes into the SDK's receive loop; the stopping token ends the subscription and disposes the stream.
2. Codes added to `DaprPubSubErrorCodes.cs`: `encina.dapr.pubsub.subscribe_failed` `Internal` (R4), `encina.dapr.pubsub.deserialize_failed` `Internal` (R4, logged and dropped, never returned to a caller).
3. `Log.cs` 40003-40007 (subscription started, handler failed with retry requested, message dropped, subscription ended, undeserializable message). `Encina.Dapr.PubSub` registers the seam and the SDK client in `AddEncinaDaprPubSub` in this pull request; its DI test resolves `IDaprMessagePublisher` and subscribes with a substituted seam.
4. Tests: `DaprSubscriptionTests`, `DaprHandlerResponseTests` (unit), guard edit, manifest edit, options edit (`MaxQueuedMessages` is used here).

</details>

<details>
<summary>Prompt for AI Agents — Phase 3</summary>

```text
CONTEXT: Publishing exists. Dapr streaming subscriptions deliver each message to a handler that answers Success, Retry or Drop; the sidecar owns redelivery and the dead-letter topic.
TASK: Implement SubscribeAsync over the SDK's streaming subscription behind an internal seam, with the handler wrapper that maps a normal return, an exception and an undeserializable message to the three actions, with unit tests.
KEY RULES: the wrapper never lets an exception escape; a handler failure is Retry and never Success; cancellation propagates and ends the subscription; the dead-letter topic is set only when the option is set; payloads and message ids never logged; start with a probe against the integration sidecar and record the SDK parameter shapes in the pull request.
REFERENCE FILES: src/Encina.Redis.PubSub/RedisPubSubMessagePublisher.cs, src/Encina.MQTT/MQTTMessagePublisher.cs, AGENTS.md section 3.
```

</details>

### Phase 4: Health check and observability (pull request 4)

<details>
<summary>Tasks</summary>

1. `Health/DaprPubSubHealthCheck : EncinaHealthCheck` (`DefaultName = "encina-dapr-pubsub"`, tags `encina`, `messaging`, `dapr-pubsub`, `ready`): `DaprClient.GetMetadataAsync` bounded by `ProviderHealthCheck.Timeout`, healthy when the configured pub/sub component is listed; the result text names the check and the exception type only. Sidecar reachability itself is the check of #387; this one adds the component presence. Registered like `AmazonSQSHealthCheck` when `ProviderHealthCheck.Enabled`.
2. `Diagnostics/DaprPubSubActivitySource` and `DaprPubSubMetrics`: spans publish and consume with a link from consumer to producer through the CloudEvent `traceparent`; counters `dapr_pubsub.published`, `.consumed`, `.dropped` with tags `topic`, `outcome`; no tenant tag. `Encina.OpenTelemetry` gains `AddSource`/`AddMeter` lines.
3. DI: the health check registration is the only DI edit of this pull request. Tests: `DaprPubSubHealthCheckTests`, `DaprPubSubDiagnosticsTests` (unit), guard edit; manifest edit; the `Encina.OpenTelemetry` registration test is extended (its manifest targets stay at today's unit 90 and guard 35).

</details>

<details>
<summary>Prompt for AI Agents — Phase 4</summary>

```text
CONTEXT: Publish and subscribe exist. The issue requires a health check and traces and metrics; Dapr's sidecar traces itself.
TASK: Add the health check, the ActivitySource and Meter, and register the source and meter names in Encina.OpenTelemetry.
KEY RULES: health results and tags never contain an exception message; spans link, never baggage (ADR-054 D2); zero cost when no listener; resolve the client through the container; no manifest target is lowered.
REFERENCE FILES: src/Encina.AmazonSQS/Health/AmazonSQSHealthCheck.cs, src/Encina.Messaging/Diagnostics/OutboxActivitySource.cs, src/Encina.OpenTelemetry/ServiceCollectionExtensions.cs.
```

</details>

### Phase 5: Cross-cutting integration

<details>
<summary>Tasks</summary>

1. Verify each row of the matrix below against the code; the deferred rows are in the issue file `artifacts/issues/transport-new-2-tenant-module-and-resilience-adoption.md`.
2. Confirm every error code has its ADR-062 category and that no log call carries a message id, payload or `EncinaError.Message`.

</details>

<details>
<summary>Prompt for AI Agents — Phase 5</summary>

```text
CONTEXT: Encina.Dapr.PubSub publishes, subscribes, reports health and emits telemetry.
TASK: Verify the cross-cutting matrix against the code and fix any gap.
KEY RULES: AGENTS.md sections 3, 6 and 7; ADR-062 categories; deferred items point to an open issue.
REFERENCE FILES: AGENTS.md section 6, docs/architecture/adr/062-error-category-contract.md.
```

</details>

### Phase 6: Contract, property and integration tests (pull request 5)

<details>
<summary>Tasks</summary>

1. `tests/Encina.TestInfrastructure/Fixtures/DaprSidecarFixture.cs` (generic Testcontainers; the pinned `daprd` image tag chosen at implementation time as the newest explicit stable tag, never `latest`; one in-memory pub/sub component file mounted into the container; HTTP and gRPC ports exposed; `ClearAllDataAsync` no-op documented because the in-memory component is per container), a `DaprCollection` class in `tests/Encina.IntegrationTests/MessageBrokers/Dapr/` and one in `tests/Encina.ContractTests/MessageBrokers/Dapr/` (per assembly, as `LocalStackCollection` is).
2. `tests/Encina.ContractTests/MessageBrokers/Dapr/DaprConformanceTests.cs`, a subclass of `TransportConformanceContractBase` of #240 (publish with message id, subscribe delivers, handler failure redelivers, dead letter, cancellation); `ContractTests.csproj` reference.
3. `tests/Encina.IntegrationTests/MessageBrokers/Dapr/DaprPubSubIntegrationTests.cs` (`[Collection]`, `[Trait("Category","Integration")]`, `[Trait("Transport","Dapr")]`): fan-out with two app ids, retry after a throwing handler, dead-letter topic content, CloudEvent attributes; `IntegrationTests.csproj` reference.
4. `tests/Encina.PropertyTests/Messaging/DaprPubSub/DaprEnvelopePropertyTests.cs` (FsCheck round trip of message id, headers and type) and `PropertyTests.csproj` reference.
5. One manifest edit that enters the contract, property and integration targets (the table of Phase 7), because those flags are reachable only now.

</details>

<details>
<summary>Prompt for AI Agents — Phase 6</summary>

```text
CONTEXT: The conformance base of #240 is merged; a real daprd sidecar runs in a container with an in-memory pub/sub component.
TASK: Write the sidecar fixture, the collections, the conformance subclass, the integration tests and the property test.
KEY RULES: one shared collection fixture, never a per-class container; no Thread.Sleep (bounded waits on TaskCompletionSource); Shouldly through Encina.Testing.Shouldly; the contract tests execute the real publisher and subscriber; pin the image tag.
REFERENCE FILES: tests/Encina.TestInfrastructure/Fixtures/LocalStackFixture.cs, tests/Encina.IntegrationTests/MessageBrokers/AmazonSQS/LocalStackCollection.cs, tests/Encina.ContractTests/MessageBrokers/AmazonSQS/LocalStackCollection.cs.
```

</details>

### Phase 7: Documentation and finalization (pull request 6)

<details>
<summary>Tasks</summary>

Per-flag targets (entered in `.github/coverage-manifest/Encina.Dapr.PubSub.json` in the pull request that adds the tests of each flag: unit and guard with the file in pull requests 1 to 4, contract, property and integration in pull request 5; measured with the commands of AGENTS.md section 9):

| File | unit | guard | contract | property | integration |
|------|:---:|:---:|:---:|:---:|:---:|
| `DaprMessagePublisher.cs` | 85 | 20 | 50 | 20 | 70 |
| `EncinaDaprPubSubOptions.cs`, `...OptionsValidator.cs` | 100 | 0 | 0 | 0 | 0 |
| `ServiceCollectionExtensions.cs` | 85 | 55 | 0 | 0 | 0 |
| `Health/DaprPubSubHealthCheck.cs` | 90 | 40 | 0 | 0 | 50 |
| `Diagnostics/*.cs` | 90 | 0 | 0 | 0 | 0 |
| `IDaprMessagePublisher.cs`, `Log.cs` | 0 | 0 | 0 | 0 | 0 |

| File and flag | Justification |
|---------------|---------------|
| Publisher unit 85, integration 70 | Unit tests substitute the `DaprClient` and the subscription seam for every branch; the integration class runs real publish, retry and dead-letter on the sidecar, leaving the sidecar-unreachable branches. |
| Publisher guard 20, contract 50, property 20 | Guard tests reach only the argument checks; the conformance base runs the publish and subscribe scenarios; the property test covers the envelope fields only. |
| Options and validator unit 100 | Pure data and validation. |
| Health check integration 50 | One real metadata call; the failure branch needs a stopped sidecar. |
| Interfaces and `Log.cs` 0 | No executable lines (interface) or generated delegates; guard tests cannot reach either. |

1. `src/Encina.Dapr.PubSub/README.md`, `docs/features/dapr-pubsub.md`, the Dapr section of `docs/messaging/transports.md` and `docs/INVENTORY.md` through `docs-writer`; changelog fragment `changelog.d/242-dapr-pubsub.added.md` through `mechanical-fixer`.
2. Load and benchmark: justification files `tests/Encina.LoadTests/Messaging/DaprPubSub/DaprPubSub.md` and `tests/Encina.BenchmarkTests/Messaging/DaprPubSub/DaprPubSub.md` (throughput is the broker's behind the sidecar; the Redis.PubSub precedent has the same two files).
3. `dotnet build --configuration Release` with zero warnings; every flag at its target; CRAP of every new method at most 10.

</details>

<details>
<summary>Prompt for AI Agents — Phase 7</summary>

```text
CONTEXT: The package is implemented and tested; documentation and the justification files remain.
TASK: Enter the manifest justifications, write the two justification files, and hand the README, feature page, transports page, INVENTORY and changelog fragment to the owning specialists.
KEY RULES: targets never go down; no hand-typed coverage figures in docs; never edit CHANGELOG.md; docs through docs-writer, fragments through mechanical-fixer; no private application names.
REFERENCE FILES: tests/Encina.LoadTests/Messaging/RedisPubSub/RedisPubSub.md, .github/coverage-manifest/Encina.AmazonSQS.json, changelog.d/README.md.
```

</details>

---

## Research

### Relevant Standards & Specifications

| Standard / Spec | Relevance | Notes |
|-----------------|-----------|-------|
| `AGENTS.md` sections 3, 5, 6, 7, 9 | Registration completeness, transport category, 12 functions, EventIds, coverage | Phases 1 to 7 |
| SPEC-000 DEC-009 and REQ-030 | Dapr is one of the six planned transports of 1.0; it needs no cloud service | Scope |
| ADR-062, ADR-054 D2 and D4 | Error categories; trace links and tenant tags | Phases 2 and 4 |
| CloudEvents 1.0 and the Dapr pub/sub building block (streaming subscriptions, dead-letter topics) | Envelope, delivery and dead lettering | Design Choices 5 to 7 |

### Existing Encina Infrastructure to Leverage

| Component | Location | Usage in this feature |
|-----------|----------|-----------------------|
| `Encina.Dapr` (planned, #387) | `src/Encina.Dapr/` (not on `origin/main`) | `AddEncinaDapr`, `EncinaDaprOptions`, the `DaprClient` singleton |
| `Encina.AmazonSQS` | `src/Encina.AmazonSQS/` | Template for options, validator, DI and health check |
| `EncinaHealthCheck`, `ProviderHealthCheckOptions` | `src/Encina.Messaging/Health/` | Health check base and options |
| `OutboxActivitySource` | `src/Encina.Messaging/Diagnostics/OutboxActivitySource.cs` | Pattern for the source and meter |
| `EventIdRanges` and the registry | `src/Encina/Diagnostics/EventIdRanges.cs`, `docs/engineering/eventid-block-registry.md` | New field `DaprPubSub`; zone rows |
| `LocalStackCollection` | `tests/Encina.IntegrationTests/MessageBrokers/AmazonSQS/LocalStackCollection.cs` | Pattern for the per-assembly collection class (a Dapr one is new) |

### Event ID Allocation

| Package | Range | Notes |
|---------|-------|-------|
| `DaprPubSub` (new) | 40000-40019 | First range of the block `40000-40099` of the Transports zone taken by transport-new-2; ids 40000-40007 used (40000-40002 publish, 40003-40007 subscribe), 40008-40019 spare. The block `9750-9799` assigned by the batch snapshot is registered as `SecurityBreakTheGlass` on `origin/main` (status `superseded` in the registry), so the family takes the zone. Verified free by a search of the registry and every `docs/plan-*` branch for `40000`. |

### Estimated File Count

| Category | Files | Notes |
|----------|------:|-------|
| Production code | 10 | csproj, options, validator, interface, publisher, error codes, `Log`, DI, health check, 2 diagnostics |
| Tests | 13 | options, DI, publisher, message id, subscription, handler response, health, diagnostics, guard, property, conformance subclass, integration, fixture |
| Shared edits | 9 | `Encina.slnx`, `Directory.Packages.props`, 5 test `.csproj` files, `EventIdRanges.cs`, allocation test, `Encina.OpenTelemetry` |
| Documentation | 8 | README, feature page, transports page, INVENTORY, registry rows, changelog, 2 justification files |
| **Pull request 1** | **12** | csproj, options, validator, DI, `Encina.slnx`, `Directory.Packages.props`, `UnitTests.csproj`, `GuardTests.csproj`, manifest, options test, DI test, guard test; about 450 lines |
| **Pull request 2** | **14** | interface, publisher, `DaprPubSubErrorCodes`, `Log`, `EventIdRanges.cs`, `src/Encina/PublicAPI.Unshipped.txt` edit, allocation test, registry, DI edit, DI test edit, 2 unit tests, guard edit, manifest edit; about 700 lines |
| **Pull request 3** | **11** | interface edit, publisher edit, subscription seam, `Log` edit, options edit, DI edit, DI test edit, 2 unit tests, guard edit, manifest edit; about 650 lines |
| **Pull request 4** | **9** | health check, 2 diagnostics, DI edit, `Encina.OpenTelemetry` edit, 2 unit tests, guard edit, manifest edit; about 500 lines |
| **Pull request 5** | **10** | sidecar fixture, 2 collection classes, conformance subclass, integration test, property test, `ContractTests.csproj`, `IntegrationTests.csproj`, `PropertyTests.csproj`, manifest; about 750 lines |
| **Pull request 6** | **8** | README, feature page, transports page, INVENTORY, changelog fragment, 2 justification files, manifest justifications; about 350 lines |

Implementers and callers: `IDaprMessagePublisher` is new, so it has no implementers or `new X(` sites to update; the shared edits are additive. Pull request 1 registers options and the client (through `AddEncinaDapr`), pull request 2 the scoped publisher, pull request 3 the subscription seam and SDK client, pull request 4 the health check. Each pull request builds alone in order 1 to 6 and needs #387 pull requests 1 and 2 merged; pull request 5 also needs #240 pull request 5 (the conformance base).

---

## Combined AI Agent Prompts

<details>
<summary>Combined prompt for all phases</summary>

```text
PROJECT CONTEXT: Encina is a pre-1.0 .NET 10 / C# 14 library; no backward compatibility, zero warnings, ROP (Either<EncinaError, T>). Transports are publisher-only except MQTT and Redis.PubSub; Encina.Dapr.PubSub adds a transport over the Dapr sidecar on top of Encina.Dapr (#387).

IMPLEMENTATION OVERVIEW: PR1 package, options, DI; PR2 publish with CloudEvent attributes and the EventId range DaprPubSub 40000-40019; PR3 streaming subscription behind a seam; PR4 health check and telemetry; PR5 sidecar fixture, conformance subclass, property and integration tests; PR6 documentation and justification files. Dilemmas D1, D3, D4, D5, D6, D7, D8, D9, D12 and D13 are implemented as the maintainer answers, the recommended options until then.

KEY PATTERNS: at-least-once, never success on a failure; the handler wrapper never lets an exception escape; named ADR-062 category on every error; constant error messages; ids and payloads never logged; no secret in these options; TimeProvider where time is used; targets never go down; CRAP <= 10.

REFERENCE FILES: src/Encina.AmazonSQS, src/Encina.Redis.PubSub, src/Encina.Messaging/Health, tests/Encina.IntegrationTests/MessageBrokers/AmazonSQS, src/Encina/Diagnostics/EventIdRanges.cs.
```

</details>

---

## Cross-Cutting Integration Matrix

| # | Function | Status | Notes |
|---|----------|--------|-------|
| 1 | Caching | ❌ | A transport does not cache messages. |
| 2 | OpenTelemetry | ✅ | Per-package `ActivitySource` and `Meter` (Design Choice 12); Dapr's sidecar emits its own spans. |
| 3 | Structured Logging | ✅ | `[LoggerMessage]` in `Log.cs`, EventIds 40000-40007 of the range 40000-40019; error codes and exception types only. |
| 4 | Health Checks | ✅ | `DaprPubSubHealthCheck` checks the pub/sub component; sidecar reachability is #387's check. |
| 5 | Validation | ❌ | Options are validated at startup; payload validation belongs to the validation behaviors of the consuming handler. |
| 6 | Resilience | ⏭️ | Retries and breakers for the new transports go through the decorator model of #742 (it covers the ten existing transports); the issue file `transport-new-2-tenant-module-and-resilience-adoption` records the new ones. Redelivery is the sidecar's. |
| 7 | Distributed Locks | ❌ | The sidecar gives each message to one instance of an app id. |
| 8 | Transactions | ❌ | Acknowledgement is the handler's response; no unit of work is involved. |
| 9 | Idempotency | ✅ | The CloudEvent id is the message id in every message; processing idempotency stays with the inbox (redelivery is possible). |
| 10 | Multi-Tenancy | ⏭️ | Tenant-scoped topics and metadata need #1164 and #2385; issue file `transport-new-2-tenant-module-and-resilience-adoption`. |
| 11 | Module Isolation | ⏭️ | Same issue file. |
| 12 | Audit Trail | ❌ | Transport is infrastructure; audit belongs to the outbox and inbox. |

---

## Prerequisites & Dependencies

- **Blocking**: #387 pull requests 1 and 2 (`Encina.Dapr`, `AddEncinaDapr`, the client registration) for every pull request of this plan under Design Choice 1 option A; #240 pull request 5 (the conformance base) for pull request 5 of this plan.
- **Related, not blocking**: #1609 (the other transports gain subscribe with the shape chosen in D3 and D4), #251, #252 and #2385 (contracts the new transports adopt through the issue files), #725 and #742 (telemetry and resilience of all transports), #1164, #260 (CloudEvents format, open).
- **Sibling plans**: #610, #611, #612 and #310 of this family and #237, #238, #239, #240 and #241 of transport-new-1 answer the same D3, D4 and D12 questions, with the same recommended answers.

---

## Next Steps

1. Ask the maintainer D1, D3, D4, D5, D6, D7, D8, D9, D12 and D13 when work on #242 starts; record the answers in `## Maintainer Decisions`.
2. Pull request 1: package skeleton (12 files), after #387 pull requests 1 and 2.
3. Pull requests 2 to 4: publish (14), subscribe (11), health and telemetry (9).
4. Pull requests 5 and 6: contract, property and integration tests (10), documentation (8).
