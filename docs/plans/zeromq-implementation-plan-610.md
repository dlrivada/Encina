# Implementation Plan: Encina.ZeroMQ, a brokerless messaging transport on NetMQ

> **Issue**: [#610](https://github.com/dlrivada/Encina/issues/610)
> **Type**: Feature
> **Verified against**: origin/main `1a661f5c1` (2026-11-24)
> **Complexity**: Medium (7 phases, 7 pull requests of about 14, 15, 9, 8, 10, 7 and 7 files)
> **Estimated Scope**: about 1,000 lines of production code and about 1,800 lines of tests
> **Family**: transport-new-2 (#242 Dapr, #610 ZeroMQ, #611 Azure Event Hubs, #612 Amazon Kinesis, #310 Event Mesh). Sibling family transport-new-1 (#237, #238, #239, #240, #241) owns the shared transport conformance base (#240); "Shared pieces" below says which plan owns what.

---

## Summary

The issue asks for a ZeroMQ transport for low-latency, brokerless messaging: publish-subscribe, request-reply and push-pull (its table also lists router-dealer), configuration of endpoints and socket options, `ServiceCollection` extensions, tests and documentation, on the package `NetMQ`. State of `origin/main`:

- There is no `Encina.ZeroMQ` project and `NetMQ` is not in `Directory.Packages.props`. There is no `IMessageTransport` in `src/` (grep), so the sketch in the issue (`ZeroMQTransport : IMessageTransport`, synchronous `SendFrame`) does not match the code: each transport has its own `I<Name>MessagePublisher` with a `messageId` parameter, validated options and an `AddEncina<Name>` method (`src/Encina.RabbitMQ/IRabbitMQMessagePublisher.cs` has `PublishAsync`, `SendToQueueAsync` and `RequestAsync`, so several patterns in one interface is the precedent). Only MQTT and Redis.PubSub subscribe; #1609 tracks the gap.
- Verified on 2026-11-23 by downloading the package from NuGet: **NetMQ 4.0.4.5**, the current stable, net10.0, the licence is **LGPL-3.0** (the nuspec carries only the licence URL `https://github.com/zeromq/netmq/blob/master/COPYING.LESSER`); its assembly has `PublisherSocket`, `SubscriberSocket`, `PushSocket`, `PullSocket`, `RequestSocket`, `ResponseSocket`, `DealerSocket`, `RouterSocket`, `NetMQPoller`, `NetMQQueue<T>`, `TryReceive(msg, timeout)` and the send and receive high-water-mark options; it depends on `AsyncIO`, `NaCl.Net` (CURVE), `System.Collections.Immutable`, `System.Security.Cryptography.Xml` and `System.ServiceModel.Primitives`. The other .NET binding, `ZeroMQ` 4.1.0.31, wraps the native libzmq (also LGPL) and targets old frameworks. Encina is MIT and no document in the repository states a policy on copyleft dependencies (search of `docs` and `AGENTS.md` for LGPL and copyleft finds none), which is Design Choice 1.
- Scope: SPEC-000 REQ-030 puts "the new transports" in 1.0 and DEC-009 excludes only AI/LLM and cloud services without a free emulator; ZeroMQ needs neither, and the issue sits in the milestone v0.19.0. `AGENTS.md` section 5 does not list ZeroMQ among the planned transports; the issue file of this family records that gap (Phase 7). The issue is planned, not skipped.
- A brokerless transport has no durable queue: a PUB socket drops messages that have no connected subscriber, and a socket at its high-water mark drops or blocks. Delivery is at-most-once to the peer; "accepted by the local socket" is all a `Right` means (Design Choice 3).

Delivered: package `Encina.ZeroMQ` with `IZeroMQMessagePublisher` (publish, push, request, subscribe, pull and respond, Design Choice 4), `EncinaZeroMQOptions` (validated, CURVE keys `[JsonIgnore]`), `AddEncinaZeroMQ`, a socket host that owns every socket on one thread (Design Choice 6), a health check, an `ActivitySource` and `Meter`, log messages in the new range `ZeroMQ` 40570-40589, unit, guard, property, contract and integration tests with real loopback sockets, and documentation.

**Provider category**: Messaging transports (AGENTS.md section 5). Brokerless means no broker connectivity check and no broker-side dead letter queue; the transport rule (send/publish, subscription, error handling and DLQ, metadata) is met as Design Choice 3 states.

**Shared pieces (family)**: the shared conformance base is owned by #240 (this plan subclasses it in pull request 6 for the scenarios a brokerless transport can honor). The EventId block `40550-40649` of the Transports zone is shared by the five plans (Event ID Allocation); #242 adds the block's rows to the registry if it lands first. The follow-up issue for tenant and module headers, adoption of #251, #252 and #2385, and the resilience decorators of the new transports is `artifacts/issues/transport-new-2-tenant-module-and-resilience-adoption.md`, owned by the #242 plan of this family.

---

## Design Choices

<details>
<summary><strong>1. The LGPL-3.0 dependency (Dilemma D1)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Ship `Encina.ZeroMQ` with `NetMQ` as an ordinary NuGet dependency of an opt-in satellite package: not bundled, not merged into the Encina assemblies; the README and the package description state the LGPL-3.0 licence of NetMQ** | Delivers the issue; the copyleft stays in an opt-in package and the application links it dynamically, as LGPL allows | Every application that ships the package carries LGPL obligations (notice, replaceable library); the repository has no stated copyleft policy |
| **B) Do not ship a ZeroMQ transport; close #610 or move it out of 1.0** | No copyleft in the package graph | Drops a transport the maintainer put in v0.19.0; no alternative library exists |
| **C) Ship it in a separate repository or package outside the Encina release** | Keeps the main graph permissive | A transport that is not part of the library, with its own release train |

### Chosen Option: **A, an opt-in satellite package with a stated licence** (recommended, pending the maintainer)

### Rationale

