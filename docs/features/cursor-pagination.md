# Cursor-Based Pagination in Encina

This guide explains how to implement efficient cursor-based (keyset) pagination using Encina's comprehensive pagination support across all data providers.

## Table of Contents

1. [Overview](#overview)
2. [Cursor vs Offset Pagination](#cursor-vs-offset-pagination)
3. [Architecture](#architecture)
4. [Quick Start](#quick-start)
5. [Usage Examples](#usage-examples)
6. [Provider-Specific Setup](#provider-specific-setup)
7. [Bidirectional Pagination](#bidirectional-pagination)
8. [Composite Keys](#composite-keys)
9. [GraphQL Integration](#graphql-integration)
10. [Best Practices](#best-practices)
11. [FAQ](#faq)

---

## Overview

Cursor-based pagination (also known as keyset pagination) provides **O(1) performance** regardless of page position, unlike offset-based pagination which degrades linearly with page number. Encina provides a unified API that works consistently across all supported data providers.

| Feature | Support |
|---------|---------|
| **Simple Key Pagination** | All providers |
| **Composite Key Pagination** | All providers |
| **Bidirectional Navigation** | Forward and Backward |
| **Opaque Cursors** | Base64 URL-safe encoding |
| **Total Count** | Optional (expensive operation) |

> **Key Benefit**: Encina's cursor pagination works identically across EF Core, Dapper, ADO.NET, and MongoDB. Switch providers without changing your pagination logic.

---

## Cursor vs Offset Pagination

### When to Use Cursor Pagination

| Scenario | Recommended |
|----------|-------------|
| Large datasets (1M+ rows) | Cursor |
| Real-time feeds (social media, logs) | Cursor |
| Infinite scroll UIs | Cursor |
| Mobile apps with "load more" | Cursor |
| Deep linking to specific pages | Offset |
| "Jump to page N" requirement | Offset |
| Small datasets (<10K rows) | Either |

### Performance Comparison

```
Dataset Size    | Offset (Page 1000) | Cursor (Any Position)
----------------|--------------------|-----------------------
10K rows        | ~50ms              | ~5ms
100K rows       | ~500ms             | ~5ms
1M rows         | ~5s                | ~5ms
10M rows        | ~50s               | ~5ms
```

### Trade-offs

| Aspect | Cursor | Offset |
|--------|--------|--------|
| **Performance** | O(1) constant | O(N) degrades with offset |
| **Deep Page Access** | Efficient | Slow for large offsets |
| **Jump to Page N** | Not possible | Supported |
| **Data Consistency** | Stable during changes | Can skip/duplicate items |
| **Implementation** | More complex | Simple |
| **Cache-Friendly** | URL-safe cursors | Page numbers |

---

## Architecture

### Type Hierarchy

Encina uses a layered architecture to support both REST and GraphQL use cases:

```
┌─────────────────────────────────────────────────────────────────┐
│                         PUBLIC TYPES                             │
├─────────────────────────────────────────────────────────────────┤
│  CursorPaginatedResult<T>     - REST API response type           │
│  CursorPaginationOptions      - Query parameters                 │
│  ICursorPaginatedQuery<T>     - Query interface for handlers     │
│  ICursorEncoder               - Cursor encoding abstraction      │
│  CursorDirection              - Forward/Backward enum            │
└─────────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────────┐
│                        INTERNAL TYPES                            │
│                    (Preserve cursor-per-item)                    │
├─────────────────────────────────────────────────────────────────┤
│  CursorPagedData<T>           - Internal pagination result       │
│  CursorItem<T>                - Item + its cursor                │
│  CursorPageInfo               - Navigation metadata              │
└─────────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────────┐
│                    GRAPHQL TYPES (Optional)                      │
│                   Encina.HotChocolate package                    │
├─────────────────────────────────────────────────────────────────┤
│  Connection<T>                - Relay-compliant connection       │
│  Edge<T>                      - Node + cursor                    │
│  PageInfo                     - GraphQL page info                │
└─────────────────────────────────────────────────────────────────┘
```

### When to Use Each Type

| Type | Use Case |
|------|----------|
| `CursorPaginatedResult<T>` | REST APIs, ASP.NET Core endpoints |
| `ICursorPaginatedQuery<T>` | CQRS query definitions |
| `Connection<T>` | GraphQL APIs with HotChocolate |

---

## Quick Start

### 1. Register Services

```csharp
// Program.cs
services.AddCursorPagination(); // Registers ICursorEncoder
```

### 2. Define a Query (CQRS Pattern)

```csharp
public sealed record GetOrdersQuery(
    string CustomerId,
    string? Cursor = null,
    int PageSize = 20,
    CursorDirection Direction = CursorDirection.Forward
) : ICursorPaginatedQuery<OrderDto>, IQuery<CursorPaginatedResult<OrderDto>>;
```

### 3. Implement the Handler

```csharp
public sealed class GetOrdersQueryHandler(
    AppDbContext db,
    ICursorEncoder encoder)
    : IQueryHandler<GetOrdersQuery, CursorPaginatedResult<OrderDto>>
{
    public async Task<CursorPaginatedResult<OrderDto>> Handle(
        GetOrdersQuery query,
        CancellationToken cancellationToken)
    {
        return await db.Orders
            .Where(o => o.CustomerId == query.CustomerId)
            .OrderByDescending(o => o.CreatedAtUtc)
            .ToCursorPaginatedDescendingAsync(
                cursor: query.Cursor,
                pageSize: query.PageSize,
                keySelector: o => o.CreatedAtUtc,
                cursorEncoder: encoder,
                cancellationToken);
    }
}
```

### 4. Create the API Endpoint

```csharp
app.MapGet("/api/orders", async (
    [FromQuery] string customerId,
    [FromQuery] string? cursor,
    [FromQuery] int pageSize = 20,
    [FromServices] IMediator mediator) =>
{
    var result = await mediator.Send(new GetOrdersQuery(customerId, cursor, pageSize));
    return Results.Ok(result);
});
```

### 5. Response Format

```json
{
  "items": [
    { "id": "ord-123", "total": 150.00, "createdAt": "2025-12-27T10:30:00Z" },
    { "id": "ord-122", "total": 200.00, "createdAt": "2025-12-27T09:15:00Z" }
  ],
  "nextCursor": "eyJjcmVhdGVkQXRVdGMiOiIyMDI1LTEyLTI3VDA5OjE1OjAwWiJ9",
  "previousCursor": "eyJjcmVhdGVkQXRVdGMiOiIyMDI1LTEyLTI3VDEwOjMwOjAwWiJ9",
  "hasNextPage": true,
  "hasPreviousPage": false,
  "totalCount": null
}
```

---

## Usage Examples

### Entity Framework Core

```csharp
// Simple key (single column ordering)
var result = await dbContext.Products
    .Where(p => p.IsActive)
    .OrderBy(p => p.Name)
    .ToCursorPaginatedAsync(
        cursor: request.Cursor,
        pageSize: 20,
        keySelector: p => p.Name,
        cursorEncoder: encoder,
        cancellationToken);

// Descending order
var result = await dbContext.Orders
    .OrderByDescending(o => o.CreatedAtUtc)
    .ToCursorPaginatedDescendingAsync(
        cursor: request.Cursor,
        pageSize: 20,
        keySelector: o => o.CreatedAtUtc,
        cursorEncoder: encoder,
        cancellationToken);

// With projection (efficient - only selects needed columns)
var result = await dbContext.Orders
    .OrderByDescending(o => o.CreatedAtUtc)
    .ToCursorPaginatedAsync(
        selector: o => new OrderDto(o.Id, o.Total, o.CreatedAtUtc),
        cursor: request.Cursor,
        pageSize: 20,
        keySelector: o => o.CreatedAtUtc,
        cursorEncoder: encoder,
        cancellationToken);
```

### Dapper

```csharp
// Using CursorPaginationHelper
var helper = new CursorPaginationHelper<Order>(connection, encoder);

var result = await helper.ToCursorPaginatedAsync(
    cursor: request.Cursor,
    pageSize: 20,
    tableName: "Orders",
    orderByColumn: "CreatedAtUtc",
    isDescending: true,
    whereClause: "CustomerId = @CustomerId",
    parameters: new { CustomerId = customerId },
    cancellationToken);
```

### MongoDB

```csharp
// Simple key
var result = await collection.ToCursorPaginatedAsync(
    filter: Builders<Order>.Filter.Eq(o => o.CustomerId, customerId),
    sort: Builders<Order>.Sort.Descending(o => o.CreatedAtUtc),
    keySelector: o => o.CreatedAtUtc,
    cursor: request.Cursor,
    pageSize: 20,
    cursorEncoder: encoder,
    cancellationToken);

// Composite key
var result = await collection.ToCursorPaginatedCompositeAsync(
    filter: filter,
    sort: Builders<Order>.Sort.Descending(o => o.CreatedAtUtc).Ascending(o => o.Id),
    keySelector: o => new { o.CreatedAtUtc, o.Id },
    cursor: request.Cursor,
    pageSize: 20,
    cursorEncoder: encoder,
    keyDescending: [true, false],
    cancellationToken);
```

---

## Provider-Specific Setup

### EF Core

```csharp
// Program.cs
services.AddDbContext<AppDbContext>(options => ...);
services.AddCursorPagination();

// Use extension methods directly on IQueryable<T>
```

### Dapper (SQLite, SQL Server, PostgreSQL, MySQL)

```csharp
// Each provider has its own package
// Encina.Dapper.Sqlite, Encina.Dapper.SqlServer, etc.

services.AddEncinaDapperSqlite(connectionString);
services.AddCursorPagination();

// Use CursorPaginationHelper<T>
```

### MongoDB

```csharp
services.AddEncinaMongoDB(options =>
{
    options.ConnectionString = "mongodb://localhost:27017";
    options.DatabaseName = "MyApp";
});
services.AddCursorPagination();

// Use extension methods on IMongoCollection<T>
```

### ADO.NET (SQLite, SQL Server, PostgreSQL, MySQL)

Each ADO.NET provider has its own package with optimized SQL generation:

```csharp
// Each provider has its own package
// Encina.ADO.Sqlite, Encina.ADO.SqlServer, Encina.ADO.PostgreSQL, Encina.ADO.MySQL

services.AddCursorPagination();

// Create helper with entity mapper
using var connection = new SqlConnection(connectionString);
await connection.OpenAsync();

var helper = new CursorPaginationHelper<Order>(
    connection,
    cursorEncoder,
    reader => new Order
    {
        Id = reader.GetGuid(reader.GetOrdinal("Id")),
        CreatedAtUtc = reader.GetDateTime(reader.GetOrdinal("CreatedAtUtc")),
        Total = reader.GetDecimal(reader.GetOrdinal("Total"))
    });

// Simple key pagination
var result = await helper.ExecuteAsync<DateTime>(
    tableName: "Orders",
    keyColumn: "CreatedAtUtc",
    cursor: null,
    pageSize: 25,
    isDescending: true);

// Composite key for tie-breaking
var compositeResult = await helper.ExecuteCompositeAsync(
    tableName: "Orders",
    keyColumns: ["CreatedAtUtc", "Id"],
    cursor: request.Cursor,
    pageSize: 25,
    keyDescending: [true, false]);
```

#### Provider-Specific SQL

Each provider generates optimized SQL with correct quoting and limiting syntax:

| Provider | Column Quoting | Row Limiting | Example |
|----------|----------------|--------------|---------|
| SQL Server | `[column]` | `TOP (n)` | `SELECT TOP (25) * FROM [Orders] WHERE [Id] > @cursor ORDER BY [Id]` |
| PostgreSQL | `"column"` | `LIMIT n` | `SELECT * FROM "Orders" WHERE "Id" > @cursor ORDER BY "Id" LIMIT 25` |
| MySQL | `` `column` `` | `LIMIT n` | ``SELECT * FROM `Orders` WHERE `Id` > @cursor ORDER BY `Id` LIMIT 25`` |
| SQLite | `"column"` | `LIMIT n` | `SELECT * FROM "Orders" WHERE "Id" > @cursor ORDER BY "Id" LIMIT 25` |

---

## Bidirectional Pagination

Encina supports both forward and backward navigation using the `CursorDirection` enum:

```csharp
// Forward navigation (default)
var options = new CursorPaginationOptions(
    Cursor: nextCursor,
    PageSize: 20,
    Direction: CursorDirection.Forward);

// Backward navigation
var options = new CursorPaginationOptions(
    Cursor: previousCursor,
    PageSize: 20,
    Direction: CursorDirection.Backward);

var result = await query.ToCursorPaginatedAsync(
    options: options,
    keySelector: o => o.CreatedAtUtc,
    cursorEncoder: encoder,
    isDescending: true,
    cancellationToken);
```

### Navigation Logic

| Current Order | Direction | Filter Applied |
|---------------|-----------|----------------|
| ASC | Forward | `key > cursor` |
| ASC | Backward | `key < cursor` |
| DESC | Forward | `key < cursor` |
| DESC | Backward | `key > cursor` |

---

## Composite Keys

For stable pagination with non-unique sort columns, use composite keys:

```csharp
// Query ordered by CreatedAt DESC, then Id ASC for tiebreaker
var result = await dbContext.Orders
    .OrderByDescending(o => o.CreatedAtUtc)
    .ThenBy(o => o.Id)
    .ToCursorPaginatedCompositeAsync(
        cursor: request.Cursor,
        pageSize: 20,
        keySelector: o => new { o.CreatedAtUtc, o.Id },
        cursorEncoder: encoder,
        keyDescending: [true, false],  // CreatedAt DESC, Id ASC
        cancellationToken);
```

### When to Use Composite Keys

| Scenario | Recommendation |
|----------|----------------|
| Unique sort column (Id, Timestamp) | Simple key |
| Non-unique column (Status, Category) | Composite key with Id tiebreaker |
| Multiple sort criteria | Composite key matching all columns |

### Composite Key Filter Logic

For `ORDER BY CreatedAt DESC, Id ASC` with cursor `{CreatedAt: '2025-01-15', Id: 42}`:

```sql
WHERE (CreatedAt < @CreatedAt)
   OR (CreatedAt = @CreatedAt AND Id > @Id)
```

---

## GraphQL Integration

Encina provides full Relay Connection specification support via the `Encina.GraphQL` package.

### Installation

```bash
dotnet add package Encina.GraphQL
```

### Relay Connection Types

| Type | Purpose |
|------|---------|
| `Connection<T>` | Relay-compliant connection with edges and page info |
| `Edge<T>` | Single item with its cursor |
| `RelayPageInfo` | Navigation information |

### Using Connection Types

```csharp
using Encina.GraphQL.Pagination;

public class Query
{
    public async Task<Connection<OrderDto>> GetOrders(
        [Service] IGraphQLEncinaBridge bridge,
        int first,
        string? after,
        CancellationToken ct)
    {
        var query = new GetOrdersQuery(after, first);
        var result = await bridge.QueryAsync<GetOrdersQuery, CursorPaginatedResult<OrderDto>>(
            query, ct);

        return result.Match(
            Right: orders => orders.ToConnection(),
            Left: error => throw new GraphQLException(error.Message));
    }
}
```

### Relay Connection Spec

The types follow the [Relay Connection specification](https://relay.dev/graphql/connections.htm):

```graphql
type OrderConnection {
  edges: [OrderEdge!]!
  nodes: [Order!]!
  pageInfo: PageInfo!
  totalCount: Int
}

type OrderEdge {
  node: Order!
  cursor: String!
}

type PageInfo {
  hasPreviousPage: Boolean!
  hasNextPage: Boolean!
  startCursor: String
  endCursor: String
}

query {
  orders(first: 20, after: "cursor") {
    edges {
      node {
        id
        total
      }
      cursor
    }
    pageInfo {
      hasNextPage
      endCursor
    }
    totalCount
  }
}
```

### Mapping Connections

Transform node types while preserving pagination metadata:

```csharp
var orderConnection = await GetOrdersConnectionAsync();
var dtoConnection = orderConnection.Map(order => new OrderDto(order.Id, order.Total));
```

### When to Use What

| Scenario | Type | Reason |
|----------|------|--------|
| Relay-compliant GraphQL | `Connection<T>` | Full spec with edges |
| Simple GraphQL | `CursorPaginatedResult<T>` | No edge wrapping needed |
| REST API | `CursorPaginatedResult<T>` | Optimized for REST |

See the [Encina.GraphQL README](../../src/Encina.GraphQL/README.md) for complete documentation.

---

## Best Practices

### 1. Index Your Sort Columns

```sql
-- Essential for cursor pagination performance
CREATE INDEX IX_Orders_CreatedAtUtc ON Orders(CreatedAtUtc DESC);

-- For composite keys
CREATE INDEX IX_Orders_CreatedAt_Id ON Orders(CreatedAtUtc DESC, Id ASC);
```

### 2. Use Projections

```csharp
// Good: Only selects needed columns
.ToCursorPaginatedAsync(
    selector: o => new OrderDto(o.Id, o.Total),
    ...);

// Less efficient: Loads entire entity
.ToCursorPaginatedAsync<Order, DateTime>(...);
```

### 3. Avoid TotalCount When Not Needed

```csharp
// TotalCount requires a separate COUNT(*) query
// Only request when UI genuinely needs it
var result = await query.ToCursorPaginatedAsync(...);
// result.TotalCount is null by default

// If you need total count (expensive!)
result = result with { TotalCount = await query.CountAsync() };
```

### 4. Use Consistent Page Sizes

```csharp
// Define constants
public static class PaginationDefaults
{
    public const int DefaultPageSize = 20;
    public const int MaxPageSize = 100;
}

// Enforce in queries
var pageSize = Math.Clamp(request.PageSize, 1, PaginationDefaults.MaxPageSize);
```

### 5. Handle Empty Results Gracefully

```csharp
var result = await query.ToCursorPaginatedAsync(...);

if (result.IsEmpty)
{
    return Results.Ok(CursorPaginatedResult<OrderDto>.Empty());
}
```

---

## FAQ

### Why are cursors opaque strings?

Opaque cursors (Base64-encoded) prevent clients from:
- Manipulating cursor values to skip items
- Making assumptions about internal structure
- Breaking when cursor format changes

### Can I decode cursors on the client?

Technically yes, but it's **strongly discouraged**. Cursor format is an implementation detail that may change.

### How do I handle deleted items?

Cursor pagination is stable: if the cursor item is deleted, pagination continues from the next available item. No items are skipped.

### What about concurrent inserts?

New items with keys that sort before the cursor position won't appear in subsequent pages. This is expected behavior - it ensures consistency during pagination.

### Can I use cursor pagination with complex filters?

Yes! Apply any filters before calling pagination methods:

```csharp
var result = await dbContext.Orders
    .Where(o => o.CustomerId == customerId)
    .Where(o => o.Status == OrderStatus.Pending)
    .Where(o => o.Total > 100)
    .OrderByDescending(o => o.CreatedAtUtc)
    .ToCursorPaginatedDescendingAsync(...);
```

### How do I customize cursor encoding?

Implement `ICursorEncoder` for custom encoding:

```csharp
public class EncryptedCursorEncoder : ICursorEncoder
{
    public string? Encode<T>(T? value) { /* encrypt + encode */ }
    public T? Decode<T>(string? cursor) { /* decode + decrypt */ }
}

// Register
services.AddCursorPagination<EncryptedCursorEncoder>();
```

---

## Related Documentation

- [Multi-Tenancy](./multi-tenancy.md)
- [Query Caching](./query-caching.md)
