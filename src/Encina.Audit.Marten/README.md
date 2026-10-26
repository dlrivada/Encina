# Encina.Audit.Marten

Event-sourced `IOperationAuditStore` implementation using Marten (PostgreSQL) with temporal crypto-shredding for compliance-grade audit trails.

## Why This Package?

| | DB Providers (13) | **Encina.Audit.Marten** |
|---|---|---|
| Storage | Mutable rows (INSERT/UPDATE/DELETE) | Immutable event streams (append-only) |
| Tamper evidence | None | Event stream integrity |
| Purge | DELETE rows | Destroy encryption keys (crypto-shredding) |
| Post-purge queries | Data lost | Structural fields remain queryable |
| Compliance | GDPR retention | SOX + NIS2 + GDPR simultaneously |

## Quick Start

```csharp
// 1. Configure Marten
services.AddMarten(options =>
{
    options.Connection("Host=localhost;Database=myapp;...");
});

// 2. Register a PII masker (required: the host fails at startup without one),
//    or call AddEncinaPII() from Encina.Security.PII instead
services.AddSingleton<IPiiMasker, DefaultSensitiveDataRedactor>();

// 3. Add core audit pipeline
services.AddEncinaAudit(options =>
{
    options.AuditAllCommands = true;
});

// 4. Add Marten event-sourced audit store (replaces InMemory)
services.AddEncinaAuditMarten(options =>
{
    options.TemporalGranularity = TemporalKeyGranularity.Monthly;
    options.RetentionPeriod = TimeSpan.FromDays(2555); // 7 years (SOX)
    options.EnableAutoPurge = true;
    options.AddHealthCheck = true;
});
```

## Configuration

| Property | Default | Description |
|----------|---------|-------------|
| `TemporalGranularity` | `Monthly` | Key partitioning: Monthly, Quarterly, Yearly |
| `EncryptionScope` | `PiiFieldsOnly` | Which fields to encrypt |
| `RetentionPeriod` | 2555 days | Retention before crypto-shredding |
| `EnableAutoPurge` | `false` | Background crypto-shredding service |
| `PurgeIntervalHours` | 24 | Auto-purge interval |
| `ShreddedPlaceholder` | `[SHREDDED]` | Placeholder for PII whose key was destroyed with an erasure reference |
| `AddHealthCheck` | `false` | Register health check |

## How Crypto-Shredding Works

1. **Record**: PII fields encrypted with temporal key for the entry's month/quarter/year
2. **Query**: Async projections decrypt PII into queryable read models
3. **Purge**: `PurgeEntriesAsync` destroys temporal keys (not events)
4. **After purge**: read-model entries already projected keep their state. Purge destroys keys without an erasure reference, so projecting events of a purged period (for example in a rebuild) throws `EncryptedFieldDecryptionException` (`KeyDestroyedWithoutErasureRecord`) and the Marten daemon pauses the shard. `[SHREDDED]` appears only when the destroyed-key marker carries an erasure reference (request id, request date and actor). See [Recover from an unreadable audit event](../../docs/features/audit-marten.md#recover-from-an-unreadable-audit-event).

## Performance

Encryption overhead is < 1% of total audit recording cost:

| Operation | Latency |
|-----------|---------|
| Encrypt 1 PII field (16B) | ~900 ns |
| Encrypt full entry (6 PII fields + 2KB payload) | ~8.6 us |
| Key lookup | ~60 ns |
| PostgreSQL SaveChangesAsync | 1-5 ms |

Load test: **508K entries/sec** (8 workers, P50: 8.7 us, P99: 0.24 ms).

## Dependencies

- `Encina.Marten` — event store infrastructure
- `Encina.Marten.GDPR` — crypto-shredding patterns
- `Encina.Security.Audit` — `IOperationAuditStore`, `IReadAuditStore`
- `Encina.Security.Encryption` — AES-256-GCM
