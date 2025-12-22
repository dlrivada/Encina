using Encina.ADO.Oracle.Inbox;
using Encina.TestInfrastructure.Extensions;
using Encina.TestInfrastructure.Fixtures;
using Xunit;

namespace Encina.ADO.Oracle.Tests.Inbox;

/// <summary>
/// Property-based tests for <see cref="InboxStoreADO"/>.
/// Verifies invariants and properties across various inputs.
/// </summary>
public sealed class InboxStoreADOPropertyTests : IClassFixture<OracleFixture>
{
    private readonly OracleFixture _database;
    private readonly InboxStoreADO _store;

    public InboxStoreADOPropertyTests(OracleFixture database)
    {
        _database = database;
        

        // Clear all data before each test to ensure clean state
        _database.ClearAllDataAsync().GetAwaiter().GetResult();

        _store = new InboxStoreADO(_database.CreateConnection());
    }

    #region RetryCount Invariants

    [Theory]
    [InlineData(1)]
    [InlineData(3)]
    [InlineData(5)]
    [InlineData(10)]
    public async Task MarkAsFailedAsync_MultipleCalls_ShouldIncrementRetryCountMonotonically(int failureCount)
    {
        // Arrange
        var messageId = $"retry-test-{failureCount}";
        var message = new InboxMessage
        {
            MessageId = messageId,
            RequestType = "TestRequest",
            ReceivedAtUtc = DateTime.UtcNow,
            ExpiresAtUtc = DateTime.UtcNow.AddDays(30),
            RetryCount = 0
        };
        await _store.AddAsync(message);

        // Act - Mark as failed N times
        for (int i = 0; i < failureCount; i++)
        {
            await _store.MarkAsFailedAsync(messageId, $"Failure {i + 1}", null);
        }

        // Assert - RetryCount should equal failureCount
        var result = await _store.GetMessageAsync(messageId);
        Assert.NotNull(result);
        Assert.Equal(failureCount, result.RetryCount);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(5)]
    public async Task RetryCount_AfterFailures_ShouldNeverDecrease(int initialRetryCount)
    {
        // Arrange
        var messageId = $"retry-monotonic-{initialRetryCount}";
        var message = new InboxMessage
        {
            MessageId = messageId,
            RequestType = "TestRequest",
            ReceivedAtUtc = DateTime.UtcNow,
            ExpiresAtUtc = DateTime.UtcNow.AddDays(30),
            RetryCount = initialRetryCount
        };
        await _store.AddAsync(message);

        // Act - Mark as failed twice
        await _store.MarkAsFailedAsync(messageId, "Error 1", null);
        await _store.MarkAsFailedAsync(messageId, "Error 2", null);

        // Assert - RetryCount increased by 2
        var result = await _store.GetMessageAsync(messageId);
        Assert.NotNull(result);
        Assert.Equal(initialRetryCount + 2, result.RetryCount);
    }

    #endregion

