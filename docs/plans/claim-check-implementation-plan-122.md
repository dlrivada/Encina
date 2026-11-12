# Implementation Plan: Claim Check Pattern — Attribute, Compression, Cleanup, Transports and Stores

> **Issue**: [#122](https://github.com/dlrivada/Encina/issues/122) (`[FEATURE] Claim Check Pattern`; absorbs the closed duplicates #247, #298 and #311)
> **Type**: Feature
> **Complexity**: High (7 broker publishers, two new packages, a cleanup loop, an ADR; 11 pull requests)
> **Estimated Scope**: ~1,800-2,400 lines of production code + ~2,500-3,200 lines of tests
> **Sibling plan**: #599 (contract, in-memory store, `ClaimCheckService`, outbox integration)

---

## Summary

Deliver everything of the Claim Check feature that sits on top of the abstraction slice of #599: the `[ClaimCheck]` attribute, compression, payload cleanup, the integration with the broker publishers, and the stores that keep the payloads (Redis and an adapter over the `IBlobStore` of #1239 that provides the file system, Amazon S3 and Azure Blob). This plan does not redefine anything the #599 plan owns; see its "Who owns what" table. The shared names are repeated here so that a worker reading only this file builds the right thing.

### Who owns what (same table as the #599 plan)

| Piece | Owner | Name or location |
|-------|-------|------------------|
| Folder `Encina.Messaging.ClaimCheck`, `IClaimCheckStore` (bytes and streams, `Either`), `ClaimCheckReference`, `ClaimCheckMetadata`, `ClaimCheckErrors` | #599 | `src/Encina.Messaging/ClaimCheck/` |
| `ClaimCheckEnvelope`, `IClaimCheckService`, `ClaimCheckService`, `ClaimCheckRequest`, `ClaimCheckMode`, `ClaimCheckOptions`, `InMemoryClaimCheckStore`, `UseClaimCheck`, outbox offload and resolve, `ClaimCheckStoreContract` | #599 | extended here where stated |
| EventIds `MessagingClaimCheck` 6000-6049 | #599 registers the range and uses 6000-6024 | **this plan uses 6025-6049** |
| `[ClaimCheck]` attribute and its cache | #122 | Phase 1 |
| Compression and the envelope `flags` field | #122 | Phase 2 |
| Payload deletion, expiry sweeper; the outbox purge deletion point is a requirement handed to #1200 | #122 | Phase 3 |
| Broker publishers and consumer-side resolve | #122 | Phase 4 |
| `Encina.ClaimCheck.Redis`, `Encina.ClaimCheck.Storage` | #122 | Phase 5, EventIds `ClaimCheckProviders` 6150-6199 |
| The single ADR of the feature | #122 | Phase 10 |

### What the code does today (2026-11-11, `main` at `7a0049c07`)

No `ClaimCheck` type or package exists. The facts that this plan depends on, beyond those in the #599 plan:

1. **There is no common transport interface.** `IMessageTransport` does not exist in `src/`. Each transport package has its own publisher that serializes the message itself: `RabbitMQMessagePublisher.cs:72` and `:114`, `KafkaMessagePublisher.cs:70` and `:192`, `NATSMessagePublisher.cs:89`, `:131` and `:198`, `AzureServiceBusMessagePublisher.cs:65`, `:108` and `:152`, `AmazonSQSMessagePublisher.cs:91`, `:152`, `:242` and `:303`, `RedisPubSubMessagePublisher.cs:60`, `MQTTMessagePublisher.cs:69`. All use `JsonSerializer.SerializeToUtf8Bytes(message)` or an equivalent. The issue's assumption that "transports only carry the reference, no per-transport code" is therefore wrong for the publish side: 7 publishers (about 20 serialization sites) need a small change.
2. **The other three transports of the category do not serialize a published body.** `Encina.InMemory` passes objects in process (no wire, no size limit); `Encina.gRPC` (`GrpcMediatorService.cs:234`, `:328`) and `Encina.GraphQL` are request and response protocols with their own server limits, not message brokers. This plan scopes the transport work to the 7 broker publishers and records the other three as not applicable (choice 2).
3. **The packages have no consumer.** `Encina.RabbitMQ` contains only a publisher (`IRabbitMQMessagePublisher`, `RabbitMQMessagePublisher`); consumers are application code. The consumer side of the pattern is therefore a public resolver that application code calls, not an interceptor inside Encina.
4. **Deletion has more than one consumer of the payload, and there is no outbox purge yet.** The outbox marks a message processed in `OutboxBatchProcessor.cs:197`, replay copies `Content` unchanged (`:389-392`), and the dead-letter capture keeps the stored content. `src/Encina.Messaging/Purge/MessagePurgeService.cs` purges the inbox only today (its XML doc: "today the inbox"); the purge of processed outbox rows is the open issue #1200, which will add SQL to the stores of all 10 database providers. When #1200 ships, it will delete processed rows whose `Content` may hold an envelope, and the payload of such a row must be deleted too (choice 5, Phase 3 point (b)).
5. **Backends already in the repository.** `Azure.Storage.Blobs` 12.30.1 and `StackExchange.Redis` are in `Directory.Packages.props`; `AWSSDK.S3` is not (the #1239 plan adds it). The fixtures `AzuriteFixture` (collection `Cloud-Azurite`) and `LocalStackFixture` (`Cloud-LocalStack`) and `RedisFixture` exist in `tests/Encina.TestInfrastructure/Fixtures/`.

### Scope

- **Affected packages**: `Encina.Messaging` (attribute, compression, cleanup), the 7 publisher packages (`Encina.RabbitMQ`, `Encina.Kafka`, `Encina.NATS`, `Encina.AzureServiceBus`, `Encina.AmazonSQS`, `Encina.Redis.PubSub`, `Encina.MQTT`), new `Encina.ClaimCheck.Redis`, new `Encina.ClaimCheck.Storage`.
- **Provider category**: Messaging transports (AGENTS.md §5): the feature touches every broker publisher, so it is made consistent across the 7 that serialize a body. Cloud: AWS and Azure through `IBlobStore` (Google Cloud Storage post-1.0, SPEC-000 §6.1). Database: not applicable unless choice 4 picks database stores.
- **Out of scope**: the abstraction slice (#599); Google Cloud Storage; the six planned transports (each gets the same two-line call when it is created).
- **Estimated files**: ~35 production files, ~45 test files, ~15 documentation and manifest files. Eleven pull requests (see "Pull request cut" under Implementation Phases).

---

## Design Choices

<details>
<summary><strong>1. Choices owned by the #599 plan — package home, store shape, envelope in <code>Content</code>, threshold scope</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Follow the #599 plan: folder of `Encina.Messaging`; bytes and streams with `Either`; envelope in `Content`; automatic threshold refined by the attribute** | One feature, one set of answers; this plan does not fork the contract | If the maintainer picks the other option for one of the four, Phase 1 and Phase 3 change shape |
| **B) Decide them again here** | None | Two plans would answer the same Dilemma and could disagree |

### Chosen Option: **A — the #599 answers apply** (recommended, pending the maintainer)

### Rationale

**Class:** Dilemma (owned by the #599 plan, choices 1, 2, 4 and 5). The maintainer answers once; this entry copies the answer into this plan's Maintainer Decisions section.

- The attribute of Phase 1 is the refinement that makes the automatic threshold safe: `Mode = Never` opts a type out, `Always` forces the offload, `ThresholdBytes` overrides the global value.
- If the reference moves into outbox columns instead of the envelope (the alternative of #599 choice 4), the cleanup of Phase 3 queries the column instead of walking the stores.

</details>

<details>
<summary><strong>2. Transport coverage — all broker publishers, or only the outbox plus a public helper</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) The 7 broker publishers call `IClaimCheckService` before they publish; a public resolver serves consumers** | The pattern solves the problem it is named for (broker size limits); consistent across the transport category as AGENTS.md §5 requires; the envelope also works for brokers without headers because it is detectable in the body | About 20 serialization sites in 7 packages; each package gains an optional dependency |
| **B) Outbox integration (#599) plus a public `IClaimCheckService`; publishers untouched** | No change in the transport packages; applications call the service where they publish large messages | The outbox publishes in process (`OutboxProcessorBase.cs:126`), so the broker limit is not addressed unless every application wires the service itself; inconsistent across transports |

### Chosen Option: **A — the 7 broker publishers, with a header marker where the broker has headers** (recommended, pending the maintainer)

### Rationale

**Class:** Dilemma. Option A wins when broker size limits are the motive, which is the first bullet of the issue's motivation. Option B wins when the application owns its publishing code and the maintainer wants the transport packages untouched for 1.0.

- We recommend A because the category rule asks for consistency across providers and the change per publisher is small and mechanical.
- Marker: on every broker that has headers the marker is mandatory, not an optimization: RabbitMQ message headers, Kafka headers, NATS headers, Azure Service Bus application properties, SQS message attributes and MQTT 5 user properties carry the marker header (its name and value are choice 10). Redis Pub/Sub and MQTT 3.1.1 have no headers, so consumers detect the body prefix `CLC:v1:` there.
- Enabling claim check on a publisher changes what its consumers receive: an offloaded message arrives as the `CLC:v1:` envelope text, not as JSON, and a consumer that does not call the resolver of Phase 4 fails to deserialize it. The behavior is opt-in (the publisher only offloads when `IClaimCheckService` is registered), and the XML doc of each publisher, the feature guide and the changelog fragment state that consumers must call the resolver before the feature is turned on.
- Not applicable: `Encina.InMemory` (no wire), `Encina.gRPC` and `Encina.GraphQL` (request and response protocols with server limits). If the maintainer wants gRPC covered, it becomes a follow-up issue.
- Delivery split: two transports per pull request, MQTT with the resolver and the transport contract test (Phase 4, PR 3-6).

</details>

<details>
<summary><strong>3. Stores — dedicated satellite packages, or an adapter over <code>IBlobStore</code></strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Dedicated packages `Encina.ClaimCheck.AzureBlob`, `.AmazonS3`, `.FileSystem`, `.Redis`** (the list in the issue) | Each package is small and independent; no dependency on #1239 | Two more integrations of the Azure and S3 SDKs next to those of `Encina.Storage`; tenant partitioning, retention and encryption hooks would be written twice |
| **B) `Encina.ClaimCheck.Storage`, an adapter that implements `IClaimCheckStore` over `IBlobStore` (file system, S3, Azure Blob), plus `Encina.ClaimCheck.Redis`** | One implementation of each cloud SDK; payloads inherit tenant partitioning, envelope encryption and retention hooks from #1239; three of the four stores come from one adapter | The feature waits for `Encina.Storage` (#1239) for its blob backends; Redis is still its own package |

### Chosen Option: **B — adapter over `IBlobStore` plus a Redis package** (recommended, pending the maintainer)

### Rationale

**Class:** Dilemma. Option B wins when the application also needs `Encina.Storage` or wants payloads under the same tenant, retention and encryption rules as other files. Option A wins when claim check must ship before `Encina.Storage`, or in an application that wants a tiny dependency set.

- #1239 states that the claim-check stores and `IBlobStore` should not duplicate each other: its settled alternative says `IBlobStore` is the storage primitive claim check can build on.
- The in-memory store of #599 and the Redis package (this plan) do not depend on #1239, so the feature is usable before it; only the blob backends wait.
- If A is chosen, the file inventory grows by three packages and the integration tests of #1239 for Azurite and LocalStack are written here as well.

</details>

<details>
<summary><strong>4. Database-backed stores — none in 1.0, or all 10 providers</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) No database stores** (blob, file system, Redis and in-memory only) | No 10-provider matrix; large payloads belong in blob storage by design | An on-premise deployment with no blob service and no file share has no durable store |
| **B) Database stores for ADO.NET (3), Dapper (3), EF Core (3) and MongoDB (GridFS)** (from #298) | Works anywhere the application already has a database | AGENTS.md §5 requires all 10 database providers once any ships; large objects in the transactional database are the problem the pattern avoids |

### Chosen Option: **A — no database stores** (recommended, pending the maintainer)

### Rationale

**Class:** Dilemma. Option A wins on size and on fit: the file-system store of `Encina.Storage` covers the on-premise case with a share or a volume. Option B wins for an on-premise application that can run neither a file share nor a blob service.

- If B is chosen, this plan is extended by one phase per provider family and the 10-provider integration tests (real databases, never a `.md` justification).
- #599 raises the same question; the answer is recorded once.

</details>

<details>
<summary><strong>5. When an outbox payload is deleted — when the outbox row no longer needs it, or by time to live only</strong></summary>

This choice covers payloads offloaded on the outbox path (#599). Payloads offloaded by the broker publishers have no outbox row and follow choice 8.

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) When the outbox row reaches processed, when the outbox purge of #1200 deletes the row, or when the dead letter expires; the time to live is the safety net** | Payloads do not wait for the TTL; under at-least-once redelivery the payload exists until success; a purged row leaves no orphan | Three deletion points to keep consistent; a delete failure must be visible (logged, counted, retried by the sweeper); the purge point depends on #1200 (10 providers) |
| **B) Time to live only** | One mechanism; fixes the lifetime independently of delivery, which suits a compliance rule with a fixed retention | Payloads outlive their messages for the whole TTL; a TTL shorter than a retry or a dead-letter replay breaks the message |

