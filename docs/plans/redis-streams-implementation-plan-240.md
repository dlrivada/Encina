# Implementation Plan: Encina.Redis.Streams, a durable Redis Streams message transport

> **Issue**: [#240](https://github.com/dlrivada/Encina/issues/240)
> **Type**: Feature
> **Verified against**: origin/main `0336d31c7` (2026-11-20)
> **Complexity**: Medium (7 phases, 6 pull requests of about 11, 15, 10, 12, 9 and 7 files)
> **Estimated Scope**: about 900 lines of production code and about 1,900 lines of tests
> **Family**: transport-new-1 (#237 Google Cloud Pub/Sub, #238 Amazon EventBridge, #239 Pulsar, #240 Redis Streams, #241 ActiveMQ Artemis). Sibling family transport-new-2 (#242, #610, #611, #612, #310) runs later; "Shared pieces" below says which plan owns what.

---

## Summary

The issue adds a durable transport next to `Encina.Redis.PubSub`: publishing with `XADD`, consumer groups with `XREADGROUP`, acknowledgement with `XACK`, recovery of pending entries with `XAUTOCLAIM`/`XCLAIM`, dead lettering and stream trimming (`MAXLEN`, `MINID`). State of `origin/main`:

- Transports are publisher-only except MQTT and Redis.PubSub (`SubscribeAsync`), which is the gap tracked by #1609. There is no `IMessageTransport` in `src/` (verified by grep); each transport has its own `I<Name>MessagePublisher` (`src/Encina.NATS/INATSMessagePublisher.cs`, `src/Encina.Redis.PubSub/IRedisPubSubMessagePublisher.cs`).
- `Encina.Redis.PubSub` is the template: options with `[JsonIgnore]` connection string and a `ToString()` override, `TryAddSingleton<IConnectionMultiplexer>`, `TryAddScoped` publisher, a `messageId` parameter carried in the envelope.
- StackExchange.Redis 2.12.14 (`Directory.Packages.props`) exposes `StreamAdd`, `StreamReadGroup`, `StreamAcknowledge`, `StreamAutoClaim`, `StreamClaim`, `StreamPending`, `StreamCreateConsumerGroup`, `StreamTrim` and `StreamTrimByMinId` (verified in the package's XML documentation in the local NuGet cache). No new package version is needed; the integration tests reuse `RedisFixture` (`redis:7-alpine`) and `Testcontainers.Redis`.

Delivered: package `Encina.Redis.Streams` with `IRedisStreamsMessagePublisher` (publish and subscribe, Design Choice 2), `EncinaRedisStreamsOptions` (validated, `[JsonIgnore]` connection string), `AddEncinaRedisStreams`, a health check, an `ActivitySource` and `Meter`, log messages in the block `RedisStreams` 40650-40659, unit, guard, property, contract and integration tests, and documentation. Delivery is at-least-once: `XACK` ends redelivery, but a crash between handling and acknowledgement redelivers, so consumers deduplicate on the message id the envelope carries (the inbox, AGENTS.md section 3).

**Provider category**: Messaging transports (10 existing, 6 planned; AGENTS.md section 5). The transport rule requires send/publish, subscription management, error handling with DLQ and metadata propagation; each is covered below.

**Shared pieces (family)**: this plan owns the shared transport conformance test base (Design Choice 8) and the follow-up issue that makes the five new transports adopt the batch (#252), priority (#251) and outbox-adapter (#2385) contracts when those land; the other four plans subclass the base. transport-new-2 plans do the same.

---

## Design Choices

<details>
<summary><strong>1. Package placement</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) New package `Encina.Redis.Streams`** | Pay-for-what-you-use; its own options, health check and EventId block; matches the transport list of AGENTS.md section 5 | One more package |
| **B) Add streams to `Encina.Redis.PubSub`** | No new package | Mixes a fire-and-forget model with a durable one; options and health check of two semantics in one package |

### Chosen Option: **A, a new package**

### Rationale

The issue names `Encina.Redis.Streams` and AGENTS.md section 5 lists Redis.Streams as its own planned transport.

**Class:** Settled
**Settled by:** AGENTS.md section 5 (transports table) and #240

</details>

<details>
<summary><strong>2. Public interface: one publisher interface or a publisher and a consumer (Dilemma D2)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) One `IRedisStreamsMessagePublisher` with `PublishAsync` and `SubscribeAsync`, the shape of `IMQTTMessagePublisher` and `IRedisPubSubMessagePublisher`** | Coherent with the two transports that already subscribe; #1609 asks the other five to take the same shape | The name says "publisher" for a consuming member |
| **B) The issue's two interfaces, `IRedisStreamsMessagePublisher` and `IRedisStreamsMessageConsumer`** | Separate concerns; a publish-only application registers no consumer | Different from MQTT and Redis.PubSub; the same split would have to be repeated by the other transports |

### Chosen Option: **A, one interface with both members** (recommended, pending the maintainer)

### Rationale

A wins when the category is judged by coherence (one call shape for every transport, #1609); B wins when publishing and consuming live in different processes and the consumer needs its own lifetime and options. The same question is open in the sibling plans (#237, #239, #241) and the answer must be the same in all.

**Class:** Dilemma

</details>

<details>
<summary><strong>3. The handler contract and failure semantics (Dilemma D3)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `Func<TMessage, ValueTask>` as in `SubscribeAsync` of MQTT and Redis.PubSub; an exception means failure** | Same call shape as the two existing subscribers | Business failure travels as an exception, against Railway Oriented Programming |
| **B) `Func<TMessage, MessageContext, CancellationToken, ValueTask<Either<EncinaError, Unit>>>`; `Left` or an exception means failure** | A `Left` from `IEncina.Send` maps straight to a nack; the context carries the message id and delivery count | A new shape that the existing two subscribers do not have |

### Chosen Option: **A, the existing shape** (recommended, pending the maintainer)

