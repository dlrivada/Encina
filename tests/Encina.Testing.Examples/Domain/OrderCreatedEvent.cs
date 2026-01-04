namespace Encina.Testing.Examples.Domain;

/// <summary>
/// Sample notification for demonstrating outbox testing patterns.
/// </summary>
public sealed record OrderCreatedEvent(
    Guid OrderId,
    string CustomerId,
    decimal Amount,
    DateTime CreatedAtUtc) : INotification;