A wins when the maintainer accepts LGPL for an opt-in dependency that is not bundled (the common reading for an MIT library that references an LGPL package); B or C win if the project wants no copyleft anywhere in its published packages, for example for adopters with a licence audit. This is a legal reading the plan cannot make; the recommendation is A because the dependency is opt-in and unmodified.

**Class:** Dilemma

</details>

<details>
<summary><strong>2. Package placement</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) New package `Encina.ZeroMQ`** | Pay-for-what-you-use; its own options, health check and EventId range; the name the issue gives | One more package |
| **B) Inside an existing transport package** | No new package | Mixes brokerless sockets into a broker transport; no existing package fits |

### Chosen Option: **A, a new package** (recommended, pending the maintainer)

### Rationale

This choice exists only under D1 option A; under D1 options B (no ZeroMQ transport) and C (outside the repository) no in-repo package exists. Under D1 A, the issue names `Encina.ZeroMQ`, SPEC-000 REQ-030 puts the new transports in 1.0 and every other transport is its own package, so A wins; B wins only if the maintainer wants brokerless sockets inside an existing transport package.

**Class:** Dilemma

</details>

<details>
<summary><strong>3. Delivery semantics and dead lettering (Dilemma D3)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) At-most-once, documented: `Right` means the local socket accepted the frames within `SendTimeout`; a timeout or a full queue is a `Left` the caller (the outbox) retries; a message dropped by a PUB socket without subscribers is not detectable; an unhandled or undeserializable message on the receiving side is logged and dropped; no dead letter queue** | Honest about what ZeroMQ guarantees; nothing invented | The category rule "DLQ" is met only as "not applicable to a brokerless transport" |
| **B) A local dead-letter sink: an optional `IZeroMQDeadLetterSink` the application registers, called with the frames of a message the receiver could not handle** | The application can persist poison messages | An API for a case most users do not have; the sender side still has no guarantee |

### Chosen Option: **A, at-most-once with no dead letter queue** (recommended, pending the maintainer)

### Rationale

A wins for a latency-first transport whose users chose brokerless on purpose; B wins when a deployment uses ZeroMQ for commands and must keep poison messages. Under either option the caller's cancellation propagates and is never a `Left`, and a failure is never reported as success (AGENTS.md section 3).

**Class:** Dilemma

</details>

<details>
<summary><strong>4. Public interface: one interface for every pattern (Dilemma D4)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) One `IZeroMQMessagePublisher` with `PublishAsync`, `PushAsync`, `RequestAsync` (senders) and `SubscribeAsync`, `PullAsync`, `RespondAsync` (receivers, each returning `ValueTask<IAsyncDisposable>`), the shape of `IRabbitMQMessagePublisher` and of the subscribing `IMQTTMessagePublisher`** | Coherent with the siblings (#240 asks the same question and recommends one interface) and with RabbitMQ's several patterns in one interface | Six members; the name says "publisher" for consuming members |
| **B) One interface per pattern (`IZeroMQPubSub`, `IZeroMQPushPull`, `IZeroMQRequestReply`)** | Small interfaces; an application that only pushes registers nothing else | Different from every other transport; three registrations |

### Chosen Option: **A, one interface** (recommended, pending the maintainer)

### Rationale

A wins when coherence across transports is the rule (#1609 asks for one subscribe shape); B wins when an application uses one pattern and wants the narrowest dependency. The same question is open in #237, #239, #240, #241 and in this family, and the answer must be the same.

**Class:** Dilemma

</details>

<details>
<summary><strong>5. The handler contract (Dilemma D5)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `Func<TMessage, ValueTask>` for subscribe and pull as in MQTT and Redis.PubSub; `Func<TRequest, ValueTask<TResponse>>` for respond; an exception is logged by exception type and the message is dropped (Design Choice 3), a respond handler that throws still gets a reply: a REP socket must send after every receive or its next receive throws, so the host sends an error reply (header frame with `error` set, no payload) and the requester gets `encina.zeromq.request_failed` at once instead of a timeout; the responder never wedges** | Same call shape as the two existing subscribers | Business failure travels as an exception, against Railway Oriented Programming |
| **B) `Func<TMessage, CancellationToken, ValueTask<Either<EncinaError, Unit>>>` (and `Either<EncinaError, TResponse>` for respond, the `Left` travelling back to the requester as an error frame)** | A `Left` from `IEncina.Send` maps straight; the requester sees a categorized failure instead of a timeout | A new shape; an error frame format to define |

### Chosen Option: **A, the existing shape** (recommended, pending the maintainer)

### Rationale

A wins for coherence; B wins when handlers wrap `IEncina.Send` and requesters need the error back. #240 and the siblings ask the same question.

**Class:** Dilemma

</details>

<details>
<summary><strong>6. Threading model for sockets (Dilemma D6)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) A socket host: one dedicated thread owns every socket (a `NetMQPoller`), callers enqueue operations on a `NetMQQueue<T>` and await a `TaskCompletionSource`; a caller's cancellation removes the pending operation; callbacks on the poller thread catch every exception** | ZeroMQ sockets are not thread-safe, and this keeps them on one thread without locks; async API without blocking pool threads | A hand-written host; one thread per host |
| **B) A lock around each socket and synchronous send and receive on the caller's thread** | Simple | Blocks thread-pool threads on `TrySendFrame` timeouts, the starvation the database rule exists to avoid; one slow peer stalls every caller |

### Chosen Option: **A, a single-owner socket host** (recommended, pending the maintainer)

### Rationale

A wins for throughput and for never blocking the pool; B wins for a very small footprint and a single publisher. Under A the poller callbacks and the receive loops follow AGENTS.md section 3: they catch per iteration, never let an exception escape (an exception on the poller thread would end the host or the process), and the stopping token ends them.

**Class:** Dilemma

</details>

