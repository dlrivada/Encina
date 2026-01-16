using Encina.DomainModeling;
using Encina.Testing.Time;

namespace Encina.GuardTests.DomainModeling;

/// <summary>
/// Tests for AggregateRoot types including guards and behavior.
/// </summary>
public class AggregateRootTests
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
        public AuditableOrder(OrderId id, TimeProvider? timeProvider = null) : base(id, timeProvider) { }
    }

    private sealed class DeletableOrder : SoftDeletableAggregateRoot<OrderId>
    {
        public DeletableOrder(OrderId id, TimeProvider? timeProvider = null) : base(id, timeProvider) { }
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
        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("domainEvent");
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
        order.DomainEvents.ShouldNotBeNull();
    }

    /// <summary>
    /// Verifies that DomainEvents is empty after ClearDomainEvents.
    /// </summary>
    [Fact]
    public void ClearDomainEvents_AfterClear_EventsIsEmpty()
    {
        // Arrange
        var order = Order.Create(OrderId.New());
        // Precondition: newly created order contains domain events

        // Act
        order.ClearDomainEvents();

        // Assert
        order.DomainEvents.ShouldBeEmpty();
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
        order.CreatedBy.ShouldBe("user@example.com");
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
        order.ModifiedBy.ShouldBe("modifier@example.com");
        order.ModifiedAtUtc.ShouldNotBeNull();
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
        order.CreatedAtUtc.ShouldBeGreaterThanOrEqualTo(before);
        order.CreatedAtUtc.ShouldBeLessThanOrEqualTo(after);
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

        // Act
        order.Delete();

        // Assert
        order.IsDeleted.ShouldBeTrue();
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
        order.IsDeleted.ShouldBeTrue();
        order.DeletedBy.ShouldBeNull();
    }

    /// <summary>
    /// Verifies that Delete sets DeletedAtUtc.
    /// </summary>
    [Fact]
    public void Delete_SetsDeletedAtUtc()
    {
        // Arrange
        var fixedTime = new DateTimeOffset(2025, 12, 31, 10, 30, 0, TimeSpan.Zero);
        var fakeTimeProvider = new FakeTimeProvider(fixedTime);
        var order = new DeletableOrder(OrderId.New(), fakeTimeProvider);

        // Act
        order.Delete("admin");

        // Assert
        order.DeletedAtUtc.ShouldNotBeNull();
        order.DeletedAtUtc!.Value.ShouldBe(fixedTime.UtcDateTime);
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

        // Act
        order.Restore();

        // Assert
        order.IsDeleted.ShouldBeFalse();
        order.DeletedAtUtc.ShouldBeNull();
        order.DeletedBy.ShouldBeNull();
    }

    #endregion
}
