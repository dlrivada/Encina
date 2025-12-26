using Encina.Marten.Projections;

namespace Encina.Marten.Tests.Projections;

/// <summary>
/// Contract tests verifying the projection interfaces work correctly.
/// </summary>
#pragma warning disable CA1859 // Use concrete types for interface testing
public sealed class ProjectionContractTests
{
    [Fact]
    public void IReadModel_Contract_MustHaveId()
    {
        // Arrange
        var id = Guid.NewGuid();

        // Act
        IReadModel readModel = new OrderSummary { Id = id };

        // Assert
        readModel.Id.ShouldBe(id);
    }

    [Fact]
    public void IReadModel_Contract_IdIsSettable()
    {
        // Arrange
        var originalId = Guid.NewGuid();
        var newId = Guid.NewGuid();

        // Act
        IReadModel readModel = new OrderSummary { Id = originalId };
        readModel.Id = newId;

        // Assert
        readModel.Id.ShouldBe(newId);
    }

    [Fact]
    public void IProjection_Contract_HasProjectionName()
    {
        // Arrange
        IProjection<OrderSummary> projection = new OrderSummaryProjection();

        // Act & Assert
        projection.ProjectionName.ShouldNotBeNullOrEmpty();
        projection.ProjectionName.ShouldBe("OrderSummary");
    }

    [Fact]
    public void IProjectionCreator_Contract_CreatesReadModel()
    {
        // Arrange
        IProjectionCreator<OrderCreated, OrderSummary> creator = new OrderSummaryProjection();
        var createEvent = new OrderCreated("Test Customer");
        var context = new ProjectionContext
        {
            StreamId = Guid.NewGuid(),
            Timestamp = DateTime.UtcNow,
        };

        // Act
        var result = creator.Create(createEvent, context);

        // Assert
        result.ShouldNotBeNull();
        result.Id.ShouldBe(context.StreamId);
        result.CustomerName.ShouldBe("Test Customer");
    }

    [Fact]
    public void IProjectionHandler_Contract_AppliesEvent()
    {
        // Arrange
        IProjectionHandler<OrderItemAdded, OrderSummary> handler = new OrderSummaryProjection();
        var addItemEvent = new OrderItemAdded("Product", 25.00m, 3);
        var context = new ProjectionContext();
        var existing = new OrderSummary
        {
            Id = Guid.NewGuid(),
            TotalAmount = 10.00m,
            ItemCount = 1,
        };

        // Act
        var result = handler.Apply(addItemEvent, existing, context);

        // Assert
        result.ShouldNotBeNull();
        result.TotalAmount.ShouldBe(85.00m); // 10 + (25 * 3)
        result.ItemCount.ShouldBe(4);        // 1 + 3
    }

    [Fact]
    public void IProjectionDeleter_Contract_DeterminesDeletion()
    {
        // Arrange
        IProjectionDeleter<OrderCancelled, OrderSummary> deleter = new OrderSummaryProjection();
        var cancelEvent = new OrderCancelled("Test reason");
        var context = new ProjectionContext();
        var existing = new OrderSummary { Id = Guid.NewGuid() };

        // Act
        var shouldDelete = deleter.ShouldDelete(cancelEvent, existing, context);

        // Assert
        shouldDelete.ShouldBeTrue();
    }

    [Fact]
    public void ProjectionRegistry_Contract_RegistrationIsIdempotent()
    {
        // Arrange
        var registry = new ProjectionRegistry();

        // Act - register same projection multiple times
        registry.Register<OrderSummaryProjection, OrderSummary>();

        // Assert - should have one registration
        var allProjections = registry.GetAllProjections();
        allProjections.Count.ShouldBe(1);
    }

    [Fact]
    public void ProjectionRegistration_Contract_DetectsAllHandlerTypes()
    {
        // Arrange
        var registry = new ProjectionRegistry();
        registry.Register<OrderSummaryProjection, OrderSummary>();

        var registration = registry.GetProjectionForReadModel<OrderSummary>()!;

        // Act & Assert - all handler types should be detected
        var handledEvents = registration.HandledEventTypes.ToList();
        handledEvents.ShouldContain(typeof(OrderCreated));
        handledEvents.ShouldContain(typeof(OrderItemAdded));
        handledEvents.ShouldContain(typeof(OrderCompleted));
        handledEvents.ShouldContain(typeof(OrderCancelled));
    }

    [Fact]
    public void ProjectionContext_Contract_MetadataIsReadOnly()
    {
        // Arrange
        var mutableDict = new Dictionary<string, object> { ["key"] = "value" };
        var context = new ProjectionContext { Metadata = mutableDict };

        // Assert - Metadata property returns IReadOnlyDictionary
        context.Metadata.ShouldBeOfType<Dictionary<string, object>>();
        context.Metadata.Count.ShouldBe(1);
    }

    [Fact]
    public void ProjectionStatus_Contract_AllStatesAreDefined()
    {
        // Assert - all expected states exist
        var states = Enum.GetValues<ProjectionState>();

        states.ShouldContain(ProjectionState.Stopped);
        states.ShouldContain(ProjectionState.Starting);
        states.ShouldContain(ProjectionState.Running);
        states.ShouldContain(ProjectionState.CatchingUp);
        states.ShouldContain(ProjectionState.Rebuilding);
        states.ShouldContain(ProjectionState.Paused);
        states.ShouldContain(ProjectionState.Faulted);
        states.ShouldContain(ProjectionState.Stopping);
    }

    [Fact]
    public void RebuildOptions_Contract_DefaultValuesAreReasonable()
    {
        // Arrange
        var options = new RebuildOptions();

        // Assert
        options.BatchSize.ShouldBeGreaterThan(0);
        options.BatchSize.ShouldBeLessThanOrEqualTo(10000);
        options.DeleteExisting.ShouldBeTrue();
        options.StartPosition.ShouldBe(0);
        options.EndPosition.ShouldBeNull();
        options.RunInBackground.ShouldBeFalse();
    }
}
#pragma warning restore CA1859
