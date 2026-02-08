# Encina.GraphQL

GraphQL integration for Encina using HotChocolate. Maps GraphQL queries, mutations, and subscriptions to Encina requests with full Relay Connection specification support for cursor-based pagination.

## Installation

```bash
dotnet add package Encina.GraphQL
```

## Features

- **Bridge Pattern**: Map GraphQL operations to Encina requests seamlessly
- **Relay Connections**: Full compliance with the [Relay Connection specification](https://relay.dev/graphql/connections.htm)
- **Cursor Pagination**: O(1) performance pagination with bidirectional navigation
- **Type Safety**: Strong typing with HotChocolate's GraphQL type system
- **Railway Oriented**: Returns `Either<EncinaError, TResult>` for explicit error handling

## Quick Start

### Configuration

```csharp
// Register GraphQL services
services.AddEncinaGraphQL(options =>
{
    options.Path = "/graphql";
    options.EnableGraphQLIDE = true;        // Enable Banana Cake Pop
    options.MaxExecutionDepth = 15;         // Prevent DoS attacks
    options.ExecutionTimeout = TimeSpan.FromSeconds(30);
});

// Configure HotChocolate
services.AddGraphQLServer()
    .AddQueryType<Query>()
    .AddMutationType<Mutation>();
```

### Using the Bridge

```csharp
public class Query
{
    public async Task<Order> GetOrder(
        [Service] IGraphQLEncinaBridge bridge,
        Guid id,
        CancellationToken ct)
    {
        var result = await bridge.QueryAsync<GetOrderQuery, Order>(
            new GetOrderQuery(id), ct);

        return result.Match(
            Right: order => order,
            Left: error => throw new GraphQLException(error.Message));
    }
}
```

## Relay Connection Types

For APIs following the GraphQL Relay specification, use the Connection types:

### Types Overview

| Type | Purpose |
|------|---------|
| `Connection<T>` | Relay-compliant connection with edges and page info |
| `Edge<T>` | Single item with its cursor |
| `RelayPageInfo` | Navigation information (hasNext, hasPrevious, cursors) |

### Schema Representation

```graphql
type OrderConnection {
  edges: [OrderEdge!]!
  nodes: [Order!]!           # Convenience accessor
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
```

### Example Query

```graphql
query GetOrders($first: Int!, $after: String) {
  orders(first: $first, after: $after) {
    edges {
      node {
        id
        total
        status
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

## Using Connection Types

### From CursorPaginatedResult (REST-style)

When you have a `CursorPaginatedResult<T>` (e.g., from REST endpoints):

```csharp
public class Query
{
    public async Task<Connection<OrderDto>> GetOrders(
        [Service] IGraphQLEncinaBridge bridge,
        [Service] ICursorEncoder encoder,
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

> **Note**: Converting from `CursorPaginatedResult<T>` approximates per-item cursors. The first item gets `PreviousCursor`, the last gets `NextCursor`, and middle items use the end cursor.

### Mapping Connections

Transform node types while preserving pagination:

```csharp
// Map entities to DTOs
var connection = await GetOrdersConnectionAsync();
var dtoConnection = connection.Map(order => new OrderDto(order.Id, order.Total));
```

## When to Use What

| Scenario | Type to Use | Reason |
|----------|-------------|--------|
| Relay-compliant GraphQL API | `Connection<T>` | Full spec compliance with edges |
| Simple GraphQL pagination | `CursorPaginatedResult<T>` | Simpler, no edge wrapping |
| REST API | `CursorPaginatedResult<T>` | Optimized for REST responses |
| BFF (Backend for Frontend) | `Connection<T>` | GraphQL clients expect Relay |

## Configuration Options

| Option | Default | Description |
|--------|---------|-------------|
| `Path` | `/graphql` | GraphQL endpoint path |
| `EnableGraphQLIDE` | `true` | Enable Banana Cake Pop IDE |
| `EnableIntrospection` | `true` | Allow schema introspection |
| `IncludeExceptionDetails` | `false` | Include exception details in errors |
| `MaxExecutionDepth` | `15` | Maximum query depth (DoS protection) |
| `ExecutionTimeout` | `30s` | Request timeout |
| `EnableSubscriptions` | `false` | Enable GraphQL subscriptions |
| `EnablePersistedQueries` | `false` | Enable persisted query support |

## Integration with HotChocolate Pagination

HotChocolate has its own pagination system. Encina's Connection types are designed to work alongside it:

```csharp
// Using HotChocolate's built-in pagination
[UseConnection]
public IQueryable<Order> GetOrders([Service] AppDbContext db)
    => db.Orders.OrderByDescending(o => o.CreatedAt);

// Using Encina's Connection for custom pagination logic
public async Task<Connection<OrderDto>> GetOrdersCustom(
    [Service] IOrderRepository repository,
    int first, string? after)
{
    var result = await repository.GetOrdersAsync(after, first);
    return result.ToConnection();
}
```

Use HotChocolate's `[UseConnection]` when you want automatic pagination over `IQueryable`. Use Encina's `Connection<T>` when you need:

- Custom pagination logic
- Non-EF Core data sources
- Complex business rules in pagination
- Integration with Encina's messaging patterns

## Error Handling

The bridge returns `Either<EncinaError, TResult>` for explicit error handling:

```csharp
var result = await bridge.QueryAsync<GetOrderQuery, Order>(query, ct);

return result.Match(
    Right: order => order,
    Left: error => error.Code switch
    {
        "NOT_FOUND" => throw new GraphQLException(
            ErrorBuilder.New()
                .SetMessage(error.Message)
                .SetCode("ORDER_NOT_FOUND")
                .Build()),
        _ => throw new GraphQLException(error.Message)
    });
```

## Related Packages

- [`Encina.DomainModeling`](../Encina.DomainModeling/README.md) - Cursor pagination types
- [`Encina.EntityFrameworkCore`](../Encina.EntityFrameworkCore/README.md) - EF Core pagination extensions

## Documentation

- [Cursor Pagination Guide](../../docs/features/cursor-pagination.md)
- [GraphQL Relay Connection Spec](https://relay.dev/graphql/connections.htm)
- [HotChocolate Documentation](https://chillicream.com/docs/hotchocolate)
