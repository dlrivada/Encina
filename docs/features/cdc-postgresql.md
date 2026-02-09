# CDC: PostgreSQL Provider

PostgreSQL CDC connector using **Logical Replication** to capture row-level changes from PostgreSQL databases.

## Overview

| Property | Value |
|----------|-------|
| **Package** | `Encina.Cdc.PostgreSql` |
| **CDC Mechanism** | Logical Replication (WAL) |
| **Position Type** | `PostgresCdcPosition` (LSN) |
| **Connector Class** | `PostgresCdcConnector` |
| **Extension Method** | `AddEncinaCdcPostgreSql()` |

PostgreSQL Logical Replication decodes the Write-Ahead Log (WAL) to stream row-level changes. It provides full before and after values when `REPLICA IDENTITY FULL` is set on the table.

## Prerequisites

### 1. Configure WAL Level

In `postgresql.conf`:

```ini
wal_level = logical
max_replication_slots = 4    # At least 1 per connector
max_wal_senders = 4
```

Restart PostgreSQL after changing `wal_level`.

### 2. Create a Publication

```sql
CREATE PUBLICATION encina_cdc_publication FOR TABLE orders, customers;
-- Or for all tables:
-- CREATE PUBLICATION encina_cdc_publication FOR ALL TABLES;
```

### 3. Set Replica Identity (for before-values)

```sql
ALTER TABLE orders REPLICA IDENTITY FULL;
ALTER TABLE customers REPLICA IDENTITY FULL;
```

### 4. Grant Replication Privileges

```sql
ALTER ROLE myuser REPLICATION;
GRANT SELECT ON ALL TABLES IN SCHEMA public TO myuser;
```

## Installation

```bash
dotnet add package Encina.Cdc
dotnet add package Encina.Cdc.PostgreSql
```

## Configuration

```csharp
services.AddEncinaCdc(config =>
{
    config.UseCdc()
          .AddHandler<Order, OrderChangeHandler>()
          .WithTableMapping<Order>("public.orders");
});

services.AddEncinaCdcPostgreSql(opts =>
{
    opts.ConnectionString = "Host=localhost;Database=mydb;Username=myuser;Password=...";
    opts.PublicationName = "encina_cdc_publication";
    opts.ReplicationSlotName = "encina_cdc_slot";
    opts.CreateSlotIfNotExists = true;
    opts.CreatePublicationIfNotExists = true;
    opts.PublicationTables = ["public.orders", "public.customers"];
});
```

### PostgresCdcOptions

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `ConnectionString` | `string` | `""` | PostgreSQL connection string (needs replication permissions) |
| `PublicationName` | `string` | `"encina_cdc_publication"` | Publication name to subscribe to |
| `ReplicationSlotName` | `string` | `"encina_cdc_slot"` | Replication slot name |
| `CreateSlotIfNotExists` | `bool` | `true` | Auto-create replication slot |
| `CreatePublicationIfNotExists` | `bool` | `true` | Auto-create publication |
| `PublicationTables` | `string[]` | `[]` | Tables for auto-created publication |

## Position Tracking

`PostgresCdcPosition` wraps a PostgreSQL Log Sequence Number (LSN):

```csharp
var position = new PostgresCdcPosition(new NpgsqlLogSequenceNumber(lsnValue));
position.Lsn;            // NpgsqlLogSequenceNumber
position.ToString();     // "LSN:0/1234ABCD"
position.ToBytes();      // 8-byte big-endian
PostgresCdcPosition.FromBytes(bytes);  // Restore from bytes
```

## Limitations

- **WAL disk usage**: Replication slots prevent WAL cleanup until consumed; monitor disk usage
- **Restart required**: Changing `wal_level` requires a PostgreSQL restart
- **REPLICA IDENTITY**: Without `FULL`, updates and deletes don't include before-values
- **Superuser or replication role**: Required for creating replication slots

## Health Check

`PostgresCdcHealthCheck` verifies connectivity and that the replication slot exists and is active.