### Chosen Option: **A — delete on processed, on purge and on dead-letter expiry; TTL as safety net** (recommended, pending the maintainer)

### Rationale

**Class:** Dilemma. Option A wins under at-least-once redelivery and replay, where the payload is needed until the message is finished. Option B wins where a retention rule must fix the payload lifetime regardless of delivery.

- The rule in one sentence: a payload is deleted when its outbox row reaches a state from which the payload is never read again, that is processed, purged by #1200, or a dead letter whose retention has expired; replay (`OutboxBatchProcessor.cs:389-392`) only concerns rows that were dead-lettered, so a processed row is never replayed and its payload can go at the processed mark.
- A failed delete never fails the message (the message succeeded), but it is not swallowed: it is logged by error code, counted in `encina.claimcheck.delete_failures`, and the sweeper of choice 6 retries through the TTL.
- The purge point is not built by this plan. The outbox purge is #1200 and runs as SQL in the stores of the 10 database providers; for the payload to be deleted, each of those stores must return the content of the rows it deletes (or the purge must read it first). That is a requirement this plan hands to the #1200 plan, with its 10-provider consequence. Until #1200 ships, there is no outbox purge and the question does not arise; if #1200 ships without it, payloads of purged rows are reclaimed only by the TTL sweeper.
- #599 ships with no default TTL (`DefaultTimeToLive = null`, its choice 7), so the sweeper reclaims nothing until a TTL is set. This plan proposes a default TTL together with the validation (choice 10): the TTL must be longer than the longest retry window plus the dead-letter retention, and `ClaimCheckOptions` validation compares it with the outbox retry settings and `DeadLetterOptions.RetentionPeriod` at start-up.

</details>

<details>
<summary><strong>6. Expiry sweeping — an optional store capability, or backend lifecycle rules only</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Optional interface `IExpiringClaimCheckStore` with `PurgeExpiredAsync(DateTime beforeUtc, int batchSize, CancellationToken)`, called by a hosted `ClaimCheckCleanupProcessor`; stores with native expiry (Redis) skip it** | Works the same on every backend including the file system; deterministic with `FakeTimeProvider`; idempotent, so several hosts need no lock | A loop to run and test |
| **B) Rely on backend lifecycle rules** (Azure Blob lifecycle management, S3 lifecycle, Redis TTL) | No code | The file system and the in-memory store have none; rules are configured outside Encina and can be forgotten |

### Chosen Option: **A — `IExpiringClaimCheckStore` and a hosted cleanup processor** (recommended, pending the maintainer)

### Rationale

**Class:** Dilemma. Option A wins when expiry must be enforced by the application whatever the backend. Option B wins when an operations team already manages lifecycle rules and wants no extra loop.

- The processor is a `BackgroundService` over a `PeriodicTimer` that catches per iteration and never lets an exception escape, with `OperationCanceledException` from the stopping token ending the loop (AGENTS.md §3, background loops; ADR-052). A failed pass is logged by error code and retried at the next tick; no operation is dropped silently.
- Several hosts may run it: `PurgeExpiredAsync` deletes by expiry and is idempotent, so a lock is not needed (matrix function 7).
- The processor is only registered when `UseClaimCheck` is true and the store implements the interface.

