using Encina.Marten.Projections;
using JasperFx.Events;
using LanguageExt;
using Marten;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging.Testing;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Shouldly;
using static LanguageExt.Prelude;

namespace Encina.UnitTests.Marten.Projections;

/// <summary>
/// Tests for <see cref="InlineProjectionRelay"/>, the piece that hands persisted events to the
/// inline projection dispatcher after an aggregate save (issue #1095).
/// </summary>
public sealed class InlineProjectionRelayTests
{
    private static readonly DateTimeOffset Persisted = new(2026, 9, 22, 10, 30, 0, TimeSpan.Zero);

    private readonly IDocumentSession _session = Substitute.For<IDocumentSession>();
    private readonly IInlineProjectionDispatcher _dispatcher = Substitute.For<IInlineProjectionDispatcher>();
    private readonly ProjectionOptions _options = new();

    private InlineProjectionRelay CreateSut(IInlineProjectionDispatcher? dispatcher) =>
        new(_session, dispatcher, _options, NullLogger.Instance);

    private void StreamHas(Guid streamId, params IEvent[] envelopes)
    {
        _session.Events
            .FetchStreamAsync(streamId, Arg.Any<long>(), Arg.Any<DateTimeOffset?>(), Arg.Any<long>(), Arg.Any<CancellationToken>())
            .Returns(envelopes);
    }

    private static Event<T> Envelope<T>(Guid streamId, long version, T data, long sequence = 0) where T : notnull =>
        new(data)
        {
            StreamId = streamId,
            Version = version,
            Sequence = sequence,
            Timestamp = Persisted,
            CorrelationId = "corr-1",
            CausationId = "cause-1",
        };

    [Fact]
    public void Constructor_NullSession_ThrowsArgumentNullException()
    {
        var act = () => new InlineProjectionRelay(null!, _dispatcher, _options, NullLogger.Instance);
        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("session");
    }

    [Fact]
    public void Constructor_NullOptions_ThrowsArgumentNullException()
    {
        var act = () => new InlineProjectionRelay(_session, _dispatcher, null!, NullLogger.Instance);
        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("options");
    }

    [Fact]
    public void IsActive_WithoutDispatcher_IsFalse()
    {
        CreateSut(null).IsActive.ShouldBeFalse();
    }

    [Fact]
    public void IsActive_WithDispatcherButInlineDisabled_IsFalse()
    {
        _options.UseInlineProjections = false;

        CreateSut(_dispatcher).IsActive.ShouldBeFalse();
    }

    [Fact]
    public async Task ProjectAsync_WithoutDispatcher_ReturnsRightWithoutReadingTheStream()
    {
        var sut = CreateSut(null);

        var result = await sut.ProjectAsync("Agg", Guid.NewGuid(), 0, CancellationToken.None);

        result.IsRight.ShouldBeTrue();
        await _session.Events.DidNotReceiveWithAnyArgs().FetchStreamAsync(Guid.Empty, 0, null, 0, CancellationToken.None);
    }

    [Fact]
    public async Task ProjectAsync_InlineDisabled_DoesNotDispatch()
    {
        _options.UseInlineProjections = false;
        var sut = CreateSut(_dispatcher);

        var result = await sut.ProjectAsync("Agg", Guid.NewGuid(), 0, CancellationToken.None);

        result.IsRight.ShouldBeTrue();
        await _dispatcher.DidNotReceiveWithAnyArgs().DispatchManyAsync(default!, default);
    }

    [Fact]
    public async Task ProjectAsync_NoEventsAfterTheVersion_DoesNotDispatch()
    {
        var streamId = Guid.NewGuid();
        StreamHas(streamId);
        var sut = CreateSut(_dispatcher);

        var result = await sut.ProjectAsync("Agg", streamId, 3, CancellationToken.None);

        result.IsRight.ShouldBeTrue();
        await _dispatcher.DidNotReceiveWithAnyArgs().DispatchManyAsync(default!, default);
    }

