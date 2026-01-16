using Encina.DomainModeling;
using Encina.Testing.Shouldly;

namespace Encina.UnitTests.Testing.Shouldly;

/// <summary>
/// Unit tests for <see cref="AggregateShouldlyExtensions"/>.
/// </summary>
public sealed class AggregateShouldlyExtensionsTests
{
    #region Test Aggregates and Events

    private sealed record OrderCreated(Guid OrderId, string CustomerName);
    private sealed record ItemAdded(Guid OrderId, string ItemName, int Quantity);
    private sealed record OrderShipped(Guid OrderId, DateTime ShippedAt);

    private sealed class TestOrder : AggregateBase
    {
        public string CustomerName { get; private set; } = string.Empty;
        public List<(string Name, int Quantity)> Items { get; } = [];
        public bool IsShipped { get; private set; }

        public void Create(Guid id, string customerName)
        {
            RaiseEvent(new OrderCreated(id, customerName));
        }

        public void AddItem(string name, int quantity)
        {
            RaiseEvent(new ItemAdded(Id, name, quantity));
        }

        public void Ship()
        {
            RaiseEvent(new OrderShipped(Id, DateTime.UtcNow));
        }

        protected override void Apply(object domainEvent)
        {
            switch (domainEvent)
            {
                case OrderCreated e:
                    Id = e.OrderId;
                    CustomerName = e.CustomerName;
                    break;
                case ItemAdded e:
                    Items.Add((e.ItemName, e.Quantity));
                    break;
                case OrderShipped:
                    IsShipped = true;
                    break;
            }
        }
    }

    #endregion

    #region ShouldHaveRaisedEvent Tests

    [Fact]
    public void ShouldHaveRaisedEvent_WhenEventExists_ReturnsEvent()
    {
        // Arrange
        var order = new TestOrder();
        order.Create(Guid.NewGuid(), "John Doe");

        // Act
        var evt = order.ShouldHaveRaisedEvent<OrderCreated>();

        // Assert
        evt.CustomerName.ShouldBe("John Doe");
    }

    [Fact]
    public void ShouldHaveRaisedEvent_WhenEventDoesNotExist_Throws()
    {
        // Arrange
        var order = new TestOrder();
        order.Create(Guid.NewGuid(), "John Doe");

        // Act & Assert
        var exception = Should.Throw<ShouldAssertException>(() =>
            order.ShouldHaveRaisedEvent<OrderShipped>());

        exception.Message.ShouldContain("OrderShipped");
        exception.Message.ShouldContain("was not");
    }

    [Fact]
    public void ShouldHaveRaisedEvent_WhenNoEventsRaised_Throws()
    {
        // Arrange
        var order = new TestOrder();

        // Act & Assert
        var exception = Should.Throw<ShouldAssertException>(() =>
            order.ShouldHaveRaisedEvent<OrderCreated>());

        exception.Message.ShouldContain("No events were raised");
    }

    [Fact]
    public void ShouldHaveRaisedEvent_ReturnsFirstEventOfType()
    {
        // Arrange
        var order = new TestOrder();
        order.Create(Guid.NewGuid(), "John Doe");
        order.AddItem("Widget", 2);
        order.AddItem("Gadget", 1);

        // Act
        var evt = order.ShouldHaveRaisedEvent<ItemAdded>();

        // Assert
        evt.ItemName.ShouldBe("Widget");
    }

    #endregion

    #region ShouldHaveRaisedEvents (count) Tests

    [Fact]
    public void ShouldHaveRaisedEvents_WhenCountMatches_ReturnsEvents()
    {
        // Arrange
        var order = new TestOrder();
        order.Create(Guid.NewGuid(), "John Doe");
        order.AddItem("Widget", 2);
        order.AddItem("Gadget", 1);

        // Act
        var events = order.ShouldHaveRaisedEvents<ItemAdded>(2);

        // Assert
        events.Count.ShouldBe(2);
        events[0].ItemName.ShouldBe("Widget");
        events[1].ItemName.ShouldBe("Gadget");
    }

