using Encina.EntityFrameworkCore;
using Encina.EntityFrameworkCore.Scheduling;
using Encina.Messaging.Scheduling;
using Encina.TestInfrastructure.Extensions;
using Encina.Testing.Shouldly;
using LanguageExt;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using Shouldly;
using Xunit;

namespace Encina.UnitTests.EntityFrameworkCore.Scheduling;

/// <summary>
/// Additional unit tests for <see cref="ScheduledMessageStoreEF"/> focusing on constructor validation
/// and error handling paths.
/// </summary>
public class ScheduledMessageStoreEFAdditionalTests : IDisposable
{
    private readonly TestDbContext _dbContext;
    private readonly ScheduledMessageStoreEF _store;

    public ScheduledMessageStoreEFAdditionalTests()
    {
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _dbContext = new TestDbContext(options);
        _store = new ScheduledMessageStoreEF(_dbContext);
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_NullDbContext_ThrowsArgumentNullException()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() => new ScheduledMessageStoreEF(null!));
    }

    #endregion

    #region AddAsync Tests

    [Fact]
    public async Task AddAsync_NullMessage_ThrowsArgumentNullException()
    {
        // Act & Assert
        await Should.ThrowAsync<ArgumentNullException>(() =>
            _store.AddAsync(null!));
    }

    [Fact]
    public async Task AddAsync_WrongMessageType_ReturnsLeftError()
    {
        // Arrange
        var wrongMessage = Substitute.For<IScheduledMessage>();

        // Act
        var result = await _store.AddAsync(wrongMessage);

        // Assert
        result.IsLeft.ShouldBeTrue();
        var error = result.ShouldBeLeft();
        error.Message.ShouldContain("ScheduledMessageStoreEF requires messages of type ScheduledMessage");
    }

    #endregion

    #region MarkAsProcessedAsync Tests

    [Fact]
    public async Task MarkAsProcessedAsync_NonExistentMessage_ReturnsRight()
    {
        // Act
        var result = await _store.MarkAsProcessedAsync(Guid.NewGuid());

        // Assert
        result.IsRight.ShouldBeTrue();
    }

    #endregion

    #region MarkAsFailedAsync Tests

    [Fact]
    public async Task MarkAsFailedAsync_NullErrorMessage_ThrowsArgumentNullException()
    {
        // Act & Assert
        await Should.ThrowAsync<ArgumentNullException>(() =>
            _store.MarkAsFailedAsync(Guid.NewGuid(), null!, DateTime.UtcNow));
    }

    [Fact]
    public async Task MarkAsFailedAsync_NonExistentMessage_ReturnsRight()
    {
        // Act
        var result = await _store.MarkAsFailedAsync(Guid.NewGuid(), "Error", DateTime.UtcNow);

        // Assert
        result.IsRight.ShouldBeTrue();
    }

    [Fact]
    public async Task MarkAsFailedAsync_WithNullNextRetry_SetsNullNextRetryAtUtc()
    {
        // Arrange
        var now = DateTime.UtcNow;
        var message = new ScheduledMessage
        {
            Id = Guid.NewGuid(),
            RequestType = "TestCommand",
            Content = "{}",
            ScheduledAtUtc = now.AddMinutes(-10),
            CreatedAtUtc = now.AddHours(-1),
            RetryCount = 0,
            IsRecurring = false
        };

        await _dbContext.ScheduledMessages.AddAsync(message);
        await _dbContext.SaveChangesAsync();

        // Act
        (await _store.MarkAsFailedAsync(message.Id, "Test error", null)).ShouldBeRight();
        (await _store.SaveChangesAsync()).ShouldBeRight();

        // Assert
        var updated = await _dbContext.ScheduledMessages.FindAsync(message.Id);
        updated!.NextRetryAtUtc.ShouldBeNull();
        updated.ErrorMessage.ShouldBe("Test error");
        updated.RetryCount.ShouldBe(1);
    }

    #endregion

    #region RescheduleRecurringMessageAsync Tests

    [Fact]
    public async Task RescheduleRecurringMessageAsync_NonExistentMessage_ReturnsRight()
    {
        // Act
        var result = await _store.RescheduleRecurringMessageAsync(Guid.NewGuid(), DateTime.UtcNow.AddDays(1));

        // Assert
        result.IsRight.ShouldBeTrue();
    }

    [Fact]
    public async Task RescheduleRecurringMessageAsync_ResetsAllRetryFields()
    {
        // Arrange
        var now = DateTime.UtcNow;
        var message = new ScheduledMessage
        {
            Id = Guid.NewGuid(),
            RequestType = "RecurringCommand",
            Content = "{}",
            ScheduledAtUtc = now.AddMinutes(-10),
            CreatedAtUtc = now.AddHours(-1),
            ProcessedAtUtc = now.AddMinutes(-5),
            ErrorMessage = "Previous error",
            RetryCount = 2,
            NextRetryAtUtc = now.AddMinutes(30),
            IsRecurring = true,
            CronExpression = "0 0 * * *"
        };

        await _dbContext.ScheduledMessages.AddAsync(message);
        await _dbContext.SaveChangesAsync();

        var nextScheduled = now.AddHours(24);

        // Act
        (await _store.RescheduleRecurringMessageAsync(message.Id, nextScheduled)).ShouldBeRight();
        (await _store.SaveChangesAsync()).ShouldBeRight();

        // Assert
        var updated = await _dbContext.ScheduledMessages.FindAsync(message.Id);
        updated!.ScheduledAtUtc.ShouldBe(nextScheduled);
        updated.ProcessedAtUtc.ShouldBeNull();
        updated.ErrorMessage.ShouldBeNull();
        updated.RetryCount.ShouldBe(0);
        updated.NextRetryAtUtc.ShouldBeNull();
    }

    #endregion

    #region CancelAsync Tests

    [Fact]
    public async Task CancelAsync_NonExistentMessage_ReturnsRight()
    {
        // Act
        var result = await _store.CancelAsync(Guid.NewGuid());

        // Assert
        result.IsRight.ShouldBeTrue();
    }

    #endregion

    #region GetDueMessagesAsync Tests

    [Fact]
    public async Task GetDueMessagesAsync_WithNextRetryInFuture_ExcludesMessage()
    {
        // Arrange
        var now = DateTime.UtcNow;
        var message = new ScheduledMessage
        {
            Id = Guid.NewGuid(),
            RequestType = "FutureRetryCommand",
            Content = "{}",
            ScheduledAtUtc = now.AddMinutes(-10),
            CreatedAtUtc = now.AddHours(-1),
            RetryCount = 1,
            NextRetryAtUtc = now.AddHours(1), // Future retry
            IsRecurring = false
        };

        await _dbContext.ScheduledMessages.AddAsync(message);
        await _dbContext.SaveChangesAsync();

        // Act
        var messages = (await _store.GetDueMessagesAsync(batchSize: 10, maxRetries: 3)).ShouldBeRight();

        // Assert
        messages.ShouldBeEmpty();
    }

    [Fact]
    public async Task GetDueMessagesAsync_WithNextRetryInPast_IncludesMessage()
    {
        // Arrange
        var now = DateTime.UtcNow;
        var message = new ScheduledMessage
        {
            Id = Guid.NewGuid(),
            RequestType = "PastRetryCommand",
            Content = "{}",
            ScheduledAtUtc = now.AddMinutes(-30),
            CreatedAtUtc = now.AddHours(-1),
            RetryCount = 1,
            NextRetryAtUtc = now.AddMinutes(-5), // Past retry
            IsRecurring = false
        };

        await _dbContext.ScheduledMessages.AddAsync(message);
        await _dbContext.SaveChangesAsync();

        // Act
        var messages = (await _store.GetDueMessagesAsync(batchSize: 10, maxRetries: 3)).ShouldBeRight();

        // Assert
        messages.Count().ShouldBe(1);
    }

    [Fact]
    public async Task GetDueMessagesAsync_ReturnsEmptyWhenNoMessages()
    {
        // Act
        var messages = (await _store.GetDueMessagesAsync(batchSize: 10, maxRetries: 3)).ShouldBeRight();

        // Assert
        messages.ShouldBeEmpty();
    }

    [Fact]
    public async Task GetDueMessagesAsync_OrdersByScheduledAtUtc()
    {
        // Arrange
        var now = DateTime.UtcNow;
        var older = new ScheduledMessage
        {
            Id = Guid.NewGuid(),
            RequestType = "OlderCommand",
            Content = "{}",
            ScheduledAtUtc = now.AddMinutes(-30),
            CreatedAtUtc = now.AddHours(-2),
            RetryCount = 0,
            IsRecurring = false
        };

        var newer = new ScheduledMessage
        {
            Id = Guid.NewGuid(),
            RequestType = "NewerCommand",
            Content = "{}",
            ScheduledAtUtc = now.AddMinutes(-10),
            CreatedAtUtc = now.AddHours(-1),
            RetryCount = 0,
            IsRecurring = false
        };

        // Add in reverse order
        await _dbContext.ScheduledMessages.AddRangeAsync(newer, older);
        await _dbContext.SaveChangesAsync();

        // Act
        var messages = (await _store.GetDueMessagesAsync(batchSize: 10, maxRetries: 3)).ShouldBeRight().ToList();

        // Assert
        messages.Count.ShouldBe(2);
        messages[0].Id.ShouldBe(older.Id);
        messages[1].Id.ShouldBe(newer.Id);
    }

    #endregion

    public void Dispose()
    {
        _dbContext.Dispose();
        GC.SuppressFinalize(this);
    }
}
