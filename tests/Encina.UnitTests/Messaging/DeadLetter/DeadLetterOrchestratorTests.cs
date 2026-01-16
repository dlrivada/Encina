using System.Text.Json;
using Encina.Messaging.DeadLetter;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Shouldly;

namespace Encina.UnitTests.Messaging.DeadLetter;

/// <summary>
/// Unit tests for DeadLetterOrchestrator.
/// </summary>
public sealed class DeadLetterOrchestratorTests
{
    private static readonly DateTime FixedUtcNow = new(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);

    private readonly IDeadLetterStore _store;
    private readonly IDeadLetterMessageFactory _messageFactory;
    private readonly DeadLetterOptions _options;
    private readonly ILogger<DeadLetterOrchestrator> _logger;
    private readonly DeadLetterOrchestrator _orchestrator;

    public DeadLetterOrchestratorTests()
    {
        _store = Substitute.For<IDeadLetterStore>();
        _messageFactory = Substitute.For<IDeadLetterMessageFactory>();
        _options = new DeadLetterOptions
        {
            RetentionPeriod = TimeSpan.FromDays(7),
            CleanupInterval = TimeSpan.FromHours(1),
            EnableAutomaticCleanup = true
        };
        _logger = Substitute.For<ILogger<DeadLetterOrchestrator>>();

        _orchestrator = new DeadLetterOrchestrator(_store, _messageFactory, _options, _logger);
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_NullStore_ThrowsArgumentNullException()
    {
        var act = () => new DeadLetterOrchestrator(null!, _messageFactory, _options, _logger);

        act.ShouldThrow<ArgumentNullException>().ParamName.ShouldBe("store");
    }

    [Fact]
    public void Constructor_NullMessageFactory_ThrowsArgumentNullException()
    {
        var act = () => new DeadLetterOrchestrator(_store, null!, _options, _logger);

        act.ShouldThrow<ArgumentNullException>().ParamName.ShouldBe("messageFactory");
    }

    [Fact]
    public void Constructor_NullOptions_ThrowsArgumentNullException()
    {
        var act = () => new DeadLetterOrchestrator(_store, _messageFactory, null!, _logger);

        act.ShouldThrow<ArgumentNullException>().ParamName.ShouldBe("options");
    }

    [Fact]
    public void Constructor_NullLogger_ThrowsArgumentNullException()
    {
        var act = () => new DeadLetterOrchestrator(_store, _messageFactory, _options, null!);

        act.ShouldThrow<ArgumentNullException>().ParamName.ShouldBe("logger");
    }

    #endregion

    #region AddAsync Tests

    [Fact]
    public async Task AddAsync_ValidRequest_CreatesAndStoresMessage()
    {
        // Arrange
        var requestId = Guid.NewGuid();
        var request = new TestDeadLetterRequest { Id = requestId, Data = "Test" };
        var error = EncinaErrors.Create("test.error", "Test error");
        var sourcePattern = DeadLetterSourcePatterns.Recoverability;
        var firstFailedAt = new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);

        var expectedRequestType = typeof(TestDeadLetterRequest).AssemblyQualifiedName!;
        var expectedRequestContent = JsonSerializer.Serialize(request);
        var expectedRetryCount = 3;
        var expectedRetentionPeriod = _options.RetentionPeriod!.Value;

        var expectedMessage = CreateTestDeadLetterMessage(Guid.NewGuid());
        _messageFactory.Create(Arg.Any<DeadLetterData>())
            .Returns(expectedMessage);

        // Act
        var context = new DeadLetterContext(error, null, sourcePattern, expectedRetryCount, firstFailedAt);
        var result = await _orchestrator.AddAsync(request, context);

        // Assert
        result.ShouldBe(expectedMessage);
        await _store.Received(1).AddAsync(expectedMessage, Arg.Any<CancellationToken>());
        await _store.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task AddAsync_WithException_IncludesExceptionDetails()
    {
        // Arrange
        var request = new TestDeadLetterRequest { Id = Guid.NewGuid() };
        var error = EncinaErrors.Create("test.error", "Test error");
        var exception = new InvalidOperationException("Something went wrong");
        var sourcePattern = DeadLetterSourcePatterns.Outbox;
        var firstFailedAt = new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);
        const int retryCount = 3;

        var expectedRequestType = typeof(TestDeadLetterRequest).AssemblyQualifiedName!;
        var expectedMessage = CreateTestDeadLetterMessage(Guid.NewGuid());

        _messageFactory.Create(Arg.Is<DeadLetterData>(d =>
            d.RequestType == expectedRequestType &&
            d.ErrorMessage == error.Message &&
            d.SourcePattern == sourcePattern &&
            d.TotalRetryAttempts == retryCount &&
            d.FirstFailedAtUtc == firstFailedAt &&
            d.ExceptionType == typeof(InvalidOperationException).FullName &&
            d.ExceptionMessage == "Something went wrong"))
            .Returns(expectedMessage);

        // Act
        var context = new DeadLetterContext(error, exception, sourcePattern, retryCount, firstFailedAt);
        await _orchestrator.AddAsync(request, context);

        // Assert
        _messageFactory.Received(1).Create(Arg.Is<DeadLetterData>(d =>
            d.RequestType == expectedRequestType &&
            d.ErrorMessage == error.Message &&
            d.SourcePattern == sourcePattern &&
            d.TotalRetryAttempts == retryCount &&
            d.FirstFailedAtUtc == firstFailedAt &&
            d.ExceptionType == typeof(InvalidOperationException).FullName &&
            d.ExceptionMessage == "Something went wrong"));
    }

