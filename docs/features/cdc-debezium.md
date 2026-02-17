# CDC: Debezium Provider

Debezium CDC connector for `Encina.Cdc.Debezium` — supports two deployment modes: **HTTP Consumer** (Debezium Server) and **Kafka Consumer** (Debezium Connect). Both modes share the same event mapper and integrate with the standard Encina CDC handler pipeline.

## Overview

| Property | HTTP Mode | Kafka Mode |
|----------|-----------|------------|
| **Package** | `Encina.Cdc.Debezium` | `Encina.Cdc.Debezium` |
| **CDC Mechanism** | Debezium Server HTTP Sink | Debezium Connect via Kafka |
| **Position Type** | `DebeziumCdcPosition` (offset JSON) | `DebeziumKafkaPosition` (topic/partition/offset) |
| **Connector Class** | `DebeziumCdcConnector` (internal) | `DebeziumKafkaConnector` (internal) |
| **Extension Method** | `AddEncinaCdcDebezium()` | `AddEncinaCdcDebeziumKafka()` |
| **Health Check** | `DebeziumCdcHealthCheck` | `DebeziumKafkaHealthCheck` |
| **Event Format Default** | `CloudEvents` | `Flat` |

Debezium is a distributed platform for CDC that supports a wide range of databases (PostgreSQL, MySQL, SQL Server, Oracle, MongoDB, Cassandra, Db2, and more). The Encina integration supports the two most common Debezium deployment topologies.

## Architecture

### HTTP Mode (Debezium Server)

```text
┌───────────┐      ┌─────────────────┐     ┌──────────────────────┐      ┌────────────────┐
│ Database  │────▶│ Debezium Server │────▶│ DebeziumHttpListener │────▶│ DebeziumCdc    │
│ (any DB)  │      │ (Java process)  │     │ (ASP.NET endpoint)   │      │ Connector      │
└───────────┘      └─────────────────┘     └──────────────────────┘      └────────────────┘
                                                     │
                                              Channel<JsonElement>
                                              (bounded, backpressure)
```

Debezium Server runs as a standalone Java process and pushes events via HTTP POST to the `DebeziumHttpListener`, which is registered as a `BackgroundService`. Events are buffered in a bounded `Channel<JsonElement>` and read by the connector.

### Kafka Mode (Debezium Connect)

```text
┌───────────┐      ┌──────────────────┐     ┌────────────┐      ┌─────────────────┐
│ Database  │────▶│ Debezium Connect │────▶│   Kafka    │────▶│ DebeziumKafka   │
│ (any DB)  │      │ (Kafka Connect)  │     │  Broker(s) │      │ Connector       │
└───────────┘      └──────────────────┘     └────────────┘      └─────────────────┘
```

Debezium Connect is a Kafka Connect connector that writes change events to Kafka topics. The `DebeziumKafkaConnector` subscribes to those topics as a standard Kafka consumer and yields `ChangeEvent` instances.

### Shared Event Mapper

Both modes use the same `DebeziumEventMapper` (internal) for parsing Debezium JSON payloads. The mapper supports:

- **CloudEvents** format (`application/cloudevents+json`) — default for HTTP mode
- **Flat** format (Debezium envelope with `before`/`after`/`source`/`op`) — default for Kafka mode

Op code mapping:

| Debezium `op` | `ChangeOperation` |
|---------------|-------------------|
| `c` | `Insert` |
| `r` | `Snapshot` |
| `u` | `Update` |
| `d` | `Delete` |

## Prerequisites

### HTTP Mode

#### 1. Install and Run Debezium Server

Using Docker:

```bash
docker run -d --name debezium \
  -p 8083:8083 \
  -e DEBEZIUM_SINK_TYPE=http \
  -e DEBEZIUM_SINK_HTTP_URL=http://host.docker.internal:8080/debezium \
  debezium/server:2.5
```

#### 2. Configure Debezium Server

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

### Kafka Mode

#### 1. Kafka Broker and Debezium Connect

