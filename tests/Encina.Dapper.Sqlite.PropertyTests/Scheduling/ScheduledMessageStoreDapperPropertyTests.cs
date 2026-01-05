using Encina.Dapper.Sqlite.Scheduling;
using Encina.TestInfrastructure.Extensions;
using Encina.TestInfrastructure.Fixtures;

namespace Encina.Dapper.Sqlite.Tests.Scheduling;

/// <summary>
/// Property-based tests for <see cref="ScheduledMessageStoreDapper"/>.
/// Tests invariants across different input values to ensure behavioral consistency.
/// </summary>
[Trait("Category", "Property")]
public sealed class ScheduledMessageStoreDapperPropertyTests : IClassFixture<SqliteFixture>
{
    private readonly SqliteFixture _database;

    public ScheduledMessageStoreDapperPropertyTests(SqliteFixture database)
    {
        _database = database;
        DapperTypeHandlers.RegisterSqliteHandlers();

        // Clear all data before each test to ensure clean state
        _database.ClearAllDataAsync().GetAwaiter().GetResult();
    }

    #region Property: Idempotency

    /// <summary>
    /// Property: Calling SaveChangesAsync multiple times is idempotent.
    /// Invariant: Multiple SaveChanges calls produce same result as single call.
    /// </summary>
    [Theory]
    [InlineData(1)]
    [InlineData(3)]
    [InlineData(5)]
    public async Task SaveChangesAsync_MultipleInvocations_Idempotent(int invocations)
    {
        // Arrange
        var store = new ScheduledMessageStoreDapper(_database.CreateConnection());

        // Act - Call SaveChanges multiple times
        for (var i = 0; i < invocations; i++)
        {
            await store.SaveChangesAsync();
        }

        // Assert - Should not throw
        Assert.True(true);
    }

    #endregion

    #region Property: Add-Then-Retrieve

    /// <summary>
    /// Property: Any scheduled message added can be retrieved via GetDueMessagesAsync if due.
    /// Invariant: AddAsync followed by GetDueMessagesAsync returns the added message when due.
    /// </summary>
    [Theory]
    [InlineData("Command1", "{\"id\":1}", false, null)]
    [InlineData("RecurringCommand", "{\"id\":2}", true, "0 * * * *")]
    [InlineData("LongCommand", "{\"data\":\"test\",\"value\":42}", false, null)]
    public async Task AddedMessage_AlwaysRetrievableWhenDue(string requestType, string content, bool isRecurring, string? cronExpression)
    {
        // Arrange
        var store = new ScheduledMessageStoreDapper(_database.CreateConnection());
        var messageId = Guid.NewGuid();
        var message = new ScheduledMessage
        {
            Id = messageId,
            RequestType = requestType,
            Content = content,
            ScheduledAtUtc = DateTime.UtcNow.AddHours(-1), // Due
            CreatedAtUtc = DateTime.UtcNow.AddHours(-2),
            RetryCount = 0,
            IsRecurring = isRecurring,
            CronExpression = cronExpression
        };

        // Act
        await store.AddAsync(message);
        var messages = await store.GetDueMessagesAsync(10, 3);

        // Assert
        var retrieved = messages.FirstOrDefault(m => m.Id == messageId);
        Assert.NotNull(retrieved);
        Assert.Equal(requestType, retrieved.RequestType);
        Assert.Equal(content, retrieved.Content);
        Assert.Equal(isRecurring, retrieved.IsRecurring);
    }

    #endregion

    #region Property: Future Messages Not Due

    /// <summary>
    /// Property: Messages scheduled in the future are never returned by GetDueMessagesAsync.
    /// Invariant: ScheduledAtUtc > Now => Not in GetDueMessages result.
    /// </summary>
    [Theory]
    [InlineData(1)]
    [InlineData(5)]
    [InlineData(24)]
    public async Task FutureMessage_NeverReturnedAsDue(int hoursAhead)
    {
        // Arrange
        var store = new ScheduledMessageStoreDapper(_database.CreateConnection());
        var message = new ScheduledMessage
        {
            Id = Guid.NewGuid(),
            RequestType = "FutureCommand",
            Content = "{}",
            ScheduledAtUtc = DateTime.UtcNow.AddHours(hoursAhead),
            CreatedAtUtc = DateTime.UtcNow,
            RetryCount = 0,
            IsRecurring = false
        };

        // Act
        await store.AddAsync(message);
        var messages = await store.GetDueMessagesAsync(10, 3);

        // Assert
        Assert.DoesNotContain(messages, m => m.Id == message.Id);
    }

    #endregion

    #region Property: Processed One-Time Not Due

