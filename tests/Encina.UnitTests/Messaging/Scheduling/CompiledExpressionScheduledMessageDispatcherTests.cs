using System.Diagnostics.CodeAnalysis;
using Encina.Messaging.Scheduling;

using LanguageExt;

using NSubstitute;

using Shouldly;

using static LanguageExt.Prelude;

namespace Encina.UnitTests.Messaging.Scheduling;

/// <summary>
/// Unit tests for <see cref="CompiledExpressionScheduledMessageDispatcher"/>.
/// </summary>
[SuppressMessage("Reliability", "CA2012:Use ValueTasks correctly",
    Justification = "NSubstitute setup calls (Substitute.For<IEncina>().Send/Publish) return ValueTask for mock configuration, not for direct consumption.")]
public sealed class CompiledExpressionScheduledMessageDispatcherTests : IDisposable
{
    // Clear the static delegate cache before each test class run to ensure isolation
    public CompiledExpressionScheduledMessageDispatcherTests()
    {
        CompiledExpressionScheduledMessageDispatcher.ClearCache();
    }

    public void Dispose()
    {
        CompiledExpressionScheduledMessageDispatcher.ClearCache();
    }

    #region Test Types

    private sealed record TestCommand(int Value) : IRequest<int>;

    private sealed record TestNotification(string Message) : INotification;

    // A type that implements neither IRequest<> nor INotification
    private sealed record UnknownShape(string Data);

    #endregion

    #region Notification Dispatch

