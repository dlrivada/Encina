using Encina.Marten;
using Encina.Marten.Projections;
using JasperFx.Events;
using LanguageExt;
using Marten;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
using Shouldly;
using static LanguageExt.Prelude;
using TestAggregate = Encina.UnitTests.Marten.MartenAggregateRepositoryTests.TestAggregate;
using TestEvent = Encina.UnitTests.Marten.MartenAggregateRepositoryTests.TestEvent;

namespace Encina.UnitTests.Marten;

/// <summary>
/// Verifies that <see cref="MartenAggregateRepository{TAggregate}"/> hands the events it has just
/// persisted to the inline projection dispatcher (issue #1095).
/// </summary>
public sealed class MartenAggregateRepositoryProjectionTests
{
    private readonly IDocumentSession _session = Substitute.For<IDocumentSession>();
    private readonly IInlineProjectionDispatcher _dispatcher = Substitute.For<IInlineProjectionDispatcher>();
    private readonly EncinaMartenOptions _options = new()
    {
        Metadata = { CorrelationIdEnabled = false, CausationIdEnabled = false, HeadersEnabled = false }
    };

    public MartenAggregateRepositoryProjectionTests()
    {
        _session.DocumentStore.Returns(Substitute.For<IDocumentStore>());
        _dispatcher
            .DispatchManyAsync(Arg.Any<IEnumerable<(object Event, ProjectionContext Context)>>(), Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, Unit>(Unit.Default));
    }

    private MartenAggregateRepository<TestAggregate> CreateSut(IInlineProjectionDispatcher? dispatcher) =>
        new(
            _session,
            Substitute.For<IRequestContext>(),
            NullLogger<MartenAggregateRepository<TestAggregate>>.Instance,
            Options.Create(_options),
            projectionDispatcher: dispatcher);

    /// <summary>Simulates the persisted stream: one envelope per version, as the store would return after the save.</summary>
    private void PersistedStreamHas(Guid streamId, params long[] versions)
    {
        var envelopes = versions
            .Select(v => (IEvent)new Event<TestEvent>(new TestEvent(streamId)) { StreamId = streamId, Version = v, Sequence = 100 + v })
            .ToArray();
        _session.Events
            .FetchStreamAsync(streamId, Arg.Any<long>(), Arg.Any<DateTimeOffset?>(), Arg.Any<long>(), Arg.Any<CancellationToken>())
            .Returns(envelopes);
    }

