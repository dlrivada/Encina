using Encina.DomainModeling;
using Encina.EntityFrameworkCore.DomainEvents;
using Encina.TestInfrastructure.Fixtures.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Xunit;

namespace Encina.IntegrationTests.Infrastructure.EntityFrameworkCore.DomainEvents;

/// <summary>
/// Proves <see cref="DomainEventDispatcherInterceptor"/> end to end on a real database (#2863): a real
/// <see cref="IEncina"/> publishes the events of the aggregates a real <c>SaveChangesAsync</c> persisted. Each
/// provider's derived class supplies its shared <c>EFCore-*</c> collection fixture and its provider configuration.
/// </summary>
public abstract class DomainEventDispatcherInterceptorEFTestsBase : IAsyncLifetime
{
    /// <summary>Gets the shared collection fixture of the provider.</summary>
    protected abstract IEFCoreFixture Fixture { get; }

    /// <summary>
    /// Configures the provider on <paramref name="options"/>. The retrying execution strategy stays off: the
    /// scenarios open their own transactions.
    /// </summary>
    /// <param name="options">The options builder.</param>
    protected abstract void UseDatabase(DbContextOptionsBuilder options);

    /// <inheritdoc />
    public async ValueTask InitializeAsync()
    {
        await Fixture.ClearAllDataAsync();
        await Fixture.EnsureSchemaCreatedAsync<DomainEventTestDbContext>();

        // ClearAllDataAsync only clears the base schema of the fixture, not this test's own table.
        await ClearOwnTableAsync();
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync() => await ClearOwnTableAsync();

    [Fact]
    public async Task SaveChanges_PublishesTheEventsOfTheSavedAggregateInRaiseOrderAndClearsThem()
    {
        await using var harness = Build();
        var aggregate = NewAggregate();
        var first = new NoteRaised(aggregate.Id, 0);
        var second = new NoteRaised(aggregate.Id, 1);
        aggregate.Raise(first);
        aggregate.Raise(second);

        await using (var context = harness.CreateContext())
        {
            context.Aggregates.Add(aggregate);
            await context.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        harness.Recorder.Events.ShouldBe([first, second]);
        aggregate.DomainEvents.ShouldBeEmpty();
        (await ExistsAsync(aggregate.Id)).ShouldBeTrue();
    }

    [Fact]
    public async Task SaveChanges_KeepsTheEventsOnTheAggregateWhenClearingIsDisabled()
    {
        await using var harness = Build(options => options.ClearEventsAfterDispatch = false);
        var aggregate = NewAggregate();
        var raised = new NoteRaised(aggregate.Id, 0);
        aggregate.Raise(raised);

        await using (var context = harness.CreateContext())
        {
            context.Aggregates.Add(aggregate);
            await context.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        harness.Recorder.Events.ShouldBe([raised]);
        aggregate.DomainEvents.ShouldBe([raised]);
    }

    [Fact]
    public async Task FailedAsyncSave_DispatchesNothingAndLeaksNothingIntoTheNextSave()
    {
        await using var harness = Build();
        var existing = NewAggregate("duplicate-" + Guid.NewGuid().ToString("N"));
        await using (var seed = harness.CreateContext())
        {
            seed.Aggregates.Add(existing);
            await seed.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        var clashing = new DomainEventTestAggregate(Guid.NewGuid(), existing.Name);
        clashing.Raise(new NoteRaised(clashing.Id, 0));
        await using (var failing = harness.CreateContext())
        {
            failing.Aggregates.Add(clashing);
            await Should.ThrowAsync<DbUpdateException>(
                async () => await failing.SaveChangesAsync(TestContext.Current.CancellationToken));
        }

        harness.Recorder.Events.ShouldBeEmpty();

        // A save without events must not dispatch the events the failed save collected.
        var silent = NewAggregate();
        await using (var withoutEvents = harness.CreateContext())
        {
            withoutEvents.Aggregates.Add(silent);
            await withoutEvents.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        harness.Recorder.Events.ShouldBeEmpty();

        var other = NewAggregate();
        var otherEvent = new NoteRaised(other.Id, 0);
        other.Raise(otherEvent);
        await using (var succeeding = harness.CreateContext())
        {
            succeeding.Aggregates.Add(other);
            await succeeding.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        harness.Recorder.Events.ShouldBe([otherEvent]);
    }

    [Fact(Skip = "Known defect: a failed synchronous SaveChanges leaves the interceptor's pre-collected events in its AsyncLocal and the next save dispatches them; see #3245 (SaveChangesFailed is not overridden). Remove the Skip when it is fixed.")]
    public async Task FailedSynchronousSave_DispatchesNothingAndLeaksNothingIntoTheNextSave()
    {
        // The synchronous SaveChanges has no async frame that would discard the interceptor's AsyncLocal state,
        // so this is the path where a leak from a failed save would show.
        await using var harness = Build();
        var existing = NewAggregate("duplicate-" + Guid.NewGuid().ToString("N"));
        await using (var seed = harness.CreateContext())
        {
            seed.Aggregates.Add(existing);
            await seed.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        var clashing = new DomainEventTestAggregate(Guid.NewGuid(), existing.Name);
        clashing.Raise(new NoteRaised(clashing.Id, 0));
        await using (var failing = harness.CreateContext())
        {
            failing.Aggregates.Add(clashing);
            Should.Throw<DbUpdateException>(() => failing.SaveChanges());
        }

        harness.Recorder.Events.ShouldBeEmpty();

        var silent = NewAggregate();
        await using (var withoutEvents = harness.CreateContext())
        {
            withoutEvents.Aggregates.Add(silent);
            withoutEvents.SaveChanges();
        }

        harness.Recorder.Events.ShouldBeEmpty();
    }

    [Fact]
    public async Task ExplicitTransaction_DispatchesOnSaveChangesEvenWhenTheTransactionLaterRollsBack()
    {
        await using var harness = Build();
        var aggregate = NewAggregate();
        var raised = new NoteRaised(aggregate.Id, 0);
        aggregate.Raise(raised);

        await using (var context = harness.CreateContext())
        {
            await using var transaction = await context.Database.BeginTransactionAsync(TestContext.Current.CancellationToken);
            context.Aggregates.Add(aggregate);
            await context.SaveChangesAsync(TestContext.Current.CancellationToken);

            harness.Recorder.Events.ShouldBe([raised]);

            await transaction.RollbackAsync(TestContext.Current.CancellationToken);
        }

        harness.Recorder.Events.ShouldBe([raised]);
        (await ExistsAsync(aggregate.Id)).ShouldBeFalse();
    }

    [Theory]
    [InlineData(HandlerBehavior.ReturnLeft, "test.domain-event-handler")]
    [InlineData(HandlerBehavior.Throw, EncinaErrorCodes.NotificationException)]
    public async Task FailingHandler_WithStopOnFirstError_ThrowsAfterPersistingAndKeepsTheEvents(
        HandlerBehavior behavior,
        string expectedErrorCode)
    {
        await using var harness = Build(options => options.StopOnFirstError = true, behavior);
        var aggregate = NewAggregate();
        var before = new NoteRaised(aggregate.Id, 0);
        var poison = new PoisonRaised(aggregate.Id);
        var after = new NoteRaised(aggregate.Id, 1);
        aggregate.Raise(before);
        aggregate.Raise(poison);
        aggregate.Raise(after);

        DomainEventDispatchException thrown;
        await using (var context = harness.CreateContext())
        {
            context.Aggregates.Add(aggregate);
            thrown = await Should.ThrowAsync<DomainEventDispatchException>(
                async () => await context.SaveChangesAsync(TestContext.Current.CancellationToken));
        }

        thrown.DomainEvent.ShouldBe(poison);
        // The dispatcher turns a throwing handler into a Left, so both behaviors reach the interceptor as an error.
        thrown.ErrorCode.ShouldBe(expectedErrorCode);
        harness.Recorder.Events.ShouldBe([before, poison]);
        aggregate.DomainEvents.Count.ShouldBe(3);
        (await ExistsAsync(aggregate.Id)).ShouldBeTrue();
    }

    [Theory]
    [InlineData(HandlerBehavior.ReturnLeft)]
    [InlineData(HandlerBehavior.Throw)]
    public async Task FailingHandler_WithoutStopOnFirstError_AttemptsEveryEventAndClearsThem(HandlerBehavior behavior)
    {
        await using var harness = Build(options => options.StopOnFirstError = false, behavior);
        var aggregate = NewAggregate();
        var before = new NoteRaised(aggregate.Id, 0);
        var poison = new PoisonRaised(aggregate.Id);
        var after = new NoteRaised(aggregate.Id, 1);
        aggregate.Raise(before);
        aggregate.Raise(poison);
        aggregate.Raise(after);

        await using (var context = harness.CreateContext())
        {
            context.Aggregates.Add(aggregate);
            await context.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        harness.Recorder.Events.ShouldBe([before, poison, after]);
        aggregate.DomainEvents.ShouldBeEmpty();
        (await ExistsAsync(aggregate.Id)).ShouldBeTrue();
    }

    [Fact]
    public async Task SeveralAggregatesInOneSave_PublishEveryEventExactlyOnceKeepingEachAggregatesOrder()
    {
        await using var harness = Build();
        var aggregates = Enumerable.Range(0, 3).Select(_ => NewAggregate()).ToList();
        var raised = new List<NoteRaised>();
        foreach (var aggregate in aggregates)
        {
            for (var sequence = 0; sequence < 3; sequence++)
            {
                var note = new NoteRaised(aggregate.Id, sequence);
                aggregate.Raise(note);
                raised.Add(note);
            }
        }

        await using (var context = harness.CreateContext())
        {
            context.Aggregates.AddRange(aggregates);
            await context.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        var published = harness.Recorder.Events.Cast<NoteRaised>().ToList();
        published.Count.ShouldBe(raised.Count);
        published.Select(e => e.EventId).Distinct().Count().ShouldBe(raised.Count);
        published.ShouldBe(raised, ignoreOrder: true);
        foreach (var aggregate in aggregates)
        {
            published.Where(e => e.AggregateId == aggregate.Id).Select(e => e.Sequence).ShouldBe([0, 1, 2]);
            aggregate.DomainEvents.ShouldBeEmpty();
        }
    }

    private static DomainEventTestAggregate NewAggregate(string? name = null) =>
        new(Guid.NewGuid(), name ?? "aggregate-" + Guid.NewGuid().ToString("N"));

    private Harness Build(Action<DomainEventDispatcherOptions>? configure = null, HandlerBehavior poison = HandlerBehavior.Succeed)
    {
        var recorder = new DomainEventRecorder();
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddEncina();
        services.AddScoped<INotificationHandler<NoteRaised>>(_ => new RecordingHandler<NoteRaised>(recorder, HandlerBehavior.Succeed));
        services.AddScoped<INotificationHandler<PoisonRaised>>(_ => new RecordingHandler<PoisonRaised>(recorder, poison));

        // IEncina is scoped and the interceptor resolves it from this provider, so scope validation stays off.
        var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true });

        // Built once per harness: every context reuses the options and so the EF internal service provider.
        var builder = new DbContextOptionsBuilder<DomainEventTestDbContext>();
        UseDatabase(builder);
        builder.UseDomainEventDispatcher(provider, configure);

        return new Harness(provider, builder.Options, recorder);
    }

    private async Task<bool> ExistsAsync(Guid id)
    {
        await using var verify = Fixture.CreateDbContext<DomainEventTestDbContext>();
        return await verify.Aggregates.AsNoTracking().AnyAsync(a => a.Id == id);
    }

    private async Task ClearOwnTableAsync()
    {
        await using var context = Fixture.CreateDbContext<DomainEventTestDbContext>();
        await context.Aggregates.ExecuteDeleteAsync();
    }

    private sealed class Harness(
        ServiceProvider provider,
        DbContextOptions<DomainEventTestDbContext> options,
        DomainEventRecorder recorder) : IAsyncDisposable
    {
        public DomainEventRecorder Recorder { get; } = recorder;

        public DomainEventTestDbContext CreateContext() => new(options);

        public ValueTask DisposeAsync() => provider.DisposeAsync();
    }
}
