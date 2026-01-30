using Encina.DomainModeling;

namespace Encina.TestInfrastructure.Entities;

/// <summary>
/// Test domain event for order operations.
/// Used in immutable update integration tests.
/// </summary>
/// <param name="OrderId">The ID of the order this event relates to.</param>
/// <param name="Action">The action that was performed on the order.</param>
public sealed record TestOrderEvent(Guid OrderId, string Action) : IDomainEvent
{
    /// <inheritdoc />
    public Guid EventId { get; init; } = Guid.NewGuid();

    /// <inheritdoc />
    public DateTime OccurredAtUtc { get; init; } = DateTime.UtcNow;
}
