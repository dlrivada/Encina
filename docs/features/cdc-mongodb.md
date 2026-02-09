# CDC: MongoDB Provider

MongoDB CDC connector using **Change Streams** to capture document-level changes from MongoDB databases.

## Overview

| Property | Value |
|----------|-------|
| **Package** | `Encina.Cdc.MongoDb` |
| **CDC Mechanism** | Change Streams |
| **Position Type** | `MongoCdcPosition` (resume token) |
| **Connector Class** | `MongoCdcConnector` |
| **Extension Method** | `AddEncinaCdcMongoDb()` |

MongoDB Change Streams provide a real-time, event-driven interface for watching changes on collections, databases, or entire deployments. They leverage the oplog (operation log) to deliver ordered change events.

## Prerequisites

### 1. Replica Set or Sharded Cluster

Change Streams require a replica set or sharded cluster. Standalone MongoDB instances are **not** supported.

```bash
# For development: start a single-node replica set
mongod --replSet rs0

# Initialize the replica set
mongosh --eval "rs.initiate()"
```

### 2. Enable Pre/Post Images (MongoDB 6.0+)

For before-values on updates and deletes:

```javascript
db.createCollection("orders", {
    changeStreamPreAndPostImages: { enabled: true }
});

// Or for existing collections:
db.runCommand({
    collMod: "orders",
    changeStreamPreAndPostImages: { enabled: true }
});
```

## Installation

```bash
dotnet add package Encina.Cdc
dotnet add package Encina.Cdc.MongoDb
```

## Configuration

```csharp
services.AddEncinaCdc(config =>
{
    config.UseCdc()
          .AddHandler<Order, OrderChangeHandler>()
          .WithTableMapping<Order>("orders");  // Collection name as "table"
});

services.AddEncinaCdcMongoDb(opts =>
{
    opts.ConnectionString = "mongodb://localhost:27017";
    opts.DatabaseName = "mydb";
    opts.CollectionNames = ["orders", "customers"];
    opts.WatchDatabase = true;          // Watch all collections in the database
    opts.FullDocument = ChangeStreamFullDocumentOption.UpdateLookup;
});
```

### MongoCdcOptions

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `ConnectionString` | `string` | `""` | MongoDB connection string |
| `DatabaseName` | `string` | `""` | Database name to watch |
| `CollectionNames` | `string[]` | `[]` | Collections to watch (empty = all) |
| `FullDocument` | `ChangeStreamFullDocumentOption` | `UpdateLookup` | Full document inclusion mode |
| `WatchDatabase` | `bool` | `true` | Watch database vs individual collections |

## Position Tracking

`MongoCdcPosition` wraps a MongoDB Change Stream resume token (`BsonDocument`):

```csharp
var position = new MongoCdcPosition(resumeTokenDocument);
position.ResumeToken;     // BsonDocument
position.ToString();      // "ResumeToken:{...}"
position.ToBytes();       // BSON-serialized bytes
MongoCdcPosition.FromBytes(bytes);  // Restore from bytes
```

The resume token is opaque and managed by the MongoDB driver. It allows the Change Stream to resume from the exact point where processing stopped.

## Limitations

- **Replica set required**: Standalone instances do not support Change Streams
- **Oplog window**: Changes are only available within the oplog retention window
- **Pre/post images**: Requires MongoDB 6.0+ and explicit collection configuration
- **Resume token validity**: Tokens expire when the oplog rolls past the recorded position

## Health Check

`MongoCdcHealthCheck` verifies connectivity and that the target database exists on a replica set.
