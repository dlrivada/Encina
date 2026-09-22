using Encina.DomainModeling;
using Encina.IntegrationTests.Infrastructure.Marten.Fixtures;
using Encina.IntegrationTests.Infrastructure.Marten.Snapshots;
using Encina.Marten;
using Encina.Marten.Projections;
using Marten;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Shouldly;

namespace Encina.IntegrationTests.Infrastructure.Marten.Projections;

/// <summary>
/// End-to-end proof for issue #1095: an aggregate saved through the DI-resolved
/// <see cref="IAggregateRepository{TAggregate}"/> updates its read model through the inline
/// projection registered with <c>AddProjection</c>, against a real PostgreSQL database.
/// </summary>
[Collection(MartenCollection.Name)]
[Trait("Category", "Integration")]
[Trait("Database", "PostgreSQL")]
public sealed class MartenInlineProjectionIntegrationTests : IAsyncLifetime
{
    private readonly MartenFixture _fixture;

    public MartenInlineProjectionIntegrationTests(MartenFixture fixture)
    {
        _fixture = fixture;
    }

    public ValueTask InitializeAsync()
    {
        Assert.SkipUnless(_fixture.IsAvailable, "Marten PostgreSQL container not available");
        return ValueTask.CompletedTask;
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    private ServiceProvider BuildServiceProvider(Action<EncinaMartenOptions>? configure = null)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(_fixture.Store!);
        services.AddScoped(sp => sp.GetRequiredService<IDocumentStore>().LightweightSession());
        services.AddScoped(_ => Substitute.For<IRequestContext>());

        services.AddEncinaMarten(configure ?? (_ => { }));
        services.AddAggregateRepository<TestSnapshotableAggregate>();
        services.AddProjection<OrderSummaryProjection, OrderSummaryReadModel>();

        return services.BuildServiceProvider();
    }

    [Fact]
    public async Task CreateAsync_ThroughDI_PopulatesTheReadModel()
    {
        await using var provider = BuildServiceProvider();
        var id = Guid.NewGuid();

        using (var scope = provider.CreateScope())
        {
            var repository = scope.ServiceProvider.GetRequiredService<IAggregateRepository<TestSnapshotableAggregate>>();
            var aggregate = new TestSnapshotableAggregate(id, "Projected order");
            aggregate.AddItem(10m);
            aggregate.AddItem(15m);

            var result = await repository.CreateAsync(aggregate);

            result.IsRight.ShouldBeTrue($"CreateAsync should succeed: {result}");
        }

        using (var scope = provider.CreateScope())
        {
            var readModels = scope.ServiceProvider.GetRequiredService<IReadModelRepository<OrderSummaryReadModel>>();

            var loaded = await readModels.GetByIdAsync(id);

            loaded.IsRight.ShouldBeTrue($"read model should exist: {loaded}");
            var summary = loaded.Match(static m => m, static _ => throw new InvalidOperationException("Expected Right"));
            summary.Name.ShouldBe("Projected order");
            summary.ItemCount.ShouldBe(2);
            summary.Total.ShouldBe(25m);
            summary.Status.ShouldBe("Created");
            summary.LastSequence.ShouldBe(3);
        }
    }

    [Fact]
    public async Task SaveAsync_ThroughDI_UpdatesTheExistingReadModel()
    {
        await using var provider = BuildServiceProvider();
        var id = Guid.NewGuid();

        using (var scope = provider.CreateScope())
        {
            var repository = scope.ServiceProvider.GetRequiredService<IAggregateRepository<TestSnapshotableAggregate>>();
            var aggregate = new TestSnapshotableAggregate(id, "Updated order");
            aggregate.AddItem(5m);
            (await repository.CreateAsync(aggregate)).IsRight.ShouldBeTrue();
        }

        using (var scope = provider.CreateScope())
        {
            var repository = scope.ServiceProvider.GetRequiredService<IAggregateRepository<TestSnapshotableAggregate>>();
            var loaded = await repository.LoadAsync(id);
            var aggregate = loaded.Match(static a => a, static _ => throw new InvalidOperationException("Expected Right"));
            aggregate.AddItem(20m);
            aggregate.Complete();

            var result = await repository.SaveAsync(aggregate);

            result.IsRight.ShouldBeTrue($"SaveAsync should succeed: {result}");
        }

        await using var session = _fixture.Store!.LightweightSession();
        var summary = await session.LoadAsync<OrderSummaryReadModel>(id);

        summary.ShouldNotBeNull();
        summary.ItemCount.ShouldBe(2);
        summary.Total.ShouldBe(25m);
        summary.Status.ShouldBe("Completed");
        summary.LastSequence.ShouldBe(4);
    }

