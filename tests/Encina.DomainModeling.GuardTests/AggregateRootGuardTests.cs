namespace Encina.DomainModeling.GuardTests;

/// <summary>
/// Guard tests for AggregateRoot types to verify null parameter handling.
/// </summary>
public class AggregateRootGuardTests
{
    private sealed class OrderId : GuidStronglyTypedId<OrderId>
    {
        public OrderId(Guid value) : base(value) { }
    }

    private sealed record OrderCreated(Guid OrderId) : DomainEvent;

    private sealed class Order : AggregateRoot<OrderId>
    {
        private Order() : base(default!) { }

        public static Order Create(OrderId id)
        {
            var order = new Order { Id = id };
            order.RaiseDomainEvent(new OrderCreated(id.Value));
            return order;
        }

        public void RaiseEvent(IDomainEvent domainEvent)
        {
            RaiseDomainEvent(domainEvent);
        }
    }

    private sealed class AuditableOrder : AuditableAggregateRoot<OrderId>
    {
        public AuditableOrder(OrderId id) : base(id) { }
    }

    private sealed class DeletableOrder : SoftDeletableAggregateRoot<OrderId>
    {
        public DeletableOrder(OrderId id) : base(id) { }
    }

    #region AggregateRoot Guards

    /// <summary>
    /// Verifies that RaiseDomainEvent throws on null event.
    /// </summary>
    [Fact]
    public void RaiseDomainEvent_NullEvent_ThrowsArgumentNullException()
    {
        // Arrange
        var order = Order.Create(OrderId.New());
        IDomainEvent nullEvent = null!;

        // Act
        var act = () => order.RaiseEvent(nullEvent);

        // Assert
        act.Should().Throw<ArgumentNullException>().WithParameterName("domainEvent");
    }

    /// <summary>
    /// Verifies that DomainEvents is never null.
    /// </summary>
    [Fact]
    public void DomainEvents_AfterCreation_IsNotNull()
    {
        // Arrange
        var order = Order.Create(OrderId.New());

        // Assert
        order.DomainEvents.Should().NotBeNull();
    }

    /// <summary>
    /// Verifies that DomainEvents is empty after ClearDomainEvents.
    /// </summary>
    [Fact]
    public void ClearDomainEvents_AfterClear_EventsIsEmpty()
    {
        // Arrange
        var order = Order.Create(OrderId.New());
        order.DomainEvents.Should().NotBeEmpty();

        // Act
        order.ClearDomainEvents();

        // Assert
        order.DomainEvents.Should().BeEmpty();
    }

    #endregion

    #region AuditableAggregateRoot Guards

    /// <summary>
    /// Verifies that SetCreatedBy accepts the value (no null check per design).
    /// </summary>
    [Fact]
    public void SetCreatedBy_WithValue_SetsCreatedBy()
    {
        // Arrange
        var order = new AuditableOrder(OrderId.New());

        // Act
        order.SetCreatedBy("user@example.com");

        // Assert
        order.CreatedBy.Should().Be("user@example.com");
    }

    /// <summary>
    /// Verifies that SetModifiedBy accepts the value.
    /// </summary>
    [Fact]
    public void SetModifiedBy_WithValue_SetsModifiedBy()
    {
        // Arrange
        var order = new AuditableOrder(OrderId.New());

        // Act
        order.SetModifiedBy("modifier@example.com");

        // Assert
        order.ModifiedBy.Should().Be("modifier@example.com");
        order.ModifiedAtUtc.Should().NotBeNull();
    }

    /// <summary>
    /// Verifies that CreatedAtUtc is set during construction.
    /// </summary>
    [Fact]
    public void Constructor_SetsCreatedAtUtc()
    {
        // Arrange & Act
        var before = DateTime.UtcNow;
        var order = new AuditableOrder(OrderId.New());
        var after = DateTime.UtcNow;

        // Assert
        order.CreatedAtUtc.Should().BeOnOrAfter(before);
        order.CreatedAtUtc.Should().BeOnOrBefore(after);
    }

    #endregion

    #region SoftDeletableAggregateRoot Guards

    /// <summary>
    /// Verifies that Delete sets IsDeleted to true.
    /// </summary>
    [Fact]
    public void Delete_SetsIsDeleted()
    {
        // Arrange
        var order = new DeletableOrder(OrderId.New());
        order.IsDeleted.Should().BeFalse();

        // Act
        order.Delete();

        // Assert
        order.IsDeleted.Should().BeTrue();
    }

    /// <summary>
    /// Verifies that Delete accepts null deletedBy.
    /// </summary>
    [Fact]
    public void Delete_NullDeletedBy_SetsDeletedByToNull()
    {
        // Arrange
        var order = new DeletableOrder(OrderId.New());

        // Act
        order.Delete(null);

        // Assert
        order.IsDeleted.Should().BeTrue();
        order.DeletedBy.Should().BeNull();
    }

    /// <summary>
    /// Verifies that Delete sets DeletedAtUtc.
    /// </summary>
    [Fact]
    public void Delete_SetsDeletedAtUtc()
    {
        // Arrange
        var order = new DeletableOrder(OrderId.New());
        var before = DateTime.UtcNow;

        // Act
        order.Delete("admin");
        var after = DateTime.UtcNow;

        // Assert
        order.DeletedAtUtc.Should().NotBeNull();
        order.DeletedAtUtc!.Value.Should().BeOnOrAfter(before);
        order.DeletedAtUtc!.Value.Should().BeOnOrBefore(after);
    }

    /// <summary>
    /// Verifies that Restore clears all deleted fields.
    /// </summary>
    [Fact]
    public void Restore_ClearsDeletedFields()
    {
        // Arrange
        var order = new DeletableOrder(OrderId.New());
        order.Delete("admin");
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
