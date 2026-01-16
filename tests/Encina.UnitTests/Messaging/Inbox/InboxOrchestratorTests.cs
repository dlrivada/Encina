using Encina.Messaging.Inbox;
using LanguageExt;
using Microsoft.Extensions.Logging;

namespace Encina.UnitTests.Messaging.Inbox;

/// <summary>
/// Unit tests for InboxOrchestrator.
/// </summary>
public sealed class InboxOrchestratorTests
{
    private static readonly DateTime FixedUtcNow = new(2020, 1, 15, 12, 0, 0, DateTimeKind.Utc);

    private readonly IInboxStore _store;
    private readonly InboxOptions _options;
    private readonly ILogger<InboxOrchestrator> _logger;
    private readonly IInboxMessageFactory _messageFactory;
    private readonly InboxOrchestrator _orchestrator;

    public InboxOrchestratorTests()
    {
        _store = Substitute.For<IInboxStore>();
        _options = new InboxOptions
        {
            MaxRetries = 3,
            MessageRetentionPeriod = TimeSpan.FromDays(30)
        };
        _logger = Substitute.For<ILogger<InboxOrchestrator>>();
        _messageFactory = Substitute.For<IInboxMessageFactory>();
        _orchestrator = new InboxOrchestrator(_store, _options, _logger, _messageFactory);
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_NullStore_ThrowsArgumentNullException()
    {
        // Arrange
        var options = new InboxOptions { MaxRetries = 3 };
        var logger = Substitute.For<ILogger<InboxOrchestrator>>();
        var messageFactory = Substitute.For<IInboxMessageFactory>();

        // Act
        var act = () => new InboxOrchestrator(null!, options, logger, messageFactory);

        // Assert
        act.ShouldThrow<ArgumentNullException>().ParamName.ShouldBe("store");
    }

    [Fact]
    public void Constructor_NullOptions_ThrowsArgumentNullException()
    {
        // Arrange
        var store = Substitute.For<IInboxStore>();
        var logger = Substitute.For<ILogger<InboxOrchestrator>>();
        var messageFactory = Substitute.For<IInboxMessageFactory>();

        // Act
        var act = () => new InboxOrchestrator(store, null!, logger, messageFactory);

        // Assert
        act.ShouldThrow<ArgumentNullException>().ParamName.ShouldBe("options");
    }

    [Fact]
    public void Constructor_NullLogger_ThrowsArgumentNullException()
    {
        // Arrange
        var store = Substitute.For<IInboxStore>();
        var options = new InboxOptions { MaxRetries = 3 };
        var messageFactory = Substitute.For<IInboxMessageFactory>();

        // Act
        var act = () => new InboxOrchestrator(store, options, null!, messageFactory);

        // Assert
        act.ShouldThrow<ArgumentNullException>().ParamName.ShouldBe("logger");
    }

    [Fact]
    public void Constructor_NullMessageFactory_ThrowsArgumentNullException()
    {
        // Arrange
        var store = Substitute.For<IInboxStore>();
        var options = new InboxOptions { MaxRetries = 3 };
        var logger = Substitute.For<ILogger<InboxOrchestrator>>();

        // Act
        var act = () => new InboxOrchestrator(store, options, logger, null!);

        // Assert
        act.ShouldThrow<ArgumentNullException>().ParamName.ShouldBe("messageFactory");
    }

    #endregion

    #region ValidateMessageId Tests

    [Fact]
    public void ValidateMessageId_NullMessageId_ReturnsSomeError()
    {
        // Act
        var result = _orchestrator.ValidateMessageId(null, "TestRequest", "correlation-123");

        // Assert
        result.IsSome.ShouldBeTrue();
        result.Match(
            Some: error => error.Message.ShouldContain("IdempotencyKey"),
            None: () => throw new InvalidOperationException("Expected Some"));
    }

    [Fact]
    public void ValidateMessageId_EmptyMessageId_ReturnsSomeError()
    {
        // Act
        var result = _orchestrator.ValidateMessageId("", "TestRequest", "correlation-123");

        // Assert
        result.IsSome.ShouldBeTrue();
    }

    [Fact]
    public void ValidateMessageId_WhitespaceMessageId_ReturnsSomeError()
    {
        // Act
        var result = _orchestrator.ValidateMessageId("   ", "TestRequest", "correlation-123");

        // Assert
        result.IsSome.ShouldBeTrue();
    }

    [Fact]
    public void ValidateMessageId_ValidMessageId_ReturnsNone()
    {
        // Act
        var result = _orchestrator.ValidateMessageId("valid-message-id", "TestRequest", "correlation-123");

        // Assert
        result.IsNone.ShouldBeTrue();
    }

    #endregion

    #region ProcessAsync Tests - New Message

    [Fact]
    public async Task ProcessAsync_NewMessage_CreatesInboxEntryAndProcesses()
    {
        // Arrange
        var messageId = "msg-123";
        var requestType = "TestRequest";
        var correlationId = "corr-456";
        var expectedResponse = "Success";

        _store.GetMessageAsync(messageId, Arg.Any<CancellationToken>())
            .Returns((IInboxMessage?)null);

        var inboxMessage = CreateTestInboxMessage(messageId, FixedUtcNow);
        _messageFactory.Create(
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<DateTime>(),
            Arg.Any<DateTime>(),
            Arg.Any<InboxMetadata?>())
            .Returns(inboxMessage);

        var callback = () => ValueTask.FromResult<Either<EncinaError, string>>(expectedResponse);

        // Act
        var result = await _orchestrator.ProcessAsync(
            messageId, requestType, correlationId, null, callback);

        // Assert
        result.IsRight.ShouldBeTrue();
        result.Match(
            Right: value => value.ShouldBe(expectedResponse),
            Left: _ => throw new InvalidOperationException("Expected Right"));

        await _store.Received(1).AddAsync(inboxMessage, Arg.Any<CancellationToken>());
        await _store.Received(1).MarkAsProcessedAsync(messageId, Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ProcessAsync_NewMessage_WithMetadata_PassesMetadataToFactory()
    {
        // Arrange
        var messageId = "msg-123";
        var metadata = new InboxMetadata
        {
            CorrelationId = "corr-123",
            UserId = "user-456",
            TenantId = "tenant-789"
        };

        _store.GetMessageAsync(messageId, Arg.Any<CancellationToken>())
            .Returns((IInboxMessage?)null);

        var inboxMessage = CreateTestInboxMessage(messageId, FixedUtcNow);
        _messageFactory.Create(
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<DateTime>(),
            Arg.Any<DateTime>(),
            Arg.Any<InboxMetadata?>())
            .Returns(inboxMessage);

        var callback = () => ValueTask.FromResult<Either<EncinaError, string>>("Success");

        // Act
        await _orchestrator.ProcessAsync(
            messageId, "TestRequest", "corr-123", metadata, callback);

        // Assert
        _messageFactory.Received(1).Create(
            messageId,
            Arg.Any<string>(),
            Arg.Any<DateTime>(),
            Arg.Any<DateTime>(),
            Arg.Is<InboxMetadata?>(m => m != null && m.UserId == "user-456"));
    }

    #endregion

    #region ProcessAsync Tests - Existing Processed Message

    [Fact]
    public async Task ProcessAsync_ExistingProcessedMessage_ReturnsCachedResponse()
    {
        // Arrange
        var messageId = "msg-123";
        var cachedResponse = """{"isSuccess":true,"value":"Cached Result"}""";

        var existingMessage = CreateTestInboxMessage(messageId, FixedUtcNow);
        existingMessage.ProcessedAtUtc = FixedUtcNow;
        existingMessage.Response = cachedResponse;

        _store.GetMessageAsync(messageId, Arg.Any<CancellationToken>())
            .Returns(existingMessage);

        var callbackInvoked = false;
        var callback = () =>
        {
            callbackInvoked = true;
            return ValueTask.FromResult<Either<EncinaError, string>>("New Result");
        };

        // Act
        var result = await _orchestrator.ProcessAsync<string>(
            messageId, "TestRequest", "corr-123", null, callback);

        // Assert
        callbackInvoked.ShouldBeFalse();
        result.IsRight.ShouldBeTrue();
        result.Match(
            Right: value => value.ShouldBe("Cached Result"),
            Left: _ => throw new InvalidOperationException("Expected Right"));
    }

    #endregion

    #region ProcessAsync Tests - Existing Failed Message

    [Fact]
    public async Task ProcessAsync_ExistingFailedMessage_RetriesIfWithinLimit()
    {
        // Arrange
        var messageId = "msg-123";
        var expectedResponse = "Retry Success";

        var existingMessage = CreateTestInboxMessage(messageId, FixedUtcNow);
        existingMessage.RetryCount = 1; // Less than MaxRetries (3)
        existingMessage.ProcessedAtUtc = null;
        existingMessage.Response = null;

        _store.GetMessageAsync(messageId, Arg.Any<CancellationToken>())
            .Returns(existingMessage);

        var callback = () => ValueTask.FromResult<Either<EncinaError, string>>(expectedResponse);

        // Act
        var result = await _orchestrator.ProcessAsync(
            messageId, "TestRequest", "corr-123", null, callback);

        // Assert
        result.IsRight.ShouldBeTrue();
        result.Match(
            Right: value => value.ShouldBe(expectedResponse),
            Left: _ => throw new InvalidOperationException("Expected Right"));

        await _store.Received(1).IncrementRetryCountAsync(messageId, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ProcessAsync_ExistingFailedMessage_ReturnsErrorWhenMaxRetriesExceeded()
    {
        // Arrange
        var messageId = "msg-123";

        var existingMessage = CreateTestInboxMessage(messageId, FixedUtcNow);
        existingMessage.RetryCount = 3; // Equal to MaxRetries
        existingMessage.ProcessedAtUtc = null;
        existingMessage.Response = null;

        _store.GetMessageAsync(messageId, Arg.Any<CancellationToken>())
            .Returns(existingMessage);

        var callbackInvoked = false;
        var callback = () =>
        {
            callbackInvoked = true;
            return ValueTask.FromResult<Either<EncinaError, string>>("Should not be called");
        };

        // Act
        var result = await _orchestrator.ProcessAsync<string>(
            messageId, "TestRequest", "corr-123", null, callback);

        // Assert
        callbackInvoked.ShouldBeFalse();
        result.IsLeft.ShouldBeTrue();
        result.Match(
            Right: _ => throw new InvalidOperationException("Expected Left"),
            Left: error => error.Message.ShouldContain("will not be retried"));
    }

    #endregion

    #region ProcessAsync Tests - Error Handling

    [Fact]
    public async Task ProcessAsync_CallbackThrows_MarksAsFailedAndRethrows()
    {
        // Arrange
        var messageId = "msg-123";

        _store.GetMessageAsync(messageId, Arg.Any<CancellationToken>())
            .Returns((IInboxMessage?)null);

        var inboxMessage = CreateTestInboxMessage(messageId, FixedUtcNow);
        _messageFactory.Create(
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<DateTime>(),
            Arg.Any<DateTime>(),
            Arg.Any<InboxMetadata?>())
            .Returns(inboxMessage);

        Func<ValueTask<Either<EncinaError, string>>> callback = () =>
            ValueTask.FromException<Either<EncinaError, string>>(new InvalidOperationException("Processing failed"));

        // Act & Assert
        var act = async () => await _orchestrator.ProcessAsync(
            messageId, "TestRequest", "corr-123", null, callback);

        await act.ShouldThrowAsync<InvalidOperationException>();

        await _store.Received(1).MarkAsFailedAsync(
            messageId,
            Arg.Is<string>(s => s.Contains("Processing failed")),
            Arg.Any<DateTime>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ProcessAsync_NullMessageId_ThrowsArgumentException()
    {
        // Arrange
        var callback = () => ValueTask.FromResult<Either<EncinaError, string>>("Success");

        // Act
        var act = async () => await _orchestrator.ProcessAsync(
            null!, "TestRequest", "corr-123", null, callback);

        // Assert
        await act.ShouldThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task ProcessAsync_NullRequestType_ThrowsArgumentException()
    {
        // Arrange
        var callback = () => ValueTask.FromResult<Either<EncinaError, string>>("Success");

        // Act
        var act = async () => await _orchestrator.ProcessAsync(
            "msg-123", null!, "corr-123", null, callback);

        // Assert
        await act.ShouldThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task ProcessAsync_NullCallback_ThrowsArgumentNullException()
    {
        // Act
        var act = async () => await _orchestrator.ProcessAsync<string>(
            "msg-123", "TestRequest", "corr-123", null, null!);

        // Assert
        await act.ShouldThrowAsync<ArgumentNullException>();
    }

    #endregion

    #region Helpers

    private static TestInboxMessage CreateTestInboxMessage(
        string messageId,
        DateTime baseUtc,
        int retentionDays = 30)
    {
        return new TestInboxMessage
        {
            MessageId = messageId,
            RequestType = "TestRequest",
            ReceivedAtUtc = baseUtc,
            ExpiresAtUtc = baseUtc.AddDays(retentionDays),
            NowProvider = () => FixedUtcNow
        };
    }

    #endregion
}

/// <summary>
/// Test implementation of IInboxMessage for unit tests.
/// </summary>
internal sealed class TestInboxMessage : IInboxMessage
{
    public string MessageId { get; set; } = string.Empty;
    public string RequestType { get; set; } = string.Empty;
    public DateTime ReceivedAtUtc { get; set; }
    public DateTime? ProcessedAtUtc { get; set; }
    public DateTime ExpiresAtUtc { get; set; }
    public string? Response { get; set; }
    public int RetryCount { get; set; }
    public DateTime? NextRetryAtUtc { get; set; }
    public string? ErrorMessage { get; set; }
    public string? CorrelationId { get; set; }
    public string? UserId { get; set; }
    public string? TenantId { get; set; }

    /// <summary>
    /// Time provider for deterministic testing. Defaults to DateTime.UtcNow.
    /// </summary>
    public Func<DateTime> NowProvider { get; set; } = () => DateTime.UtcNow;

    public bool IsProcessed => ProcessedAtUtc.HasValue;
    public bool IsExpired() => NowProvider() > ExpiresAtUtc;
}
