namespace Encina.Testing.Examples.Domain;

/// <summary>
/// Sample query handler demonstrating typical query handling.
/// </summary>
public sealed class GetOrderHandler : IRequestHandler<GetOrderQuery, OrderDto>
{
    private readonly IOrderRepository _repository;

    public GetOrderHandler(IOrderRepository repository)
    {
        _repository = repository;
    }

    public async Task<Either<EncinaError, OrderDto>> Handle(
        GetOrderQuery request,
        CancellationToken cancellationToken)
    {
        var order = await _repository.GetByIdAsync(request.OrderId, cancellationToken);

        if (order is null)
        {
            return EncinaErrors.Create("encina.notfound.order", $"Order '{request.OrderId}' not found");
        }

        return order;
    }
}

/// <summary>
/// Repository interface for order storage.
/// </summary>
public interface IOrderRepository
{
    Task<OrderDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task AddAsync(OrderDto order, CancellationToken cancellationToken = default);
}

/// <summary>
/// In-memory implementation for testing.
/// </summary>
public sealed class InMemoryOrderRepository : IOrderRepository
{
    private readonly Dictionary<Guid, OrderDto> _orders = [];

    public Task<OrderDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        _orders.TryGetValue(id, out var order);
        return Task.FromResult(order);
    }

    public Task AddAsync(OrderDto order, CancellationToken cancellationToken = default)
    {
        _orders[order.Id] = order;
        return Task.CompletedTask;
    }

    public void Seed(OrderDto order) => _orders[order.Id] = order;
}
