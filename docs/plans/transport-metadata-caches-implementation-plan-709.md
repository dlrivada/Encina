# Implementation Plan: Per-entity caching in the message transport publishers

> **Issue**: [#709](https://github.com/dlrivada/Encina/issues/709)
> **Type**: Feature
> **Complexity**: Low (4 phases, 1 pull request of 14 files; one optional second pull request of 11 files)
> **Estimated Scope**: about 150 lines of production code, about 500 lines of tests
> **Milestone**: v0.18.0 - Integration & Platform Capabilities (transport, caching, performance, messaging)

---

## Summary

Issue #709 asks for in-memory caches in the publishers of six transports (RabbitMQ, Kafka, NATS, MQTT, AmazonSQS, AzureServiceBus): topic or queue names per message type, RabbitMQ exchange declarations, routing keys, header templates and Kafka cluster metadata. Re-checked against `origin/main` (db1e7f688), almost none of those costs exist:

| Item of the issue | State in the code |
|-------------------|-------------------|
| Message type to topic or queue mapping evaluated per publish | No such mapping. Every publisher takes the destination from the caller or from options: `topic ?? _options.DefaultEventTopic` (Kafka, `KafkaMessagePublisher.cs`), `queueName ?? _options.DefaultQueueName` (Azure Service Bus), the SQS queue URL and SNS topic ARN from options. Only MQTT (`$"{TopicPrefix}/{typeof(TMessage).Name}"`) and NATS (`$"{SubjectPrefix}.{Name}"`) derive a default from the type, with one string interpolation per publish |
| RabbitMQ exchange declarations before each publish | None. `src/Encina.RabbitMQ` contains no `ExchangeDeclare`, `QueueDeclare` or `Declare` call; the publisher calls `BasicPublishAsync` on the configured `ExchangeName` |
| Routing key calculated per message | `routingKey ?? typeof(TMessage).Name`: a property read of a runtime-cached string |
| Header templates built per message | The headers carry a unique message id and timestamp per message (`CreateProperties`, `CreateHeaders`, the SQS attribute dictionaries), so there is no per-type template; the only per-type constant is the Kafka `MessageType` header, `Encoding.UTF8.GetBytes(typeof(TMessage).FullName ...)` (2 sites, lines 73 and 185) |
| Kafka cluster metadata requested periodically | Handled inside librdkafka by the singleton `IProducer<string, byte[]>`; Encina issues no metadata request |

One real per-publish cost remains, and it is I/O, not CPU: `AzureServiceBusMessagePublisher` creates a `ServiceBusSender` with `_client.CreateSender(name)` and disposes it with `await using` on every send, publish, schedule and cancel (4 sites, lines 63, 106, 150 and 192), so each call attaches and detaches an AMQP link. All five other transports reuse one singleton client, channel, producer or connection (registered with `TryAddSingleton`) while their publishers are scoped.

The plan caches Azure Service Bus senders per entity name in a singleton `ServiceBusSenderCache` owned by the container, and records the other five transports as investigated with no change. A second, optional pull request caches the three per-type constants (Kafka `MessageType` bytes, MQTT and NATS default destination strings) only if the maintainer chooses it after the benchmark (Design Choice 1).

**Affected packages:** `Encina.AzureServiceBus` (cache, publisher, registration, log). `Encina.RabbitMQ`, `Encina.NATS`, `Encina.AmazonSQS` are investigated and unchanged; `Encina.Kafka`, `Encina.MQTT`, `Encina.NATS` change only in the optional second pull request.

**Provider category**: Transports (10 + 6 planned; AGENTS.md section 5). The change is specific to the one transport whose client API hands out per-entity senders; the other nine existing transports have no per-entity sender object, which is why they do not need the same cache. Coherence is therefore by outcome (no per-publish link or channel setup on any transport), and the table in Research records the evidence per transport.

**Sibling plans (caching family)**: none of the sibling plans changes a transport publisher.

---

## Design Choices

<details>
<summary><strong>1. Scope after verification</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Cache Azure Service Bus senders only; record the other transports as investigated with no change** | Removes the one real per-publish cost (a link attach per call); no code for costs that do not exist | Most of the issue's headings close without code |
| **B) A plus per-type constants: Kafka `MessageType` header bytes, MQTT and NATS default destinations (a second pull request, gated by a benchmark of allocations per publish)** | Removes about one small allocation per publish in three transports | Micro-gain next to the broker round trip; shared `byte[]` header values must never be mutated |
| **C) The issue literally** (topic, exchange, routing-key and header caches in all six) | Matches the text | Caches that hold nothing: no mapping, no declaration, no template exists |