<details>
<summary><strong>7. Transport security (Dilemma D7) and key handling</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Fail closed: a `tcp` endpoint that is not loopback requires CURVE keys, or startup fails; the explicit opt-out `AllowInsecureTransport` is logged as a warning (EventId 40570) when the host starts** | An unauthenticated port is never opened by default; follows the fail-closed rule for gates | Extra setup for a trusted network; key distribution is the user's |
| **B) Plain sockets by default, CURVE opt-in, a warning in the README** | Zero configuration | Any process that can reach the port can publish or subscribe |

### Chosen Option: **A, CURVE required off loopback** (recommended, pending the maintainer)

### Rationale

A wins for anything that leaves the host; B wins for a trusted private network or same-machine use, which loopback and `ipc` already cover under A. Whichever option is chosen, the CURVE secret key is a secret: the property is `[JsonIgnore]`, `ToString()` is overridden so logging and serialization cannot print it (AGENTS.md section 3), and it is never logged.

**Class:** Dilemma

</details>

<details>
<summary><strong>8. Request-reply and the router-dealer pattern (Dilemma D8)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) REQ/REP only, as the acceptance criterion says: one request in flight per socket, a per-request timeout (`RequestTimeout`), the REQ socket recreated after a timeout (a lost reply otherwise leaves it unusable); the router-dealer row of the issue's table becomes a follow-up issue** | Matches the criterion; the failure model of REQ/REP is well known | Concurrent requests queue behind each other on one socket (the host serializes them) |
| **B) DEALER/ROUTER with a correlation id per request, concurrent requests on one socket** | Concurrency and no socket recreation | A correlation protocol of our own; the issue's criterion only names request-reply |

### Chosen Option: **A, REQ/REP now** (recommended, pending the maintainer)

### Rationale

A wins for a first release; B wins when many concurrent requests share one peer. The router-dealer pattern appears in the issue's table, not in its acceptance criteria; the request timeout is a `Left` of category `Timeout`, the caller's cancellation propagates.

**Class:** Dilemma

</details>

<details>
<summary><strong>9. Envelope and metadata propagation</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) A multipart message: frame 0 the topic (the PUB/SUB prefix filter), frame 1 a small header (`message-id`, `type`, `traceparent`, caller headers as JSON), frame 2 the UTF-8 JSON payload; a new message id only when the caller gives none** | The filter works on a dedicated frame; headers are readable without parsing the payload; the same message-id rule as the other publishers | A frame layout to maintain |
| **B) One JSON envelope in a single frame** | Simple | The SUB prefix filter cannot see the topic; double parsing |

### Chosen Option: **A, three frames** (recommended, pending the maintainer)

### Rationale

A wins when the topic filter of SUB sockets must work on a dedicated frame; B wins for the smallest frame count when only request-reply is used. The layout is the baseline under the recommended options: an error reply of D5 A is a reply whose header frame carries an `error` entry and no payload frame, and D8 B (DEALER/ROUTER) would add a correlation frame, so the layout is a Dilemma that follows those two. Under every option the message id follows the existing rule: every publisher takes `messageId` and generates one only when null or empty (`src/Encina.NATS/INATSMessagePublisher.cs`, `src/Encina.AmazonSQS/IAmazonSQSMessagePublisher.cs`) so the outbox row id reaches consumers. The subscriber checks the topic frame for an exact match after the prefix filter, so a subscription to `orders` does not receive `orders.eu`.

**Class:** Dilemma

</details>

<details>
<summary><strong>10. Integration test infrastructure</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Real NetMQ sockets on the loopback address with ports bound at random (`BindRandomPort`) or `inproc` endpoints, in process, no container** | Executes the real package and the real library; ZeroMQ has no server to run | No shared collection fixture is needed; tests choose their own ports |
| **B) Mocked sockets** | Fast | NetMQ sockets are concrete classes; a mock tests nothing about delivery, filters or high-water marks |

### Chosen Option: **A, real sockets in process**

### Rationale

AGENTS.md section 9 requires tests that execute real package code against real externals; the external of a brokerless transport is the socket library itself, which runs in process, so no Testcontainers module or emulator applies. The issue's acceptance criteria list integration tests.

**Class:** Settled
**Settled by:** AGENTS.md section 9 (tests execute real package code) and #610 acceptance criteria

</details>

<details>
<summary><strong>11. OpenTelemetry: per-package source or the shared transport design of #725 (Dilemma D11)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `ActivitySource` and `Meter` named `Encina.ZeroMQ` in the package, registered by name in `Encina.OpenTelemetry`; the W3C `traceparent` travels in the header frame** | Meets the issue's cross-cutting row now; the same pattern as `Encina.Messaging.Outbox` | The name list in `Encina.OpenTelemetry` is edited by every transport plan |
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
| **A) Defer to an issue: tenant-scoped topic names and tenant and module header entries need the persisted request context (#1164) and the route metadata (#2385)** | No invented scheme; one design for all transports | Not in this plan |
| **B) Stamp a tenant segment into the topic name now** | Early isolation | Needs the reserved `default` tenant segment and an injective encoding that no transport has defined |

### Chosen Option: **A, defer to a follow-up issue** (recommended, pending the maintainer)

### Rationale

A wins because one design for all transports needs #1164 and #2385; B wins for a tenant-isolated deployment that needs scoped topics before those land. The issue file is `artifacts/issues/transport-new-2-tenant-module-and-resilience-adoption.md`.

**Class:** Dilemma

</details>

---

## Implementation Phases

### Phase 1: Package, options, envelope and DI (pull request 1)

<details>
<summary>Tasks</summary>

