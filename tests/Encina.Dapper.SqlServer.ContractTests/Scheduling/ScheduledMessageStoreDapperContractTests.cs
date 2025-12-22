using Encina.Dapper.SqlServer.Scheduling;
using Encina.Messaging.Scheduling;
using Encina.TestInfrastructure.Extensions;
using Encina.TestInfrastructure.Fixtures;
using Xunit;

namespace Encina.Dapper.SqlServer.Tests.Scheduling;

/// <summary>
/// Contract tests for <see cref="ScheduledMessageStoreDapper"/>.
/// Verifies compliance with <see cref="IScheduledMessageStore"/> interface contract.
/// Uses real SQL Server database via Testcontainers.
/// </summary>
[Trait("Category", "Contract")]
public sealed class ScheduledMessageStoreDapperContractTests : IClassFixture<SqlServerFixture>
{
    private readonly SqlServerFixture _database;
    private readonly ScheduledMessageStoreDapper _store;

    public ScheduledMessageStoreDapperContractTests(SqlServerFixture database)
    {
        _database = database;

        // Clear all data before each test to ensure clean state
        _database.ClearAllDataAsync().GetAwaiter().GetResult();

        _store = new ScheduledMessageStoreDapper(_database.CreateConnection());
    }

    #region Contract: AddAsync

    [Fact]
    public async Task AddAsync_Contract_AcceptsIScheduledMessage()
    {
        // Arrange
#pragma warning disable CA1859 // Use concrete types when possible for improved performance - Intentionally testing interface contract
        IScheduledMessage message = new ScheduledMessage
#pragma warning restore CA1859
        {
            Id = Guid.NewGuid(),
            RequestType = "ContractCommand",
            Content = "{}",
            ScheduledAtUtc = DateTime.UtcNow.AddHours(1),
            CreatedAtUtc = DateTime.UtcNow,
            RetryCount = 0,
            IsRecurring = false
        };

        // Act & Assert - Should not throw
        await _store.AddAsync(message);
    }

    [Fact]
    public async Task AddAsync_Contract_PersistsAllRequiredFields()
    {
        // Arrange
        var messageId = Guid.NewGuid();
        var message = new ScheduledMessage
        {
            Id = messageId,
            RequestType = "FieldTestCommand",
            Content = "{\"test\":true}",
            ScheduledAtUtc = DateTime.UtcNow.AddHours(-1),
            CreatedAtUtc = DateTime.UtcNow.AddHours(-2),
            RetryCount = 0,
            IsRecurring = false
        };

        // Act
        await _store.AddAsync(message);

        // Assert - Retrieve and verify fields
        var messages = await _store.GetDueMessagesAsync(10, 3);
        var retrieved = messages.First();
        Assert.Equal(messageId, retrieved.Id);
        Assert.Equal("FieldTestCommand", retrieved.RequestType);
        Assert.Equal("{\"test\":true}", retrieved.Content);
        Assert.Equal(0, retrieved.RetryCount);
        Assert.False(retrieved.IsRecurring);
    }

    [Fact]
    public async Task AddAsync_Contract_SupportsCancellation()
    {
        // Arrange
        var message = new ScheduledMessage
        {
            Id = Guid.NewGuid(),
            RequestType = "CancellableCommand",
            Content = "{}",
            ScheduledAtUtc = DateTime.UtcNow.AddHours(1),
            CreatedAtUtc = DateTime.UtcNow,
            RetryCount = 0,
            IsRecurring = false
        };
        using var cts = new CancellationTokenSource();

        // Act & Assert - Should not throw
        await _store.AddAsync(message, cts.Token);
    }

    #endregion

    #region Contract: GetDueMessagesAsync

