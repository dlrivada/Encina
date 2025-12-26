using Encina.Marten.Projections;
using FsCheck;
using FsCheck.Xunit;

namespace Encina.Marten.Tests.Projections;

/// <summary>
/// Property-based tests for projection system that verify invariants hold for all inputs.
/// </summary>
public sealed class ProjectionPropertyTests
{
    #region ProjectionContext Invariants

    [Property(MaxTest = 100)]
    public bool ProjectionContext_PreservesStreamId(Guid streamId)
    {
        var context = new ProjectionContext { StreamId = streamId };
        return context.StreamId == streamId;
    }

    [Property(MaxTest = 100)]
    public bool ProjectionContext_PreservesSequenceNumber(PositiveInt sequenceNumber)
    {
        var context = new ProjectionContext { SequenceNumber = sequenceNumber.Get };
        return context.SequenceNumber == sequenceNumber.Get;
    }

    [Property(MaxTest = 100)]
    public bool ProjectionContext_PreservesGlobalPosition(PositiveInt globalPosition)
    {
        var context = new ProjectionContext { GlobalPosition = globalPosition.Get };
        return context.GlobalPosition == globalPosition.Get;
    }

    [Property(MaxTest = 100)]
    public bool ProjectionContext_ConstructorPreservesStreamId(Guid streamId)
    {
        var context = new ProjectionContext(streamId, 0, 0, DateTime.UtcNow);
        return context.StreamId == streamId;
    }

    [Property(MaxTest = 100)]
    public bool ProjectionContext_ConstructorPreservesSequenceNumber(PositiveInt seqNum)
    {
        var context = new ProjectionContext(Guid.NewGuid(), seqNum.Get, 0, DateTime.UtcNow);
        return context.SequenceNumber == seqNum.Get;
    }

    #endregion

    #region ProjectionRegistry Invariants

    [Property(MaxTest = 100)]
    public bool ProjectionRegistry_RegisteredEventTypesAreRetrievable(PositiveInt _)
    {
        var registry = new ProjectionRegistry();
        registry.Register<OrderSummaryProjection, OrderSummary>();

        var registrations = registry.GetProjectionsForEvent(typeof(OrderCreated));
        return registrations.Count == 1
            && registrations[0].ProjectionType == typeof(OrderSummaryProjection);
    }

    [Property(MaxTest = 100)]
    public bool ProjectionRegistry_GetAllProjectionsReturnsRegistered(PositiveInt _)
    {
        var registry = new ProjectionRegistry();
        registry.Register<OrderSummaryProjection, OrderSummary>();

        var all = registry.GetAllProjections();
        return all.Count == 1;
    }

    [Property(MaxTest = 100)]
    public bool ProjectionRegistry_UnregisteredEventReturnsEmpty(PositiveInt _)
    {
        var registry = new ProjectionRegistry();

        var registrations = registry.GetProjectionsForEvent(typeof(OrderCreated));
        return registrations.Count == 0;
    }

    #endregion

    #region ProjectionStatus Invariants

    [Property(MaxTest = 100)]
    public bool ProjectionStatus_LastProcessedPositionPreserved(PositiveInt position)
    {
        var status = new ProjectionStatus { LastProcessedPosition = position.Get };
        return status.LastProcessedPosition == position.Get;
    }

    [Property(MaxTest = 100)]
    public bool ProjectionStatus_EventsProcessedPreserved(PositiveInt events)
    {
        var status = new ProjectionStatus { EventsProcessed = events.Get };
        return status.EventsProcessed == events.Get;
    }

    [Property(MaxTest = 100)]
    public bool ProjectionStatus_DefaultStateIsStopped(PositiveInt _)
    {
        var status = new ProjectionStatus();
        return status.State == ProjectionState.Stopped;
    }

    #endregion

    #region RebuildOptions Invariants

    [Property(MaxTest = 100)]
    public bool RebuildOptions_BatchSizeDefaultIsPositive(PositiveInt _)
    {
        var options = new RebuildOptions();
        return options.BatchSize > 0;
    }

    [Property(MaxTest = 100)]
    public bool RebuildOptions_StartPositionDefaultIsZero(PositiveInt _)
    {
        var options = new RebuildOptions();
        return options.StartPosition == 0;
    }

    [Property(MaxTest = 100)]
    public bool RebuildOptions_DeleteExistingDefaultIsTrue(PositiveInt _)
    {
        var options = new RebuildOptions();
        return options.DeleteExisting;
    }

    [Property(MaxTest = 100)]
    public bool RebuildOptions_RunInBackgroundDefaultIsFalse(PositiveInt _)
    {
        var options = new RebuildOptions();
        return !options.RunInBackground;
    }

    [Property(MaxTest = 100)]
    public bool RebuildOptions_BatchSizeCanBeSet(PositiveInt batchSize)
    {
        var actualBatchSize = Math.Max(1, batchSize.Get % 10001); // Keep reasonable size
        var options = new RebuildOptions { BatchSize = actualBatchSize };
        return options.BatchSize == actualBatchSize;
    }

    #endregion

    #region ReadModel Invariants

    [Property(MaxTest = 100)]
    public bool ReadModel_IdIsPreserved(Guid id)
    {
        var readModel = new OrderSummary { Id = id };
        return readModel.Id == id;
    }

    [Property(MaxTest = 100)]
    public bool ReadModel_TotalAmountPreserved(decimal amount)
    {
        var readModel = new OrderSummary { TotalAmount = amount };
        return readModel.TotalAmount == amount;
    }

    #endregion

    #region ProjectionHandler Invariants

    [Property(MaxTest = 100)]
    public bool ProjectionHandler_CreateSetsId(Guid streamId, NonEmptyString customerName)
    {
        var projection = new OrderSummaryProjection();
        var createEvent = new OrderCreated(customerName.Get);
        var context = new ProjectionContext { StreamId = streamId };

        var result = projection.Create(createEvent, context);

        return result.Id == streamId;
    }

    [Property(MaxTest = 100)]
    public bool ProjectionHandler_ApplyAddsItemCount(PositiveInt quantity)
    {
        var projection = new OrderSummaryProjection();
        var addItemEvent = new OrderItemAdded("Product", 10m, quantity.Get);
        var context = new ProjectionContext();
        var existing = new OrderSummary
        {
            Id = Guid.NewGuid(),
            TotalAmount = 0m,
            ItemCount = 0,
        };

        var result = projection.Apply(addItemEvent, existing, context);

        return result.ItemCount == quantity.Get;
    }

    [Property(MaxTest = 100)]
    public bool ProjectionHandler_ApplyCalculatesTotal(PositiveInt quantity)
    {
        var price = 10m;
        var projection = new OrderSummaryProjection();
        var addItemEvent = new OrderItemAdded("Product", price, quantity.Get);
        var context = new ProjectionContext();
        var existing = new OrderSummary
        {
            Id = Guid.NewGuid(),
            TotalAmount = 0m,
            ItemCount = 0,
        };

        var result = projection.Apply(addItemEvent, existing, context);

        return result.TotalAmount == price * quantity.Get;
    }

    [Property(MaxTest = 100)]
    public bool ProjectionDeleter_CancelledOrderShouldDelete(NonEmptyString reason)
    {
        var projection = new OrderSummaryProjection();
        var cancelEvent = new OrderCancelled(reason.Get);
        var context = new ProjectionContext();
        var existing = new OrderSummary { Id = Guid.NewGuid() };

        return projection.ShouldDelete(cancelEvent, existing, context);
    }

    #endregion
}
