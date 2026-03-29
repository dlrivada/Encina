using Encina.Marten.Projections;
using LanguageExt;
using Marten;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Shouldly;

namespace Encina.UnitTests.Marten.Projections;

public class MartenProjectionManagerTests
{
    private readonly IDocumentStore _store;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<MartenProjectionManager> _logger;
    private readonly ProjectionRegistry _registry;
    private readonly TimeProvider _timeProvider;

    public MartenProjectionManagerTests()
    {
        _store = Substitute.For<IDocumentStore>();
        _serviceProvider = Substitute.For<IServiceProvider>();
        _logger = NullLogger<MartenProjectionManager>.Instance;
        _registry = new ProjectionRegistry();
        _timeProvider = Substitute.For<TimeProvider>();
        _timeProvider.GetUtcNow().Returns(new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero));
    }

    private MartenProjectionManager CreateSut()
    {
        return new MartenProjectionManager(
            _store, _serviceProvider, _logger, _registry, _timeProvider);
    }

    // Constructor null guard tests

    [Fact]
    public void Constructor_NullStore_ThrowsArgumentNullException()
    {
        var act = () => new MartenProjectionManager(
            null!, _serviceProvider, _logger, _registry);
        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("store");
    }

    [Fact]
    public void Constructor_NullServiceProvider_ThrowsArgumentNullException()
    {
        var act = () => new MartenProjectionManager(
            _store, null!, _logger, _registry);
        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("serviceProvider");
    }

    [Fact]
    public void Constructor_NullLogger_ThrowsArgumentNullException()
    {
        var act = () => new MartenProjectionManager(
            _store, _serviceProvider, null!, _registry);
        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("logger");
    }

    [Fact]
    public void Constructor_NullRegistry_ThrowsArgumentNullException()
    {
        var act = () => new MartenProjectionManager(
            _store, _serviceProvider, _logger, null!);
        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("registry");
    }

    [Fact]
    public void Constructor_NullTimeProvider_UsesSystemTimeProvider()
    {
        // Should not throw - null TimeProvider uses default
        var sut = new MartenProjectionManager(
            _store, _serviceProvider, _logger, _registry, null);
        sut.ShouldNotBeNull();
    }

    // GetStatusAsync tests

    [Fact]
    public async Task GetStatusAsync_NotRegistered_ReturnsLeft()
    {
        // Arrange - empty registry
        var sut = CreateSut();

        // Act
        var result = await sut.GetStatusAsync<TestReadModel>();

        // Assert
        result.IsLeft.ShouldBeTrue();
    }

    [Fact]
    public async Task GetStatusAsync_Registered_ReturnsStatus()
    {
        // Arrange
        _registry.Register<TestProjection, TestReadModel>();
        var sut = CreateSut();

        // Act
        var result = await sut.GetStatusAsync<TestReadModel>();

        // Assert
        result.IsRight.ShouldBeTrue();
        result.Match(
            Right: status =>
            {
                status.ProjectionName.ShouldBe(nameof(TestProjection));
                status.State.ShouldBe(ProjectionState.Stopped);
            },
            Left: _ => throw new InvalidOperationException("Expected Right"));
    }

    // GetAllStatusesAsync tests

    [Fact]
    public async Task GetAllStatusesAsync_EmptyRegistry_ReturnsEmptyDictionary()
    {
        // Arrange
        var sut = CreateSut();

        // Act
        var result = await sut.GetAllStatusesAsync();

        // Assert
        result.IsRight.ShouldBeTrue();
        result.Match(
            Right: statuses => statuses.Count.ShouldBe(0),
            Left: _ => throw new InvalidOperationException("Expected Right"));
    }

    [Fact]
    public async Task GetAllStatusesAsync_WithRegistrations_ReturnsAllStatuses()
    {
        // Arrange
        _registry.Register<TestProjection, TestReadModel>();
        var sut = CreateSut();

        // Act
        var result = await sut.GetAllStatusesAsync();

        // Assert
        result.IsRight.ShouldBeTrue();
        result.Match(
            Right: statuses => statuses.Count.ShouldBe(1),
            Left: _ => throw new InvalidOperationException("Expected Right"));
    }

    // StartAsync tests

    [Fact]
    public async Task StartAsync_NotRegistered_ReturnsLeft()
    {
        var sut = CreateSut();
        var result = await sut.StartAsync<TestReadModel>();
        result.IsLeft.ShouldBeTrue();
    }

    [Fact]
    public async Task StartAsync_Registered_SetsStateToRunning()
    {
        // Arrange
        _registry.Register<TestProjection, TestReadModel>();
        var sut = CreateSut();

        // Act
        var result = await sut.StartAsync<TestReadModel>();

        // Assert
        result.IsRight.ShouldBeTrue();

        var statusResult = await sut.GetStatusAsync<TestReadModel>();
        statusResult.Match(
            Right: status => status.State.ShouldBe(ProjectionState.Running),
            Left: _ => throw new InvalidOperationException("Expected Right"));
    }

    // StopAsync tests

    [Fact]
    public async Task StopAsync_NotRegistered_ReturnsLeft()
    {
        var sut = CreateSut();
        var result = await sut.StopAsync<TestReadModel>();
        result.IsLeft.ShouldBeTrue();
    }

    [Fact]
    public async Task StopAsync_Registered_SetsStateToStopped()
    {
        // Arrange
        _registry.Register<TestProjection, TestReadModel>();
        var sut = CreateSut();
        await sut.StartAsync<TestReadModel>(); // Set to Running first

        // Act
        var result = await sut.StopAsync<TestReadModel>();

        // Assert
        result.IsRight.ShouldBeTrue();

        var statusResult = await sut.GetStatusAsync<TestReadModel>();
        statusResult.Match(
            Right: status => status.State.ShouldBe(ProjectionState.Stopped),
            Left: _ => throw new InvalidOperationException("Expected Right"));
    }

    // PauseAsync tests

    [Fact]
    public async Task PauseAsync_NotRegistered_ReturnsLeft()
    {
        var sut = CreateSut();
        var result = await sut.PauseAsync<TestReadModel>();
        result.IsLeft.ShouldBeTrue();
    }

    [Fact]
    public async Task PauseAsync_Registered_SetsStateToPaused()
    {
        // Arrange
        _registry.Register<TestProjection, TestReadModel>();
        var sut = CreateSut();
        await sut.StartAsync<TestReadModel>();

        // Act
        var result = await sut.PauseAsync<TestReadModel>();

        // Assert
        result.IsRight.ShouldBeTrue();

        var statusResult = await sut.GetStatusAsync<TestReadModel>();
        statusResult.Match(
            Right: status => status.State.ShouldBe(ProjectionState.Paused),
            Left: _ => throw new InvalidOperationException("Expected Right"));
    }

    // ResumeAsync tests

    [Fact]
    public async Task ResumeAsync_NotRegistered_ReturnsLeft()
    {
        var sut = CreateSut();
        var result = await sut.ResumeAsync<TestReadModel>();
        result.IsLeft.ShouldBeTrue();
    }

    [Fact]
    public async Task ResumeAsync_Registered_SetsStateToRunning()
    {
        // Arrange
        _registry.Register<TestProjection, TestReadModel>();
        var sut = CreateSut();
        await sut.PauseAsync<TestReadModel>();

        // Act
        var result = await sut.ResumeAsync<TestReadModel>();

        // Assert
        result.IsRight.ShouldBeTrue();

        var statusResult = await sut.GetStatusAsync<TestReadModel>();
        statusResult.Match(
            Right: status => status.State.ShouldBe(ProjectionState.Running),
            Left: _ => throw new InvalidOperationException("Expected Right"));
    }

    // RebuildAsync tests

    [Fact]
    public async Task RebuildAsync_NotRegistered_ReturnsLeft()
    {
        var sut = CreateSut();
        var result = await sut.RebuildAsync<TestReadModel>();
        result.IsLeft.ShouldBeTrue();
    }

    [Fact]
    public async Task RebuildAsync_NullOptions_ThrowsArgumentNullException()
    {
        // Arrange
        _registry.Register<TestProjection, TestReadModel>();
        var sut = CreateSut();

        // Act
        var act = () => sut.RebuildAsync<TestReadModel>(null!);

        // Assert
        await Should.ThrowAsync<ArgumentNullException>(act);
    }

    // Test types

    public sealed record TestCreatedEvent(string Name);

    public sealed class TestReadModel : IReadModel
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    public sealed class TestProjection :
        IProjection<TestReadModel>,
        IProjectionCreator<TestCreatedEvent, TestReadModel>
    {
        public string ProjectionName => "TestProjection";

        public TestReadModel Create(TestCreatedEvent domainEvent, ProjectionContext context)
        {
            return new TestReadModel { Id = context.StreamId, Name = domainEvent.Name };
        }
    }
}
