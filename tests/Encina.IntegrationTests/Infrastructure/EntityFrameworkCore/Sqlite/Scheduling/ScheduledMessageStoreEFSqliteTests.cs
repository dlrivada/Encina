using Encina.EntityFrameworkCore.Scheduling;
using Encina.TestInfrastructure.Fixtures.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Shouldly;
using Xunit;

namespace Encina.IntegrationTests.Infrastructure.EntityFrameworkCore.Sqlite.Scheduling;

/// <summary>
/// SQLite-specific integration tests for <see cref="ScheduledMessageStoreEF"/>.
/// Uses in-memory SQLite database.
/// </summary>
[Trait("Category", "Integration")]
[Trait("Database", "Sqlite")]
[Collection("EFCore-Sqlite")]
public sealed class ScheduledMessageStoreEFSqliteTests : IAsyncLifetime
{
    private readonly EFCoreSqliteFixture _fixture;

    public ScheduledMessageStoreEFSqliteTests(EFCoreSqliteFixture fixture)
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
        var store = new ScheduledMessageStoreEF(context);

        var message = new ScheduledMessage
        {
            Id = Guid.NewGuid(),
            RequestType = "TestRequest",
            Content = "{\"test\":\"data\"}",
            ScheduledAtUtc = DateTime.UtcNow.AddHours(1),
            RetryCount = 0
        };

        // Act
        await store.AddAsync(message);
        await store.SaveChangesAsync();

        // Assert
        await using var verifyContext = _fixture.CreateDbContext<TestEFDbContext>();
        var stored = await verifyContext.Set<ScheduledMessage>().FindAsync(message.Id);
        stored.ShouldNotBeNull();
        stored!.RequestType.ShouldBe("TestRequest");
    }

    [Fact]
    public async Task GetDueMessagesAsync_ShouldReturnScheduledMessages()
    {
        // Arrange
        await using var context = _fixture.CreateDbContext<TestEFDbContext>();
        await context.Database.EnsureCreatedAsync();
        var store = new ScheduledMessageStoreEF(context);

        var dueNow = new ScheduledMessage
        {
            Id = Guid.NewGuid(),
            RequestType = "DueNow",
            Content = "{}",
            ScheduledAtUtc = DateTime.UtcNow.AddMinutes(-5),
            RetryCount = 0
        };

        var futureMessage = new ScheduledMessage
        {
            Id = Guid.NewGuid(),
            RequestType = "Future",
            Content = "{}",
            ScheduledAtUtc = DateTime.UtcNow.AddHours(1),
            RetryCount = 0
        };

        context.Set<ScheduledMessage>().AddRange(dueNow, futureMessage);
        await context.SaveChangesAsync();

        // Act
        var messages = await store.GetDueMessagesAsync(batchSize: 10, maxRetries: 3);

        // Assert
        var messageList = messages.ToList();
        messageList.ShouldContain(m => m.Id == dueNow.Id);
        messageList.ShouldNotContain(m => m.Id == futureMessage.Id);
    }

    [Fact]
    public async Task MarkAsProcessedAsync_ShouldUpdateMessage()
    {
        // Arrange
        await using var context = _fixture.CreateDbContext<TestEFDbContext>();
        await context.Database.EnsureCreatedAsync();
        var store = new ScheduledMessageStoreEF(context);

        var message = new ScheduledMessage
        {
            Id = Guid.NewGuid(),
            RequestType = "Test",
            Content = "{}",
            ScheduledAtUtc = DateTime.UtcNow,
            RetryCount = 0
        };

        context.Set<ScheduledMessage>().Add(message);
        await context.SaveChangesAsync();

        // Act
        await store.MarkAsProcessedAsync(message.Id);
        await store.SaveChangesAsync();

        // Assert
        await using var verifyContext = _fixture.CreateDbContext<TestEFDbContext>();
        var updated = await verifyContext.Set<ScheduledMessage>().FindAsync(message.Id);
        updated!.ProcessedAtUtc.ShouldNotBeNull();
    }

    [Fact]
    public async Task MarkAsFailedAsync_ShouldUpdateErrorInfo()
    {
        // Arrange
        await using var context = _fixture.CreateDbContext<TestEFDbContext>();
        await context.Database.EnsureCreatedAsync();
        var store = new ScheduledMessageStoreEF(context);

        var message = new ScheduledMessage
        {
            Id = Guid.NewGuid(),
            RequestType = "Test",
            Content = "{}",
            ScheduledAtUtc = DateTime.UtcNow,
            RetryCount = 0
        };

        context.Set<ScheduledMessage>().Add(message);
        await context.SaveChangesAsync();

        var nextRetry = DateTime.UtcNow.AddMinutes(5);

        // Act
        await store.MarkAsFailedAsync(message.Id, "Test error", nextRetry);
        await store.SaveChangesAsync();

        // Assert
        await using var verifyContext = _fixture.CreateDbContext<TestEFDbContext>();
        var updated = await verifyContext.Set<ScheduledMessage>().FindAsync(message.Id);
        updated!.ErrorMessage.ShouldBe("Test error");
        updated.RetryCount.ShouldBe(1);
    }
}
