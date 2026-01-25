using Encina.EntityFrameworkCore.Inbox;
using Encina.TestInfrastructure.Fixtures.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Shouldly;
using Xunit;

namespace Encina.IntegrationTests.Infrastructure.EntityFrameworkCore.MySQL.Inbox;

/// <summary>
/// MySQL-specific integration tests for <see cref="InboxStoreEF"/>.
/// Uses real MySQL database via Testcontainers.
/// Tests are skipped until Pomelo.EntityFrameworkCore.MySql v10 is released.
/// </summary>
[Trait("Category", "Integration")]
[Trait("Database", "MySQL")]
[Collection("EFCore-MySQL")]
public sealed class InboxStoreEFMySqlTests : IAsyncLifetime
{
    private readonly EFCoreMySqlFixture _fixture;

    public InboxStoreEFMySqlTests(EFCoreMySqlFixture fixture)
    {
        _fixture = fixture;
    }

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync()
    {
        await _fixture.ClearAllDataAsync();
    }

    [SkippableFact]
    public async Task AddAsync_WithRealDatabase_ShouldPersistMessage()
    {
        Skip.If(true, "MySQL support requires Pomelo.EntityFrameworkCore.MySql v10.0.0 for EF Core 10 compatibility");

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

    [SkippableFact]
    public async Task GetMessageAsync_ExistingMessage_ShouldReturnMessage()
    {
        Skip.If(true, "MySQL support requires Pomelo.EntityFrameworkCore.MySql v10.0.0 for EF Core 10 compatibility");

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

    [SkippableFact]
    public async Task MarkAsProcessedAsync_ShouldUpdateTimestamp()
    {
        Skip.If(true, "MySQL support requires Pomelo.EntityFrameworkCore.MySql v10.0.0 for EF Core 10 compatibility");

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
}
