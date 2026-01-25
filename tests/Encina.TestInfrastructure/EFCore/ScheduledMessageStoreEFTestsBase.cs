using Encina.EntityFrameworkCore.Scheduling;
using Encina.TestInfrastructure.Fixtures.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Shouldly;
using Xunit;

namespace Encina.TestInfrastructure.EFCore;

/// <summary>
/// Abstract base class for ScheduledMessageStoreEF integration tests.
/// Contains test methods that run against any database provider.
/// </summary>
/// <typeparam name="TFixture">The type of EF Core fixture to use.</typeparam>
/// <typeparam name="TContext">The type of DbContext to use.</typeparam>
public abstract class ScheduledMessageStoreEFTestsBase<TFixture, TContext> : EFCoreTestBase<TFixture>
    where TFixture : class, IEFCoreFixture
    where TContext : DbContext
{
    /// <summary>
    /// Gets the ScheduledMessages DbSet from the context.
    /// </summary>
    protected abstract DbSet<ScheduledMessage> GetScheduledMessages(TContext context);

    [Fact]
    public async Task AddAsync_WithRealDatabase_ShouldPersistMessage()
    {
        // Arrange
        await using var context = CreateDbContext<TContext>();
        var store = new ScheduledMessageStoreEF(context);

        var message = new ScheduledMessage
        {
            Id = Guid.NewGuid(),
            RequestType = "TestScheduledCommand",
            Content = "{\"test\":\"data\"}",
            ScheduledAtUtc = DateTime.UtcNow.AddMinutes(30),
            CreatedAtUtc = DateTime.UtcNow,
            RetryCount = 0,
            IsRecurring = false
        };

        // Act
        await store.AddAsync(message);
        await store.SaveChangesAsync();

        // Assert
        await using var verifyContext = CreateDbContext<TContext>();
        var stored = await GetScheduledMessages(verifyContext).FindAsync(message.Id);
        stored.ShouldNotBeNull();
        stored!.RequestType.ShouldBe("TestScheduledCommand");
        stored.IsRecurring.ShouldBeFalse();
    }

    [Fact]
    public async Task GetDueMessagesAsync_ShouldReturnOnlyDueMessages()
    {
        // Arrange
        await using var context = CreateDbContext<TContext>();
        var store = new ScheduledMessageStoreEF(context);

        var dueMessage = new ScheduledMessage
        {
            Id = Guid.NewGuid(),
            RequestType = "DueMessage",
            Content = "{}",
            ScheduledAtUtc = DateTime.UtcNow.AddMinutes(-5), // Due
            CreatedAtUtc = DateTime.UtcNow.AddMinutes(-10),
            RetryCount = 0,
            IsRecurring = false
        };

        var futureMessage = new ScheduledMessage
        {
            Id = Guid.NewGuid(),
            RequestType = "FutureMessage",
            Content = "{}",
            ScheduledAtUtc = DateTime.UtcNow.AddMinutes(30), // Not due
            CreatedAtUtc = DateTime.UtcNow,
            RetryCount = 0,
            IsRecurring = false
        };

        var processedMessage = new ScheduledMessage
        {
            Id = Guid.NewGuid(),
            RequestType = "ProcessedMessage",
            Content = "{}",
            ScheduledAtUtc = DateTime.UtcNow.AddMinutes(-10),
            CreatedAtUtc = DateTime.UtcNow.AddMinutes(-20),
            ProcessedAtUtc = DateTime.UtcNow.AddMinutes(-5), // Already processed
            RetryCount = 0,
            IsRecurring = false
        };

        GetScheduledMessages(context).AddRange(dueMessage, futureMessage, processedMessage);
        await context.SaveChangesAsync();

        // Act
        var dueMessages = await store.GetDueMessagesAsync(batchSize: 10, maxRetries: 3);

        // Assert
        var messageList = dueMessages.ToList();
        messageList.Count.ShouldBe(1);
        messageList.ShouldContain(m => m.Id == dueMessage.Id);
    }

    [Fact]
    public async Task MarkAsProcessedAsync_ShouldUpdateTimestamp()
    {
        // Arrange
        await using var context = CreateDbContext<TContext>();
        var store = new ScheduledMessageStoreEF(context);

        var message = new ScheduledMessage
        {
            Id = Guid.NewGuid(),
            RequestType = "Test",
            Content = "{}",
            ScheduledAtUtc = DateTime.UtcNow.AddMinutes(-5),
            CreatedAtUtc = DateTime.UtcNow.AddMinutes(-10),
            RetryCount = 0,
            IsRecurring = false
        };

        GetScheduledMessages(context).Add(message);
        await context.SaveChangesAsync();

        // Act
        await store.MarkAsProcessedAsync(message.Id);
        await store.SaveChangesAsync();

        // Assert
        await using var verifyContext = CreateDbContext<TContext>();
        var updated = await GetScheduledMessages(verifyContext).FindAsync(message.Id);
        updated!.ProcessedAtUtc.ShouldNotBeNull();
    }

    [Fact]
    public async Task MarkAsFailedAsync_ShouldUpdateErrorAndRetryInfo()
    {
        // Arrange
        await using var context = CreateDbContext<TContext>();
        var store = new ScheduledMessageStoreEF(context);

        var message = new ScheduledMessage
        {
            Id = Guid.NewGuid(),
            RequestType = "Test",
            Content = "{}",
            ScheduledAtUtc = DateTime.UtcNow.AddMinutes(-5),
            CreatedAtUtc = DateTime.UtcNow.AddMinutes(-10),
            RetryCount = 0,
            IsRecurring = false
        };

        GetScheduledMessages(context).Add(message);
        await context.SaveChangesAsync();

        var nextRetry = DateTime.UtcNow.AddMinutes(5);

        // Act
        await store.MarkAsFailedAsync(message.Id, "Test error", nextRetry);
        await store.SaveChangesAsync();

        // Assert
        await using var verifyContext = CreateDbContext<TContext>();
        var updated = await GetScheduledMessages(verifyContext).FindAsync(message.Id);
        updated!.ErrorMessage.ShouldBe("Test error");
        updated.RetryCount.ShouldBe(1);
        updated.NextRetryAtUtc.ShouldNotBeNull();
    }

    [Fact]
    public async Task CancelAsync_ShouldMarkAsProcessed()
    {
        // Arrange
        await using var context = CreateDbContext<TContext>();
        var store = new ScheduledMessageStoreEF(context);

        var message = new ScheduledMessage
        {
            Id = Guid.NewGuid(),
            RequestType = "Test",
            Content = "{}",
            ScheduledAtUtc = DateTime.UtcNow.AddMinutes(30),
            CreatedAtUtc = DateTime.UtcNow,
            RetryCount = 0,
            IsRecurring = false
        };

        GetScheduledMessages(context).Add(message);
        await context.SaveChangesAsync();

        // Act
        await store.CancelAsync(message.Id);
        await store.SaveChangesAsync();

        // Assert
        await using var verifyContext = CreateDbContext<TContext>();
        var cancelled = await GetScheduledMessages(verifyContext).FindAsync(message.Id);
        cancelled!.ProcessedAtUtc.ShouldNotBeNull();
    }

    [Fact]
    public async Task RescheduleRecurringMessageAsync_ShouldCreateNewMessage()
    {
        // Arrange
        await using var context = CreateDbContext<TContext>();
        var store = new ScheduledMessageStoreEF(context);

        var originalMessage = new ScheduledMessage
        {
            Id = Guid.NewGuid(),
            RequestType = "RecurringTest",
            Content = "{}",
            ScheduledAtUtc = DateTime.UtcNow.AddMinutes(-5),
            CreatedAtUtc = DateTime.UtcNow.AddHours(-1),
            RetryCount = 0,
            IsRecurring = true,
            CronExpression = "0 * * * *" // Every hour
        };

        GetScheduledMessages(context).Add(originalMessage);
        await context.SaveChangesAsync();

        var nextScheduledTime = DateTime.UtcNow.AddHours(1);

        // Act
        await store.RescheduleRecurringMessageAsync(originalMessage.Id, nextScheduledTime);
        await store.SaveChangesAsync();

        // Assert
        await using var verifyContext = CreateDbContext<TContext>();
        var messages = await GetScheduledMessages(verifyContext).ToListAsync();
        messages.Count.ShouldBe(2);

        var newMessage = messages.FirstOrDefault(m => m.Id != originalMessage.Id);
        newMessage.ShouldNotBeNull();
        newMessage!.RequestType.ShouldBe("RecurringTest");
        newMessage.IsRecurring.ShouldBeTrue();
        newMessage.ScheduledAtUtc.ShouldBe(nextScheduledTime, TimeSpan.FromSeconds(1));
    }
}
