# v0.10.0 - DDD Foundations

> **Release Date**: 2026-01-19
> **Milestone**: [v0.10.0 - DDD Foundations](https://github.com/dlrivada/Encina/milestone/7)
> **Status**: Completed (31 issues closed)

## Overview

Domain-Driven Design foundational patterns: Value Objects, Entities, Aggregates, Specifications, Domain Events, ACL, Ports & Adapters. These are prerequisites for all other features.

## Completed Features

### Core Domain Primitives

- Entity base class with identity-based equality [#369](https://github.com/dlrivada/Encina/issues/369)
- Value Objects with structural equality [#367](https://github.com/dlrivada/Encina/issues/367)
- Strongly Typed IDs (Guid, Int, Long, String) [#374](https://github.com/dlrivada/Encina/issues/374)
- AggregateRoot with domain event support
- AuditableAggregateRoot (CreatedAt/ModifiedAt)
- SoftDeletableAggregateRoot (soft delete pattern)

### Domain Events

- Domain Events vs Integration Events separation [#312](https://github.com/dlrivada/Encina/issues/312)
- Rich Domain Event Envelope [#368](https://github.com/dlrivada/Encina/issues/368) - DomainEventMetadata, DomainEventEnvelope<T>, extensions
- Integration Event Extensions [#373](https://github.com/dlrivada/Encina/issues/373) - Async mappers, fallible mappers, publishers

### Query & Specification

- Specification Pattern for query composition [#295](https://github.com/dlrivada/Encina/issues/295) - With And/Or/Not composition, QuerySpecification
- Generic Repository Pattern [#380](https://github.com/dlrivada/Encina/issues/380) - IRepository<T,TId>, IReadOnlyRepository, PagedResult

### Business Logic

- Business Rules Pattern for domain invariants [#372](https://github.com/dlrivada/Encina/issues/372) - Separate from input validation
- Domain Services abstraction [#377](https://github.com/dlrivada/Encina/issues/377) - IDomainService marker interface
- Result Pattern extensions (Either Fluent API) [#468](https://github.com/dlrivada/Encina/issues/468) - Map, Bind, Combine, Ensure, Tap, etc.

### Architectural Patterns

- Anti-Corruption Layer mapper interface [#299](https://github.com/dlrivada/Encina/issues/299)
- Ports & Adapters Factory Pattern [#475](https://github.com/dlrivada/Encina/issues/475) - IPort, IInboundPort, IOutboundPort, AdapterBase
- Bounded Context helpers [#379](https://github.com/dlrivada/Encina/issues/379) - BoundedContextAttribute, ContextMap, BoundedContextModule
- Bounded Context & Module Boundaries [#477](https://github.com/dlrivada/Encina/issues/477) - IBoundedContextModule, BoundedContextValidator
- Vertical Slice + Hexagonal Hybrid [#476](https://github.com/dlrivada/Encina/issues/476) - FeatureSlice, IUseCaseHandler, SliceDependency

### Application Layer

- Result/DTO Mapping with ROP [#478](https://github.com/dlrivada/Encina/issues/478) - IResultMapper, IAsyncResultMapper, MappingError
- Application Services Interface [#479](https://github.com/dlrivada/Encina/issues/479) - IApplicationService, ApplicationServiceError

### Domain DSL

- Domain Language DSL [#381](https://github.com/dlrivada/Encina/issues/381) - DomainBuilder, AggregateBuilder, Quantity/Percentage/DateRange/TimeRange

### EF Core Integration

- Immutable Records Support for EF Core [#569](https://github.com/dlrivada/Encina/issues/569) - UpdateImmutable, WithPreservedEvents, domain event preservation

## Implementation Notes

This milestone was implemented in January 2026 alongside v0.11.0 (Testing Infrastructure). The DDD foundations provide the building blocks used by all subsequent features.

For detailed implementation history of this period, see [v0.11.0 release notes](../v0.11.0/README.md) which covers the January 2026 development cycle.

## Related Documentation

- [ROADMAP.md](../../../ROADMAP.md) - Milestone details
- [Immutable Domain Models](../../features/immutable-domain-models.md)
