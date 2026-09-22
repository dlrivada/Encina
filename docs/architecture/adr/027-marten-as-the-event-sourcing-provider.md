# ADR-027: Marten Is the Event-Sourcing Provider; EventStoreDB Is Deprecated

## Status

**Accepted** - decided in issues #17 (2025) and #321 (2026); recorded as an ADR on 2026-09-22 from the project history (Historian pass, `docs/engineering/PROJECT-HISTORY.md`).

## Context

Encina needed one event-sourcing backend for aggregates, projections, snapshots and the compliance modules. Two candidates existed as packages: `Encina.Marten` (Marten on PostgreSQL) and `Encina.EventStoreDB` (EventStoreDB/KurrentDB). Keeping both meant every event-sourcing feature had to be implemented, tested and documented twice, and the provider-coherence rule would have required parity between two stores with different consistency models, projection engines and operational footprints.

## Decision

`Encina.Marten` is the event-sourcing provider. `Encina.EventStoreDB` is deprecated and excluded from every new feature: it receives no new capabilities, no cross-cutting integrations and no place in the 1.0 package list (#17 comment: "deprecate Encina.EventStoreDB and keep only Encina.Marten for Event Sourcing"; #321: "❌ Encina.EventStoreDB - Excluded"). The compliance modules were built or migrated on Marten under ADR-019, which depends on this decision.

## Rationale

- Marten runs on PostgreSQL, which the project already operates for three provider families; EventStoreDB requires dedicated infrastructure.
- Marten projections are C#; EventStoreDB projections are JavaScript, outside the project's single-language policy.
- One provider keeps the aggregate abstractions (`IAggregate`, `AggregateBase`, `LoadFromHistory`, snapshots) in `Encina.DomainModeling` with a single implementation to verify against a real store.

## Alternatives rejected

- **Keep both with a common Strategy abstraction:** rejected because the two stores differ in fundamentals (stream versioning, projection lifecycle, tenancy), so the abstraction would either leak or hide what users need (#15, #17).
- **EventStoreDB as the primary:** rejected for the infrastructure and projection-language reasons above.

## Consequences

- Event-sourcing features are complete when they work on Marten; there is no second provider to cover.
- Marten major upgrades are architectural events (Marten 9 changed aggregate rebuilding; see #1088 and the replay through `IAggregate.LoadFromHistory`).
- `Encina.EventStoreDB` remains in the repository as deprecated code until a removal decision; it is not part of SPEC-000's 1.0 list.

## References

- Issues #17, #321, #15; ADR-019 (compliance event sourcing on Marten); `docs/engineering/PROJECT-HISTORY.md`, "Event sourcing and Marten".