### Rationale

Under either option a failure leaves the entry pending (no `XACK`), it is redelivered by `XAUTOCLAIM` after the idle time, and after `MaxDeliveryAttempts` it is dead lettered (Design Choice 4); an error is never reported as success (AGENTS.md section 3). A wins for coherence with the two existing subscribers; B wins when handlers are thin wrappers over `IEncina.Send` returning `Either`.

**Class:** Dilemma

</details>

<details>
<summary><strong>4. Dead-letter destination (Dilemma D4)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) A dead-letter stream `<stream key>:dead` written together with the `XACK` of the original in one `MULTI`/`EXEC`; both keys share the hash tag of the stream key (Design Choice 11), so they share a cluster slot** | Stays on the broker; atomic move; the issue's "dead-letter handling via XCLAIM" | Operators read a second stream; no Encina tooling reads it |
| **B) Hand the entry to the Encina dead letter store (`IDeadLetterStore`, `src/Encina.Messaging/DeadLetter/IDeadLetterStore.cs`)** | One place for all dead letters; no second stream; no hash-tag rule needed for dead lettering | Couples the transport to a database store registered in the application; the move is two systems (acknowledge in Redis, add in the store), so a crash between them duplicates or loses the entry unless ordered store-first |

### Chosen Option: **A, a dead-letter stream** (recommended, pending the maintainer)

### Rationale

A wins for a transport that must work without a database; B wins when operations already monitor the Encina dead letter store. The dead-letter entry keeps the original fields plus `dead-reason` (an error code, never an exception message) and `delivery-count`.

**Class:** Dilemma

</details>

<details>
<summary><strong>5. Trimming default (Dilemma D5)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) No trimming by default; `MaxLength` (approximate `~`) and `MinIdAge` (via `TimeProvider`) are opt-in** | Pay-for-what-you-use; trimming can delete entries that are not yet delivered or acknowledged, so it is never silent | Unbounded stream until configured |
| **B) The issue's example `MaxLength = 10000` as the default** | Bounded memory | Silent data loss for a slow consumer group |

### Chosen Option: **A, opt-in trimming** (recommended, pending the maintainer)

### Rationale

A wins for reliability; B wins for a cache-like stream where freshness matters more than completeness. Either way the options validator rejects `MaxLength <= 0` and a negative `MinIdAge`.

**Class:** Dilemma

</details>

<details>
<summary><strong>6. Consumer loop and recovery (Dilemma D6)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) One background loop per subscription: `XREADGROUP` with a block time, then `XAUTOCLAIM` of entries idle longer than `ClaimIdleTime`; exceptions are caught per iteration, cancellation propagates** | Matches the rule for background loops; recovery needs no second timer; `XAUTOCLAIM` is a single command (Redis 6.2 or later, the image is 7) | A subscription holds a connection-bound loop |
| **B) A separate recovery timer calling `XPENDING` and `XCLAIM`** | Independent cadence | Two loops, more states, a timer callback that must never throw |

### Chosen Option: **A, one loop per subscription** (recommended, pending the maintainer)

### Rationale

Both options satisfy AGENTS.md section 3 (a loop catches per iteration, a timer callback never throws), so the choice is not settled by it. A wins when a subscription should own one cancellable unit of work; B wins when recovery must run on its own cadence while handlers are slow. Under A a failed iteration is logged with the exception type (EventId 40659) and the loop continues, a handler failure leaves the entry pending, and cancellation ends the loop and disposes the subscription.

**Class:** Dilemma

</details>

<details>
<summary><strong>7. Envelope and metadata propagation (Dilemma D7)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Stream fields: `payload` (UTF-8 JSON), `type`, `message-id`, `traceparent` (stamped from the current activity when there is one) and one `h:<name>` field per caller header; a new id only when the caller gives none** | Native fields, no wrapper JSON; headers visible to `XRANGE`; the same message-id rule as the other publishers | Header names are limited to what a field name accepts |
| **B) One JSON envelope in a single `data` field** | Simple | Opaque to Redis tooling; double serialization |

### Chosen Option: **A, native fields** (recommended, pending the maintainer)

### Rationale

A wins when operators inspect streams with `XRANGE` and consumers filter by field; B wins when one opaque payload is easier to version. The message-id rule itself is common to both: every existing publisher takes `messageId` and generates one only when null or empty (`INATSMessagePublisher`, `IRedisPubSubMessagePublisher`), and the outbox passes its row id there so consumers deduplicate. The `traceparent` field follows Design Choice 9 and is dropped if that Dilemma is answered B. Tenant and module headers are not stamped in this plan (Design Choice 10).

**Class:** Dilemma

</details>

<details>
<summary><strong>8. Shared transport conformance tests (Dilemma D8)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) An abstract base in `tests/Encina.ContractTests/MessageBrokers/Conformance/` (publish, subscribe, message-id round trip, failure then redelivery, dead lettering after N attempts), owned by this plan and subclassed by every later transport** | One definition of the category rule "send, subscribe, DLQ, metadata"; new transports cost one small subclass | The later plans depend on this plan's contract pull request landing first |
| **B) Each transport writes its own contract tests** | No cross-plan dependency | Five or more diverging definitions of the same rule |

### Chosen Option: **A, a shared base owned by this plan** (recommended, pending the maintainer)

### Rationale

A wins as the category grows to 16 transports; B wins if the maintainer wants the five plans to merge in any order. The base takes the transport under test as an adapter delegate, so publish-only transports (EventBridge) subclass only the publish scenarios.

**Class:** Dilemma

</details>

