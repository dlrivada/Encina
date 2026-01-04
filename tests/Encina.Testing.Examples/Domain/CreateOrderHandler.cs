using Encina.Messaging.Outbox;

namespace Encina.Testing.Examples.Domain;

/// <summary>
/// Sample handler demonstrating typical command handling with outbox pattern.
/// </summary>
public sealed class CreateOrderHandler : IRequestHandler<CreateOrderCommand, Guid>
{
    private readonly IOutboxStore? _outboxStore;
    private readonly IOutboxMessageFactory? _outboxMessageFactory;
    private readonly TimeProvider _timeProvider;

    public CreateOrderHandler(
        TimeProvider? timeProvider = null,
        IOutboxStore? outboxStore = null,
        IOutboxMessageFactory? outboxMessageFactory = null)
    {
        _timeProvider = timeProvider ?? TimeProvider.System;
        _outboxStore = outboxStore;
        _outboxMessageFactory = outboxMessageFactory;
    }

    public async Task<Either<EncinaError, Guid>> Handle(
        CreateOrderCommand request,
        CancellationToken cancellationToken)
    {
        // Validate
        if (string.IsNullOrWhiteSpace(request.CustomerId))
        {
            return EncinaErrors.Create("encina.validation.customerid", "Customer ID is required");
        }

        if (request.Amount <= 0)
        {
            return EncinaErrors.Create("encina.validation.amount", "Amount must be greater than zero");
        }

        // Create order
        var orderId = Guid.NewGuid();
        var createdAt = _timeProvider.GetUtcNow().UtcDateTime;

        // Publish event to outbox if configured
        if (_outboxStore is not null && _outboxMessageFactory is not null)
        {
            var @event = new OrderCreatedEvent(orderId, request.CustomerId, request.Amount, createdAt);
            var content = System.Text.Json.JsonSerializer.Serialize(@event);
            var outboxMessage = _outboxMessageFactory.Create(
                Guid.NewGuid(),
                typeof(OrderCreatedEvent).FullName ?? nameof(OrderCreatedEvent),
                content,
                createdAt);
            await _outboxStore.AddAsync(outboxMessage, cancellationToken);
        }

        return orderId;
    }
}