    #region ProcessedAtUtc Invariants

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("{\"result\":\"success\"}")]
    [InlineData("{\"orderId\":123,\"total\":249.99}")]
    public async Task MarkAsProcessedAsync_WithAnyResponse_ShouldSetProcessedAtUtc(string? response)
    {
        // Arrange
        var messageId = $"processed-test-{Guid.NewGuid()}";
        var message = new InboxMessage
        {
            MessageId = messageId,
            RequestType = "TestRequest",
            ReceivedAtUtc = DateTime.UtcNow,
            ExpiresAtUtc = DateTime.UtcNow.AddDays(30),
            RetryCount = 0
        };
        await _store.AddAsync(message);

        // Act
        await _store.MarkAsProcessedAsync(messageId, response);

        // Assert - ProcessedAtUtc should be set
        var result = await _store.GetMessageAsync(messageId);
        Assert.NotNull(result);
        Assert.NotNull(result.ProcessedAtUtc);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(5)]
    public async Task MarkAsProcessedAsync_AfterFailures_ShouldStillSetProcessedAtUtc(int previousRetries)
    {
        // Arrange
        var messageId = $"recovery-test-{previousRetries}";
        var message = new InboxMessage
        {
            MessageId = messageId,
            RequestType = "TestRequest",
            ReceivedAtUtc = DateTime.UtcNow,
            ExpiresAtUtc = DateTime.UtcNow.AddDays(30),
            RetryCount = previousRetries,
            ErrorMessage = previousRetries > 0 ? "Previous error" : null
        };
        await _store.AddAsync(message);

        // Act
        await _store.MarkAsProcessedAsync(messageId, "{\"status\":\"recovered\"}");

        // Assert
        var result = await _store.GetMessageAsync(messageId);
        Assert.NotNull(result);
        Assert.NotNull(result.ProcessedAtUtc);
        Assert.Null(result.ErrorMessage); // Error cleared
    }

    #endregion

    #region ExpiresAtUtc Filtering

    [Theory]
    [InlineData(-1)]  // Expired 1 day ago
    [InlineData(-7)]  // Expired 1 week ago
    [InlineData(-30)] // Expired 30 days ago
    public async Task GetExpiredMessagesAsync_ExpiredByDays_ShouldReturnIfProcessed(int daysExpired)
    {
        // Arrange
        var messageId = $"expired-test-{Math.Abs(daysExpired)}";
        var message = new InboxMessage
        {
            MessageId = messageId,
            RequestType = "TestRequest",
            ReceivedAtUtc = DateTime.UtcNow.AddDays(daysExpired - 5),
            ProcessedAtUtc = DateTime.UtcNow.AddDays(daysExpired - 3),
            ExpiresAtUtc = DateTime.UtcNow.AddDays(daysExpired),
            RetryCount = 0
        };
        await _store.AddAsync(message);

        // Act
        var expired = await _store.GetExpiredMessagesAsync(10);

        // Assert
        Assert.Single(expired);
        Assert.Equal(messageId, expired.First().MessageId);
    }

    [Theory]
    [InlineData(1)]   // Expires tomorrow
    [InlineData(7)]   // Expires in 1 week
    [InlineData(30)]  // Expires in 30 days
    public async Task GetExpiredMessagesAsync_NotYetExpired_ShouldNotReturn(int daysUntilExpiry)
    {
        // Arrange
        var messageId = $"future-test-{daysUntilExpiry}";
        var message = new InboxMessage
        {
            MessageId = messageId,
            RequestType = "TestRequest",
            ReceivedAtUtc = DateTime.UtcNow,
            ProcessedAtUtc = DateTime.UtcNow,
            ExpiresAtUtc = DateTime.UtcNow.AddDays(daysUntilExpiry),
            RetryCount = 0
        };
        await _store.AddAsync(message);

        // Act
        var expired = await _store.GetExpiredMessagesAsync(10);

        // Assert
        Assert.Empty(expired);
    }

    #endregion

    #region MessageId Properties

    [Theory]
    [InlineData("simple-id")]
    [InlineData("id-with-dashes-123")]
    [InlineData("ID_WITH_UNDERSCORES")]
    [InlineData("MixedCase123")]
    public async Task AddAsync_VariousMessageIdFormats_ShouldPersistAndRetrieve(string messageId)
    {
        // Arrange
        var message = new InboxMessage
        {
            MessageId = messageId,
            RequestType = "TestRequest",
            ReceivedAtUtc = DateTime.UtcNow,
            ExpiresAtUtc = DateTime.UtcNow.AddDays(30),
            RetryCount = 0
        };

        // Act
        await _store.AddAsync(message);

        // Assert
        var retrieved = await _store.GetMessageAsync(messageId);
        Assert.NotNull(retrieved);
        Assert.Equal(messageId, retrieved.MessageId);
    }

    [Theory]
    [InlineData("msg-1", "msg-2")]
    [InlineData("order-123", "order-456")]
    [InlineData("a", "b")]
    public async Task AddAsync_DifferentMessageIds_ShouldCoexist(string messageId1, string messageId2)
    {
        // Arrange
        var message1 = new InboxMessage
        {
            MessageId = messageId1,
            RequestType = "TestRequest",
            ReceivedAtUtc = DateTime.UtcNow,
            ExpiresAtUtc = DateTime.UtcNow.AddDays(30),
            RetryCount = 0
        };
        var message2 = new InboxMessage
        {
            MessageId = messageId2,
            RequestType = "TestRequest",
            ReceivedAtUtc = DateTime.UtcNow,
            ExpiresAtUtc = DateTime.UtcNow.AddDays(30),
            RetryCount = 0
        };

        // Act
        await _store.AddAsync(message1);
        await _store.AddAsync(message2);

        // Assert - Both messages exist
        var retrieved1 = await _store.GetMessageAsync(messageId1);
        var retrieved2 = await _store.GetMessageAsync(messageId2);
        Assert.NotNull(retrieved1);
        Assert.NotNull(retrieved2);
        Assert.Equal(messageId1, retrieved1.MessageId);
        Assert.Equal(messageId2, retrieved2.MessageId);
    }

    #endregion

    #region RequestType Properties

    [Theory]
    [InlineData("CreateOrderCommand")]
    [InlineData("ProcessPaymentCommand")]
    [InlineData("SendEmailCommand")]
    [InlineData("UpdateUserCommand")]
    public async Task AddAsync_VariousRequestTypes_ShouldPersist(string requestType)
    {
        // Arrange
        var messageId = $"type-test-{requestType}";
        var message = new InboxMessage
        {
            MessageId = messageId,
            RequestType = requestType,
            ReceivedAtUtc = DateTime.UtcNow,
            ExpiresAtUtc = DateTime.UtcNow.AddDays(30),
            RetryCount = 0
        };

        // Act
        await _store.AddAsync(message);

        // Assert
        var retrieved = await _store.GetMessageAsync(messageId);
        Assert.NotNull(retrieved);
        Assert.Equal(requestType, retrieved.RequestType);
    }

    #endregion

    #region Response Payload Properties

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("simple VARCHAR2(4000)")]
    [InlineData("{\"orderId\":123}")]
    [InlineData("{\"data\":{\"nested\":{\"deeply\":true}}}")]
    public async Task MarkAsProcessedAsync_VariousResponses_ShouldPersist(string? response)
    {
        // Arrange
        var messageId = $"response-test-{Guid.NewGuid()}";
        var message = new InboxMessage
        {
            MessageId = messageId,
            RequestType = "TestRequest",
            ReceivedAtUtc = DateTime.UtcNow,
            ExpiresAtUtc = DateTime.UtcNow.AddDays(30),
            RetryCount = 0
        };
        await _store.AddAsync(message);

        // Act
        await _store.MarkAsProcessedAsync(messageId, response);

        // Assert
        var retrieved = await _store.GetMessageAsync(messageId);
        Assert.NotNull(retrieved);
        Assert.Equal(response, retrieved.Response);
    }

    [Theory]
    [InlineData(100)]    // 100 bytes
    [InlineData(1000)]   // 1KB
    [InlineData(10000)]  // 10KB
    public async Task MarkAsProcessedAsync_LargeResponses_ShouldPersist(int responseSize)
    {
        // Arrange
        var messageId = $"large-response-{responseSize}";
        var largeResponse = new string('X', responseSize);
        var message = new InboxMessage
        {
            MessageId = messageId,
            RequestType = "TestRequest",
            ReceivedAtUtc = DateTime.UtcNow,
            ExpiresAtUtc = DateTime.UtcNow.AddDays(30),
            RetryCount = 0
        };
        await _store.AddAsync(message);

        // Act
        await _store.MarkAsProcessedAsync(messageId, largeResponse);

        // Assert
        var retrieved = await _store.GetMessageAsync(messageId);
        Assert.NotNull(retrieved);
        Assert.Equal(responseSize, retrieved.Response!.Length);
    }

    #endregion

    #region ErrorMessage Properties

    [Theory]
    [InlineData("Database connection failed")]
    [InlineData("Timeout exceeded")]
    [InlineData("Validation failed: Email is required")]
    [InlineData("External service unavailable (HTTP 503)")]
    public async Task MarkAsFailedAsync_VariousErrorMessages_ShouldPersist(string errorMessage)
    {
        // Arrange
        var messageId = $"error-test-{Guid.NewGuid()}";
        var message = new InboxMessage
        {
            MessageId = messageId,
            RequestType = "TestRequest",
            ReceivedAtUtc = DateTime.UtcNow,
            ExpiresAtUtc = DateTime.UtcNow.AddDays(30),
            RetryCount = 0
        };
        await _store.AddAsync(message);

        // Act
        await _store.MarkAsFailedAsync(messageId, errorMessage, null);

        // Assert
        var retrieved = await _store.GetMessageAsync(messageId);
        Assert.NotNull(retrieved);
        Assert.Equal(errorMessage, retrieved.ErrorMessage);
    }

    #endregion

    #region NextRetryAtUtc Properties

    [Theory]
    [InlineData(5)]    // Retry in 5 minutes
    [InlineData(15)]   // Retry in 15 minutes
    [InlineData(60)]   // Retry in 1 hour
    [InlineData(1440)] // Retry in 24 hours
    public async Task MarkAsFailedAsync_VariousRetryDelays_ShouldPersist(int minutesDelay)
    {
        // Arrange
        var messageId = $"retry-delay-{minutesDelay}";
        var message = new InboxMessage
        {
            MessageId = messageId,
            RequestType = "TestRequest",
            ReceivedAtUtc = DateTime.UtcNow,
            ExpiresAtUtc = DateTime.UtcNow.AddDays(30),
            RetryCount = 0
        };
        await _store.AddAsync(message);
        var nextRetry = DateTime.UtcNow.AddMinutes(minutesDelay);

        // Act
        await _store.MarkAsFailedAsync(messageId, "Temporary error", nextRetry);

        // Assert
        var retrieved = await _store.GetMessageAsync(messageId);
        Assert.NotNull(retrieved);
        Assert.NotNull(retrieved.NextRetryAtUtc);
        // Allow 2 second tolerance for DateTime comparison
        Assert.True(Math.Abs((retrieved.NextRetryAtUtc!.Value - nextRetry).TotalSeconds) < 2);
    }

    #endregion

    #region Batch Size Properties

    [Theory]
    [InlineData(1, 5)]   // Request 1, have 5
    [InlineData(3, 5)]   // Request 3, have 5
    [InlineData(5, 5)]   // Request 5, have 5
    [InlineData(10, 5)]  // Request 10, have 5
    public async Task GetExpiredMessagesAsync_VariousBatchSizes_ShouldRespectLimit(
        int batchSize,
        int totalExpired)
    {
        // Arrange - Create N expired messages
        for (int i = 0; i < totalExpired; i++)
        {
            var message = new InboxMessage
            {
                MessageId = $"batch-test-{i}",
                RequestType = "TestRequest",
                ReceivedAtUtc = DateTime.UtcNow.AddDays(-40),
                ProcessedAtUtc = DateTime.UtcNow.AddDays(-35),
                ExpiresAtUtc = DateTime.UtcNow.AddDays(-i - 1),
                RetryCount = 0
            };
            await _store.AddAsync(message);
        }

        // Act
        var expired = await _store.GetExpiredMessagesAsync(batchSize);

        // Assert - Should return min(batchSize, totalExpired)
        var expectedCount = Math.Min(batchSize, totalExpired);
        Assert.Equal(expectedCount, expired.Count());
    }

    #endregion

    #region State Transition Properties

    [Fact]
    public async Task MessageLifecycle_UnprocessedToProcessed_ShouldTransitionCorrectly()
    {
        // Arrange
        var messageId = "lifecycle-test";
        var message = new InboxMessage
        {
            MessageId = messageId,
            RequestType = "TestRequest",
            ReceivedAtUtc = DateTime.UtcNow,
            ExpiresAtUtc = DateTime.UtcNow.AddDays(30),
            RetryCount = 0
        };

        // Act & Assert - Initial state
        await _store.AddAsync(message);
        var initial = await _store.GetMessageAsync(messageId);
        Assert.NotNull(initial);
        Assert.Null(initial.ProcessedAtUtc);
        Assert.False(initial.IsProcessed);

        // Act & Assert - After processing
        await _store.MarkAsProcessedAsync(messageId, "{\"result\":\"success\"}");
        var processed = await _store.GetMessageAsync(messageId);
        Assert.NotNull(processed);
        Assert.NotNull(processed.ProcessedAtUtc);
        Assert.True(processed.IsProcessed);
    }

    [Fact]
    public async Task MessageLifecycle_FailedToRecovered_ShouldClearError()
    {
        // Arrange
        var messageId = "recovery-lifecycle-test";
        var message = new InboxMessage
        {
            MessageId = messageId,
            RequestType = "TestRequest",
            ReceivedAtUtc = DateTime.UtcNow,
            ExpiresAtUtc = DateTime.UtcNow.AddDays(30),
            RetryCount = 0
        };

        // Act & Assert - After failure
        await _store.AddAsync(message);
        await _store.MarkAsFailedAsync(messageId, "Temporary failure", DateTime.UtcNow.AddMinutes(5));
        var failed = await _store.GetMessageAsync(messageId);
        Assert.NotNull(failed);
        Assert.NotNull(failed.ErrorMessage);
        Assert.Equal(1, failed.RetryCount);

        // Act & Assert - After recovery
        await _store.MarkAsProcessedAsync(messageId, "{\"status\":\"recovered\"}");
        var recovered = await _store.GetMessageAsync(messageId);
        Assert.NotNull(recovered);
        Assert.Null(recovered.ErrorMessage);
        Assert.True(recovered.IsProcessed);
    }

    #endregion

    #region IsProcessed Property

    [Theory]
    [InlineData(true)]  // With ProcessedAtUtc
    [InlineData(false)] // Without ProcessedAtUtc
    public async Task IsProcessed_BasedOnProcessedAtUtc_ShouldReflectState(bool isProcessed)
    {
        // Arrange
        var messageId = $"isprocessed-test-{isProcessed}";
        var message = new InboxMessage
        {
            MessageId = messageId,
            RequestType = "TestRequest",
            ReceivedAtUtc = DateTime.UtcNow,
            ProcessedAtUtc = isProcessed ? DateTime.UtcNow : null,
            ExpiresAtUtc = DateTime.UtcNow.AddDays(30),
            RetryCount = 0
        };

        // Act
        await _store.AddAsync(message);

        // Assert
        var retrieved = await _store.GetMessageAsync(messageId);
        Assert.NotNull(retrieved);
        Assert.Equal(isProcessed, retrieved.IsProcessed);
    }

    #endregion
}
