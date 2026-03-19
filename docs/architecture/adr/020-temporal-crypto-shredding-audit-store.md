# ADR-020: Temporal Crypto-Shredding for Marten-Based Audit Store

## Status

Accepted

## Date

2026-03-19

## Context

`Encina.Security.Audit` provides `IAuditStore` with a `PurgeEntriesAsync(DateTime olderThanUtc)` method that physically deletes audit entries older than a given date. The 13 database providers (ADO, Dapper, EF Core, MongoDB) implement this as `DELETE FROM audit WHERE TimestampUtc < @date`.

When using Marten as the audit store backend (`Encina.Audit.Marten`), physical deletion violates event store immutability — the fundamental invariant of event sourcing. Events, once appended, must never be mutated or removed.

`Encina.Marten.GDPR` already solves a similar problem for GDPR Article 17 (Right to Erasure) using **per-subject crypto-shredding**: PII fields are encrypted with per-data-subject keys (`ISubjectKeyProvider`). When a subject exercises their right to be forgotten, all their key versions are deleted, rendering encrypted fields permanently unreadable while preserving event stream integrity.

The audit store needs a different partitioning strategy: **per-time-period keys**. Audit entries within a time window share an encryption key. When the retention period expires, the key is destroyed, rendering all entries in that period unreadable — achieving the same effect as physical deletion without breaking immutability.

### Questions Evaluated

1. **Key partitioning granularity**: monthly vs quarterly vs yearly vs configurable
2. **Key rotation strategy**: how to transition between periods seamlessly
3. **Projection impact**: how Marten projections handle encrypted vs shredded events
4. **Query performance**: can projections index decrypted fields for `QueryAsync` pagination
5. **Interaction with existing `ISubjectKeyProvider`**: reuse vs separate provider

## Decision

### 1. Key Partitioning: Configurable Granularity with Monthly Default

The key granularity is **configurable per deployment** via `TemporalKeyGranularity` enum, defaulting to `Monthly`.

**Rationale**: Different compliance regimes require different retention periods:

| Compliance | Typical Retention | Recommended Granularity |
|------------|-------------------|-------------------------|
| GDPR | 6-24 months | Monthly |
| SOX (Sarbanes-Oxley) | 7 years | Yearly |
| HIPAA | 6 years | Quarterly |
| PCI-DSS | 1 year | Monthly |
| Custom/Internal | Varies | Configurable |

**Key identification format**: `audit-temporal:{granularity}:{period}`

| Granularity | Key ID Example | Period Format |
|-------------|---------------|---------------|
| Monthly | `audit-temporal:monthly:2026-03` | `yyyy-MM` |
| Quarterly | `audit-temporal:quarterly:2026-Q1` | `yyyy-QN` |
| Yearly | `audit-temporal:yearly:2026` | `yyyy` |

Monthly is the default because it provides the finest practical granularity for most compliance regimes. Coarser granularities (quarterly, yearly) reduce operational overhead at the cost of less precise purge boundaries.

### 2. Encryption Scope: Partial (PII Fields Only)

Only PII-sensitive fields of `AuditEntry` are encrypted:

| Field | Encrypted | Rationale |
|-------|-----------|-----------|
| `UserId` | ✅ Yes | Personal identifier |
| `IpAddress` | ✅ Yes | Personal data under GDPR |
| `UserAgent` | ✅ Yes | Device fingerprinting data |
| `RequestPayload` | ✅ Yes | May contain PII |
| `ResponsePayload` | ✅ Yes | May contain PII |
| `Metadata` | ✅ Yes | May contain arbitrary PII |
| `Id` | ❌ No | Technical identifier, not PII |
| `CorrelationId` | ❌ No | Tracing identifier, not PII |
| `Action` | ❌ No | Operation type, not PII |
| `EntityType` | ❌ No | Schema information |
| `EntityId` | ❌ No | Business identifier (not PII) |
| `Outcome` | ❌ No | Enum, non-sensitive |
| `ErrorMessage` | ❌ No | Technical error (redacted at capture) |
| `TimestampUtc` | ❌ No | Required for time-based queries |
| `StartedAtUtc` | ❌ No | Required for duration calculation |
| `CompletedAtUtc` | ❌ No | Required for duration calculation |
| `TenantId` | ❌ No | Required for multi-tenant queries |

