using Encina.DomainModeling;

namespace Encina.DomainModeling.Tests;

public class AggregateRootTests
{
    private sealed class OrderId : GuidStronglyTypedId<OrderId>
    {
        public OrderId(Guid value) : base(value) { }
    }

    private sealed record OrderCreated(Guid OrderId, string CustomerName) : DomainEvent;
    private sealed record OrderLineAdded(Guid OrderId, string ProductName) : DomainEvent;

    private sealed class Order : AggregateRoot<OrderId>
    {
        public string CustomerName { get; private set; } = string.Empty;
        private readonly List<string> _lines = [];
        public IReadOnlyList<string> Lines => _lines.AsReadOnly();

        private Order() : base(default!) { }

        public static Order Create(OrderId id, string customerName)
        {
            var order = new Order { Id = id, CustomerName = customerName };
            order.RaiseDomainEvent(new OrderCreated(id.Value, customerName));
            return order;
        }

        public void AddLine(string productName)
        {
            _lines.Add(productName);
            RaiseDomainEvent(new OrderLineAdded(Id.Value, productName));
        }
    }

    private sealed class AuditableOrder : AuditableAggregateRoot<OrderId>
    {
        public string CustomerName { get; private set; } = string.Empty;

        public AuditableOrder(OrderId id, TimeProvider? timeProvider = null) : base(id, timeProvider) { }
    }

    private sealed class DeletableOrder : SoftDeletableAggregateRoot<OrderId>
    {
        public string CustomerName { get; private set; } = string.Empty;

        public DeletableOrder(OrderId id, TimeProvider? timeProvider = null) : base(id, timeProvider) { }
    }

    #region Basic AggregateRoot Tests

    [Fact]
    public void AggregateRoot_Create_ShouldRaiseDomainEvent()
    {
        // Arrange & Act
        var id = OrderId.New();
        var order = Order.Create(id, "John Doe");

        // Assert
        order.DomainEvents.Count.ShouldBe(1);
        order.DomainEvents[0].ShouldBeOfType<OrderCreated>();
        var @event = (OrderCreated)order.DomainEvents[0];
        @event.OrderId.ShouldBe(id.Value);
        @event.CustomerName.ShouldBe("John Doe");
    }

    [Fact]
    public void AggregateRoot_MultipleEvents_ShouldAccumulate()
    {
        // Arrange
        var order = Order.Create(OrderId.New(), "John Doe");

        // Act
        order.AddLine("Product 1");
        order.AddLine("Product 2");

        // Assert
        order.DomainEvents.Count.ShouldBe(3);
        order.DomainEvents[0].ShouldBeOfType<OrderCreated>();
        order.DomainEvents[1].ShouldBeOfType<OrderLineAdded>();
        order.DomainEvents[2].ShouldBeOfType<OrderLineAdded>();
    }

    [Fact]
    public void AggregateRoot_ClearDomainEvents_ShouldRemoveAllEvents()
    {
        // Arrange
        var order = Order.Create(OrderId.New(), "John Doe");
        order.AddLine("Product 1");
        // precondition: order has 2 domain events (OrderCreated + OrderLineAdded)

        // Act
        order.ClearDomainEvents();

        // Assert
        order.DomainEvents.ShouldBeEmpty();
    }

    [Fact]
    public void AggregateRoot_DomainEvents_ShouldBeReadOnly()
    {
        // Arrange
        var order = Order.Create(OrderId.New(), "John Doe");

        // Act & Assert
        order.DomainEvents.ShouldBeOfType<System.Collections.ObjectModel.ReadOnlyCollection<IDomainEvent>>();
    }

    [Fact]
    public void AggregateRoot_Id_ShouldBeSet()
    {
        // Arrange
        var id = OrderId.New();

        // Act
        var order = Order.Create(id, "John Doe");

        // Assert
        order.Id.ShouldBe(id);
    }

    #endregion

    #region AuditableAggregateRoot Tests

