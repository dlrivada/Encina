# Example: CDC Outbox Integration

This example shows how to replace the polling-based `OutboxProcessor` with CDC-driven outbox processing using `UseOutboxCdc()`.

## The Problem

The traditional outbox pattern uses polling to check for pending messages:

```text
┌───────────┐  poll every N sec  ┌──────────────────┐  publish   ┌───────────┐
│ Outbox DB │◀──────────────────│ OutboxProcessor   │──────────▶│ Transport │
│ (table)   │                    │ (polling-based)  │            │ (broker)  │
└───────────┘                    └──────────────────┘            └───────────┘
```

Polling introduces latency (up to `PollingInterval` delay) and constant database load even when no new messages exist.

## The CDC Solution

CDC-driven outbox processing publishes notifications immediately when new outbox rows are inserted:

```text
┌───────────┐  CDC stream   ┌─────────────────┐  publish  ┌───────────┐
│ Outbox DB │─────────────▶│ OutboxCdcHandler│──────────▶│ IEncina   │
│ (table)   │               │ (real-time)     │           │ .Publish()│
└───────────┘               └─────────────────┘           └───────────┘
```

## Configuration

```csharp
// Register Encina core with outbox enabled
services.AddEncina(config =>
{
    config.UseOutbox = true;
});

// Register CDC with outbox CDC bridge
services.AddEncinaCdc(config =>
{
    config.UseCdc()
          .UseOutboxCdc("OutboxMessages");  // Table name of your outbox
});

// Register the database provider
services.AddEncinaCdcSqlServer(opts =>
{
    opts.ConnectionString = connectionString;
    opts.TrackedTables = ["dbo.OutboxMessages"];
});
```

## How It Works

1. Your application saves an `OutboxMessage` to the database (via `IEncina.Send()` or `IEncina.Publish()`)
2. CDC captures the INSERT on the `OutboxMessages` table
3. `OutboxCdcHandler` reads the `NotificationType` and `Content` fields from the row
4. The handler deserializes the original notification and publishes it via `IEncina.Publish()`
5. The notification flows through Encina's standard pipeline (handlers, transports, etc.)

## Safety: Coexistence with Polling Processor

`OutboxCdcHandler` skips rows where `ProcessedAtUtc` is already set. This means:

- If the traditional `OutboxProcessor` processes a message first, CDC skips it
- If CDC processes a message first, the `OutboxProcessor` also skips it (already processed)
- Both can run simultaneously without duplicate processing

## When to Use Each Approach

| Approach | Latency | Database Load | Complexity |
|----------|---------|---------------|------------|
| **Polling (OutboxProcessor)** | Up to `PollingInterval` | Constant (queries even when idle) | Simple |
| **CDC (UseOutboxCdc)** | Near real-time | Event-driven (no idle queries) | Requires CDC setup |
| **Both (hybrid)** | Best of both | Slightly higher | Most robust |

**Recommendation**: Use CDC for low-latency requirements. Keep polling as a fallback for reliability. Both can run safely together.

## Related

- [CDC Feature Guide](../features/cdc.md)
- [Messaging Bridge Example](cdc-messaging-bridge.md)
