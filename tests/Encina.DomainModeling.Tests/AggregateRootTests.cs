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

        public AuditableOrder(OrderId id) : base(id) { }
    }

    private sealed class DeletableOrder : SoftDeletableAggregateRoot<OrderId>
    {
        public string CustomerName { get; private set; } = string.Empty;

        public DeletableOrder(OrderId id) : base(id) { }
    }

    #region Basic AggregateRoot Tests

    [Fact]
    public void AggregateRoot_Create_ShouldRaiseDomainEvent()
    {
        // Arrange & Act
        var id = OrderId.New();
        var order = Order.Create(id, "John Doe");

        // Assert
        order.DomainEvents.Should().HaveCount(1);
        order.DomainEvents[0].Should().BeOfType<OrderCreated>();
        var @event = (OrderCreated)order.DomainEvents[0];
        @event.OrderId.Should().Be(id.Value);
        @event.CustomerName.Should().Be("John Doe");
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
        order.DomainEvents.Should().HaveCount(3);
        order.DomainEvents[0].Should().BeOfType<OrderCreated>();
        order.DomainEvents[1].Should().BeOfType<OrderLineAdded>();
        order.DomainEvents[2].Should().BeOfType<OrderLineAdded>();
    }

    [Fact]
    public void AggregateRoot_ClearDomainEvents_ShouldRemoveAllEvents()
    {
        // Arrange
        var order = Order.Create(OrderId.New(), "John Doe");
        order.AddLine("Product 1");
        order.DomainEvents.Should().HaveCount(2);

        // Act
        order.ClearDomainEvents();

        // Assert
        order.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void AggregateRoot_DomainEvents_ShouldBeReadOnly()
    {
        // Arrange
        var order = Order.Create(OrderId.New(), "John Doe");

        // Act & Assert
        order.DomainEvents.Should().BeOfType<System.Collections.ObjectModel.ReadOnlyCollection<IDomainEvent>>();
    }

    [Fact]
    public void AggregateRoot_Id_ShouldBeSet()
    {
        // Arrange
        var id = OrderId.New();

        // Act
        var order = Order.Create(id, "John Doe");

        // Assert
        order.Id.Should().Be(id);
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
        order.CreatedAtUtc.Should().BeOnOrAfter(before);
        order.CreatedAtUtc.Should().BeOnOrBefore(after);
    }

    [Fact]
    public void AuditableAggregateRoot_SetCreatedBy_ShouldUpdateAuditInfo()
    {
        // Arrange
        var order = new AuditableOrder(OrderId.New());
        var before = DateTime.UtcNow;

        // Act
        order.SetCreatedBy("user@example.com");
        var after = DateTime.UtcNow;

        // Assert
        order.CreatedBy.Should().Be("user@example.com");
        order.CreatedAtUtc.Should().BeOnOrAfter(before);
        order.CreatedAtUtc.Should().BeOnOrBefore(after);
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
        order.ModifiedBy.Should().Be("modifier@example.com");
        order.ModifiedAtUtc.Should().NotBeNull();
        order.ModifiedAtUtc!.Value.Should().BeOnOrAfter(before);
        order.ModifiedAtUtc!.Value.Should().BeOnOrBefore(after);
    }

    [Fact]
    public void AuditableAggregateRoot_InitialState_ShouldHaveNullModifiedInfo()
    {
        // Arrange & Act
        var order = new AuditableOrder(OrderId.New());

        // Assert
        order.CreatedBy.Should().BeNull();
        order.ModifiedAtUtc.Should().BeNull();
        order.ModifiedBy.Should().BeNull();
    }

    #endregion

    #region SoftDeletableAggregateRoot Tests

    [Fact]
    public void SoftDeletableAggregateRoot_InitialState_ShouldNotBeDeleted()
    {
        // Arrange & Act
        var order = new DeletableOrder(OrderId.New());

        // Assert
        order.IsDeleted.Should().BeFalse();
        order.DeletedAtUtc.Should().BeNull();
        order.DeletedBy.Should().BeNull();
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
        order.IsDeleted.Should().BeTrue();
        order.DeletedBy.Should().Be("admin@example.com");
        order.DeletedAtUtc.Should().NotBeNull();
        order.DeletedAtUtc!.Value.Should().BeOnOrAfter(before);
        order.DeletedAtUtc!.Value.Should().BeOnOrBefore(after);
    }

    [Fact]
    public void SoftDeletableAggregateRoot_Delete_WithoutUser_ShouldStillMarkAsDeleted()
    {
        // Arrange
        var order = new DeletableOrder(OrderId.New());

        // Act
        order.Delete();

        // Assert
        order.IsDeleted.Should().BeTrue();
        order.DeletedBy.Should().BeNull();
        order.DeletedAtUtc.Should().NotBeNull();
    }

    [Fact]
    public void SoftDeletableAggregateRoot_Restore_ShouldClearDeletedState()
    {
        // Arrange
        var order = new DeletableOrder(OrderId.New());
        order.Delete("admin@example.com");
        order.IsDeleted.Should().BeTrue();

        // Act
        order.Restore();

        // Assert
        order.IsDeleted.Should().BeFalse();
        order.DeletedAtUtc.Should().BeNull();
        order.DeletedBy.Should().BeNull();
    }

    #endregion
}
