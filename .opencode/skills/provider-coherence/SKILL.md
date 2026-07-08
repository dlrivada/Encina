---
name: provider-coherence
description: Ensures provider-dependent features are implemented consistently across ALL 10 DB providers (ADO.NET + Dapper + EF Core × 3 DBs + MongoDB), 8 caching, 10+ messaging transports. Triggers on new store, repository, service, or provider-specific SQL code.
---

# Provider Coherence Skill

Ensures every provider-dependent feature in Encina is implemented consistently across ALL providers.

## The 10 Database Providers

| Category | Providers | Count |
|----------|-----------|-------|
| ADO.NET | SqlServer, PostgreSQL, MySQL | 3 |
| Dapper | SqlServer, PostgreSQL, MySQL | 3 |
| EF Core | SqlServer, PostgreSQL, MySQL | 3 |
| MongoDB | MongoDB | 1 |

## SQL Differences to Check

| Provider | Parameters | LIMIT | Boolean | Notes |
|----------|------------|-------|---------|-------|
| SQL Server | `@param` | `TOP (@n)` | `bit` | Native DateTime, GUID |
| PostgreSQL | `@param` | `LIMIT @n` | `true/false` | Case-sensitive identifiers |
| MySQL | `@param` | `LIMIT @n` | `0/1` | Backtick identifiers |

## Provider Categories

| Category | Count | Triggers This Skill |
|----------|-------|---------------------|
| **Database (10)** | 10 | Any new store, repository, UoW feature |
| **Caching (8)** | 8 | Any `ICacheProvider` implementation |
| **Transport (10+)** | 10+ | Any `IMessageTransport` feature |
| **Lock (4+)** | 4+ | Any `IDistributedLockProvider` |
| **Validation (3)** | 3 | Any `IValidationProvider` |
| **Scheduling (2+)** | 2+ | Any `IScheduledMessageStore` |
| **Cloud (3)** | 3 | Any cloud provider feature |
| **Resilience (3)** | 3 | Any resilience pattern |
| **Observability (1+)** | 1+ | Any observability feature |

## Coherence Checklist

- [ ] Interface is shared across all providers (same interface definition)
- [ ] Configuration options match across providers
- [ ] Provider-specific SQL is correct (`@param`, `TOP/LIMIT`, boolean types)
- [ ] Service registration in `ServiceCollectionExtensions` supports all providers
- [ ] Tests cover ALL providers (check manifest)
- [ ] No provider-specific code leaks into abstractions

## When to Trigger

- Creating a new store (OutboxStore, InboxStore, SagaStore, etc.)
- Implementing repositories or Unit of Work
- Bulk operations across providers
- Provider-specific registration (`ServiceCollectionExtensions`)

## Exclusions

- Message brokers (RabbitMQ, Kafka, NATS, MQTT) — separate category
- Event sourcing (Marten) — separate category
- Oracle (pre-1.0 scope removed)
- SQLite (code preserved but untested)