    [Fact]
    public async Task ProjectAsync_BuildsEachContextFromThePersistedEnvelope()
    {
        var streamId = Guid.NewGuid();
        var first = new FirstEvent();
        var second = new SecondEvent();
        StreamHas(streamId, Envelope(streamId, 6, first, sequence: 41), Envelope(streamId, 7, second, sequence: 42));
        List<(object Event, ProjectionContext Context)>? captured = null;
        _dispatcher
            .DispatchManyAsync(Arg.Do<IEnumerable<(object Event, ProjectionContext Context)>>(items => captured = [.. items]), Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, Unit>(Unit.Default));
        var sut = CreateSut(_dispatcher);

        var result = await sut.ProjectAsync("Agg", streamId, 5, CancellationToken.None);

        result.IsRight.ShouldBeTrue();
        await _session.Events.Received(1).FetchStreamAsync(streamId, Arg.Any<long>(), Arg.Any<DateTimeOffset?>(), 6, Arg.Any<CancellationToken>());
        captured.ShouldNotBeNull();
        captured.Count.ShouldBe(2);
        captured[0].Event.ShouldBeSameAs(first);
        captured[0].Context.StreamId.ShouldBe(streamId);
        captured[0].Context.SequenceNumber.ShouldBe(6);
        captured[0].Context.GlobalPosition.ShouldBe(41);
        captured[0].Context.Timestamp.ShouldBe(Persisted.UtcDateTime);
        captured[0].Context.CorrelationId.ShouldBe("corr-1");
        captured[0].Context.CausationId.ShouldBe("cause-1");
        captured[1].Event.ShouldBeSameAs(second);
        captured[1].Context.SequenceNumber.ShouldBe(7);
        captured[1].Context.GlobalPosition.ShouldBe(42);
    }

    [Fact]
    public async Task ProjectAsync_IgnoresEnvelopesAtOrBelowTheVersionBeforeAppend()
    {
        var streamId = Guid.NewGuid();
        StreamHas(streamId, Envelope(streamId, 2, new FirstEvent()), Envelope(streamId, 3, new SecondEvent()));
        List<(object Event, ProjectionContext Context)>? captured = null;
        _dispatcher
            .DispatchManyAsync(Arg.Do<IEnumerable<(object Event, ProjectionContext Context)>>(items => captured = [.. items]), Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, Unit>(Unit.Default));
        var sut = CreateSut(_dispatcher);

        await sut.ProjectAsync("Agg", streamId, 2, CancellationToken.None);

        captured.ShouldNotBeNull();
        captured.Single().Context.SequenceNumber.ShouldBe(3);
    }

    [Fact]
    public async Task ProjectAsync_DispatcherFails_ThrowOnProjectionErrorFalse_ReturnsRight()
    {
        var streamId = Guid.NewGuid();
        StreamHas(streamId, Envelope(streamId, 1, new FirstEvent()));
        _options.ThrowOnProjectionError = false;
        _dispatcher
            .DispatchManyAsync(Arg.Any<IEnumerable<(object Event, ProjectionContext Context)>>(), Arg.Any<CancellationToken>())
            .Returns(Left<EncinaError, Unit>(EncinaErrors.Create("projection.failed", "boom")));
        var sut = CreateSut(_dispatcher);

        var result = await sut.ProjectAsync("Agg", streamId, 0, CancellationToken.None);

        result.IsRight.ShouldBeTrue();
    }