1. `src/Encina.ZeroMQ/Encina.ZeroMQ.csproj` (net10.0; packages `NetMQ` (new pin 4.0.4.5 in `Directory.Packages.props`), `Microsoft.Extensions.*.Abstractions`, `LanguageExt.Core`; project references to `Encina` and `Encina.Messaging`; `InternalsVisibleTo Encina.UnitTests`) and the `Encina.slnx` entry. Transport packages carry no `PublicAPI.*.txt` today (none in NATS, AmazonSQS, Redis.PubSub), so none is added.
2. `EncinaZeroMQOptions` (namespace `Encina.ZeroMQ`, public sealed): `Endpoints` (`IList<ZeroMQEndpointOptions>`: `Name`, `Pattern` (an Encina enum `Publish`, `Subscribe`, `Push`, `Pull`, `Request`, `Reply`), `Address`, `Bind`), `SendHighWaterMark`, `ReceiveHighWaterMark`, `SendTimeout`, `RequestTimeout`, `ReconnectInterval`, `Linger`, `CurveServerPublicKey`, `CurveSecretKey` (`[JsonIgnore]`; `ToString()` prints the endpoint names only), `CurvePublicKey`, `AllowInsecureTransport`, `ProviderHealthCheck`. Internal `EncinaZeroMQOptionsValidator : IValidateOptions<>`: addresses are `tcp://`, `ipc://` or `inproc://`; a `tcp` address is loopback only when its host is a literal address in `127.0.0.0/8` or `::1` (no name resolution happens at validation, so `localhost` and every other host name count as non-loopback), and a wildcard bind (`tcp://*:5555`, `0.0.0.0`, `::`) counts as non-loopback; `ipc://` and `inproc://` are exempt; a non-loopback `tcp` address requires the CURVE keys unless `AllowInsecureTransport` (Design Choice 7), with unit tests for each classification; positive timeouts and high-water marks; unique endpoint names.
3. Internal `ZeroMQEnvelope` (Design Choice 9): the three-frame codec, with the message id rule and the topic frame.
4. `ServiceCollectionExtensions.AddEncinaZeroMQ(this IServiceCollection, Action<EncinaZeroMQOptions>? configure = null)`: options with `ValidateOnStart`, the validator through `TryAddEnumerable`; a DI test that builds the provider with `ValidateOnBuild` and `ValidateScopes`.
5. Tests: `EncinaZeroMQOptionsTests`, `ZeroMQEnvelopeTests`, `ServiceCollectionExtensionsTests` (unit), `ZeroMQGuardTests` (guard: null `services`, null `configure` accepted); manifest `.github/coverage-manifest/Encina.ZeroMQ.json` with the unit and guard targets of these files and their one-sentence justifications at file and package level (`coverage-report.cs --check-justifications` fails a target without one; every later pull request adds the targets and justifications of the files and flags it introduces); `UnitTests.csproj` and `GuardTests.csproj` ProjectReference edits.

</details>

<details>
<summary>Prompt for AI Agents — Phase 1</summary>

```text
CONTEXT: Encina.AmazonSQS and Encina.Redis.PubSub are the templates (options, validator, AddEncina*). NetMQ 4.0.4.5 is the only .NET ZeroMQ library for net10.0 and is LGPL-3.0.
TASK: Create the Encina.ZeroMQ package skeleton with validated options, the three-frame envelope codec and AddEncinaZeroMQ, with unit and guard tests.
KEY RULES: CurveSecretKey is [JsonIgnore] with a ToString() override that never prints it; validation fails at startup with OptionsValidationException; a non-loopback tcp address without CURVE keys fails unless AllowInsecureTransport; TryAdd registrations only; a DI test with ValidateOnBuild and ValidateScopes; no Obsolete, no compatibility code.
REFERENCE FILES: src/Encina.AmazonSQS/*.cs, src/Encina.Redis.PubSub/*.cs, src/Encina.AmazonSQS/EncinaAmazonSQSOptionsValidator.cs, .github/coverage-manifest/Encina.AmazonSQS.json.
```

</details>

### Phase 2: Socket host and publish-subscribe publishing (pull request 2)

<details>
<summary>Tasks</summary>

1. Internal `ZeroMQSocketHost` (Design Choice 6): owns a `NetMQPoller` thread, a `NetMQQueue<T>` of operations, creates and disposes every socket, applies the options (high-water marks, linger, reconnect interval, CURVE), completes each operation through a `TaskCompletionSource`; callbacks catch every exception; disposal stops the poller and fails pending operations with a `Left`. The host is a singleton registered by `AddEncinaZeroMQ` with `IHostedService` start/stop, started lazily on the first operation so an application that registers the package but sends nothing opens no socket.
2. `IZeroMQMessagePublisher.PublishAsync<TMessage>(TMessage message, string? topic = null, string? messageId = null, IReadOnlyDictionary<string,string>? headers = null, CancellationToken cancellationToken = default) where TMessage : class` returning `ValueTask<Either<EncinaError, string>>` (the message id). `ZeroMQMessagePublisher` (scoped; the host, `IOptions`, `ILogger`).
3. Errors (ADR-062), in `ZeroMQErrorCodes.cs`, each created with the named argument `category:` and a constant message: `encina.zeromq.publish_failed` `Internal` (R4), `encina.zeromq.send_timeout` `Timeout` (R7), `encina.zeromq.endpoint_not_configured` `Internal` (R1: a configuration entry). Cancellation propagates as `OperationCanceledException`, never a `Left` (R5). The exception goes to `ForLogging()`, never its message.
4. `Log.cs` 40570-40573 (insecure transport enabled, publishing, published, publish failed; no message ids, no payload, no endpoint credentials). `EventIdRanges.ZeroMQ = (40570, 40589)`; the line `static readonly Encina.Diagnostics.EventIdRanges.ZeroMQ -> (int Min, int Max)` in `src/Encina/PublicAPI.Unshipped.txt` (through `mechanical-fixer`); the `AssemblyRanges` entry in `EncinaEventIdAllocationTests`; `docs/engineering/eventid-block-registry.md` (through `docs-writer`): the row `| ZeroMQ | 40570-40589 | Transports | zone |` in the registered ranges table and the Blocks row `| 40570-40589 | Transports | transport-new-2: #610 | registered |` (exact bounds, as `EventIdRegistryAgreementTests` rules 2 and 3 require; the other four plans of the family add their own disjoint rows, so a conflict is resolved by keeping both, never by a rebase of a pushed branch).
5. DI: `AddEncinaZeroMQ` gains the host singleton, its hosted service (`TryAddEnumerable(ServiceDescriptor.Singleton<IHostedService, ...>)` resolving the same singleton host instance, not a second one) and `TryAddScoped<IZeroMQMessagePublisher, ZeroMQMessagePublisher>()` in this pull request (registration completeness, AGENTS.md section 3); the DI test resolves both and asserts one host instance with `ValidateOnBuild` and `ValidateScopes`.
6. Tests: `ZeroMQSocketHostTests` and `ZeroMQMessagePublisherTests` (unit, over in-process `inproc` sockets: the host is concrete, so its tests use real sockets), guard edit, manifest edit with the unit and guard targets (contract, property and integration targets are entered in pull request 6, where those tests land).

