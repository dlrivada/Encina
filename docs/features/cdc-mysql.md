# CDC: MySQL Provider

MySQL CDC connector using **Binary Log (binlog) Replication** to capture row-level changes from MySQL databases.

## Overview

| Property | Value |
|----------|-------|
| **Package** | `Encina.Cdc.MySql` |
| **CDC Mechanism** | Binary Log Replication |
| **Position Type** | `MySqlCdcPosition` (GTID or file/position) |
| **Connector Class** | `MySqlCdcConnector` |
| **Extension Method** | `AddEncinaCdcMySql()` |

MySQL binlog replication captures row-level changes by reading the binary log. It supports both GTID-based and traditional file/position-based tracking.

## Prerequisites

### 1. Enable Row-Based Binary Logging

In `my.cnf` / `my.ini`:

```ini
[mysqld]
binlog_format = ROW         # Required (default in MySQL 8+)
binlog_row_image = FULL     # Recommended for before-values
server-id = 1               # Unique server ID
log_bin = mysql-bin          # Enable binary logging
```

### 2. Grant Replication Privileges

```sql
CREATE USER 'cdc_user'@'%' IDENTIFIED BY 'password';
GRANT REPLICATION SLAVE, REPLICATION CLIENT ON *.* TO 'cdc_user'@'%';
GRANT SELECT ON mydb.* TO 'cdc_user'@'%';
FLUSH PRIVILEGES;
```

### 3. Verify Binary Logging

```sql
SHOW VARIABLES LIKE 'binlog_format';     -- Should be ROW
SHOW VARIABLES LIKE 'binlog_row_image';  -- Should be FULL
SHOW MASTER STATUS;                       -- Shows current binlog file/position
```

## Installation

```bash
dotnet add package Encina.Cdc
dotnet add package Encina.Cdc.MySql
```

## Configuration

```csharp
services.AddEncinaCdc(config =>
{
    config.UseCdc()
          .AddHandler<Order, OrderChangeHandler>()
          .WithTableMapping<Order>("mydb.orders");
});

services.AddEncinaCdcMySql(opts =>
{
    opts.ConnectionString = "Server=localhost;Database=mydb;User=cdc_user;Password=...";
    opts.Hostname = "localhost";
    opts.Port = 3306;
    opts.Username = "cdc_user";
    opts.Password = "password";
    opts.ServerId = 100;            // Must be unique per replication client
    opts.UseGtid = true;           // Recommended for MySQL 5.6+
    opts.IncludeDatabases = ["mydb"];
    opts.IncludeTables = ["mydb.orders", "mydb.customers"];
});
```

### MySqlCdcOptions

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `ConnectionString` | `string` | `""` | MySQL connection string (for health checks) |
| `Hostname` | `string` | `"localhost"` | MySQL server hostname |
| `Port` | `int` | `3306` | MySQL server port |
| `Username` | `string` | `""` | Replication username |
| `Password` | `string` | `""` | Replication password |
| `ServerId` | `long` | `1` | Unique server ID for this replication client |
| `UseGtid` | `bool` | `true` | Use GTID-based tracking (recommended) |
| `IncludeDatabases` | `string[]` | `[]` | Databases to include (empty = all) |
| `IncludeTables` | `string[]` | `[]` | Tables to include as `database.table` (empty = all) |

## Position Tracking

`MySqlCdcPosition` supports two tracking modes:

**GTID mode** (recommended):

```csharp
var position = new MySqlCdcPosition("3E11FA47-71CA-11E1-9E33-C80AA9429562:1-23");
position.GtidSet;         // "3E11FA47-..."
position.ToString();      // "GTID:3E11FA47-..."
```

**File/position mode**:

```csharp
var position = new MySqlCdcPosition("mysql-bin.000003", 12345);
position.BinlogFileName;  // "mysql-bin.000003"
position.BinlogPosition;  // 12345
position.ToString();      // "Binlog:mysql-bin.000003:12345"
```

Both serialize to UTF-8 JSON via `ToBytes()` / `FromBytes()`.

## Limitations

- **Server ID uniqueness**: Each replication client must have a unique `ServerId`
- **Binlog retention**: Binary logs are rotated; ensure retention covers your processing window
- **GTID recommended**: File/position tracking can break after failover
- **Separate credentials**: Replication requires dedicated MySQL user with replication privileges

## Health Check

`MySqlCdcHealthCheck` verifies connectivity and that binary logging is enabled with `ROW` format.
