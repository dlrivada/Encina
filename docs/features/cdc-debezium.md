# CDC: Debezium Provider

Debezium CDC connector that receives change events from **Debezium Server** via HTTP. This enables CDC from any database supported by Debezium without requiring database-specific client libraries in your .NET application.

## Overview

| Property | Value |
|----------|-------|
| **Package** | `Encina.Cdc.Debezium` |
| **CDC Mechanism** | Debezium Server HTTP Consumer |
| **Position Type** | `DebeziumCdcPosition` (offset JSON) |
| **Connector Class** | `DebeziumCdcConnector` |
| **Extension Method** | `AddEncinaCdcDebezium()` |

Debezium is a distributed platform for CDC that supports a wide range of databases (PostgreSQL, MySQL, SQL Server, Oracle, MongoDB, Cassandra, Db2, and more). Debezium Server runs as a separate Java process and pushes events via HTTP to your .NET application.

### Architecture

```text
┌───────────┐      ┌─────────────────┐     ┌──────────────────────┐      ┌────────────────┐
│ Database  │────▶│ Debezium Server │────▶│ DebeziumHttpListener │────▶│ DebeziumCdc    │
│ (any DB)  │      │ (Java process)  │     │ (ASP.NET endpoint)   │      │ Connector      │
└───────────┘      └─────────────────┘     └──────────────────────┘      └────────────────┘
```

## Prerequisites

### 1. Install and Run Debezium Server

Using Docker:

```bash
docker run -d --name debezium \
  -p 8083:8083 \
  -e DEBEZIUM_SINK_TYPE=http \
  -e DEBEZIUM_SINK_HTTP_URL=http://host.docker.internal:8080/debezium \
  debezium/server:2.5
```

### 2. Configure Debezium Server

Create `application.properties` for Debezium Server:

```properties
# Sink configuration - sends events to your .NET app
debezium.sink.type=http
debezium.sink.http.url=http://your-app:8080/debezium

# Source configuration (PostgreSQL example)
debezium.source.connector.class=io.debezium.connector.postgresql.PostgresConnector
debezium.source.database.hostname=db-host
debezium.source.database.port=5432
debezium.source.database.user=debezium
debezium.source.database.password=dbz_pass
debezium.source.database.dbname=mydb
debezium.source.topic.prefix=myapp
debezium.source.plugin.name=pgoutput

# Format
debezium.format.key=json
debezium.format.value=cloudevents
```

## Installation

```bash
dotnet add package Encina.Cdc
dotnet add package Encina.Cdc.Debezium
```

## Configuration

```csharp
services.AddEncinaCdc(config =>
{
    config.UseCdc()
          .AddHandler<Order, OrderChangeHandler>()
          .WithTableMapping<Order>("public.orders");
});

services.AddEncinaCdcDebezium(opts =>
{
    opts.ListenUrl = "http://+";
    opts.ListenPort = 8080;
    opts.ListenPath = "/debezium";
    opts.EventFormat = DebeziumEventFormat.CloudEvents;
    opts.BearerToken = "my-secret-token";  // Optional authentication
    opts.DebeziumServerUrl = "http://debezium:8083";  // For health checks
});
```

### DebeziumCdcOptions

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `ListenUrl` | `string` | `"http://+"` | URL to listen on for HTTP POST events |
| `ListenPort` | `int` | `8080` | Listening port |
| `ListenPath` | `string` | `"/debezium"` | HTTP path for receiving events |
| `DebeziumServerUrl` | `string?` | `null` | Debezium Server URL (for health checks) |
| `BearerToken` | `string?` | `null` | Bearer token for authenticating incoming requests |
| `EventFormat` | `DebeziumEventFormat` | `CloudEvents` | Expected event format |

### Event Formats

| Format | Description |
|--------|-------------|
| `DebeziumEventFormat.CloudEvents` | CloudEvents format (`application/cloudevents+json`). Default for Debezium Server HTTP sink |
| `DebeziumEventFormat.Flat` | Flat JSON with Debezium envelope (`before`/`after`/`source`/`op`) |

## How It Works

1. **DebeziumHttpListener** runs as a `BackgroundService` and listens for HTTP POST requests
2. Incoming events are validated (bearer token, format) and written to an in-memory `Channel<JsonElement>`
3. **DebeziumCdcConnector** reads from the channel and yields `ChangeEvent` instances
4. The standard `CdcProcessor` → `ICdcDispatcher` → `IChangeEventHandler<T>` pipeline processes the events

## Position Tracking

`DebeziumCdcPosition` wraps the Debezium source offset as a JSON string:

```csharp
var position = new DebeziumCdcPosition("{\"lsn\":12345,\"txId\":67}");
position.OffsetJson;       // The JSON offset string
position.ToString();       // "Debezium-Offset:{\"lsn\":12345,\"txId\":67}"
position.ToBytes();        // UTF-8 encoded JSON
DebeziumCdcPosition.FromBytes(bytes);  // Restore from bytes
```

The offset format is source-specific (depends on the Debezium source connector type).

## Advantages

- **Database agnostic**: Supports any database that Debezium supports
- **No native drivers**: No need for database-specific client libraries in .NET
- **Full CDC features**: Before/after values, schema change events, etc.
- **Production proven**: Debezium is widely used in production systems
- **Decoupled deployment**: CDC logic runs in a separate process

## Limitations

- **Java dependency**: Requires running Debezium Server (Java) as a separate process
- **Network overhead**: HTTP transport adds latency compared to native connectors
- **Operational complexity**: Additional infrastructure to manage
- **Channel buffering**: Events are buffered in memory between the HTTP listener and the connector

## Health Check

`DebeziumCdcHealthCheck` verifies that the HTTP listener is running and optionally checks connectivity to the Debezium Server URL.
