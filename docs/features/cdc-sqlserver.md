# CDC: SQL Server Provider

SQL Server CDC connector using **Change Tracking** to capture row-level changes from SQL Server databases.

## Overview

| Property | Value |
|----------|-------|
| **Package** | `Encina.Cdc.SqlServer` |
| **CDC Mechanism** | SQL Server Change Tracking |
| **Position Type** | `SqlServerCdcPosition` (version `long`) |
| **Connector Class** | `SqlServerCdcConnector` |
| **Extension Method** | `AddEncinaCdcSqlServer()` |

SQL Server Change Tracking is a lightweight built-in feature that records which rows changed and the type of change. It uses a monotonically increasing version number to track progress.

> **Note**: Change Tracking does NOT store old column values. For Update operations, only the `After` value is available. For Delete operations, only primary key columns are available in the `Before` value. If you need full before/after values, consider using Debezium with SQL Server CDC (not Change Tracking).

## Prerequisites

### 1. Enable Change Tracking on the Database

```sql
ALTER DATABASE MyDatabase
SET CHANGE_TRACKING = ON
(CHANGE_RETENTION = 2 DAYS, AUTO_CLEANUP = ON);
```

### 2. Enable Change Tracking on Tables

```sql
ALTER TABLE dbo.Orders
ENABLE CHANGE_TRACKING
WITH (TRACK_COLUMNS_UPDATED = ON);
```

### 3. Verify Change Tracking is Active

```sql
SELECT DB_NAME(database_id), is_auto_cleanup_on, retention_period
FROM sys.change_tracking_databases;

SELECT OBJECT_NAME(object_id) AS TableName
FROM sys.change_tracking_tables;
```

## Installation

```bash
dotnet add package Encina.Cdc
dotnet add package Encina.Cdc.SqlServer
```

## Configuration

```csharp
services.AddEncinaCdc(config =>
{
    config.UseCdc()
          .AddHandler<Order, OrderChangeHandler>()
          .WithTableMapping<Order>("dbo.Orders");
});

services.AddEncinaCdcSqlServer(opts =>
{
    opts.ConnectionString = "Server=.;Database=MyDb;Trusted_Connection=true;";
    opts.TrackedTables = ["dbo.Orders", "dbo.Customers"];
    opts.SchemaName = "dbo";           // Default schema prefix
    opts.StartFromVersion = null;      // null = start from current version
});
```

### SqlServerCdcOptions

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `ConnectionString` | `string` | `""` | SQL Server connection string |
| `TrackedTables` | `string[]` | `[]` | Tables to track (include schema, e.g., `dbo.Orders`) |
| `SchemaName` | `string` | `"dbo"` | Default schema for tables without prefix |
| `StartFromVersion` | `long?` | `null` | Starting version (`null` = current, `0` = all history) |

## Position Tracking

`SqlServerCdcPosition` wraps a Change Tracking version number (`long`):

```csharp
var position = new SqlServerCdcPosition(version: 42);
position.Version;        // 42
position.ToString();     // "CT-Version:42"
position.ToBytes();      // 8-byte big-endian
SqlServerCdcPosition.FromBytes(bytes);  // Restore from bytes
```

## Limitations

- **No before-values**: Change Tracking only records which rows changed, not old values
- **Retention period**: Changes are only available within the configured retention window
- **Primary key required**: All tracked tables must have a primary key
- **Version gaps**: Version numbers may have gaps due to concurrent transactions

## Health Check

`SqlServerCdcHealthCheck` verifies connectivity and that Change Tracking is enabled on the database.