**Rationale**: Full encryption would prevent projections from indexing any field, making `QueryAsync` pagination impossible without decrypting every event. Partial encryption keeps structural/operational metadata queryable while protecting personal data.

After shredding, encrypted fields return `[SHREDDED]` placeholder values (consistent with `IForgottenSubjectHandler` pattern in `Encina.Marten.GDPR`).

### 3. New `ITemporalKeyProvider` Interface (Separate from `ISubjectKeyProvider`)

A new `ITemporalKeyProvider` interface is introduced rather than extending `ISubjectKeyProvider`.

**Rationale for separation**:

| Concern | `ISubjectKeyProvider` | `ITemporalKeyProvider` |
|---------|----------------------|------------------------|
| Partitioning | Per data subject | Per time period |
| Key lifecycle | Indefinite until erasure request | Automatic expiry after retention |
| Rotation trigger | Manual (security policy) | Calendar-based (period change) |
| Deletion trigger | GDPR Art. 17 request | Retention period expiry |
| Scope | Single person's data | All data in a time window |
| Coexistence | May coexist with temporal | May coexist with subject |

Both providers can coexist: an audit entry for a GDPR-covered user could have PII encrypted by *both* a subject key and a temporal key. The temporal key provides time-based purge; the subject key provides individual erasure. Decryption requires both keys to be present.

### 4. `PurgeEntriesAsync` Semantics: Period-Based Shredding

For the Marten implementation, `PurgeEntriesAsync(DateTime olderThanUtc)` destroys temporal keys for all periods that end before `olderThanUtc`.

**Return value semantics change**:

| Provider | `PurgeEntriesAsync` Returns |
|----------|---------------------------|
| ADO/Dapper/EF Core/MongoDB | Count of deleted rows |
| Marten | Count of entries in shredded periods (estimated from projection) |

The `IAuditStore` contract returns `Either<EncinaError, int>` where `int` represents "number of entries affected". For Marten, this is the count of audit entries whose temporal keys were destroyed. The count is obtained from the audit projection's period summary before key deletion.

This semantic is consistent: the caller learns how many entries are no longer readable, regardless of mechanism (physical deletion vs crypto-shredding).

### 5. Projection Strategy: Inline Projection with Shredded Entry Handling

The `MartenAuditStore` uses an **inline projection** that maintains a denormalized read model (`AuditEntryReadModel`) for efficient querying.

**Projection behavior**:

1. **On event append**: Decrypt PII fields using `ITemporalKeyProvider`, store decrypted values in read model
2. **On query (`QueryAsync`)**: Query the read model directly (already decrypted, indexed)
3. **On purge**: Delete temporal keys → mark affected read model entries as `[SHREDDED]`
4. **On rebuild**: Events with deleted temporal keys produce `[SHREDDED]` entries

**Shredded entries in query results**:

- Shredded entries are **included** in query results (not skipped)
- PII fields contain `[SHREDDED]` placeholder string
- Non-PII fields (Action, EntityType, Outcome, timestamps) remain readable
- Pagination remains predictable (no skipped rows)
- A `bool IsShredded` property on the read model indicates shredded status

**Rationale for including shredded entries**: Compliance officers need to know that audit activity existed in a period, even if PII is no longer available. Skipping entries would create misleading gaps and unpredictable pagination.

### 6. Key Storage: Marten Document Store

Temporal keys are stored as Marten documents in the same PostgreSQL database, in a dedicated `temporal_keys` collection. This keeps all audit infrastructure in one database, simplifying backup/restore and operational concerns.

**Key document structure**:

