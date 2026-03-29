using Encina.Marten.Projections;
using Shouldly;

namespace Encina.UnitTests.Marten.Projections;

public class ProjectionRegistryTests
{
    // Register tests

    [Fact]
    public void Register_ValidProjection_AddsToRegistry()
    {
        // Arrange
        var registry = new ProjectionRegistry();

        // Act
        registry.Register<TestProjection, TestReadModel>();

        // Assert
        var all = registry.GetAllProjections();
        all.Count.ShouldBe(1);
        all[0].ProjectionType.ShouldBe(typeof(TestProjection));
        all[0].ReadModelType.ShouldBe(typeof(TestReadModel));
    }

    [Fact]
    public void Register_ByType_NullProjectionType_ThrowsArgumentNullException()
    {
        var registry = new ProjectionRegistry();
        var act = () => registry.Register(null!, typeof(TestReadModel));
        Should.Throw<ArgumentNullException>(act);
    }

    [Fact]
    public void Register_ByType_NullReadModelType_ThrowsArgumentNullException()
    {
        var registry = new ProjectionRegistry();
        var act = () => registry.Register(typeof(TestProjection), null!);
        Should.Throw<ArgumentNullException>(act);
    }

    [Fact]
    public void Register_ByType_ValidTypes_AddsToRegistry()
    {
        // Arrange
        var registry = new ProjectionRegistry();

        // Act
        registry.Register(typeof(TestProjection), typeof(TestReadModel));

        // Assert
        var all = registry.GetAllProjections();
        all.Count.ShouldBe(1);
    }

    // GetProjectionsForEvent tests

    [Fact]
    public void GetProjectionsForEvent_NullEventType_ThrowsArgumentNullException()
    {
        var registry = new ProjectionRegistry();
        var act = () => registry.GetProjectionsForEvent(null!);
        Should.Throw<ArgumentNullException>(act);
    }

    [Fact]
    public void GetProjectionsForEvent_RegisteredEvent_ReturnsProjections()
    {
        // Arrange
        var registry = new ProjectionRegistry();
        registry.Register<TestProjection, TestReadModel>();

        // Act
        var result = registry.GetProjectionsForEvent(typeof(TestCreatedEvent));

        // Assert
        result.Count.ShouldBe(1);
    }

    [Fact]
    public void GetProjectionsForEvent_UnregisteredEvent_ReturnsEmpty()
    {
        // Arrange
        var registry = new ProjectionRegistry();
        registry.Register<TestProjection, TestReadModel>();

        // Act
        var result = registry.GetProjectionsForEvent(typeof(string));

        // Assert
        result.Count.ShouldBe(0);
    }

    [Fact]
    public void GetProjectionsForEvent_MultipleProjections_ReturnsAll()
    {
        // Arrange
        var registry = new ProjectionRegistry();
        registry.Register<TestProjection, TestReadModel>();
        registry.Register<TestProjection2, TestReadModel2>();

        // Act - both projections handle TestCreatedEvent
        var result = registry.GetProjectionsForEvent(typeof(TestCreatedEvent));

        // Assert
        result.Count.ShouldBe(2);
    }

    // GetProjectionForReadModel tests

    [Fact]
    public void GetProjectionForReadModel_Generic_Registered_ReturnsRegistration()
    {
        // Arrange
        var registry = new ProjectionRegistry();
        registry.Register<TestProjection, TestReadModel>();

        // Act
        var result = registry.GetProjectionForReadModel<TestReadModel>();

        // Assert
        result.ShouldNotBeNull();
        result!.ProjectionType.ShouldBe(typeof(TestProjection));
    }

    [Fact]
    public void GetProjectionForReadModel_Generic_NotRegistered_ReturnsNull()
    {
        // Arrange
        var registry = new ProjectionRegistry();

        // Act
        var result = registry.GetProjectionForReadModel<TestReadModel>();

        // Assert
        result.ShouldBeNull();
    }

    [Fact]
    public void GetProjectionForReadModel_ByType_NullType_ThrowsArgumentNullException()
    {
        var registry = new ProjectionRegistry();
        var act = () => registry.GetProjectionForReadModel(null!);
        Should.Throw<ArgumentNullException>(act);
    }