### Chosen Option: **A - Azure Service Bus sender cache only** (recommended, pending the maintainer)

### Rationale

- Option B wins when publishing is allocation-sensitive at very high volume and a benchmark shows a gain; the optional pull request is written so that it can be briefed without redoing the investigation.
- Option C is rejected by the code, not by taste: each of its caches would have no source of values.
- Class: Dilemma

</details>

<details>
<summary><strong>2. Where the senders live</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) A public sealed `ServiceBusSenderCache : IAsyncDisposable`, registered with `TryAddSingleton` next to the client, injected into the scoped publisher** | The container owns and disposes the senders before the client (it is created after it); the scoped publisher does not own anything, as the regression #1608 requires; no scoped service in a singleton | The publisher constructor gains a parameter (15 construction sites in 4 test files: `AzureServiceBusGuardTests.cs` 9, `AzureServiceBusMessagePublisherTests.cs` 4, `AzureServiceBusPublisherMessageIdTests.cs` 1, `AzureServiceBusPublisherContractTests.cs` 1) |
| **B) Static `ConcurrentDictionary` in the publisher** | No constructor change | Senders would outlive the container-owned `ServiceBusClient` and nothing disposes them |
| **C) Cache inside a wrapper around `ServiceBusClient`** | One object | Changes what the client registration means for every consumer of `ServiceBusClient` |

### Chosen Option: **A - singleton cache owned by the container**

### Rationale

- **Class:** Trivial
- **Settled by:** #1608 (a scope that resolved the publisher must not dispose the singleton `ServiceBusClient`; `AzureServiceBusPublisherLifetimeTests`) and AGENTS.md section 3 (registration completeness proven by a DI test with `ValidateOnBuild` and `ValidateScopes`; no scoped service injected into a singleton).

</details>

<details>
<summary><strong>3. Key, bound and replacement of cached senders</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Key: the entity name, `StringComparer.OrdinalIgnoreCase` (Service Bus entity names are case-insensitive); at most 256 entries, beyond which a sender is created, used and disposed as today; a cached sender whose `IsClosed` is true is replaced and the replaced instance is disposed (a failing dispose is logged by `SenderCloseFailed`, never thrown); a send that throws never evicts** | Bounded memory when a caller derives names from data; a closed sender is never reused; the SDK recovers links itself | 256 is a constant, not an option |
| **B) Unbounded** | Simplest | A caller generating a queue name per message grows the cache without limit |
| **C) LRU with eviction and disposal** | Bounded and reusable | An evicted sender can be in use by a concurrent send; more code than the issue's scope |

### Chosen Option: **A - bounded, case-insensitive, replace when closed** (recommended, pending the maintainer)

### Rationale

- `ServiceBusSender.IsClosed` is the SDK's own signal that the sender must not be used (https://learn.microsoft.com/dotnet/api/azure.messaging.servicebus.servicebussender.isclosed); a transient `ServiceBusException` is retried by the SDK and is not a reason to drop the link.
- Entity names come from options or from the caller; the publisher never builds a tenant into a name, so two tenants publishing to different queues use two entries and a name never merges tenants. No request value, tenant or message content is cached.
- Option C wins if a deployment legitimately publishes to thousands of dynamic entities.
- Class: Dilemma

</details>

<details>
<summary><strong>4. Failure and disposal</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `DisposeAsync` closes every sender, catching each failure and logging it by exception type (EventId 4312, inside the existing `AzureServiceBus` range 4300-4349), never throwing; `GetSender` after disposal throws `ObjectDisposedException`, which the publisher's existing `catch` turns into its existing `Left`** | Nothing swallowed silently; shutdown always completes | One log message |
| **B) Ignore close failures** | No log | A swallowed failure |
| **C) Close every sender, collect the failures and throw an `AggregateException` from `DisposeAsync`** | Nothing is hidden from the caller | A container shutdown fails on a broker fault; throwing from `DisposeAsync` breaks the dispose contract for the remaining services the container is disposing |

