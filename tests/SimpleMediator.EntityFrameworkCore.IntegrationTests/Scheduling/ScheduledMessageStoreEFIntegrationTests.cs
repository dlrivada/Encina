using FluentAssertions;
using SimpleMediator.EntityFrameworkCore.Scheduling;
using Xunit;

namespace SimpleMediator.EntityFrameworkCore.IntegrationTests.Scheduling;

/// <summary>
/// Integration tests for ScheduledMessageStoreEF using real SQL Server via Testcontainers.
/// </summary>
[Trait("Category", "Integration")]
[Trait("Database", "SqlServer")]
public sealed class ScheduledMessageStoreEFIntegrationTests : IClassFixture<EFCoreFixture>
{
    private readonly EFCoreFixture _fixture;

    public ScheduledMessageStoreEFIntegrationTests(EFCoreFixture fixture)
    {
        _fixture = fixture;
        _fixture.ClearAllDataAsync().GetAwaiter().GetResult();
    }

    [Fact]
    public async Task AddAsync_WithRealDatabase_ShouldPersistMessage()
    {
        // Arrange
        using var context = _fixture.CreateDbContext();
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
        using var verifyContext = _fixture.CreateDbContext();
        var stored = await verifyContext.ScheduledMessages.FindAsync(message.Id);
        stored.Should().NotBeNull();
        stored!.RequestType.Should().Be("TestRequest");
    }

    [Fact]
    public async Task GetDueMessagesAsync_ShouldReturnScheduledMessages()
    {
        // Arrange
        using var context = _fixture.CreateDbContext();
        var store = new ScheduledMessageStoreEF(context);

        var dueNow = new ScheduledMessage
        {
            Id = Guid.NewGuid(),
            RequestType = "DueNow",
            Content = "{}",
            ScheduledAtUtc = DateTime.UtcNow.AddMinutes(-5), // In past
            RetryCount = 0
        };

        var dueSoon = new ScheduledMessage
        {
            Id = Guid.NewGuid(),
            RequestType = "DueSoon",
            Content = "{}",
            ScheduledAtUtc = DateTime.UtcNow.AddMinutes(-1), // In past
            RetryCount = 0
        };

        var futureMessage = new ScheduledMessage
        {
            Id = Guid.NewGuid(),
            RequestType = "Future",
            Content = "{}",
            ScheduledAtUtc = DateTime.UtcNow.AddHours(1), // In future
            RetryCount = 0
        };

        context.ScheduledMessages.AddRange(dueNow, dueSoon, futureMessage);
        await context.SaveChangesAsync();

        // Act
        var messages = await store.GetDueMessagesAsync(batchSize: 10, maxRetries: 3);

        // Assert
        var messageList = messages.ToList();
        messageList.Should().HaveCount(2);
        messageList.Should().Contain(m => m.Id == dueNow.Id);
        messageList.Should().Contain(m => m.Id == dueSoon.Id);
        messageList.Should().NotContain(m => m.Id == futureMessage.Id);
    }

    [Fact]
    public async Task GetDueMessagesAsync_ShouldExcludeProcessedMessages()
    {
        // Arrange
        using var context = _fixture.CreateDbContext();
        var store = new ScheduledMessageStoreEF(context);

        var processed = new ScheduledMessage
        {
            Id = Guid.NewGuid(),
            RequestType = "Processed",
            Content = "{}",
            ScheduledAtUtc = DateTime.UtcNow.AddMinutes(-5),
            ProcessedAtUtc = DateTime.UtcNow,
            RetryCount = 0
        };

        context.ScheduledMessages.Add(processed);
        await context.SaveChangesAsync();

        // Act
        var messages = await store.GetDueMessagesAsync(batchSize: 10, maxRetries: 3);

        // Assert
        messages.Should().BeEmpty();
    }