    [Fact]
    public void GetProjectionForReadModel_ByType_Registered_ReturnsRegistration()
    {
        // Arrange
        var registry = new ProjectionRegistry();
        registry.Register<TestProjection, TestReadModel>();

        // Act
        var result = registry.GetProjectionForReadModel(typeof(TestReadModel));

        // Assert
        result.ShouldNotBeNull();
    }

    [Fact]
    public void GetProjectionForReadModel_ByType_NotRegistered_ReturnsNull()
    {
        // Arrange
        var registry = new ProjectionRegistry();

        // Act
        var result = registry.GetProjectionForReadModel(typeof(TestReadModel));

        // Assert
        result.ShouldBeNull();
    }

    // GetAllProjections tests

    [Fact]
    public void GetAllProjections_EmptyRegistry_ReturnsEmpty()
    {
        var registry = new ProjectionRegistry();
        registry.GetAllProjections().Count.ShouldBe(0);
    }

    [Fact]
    public void GetAllProjections_WithRegistrations_ReturnsAll()
    {
        // Arrange
        var registry = new ProjectionRegistry();
        registry.Register<TestProjection, TestReadModel>();
        registry.Register<TestProjection2, TestReadModel2>();

        // Act
        var all = registry.GetAllProjections();

        // Assert
        all.Count.ShouldBe(2);
    }

    // ProjectionRegistration tests

    [Fact]
    public void ProjectionRegistration_DiscoverHandlers_FindsCreatorHandlers()
    {
        // Arrange
        var registration = new ProjectionRegistration(typeof(TestProjection), typeof(TestReadModel));

        // Act
        var handlerInfo = registration.GetHandlerInfo(typeof(TestCreatedEvent));

        // Assert
        handlerInfo.ShouldNotBeNull();
        handlerInfo!.Value.HandlerType.ShouldBe(ProjectionHandlerType.Creator);
    }

    [Fact]
    public void ProjectionRegistration_DiscoverHandlers_FindsApplyHandlers()
    {
        // Arrange
        var registration = new ProjectionRegistration(typeof(TestProjection), typeof(TestReadModel));

        // Act
        var handlerInfo = registration.GetHandlerInfo(typeof(TestUpdatedEvent));

        // Assert
        handlerInfo.ShouldNotBeNull();
        handlerInfo!.Value.HandlerType.ShouldBe(ProjectionHandlerType.Handler);
    }

    [Fact]
    public void ProjectionRegistration_DiscoverHandlers_FindsDeleterHandlers()
    {
        // Arrange
        var registration = new ProjectionRegistration(typeof(TestProjection), typeof(TestReadModel));

        // Act
        var handlerInfo = registration.GetHandlerInfo(typeof(TestDeletedEvent));

        // Assert
        handlerInfo.ShouldNotBeNull();
        handlerInfo!.Value.HandlerType.ShouldBe(ProjectionHandlerType.Deleter);
    }

    [Fact]
    public void ProjectionRegistration_GetHandlerInfo_UnknownEvent_ReturnsNull()
    {
        // Arrange
        var registration = new ProjectionRegistration(typeof(TestProjection), typeof(TestReadModel));

        // Act
        var handlerInfo = registration.GetHandlerInfo(typeof(string));

        // Assert
        handlerInfo.ShouldBeNull();
    }

    [Fact]
    public void ProjectionRegistration_HandledEventTypes_ReturnsAllHandledEvents()
    {
        // Arrange
        var registration = new ProjectionRegistration(typeof(TestProjection), typeof(TestReadModel));

        // Act
        var handledTypes = registration.HandledEventTypes.ToList();

        // Assert
        handledTypes.ShouldContain(typeof(TestCreatedEvent));
        handledTypes.ShouldContain(typeof(TestUpdatedEvent));
        handledTypes.ShouldContain(typeof(TestDeletedEvent));
    }