    [Fact]
    public async Task SaveAsync_InlineProjectionsDisabled_LeavesTheReadModelUntouched()
    {
        await using var provider = BuildServiceProvider(options => options.Projections.UseInlineProjections = false);
        var id = Guid.NewGuid();

        using (var scope = provider.CreateScope())
        {
            var repository = scope.ServiceProvider.GetRequiredService<IAggregateRepository<TestSnapshotableAggregate>>();
            var aggregate = new TestSnapshotableAggregate(id, "Not projected");
            (await repository.CreateAsync(aggregate)).IsRight.ShouldBeTrue();
        }

        await using var session = _fixture.Store!.LightweightSession();
        (await session.LoadAsync<OrderSummaryReadModel>(id)).ShouldBeNull();
    }

}

// Top-level types: Marten derives the document table name from the CLR type name and PostgreSQL
// caps identifiers at 63 characters, which a nested type name would exceed.

/// <summary>Read model projected from <see cref="TestSnapshotableAggregate"/> events.</summary>
public sealed class OrderSummaryReadModel : IReadModel
{
    /// <summary>The aggregate (stream) identifier.</summary>
    public Guid Id { get; set; }

    /// <summary>The name given when the aggregate was created.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Sum of the item amounts added so far.</summary>
    public decimal Total { get; set; }

    /// <summary>Number of items added so far.</summary>
    public int ItemCount { get; set; }

    /// <summary>"Created" until the aggregate completes, then "Completed".</summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>Stream version of the last event applied, to prove the projection context carries it.</summary>
    public long LastSequence { get; set; }
}

/// <summary>Inline projection that keeps <see cref="OrderSummaryReadModel"/> in step with the stream.</summary>
public sealed class OrderSummaryProjection :
    IProjection<OrderSummaryReadModel>,
    IProjectionCreator<TestAggregateCreated, OrderSummaryReadModel>,
    IProjectionHandler<TestItemAdded, OrderSummaryReadModel>,
    IProjectionHandler<TestAggregateCompleted, OrderSummaryReadModel>
{
    /// <inheritdoc />
    public string ProjectionName => "OrderSummary";

    /// <summary>Creates the read model from the first event of the stream.</summary>
    /// <param name="domainEvent">The creation event.</param>
    /// <param name="context">The projection context.</param>
    /// <returns>A new read model in the "Created" status.</returns>
    public OrderSummaryReadModel Create(TestAggregateCreated domainEvent, ProjectionContext context) => new()
    {
        Id = context.StreamId,
        Name = domainEvent.Name,
        Status = "Created",
        LastSequence = context.SequenceNumber
    };

    /// <summary>Adds the item amount to the totals.</summary>
    /// <param name="domainEvent">The item-added event.</param>
    /// <param name="current">The current read model.</param>
    /// <param name="context">The projection context.</param>
    /// <returns>The updated read model.</returns>
    public OrderSummaryReadModel Apply(TestItemAdded domainEvent, OrderSummaryReadModel current, ProjectionContext context)
    {
        current.Total += domainEvent.Amount;
        current.ItemCount++;
        current.LastSequence = context.SequenceNumber;
        return current;
    }

    /// <summary>Marks the read model as completed.</summary>
    /// <param name="domainEvent">The completion event.</param>
    /// <param name="current">The current read model.</param>
    /// <param name="context">The projection context.</param>
    /// <returns>The updated read model.</returns>
    public OrderSummaryReadModel Apply(TestAggregateCompleted domainEvent, OrderSummaryReadModel current, ProjectionContext context)
    {
        current.Status = "Completed";
        current.LastSequence = context.SequenceNumber;
        return current;
    }
}