A running Kafka cluster with Debezium Connect configured:

```properties
# Debezium Connect connector configuration
connector.class=io.debezium.connector.sqlserver.SqlServerConnector
database.hostname=db-host
database.port=1433
database.user=sa
database.password=YourStrong!Passw0rd
database.names=mydb
topic.prefix=dbserver1
```

Each tracked table produces a topic named `{topic.prefix}.{schema}.{table}` (e.g., `dbserver1.dbo.Orders`).

## Installation

```bash
dotnet add package Encina.Cdc
dotnet add package Encina.Cdc.Debezium
```

Both HTTP and Kafka modes are included in the same package.

## Configuration

### HTTP Mode

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
    opts.BearerToken = "my-secret-token";       // Optional authentication
    opts.DebeziumServerUrl = "http://debezium:8083"; // For health checks
    opts.ChannelCapacity = 1000;                // Backpressure buffer size
    opts.MaxListenerRetries = 5;                // Listener startup retries
    opts.ListenerRetryDelay = TimeSpan.FromSeconds(2); // Base retry delay
});
```

### Kafka Mode

```csharp
services.AddEncinaCdc(config =>
{
    config.UseCdc()
          .AddHandler<Order, OrderChangeHandler>()
          .WithTableMapping<Order>("dbo.Orders");
});

services.AddEncinaCdcDebeziumKafka(opts =>
{
    opts.BootstrapServers = "kafka1:9092,kafka2:9092";
    opts.GroupId = "my-cdc-consumer";
    opts.Topics = ["dbserver1.dbo.Orders", "dbserver1.dbo.Products"];
    opts.AutoOffsetReset = "earliest";
    opts.EventFormat = DebeziumEventFormat.Flat;
});
```

### Kafka Mode with Security (SASL/SSL)

```csharp
services.AddEncinaCdcDebeziumKafka(opts =>
{
    opts.BootstrapServers = "kafka.cloud.example.com:9093";
    opts.GroupId = "production-cdc";
    opts.Topics = ["myapp.public.orders"];
    opts.SecurityProtocol = "SASL_SSL";
    opts.SaslMechanism = "PLAIN";
    opts.SaslUsername = "api-key";
    opts.SaslPassword = "api-secret";
    opts.SslCaLocation = "/etc/ssl/certs/ca-certificates.crt";
});
```

### Mutual Exclusivity

Both modes register `ICdcConnector` via `TryAddSingleton`. The first mode registered wins:

```csharp
// Register one mode
services.AddEncinaCdcDebezium(opts => { /* ... */ });     // HTTP mode

// This is silently ignored (TryAddSingleton)
services.AddEncinaCdcDebeziumKafka(opts => { /* ... */ }); // No-op, HTTP was first
```

Choose one mode per application deployment. Use configuration-driven selection for environments:

```csharp
if (config.GetValue<bool>("Cdc:UseKafka"))
    services.AddEncinaCdcDebeziumKafka(opts => config.Bind("Cdc:Kafka", opts));
else
    services.AddEncinaCdcDebezium(opts => config.Bind("Cdc:Http", opts));