    [Fact]
    public void ProjectionRegistration_ProjectionName_UsesTypeName()
    {
        // Arrange
        var registration = new ProjectionRegistration(typeof(TestProjection), typeof(TestReadModel));

        // Assert
        registration.ProjectionName.ShouldBe(nameof(TestProjection));
    }

    // ProjectionHandlerInfo tests

    [Fact]
    public void ProjectionHandlerInfo_InvokeCreate_ReturnsReadModel()
    {
        // Arrange
        var registration = new ProjectionRegistration(typeof(TestProjection), typeof(TestReadModel));
        var handlerInfo = registration.GetHandlerInfo(typeof(TestCreatedEvent))!.Value;
        var projection = new TestProjection();
        var context = new ProjectionContext { StreamId = Guid.NewGuid() };
        var domainEvent = new TestCreatedEvent("name");

        // Act
        var result = handlerInfo.InvokeCreate(projection, domainEvent, context);

        // Assert
        result.ShouldNotBeNull();
        result.ShouldBeOfType<TestReadModel>();
        ((TestReadModel)result).Name.ShouldBe("name");
    }

    [Fact]
    public void ProjectionHandlerInfo_InvokeApply_UpdatesReadModel()
    {
        // Arrange
        var registration = new ProjectionRegistration(typeof(TestProjection), typeof(TestReadModel));
        var handlerInfo = registration.GetHandlerInfo(typeof(TestUpdatedEvent))!.Value;
        var projection = new TestProjection();
        var context = new ProjectionContext { StreamId = Guid.NewGuid() };
        var existing = new TestReadModel { Id = Guid.NewGuid(), Name = "old" };
        var domainEvent = new TestUpdatedEvent("new");

        // Act
        var result = handlerInfo.InvokeApply(projection, domainEvent, existing, context);

        // Assert
        ((TestReadModel)result).Name.ShouldBe("new");
    }

    [Fact]
    public void ProjectionHandlerInfo_InvokeShouldDelete_ReturnsBoolean()
    {
        // Arrange
        var registration = new ProjectionRegistration(typeof(TestProjection), typeof(TestReadModel));
        var handlerInfo = registration.GetHandlerInfo(typeof(TestDeletedEvent))!.Value;
        var projection = new TestProjection();
        var context = new ProjectionContext { StreamId = Guid.NewGuid() };
        var existing = new TestReadModel { Id = Guid.NewGuid(), Name = "test" };

        // Act
        var shouldDelete = handlerInfo.InvokeShouldDelete(projection, new TestDeletedEvent(), existing, context);

        // Assert
        shouldDelete.ShouldBeTrue();
    }

    // Test types

    public sealed record TestCreatedEvent(string Name);
    public sealed record TestUpdatedEvent(string NewName);
    public sealed record TestDeletedEvent;

    public sealed class TestReadModel : IReadModel
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    public sealed class TestReadModel2 : IReadModel
    {
        public Guid Id { get; set; }
    }

    public sealed class TestProjection :
        IProjection<TestReadModel>,
        IProjectionCreator<TestCreatedEvent, TestReadModel>,
        IProjectionHandler<TestUpdatedEvent, TestReadModel>,
        IProjectionDeleter<TestDeletedEvent, TestReadModel>
    {
        public string ProjectionName => "TestProjection";

        public TestReadModel Create(TestCreatedEvent domainEvent, ProjectionContext context)
        {
            return new TestReadModel { Id = context.StreamId, Name = domainEvent.Name };
        }

        public TestReadModel Apply(TestUpdatedEvent domainEvent, TestReadModel current, ProjectionContext context)
        {
            current.Name = domainEvent.NewName;
            return current;
        }

        public bool ShouldDelete(TestDeletedEvent domainEvent, TestReadModel current, ProjectionContext context)
        {
            return true;
        }
    }

    public sealed class TestProjection2 :
        IProjection<TestReadModel2>,
        IProjectionCreator<TestCreatedEvent, TestReadModel2>
    {
        public string ProjectionName => "TestProjection2";

        public TestReadModel2 Create(TestCreatedEvent domainEvent, ProjectionContext context)
        {
            return new TestReadModel2 { Id = context.StreamId };
        }
    }
}
