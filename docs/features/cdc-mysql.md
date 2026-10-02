---
title: "CDC: MySQL Provider"
layout: default
parent: "Features"
---

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
    opts.ConnectionString = "Server=mysql.internal.example.com;Database=mydb;User=cdc_user;Password=...";
    opts.Hostname = "mysql.internal.example.com";
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
| `ConnectionString` | `string` | `""` | MySQL connection string (reads the current binlog position). Required; every server in it is validated |
| `Hostname` | `string` | `"localhost"` | MySQL server hostname. The default is rejected unless `AllowLocalEndpoints` is `true` |
| `AllowLocalEndpoints` | `bool` | `false` | Allows `Hostname` and the connection string servers to target `localhost`, loopback or a local socket |
| `Port` | `int` | `3306` | MySQL server port |
| `Username` | `string` | `""` | Replication username |
| `Password` | `string` | `""` | Replication password |
| `ServerId` | `long` | `1` | Unique server ID for this replication client |
| `UseGtid` | `bool` | `true` | Use GTID-based tracking (recommended) |
| `IncludeDatabases` | `string[]` | `[]` | Databases to include (empty = all) |
| `IncludeTables` | `string[]` | `[]` | Tables to include as `database.table` (empty = all) |

### Endpoint validation

`Encina.Validation.EndpointValidator` checks `Hostname` and every server inside `ConnectionString` (issue #852). A host is rejected when it is loopback (`127.0.0.0/8`, `::1`, `localhost`, `*.localhost`), link-local (`169.254.0.0/16`, `fe80::/10`), a cloud metadata endpoint (for example `169.254.169.254`) or an unspecified address (`0.0.0.0/8`, `::`), including IPv4-mapped IPv6, decimal, octal and hex IPv4 forms and trailing dots. Private ranges (RFC 1918, `fc00::/7`) are allowed. A Unix socket, named pipe or shared memory connection counts as local.

- `AllowLocalEndpoints = true` is the only opt-out and covers loopback and local sockets only; it logs one warning per options instance at startup. Link-local, metadata and unspecified addresses are always rejected.
- The default `Hostname` of `localhost` therefore fails unless `AllowLocalEndpoints` is `true`. Set it for local development or a database on the same host:

  ```csharp
  services.AddEncinaCdcMySql(opts =>
  {
      opts.ConnectionString = "Server=localhost;Database=mydb;User=cdc_user;Password=...";
      opts.Hostname = "localhost";
      opts.AllowLocalEndpoints = true;   // local development only
  });
  ```

- Validation runs when `AddEncinaCdcMySql` is called (it throws `Microsoft.Extensions.Options.OptionsValidationException`) and again at host startup. Error messages never echo the configured host or connection string.
- Only literal hosts are checked. A DNS name that resolves to an internal address (DNS rebinding) is not detected at configuration time.

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