```

## Options Reference

### DebeziumCdcOptions (HTTP Mode)

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `ListenUrl` | `string` | `"http://+"` | URL to listen on for HTTP POST events |
| `ListenPort` | `int` | `8080` | Listening port |
| `ListenPath` | `string` | `"/debezium"` | HTTP path for receiving events |
| `DebeziumServerUrl` | `string?` | `null` | Debezium Server URL (for health checks) |
| `BearerToken` | `string?` | `null` | Bearer token for authenticating incoming requests |
| `EventFormat` | `DebeziumEventFormat` | `CloudEvents` | Expected event format |
| `ChannelCapacity` | `int` | `1000` | Maximum events buffered in the internal channel. Returns 503 when full |
| `MaxListenerRetries` | `int` | `5` | Maximum retries when the HTTP listener fails to start |
| `ListenerRetryDelay` | `TimeSpan` | `2 seconds` | Base delay between retries (exponential backoff: delay x 2^attempt) |

### DebeziumKafkaOptions (Kafka Mode)

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `BootstrapServers` | `string` | `"localhost:9092"` | Kafka broker connection string |
| `GroupId` | `string` | `"encina-cdc-debezium"` | Consumer group ID for partition assignment |
| `Topics` | `string[]` | `[]` | Kafka topics to subscribe to |
| `AutoOffsetReset` | `string` | `"earliest"` | Behavior when no committed offset exists: `"earliest"` or `"latest"` |
| `SessionTimeoutMs` | `int` | `45000` | Session timeout in ms — consumer removed if no heartbeat within this period |
| `MaxPollIntervalMs` | `int` | `300000` | Max poll interval in ms — consumer removed if it doesn't poll within this period |
| `EventFormat` | `DebeziumEventFormat` | `Flat` | Expected event format from Kafka |
| `SecurityProtocol` | `string?` | `null` | Security protocol: `PLAINTEXT`, `SSL`, `SASL_PLAINTEXT`, `SASL_SSL` |
| `SaslMechanism` | `string?` | `null` | SASL mechanism: `PLAIN`, `SCRAM-SHA-256`, `SCRAM-SHA-512`, `GSSAPI` |
| `SaslUsername` | `string?` | `null` | SASL username for authentication |
| `SaslPassword` | `string?` | `null` | SASL password for authentication |
| `SslCaLocation` | `string?` | `null` | SSL CA certificate file path for secure connections |

### Event Formats

| Format | Description | Typical Use |
|--------|-------------|-------------|
| `DebeziumEventFormat.CloudEvents` | CloudEvents envelope (`application/cloudevents+json`). Event payload in `data` property | HTTP mode (Debezium Server) |
| `DebeziumEventFormat.Flat` | Flat JSON with Debezium envelope (`before`/`after`/`source`/`op`) | Kafka mode (Debezium Connect) |

## How It Works

### HTTP Mode Flow

1. **`DebeziumHttpListener`** runs as a `BackgroundService` and starts an HTTP listener on the configured URL/port/path
2. Incoming POST requests are validated (bearer token if configured) and the JSON body is written to a bounded `Channel<JsonElement>`
3. If the channel is full, the listener returns **HTTP 503** (Service Unavailable) to apply backpressure to Debezium Server
4. **`DebeziumCdcConnector`** reads from the channel and uses `DebeziumEventMapper` to parse events into `ChangeEvent` instances
5. Events flow through the standard `CdcProcessor` → `ICdcDispatcher` → `IChangeEventHandler<T>` pipeline

### Kafka Mode Flow

1. **`DebeziumKafkaConnector`** creates a Kafka consumer subscribed to the configured topics
2. Messages are consumed and deserialized as `JsonElement` payloads
3. The shared `DebeziumEventMapper` parses each message into a `ChangeEvent`
4. Position is tracked as `DebeziumKafkaPosition` (topic + partition + offset)
5. Events flow through the standard `CdcProcessor` → `ICdcDispatcher` → `IChangeEventHandler<T>` pipeline
6. Consumer group rebalancing is handled automatically by the Kafka client library

### Backpressure Handling

**HTTP Mode**: The bounded `Channel<JsonElement>` with capacity `ChannelCapacity` (default: 1000) provides backpressure. When the buffer is full, the HTTP listener returns 503 to Debezium Server, which retries with its own backoff strategy.

**Kafka Mode**: Kafka's consumer group protocol provides natural backpressure. If the consumer falls behind, it continues consuming from the last committed offset. The `MaxPollIntervalMs` setting controls the maximum time between polls before the consumer is removed from the group.

## Position Tracking

### HTTP Mode — `DebeziumCdcPosition`

Wraps the Debezium source offset as a JSON string:

```csharp
var position = new DebeziumCdcPosition("{\"lsn\":12345,\"txId\":67}");
position.OffsetJson;       // The JSON offset string
position.ToString();       // "Debezium-Offset:{\"lsn\":12345,..."  (truncated at 50 chars)
position.ToBytes();        // UTF-8 encoded offset JSON
DebeziumCdcPosition.FromBytes(bytes);  // Restore from bytes
```

The offset format is source-specific (depends on the Debezium source connector type — PostgreSQL uses LSN, SQL Server uses change version, MySQL uses binlog position, etc.).

### Kafka Mode — `DebeziumKafkaPosition`

Tracks the Kafka topic, partition, and offset:

```csharp
var position = new DebeziumKafkaPosition(
    offsetJson: "{\"lsn\":12345}",
    topic: "dbserver1.dbo.Orders",
    partition: 0,
    offset: 42);

