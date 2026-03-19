# Implementation Plan: `Encina.Audit.Marten` — Event-Sourced IAuditStore with Temporal Crypto-Shredding

> **Issue**: [#800](https://github.com/dlrivada/Encina/issues/800)
> **Type**: Feature
> **Complexity**: High (8 phases, ~50 files)
> **Estimated Scope**: ~3,000–4,000 lines of production code + ~2,000–3,000 lines of tests
> **Milestone**: v0.13.0 — Security & Compliance
> **Provider Category**: Event Sourcing (Marten/PostgreSQL — specialized provider, not the 13 DB providers)

---

## Summary

This plan implements `Encina.Audit.Marten`, a new **event-sourced `IAuditStore` and `IReadAuditStore` implementation** backed by Marten (PostgreSQL). It provides compliance-grade audit trails with immutable event streams and temporal crypto-shredding for data minimization (GDPR Art. 5(1)(e)).

**Key differentiator from existing providers**: Current DB providers (13 × ADO/Dapper/EF/MongoDB) store audit entries as mutable rows — they can be UPDATEd or DELETEd. The Marten provider uses append-only event streams, making audit entries **immutable by design**. `PurgeEntriesAsync()` destroys encryption keys instead of deleting events (crypto-shredding), preserving the event stream's integrity chain while achieving effective data erasure.

**What this plan delivers:**
1. `MartenAuditStore` implementing `IAuditStore` with encrypted event-sourced persistence
2. `MartenReadAuditStore` implementing `IReadAuditStore` with encrypted event-sourced persistence
3. `ITemporalKeyProvider` interface for time-partitioned encryption key management
4. Marten async projections for efficient querying with transparent decryption
5. `AddEncinaAuditMarten()` DI extension for opt-in registration
6. Health checks, OpenTelemetry, structured logging
7. Unit + integration tests (PostgreSQL via Docker)

**Affected packages:**
- `Encina.Audit.Marten` (NEW)
- `Encina.Security.Audit` (no changes — only consumes interfaces)

**Dependencies:**
- `Encina.Marten` — aggregate repository, projections, event infrastructure
- `Encina.Marten.GDPR` — reference patterns for crypto-shredding, `IFieldEncryptor`
- `Encina.Security.Audit` — `IAuditStore`, `IReadAuditStore`, `AuditEntry`, `ReadAuditEntry`
- `Encina.Security.Encryption` — `IFieldEncryptor` for AES-256-GCM encryption
- ADR-020 (temporal crypto-shredding) — architectural decisions (completed in #799 spike)

---

## Design Choices

<details>
<summary><strong>1. Package Placement — New `Encina.Audit.Marten` package</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) New `Encina.Audit.Marten` package** | Clean separation; opt-in; no coupling between audit + Marten for apps that don't need it; follows satellite package philosophy | One more NuGet package to maintain |
| **B) Add to `Encina.Marten`** | Fewer packages; Marten already has DI infra | Forces Marten users to take audit dependency; violates pay-for-what-you-use |
| **C) Add to `Encina.Security.Audit`** | Consolidates audit code | Forces Security.Audit to depend on Marten; breaks users who don't want PostgreSQL |

### Decision

**Selected**: Option A — New `Encina.Audit.Marten` package

### Rationale

Follows Encina's "pay-for-what-you-use" philosophy. The Marten audit provider is specialized infrastructure for compliance-heavy apps. Most apps will continue using the 13 DB providers. The new package depends on `Encina.Marten`, `Encina.Marten.GDPR`, and `Encina.Security.Audit` but doesn't force any of those to change.

### Implementation Impact

- New `.csproj` with `<ProjectReference>` to Encina.Marten, Encina.Marten.GDPR, Encina.Security.Audit, Encina.Security.Encryption
- New `PublicAPI.Shipped.txt` / `PublicAPI.Unshipped.txt`
- Registration via `services.AddEncinaAuditMarten()`
- Added to `Encina.slnx`

</details>

<details>
<summary><strong>2. Temporal Key Management — New `ITemporalKeyProvider` interface</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Reuse `ISubjectKeyProvider` from Marten.GDPR** | No new interface; reuse existing code | Wrong abstraction — subject keys are per-user, temporal keys are per-time-period; lifecycle differs (subject forget vs. retention expiry); mixing concerns |
| **B) New `ITemporalKeyProvider` interface** | Clean separation; purpose-built for time-partitioned keys; can coexist with subject keys for dual encryption | New interface to maintain |
| **C) Time-partition via `ISubjectKeyProvider` with temporal subject IDs** | Reuses existing infra, just uses "2026-Q1" as subject ID | Semantic mismatch; health checks/metrics would be confusing; `IsSubjectForgottenAsync` semantics don't apply |

### Decision

**Selected**: Option B — New `ITemporalKeyProvider` interface (as specified in ADR-020)

### Rationale

ADR-020 explicitly mandates a separate `ITemporalKeyProvider` because:
1. **Different lifecycle**: Subject keys are deleted when a user exercises GDPR Art. 17 (forget). Temporal keys are deleted when a retention period expires (compliance purge).
2. **Different granularity**: Subject = per-user, Temporal = per-time-period (month/quarter/year).
3. **Orthogonal concerns**: Both can coexist — an audit entry can be encrypted with temporal key AND a subject key simultaneously.
4. **Different storage model**: Temporal keys are partitioned by time period, not by user ID.

### Implementation Impact

- `ITemporalKeyProvider` in `Encina.Audit.Marten/Crypto/`
- `MartenTemporalKeyProvider` stores keys as Marten documents with time-period partitioning
- `InMemoryTemporalKeyProvider` for testing
- Key ID format: `"temporal:{period}:v{version}"` (e.g., `"temporal:2026-03:v1"`)

</details>

<details>
<summary><strong>3. Encryption Scope — PII Fields Only (partial encryption)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Encrypt entire event payload** | Maximum privacy; simplest implementation | Non-PII fields (Action, EntityType, Outcome, timestamps) become unqueryable after shredding; projections break completely |
| **B) Encrypt only PII-sensitive fields** | Structural fields remain queryable even after shredding; projections can still filter/sort on non-PII; compliance-friendly (shredded entries visible in results as placeholders) | More complex serialization; must define which fields are PII |
| **C) No encryption, physical deletion** | Simplest | Breaks event store immutability; defeats the purpose of Marten ES |

### Decision

**Selected**: Option B — Encrypt only PII-sensitive fields (as specified in ADR-020)

### Rationale

ADR-020 identifies the PII-sensitive fields in `AuditEntry`: `UserId`, `IpAddress`, `UserAgent`, `RequestPayload`, `ResponsePayload`, and `Metadata` values. Non-PII structural fields (`Action`, `EntityType`, `EntityId`, `Outcome`, `TimestampUtc`, `CorrelationId`, etc.) remain in plaintext and are queryable even after crypto-shredding. This enables:
- Projections to filter by action, entity, outcome, date range
- Shredded entries appear in results with `[SHREDDED]` placeholders for PII
- Compliance officers can see "someone did X to entity Y at time Z" without knowing who

### Implementation Impact

