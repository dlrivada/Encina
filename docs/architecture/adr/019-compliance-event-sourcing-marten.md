# ADR-019: Compliance Modules Event Sourcing Strategy with Marten

**Status:** Accepted
**Date:** 2026-03-14
**Deciders:** David Lozano Rivada
**Technical Story:** [#776 - SPIKE: ADR-019 Compliance Modules Event Sourcing Strategy with Marten](https://github.com/dlrivada/Encina/issues/776)

## Context

All Encina compliance modules (Consent, DataSubjectRights, LawfulBasis, BreachNotification, DPIA, ProcessorAgreements, Retention, DataResidency, CrossBorderTransfer) currently use **entity-based persistence** with store interfaces designed for the 13 database providers (ADO.NET x4, Dapper x4, EF Core x4, MongoDB).

However, GDPR Article 5(2) — the **accountability principle** — requires that organizations can **prove** compliance, not merely assert it. This means organizations must demonstrate:

- **When** a consent was given, modified, or withdrawn
- **Who** processed a data subject request and **what steps** were taken
- **How** a breach was assessed, escalated, and notified within the 72-hour window
- **Why** a specific lawful basis was chosen for each processing activity
- **What** was evaluated during a Data Protection Impact Assessment

Entity-based persistence can only answer: *"What is the current state?"* It cannot answer: *"How did we arrive at this state?"* — which is precisely what regulators ask during audits.

### The Specialized Provider Analogy

Encina's 13-database-provider rule exists for **general-purpose CRUD** features (outbox, inbox, saga, scheduling, repositories). It ensures broad database compatibility for features where any relational database or document store can serve equally well.

Event sourcing is a **specialized infrastructure concern**, analogous to caching:

| Concern | General-Purpose | Specialized Provider |
|---------|----------------|---------------------|
| **Caching** | Could store cache in any DB table | Redis/Valkey/Dragonfly (8 providers) |
| **Event Sourcing** | Could store events in any DB table | Marten/EventStoreDB (specialized) |
| **Full-Text Search** | Could use SQL LIKE queries | Elasticsearch/Algolia (specialized) |

Just as we don't implement caching across 13 databases, we shouldn't implement event sourcing across 13 databases. Marten is to event sourcing what Redis is to caching — the right tool for the job.

### Existing Encina.Marten Infrastructure

Encina already has mature, production-ready event sourcing infrastructure in `Encina.Marten`:

| Component | Interface/Class | Status |
|-----------|----------------|--------|
| **Aggregates** | `IAggregate`, `AggregateBase` (in `Encina.DomainModeling`) | Production |
| **Repository** | `IAggregateRepository<T>` with ROP (`Either<EncinaError, T>`) | Production |
| **Snapshots** | `ISnapshotStore<T>`, `SnapshotAwareAggregateRepository<T>` | Production |
| **Projections** | `IProjection<T>`, `IProjectionHandler<TEvent, TReadModel>` | Production |
| **Event Versioning** | `IEventUpcaster<TFrom, TTo>`, `EventUpcasterRegistry` | Production |
| **Metadata** | `IEventMetadataEnricher`, correlation/causation IDs | Production |
| **Health Checks** | `MartenHealthCheck` | Production |
| **DI Registration** | `AddEncinaMarten()`, `AddAggregateRepository<T>()` | Production |

No new infrastructure is needed. Compliance modules only need to define their aggregates, events, and projections using the existing `Encina.Marten` foundation.

## Decision

**Migrate all stateful compliance modules from entity-based 13-provider persistence to Marten event sourcing.**

### Which Modules Migrate

| Module | Migrate? | GDPR Article | Rationale |
|--------|:--------:|:------------:|-----------|
| **Consent** | Yes | Art. 7 | Consent lifecycle must be provable: given, modified, withdrawn |
| **DataSubjectRights** | Yes | Arts. 15-22 | Request processing steps and timelines must be auditable |
| **LawfulBasis** | Yes | Art. 6 | Lawful basis determination and changes must be traceable |
| **BreachNotification** | Yes | Arts. 33-34 | 72-hour notification compliance requires timestamped evidence |
| **DPIA** | Yes | Art. 35 | Assessment process must demonstrate methodological rigor |
| **ProcessorAgreements** | Yes | Art. 28 | DPA lifecycle (negotiation, signing, amendment, termination) |
| **Retention** | Yes | Art. 5(1)(e) | Prove retention policies were applied and data was deleted on schedule |
| **DataResidency** | Yes | Ch. V | Prove data sovereignty was maintained across regions |
| **CrossBorderTransfer** | Yes | Ch. V, Schrems II | New module — implements with ES from start (establishes pattern) |
| **Anonymization** | No | — | Stateless data transformation tool, no lifecycle to track |
| **PrivacyByDesign** | No | Art. 25 | Pipeline behavior enforcement, no own persistent state |

### Migration Pattern

Each compliance module follows the same transformation:

**Before (Entity-Based):**

```
Module/
├── Abstractions/
│   ├── IEntityStore.cs          # CRUD operations
│   └── IAuditStore.cs           # Separate audit trail
├── Model/
│   └── Entity.cs                # Mutable entity
├── InMemory/
│   └── InMemoryEntityStore.cs   # Development implementation
└── ServiceCollectionExtensions.cs
```

**After (Event-Sourced):**

```
Module/
├── Aggregates/
│   └── EntityAggregate.cs       # Event-sourced aggregate (extends AggregateBase)
├── Events/
│   ├── EntityCreated.cs         # Domain event records
│   ├── EntityUpdated.cs
│   └── EntityClosed.cs
├── Projections/
│   ├── EntityReadModel.cs       # Query-optimized view (implements IReadModel)
│   └── EntityProjection.cs      # Event → ReadModel transformation
├── Abstractions/
│   └── IEntityReadModelQuery.cs # Read-side queries (replaces IEntityStore reads)
├── Health/
│   └── ModuleHealthCheck.cs     # Health check (existing pattern)
└── ServiceCollectionExtensions.cs
```

### Aggregate Design Principles

1. **Aggregates encapsulate behavior** — State changes only through domain events
2. **Events are immutable facts** — `record` types with UTC timestamps and actor context
3. **Projections for queries** — Read models built from event streams for efficient queries
4. **Snapshots for performance** — Long-lived aggregates (Consent, ProcessorAgreements) use `ISnapshotable<T>`
5. **Upcasters for evolution** — Schema changes via `IEventUpcaster<TFrom, TTo>`, never modify existing events

### Example: Consent Module Transformation

**Aggregate:**

```csharp
public sealed class ConsentAggregate : AggregateBase
{
    public string DataSubjectId { get; private set; } = string.Empty;
    public string Purpose { get; private set; } = string.Empty;
    public ConsentStatus Status { get; private set; }
    public DateTimeOffset? ExpiresAtUtc { get; private set; }

    // State reconstruction from events
    protected override void Apply(object domainEvent)
    {
        switch (domainEvent)
        {
            case ConsentGranted e:
                DataSubjectId = e.DataSubjectId;
                Purpose = e.Purpose;
                Status = ConsentStatus.Active;
                ExpiresAtUtc = e.ExpiresAtUtc;
                break;
            case ConsentWithdrawn:
                Status = ConsentStatus.Withdrawn;
                break;
            case ConsentExpired:
                Status = ConsentStatus.Expired;
                break;
        }
    }

    // Command methods raise events
    public static ConsentAggregate Grant(
        Guid id, string dataSubjectId, string purpose,
        DateTimeOffset? expiresAtUtc, string grantedBy)
    {
        var aggregate = new ConsentAggregate { Id = id };
        aggregate.RaiseEvent(new ConsentGranted(
            dataSubjectId, purpose, expiresAtUtc,
            grantedBy, DateTimeOffset.UtcNow));
        return aggregate;
    }

    public void Withdraw(string withdrawnBy, string reason)
    {
        if (Status != ConsentStatus.Active)
            throw new InvalidOperationException("Cannot withdraw non-active consent.");

        RaiseEvent(new ConsentWithdrawn(withdrawnBy, reason, DateTimeOffset.UtcNow));
    }
}
```

**Events:**

```csharp
public sealed record ConsentGranted(
    string DataSubjectId, string Purpose,
    DateTimeOffset? ExpiresAtUtc,
    string GrantedBy, DateTimeOffset OccurredAtUtc);

public sealed record ConsentWithdrawn(
    string WithdrawnBy, string Reason,
    DateTimeOffset OccurredAtUtc);

public sealed record ConsentExpired(
    DateTimeOffset OccurredAtUtc);
```

**Projection (Read Model):**

```csharp
public sealed class ConsentReadModel : IReadModel
{
    public Guid Id { get; set; }
    public string DataSubjectId { get; set; } = string.Empty;
    public string Purpose { get; set; } = string.Empty;
    public ConsentStatus Status { get; set; }
    public DateTimeOffset GrantedAtUtc { get; set; }
    public DateTimeOffset? WithdrawnAtUtc { get; set; }
    public DateTimeOffset? ExpiresAtUtc { get; set; }
}
```

**Registration:**

```csharp
services.AddEncinaMarten(options => { /* connection config */ });
services.AddAggregateRepository<ConsentAggregate>();
services.AddProjection<ConsentProjection, ConsentReadModel>();
```

### PostgreSQL Requirement

Compliance modules using event sourcing **require PostgreSQL** via Marten. This is an acceptable constraint because:

1. **Specialized infrastructure** — Event sourcing is not general-purpose CRUD
2. **PostgreSQL is the #1 database** — 25%+ market share and growing
3. **Marten leverages PostgreSQL natively** — JSONB storage, advanced indexing, built-in projections
4. **Production compliance systems already use PostgreSQL** — Common in GDPR-regulated environments
5. **Non-compliance modules remain database-agnostic** — Outbox, inbox, saga, scheduling still support all 13 providers

### Store Interface Removal

Current `IEntityStore` interfaces with 13-provider implementations are **replaced**, not deprecated (pre-1.0, no backward compatibility):

- **Write operations** → Aggregate command methods (e.g., `ConsentAggregate.Grant()`, `.Withdraw()`)
- **Read operations** → Projection read models via `IReadModelRepository<TReadModel>`
- **Audit operations** → Inherent in the event stream (no separate `IAuditStore` needed)

### GDPR-Specific Event Sourcing Benefits

| GDPR Requirement | Entity-Based | Event-Sourced |
|-----------------|:------------:|:-------------:|
| Art. 5(2) Accountability | Current state only | Full history with timestamps |
| Art. 7 Consent proof | Boolean flag | Complete consent lifecycle |
| Art. 17 Right to Erasure | Delete record | Crypto-shredding (encrypt events, destroy key) |
| Art. 30 Records of processing | Separate audit log | Event stream IS the record |
| Art. 33 72h breach notification | Timestamp check | Precise timeline reconstruction |
| Art. 35 DPIA documentation | Final assessment | Full assessment process trail |

### Crypto-Shredding for Right to Erasure (Art. 17)

Event sourcing's immutability appears to conflict with GDPR's Right to Erasure. **Crypto-shredding** resolves this:

1. Personal data in events is encrypted with a per-data-subject key
2. When erasure is requested, the encryption key is destroyed
3. Events remain in the stream but personal data becomes unrecoverable
4. Non-personal metadata (timestamps, event types, aggregate IDs) remain intact for audit

This is a well-established pattern in GDPR-compliant event-sourced systems. Implementation details will be addressed in individual module refactoring issues.

## Consequences

### Positive

- **GDPR Art. 5(2) accountability** — Every state change is an immutable, timestamped fact
- **Complete audit trail** — No separate audit infrastructure needed; the event stream IS the audit trail
- **Historical state reconstruction** — Can answer "what was the state on date X?" for any compliance entity
- **Regulatory evidence** — Event streams serve as direct evidence during DPA audits
- **Leverages existing infrastructure** — `Encina.Marten` is production-ready, no new framework needed
- **Eliminates 13-provider duplication** — No need to implement/maintain compliance stores for 13 databases
- **Natural CQRS** — Write model (aggregates) and read model (projections) are cleanly separated
- **Schema evolution** — Event upcasters handle changes without data migration scripts

### Negative

- **PostgreSQL requirement for compliance** — Organizations using only SQL Server/MySQL cannot use compliance modules without adding PostgreSQL
- **Higher learning curve** — Event sourcing is more complex than CRUD for developers unfamiliar with the pattern
- **Breaking change** — All existing compliance module store interfaces are replaced (acceptable pre-1.0)
- **Projection maintenance** — Read models must be rebuilt when projection logic changes

### Neutral

- Non-compliance modules (outbox, inbox, saga, scheduling, repositories) are unaffected — they continue to support all 13 database providers
- The `Encina.Marten` package already exists and is maintained; this decision increases its importance but not its maintenance burden
- Compliance modules that don't migrate (Anonymization, PrivacyByDesign) remain unchanged

## Alternatives Considered

### Option A: Keep Entity-Based (13 Providers)

Maintain current approach with entity-based stores across 13 database providers.

**Rejected because:** Cannot satisfy GDPR Art. 5(2) accountability. Entity-based persistence only captures current state, not the history of how that state was reached. Bolting on a separate audit log is fragile, duplicative, and can become inconsistent with the entity state.

### Option C: Custom Provider-Agnostic Event Store

Build a new `IEventStore` abstraction with implementations for all 13 database providers.

**Rejected because:** This is essentially reinventing Marten from scratch — event storage, projections, snapshots, versioning, concurrency — for every database provider. Estimated effort: 6-12 months for a suboptimal result. Marten has years of production hardening and PostgreSQL-specific optimizations that a generic implementation cannot match.

### Option D: Hybrid (Entity-Based + Separate Event Log)

Keep entity-based stores but add a parallel event log for audit purposes.

**Rejected because:** Creates dual-write consistency problems. The entity state and event log can diverge, which is precisely the failure mode that event sourcing eliminates. Additionally, it doubles the maintenance burden without the benefits of event sourcing (state reconstruction, temporal queries, natural CQRS).

## Migration Sequence

The migration follows a deliberate order, starting with a new module to establish the pattern:

| Phase | Module | Issue | Notes |
|-------|--------|-------|-------|
| 1 | **CrossBorderTransfer** | #412 | New module — establishes ES pattern from scratch |
| 2 | **Consent** | #403 | High regulatory visibility, clear event model |
| 3 | **DataSubjectRights** | #404 | Complex workflow, benefits most from ES |
| 4 | **BreachNotification** | #408 | Time-critical (72h), needs precise timeline |
| 5 | **LawfulBasis** | #413 | Simpler model, validates pattern at scale |
| 6 | **DPIA** | #409 | Assessment lifecycle maps well to ES |
| 7 | **ProcessorAgreements** | #410 | Contract lifecycle (negotiation → termination) |
| 8 | **Retention** | #406 | Policy application audit trail |
| 9 | **DataResidency** | #405 | Regional sovereignty evidence |

CrossBorderTransfer (#412) serves as the **reference implementation** — all subsequent modules follow the patterns established there.

## Cross-Cutting Integration (per ADR-018)

| # | Function | Assessment |
|---|----------|------------|
| 1 | Caching | Projections may be cached via `ICacheProvider` for frequently-queried read models |
| 2 | OpenTelemetry | `ActivitySource` for aggregate operations (load, save); `Meter` for event counts per module |
| 3 | Structured Logging | `[LoggerMessage]` in per-module `Log.cs` for aggregate lifecycle events |
| 4 | Health Checks | `MartenHealthCheck` covers PostgreSQL connectivity; per-module checks for projection lag |
| 5 | Validation | Aggregate command methods validate invariants; pipeline validation for incoming requests |
| 6 | Resilience | Marten session operations wrapped with retry for transient PostgreSQL failures |
| 7 | Distributed Locks | Marten uses optimistic concurrency by default; distributed locks for cross-aggregate operations |
| 8 | Transactions | Marten session is inherently transactional (events + projections in single commit) |
| 9 | Idempotency | Event deduplication via aggregate version + correlation ID |
| 10 | Multi-Tenancy | `TenantId` as aggregate field; Marten supports native tenancy via `IDocumentSession` |
| 11 | Module Isolation | `ModuleId` for modular monolith scoping of event streams |
| 12 | Audit Trail | Inherent — the event stream IS the audit trail |

## Related

- [ADR-014 — Data Residency GDPR Chapter V](014-data-residency-gdpr-chapter-v.md)
- [ADR-018 — Cross-Cutting Integration Principle](018-cross-cutting-integration-principle.md)
- [#412 — CrossBorderTransfer (reference implementation)](https://github.com/dlrivada/Encina/issues/412)
- [#668 — EPIC v0.13.0 Security & Compliance](https://github.com/dlrivada/Encina/issues/668)
- [#776 — This spike](https://github.com/dlrivada/Encina/issues/776)

## References

- [GDPR Article 5(2) — Accountability Principle](https://gdpr-info.eu/art-5-gdpr/)
- [Marten Documentation — Event Sourcing](https://martendb.io/events/)
- [Crypto-Shredding in Event-Sourced Systems](https://www.eventstore.com/blog/protecting-sensitive-data-in-event-sourced-systems-with-crypto-shredding)
- [Greg Young — Versioning in Event-Sourced Systems](https://leanpub.com/esversioning)
