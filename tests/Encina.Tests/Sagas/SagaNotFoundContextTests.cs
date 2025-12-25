using Encina.Messaging.Sagas;
using Shouldly;

namespace Encina.Tests.Sagas;

public sealed class SagaNotFoundContextTests
{
    private readonly Guid _sagaId = Guid.NewGuid();
    private const string SagaType = "OrderSaga";
    private readonly Type _messageType = typeof(TestMessage);

    [Fact]
    public void Constructor_SetsPropertiesCorrectly()
    {
        // Act
        var context = new SagaNotFoundContext(_sagaId, SagaType, _messageType);

        // Assert
        context.SagaId.ShouldBe(_sagaId);
        context.SagaType.ShouldBe(SagaType);
        context.MessageType.ShouldBe(_messageType);
        context.Action.ShouldBe(SagaNotFoundAction.None);
        context.DeadLetterReason.ShouldBeNull();
        context.WasIgnored.ShouldBeFalse();
        context.WasMovedToDeadLetter.ShouldBeFalse();
    }

    [Fact]
    public void Constructor_WithNullSagaType_ThrowsArgumentNullException()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() => new SagaNotFoundContext(_sagaId, null!, _messageType));
    }

    [Fact]
    public void Constructor_WithNullMessageType_ThrowsArgumentNullException()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() => new SagaNotFoundContext(_sagaId, SagaType, null!));
    }

    [Fact]
    public void Ignore_SetsActionToIgnored()
    {
        // Arrange
        var context = new SagaNotFoundContext(_sagaId, SagaType, _messageType);

        // Act
        context.Ignore();

        // Assert
        context.Action.ShouldBe(SagaNotFoundAction.Ignored);
        context.WasIgnored.ShouldBeTrue();
        context.WasMovedToDeadLetter.ShouldBeFalse();
    }

    [Fact]
    public async Task MoveToDeadLetterAsync_SetsActionToMovedToDeadLetter()
    {
        // Arrange
        var deadLetterCalled = false;
        string? capturedReason = null;
        Task MoveToDeadLetter(string reason, CancellationToken ct)
        {
            deadLetterCalled = true;
            capturedReason = reason;
            return Task.CompletedTask;
        }
        var context = new SagaNotFoundContext(_sagaId, SagaType, _messageType, MoveToDeadLetter);

        // Act
        await context.MoveToDeadLetterAsync("Saga not found");

        // Assert
        context.Action.ShouldBe(SagaNotFoundAction.MovedToDeadLetter);
        context.WasMovedToDeadLetter.ShouldBeTrue();
        context.WasIgnored.ShouldBeFalse();
        context.DeadLetterReason.ShouldBe("Saga not found");
        deadLetterCalled.ShouldBeTrue();
        capturedReason.ShouldBe("Saga not found");
    }

    [Fact]
    public async Task MoveToDeadLetterAsync_WithNullReason_ThrowsArgumentException()
    {
        // Arrange
        var context = new SagaNotFoundContext(_sagaId, SagaType, _messageType, (_, _) => Task.CompletedTask);

        // Act & Assert
        await Should.ThrowAsync<ArgumentException>(() => context.MoveToDeadLetterAsync(null!));
    }

    [Fact]
    public async Task MoveToDeadLetterAsync_WithEmptyReason_ThrowsArgumentException()
    {
        // Arrange
        var context = new SagaNotFoundContext(_sagaId, SagaType, _messageType, (_, _) => Task.CompletedTask);

        // Act & Assert
        await Should.ThrowAsync<ArgumentException>(() => context.MoveToDeadLetterAsync(string.Empty));
    }

    [Fact]
    public async Task MoveToDeadLetterAsync_WithWhitespaceReason_ThrowsArgumentException()
    {
        // Arrange
        var context = new SagaNotFoundContext(_sagaId, SagaType, _messageType, (_, _) => Task.CompletedTask);

        // Act & Assert
        await Should.ThrowAsync<ArgumentException>(() => context.MoveToDeadLetterAsync("   "));
    }

    [Fact]
    public async Task MoveToDeadLetterAsync_WithNoHandler_ThrowsInvalidOperationException()
    {
        // Arrange
        var context = new SagaNotFoundContext(_sagaId, SagaType, _messageType);

        // Act & Assert
        var exception = await Should.ThrowAsync<InvalidOperationException>(() => context.MoveToDeadLetterAsync("Saga not found"));
        exception.Message.ShouldContain("Dead letter handling is not configured");
    }

    [Fact]
    public async Task MoveToDeadLetterAsync_PassesCancellationToken()
    {
        // Arrange
        CancellationToken capturedToken = default;
        Task MoveToDeadLetter(string reason, CancellationToken ct)
        {
            capturedToken = ct;
            return Task.CompletedTask;
        }
        var context = new SagaNotFoundContext(_sagaId, SagaType, _messageType, MoveToDeadLetter);
        using var cts = new CancellationTokenSource();
        var token = cts.Token;

        // Act
        await context.MoveToDeadLetterAsync("test", token);

        // Assert
        capturedToken.ShouldBe(token);
    }

    private sealed class TestMessage;
}