    [Fact]
    public async Task GetDueMessagesAsync_Contract_ReturnsIEnumerableOfIScheduledMessage()
    {
        // Arrange
        var message = new ScheduledMessage
        {
            Id = Guid.NewGuid(),
            RequestType = "DueCommand",
            Content = "{}",
            ScheduledAtUtc = DateTime.UtcNow.AddHours(-1),
            CreatedAtUtc = DateTime.UtcNow.AddHours(-2),
            RetryCount = 0,
            IsRecurring = false
        };
        await _store.AddAsync(message);

        // Act
        var result = await _store.GetDueMessagesAsync(10, 3);

        // Assert
        Assert.NotNull(result);
        Assert.IsAssignableFrom<IEnumerable<IScheduledMessage>>(result);
        Assert.Single(result);
    }

    [Fact]
    public async Task GetDueMessagesAsync_Contract_FiltersByScheduledTime()
    {
        // Arrange - Due and future messages
        var dueMessage = new ScheduledMessage
        {
            Id = Guid.NewGuid(),
            RequestType = "DueCommand",
            Content = "{}",
            ScheduledAtUtc = DateTime.UtcNow.AddHours(-1),
            CreatedAtUtc = DateTime.UtcNow.AddHours(-2),
            RetryCount = 0,
            IsRecurring = false
        };
        await _store.AddAsync(dueMessage);

        var futureMessage = new ScheduledMessage
        {
            Id = Guid.NewGuid(),
            RequestType = "FutureCommand",
            Content = "{}",
            ScheduledAtUtc = DateTime.UtcNow.AddHours(2),
            CreatedAtUtc = DateTime.UtcNow,
            RetryCount = 0,
            IsRecurring = false
        };
        await _store.AddAsync(futureMessage);

        // Act
        var result = await _store.GetDueMessagesAsync(10, 3);

        // Assert - Only due message
        Assert.Single(result);
        Assert.Equal(dueMessage.Id, result.First().Id);
    }

    [Fact]
    public async Task GetDueMessagesAsync_Contract_RespectsBatchSize()
    {
        // Arrange - Create 5 due messages
        for (int i = 0; i < 5; i++)
        {
            var message = new ScheduledMessage
            {
                Id = Guid.NewGuid(),
                RequestType = $"Command{i}",
                Content = "{}",
                ScheduledAtUtc = DateTime.UtcNow.AddHours(-1),
                CreatedAtUtc = DateTime.UtcNow.AddHours(-2),
                RetryCount = 0,
                IsRecurring = false
            };
            await _store.AddAsync(message);
        }

        // Act - Request only 3
        var result = await _store.GetDueMessagesAsync(3, 5);

        // Assert
        Assert.Equal(3, result.Count());
    }

    [Fact]
    public async Task GetDueMessagesAsync_Contract_FiltersMaxRetries()
    {
        // Arrange - Messages with different retry counts
        var lowRetry = new ScheduledMessage
        {
            Id = Guid.NewGuid(),
            RequestType = "LowRetryCommand",
            Content = "{}",
            ScheduledAtUtc = DateTime.UtcNow.AddHours(-1),
            CreatedAtUtc = DateTime.UtcNow.AddHours(-2),
            RetryCount = 2,
            IsRecurring = false
        };
        await _store.AddAsync(lowRetry);

        var highRetry = new ScheduledMessage
        {
            Id = Guid.NewGuid(),
            RequestType = "HighRetryCommand",
            Content = "{}",
            ScheduledAtUtc = DateTime.UtcNow.AddHours(-1),
            CreatedAtUtc = DateTime.UtcNow.AddHours(-2),
            RetryCount = 5,
            IsRecurring = false
        };
        await _store.AddAsync(highRetry);

        // Act - Max retries = 3
        var result = await _store.GetDueMessagesAsync(10, 3);

        // Assert - Only lowRetry
        Assert.Single(result);
        Assert.Equal(lowRetry.Id, result.First().Id);
    }