    [Fact]
    public async Task ProjectAsync_DispatcherFails_ThrowOnProjectionErrorFalse_LogsOnlyTheErrorCodeNotTheMessage()
    {
        // Arrange - a distinctive sentinel stands in for data that must never leave the process
        // through structured logs (AGENTS.md #3: EncinaError.Message never reaches logs; #1328).
        const string sentinel = "SENTINEL-do-not-log-4f2a";
        var streamId = Guid.NewGuid();
        StreamHas(streamId, Envelope(streamId, 1, new FirstEvent()));
        _options.ThrowOnProjectionError = false;
        _dispatcher
            .DispatchManyAsync(Arg.Any<IEnumerable<(object Event, ProjectionContext Context)>>(), Arg.Any<CancellationToken>())
            .Returns(Left<EncinaError, Unit>(EncinaErrors.Create("test.projection.error", $"Projection failed: {sentinel}")));
        var logger = new FakeLogger();
        var sut = new InlineProjectionRelay(_session, _dispatcher, _options, logger);

        // Act
        var result = await sut.ProjectAsync("Agg", streamId, 0, CancellationToken.None);

        // Assert
        result.IsRight.ShouldBeTrue();
        var logs = logger.Collector.GetSnapshot();
        logs.ShouldContain(r => r.Message.Contains("test.projection.error"));
        logs.ShouldAllBe(r => !r.Message.Contains(sentinel));
    }

    [Fact]
    public async Task ProjectAsync_DispatcherFails_ThrowOnProjectionErrorTrue_ReturnsTheProjectionError()
    {
        var streamId = Guid.NewGuid();
        StreamHas(streamId, Envelope(streamId, 1, new FirstEvent()));
        _options.ThrowOnProjectionError = true;
        _dispatcher
            .DispatchManyAsync(Arg.Any<IEnumerable<(object Event, ProjectionContext Context)>>(), Arg.Any<CancellationToken>())
            .Returns(Left<EncinaError, Unit>(EncinaErrors.Create("projection.failed", "boom")));
        var sut = CreateSut(_dispatcher);

        var result = await sut.ProjectAsync("Agg", streamId, 0, CancellationToken.None);

        result.IsLeft.ShouldBeTrue();
        result.Match(
            Right: _ => throw new InvalidOperationException("Expected Left"),
            Left: left => left.Message.ShouldBe("boom"));
    }

    [Fact]
    public async Task ProjectAsync_DispatcherThrows_ThrowOnProjectionErrorTrue_ReturnsApplyFailedNotAnException()
    {
        var streamId = Guid.NewGuid();
        StreamHas(streamId, Envelope(streamId, 1, new FirstEvent()));
        _options.ThrowOnProjectionError = true;
        _dispatcher
            .DispatchManyAsync(Arg.Any<IEnumerable<(object Event, ProjectionContext Context)>>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("dispatcher exploded"));
        var sut = CreateSut(_dispatcher);

        var result = await sut.ProjectAsync("Agg", streamId, 0, CancellationToken.None);

        result.IsLeft.ShouldBeTrue();
        result.Match(
            Right: _ => throw new InvalidOperationException("Expected Left"),
            Left: left => left.Message.ShouldContain("Failed to dispatch inline projections"));
    }

    [Fact]
    public async Task ProjectAsync_ReadingTheStreamThrows_ThrowOnProjectionErrorFalse_ReturnsRight()
    {
        var streamId = Guid.NewGuid();
        _session.Events
            .FetchStreamAsync(streamId, Arg.Any<long>(), Arg.Any<DateTimeOffset?>(), Arg.Any<long>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("db down"));
        var sut = CreateSut(_dispatcher);

        var result = await sut.ProjectAsync("Agg", streamId, 0, CancellationToken.None);

        result.IsRight.ShouldBeTrue();
    }

    [Fact]
    public async Task ProjectAsync_Cancelled_Propagates()
    {
        var streamId = Guid.NewGuid();
        _session.Events
            .FetchStreamAsync(streamId, Arg.Any<long>(), Arg.Any<DateTimeOffset?>(), Arg.Any<long>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new OperationCanceledException());
        var sut = CreateSut(_dispatcher);

        await Should.ThrowAsync<OperationCanceledException>(() => sut.ProjectAsync("Agg", streamId, 0, CancellationToken.None));
    }

    private sealed record FirstEvent;
    private sealed record SecondEvent;
}