- `AuditEntryRecordedEvent` stores PII fields as `EncryptedField<string>` wrappers
- Encryption happens in `MartenAuditStore.RecordAsync()` before appending the event
- Decryption happens in projection `Apply()` methods when building read models
- `[SHREDDED]` placeholder when temporal key has been destroyed

</details>

<details>
<summary><strong>4. Projection Strategy — Async Projections</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Inline projections** | Strongly consistent; automatically updated within same transaction; no eventual consistency lag; Marten handles lifecycle | Adds overhead to every `SaveChangesAsync()` — penalizes write path which is on the hot path of every command via `AuditPipelineBehavior`; cannot scale independently; projection rebuild requires re-reading events through write path |
| **B) Async projections** | Scalable; can be rebuilt independently without affecting write path; better write performance; fault isolation (projection failure doesn't block event append) | Eventually consistent; query may return stale data; more complex error handling |
| **C) Live aggregation (no projection)** | Simplest; always up-to-date; no projection table | O(n) per query — unacceptable for audit trails with millions of entries |

### Decision

**Selected**: Option B — Async projections

### Rationale

Audit trails have an **asymmetric access pattern**: high-frequency writes (every command triggers `RecordAsync` via `AuditPipelineBehavior`) but low-frequency reads (compliance officers querying historical data hours/days/weeks later). This makes eventual consistency perfectly acceptable:

1. **No "record-then-immediately-query" scenario**: Nobody records an audit entry and queries for it in the same request. Compliance reports are retrospective by nature.
2. **Write performance matters**: Inline projections add decryption + document upsert overhead to every `SaveChangesAsync()`. Since audit recording is in the pipeline of every command, this penalizes the entire application.
3. **Independent rebuild**: When projection schema changes (new indexes, new computed fields), async projections rebuild in background without affecting the write path.
4. **Fault isolation**: If the projection fails (e.g., decryption error on a corrupted event), the event is already safely persisted. The projection retries independently. With inline, a projection failure could roll back the event append.
5. **Natural fit for Marten's async daemon**: Marten's `AsyncProjectionDaemon` handles the lifecycle, retries, and high-water mark tracking automatically.

### Implementation Impact

- `AuditEntryProjection` as Marten `MultiStreamProjection<AuditEntryReadModel, Guid>` registered via `Projections.Async()`
- `ReadAuditEntryProjection` as Marten `MultiStreamProjection<ReadAuditEntryReadModel, Guid>` registered via `Projections.Async()`
- Read models stored as Marten documents for efficient querying
- Projections registered via `StoreOptions.Projections.Async()` with `AsyncProjectionDaemon`
- Health check monitors projection high-water mark to detect lag
- Eventual consistency window: typically milliseconds to low seconds under normal load

</details>

<details>
<summary><strong>5. Aggregate Design — Stream-per-Entity vs. Stream-per-Entry</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Stream-per-entity** (stream ID = `"audit:{entityType}:{entityId}"`) | Natural grouping; efficient per-entity queries; enables temporal queries on entity history | Streams grow unbounded for hot entities; `GetByUserAsync` requires cross-stream queries |
| **B) Stream-per-entry** (stream ID = `"audit:{entryId}"`) | Bounded stream size (1 event each); simple | Defeats event sourcing benefits; every query is a cross-stream query |
| **C) Stream-per-time-partition** (stream ID = `"audit:{period}"`) | Aligns with temporal key partitioning; efficient purge (destroy key = shred entire partition) | Large streams; complex querying |
| **D) Flat projections only** (no meaningful streams, use Marten document queries) | Simplest query model; leverages PostgreSQL JSONB indexing | Loses event stream integrity guarantees; essentially a document store |

### Decision

**Selected**: Option D — Flat projections with single audit event stream

### Rationale

Audit entries are fundamentally independent records, not aggregates with state transitions. Using a single well-known stream (e.g., `"audit-trail"`) or per-entity streams would create unbounded growth issues. Instead, we:
1. Append events to categorized streams (`"audit:{entityType}:{entityId}"` when entityId exists, `"audit:{entityType}"` otherwise)
2. Use Marten's `MultiStreamProjection` to maintain a flat document table of `AuditEntryReadModel`
3. Query against the projected document table using Marten's LINQ/compiled queries
4. The event stream provides immutability and integrity; the projection provides query efficiency

This hybrid approach gives us the best of both worlds: immutable event log + efficient SQL queries.

### Implementation Impact

- Events appended to entity-scoped streams for natural grouping
- `AuditEntryReadModel` document with PostgreSQL indexes for common query patterns
- `QueryAsync` uses Marten `IDocumentSession.Query<AuditEntryReadModel>()` with LINQ
- `PurgeEntriesAsync` destroys temporal keys, not events or documents

</details>

<details>
<summary><strong>6. PurgeEntriesAsync Semantics — Crypto-Shred (key destruction)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Physical deletion** | Simple; matches DB provider behavior | Breaks event store immutability; creates gaps in event log; loses compliance evidence |
| **B) Crypto-shredding (destroy temporal keys)** | Preserves immutability; shredded entries remain as evidence of "something happened"; compliant with SOX (evidence preservation) + GDPR (data minimization) | More complex; queries must handle shredded entries; projection rebuild after shredding shows placeholders |
| **C) Soft-delete flag** | Simple; preserves events | PII still accessible to anyone with DB access; doesn't satisfy GDPR data minimization |

### Decision

**Selected**: Option B — Crypto-shredding via temporal key destruction

### Rationale

This is the core value proposition of `Encina.Audit.Marten`. By destroying temporal encryption keys:
- Events remain in the stream (immutability preserved, SOX compliant)
- PII fields become undecryptable (GDPR data minimization)
- Non-PII fields remain queryable (Action, EntityType, Outcome, timestamps)
- `PurgeEntriesAsync(olderThanUtc)` returns the count of entries whose temporal keys were destroyed
- Shredded entries appear in query results with `[SHREDDED]` placeholders

### Implementation Impact

- `PurgeEntriesAsync` calls `ITemporalKeyProvider.DestroyKeysBeforeAsync(olderThanUtc)`
- Returns count of affected time periods × entries per period
- Projection read models mark PII fields as `"[SHREDDED]"` when decryption fails due to missing key
- Health check reports count of active vs. shredded temporal keys

</details>

---

## Implementation Phases

### Phase 1: Project Setup & Core Models

<details>
<summary><strong>Tasks</strong></summary>

1. **Create project `src/Encina.Audit.Marten/Encina.Audit.Marten.csproj`**
   - Target: `net10.0`
   - Dependencies: `Encina.Marten`, `Encina.Marten.GDPR`, `Encina.Security.Audit`, `Encina.Security.Encryption`, `Microsoft.Extensions.Options`, `Microsoft.Extensions.Logging.Abstractions`, `Microsoft.Extensions.Diagnostics.HealthChecks`, `LanguageExt.Core`
   - Include `PublicAPI.Shipped.txt` + `PublicAPI.Unshipped.txt`
   - Add to `Encina.slnx`

