using System.Diagnostics.CodeAnalysis;
using Encina.Marten;
using JasperFx.Events;
using LanguageExt;
using Marten;
using Marten.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging.Testing;
using Microsoft.Extensions.Options;
using NSubstitute;
using Shouldly;
using static LanguageExt.Prelude;

namespace Encina.UnitTests.Marten;

[SuppressMessage("Reliability", "CA2012:Use ValueTasks correctly", Justification = "Mock setup pattern for NSubstitute")]
public class EventPublishingPipelineBehaviorTests
{
    private readonly IDocumentSession _session;
    private readonly IEncina _encina;
    private readonly ILogger<EventPublishingPipelineBehavior<TestCommand, TestResponse>> _logger;
    private readonly IOptions<EncinaMartenOptions> _options;
    private readonly IRequestContext _requestContext;

    public EventPublishingPipelineBehaviorTests()
    {
        _session = Substitute.For<IDocumentSession>();
        _encina = Substitute.For<IEncina>();
        _logger = NullLogger<EventPublishingPipelineBehavior<TestCommand, TestResponse>>.Instance;
        _options = Options.Create(new EncinaMartenOptions());
        _requestContext = Substitute.For<IRequestContext>();
    }

    private EventPublishingPipelineBehavior<TestCommand, TestResponse> CreateSut()
    {
        return new EventPublishingPipelineBehavior<TestCommand, TestResponse>(
            _session, _encina, _logger, _options);
    }

    // Constructor null guard tests

    [Fact]
    public void Constructor_NullSession_ThrowsArgumentNullException()
    {
        var act = () => new EventPublishingPipelineBehavior<TestCommand, TestResponse>(
            null!, _encina, _logger, _options);
        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("session");
    }

    [Fact]
    public void Constructor_NullEncina_ThrowsArgumentNullException()
    {
        var act = () => new EventPublishingPipelineBehavior<TestCommand, TestResponse>(
            _session, null!, _logger, _options);
        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("encina");
    }

    [Fact]
    public void Constructor_NullLogger_ThrowsArgumentNullException()
    {
        var act = () => new EventPublishingPipelineBehavior<TestCommand, TestResponse>(
            _session, _encina, null!, _options);
        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("logger");
    }

    [Fact]
    public void Constructor_NullOptions_ThrowsArgumentNullException()
    {
        var act = () => new EventPublishingPipelineBehavior<TestCommand, TestResponse>(
            _session, _encina, _logger, null!);
        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("options");
    }

    // Handle tests