    [Fact]
    public void AuditableAggregateRoot_CreatedAtUtc_ShouldBeSet()
    {
        // Arrange
        var before = DateTime.UtcNow;

        // Act
        var order = new AuditableOrder(OrderId.New());
        var after = DateTime.UtcNow;

        // Assert
        order.CreatedAtUtc.ShouldBeGreaterThanOrEqualTo(before);
        order.CreatedAtUtc.ShouldBeLessThanOrEqualTo(after);
    }

    [Fact]
    public void AuditableAggregateRoot_SetCreatedBy_ShouldUpdateAuditInfo()
    {
        // Arrange
        var order = new AuditableOrder(OrderId.New());
        var before = DateTime.UtcNow.AddMilliseconds(-1); // Allow for timing variance

        // Act
        order.SetCreatedBy("user@example.com");
        var after = DateTime.UtcNow.AddMilliseconds(1); // Allow for timing variance

        // Assert
        order.CreatedBy.ShouldBe("user@example.com");
        order.CreatedAtUtc.ShouldBeGreaterThanOrEqualTo(before);
        order.CreatedAtUtc.ShouldBeLessThanOrEqualTo(after);
    }

    [Fact]
    public void AuditableAggregateRoot_SetModifiedBy_ShouldUpdateAuditInfo()
    {
        // Arrange
        var order = new AuditableOrder(OrderId.New());
        var before = DateTime.UtcNow;

        // Act
        order.SetModifiedBy("modifier@example.com");
        var after = DateTime.UtcNow;

        // Assert
        order.ModifiedBy.ShouldBe("modifier@example.com");
        order.ModifiedAtUtc.ShouldNotBeNull();
        order.ModifiedAtUtc!.Value.ShouldBeGreaterThanOrEqualTo(before);
        order.ModifiedAtUtc!.Value.ShouldBeLessThanOrEqualTo(after);
    }

    [Fact]
    public void AuditableAggregateRoot_InitialState_ShouldHaveNullModifiedInfo()
    {
        // Arrange & Act
        var order = new AuditableOrder(OrderId.New());

        // Assert
        order.CreatedBy.ShouldBeNull();
        order.ModifiedAtUtc.ShouldBeNull();
        order.ModifiedBy.ShouldBeNull();
    }

    #endregion

    #region SoftDeletableAggregateRoot Tests

    [Fact]
    public void SoftDeletableAggregateRoot_InitialState_ShouldNotBeDeleted()
    {
        // Arrange & Act
        var order = new DeletableOrder(OrderId.New());

        // Assert
        order.IsDeleted.ShouldBeFalse();
        order.DeletedAtUtc.ShouldBeNull();
        order.DeletedBy.ShouldBeNull();
    }

    [Fact]
    public void SoftDeletableAggregateRoot_Delete_ShouldMarkAsDeleted()
    {
        // Arrange
        var order = new DeletableOrder(OrderId.New());
        var before = DateTime.UtcNow;

        // Act
        order.Delete("admin@example.com");
        var after = DateTime.UtcNow;

        // Assert
        order.IsDeleted.ShouldBeTrue();
        order.DeletedBy.ShouldBe("admin@example.com");
        order.DeletedAtUtc.ShouldNotBeNull();
        order.DeletedAtUtc!.Value.ShouldBeGreaterThanOrEqualTo(before);
        order.DeletedAtUtc!.Value.ShouldBeLessThanOrEqualTo(after);
    }

    [Fact]
    public void SoftDeletableAggregateRoot_Delete_WithoutUser_ShouldStillMarkAsDeleted()
    {
        // Arrange
        var order = new DeletableOrder(OrderId.New());

        // Act
        order.Delete();

        // Assert
        order.IsDeleted.ShouldBeTrue();
        order.DeletedBy.ShouldBeNull();
        order.DeletedAtUtc.ShouldNotBeNull();
    }

    [Fact]
    public void SoftDeletableAggregateRoot_Restore_ShouldClearDeletedState()
    {
        // Arrange
        var order = new DeletableOrder(OrderId.New());
        order.Delete("admin@example.com");
        // precondition: order marked deleted

        // Act
        order.Restore();

        // Assert
        order.IsDeleted.ShouldBeFalse();
        order.DeletedAtUtc.ShouldBeNull();
        order.DeletedBy.ShouldBeNull();
    }

    #endregion
}