</details>

<details>
<summary>Prompt for AI Agents — Phase 2</summary>

```text
CONTEXT: The package skeleton and the envelope exist. NetMQ sockets are not thread-safe; one host thread owns them all.
TASK: Implement the socket host, IZeroMQMessagePublisher.PublishAsync, the error codes, the log messages 40570-40573, the EventId range ZeroMQ, the host and publisher DI registrations, with unit and guard tests.
KEY RULES: no exception escapes the poller thread or any loop; EncinaErrors.Create with the named category: argument; constant messages; ids and payloads never logged; cancellation propagates and removes the pending operation; EventIds only 40570-40589, registered first in EventIdRanges.cs and in the registry (Zone zone); no tenant tag.
REFERENCE FILES: src/Encina.RabbitMQ/RabbitMQMessagePublisher.cs, src/Encina/Diagnostics/EventIdRanges.cs, tests/Encina.UnitTests/Testing/Architecture/EncinaEventIdAllocationTests.cs, docs/engineering/eventid-block-registry.md.
```

</details>

### Phase 3: Subscribe, push and pull (pull request 3)

<details>
<summary>Tasks</summary>

1. `SubscribeAsync<TMessage>(Func<TMessage, ValueTask> handler, string? topic = null, CancellationToken cancellationToken = default)` returning `ValueTask<IAsyncDisposable>` (a SUB socket with the prefix filter, the exact topic check of Design Choice 9, one receive loop on the host); `PushAsync<TMessage>(...)` (PUSH, blocks at the high-water mark up to `SendTimeout`, then `encina.zeromq.send_timeout`); `PullAsync<TMessage>(Func<TMessage, ValueTask> handler, string? endpointName = null, CancellationToken cancellationToken = default)` (PULL).
2. A handler that throws is logged by exception type and the message is dropped (Design Choice 3); an undeserializable message is dropped with `encina.zeromq.deserialize_failed` `Internal` (R4, logged, never returned to a caller); a receive loop catches per iteration and the stopping token ends it.
3. Codes added to `ZeroMQErrorCodes.cs`: `encina.zeromq.subscribe_failed` `Internal` (R4), `encina.zeromq.deserialize_failed` `Internal` (R4). `Log.cs` 40574-40580 (subscription started, handler failed, message dropped, subscription ended, receive iteration failed, pushing, pull started).
4. Tests: `ZeroMQSubscribeTests`, `ZeroMQPushPullTests` (unit over in-process sockets), guard edit, manifest edit; DI test unchanged (the publisher is already registered).

</details>

<details>
<summary>Prompt for AI Agents — Phase 3</summary>

```text
CONTEXT: Publishing and the socket host exist. SUB sockets filter by prefix of the first frame; PUSH/PULL load-balance across pullers and block at the high-water mark.
TASK: Implement SubscribeAsync, PushAsync and PullAsync on the host with the exact-topic check, the drop-and-log failure rule and the receive loops, with unit tests.
KEY RULES: a receive loop catches per iteration and never lets an exception escape; a handler failure is never reported as success to the sender (there is no ack: at-most-once, documented); cancellation ends the loop and disposes the socket; payloads and ids never logged.
REFERENCE FILES: src/Encina.MQTT/MQTTMessagePublisher.cs, src/Encina.Redis.PubSub/RedisPubSubMessagePublisher.cs, AGENTS.md section 3.
```

</details>

### Phase 4: Request-reply (pull request 4)

<details>
<summary>Tasks</summary>