### Chosen Option: **A** (recommended, pending the maintainer)

### Rationale

- AGENTS.md section 3 excludes option B (errors are never swallowed; an exception passed to a logger goes through `ForLogging()`, never its message), but it does not choose between A and C: whether a failed close at shutdown is a log line or an exception, and its level (Warning), are choices of this plan. The EventId rule of section 7 is met by either (ids inside the package's existing range need no new range: 4312 follows the last used id, 4311).
- Option C wins if the application wants a failed shutdown to surface as an error.
- Class: Dilemma

</details>

<details>
<summary><strong>5. How the integration test observes sender reuse</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) A counting `ServiceBusClient` subclass registered by the test (`CreateSender` is `virtual`; the cache receives it through the container) that counts calls and delegates; two sends to one queue assert one `CreateSender` call** | No production member for tests; no public API change | The test registers its own client, so it covers the cache through that registration |
| **B) A public read-only `CachedSenderCount` on `ServiceBusSenderCache`** | Simple assertion | A public member that exists only for tests |
| **C) An `InternalsVisibleTo` for the integration project and an internal `Count`** | No public member | Verified on `origin/main`: no `InternalsVisibleTo` exists in `src/Encina.AzureServiceBus` or its csproj; adding one is a package-level decision |

### Chosen Option: **A - counting client** (recommended, pending the maintainer)

### Rationale

- The package ships no `PublicAPI.*.txt` files, so B would add a member to an untracked surface; A needs neither a public member nor a visibility attribute. The unit tests count with `Received(1).CreateSender(...)` on a substitute client.
- Option B wins if the maintainer wants `CachedSenderCount` as an operational diagnostic; option C wins if `InternalsVisibleTo` is wanted for the package anyway.
- Class: Dilemma

</details>

---

## Implementation Phases

### Phase 1: Azure Service Bus sender cache (pull request 1)

<details>
<summary><strong>Tasks</strong></summary>

PR 1 holds exactly these 14 files:

| # | File | Change |
|---|------|--------|
| 1 | `src/Encina.AzureServiceBus/ServiceBusSenderCache.cs` | new |
| 2 | `src/Encina.AzureServiceBus/AzureServiceBusMessagePublisher.cs` | modify (4 sites take the sender from the cache and no longer dispose it) |
| 3 | `src/Encina.AzureServiceBus/ServiceCollectionExtensions.cs` | modify (`TryAddSingleton<ServiceBusSenderCache>`) |
| 4 | `src/Encina.AzureServiceBus/Log.cs` | modify (EventId 4312 `SenderCloseFailed`) |
| 5 | `tests/Encina.UnitTests/AzureServiceBus/Publishing/ServiceBusSenderCacheTests.cs` | new |
| 6 | `tests/Encina.UnitTests/AzureServiceBus/Publishing/AzureServiceBusMessagePublisherTests.cs` | modify (construction sites; the sender substitute comes from the cache) |
| 7 | `tests/Encina.UnitTests/AzureServiceBus/Publishing/AzureServiceBusPublisherMessageIdTests.cs` | modify (construction sites) |
| 8 | `tests/Encina.UnitTests/AzureServiceBus/Publishing/AzureServiceBusPublisherLifetimeTests.cs` | modify (cache disposed with the container, client untouched by the scope) |
| 9 | `tests/Encina.GuardTests/AzureServiceBus/AzureServiceBusGuardTests.cs` | modify (publisher and cache constructors) |
| 10 | `tests/Encina.ContractTests/MessageBrokers/AzureServiceBus/AzureServiceBusPublisherContractTests.cs` | modify (construction sites) |
| 11 | `tests/Encina.IntegrationTests/MessageBrokers/AzureServiceBus/AzureServiceBusPublisherIntegrationTests.cs` | modify (the existing emulator collection fixture: two sends to one queue use one cached sender, observed through a counting client of Design Choice 5; a closed sender is replaced) |
| 12 | `tests/Encina.BenchmarkTests/Encina.Benchmarks/Messaging/AzureServiceBus/SenderCache.md` | new (benchmark justification, see task 7) |
| 13 | `.github/coverage-manifest/Encina.AzureServiceBus.json` | modify (`mechanical-fixer`) |
| 14 | `changelog.d/709-azure-service-bus-sender-cache.changed.md` | new (`mechanical-fixer`) |