2. **Create `src/Encina.Audit.Marten/MartenAuditOptions.cs`**
   - Namespace: `Encina.Audit.Marten`
   - Properties:
     - `KeyRotationPeriod` (`TimeSpan`, default: 30 days / monthly)
     - `TemporalGranularity` (`TemporalKeyGranularity` enum: Monthly, Quarterly, Yearly — default Monthly)
     - `EncryptionScope` (`AuditEncryptionScope` enum: PiiFieldsOnly, AllFields — default PiiFieldsOnly)
     - `RetentionPeriod` (`TimeSpan`, default: 2555 days / ~7 years)
     - `EnableAutoPurge` (`bool`, default: false)
     - `PurgeIntervalHours` (`int`, default: 24)
     - `ShreddedPlaceholder` (`string`, default: `"[SHREDDED]"`)
     - `AddHealthCheck` (`bool`, default: false)

3. **Create `src/Encina.Audit.Marten/TemporalKeyGranularity.cs`**
   - Enum: `Monthly = 0`, `Quarterly = 1`, `Yearly = 2`

4. **Create `src/Encina.Audit.Marten/AuditEncryptionScope.cs`**
   - Enum: `PiiFieldsOnly = 0`, `AllFields = 1`

5. **Create `src/Encina.Audit.Marten/MartenAuditErrorCodes.cs`**
   - Static class with error code constants:
     - `EncryptionFailed`, `DecryptionFailed`, `KeyNotFound`, `KeyDestructionFailed`
     - `ProjectionFailed`, `QueryFailed`, `StoreUnavailable`
     - `TemporalKeyExpired`, `ShreddedEntry`

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 1</strong></summary>

```
CONTEXT:
You are implementing a new NuGet package `Encina.Audit.Marten` for the Encina framework.
This package provides an event-sourced IAuditStore implementation using Marten (PostgreSQL)
with temporal crypto-shredding for compliance-grade audit trails.

The project uses .NET 10 / C# 14, nullable enabled, Railway Oriented Programming (Either<EncinaError, T>).

TASK:
1. Create the project file `src/Encina.Audit.Marten/Encina.Audit.Marten.csproj`:
   - Target net10.0
   - Reference Encina.Marten, Encina.Marten.GDPR, Encina.Security.Audit, Encina.Security.Encryption
   - Include PublicAPI.Shipped.txt and PublicAPI.Unshipped.txt (empty initially)
   - Follow the same .csproj pattern as src/Encina.Marten.GDPR/Encina.Marten.GDPR.csproj

2. Create MartenAuditOptions.cs with configuration properties (see plan Phase 1, task 2)

3. Create TemporalKeyGranularity.cs enum (Monthly, Quarterly, Yearly)

4. Create AuditEncryptionScope.cs enum (PiiFieldsOnly, AllFields)

5. Create MartenAuditErrorCodes.cs with static error code constants

6. Add the project to Encina.slnx

KEY RULES:
- XML documentation on ALL public APIs
- Namespace: Encina.Audit.Marten
- Follow patterns from src/Encina.Marten.GDPR/ for .csproj structure
- No [Obsolete] attributes, no backward compatibility

REFERENCE FILES:
- src/Encina.Marten.GDPR/Encina.Marten.GDPR.csproj (csproj template)
- src/Encina.Marten.GDPR/CryptoShreddingOptions.cs (options pattern)
- src/Encina.Marten.GDPR/CryptoShreddingErrors.cs (error codes pattern)
```

</details>

---

### Phase 2: Temporal Key Provider

<details>
<summary><strong>Tasks</strong></summary>

1. **Create `src/Encina.Audit.Marten/Crypto/ITemporalKeyProvider.cs`**
   - Namespace: `Encina.Audit.Marten.Crypto`
   - Methods:
     - `GetOrCreateKeyAsync(string period, CancellationToken) → Either<EncinaError, TemporalKeyInfo>`
     - `GetKeyAsync(string period, int? version, CancellationToken) → Either<EncinaError, TemporalKeyInfo>`
     - `DestroyKeysBeforeAsync(DateTime olderThanUtc, CancellationToken) → Either<EncinaError, int>` (returns count of destroyed periods)
     - `IsKeyDestroyedAsync(string period, CancellationToken) → Either<EncinaError, bool>`
     - `GetActivePeriodKeysAsync(CancellationToken) → Either<EncinaError, IReadOnlyList<TemporalKeyInfo>>`
   - Period format helper: `GetPeriodKey(DateTimeOffset timestamp, TemporalKeyGranularity granularity) → string`
     - Monthly: `"2026-03"`, Quarterly: `"2026-Q1"`, Yearly: `"2026"`

2. **Create `src/Encina.Audit.Marten/Crypto/TemporalKeyInfo.cs`**
   - Record: `Period`, `KeyMaterial` (byte[]), `Version` (int), `Status` (TemporalKeyStatus), `CreatedAtUtc`, `DestroyedAtUtc?`

3. **Create `src/Encina.Audit.Marten/Crypto/TemporalKeyStatus.cs`**
   - Enum: `Active = 0`, `Rotated = 1`, `Destroyed = 2`

4. **Create `src/Encina.Audit.Marten/Crypto/TemporalKeyDocument.cs`**
   - Marten document entity for persistence
   - `Id` format: `"temporal:{period}:v{version}"`
   - Fields: `Period`, `KeyMaterial`, `Version`, `Status`, `CreatedAtUtc`, `DestroyedAtUtc`

5. **Create `src/Encina.Audit.Marten/Crypto/MartenTemporalKeyProvider.cs`**
   - Implements `ITemporalKeyProvider`
   - Uses `IDocumentSession` for persistence
   - AES-256 key generation via `RandomNumberGenerator`
   - `DestroyKeysBeforeAsync`: hard-deletes key material, inserts destruction marker
   - Thread-safe via session-per-operation

6. **Create `src/Encina.Audit.Marten/Crypto/InMemoryTemporalKeyProvider.cs`**
   - Implements `ITemporalKeyProvider`
   - Uses `ConcurrentDictionary`
   - For testing only

7. **Create `src/Encina.Audit.Marten/Crypto/TemporalKeyDestroyedMarker.cs`**
   - Marten document that records when a period's keys were destroyed
   - `Id` format: `"temporal-destroyed:{period}"`
   - Fields: `Period`, `DestroyedAtUtc`, `KeyVersionsDestroyed`

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 2</strong></summary>

```
CONTEXT:
You are implementing the temporal key management infrastructure for Encina.Audit.Marten.
Temporal keys are time-partitioned encryption keys used for crypto-shredding audit entries.
Unlike subject keys (per-user), temporal keys are per-time-period (monthly/quarterly/yearly).

The project uses .NET 10, C# 14, nullable enabled, ROP (Either<EncinaError, T>).
Encryption uses AES-256-GCM via Encina.Security.Encryption's IFieldEncryptor.

TASK:
1. Create ITemporalKeyProvider interface with methods for key lifecycle management
2. Create TemporalKeyInfo record, TemporalKeyStatus enum, TemporalKeyDocument
3. Create MartenTemporalKeyProvider — persists keys to Marten document store
4. Create InMemoryTemporalKeyProvider — for testing
5. Create TemporalKeyDestroyedMarker — tracks destroyed periods

KEY RULES:
- All methods return Either<EncinaError, T>
- Key ID format: "temporal:{period}:v{version}"
- Period format: "2026-03" (monthly), "2026-Q1" (quarterly), "2026" (yearly)
- Key material: 32 bytes generated via RandomNumberGenerator.GetBytes()
- DestroyKeysBeforeAsync: delete key material, insert marker, return count
- MartenTemporalKeyProvider uses IDocumentSession (scoped)
- InMemoryTemporalKeyProvider uses ConcurrentDictionary (singleton)
- XML documentation on all public APIs
- Namespace: Encina.Audit.Marten.Crypto

REFERENCE FILES:
- src/Encina.Marten.GDPR/Abstractions/ISubjectKeyProvider.cs (interface pattern)
- src/Encina.Marten.GDPR/KeyStore/PostgreSqlSubjectKeyProvider.cs (Marten persistence)
- src/Encina.Marten.GDPR/KeyStore/InMemorySubjectKeyProvider.cs (in-memory pattern)
- src/Encina.Marten.GDPR/KeyStore/SubjectKeyDocument.cs (document pattern)
- docs/architecture/adr/020-temporal-crypto-shredding-audit-store.md (design decisions)
```

