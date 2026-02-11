using Encina.EntityFrameworkCore.Scheduling;
using Encina.TestInfrastructure.Fixtures.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Shouldly;
using Xunit;

namespace Encina.IntegrationTests.Infrastructure.EntityFrameworkCore.MySQL.Scheduling;

/// <summary>
/// MySQL-specific integration tests for <see cref="ScheduledMessageStoreEF"/>.
/// Uses real MySQL database via Testcontainers.
/// Tests are skipped until Pomelo.EntityFrameworkCore.MySql v10 is released.
/// </summary>
[Trait("Category", "Integration")]
[Trait("Database", "MySQL")]
[Collection("EFCore-MySQL")]
public sealed class ScheduledMessageStoreEFMySqlTests : IAsyncLifetime
{
    private readonly EFCoreMySqlFixture _fixture;

    public ScheduledMessageStoreEFMySqlTests(EFCoreMySqlFixture fixture)
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
        Assert.SkipWhen(true, "MySQL support requires Pomelo.EntityFrameworkCore.MySql v10.0.0 for EF Core 10 compatibility");

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
        Assert.SkipWhen(true, "MySQL support requires Pomelo.EntityFrameworkCore.MySql v10.0.0 for EF Core 10 compatibility");

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
        Assert.SkipWhen(true, "MySQL support requires Pomelo.EntityFrameworkCore.MySql v10.0.0 for EF Core 10 compatibility");

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
        Assert.SkipWhen(true, "MySQL support requires Pomelo.EntityFrameworkCore.MySql v10.0.0 for EF Core 10 compatibility");

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