    [Fact]
    public async Task AddAsync_WithOnDeadLetterCallback_InvokesCallback()
    {
        // Arrange
        var callbackInvoked = false;
        IDeadLetterMessage? callbackMessage = null;

        var optionsWithCallback = new DeadLetterOptions
        {
            RetentionPeriod = TimeSpan.FromDays(7),
            OnDeadLetter = (msg, ct) =>
            {
                callbackInvoked = true;
                callbackMessage = msg;
                return Task.CompletedTask;
            }
        };

        var orchestrator = new DeadLetterOrchestrator(
            _store, _messageFactory, optionsWithCallback, _logger);

        var request = new TestDeadLetterRequest { Id = Guid.NewGuid() };
        var error = EncinaErrors.Create("test.error", "Test error");
        var expectedMessage = CreateTestDeadLetterMessage(Guid.NewGuid());

        _messageFactory.Create(Arg.Any<DeadLetterData>())
            .Returns(expectedMessage);

        // Act
        var context = new DeadLetterContext(error, null, DeadLetterSourcePatterns.Recoverability, 3, FixedUtcNow);
        await orchestrator.AddAsync(request, context);

        // Assert
        callbackInvoked.ShouldBeTrue();
        callbackMessage.ShouldBe(expectedMessage);
    }

    [Fact]
    public async Task AddAsync_CallbackThrows_DoesNotPropagateException()
    {
        // Arrange
        var optionsWithCallback = new DeadLetterOptions
        {
            RetentionPeriod = TimeSpan.FromDays(7),
            OnDeadLetter = (msg, ct) => throw new InvalidOperationException("Callback failed")
        };

        var orchestrator = new DeadLetterOrchestrator(
            _store, _messageFactory, optionsWithCallback, _logger);

        var request = new TestDeadLetterRequest { Id = Guid.NewGuid() };
        var error = EncinaErrors.Create("test.error", "Test error");
        var expectedMessage = CreateTestDeadLetterMessage(Guid.NewGuid());

        _messageFactory.Create(Arg.Any<DeadLetterData>())
            .Returns(expectedMessage);

        // Act & Assert - should not throw
        var context = new DeadLetterContext(error, null, DeadLetterSourcePatterns.Recoverability, 3, FixedUtcNow);
        var result = await orchestrator.AddAsync(request, context);

        result.ShouldBe(expectedMessage);
    }

    [Fact]
    public async Task AddAsync_NullRequest_ThrowsArgumentNullException()
    {
        // Arrange
        var error = EncinaErrors.Create("test.error", "Test error");
        var context = new DeadLetterContext(error, null, DeadLetterSourcePatterns.Recoverability, 3, FixedUtcNow);

        // Act
        var act = async () => await _orchestrator.AddAsync<TestDeadLetterRequest>(null!, context);

        // Assert
        var ex = await act.ShouldThrowAsync<ArgumentNullException>();
        ex.ParamName.ShouldBe("request");
    }

    [Fact]
    public async Task AddAsync_NullContext_ThrowsArgumentNullException()
    {
        // Arrange
        var request = new TestDeadLetterRequest { Id = Guid.NewGuid() };

        // Act
        var act = async () => await _orchestrator.AddAsync(request, null!);

        // Assert
        var ex = await act.ShouldThrowAsync<ArgumentNullException>();
        ex.ParamName.ShouldBe("context");
    }

    [Fact]
    public async Task AddAsync_NullSourcePattern_ThrowsArgumentException()
    {
        // Arrange
        var request = new TestDeadLetterRequest { Id = Guid.NewGuid() };
        var error = EncinaErrors.Create("test.error", "Test error");
        var context = new DeadLetterContext(error, null, null!, 3, FixedUtcNow);

        // Act
        var act = async () => await _orchestrator.AddAsync(request, context);

        // Assert
        var ex = await act.ShouldThrowAsync<ArgumentException>();
    }

    #endregion

    #region GetAsync Tests

    [Fact]
    public async Task GetAsync_ExistingMessage_ReturnsMessage()
    {
        // Arrange
        var messageId = Guid.NewGuid();
        var expectedMessage = CreateTestDeadLetterMessage(messageId);

        _store.GetAsync(messageId, Arg.Any<CancellationToken>())
            .Returns(expectedMessage);

        // Act
        var result = await _orchestrator.GetAsync(messageId);

        // Assert
        result.ShouldBe(expectedMessage);
    }

    [Fact]
    public async Task GetAsync_NonExistentMessage_ReturnsNull()
    {
        // Arrange
        var messageId = Guid.NewGuid();
        _store.GetAsync(messageId, Arg.Any<CancellationToken>())
            .Returns((IDeadLetterMessage?)null);

        // Act
        var result = await _orchestrator.GetAsync(messageId);

        // Assert
        result.ShouldBeNull();
    }