1. `RequestAsync<TRequest, TResponse>(TRequest request, string? endpointName = null, string? messageId = null, CancellationToken cancellationToken = default)` returning `ValueTask<Either<EncinaError, TResponse>>` (REQ, one request in flight per socket, `RequestTimeout`; after a timeout the host disposes and recreates the REQ socket, Design Choice 8); `RespondAsync<TRequest, TResponse>(Func<TRequest, ValueTask<TResponse>> handler, string? endpointName = null, CancellationToken cancellationToken = default)` (REP loop; the host always sends a reply after a receive: the handler's response, or an error reply when the handler throws or the request cannot be deserialized, so the REP socket never leaves the send state; the requester maps an error reply to `encina.zeromq.request_failed`).
2. Codes added: `encina.zeromq.request_timeout` `Timeout` (R7), `encina.zeromq.request_failed` `Internal` (R4). `Log.cs` 40581-40584 (requesting, request timed out, socket recreated, responding).
3. Tests: `ZeroMQRequestReplyTests` (unit over in-process sockets: reply, timeout then recreation, concurrent requests serialized), guard edit, manifest edit.

</details>

<details>
<summary>Prompt for AI Agents — Phase 4</summary>

```text
CONTEXT: The host, publish, subscribe and push-pull exist. A REQ socket that loses a reply is unusable until it is recreated.
TASK: Implement RequestAsync and RespondAsync with the per-request timeout and the socket recreation, with unit tests.
KEY RULES: a timeout is a Left of category Timeout; the caller's cancellation propagates; no exception escapes the REP loop; never block a pool thread on a socket call.
REFERENCE FILES: src/Encina.RabbitMQ/RabbitMQMessagePublisher.cs (RequestAsync), AGENTS.md section 3.
```

</details>

### Phase 5: Health check and observability (pull request 5)

<details>
<summary>Tasks</summary>

1. `Health/ZeroMQHealthCheck : EncinaHealthCheck` (`DefaultName = "encina-zeromq"`, tags `encina`, `messaging`, `zeromq`, `ready`): the host starts lazily on the first operation (pull request 2), so before first use the check reports `Healthy` with the text "not started" and never starts the host itself; after the first use it is healthy when the host thread is running and every configured `Bind` endpoint is bound, and unhealthy after a failed bind or a stopped host (a brokerless transport has no server to ping); the result text names the check and the exception type only. Registered like `AmazonSQSHealthCheck` when `ProviderHealthCheck.Enabled`.
2. `Diagnostics/ZeroMQActivitySource` and `ZeroMQMetrics`: spans publish, consume, request, respond with a link from consumer to producer through the `traceparent` header; counters `zeromq.published`, `.received`, `.dropped`, `.request_timeouts` with tags `endpoint`, `outcome`; no tenant tag. `Encina.OpenTelemetry` gains `AddSource`/`AddMeter` lines.
3. Tests: `ZeroMQHealthCheckTests`, `ZeroMQDiagnosticsTests` (unit), guard edit, manifest edit; the `Encina.OpenTelemetry` registration test is extended and counted as a file (its manifest targets and justifications stay at today's unit 90 and guard 35).

</details>

<details>
<summary>Prompt for AI Agents — Phase 5</summary>

```text
CONTEXT: All patterns exist. The issue's matrix asks for health checks, logging and telemetry.
TASK: Add the health check, the ActivitySource and Meter, and register their names in Encina.OpenTelemetry.
KEY RULES: health results and tags never contain an exception message; spans link, never baggage (ADR-054 D2); zero cost when no listener; no manifest target is lowered.
REFERENCE FILES: src/Encina.AmazonSQS/Health/AmazonSQSHealthCheck.cs, src/Encina.Messaging/Diagnostics/OutboxActivitySource.cs, src/Encina.OpenTelemetry/ServiceCollectionExtensions.cs.
```

</details>

### Phase 6: Contract, property and integration tests (pull request 6)

<details>
<summary>Tasks</summary>

1. `tests/Encina.ContractTests/MessageBrokers/ZeroMQ/ZeroMQConformanceTests.cs`, a subclass of `TransportConformanceContractBase` of #240 for the scenarios a brokerless transport can honor (publish with message id, subscribe delivers, cancellation); the redelivery and dead-letter scenarios are not applicable and the subclass opts out of them as the base allows for publish-only transports; `ContractTests.csproj` reference.
2. `tests/Encina.IntegrationTests/MessageBrokers/ZeroMQ/ZeroMQIntegrationTests.cs` (`[Trait("Category","Integration")]`, `[Trait("Transport","ZeroMQ")]`; real loopback sockets with random ports, Design Choice 10): fan-out to two subscribers, topic exactness, round robin across two pullers, request-reply with timeout and recovery, a CURVE pair, the high-water-mark behavior; `IntegrationTests.csproj` reference.
3. `tests/Encina.PropertyTests/Messaging/ZeroMQ/ZeroMQEnvelopePropertyTests.cs` (FsCheck round trip of the three frames: topic, headers, message id, payload) and `PropertyTests.csproj` reference.
4. One manifest edit entering the contract, property and integration targets of the table of Phase 7 together with their one-sentence justifications, the package-level contract and integration targets and justifications included.

</details>

<details>
<summary>Prompt for AI Agents — Phase 6</summary>

```text
CONTEXT: The conformance base of #240 is merged; ZeroMQ needs no container, so the tests run real sockets on loopback.
TASK: Write the conformance subclass, the integration tests with real sockets and the envelope property test.
KEY RULES: random ports through BindRandomPort, never fixed ports; no Thread.Sleep (bounded waits on TaskCompletionSource); the sockets are disposed by the host; Shouldly through Encina.Testing.Shouldly; contract tests execute the real publisher.
REFERENCE FILES: tests/Encina.IntegrationTests/MessageBrokers/AmazonSQS/*.cs, tests/Encina.ContractTests/MessageBrokers/AmazonSQS/*.cs, docs/en/guides/TESTING.md.
```

</details>

### Phase 7: Documentation and finalization (pull request 7)

<details>
<summary>Tasks</summary>

Per-flag targets, each written with its one-sentence justification in `.github/coverage-manifest/Encina.ZeroMQ.json` in the same pull request that adds the tests of the flag (unit and guard with the file in pull requests 1 to 5, contract, property and integration in pull request 6; this pull request adds no target); the table is the final state, measured with the commands of AGENTS.md section 9:

| File | unit | guard | contract | property | integration |
|------|:---:|:---:|:---:|:---:|:---:|
| `ZeroMQMessagePublisher.cs` | 85 | 20 | 50 | 0 | 75 |
| `ZeroMQSocketHost.cs` | 80 | 0 | 40 | 0 | 75 |
| `ZeroMQEnvelope.cs` | 95 | 0 | 0 | 80 | 0 |
| `EncinaZeroMQOptions.cs`, `...OptionsValidator.cs` | 100 | 0 | 0 | 0 | 0 |
| `ServiceCollectionExtensions.cs` | 85 | 55 | 0 | 0 | 0 |
| `Health/ZeroMQHealthCheck.cs` | 90 | 40 | 0 | 0 | 50 |
| `Diagnostics/*.cs` | 90 | 0 | 0 | 0 | 0 |
| `IZeroMQMessagePublisher.cs`, `Log.cs` | 0 | 0 | 0 | 0 | 0 |

| File and flag | Justification |
|---------------|---------------|
| Publisher unit 85, integration 75 | Unit tests drive each pattern over in-process sockets; integration tests add real tcp loopback, CURVE and high-water-mark behavior, leaving the socket-failure branches. |
| Socket host unit 80, contract 40, integration 75 | The host is concrete and thread-bound, so its tests are real sockets; the remainder is disposal races and poller failure paths that need injected faults. |
| Envelope unit 95, property 80 | Pure frame codec; the property test covers the round trip and leaves malformed-frame branches to unit tests. |
| Options and validator unit 100 | Pure data and validation. |
| Health check integration 50 | One real bound endpoint; the failure branch needs a failed bind. |
| Interface, `Log.cs` 0 | No executable lines (interface) or generated delegates; guard tests cannot reach either. |

1. `src/Encina.ZeroMQ/README.md` (with the LGPL-3.0 notice of NetMQ and the at-most-once statement), `docs/features/zeromq.md`, the ZeroMQ section of `docs/messaging/transports.md` and `docs/INVENTORY.md` through `docs-writer`; changelog fragment `changelog.d/610-zeromq.added.md` through `mechanical-fixer`.
2. Load and benchmark: justification files `tests/Encina.LoadTests/Messaging/ZeroMQ/ZeroMQ.md` and `tests/Encina.BenchmarkTests/Messaging/ZeroMQ/ZeroMQ.md` (throughput is the library's; the Redis.PubSub precedent has the same two files).
3. `dotnet build --configuration Release` with zero warnings; every flag at its target; CRAP of every new method at most 10.

</details>

<details>
<summary>Prompt for AI Agents — Phase 7</summary>

```text
CONTEXT: The package is implemented and tested; documentation and the justification files remain.
TASK: Check that every manifest target already has its justification (run coverage-report.cs --check-justifications), write the two justification files, and hand the README, feature page, transports page, INVENTORY and changelog fragment to the owning specialists.
KEY RULES: targets never go down; no hand-typed coverage figures in docs; never edit CHANGELOG.md; docs through docs-writer, fragments through mechanical-fixer; the README states the NetMQ licence.
REFERENCE FILES: tests/Encina.LoadTests/Messaging/RedisPubSub/RedisPubSub.md, .github/coverage-manifest/Encina.AmazonSQS.json, changelog.d/README.md.
```

</details>

---

## Research

### Relevant Standards & Specifications

| Standard / Spec | Relevance | Notes |
|-----------------|-----------|-------|
| `AGENTS.md` sections 3, 5, 6, 7, 9 | Secrets in options, background loops, transport category, 12 functions, EventIds, coverage | Phases 1 to 7 |
| SPEC-000 REQ-030 and DEC-009 | The new transports are in 1.0; ZeroMQ needs no cloud service | Scope |
| ADR-062, ADR-054 D2 and D4 | Error categories; trace links and tenant tags | Phases 2 and 5 |
| ZeroMQ RFC 23 (ZMTP 3.0) and the CURVE security mechanism (RFC 25) | Socket semantics, filtering, security | Design Choices 3, 7 and 8 |

### Existing Encina Infrastructure to Leverage

| Component | Location | Usage in this feature |
|-----------|----------|-----------------------|
| `Encina.AmazonSQS`, `Encina.Redis.PubSub` | `src/Encina.AmazonSQS/`, `src/Encina.Redis.PubSub/` | Templates for options, validator, DI and the subscription shape |
| `IRabbitMQMessagePublisher` | `src/Encina.RabbitMQ/IRabbitMQMessagePublisher.cs` | Precedent for several patterns in one interface |
| `EncinaHealthCheck`, `ProviderHealthCheckOptions` | `src/Encina.Messaging/Health/` | Health check base and options |
| `OutboxActivitySource` | `src/Encina.Messaging/Diagnostics/OutboxActivitySource.cs` | Pattern for the source and meter |
| `EventIdRanges` and the registry | `src/Encina/Diagnostics/EventIdRanges.cs`, `docs/engineering/eventid-block-registry.md` | New field `ZeroMQ`; zone rows |

### Event ID Allocation

| Package | Range | Notes |
|---------|-------|-------|
| `ZeroMQ` (new) | 40570-40589 | Second range of the block `40550-40649` of the Transports zone taken by transport-new-2 (`DaprPubSub` 40550-40569 is #242's); ids 40570-40584 used (40570-40573 host and publish, 40574-40580 subscribe, push and pull, 40581-40584 request-reply), 40585-40589 spare. The block `9750-9799` of the batch snapshot is registered as `SecurityBreakTheGlass` on `origin/main` (status `superseded` in the registry), so the family takes the zone after the `reserved` rows 40000-40549 that #4007 (S0, #4174) holds on `origin/main` for the existing transports (`EventIdRegistryAgreementTests` rule 3 forbids a registered range inside a reserved block). Verified free on `1a661f5c1` by reading the Blocks table of the registry and `EventIdRanges.cs`. |

### Estimated File Count

| Category | Files | Notes |
|----------|------:|-------|
| Production code | 13 | csproj, options, validator, envelope, host, interface, publisher, error codes, `Log`, DI, health check, 2 diagnostics |
| Tests | 12 | options, envelope, DI, host, publisher, subscribe, push-pull, request-reply, health, diagnostics, guard, property, conformance subclass, integration |
| Shared edits | 9 | `Encina.slnx`, `Directory.Packages.props`, 5 test `.csproj` files, `EventIdRanges.cs`, allocation test, `Encina.OpenTelemetry` |
| Documentation | 8 | README, feature page, transports page, INVENTORY, registry rows, changelog, 2 justification files |
| **Pull request 1** | **14** | csproj, options, validator, envelope, DI, `Encina.slnx`, `Directory.Packages.props`, `UnitTests.csproj`, `GuardTests.csproj`, manifest, options test, envelope test, DI test, guard test; about 650 lines |
| **Pull request 2** | **15** | interface, publisher, host (it implements `IHostedService` itself, so there is no separate hosted-service class), error codes, `Log`, `EventIdRanges.cs`, `src/Encina/PublicAPI.Unshipped.txt` edit, allocation test, registry, DI edit, DI test edit, `ZeroMQSocketHostTests`, `ZeroMQMessagePublisherTests`, guard edit, manifest edit; about 800 lines; at the limit: if one more file appears, `Log.cs`, the range registration (`EventIdRanges.cs`, PublicAPI line, allocation test, registry) move to a pull request 2a |
| **Pull request 3** | **9** | interface edit, publisher edit, host edit, error codes edit, `Log` edit, 2 unit tests, guard edit, manifest edit; about 600 lines |
| **Pull request 4** | **8** | interface edit, publisher edit, host edit, error codes edit, `Log` edit, 1 unit test, guard edit, manifest edit; about 450 lines |
| **Pull request 5** | **10** | health check, 2 diagnostics, DI edit, `Encina.OpenTelemetry` edit, `Encina.OpenTelemetry` registration test edit, 2 unit tests, guard edit, manifest edit; about 500 lines |
| **Pull request 6** | **7** | conformance subclass, integration test, property test, `ContractTests.csproj`, `IntegrationTests.csproj`, `PropertyTests.csproj`, manifest; about 650 lines |
| **Pull request 7** | **7** | README, feature page, transports page, INVENTORY, changelog fragment, 2 justification files; about 350 lines |

Implementers and callers: `IZeroMQMessagePublisher` is new, so it has no implementers or `new X(` sites to update; the shared edits are additive. Pull request 1 registers the options and their validator, pull request 2 the host, hosted service and scoped publisher (pull requests 3 and 4 add members to the registered types), pull request 5 the health check. Each pull request builds alone in order 1 to 7; pull request 6 also needs #240 pull request 5 (the conformance base).

---

## Combined AI Agent Prompts

<details>
<summary>Combined prompt for all phases</summary>

```text
PROJECT CONTEXT: Encina is a pre-1.0 .NET 10 / C# 14 library; no backward compatibility, zero warnings, ROP (Either<EncinaError, T>). Transports are publisher-only except MQTT and Redis.PubSub; Encina.ZeroMQ adds a brokerless transport on NetMQ (LGPL-3.0, opt-in package).

IMPLEMENTATION OVERVIEW: PR1 package, options, envelope, DI; PR2 the single-owner socket host, publish and the EventId range ZeroMQ 40570-40589; PR3 subscribe, push and pull; PR4 request-reply with timeout and socket recreation; PR5 health check and telemetry; PR6 conformance subclass, property and integration tests with real loopback sockets; PR7 documentation and justification files. Dilemmas D1, D2, D3, D4, D5, D6, D7, D8, D9, D11 and D12 are implemented as the maintainer answers, the recommended options until then.

KEY PATTERNS: at-most-once, never success on a failure; no exception escapes the poller or a receive loop; never block a pool thread on a socket; named ADR-062 category on every error; constant error messages; ids and payloads never logged; CURVE secret [JsonIgnore] with ToString override; targets never go down; CRAP <= 10.

REFERENCE FILES: src/Encina.AmazonSQS, src/Encina.Redis.PubSub, src/Encina.RabbitMQ, src/Encina.Messaging/Health, src/Encina/Diagnostics/EventIdRanges.cs.
```

</details>

---

## Cross-Cutting Integration Matrix

| # | Function | Status | Notes |
|---|----------|--------|-------|
| 1 | Caching | ❌ | A transport does not cache messages. |
| 2 | OpenTelemetry | ✅ | Per-package `ActivitySource` and `Meter` (Design Choice 11); the shared design of #725 may rename them. |
| 3 | Structured Logging | ✅ | `[LoggerMessage]` in `Log.cs`, EventIds 40570-40584 of the range 40570-40589; error codes and exception types only. |
| 4 | Health Checks | ✅ | `ZeroMQHealthCheck` checks the host and the bound endpoints (no broker to ping). |
| 5 | Validation | ❌ | Options are validated at startup; payload validation belongs to the validation behaviors of the consuming handler. |
| 6 | Resilience | ✅ | ZeroMQ reconnects by itself: `ReconnectInterval` and `Linger` are options and a REQ socket is recreated after a timeout; retry and breaker decorators for the new transports (#742 covers the ten existing ones) are in the issue file `transport-new-2-tenant-module-and-resilience-adoption`. |
| 7 | Distributed Locks | ❌ | Brokerless, no shared state; PUSH/PULL gives each message to one puller. |
| 8 | Transactions | ❌ | No acknowledgement and no unit of work (Design Choice 3). |
| 9 | Idempotency | ✅ | The message id travels in every message; there is no redelivery, so duplicates come only from sender retries, handled by the inbox. |
| 10 | Multi-Tenancy | ⏭️ | Tenant-scoped topics and headers need #1164 and #2385; issue file `transport-new-2-tenant-module-and-resilience-adoption`. |
| 11 | Module Isolation | ⏭️ | Same issue file. |
| 12 | Audit Trail | ❌ | Transport is infrastructure; audit belongs to the outbox and inbox. |

---

## Prerequisites & Dependencies

- None blocking pull request 1. Pull requests 2 to 7 follow in order; pull request 6 needs #240 pull request 5 (the conformance base).
- **Related, not blocking**: #1609 (the other transports gain subscribe with the shape chosen in D4 and D5), #251, #252 and #2385 (contracts the new transports adopt through the issue file), #725 and #742 (telemetry and resilience of all transports), #1164.
- **Sibling plans**: #242, #611, #612 and #310 of this family and the five plans of transport-new-1 answer the same D4, D5 and D11 questions, with the same recommended answers.

---

## Next Steps

1. Ask the maintainer D1, D2, D3, D4, D5, D6, D7, D8, D9, D11 and D12 when work on #610 starts, D1 first because it can stop the issue; record the answers in `## Maintainer Decisions`.
2. Pull request 1: package skeleton (14 files).
3. Pull requests 2 to 5: publish (15), subscribe and push-pull (9), request-reply (8), health and telemetry (10).
4. Pull requests 6 and 7: contract, property and integration tests (7), documentation (7).