    [Fact]
    public async Task DispatchAsync_NotificationType_InvokesPublishOnEncina()
    {
        // Arrange
        var encina = Substitute.For<IEncina>();
        encina.Publish(Arg.Any<TestNotification>(), Arg.Any<CancellationToken>())
            .Returns(_ => new ValueTask<Either<EncinaError, Unit>>(Right<EncinaError, Unit>(Unit.Default)));

        var sut = new CompiledExpressionScheduledMessageDispatcher(encina);
        var notification = new TestNotification("hello");

        // Act
        var result = await sut.DispatchAsync(typeof(TestNotification), notification, CancellationToken.None);

        // Assert
        result.IsRight.ShouldBeTrue();
        await encina.Received(1).Publish(Arg.Any<TestNotification>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DispatchAsync_PublishReturnsLeft_PropagatesAsLeft()
    {
        // Arrange
        var expectedError = EncinaError.New("publish failed");
        var encina = Substitute.For<IEncina>();
        encina.Publish(Arg.Any<TestNotification>(), Arg.Any<CancellationToken>())
            .Returns(_ => new ValueTask<Either<EncinaError, Unit>>(Left<EncinaError, Unit>(expectedError)));

        var sut = new CompiledExpressionScheduledMessageDispatcher(encina);

        // Act
        var result = await sut.DispatchAsync(typeof(TestNotification), new TestNotification("fail"), CancellationToken.None);

        // Assert
        result.IsLeft.ShouldBeTrue();
        result.LeftAsEnumerable().First().Message.ShouldBe("publish failed");
    }

    #endregion

    #region Request Dispatch

    [Fact]
    public async Task DispatchAsync_RequestType_InvokesSendOnEncina()
    {
        // Arrange
        var encina = Substitute.For<IEncina>();
        encina.Send(Arg.Any<TestCommand>(), Arg.Any<CancellationToken>())
            .Returns(_ => new ValueTask<Either<EncinaError, int>>(Right<EncinaError, int>(42)));

        var sut = new CompiledExpressionScheduledMessageDispatcher(encina);
        var command = new TestCommand(42);

        // Act
        var result = await sut.DispatchAsync(typeof(TestCommand), command, CancellationToken.None);

        // Assert: success (response value discarded — processor only cares about success/failure)
        result.IsRight.ShouldBeTrue();
        await encina.Received(1).Send(Arg.Any<TestCommand>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DispatchAsync_SendReturnsLeft_PropagatesAsLeft()
    {
        // Arrange
        var expectedError = EncinaError.New("send failed");
        var encina = Substitute.For<IEncina>();
        encina.Send(Arg.Any<TestCommand>(), Arg.Any<CancellationToken>())
            .Returns(_ => new ValueTask<Either<EncinaError, int>>(Left<EncinaError, int>(expectedError)));

        var sut = new CompiledExpressionScheduledMessageDispatcher(encina);

        // Act
        var result = await sut.DispatchAsync(typeof(TestCommand), new TestCommand(1), CancellationToken.None);

        // Assert
        result.IsLeft.ShouldBeTrue();
        result.LeftAsEnumerable().First().Message.ShouldBe("send failed");
    }

    #endregion

    #region Unknown Shape

    [Fact]
    public async Task DispatchAsync_UnknownShape_ReturnsLeftWithUnknownRequestShape()
    {
        // Arrange
        var encina = Substitute.For<IEncina>();
        var sut = new CompiledExpressionScheduledMessageDispatcher(encina);

        // Act
        var result = await sut.DispatchAsync(typeof(UnknownShape), new UnknownShape("x"), CancellationToken.None);

        // Assert: Left with correct error code, NO exception thrown
        result.IsLeft.ShouldBeTrue();
        var error = result.LeftAsEnumerable().First();
        error.Message.ShouldContain("IRequest<TResponse>");
        error.Message.ShouldContain("INotification");
    }

    #endregion

    #region Cache Behavior

    [Fact]
    public async Task DispatchAsync_SameTypeTwice_CachesDelegateOnce()
    {
        // Arrange
        var encina = Substitute.For<IEncina>();
        encina.Publish(Arg.Any<TestNotification>(), Arg.Any<CancellationToken>())
            .Returns(_ => new ValueTask<Either<EncinaError, Unit>>(Right<EncinaError, Unit>(Unit.Default)));

        var sut = new CompiledExpressionScheduledMessageDispatcher(encina);
        var countBefore = CompiledExpressionScheduledMessageDispatcher.CacheCount;

        // Act
        await sut.DispatchAsync(typeof(TestNotification), new TestNotification("a"), CancellationToken.None);
        await sut.DispatchAsync(typeof(TestNotification), new TestNotification("b"), CancellationToken.None);

        // Assert: only 1 new cache entry
        var countAfter = CompiledExpressionScheduledMessageDispatcher.CacheCount;
        (countAfter - countBefore).ShouldBe(1);
    }

    [Fact]
    public async Task DispatchAsync_DifferentTypes_BuildSeparateDelegates()
    {
        // Arrange
        var encina = Substitute.For<IEncina>();
        encina.Publish(Arg.Any<TestNotification>(), Arg.Any<CancellationToken>())
            .Returns(_ => new ValueTask<Either<EncinaError, Unit>>(Right<EncinaError, Unit>(Unit.Default)));
        encina.Send(Arg.Any<TestCommand>(), Arg.Any<CancellationToken>())
            .Returns(_ => new ValueTask<Either<EncinaError, int>>(Right<EncinaError, int>(0)));

        var sut = new CompiledExpressionScheduledMessageDispatcher(encina);
        var countBefore = CompiledExpressionScheduledMessageDispatcher.CacheCount;

        // Act
        await sut.DispatchAsync(typeof(TestNotification), new TestNotification("a"), CancellationToken.None);
        await sut.DispatchAsync(typeof(TestCommand), new TestCommand(1), CancellationToken.None);
        await sut.DispatchAsync(typeof(UnknownShape), new UnknownShape("x"), CancellationToken.None);

        // Assert: 3 separate cache entries
        var countAfter = CompiledExpressionScheduledMessageDispatcher.CacheCount;
        (countAfter - countBefore).ShouldBe(3);
    }

    #endregion

    #region Cancellation Token Flow

    [Fact]
    public async Task DispatchAsync_CancellationToken_FlowsToSendCall()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        var token = cts.Token;
        CancellationToken receivedToken = default;

        var encina = Substitute.For<IEncina>();
        encina.Send(Arg.Any<TestCommand>(), Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                receivedToken = callInfo.ArgAt<CancellationToken>(1);
                return new ValueTask<Either<EncinaError, int>>(Right<EncinaError, int>(42));
            });

        var sut = new CompiledExpressionScheduledMessageDispatcher(encina);

        // Act
        await sut.DispatchAsync(typeof(TestCommand), new TestCommand(1), token);

        // Assert
        receivedToken.ShouldBe(token);
    }

    [Fact]
    public async Task DispatchAsync_CancellationToken_FlowsToPublishCall()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        var token = cts.Token;
        CancellationToken receivedToken = default;

        var encina = Substitute.For<IEncina>();
        encina.Publish(Arg.Any<TestNotification>(), Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                receivedToken = callInfo.ArgAt<CancellationToken>(1);
                return new ValueTask<Either<EncinaError, Unit>>(Right<EncinaError, Unit>(Unit.Default));
            });

        var sut = new CompiledExpressionScheduledMessageDispatcher(encina);

        // Act
        await sut.DispatchAsync(typeof(TestNotification), new TestNotification("test"), token);

        // Assert
        receivedToken.ShouldBe(token);
    }

    #endregion

    #region Argument Validation

    [Fact]
    public void DispatchAsync_NullRequestType_ThrowsArgumentNullException()
    {
        var encina = Substitute.For<IEncina>();
        var sut = new CompiledExpressionScheduledMessageDispatcher(encina);

        Should.Throw<ArgumentNullException>(
            () => sut.DispatchAsync(null!, new object(), CancellationToken.None).AsTask());
    }

    [Fact]
    public void DispatchAsync_NullRequest_ThrowsArgumentNullException()
    {
        var encina = Substitute.For<IEncina>();
        var sut = new CompiledExpressionScheduledMessageDispatcher(encina);

        Should.Throw<ArgumentNullException>(
            () => sut.DispatchAsync(typeof(TestCommand), null!, CancellationToken.None).AsTask());
    }

    #endregion
}
