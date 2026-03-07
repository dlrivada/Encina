using Encina.EntityFrameworkCore.Inbox;
using Encina.TestInfrastructure.Extensions;
using Encina.TestInfrastructure.Fixtures.EntityFrameworkCore;
using Encina.Testing.Shouldly;
using LanguageExt;
using Microsoft.EntityFrameworkCore;
using Shouldly;
using Xunit;

namespace Encina.TestInfrastructure.EFCore;

/// <summary>
/// Abstract base class for InboxStoreEF integration tests.
/// Contains test methods that run against any database provider.
/// </summary>
/// <typeparam name="TFixture">The type of EF Core fixture to use.</typeparam>
/// <typeparam name="TContext">The type of DbContext to use.</typeparam>
public abstract class InboxStoreEFTestsBase<TFixture, TContext> : EFCoreTestBase<TFixture>
    where TFixture : class, IEFCoreFixture
    where TContext : DbContext
{
    /// <summary>
    /// Gets the InboxMessages DbSet from the context.
    /// </summary>
    protected abstract DbSet<InboxMessage> GetInboxMessages(TContext context);

    [Fact]
    public async Task AddAsync_WithRealDatabase_ShouldPersistMessage()
    {
        // Arrange
        await using var context = CreateDbContext<TContext>();
        var store = new InboxStoreEF(context);

        var message = new InboxMessage
        {
            MessageId = Guid.NewGuid().ToString(),
            RequestType = "TestRequest",
            ReceivedAtUtc = DateTime.UtcNow,
            ExpiresAtUtc = DateTime.UtcNow.AddDays(7),
            RetryCount = 0
        };

        // Act
        (await store.AddAsync(message)).ShouldBeRight();
        (await store.SaveChangesAsync()).ShouldBeRight();

        // Assert
        await using var verifyContext = CreateDbContext<TContext>();
        var stored = await GetInboxMessages(verifyContext).FindAsync(message.MessageId);
        stored.ShouldNotBeNull();
        stored!.RequestType.ShouldBe("TestRequest");
    }

    [Fact]
    public async Task GetMessageAsync_ExistingMessage_ShouldReturnMessage()
    {
        // Arrange
        await using var context = CreateDbContext<TContext>();
        var store = new InboxStoreEF(context);

        var messageId = Guid.NewGuid().ToString();
        var message = new InboxMessage
        {
            MessageId = messageId,
            RequestType = "TestRequest",
            ReceivedAtUtc = DateTime.UtcNow,
            ExpiresAtUtc = DateTime.UtcNow.AddDays(7),
            RetryCount = 0
        };

        GetInboxMessages(context).Add(message);
        await context.SaveChangesAsync();

        // Act
        var result = await store.GetMessageAsync(messageId);

        // Assert
        result.IsRight.ShouldBeTrue();
        var option = result.Match(Right: r => r, Left: _ => default);
        option.IsSome.ShouldBeTrue();
        option.IfSome(msg => msg.MessageId.ShouldBe(messageId));
    }

    [Fact]
    public async Task GetMessageAsync_NonExistingMessage_ShouldReturnNull()
    {
        // Arrange
        await using var context = CreateDbContext<TContext>();
        var store = new InboxStoreEF(context);

        // Act
        var result = await store.GetMessageAsync("non-existing-id");

        // Assert
        result.IsRight.ShouldBeTrue();
        var option = result.Match(Right: r => r, Left: _ => default);
        option.IsNone.ShouldBeTrue();
    }

    [Fact]
    public async Task MarkAsProcessedAsync_ShouldUpdateTimestamp()
    {
        // Arrange
        await using var context = CreateDbContext<TContext>();
        var store = new InboxStoreEF(context);

        var messageId = Guid.NewGuid().ToString();
        var message = new InboxMessage
        {
            MessageId = messageId,
            RequestType = "TestRequest",
            ReceivedAtUtc = DateTime.UtcNow,
            ExpiresAtUtc = DateTime.UtcNow.AddDays(7),
            RetryCount = 0
        };

        GetInboxMessages(context).Add(message);
        await context.SaveChangesAsync();

        // Act
        (await store.MarkAsProcessedAsync(messageId, "{\"result\":\"success\"}")).ShouldBeRight();
        (await store.SaveChangesAsync()).ShouldBeRight();

        // Assert
        await using var verifyContext = CreateDbContext<TContext>();
        var updated = await GetInboxMessages(verifyContext).FindAsync(messageId);
        updated!.ProcessedAtUtc.ShouldNotBeNull();
        updated.Response.ShouldBe("{\"result\":\"success\"}");
    }

    [Fact]
    public async Task MarkAsFailedAsync_ShouldUpdateErrorAndRetryInfo()
    {
        // Arrange
        await using var context = CreateDbContext<TContext>();
        var store = new InboxStoreEF(context);

        var messageId = Guid.NewGuid().ToString();
        var message = new InboxMessage
        {
            MessageId = messageId,
            RequestType = "TestRequest",
            ReceivedAtUtc = DateTime.UtcNow,
            ExpiresAtUtc = DateTime.UtcNow.AddDays(7),
            RetryCount = 0
        };

        GetInboxMessages(context).Add(message);
        await context.SaveChangesAsync();

        var nextRetry = DateTime.UtcNow.AddMinutes(5);

        // Act
        (await store.MarkAsFailedAsync(messageId, "Test error", nextRetry)).ShouldBeRight();
        (await store.SaveChangesAsync()).ShouldBeRight();

        // Assert
        await using var verifyContext = CreateDbContext<TContext>();
        var updated = await GetInboxMessages(verifyContext).FindAsync(messageId);
        updated!.ErrorMessage.ShouldBe("Test error");
        updated.RetryCount.ShouldBe(1);
        updated.NextRetryAtUtc.ShouldNotBeNull();
    }

    [Fact]
    public async Task GetExpiredMessagesAsync_ShouldReturnOnlyExpired()
    {
        // Arrange
        await using var context = CreateDbContext<TContext>();
        var store = new InboxStoreEF(context);

        var expired = new InboxMessage
        {
            MessageId = Guid.NewGuid().ToString(),
            RequestType = "Expired",
            ReceivedAtUtc = DateTime.UtcNow.AddDays(-10),
            ExpiresAtUtc = DateTime.UtcNow.AddDays(-1), // Expired yesterday
            RetryCount = 0
        };

        var notExpired = new InboxMessage
        {
            MessageId = Guid.NewGuid().ToString(),
            RequestType = "NotExpired",
            ReceivedAtUtc = DateTime.UtcNow,
            ExpiresAtUtc = DateTime.UtcNow.AddDays(7), // Not expired
            RetryCount = 0
        };

        GetInboxMessages(context).AddRange(expired, notExpired);
        await context.SaveChangesAsync();

        // Act
        var expiredResult = await store.GetExpiredMessagesAsync(batchSize: 10);

        // Assert
        expiredResult.IsRight.ShouldBeTrue();
        var messageList = expiredResult.Match(Right: r => r.ToList(), Left: _ => []);
        messageList.Count.ShouldBe(1);
        messageList.ShouldContain(m => m.MessageId == expired.MessageId);
    }

    [Fact]
    public async Task RemoveExpiredMessagesAsync_ShouldDeleteExpiredMessages()
    {
        // Arrange
        await using var context = CreateDbContext<TContext>();
        var store = new InboxStoreEF(context);

        var expired = new InboxMessage
        {
            MessageId = Guid.NewGuid().ToString(),
            RequestType = "Expired",
            ReceivedAtUtc = DateTime.UtcNow.AddDays(-10),
            ExpiresAtUtc = DateTime.UtcNow.AddDays(-1),
            RetryCount = 0
        };

        var notExpired = new InboxMessage
        {
            MessageId = Guid.NewGuid().ToString(),
            RequestType = "NotExpired",
            ReceivedAtUtc = DateTime.UtcNow,
            ExpiresAtUtc = DateTime.UtcNow.AddDays(7),
            RetryCount = 0
        };

        GetInboxMessages(context).AddRange(expired, notExpired);
        await context.SaveChangesAsync();

        // Get the expired message IDs first
        var expiredResult = await store.GetExpiredMessagesAsync(batchSize: 10);
        var expiredMessages = expiredResult.Match(Right: r => r, Left: _ => []);
        var expiredIds = expiredMessages.Select(m => m.MessageId).ToList();

        // Act
        (await store.RemoveExpiredMessagesAsync(expiredIds)).ShouldBeRight();
        (await store.SaveChangesAsync()).ShouldBeRight();

        // Assert
        await using var verifyContext = CreateDbContext<TContext>();
        var remaining = await GetInboxMessages(verifyContext).ToListAsync();
        remaining.Count.ShouldBe(1);
        remaining.ShouldContain(m => m.MessageId == notExpired.MessageId);
    }
}