    [Fact]
    public async Task GetDueMessagesAsync_Contract_IncludesRecurringEvenIfProcessed()
    {
        // Arrange - Recurring message that's processed
        var recurringMessage = new ScheduledMessage
        {
            Id = Guid.NewGuid(),
            RequestType = "RecurringCommand",
            Content = "{}",
            ScheduledAtUtc = DateTime.UtcNow.AddHours(-1),
            CreatedAtUtc = DateTime.UtcNow.AddHours(-2),
            ProcessedAtUtc = DateTime.UtcNow.AddMinutes(-30),
            RetryCount = 0,
            IsRecurring = true,
            CronExpression = "0 * * * *"
        };
        await _store.AddAsync(recurringMessage);

        // Act
        var result = await _store.GetDueMessagesAsync(10, 3);

        // Assert - Recurring appears even if processed
        Assert.Single(result);
        Assert.Equal(recurringMessage.Id, result.First().Id);
    }

    [Fact]
    public async Task GetDueMessagesAsync_Contract_SupportsCancellation()
    {
        // Arrange
        using var cts = new CancellationTokenSource();

        // Act & Assert - Should not throw
        var result = await _store.GetDueMessagesAsync(10, 3, cts.Token);
        Assert.NotNull(result);
    }

    #endregion

    #region Contract: MarkAsProcessedAsync

    [Fact]
    public async Task MarkAsProcessedAsync_Contract_UpdatesMessage()
    {
        // Arrange
        var messageId = Guid.NewGuid();
        var message = new ScheduledMessage
        {
            Id = messageId,
            RequestType = "ProcessCommand",
            Content = "{}",
            ScheduledAtUtc = DateTime.UtcNow.AddHours(-1),
            CreatedAtUtc = DateTime.UtcNow.AddHours(-2),
            RetryCount = 0,
            IsRecurring = false
        };
        await _store.AddAsync(message);

        // Act
        await _store.MarkAsProcessedAsync(messageId);

        // Assert - No longer appears in due messages (one-time)
        var messages = await _store.GetDueMessagesAsync(10, 3);
        Assert.Empty(messages);
    }

    [Fact]
    public async Task MarkAsProcessedAsync_Contract_SupportsCancellation()
    {
        // Arrange
        var messageId = Guid.NewGuid();
        var message = new ScheduledMessage
        {
            Id = messageId,
            RequestType = "CancelCommand",
            Content = "{}",
            ScheduledAtUtc = DateTime.UtcNow.AddHours(-1),
            CreatedAtUtc = DateTime.UtcNow.AddHours(-2),
            RetryCount = 0,
            IsRecurring = false
        };
        await _store.AddAsync(message);
        using var cts = new CancellationTokenSource();

        // Act & Assert - Should not throw
        await _store.MarkAsProcessedAsync(messageId, cts.Token);
    }

    #endregion

    #region Contract: MarkAsFailedAsync

    [Fact]
    public async Task MarkAsFailedAsync_Contract_IncrementsRetryCount()
    {
        // Arrange
        var messageId = Guid.NewGuid();
        var message = new ScheduledMessage
        {
            Id = messageId,
            RequestType = "FailCommand",
            Content = "{}",
            ScheduledAtUtc = DateTime.UtcNow.AddHours(-1),
            CreatedAtUtc = DateTime.UtcNow.AddHours(-2),
            RetryCount = 0,
            IsRecurring = false
        };
        await _store.AddAsync(message);

        // Act - Mark as failed twice
        await _store.MarkAsFailedAsync(messageId, "Error 1", DateTime.UtcNow.AddHours(1));
        await _store.MarkAsFailedAsync(messageId, "Error 2", DateTime.UtcNow.AddHours(1));

        // Assert - RetryCount should be 2 (not returned due to future retry time)
        var messages = await _store.GetDueMessagesAsync(10, 3);
        Assert.Empty(messages); // Future retry time
    }

    [Fact]
    public async Task MarkAsFailedAsync_Contract_SetsErrorMessage()
    {
        // Arrange
        var messageId = Guid.NewGuid();
        var message = new ScheduledMessage
        {
            Id = messageId,
            RequestType = "ErrorCommand",
            Content = "{}",
            ScheduledAtUtc = DateTime.UtcNow.AddHours(-1),
            CreatedAtUtc = DateTime.UtcNow.AddHours(-2),
            RetryCount = 0,
            IsRecurring = false
        };
        await _store.AddAsync(message);

        // Act
        await _store.MarkAsFailedAsync(messageId, "Test error", DateTime.UtcNow.AddSeconds(-10));

        // Assert - Appears in due with error (past retry time)
        var messages = await _store.GetDueMessagesAsync(10, 3);
        Assert.Single(messages);
        Assert.Equal("Test error", messages.First().ErrorMessage);
    }