    [Fact]
    public async Task Handle_CommandFails_ReturnLeftWithoutPublishing()
    {
        // Arrange
        var sut = CreateSut();
        var error = EncinaErrors.Create("test", "command failed");
        RequestHandlerCallback<TestResponse> next = () =>
            new ValueTask<Either<EncinaError, TestResponse>>(
                Left<EncinaError, TestResponse>(error));

        // Act
        var result = await sut.Handle(new TestCommand(), _requestContext, next, CancellationToken.None);

        // Assert
        result.IsLeft.ShouldBeTrue();
        await _encina.DidNotReceive().Publish(
            Arg.Any<INotification>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_AutoPublishDisabled_DoesNotPublish()
    {
        // Arrange
        var options = Options.Create(new EncinaMartenOptions { AutoPublishDomainEvents = false });
        var sut = new EventPublishingPipelineBehavior<TestCommand, TestResponse>(
            _session, _encina, _logger, options);

        var response = new TestResponse();
        RequestHandlerCallback<TestResponse> next = () =>
            new ValueTask<Either<EncinaError, TestResponse>>(
                Right<EncinaError, TestResponse>(response));

        // Act
        var result = await sut.Handle(new TestCommand(), _requestContext, next, CancellationToken.None);

        // Assert
        result.IsRight.ShouldBeTrue();
        await _encina.DidNotReceive().Publish(
            Arg.Any<INotification>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_NoPendingEvents_ReturnsResultWithoutPublishing()
    {
        // Arrange
        var sut = CreateSut();
        var response = new TestResponse();
        RequestHandlerCallback<TestResponse> next = () =>
            new ValueTask<Either<EncinaError, TestResponse>>(
                Right<EncinaError, TestResponse>(response));

        // Act
        var result = await sut.Handle(new TestCommand(), _requestContext, next, CancellationToken.None);

        // Assert
        result.IsRight.ShouldBeTrue();
        await _encina.DidNotReceive().Publish(
            Arg.Any<INotification>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_PublishFails_LogsOnlyTheErrorCodeNotTheMessage()
    {
        // Arrange - a distinctive sentinel stands in for data that must never leave the process
        // through structured logs (AGENTS.md #3: EncinaError.Message never reaches logs; #1328).
        const string sentinel = "SENTINEL-do-not-log-4f2a";
        var error = EncinaErrors.Create("test.publish.error", $"Publish failed: {sentinel}");
        var logger = new FakeLogger<EventPublishingPipelineBehavior<TestCommand, TestResponse>>();
        var sut = new EventPublishingPipelineBehavior<TestCommand, TestResponse>(
            _session, _encina, logger, _options);

        var listeners = UseRealListeners();

        _encina.Publish(Arg.Any<INotification>(), Arg.Any<CancellationToken>())
            .Returns(new ValueTask<Either<EncinaError, Unit>>(Left<EncinaError, Unit>(error)));

        RequestHandlerCallback<TestResponse> next = async () =>
        {
            await listeners.Single().AfterCommitAsync(
                _session, CommitOf(new TestNotification("hello")), CancellationToken.None);
            return Right<EncinaError, TestResponse>(new TestResponse());
        };

        // Act
        var result = await sut.Handle(new TestCommand(), _requestContext, next, CancellationToken.None);

        // Assert
        result.IsLeft.ShouldBeTrue();
        var logs = logger.Collector.GetSnapshot();
        logs.ShouldContain(r => r.Message.Contains("test.publish.error"));
        logs.ShouldAllBe(r => !r.Message.Contains(sentinel));
    }

    // Events committed inside the handler (the aggregate repository saves the session itself)

    private List<IDocumentSessionListener> UseRealListeners()
    {
        var listeners = new List<IDocumentSessionListener>();
        _session.Listeners.Returns(listeners);
        return listeners;
    }

    private static IChangeSet CommitOf(params object[] events)
    {
        var commit = Substitute.For<IChangeSet>();
        commit.GetEvents().Returns(events.Select(e => (IEvent)new Event<object>(e)).ToList());
        return commit;
    }

    [Fact]
    public async Task Handle_EventsCommittedByTheHandler_PublishesEachOnceAndDetachesTheListener()
    {
        // Arrange
        var listeners = UseRealListeners();
        var sut = CreateSut();
        var first = new TestNotification("first");
        var second = new TestNotification("second");
        _encina.Publish(Arg.Any<INotification>(), Arg.Any<CancellationToken>())
            .Returns(new ValueTask<Either<EncinaError, Unit>>(Right<EncinaError, Unit>(unit)));

        RequestHandlerCallback<TestResponse> next = async () =>
        {
            await listeners.Single().AfterCommitAsync(_session, CommitOf(first, second, "not a notification"), CancellationToken.None);
            return Right<EncinaError, TestResponse>(new TestResponse());
        };

        // Act
        var result = await sut.Handle(new TestCommand(), _requestContext, next, CancellationToken.None);

        // Assert
        result.IsRight.ShouldBeTrue();
        await _encina.Received(1).Publish(first, Arg.Any<CancellationToken>());
        await _encina.Received(1).Publish(second, Arg.Any<CancellationToken>());
        await _encina.Received(2).Publish(Arg.Any<INotification>(), Arg.Any<CancellationToken>());
        listeners.ShouldBeEmpty();
    }

    [Fact]
    public async Task Handle_CommandFailsAfterCommit_PublishesNothingAndDetachesTheListener()
    {
        // Arrange
        var listeners = UseRealListeners();
        var sut = CreateSut();

        RequestHandlerCallback<TestResponse> next = async () =>
        {
            await listeners.Single().AfterCommitAsync(_session, CommitOf(new TestNotification("x")), CancellationToken.None);
            return Left<EncinaError, TestResponse>(EncinaErrors.Create("test", "failed"));
        };

        // Act
        var result = await sut.Handle(new TestCommand(), _requestContext, next, CancellationToken.None);

        // Assert
        result.IsLeft.ShouldBeTrue();
        await _encina.DidNotReceive().Publish(Arg.Any<INotification>(), Arg.Any<CancellationToken>());
        listeners.ShouldBeEmpty();
    }

    [Fact]
    public async Task Handle_SecondBehaviorOnTheSameSession_OnlyTheOwnerPublishesWhenItFinishes()
    {
        // Arrange
        var listeners = UseRealListeners();
        var outer = CreateSut();
        var inner = CreateSut();
        var notification = new TestNotification("shared");
        _encina.Publish(Arg.Any<INotification>(), Arg.Any<CancellationToken>())
            .Returns(new ValueTask<Either<EncinaError, Unit>>(Right<EncinaError, Unit>(unit)));

        RequestHandlerCallback<TestResponse> innerNext = async () =>
        {
            listeners.Count.ShouldBe(1);
            await listeners.Single().AfterCommitAsync(_session, CommitOf(notification), CancellationToken.None);
            return Right<EncinaError, TestResponse>(new TestResponse());
        };

        var publishedWhenInnerReturned = -1;
        RequestHandlerCallback<TestResponse> outerNext = async () =>
        {
            var innerResult = await inner.Handle(new TestCommand(), _requestContext, innerNext, CancellationToken.None);
            publishedWhenInnerReturned = _encina.ReceivedCalls().Count(c => c.GetMethodInfo().Name == nameof(IEncina.Publish));
            return innerResult;
        };

        // Act
        var result = await outer.Handle(new TestCommand(), _requestContext, outerNext, CancellationToken.None);

        // Assert
        result.IsRight.ShouldBeTrue();
        publishedWhenInnerReturned.ShouldBe(0);
        await _encina.Received(1).Publish(notification, Arg.Any<CancellationToken>());
        listeners.ShouldBeEmpty();
    }

    [Fact]
    public async Task Handle_EventsAppendedButNeverSaved_AreNotPublished()
    {
        // Arrange - the handler appends to the session without committing
        var listeners = UseRealListeners();
        var sut = CreateSut();
        var unsaved = new Event<TestNotification>(new TestNotification("unsaved"));
        _session.PendingChanges.Streams().Returns([StreamAction.Start(Guid.NewGuid(), unsaved)]);

        RequestHandlerCallback<TestResponse> next = () =>
            new ValueTask<Either<EncinaError, TestResponse>>(
                Right<EncinaError, TestResponse>(new TestResponse()));

        // Act
        var result = await sut.Handle(new TestCommand(), _requestContext, next, CancellationToken.None);

        // Assert
        result.IsRight.ShouldBeTrue();
        await _encina.DidNotReceive().Publish(Arg.Any<INotification>(), Arg.Any<CancellationToken>());
        listeners.ShouldBeEmpty();
    }

    [Fact]
    public async Task Handle_DifferentCommandTypeOnTheSameSession_PassesThroughWithoutPublishing()
    {
        // Arrange
        var listeners = UseRealListeners();
        var outer = CreateSut();
        var otherCommandBehavior = new EventPublishingPipelineBehavior<OtherCommand, TestResponse>(
            _session, _encina, NullLogger<EventPublishingPipelineBehavior<OtherCommand, TestResponse>>.Instance, _options);
        var notification = new TestNotification("shared");
        _encina.Publish(Arg.Any<INotification>(), Arg.Any<CancellationToken>())
            .Returns(new ValueTask<Either<EncinaError, Unit>>(Right<EncinaError, Unit>(unit)));

        var publishedWhenOtherReturned = -1;
        RequestHandlerCallback<TestResponse> outerNext = async () =>
        {
            await listeners.Single().AfterCommitAsync(_session, CommitOf(notification), CancellationToken.None);
            var otherResult = await otherCommandBehavior.Handle(
                new OtherCommand(),
                _requestContext,
                () => new ValueTask<Either<EncinaError, TestResponse>>(Right<EncinaError, TestResponse>(new TestResponse())),
                CancellationToken.None);
            publishedWhenOtherReturned = _encina.ReceivedCalls().Count(c => c.GetMethodInfo().Name == nameof(IEncina.Publish));
            return otherResult;
        };

        // Act
        var result = await outer.Handle(new TestCommand(), _requestContext, outerNext, CancellationToken.None);

        // Assert
        result.IsRight.ShouldBeTrue();
        publishedWhenOtherReturned.ShouldBe(0);
        await _encina.Received(1).Publish(notification, Arg.Any<CancellationToken>());
        listeners.ShouldBeEmpty();
    }

    [Fact]
    public async Task Handle_HandlerThrows_DetachesTheListenerAndPropagates()
    {
        // Arrange
        var listeners = UseRealListeners();
        var sut = CreateSut();
        RequestHandlerCallback<TestResponse> next = () => throw new InvalidOperationException("boom");

        // Act
        var act = async () => await sut.Handle(new TestCommand(), _requestContext, next, CancellationToken.None);

        // Assert
        await Should.ThrowAsync<InvalidOperationException>(act);
        listeners.ShouldBeEmpty();
    }

    [Fact]
    public async Task Handle_SecondEventFailsToPublish_StopsAtTheFailureAndReturnsLeft()
    {
        // Arrange
        var listeners = UseRealListeners();
        var sut = CreateSut();
        var first = new TestNotification("first");
        var second = new TestNotification("second");
        var third = new TestNotification("third");
        _encina.Publish(first, Arg.Any<CancellationToken>())
            .Returns(new ValueTask<Either<EncinaError, Unit>>(Right<EncinaError, Unit>(unit)));
        _encina.Publish(second, Arg.Any<CancellationToken>())
            .Returns(new ValueTask<Either<EncinaError, Unit>>(
                Left<EncinaError, Unit>(EncinaErrors.Create("test.publish.error", "SENTINEL-inner-message"))));

        RequestHandlerCallback<TestResponse> next = async () =>
        {
            await listeners.Single().AfterCommitAsync(_session, CommitOf(first, second, third), CancellationToken.None);
            return Right<EncinaError, TestResponse>(new TestResponse());
        };

        // Act
        var result = await sut.Handle(new TestCommand(), _requestContext, next, CancellationToken.None);

        // Assert
        result.IsLeft.ShouldBeTrue();
        result.IfLeft(e => e.Message.ShouldNotContain("SENTINEL-inner-message"));
        await _encina.DidNotReceive().Publish(third, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_CommittedEventPublishFails_ReturnsLeft()
    {
        // Arrange
        var listeners = UseRealListeners();
        var sut = CreateSut();
        _encina.Publish(Arg.Any<INotification>(), Arg.Any<CancellationToken>())
            .Returns(new ValueTask<Either<EncinaError, Unit>>(
                Left<EncinaError, Unit>(EncinaErrors.Create("test.publish.error", "boom"))));

        RequestHandlerCallback<TestResponse> next = async () =>
        {
            await listeners.Single().AfterCommitAsync(_session, CommitOf(new TestNotification("x")), CancellationToken.None);
            return Right<EncinaError, TestResponse>(new TestResponse());
        };

        // Act
        var result = await sut.Handle(new TestCommand(), _requestContext, next, CancellationToken.None);

        // Assert
        result.IsLeft.ShouldBeTrue();
        listeners.ShouldBeEmpty();
    }

    // Test types

    public sealed record TestCommand : ICommand<TestResponse>;
    public sealed record OtherCommand : ICommand<TestResponse>;
    public sealed record TestResponse;
    public sealed record TestNotification(string Message) : INotification;
}