</details>

<details>
<summary><strong>7. Compression, integrity and encryption — where each applies</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Compression inside `ClaimCheckService` (gzip, optional, flag in the envelope); SHA-256 always (from #599); encryption by composition: the serializer chain encrypts before the offload, and `IBlobStore` envelope encryption (#1239) protects blobs at rest** | No new encryption code; the order is explicit; already-encrypted content is not compressed (it does not compress) | Compression is skipped for content that starts with `ENC:v1:` |
| **B) Compression and encryption options on the store** (the first draft of #122: `ClaimCheckOptions.Compress`, `Encryption`) | Visible on the store API | Each store would implement them; duplicates `Encina.Messaging.Encryption` and the encryption of `Encina.Storage` |

| **C) Compression as an `IMessageSerializer` decorator** (the seam that `IMessageSerializer.cs` names for "encryption, compression, and schema versioning via the decorator pattern") | Follows the documented extension point; compression also applies to messages that are not offloaded | The serializer API is synchronous and works on strings (#599 explains why the claim check does not sit there); a compressed body is binary, so it would need a text encoding inside `Content`; the envelope could not record the flag |

### Chosen Option: **A — compression in the service, encryption by composition** (recommended, pending the maintainer)

### Rationale

**Class:** Dilemma. Option A wins when compression is only worth its cost for the large payloads that leave the row, and when the flag must travel in the envelope. Option C wins when the maintainer wants every message compressed through the documented serializer seam, whatever its size. Option B duplicates the encryption that `Encina.Messaging.Encryption` and `Encina.Storage` already provide, so it is not recommended.

- The envelope gains a `flags` field in this plan (`-` or `gz`); the #599 plan's envelope text includes the field from the start so that v1 does not change.
- #311's `ClaimTicket` and #247's checksum are covered by `ClaimCheckReference` and the SHA-256 of #599.
- Defaults that come with option A (recommended, pending the maintainer): `Compress = false` (opt-in, pay for what you use); `CompressionLevel = Fastest` (the offload runs on the publish path); `MinCompressBytes = 1024` (gzip headers cost more than they save below about 1 KiB); content that starts with `ENC:v1:` is never compressed (ciphertext does not compress). Each is an option the application can change.

</details>

<details>
<summary><strong>8. Lifetime of payloads offloaded by the broker publishers</strong></summary>

A payload offloaded inside a publisher (Phase 4) has no outbox row, so none of the deletion points of choice 5 applies. With the #599 default (`DefaultTimeToLive = null`) such a payload would never be deleted.

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Time to live only, with a required value for transport offloads**: `ClaimCheckOptions.TransportTimeToLive` (default 14 days), validated at start-up to be at least 1 day; the publisher always passes it to the store | Works for fan-out (several consumers, Kafka consumer groups, Redis Pub/Sub subscribers), where no single consumer knows that the others are done; one mechanism, enforced by every store or the sweeper | A consumer that lags behind the TTL, or a broker dead-letter queue kept longer than the TTL, gets `claimcheck.not_found`; payloads (which may hold personal data) live for the whole TTL |
| **B) Explicit consumer-side completion**: `IClaimCheckService.CompleteAsync(reference)` called by the consumer after success, plus the TTL as safety net | Payloads go as soon as the single consumer succeeds | Wrong for fan-out: the first consumer deletes the payload the others still need; depends on application code remembering to call it; a crash between success and completion leaves the payload to the TTL anyway |

### Chosen Option: **A — TTL only, required for transport offloads, 14 days by default, minimum 1 day** (recommended, pending the maintainer)

### Rationale

**Class:** Dilemma. Option A wins when a message may have several consumers or when consumer code is not under the library's control. Option B wins for point-to-point queues with one consumer where the payload must not outlive its use.

- The feature guide documents the rule: the TTL must exceed the longest consumer lag and the broker's own dead-letter retention; a value below the minimum fails start-up.
- The 14-day default and the 1-day minimum are proposals; the maintainer may pick other values.

</details>

<details>
<summary><strong>9. Offload threshold of the broker publishers — the global threshold, or a per-transport threshold with an overhead margin</strong></summary>

The global `ThresholdBytes` of #599 is 256 KiB, the Azure Service Bus Standard limit. Broker limits count the whole message, including headers, application properties and message attributes, so a body just under 256 KiB plus the marker header and the broker's own metadata still fails to publish.

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) A per-transport threshold in each transport's options** (`ClaimCheckThresholdBytes`), defaulting to the broker's documented default limit minus a 64 KiB margin for headers and metadata; the `[ClaimCheck]` attribute still overrides per type | Each broker gets a value that publishes; the margin is documented and testable; brokers with large limits (RabbitMQ, Kafka) do not offload messages they can carry | One more option per transport; each default must be checked against the broker's documentation when the PR is implemented |
| **B) The global threshold lowered to leave the margin** (for example 192 KiB for every transport and the outbox) | One value | Offloads messages that brokers with larger limits would carry; still wrong for a broker configured below the default |

### Chosen Option: **A — per-transport threshold, broker default limit minus 64 KiB** (recommended, pending the maintainer)

### Rationale

**Class:** Dilemma. Option A wins when the transports differ in limits, which they do. Option B wins when one value is easier to operate and the extra offloads are acceptable.

- Proposed defaults (each verified against the broker's documentation in the PR that touches the publisher): Azure Service Bus 192 KiB (Standard tier 256 KiB); Amazon SQS 192 KiB (the long-standing 256 KiB limit; the implementation checks the current AWS quota and raises the default if the quota is larger); Kafka 960 KiB (broker default `message.max.bytes` of about 1 MiB); NATS 960 KiB (server default `max_payload` 1 MiB); RabbitMQ, MQTT and Redis Pub/Sub 960 KiB (their limits are broker configuration; 1 MiB keeps the same order as Kafka and NATS).
- Validation: a per-transport threshold must be greater than 0 and not above `MaxPayloadBytes`.

</details>

<details>
<summary><strong>10. Defaults and public names of the stores, the sweeper and the marker</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) The values below** | Each has a stated reason; all are options the application can change | They are proposals, not derived from a rule |
| **B) Other values chosen by the maintainer** | Fits a deployment the maintainer knows | Needs a reason recorded per value |

### Chosen Option: **A — the values below** (recommended, pending the maintainer)

### Rationale

**Class:** Dilemma (values that no rule fixes).

| Default or name | Value | Reason |
|---|---|---|
| Redis `MaxPayloadBytes` | 8 MiB | Redis keeps values in memory; larger payloads belong in blob storage |
| Redis key prefix | `encina:claimcheck:` | Same `encina:` prefix style as the other Redis packages |
| Redis behavior for a null TTL | the key is written without expiry and removed only by the deletion points of choice 5 | Same meaning as null in #599 (no expiry); the alternative, refusing a null TTL in the Redis store, would make the store disagree with the contract |
| Blob container of the Storage adapter | `claimchecks` | One container for the feature, partitioned by tenant by `IBlobStore` |
| Cleanup `Interval` and `BatchSize` | 15 minutes, 500 | Expiry is measured in days, so a sweep every 15 minutes is precise enough; 500 keeps one pass short |
| Outbox default TTL (choice 5) | 30 days | Above the defaults of the retry window and of `DeadLetterOptions.RetentionPeriod` (7 days), so the start-up validation passes with default options |
| Marker header name and value | `encina-claim-check: v1` | Lower-case with a hyphen works in every header model of the 7 brokers (Kafka and NATS headers, AMQP headers, SQS attribute names allow it) |

</details>

---

## Implementation Phases

**Pull request cut.** Phases 6-9 are not pull requests of their own: each item lands in the pull request of the code it concerns, as the table shows. Counts include tests, PublicAPI files and coverage manifests; every pull request stays within 15 files and 1,500 changed lines.

