# Example: Basic CDC Setup

This example shows the minimal setup to capture and handle database changes using Encina CDC with SQL Server.

## Step 1: Define Your Entity

```csharp
public class Order
{
    public int Id { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public decimal Total { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}
```

## Step 2: Create a Change Handler

```csharp
using Encina.Cdc;
using Encina.Cdc.Abstractions;
using LanguageExt;
using static LanguageExt.Prelude;

public class OrderChangeHandler : IChangeEventHandler<Order>
{
    private readonly ILogger<OrderChangeHandler> _logger;

    public OrderChangeHandler(ILogger<OrderChangeHandler> logger)
        => _logger = logger;

    public ValueTask<Either<EncinaError, Unit>> HandleInsertAsync(
        Order entity, ChangeContext context)
    {
        _logger.LogInformation(
            "New order #{OrderId} for {Customer} (${Total}) at {Time}",
            entity.Id, entity.CustomerName, entity.Total,
            context.Metadata.CapturedAtUtc);
        return new(Right(unit));
    }

    public ValueTask<Either<EncinaError, Unit>> HandleUpdateAsync(
        Order before, Order after, ChangeContext context)
    {
        _logger.LogInformation(
            "Order #{OrderId} updated: total changed from ${OldTotal} to ${NewTotal}",
            after.Id, before.Total, after.Total);
        return new(Right(unit));
    }

    public ValueTask<Either<EncinaError, Unit>> HandleDeleteAsync(
        Order entity, ChangeContext context)
    {
        _logger.LogWarning("Order #{OrderId} was deleted", entity.Id);
        return new(Right(unit));
    }
}
```

## Step 3: Register Services

```csharp
var builder = Host.CreateApplicationBuilder(args);

// 1. Register CDC core services
builder.Services.AddEncinaCdc(config =>
{
    config.UseCdc()
          .AddHandler<Order, OrderChangeHandler>()
          .WithTableMapping<Order>("dbo.Orders")
          .WithOptions(opts =>
          {
              opts.PollingInterval = TimeSpan.FromSeconds(2);
              opts.BatchSize = 50;
              opts.EnablePositionTracking = true;
          });
});

// 2. Register the SQL Server provider
builder.Services.AddEncinaCdcSqlServer(opts =>
{
    opts.ConnectionString = builder.Configuration.GetConnectionString("Default")!;
    opts.TrackedTables = ["dbo.Orders"];
});

var app = builder.Build();
await app.RunAsync();
```

## Step 4: Enable Change Tracking in SQL Server

```sql
-- Enable on the database
ALTER DATABASE MyDatabase
SET CHANGE_TRACKING = ON
(CHANGE_RETENTION = 2 DAYS, AUTO_CLEANUP = ON);

-- Enable on the table
ALTER TABLE dbo.Orders
ENABLE CHANGE_TRACKING;
```

## Step 5: Run

The `CdcProcessor` starts automatically as a `BackgroundService`. Make changes to the `Orders` table and observe the handler log output.

## Multiple Handlers

You can register multiple handlers for different entity types:

```csharp
builder.Services.AddEncinaCdc(config =>
{
    config.UseCdc()
          .AddHandler<Order, OrderChangeHandler>()
          .AddHandler<Customer, CustomerChangeHandler>()
          .WithTableMapping<Order>("dbo.Orders")
          .WithTableMapping<Customer>("dbo.Customers");
});
```

## Related

- [CDC Feature Guide](../features/cdc.md)
- [SQL Server CDC Provider](../features/cdc-sqlserver.md)
- [Position Tracking Example](cdc-position-tracking.md)
