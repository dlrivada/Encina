using Encina.Marten.Projections;

namespace Encina.ContractTests.Marten.Core;

/// <summary>
/// Behavioral contract tests for <see cref="ProjectionRegistry"/> verifying
/// registration, lookup, and handler discovery work correctly.
/// </summary>
[Trait("Category", "Contract")]
[Trait("Provider", "Marten")]
public sealed class ProjectionRegistryContractTests
{
    #region Registration Contract

    [Fact]
    public void Register_GenericOverload_RegistersProjection()
    {
        var registry = new ProjectionRegistry();
        registry.Register<TestProjection, TestReadModel>();

        var all = registry.GetAllProjections();
        all.Count.ShouldBe(1);
        all[0].ProjectionType.ShouldBe(typeof(TestProjection));
        all[0].ReadModelType.ShouldBe(typeof(TestReadModel));
    }

    [Fact]
    public void Register_TypeOverload_RegistersProjection()
    {
        var registry = new ProjectionRegistry();
        registry.Register(typeof(TestProjection), typeof(TestReadModel));

        var all = registry.GetAllProjections();
        all.Count.ShouldBe(1);
    }

    [Fact]
    public void Register_NullProjectionType_Throws()
    {
        var registry = new ProjectionRegistry();
        Should.Throw<ArgumentNullException>(() =>
            registry.Register(null!, typeof(TestReadModel)));
    }

    [Fact]
    public void Register_NullReadModelType_Throws()
    {
        var registry = new ProjectionRegistry();
        Should.Throw<ArgumentNullException>(() =>
            registry.Register(typeof(TestProjection), null!));
    }

    #endregion

    #region GetProjectionForReadModel Contract

    [Fact]
    public void GetProjectionForReadModel_Registered_ReturnsRegistration()
    {
        var registry = new ProjectionRegistry();
        registry.Register<TestProjection, TestReadModel>();

        var result = registry.GetProjectionForReadModel<TestReadModel>();
        result.ShouldNotBeNull();
        result!.ProjectionType.ShouldBe(typeof(TestProjection));
    }

    [Fact]
    public void GetProjectionForReadModel_NotRegistered_ReturnsNull()
    {
        var registry = new ProjectionRegistry();
        var result = registry.GetProjectionForReadModel<TestReadModel>();
        result.ShouldBeNull();
    }

    [Fact]
    public void GetProjectionForReadModel_TypeOverload_NullType_Throws()
    {
        var registry = new ProjectionRegistry();
        Should.Throw<ArgumentNullException>(() =>
            registry.GetProjectionForReadModel(null!));
    }

    #endregion

    #region GetProjectionsForEvent Contract

    [Fact]
    public void GetProjectionsForEvent_RegisteredEvent_ReturnsProjections()
    {
        var registry = new ProjectionRegistry();
        registry.Register<TestProjectionWithHandler, TestReadModel>();

        var results = registry.GetProjectionsForEvent(typeof(TestEvent));
        results.Count.ShouldBeGreaterThan(0);
    }

    [Fact]
    public void GetProjectionsForEvent_UnregisteredEvent_ReturnsEmpty()
    {
        var registry = new ProjectionRegistry();
        var results = registry.GetProjectionsForEvent(typeof(string));
        results.Count.ShouldBe(0);
    }

    [Fact]
    public void GetProjectionsForEvent_NullType_Throws()
    {
        var registry = new ProjectionRegistry();
        Should.Throw<ArgumentNullException>(() =>
            registry.GetProjectionsForEvent(null!));
    }

    #endregion

    #region ProjectionRegistration Handler Discovery

    [Fact]
    public void ProjectionRegistration_DiscoverHandlers_FindsCreator()
    {
        var reg = new ProjectionRegistration(typeof(TestProjectionWithCreator), typeof(TestReadModel));
        var handler = reg.GetHandlerInfo(typeof(TestEvent));

        handler.ShouldNotBeNull();
        handler!.Value.HandlerType.ShouldBe(ProjectionHandlerType.Creator);
    }

