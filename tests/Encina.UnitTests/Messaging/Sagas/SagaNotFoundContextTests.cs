using Encina.Messaging.Sagas;
using Encina.Testing.Shouldly;
using LanguageExt;
using Shouldly;

namespace Encina.UnitTests.Messaging.Sagas;

/// <summary>
/// Unit tests for <see cref="SagaNotFoundContext"/> and <see cref="SagaNotFoundAction"/>.
/// </summary>
public sealed class SagaNotFoundContextTests
{
    #region Constructor Tests

    [Fact]
    public void Constructor_ValidParameters_SetsProperties()
    {
        // Arrange
        var sagaId = Guid.NewGuid();
        var sagaType = "OrderSaga";
        var messageType = typeof(TestMessage);

        // Act
        var context = new SagaNotFoundContext(sagaId, sagaType, messageType);

        // Assert
        context.SagaId.ShouldBe(sagaId);
        context.SagaType.ShouldBe(sagaType);
        context.MessageType.ShouldBe(messageType);
        context.Action.ShouldBe(SagaNotFoundAction.None);
        context.DeadLetterReason.ShouldBeNull();
        context.WasIgnored.ShouldBeFalse();
        context.WasMovedToDeadLetter.ShouldBeFalse();
    }

    [Fact]
    public void Constructor_NullSagaType_ThrowsArgumentNullException()
    {
        // Arrange
        var sagaId = Guid.NewGuid();

        // Act
        var act = () => new SagaNotFoundContext(sagaId, null!, typeof(TestMessage));

        // Assert
        act.ShouldThrow<ArgumentNullException>();
    }

    [Fact]
    public void Constructor_NullMessageType_ThrowsArgumentNullException()
    {
        // Arrange
        var sagaId = Guid.NewGuid();

        // Act
        var act = () => new SagaNotFoundContext(sagaId, "OrderSaga", null!);

        // Assert
        act.ShouldThrow<ArgumentNullException>();
    }

    #endregion

    #region Ignore Tests

    [Fact]
    public void Ignore_SetsActionToIgnored()
    {
        // Arrange
        var context = new SagaNotFoundContext(Guid.NewGuid(), "Saga", typeof(TestMessage));

        // Act
        context.Ignore();

        // Assert
        context.Action.ShouldBe(SagaNotFoundAction.Ignored);
        context.WasIgnored.ShouldBeTrue();
        context.WasMovedToDeadLetter.ShouldBeFalse();
    }

    #endregion

    #region MoveToDeadLetterAsync Tests

    [Fact]
    public async Task MoveToDeadLetterAsync_WithHandler_InvokesHandler()
    {
        // Arrange
        var handlerInvoked = false;
        string? capturedReason = null;

        Task Handler(string reason, CancellationToken ct)
        {
            handlerInvoked = true;
            capturedReason = reason;
            return Task.CompletedTask;
        }

        var context = new SagaNotFoundContext(Guid.NewGuid(), "Saga", typeof(TestMessage), Handler);

        // Act
        var result = await context.MoveToDeadLetterAsync("Test reason");

        // Assert
        result.ShouldBeRight();
        handlerInvoked.ShouldBeTrue();
        capturedReason.ShouldBe("Test reason");
        context.Action.ShouldBe(SagaNotFoundAction.MovedToDeadLetter);
        context.DeadLetterReason.ShouldBe("Test reason");
        context.WasMovedToDeadLetter.ShouldBeTrue();
        context.WasIgnored.ShouldBeFalse();
    }

    [Fact]
    public async Task MoveToDeadLetterAsync_NullReason_ThrowsArgumentException()
    {
        // Arrange
        var context = new SagaNotFoundContext(Guid.NewGuid(), "Saga", typeof(TestMessage), (_, _) => Task.CompletedTask);

        // Act
        var act = async () => await context.MoveToDeadLetterAsync(null!);

        // Assert
        await act.ShouldThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task MoveToDeadLetterAsync_EmptyReason_ThrowsArgumentException()
    {
        // Arrange
        var context = new SagaNotFoundContext(Guid.NewGuid(), "Saga", typeof(TestMessage), (_, _) => Task.CompletedTask);

        // Act
        var act = async () => await context.MoveToDeadLetterAsync(string.Empty);

        // Assert
        await act.ShouldThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task MoveToDeadLetterAsync_WhitespaceReason_ThrowsArgumentException()
    {
        // Arrange
        var context = new SagaNotFoundContext(Guid.NewGuid(), "Saga", typeof(TestMessage), (_, _) => Task.CompletedTask);

        // Act
        var act = async () => await context.MoveToDeadLetterAsync("   ");

        // Assert
        await act.ShouldThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task MoveToDeadLetterAsync_NoHandler_ReturnsError()
    {
        // Arrange
        var context = new SagaNotFoundContext(Guid.NewGuid(), "Saga", typeof(TestMessage));

        // Act
        var result = await context.MoveToDeadLetterAsync("Test reason");

        // Assert
        var error = result.ShouldBeLeft();
        error.Message.ShouldContain("Dead letter handling is not configured");
    }

    [Fact]
    public async Task MoveToDeadLetterAsync_PassesCancellationToken()
    {
        // Arrange
        CancellationToken capturedToken = default;

        Task Handler(string reason, CancellationToken ct)
        {
            capturedToken = ct;
            return Task.CompletedTask;
        }

        var context = new SagaNotFoundContext(Guid.NewGuid(), "Saga", typeof(TestMessage), Handler);
        using var cts = new CancellationTokenSource();
        var expectedToken = cts.Token;

        // Act
        var result = await context.MoveToDeadLetterAsync("Reason", expectedToken);

        // Assert
        result.ShouldBeRight();
        capturedToken.ShouldBe(expectedToken);
    }

    #endregion

    #region SagaNotFoundAction Enum

    [Fact]
    public void SagaNotFoundAction_Values_AreCorrect()
    {
        // Assert
        ((int)SagaNotFoundAction.None).ShouldBe(0);
        ((int)SagaNotFoundAction.Ignored).ShouldBe(1);
        ((int)SagaNotFoundAction.MovedToDeadLetter).ShouldBe(2);
    }

    #endregion

    private sealed class TestMessage { }
}