    [Fact]
    public async Task SaveAsync_WithoutDispatcher_StillSaves()
    {
        var aggregate = new TestAggregate();
        aggregate.DoSomething();
        var sut = CreateSut(null);

        var result = await sut.SaveAsync(aggregate);

        result.IsRight.ShouldBeTrue();
        await _session.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SaveAsync_WithDispatcher_DispatchesThePersistedEnvelopes_AfterSaveChanges()
    {
        var aggregate = new TestAggregate();
        aggregate.DoSomething();
        aggregate.DoSomething();
        PersistedStreamHas(aggregate.Id, 1, 2);
        List<(object Event, ProjectionContext Context)>? captured = null;
        _dispatcher
            .DispatchManyAsync(Arg.Do<IEnumerable<(object Event, ProjectionContext Context)>>(items => captured = [.. items]), Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, Unit>(Unit.Default));
        var sut = CreateSut(_dispatcher);

        var result = await sut.SaveAsync(aggregate);

        result.IsRight.ShouldBeTrue();
        Received.InOrder(async () =>
        {
            await _session.SaveChangesAsync(Arg.Any<CancellationToken>());
            await _session.Events.FetchStreamAsync(aggregate.Id, Arg.Any<long>(), Arg.Any<DateTimeOffset?>(), 1, Arg.Any<CancellationToken>());
            await _dispatcher.DispatchManyAsync(Arg.Any<IEnumerable<(object Event, ProjectionContext Context)>>(), Arg.Any<CancellationToken>());
        });
        captured.ShouldNotBeNull();
        captured.Select(static c => c.Context.SequenceNumber).ShouldBe([1L, 2L]);
        captured.Select(static c => c.Context.GlobalPosition).ShouldBe([101L, 102L]);
        captured.ShouldAllBe(c => c.Context.StreamId == aggregate.Id);
        aggregate.UncommittedEvents.ShouldBeEmpty();
    }

    [Fact]
    public async Task SaveAsync_ExistingStream_FetchesOnlyTheEventsAfterThePreviousVersion()
    {
        var aggregate = new TestAggregate();
        aggregate.DoSomething();
        aggregate.DoSomething();
        aggregate.ClearUncommittedEvents(); // simulate an aggregate loaded at version 2
        aggregate.DoSomething();
        PersistedStreamHas(aggregate.Id, 3);
        var sut = CreateSut(_dispatcher);

        await sut.SaveAsync(aggregate);

        await _session.Events.Received(1).FetchStreamAsync(aggregate.Id, Arg.Any<long>(), Arg.Any<DateTimeOffset?>(), 3, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateAsync_WithDispatcher_DispatchesTheStreamEvents()
    {
        var aggregate = new TestAggregate();
        aggregate.DoSomething();
        PersistedStreamHas(aggregate.Id, 1);
        List<(object Event, ProjectionContext Context)>? captured = null;
        _dispatcher
            .DispatchManyAsync(Arg.Do<IEnumerable<(object Event, ProjectionContext Context)>>(items => captured = [.. items]), Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, Unit>(Unit.Default));
        var sut = CreateSut(_dispatcher);

        var result = await sut.CreateAsync(aggregate);

        result.IsRight.ShouldBeTrue();
        await _session.Events.Received(1).FetchStreamAsync(aggregate.Id, Arg.Any<long>(), Arg.Any<DateTimeOffset?>(), 1, Arg.Any<CancellationToken>());
        captured.ShouldNotBeNull();
        captured.Single().Context.SequenceNumber.ShouldBe(1);
        captured.Single().Context.StreamId.ShouldBe(aggregate.Id);
    }

    [Fact]
    public async Task SaveAsync_InlineProjectionsDisabled_DoesNotDispatch()
    {
        _options.Projections.UseInlineProjections = false;
        var aggregate = new TestAggregate();
        aggregate.DoSomething();
        var sut = CreateSut(_dispatcher);

        var result = await sut.SaveAsync(aggregate);

        result.IsRight.ShouldBeTrue();
        await _dispatcher.DidNotReceiveWithAnyArgs().DispatchManyAsync(default!, default);
    }

    [Fact]
    public async Task SaveAsync_ProjectionFails_ThrowOnProjectionErrorTrue_ReturnsLeftButEventsAreCommitted()
    {
        _options.Projections.ThrowOnProjectionError = true;
        var aggregate = new TestAggregate();
        aggregate.DoSomething();
        PersistedStreamHas(aggregate.Id, 1);
        _dispatcher
            .DispatchManyAsync(Arg.Any<IEnumerable<(object Event, ProjectionContext Context)>>(), Arg.Any<CancellationToken>())
            .Returns(Left<EncinaError, Unit>(EncinaErrors.Create("projection.failed", "boom")));
        var sut = CreateSut(_dispatcher);

        var result = await sut.SaveAsync(aggregate);

        result.IsLeft.ShouldBeTrue();
        await _session.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        aggregate.UncommittedEvents.ShouldBeEmpty("the events were persisted; a retry must not append them again");
    }

    [Fact]
    public async Task SaveAsync_ProjectionFails_ThrowOnProjectionErrorFalse_ReturnsRight()
    {
        var aggregate = new TestAggregate();
        aggregate.DoSomething();
        PersistedStreamHas(aggregate.Id, 1);
        _dispatcher
            .DispatchManyAsync(Arg.Any<IEnumerable<(object Event, ProjectionContext Context)>>(), Arg.Any<CancellationToken>())
            .Returns(Left<EncinaError, Unit>(EncinaErrors.Create("projection.failed", "boom")));
        var sut = CreateSut(_dispatcher);

        var result = await sut.SaveAsync(aggregate);

        result.IsRight.ShouldBeTrue();
    }

    [Fact]
    public async Task SaveAsync_SaveChangesFails_DoesNotDispatch()
    {
        _session.SaveChangesAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromException(new InvalidOperationException("db down")));
        var aggregate = new TestAggregate();
        aggregate.DoSomething();
        var sut = CreateSut(_dispatcher);

        var result = await sut.SaveAsync(aggregate);

        result.IsLeft.ShouldBeTrue();
        await _dispatcher.DidNotReceiveWithAnyArgs().DispatchManyAsync(default!, default);
    }
}