    #endregion

    #region GetPendingCountAsync Tests

    [Fact]
    public async Task GetPendingCountAsync_ReturnsCountFromStore()
    {
        // Arrange
        _store.GetCountAsync(
            Arg.Is<DeadLetterFilter>(f => f.ExcludeReplayed == true),
            Arg.Any<CancellationToken>())
            .Returns(5);

        // Act
        var result = await _orchestrator.GetPendingCountAsync();

        // Assert
        result.ShouldBe(5);
    }

    #endregion

    #region CleanupExpiredAsync Tests

    [Fact]
    public async Task CleanupExpiredAsync_DelegatesToStore()
    {
        // Arrange
        _store.DeleteExpiredAsync(Arg.Any<CancellationToken>())
            .Returns(10);

        // Act
        var result = await _orchestrator.CleanupExpiredAsync();

        // Assert
        result.ShouldBe(10);
        await _store.Received(1).DeleteExpiredAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CleanupExpiredAsync_NoExpiredMessages_ReturnsZero()
    {
        // Arrange
        _store.DeleteExpiredAsync(Arg.Any<CancellationToken>())
            .Returns(0);

        // Act
        var result = await _orchestrator.CleanupExpiredAsync();

        // Assert
        result.ShouldBe(0);
    }

    #endregion

    #region GetStatisticsAsync Tests

    [Fact]
    public async Task GetStatisticsAsync_ReturnsCorrectStatistics()
    {
        // Arrange
        _store.GetCountAsync(null, Arg.Any<CancellationToken>())
            .Returns(100);
        _store.GetCountAsync(
            Arg.Is<DeadLetterFilter>(f => f.ExcludeReplayed == true),
            Arg.Any<CancellationToken>())
            .Returns(80);
        _store.GetCountAsync(
            Arg.Is<DeadLetterFilter>(f => f.ExcludeReplayed == false),
            Arg.Any<CancellationToken>())
            .Returns(100);
        _store.GetMessagesAsync(
            Arg.Any<DeadLetterFilter>(),
            Arg.Any<int>(),
            Arg.Any<int>(),
            Arg.Any<CancellationToken>())
            .Returns([]);

        // Act
        var result = await _orchestrator.GetStatisticsAsync();

        // Assert
        result.TotalCount.ShouldBe(100);
        result.PendingCount.ShouldBe(80);
        result.ReplayedCount.ShouldBe(20);
    }

    #endregion

    #region Helpers

    private static TestDeadLetterMessage CreateTestDeadLetterMessage(Guid id)
    {
        return new TestDeadLetterMessage
        {
            Id = id,
            RequestType = typeof(TestDeadLetterRequest).AssemblyQualifiedName!,
            RequestContent = "{}",
            ErrorMessage = "Test error",
            SourcePattern = DeadLetterSourcePatterns.Recoverability,
            TotalRetryAttempts = 3,
            FirstFailedAtUtc = FixedUtcNow.AddMinutes(-5),
            DeadLetteredAtUtc = FixedUtcNow
        };
    }

    #endregion
}

/// <summary>
/// Test request for dead letter tests.
/// </summary>
public sealed class TestDeadLetterRequest
{
    public Guid Id { get; set; }
    public string Data { get; set; } = string.Empty;
}

/// <summary>
/// Test implementation of IDeadLetterMessage for unit tests.
/// </summary>
internal sealed class TestDeadLetterMessage : IDeadLetterMessage
{
    /// <summary>
    /// Fixed UTC time used for deterministic testing (2026-01-01 12:00:00 UTC).
    /// </summary>
    private static readonly DateTime DefaultFixedUtcNow = new(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);

    public Guid Id { get; set; }
    public string RequestType { get; set; } = string.Empty;
    public string RequestContent { get; set; } = string.Empty;
    public string ErrorMessage { get; set; } = string.Empty;
    public string? ExceptionType { get; set; }
    public string? ExceptionMessage { get; set; }
    public string? ExceptionStackTrace { get; set; }
    public string? CorrelationId { get; set; }
    public string SourcePattern { get; set; } = string.Empty;
    public int TotalRetryAttempts { get; set; }
    public DateTime FirstFailedAtUtc { get; set; }
    public DateTime DeadLetteredAtUtc { get; set; }
    public DateTime? ExpiresAtUtc { get; set; }
    public DateTime? ReplayedAtUtc { get; set; }
    public string? ReplayResult { get; set; }

    /// <summary>
    /// Time provider for deterministic testing. Defaults to 2026-01-01 12:00:00 UTC
    /// for predictable <see cref="IsExpired"/> behavior.
    /// Override in tests that require real-time behavior or specific time scenarios.
    /// </summary>
    public Func<DateTime> NowProvider { get; set; } = () => DefaultFixedUtcNow;

    public bool IsReplayed => ReplayedAtUtc.HasValue;
    public bool IsExpired => ExpiresAtUtc.HasValue && ExpiresAtUtc.Value <= NowProvider();
}