The package ships no `PublicAPI.*.txt` files and no README today (verified), so neither is counted. `docs/INVENTORY.md` lists packages and categories, not public types (the Azure Service Bus entries are at lines 2084, 2097 and 7390), so a new type gives it nothing to record and it is not a file of this plan.

Tasks:

1. **Create** `public sealed class ServiceBusSenderCache : IAsyncDisposable` with constructor `(ServiceBusClient client, ILogger<ServiceBusSenderCache> logger)` (`ArgumentNullException.ThrowIfNull` on both) and `ServiceBusSender GetSender(string entityName)` (`ArgumentException.ThrowIfNullOrWhiteSpace`): a `ConcurrentDictionary<string, ServiceBusSender>` with `StringComparer.OrdinalIgnoreCase`; a closed sender is replaced through `TryUpdate` and the replaced (closed) instance is disposed after the update (a sender created by a caller that loses the `TryUpdate` race is disposed too; a failing dispose is logged by `SenderCloseFailed` and never thrown); beyond 256 entries the call returns a sender the caller must dispose, and the publisher disposes it (`GetSender` returns a small struct `(ServiceBusSender Sender, bool Owned)` for that reason).
2. **Modify** the four sites of the publisher to `var lease = _senderCache.GetSender(name)` and `if (lease.Owned) await lease.Sender.DisposeAsync()` in a `finally`; the existing `try/catch` and `Left` codes are unchanged. The constructor becomes `(ServiceBusClient client, ServiceBusSenderCache senderCache, ILogger<AzureServiceBusMessagePublisher> logger, IOptions<EncinaAzureServiceBusOptions> options)`; update the 15 construction sites in 4 test files (`Select-String -Pattern 'new AzureServiceBusMessagePublisher\('` finds them: `AzureServiceBusGuardTests.cs` 9, `AzureServiceBusMessagePublisherTests.cs` 4, `AzureServiceBusPublisherMessageIdTests.cs` 1, `AzureServiceBusPublisherContractTests.cs` 1; `AzureServiceBusPublisherLifetimeTests.cs` has none because it resolves the publisher through DI). The guard tests also gain a null check of the new `senderCache` parameter.
3. **Register** the cache with `services.TryAddSingleton<ServiceBusSenderCache>()` after the client (`ServiceCollectionExtensions.cs`, after the `ServiceBusClient` registration), so a test that registers its own client keeps working.
4. **Dispose** closes every cached sender, catching each failure (`SenderCloseFailed(ILogger, Exception)`, EventId 4312, `ex.ForLogging()`).
5. **Tests**: same entity name returns the same sender (`ShouldBeSameAs`), different names return different senders, case differences share one entry, a closed sender is replaced and the replaced instance is disposed, the 257th entity returns an owned sender, concurrent `GetSender` calls (64, `Parallel.For`) create one sender per name (use `Lazy` or `GetOrAdd` with a factory guard so a sender created twice is disposed), `DisposeAsync` closes all and logs a failing close without throwing, a send after disposal returns the publisher's existing `Left`; a DI test with `ValidateOnBuild` and `ValidateScopes` resolves the publisher in two scopes and finds the same cache and no client disposal; guard tests for both constructors and `GetSender`.
6. **Integration** (shared `ServiceBusEmulatorFixture` collection): two sends and one publish to the same queue and topic succeed; the second send reuses the sender (asserted by a counting `ServiceBusClient` subclass that overrides the virtual `CreateSender`, counts and delegates, Design Choice 5; no production member is added for the test).
7. **Benchmark justification** `SenderCache.md` (the format of `Messaging/DeadLetter/DeadLetterStore.md`): a meaningful benchmark needs a real broker round trip (the emulator is a Docker container, not a micro-benchmark target) and the saving is one AMQP link attach per call; the integration test proves the reuse and the cache is covered by unit tests.
8. **Manifest**: targets in Phase 3.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 1</strong></summary>