| PR | Content | Files | Est. lines |
|----|---------|-------|-----------|
| 1 | Phases 1-2: `ClaimCheckAttribute`, `ClaimCheckAttributeCache`, `ClaimCheckCompression`, `ClaimCheckService` and `ClaimCheckOptions` (modified), Messaging `PublicAPI.Unshipped.txt`; unit tests (attribute cache, compression, service policy), guard test, property test (compression round trip and threshold boundary); `Encina.Messaging.json` manifest | 12 | ~1,000 |
| 2 | Phase 3 and the Messaging part of Phase 8: `IExpiringClaimCheckStore`, `ClaimCheckCleanupOptions`, `ClaimCheckCleanupProcessor`, `ClaimCheckLog` (6025-6034), `InMemoryClaimCheckStore`, `OutboxBatchProcessor`, `ClaimCheckOptions` (validation, default TTL) and the messaging registration (modified), `PublicAPI.Unshipped.txt`; unit tests (processor with `FakeTimeProvider`, deletion point, validation), guard test, integration test (outbox end to end with deletion), load-test justification; manifest | 15 | ~1,300 |
| 3 | Phase 4 base + RabbitMQ, Kafka: byte overloads in `ClaimCheckService`, `ClaimCheckBody`, transport offload log (6035-6039) in `ClaimCheckLog`, Messaging `PublicAPI.Unshipped.txt`; the 2 publishers and their 2 registrations, 2 `PublicAPI.Unshipped.txt`; unit tests (overloads, 2 publishers); 2 manifests | 15 | ~1,100 |
| 4 | Phase 4: NATS, Azure Service Bus (publisher, registration, PublicAPI, unit test, manifest each) | 10 | ~700 |
| 5 | Phase 4: Amazon SQS, Redis Pub/Sub (same shape) | 10 | ~800 |
| 6 | Phase 4: MQTT (same shape) + `ClaimCheckResolver` + its unit test + transport contract test across the 7 publishers + Messaging PublicAPI | 9 | ~800 |
| 7 | Phase 6 (keyed stores in `Encina.Messaging`): `ClaimCheckService`, `ClaimCheckErrors` (`claimcheck.store_not_found`), `PublicAPI.Unshipped.txt`; unit test, DI test (`ValidateOnBuild`, `ValidateScopes`, two keyed stores); manifest | 6 | ~400 |
| 8 | Phase 5 Redis store: csproj, `RedisClaimCheckStore`, `RedisClaimCheckOptions` (with `Validate`), registration, `ClaimCheckProvidersLog` (6150-6159), README, `PublicAPI.Shipped.txt`, `PublicAPI.Unshipped.txt`, `EventIdRanges.cs` (`ClaimCheckProviders`), `EncinaEventIdAllocationTests.cs`, `Encina.slnx`; unit and guard tests; manifest (the contract test goes to PR 9 to stay within 15 files) | 15 | ~1,200 |
| 9 | Phase 7 for Redis: `ResilientClaimCheckStore`, health check, registration and log (6160-6164) modified, PublicAPI; unit tests (decorator, health check); contract test (from PR 8); integration test (`RedisFixture`); manifest | 9 | ~800 |
| 10 | Phase 5 Storage adapter: csproj, `BlobClaimCheckStore`, registration, log (6165-6174), health check, README, 2 PublicAPI files, `EncinaEventIdAllocationTests.cs`, `Encina.slnx`; unit, guard and contract tests; manifest | 14 | ~1,100 |
| 11 | Phase 5 integration and Phase 10: integration tests through the adapter (file system, Azurite, LocalStack), benchmark of the offload path, ADR, `docs/features/claim-check.md`, `docs/INVENTORY.md`, `ROADMAP.md`, changelog fragment | 9 | ~1,000 |

### Phase 1: `[ClaimCheck]` attribute and per-type policy

> **Goal**: A message type can set its own threshold, mode and store.

<details>
<summary><strong>Tasks</strong></summary>

