# ADR-028: Domain Events and Integration Events Are Distinct, and Integration Events Leave the Process Only Through the Outbox

## Status

**Accepted** - decided in issues #312, #373 and #384 (2026); recorded as an ADR on 2026-09-22 from the project history.

## Context

A single "event" concept was serving two purposes: facts raised inside an aggregate and consumed by handlers in the same process, and messages published to other systems. Mixing them coupled external contracts to internal domain types, made bounded-context isolation impossible to enforce, and left the choice between in-process dispatch and reliable publication to each call site.

## Decision

- **Domain events** (`IDomainEvent`) are internal facts: raised by aggregates, immutable records, dispatched in-process (EF Core `SaveChanges` interceptor or the collector interface for the other providers). They never cross a process boundary as they are.
- **Integration events** (`IIntegrationEvent`) are the external contract of a bounded context: a separate type, stable, versioned, mapped from domain events through `IDomainEventToIntegrationEventMapper` in `Encina.DomainModeling` (#312).
- **Integration events are published only through the Outbox pattern** (#384: "Integration events always go through Outbox"). Direct publication to a transport from a handler is not a supported path; the Outbox gives at-least-once delivery inside the same transaction as the state change.

## Rationale

- Bounded-context isolation and stable external contracts (#373): a domain refactoring must not break consumers.
- Reliability: the Outbox is the only mechanism that ties publication to the committed transaction across the ten database providers.
- One rule instead of per-call-site choices: reviewers and the cross-cutting rule (`CLAUDE.md`, Transactions and Idempotency rows) can check it mechanically.

## Alternatives rejected

- **One event type for both uses:** rejected; it leaks internal types into external contracts.
- **Publishing integration events directly from handlers when "simple enough":** rejected; it reintroduces dual writes and undefined delivery guarantees.

## Consequences

- Every transport package consumes integration events from the Outbox relay, never domain events.
- A new aggregate that must notify other systems needs a mapper and an Outbox-enabled configuration; the cross-cutting evaluation of a feature must state which events cross the boundary.
- Tests for publication behaviour target the Outbox stores of the providers, not the transports.

## References

- Issues #312, #373, #384; `CLAUDE.md`, "Messaging Patterns" and "Cross-Cutting Integration Rule"; `docs/engineering/PROJECT-HISTORY.md`, "Data access and database providers" and "Messaging patterns and transports".
