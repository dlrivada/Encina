using Encina.EntityFrameworkCore.Inbox;
using Encina.TestInfrastructure.Fixtures.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Shouldly;
using Xunit;

namespace Encina.IntegrationTests.Infrastructure.EntityFrameworkCore.SqlServer.Inbox;

/// <summary>
/// SQL Server-specific integration tests for <see cref="InboxStoreEF"/>.
/// Uses real SQL Server database via Testcontainers.
/// </summary>
[Trait("Category", "Integration")]
[Trait("Database", "SqlServer")]
[Collection("EFCore-SqlServer")]
public sealed class InboxStoreEFSqlServerTests : IAsyncLifetime
{
    private readonly EFCoreSqlServerFixture _fixture;

    public InboxStoreEFSqlServerTests(EFCoreSqlServerFixture fixture)
    {
        _fixture = fixture;
    }

    public ValueTask InitializeAsync() => ValueTask.CompletedTask;

    public async ValueTask DisposeAsync()
    {
        await _fixture.ClearAllDataAsync();
    }

    [Fact]
    public async Task AddAsync_WithRealDatabase_ShouldPersistMessage()
    {
        // Arrange
        await using var context = _fixture.CreateDbContext<TestEFDbContext>();
        await context.Database.EnsureCreatedAsync();
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
        await store.AddAsync(message);
        await store.SaveChangesAsync();

        // Assert
        await using var verifyContext = _fixture.CreateDbContext<TestEFDbContext>();
        var stored = await verifyContext.Set<InboxMessage>().FindAsync(message.MessageId);
        stored.ShouldNotBeNull();
        stored!.RequestType.ShouldBe("TestRequest");
    }

    [Fact]
    public async Task GetMessageAsync_ExistingMessage_ShouldReturnMessage()
    {
        // Arrange
        await using var context = _fixture.CreateDbContext<TestEFDbContext>();
        await context.Database.EnsureCreatedAsync();
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

        context.Set<InboxMessage>().Add(message);
        await context.SaveChangesAsync();

        // Act
        var result = await store.GetMessageAsync(messageId);

        // Assert
        result.ShouldNotBeNull();
        result!.MessageId.ShouldBe(messageId);
    }

    [Fact]
    public async Task MarkAsProcessedAsync_ShouldUpdateTimestamp()
    {
        // Arrange
        await using var context = _fixture.CreateDbContext<TestEFDbContext>();
        await context.Database.EnsureCreatedAsync();
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

        context.Set<InboxMessage>().Add(message);
        await context.SaveChangesAsync();

        // Act
        await store.MarkAsProcessedAsync(messageId, "{\"result\":\"success\"}");
        await store.SaveChangesAsync();

        // Assert
        await using var verifyContext = _fixture.CreateDbContext<TestEFDbContext>();
        var updated = await verifyContext.Set<InboxMessage>().FindAsync(messageId);
        updated!.ProcessedAtUtc.ShouldNotBeNull();
        updated.Response.ShouldBe("{\"result\":\"success\"}");
    }

    [Fact]
    public async Task GetExpiredMessagesAsync_ShouldReturnOnlyExpired()
    {
        // Arrange
        await using var context = _fixture.CreateDbContext<TestEFDbContext>();
        await context.Database.EnsureCreatedAsync();
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

        context.Set<InboxMessage>().AddRange(expired, notExpired);
        await context.SaveChangesAsync();

        // Act
        var expiredMessages = await store.GetExpiredMessagesAsync(batchSize: 10);

        // Assert
        var messageList = expiredMessages.ToList();
        messageList.Count.ShouldBe(1);
        messageList.ShouldContain(m => m.MessageId == expired.MessageId);
    }
}