</details>

---

### Phase 3: Event-Sourced Events & Encryption

<details>
<summary><strong>Tasks</strong></summary>

1. **Create `src/Encina.Audit.Marten/Events/AuditEntryRecordedEvent.cs`**
   - Namespace: `Encina.Audit.Marten.Events`
   - Represents an audit entry being appended to the event stream
   - PII fields stored as `EncryptedField` (string containing JSON with ciphertext + nonce + tag + keyId)
   - Non-PII fields stored in plaintext:
     - `Id` (Guid), `CorrelationId`, `Action`, `EntityType`, `EntityId`, `Outcome` (int), `ErrorMessage`
     - `TimestampUtc`, `StartedAtUtc`, `CompletedAtUtc`
     - `RequestPayloadHash`
   - PII fields (encrypted):
     - `EncryptedUserId`, `EncryptedIpAddress`, `EncryptedUserAgent`
     - `EncryptedRequestPayload`, `EncryptedResponsePayload`, `EncryptedMetadata`
   - `TemporalKeyPeriod` (string) — which temporal key was used
   - `TenantId` (plaintext — needed for tenant-scoped queries)

2. **Create `src/Encina.Audit.Marten/Events/ReadAuditEntryRecordedEvent.cs`**
   - Same pattern for `ReadAuditEntry`
   - Non-PII: `Id`, `EntityType`, `EntityId`, `AccessedAtUtc`, `AccessMethod`, `EntityCount`
   - PII (encrypted): `EncryptedUserId`, `EncryptedPurpose`, `EncryptedMetadata`
   - `TenantId` (plaintext), `CorrelationId` (plaintext)
   - `TemporalKeyPeriod`

3. **Create `src/Encina.Audit.Marten/Events/EncryptedField.cs`**
   - Value object: `string? Ciphertext`, `string? Nonce`, `string? Tag`, `string? KeyId`
   - Static factory: `EncryptedField.Create(plaintext, keyMaterial, keyId)` using AES-256-GCM
   - Instance method: `Decrypt(keyMaterial) → string?`
   - Static factory: `EncryptedField.Shredded()` returns placeholder

4. **Create `src/Encina.Audit.Marten/AuditEventEncryptor.cs`**
   - Namespace: `Encina.Audit.Marten`
   - Utility class that maps `AuditEntry` → `AuditEntryRecordedEvent` (encrypts PII)
   - And `ReadAuditEntry` → `ReadAuditEntryRecordedEvent` (encrypts PII)
   - Depends on `ITemporalKeyProvider` + `MartenAuditOptions`
   - Returns `Either<EncinaError, TEvent>`

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 3</strong></summary>

```
CONTEXT:
You are implementing the event-sourced events and encryption layer for Encina.Audit.Marten.
Audit entries are stored as encrypted events in Marten event streams. PII-sensitive fields
(UserId, IpAddress, UserAgent, payloads, metadata) are encrypted with temporal keys.
Non-PII fields (Action, EntityType, Outcome, timestamps) remain in plaintext for querying.

TASK:
1. Create AuditEntryRecordedEvent with encrypted PII fields and plaintext structural fields
2. Create ReadAuditEntryRecordedEvent with same pattern for read audit entries
3. Create EncryptedField value object with AES-256-GCM encrypt/decrypt using System.Security.Cryptography
4. Create AuditEventEncryptor utility that maps AuditEntry → encrypted event and ReadAuditEntry → encrypted event

KEY RULES:
- AES-256-GCM: 32-byte key, 12-byte nonce (random), 16-byte auth tag
- EncryptedField stores Base64-encoded ciphertext, nonce, tag, and keyId as JSON
- KeyId format: "temporal:{period}:v{version}"
- When decryption key is not found (destroyed), return "[SHREDDED]" placeholder
- Events are C# records (immutable)
- Use System.Security.Cryptography.AesGcm for encryption
- XML documentation on all public APIs
- Namespace: Encina.Audit.Marten.Events (events), Encina.Audit.Marten (encryptor)

REFERENCE FILES:
- src/Encina.Security.Audit/AuditEntry.cs (source model)
- src/Encina.Security.Audit/ReadAuditEntry.cs (source model)
- src/Encina.Marten.GDPR/Serialization/CryptoShredderSerializer.cs (encryption pattern)
- docs/architecture/adr/020-temporal-crypto-shredding-audit-store.md (PII field list)
```

</details>

---

### Phase 4: Projections & Read Models

<details>
<summary><strong>Tasks</strong></summary>

1. **Create `src/Encina.Audit.Marten/Projections/AuditEntryReadModel.cs`**
   - Marten document for query-efficient audit entry storage
   - All fields from `AuditEntry` (decrypted if key available, `[SHREDDED]` if not)
   - Additional: `IsShredded` (bool), `TemporalKeyPeriod` (string)
   - Marten indexes: `TimestampUtc`, `EntityType`, `EntityId`, `UserId`, `TenantId`, `CorrelationId`, `Action`, `Outcome`

2. **Create `src/Encina.Audit.Marten/Projections/ReadAuditEntryReadModel.cs`**
   - Same pattern for read audit entries
   - Fields from `ReadAuditEntry` + `IsShredded`, `TemporalKeyPeriod`
   - Indexes: `AccessedAtUtc`, `EntityType`, `EntityId`, `UserId`, `TenantId`, `AccessMethod`

3. **Create `src/Encina.Audit.Marten/Projections/AuditEntryProjection.cs`**
   - Marten `MultiStreamProjection<AuditEntryReadModel, Guid>` registered as **async**
   - `Apply(AuditEntryRecordedEvent)` → decrypts PII fields, maps to read model
   - Handles missing key (shredded) gracefully with placeholder
   - Depends on `ITemporalKeyProvider` (resolved from DI)

4. **Create `src/Encina.Audit.Marten/Projections/ReadAuditEntryProjection.cs`**
   - Same pattern for `ReadAuditEntryRecordedEvent` → `ReadAuditEntryReadModel`
   - Registered as **async** projection