1. **`src/Encina.Messaging/ClaimCheck/ClaimCheckAttribute.cs`**: `[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, Inherited = true)] public sealed class ClaimCheckAttribute : Attribute` with `int ThresholdBytes` (0 means the global value), `ClaimCheckMode Mode` (the enum of #599; default `Conditional`), `string? Store` (a keyed store name; null means `DefaultStoreKey`). The issue's `StorageProvider` property is named `Store` because the stores are keyed services, not providers.
2. **`ClaimCheckAttributeCache.cs`**: static per-generic-type cache (pattern: `EncryptedMessageAttributeCache` in `Encina.Messaging.Encryption`), returns a `ClaimCheckRequest` for a `Type`.
3. **`ClaimCheckService`** (from #599): `OffloadAsync` accepts the request built from the cache; no signature change.
4. `PublicAPI.Unshipped.txt`.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 1</strong></summary>

```
You are implementing Phase 1 of issue #122 in Encina: the [ClaimCheck] attribute and its cache.

CONTEXT:
- The #599 plan created ClaimCheckService, ClaimCheckRequest(TypeName, ThresholdBytes?, Mode) and ClaimCheckMode in
  src/Encina.Messaging/ClaimCheck/. This phase only adds the attribute and the cache that build the request.
- Model: src/Encina.Messaging.Encryption/EncryptedMessageAttributeCache.cs (static per-type cache).

TASK:
Create ClaimCheckAttribute (ThresholdBytes, Mode, Store) and ClaimCheckAttributeCache, use the cache in the outbox call sites
of the #599 plan, and add unit, guard and contract tests (Never skips offload, Always forces it, threshold override, named store).

KEY RULES:
- The cache is static per type and thread safe; no reflection on the hot path after the first call.
- Mode = Never wins over any threshold; Always wins over the threshold but not over MaxPayloadBytes.
- Public symbols into PublicAPI.Unshipped.txt; XML docs with an example.

REFERENCE FILES:
- src/Encina.Messaging.Encryption/EncryptedMessageAttributeCache.cs
- src/Encina.Messaging/ClaimCheck/ClaimCheckService.cs (after the #599 PR 1)
```

</details>

---

### Phase 2: Compression

> **Goal**: Optional gzip with the flag carried in the envelope.

<details>
<summary><strong>Tasks</strong></summary>

1. **`ClaimCheckOptions`**: `bool Compress` (default false), `CompressionLevel CompressionLevel` (default `Fastest`), `int MinCompressBytes` (default 1024).
2. **`ClaimCheckService`**: when `Compress` is on and the content does not start with `ENC:v1:`, gzip the UTF-8 bytes before storing, set the envelope `flags` to `gz`, keep `SizeBytes` and `Sha256` of the original bytes, and on resolve decompress and then verify the checksum. A decompression failure is `Left(claimcheck.integrity_failed)`; a decompressed size above `MaxPayloadBytes` is `Left(claimcheck.payload_too_large)` (decompression bomb guard).
3. Tests: round trip, incompressible data, bomb guard, encrypted content skipped.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 2</strong></summary>

```
You are implementing Phase 2 of issue #122 in Encina: gzip compression for claim check payloads.

CONTEXT:
- ClaimCheckEnvelope (from #599) has a flags field: "-" for none, "gz" for gzip.
- ClaimCheckService verifies SHA-256 of the original bytes after decompression.

TASK:
Add the three options, compress before store and decompress after retrieve in ClaimCheckService, cap the decompressed size,
and add unit and property tests (round trip of random payloads, threshold boundary with compression).

KEY RULES:
- Use System.IO.Compression only; no new package.
- Stream the decompression with a size cap; never trust the declared size.
- Skip compression for content starting with ENC:v1: (already encrypted).
- Failures are Left with claimcheck.* codes; no exceptions escape.

REFERENCE FILES:
- src/Encina.Messaging/ClaimCheck/ClaimCheckService.cs, ClaimCheckEnvelope.cs
- src/Encina.Messaging.Encryption/EncryptedPayloadFormatter.cs (prefix constant)
```

</details>

---

### Phase 3: Cleanup — deletion points and the expiry sweeper

> **Goal**: Payloads are deleted when the message no longer needs them, and expired payloads never accumulate.

<details>
<summary><strong>Tasks</strong></summary>

1. **`IExpiringClaimCheckStore.cs`** (`Encina.Messaging.ClaimCheck`): `ValueTask<Either<EncinaError, int>> PurgeExpiredAsync(DateTime beforeUtc, int batchSize, CancellationToken)`. Implemented by `InMemoryClaimCheckStore` (small addition to the #599 class) and by the Storage adapter; Redis expires natively.
2. **`ClaimCheckCleanupOptions.cs`**: `TimeSpan Interval` (default 15 minutes), `int BatchSize` (default 500).
3. **`ClaimCheckCleanupProcessor.cs`**: `BackgroundService` over `PeriodicTimer`, `TimeProvider`-based, catches per iteration, logs by error code, counts purged and failed. Registered by `RegisterClaimCheck` when the store implements `IExpiringClaimCheckStore`.
4. **Deletion points**: (a) `OutboxBatchProcessor` after `MarkAsProcessedAsync` returns `Right` for a message whose content is an envelope: `DeleteAsync`, a `Left` is logged and counted (`delete_failures`) and does not fail the message; (b) the outbox purge: **not built here**. It belongs to #1200, which adds the outbox purge to the stores of the 10 database providers; this plan hands #1200 the requirement that each store returns the envelopes of the rows it deletes so that their payloads are deleted (choice 5). Until then, payloads of purged rows are reclaimed by the sweeper's TTL; (c) dead letters: the payload follows the dead-letter retention, so the sweeper's TTL is validated to be longer than that retention.
5. **Start-up validation** (`ClaimCheckOptions.Validate`): the default TTL (choice 10) is longer than the outbox retry window and `DeadLetterOptions.RetentionPeriod`; `TransportTimeToLive` (choice 8) is at least its minimum.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 3</strong></summary>

```
You are implementing Phase 3 of issue #122 in Encina: payload deletion and the expiry sweeper.

CONTEXT:
- OutboxBatchProcessor marks a message processed at src/Encina.Messaging/Outbox/OutboxBatchProcessor.cs:197; replay copies Content at :389-392.
- There is no outbox purge yet: MessagePurgeService purges the inbox only, and the outbox purge is issue #1200 (10 providers).
  Do not add a purge deletion point here.
- Background loop rule (AGENTS.md §3): a PeriodicTimer loop catches per iteration and never lets an exception escape;
  OperationCanceledException from the stopping token ends the loop.

TASK:
Create IExpiringClaimCheckStore, ClaimCheckCleanupOptions and ClaimCheckCleanupProcessor, add the processed-mark deletion point
and the start-up validation (default TTL, TransportTimeToLive minimum), and test with FakeTimeProvider (advance time, assert purge; a throwing store does not stop the loop and the
failure is counted and logged by code).

KEY RULES:
- A failed delete never fails the message but is never silent: log by code, count it, retry via the sweeper.
- The sweeper is idempotent so several hosts run it without a lock.
- A test never swallows an exception thrown by the code under test (AGENTS.md §9); fix the throwing code instead.
- TimeProvider only; no Thread.Sleep.

REFERENCE FILES:
- src/Encina.Messaging/Outbox/OutboxProcessorBase.cs (loop shape)
- src/Encina.Messaging/DeadLetter/DeadLetterOptions.cs (RetentionPeriod)
- docs/architecture/adr/052-distributed-lock-failure-contract.md
```

</details>

---

### Phase 4: Broker publishers and consumer-side resolve

> **Goal**: Large messages published to a broker travel as a reference; application consumers can resolve them.

<details>
<summary><strong>Tasks</strong></summary>

1. **`ClaimCheckService`** gains byte overloads: `OffloadAsync(ReadOnlyMemory<byte> body, ClaimCheckRequest, CancellationToken)` returning `Either<EncinaError, ClaimCheckBody>` where `ClaimCheckBody(ReadOnlyMemory<byte> Body, bool IsReference)`, and `ResolveAsync(ReadOnlyMemory<byte> body, CancellationToken)`. `IsReference` drives the header marker.
2. **Public consumer helper**: `ClaimCheckResolver` (static extension `ResolveClaimCheckAsync(this IClaimCheckService, ReadOnlyMemory<byte>)`) for application consumers; documented with a RabbitMQ and an Azure Service Bus consumer example.
3. **Publishers** (each: optional `IClaimCheckService? claimCheck = null` constructor parameter, call after serialization at every site listed in "What the code does today" with the transport's threshold (choice 9) and `TransportTimeToLive` (choice 8), mandatory marker header where the broker has headers, `Left` returned as the publish result):
   - PR 3: `RabbitMQMessagePublisher`, `KafkaMessagePublisher`.
   - PR 4: `NATSMessagePublisher`, `AzureServiceBusMessagePublisher`.
   - PR 5: `AmazonSQSMessagePublisher`, `RedisPubSubMessagePublisher`.
   - PR 6: `MQTTMessagePublisher`, the consumer helper and the transport contract test.
4. **Per-transport threshold** (choice 9): each transport's options gain `ClaimCheckThresholdBytes` with the default of choice 9; a unit test publishes a body of threshold minus one byte and one of threshold plus one byte.
5. Each `AddEncina<Transport>` registration resolves the service with `GetService` (optional), so the transport works without the feature.
6. Transport contract test (ContractTests): the same assertions on all 7 publishers with their existing test doubles: above the threshold the broker receives a reference, below it the unchanged body.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 4</strong></summary>

```
You are implementing Phase 4 of issue #122 in Encina: claim check in the broker publishers.

CONTEXT:
- Each publisher serializes with JsonSerializer.SerializeToUtf8Bytes: RabbitMQMessagePublisher.cs:72/:114, KafkaMessagePublisher.cs:70/:192,
  NATSMessagePublisher.cs:89/:131/:198, AzureServiceBusMessagePublisher.cs:65/:108/:152, AmazonSQSMessagePublisher.cs:91/:152/:242/:303,
  RedisPubSubMessagePublisher.cs:60, MQTTMessagePublisher.cs:69. Publishers return ValueTask<Either<EncinaError, Unit>>.
- IClaimCheckService (plan #599 + Phase 1-2 of this plan) offloads above the threshold and resolves envelopes.
- Encina.InMemory, Encina.gRPC and Encina.GraphQL are out of scope (no body published to a broker).

TASK:
For the publishers of the pull request you were given, add the optional IClaimCheckService dependency, offload after
serialization at each site, set the marker header/attribute where the broker supports it, return the Left on failure, and update
the DI. Add the byte overloads and the public consumer helper if they are not present. Add unit, guard and contract tests.

KEY RULES:
- Offload with the transport's own ClaimCheckThresholdBytes (broker default limit minus 64 KiB, choice 9) and with
  ClaimCheckOptions.TransportTimeToLive (choice 8); a transport offload never stores a payload without a TTL.
- The marker header is mandatory where the broker has headers; Redis Pub/Sub and MQTT 3.1.1 rely on the CLC:v1: prefix.
- The transport must work unchanged when the service is not registered (optional dependency, no behavior change).
- A Left from the service is the publish result; never publish the large body as a fallback.
- Do not log bodies; log the message type and the claimcheck.* code.
- Keep the change per site to a few lines; no shared base class is introduced (there is none today).

REFERENCE FILES:
- src/Encina.RabbitMQ/RabbitMQMessagePublisher.cs and the other publishers listed above
- src/Encina.Messaging/ClaimCheck/ClaimCheckService.cs
- tests/Encina.UnitTests/ and tests/Encina.ContractTests/MessageBrokers/ (existing publisher tests)
```

</details>

---

### Phase 5: Stores — Redis and the adapter over `IBlobStore`

> **Goal**: Durable stores behind the contract, tested against real services.

<details>
<summary><strong>Tasks</strong></summary>

1. **`src/Encina.ClaimCheck.Redis/`** (new package): `RedisClaimCheckStore : IClaimCheckStore` using `IConnectionMultiplexer`, `SET key value EX ttl`, payload cap `MaxPayloadBytes` (default 8 MiB: Redis is for small payloads), key prefix `encina:claimcheck:`; `RedisClaimCheckOptions` (connection string `[JsonIgnore]` with `ToString()` override, AGENTS.md §3); `AddEncinaClaimCheckRedis(...)` registering the store as the keyed store with `TryAdd`; log messages in `ClaimCheckProviders` 6150-6164.
2. **`src/Encina.ClaimCheck.Storage/`** (new package, depends on `Encina.Storage` of #1239): `BlobClaimCheckStore : IClaimCheckStore, IExpiringClaimCheckStore` over `IBlobStore` (id = blob name under the container `claimchecks`, TTL kept as blob metadata, `PurgeExpiredAsync` lists by prefix and deletes expired); `AddEncinaClaimCheckStorage(...)`; log messages 6165-6174.
3. Both stores subclass `ClaimCheckStoreContract` (#599) in the contract tests; integration tests: Redis (`RedisFixture`), file system (temp directory), Azure Blob (Azurite) and S3 (LocalStack) through the adapter.
4. `Directory.Packages.props`: no new version for these two packages beyond what #1239 adds.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 5</strong></summary>

```
You are implementing Phase 5 of issue #122 in Encina: Encina.ClaimCheck.Redis and Encina.ClaimCheck.Storage.

CONTEXT:
- IClaimCheckStore and ClaimCheckStoreContract come from the #599 plan; IBlobStore comes from the #1239 plan (Encina.Storage).
- Fixtures: RedisFixture, AzuriteFixture (Cloud-Azurite) and LocalStackFixture (Cloud-LocalStack) in tests/Encina.TestInfrastructure/Fixtures/.
- Satellite DI convention: AddEncina{Feature}{Provider}() with TryAdd*; a store registered by the application wins in any order.

TASK:
Create the two packages as described, with options validators, DI, EventIds in 6150-6199 (register ClaimCheckProviders in
EventIdRanges and the two assemblies in EncinaEventIdAllocationTests first), the contract subclasses and the integration tests.

KEY RULES:
- Secrets in options are [JsonIgnore] with a ToString() override; no connection string in logs.
- Every I/O call is async with a CancellationToken; Left on failure with claimcheck.* codes; never swallow.
- Integration tests use shared [Collection] fixtures and never dispose the fixture's clients.
- Each package has a README and a coverage manifest in .github/coverage-manifest/.

REFERENCE FILES:
- src/Encina.Caching.Redis/ (Redis registration and options style)
- src/Encina.Security.Secrets.AzureKeyVault/ (satellite package layout, options validator)
- tests/Encina.IntegrationTests/Infrastructure/Azurite/
```

</details>

---

### Phase 6: Configuration, DI and named stores

> **Goal**: `[ClaimCheck(Store = "...")]` resolves a keyed store; registration is complete.

<details>
<summary><strong>Tasks</strong></summary>

1. `ClaimCheckService` resolves `IClaimCheckStore` with `GetKeyedService<IClaimCheckStore>(key)`; an unknown key is `Left(claimcheck.store_not_found)` (new error code).
2. `AddEncinaClaimCheckRedis(key)` and `AddEncinaClaimCheckStorage(key)` accept a key (default `"default"`).
3. DI tests with `ValidateOnBuild` and `ValidateScopes` for each satellite and for two stores registered under different keys.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 6</strong></summary>

```
You are implementing Phase 6 of issue #122 in Encina: keyed store registration for claim check.

CONTEXT:
- #599 registers the in-memory store as the keyed service "default". [ClaimCheck(Store = "x")] selects another key.

TASK:
Make the satellites register under a key, make ClaimCheckService return Left(claimcheck.store_not_found) for an unknown key,
and test the DI graph with ValidateOnBuild and ValidateScopes.

KEY RULES:
- TryAdd semantics; an application's own registration wins in any order.
- No exception for an unknown key; a Left with the code.

REFERENCE FILES:
- src/Encina.Messaging/MessagingServiceCollectionExtensions.cs
- src/Encina.Messaging/ClaimCheck/ClaimCheckService.cs
```

</details>

---

### Phase 7: Cross-cutting integration

> **Goal**: The matrix items marked included.

<details>
<summary><strong>Tasks</strong></summary>

1. **Resilience**: `ResilientClaimCheckStore` decorator for the Redis store, shaped like `ResilientSecretReaderDecorator` (`src/Encina.Security.Secrets/Resilience/`), registered when `Encina.Resilience` is present (ADR-055); the Storage adapter inherits the resilience of `IBlobStore` (#1239).
2. **Health checks**: the Redis and Storage packages add a check of their backend through `IEncinaHealthCheck`; the #599 check covers the default store.
3. **Tenancy**: inherited from `ClaimCheckService` (#599); the Storage adapter relies on `IBlobStore` tenant partitioning, so the claim id keeps the tenant segment.
4. **Deferred** (matrix): caching and audit trail (the follow-up issues opened with the #599 plan), idempotency, module isolation.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 7</strong></summary>

```
You are implementing Phase 7 of issue #122 in Encina: resilience and health checks for the claim check stores.

CONTEXT:
- Pattern: src/Encina.Security.Secrets/Resilience/ResilientSecretReaderDecorator.cs and Health/SecretsHealthCheck.cs.
- ADR-055 defines the resilience family; the decorator is registered only when it is present.

TASK:
Add ResilientClaimCheckStore for Redis and the backend health checks of both packages, with tests for transient failures
(retry then success, retry exhausted -> Left) and health states.

KEY RULES:
- Health results carry error codes only; never EncinaError.Message.
- The decorator never swallows a Left; exhausted retries return the last Left.

REFERENCE FILES:
- src/Encina.Security.Secrets/Resilience/
- docs/architecture/adr/055-resilience-family.md
```

</details>

---

### Phase 8: Observability

> **Goal**: Cleanup, delete and transport metrics, logs and spans.

<details>
<summary><strong>Tasks</strong></summary>

1. **EventIds**: `MessagingClaimCheck` 6025-6049 in `Encina.Messaging` (cleanup, delete failures, attribute skip, transport offload); `ClaimCheckProviders` 6150-6199 registered in `EventIdRanges.cs` with `Encina.ClaimCheck.Redis` (6150-6164) and `Encina.ClaimCheck.Storage` (6165-6174) added to `EncinaEventIdAllocationTests`.
2. **Metrics** (Meter of #599): `encina.claimcheck.deleted`, `encina.claimcheck.delete_failures`, `encina.claimcheck.purged`, `encina.claimcheck.transport_offloaded` (tag: transport name).
3. **Spans**: `encina.claimcheck.delete`, `encina.claimcheck.purge` (no payload, bounded tags).

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 8</strong></summary>

```
You are implementing Phase 8 of issue #122 in Encina: observability additions for claim check.

CONTEXT:
- #599 registered MessagingClaimCheck 6000-6049 and uses 6000-6024; this plan uses 6025-6049 (packed from 6025).
- ClaimCheckProviders 6150-6199 is new in this plan.

TASK:
Register ClaimCheckProviders, add the log messages, counters and spans, and test them with an in-memory exporter and the
EventId architecture tests.

KEY RULES:
- Register ranges before use (ADR-021); pack sequentially; never outside the plan's range.
- Bounded tags only; no payload, no ids of data subjects.

REFERENCE FILES:
- src/Encina/Diagnostics/EventIdRanges.cs
- tests/Encina.UnitTests/Testing/Architecture/EncinaEventIdAllocationTests.cs
```

</details>

---

### Phase 9: Testing

> **Goal**: Every flag reaches its target for the code of this plan.

<details>
<summary><strong>Tasks</strong></summary>

| Flag | Scope | Per-file target and justification |
|------|-------|-----------------------------------|
| Unit | attribute and cache, compression, cleanup processor, deletion points, publishers (7), Redis and Storage stores | 85-90: all logic is reachable with test doubles and `FakeTimeProvider`; publishers use their existing doubles |
| Guard | every public method with parameters in the new types and both packages | 90 |
| Contract | `ClaimCheckStoreContract` subclasses for Redis and Storage; transport contract across the 7 publishers | 85: instantiates the real stores and publishers |
| Property | compression round trip, threshold boundary with attribute overrides | 70: pure logic |
| Integration | Redis, file system, Azurite, LocalStack through the adapter; outbox end to end with deletion at the processed mark and the expiry sweep; one broker end to end (RabbitMQ with its container fixture) | real services, shared `[Collection]` fixtures, `[Trait("Category", "Integration")]` |

- Load: `.md` justification unless the cleanup loop shows shared state under concurrency; benchmark: implement a benchmark of the offload path for a 1 MiB payload (hot path for large messages) using `BenchmarkSwitcher.FromAssembly(...)`, results to `artifacts/performance/`.
- Update the coverage manifests of `Encina.Messaging`, the seven publisher packages and the two new packages. Measure each flag and the CRAP of the changed methods (AGENTS.md §9).

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 9</strong></summary>

```
You are implementing Phase 9 of issue #122 in Encina: the tests of the claim check feature built on the #599 abstraction.

CONTEXT:
- Per-flag coverage targets live in .github/coverage-manifest/{Package}.json; tests must execute real package code.
- Integration fixtures: RedisFixture, AzuriteFixture (Cloud-Azurite), LocalStackFixture (Cloud-LocalStack), database collections.

TASK:
Write the unit, guard, contract, property and integration tests of Phases 1-8, the benchmark, the load justification, and update
the manifests; report measured per-flag coverage and the CRAP table for the changed methods.

KEY RULES:
- Shouldly via Encina.Testing.Shouldly; FakeTimeProvider; no Thread.Sleep; no reflection-only tests.
- BenchmarkSwitcher.FromAssembly, never BenchmarkRunner.Run<T>(); never return IQueryable.
- A test never swallows an exception from the code it drives.

REFERENCE FILES:
- tests/Encina.ContractTests/MessageBrokers/ (publisher contract tests)
- tests/Encina.BenchmarkTests/
- docs/testing/coverage-measurement-methodology.md
```

</details>

---

### Phase 10: Documentation and finalization

> **Goal**: Everything the prompt lists, including the single ADR of the feature.

<details>
<summary><strong>Tasks</strong></summary>

1. XML docs on all new public APIs; `PublicAPI.Unshipped.txt` complete for every touched package.
2. `changelog.d/122-claim-check.added.md` (bullets for the attribute, compression, cleanup, publishers and the two packages).
3. ADR (next free number; 065 on 2026-11-11, reserved in the ADR index table when work starts): package home, store shape, envelope in `Content`, threshold scope, transport coverage, stores over `IBlobStore`, deletion rule, no atomicity between payload and outbox row.
4. Package READMEs for `Encina.ClaimCheck.Redis` and `Encina.ClaimCheck.Storage`; `docs/features/claim-check.md` extended from the #599 part (configuration, thresholds per broker, consumer helper, lifecycle); `docs/INVENTORY.md`; `ROADMAP.md`.
5. `dotnet build --configuration Release` with 0 warnings, `dotnet test`, every flag at its manifest target.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 10</strong></summary>

```
You are implementing Phase 10 of issue #122 in Encina: documentation and the ADR of the claim check feature.

CONTEXT:
- Documentation is written by docs-writer; the changelog fragment and PublicAPI lines by mechanical-fixer.
- docs/features/claim-check.md already holds the abstraction part from #599.

TASK:
Write the ADR, the READMEs and the guide extensions, verify every identifier in src/, and run the build and the tests.

KEY RULES:
- Never edit CHANGELOG.md; use changelog.d/122-claim-check.added.md.
- Coverage figures only through covref markers; English only; no emojis; no private client names.

REFERENCE FILES:
- .claude/skills/encina-docs/SKILL.md
- docs/architecture/adr/index.md (reserve the ADR number)
```

</details>

---

## Research

### Relevant Standards & Specifications

| Source | Reference | Relevance |
|--------|-----------|-----------|
| Enterprise Integration Patterns | Claim Check | The pattern |
| Azure Architecture Center | Claim-Check pattern; Service Bus Sample10_ClaimCheck | Comparable product; Standard tier limit 256 KB, Premium up to 100 MB |
| AWS | SQS Extended Client Library (S3-backed) | Comparable product; not portable across brokers |
| AGENTS.md §5 | Transports: the feature must be consistent across providers of the category | Scope of Phase 4 |
| AGENTS.md §3 | ROP, background loop rule, secrets in options, `TimeProvider` | Cleanup processor, Redis options |
| SPEC-000 §6.1 | Cloud triangle: AWS and Azure in 1.0, GCP post-1.0 | Blob backends through #1239 |
| ADR-018, ADR-021, ADR-050, ADR-052, ADR-055 | Cross-cutting, EventIds, closed module list, background failure contract, resilience family | Matrix, ranges, cleanup loop, decorator |

### Existing Encina Infrastructure to Leverage

| Component | Location | Usage in this feature |
|-----------|----------|-----------------------|
| IClaimCheckStore, IClaimCheckService, ClaimCheckEnvelope, InMemoryClaimCheckStore | src/Encina.Messaging/ClaimCheck/ (created by the #599 plan) | the contract this plan extends with attribute, compression, cleanup, transports and stores |
| OutboxBatchProcessor | src/Encina.Messaging/Outbox/OutboxBatchProcessor.cs:197 | after MarkAsProcessedAsync succeeds the payload is deleted |
| OutboxProcessorBase and PeriodicTimer loops | src/Encina.Messaging/Outbox/OutboxProcessorBase.cs | model for the cleanup processor loop that catches per iteration |
| DeadLetterOptions.RetentionPeriod | src/Encina.Messaging/DeadLetter/DeadLetterOptions.cs | input of the TTL validation at start-up |
| RabbitMQMessagePublisher | src/Encina.RabbitMQ/RabbitMQMessagePublisher.cs:72 | JsonSerializer.SerializeToUtf8Bytes is the offload point; the same shape exists in the other publishers |
| KafkaMessagePublisher, NATSMessagePublisher, AzureServiceBusMessagePublisher, AmazonSQSMessagePublisher, RedisPubSubMessagePublisher, MQTTMessagePublisher | src/Encina.Kafka, src/Encina.NATS, src/Encina.AzureServiceBus, src/Encina.AmazonSQS, src/Encina.Redis.PubSub, src/Encina.MQTT | transport publishers to adapt |
| StackExchange.Redis and RedisFixture | Directory.Packages.props, tests/Encina.TestInfrastructure/Fixtures/RedisFixture.cs | Redis store and its integration tests |
| AzuriteFixture and LocalStackFixture | tests/Encina.TestInfrastructure/Fixtures/ | integration tests of the blob backends (collections Cloud-Azurite and Cloud-LocalStack) |
| IBlobStore (Encina.Storage, created by the #1239 plan) | src/Encina.Storage/ | blob, file system and S3 backends for claim check payloads |
| EncryptedMessageAttributeCache and EncryptingMessageSerializer | src/Encina.Messaging.Encryption/ | static attribute cache model; encryption already applied before the offload |
| ResilientSecretReaderDecorator | src/Encina.Security.Secrets/Resilience/ | model for ResilientClaimCheckStore |
| ITenantProvider and MultiTenancyMarker | src/Encina.Tenancy/Abstractions/ITenantProvider.cs, src/Encina/Abstractions/MultiTenancyMarker.cs | tenant prefix on claim ids, fail closed |
| Encina.Messaging coverage manifest | .github/coverage-manifest/Encina.Messaging.json | per-file targets |

### Event ID Allocation

| Package | Range | Notes |
|---------|-------|-------|
| `Encina.Messaging` (claim check) | `MessagingClaimCheck` 6000-6049 | Range registered by the #599 plan; **6025-6049** used by this plan |
| `Encina.ClaimCheck.Redis` | `ClaimCheckProviders` 6150-6199 (new, registered by this plan) | **6150-6164** |
| `Encina.ClaimCheck.Storage` | `ClaimCheckProviders` 6150-6199 | **6165-6174**; 6175-6199 free |
| Publisher packages | their existing ranges (RabbitMQ 4100-4149, Kafka 4150-4199, NATS 4200-4249, Azure Service Bus 4300-4349, Amazon SQS 4350-4399, Redis Pub/Sub 4400-4449, MQTT 4250-4299) | No new IDs; the offload logs come from `Encina.Messaging` |
| Family of plans | 6050-6099 and 6100-6149 (#1239), 6200-6249 (#1250), `SecuritySecrets` 5151-5170 (#1246) | Shared reservation of the family (registry eventid-blocks.md) |

### Open Questions to Verify During Implementation

| Question | How to verify |
|----------|---------------|
| Do the other two serialization sites of the AWS and NATS publishers (request/reply, batch) need the offload? | Read each publisher method; offload only methods that publish a body to a broker |
| Do MQTT 3.1.1 brokers carry the marker? | They do not; the body prefix is the fallback, as for Redis Pub/Sub |
| Does the #1200 outbox purge return the content of the rows it deletes on all 10 providers? | Check the #1200 plan or pull request; if it does not, payloads of purged rows are reclaimed by the TTL sweeper only (choice 5) |
| Are the per-transport default thresholds of choice 9 still below each broker's documented default limit (including the current Amazon SQS quota)? | Read the broker documentation in the pull request that touches the publisher |

### Estimated File Count

| Category | Files | Notes |
|----------|-------|-------|
| New production files in Encina.Messaging (attribute, attribute cache, compression helper, cleanup processor, cleanup options, resolver, expiring store interface) | 7 | src/Encina.Messaging/ClaimCheck/ |
| New package Encina.ClaimCheck.Redis (store, options, DI, log messages, csproj, README, PublicAPI) | 7 | |
| New package Encina.ClaimCheck.Storage (adapter, DI, csproj, README, PublicAPI) | 5 | |
| Modified transport publishers | 7 | RabbitMQ, Kafka, NATS, AzureServiceBus, AmazonSQS, Redis.PubSub, MQTT |
| Modified existing files (OutboxBatchProcessor, ClaimCheckService, ClaimCheckOptions, ClaimCheckErrors, EventIdRanges, EncinaEventIdAllocationTests) | 6 | |
| Unit tests | 14 | |
| Guard tests | 4 | |
| Contract tests | 4 | store contract subclasses and transport contract |
| Property tests | 2 | |
| Integration tests | 6 | Redis, file system, Azurite via Storage, LocalStack via Storage, outbox end to end, one broker |
| Benchmark and load justification files | 2 | |
| Documentation | 7 | feature guide extension, ADR, two READMEs, changelog fragment, ROADMAP line, INVENTORY |
| Coverage manifest edits | 10 | Messaging, seven publisher packages, two new packages |
| Total | 81 | Distinct files; the per-PR table under Implementation Phases counts 124 file changes over 11 pull requests, because some files (`ClaimCheckService`, the Messaging `PublicAPI.Unshipped.txt`, its manifest) change in several |

---

## Combined AI Agent Prompts

<details>
<summary><strong>Full combined prompt for all phases</strong></summary>

```
PROJECT CONTEXT:
You are implementing issue #122 in Encina (.NET 10, C# 14, nullable enabled, pre-1.0, no backward compatibility): the part of the
Claim Check feature that sits on top of the abstraction of issue #599 (its plan is linked from #599). Rules: AGENTS.md.

IMPLEMENTATION OVERVIEW:
1. [ClaimCheck] attribute (ThresholdBytes, Mode, Store) and its static cache.
2. Optional gzip compression with the envelope flags field; skip content starting with ENC:v1:.
3. Cleanup: IExpiringClaimCheckStore, ClaimCheckCleanupProcessor (PeriodicTimer, catch per iteration), deletion after the processed
   mark and by dead-letter expiry (the outbox purge is #1200, not built here); TTL validated against the retry and dead-letter windows.
4. Seven broker publishers (RabbitMQ, Kafka, NATS, Azure Service Bus, Amazon SQS, Redis Pub/Sub, MQTT) offload after serialization
   with a per-transport threshold (broker limit minus 64 KiB) and a required transport TTL, and set a mandatory header marker where
   the broker has headers; consumers must call the public resolver.
5. Encina.ClaimCheck.Redis and Encina.ClaimCheck.Storage (adapter over IBlobStore from the #1239 plan); keyed registration.
6. Resilience decorator, health checks, EventIds (MessagingClaimCheck 6025-6049, ClaimCheckProviders 6150-6199), metrics, spans.
7. Tests per flag (+ transport contract, benchmark), docs, the single ADR.

KEY PATTERNS:
- Either<EncinaError, T>; errors never swallowed; no EncinaError.Message in logs; secrets in options are [JsonIgnore] with ToString().
- Optional dependencies keep every transport unchanged when the feature is off; opt-in registration with TryAdd.
- TimeProvider everywhere; CancellationToken on every async call; background loops catch per iteration.
- EventIds registered first and packed; ranges of the five plans of this family do not overlap.
- Tests execute real code; per-flag targets; CRAP <= 10 on changed methods; shared [Collection] fixtures.

REFERENCE FILES:
- the plans of #599 and #1239 (linked from those issues)
- src/Encina.Messaging/Outbox/OutboxBatchProcessor.cs, src/Encina.Messaging/DeadLetter/DeadLetterOptions.cs
- src/Encina.RabbitMQ/RabbitMQMessagePublisher.cs (and the six other publishers)
- src/Encina.Security.Secrets/Resilience/ResilientSecretReaderDecorator.cs
```

</details>

---

## Cross-Cutting Integration Matrix

| # | Function | Status | Notes |
|---|----------|--------|-------|
| 1 | Caching | ⏭️ | Retrieved payloads could be cached; the follow-up issue is opened by the orchestrator before the first worker brief of #599 (see #599's Next Steps) and its number is written here |
| 2 | OpenTelemetry | ✅ | Phase 8: delete, purge and transport counters, spans `encina.claimcheck.delete` and `encina.claimcheck.purge` |
| 3 | Structured Logging | ✅ | Phase 8: EventIds 6025-6049 and 6150-6199, registered first (ADR-021) |
| 4 | Health Checks | ✅ | Phase 7: Redis and Storage backend checks; default-store check from #599 |
| 5 | Validation | ✅ | Options validated at start-up, including the TTL against retry and dead-letter windows (Phase 3); attribute values validated |
| 6 | Resilience | ✅ | Phase 7: `ResilientClaimCheckStore` for Redis; the Storage adapter inherits `IBlobStore` resilience |
| 7 | Distributed Locks | ❌ | The sweeper is idempotent (delete by expiry), so several hosts need no lock |
| 8 | Transactions | ⏭️ | Payload and outbox row are two systems; no atomicity is offered. Orphans are reclaimed by TTL; recorded in the ADR. No issue needed |
| 9 | Idempotency | ❌ | A redelivered message resolves the same stored payload; no deduplication is added by this feature |
| 10 | Multi-Tenancy | ✅ | Inherited from `ClaimCheckService` (#599); the Storage adapter relies on `IBlobStore` partitioning |
| 11 | Module Isolation | ❌ | SPEC-002 requires no module scoping; `ModuleId` in messaging stays with #747 |
| 12 | Audit Trail | ⏭️ | Payload access audit; the follow-up issue is opened by the orchestrator before the first worker brief of #599 (function 12 there) and its number is written here |

---

## Prerequisites & Dependencies

### Advisable before or with this work

- The #599 plan, PR 1 (contract, service, in-memory store): required by every phase here.
- The #1239 plan (`Encina.Storage`): required by Phase 5 for the blob backends only; Phases 1-4 and the Redis store do not wait for it.
- #1250 (column-level encryption, owner of `IKeyWrapper`), through #1239: `Encina.Storage` turns envelope encryption on by default and, in its Phase 7, fails start-up when no `IKeyWrapper` is registered unless `AllowUnencrypted` is set explicitly (and logged). `Encina.ClaimCheck.Storage` inherits that rule, so every blob backend of this plan needs #1250's key wrapper or the explicit opt-out; the feature guide states it and the integration tests of PR 11 register a key wrapper.
- #1200 (outbox purge on the 10 database providers): the purge deletion point of choice 5 is delivered there, not here; this plan works without it.

### Parallel and follow-up work

- #600 (cloud blob implementations of `IClaimCheckStore`): covered by the adapter over `IBlobStore`; close it with the adapter's pull request if the maintainer picks option B of choice 3.
- Google Cloud Storage: post-1.0 (SPEC-000 §6.1), noted as the cloud triangle gap.
- gRPC and GraphQL: a follow-up issue if the maintainer wants them covered (choice 2).

---

## Next Steps

1. The maintainer answers the Dilemmas of this plan (choices 2 to 10) and the four shared with the #599 plan once; the orchestrator adds the Maintainer Decisions section to both plans.
2. Link this plan from #122 and the #599 plan from #599.
3. Start with the #599 pull request 1; then this plan's PR 1 and PR 2; the transport pull requests (PR 3 first, then PR 4-6 in parallel) need PR 1; PR 8-9 need PR 7; PR 10-11 need #1239.
4. Hand the #1200 plan the requirement of choice 5 (the outbox purge returns the envelopes it deletes on all 10 providers).
5. Open the ADR number reservation in `docs/architecture/adr/index.md` when work starts.