```
CONTEXT:
Encina is a .NET 10 / C# 14 library, pre-1.0. Issue #709: AzureServiceBusMessagePublisher creates and disposes a ServiceBusSender
on every call (4 sites). The publisher is scoped, ServiceBusClient is a singleton owned by the container (#1608).

TASK:
1. Create ServiceBusSenderCache (singleton, IAsyncDisposable) with the key, bound and replacement rules of the plan.
2. Inject it into the publisher, replace the four CreateSender sites, update the 15 test construction sites.
3. Register it with TryAddSingleton; add the 4312 log; write the unit, guard, contract, DI and integration tests.
4. Ask mechanical-fixer for the manifest and the changelog fragment.

KEY RULES:
- The publisher must not dispose cached senders or the client; only the cache disposes senders, only the container disposes the cache.
- Nothing tenant- or message-specific is cached; the key is the entity name only.
- DisposeAsync never throws; a failed close is logged by exception type through ForLogging(), never the message.
- A concurrent first use must not leak a second sender: dispose the loser.
- Integration tests use the shared ServiceBusEmulatorFixture collection, never a per-class container.
- Zero warnings; CRAP of every added method at most 10.

REFERENCE FILES:
- src/Encina.AzureServiceBus/AzureServiceBusMessagePublisher.cs
- src/Encina.AzureServiceBus/ServiceCollectionExtensions.cs
- src/Encina.AzureServiceBus/Log.cs
- tests/Encina.UnitTests/AzureServiceBus/Publishing/AzureServiceBusPublisherLifetimeTests.cs
- tests/Encina.IntegrationTests/MessageBrokers/AzureServiceBus/ServiceBusEmulatorCollection.cs
```

</details>

### Phase 2: Optional per-type constants (pull request 2, only if Design Choice 1 is answered B)

<details>
<summary><strong>Tasks</strong></summary>

PR 2 holds exactly these 11 files and is briefed only after the maintainer answers B and a benchmark shows the gain:

| # | File | Change |
|---|------|--------|
| 1 | `src/Encina.Kafka/KafkaMessagePublisher.cs` | modify (a static generic `MessageTypeHeader<TMessage>.Bytes`, built once, never mutated, used at the 2 sites) |
| 2 | `src/Encina.MQTT/MQTTMessagePublisher.cs` | modify (default topic per `(prefix, type)` from a static cache) |
| 3 | `src/Encina.NATS/NATSMessagePublisher.cs` | modify (default subject per `(prefix, type)`) |
| 4 | `tests/Encina.UnitTests/Kafka/Publishing/KafkaMessagePublisherTests.cs` | modify |
| 5 | `tests/Encina.UnitTests/MQTT/Publishing/MQTTMessagePublisherTests.cs` | modify |
| 6 | `tests/Encina.UnitTests/NATS/Publishing/NATSMessagePublisherTests.cs` | modify |
| 7 | `tests/Encina.BenchmarkTests/Encina.Benchmarks/Messaging/Transports/PublisherAllocationBenchmarks.cs` | new (allocations per publish, the gate) |
| 8 | `.github/coverage-manifest/Encina.Kafka.json` | modify (`mechanical-fixer`) |
| 9 | `.github/coverage-manifest/Encina.MQTT.json` | modify (`mechanical-fixer`) |
| 10 | `.github/coverage-manifest/Encina.NATS.json` | modify (`mechanical-fixer`) |
| 11 | `changelog.d/709-transport-per-type-constants.changed.md` | new (`mechanical-fixer`) |

Tasks: the MQTT and NATS caches key by `(prefix, Type)` (a static generic holder per `TMessage` holding a small dictionary by prefix), because the prefix is instance state of a scoped publisher; the key is injective by construction (the prefix is a separate dictionary key, not concatenated). The Kafka header array is shared and read-only: a test asserts the same instance is passed for two messages and that `Headers.Add` is the only consumer. No other transport changes.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 2</strong></summary>

