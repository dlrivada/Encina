# Messaging in Encina

Encina provides comprehensive support for messaging patterns and transports in distributed systems.

## Messaging Transports

Encina supports **10 messaging transports**, each exposing its native API:

| Category | Transports | Use Case |
|----------|-----------|----------|
| **Message Broker** | RabbitMQ, AzureServiceBus, AmazonSQS | Task distribution |
| **Event Streaming** | Kafka, NATS JetStream | Event sourcing, replay |
| **Pub/Sub** | Redis.PubSub, NATS Core, MQTT | Real-time, IoT |
| **In-Memory** | Encina.InMemory | Testing |
| **API Bridge** | gRPC, GraphQL | Request/response |

See [Messaging Transports](transports.md) for the decision flowchart and detailed comparison.

## Messaging Patterns

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

- [Messaging Transports](transports.md) - Choose the right transport for your use case
- [Saga Patterns](sagas.md) - Learn about Orchestration vs Choreography
- [Outbox Pattern](outbox.md) - Reliable event publishing *(coming soon)*
- [Inbox Pattern](inbox.md) - Idempotent message processing *(coming soon)*
