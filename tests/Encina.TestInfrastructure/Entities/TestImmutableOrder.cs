using Encina.DomainModeling;

namespace Encina.TestInfrastructure.Entities;

/// <summary>
/// Test aggregate root for immutable update integration tests.
/// Represents an order with immutable properties that can be updated
/// using the with-expression pattern.
/// </summary>
public class TestImmutableOrder : AggregateRoot<Guid>
{
    /// <summary>
    /// Gets the customer name for this order.
    /// </summary>
    public string CustomerName { get; init; } = string.Empty;

    /// <summary>
    /// Gets the current status of the order.
    /// </summary>
    public string Status { get; init; } = string.Empty;

    /// <summary>
    /// Initializes a new instance of the <see cref="TestImmutableOrder"/> class.
    /// </summary>
    public TestImmutableOrder() : base(Guid.NewGuid()) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="TestImmutableOrder"/> class with a specific ID.
    /// </summary>
    /// <param name="id">The unique identifier for this order.</param>
    public TestImmutableOrder(Guid id) : base(id) { }

    /// <summary>
    /// Raises a test domain event. Exposed for testing purposes.
    /// </summary>
    /// <param name="domainEvent">The domain event to raise.</param>
    public void RaiseTestEvent(IDomainEvent domainEvent) => RaiseDomainEvent(domainEvent);
}
