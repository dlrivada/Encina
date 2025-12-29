using Encina.DomainModeling;
using FsCheck;
using FsCheck.Xunit;

namespace Encina.DomainModeling.PropertyTests;

/// <summary>
/// Property-based tests for AggregateRoot domain event invariants.
/// </summary>
public sealed class AggregateRootProperties
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

        private Order() : base(default!) { }

        public static Order Create(OrderId id, string customerName)
        {
            var order = new Order { Id = id, CustomerName = customerName };
            order.RaiseDomainEvent(new OrderCreated(id.Value, customerName));
            return order;
        }

        public void AddLine(string productName)
        {
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

    #region Domain Event Properties

    [Property(MaxTest = 200)]
    public bool AggregateRoot_CreateRaisesOneEvent(Guid id, NonEmptyString customerName)
    {
        var order = Order.Create(OrderId.From(id), customerName.Get);
        return order.DomainEvents.Count == 1;
    }

    [Property(MaxTest = 200)]
    public bool AggregateRoot_EventsAccumulate(Guid id, NonEmptyString customerName, List<NonEmptyString> lines)
    {
        lines ??= [];
        var order = Order.Create(OrderId.From(id), customerName.Get);

        foreach (var line in lines)
        {
            order.AddLine(line.Get);
        }

        var expectedCount = 1 + lines.Count;
        return order.DomainEvents.Count == expectedCount;
    }

    [Property(MaxTest = 200)]
    public bool AggregateRoot_ClearRemovesAllEvents(Guid id, NonEmptyString customerName, List<NonEmptyString> lines)
    {
        lines ??= [];
        var order = Order.Create(OrderId.From(id), customerName.Get);

        foreach (var line in lines)
        {
            order.AddLine(line.Get);
        }

        order.ClearDomainEvents();

        return order.DomainEvents.Count == 0;
    }

    [Property(MaxTest = 200)]
    public bool AggregateRoot_EventsPreserveOrder(Guid id, NonEmptyString customerName, List<NonEmptyString> lines)
    {
        lines ??= [];
        var order = Order.Create(OrderId.From(id), customerName.Get);

        foreach (var line in lines)
        {
            order.AddLine(line.Get);
        }

        var events = order.DomainEvents;

        if (events.Count == 0) return true;
        if (events[0] is not OrderCreated) return false;

        for (var i = 1; i < events.Count; i++)
        {
            if (events[i] is not OrderLineAdded) return false;
        }

        return true;
    }

    [Property(MaxTest = 100)]
    public bool AggregateRoot_Id_IsPreserved(Guid id, NonEmptyString customerName)
    {
        var orderId = OrderId.From(id);
        var order = Order.Create(orderId, customerName.Get);

        return order.Id.Value == id;
    }

    #endregion

    #region Auditable Properties

    [Property(MaxTest = 100)]
    public bool AuditableAggregateRoot_CreatedAtUtc_IsReasonable(Guid id)
    {
        var before = DateTime.UtcNow;
        var order = new AuditableOrder(OrderId.From(id));
        var after = DateTime.UtcNow;

        return order.CreatedAtUtc >= before && order.CreatedAtUtc <= after;
    }

    [Property(MaxTest = 100)]
    public bool AuditableAggregateRoot_SetCreatedBy_UpdatesField(Guid id, NonEmptyString createdBy)
    {
        var order = new AuditableOrder(OrderId.From(id));
        order.SetCreatedBy(createdBy.Get);

        return order.CreatedBy == createdBy.Get;
    }

    [Property(MaxTest = 100)]
    public bool AuditableAggregateRoot_SetModifiedBy_UpdatesFields(Guid id, NonEmptyString modifiedBy)
    {
        var order = new AuditableOrder(OrderId.From(id));
        var before = DateTime.UtcNow;
        order.SetModifiedBy(modifiedBy.Get);
        var after = DateTime.UtcNow;

        return order.ModifiedBy == modifiedBy.Get
            && order.ModifiedAtUtc.HasValue
            && order.ModifiedAtUtc.Value >= before
            && order.ModifiedAtUtc.Value <= after;
    }

    [Property(MaxTest = 100)]
    public bool AuditableAggregateRoot_InitialState_HasNullModifiedInfo(Guid id)
    {
        var order = new AuditableOrder(OrderId.From(id));

        return order.ModifiedAtUtc is null
            && order.ModifiedBy is null;
    }

    #endregion

    #region SoftDeletable Properties

    [Property(MaxTest = 100)]
    public bool SoftDeletableAggregateRoot_InitialState_IsNotDeleted(Guid id)
    {
        var order = new DeletableOrder(OrderId.From(id));

        return !order.IsDeleted
            && order.DeletedAtUtc is null
            && order.DeletedBy is null;
    }

    [Property(MaxTest = 100)]
    public bool SoftDeletableAggregateRoot_Delete_SetsAllFields(Guid id, NonEmptyString deletedBy)
    {
        var order = new DeletableOrder(OrderId.From(id));
        var before = DateTime.UtcNow;
        order.Delete(deletedBy.Get);
        var after = DateTime.UtcNow;

        return order.IsDeleted
            && order.DeletedBy == deletedBy.Get
            && order.DeletedAtUtc.HasValue
            && order.DeletedAtUtc.Value >= before
            && order.DeletedAtUtc.Value <= after;
    }

    [Property(MaxTest = 100)]
    public bool SoftDeletableAggregateRoot_Delete_WithoutUser_StillSetsDeleted(Guid id)
    {
        var order = new DeletableOrder(OrderId.From(id));
        order.Delete();

        return order.IsDeleted
            && order.DeletedBy is null
            && order.DeletedAtUtc.HasValue;
    }

    [Property(MaxTest = 100)]
    public bool SoftDeletableAggregateRoot_Restore_ClearsAllFields(Guid id, NonEmptyString deletedBy)
    {
        var order = new DeletableOrder(OrderId.From(id));
        order.Delete(deletedBy.Get);
        order.Restore();

        return !order.IsDeleted
            && order.DeletedAtUtc is null
            && order.DeletedBy is null;
    }

    [Property(MaxTest = 100)]
    public bool SoftDeletableAggregateRoot_DeleteRestore_IsIdempotent(Guid id)
    {
        var order = new DeletableOrder(OrderId.From(id));

        order.Delete();
        order.Restore();
        order.Delete();
        order.Restore();

        return !order.IsDeleted
            && order.DeletedAtUtc is null
            && order.DeletedBy is null;
    }

    #endregion
}
