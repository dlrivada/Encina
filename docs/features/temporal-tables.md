# Temporal Tables

Temporal tables provide built-in support for tracking data changes over time. Encina provides a repository abstraction for querying temporal data with point-in-time queries and history retrieval.

## Overview

Encina's `ITemporalRepository<TEntity, TId>` provides:

- **Point-in-time queries**: Retrieve entity state at any past moment
- **History retrieval**: Get all versions of an entity
- **Time range queries**: Find entities changed within a specific period
- **Specification support**: Apply filters to historical queries

## Provider Support

| Provider | Implementation | Native Support | Notes |
|----------|---------------|----------------|-------|
| **SQL Server (EF Core)** | `TemporalRepositoryEF` | ✅ Native | Uses EF Core's built-in `TemporalAsOf`, `TemporalAll`, `TemporalBetween` |
| **SQL Server (Dapper)** | `TemporalRepositoryDapper` | ✅ Native | Uses `FOR SYSTEM_TIME` clauses directly |
| **SQL Server (ADO.NET)** | `TemporalRepositoryADO` | ✅ Native | Uses `FOR SYSTEM_TIME` clauses directly |
| **PostgreSQL (EF Core)** | `TemporalRepositoryPostgreSqlEF` | ⚠️ Extension | Requires `temporal_tables` extension |
| **PostgreSQL (Dapper)** | `TemporalRepositoryDapper` | ⚠️ Extension | Requires `temporal_tables` extension |
| **PostgreSQL (ADO.NET)** | `TemporalRepositoryADO` | ⚠️ Extension | Requires `temporal_tables` extension |
| MySQL | ❌ Not available | ❌ | See [Manual Implementation](#manual-implementation-for-unsupported-databases) |
| SQLite | ❌ Not available | ❌ | See [Manual Implementation](#manual-implementation-for-unsupported-databases) |
| MongoDB | ❌ Not available | ❌ | Use document versioning pattern instead |

### Why Some Providers Are Not Supported

- **MySQL**: Does not have native temporal table support. As of MySQL 8.0, there is no system-versioned table feature. You can implement audit tracking manually using triggers.
- **SQLite**: SQLite is a lightweight embedded database without temporal table functionality. For SQLite use cases, manual history tables with triggers are the recommended approach.
- **MongoDB**: MongoDB is a document database with a different data model. Consider using document versioning (embedding version history in documents) or a separate audit collection.

## SQL Server Implementation

SQL Server has **native temporal table support** since SQL Server 2016. This is the most robust and performant implementation.

### Prerequisites

- **SQL Server**: 2016 or later
- **.NET**: 10.0+
- **Packages**:
  - `Encina.EntityFrameworkCore` (for EF Core)
  - `Encina.Dapper.SqlServer` (for Dapper)
  - `Encina.ADO.SqlServer` (for ADO.NET)

### DbContext Configuration (EF Core)

```csharp
public class AppDbContext : DbContext
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Configure temporal table with defaults
        modelBuilder.Entity<Order>().ConfigureTemporalTable();

        // Or with custom configuration
        modelBuilder.Entity<Order>().ConfigureTemporalTable(builder =>
        {
            builder.UseHistoryTable("OrderHistory", "history");
            builder.HasPeriodStart("ValidFrom");
            builder.HasPeriodEnd("ValidTo");
        });
    }
}
```

### SQL Server Table Structure

```sql
CREATE TABLE Orders (
    Id UNIQUEIDENTIFIER PRIMARY KEY,
    CustomerName NVARCHAR(200) NOT NULL,
    Total DECIMAL(18,2) NOT NULL,
    Status NVARCHAR(50) NOT NULL,

    -- System-versioning columns (managed automatically by SQL Server)
    ValidFrom DATETIME2 GENERATED ALWAYS AS ROW START NOT NULL,
    ValidTo DATETIME2 GENERATED ALWAYS AS ROW END NOT NULL,
    PERIOD FOR SYSTEM_TIME (ValidFrom, ValidTo)
)
WITH (SYSTEM_VERSIONING = ON (HISTORY_TABLE = dbo.OrderHistory));
```

### Service Registration

```csharp
// EF Core
services.AddScoped<ITemporalRepository<Order, OrderId>, TemporalRepositoryEF<Order, OrderId>>();

// Dapper
services.AddScoped<ITemporalRepository<Order, OrderId>, TemporalRepositoryDapper<Order, OrderId>>();

// ADO.NET
services.AddScoped<ITemporalRepository<Order, OrderId>, TemporalRepositoryADO<Order, OrderId>>();
```

## PostgreSQL Implementation

PostgreSQL does **NOT** have native temporal table support. Encina's PostgreSQL implementation uses the third-party [`temporal_tables`](https://github.com/arkhipov/temporal_tables) extension.

> **Warning**: The `temporal_tables` extension may not be available in all PostgreSQL deployments, particularly managed cloud instances (Amazon RDS, Google Cloud SQL, Azure Database for PostgreSQL). Verify extension availability before using this feature.

### Prerequisites

- **PostgreSQL**: 9.5 or later
- **Extension**: `temporal_tables` extension installed
- **.NET**: 10.0+
- **Packages**:
  - `Encina.EntityFrameworkCore` (for EF Core + PostgreSQL)
  - `Encina.Dapper.PostgreSQL` (for Dapper)
  - `Encina.ADO.PostgreSQL` (for ADO.NET)

### PostgreSQL Setup

1. **Install the extension**:

```sql
CREATE EXTENSION IF NOT EXISTS temporal_tables;
```

1. **Create the main table with a period column**:

```sql
CREATE TABLE orders (
    id UUID PRIMARY KEY,
    customer_name VARCHAR(200) NOT NULL,
    total DECIMAL(18,2) NOT NULL,
    status VARCHAR(50) NOT NULL,

    -- Period column for temporal tracking (tstzrange = timestamp with timezone range)
    sys_period tstzrange NOT NULL DEFAULT tstzrange(current_timestamp, NULL)
);
```

1. **Create the history table**:

```sql
CREATE TABLE orders_history (LIKE orders);
```

1. **Create the versioning trigger**:

```sql
CREATE TRIGGER versioning_trigger
BEFORE INSERT OR UPDATE OR DELETE ON orders
FOR EACH ROW EXECUTE PROCEDURE versioning('sys_period', 'orders_history', true);
```

### Service Registration (PostgreSQL)

```csharp
// EF Core for PostgreSQL
services.AddScoped<ITemporalRepository<Order, OrderId>>(sp =>
{
    var dbContext = sp.GetRequiredService<AppDbContext>();
    var options = new TemporalTableOptions { ValidateUtcDateTime = true };
    var pgOptions = new PostgreSqlTemporalOptions
    {
        MainTableName = "orders",
        HistoryTableName = "orders_history",
        PeriodColumnName = "sys_period"
    };
    var logger = sp.GetRequiredService<ILogger<TemporalRepositoryPostgreSqlEF<Order, OrderId>>>();
    return new TemporalRepositoryPostgreSqlEF<Order, OrderId>(dbContext, options, pgOptions, logger);
});
```

## Temporal Repository Interface

```csharp
public interface ITemporalRepository<TEntity, TId>
    where TEntity : class, IEntity<TId>
    where TId : notnull
{
    /// <summary>
    /// Gets the entity state at a specific point in time.
    /// </summary>
    Task<Either<RepositoryError, TEntity>> GetAsOfAsync(
        TId id,
        DateTime asOfUtc,
        CancellationToken ct = default);

    /// <summary>
    /// Gets the complete history of an entity.
    /// </summary>
    Task<Either<RepositoryError, IReadOnlyList<TEntity>>> GetHistoryAsync(
        TId id,
        CancellationToken ct = default);

    /// <summary>
    /// Gets entities that changed within a time range.
    /// </summary>
    Task<Either<RepositoryError, IReadOnlyList<TEntity>>> GetChangedBetweenAsync(
        DateTime startUtc,
        DateTime endUtc,
        CancellationToken ct = default);

    /// <summary>
    /// Lists entities matching a specification at a specific point in time.
    /// </summary>
    Task<Either<RepositoryError, IReadOnlyList<TEntity>>> ListAsOfAsync(
        Specification<TEntity> specification,
        DateTime asOfUtc,
        CancellationToken ct = default);
}
```

## Usage Examples

### Point-in-Time Query

Retrieve the state of an entity at a specific moment:

```csharp
// Get order state from last week
var lastWeek = DateTime.UtcNow.AddDays(-7);
var result = await temporalRepository.GetAsOfAsync(orderId, lastWeek);

result.Match(
    Right: order => Console.WriteLine($"Order status last week: {order.Status}"),
    Left: error => Console.WriteLine($"Error: {error.Message}")
);
```

### Entity History

Get all versions of an entity to see how it changed over time:

```csharp
var result = await temporalRepository.GetHistoryAsync(orderId);

result.Match(
    Right: history =>
    {
        Console.WriteLine($"Order has {history.Count} historical versions:");
        foreach (var version in history)
        {
            Console.WriteLine($"  Status: {version.Status}, Total: {version.Total}");
        }
    },
    Left: error => Console.WriteLine($"Error: {error.Message}")
);
```

### Time Range Query

Find all entities that changed within a specific period:

```csharp
var startOfMonth = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
var endOfMonth = new DateTime(2026, 1, 31, 23, 59, 59, DateTimeKind.Utc);

var result = await temporalRepository.GetChangedBetweenAsync(startOfMonth, endOfMonth);

result.Match(
    Right: changes => Console.WriteLine($"Found {changes.Count} order changes in January"),
    Left: error => Console.WriteLine($"Error: {error.Message}")
);
```

### Historical Query with Specification

Apply filters to historical data:

```csharp
var spec = new OrdersByStatusSpec(OrderStatus.Completed);
var lastQuarter = DateTime.UtcNow.AddMonths(-3);

var result = await temporalRepository.ListAsOfAsync(spec, lastQuarter);

result.Match(
    Right: orders => Console.WriteLine($"Completed orders 3 months ago: {orders.Count}"),
    Left: error => Console.WriteLine($"Error: {error.Message}")
);
```

## Manual Implementation for Unsupported Databases

For databases without temporal table support (MySQL, SQLite), you can implement history tracking manually using triggers. This approach is not as integrated as native temporal tables but provides similar functionality.

### Strategy Overview

| Aspect | Pros | Cons |
|--------|------|------|
| **Trigger-based history** | Works on any database, no extensions needed | Manual setup, performance overhead on writes |
| **Application-level versioning** | Full control, database agnostic | More code to maintain, risk of missed changes |
| **Event sourcing** | Complete audit trail, replay capability | Significant architectural change |

### MySQL Example: Trigger-Based History

1. **Create the main table**:

```sql
CREATE TABLE orders (
    id CHAR(36) PRIMARY KEY,
    customer_name VARCHAR(200) NOT NULL,
    total DECIMAL(18,2) NOT NULL,
    status VARCHAR(50) NOT NULL,
    valid_from DATETIME(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
    valid_to DATETIME(6) NULL,

    INDEX idx_orders_valid_from (valid_from),
    INDEX idx_orders_valid_to (valid_to)
);
```

1. **Create the history table**:

```sql
CREATE TABLE orders_history (
    history_id BIGINT AUTO_INCREMENT PRIMARY KEY,
    id CHAR(36) NOT NULL,
    customer_name VARCHAR(200) NOT NULL,
    total DECIMAL(18,2) NOT NULL,
    status VARCHAR(50) NOT NULL,
    valid_from DATETIME(6) NOT NULL,
    valid_to DATETIME(6) NOT NULL,

    INDEX idx_orders_history_id (id),
    INDEX idx_orders_history_period (valid_from, valid_to)
);
```

1. **Create triggers**:

```sql
-- Before UPDATE: Archive the old version
DELIMITER //
CREATE TRIGGER orders_before_update
BEFORE UPDATE ON orders
FOR EACH ROW
BEGIN
    INSERT INTO orders_history (id, customer_name, total, status, valid_from, valid_to)
    VALUES (OLD.id, OLD.customer_name, OLD.total, OLD.status, OLD.valid_from, NOW(6));

    SET NEW.valid_from = NOW(6);
END//
DELIMITER ;

-- Before DELETE: Archive the deleted version
DELIMITER //
CREATE TRIGGER orders_before_delete
BEFORE DELETE ON orders
FOR EACH ROW
BEGIN
    INSERT INTO orders_history (id, customer_name, total, status, valid_from, valid_to)
    VALUES (OLD.id, OLD.customer_name, OLD.total, OLD.status, OLD.valid_from, NOW(6));
END//
DELIMITER ;
```

1. **Query historical data**:

```sql
-- Point-in-time query
SELECT * FROM (
    SELECT id, customer_name, total, status, valid_from, valid_to
    FROM orders
    WHERE id = @OrderId
    UNION ALL
    SELECT id, customer_name, total, status, valid_from, valid_to
    FROM orders_history
    WHERE id = @OrderId
) AS temporal_data
WHERE valid_from <= @AsOfUtc AND (valid_to IS NULL OR valid_to > @AsOfUtc)
LIMIT 1;
```

### SQLite Example: Trigger-Based History

SQLite also supports triggers for history tracking:

1. **Create tables**:

```sql
CREATE TABLE orders (
    id TEXT PRIMARY KEY,
    customer_name TEXT NOT NULL,
    total REAL NOT NULL,
    status TEXT NOT NULL,
    valid_from TEXT NOT NULL DEFAULT (datetime('now'))
);

CREATE TABLE orders_history (
    history_id INTEGER PRIMARY KEY AUTOINCREMENT,
    id TEXT NOT NULL,
    customer_name TEXT NOT NULL,
    total REAL NOT NULL,
    status TEXT NOT NULL,
    valid_from TEXT NOT NULL,
    valid_to TEXT NOT NULL
);
```

1. **Create triggers**:

```sql
CREATE TRIGGER orders_before_update
BEFORE UPDATE ON orders
BEGIN
    INSERT INTO orders_history (id, customer_name, total, status, valid_from, valid_to)
    VALUES (OLD.id, OLD.customer_name, OLD.total, OLD.status, OLD.valid_from, datetime('now'));
END;

CREATE TRIGGER orders_before_delete
BEFORE DELETE ON orders
BEGIN
    INSERT INTO orders_history (id, customer_name, total, status, valid_from, valid_to)
    VALUES (OLD.id, OLD.customer_name, OLD.total, OLD.status, OLD.valid_from, datetime('now'));
END;
```

### Considerations for Manual Implementation

- **Performance**: Triggers add overhead to write operations
- **Maintenance**: History tables can grow large; implement retention policies
- **Consistency**: Application must not bypass triggers (no bulk operations that skip triggers)
- **Testing**: Thoroughly test temporal queries across different time zones

## Important Notes

### UTC DateTime Requirement

All temporal queries require UTC timestamps:

```csharp
// Correct
var utcTime = DateTime.UtcNow.AddDays(-7);
await temporalRepository.GetAsOfAsync(id, utcTime);

// Incorrect - will produce unexpected results
var localTime = DateTime.Now.AddDays(-7);
await temporalRepository.GetAsOfAsync(id, localTime);
```

When `TemporalTableOptions.ValidateUtcDateTime` is enabled (default), the repository will return an error if a non-UTC DateTime is provided.

### Performance Considerations

- History tables can grow large over time
- Consider implementing a retention policy for historical data
- Add appropriate indexes on history tables for common queries
- Use `ListAsOfAsync` with specifications to limit result sets

### Combining with Soft Delete

Temporal tables work independently of soft delete:

```csharp
public class Order : IEntity<OrderId>, ISoftDeletableEntity
{
    public OrderId Id { get; set; }
    public string CustomerName { get; set; }

    // Soft delete properties
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAtUtc { get; set; }
    public string? DeletedBy { get; set; }

    // Temporal columns are managed by the database
}
```

When an entity is soft-deleted:

1. The soft delete fields are updated (IsDeleted = true)
2. The database automatically captures this change in the history table
3. You can query historical state before and after the soft delete

## Best Practices

1. **Use UTC consistently**: All timestamps must be UTC
2. **Plan for storage growth**: History tables accumulate data
3. **Index strategically**: Add indexes for common temporal queries
4. **Implement retention**: Use database-specific retention policies
5. **Test time boundaries**: Verify behavior at exact timestamps
6. **Verify extension availability**: For PostgreSQL, ensure `temporal_tables` is available before deployment

## Related Features

- [Soft Delete](soft-delete.md) - Logical deletion pattern
- [Audit Trail](audit-tracking.md) - Track who created/modified entities
- [Patterns Guide](../architecture/patterns-guide.md) - Data access patterns
