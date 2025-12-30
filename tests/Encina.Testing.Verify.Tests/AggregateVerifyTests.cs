using Encina.DomainModeling;

namespace Encina.Testing.Verify.Tests;

/// <summary>
/// Unit tests for aggregate verification methods in <see cref="EncinaVerify"/>.
/// </summary>
public class AggregateVerifyTests
{
    [Fact]
    public Task PrepareUncommittedEvents_WithEvents_ReturnsCorrectSnapshot()
    {
        // Arrange
        var aggregate = new TestAggregate();
        aggregate.Create(Guid.Parse("55555555-5555-5555-5555-555555555555"), "Test Order");
        aggregate.AddItem("PROD-001", 2);

        // Act
        var result = EncinaVerify.PrepareUncommittedEvents(aggregate);

        // Assert
        return Verifier.Verify(result);
    }

    [Fact]
    public Task PrepareUncommittedEvents_WithNoEvents_ReturnsEmptySnapshot()
    {
        // Arrange
        var aggregate = new TestAggregate();

        // Act
        var result = EncinaVerify.PrepareUncommittedEvents(aggregate);

        // Assert
        return Verifier.Verify(result);
    }

    [Fact]
    public void PrepareUncommittedEvents_WithNull_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => EncinaVerify.PrepareUncommittedEvents(null!));
    }
}

#region Test Aggregates and Events

/// <summary>
/// Test aggregate for snapshot testing.
/// </summary>
public class TestAggregate : AggregateBase<Guid>
{
    public string Name { get; private set; } = string.Empty;
    public List<TestItem> Items { get; private set; } = [];

    public void Create(Guid id, string name)
    {
        RaiseEvent(new TestOrderCreated(id, name));
    }

    public void AddItem(string productId, int quantity)
    {
        RaiseEvent(new TestItemAdded(productId, quantity));
    }

    protected override void Apply(object domainEvent)
    {
        switch (domainEvent)
        {
            case TestOrderCreated created:
                Id = created.OrderId;
                Name = created.Name;
                break;
            case TestItemAdded itemAdded:
                Items.Add(new TestItem(itemAdded.ProductId, itemAdded.Quantity));
                break;
        }
    }
}

public sealed record TestOrderCreated(Guid OrderId, string Name);
public sealed record TestItemAdded(string ProductId, int Quantity);
public sealed record TestItem(string ProductId, int Quantity);

#endregion
