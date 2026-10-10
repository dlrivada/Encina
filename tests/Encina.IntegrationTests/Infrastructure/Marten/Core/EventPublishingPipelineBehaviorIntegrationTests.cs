using Encina.DomainModeling;
using Encina.IntegrationTests.Infrastructure.Marten.Fixtures;
using Encina.Marten;
using LanguageExt;
using Marten;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using static LanguageExt.Prelude;

namespace Encina.IntegrationTests.Infrastructure.Marten.Core;

/// <summary>
/// End-to-end proof (issue #2030) that <c>AddEncinaMarten</c> wires
/// <see cref="EventPublishingPipelineBehavior{TRequest, TResponse}"/> into the pipeline: a command that
/// saves an event-sourced aggregate through the repository publishes its domain events to the
/// notification handlers exactly once, against a real Marten/PostgreSQL store.
/// </summary>
[Collection(MartenCollection.Name)]
[Trait("Category", "Integration")]
[Trait("Database", "PostgreSQL")]
public sealed class EventPublishingPipelineBehaviorIntegrationTests : IAsyncLifetime
{
    private readonly MartenFixture _fixture;

    public EventPublishingPipelineBehaviorIntegrationTests(MartenFixture fixture)
    {
        _fixture = fixture;
    }

    public ValueTask InitializeAsync()
    {
        Assert.SkipUnless(_fixture.IsAvailable, "Marten PostgreSQL container not available");
        return ValueTask.CompletedTask;
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    private ServiceProvider BuildProvider(PublishedEventLog log, bool autoPublish = true, bool failingHandler = false)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddEncina();
        services.AddScoped(_ => RequestContext.Create());
        services.AddSingleton(_fixture.Store!);
        services.AddScoped(sp => sp.GetRequiredService<IDocumentStore>().LightweightSession());
        services.AddEncinaMarten(options => options.AutoPublishDomainEvents = autoPublish);
        services.AddSingleton(log);
        services.AddScoped<IRequestHandler<PlaceOrder, Guid>, PlaceOrderHandler>();
        services.AddScoped<IRequestHandler<AddLine, Guid>, AddLineHandler>();
        services.AddScoped<INotificationHandler<OrderPlaced>>(
            sp => new RecordingHandler<OrderPlaced>(sp.GetRequiredService<PublishedEventLog>(), failingHandler));
        services.AddScoped<INotificationHandler<LineAdded>>(
            sp => new RecordingHandler<LineAdded>(sp.GetRequiredService<PublishedEventLog>(), failure: false));

        return services.BuildServiceProvider(
            new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });
    }

    [Fact]
    public async Task Send_CommandThatCreatesAnAggregate_PublishesItsEventsExactlyOnce()
    {
        var log = new PublishedEventLog();
        using var provider = BuildProvider(log);
        using var scope = provider.CreateScope();
        var encina = scope.ServiceProvider.GetRequiredService<IEncina>();
        var orderId = Guid.NewGuid();

        var result = await encina.Send(new PlaceOrder(orderId, "first"));

        result.IsRight.ShouldBeTrue($"Send should succeed: {result}");
        log.Events.OfType<OrderPlaced>().Count(e => e.OrderId == orderId).ShouldBe(1);
        log.Events.Count.ShouldBe(1);
    }

    [Fact]
    public async Task Send_CommandThatSavesALoadedAggregate_PublishesOnlyTheNewEvents()
    {
        var log = new PublishedEventLog();
        using var provider = BuildProvider(log);
        var orderId = Guid.NewGuid();

        using (var createScope = provider.CreateScope())
        {
            var created = await createScope.ServiceProvider.GetRequiredService<IEncina>()
                .Send(new PlaceOrder(orderId, "second"));
            created.IsRight.ShouldBeTrue();
        }

        log.Events.Clear();

        using var addScope = provider.CreateScope();
        var added = await addScope.ServiceProvider.GetRequiredService<IEncina>()
            .Send(new AddLine(orderId, 25m));

        added.IsRight.ShouldBeTrue($"Send should succeed: {added}");
        log.Events.Count.ShouldBe(1);
        log.Events.OfType<LineAdded>().Single().OrderId.ShouldBe(orderId);
    }

    [Fact]
    public async Task Send_AutoPublishDisabled_StoresTheEventsButPublishesNothing()
    {
        var log = new PublishedEventLog();
        using var provider = BuildProvider(log, autoPublish: false);
        using var scope = provider.CreateScope();
        var encina = scope.ServiceProvider.GetRequiredService<IEncina>();
        var orderId = Guid.NewGuid();

        var result = await encina.Send(new PlaceOrder(orderId, "third"));

        result.IsRight.ShouldBeTrue();
        log.Events.ShouldBeEmpty();

        await using var verify = _fixture.Store!.LightweightSession();
        var stream = await verify.Events.FetchStreamAsync(orderId);
        stream.Count.ShouldBe(1);
    }

    [Fact]
    public async Task Send_NotificationHandlerFails_TheCommandReturnsLeft()
    {
        var log = new PublishedEventLog();
        using var provider = BuildProvider(log, failingHandler: true);
        using var scope = provider.CreateScope();
        var encina = scope.ServiceProvider.GetRequiredService<IEncina>();

        var result = await encina.Send(new PlaceOrder(Guid.NewGuid(), "fourth"));

        result.IsLeft.ShouldBeTrue("a failed publication must fail the command, never report success");
    }

    // Test domain

    public sealed record OrderPlaced(Guid OrderId, string Name) : INotification;

    public sealed record LineAdded(Guid OrderId, decimal Amount) : INotification;

    public sealed class Order : AggregateBase
    {
        public string Name { get; set; } = string.Empty;

        public Order()
        {
        }

        public Order(Guid id, string name) => RaiseEvent(new OrderPlaced(id, name));

        public void AddLine(decimal amount) => RaiseEvent(new LineAdded(Id, amount));

        protected override void Apply(object domainEvent)
        {
            if (domainEvent is OrderPlaced placed)
            {
                Id = placed.OrderId;
                Name = placed.Name;
            }
        }
    }

    public sealed record PlaceOrder(Guid OrderId, string Name) : ICommand<Guid>;

    public sealed record AddLine(Guid OrderId, decimal Amount) : ICommand<Guid>;

    public sealed class PlaceOrderHandler(IAggregateRepository<Order> repository) : ICommandHandler<PlaceOrder, Guid>
    {
        public async Task<Either<EncinaError, Guid>> Handle(PlaceOrder request, CancellationToken cancellationToken)
        {
            var saved = await repository.CreateAsync(new Order(request.OrderId, request.Name), cancellationToken);
            return saved.Map(_ => request.OrderId);
        }
    }

    public sealed class AddLineHandler(IAggregateRepository<Order> repository) : ICommandHandler<AddLine, Guid>
    {
        public async Task<Either<EncinaError, Guid>> Handle(AddLine request, CancellationToken cancellationToken)
        {
            var loaded = await repository.LoadAsync(request.OrderId, cancellationToken);
            return await loaded.MatchAsync(
                async order =>
                {
                    order.AddLine(request.Amount);
                    var saved = await repository.SaveAsync(order, cancellationToken);
                    return saved.Map(_ => request.OrderId);
                },
                error => Task.FromResult(Left<EncinaError, Guid>(error)));
        }
    }

    public sealed class PublishedEventLog
    {
        public List<INotification> Events { get; } = [];
    }

    public sealed class RecordingHandler<TNotification>(PublishedEventLog log, bool failure) : INotificationHandler<TNotification>
        where TNotification : INotification
    {
        public Task<Either<EncinaError, Unit>> Handle(TNotification notification, CancellationToken cancellationToken)
        {
            if (failure)
            {
                return Task.FromResult(Left<EncinaError, Unit>(EncinaErrors.Create("test.handler.failed", "handler failed")));
            }

            lock (log.Events)
            {
                log.Events.Add(notification);
            }

            return Task.FromResult(Right<EncinaError, Unit>(unit));
        }
    }
}