<details>
<summary><strong>9. OpenTelemetry: per-package source or the shared transport design of #725 (Dilemma D9)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `ActivitySource` and `Meter` named `Encina.Redis.Streams` in the package, registered by name in `Encina.OpenTelemetry`** | Meets the issue's acceptance criterion now; the same pattern as `Encina.Messaging.Outbox` | The name list in `Encina.OpenTelemetry` is edited by every transport plan |
| **B) Wait for #725 to define one shared transport instrumentation** | One design for all transports | #725 has no plan on `origin/main`; the criterion would stay open |

### Chosen Option: **A, per-package instrumentation** (recommended, pending the maintainer)

### Rationale

A wins because the acceptance criterion is in this issue; B wins if #725 should rename every source at once. Spans link producer and consumer (ADR-054 D2) with no baggage; the tenant tag of ADR-054 D4 is added by the multi-tenancy follow-up (Design Choice 10), so no tenant tag is emitted here.

**Class:** Dilemma

</details>

<details>
<summary><strong>10. Multi-tenancy and module headers (Dilemma D10)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Defer to an issue: tenant-scoped stream keys and tenant/module headers need the persisted context of #1164 and the route metadata of #2385** | No invented key scheme; one design for all transports | Not in this plan |
| **B) Stamp a tenant segment into the stream key now** | Early isolation | Needs the reserved `default` tenant segment and an injective key encoding that no transport has defined |

### Chosen Option: **A, defer to a follow-up issue** (recommended, pending the maintainer)

### Rationale

A wins because one design for all transports needs #1164 and #2385, and the issue itself defers it ("Tenant-scoped stream keys"); B wins for a tenant-isolated deployment that needs scoped keys before those land. The issue file is `artifacts/issues/new-transports-tenant-and-shared-contracts.md`.

**Class:** Dilemma

</details>

<details>
<summary><strong>11. Stream key shape: hash tag required or not (Dilemma D11)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) A hash tag is always required: the default `StreamKey` is `{encina}:events`, the options validator and `PublishAsync`/`SubscribeAsync` reject a key without a non-empty `{tag}` with `REDIS_STREAMS_STREAM_KEY_INVALID` (`InvalidRequest`, R3), and the dead stream is the key plus `:dead`** | Works on a standalone server and on a cluster; the dead-letter `MULTI`/`EXEC` never hits CROSSSLOT | Braces in every key name, also for users with no cluster |
| **B) No tag rule: any key is accepted; dead lettering with `MULTI`/`EXEC` is attempted only on a server that is not a cluster, and on a cluster the dead letter move is two commands (add then acknowledge)** | Plain key names; no validator rule | Two code paths; on a cluster a crash between the commands duplicates the dead entry |

### Chosen Option: **A, a hash tag is always required** (recommended, pending the maintainer)

### Rationale

A wins for one behaviour on every topology; B wins for an application with existing stream names it cannot rename. The phases below are written for A and for D4 option A; under D4 option B (the Encina dead letter store) the rule is still harmless but no longer needed.

**Class:** Dilemma

</details>

<details>
<summary><strong>12. Testability of the Redis calls (Dilemma D12)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) An internal seam `IRedisStreamOperations` over `IDatabase` (add, read group, pending, auto-claim, acknowledge, dead-letter move) that returns Encina-owned records (`PendingEntry`, `ClaimedEntries`); the publisher uses only the seam, and the thin `RedisStreamOperations` adapter is covered by the integration flag** | `StreamPendingMessageInfo` and `StreamAutoClaimResult` have internal constructors in StackExchange.Redis 2.12.14 (checked by reflection on the package assembly), so a substituted `IDatabase` cannot produce a delivery count or a claimed entry; with the seam the dead-letter and recovery branches are unit tested | Two more files; the adapter has no unit coverage |
| **B) No seam: the publisher calls `IDatabase` directly and the pending, claim and dead-letter branches are covered only by the integration flag, with a lower unit target and a justification, as `Encina.Redis.PubSub.json` does for its subscribe paths** | Fewer files | The branches with the most logic (delivery count, dead letter) have no fast test |

### Chosen Option: **A, an internal seam** (recommended, pending the maintainer)

### Rationale

A wins because the dead-letter decision is the riskiest logic of the package and needs fast tests; B wins if the maintainer prefers fewer abstractions and accepts integration-only coverage of those branches.

**Class:** Dilemma

</details>

---

## Implementation Phases

### Phase 1: Package, options and DI (pull request 1)

<details>
<summary>Tasks</summary>

1. `src/Encina.Redis.Streams/Encina.Redis.Streams.csproj` (net10.0, packages `StackExchange.Redis`, `Microsoft.Extensions.*.Abstractions`, `LanguageExt.Core`, project reference to `Encina`; `InternalsVisibleTo` for `Encina.UnitTests`, `Encina.PropertyTests` and `DynamicProxyGenAssembly2`, so the internal seam of Design Choice 12 can be substituted and driven by the property test) and the `Encina.slnx` entry.
2. `EncinaRedisStreamsOptions` (namespace `Encina.Redis.Streams`): `ConnectionString` (`[JsonIgnore]`, `ToString()` override that prints the stream key only), `StreamKey` (default `{encina}:events`; the validator rejects a key without a non-empty hash tag, Design Choice 11 option A), `ConsumerGroup`, `ConsumerName`, `MaxLength`, `MinIdAge`, `ClaimIdleTime`, `MaxDeliveryAttempts`, `BatchSize`, `BlockTimeout`, `ProviderHealthCheck`. Internal `EncinaRedisStreamsOptionsValidator : IValidateOptions<>`.
3. `ServiceCollectionExtensions.AddEncinaRedisStreams(this IServiceCollection, Action<EncinaRedisStreamsOptions>? configure = null)`: options with `ValidateOnStart`, `TryAddSingleton<IConnectionMultiplexer>`, `TryAddSingleton(TimeProvider.System)`; a DI test that builds the provider with `ValidateOnBuild` and `ValidateScopes`.
4. Tests: `EncinaRedisStreamsOptionsTests`, `ServiceCollectionExtensionsTests` (unit), `RedisStreamsGuardTests` (guard: null `services`, null `configure` accepted); manifest `.github/coverage-manifest/Encina.Redis.Streams.json`; `UnitTests.csproj` and `GuardTests.csproj` ProjectReference edits. The manifest of this pull request carries the package-level `targets` for the flags its files expect (unit and guard) and the per-file targets with one-sentence justifications of the files it adds (the table of Phase 7, rows of pull request 1), because `coverage-report.cs --check-justifications` (run by `ci.yml`) fails a manifest without them.