    [Fact]
    public void ShouldHaveRaisedEvents_WhenCountDoesNotMatch_Throws()
    {
        // Arrange
        var order = new TestOrder();
        order.Create(Guid.NewGuid(), "John Doe");
        order.AddItem("Widget", 2);

        // Act & Assert
        Should.Throw<ShouldAssertException>(() =>
            order.ShouldHaveRaisedEvents<ItemAdded>(3));
    }

    #endregion

    #region ShouldHaveRaisedEvent with predicate Tests

    [Fact]
    public void ShouldHaveRaisedEvent_WithPredicate_WhenMatches_ReturnsEvent()
    {
        // Arrange
        var order = new TestOrder();
        order.Create(Guid.NewGuid(), "John Doe");
        order.AddItem("Widget", 2);
        order.AddItem("Gadget", 5);

        // Act
        var evt = order.ShouldHaveRaisedEvent<ItemAdded>(e => e.Quantity > 3);

        // Assert
        evt.ItemName.ShouldBe("Gadget");
        evt.Quantity.ShouldBe(5);
    }

    [Fact]
    public void ShouldHaveRaisedEvent_WithPredicate_WhenNoMatch_Throws()
    {
        // Arrange
        var order = new TestOrder();
        order.Create(Guid.NewGuid(), "John Doe");
        order.AddItem("Widget", 2);

        // Act & Assert
        Should.Throw<ShouldAssertException>(() =>
            order.ShouldHaveRaisedEvent<ItemAdded>(e => e.Quantity > 10));
    }

    #endregion

    #region ShouldNotHaveRaisedEvent Tests

    [Fact]
    public void ShouldNotHaveRaisedEvent_WhenEventNotRaised_Succeeds()
    {
        // Arrange
        var order = new TestOrder();
        order.Create(Guid.NewGuid(), "John Doe");

        // Act & Assert - should not throw
        order.ShouldNotHaveRaisedEvent<OrderShipped>();
    }

    [Fact]
    public void ShouldNotHaveRaisedEvent_WhenEventRaised_Throws()
    {
        // Arrange
        var order = new TestOrder();
        order.Create(Guid.NewGuid(), "John Doe");
        order.Ship();

        // Act & Assert
        Should.Throw<ShouldAssertException>(() =>
            order.ShouldNotHaveRaisedEvent<OrderShipped>());
    }

    #endregion

    #region ShouldHaveNoUncommittedEvents Tests

    [Fact]
    public void ShouldHaveNoUncommittedEvents_WhenNoEvents_Succeeds()
    {
        // Arrange
        var order = new TestOrder();

        // Act & Assert - should not throw
        order.ShouldHaveNoUncommittedEvents();
    }

    [Fact]
    public void ShouldHaveNoUncommittedEvents_AfterClear_Succeeds()
    {
        // Arrange
        var order = new TestOrder();
        order.Create(Guid.NewGuid(), "John Doe");
        order.ClearUncommittedEvents();

        // Act & Assert - should not throw
        order.ShouldHaveNoUncommittedEvents();
    }

    [Fact]
    public void ShouldHaveNoUncommittedEvents_WhenHasEvents_Throws()
    {
        // Arrange
        var order = new TestOrder();
        order.Create(Guid.NewGuid(), "John Doe");

        // Act & Assert
        var exception = Should.Throw<ShouldAssertException>(() =>
            order.ShouldHaveNoUncommittedEvents());

        exception.Message.ShouldContain("found 1");
    }

    #endregion

    #region ShouldHaveUncommittedEventCount Tests