5. **Create `src/Encina.Audit.Marten/Projections/ConfigureMartenAuditProjections.cs`**
   - `IConfigureOptions<StoreOptions>` that registers **async** projections and document indexes
   - Projections registered via `options.Projections.Async()` (Marten's `AsyncProjectionDaemon` handles lifecycle)

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 4</strong></summary>

```
CONTEXT:
You are implementing Marten inline projections for the audit store. Events are encrypted —
projections decrypt PII fields at apply-time and store the results in read model documents.
When temporal keys have been destroyed (crypto-shredding), projections store "[SHREDDED]"
placeholder values and set IsShredded = true.

TASK:
1. Create AuditEntryReadModel — Marten document with all AuditEntry fields + IsShredded, TemporalKeyPeriod
2. Create ReadAuditEntryReadModel — same for ReadAuditEntry
3. Create AuditEntryProjection — MultiStreamProjection that decrypts and maps events to read models
4. Create ReadAuditEntryProjection — same for read audit events
5. Create ConfigureMartenAuditProjections — IConfigureOptions<StoreOptions> for registration

KEY RULES:
- Projections are ASYNC (eventually consistent) — registered via Projections.Async()
- Marten's AsyncProjectionDaemon handles lifecycle, retries, high-water mark
- Decrypt PII fields using ITemporalKeyProvider.GetKeyAsync()
- If key not found → set field to "[SHREDDED]", IsShredded = true
- Read models are regular Marten documents with JSONB indexes
- Index columns for efficient querying: TimestampUtc, EntityType, UserId, TenantId, etc.
- XML documentation on all public APIs
- Namespace: Encina.Audit.Marten.Projections

REFERENCE FILES:
- src/Encina.Marten/Projections/IProjection.cs (projection interface)
- src/Encina.Marten/Projections/MartenProjectionManager.cs (projection management)
- src/Encina.Audit.Marten/Events/AuditEntryRecordedEvent.cs (source event from Phase 3)
- src/Encina.Security.Audit/AuditEntry.cs (target model fields)
```

</details>

---

### Phase 5: Store Implementations

<details>
<summary><strong>Tasks</strong></summary>

1. **Create `src/Encina.Audit.Marten/MartenAuditStore.cs`**
   - Implements `IAuditStore`
   - Constructor: `IDocumentSession`, `AuditEventEncryptor`, `ITemporalKeyProvider`, `TimeProvider`, `IOptions<MartenAuditOptions>`, `ILogger<MartenAuditStore>`
   - `RecordAsync(entry)`:
     1. Get temporal period from `entry.TimestampUtc` + granularity
     2. Encrypt PII fields via `AuditEventEncryptor`
     3. Append `AuditEntryRecordedEvent` to stream `"audit:{entityType}:{entityId}"` (or `"audit:{entityType}"` if no entityId)
     4. `SaveChangesAsync()`
   - `GetByEntityAsync(entityType, entityId)`: query `AuditEntryReadModel` via Marten LINQ
   - `GetByUserAsync(userId, from, to)`: query projected documents
   - `GetByCorrelationIdAsync(correlationId)`: query projected documents
   - `QueryAsync(query)`: build LINQ query from `AuditQuery` filters, apply pagination
   - `PurgeEntriesAsync(olderThanUtc)`: call `ITemporalKeyProvider.DestroyKeysBeforeAsync()`

2. **Create `src/Encina.Audit.Marten/MartenReadAuditStore.cs`**
   - Implements `IReadAuditStore`
   - Same pattern as `MartenAuditStore` for read audit entries
   - `LogReadAsync(entry)`: encrypt + append `ReadAuditEntryRecordedEvent`
   - Query methods use `ReadAuditEntryReadModel`

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 5</strong></summary>

```
CONTEXT:
You are implementing the core IAuditStore and IReadAuditStore implementations for Marten.
These stores append encrypted events to Marten event streams and query against projected
read model documents. PurgeEntriesAsync destroys temporal encryption keys (crypto-shredding)
instead of deleting events.

TASK:
1. Create MartenAuditStore implementing IAuditStore with event-sourced persistence
2. Create MartenReadAuditStore implementing IReadAuditStore with event-sourced persistence

KEY RULES:
- RecordAsync: encrypt PII → append event → SaveChangesAsync
- Query methods: use IDocumentSession.Query<TReadModel>() with LINQ filters
- PurgeEntriesAsync: call ITemporalKeyProvider.DestroyKeysBeforeAsync(), return count
- All methods return Either<EncinaError, T>
- Use try/catch → map exceptions to EncinaError
- Stream IDs: "audit:{entityType}:{entityId}" or "audit:{entityType}"
- Read audit streams: "read-audit:{entityType}:{entityId}" or "read-audit:{entityType}"
- PagedResult<T> for QueryAsync pagination
- XML documentation on all public APIs
- Namespace: Encina.Audit.Marten

REFERENCE FILES:
- src/Encina.Security.Audit/Abstractions/IAuditStore.cs (interface to implement)
- src/Encina.Security.Audit/Abstractions/IReadAuditStore.cs (interface to implement)
- src/Encina.Security.Audit/InMemoryAuditStore.cs (reference implementation)
- src/Encina.Security.Audit/AuditQuery.cs (query model)
- src/Encina.Security.Audit/ReadAuditQuery.cs (query model)
```

</details>

---

### Phase 6: DI Registration, Health Checks & Diagnostics

<details>
<summary><strong>Tasks</strong></summary>

1. **Create `src/Encina.Audit.Marten/ServiceCollectionExtensions.cs`**
   - `AddEncinaAuditMarten(IServiceCollection, Action<MartenAuditOptions>?)`:
     - Register `MartenAuditOptions` via Options pattern
     - Register `ITemporalKeyProvider` → `MartenTemporalKeyProvider` (scoped)
     - Register `AuditEventEncryptor` (scoped)
     - Register `IAuditStore` → `MartenAuditStore` (replaces default InMemory)
     - Register `IReadAuditStore` → `MartenReadAuditStore` (replaces default InMemory)
     - Register `IConfigureOptions<StoreOptions>` → `ConfigureMartenAuditProjections`
     - Conditionally register health check
     - Conditionally register `MartenAuditRetentionService` (if `EnableAutoPurge`)

2. **Create `src/Encina.Audit.Marten/Health/MartenAuditHealthCheck.cs`**
   - Extends `EncinaHealthCheck`
   - DefaultName: `"encina-audit-marten"`
   - Tags: `["audit", "marten", "security", "ready"]`
   - Checks: Marten connectivity, temporal key provider availability, async projection high-water mark (lag detection)

3. **Create `src/Encina.Audit.Marten/Diagnostics/MartenAuditActivitySource.cs`**
   - ActivitySource: `"Encina.Audit.Marten"` version `"1.0"`
   - Activities:
     - `AuditMarten.Record` (record audit entry)
     - `AuditMarten.Query` (query audit entries)
     - `AuditMarten.Purge` (crypto-shred temporal keys)
     - `AuditMarten.Encrypt` (encrypt PII fields)
     - `AuditMarten.Decrypt` (decrypt PII fields)

4. **Create `src/Encina.Audit.Marten/Diagnostics/MartenAuditMeter.cs`**
   - Meter: `"Encina.Audit.Marten"` version `"1.0"`
   - Counters:
     - `encina.audit.marten.entries_recorded_total` (tags: entity_type, action)
     - `encina.audit.marten.entries_queried_total` (tags: query_type)
     - `encina.audit.marten.entries_shredded_total` (tags: period)
     - `encina.audit.marten.encryption_total` (tags: outcome)
     - `encina.audit.marten.decryption_total` (tags: outcome)
   - Histograms:
     - `encina.audit.marten.record_duration_ms`
     - `encina.audit.marten.query_duration_ms`
     - `encina.audit.marten.purge_duration_ms`

5. **Create `src/Encina.Audit.Marten/Diagnostics/MartenAuditLog.cs`**
   - `[LoggerMessage]` source generator
   - EventId range: **2500–2539** (next available after QueryCache 2400–2405)
   - Categories:
     - 2500–2509: Record operations
     - 2510–2519: Query operations
     - 2520–2529: Purge/crypto-shredding operations
     - 2530–2539: Key management operations

6. **Create `src/Encina.Audit.Marten/MartenAuditRetentionService.cs`**
   - Background service for auto-purge
   - Periodic timer based on `PurgeIntervalHours`
   - Calls `PurgeEntriesAsync()` with calculated retention cutoff

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 6</strong></summary>

```
CONTEXT:
You are implementing the DI registration, health checks, and observability for Encina.Audit.Marten.
This follows Encina's standard patterns: AddEncina{Feature}() extension method, EncinaHealthCheck base class,
ActivitySource + Meter + [LoggerMessage] for observability.

TASK:
1. Create ServiceCollectionExtensions.AddEncinaAuditMarten() — registers all components
2. Create MartenAuditHealthCheck — verifies Marten connectivity and key provider
3. Create MartenAuditActivitySource — OTel tracing for record/query/purge operations
4. Create MartenAuditMeter — OTel metrics (counters + histograms)
5. Create MartenAuditLog — structured logging with [LoggerMessage], EventIds 2500-2539
6. Create MartenAuditRetentionService — background auto-purge service

KEY RULES:
- DI: Use services.Replace() for IAuditStore/IReadAuditStore (override InMemory defaults)
- Health check: DefaultName const, Tags static array, scoped resolution
- ActivitySource: check HasListeners() before creating activities (zero-cost guard)
- Meter: dimensional counters with tags
- [LoggerMessage]: static partial methods, source generator
- EventId range: 2500-2539 (non-colliding with existing ranges)
- Background service: use PeriodicTimer, respect cancellation
- XML documentation on all public APIs

REFERENCE FILES:
- src/Encina.Security.Audit/ServiceCollectionExtensions.cs (DI pattern)
- src/Encina.Security.Audit/Health/AuditStoreHealthCheck.cs (health check pattern)
- src/Encina.Security.Audit/Diagnostics/ReadAuditLog.cs (logging pattern)
- src/Encina.Security.Audit/Diagnostics/ReadAuditActivitySource.cs (activity source pattern)
- src/Encina.Marten.GDPR/Diagnostics/CryptoShreddingDiagnostics.cs (Marten diagnostics pattern)
- src/Encina.Marten.GDPR/ServiceCollectionExtensions.cs (Marten DI pattern)
```

</details>

---

### Phase 7: Testing

<details>
<summary><strong>Tasks</strong></summary>

1. **Unit Tests** (`tests/Encina.UnitTests/AuditMarten/`)
   - `MartenAuditStoreTests.cs` — mock `IDocumentSession`, verify event appended with encryption
   - `MartenReadAuditStoreTests.cs` — mock session, verify read audit event appended
   - `AuditEventEncryptorTests.cs` — verify encryption/decryption of PII fields
   - `EncryptedFieldTests.cs` — AES-256-GCM round-trip, shredded placeholder
   - `MartenTemporalKeyProviderTests.cs` — mock session, key lifecycle
   - `InMemoryTemporalKeyProviderTests.cs` — full lifecycle without mocks
   - `MartenAuditOptionsTests.cs` — defaults, validation
   - `TemporalKeyGranularityTests.cs` — period format generation (monthly/quarterly/yearly)
   - `AuditEntryProjectionTests.cs` — event → read model mapping, shredded handling
   - `ServiceCollectionExtensionsTests.cs` — verify registrations

2. **Guard Tests** (`tests/Encina.GuardTests/AuditMarten/`)
   - `MartenAuditStoreGuardTests.cs` — null checks on all public methods
   - `MartenReadAuditStoreGuardTests.cs`
   - `AuditEventEncryptorGuardTests.cs`
   - `TemporalKeyProviderGuardTests.cs`

3. **Integration Tests** (`tests/Encina.IntegrationTests/AuditMarten/`)
   - Create `MartenAuditFixture.cs` — starts PostgreSQL container, configures Marten
   - Create `Collections.cs` — `[CollectionDefinition("AuditMarten-PostgreSQL")]`
   - `MartenAuditStoreIntegrationTests.cs`:
     - `RecordAsync_ShouldPersistEncryptedEvent`
     - `GetByEntityAsync_ShouldReturnDecryptedEntries`
     - `GetByUserAsync_ShouldFilterByDateRange`
     - `QueryAsync_ShouldSupportPagination`
     - `PurgeEntriesAsync_ShouldDestroyTemporalKeys`
     - `QueryAsync_AfterPurge_ShouldReturnShreddedPlaceholders`
   - `MartenReadAuditStoreIntegrationTests.cs` — same pattern
   - `TemporalKeyProviderIntegrationTests.cs`:
     - `GetOrCreateKeyAsync_ShouldPersistToMarten`
     - `DestroyKeysBeforeAsync_ShouldRemoveKeyMaterial`
     - `IsKeyDestroyedAsync_ShouldReturnTrueAfterDestruction`

4. **Property Tests** (`tests/Encina.PropertyTests/AuditMarten/`)
   - `EncryptedFieldPropertyTests.cs` — round-trip: encrypt → decrypt = original (FsCheck)
   - `TemporalKeyGranularityPropertyTests.cs` — period format deterministic for same timestamp+granularity

5. **Load/Benchmark justifications** (`tests/Encina.LoadTests/AuditMarten/AuditMarten.md`, `tests/Encina.BenchmarkTests/AuditMarten/AuditMarten.md`)
   - Justify skip: audit recording is fire-and-forget, not on critical path; Marten session is the bottleneck, not our code

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 7</strong></summary>

```
CONTEXT:
You are implementing tests for Encina.Audit.Marten. The package provides event-sourced
IAuditStore with temporal crypto-shredding via Marten (PostgreSQL).

Test infrastructure uses xUnit with [Collection] fixtures for shared containers,
AAA pattern, and comprehensive coverage across unit, guard, integration, and property tests.

TASK:
1. Create unit tests in tests/Encina.UnitTests/AuditMarten/ — mock IDocumentSession, test encryption
2. Create guard tests in tests/Encina.GuardTests/AuditMarten/ — null parameter validation
3. Create integration tests in tests/Encina.IntegrationTests/AuditMarten/ — real PostgreSQL via Docker
4. Create property tests in tests/Encina.PropertyTests/AuditMarten/ — FsCheck round-trip invariants
5. Create justification .md files for load tests and benchmarks

KEY RULES:
- Integration tests: use [Collection("AuditMarten-PostgreSQL")] shared fixture
- Fixture: PostgreSQL container via Testcontainers, Marten StoreOptions configured
- ClearAllDataAsync() in InitializeAsync() (never dispose fixture from tests)
- Unit tests: mock IDocumentSession, ITemporalKeyProvider, ILogger
- Guard tests: verify ArgumentNullException for all public parameters
- Property tests: FsCheck Arbitrary<EncryptedField> with round-trip assertion
- Target: ≥85% line coverage
- AAA pattern, descriptive names, single responsibility per test

REFERENCE FILES:
- tests/Encina.IntegrationTests/Collections.cs (collection fixture pattern)
- tests/Encina.UnitTests/Security/ (security test patterns)
- tests/Encina.GuardTests/ (guard test patterns)
- tests/Encina.PropertyTests/ (property test patterns)
```

</details>

---

### Phase 8: Documentation & Finalization

<details>
<summary><strong>Tasks</strong></summary>

1. **XML doc comments** — verify all public APIs have `<summary>`, `<remarks>`, `<param>`, `<returns>`, `<example>`

2. **CHANGELOG.md** — add under `## [Unreleased]` → `### Added`:
   ```
   - `Encina.Audit.Marten` — Event-sourced `IAuditStore` with temporal crypto-shredding for compliance-grade audit trails (#800)
   ```

3. **ROADMAP.md** — update v0.13.0 Security & Compliance section if applicable

4. **Package README** — create `src/Encina.Audit.Marten/README.md` with:
   - Overview, motivation, quick start, configuration, encryption scope, purge semantics
   - Comparison with DB providers
   - Architecture diagram (text-based)

5. **Feature documentation** — create `docs/features/audit-marten.md` with:
   - Usage guide, configuration reference, temporal key management
   - Crypto-shredding explained, compliance mapping (SOX, NIS2, GDPR)
   - Integration with existing audit infrastructure
   - Testing guide

6. **docs/INVENTORY.md** — add new package entry

7. **PublicAPI files** — ensure all public symbols tracked in `PublicAPI.Unshipped.txt`

8. **Build verification**: `dotnet build --configuration Release` → 0 errors, 0 warnings

9. **Test verification**: `dotnet test` → all pass

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 8</strong></summary>

```
CONTEXT:
You are finalizing the Encina.Audit.Marten package. All code is implemented. Now ensure
documentation, build verification, and API tracking are complete.

TASK:
1. Verify XML doc comments on all public APIs (summary, remarks, param, returns, example)
2. Update CHANGELOG.md — add Encina.Audit.Marten under Unreleased → Added
3. Update ROADMAP.md if v0.13.0 section needs updating
4. Create src/Encina.Audit.Marten/README.md — package overview, quick start, config reference
5. Create docs/features/audit-marten.md — comprehensive usage guide
6. Update docs/INVENTORY.md — add new package
7. Populate PublicAPI.Unshipped.txt with all public symbols
8. Run dotnet build --configuration Release — verify 0 errors, 0 warnings
9. Run dotnet test — verify all tests pass

KEY RULES:
- README: concise, code examples, comparison with DB providers
- Feature doc: comprehensive, compliance mapping, architecture explanation
- PublicAPI format: "Namespace.Type.Member(params) -> ReturnType"
- Build must be clean (0 warnings)
- All tests must pass
- No [Obsolete] attributes anywhere

REFERENCE FILES:
- src/Encina.Marten.GDPR/README.md (package README pattern)
- docs/features/crypto-shredding.md (feature documentation pattern)
- CHANGELOG.md (changelog format)
- ROADMAP.md (roadmap format)
```

</details>

---

## Research

### Relevant Standards & Specifications

| Standard | Articles/Sections | Relevance |
|----------|-------------------|-----------|
| **GDPR** | Art. 5(1)(e) — Data minimization | Crypto-shredding enables purging PII without breaking immutability |
| **GDPR** | Art. 17 — Right to erasure | Temporal key destruction achieves effective erasure |
| **SOX** | §302, §404 — Internal controls | Immutable event streams provide tamper-proof audit trail |
| **NIS2** | Art. 10 — Logging with integrity | Append-only event store guarantees integrity |
| **HIPAA** | §164.312(b) — Audit controls | Comprehensive access tracking with retention |
| **PCI-DSS** | Req. 10.2 — Logging | Cardholder data monitoring |

### Existing Encina Infrastructure to Leverage

| Component | Location | Usage in This Feature |
|-----------|----------|----------------------|
| `IAuditStore` | `Encina.Security.Audit/Abstractions/` | Interface to implement |
| `IReadAuditStore` | `Encina.Security.Audit/Abstractions/` | Interface to implement |
| `AuditEntry` | `Encina.Security.Audit/AuditEntry.cs` | Domain model (input) |
| `ReadAuditEntry` | `Encina.Security.Audit/ReadAuditEntry.cs` | Domain model (input) |
| `AuditQuery` / `ReadAuditQuery` | `Encina.Security.Audit/` | Query models |
| `PagedResult<T>` | `Encina/` | Pagination result |
| `EncinaError` | `Encina/` | ROP error type |
| `EncinaHealthCheck` | `Encina/` | Health check base class |
| `ISubjectKeyProvider` | `Encina.Marten.GDPR/Abstractions/` | Reference pattern for key management |
| `CryptoShredderSerializer` | `Encina.Marten.GDPR/Serialization/` | Reference pattern for encryption |
| `IFieldEncryptor` | `Encina.Security.Encryption/` | AES-256-GCM encryption |
| Marten `IDocumentSession` | `Marten` (NuGet) | Event append + document query |
| `InMemoryAuditStore` | `Encina.Security.Audit/` | Reference implementation |

### Event ID Allocation

| Package | Range | Notes |
|---------|-------|-------|
| Encina.Security.Secrets | 1–127 | Secret operations |
| Encina.Cdc | 100–214 | CDC processing |
| Repository | 1100–1103 | Repository operations |
| UnitOfWork | 1200–1205 | Transaction lifecycle |
| BulkOperations | 1300–1303 | Bulk operations |
| Specification | 1400–1401 | Specification evaluation |
| SoftDelete | 1500–1506 | Soft delete |
| Write Audit | 1600–1605 | Write audit trail |
| Read Audit | 1700–1738 | Read audit trail |
| Tenancy | 1800–1804 | Tenant resolution |
| Modules | 1900–1906 | Module lifecycle |
| Outbox | 2000–2006 | Outbox messages |
| Inbox | 2100–2106 | Inbox messages |
| Saga | 2200–2205 | Saga transitions |
| Scheduling | 2300–2306 | Scheduled messages |
| QueryCache | 2400–2405 | Query caching |
| **Encina.Audit.Marten** | **2500–2539** | **NEW — Marten audit (this plan)** |
| IdGeneration / Security | 8000–8029 | ID generation + security |
| GDPR | 8100–8220 | GDPR compliance |
| Crypto-Shredding | 8400–8415 | Crypto-shredding |
| Anti-Tampering | 9100–9105 | Signature validation |

### Estimated File Count

| Category | Count |
|----------|-------|
| Project setup (.csproj, PublicAPI) | 3 |
| Configuration (options, enums, errors) | 5 |
| Crypto (key provider, models, documents) | 7 |
| Events (encrypted events, EncryptedField) | 4 |
| Projections (read models, projections, config) | 5 |
| Stores (MartenAuditStore, MartenReadAuditStore) | 2 |
| DI & infrastructure | 3 |
| Diagnostics (activity, meter, log) | 3 |
| Health check | 1 |
| Background service | 1 |
| **Production total** | **~34** |
| Unit tests | ~10 |
| Guard tests | ~4 |
| Integration tests | ~4 |
| Property tests | ~2 |
| Justification .md files | ~2 |
| **Test total** | **~22** |
| Documentation (README, feature doc) | 2 |
| **Grand total** | **~58** |

---

## Combined AI Agent Prompts

<details>
<summary><strong>Complete Implementation Prompt (All Phases)</strong></summary>

```
PROJECT CONTEXT:
You are implementing the Encina.Audit.Marten package — an event-sourced IAuditStore
with temporal crypto-shredding for compliance-grade audit trails.

TECHNOLOGY: .NET 10, C# 14, nullable enabled, Railway Oriented Programming (Either<EncinaError, T>)
PERSISTENCE: Marten event store (PostgreSQL)
ENCRYPTION: AES-256-GCM via System.Security.Cryptography
STANDARDS: SOX §404, NIS2 Art. 10, GDPR Art. 5(1)(e)/17

ARCHITECTURE:
- Events are encrypted with temporal keys (monthly/quarterly/yearly partitions)
- PII fields encrypted: UserId, IpAddress, UserAgent, RequestPayload, ResponsePayload, Metadata
- Non-PII fields remain plaintext: Action, EntityType, EntityId, Outcome, timestamps, CorrelationId
- PurgeEntriesAsync destroys temporal keys (crypto-shredding), NOT events
- Async projections (eventually consistent) decrypt events into queryable read models
- Shredded entries: PII fields replaced with "[SHREDDED]", IsShredded = true

IMPLEMENTATION OVERVIEW:

Phase 1: Project setup — .csproj, options, enums, error codes
Phase 2: ITemporalKeyProvider — interface, Marten + InMemory implementations, key documents
Phase 3: Events — AuditEntryRecordedEvent, ReadAuditEntryRecordedEvent, EncryptedField, AuditEventEncryptor
Phase 4: Projections — AuditEntryReadModel, ReadAuditEntryReadModel, inline projections
Phase 5: Stores — MartenAuditStore (IAuditStore), MartenReadAuditStore (IReadAuditStore)
Phase 6: DI, health checks, diagnostics — AddEncinaAuditMarten(), OTel, [LoggerMessage]
Phase 7: Tests — unit, guard, integration (PostgreSQL Docker), property (FsCheck)
Phase 8: Documentation — XML docs, CHANGELOG, README, feature guide, PublicAPI, build verify

KEY PATTERNS:
- Store naming: MartenAuditStore, MartenReadAuditStore
- DI: AddEncinaAuditMarten() with TryAdd/Replace pattern
- Health check: EncinaHealthCheck base, DefaultName const, Tags static array
- Diagnostics: ActivitySource "Encina.Audit.Marten", Meter "Encina.Audit.Marten"
- Logging: [LoggerMessage] EventIds 2500-2539
- Key format: "temporal:{period}:v{version}"
- Stream IDs: "audit:{entityType}:{entityId}", "read-audit:{entityType}:{entityId}"
- Projection: MultiStreamProjection with async lifecycle (AsyncProjectionDaemon)
- Tests: [Collection("AuditMarten-PostgreSQL")] shared fixture

REFERENCE FILES:
- src/Encina.Security.Audit/Abstractions/IAuditStore.cs
- src/Encina.Security.Audit/Abstractions/IReadAuditStore.cs
- src/Encina.Security.Audit/AuditEntry.cs
- src/Encina.Security.Audit/ReadAuditEntry.cs
- src/Encina.Security.Audit/AuditQuery.cs
- src/Encina.Security.Audit/ReadAuditQuery.cs
- src/Encina.Security.Audit/ServiceCollectionExtensions.cs
- src/Encina.Security.Audit/InMemoryAuditStore.cs
- src/Encina.Security.Audit/Health/AuditStoreHealthCheck.cs
- src/Encina.Security.Audit/Diagnostics/ReadAuditLog.cs
- src/Encina.Security.Audit/Diagnostics/ReadAuditActivitySource.cs
- src/Encina.Marten/ServiceCollectionExtensions.cs
- src/Encina.Marten.GDPR/Abstractions/ISubjectKeyProvider.cs
- src/Encina.Marten.GDPR/KeyStore/PostgreSqlSubjectKeyProvider.cs
- src/Encina.Marten.GDPR/KeyStore/InMemorySubjectKeyProvider.cs
- src/Encina.Marten.GDPR/Serialization/CryptoShredderSerializer.cs
- src/Encina.Marten.GDPR/ServiceCollectionExtensions.cs
- src/Encina.Marten.GDPR/Diagnostics/CryptoShreddingDiagnostics.cs
- docs/architecture/adr/020-temporal-crypto-shredding-audit-store.md
```

</details>

---

## Cross-Cutting Integration Matrix

| # | Function | Status | Notes |
|---|----------|--------|-------|
| 1 | **Caching** | ⏭️ Defer | Evaluate caching projections for read-heavy query patterns. Audit reads are typically low-frequency compliance queries. Create issue if benchmarks show need. |
| 2 | **OpenTelemetry** | ✅ Include | ActivitySource `Encina.Audit.Marten` + Meter with counters/histograms (Phase 6) |
| 3 | **Structured Logging** | ✅ Include | `[LoggerMessage]` source generator, EventIds 2500–2539 (Phase 6) |
| 4 | **Health Checks** | ✅ Include | `MartenAuditHealthCheck` — verify Marten/PostgreSQL connectivity + key provider (Phase 6) |
| 5 | **Validation** | ❌ N/A | Audit entries come pre-validated from the audit pipeline behavior. No user input boundary. |
| 6 | **Resilience** | ❌ N/A | Marten connects to local PostgreSQL (same as existing audit stores). Audit recording is already fire-and-forget — failures logged, never blocking. |
| 7 | **Distributed Locks** | ❌ N/A | No background processor with concurrent access. Retention service runs on a single timer with no contention. Key operations are idempotent. |
| 8 | **Transactions** | ❌ N/A | Marten session manages its own transactions. Event append + projection update are atomic within `SaveChangesAsync()`. |
| 9 | **Idempotency** | ❌ N/A | Event versioning in Marten handles concurrency. Each audit entry has a unique ID. Duplicate recording is handled gracefully (update or reject). |
| 10 | **Multi-Tenancy** | ⏭️ Defer | `TenantId` is stored in plaintext (not encrypted) to enable tenant-scoped queries. Full multi-tenant data isolation (separate schemas/databases per tenant) deferred to #798. |
| 11 | **Module Isolation** | ❌ N/A | Audit trail is inherently cross-module (global service). Module-scoped auditing is not meaningful. |
| 12 | **Audit Trail** | ✅ Include | This IS the audit trail implementation. Self-referential: the Marten audit store is itself an audit store provider. |

**Deferred items:**
- **Caching**: No existing issue. Create if benchmarks warrant it.
- **Multi-Tenancy**: Tracked in [#798](https://github.com/dlrivada/Encina/issues/798).