</details>

<details>
<summary>Prompt for AI Agents — Phase 1</summary>

```text
CONTEXT: Encina.Redis.PubSub is the template (options, DI, TryAddSingleton multiplexer). StackExchange.Redis 2.12.14 is already in Directory.Packages.props.
TASK: Create the Encina.Redis.Streams package skeleton with validated options and AddEncinaRedisStreams, and their unit and guard tests.
KEY RULES: ConnectionString is [JsonIgnore] with a ToString() override that never prints it; validation fails at startup with OptionsValidationException; TryAdd registrations only; a DI test with ValidateOnBuild and ValidateScopes; no Obsolete, no compatibility code.
REFERENCE FILES: src/Encina.Redis.PubSub/*.cs, src/Encina.NATS/EncinaNATSOptionsValidator.cs, tests/Encina.UnitTests/Messaging/RedisPubSub/ServiceCollectionExtensionsTests.cs, .github/coverage-manifest/Encina.Redis.PubSub.json.
```

</details>

### Phase 2: Publishing, envelope and trimming (pull request 2)

<details>
<summary>Tasks</summary>

1. `IRedisStreamsMessagePublisher.PublishAsync<TMessage>(TMessage message, string? streamKey = null, string? messageId = null, IReadOnlyDictionary<string,string>? headers = null, CancellationToken cancellationToken = default) where TMessage : class` returning `ValueTask<Either<EncinaError, string>>` (the stream entry id). `RedisStreamsMessagePublisher` (scoped, `IRedisStreamOperations`, `IOptions`, `ILogger`, `TimeProvider`): adds with the envelope fields of Design Choice 7 and trims from the options (`MaxLength` approximate, `MinIdAge` through the trim-by-minimum-id call computed from `TimeProvider`). The internal seam of Design Choice 12 is `IRedisStreamOperations` (here the add and trim members; pull request 3 adds read, pending, claim, acknowledge and dead-letter move) and its adapter `RedisStreamOperations` over `IConnectionMultiplexer`, registered with `TryAddSingleton` in this pull request.
2. Errors (ADR-062), in a new `RedisStreamsErrorCodes.cs` (one constants class, so later pull requests add their codes to the same file): `REDIS_STREAMS_PUBLISH_FAILED` category `Internal` (R4, provider failure), `REDIS_STREAMS_STREAM_KEY_REQUIRED` `InvalidRequest` (R3), `REDIS_STREAMS_STREAM_KEY_INVALID` `InvalidRequest` (R3, no hash tag, Design Choice 11). Cancellation propagates as `OperationCanceledException`, never a `Left` (R5). Messages are constants; the exception goes to `ForLogging()`.
3. `Log.cs` 40650-40652 (publishing, published, failed; no message ids, no payload); `EventIdRanges.RedisStreams = (40650, 40659)`; `AssemblyRanges` entry in `EncinaEventIdAllocationTests`; `docs/engineering/eventid-block-registry.md` (through `docs-writer`): the registered-ranges row `| RedisStreams | 40650-40659 | Transports | zone |` and the Blocks row `| 40650-40659 | Transports | transport-new-1 (#240) | registered |` (see "EventId allocation" in Research; no code range below 10000 is added, as `EventIdRegistryAgreementTests` rule 5 requires); and `src/Encina/PublicAPI.Unshipped.txt` (through `mechanical-fixer`) gets the line `static readonly Encina.Diagnostics.EventIdRanges.RedisStreams -> (int Min, int Max)`, because the range is a public field of `Encina`. The other four transport plans take the following slices, so their pull requests are merged one at a time (a new commit on a branch when a slot moves, never a rebase of a pushed branch).
4. DI: `AddEncinaRedisStreams` gains `services.TryAddScoped<IRedisStreamsMessagePublisher, RedisStreamsMessagePublisher>()` in this pull request (registration completeness, AGENTS.md section 3); `ServiceCollectionExtensionsTests` gains the resolution test with `ValidateOnBuild` and `ValidateScopes`. The publisher stamps the `traceparent` field of Design Choice 7 from the current activity.
5. Tests: `RedisStreamsMessagePublisherTests` (unit over a substituted `IRedisStreamOperations`, including the message-id rule), guard edit; manifest edit with the unit and guard targets of the files added here and their one-sentence justifications (the publisher's contract and integration targets, and their justifications, are entered in pull request 5, where those tests land). This pull request has 15 files: interface, publisher, `IRedisStreamOperations`, `RedisStreamOperations`, error codes, `Log`, `EventIdRanges.cs`, `src/Encina/PublicAPI.Unshipped.txt`, allocation test, registry, DI edit, DI test edit, 1 unit test, guard edit, manifest.

</details>

<details>
<summary>Prompt for AI Agents — Phase 2</summary>

```text
CONTEXT: The package skeleton exists. Publishing follows Encina.Redis.PubSub: a messageId parameter, generated only when null or empty.
TASK: Implement IRedisStreamsMessagePublisher.PublishAsync, the envelope with the traceparent field, trimming, the error codes class, the scoped DI registration and the log messages 40650-40652, with unit and guard tests.
KEY RULES: EncinaErrors.Create/FromException with a named category: argument (ADR-062); constant messages, never ex.Message; ids and payloads never logged; TimeProvider for MinIdAge; cancellation propagates; EventIds only 40650-40659, registered first in EventIdRanges.cs.
REFERENCE FILES: src/Encina.Redis.PubSub/RedisPubSubMessagePublisher.cs, src/Encina.NATS/NATSMessagePublisher.cs, src/Encina/Diagnostics/EventIdRanges.cs, tests/Encina.UnitTests/Testing/Architecture/EncinaEventIdAllocationTests.cs.
```

</details>

### Phase 3: Consumer groups, recovery and dead lettering (pull request 3)

<details>
<summary>Tasks</summary>

1. `SubscribeAsync<TMessage>(Func<TMessage, ValueTask> handler, string? streamKey = null, CancellationToken cancellationToken = default)` returning `ValueTask<IAsyncDisposable>`; creates the group (`StreamCreateConsumerGroupAsync` from `$`, ignoring `BUSYGROUP`), then runs the loop of Design Choice 6; a successful handler is followed by `StreamAcknowledgeAsync`.
2. Dead lettering (Design Choice 4, option A): when the delivery count of the pending entry (read through the seam, as an Encina-owned `PendingEntry`) exceeds `MaxDeliveryAttempts`, one `MULTI`/`EXEC` adds the entry to `<stream key>:dead` (same hash tag, same cluster slot) and acknowledges the original. Codes, added to `RedisStreamsErrorCodes.cs`: `REDIS_STREAMS_SUBSCRIBE_FAILED` `Internal` (R4), `REDIS_STREAMS_DEAD_LETTER_FAILED` `Internal` (R4); an undeserializable entry is dead lettered at once with reason code `REDIS_STREAMS_DESERIALIZE_FAILED` `Internal`.
3. `Log.cs` 40653-40659 (subscription started, group created, handler failed, claimed count, dead lettered, dead letter failed, loop iteration failed).
4. Tests: `RedisStreamsConsumerLoopTests`, `RedisStreamsDeadLetterTests` (unit over a substituted `IRedisStreamOperations`, so a delivery count above the maximum and a claimed entry can be produced), guard edit, manifest edit with the targets and justifications of the files changed here. This pull request has 10 files: publisher interface edit, publisher edit, seam interface edit, seam adapter edit, `Log` edit, error codes edit, 2 unit tests, guard edit, manifest.

</details>

<details>
<summary>Prompt for AI Agents — Phase 3</summary>

```text
CONTEXT: Publishing exists. Redis Streams consumer groups give at-least-once delivery; XACK ends redelivery.
TASK: Implement SubscribeAsync with group creation, the read and claim loop, the acknowledgement and the dead-letter move, with unit tests.
KEY RULES: The loop catches per iteration and never lets an exception escape; a handler failure leaves the entry pending and is never reported as success; OperationCanceledException from the stopping token ends the loop; dead-letter reason is an error code, never an exception message; no per-class fixtures.
REFERENCE FILES: src/Encina.Redis.PubSub/RedisPubSubMessagePublisher.cs, docs/en/guides/TESTING.md, AGENTS.md section 3.
```

</details>

### Phase 4: Health check and observability (pull request 4)

<details>
<summary>Tasks</summary>

1. `Health/RedisStreamsHealthCheck : EncinaHealthCheck` (`DefaultName = "encina-redis-streams"`, tags `encina`, `messaging`, `redis-streams`, `ready`; `PingAsync` bounded by `ProviderHealthCheck.Timeout`; result text names the check and the exception type only), registered like `NATSHealthCheck` when `ProviderHealthCheck.Enabled`.
2. `Diagnostics/RedisStreamsActivitySource` and `RedisStreamsMetrics`: spans publish, consume, acknowledge, dead-letter with a link from consumer to producer through the `traceparent` field; counters `redis_streams.published`, `.consumed`, `.dead_lettered`, `.claimed` with tags `stream`, `outcome`; no tenant tag yet. `Encina.OpenTelemetry` gains `AddSource`/`AddMeter` lines. The publisher (pull requests 2 and 3) is the emitter, so its file is edited here to start and stop the spans and increment the counters, and the metrics land in the pull request that wires them.
3. DI: the health check registration of this phase is the only DI edit of pull request 4 (the publisher was registered in pull request 2). Tests: `RedisStreamsHealthCheckTests`, `RedisStreamsDiagnosticsTests` and an edit of `RedisStreamsMessagePublisherTests` (unit), guard edit (health check constructor); manifest edit with the targets and justifications of the files added or changed here. The `Encina.OpenTelemetry` edit (the new `AddSource`/`AddMeter` lines in `src/Encina.OpenTelemetry/ServiceCollectionExtensions.cs`) is tested by a new unit test `RedisStreamsOpenTelemetryRegistrationTests` that builds the tracer and meter providers with `AddEncinaOpenTelemetry` and asserts the package source and meter names are subscribed; that file's existing per-file targets in `.github/coverage-manifest/Encina.OpenTelemetry.json` (unit 90, guard 35) are not lowered and need no new entry. This pull request has 12 files: health check, 2 diagnostics, publisher edit, DI edit, `Encina.OpenTelemetry` edit, 4 unit tests (health, diagnostics, publisher edit, OpenTelemetry registration), guard edit, manifest.

</details>

<details>
<summary>Prompt for AI Agents — Phase 4</summary>

```text
CONTEXT: Publish and consume exist. The issue requires a health check and traces and metrics.
TASK: Add the health check, the ActivitySource and Meter, and register the source and meter names in Encina.OpenTelemetry.
KEY RULES: Health results and tags never contain an exception message; spans link, never baggage (ADR-054 D2); zero cost when no listener; resolve the multiplexer through the container as NATSHealthCheck does.
REFERENCE FILES: src/Encina.NATS/Health/NATSHealthCheck.cs, src/Encina.Messaging/Diagnostics/OutboxActivitySource.cs, src/Encina.OpenTelemetry/ServiceCollectionExtensions.cs.
```

</details>

### Phase 5: Cross-cutting integration

<details>
<summary>Tasks</summary>

1. Verify each row of the matrix below against the code; the deferred rows are in the issue file `artifacts/issues/new-transports-tenant-and-shared-contracts.md` (tenant and module headers; adoption of #252, #251 and #2385 contracts).
2. Confirm every error code has its ADR-062 category and that no log call carries a message id, payload or `EncinaError.Message`.

</details>

<details>
<summary>Prompt for AI Agents — Phase 5</summary>

```text
CONTEXT: Redis.Streams publishes, consumes, dead letters, reports health and emits telemetry.
TASK: Verify the cross-cutting matrix against the code and fix any gap.
KEY RULES: AGENTS.md sections 3, 6 and 7; ADR-062 categories; deferred items point to an open issue.
REFERENCE FILES: AGENTS.md section 6, docs/architecture/adr/062-error-category-contract.md.
```

</details>

### Phase 6: Contract, property and integration tests (pull request 5)

<details>
<summary>Tasks</summary>

1. `tests/Encina.ContractTests/MessageBrokers/Conformance/TransportConformanceContractBase.cs` (Design Choice 8: publish with message id, subscribe delivers, handler failure redelivers, dead letter after N attempts, cancellation) and `tests/Encina.ContractTests/MessageBrokers/RedisStreams/RedisStreamsConformanceTests.cs` over `RedisFixture`; `ContractTests.csproj` reference.
2. `tests/Encina.IntegrationTests/MessageBrokers/RedisStreams/RedisStreamsIntegrationTests.cs` in the shared `RedisCollection` (`[Trait("Category","Integration")]`, `[Trait("Transport","Redis")]`): fanout with two groups, round robin with two consumers of one group, `XAUTOCLAIM` recovery after a consumer stops, dead-letter stream content, trimming; `RedisStreamsHealthCheckIntegrationTests.cs` (one healthy ping, the pattern of `NATSHealthCheckIntegrationTests`); `IntegrationTests.csproj` reference.
3. `tests/Encina.PropertyTests/Messaging/RedisStreams/RedisStreamsEnvelopePropertyTests.cs` (FsCheck round trip of fields, headers and message id) and the `PropertyTests.csproj` reference.
4. One manifest edit that enters the contract and integration targets of the publisher and health check (the table of Phase 7), because those flags are reachable only now.

</details>

<details>
<summary>Prompt for AI Agents — Phase 6</summary>

```text
CONTEXT: The conformance base is new; Redis is the first subclass; other transports follow.
TASK: Write the conformance base, the Redis subclass and the integration tests against the shared Redis collection.
KEY RULES: Real Redis through the existing RedisFixture and RedisCollection, never a per-class container; ClearAllDataAsync-style cleanup in InitializeAsync; no Thread.Sleep (use bounded waits on TaskCompletionSource); Shouldly through Encina.Testing.Shouldly.
REFERENCE FILES: tests/Encina.IntegrationTests/Messaging/RedisPubSub/RedisPubSubMessagePublisherIntegrationTests.cs, tests/Encina.ContractTests/Messaging/RedisPubSub/RedisPubSubPublisherContractTests.cs, tests/Encina.TestInfrastructure/Fixtures/RedisFixture.cs.
```

</details>

### Phase 7: Documentation and finalization (pull request 6)

<details>
<summary>Tasks</summary>

Per-flag targets are entered in `.github/coverage-manifest/Encina.Redis.Streams.json` in the pull request that adds the tests of each flag, each with its one-sentence justification and with the package-level `targets` of that flag in the same pull request (`coverage-report.cs --check-justifications`, run by `ci.yml`, fails a manifest without them). The numbers are the planned ceilings; each pull request replaces them with the measured value floored to a multiple of 5 when that is lower (never lowering a target already entered), measured with the commands of AGENTS.md section 9.

| Pull request | File | unit | guard | contract | property | integration | Justification |
|---|---|:---:|:---:|:---:|:---:|:---:|---|
| 1 | `EncinaRedisStreamsOptions.cs`, `EncinaRedisStreamsOptionsValidator.cs` | 100 | 0 | 0 | 0 | 0 | Pure data and validation; guard tests have no argument to check. |
| 1 | `ServiceCollectionExtensions.cs` | 85 | 10 | 0 | 0 | 0 | Unit tests build the provider for each option combination; guard tests reach only the null check of `services`, a small share of the lines. |
| 2 | `RedisStreamsMessagePublisher.cs` (publish part) | 85 | 20 | 0 | 0 | 0 | Unit tests over the seam reach every publish branch; guard tests reach only the argument checks. |
| 2 | `RedisStreamOperations.cs` | 0 | 0 | 0 | 0 | 0 | The adapter only forwards to StackExchange.Redis, whose result types cannot be built in a unit test; its target is entered with its integration tests in pull request 5. |
| 2 | `RedisStreamsErrorCodes.cs`, `IRedisStreamsMessagePublisher.cs`, `IRedisStreamOperations.cs`, `Log.cs` | 0 | 0 | 0 | 0 | 0 | Constants, interfaces and generated delegates have no executable lines. |
| 3 | `RedisStreamsMessagePublisher.cs` (consume part) | 85 | 20 | 0 | 0 | 0 | The seam returns Encina-owned records, so delivery counts above the maximum, claimed entries and failing handlers are unit tested. |
| 4 | `Health/RedisStreamsHealthCheck.cs` | 90 | 40 | 0 | 0 | 0 | A substituted multiplexer reaches the healthy and unhealthy branches; guard tests reach the constructor checks. |
| 4 | `Diagnostics/*.cs` | 80 | 0 | 0 | 0 | 0 | Counters and spans are exercised through publisher and consumer unit tests; the static initializers are the remainder. |
| 5 | `RedisStreamsMessagePublisher.cs` | 85 | 20 | 50 | 0 | 70 | The conformance base runs the common publish and subscribe scenarios; integration tests run the real group, claim and dead-letter paths on Redis, leaving connection-failure branches. |
| 5 | `RedisStreamOperations.cs` | 0 | 0 | 0 | 0 | 80 | Integration tests drive every member against a real Redis. |
| 5 | `Health/RedisStreamsHealthCheck.cs` | 90 | 40 | 0 | 0 | 50 | One real ping scenario (the health-check integration class); the failure branch needs a broken connection. |
| 5 | envelope helper (inside the publisher) | 85 | 20 | 50 | 40 | 70 | The property test round-trips fields, headers and message id through the publisher over the seam. |

1. `src/Encina.Redis.Streams/README.md`, `docs/features/redis-streams.md`, the Redis Streams section of `docs/messaging/transports.md` and `docs/INVENTORY.md` through `docs-writer`; changelog fragment `changelog.d/240-redis-streams.added.md` through `mechanical-fixer`. Transport packages carry no `PublicAPI.*.txt` today (none in NATS, Redis.PubSub, Kafka), so none is added.
2. Load and benchmark: justification files `tests/Encina.LoadTests/Messaging/RedisStreams/RedisStreams.md` and `tests/Encina.BenchmarkTests/Messaging/RedisStreams/RedisStreams.md` (throughput is Redis's; the Redis.PubSub precedent has the same two files).
3. `dotnet build --configuration Release` with zero warnings; every flag at its target; CRAP of every new method at most 10.

</details>

<details>
<summary>Prompt for AI Agents — Phase 7</summary>

```text
CONTEXT: The package is implemented and tested; documentation and the justification files remain.
TASK: Write the two justification files (the manifest targets and their justifications were entered in pull requests 1 to 5), and hand the README, feature page, transports page, INVENTORY and changelog fragment to the owning specialists.
KEY RULES: Targets never go down; no hand-typed coverage figures in docs; never edit CHANGELOG.md; docs through docs-writer, fragments through mechanical-fixer.
REFERENCE FILES: tests/Encina.LoadTests/Messaging/RedisPubSub/RedisPubSub.md, .github/coverage-manifest/Encina.Redis.PubSub.json, changelog.d/README.md.
```

</details>

---

## Research

### Relevant Standards & Specifications

| Standard / Spec | Relevance | Notes |
|-----------------|-----------|-------|
| `AGENTS.md` sections 3, 5, 6, 7, 9 | Background loops, transport category, 12 functions, EventIds, coverage | Phases 1 to 7 |
| SPEC-000 DEC-009 | Redis.Streams is one of the six planned transports in 1.0; Redis needs no cloud emulator | Scope |
| ADR-062, ADR-054 D2 and D4 | Error categories; trace links and tenant tags | Phases 2 and 4 |
| Redis Streams commands `XADD`, `XREADGROUP`, `XACK`, `XAUTOCLAIM`, `XCLAIM`, `XTRIM` (named by the issue) | Native mechanisms | Design Choices 4 to 6 |

### Existing Encina Infrastructure to Leverage

| Component | Location | Usage in this feature |
|-----------|----------|-----------------------|
| `Encina.Redis.PubSub` | `src/Encina.Redis.PubSub/` | Template for options, DI and the subscription shape |
| `EncinaHealthCheck`, `ProviderHealthCheckOptions` | `src/Encina.Messaging/Health/` | Health check base and options |
| `RedisFixture`, `RedisCollection` | `tests/Encina.TestInfrastructure/Fixtures/RedisFixture.cs`, `tests/Encina.IntegrationTests/Infrastructure/Caching/Fixtures/RedisFixture.cs` | Shared Redis container (`redis:7-alpine`) |
| `OutboxActivitySource` | `src/Encina.Messaging/Diagnostics/OutboxActivitySource.cs` | Pattern for the source and meter |
| `EventIdRanges` | `src/Encina/Diagnostics/EventIdRanges.cs` | New field `RedisStreams` |

### Event ID Allocation

| Package | Range | Notes |
|---------|-------|-------|
| `RedisStreams` (new) | 40650-40659 | 40650-40652 publish, 40653-40659 consume; first slice of the family |

**EventId allocation.** AGENTS.md section 7 allows new blocks only in the 10000+ zone and `EventIdRegistryAgreementTests` rule 5 fails any new range below 10000, so the family does not use the old 1050-1099 reservation. The Transports blocks 40000-40549 are reserved by #4007 for the existing transports (RabbitMQ 40000-40049 through SignalR 40500-40549) and 40550-40649 are claimed by the transport-new-2 plans (#242, #610, #611, #612, #310, 20 ids each, in their open pull requests), so the five plans of transport-new-1 take contiguous 10-id slices from 40650: `RedisStreams` 40650-40659 (this plan), `Pulsar` 40660-40669 (#239), `ActiveMQ` 40670-40679 (#241), `GoogleCloudPubSub` 40680-40689 (#237), `AmazonEventBridge` 40690-40699 (#238). Each plan's registering pull request adds its own registered-ranges row (Zone `zone`) and its own Blocks row with status `registered`, so no `reserved` row overlaps a registered range. If another plan has taken the slot first, the registering pull request takes the next free slot and the ids of the plan shift by the same offset.

### Estimated File Count

| Category | Files | Notes |
|----------|------:|-------|
| Production code | 10 | csproj, options, validator, interface, publisher, `Log`, DI, health check, 2 diagnostics |
| Tests | 14 | options, DI, publisher, message id, consumer loop, dead letter, health, diagnostics, guard, property, conformance base and subclass, integration, manifest |
| Shared edits | 8 | `Encina.slnx`, 4 test `.csproj` files, `EventIdRanges.cs`, allocation test, `Encina.OpenTelemetry` |
| Documentation | 8 | README, feature page, transports page, INVENTORY, registry row, changelog, 2 justification files |
| **Pull request 1** | **11** | csproj (with the three `InternalsVisibleTo` entries), options, validator, DI, slnx, 2 test csproj, manifest (package targets, targets and justifications), options test, DI test, guard test; about 450 lines |
| **Pull request 2** | **15** | publisher interface, publisher, `IRedisStreamOperations`, `RedisStreamOperations`, `RedisStreamsErrorCodes`, `Log`, `EventIdRanges.cs`, `PublicAPI.Unshipped.txt`, allocation test, registry (registered row and Blocks row), DI edit (seam and scoped publisher), DI test edit, 1 unit test, guard edit, manifest edit; about 850 lines |
| **Pull request 3** | **10** | publisher interface edit, publisher edit, seam interface and adapter edits, `Log` edit, error codes edit, 2 unit tests, guard edit, manifest edit; about 700 lines |
| **Pull request 4** | **12** | health check, 2 diagnostics, publisher edit (spans and counters), DI edit (health check only), `Encina.OpenTelemetry` edit, 4 unit tests (health, diagnostics, publisher edit, OpenTelemetry registration), guard edit, manifest edit; about 650 lines |
| **Pull request 5** | **9** | conformance base, subclass, integration test, health check integration test, property test, `ContractTests.csproj`, `IntegrationTests.csproj`, `PropertyTests.csproj`, manifest (contract, property and integration targets with justifications); about 750 lines |
| **Pull request 6** | **7** | README, feature page, transports page, INVENTORY, changelog fragment, 2 justification files; about 350 lines |

Implementers and callers: `IRedisStreamsMessagePublisher` is new, so it has no implementers or `new X(` sites to update; the shared edits are additive. Each pull request builds alone in the order 1 to 6 (each needs the previous one); pull request 1 registers only options, the multiplexer and `TimeProvider`, pull request 2 the scoped publisher, pull request 4 the health check.

---

## Combined AI Agent Prompts

<details>
<summary>Combined prompt for all phases</summary>

```text
PROJECT CONTEXT: Encina is a pre-1.0 .NET 10 / C# 14 library; no backward compatibility, zero warnings, ROP (Either<EncinaError, T>). Transports are publisher-only except MQTT and Redis.PubSub; Encina.Redis.Streams adds a durable transport.

IMPLEMENTATION OVERVIEW: PR1 package, options, DI; PR2 publish with envelope and trimming and the EventId block 40650-40659; PR3 consumer groups, XAUTOCLAIM recovery and dead-letter stream; PR4 health check and telemetry; PR5 conformance base, property and integration tests on the shared Redis collection; PR6 documentation and justification files. Dilemmas D2 to D12 are implemented as the maintainer answers, the recommended options until then.

KEY PATTERNS: at-least-once, never success on a failure; background loop catches per iteration; named ADR-062 category on every error; constant error messages; ids and payloads never logged; secrets [JsonIgnore] with ToString override; TimeProvider; targets never go down; CRAP <= 10.

REFERENCE FILES: src/Encina.Redis.PubSub, src/Encina.NATS, src/Encina.Messaging/Health, tests/Encina.IntegrationTests/Messaging/RedisPubSub, src/Encina/Diagnostics/EventIdRanges.cs.
```

</details>

---

## Cross-Cutting Integration Matrix

| # | Function | Status | Notes |
|---|----------|--------|-------|
| 1 | Caching | ❌ | A transport does not cache messages. |
| 2 | OpenTelemetry | ✅ | Per-package `ActivitySource` and `Meter` (Design Choice 9); the shared design of #725 may rename them. |
| 3 | Structured Logging | ✅ | `[LoggerMessage]` in `Log.cs`, EventIds 40650-40659; error codes and exception types only. |
| 4 | Health Checks | ✅ | `RedisStreamsHealthCheck` pings Redis. |
| 5 | Validation | ❌ | Options are validated at startup; payload validation at the boundary belongs to the validation behaviors of the consuming handler. |
| 6 | Resilience | ⏭️ | Retry and circuit breaking for transports is #742 (open); redelivery of failed entries is built in. |
| 7 | Distributed Locks | ❌ | Consumer groups give each entry to one consumer. |
| 8 | Transactions | ❌ | The only atomic step is the dead-letter `MULTI`/`EXEC`; no unit of work is involved. |
| 9 | Idempotency | ✅ | The message id is carried in every entry; processing idempotency stays with the inbox (redelivery is possible). |
| 10 | Multi-Tenancy | ⏭️ | Tenant-scoped keys and headers need #1164 and #2385; issue file `new-transports-tenant-and-shared-contracts`. |
| 11 | Module Isolation | ⏭️ | Same issue file. |
| 12 | Audit Trail | ❌ | Transport is infrastructure; audit belongs to the outbox and inbox. |

---

## Prerequisites & Dependencies

- None blocking pull request 1. Pull requests 2 to 6 follow in order.
- **Related, not blocking**: #1609 (the other five transports gain subscribe with the shape chosen in D2 and D3), #256 (Consumer Groups abstraction, open; the consumer-group options here are Redis-specific until it lands), #251, #252 and #2385 (contracts the new transports adopt through the issue file), #725 and #742 (telemetry and resilience of all transports), #1164.
- **Sibling plans**: #237, #238, #239 and #241 subclass the conformance base of Design Choice 8 after pull request 5 lands; transport-new-2 does the same.

---

## Next Steps

1. Ask the maintainer D2, D3, D4, D5, D6, D7, D8, D9, D10, D11 and D12 when work on #240 starts; record the answers in `## Maintainer Decisions`.
2. Pull request 1: package skeleton (11 files).
3. Pull requests 2 to 4: publish (15), consume and dead letter (10), health and telemetry (12).
4. Pull requests 5 and 6: contract, property and integration tests (9), documentation (7).
