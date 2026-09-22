using Encina.Marten.Projections;
using LanguageExt;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Time.Testing;
using NSubstitute;
using Shouldly;
using static LanguageExt.Prelude;

namespace Encina.UnitTests.Marten.Projections;

/// <summary>
/// Tests for <see cref="InlineProjectionRelay"/>, the piece that hands persisted events to the
/// inline projection dispatcher after an aggregate save (issue #1095).
/// </summary>
public sealed class InlineProjectionRelayTests
{
    private static readonly DateTimeOffset FixedNow = new(2026, 9, 22, 10, 30, 0, TimeSpan.Zero);

    private readonly IInlineProjectionDispatcher _dispatcher = Substitute.For<IInlineProjectionDispatcher>();
    private readonly ProjectionOptions _options = new();
    private readonly FakeTimeProvider _time = new(FixedNow);

    private InlineProjectionRelay CreateSut(IInlineProjectionDispatcher? dispatcher) =>
        new(dispatcher, _options, _time, NullLogger.Instance);

    [Fact]
    public void Constructor_NullOptions_ThrowsArgumentNullException()
    {
        var act = () => new InlineProjectionRelay(_dispatcher, null!, _time, NullLogger.Instance);
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
    public async Task ProjectAsync_WithoutDispatcher_ReturnsRightWithoutDispatching()
    {
        var sut = CreateSut(null);

        var result = await sut.ProjectAsync("Agg", Guid.NewGuid(), 0, [new object()], CancellationToken.None);

        result.IsRight.ShouldBeTrue();
    }

    [Fact]
    public async Task ProjectAsync_InlineDisabled_DoesNotDispatch()
    {
        _options.UseInlineProjections = false;
        var sut = CreateSut(_dispatcher);

        var result = await sut.ProjectAsync("Agg", Guid.NewGuid(), 0, [new object()], CancellationToken.None);

        result.IsRight.ShouldBeTrue();
        await _dispatcher.DidNotReceiveWithAnyArgs().DispatchManyAsync(default!, default);
    }

    [Fact]
    public async Task ProjectAsync_NoEvents_DoesNotDispatch()
    {
        var sut = CreateSut(_dispatcher);

        var result = await sut.ProjectAsync("Agg", Guid.NewGuid(), 3, [], CancellationToken.None);

        result.IsRight.ShouldBeTrue();
        await _dispatcher.DidNotReceiveWithAnyArgs().DispatchManyAsync(default!, default);
    }

    [Fact]
    public async Task ProjectAsync_BuildsOneContextPerEvent_WithSequenceAfterTheVersionBeforeAppend()
    {
        var streamId = Guid.NewGuid();
        var first = new FirstEvent();
        var second = new SecondEvent();
        List<(object Event, ProjectionContext Context)>? captured = null;
        _dispatcher
            .DispatchManyAsync(Arg.Do<IEnumerable<(object Event, ProjectionContext Context)>>(items => captured = [.. items]), Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, Unit>(Unit.Default));
        var sut = CreateSut(_dispatcher);

        var result = await sut.ProjectAsync("Agg", streamId, 5, [first, second], CancellationToken.None);

        result.IsRight.ShouldBeTrue();
        captured.ShouldNotBeNull();
        captured.Count.ShouldBe(2);
        captured[0].Event.ShouldBeSameAs(first);
        captured[0].Context.StreamId.ShouldBe(streamId);
        captured[0].Context.SequenceNumber.ShouldBe(6);
        captured[0].Context.EventType.ShouldBe(nameof(FirstEvent));
        captured[0].Context.Timestamp.ShouldBe(FixedNow.UtcDateTime);
        captured[1].Event.ShouldBeSameAs(second);
        captured[1].Context.SequenceNumber.ShouldBe(7);
        captured[1].Context.EventType.ShouldBe(nameof(SecondEvent));
    }

    [Fact]
    public async Task ProjectAsync_DispatcherFails_ThrowOnProjectionErrorFalse_ReturnsRight()
    {
        _options.ThrowOnProjectionError = false;
        _dispatcher
            .DispatchManyAsync(Arg.Any<IEnumerable<(object Event, ProjectionContext Context)>>(), Arg.Any<CancellationToken>())
            .Returns(Left<EncinaError, Unit>(EncinaErrors.Create("projection.failed", "boom")));
        var sut = CreateSut(_dispatcher);

        var result = await sut.ProjectAsync("Agg", Guid.NewGuid(), 0, [new FirstEvent()], CancellationToken.None);

        result.IsRight.ShouldBeTrue();
    }

    [Fact]
    public async Task ProjectAsync_DispatcherFails_ThrowOnProjectionErrorTrue_ReturnsTheProjectionError()
    {
        _options.ThrowOnProjectionError = true;
        var error = EncinaErrors.Create("projection.failed", "boom");
        _dispatcher
            .DispatchManyAsync(Arg.Any<IEnumerable<(object Event, ProjectionContext Context)>>(), Arg.Any<CancellationToken>())
            .Returns(Left<EncinaError, Unit>(error));
        var sut = CreateSut(_dispatcher);

        var result = await sut.ProjectAsync("Agg", Guid.NewGuid(), 0, [new FirstEvent()], CancellationToken.None);

        result.IsLeft.ShouldBeTrue();
        result.Match(
            Right: _ => throw new InvalidOperationException("Expected Left"),
            Left: left => left.Message.ShouldBe("boom"));
    }

    private sealed record FirstEvent;
    private sealed record SecondEvent;
}
