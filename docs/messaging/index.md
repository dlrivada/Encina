# Messaging Patterns

Encina provides comprehensive support for messaging patterns in distributed systems.

## Available Patterns

| Pattern | Purpose | Package |
|---------|---------|---------|
| [Sagas](sagas.md) | Distributed transactions with compensation | `Encina.Messaging` |
| Outbox | Reliable event publishing (at-least-once) | `Encina.Messaging` |
| Inbox | Idempotent message processing | `Encina.Messaging` |
| Scheduled Messages | Delayed/recurring execution | `Encina.Messaging` |

## Persistence Providers

All messaging patterns support multiple persistence providers:

| Provider | Package | Use Case |
|----------|---------|----------|
| Entity Framework Core | `Encina.EntityFrameworkCore` | Full ORM support, migrations |
| Dapper (SQL Server) | `Encina.Dapper.SqlServer` | High performance, SQL Server |
| Dapper (PostgreSQL) | `Encina.Dapper.PostgreSQL` | High performance, PostgreSQL |
| Dapper (MySQL) | `Encina.Dapper.MySQL` | High performance, MySQL/MariaDB |
| Dapper (SQLite) | `Encina.Dapper.Sqlite` | Testing, embedded |
| Dapper (Oracle) | `Encina.Dapper.Oracle` | Enterprise, Oracle DB |
| ADO.NET (all databases) | `Encina.ADO.*` | Maximum performance |
| MongoDB | `Encina.MongoDB` | Document store |

## Quick Start

```csharp
// Enable messaging patterns
services.AddEncina(config =>
{
    config.UseTransactions = true;  // Automatic transaction management
    config.UseOutbox = true;        // Reliable event publishing
    config.UseInbox = true;         // Idempotent processing
    config.UseSagas = true;         // Distributed transactions
});

// Choose a persistence provider
services.AddEncinaEntityFrameworkCore<AppDbContext>();
```

## Next Steps

- [Saga Patterns](sagas.md) - Learn about Orchestration vs Choreography
- [Outbox Pattern](outbox.md) - Reliable event publishing *(coming soon)*
- [Inbox Pattern](inbox.md) - Idempotent message processing *(coming soon)*
