using Encina.Marten.Projections;
using LanguageExt;
using Marten;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Shouldly;

namespace Encina.UnitTests.Marten.Projections;

public class InlineProjectionDispatcherTests
{
    private readonly IDocumentSession _session;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<MartenInlineProjectionDispatcher> _logger;
    private readonly ProjectionRegistry _registry;

    public InlineProjectionDispatcherTests()
    {
        _session = Substitute.For<IDocumentSession>();
        _serviceProvider = Substitute.For<IServiceProvider>();
        _logger = NullLogger<MartenInlineProjectionDispatcher>.Instance;
        _registry = new ProjectionRegistry();
    }

    private MartenInlineProjectionDispatcher CreateSut()
    {
        return new MartenInlineProjectionDispatcher(
            _session, _serviceProvider, _logger, _registry);
    }

    // Constructor null guard tests

    [Fact]
    public void Constructor_NullSession_ThrowsArgumentNullException()
    {
        var act = () => new MartenInlineProjectionDispatcher(
            null!, _serviceProvider, _logger, _registry);
        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("session");
    }

    [Fact]
    public void Constructor_NullServiceProvider_ThrowsArgumentNullException()
    {
        var act = () => new MartenInlineProjectionDispatcher(
            _session, null!, _logger, _registry);
        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("serviceProvider");
    }

    [Fact]
    public void Constructor_NullLogger_ThrowsArgumentNullException()
    {
        var act = () => new MartenInlineProjectionDispatcher(
            _session, _serviceProvider, null!, _registry);
        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("logger");
    }

    [Fact]
    public void Constructor_NullRegistry_ThrowsArgumentNullException()
    {
        var act = () => new MartenInlineProjectionDispatcher(
            _session, _serviceProvider, _logger, null!);
        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("registry");
    }

    // DispatchAsync tests

    [Fact]
    public async Task DispatchAsync_NullEvent_ThrowsArgumentNullException()
    {
        var sut = CreateSut();
        var context = new ProjectionContext();
        var act = () => sut.DispatchAsync(null!, context);
        await Should.ThrowAsync<ArgumentNullException>(act);
    }

    [Fact]
    public async Task DispatchAsync_NullContext_ThrowsArgumentNullException()
    {
        var sut = CreateSut();
        var act = () => sut.DispatchAsync(new object(), null!);
        await Should.ThrowAsync<ArgumentNullException>(act);
    }

    [Fact]
    public async Task DispatchAsync_NoRegisteredProjection_ReturnsRight()
    {
        // Arrange - no projections registered
        var sut = CreateSut();
        var context = new ProjectionContext { StreamId = Guid.NewGuid() };

        // Act
        var result = await sut.DispatchAsync(new TestCreatedEvent("test"), context);

        // Assert
        result.IsRight.ShouldBeTrue();
    }

    // DispatchManyAsync tests

    [Fact]
    public async Task DispatchManyAsync_NullEvents_ThrowsArgumentNullException()
    {
        var sut = CreateSut();
        var act = () => sut.DispatchManyAsync(null!);
        await Should.ThrowAsync<ArgumentNullException>(act);
    }

    [Fact]
    public async Task DispatchManyAsync_EmptyEvents_ReturnsRight()
    {
        // Arrange
        var sut = CreateSut();

        // Act
        var result = await sut.DispatchManyAsync([]);

        // Assert
        result.IsRight.ShouldBeTrue();
    }

    [Fact]
    public async Task DispatchManyAsync_NoRegisteredProjections_ReturnsRight()
    {
        // Arrange
        var sut = CreateSut();
        var events = new[]
        {
            (Event: (object)new TestCreatedEvent("test"), Context: new ProjectionContext { StreamId = Guid.NewGuid() })
        };

        // Act
        var result = await sut.DispatchManyAsync(events);

        // Assert
        result.IsRight.ShouldBeTrue();
    }

    // Test types

    public sealed record TestCreatedEvent(string Name);

    public sealed class TestReadModel : IReadModel
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }
}