```
TemporalKeyDocument
├── Id: string              (e.g., "audit-temporal:monthly:2026-03")
├── Granularity: enum       (Monthly, Quarterly, Yearly)
├── PeriodStart: DateTime   (inclusive)
├── PeriodEnd: DateTime     (exclusive)
├── EncryptedKey: byte[]    (AES-256 key, encrypted with master key)
├── CreatedAtUtc: DateTime
├── Status: enum            (Active, Expired, Deleted)
```

### 7. Dual Encryption Interaction

When both `ISubjectKeyProvider` and `ITemporalKeyProvider` are configured:

```
AuditEntry.UserId = Encrypt(temporal_key, Encrypt(subject_key, "user-42"))
```

- **Subject erasure** (GDPR Art. 17): Deletes subject key → that user's PII becomes `[SHREDDED]` across all time periods
- **Temporal purge** (retention): Deletes temporal key → all PII in that period becomes `[SHREDDED]` for all users
- **Decryption**: Requires both keys. If either is missing, the field is `[SHREDDED]`

This layered approach satisfies both compliance requirements independently.

## Consequences

### Positive

1. **Event store immutability preserved**: No events are ever deleted or mutated
2. **Compliance-equivalent to physical deletion**: Data is cryptographically unrecoverable
3. **Configurable granularity**: Adapts to SOX, GDPR, HIPAA, PCI-DSS requirements
4. **Predictable pagination**: Shredded entries included in results, no gaps
5. **Coexists with GDPR per-subject shredding**: Orthogonal concerns, composable
6. **Audit trail of audit trail**: The event stream itself records when purges occurred

### Negative

1. **PostgreSQL-only**: Marten requires PostgreSQL (consistent with ADR-019)
2. **Operational complexity**: Master key management for temporal key encryption
3. **Projection rebuild cost**: Rebuilding projections with shredded keys produces `[SHREDDED]` entries
4. **Return value approximation**: `PurgeEntriesAsync` count is estimated from projection, not exact
5. **Dual encryption overhead**: When both subject and temporal keys are active, double encryption/decryption cost

### Mitigations

- **Master key**: Use Azure Key Vault / AWS KMS / HashiCorp Vault via `IMasterKeyProvider` abstraction
- **Projection rebuild**: Partial rebuild (only affected periods) reduces cost
- **Performance**: AES-256 encryption is hardware-accelerated on modern CPUs; overhead is negligible for audit workloads

## Alternatives Considered

### 1. Extend `ISubjectKeyProvider` with Temporal Support

Rejected: The two concerns have fundamentally different lifecycles, triggers, and semantics. Merging them would create a confusing API and violate SRP.

### 2. Full Event Encryption

Rejected: Prevents projection indexing, makes `QueryAsync` with pagination impossible without full table scans and in-memory decryption. Audit stores are query-heavy; this would be impractical.

### 3. Separate Event Streams Per Period

Rejected: Marten manages streams per aggregate. Creating artificial aggregates per time period would fight the framework's design and complicate querying across periods.

### 4. Physical Deletion with Marten Overrides

Rejected: Violates event store immutability. Marten explicitly prevents event deletion by design. Working around this creates data integrity risks and breaks projections.

### 5. Skip Shredded Entries in Query Results

Rejected: Creates unpredictable pagination (page sizes vary), hides evidence that audit activity existed, and complicates compliance reporting.

## References

- Issue: [#799](https://github.com/dlrivada/Encina/issues/799)
- Depends on: [#395](https://github.com/dlrivada/Encina/issues/395) (Audit Trail Logging — `IAuditStore` interface)
- Related: [ADR-019](019-compliance-event-sourcing-marten.md) (Compliance Modules Event Sourcing Strategy with Marten)
- Related: [ADR-018](018-cross-cutting-integration-principle.md) (Cross-Cutting Integration Principle)
- Pattern reference: `Encina.Marten.GDPR` — `ISubjectKeyProvider`, `CryptoShreddedAttribute`