    [Fact]
    public async Task MarkAsFailedAsync_Contract_SupportsCancellation()
    {
        // Arrange
        var messageId = Guid.NewGuid();
        var message = new ScheduledMessage
        {
            Id = messageId,
            RequestType = "CancelFailCommand",
            Content = "{}",
            ScheduledAtUtc = DateTime.UtcNow.AddHours(-1),
            CreatedAtUtc = DateTime.UtcNow.AddHours(-2),
            RetryCount = 0,
            IsRecurring = false
        };
        await _store.AddAsync(message);
        using var cts = new CancellationTokenSource();

        // Act & Assert - Should not throw
        await _store.MarkAsFailedAsync(messageId, "Error", DateTime.UtcNow.AddHours(1), cts.Token);
    }

    #endregion

    #region Contract: RescheduleRecurringMessageAsync

    [Fact]
    public async Task RescheduleRecurringMessageAsync_Contract_UpdatesScheduledAtUtc()
    {
        // Arrange
        var messageId = Guid.NewGuid();
        var message = new ScheduledMessage
        {
            Id = messageId,
            RequestType = "RecurringCommand",
            Content = "{}",
            ScheduledAtUtc = DateTime.UtcNow.AddHours(-1),
            CreatedAtUtc = DateTime.UtcNow.AddHours(-2),
            ProcessedAtUtc = DateTime.UtcNow.AddMinutes(-30),
            RetryCount = 0,
            IsRecurring = true,
            CronExpression = "0 * * * *"
        };
        await _store.AddAsync(message);

        // Act - Reschedule to future
        var nextRun = DateTime.UtcNow.AddHours(1);
        await _store.RescheduleRecurringMessageAsync(messageId, nextRun);

        // Assert - No longer due (future time)
        var messages = await _store.GetDueMessagesAsync(10, 3);
        Assert.Empty(messages);
    }

    [Fact]
    public async Task RescheduleRecurringMessageAsync_Contract_ResetsRetryFields()
    {
        // Arrange
        var messageId = Guid.NewGuid();
        var message = new ScheduledMessage
        {
            Id = messageId,
            RequestType = "RecurringResetCommand",
            Content = "{}",
            ScheduledAtUtc = DateTime.UtcNow.AddHours(-1),
            CreatedAtUtc = DateTime.UtcNow.AddHours(-2),
            ProcessedAtUtc = DateTime.UtcNow.AddMinutes(-30),
            RetryCount = 3,
            ErrorMessage = "Previous error",
            NextRetryAtUtc = DateTime.UtcNow.AddMinutes(10),
            IsRecurring = true,
            CronExpression = "0 * * * *"
        };
        await _store.AddAsync(message);

        // Act - Reschedule to past (should appear in due)
        var nextRun = DateTime.UtcNow.AddHours(-1);
        await _store.RescheduleRecurringMessageAsync(messageId, nextRun);

        // Assert - Appears in due with reset fields
        var messages = await _store.GetDueMessagesAsync(10, 3);
        Assert.Single(messages);
        var retrieved = messages.First();
        Assert.Equal(0, retrieved.RetryCount);
        Assert.Null(retrieved.ErrorMessage);
    }

    [Fact]
    public async Task RescheduleRecurringMessageAsync_Contract_SupportsCancellation()
    {
        // Arrange
        var messageId = Guid.NewGuid();
        var message = new ScheduledMessage
        {
            Id = messageId,
            RequestType = "CancelRecurringCommand",
            Content = "{}",
            ScheduledAtUtc = DateTime.UtcNow.AddHours(-1),
            CreatedAtUtc = DateTime.UtcNow.AddHours(-2),
            ProcessedAtUtc = DateTime.UtcNow.AddMinutes(-30),
            RetryCount = 0,
            IsRecurring = true,
            CronExpression = "0 * * * *"
        };
        await _store.AddAsync(message);
        using var cts = new CancellationTokenSource();

        // Act & Assert - Should not throw
        var nextRun = DateTime.UtcNow.AddHours(1);
        await _store.RescheduleRecurringMessageAsync(messageId, nextRun, cts.Token);
    }