```
CONTEXT:
Only briefed if the maintainer chose option B of Design Choice 1 and the benchmark shows a gain. Phase 1 is merged.

TASK:
Write PublisherAllocationBenchmarks first; if allocations per publish do not drop measurably, stop and report. Then cache the Kafka
MessageType header bytes and the MQTT and NATS default destinations per (prefix, type).

KEY RULES:
- The prefix is instance state: never cache a destination keyed by Type alone.
- Cached byte[] values are never mutated; never cache a message id, a timestamp or any per-message value.
- BenchmarkSwitcher, not BenchmarkRunner.Run<T>().

REFERENCE FILES:
- src/Encina.Kafka/KafkaMessagePublisher.cs
- src/Encina.MQTT/MQTTMessagePublisher.cs
- src/Encina.NATS/NATSMessagePublisher.cs
```

</details>

### Phase 3: Observability, logging and coverage targets

<details>
<summary><strong>Tasks</strong></summary>

1. **Logging**: one message, EventId 4312 `SenderCloseFailed` (Warning, exception through `ForLogging()`), inside `AzureServiceBus` 4300-4349 (4300-4311 used); no new range, so `EncinaEventIdAllocationTests` is unchanged. A cache hit or miss is not logged.
2. **Telemetry**: none added. ADR-054 D4 (tenant tag when multi-tenancy is on) does not apply because no span or metric is created; the cache holds entity names only.
3. **Error codes**: none added; a failure keeps the publisher's existing `Left` codes (ADR-062 categories of those codes belong to the transport producer slice, outside this issue).
4. **Coverage targets (per flag, new or touched files)**, entered by `mechanical-fixer` in the PR that adds the tests:

| File | Unit | Guard | Contract | Integration | Justification |
|------|:----:|:-----:|:--------:|:-----------:|---------------|
| `Encina.AzureServiceBus/ServiceBusSenderCache.cs` | 95 | 25 | 0 | 60 | Every branch (hit, miss, case, closed, bound, dispose failure) is driven with substituted senders; guard reaches the argument checks; the emulator test reaches the real reuse; no contract flag targets a concrete cache |
| `Encina.AzureServiceBus/AzureServiceBusMessagePublisher.cs` | keeps 90 | keeps 15 | keeps 90 | keeps 90 | Existing targets are not lowered (target-ratchet); the changed sites are covered by the existing publisher tests |
| `Encina.AzureServiceBus/ServiceCollectionExtensions.cs` | keeps 95 | keeps 95 | 0 | keeps 90 | The registration line is covered by the DI test |
| `Encina.AzureServiceBus/Log.cs` | 60 | 0 | 0 | 0 | Generated logger bodies run only when the failing close is exercised; no parameters a guard test could reach |

5. **Property, load**: not required (a dictionary of senders with no non-trivial invariant beyond the unit tests; the concurrent unit test covers the shared state); the manifest `reason` says so.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 3</strong></summary>

```
CONTEXT:
The cache, the publisher changes and the tests exist.

TASK:
Measure per-flag coverage of the touched files with coverage-report.cs and record targets with one-sentence justifications
through mechanical-fixer; run --check-justifications.

KEY RULES:
- Never lower an existing target.
- EventId 4312 only; no new range.

REFERENCE FILES:
- .github/coverage-manifest/Encina.AzureServiceBus.json
- src/Encina/Diagnostics/EventIdRanges.cs
```

</details>

### Phase 4: Documentation and finalization

<details>
<summary><strong>Tasks</strong></summary>

1. XML documentation with an example on `ServiceBusSenderCache` and a note on the changed `AzureServiceBusMessagePublisher` constructor.
2. Changelog fragment (`changed`): the publisher keeps one sender per entity; the constructor takes the cache; the fragment names the five transports investigated without change and why.
3. No documentation page changes: `docs/INVENTORY.md` has nothing to record for a new type and no README exists for the package (creating one is outside this issue).
4. Verification: `dotnet build --configuration Release` with zero warnings; `dotnet test` of the unit, guard and contract projects; the integration test with Docker (the emulator); per-flag coverage and the CRAP table of changed methods.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 4</strong></summary>

```
CONTEXT:
Phases 1 to 3 are done. Finalization remains.

TASK:
Confirm XML docs, the changelog fragment and a clean Release build; run the AzureServiceBus tests in all four unit-level projects.

KEY RULES:
- Fragment section is changed; never edit CHANGELOG.md.
- Documentation never types coverage figures.

REFERENCE FILES:
- changelog.d/README.md
```