position.Topic;       // "dbserver1.dbo.Orders"
position.Partition;   // 0
position.Offset;      // 42
position.OffsetJson;  // "{\"lsn\":12345}"
position.ToString();  // "Kafka:dbserver1.dbo.Orders[0]@42"
```

`CompareTo` ordering: same topic and partition are compared by Kafka offset; different topic or partition are compared by topic (ordinal) then partition number.

## Health Checks

### HTTP Mode

`DebeziumCdcHealthCheck` (default name: `"encina-cdc-debezium"`) verifies that the HTTP listener is running and optionally checks connectivity to the Debezium Server URL.

### Kafka Mode

`DebeziumKafkaHealthCheck` (default name: `"encina-cdc-debezium-kafka"`) verifies connector health and position store connectivity. Tags: `["debezium", "kafka"]`.

## Choosing Between Modes

| Consideration | HTTP Mode | Kafka Mode |
|---------------|-----------|------------|
| **Infrastructure** | Debezium Server (lightweight) | Kafka cluster + Debezium Connect |
| **Complexity** | Lower — single process | Higher — Kafka + Connect cluster |
| **Scalability** | Single consumer | Consumer groups, partition-based |
| **Durability** | Events may be lost if app is down | Events retained in Kafka topics |
| **Replay** | Not supported | Reset consumer offset to replay |
| **Use case** | Simple deployments, dev/test | Production, multi-consumer, at-least-once |
| **Latency** | Lower (direct HTTP push) | Slightly higher (Kafka hop) |
| **Event format** | CloudEvents (default) | Flat (default) |

### Recommendation

- **Development / simple deployments**: Use HTTP mode with Debezium Server
- **Production / high availability**: Use Kafka mode with Debezium Connect for durability, replay, and scaling

## Advantages

- **Database agnostic**: Supports any database that Debezium supports (30+ connectors)
- **No native drivers**: No need for database-specific client libraries in .NET
- **Full CDC features**: Before/after values, schema metadata, source tracking
- **Production proven**: Debezium is widely used in production systems
- **Flexible deployment**: Choose HTTP (simple) or Kafka (scalable) mode
- **Shared event mapping**: Both modes use the same `DebeziumEventMapper` for consistent behavior

## Limitations

- **Java dependency**: Requires running Debezium Server or Debezium Connect (Java) as a separate process
- **Operational complexity**: Additional infrastructure to manage (Debezium, optionally Kafka)
- **HTTP mode buffering**: Events are buffered in memory between the HTTP listener and the connector
- **Kafka mode dependency**: Requires a Kafka cluster for the Kafka consumer mode
- **Mutual exclusivity**: Only one mode (HTTP or Kafka) can be active per application instance

## Test Coverage

The Debezium provider has comprehensive test coverage across all test types:

| Test Type | Tests | Description |
|-----------|-------|-------------|
| Unit Tests | ~76 | Event mapper, positions, options, service collection, health checks |
| Guard Tests | ~19 | Null/parameter validation for constructors and extension methods |
| Contract Tests | ~24 | CdcPosition contract compliance for both position types |
| Property Tests | ~19 | Round-trip, ordering, and invariant verification with FsCheck |
| Integration Tests | ~5 | Real JSON payload parsing with Debezium event formats |
| **Total** | **~143** | Covering both HTTP and Kafka modes |