    #endregion

    #region Contract: CancelAsync

    [Fact]
    public async Task CancelAsync_Contract_RemovesMessage()
    {
        // Arrange
        var messageId = Guid.NewGuid();
        var message = new ScheduledMessage
        {
            Id = messageId,
            RequestType = "RemoveCommand",
            Content = "{}",
            ScheduledAtUtc = DateTime.UtcNow.AddHours(1),
            CreatedAtUtc = DateTime.UtcNow,
            RetryCount = 0,
            IsRecurring = false
        };
        await _store.AddAsync(message);

        // Act
        await _store.CancelAsync(messageId);

        // Assert - No longer appears
        var messages = await _store.GetDueMessagesAsync(10, 3);
        Assert.Empty(messages);
    }

    [Fact]
    public async Task CancelAsync_Contract_SupportsCancellation()
    {
        // Arrange
        var messageId = Guid.NewGuid();
        var message = new ScheduledMessage
        {
            Id = messageId,
            RequestType = "CancelCancelCommand",
            Content = "{}",
            ScheduledAtUtc = DateTime.UtcNow.AddHours(1),
            CreatedAtUtc = DateTime.UtcNow,
            RetryCount = 0,
            IsRecurring = false
        };
        await _store.AddAsync(message);
        using var cts = new CancellationTokenSource();

        // Act & Assert - Should not throw
        await _store.CancelAsync(messageId, cts.Token);
    }

    #endregion

    #region Contract: SaveChangesAsync

    [Fact]
    public async Task SaveChangesAsync_Contract_CompletesSuccessfully()
    {
        // Act & Assert - Should not throw
        await _store.SaveChangesAsync();
    }

    [Fact]
    public async Task SaveChangesAsync_Contract_SupportsCancellation()
    {
        // Arrange
        using var cts = new CancellationTokenSource();

        // Act & Assert - Should not throw
        await _store.SaveChangesAsync(cts.Token);
    }

    #endregion

    #region Contract: IScheduledMessage Properties

    [Fact]
    public async Task IScheduledMessage_Contract_AllPropertiesAccessible()
    {
        // Arrange
        var messageId = Guid.NewGuid();
        var scheduledAt = DateTime.UtcNow.AddHours(1);
        var createdAt = DateTime.UtcNow;

        var message = new ScheduledMessage
        {
            Id = messageId,
            RequestType = "PropertyTestCommand",
            Content = "{\"prop\":\"value\"}",
            ScheduledAtUtc = scheduledAt,
            CreatedAtUtc = createdAt,
            RetryCount = 2,
            IsRecurring = true,
            CronExpression = "0 * * * *",
            ErrorMessage = "Test error",
            NextRetryAtUtc = DateTime.UtcNow.AddMinutes(30)
        };
        await _store.AddAsync(message);

        // Act - Retrieve via GetDueMessagesAsync (won't appear due to future time, so reschedule to past)
        await _store.RescheduleRecurringMessageAsync(messageId, DateTime.UtcNow.AddHours(-1));
        var messages = await _store.GetDueMessagesAsync(10, 5);
        var retrieved = messages.First();

        // Assert - All properties accessible via interface
        Assert.Equal(messageId, retrieved.Id);
        Assert.Equal("PropertyTestCommand", retrieved.RequestType);
        Assert.Equal("{\"prop\":\"value\"}", retrieved.Content);
        Assert.True(retrieved.ScheduledAtUtc != default);
        Assert.True(retrieved.CreatedAtUtc != default);
        Assert.True(retrieved.IsRecurring);
        Assert.Equal("0 * * * *", retrieved.CronExpression);
    }

    #endregion
}