    [Fact]
    public void ShouldHaveUncommittedEventCount_WhenCountMatches_ReturnsEvents()
    {
        // Arrange
        var order = new TestOrder();
        order.Create(Guid.NewGuid(), "John Doe");
        order.AddItem("Widget", 2);
        order.Ship();

        // Act
        var events = order.ShouldHaveUncommittedEventCount(3);

        // Assert
        events.Count.ShouldBe(3);
    }

    [Fact]
    public void ShouldHaveUncommittedEventCount_WhenCountDoesNotMatch_Throws()
    {
        // Arrange
        var order = new TestOrder();
        order.Create(Guid.NewGuid(), "John Doe");

        // Act & Assert
        Should.Throw<ShouldAssertException>(() =>
            order.ShouldHaveUncommittedEventCount(5));
    }

    #endregion

    #region ShouldHaveVersion Tests

    [Fact]
    public void ShouldHaveVersion_WhenVersionMatches_Succeeds()
    {
        // Arrange
        var order = new TestOrder();
        order.Create(Guid.NewGuid(), "John Doe");
        order.AddItem("Widget", 2);

        // Act & Assert - should not throw
        order.ShouldHaveVersion(2);
    }

    [Fact]
    public void ShouldHaveVersion_WhenVersionDoesNotMatch_Throws()
    {
        // Arrange
        var order = new TestOrder();
        order.Create(Guid.NewGuid(), "John Doe");

        // Act & Assert
        Should.Throw<ShouldAssertException>(() =>
            order.ShouldHaveVersion(5));
    }

    #endregion

    #region ShouldHaveId Tests

    [Fact]
    public void ShouldHaveId_WhenIdMatches_Succeeds()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var order = new TestOrder();
        order.Create(orderId, "John Doe");

        // Act & Assert - should not throw
        order.ShouldHaveId(orderId);
    }

    [Fact]
    public void ShouldHaveId_WhenIdDoesNotMatch_Throws()
    {
        // Arrange
        var order = new TestOrder();
        order.Create(Guid.NewGuid(), "John Doe");

        // Act & Assert
        Should.Throw<ShouldAssertException>(() =>
            order.ShouldHaveId(Guid.Empty));
    }

    #endregion

    #region GetRaisedEvents / GetLastRaisedEvent Tests

    [Fact]
    public void GetRaisedEvents_ReturnsAllEventsOfType()
    {
        // Arrange
        var order = new TestOrder();
        order.Create(Guid.NewGuid(), "John Doe");
        order.AddItem("Widget", 2);
        order.AddItem("Gadget", 1);
        order.Ship();

        // Act
        var itemEvents = order.GetRaisedEvents<ItemAdded>();

        // Assert
        itemEvents.Count.ShouldBe(2);
    }

    [Fact]
    public void GetRaisedEvents_WhenNoEventsOfType_ReturnsEmpty()
    {
        // Arrange
        var order = new TestOrder();
        order.Create(Guid.NewGuid(), "John Doe");

        // Act
        var shippedEvents = order.GetRaisedEvents<OrderShipped>();

        // Assert
        shippedEvents.ShouldBeEmpty();
    }

    [Fact]
    public void GetLastRaisedEvent_ReturnsLastEventOfType()
    {
        // Arrange
        var order = new TestOrder();
        order.Create(Guid.NewGuid(), "John Doe");
        order.AddItem("Widget", 2);
        order.AddItem("Gadget", 1);

        // Act
        var lastItem = order.GetLastRaisedEvent<ItemAdded>();

        // Assert
        lastItem.ShouldNotBeNull();
        lastItem.ItemName.ShouldBe("Gadget");
    }

    [Fact]
    public void GetLastRaisedEvent_WhenNoEventsOfType_ReturnsNull()
    {
        // Arrange
        var order = new TestOrder();
        order.Create(Guid.NewGuid(), "John Doe");

        // Act
        var lastShipped = order.GetLastRaisedEvent<OrderShipped>();

        // Assert
        lastShipped.ShouldBeNull();
    }

    #endregion
}
