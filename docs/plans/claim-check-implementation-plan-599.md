# Implementation Plan: Claim Check Abstraction — `IClaimCheckStore`, In-Memory Store and Outbox Auto-Offload

> **Issue**: [#599](https://github.com/dlrivada/Encina/issues/599) (`[FEATURE] IClaimCheckStore abstraction`; the abstraction slice of the Claim Check feature owned by [#122](https://github.com/dlrivada/Encina/issues/122))
> **Type**: Feature
> **Complexity**: Medium-High (new contract in `Encina.Messaging`, outbox integration, opt-in registration, observability; 7 pull requests)
> **Estimated Scope**: ~1,100-1,500 lines of production code + ~1,800-2,300 lines of tests
> **Sibling plan**: #122 (attribute, compression, cleanup, transports, stores, ADR)

---

## Summary

Define the Claim Check pattern (EIP) as an opt-in part of `Encina.Messaging`: a store contract that keeps a large payload outside the message, a small reference that travels instead, a service that decides by size when to offload and checks integrity when the payload comes back, an in-memory store for tests, and the first integration point, the outbox. #599 and #122 are one feature. This plan owns the abstraction slice; the #122 plan (#122) owns everything built on top of it. The names below are shared by both plans and must not change in one without the other.

### Who owns what

| Piece | Owner | Name or location |
|-------|-------|------------------|
| Namespace and folder | #599 | `Encina.Messaging.ClaimCheck`, `src/Encina.Messaging/ClaimCheck/` (choice 1) |
| Store contract | #599 | `IClaimCheckStore` (bytes and streams, `Either` results; choice 2) |
| Reference and metadata records | #599 | `ClaimCheckReference` (the issue's `ClaimCheck` record, renamed; choice 3), `ClaimCheckMetadata` |
| Envelope in `Content` | #599 | `ClaimCheckEnvelope` (`CLC:v1:...`; choice 4) |
| Offload and resolve service | #599, extended by #122 | `IClaimCheckService`, `ClaimCheckService` (threshold and checksum here; attribute policy and compression in #122) |
| In-memory store | #599 | `InMemoryClaimCheckStore` |
| Opt-in flag and options | #599 | `MessagingConfiguration.UseClaimCheck`, `ClaimCheckOptions` (choice 5 for the threshold) |
| Outbox offload and resolve | #599 | `OutboxOrchestrator.AddAsync`, `OutboxBatchProcessor` |
| Contract test base class | #599 | `ClaimCheckStoreContract`, subclassed by every store of #122 |
| EventIds | #599 and #122 | `MessagingClaimCheck` 6000-6049: **6000-6024 here**, 6025-6049 in #122 |
| `[ClaimCheck]` attribute, compression, cleanup and payload deletion | #122 | |
| Transport publishers (10) and consumer-side resolve | #122 | |
| Stores (Redis, adapter over `IBlobStore`) and the ADR | #122 | ADR number is taken by #122 |

### What the code does today (2026-11-11, `main` at `7a0049c07`)

Nothing of #599 or #122 exists: `src/` has no type or package named `ClaimCheck`, and `src/Encina.Messaging` has no such folder. Facts that shape the design:

1. **The outbox stores the payload as a string.** `IOutboxMessage.Content` is a `string` (`src/Encina.Messaging/Outbox/IOutboxMessage.cs`). `OutboxOrchestrator.AddAsync` calls `_messageSerializer.Serialize(notification)` (`OutboxOrchestrator.cs:96`) and `OutboxBatchProcessor` calls `_serializer.Deserialize(message.Content, notificationType)` (`OutboxBatchProcessor.cs:182`). Both are the natural hooks, and both are async methods, so an async store call fits there.
2. **The serializer is the wrong seam.** `IMessageSerializer.Serialize` and `Deserialize` are synchronous and return `string` (`src/Encina.Messaging/Serialization/IMessageSerializer.cs`). A store call is I/O, so a serializer decorator would block a thread. The offload therefore happens after the serializer chain, which also means that an `EncryptingMessageSerializer` (`src/Encina.Messaging.Encryption/Serialization/EncryptingMessageSerializer.cs`, output prefix `ENC:v1:`) has already encrypted the content when it is stored.
3. **The outbox publishes in process.** `OutboxProcessorBase` resolves `IEncina` and calls `Publish` (`OutboxProcessorBase.cs:126`). The broker size limit is therefore not hit by the outbox row. It is hit by the transport publishers, which serialize on their own (for example `RabbitMQMessagePublisher.cs:72`, `JsonSerializer.SerializeToUtf8Bytes(message)`), and there is no common transport interface in `src/`. The transport integration is owned by the #122 plan. The outbox integration of this plan serves the other limits that also motivate the pattern: row size and replay cost in the database (MongoDB documents are limited to 16 MiB; the relational columns are `LONGTEXT`, `TEXT` or `NVARCHAR(MAX)`), and the cost of keeping large payloads in a hot table.
4. **Replay copies `Content` as it is** (`OutboxBatchProcessor.cs:389-392`), so a replayed or dead-lettered message keeps pointing at the same stored payload. The #122 plan handles the lifetime consequences.
5. **Opt-in patterns follow one shape.** `MessagingConfiguration` has one `Use*` flag per pattern (`src/Encina.Messaging/MessagingConfiguration.cs`), registered in `MessagingServiceCollectionExtensions.cs`. The `Messaging` EventId range (2800-2999) is almost full (2989-2999 left after the plans of #718, #1200 and #1251), so claim check takes a new range in the block 6000-6249 allotted to the claim check, storage and encryption plans.

### Scope

- **Affected packages**: `Encina.Messaging` only. No provider package changes: the reference lives inside `Content`, so no schema changes on the 10 database providers (choice 4).
- **Provider category**: none for this plan (provider-independent contract). The store backends (blob, file system, Redis) belong to #122, which also records the 10-provider rule if database stores are ever added.
- **Out of scope**: attribute, compression, cleanup, transports, stores, ADR (all #122).
- **"Pipeline auto-offload" of the issue**: this plan offloads on the outbox path only. The issue's wider "message pipeline" (the transport publishers and the consumer-side resolve) is delivered by the #122 plan on top of `IClaimCheckService`.
- **Database stores (the issue's D3)**: not addressed here; whether claim check payloads may live in the 10 database providers is choice 4 of the #122 plan, which then carries the 10-provider rule.
- **Estimated files**: ~15 production files created, 7 modified, ~20 test files, ~5 documentation files. Seven pull requests, each with its own tests (see the pull request cut under Implementation Phases).

---

## Design Choices

<details>
<summary><strong>1. Home of the abstraction — a folder of <code>Encina.Messaging</code> or a new <code>Encina.ClaimCheck</code> package</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Folder `src/Encina.Messaging/ClaimCheck/`** | The outbox integration needs no new package edge; follows how every other pattern ships (`Outbox/`, `Inbox/`, `Sagas/`, `Scheduling/`); adopters add no package for the in-memory store; the satellite stores of #122 reference one package | Every `Encina.Messaging` user carries about ten small types they do not use (no runtime cost: nothing is registered unless `UseClaimCheck` is set) |
| **B) New package `Encina.ClaimCheck`** (the first draft of #122) | Pay-for-what-you-use at package level: an application that never uses the pattern never references it | The outbox needs a hook that the new package fills, so `Encina.Messaging` would define an extension seam only this package uses; one more package to version, document and register |

### Chosen Option: **A — folder of `Encina.Messaging`** (recommended, pending the maintainer)

### Rationale

**Class:** Dilemma. Option A wins when the pattern is expected to be used mostly with the outbox and the messaging stack is already a dependency. Option B wins when package-level opt-in matters more than one extra seam, for instance an application that uses `Encina.Messaging` only for sagas and wants no claim check types in its dependency graph.

- We recommend A because opt-in in Encina is a runtime switch (`Use*` flags, AGENTS.md §3), the types are small, and the existing patterns set the precedent.
- The issue #599 proposes A, the first draft of #122 proposes B. Both issues are one feature, so the maintainer answers once and both plans follow.

</details>

<details>
<summary><strong>2. Store shape — <code>byte[]</code> and <code>Stream</code> with <code>Either</code>, or typed <code>T</code></strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Typed `StoreAsync<T>` / `RetrieveAsync<T>`** (the first draft of #122) | The caller works with message objects and picks the serializer once | The store depends on a serializer; the whole object is materialized on both sides; streaming is impossible |
| **B) `ReadOnlyMemory<byte>` and `Stream` in, `Either<EncinaError, ...>` out** (#599) | The store is independent of `IMessageSerializer` (typing stays where it is today); streaming avoids buffering a large payload twice; every backend (blob, file, Redis) is naturally bytes | The caller serializes and deserializes itself (the service of Phase 3 does it for the outbox) |

### Chosen Option: **B — bytes and streams, `Either` results** (recommended, pending the maintainer)

### Rationale

**Class:** Dilemma. Option B wins for very large payloads and for decorators that must not know message types (compression, encryption, checksum). Option A wins in application code that offloads a single object type and wants no serializer in sight.

- We recommend B because the serializer chain already exists and is shared with the outbox and inbox; a typed store would duplicate it and would still need a byte-level path for streaming.
- Results are `Either<EncinaError, T>` and not `Task<T?>` (see choice 6). A missing payload is `Left` with `claimcheck.not_found`, not `null`.
- A typed convenience can be added later as an extension method over the byte contract without changing it.

</details>

<details>
<summary><strong>3. Naming of the reference record — <code>ClaimCheckReference</code></strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `ClaimCheckReference`** | No clash with the namespace `Encina.Messaging.ClaimCheck`; says what the value is | Differs from the issue's sketch (`ClaimCheck`) |
| **B) Keep `ClaimCheck` as the record name** | Matches the issue text | A type with the same name as its namespace triggers naming warnings (CA1724) and makes `ClaimCheck.ClaimCheck` ambiguous in `using` code |

### Chosen Option: **A — `ClaimCheckReference`**

### Rationale

**Class:** Trivial. **Settled by:** AGENTS.md §8 (every CA warning is fixed, and a naming warning is fixed by renaming, as CA1716 is); AGENTS.md §4 (descriptive names).

- The record carries `Id`, `SizeBytes`, `CreatedAtUtc`, `ExpiresAtUtc` and `Sha256` (hex). Timestamps use the `AtUtc` suffix (AGENTS.md §4) and come from `TimeProvider` (AGENTS.md §3).
- The implementation verifies the name with a build before it is published in `PublicAPI.Unshipped.txt`.

</details>

<details>
<summary><strong>4. Where the reference lives in the outbox row — an envelope inside <code>Content</code>, or new columns</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) A text envelope stored in `Content`**: `CLC:v1:{storeKey}:{flags}:{sizeBytes}:{sha256}:{id}` (`flags` is `-` here; the #122 plan adds `gz`; same idea as `ENC:v1:` in `EncryptedPayloadFormatter`) | No schema change on the 10 database providers; no change to `IOutboxMessage` or its 8 provider classes; works for MongoDB and every relational store the same way; old rows stay readable | The cleanup job (#122) cannot find "rows that hold a reference" with a column query; it finds payloads by TTL or by the processing path instead |
| **B) New `ClaimCheckReference` and `ClaimCheckProvider` fields** on `IOutboxMessage` and its provider classes (the first draft of #122) | Cheap query for "rows holding a reference" | A schema change and a new interface member on 8 classes and the DDL scripts of 10 providers; ordinary outbox rows pay for two nullable columns |

### Chosen Option: **A — envelope inside `Content`** (recommended, pending the maintainer)

### Rationale

**Class:** Dilemma. Option A wins when no provider schema may change for an opt-in pattern. Option B wins when an operator needs to query or purge by reference with plain SQL, for instance a compliance sweep that must list every payload held for one tenant without reading the payloads.

- We recommend A because pay-for-what-you-use (AGENTS.md §1) means a pattern an application did not enable costs it nothing, and a column on every outbox table is a cost for everyone.
- The envelope is parsed by `ClaimCheckEnvelope.TryParse`; content that does not start with `CLC:v1:` is passed through untouched, so the feature can be switched on in a running system.
- If the maintainer picks B, Phase 5 gains the schema work of the row-claiming plan (#1251) as a model: 10 providers, DDL in `Scripts/`, and the #122 cleanup uses the column.

</details>

<details>
<summary><strong>5. Where the threshold applies — every outbox message, or only messages with <code>[ClaimCheck]</code></strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Automatic: with `UseClaimCheck`, every outbox message whose serialized size is above `ClaimCheckOptions.ThresholdBytes` is offloaded** (#599) | Applications annotate nothing; the safe default for payloads that vary in size | A message type that should never be offloaded needs an opt-out (the `[ClaimCheck]` attribute of #122 gives `Mode = Never`) |
| **B) Only messages carrying `[ClaimCheck]`** (#122) | Offloading is an explicit per-message decision with its own threshold | Large messages without the attribute still hit the limits; every message type must be reviewed |

### Chosen Option: **A — automatic above the global threshold; the attribute of #122 refines it per type** (recommended, pending the maintainer)

### Rationale

**Class:** Dilemma. Option A wins when payload sizes are unpredictable. Option B wins in a codebase where only two or three message types are ever large and offloading must stay visible in the type definition.

- We recommend A with the attribute as a refinement (per-type threshold, `Always`, `Never`), because it covers both scenarios and the attribute work is already in #122.
- The default value of the threshold is choice 8. On the outbox path no broker limit applies (fact 3), so the threshold there serves row size and replay cost; the broker-driven threshold is a concern of the #122 transport integration.

</details>

<details>
<summary><strong>6. Result type, activation and failure semantics</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `Either<EncinaError, T>` everywhere; opt-in `UseClaimCheck`; a store failure fails the outbox operation** | Follows the repository rules; the failure goes to retry and dead letter like any other `Left` | The first-draft signatures change |
| **B) Exceptions for store failures, returning plain values** | Simpler signatures | Violates ROP; a thrown exception inside the processor loop is exactly what AGENTS.md §3 forbids from escaping |

### Chosen Option: **A — `Either`, opt-in, failures fail the operation**

### Rationale

**Class:** Settled. **Settled by:** AGENTS.md §3 (Railway Oriented Programming, ADR-001, ADR-006; errors are never swallowed in background infrastructure; every messaging pattern is optional and disabled by default); ADR-050 (claim check is not in the closed list of required modules).

- `UseClaimCheck = true` without a registered `IClaimCheckStore` is a start-up error, never a silent default (AGENTS.md §3, registration completeness; ADR-050 rule 3). The in-memory store is registered explicitly (`AddEncinaClaimCheckInMemory()`), never implicitly.
- A `Left` from the store during offload fails `AddAsync`; a `Left` during resolve marks the outbox message failed (retry, then dead letter). Neither reports success.
- Errors never reach logs through `EncinaError.Message` (AGENTS.md §3): only `claimcheck.*` codes and the exception type.

</details>

<details>
<summary><strong>7. Default payload lifetime — no TTL until the #122 cleanup, or 7 days with a retry and dead-letter check here</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `ClaimCheckOptions.DefaultTimeToLive = null` (no expiry by default)** | A retried, dead-lettered or replayed outbox row always resolves: replay copies the envelope in `Content` as it is (`OutboxBatchProcessor.cs:389-392`), so the payload must outlive the row; no cross-option validation is needed in this slice | Payloads accumulate until the #122 cleanup deletes them; in this slice the only store is the in-memory one, bounded by its cap (choice 8), so no production store grows unbounded before #122 ships |
| **B) 7 days by default, plus a start-up validation in this plan** that rejects a `DefaultTimeToLive` shorter than the outbox's worst-case retry window plus `DeadLetterOptions.RetentionPeriod` (default 7 days, `null` disables expiry) | Space is reclaimed without the #122 cleanup | The validation must read the outbox and dead-letter options and reject the combination `RetentionPeriod = null` with any TTL; with the defaults of both, a message dead-lettered after its retries is still within its dead-letter retention when its 7-day payload has already expired (`claimcheck.not_found` on replay) |

### Chosen Option: **A — no default TTL** (recommended, pending the maintainer)

### Rationale

**Class:** Dilemma. Option A wins when every outbox row must stay replayable for as long as it exists. Option B wins when storage space matters more and the operator accepts that the TTL must be kept above the retry and dead-letter windows, enforced at start-up.

- We recommend A because a payload that expires before its row turns a recoverable dead letter into permanent data loss, and the TTL-versus-retention validation of the #122 plan (its PR 2) ships after this slice. When #122 delivers the cleanup and the validation, it may introduce a default TTL.
- An explicit `ClaimCheckMetadata.TimeToLive` or `DefaultTimeToLive` set by the application is honoured; the XML doc of `DefaultTimeToLive` states the replay risk of a value shorter than the dead-letter retention.

</details>

<details>
<summary><strong>8. Size defaults — threshold, maximum payload and in-memory cap</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `ThresholdBytes` = 256 KiB, `MaxPayloadBytes` = 100 MiB, `InMemoryClaimCheckStoreOptions.MaxTotalBytes` = 256 MiB** | One threshold serves the outbox and the #122 transports alike: 256 KiB is the Azure Service Bus Standard message limit, the smallest among the brokers of the transport matrix, and on the outbox it keeps rows small for replay and for the hot table; 100 MiB matches the largest broker message (Azure Service Bus Premium) and bounds the memory the service uses for the UTF-8 copy and the SHA-256 of one payload; the in-memory cap protects a test or development process | The threshold offloads payloads an outbox table could hold inline (MongoDB documents allow 16 MiB, the relational columns are unbounded text) |
| **B) A larger outbox threshold (for example 1 MiB) and no `MaxPayloadBytes` cap** | Fewer offloads on the outbox path; no artificial limit | The #122 transports need a second, lower threshold; an unbounded payload can exhaust memory in the service, which buffers the whole string |

### Chosen Option: **A — 256 KiB, 100 MiB, 256 MiB** (recommended, pending the maintainer)

### Rationale

**Class:** Dilemma. Option A wins when one configuration should serve the outbox and the transports. Option B wins in an outbox-only application with large but bounded payloads that would rather keep them inline.

- All three values are options, validated by `ClaimCheckOptions.Validate` (`ThresholdBytes > 0`, `MaxPayloadBytes >= ThresholdBytes`) and `InMemoryClaimCheckStoreOptions` (`MaxTotalBytes > 0`).
- The in-memory cap only protects tests and local development: the in-memory store is never a production default (choice 6).
- A payload above `MaxPayloadBytes` is `Left(claimcheck.payload_too_large)`, never truncated.

</details>

<details>
<summary><strong>9. Tenant source — the ambient request context of core, not <code>ITenantProvider</code></strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `IRequestContextAccessor.RequestContext?.TenantId` plus `MultiTenancyMarker`** (both in the core package `Encina`) | `Encina.Messaging` already references `Encina` (its only project reference); same mechanism as `DeadLetterManager` in the same package (`src/Encina.Messaging/DeadLetter/DeadLetterManager.cs`, `AmbientTenantId()` and `TenantMissing()`) | None found |
| **B) `ITenantProvider` from `Encina.Tenancy`** | Direct tenant API | `Encina.Tenancy` references `Encina.Messaging` (`src/Encina.Tenancy/Encina.Tenancy.csproj`), so the reverse reference creates a project cycle and does not build |

### Chosen Option: **A — ambient request context and `MultiTenancyMarker`**

### Rationale

**Class:** Settled. **Settled by:** the project graph (option B is a reference cycle, so it is not a valid option) and the precedent of `DeadLetterManager` in the same package.

</details>

<details>
<summary><strong>10. Offload without an ambient tenant while multi-tenancy is on</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Fail closed: `Left(claimcheck.tenant_required)` from `OffloadAsync`, so `AddAsync` fails** | No payload is stored outside a tenant scope by accident; same behavior as `DeadLetterManager` (`TenantMissing()`) | A system publish (a background job without a request context) of a payload above the threshold fails while tenancy is on; such a job must set the tenant in the request context or the payload must stay below the threshold |
| **B) Store under an explicit system scope when no tenant resolves** | System publishes keep working | A request that lost its tenant by mistake stores the payload in the shared scope instead of failing, which is the fail-open behavior AGENTS.md §3 forbids for security gates |

### Chosen Option: **A — fail closed** (recommended, pending the maintainer)

### Rationale

**Class:** Dilemma. AGENTS.md §3 makes compliance and security gates fail closed; the tenant segment of a claim id is an isolation boundary, but whether a size-based offload is a gate is a judgement, and option B with an explicit, logged system scope is the "explicit opt-out" the same rule allows. Option A wins when every message must belong to a tenant; option B wins when tenant-less system messages are a normal part of the application.

- With tenancy off (no `MultiTenancyMarker`) no tenant segment is added and no tenant is required.
- The tenant segment is the URL-safe Base64 (no padding) of the UTF-8 tenant id followed by `.`; the Base64 alphabet contains neither `.` nor `:`, so the prefix is injective (no two tenants share a prefix) and never breaks the `:`-separated envelope.

</details>

---

## Implementation Phases

### Pull request cut

Seven pull requests, merged in order. Every pull request carries the tests of the code it adds (Phase 8 is the per-flag target table each one applies, not a separate pull request) and its own `.github/coverage-manifest/Encina.Messaging.json` targets. The counts include tests; none exceeds 15 files or 1,500 changed lines (AGENTS.md §11). Paths without a folder are under `src/Encina.Messaging/ClaimCheck/`.

| PR | Phases | Files | Est. lines |
|----|--------|-------|-----------|
| 1 | 1: value types, errors, envelope | `ClaimCheckReference.cs`, `ClaimCheckMetadata.cs`, `ClaimCheckErrors.cs`, `ClaimCheckEnvelope.cs`, `src/Encina.Messaging/PublicAPI.Unshipped.txt`; tests: unit `ClaimCheckEnvelopeTests`, `ClaimCheckErrorsTests`, guard `ClaimCheckEnvelopeGuardTests`, property `ClaimCheckEnvelopeProperties`; manifest; `changelog.d/599-claim-check-abstraction.added.md` (11) | ~650 |
| 2 | 2: store contract, in-memory store | `IClaimCheckStore.cs`, `InMemoryClaimCheckStore.cs`, `InMemoryClaimCheckStoreOptions.cs`, PublicAPI; tests: unit `InMemoryClaimCheckStoreTests`, guard `InMemoryClaimCheckStoreGuardTests`, contract `ClaimCheckStoreContract` and `InMemoryClaimCheckStoreContractTests`, property `InMemoryClaimCheckStoreProperties`; manifest (10) | ~1,000 |
| 3 | 7: EventIds and diagnostics | `src/Encina/Diagnostics/EventIdRanges.cs`, `src/Encina/PublicAPI.Unshipped.txt`, `tests/Encina.UnitTests/Testing/Architecture/EncinaEventIdAllocationTests.cs`, `src/Encina.Messaging/Diagnostics/ClaimCheckLog.cs`, `ClaimCheckActivitySource.cs`, `ClaimCheckMetrics.cs`, Messaging PublicAPI; tests: unit `ClaimCheckLogTests`, `ClaimCheckTelemetryTests`; manifest (10) | ~600 |
| 4 | 3: options, service | `ClaimCheckOptions.cs`, `IClaimCheckService.cs`, `ClaimCheckService.cs`, `ClaimCheckRequest.cs`, `ClaimCheckMode.cs`, PublicAPI; tests: unit `ClaimCheckServiceTests`, `ClaimCheckOptionsTests`, guard `ClaimCheckServiceGuardTests`, property `ClaimCheckServiceProperties`; manifest (11) | ~1,300 |
| 5 | 4 and 6: configuration, DI, start-up check, health check | `src/Encina.Messaging/MessagingConfiguration.cs`, `src/Encina.Messaging/MessagingServiceCollectionExtensions.cs`, `ClaimCheckServiceCollectionExtensions.cs`, `ClaimCheckStartupCheck.cs`, `ClaimCheckHealthCheck.cs`, PublicAPI; tests: unit `ClaimCheckRegistrationTests`, `ClaimCheckStartupCheckTests`, `ClaimCheckHealthCheckTests`, guard `ClaimCheckRegistrationGuardTests`; manifest; changelog fragment bullet (12) | ~1,100 |
| 6 | 5: outbox integration | `src/Encina.Messaging/Outbox/OutboxOrchestrator.cs`, `src/Encina.Messaging/Outbox/OutboxBatchProcessor.cs`; tests: unit `OutboxOrchestratorClaimCheckTests`, `OutboxBatchProcessorClaimCheckTests`, integration `OutboxClaimCheckIntegrationTests`; manifest; changelog fragment bullet (7) | ~900 |
| 7 | 8 (justifications) and 9: documentation | load and benchmark `.md` justifications (2), `docs/features/claim-check.md`, `src/Encina.Messaging/README.md`, `docs/INVENTORY.md` (5) | ~500 |

Phase 7 (diagnostics) lands before Phase 3 (service) so the service logs through registered EventIds from its first pull request.

### Phase 1: Core models, errors and envelope

> **Goal**: The value types and the text envelope, with no I/O.

<details>
<summary><strong>Tasks</strong></summary>

1. **`src/Encina.Messaging/ClaimCheck/ClaimCheckReference.cs`**: `public sealed record ClaimCheckReference(string Id, long SizeBytes, DateTime CreatedAtUtc, DateTime? ExpiresAtUtc, string Sha256)`.
2. **`ClaimCheckMetadata.cs`**: `public sealed record ClaimCheckMetadata(string? ContentType = null, string? CorrelationId = null, TimeSpan? TimeToLive = null, IReadOnlyDictionary<string, string>? Tags = null)`. Tags never carry payload text or personal data (XML doc).
3. **`ClaimCheckErrors.cs`**: codes `claimcheck.not_found`, `claimcheck.store_failed`, `claimcheck.retrieve_failed`, `claimcheck.integrity_failed`, `claimcheck.payload_too_large`, `claimcheck.invalid_envelope`, `claimcheck.tenant_required`; factory methods returning `EncinaError` (pattern: `SecretsErrors` in `src/Encina.Security.Secrets/SecretsErrors.cs`).
4. **`ClaimCheckEnvelope.cs`**: `internal static` formatter and parser: `string Format(string storeKey, string flags, ClaimCheckReference reference)` and `bool TryParse(string content, out ClaimCheckEnvelopeData data)`. Prefix `CLC:v1:`; fields separated by `:`; the id may not contain `:` (validated). Parsing is allocation-light and never throws.
5. **`PublicAPI.Unshipped.txt`** lines for the public types.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 1</strong></summary>

```
You are implementing Phase 1 of issue #599 in Encina: the claim check value types, error codes and envelope.

CONTEXT:
- New folder src/Encina.Messaging/ClaimCheck/ in the existing package Encina.Messaging (namespace Encina.Messaging.ClaimCheck).
- The envelope format copies the idea of ENC:v1: in src/Encina.Messaging.Encryption/EncryptedPayloadFormatter.cs.
- Error factories follow src/Encina.Security.Secrets/SecretsErrors.cs.

TASK:
Create ClaimCheckReference, ClaimCheckMetadata, ClaimCheckErrors and the internal ClaimCheckEnvelope (Format, TryParse),
with XML docs on every public member and unit tests (round trip, malformed input, ':' in id, null guards).

KEY RULES:
- Timestamps end in AtUtc; no DateTime.UtcNow.
- TryParse never throws and passes non-envelope content through as "not an envelope".
- Error codes use the claimcheck.* prefix; no EncinaError.Message in logs.
- Add the public symbols to PublicAPI.Unshipped.txt; zero warnings.

REFERENCE FILES:
- src/Encina.Messaging.Encryption/EncryptedPayloadFormatter.cs
- src/Encina.Security.Secrets/SecretsErrors.cs
- docs/plans/claim-check-implementation-plan-599.md (this plan, Design Choices 3 and 4)
```

</details>

---

### Phase 2: Store contract and in-memory store

> **Goal**: `IClaimCheckStore` and a deterministic in-memory implementation for tests and local development.

<details>
<summary><strong>Tasks</strong></summary>

1. **`IClaimCheckStore.cs`**: the six methods of choice 2: `StoreAsync(ReadOnlyMemory<byte>, ClaimCheckMetadata, CancellationToken)`, `StoreAsync(Stream, ...)`, `RetrieveAsync(string, ...)`, `RetrieveStreamAsync(string, ...)`, `DeleteAsync(string, ...)` (idempotent: deleting a missing id is `Right`), `ExistsAsync(string, ...)`, each returning `ValueTask<Either<EncinaError, T>>`.
2. **`InMemoryClaimCheckStore.cs`**: `ConcurrentDictionary<string, Entry>`; constructor takes `TimeProvider` (default `TimeProvider.System`) and `IOptions<InMemoryClaimCheckStoreOptions>` (`MaxTotalBytes`, default 256 MiB per choice 8, `Left(payload_too_large)` beyond it). Entries with an `ExpiresAtUtc` are evicted lazily on access once expired; entries without one (the default, choice 7) stay until `DeleteAsync`. The id is a new `Guid` in `"N"` format. SHA-256 is computed on store.
3. **`InMemoryClaimCheckStoreOptions.cs`**.
4. XML docs state: "for tests and local development; not a production default" (registration is explicit, choice 6).

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 2</strong></summary>

```
You are implementing Phase 2 of issue #599 in Encina: IClaimCheckStore and InMemoryClaimCheckStore.

CONTEXT:
- Phase 1 created ClaimCheckReference, ClaimCheckMetadata and ClaimCheckErrors in src/Encina.Messaging/ClaimCheck/.
- Results are ValueTask<Either<EncinaError, T>> (LanguageExt); a missing payload is Left(claimcheck.not_found).

TASK:
Create IClaimCheckStore (six methods, XML docs with <example>) and InMemoryClaimCheckStore with TimeProvider-based TTL,
a total-size cap and SHA-256 on store, plus unit, guard and property tests (round trip of random payloads).

KEY RULES:
- Time comes from TimeProvider injected through the constructor; tests use FakeTimeProvider.
- DeleteAsync of a missing id is Right (idempotent); RetrieveAsync of an expired entry is Left(not_found).
- The store is thread safe; no static state; Stream overloads copy into a pooled buffer and respect the cancellation token.
- Add an abstract ClaimCheckStoreContract test base (store/retrieve/delete/exists/not_found/ttl) that Phase 8 and the #122 stores reuse.

REFERENCE FILES:
- src/Encina.Messaging/Outbox/IOutboxStore.cs (store contract style)
- src/Encina.Testing.Fakes/ (FakeTimeProvider usage in existing store tests)
- tests/Encina.ContractTests/ (existing contract test layout)
```

</details>

---

### Phase 3: `ClaimCheckService` and options

> **Goal**: One service that decides by size, offloads, resolves and verifies. The outbox (Phase 5) and the transports of #122 use only this service.

<details>
<summary><strong>Tasks</strong></summary>

1. **`ClaimCheckOptions.cs`**: `int ThresholdBytes` (default 262_144 per choice 8, must be > 0), `TimeSpan? DefaultTimeToLive` (default `null`, no expiry, per choice 7; when set it must be > 0), `string DefaultStoreKey` (default `"default"`), `bool VerifyChecksumOnRetrieve` (default true), `int MaxPayloadBytes` (default 100 MiB per choice 8, must be >= `ThresholdBytes`). `internal void Validate()` called by the service constructor and DI validation.
2. **`IClaimCheckService.cs`**:
   - `ValueTask<Either<EncinaError, string>> OffloadAsync(string content, ClaimCheckRequest request, CancellationToken)` returns the content unchanged below the threshold, the envelope above it.
   - `ValueTask<Either<EncinaError, string>> ResolveAsync(string content, CancellationToken)` returns non-envelope content unchanged, otherwise the original string.
   - `ClaimCheckRequest(string TypeName, int? ThresholdBytes = null, ClaimCheckMode Mode = ClaimCheckMode.Conditional)`. The enum `ClaimCheckMode { Conditional, Always, Never }` is created here and used by #122.
3. **`ClaimCheckService.cs`**: UTF-8 encodes the content, compares with the threshold, stores through the store resolved by the store key (keyed service; `DefaultStoreKey`), builds the envelope, and on resolve retrieves, verifies SHA-256 (`claimcheck.integrity_failed` on mismatch) and decodes. Tenant (choices 9 and 10): the tenant is `IRequestContextAccessor.RequestContext?.TenantId`, as in `DeadLetterManager`; when `MultiTenancyMarker` is registered and no tenant resolves at offload time, returns `Left(claimcheck.tenant_required)` (fail closed, recommended in choice 10); the store id is prefixed with the tenant segment (URL-safe Base64 of the tenant id, then `.`). With tenancy off no prefix is added. Resolve needs no ambient tenant: the processor runs without a request context and the id already carries the segment.
4. Constructor: `IServiceProvider` (keyed store lookup), `IOptions<ClaimCheckOptions>`, `TimeProvider`, `ILogger<ClaimCheckService>`, `IRequestContextAccessor`, optional `MultiTenancyMarker?`. All of them live in `Encina` or `Encina.Messaging`; no new project reference (choice 9).

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 3</strong></summary>

```
You are implementing Phase 3 of issue #599 in Encina: ClaimCheckOptions, IClaimCheckService and ClaimCheckService.

CONTEXT:
- Phases 1-2 created the value types, the envelope, IClaimCheckStore and InMemoryClaimCheckStore.
- Tenancy signal: src/Encina/Abstractions/MultiTenancyMarker.cs and IRequestContextAccessor.RequestContext?.TenantId, exactly as
  src/Encina.Messaging/DeadLetter/DeadLetterManager.cs does. Never ITenantProvider: Encina.Tenancy references Encina.Messaging,
  so referencing it back is a project cycle.
- The #122 plan extends this service with the [ClaimCheck] attribute policy and compression; keep ClaimCheckRequest and ClaimCheckMode as the extension points.

TASK:
Implement the options (with Validate), the service interface and the service as described, with unit tests for: below/above the
threshold, Always/Never modes, integrity failure, store Left propagated, tenant fail-closed and tenant prefix, envelope pass-through.

KEY RULES:
- Either results only; no exceptions for business failures; a store Left is returned, never logged-and-swallowed.
- Logs carry claimcheck.* codes and exception types only (use ForLogging()); never payload text or EncinaError.Message.
- TimeProvider for timestamps; CancellationToken on every async call.
- Threshold compares UTF-8 byte length, not string length.

REFERENCE FILES:
- src/Encina.Messaging/Outbox/OutboxOrchestrator.cs (service style, logging)
- src/Encina/Abstractions/MultiTenancyMarker.cs
- src/Encina.Messaging/DeadLetter/DeadLetterManager.cs (AmbientTenantId, TenantMissing)
- src/Encina.Security.Secrets/SecretsErrors.cs
```

</details>

---

### Phase 4: Configuration, DI and start-up validation

> **Goal**: `UseClaimCheck` and registration that is complete and fails at start-up when a dependency is missing.

<details>
<summary><strong>Tasks</strong></summary>

1. **`src/Encina.Messaging/MessagingConfiguration.cs`**: `public bool UseClaimCheck { get; set; }` (XML doc, `Default: false (opt-in)`), added to the class remarks and example. `ClaimCheckThreshold` is not a second property: the threshold lives in `ClaimCheckOptions` (one place), configured with `services.Configure<ClaimCheckOptions>(...)` or the overload `AddEncinaMessaging(config, claimCheck => ...)` if the existing overloads allow it.
2. **`src/Encina.Messaging/MessagingServiceCollectionExtensions.cs`**: `RegisterClaimCheck` (pattern of `RegisterOutbox`, `:272`, called at `:259` and `:459`) registers `ClaimCheckOptions`, `IClaimCheckService` as singleton, the health check, and `ClaimCheckStartupCheck` (an `IHostedService` that fails start-up with `InvalidOperationException` naming the feature and the missing store, until the validator of #2382 exists; the same interim mechanism as `DelayedRetryStartupCheck` in ADR-050).
3. **`AddEncinaClaimCheckInMemory(this IServiceCollection)`**: registers `InMemoryClaimCheckStore` as the keyed store `"default"` with `TryAdd`, so a database or blob store registered by the application wins in any order.
4. **DI test**: builds the provider with `ValidateOnBuild` and `ValidateScopes`, with and without a store.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 4</strong></summary>

```
You are implementing Phase 4 of issue #599 in Encina: the UseClaimCheck flag, registration and start-up validation.

CONTEXT:
- MessagingConfiguration (src/Encina.Messaging/MessagingConfiguration.cs) has one Use* flag per pattern.
- Registration lives in src/Encina.Messaging/MessagingServiceCollectionExtensions.cs (RegisterOutbox at :272 is the model).
- AGENTS.md §3: an AddEncina* method registers every option type and dependency it resolves, proven by a ValidateOnBuild DI test;
  a store registered by the application must win over the in-memory default in any registration order.

TASK:
Add UseClaimCheck, RegisterClaimCheck, AddEncinaClaimCheckInMemory and ClaimCheckStartupCheck, and the DI tests
(with store, without store -> start-up failure, store registered before and after).

KEY RULES:
- Opt-in and off by default; nothing is registered unless UseClaimCheck is true.
- TryAdd for defaults; no silent default store.
- The failure message names ClaimCheck, the missing IClaimCheckStore and how to add one; ADR-050 rule 3.

REFERENCE FILES:
- src/Encina.Messaging/MessagingConfiguration.cs
- src/Encina.Messaging/MessagingServiceCollectionExtensions.cs
- docs/architecture/adr/050-modules-may-require-modules.md
```

</details>

---

### Phase 5: Outbox integration

> **Goal**: Large outbox payloads are offloaded on write and resolved on dispatch.

<details>
<summary><strong>Tasks</strong></summary>

1. **`OutboxOrchestrator.AddAsync`** (`:87`): after `Serialize` (`:96`), when an `IClaimCheckService` is registered, call `OffloadAsync(content, new ClaimCheckRequest(notificationType))`; a `Left` is returned from `AddAsync`; the outbox message is created with the returned content. The optional constructor parameter is `IClaimCheckService? claimCheckService = null` so the outbox works unchanged without the feature.
2. **`OutboxBatchProcessor`** (`:182`): before `Deserialize`, call `ResolveAsync(message.Content)`; a `Left` takes the existing failure path (`MarkAsFailedAsync` with the error code as reason, retry then dead letter), exactly like a deserialization failure (`OutboxErrorCodes.DeserializationFailed` today). The resolved string is used for the notification and for the dead-letter capture is **not** replaced: the capture keeps the stored `Content` (the envelope), so a replay does not need a second offload (`:389-392`).
3. **Payload deletion is not in this plan.** The trigger (after the processed mark, on purge, on dead-letter expiry) is the cleanup of #122; this phase leaves the payload in the store until TTL.
4. Tests: below the threshold nothing changes; above it the row holds an envelope and the dispatch publishes the original notification; store `Left` on write fails `AddAsync`; store `Left` on dispatch marks the message failed and never processed; feature off means byte-for-byte unchanged behavior.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 5</strong></summary>

```
You are implementing Phase 5 of issue #599 in Encina: claim check integration in the outbox.

CONTEXT:
- OutboxOrchestrator.AddAsync serializes at src/Encina.Messaging/Outbox/OutboxOrchestrator.cs:96 and creates the message through _messageFactory.
- OutboxBatchProcessor deserializes at src/Encina.Messaging/Outbox/OutboxBatchProcessor.cs:182 and marks outcomes through the store;
  every store Left is already checked (OutcomeNotRecorded).
- IClaimCheckService (Phase 3) offloads above the threshold and resolves envelopes; it returns non-envelope content unchanged.

TASK:
Inject IClaimCheckService? (optional) into OutboxOrchestrator and OutboxBatchProcessor (and the DI that builds them), call
OffloadAsync after Serialize and ResolveAsync before Deserialize, and add the unit and integration tests listed in the plan.

KEY RULES:
- A Left from the service fails the operation: AddAsync returns it; the processor marks the message failed (retry/dead letter).
- The feature off means identical behavior and no new allocations on the hot path.
- Do not delete payloads here (the #122 plan owns deletion); do not change IOutboxMessage or any provider schema.
- Do not log content; log the message id and the claimcheck.* code.

REFERENCE FILES:
- src/Encina.Messaging/Outbox/OutboxOrchestrator.cs, OutboxBatchProcessor.cs, OutboxProcessorBase.cs
- src/Encina.Messaging/Outbox/IOutboxMessage.cs
- tests/Encina.UnitTests/Messaging/ (existing outbox tests)
```

</details>

---

### Phase 6: Cross-cutting integration

> **Goal**: The functions marked included in the matrix, other than telemetry (Phase 7).

<details>
<summary><strong>Tasks</strong></summary>

1. **Health check**: `ClaimCheckHealthCheck : IEncinaHealthCheck` (`src/Encina.Messaging/Health/IEncinaHealthCheck.cs`), `Name = "claimcheck"`, tags `["messaging", "claimcheck"]`; the probe is `ExistsAsync("health-probe")` on the default store: `Right(false)` is healthy, `Left` is unhealthy with the error code only. Registered with the other messaging health checks.
2. **Multi-tenancy**: implemented in the service (Phase 3).
3. **Validation**: `ClaimCheckOptions.Validate` at start-up (Phase 3/4).
4. **Deferred** (see matrix): caching, resilience of external stores (#122, where the stores are), distributed locks, idempotency, module isolation and audit trail (follow-up issues in Next Steps).

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 6</strong></summary>

```
You are implementing Phase 6 of issue #599 in Encina: the claim check health check.

CONTEXT:
- IEncinaHealthCheck lives in src/Encina.Messaging/Health/IEncinaHealthCheck.cs; the existing messaging health checks are the model.
- The default store is the keyed IClaimCheckStore "default".

TASK:
Create ClaimCheckHealthCheck, register it when UseClaimCheck is true, and test healthy (Right(false)), unhealthy (Left) and
cancelled cases.

KEY RULES:
- Health results carry the error code or exception type only, never EncinaError.Message (AGENTS.md §3).
- DefaultName const and static Tags array; scoped resolution through IServiceProvider.CreateScope() if the store is scoped.

REFERENCE FILES:
- src/Encina.Messaging/Health/IEncinaHealthCheck.cs
- src/Encina.Security.Secrets/Health/SecretsHealthCheck.cs
```

</details>

---

### Phase 7: Observability

> **Goal**: Traces, metrics and logs for store, retrieve and delete.

<details>
<summary><strong>Tasks</strong></summary>

1. **EventIds**: register `public static readonly (int Min, int Max) MessagingClaimCheck = (6000, 6049);` in `src/Encina/Diagnostics/EventIdRanges.cs` first, and add `nameof(EventIdRanges.MessagingClaimCheck)` to the `Encina.Messaging` entry of `tests/Encina.UnitTests/Testing/Architecture/EncinaEventIdAllocationTests.cs:91`. This plan uses 6000-6024; 6025-6049 are reserved for the #122 plan.
2. **`Diagnostics/ClaimCheckLog.cs`**: `[LoggerMessage]` methods packed from 6000 (offloaded, resolved, store failed, retrieve failed, integrity failed, tenant required, threshold skipped), XML doc `Event IDs: 6000-6024 (see EventIdRanges.MessagingClaimCheck)`.
3. **`Diagnostics/ClaimCheckActivitySource.cs`**: `ActivitySource("Encina.Messaging.ClaimCheck", "1.0")` (model: `OutboxActivitySource`); spans `encina.claimcheck.store` and `encina.claimcheck.retrieve`; attributes: `claimcheck.store_key`, `claimcheck.size_bucket`, tenant id; no ids of data subjects, no payload.
4. **`Diagnostics/ClaimCheckMetrics.cs`**: counters `encina.claimcheck.stored`, `encina.claimcheck.retrieved`, `encina.claimcheck.failures` (tag: error code), histogram `encina.claimcheck.payload_bytes`. Names are the ones proposed in #122; bounded tags only.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 7</strong></summary>

```
You are implementing Phase 7 of issue #599 in Encina: EventIds, ActivitySource, Meter and log messages for claim check.

CONTEXT:
- ADR-021 / AGENTS.md §7: register the range in src/Encina/Diagnostics/EventIdRanges.cs before any [LoggerMessage];
  add it to the Encina.Messaging entry of EncinaEventIdAllocationTests.AssemblyRanges.
- Existing patterns: src/Encina.Messaging/Diagnostics/OutboxActivitySource.cs, OutboxProcessorMetrics.cs, OutboxStoreLog.cs.

TASK:
Register MessagingClaimCheck 6000-6049, write ClaimCheckLog (6000-6024, packed), ClaimCheckActivitySource and ClaimCheckMetrics,
wire them into ClaimCheckService, and add tests with an in-memory exporter asserting spans/metrics and absence of payloads.

KEY RULES:
- EventIds packed sequentially, no gaps; never outside 6000-6024 in this plan.
- Tags are bounded (store key, size bucket, error code); no payload, no content type values supplied by users.
- Run the architecture tests (EncinaEventIdAllocationTests) before finishing.

REFERENCE FILES:
- src/Encina/Diagnostics/EventIdRanges.cs
- tests/Encina.UnitTests/Testing/Architecture/EncinaEventIdAllocationTests.cs
- src/Encina.Messaging/Diagnostics/
```

</details>

---

### Phase 8: Testing

> **Goal**: Every applicable flag reaches its own target; tests execute real package code.

<details>
<summary><strong>Tasks</strong></summary>

| Flag | Scope | Per-file target and justification |
|------|-------|-----------------------------------|
| Unit | store, service, envelope, options, outbox integration, DI, health check, metrics | `ClaimCheckService.cs`, `ClaimCheckEnvelope.cs`, `InMemoryClaimCheckStore.cs`: 90; every branch is reachable with `FakeTimeProvider` and an in-memory store, so a lower target has no reason. Options and records: 80 (property bodies) |
| Guard | every public method with parameters (store, service, options) | 90: `ArgumentNullException`/`ArgumentException` checks are a single line each and fully coverable |
| Contract | `ClaimCheckStoreContract` abstract base plus the in-memory subclass | 85: the base exercises each interface method on the real store; the #122 stores add subclasses |
| Property | round trip of random payloads, threshold boundary (`N-1`, `N`, `N+1` bytes), envelope round trip | 70 for the envelope and the service: the properties cover the pure logic only |
| Integration | outbox with a real database fixture (one provider family is enough, the integration code is provider-independent) and the in-memory store | the processor loop and the `Content` round trip through a real table; `[Collection]` fixture, `[Trait("Category", "Integration")]` |

- Load and benchmark: `.md` justifications (thin, non-concurrent code in this slice; the benchmark of the large payload path belongs to #122 where the real stores are).
- Update `.github/coverage-manifest/Encina.Messaging.json` with the per-file targets above (a new file lowers nothing; the ratchet only blocks decreases).
- Measure each flag with `dotnet test tests\Encina.<Flag>Tests --collect "XPlat Code Coverage"` and `dotnet run --file .github/scripts/coverage-report.cs`, and run the CRAP gate on the changed methods (AGENTS.md §9; keep `ClaimCheckService` methods small).

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 8</strong></summary>

```
You are implementing Phase 8 of issue #599 in Encina: the test suite for the claim check abstraction.

CONTEXT:
- Per-flag coverage (unit, guard, contract, property, integration) is measured independently against
  .github/coverage-manifest/Encina.Messaging.json; tests must execute real package code.
- Shouldly through Encina.Testing.Shouldly; FsCheck through Encina.Testing.FsCheck; FakeTimeProvider for time.

TASK:
Complete the unit, guard, contract (ClaimCheckStoreContract + InMemory subclass), property and integration tests, write the
.md justifications for load and benchmark tests, update the manifest, and report measured per-flag coverage and CRAP.

KEY RULES:
- Integration tests use a shared [Collection("...")] database fixture and ClearAllDataAsync; never a per-class fixture.
- No Thread.Sleep; no reflection-only tests; one behavior per test; builders for test data.
- Justification files follow the layout of AGENTS.md §9 (title, Status, Justification, adequate coverage, Date, Issue).

REFERENCE FILES:
- .github/coverage-manifest/Encina.Messaging.json
- tests/Encina.IntegrationTests/ (outbox integration tests and Collections.cs)
- docs/testing/coverage-measurement-methodology.md
```

</details>

---

### Phase 9: Documentation and finalization

> **Goal**: Everything the prompt lists for the documentation phase, for the abstraction slice.

<details>
<summary><strong>Tasks</strong></summary>

1. XML docs with `<summary>`, `<remarks>`, `<param>`, `<returns>`, `<example>` on every new public API.
2. `changelog.d/599-claim-check-abstraction.added.md` (one bullet).
3. `ROADMAP.md`: the pattern is already listed; move it to in-progress wording only if the milestone text changes.
4. Package README of `Encina.Messaging`: a short Claim Check section (configuration, in-memory store, outbox behavior).
5. `docs/features/claim-check.md`: usage and configuration guide, shared with the #122 plan (this plan writes the abstraction part, #122 extends it).
6. `docs/INVENTORY.md`: new files under `src/Encina.Messaging/ClaimCheck/`.
7. ADR: not written here. The #122 plan writes the single ADR of the feature (next free number at the time; 065 on 2026-11-11) and records choices 1, 2, 4 and 5 of this plan.
8. `PublicAPI.Unshipped.txt` complete; `dotnet build --configuration Release` with 0 warnings; `dotnet test` all pass; every flag at its manifest target.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 9</strong></summary>

```
You are implementing Phase 9 of issue #599 in Encina: documentation and finalization of the claim check abstraction.

CONTEXT:
- Documentation is written by the docs-writer specialist; the changelog fragment and PublicAPI lines by mechanical-fixer.
- docs/features/claim-check.md is shared with issue #122; this plan writes the abstraction part.

TASK:
Produce the documentation list of the plan's Phase 9, verify every identifier in the docs exists in src/, and run
dotnet build --configuration Release (0 warnings) and dotnet test.

KEY RULES:
- Never edit CHANGELOG.md; use changelog.d/599-claim-check-abstraction.added.md.
- Coverage figures are never typed by hand (covref markers).
- English only; no emojis; no private client names.

REFERENCE FILES:
- .claude/skills/encina-docs/SKILL.md
- changelog.d/README.md
- docs/features/ (existing feature pages)
```

</details>

---

## Research

### Relevant Standards & Specifications

| Source | Reference | Relevance |
|--------|-----------|-----------|
| Enterprise Integration Patterns | Claim Check | Store the payload, pass a reference, retrieve on demand |
| Azure Architecture Center | Claim-Check pattern; Service Bus message size 256 KB (Standard) to 100 MB (Premium) | Motivation and the default threshold |
| AWS | SQS Extended Client Library | Comparable product: an S3-backed large message library, not portable across brokers |
| AGENTS.md §3 | ROP, opt-in patterns, registration completeness, errors never swallowed, `TimeProvider` | Result types, `UseClaimCheck`, start-up validation |
| ADR-001, ADR-006 | Railway Oriented Programming | `Either` results |
| ADR-018 | Cross-cutting integration | Matrix below |
| ADR-021 | EventId ranges | `MessagingClaimCheck` 6000-6049 |
| ADR-050 | Modules may require modules (closed list) | Claim check requires a store only through start-up validation, not auto-registration |
| SPEC-000 | 1.0 scope | Messaging patterns in scope |

### Existing Encina Infrastructure to Leverage

| Component | Location | Usage in this feature |
|-----------|----------|-----------------------|
| IMessageSerializer and JsonMessageSerializer | src/Encina.Messaging/Serialization/ | typing of payloads stays here; the claim check store works on bytes |
| EncryptingMessageSerializer (decorator) | src/Encina.Messaging.Encryption/Serialization/EncryptingMessageSerializer.cs | offload runs after the serializer chain, so encrypted content is stored encrypted; the ENC:v1 prefix is the model for the claim check envelope |
| OutboxOrchestrator.AddAsync | src/Encina.Messaging/Outbox/OutboxOrchestrator.cs:87 | offload hook after Serialize |
| OutboxBatchProcessor | src/Encina.Messaging/Outbox/OutboxBatchProcessor.cs:182 | resolve hook before Deserialize |
| MessagingConfiguration | src/Encina.Messaging/MessagingConfiguration.cs | new UseClaimCheck flag next to UseOutbox |
| AddOutboxInboxSagaSchedulingServices | src/Encina.Messaging/MessagingServiceCollectionExtensions.cs | registration pattern for opt-in patterns |
| IEncinaHealthCheck | src/Encina.Messaging/Health/IEncinaHealthCheck.cs | store health check |
| OutboxActivitySource and OutboxProcessorMetrics | src/Encina.Messaging/Diagnostics/ | pattern for ClaimCheckActivitySource and ClaimCheckMetrics |
| EventIdRanges | src/Encina/Diagnostics/EventIdRanges.cs | register MessagingClaimCheck 6000-6049 first |
| EncinaEventIdAllocationTests | tests/Encina.UnitTests/Testing/Architecture/EncinaEventIdAllocationTests.cs:91 | add the new range to the Encina.Messaging entry |
| FakeTimeProvider | Microsoft.Extensions.Time.Testing | deterministic TTL tests of the in-memory store |
| MultiTenancyMarker and ITenantProvider | src/Encina/Abstractions/MultiTenancyMarker.cs, src/Encina.Tenancy/Abstractions/ITenantProvider.cs | fail-closed tenant prefix |
| Messaging coverage manifest | .github/coverage-manifest/Encina.Messaging.json | per-file targets |

### Event ID Allocation

| Package | Range | Notes |
|---------|-------|-------|
| `Encina.Messaging` (claim check, this plan) | `MessagingClaimCheck` 6000-6049 (new) | **6000-6024** used by this plan; 6025-6049 reserved for the #122 plan (cleanup, attribute, transports helper) |
| `Encina.Messaging` (other ranges) | 2000-2399, 2800-2999, 5450-5499 | Not used; `Messaging` 2800-2999 has only 2989-2999 left |
| Family of plans | 6050-6099 `StorageCore`, 6100-6149 `StorageProviders` (#1239); 6150-6199 `ClaimCheckProviders` (#122); 6200-6249 `SecurityColumnEncryption` (#1250); `SecuritySecrets` 5151-5170 (#1246) | Shared reservation of the family (registry eventid-blocks.md) |

### Estimated File Count

| Category | Files | Notes |
|----------|-------|-------|
| New production files in src/Encina.Messaging/ClaimCheck/ | 10 | interface, reference, metadata, errors, options, envelope, service interface, service, in-memory store, health check |
| New diagnostics files in src/Encina.Messaging/Diagnostics/ | 3 | log messages, activity source, metrics |
| Modified production files | 6 | MessagingConfiguration, MessagingServiceCollectionExtensions, OutboxOrchestrator, OutboxBatchProcessor, EventIdRanges, PublicAPI.Unshipped.txt |
| Unit tests | 8 | store, service, envelope, options, outbox integration, DI, health check, metrics |
| Guard tests | 2 | store and service |
| Contract tests | 2 | abstract store contract plus in-memory subclass |
| Property tests | 1 | round trip and threshold boundary |
| Integration tests | 1 | outbox with a real database fixture and the in-memory store |
| Load and benchmark justification files | 2 | .md justifications |
| Documentation | 5 | docs/features page, package README section, changelog fragment, ROADMAP line, INVENTORY |
| Coverage manifest edits | 1 | Encina.Messaging.json per-file targets |
| Total | 41 | Sum of all categories |

---

## Combined AI Agent Prompts

<details>
<summary><strong>Full combined prompt for all phases</strong></summary>

```
PROJECT CONTEXT:
You are implementing issue #599 in Encina (.NET 10, C# 14, nullable enabled, pre-1.0, no backward compatibility): the abstraction
slice of the Claim Check pattern in Encina.Messaging. Issue #122 owns the rest of the feature (attribute, compression, cleanup,
transports, stores, ADR); its plan is docs/plans/claim-check-implementation-plan-122.md. Rules: AGENTS.md.

IMPLEMENTATION OVERVIEW:
1. ClaimCheckReference, ClaimCheckMetadata, ClaimCheckErrors, ClaimCheckEnvelope (CLC:v1:{storeKey}:{flags}:{size}:{sha256}:{id}, flags "-" in this plan).
2. IClaimCheckStore (bytes/Stream, Either) and InMemoryClaimCheckStore with TimeProvider TTL.
3. ClaimCheckOptions, IClaimCheckService/ClaimCheckService (threshold in UTF-8 bytes, SHA-256 verification, tenant fail-closed).
4. UseClaimCheck, RegisterClaimCheck, AddEncinaClaimCheckInMemory, start-up check for the missing store.
5. Outbox: OffloadAsync after Serialize in OutboxOrchestrator.AddAsync; ResolveAsync before Deserialize in OutboxBatchProcessor.
6. ClaimCheckHealthCheck.
7. EventIds MessagingClaimCheck 6000-6049 (this plan 6000-6024), ActivitySource, Meter.
8. Tests per flag (+ ClaimCheckStoreContract base), docs.

KEY PATTERNS:
- Either<EncinaError, T> results; errors never swallowed; no EncinaError.Message in logs or tags (use codes and ForLogging()).
- TimeProvider for every timestamp; CancellationToken on every async call.
- Opt-in: nothing registered unless UseClaimCheck; TryAdd defaults; a store registered by the application wins in any order.
- EventIds registered first, packed, inside the plan's range; architecture test updated.
- Tests execute real code; per-flag targets from the manifest; CRAP <= 10 on changed methods.
- No [Obsolete], no compatibility layers, English only, no AI attribution in commits.

REFERENCE FILES:
- src/Encina.Messaging/Outbox/OutboxOrchestrator.cs, OutboxBatchProcessor.cs
- src/Encina.Messaging/MessagingConfiguration.cs, MessagingServiceCollectionExtensions.cs
- src/Encina.Messaging.Encryption/EncryptedPayloadFormatter.cs
- src/Encina.Security.Secrets/SecretsErrors.cs
- docs/plans/claim-check-implementation-plan-122.md
```

</details>

---

## Cross-Cutting Integration Matrix

| # | Function | Status | Notes |
|---|----------|--------|-------|
| 1 | Caching | ⏭️ | Frequently retrieved payloads could be cached; the store contract is cache-friendly (immutable ids). Follow-up issue to open at implementation start (see Next Steps) |
| 2 | OpenTelemetry | ✅ | Phase 7: `ActivitySource` `Encina.Messaging.ClaimCheck`, counters and the payload size histogram |
| 3 | Structured Logging | ✅ | Phase 7: `[LoggerMessage]` EventIds 6000-6024 in the new `MessagingClaimCheck` range, registered first |
| 4 | Health Checks | ✅ | Phase 6: `ClaimCheckHealthCheck` over the default store |
| 5 | Validation | ✅ | `ClaimCheckOptions.Validate` at start-up and `ClaimCheckStartupCheck` (missing store); payload size cap `MaxPayloadBytes` |
| 6 | Resilience | ⏭️ | The in-memory store has no external call; retry and circuit breaker for external stores are in the #122 plan where the stores live |
| 7 | Distributed Locks | ❌ | Each claim id is written once and never updated; no shared mutable state |
| 8 | Transactions | ⏭️ | The payload write and the outbox row write are two systems; a crash between them leaves an orphan payload that TTL (and the #122 cleanup) reclaims. Atomicity is not offered; recorded in the ADR of #122 |
| 9 | Idempotency | ❌ | Offload is not a request entry point; a retried `AddAsync` creates a new claim and the earlier one expires by TTL |
| 10 | Multi-Tenancy | ✅ | Phase 3: tenant segment in the claim id, `Left(claimcheck.tenant_required)` when tenancy is on and no tenant resolves |
| 11 | Module Isolation | ⏭️ | Module-scoped claim namespaces are not needed by SPEC-002 (REQ-061 requires no module scoping); `ModuleId` in messaging stays with #747 |
| 12 | Audit Trail | ⏭️ | Payload access audit for compliance; payloads may hold personal data. Follow-up issue to open at implementation start (see Next Steps) |

---

## Prerequisites & Dependencies

### Advisable before or with this work

- None blocks the abstraction slice. The decisions of choices 1, 2, 4 and 5 are shared with #122; the maintainer answers them once.
- #2382 (the startup validator for ADR-050): `ClaimCheckStartupCheck` migrates into it when it exists.

### Parallel and follow-up work

- `claim-check-implementation-plan-122.md` (#122): builds on the names and the `ClaimCheckService` of this plan; merge order is this plan's PR 1 first.
- #600 (cloud blob implementations of `IClaimCheckStore`): related; the #122 plan proposes to cover it through the adapter over `IBlobStore` of #1239.
- #1239 (`Encina.Storage`): the preferred backend for the stores of #122.

---

## Next Steps

1. The maintainer reviews the Dilemmas (choices 1, 2, 4 and 5, shared with the #122 plan) and records the answers once; the orchestrator then adds the Maintainer Decisions section to both plans.
2. At implementation start open two follow-up issues from the matrix: caching of retrieved payloads (function 1) and payload access audit (function 12).
3. Link this plan from #599 and the #122 plan from #122 before any worker is briefed.
4. Implement PR 1 (Phases 1-4), then PR 2 (Phases 5-7), then PR 3 (Phases 8-9), each with its own tests and measured per-flag coverage.