    [Fact]
    public void ProjectionRegistration_DiscoverHandlers_FindsHandler()
    {
        var reg = new ProjectionRegistration(typeof(TestProjectionWithHandler), typeof(TestReadModel));
        var handler = reg.GetHandlerInfo(typeof(TestEvent));

        handler.ShouldNotBeNull();
        handler!.Value.HandlerType.ShouldBe(ProjectionHandlerType.Handler);
    }

    [Fact]
    public void ProjectionRegistration_DiscoverHandlers_FindsDeleter()
    {
        var reg = new ProjectionRegistration(typeof(TestProjectionWithDeleter), typeof(TestReadModel));
        var handler = reg.GetHandlerInfo(typeof(TestEvent));

        handler.ShouldNotBeNull();
        handler!.Value.HandlerType.ShouldBe(ProjectionHandlerType.Deleter);
    }

    [Fact]
    public void ProjectionRegistration_GetHandlerInfo_UnhandledEvent_ReturnsNull()
    {
        var reg = new ProjectionRegistration(typeof(TestProjection), typeof(TestReadModel));
        var handler = reg.GetHandlerInfo(typeof(string));
        handler.ShouldBeNull();
    }

    [Fact]
    public void ProjectionRegistration_HandledEventTypes_IncludesRegisteredEvents()
    {
        var reg = new ProjectionRegistration(typeof(TestProjectionWithHandler), typeof(TestReadModel));
        reg.HandledEventTypes.ShouldContain(typeof(TestEvent));
    }

    [Fact]
    public void ProjectionRegistration_ProjectionName_IsTypeName()
    {
        var reg = new ProjectionRegistration(typeof(TestProjection), typeof(TestReadModel));
        reg.ProjectionName.ShouldBe("TestProjection");
    }

    #endregion

    #region GetAllProjections Contract

    [Fact]
    public void GetAllProjections_Empty_ReturnsEmptyList()
    {
        var registry = new ProjectionRegistry();
        registry.GetAllProjections().Count.ShouldBe(0);
    }

    [Fact]
    public void GetAllProjections_MultipleRegistrations_ReturnsAll()
    {
        var registry = new ProjectionRegistry();
        registry.Register<TestProjection, TestReadModel>();
        registry.Register<TestProjection2, TestReadModel2>();

        registry.GetAllProjections().Count.ShouldBe(2);
    }

    #endregion

    #region Test Types

    public sealed class TestReadModel : IReadModel
    {
        public Guid Id { get; set; }
    }

    public sealed class TestReadModel2 : IReadModel
    {
        public Guid Id { get; set; }
    }

    public sealed class TestEvent;

    public sealed class TestProjection : IProjection<TestReadModel>
    {
        public string ProjectionName => "TestProjection";
    }

    public sealed class TestProjection2 : IProjection<TestReadModel2>
    {
        public string ProjectionName => "TestProjection2";
    }

    public sealed class TestProjectionWithCreator : IProjection<TestReadModel>,
        IProjectionCreator<TestEvent, TestReadModel>
    {
        public string ProjectionName => "TestProjectionWithCreator";

        public TestReadModel Create(TestEvent domainEvent, ProjectionContext context)
            => new() { Id = context.StreamId };
    }

    public sealed class TestProjectionWithHandler : IProjection<TestReadModel>,
        IProjectionHandler<TestEvent, TestReadModel>
    {
        public string ProjectionName => "TestProjectionWithHandler";

        public TestReadModel Apply(TestEvent domainEvent, TestReadModel current, ProjectionContext context)
            => current;
    }

    public sealed class TestProjectionWithDeleter : IProjection<TestReadModel>,
        IProjectionDeleter<TestEvent, TestReadModel>
    {
        public string ProjectionName => "TestProjectionWithDeleter";

        public bool ShouldDelete(TestEvent domainEvent, TestReadModel current, ProjectionContext context)
            => true;
    }

    #endregion
}