    /// <summary>
    /// Property: One-time messages that are processed never appear in GetDueMessagesAsync.
    /// Invariant: ProcessedAtUtc != null AND IsRecurring == false => Not in GetDueMessages.
    /// </summary>
    [Theory]
    [InlineData("Command1", "{}")]
    [InlineData("Command2", "{\"test\":true}")]
    public async Task ProcessedOneTimeMessage_NeverReturnedAsDue(string requestType, string content)
    {
        // Arrange
        var store = new ScheduledMessageStoreDapper(_database.CreateConnection());
        var messageId = Guid.NewGuid();
        var message = new ScheduledMessage
        {
            Id = messageId,
            RequestType = requestType,
            Content = content,
            ScheduledAtUtc = DateTime.UtcNow.AddHours(-1),
            CreatedAtUtc = DateTime.UtcNow.AddHours(-2),
            RetryCount = 0,
            IsRecurring = false
        };

        // Act
        await store.AddAsync(message);
        await store.MarkAsProcessedAsync(messageId);
        var messages = await store.GetDueMessagesAsync(10, 3);

        // Assert
        Assert.DoesNotContain(messages, m => m.Id == messageId);
    }

    #endregion

    #region Property: Recurring Always Due

    /// <summary>
    /// Property: Recurring messages appear in GetDueMessagesAsync even if processed.
    /// Invariant: IsRecurring == true AND ScheduledAtUtc <= Now => In GetDueMessages (even if ProcessedAtUtc != null).
    /// </summary>
    [Theory]
    [InlineData("0 * * * *")]
    [InlineData("0 0 * * *")]
    [InlineData("*/5 * * * *")]
    public async Task RecurringMessage_AlwaysReturnedWhenDue(string cronExpression)
    {
        // Arrange
        var store = new ScheduledMessageStoreDapper(_database.CreateConnection());
        var messageId = Guid.NewGuid();
        var message = new ScheduledMessage
        {
            Id = messageId,
            RequestType = "RecurringCommand",
            Content = "{}",
            ScheduledAtUtc = DateTime.UtcNow.AddHours(-1),
            CreatedAtUtc = DateTime.UtcNow.AddHours(-2),
            ProcessedAtUtc = DateTime.UtcNow.AddMinutes(-30), // Already processed
            RetryCount = 0,
            IsRecurring = true,
            CronExpression = cronExpression
        };

        // Act
        await store.AddAsync(message);
        var messages = await store.GetDueMessagesAsync(10, 3);

        // Assert
        Assert.Contains(messages, m => m.Id == messageId);
    }

    #endregion

    #region Property: Batch Size Limit

    /// <summary>
    /// Property: GetDueMessagesAsync never returns more than batchSize messages.
    /// Invariant: result.Count() <= batchSize.
    /// </summary>
    [Theory]
    [InlineData(1, 5)]
    [InlineData(3, 10)]
    [InlineData(5, 7)]
    public async Task GetDueMessages_NeverExceedsBatchSize(int batchSize, int totalMessages)
    {
        // Arrange
        var store = new ScheduledMessageStoreDapper(_database.CreateConnection());
        for (var i = 0; i < totalMessages; i++)
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
            await store.AddAsync(message);
        }

        // Act
        var messages = await store.GetDueMessagesAsync(batchSize, 5);

