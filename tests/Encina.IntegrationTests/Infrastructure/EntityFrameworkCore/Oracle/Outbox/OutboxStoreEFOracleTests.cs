using Encina.EntityFrameworkCore.Outbox;
using Encina.TestInfrastructure.Fixtures.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Shouldly;
using Xunit;

namespace Encina.IntegrationTests.Infrastructure.EntityFrameworkCore.Oracle.Outbox;

/// <summary>
/// Oracle-specific integration tests for <see cref="OutboxStoreEF"/>.
/// Uses real Oracle database via Testcontainers.
/// </summary>
/// <remarks>
/// Oracle tests require Docker and may take longer to initialize.
/// Use CI filtering with [Trait("Database", "Oracle")] to run separately.
/// </remarks>
[Trait("Category", "Integration")]
[Trait("Database", "Oracle")]
[Collection("EFCore-Oracle")]
public sealed class OutboxStoreEFOracleTests : IAsyncLifetime
{
    private readonly EFCoreOracleFixture _fixture;

    public OutboxStoreEFOracleTests(EFCoreOracleFixture fixture)
    {
        _fixture = fixture;
    }

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync()
    {
        await _fixture.ClearAllDataAsync();
    }

    [Fact]
    public async Task AddAsync_WithRealDatabase_ShouldPersistMessage()
    {
        // Arrange
        await using var context = _fixture.CreateDbContext<TestEFDbContext>();
        await context.Database.EnsureCreatedAsync();
        var store = new OutboxStoreEF(context);

        var message = new OutboxMessage
        {
            Id = Guid.NewGuid(),
            NotificationType = "TestNotification",
            Content = "{\"test\":\"data\"}",
            CreatedAtUtc = DateTime.UtcNow,
            RetryCount = 0
        };

        // Act
        await store.AddAsync(message);
        await store.SaveChangesAsync();

        // Assert
        await using var verifyContext = _fixture.CreateDbContext<TestEFDbContext>();
        var stored = await verifyContext.Set<OutboxMessage>().FindAsync(message.Id);
        stored.ShouldNotBeNull();
        stored!.NotificationType.ShouldBe("TestNotification");
        stored.Content.ShouldBe("{\"test\":\"data\"}");
    }

    [Fact]
    public async Task GetPendingMessagesAsync_WithMultipleMessages_ShouldReturnUnprocessedOnly()
    {
        // Arrange
        await using var context = _fixture.CreateDbContext<TestEFDbContext>();
        await context.Database.EnsureCreatedAsync();
        var store = new OutboxStoreEF(context);

        var pending1 = new OutboxMessage
        {
            Id = Guid.NewGuid(),
            NotificationType = "Pending1",
            Content = "{}",
            CreatedAtUtc = DateTime.UtcNow.AddMinutes(-10),
            RetryCount = 0
        };

        var pending2 = new OutboxMessage
        {
            Id = Guid.NewGuid(),
            NotificationType = "Pending2",
            Content = "{}",
            CreatedAtUtc = DateTime.UtcNow.AddMinutes(-5),
            RetryCount = 0
        };

        var processed = new OutboxMessage
        {
            Id = Guid.NewGuid(),
            NotificationType = "Processed",
            Content = "{}",
            CreatedAtUtc = DateTime.UtcNow.AddMinutes(-15),
            ProcessedAtUtc = DateTime.UtcNow,
            RetryCount = 0
        };

        context.Set<OutboxMessage>().AddRange(pending1, pending2, processed);
        await context.SaveChangesAsync();

        // Act
        var messages = await store.GetPendingMessagesAsync(batchSize: 10, maxRetries: 3);

        // Assert
        var messageList = messages.ToList();
        messageList.Count.ShouldBe(2);
        messageList.ShouldContain(m => m.Id == pending1.Id);
        messageList.ShouldContain(m => m.Id == pending2.Id);
        messageList.ShouldNotContain(m => m.Id == processed.Id);
    }

    [Fact]
    public async Task MarkAsProcessedAsync_ShouldUpdateTimestampAndClearError()
    {
        // Arrange
        await using var context = _fixture.CreateDbContext<TestEFDbContext>();
        await context.Database.EnsureCreatedAsync();
        var store = new OutboxStoreEF(context);

        var message = new OutboxMessage
        {
            Id = Guid.NewGuid(),
            NotificationType = "Test",
            Content = "{}",
            CreatedAtUtc = DateTime.UtcNow,
            ErrorMessage = "Previous error",
            RetryCount = 1
        };

        context.Set<OutboxMessage>().Add(message);
        await context.SaveChangesAsync();

        // Act
        await store.MarkAsProcessedAsync(message.Id);
        await store.SaveChangesAsync();

        // Assert
        await using var verifyContext = _fixture.CreateDbContext<TestEFDbContext>();
        var updated = await verifyContext.Set<OutboxMessage>().FindAsync(message.Id);
        updated!.ProcessedAtUtc.ShouldNotBeNull();
        updated.ErrorMessage.ShouldBeNull();
    }
}