    [Fact]
    public async Task MarkAsProcessedAsync_ShouldUpdateMessage()
    {
        // Arrange
        using var context = _fixture.CreateDbContext();
        var store = new ScheduledMessageStoreEF(context);

        var message = new ScheduledMessage
        {
            Id = Guid.NewGuid(),
            RequestType = "Test",
            Content = "{}",
            ScheduledAtUtc = DateTime.UtcNow,
            RetryCount = 0
        };

        context.ScheduledMessages.Add(message);
        await context.SaveChangesAsync();

        // Act
        await store.MarkAsProcessedAsync(message.Id);
        await store.SaveChangesAsync();

        // Assert
        using var verifyContext = _fixture.CreateDbContext();
        var updated = await verifyContext.ScheduledMessages.FindAsync(message.Id);
        updated!.ProcessedAtUtc.Should().NotBeNull();
        updated.ErrorMessage.Should().BeNull();
    }

    [Fact]
    public async Task MarkAsFailedAsync_ShouldUpdateErrorInfo()
    {
        // Arrange
        using var context = _fixture.CreateDbContext();
        var store = new ScheduledMessageStoreEF(context);

        var message = new ScheduledMessage
        {
            Id = Guid.NewGuid(),
            RequestType = "Test",
            Content = "{}",
            ScheduledAtUtc = DateTime.UtcNow,
            RetryCount = 0
        };

        context.ScheduledMessages.Add(message);
        await context.SaveChangesAsync();

        var nextRetry = DateTime.UtcNow.AddMinutes(5);

        // Act
        await store.MarkAsFailedAsync(message.Id, "Test error", nextRetry);
        await store.SaveChangesAsync();

        // Assert
        using var verifyContext = _fixture.CreateDbContext();
        var updated = await verifyContext.ScheduledMessages.FindAsync(message.Id);
        updated!.ErrorMessage.Should().Be("Test error");
        updated.RetryCount.Should().Be(1);
        updated.NextRetryAtUtc.Should().BeCloseTo(nextRetry, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public async Task GetRecurringMessagesAsync_ShouldReturnMessagesWithPattern()
    {
        // Arrange
        using var context = _fixture.CreateDbContext();
        var store = new ScheduledMessageStoreEF(context);

        var recurring = new ScheduledMessage
        {
            Id = Guid.NewGuid(),
            RequestType = "RecurringTask",
            Content = "{}",
            ScheduledAtUtc = DateTime.UtcNow,
            RecurrencePattern = "0 0 * * *", // Daily
            RetryCount = 0
        };

        var oneTime = new ScheduledMessage
        {
            Id = Guid.NewGuid(),
            RequestType = "OneTime",
            Content = "{}",
            ScheduledAtUtc = DateTime.UtcNow,
            RecurrencePattern = null,
            RetryCount = 0
        };

        context.ScheduledMessages.AddRange(recurring, oneTime);
        await context.SaveChangesAsync();

        // Act
        var messages = await store.GetRecurringMessagesAsync(batchSize: 10);

        // Assert
        var messageList = messages.ToList();
        messageList.Should().ContainSingle();
        messageList[0].Id.Should().Be(recurring.Id);
    }

    [Fact]
    public async Task ConcurrentWrites_ShouldNotCorruptData()
    {
        // Arrange
        var tasks = Enumerable.Range(0, 10).Select(async i =>
        {
            using var context = _fixture.CreateDbContext();
            var store = new ScheduledMessageStoreEF(context);

            var message = new ScheduledMessage
            {
                Id = Guid.NewGuid(),
                RequestType = $"Concurrent{i}",
                Content = $"{{\"index\":{i}}}",
                ScheduledAtUtc = DateTime.UtcNow.AddMinutes(i),
                RetryCount = 0
            };

            await store.AddAsync(message);
            await store.SaveChangesAsync();
            return message.Id;
        });

        // Act
        var messageIds = await Task.WhenAll(tasks);

        // Assert
        using var verifyContext = _fixture.CreateDbContext();
        foreach (var id in messageIds)
        {
            var stored = await verifyContext.ScheduledMessages.FindAsync(id);
            stored.Should().NotBeNull();
        }
    }
}
