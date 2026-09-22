using Encina.Marten.Projections;
using Marten;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Shouldly;

namespace Encina.UnitTests.Marten.Projections;

/// <summary>
/// Exercises <see cref="MartenInlineProjectionDispatcher"/> against registered projections, which
/// drives the session through its generic Load/Store/Delete helpers (issue #1095).
/// </summary>
public sealed class InlineProjectionDispatcherBehaviorTests
{
    private readonly IDocumentSession _session = Substitute.For<IDocumentSession>();
    private readonly ProjectionRegistry _registry = new();
    private readonly IServiceProvider _serviceProvider;

    public InlineProjectionDispatcherBehaviorTests()
    {
        var services = new ServiceCollection();
        services.AddSingleton<CounterProjection>();
        _serviceProvider = services.BuildServiceProvider();
        _registry.Register<CounterProjection, CounterReadModel>();
    }

    private MartenInlineProjectionDispatcher CreateSut() =>
        new(_session, _serviceProvider, NullLogger<MartenInlineProjectionDispatcher>.Instance, _registry);

    [Fact]
    public async Task DispatchAsync_CreatorEvent_NoExistingReadModel_StoresAndSaves()
    {
        var streamId = Guid.NewGuid();
        _session.LoadAsync<CounterReadModel>(streamId, Arg.Any<CancellationToken>()).Returns((CounterReadModel?)null);
        var sut = CreateSut();

        var result = await sut.DispatchAsync(new CounterCreated("first"), new ProjectionContext(streamId, 1, 0, DateTime.UtcNow));

        result.IsRight.ShouldBeTrue();
        _session.Received(1).Store(Arg.Is<CounterReadModel[]>(models => models.Length == 1 && models[0].Id == streamId && models[0].Name == "first"));
        await _session.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DispatchAsync_CreatorEvent_ReadModelAlreadyExists_IsIdempotent()
    {
        var streamId = Guid.NewGuid();
        _session.LoadAsync<CounterReadModel>(streamId, Arg.Any<CancellationToken>()).Returns(new CounterReadModel { Id = streamId });
        var sut = CreateSut();

        var result = await sut.DispatchAsync(new CounterCreated("again"), new ProjectionContext(streamId, 1, 0, DateTime.UtcNow));

        result.IsRight.ShouldBeTrue();
        _session.DidNotReceiveWithAnyArgs().Store(Arg.Any<CounterReadModel[]>());
    }

    [Fact]
    public async Task DispatchAsync_HandlerEvent_ExistingReadModel_AppliesAndStores()
    {
        var streamId = Guid.NewGuid();
        _session.LoadAsync<CounterReadModel>(streamId, Arg.Any<CancellationToken>()).Returns(new CounterReadModel { Id = streamId, Count = 2 });
        var sut = CreateSut();

        var result = await sut.DispatchAsync(new CounterIncremented(), new ProjectionContext(streamId, 2, 0, DateTime.UtcNow));

        result.IsRight.ShouldBeTrue();
        _session.Received(1).Store(Arg.Is<CounterReadModel[]>(models => models.Length == 1 && models[0].Count == 3));
    }

    [Fact]
    public async Task DispatchAsync_HandlerEvent_NoReadModel_Skips()
    {
        var streamId = Guid.NewGuid();
        _session.LoadAsync<CounterReadModel>(streamId, Arg.Any<CancellationToken>()).Returns((CounterReadModel?)null);
        var sut = CreateSut();

        var result = await sut.DispatchAsync(new CounterIncremented(), new ProjectionContext(streamId, 2, 0, DateTime.UtcNow));

        result.IsRight.ShouldBeTrue();
        _session.DidNotReceiveWithAnyArgs().Store(Arg.Any<CounterReadModel[]>());
    }

    [Fact]
    public async Task DispatchAsync_DeleterEvent_ExistingReadModel_DeletesAndSaves()
    {
        var streamId = Guid.NewGuid();
        _session.LoadAsync<CounterReadModel>(streamId, Arg.Any<CancellationToken>()).Returns(new CounterReadModel { Id = streamId });
        var sut = CreateSut();

        var result = await sut.DispatchAsync(new CounterRemoved(), new ProjectionContext(streamId, 3, 0, DateTime.UtcNow));

        result.IsRight.ShouldBeTrue();
        _session.Received(1).Delete<CounterReadModel>(streamId);
        await _session.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DispatchAsync_ProjectionThrows_ReturnsLeftWithApplyFailed()
    {
        var streamId = Guid.NewGuid();
        _session.LoadAsync<CounterReadModel>(streamId, Arg.Any<CancellationToken>()).Returns((CounterReadModel?)null);
        var sut = CreateSut();

        var result = await sut.DispatchAsync(new CounterCreated("explode"), new ProjectionContext(streamId, 1, 0, DateTime.UtcNow));

        result.IsLeft.ShouldBeTrue();
        result.Match(
            Right: _ => throw new InvalidOperationException("Expected Left"),
            Left: error => error.Message.ShouldContain("CounterCreated"));
    }

    [Fact]
    public async Task DispatchManyAsync_StopsAtTheFirstFailure()
    {
        var streamId = Guid.NewGuid();
        _session.LoadAsync<CounterReadModel>(streamId, Arg.Any<CancellationToken>()).Returns((CounterReadModel?)null);
        var context = new ProjectionContext(streamId, 1, 0, DateTime.UtcNow);
        var sut = CreateSut();

        var result = await sut.DispatchManyAsync([(new CounterCreated("explode"), context), (new CounterCreated("never"), context)]);

        result.IsLeft.ShouldBeTrue();
        _session.DidNotReceiveWithAnyArgs().Store(Arg.Any<CounterReadModel[]>());
    }

    public sealed class CounterReadModel : IReadModel
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public int Count { get; set; }
    }

    public sealed record CounterCreated(string Name);
    public sealed record CounterIncremented;
    public sealed record CounterRemoved;

    public sealed class CounterProjection :
        IProjection<CounterReadModel>,
        IProjectionCreator<CounterCreated, CounterReadModel>,
        IProjectionHandler<CounterIncremented, CounterReadModel>,
        IProjectionDeleter<CounterRemoved, CounterReadModel>
    {
        public string ProjectionName => "Counter";

        public CounterReadModel Create(CounterCreated domainEvent, ProjectionContext context)
        {
            if (domainEvent.Name == "explode")
            {
                throw new InvalidOperationException("projection exploded");
            }

            return new CounterReadModel { Id = context.StreamId, Name = domainEvent.Name };
        }

        public CounterReadModel Apply(CounterIncremented domainEvent, CounterReadModel current, ProjectionContext context)
        {
            current.Count++;
            return current;
        }

        public bool ShouldDelete(CounterRemoved domainEvent, CounterReadModel current, ProjectionContext context) => true;
    }
}
