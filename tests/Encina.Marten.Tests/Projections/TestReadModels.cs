using Encina.Marten.Projections;

namespace Encina.Marten.Tests.Projections;

/// <summary>
/// Sample read model for testing.
/// </summary>
public sealed class OrderSummary : IReadModel
{
    public Guid Id { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public int ItemCount { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; }
}

/// <summary>
/// Sample events for testing.
/// </summary>
public sealed record OrderCreated(string CustomerName);

public sealed record OrderItemAdded(string ProductName, decimal Price, int Quantity);

public sealed record OrderCompleted;

public sealed record OrderCancelled(string Reason);

/// <summary>
/// Sample projection for testing.
/// </summary>
public sealed class OrderSummaryProjection :
    IProjection<OrderSummary>,
    IProjectionCreator<OrderCreated, OrderSummary>,
    IProjectionHandler<OrderItemAdded, OrderSummary>,
    IProjectionHandler<OrderCompleted, OrderSummary>,
    IProjectionDeleter<OrderCancelled, OrderSummary>
{
    public string ProjectionName => "OrderSummary";

    public OrderSummary Create(OrderCreated domainEvent, ProjectionContext context)
    {
        return new OrderSummary
        {
            Id = context.StreamId,
            CustomerName = domainEvent.CustomerName,
            Status = "Created",
            CreatedAtUtc = context.Timestamp,
        };
    }

    public OrderSummary Apply(OrderItemAdded domainEvent, OrderSummary current, ProjectionContext context)
    {
        current.TotalAmount += domainEvent.Price * domainEvent.Quantity;
        current.ItemCount += domainEvent.Quantity;
        return current;
    }

    public OrderSummary Apply(OrderCompleted domainEvent, OrderSummary current, ProjectionContext context)
    {
        current.Status = "Completed";
        return current;
    }

    public bool ShouldDelete(OrderCancelled domainEvent, OrderSummary current, ProjectionContext context)
    {
        return true;
    }
}