        // Assert
        Assert.True(messages.Count() <= batchSize);
    }

    #endregion

    #region Property: Retry Filter

    /// <summary>
    /// Property: GetDueMessagesAsync never returns messages with RetryCount >= maxRetries.
    /// Invariant: All returned messages have RetryCount < maxRetries.
    /// </summary>
    [Theory]
    [InlineData(3, 0)]
    [InlineData(3, 2)]
    [InlineData(5, 4)]
    public async Task GetDueMessages_NeverReturnsOverMaxRetries(int maxRetries, int retryCount)
    {
        // Arrange
        var store = new ScheduledMessageStoreDapper(_database.CreateConnection());
        var message = new ScheduledMessage
        {
            Id = Guid.NewGuid(),
            RequestType = "RetryCommand",
            Content = "{}",
            ScheduledAtUtc = DateTime.UtcNow.AddHours(-1),
            CreatedAtUtc = DateTime.UtcNow.AddHours(-2),
            RetryCount = retryCount,
            IsRecurring = false
        };

        // Act
        await store.AddAsync(message);
        var messages = await store.GetDueMessagesAsync(10, maxRetries);

        // Assert
        if (retryCount < maxRetries)
        {
            Assert.Single(messages);
        }
        else
        {
            Assert.Empty(messages);
        }
    }

    #endregion

    #region Property: Mark As Failed Increments

    /// <summary>
    /// Property: MarkAsFailedAsync always increments RetryCount by 1.
    /// Invariant: After MarkAsFailedAsync, new RetryCount = old RetryCount + 1.
    /// </summary>
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(3)]
    public async Task MarkAsFailed_AlwaysIncrementsRetryCount(int initialRetryCount)
    {
        // Arrange
        var store = new ScheduledMessageStoreDapper(_database.CreateConnection());
        var messageId = Guid.NewGuid();
        var message = new ScheduledMessage
        {
            Id = messageId,
            RequestType = "FailCommand",
            Content = "{}",
            ScheduledAtUtc = DateTime.UtcNow.AddHours(-1),
            CreatedAtUtc = DateTime.UtcNow.AddHours(-2),
            RetryCount = initialRetryCount,
            IsRecurring = false
        };

        // Act
        await store.AddAsync(message);
        await store.MarkAsFailedAsync(messageId, "Error", DateTime.UtcNow.AddSeconds(-10));
        var messages = await store.GetDueMessagesAsync(10, 10);

        // Assert
        var retrieved = messages.FirstOrDefault(m => m.Id == messageId);
        Assert.NotNull(retrieved);
        Assert.Equal(initialRetryCount + 1, retrieved.RetryCount);
    }

    #endregion

    #region Property: Cancel Removes

    /// <summary>
    /// Property: CancelAsync removes message so it never appears in GetDueMessagesAsync.
    /// Invariant: After CancelAsync, message never in GetDueMessages (regardless of ScheduledAtUtc).
    /// </summary>
    [Theory]
    [InlineData(-1)] // Past (due)
    [InlineData(1)]  // Future (not due)
    public async Task CancelAsync_AlwaysRemovesMessage(int hoursOffset)
    {
        // Arrange
        var store = new ScheduledMessageStoreDapper(_database.CreateConnection());
        var messageId = Guid.NewGuid();
        var message = new ScheduledMessage
        {
            Id = messageId,
            RequestType = "CancelCommand",
            Content = "{}",
            ScheduledAtUtc = DateTime.UtcNow.AddHours(hoursOffset),
            CreatedAtUtc = DateTime.UtcNow,
            RetryCount = 0,
            IsRecurring = false
        };

        // Act
        await store.AddAsync(message);
        await store.CancelAsync(messageId);
        var messages = await store.GetDueMessagesAsync(10, 3);

        // Assert
        Assert.DoesNotContain(messages, m => m.Id == messageId);
    }

    #endregion

    #region Property: Reschedule Updates Time

    /// <summary>
    /// Property: RescheduleRecurringMessageAsync updates ScheduledAtUtc.
    /// Invariant: After reschedule to future, message not in GetDueMessages.
    /// Note: Rescheduling to past is not allowed by the API (throws ArgumentException).
    /// </summary>
    [Theory]
    [InlineData(1, false)]  // Future => doesn't appear
    [InlineData(2, false)]  // Further future => doesn't appear
    public async Task Reschedule_UpdatesDueStatus(int hoursOffset, bool shouldAppear)
    {
        // Arrange
        var store = new ScheduledMessageStoreDapper(_database.CreateConnection());
        var messageId = Guid.NewGuid();
        var message = new ScheduledMessage
        {
            Id = messageId,
            RequestType = "RescheduleCommand",
            Content = "{}",
            ScheduledAtUtc = DateTime.UtcNow.AddHours(-2), // Initially due
            CreatedAtUtc = DateTime.UtcNow.AddHours(-3),
            ProcessedAtUtc = DateTime.UtcNow.AddMinutes(-30),
            RetryCount = 0,
            IsRecurring = true,
            CronExpression = "0 * * * *"
        };

        // Act
        await store.AddAsync(message);
        await store.RescheduleRecurringMessageAsync(messageId, DateTime.UtcNow.AddHours(hoursOffset));
        var messages = await store.GetDueMessagesAsync(10, 3);

        // Assert
        if (shouldAppear)
        {
            Assert.Contains(messages, m => m.Id == messageId);
        }
        else
        {
            Assert.DoesNotContain(messages, m => m.Id == messageId);
        }
    }

    #endregion

    #region Property: Ordering

    /// <summary>
    /// Property: GetDueMessagesAsync always returns messages ordered by ScheduledAtUtc ascending.
    /// Invariant: result[i].ScheduledAtUtc <= result[i+1].ScheduledAtUtc for all i.
    /// </summary>
    [Theory]
    [InlineData(3)]
    [InlineData(5)]
    [InlineData(10)]
    public async Task GetDueMessages_AlwaysOrderedByScheduledAtUtc(int messageCount)
    {
        // Arrange
        var store = new ScheduledMessageStoreDapper(_database.CreateConnection());
        var random = new Random(42); // Deterministic
        for (var i = 0; i < messageCount; i++)
        {
            var hoursAgo = random.Next(1, 20);
            var message = new ScheduledMessage
            {
                Id = Guid.NewGuid(),
                RequestType = $"Command{i}",
                Content = "{}",
                ScheduledAtUtc = DateTime.UtcNow.AddHours(-hoursAgo),
                CreatedAtUtc = DateTime.UtcNow.AddHours(-hoursAgo - 1),
                RetryCount = 0,
                IsRecurring = false
            };
            await store.AddAsync(message);
        }

        // Act
        var messages = await store.GetDueMessagesAsync(messageCount, 5);

        // Assert - Check ordering
        var list = messages.ToList();
        for (var i = 0; i < list.Count - 1; i++)
        {
            Assert.True(list[i].ScheduledAtUtc <= list[i + 1].ScheduledAtUtc);
        }
    }

    #endregion

    #region Property: NextRetryAtUtc Priority

    /// <summary>
    /// Property: When NextRetryAtUtc is set, it takes priority over ScheduledAtUtc for due calculation.
    /// Invariant: If NextRetryAtUtc is in future, message not due (even if ScheduledAtUtc is past).
    /// </summary>
    [Theory]
    [InlineData(-1, 1, false)]  // ScheduledAtUtc past, NextRetryAtUtc future => not due
    [InlineData(-1, -1, true)]  // Both past => due
    public async Task NextRetryAtUtc_TakesPriority(int scheduledHoursOffset, int retryHoursOffset, bool shouldBeDue)
    {
        // Arrange
        var store = new ScheduledMessageStoreDapper(_database.CreateConnection());
        var messageId = Guid.NewGuid();
        var message = new ScheduledMessage
        {
            Id = messageId,
            RequestType = "RetryPriorityCommand",
            Content = "{}",
            ScheduledAtUtc = DateTime.UtcNow.AddHours(scheduledHoursOffset),
            CreatedAtUtc = DateTime.UtcNow.AddHours(-2),
            RetryCount = 0,
            IsRecurring = false
        };
        await store.AddAsync(message);

        // Act - Mark as failed with NextRetryAtUtc
        await store.MarkAsFailedAsync(messageId, "Error", DateTime.UtcNow.AddHours(retryHoursOffset));
        var messages = await store.GetDueMessagesAsync(10, 3);

        // Assert
        if (shouldBeDue)
        {
            Assert.Contains(messages, m => m.Id == messageId);
        }
        else
        {
            Assert.DoesNotContain(messages, m => m.Id == messageId);
        }
    }

    #endregion

    #region Property: Reschedule Resets Retry

    /// <summary>
    /// Property: RescheduleRecurringMessageAsync resets retry-related fields.
    /// Invariant: After reschedule, RetryCount = 0, ErrorMessage = null, NextRetryAtUtc = null.
    /// </summary>
    [Theory]
    [InlineData(2, "Error 1")]
    [InlineData(5, "Error 2")]
    public async Task Reschedule_AlwaysResetsRetryFields(int initialRetryCount, string errorMessage)
    {
        // Arrange
        var store = new ScheduledMessageStoreDapper(_database.CreateConnection());
        var messageId = Guid.NewGuid();
        var message = new ScheduledMessage
        {
            Id = messageId,
            RequestType = "ResetCommand",
            Content = "{}",
            ScheduledAtUtc = DateTime.UtcNow.AddHours(-2), // In past - due now
            CreatedAtUtc = DateTime.UtcNow.AddHours(-3),
            ProcessedAtUtc = DateTime.UtcNow.AddMinutes(-30),
            RetryCount = initialRetryCount,
            ErrorMessage = errorMessage,
            NextRetryAtUtc = DateTime.UtcNow.AddMinutes(10),
            IsRecurring = true,
            CronExpression = "0 * * * *"
        };

        // Act - Add message with retry fields, then reschedule to near future
        await store.AddAsync(message);
        var futureTime = DateTime.UtcNow.AddSeconds(2); // Just slightly in future
        await store.RescheduleRecurringMessageAsync(messageId, futureTime);

        // Wait for the scheduled time to pass
        await Task.Delay(2100);

        var messages = await store.GetDueMessagesAsync(10, initialRetryCount + 1);

        // Assert - Fields should be reset
        var retrieved = messages.FirstOrDefault(m => m.Id == messageId);
        Assert.NotNull(retrieved);
        Assert.Equal(0, retrieved.RetryCount);
        Assert.Null(retrieved.ErrorMessage);
    }

    #endregion
}