</details>

---

## Research

### Relevant Standards & Specifications

| Standard | Relevance |
|----------|-----------|
| AGENTS.md section 5 (transports) | A feature touching the category is consistent across providers; here the outcome is, the mechanism applies to the one transport that has per-entity senders |
| AGENTS.md section 3 | Registration completeness (DI test with `ValidateOnBuild`, `ValidateScopes`); no scoped service in a singleton; errors never swallowed |
| #1608 | The publisher must not dispose the singleton client |
| ADR-054 D4 | Tenant tag on telemetry; not applicable, no telemetry added |
| SPEC-000 DEC-009 | Cloud-tied transports are tested with local emulators (the Service Bus emulator fixture) |

### Existing Encina Infrastructure to Leverage

| Component | Location | Usage in This Feature |
|-----------|----------|----------------------|
| `AzureServiceBusMessagePublisher` | `src/Encina.AzureServiceBus` | The four `CreateSender` sites |
| `ServiceBusClient` registration (singleton) | `src/Encina.AzureServiceBus/ServiceCollectionExtensions.cs` | The owner of the senders |
| `AzureServiceBusPublisherLifetimeTests` | `tests/Encina.UnitTests/AzureServiceBus/Publishing` | The lifetime regression the cache must keep |
| `ServiceBusEmulatorFixture` collection | `tests/Encina.IntegrationTests/MessageBrokers/AzureServiceBus` | Integration tests |

Investigated transports (no change in this plan):

| Transport | Per-publish cost found | Verdict |
|-----------|------------------------|---------|
| RabbitMQ | `routingKey ?? typeof(TMessage).Name`; no declarations; singleton channel | No cache has anything to hold |
| Kafka | `topic ?? DefaultEventTopic`; `MessageType` header bytes per message (2 sites); metadata inside librdkafka | Optional micro-cache (Phase 2) |
| NATS | `$"{SubjectPrefix}.{Name}"` per publish when no subject is given; singleton connection | Optional micro-cache (Phase 2) |
| MQTT | `$"{TopicPrefix}/{Name}"` per publish when no topic is given; singleton client | Optional micro-cache (Phase 2) |
| AmazonSQS | Queue URL and topic ARN from options; per-message attribute dictionaries with unique values | Nothing cacheable per type |
| AzureServiceBus | One `CreateSender` and disposal per call (4 sites) | Phase 1 |
| Redis.PubSub, InMemory, gRPC, GraphQL | Not listed in the issue; no per-type topic mapping found in the Redis.PubSub publisher (channel is a caller argument) | Not examined further |

### Event ID Allocation

| Package | Range | Notes |
|---------|-------|-------|
| `Encina.AzureServiceBus` | 4300-4349 (existing) | 4300-4311 used; this plan takes 4312 |

No new range is registered.

### Estimated File Count

| Category | New Files | Modified Files | Notes |
|----------|-----------|----------------|-------|
| Production code | 1 | 3 | New: `ServiceBusSenderCache`; modified: publisher, DI extension, `Log.cs` |
| Tests | 2 | 6 | New: cache unit tests, the benchmark justification; modified: unit publisher tests, message id tests, lifetime tests, guard tests, contract tests, integration tests |
| Documentation | 0 | 0 | None (`INVENTORY.md` has nothing to record for a new type) |
| Configuration | 1 | 1 | One changelog fragment; one manifest |
| **Total (PR 1)** | **4** | **10** | 4 + 10 = 14 files, the 14 rows of the Phase 1 table; about 600 lines |

PR 2 (optional, Phase 2): 2 new files (the benchmark and a changelog fragment) and 9 modified files (3 publishers, 3 unit test files, 3 manifests) = 11 files, about 300 lines. Neither pull request exceeds 15 files or 1500 lines.

---

## Combined AI Agent Prompts

<details>
<summary><strong>Complete Implementation Prompt - All Phases</strong></summary>

```
PROJECT CONTEXT:
Encina is a .NET 10 / C# 14 library, pre-1.0, no backward compatibility. Issue #709 asked for topology caches in six transports;
verification shows the only real per-publish cost is the Azure Service Bus sender created and disposed on every call.

IMPLEMENTATION OVERVIEW:
1. ServiceBusSenderCache (public sealed, IAsyncDisposable, singleton): entity-name key (OrdinalIgnoreCase), at most 256 entries,
   closed senders replaced, DisposeAsync logs close failures (EventId 4312).
2. AzureServiceBusMessagePublisher takes the cache and no longer disposes senders (4 sites, 15 test construction sites in 4 files).
3. Optional pull request: Kafka MessageType header bytes and MQTT/NATS default destinations per (prefix, type), only if the
   maintainer answers B and a benchmark shows a gain.
Phase 1 is the pull request; Phase 2 is conditional.

KEY PATTERNS:
- The container owns the cache and the client; the scoped publisher owns nothing (#1608).
- Cache only entity names and per-type constants; never tenant, message or per-message values.
- Apply Design Choices 1, 3, 4 and 5 as the maintainer answers them (the answers are recorded in the plan when given).

REFERENCE FILES:
- src/Encina.AzureServiceBus/AzureServiceBusMessagePublisher.cs
- src/Encina.AzureServiceBus/ServiceCollectionExtensions.cs
- tests/Encina.UnitTests/AzureServiceBus/Publishing/AzureServiceBusPublisherLifetimeTests.cs
- .github/coverage-manifest/Encina.AzureServiceBus.json
```

</details>

---

## Cross-Cutting Integration Matrix

| # | Function | Status | Notes |
|---|----------|--------|-------|
| 1 | Caching | ✅ Include | An in-process cache of Service Bus senders per entity (and, optionally, per-type constants); not `ICacheProvider`. |
| 2 | OpenTelemetry | ❌ N/A | No telemetry is added; the cache holds entity names only, so no tenant tag applies (ADR-054 D4). |
| 3 | Structured Logging | ✅ Include | EventId 4312 inside the existing Azure Service Bus range, exception type only. |
| 4 | Health Checks | ❌ N/A | The existing Azure Service Bus health check covers the broker connection. |
| 5 | Validation | ❌ N/A | No new input beyond the entity name, validated as non-blank by `GetSender`. |
| 6 | Resilience | ✅ Include | A closed sender is replaced on the next publish; the SDK recovers transient link faults itself. |
| 7 | Distributed Locks | ❌ N/A | No cross-process state. |
| 8 | Transactions | ❌ N/A | No mutation of stored state. |
| 9 | Idempotency | ✅ Include | Concurrent first use may create two senders for one name; the loser is disposed, so the outcome is the same. |
| 10 | Multi-Tenancy | ✅ Include | The key is the entity name; two tenants publishing to different entities use different entries and nothing tenant-specific is stored. |
| 11 | Module Isolation | ❌ N/A | Not module scoped. |
| 12 | Audit Trail | ❌ N/A | Publishing is not an audit event. |

**Deferred items:** none.

---

## Prerequisites & Dependencies

| Prerequisite | Status | Notes |
|--------------|--------|-------|
| `ServiceBusEmulatorFixture` collection | ✅ Available | `tests/Encina.IntegrationTests/MessageBrokers/AzureServiceBus` |
| #1608 lifetime regression tests | ✅ Available | `AzureServiceBusPublisherLifetimeTests` |
| Maintainer answers to Design Choices 1, 3, 4 and 5 | Pending | Asked when work starts |

No blocking prerequisites.

---

## Next Steps

1. The orchestrator asks the maintainer the Dilemmas (Design Choices 1, 3, 4 and 5) when work starts, records the answers under `## Maintainer Decisions`, then briefs one worker for Phase 1 (and Phase 2 only if the answer is B).
2. Option A of Design Choice 1 leaves the acceptance criteria of #709 (topic cache in all transports, exchange declaration cache, routing key cache) unmet by design, because the code they describe does not exist. When Design Choice 1 is answered, the maintainer amends #709 to the verified scope (draft of the replacement criteria: `artifacts/decisions/plan-batch/followups/709-acceptance-criteria-amendment.md`; the plan never edits or comments on the issue); until then the pull request says `Refs #709`, not `Fixes #709`.
3. After the amendment and the merge, the maintainer closes #709 citing the investigation table of this plan, so the five transports are closed with evidence.
