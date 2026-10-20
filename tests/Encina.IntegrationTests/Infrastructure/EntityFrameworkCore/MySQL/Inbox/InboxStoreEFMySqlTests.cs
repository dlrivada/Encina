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

    public ValueTask InitializeAsync() => ValueTask.CompletedTask;

    public async ValueTask DisposeAsync()
    {
        await _fixture.ClearAllDataAsync();
    }

    [Theory]
    [InlineData(1, true)]
    [InlineData(3, true)]
    [InlineData(3, false)]
    public async Task Pipeline_ThrowingHandler_PersistsRetryCountAndRunsMaxRetriesTimes(int maxRetries, bool transactional)
    {
        Assert.SkipWhen(true, "MySQL support requires Pomelo.EntityFrameworkCore.MySql v10.0.0 for EF Core 10 compatibility");

        // Enable together with the other MySQL tests once the Pomelo provider supports EF Core 10 (#2086):
        // pass o => o.UseMySql(...) as the database configuration.
        await global::Encina.IntegrationTests.Messaging.InboxPipelineScenario.AssertThrowingHandlerRunsMaxRetriesTimesAsync(
            Harness(), maxRetries, transactional);
    }

    [Fact]
    public async Task Pipeline_FailedBusinessCommit_LeavesMessageUnprocessed()
    {
        Assert.SkipWhen(true, "MySQL support requires Pomelo.EntityFrameworkCore.MySql v10.0.0 for EF Core 10 compatibility");

        await global::Encina.IntegrationTests.Messaging.InboxPipelineScenario.AssertFailedBusinessCommitLeavesMessageUnprocessedAsync(
            Harness());
    }

    private global::Encina.IntegrationTests.Messaging.InboxPipelineScenario.Harness Harness() =>
        global::Encina.IntegrationTests.Messaging.InboxPipelineHarnesses.Ef<TestEFDbContext>(
            _fixture.ConnectionString,
            (_, _) => { /* o.UseMySql(...) once the Pomelo provider supports EF Core 10 (#2086) */ },
            () => _fixture.CreateDbContext<TestEFDbContext>());

    [Theory]
    [InlineData(1)]
    [InlineData(3)]
    public async Task Orchestrator_HandlerAlwaysThrows_RunsHandlerMaxRetriesTimesThenRejects(int maxRetries)
    {
        Assert.SkipWhen(true, "MySQL support requires Pomelo.EntityFrameworkCore.MySql v10.0.0 for EF Core 10 compatibility");

        await using var context = _fixture.CreateDbContext<TestEFDbContext>();
        await context.Database.EnsureCreatedAsync();
        var store = new InboxStoreEF(context);

        await global::Encina.IntegrationTests.Messaging.InboxRetryScenario.AssertHandlerRunsMaxRetriesTimesAsync(
            store, new InboxMessageFactory(), maxRetries, () => context.SaveChangesAsync());
    }

    [Fact]
    public async Task Orchestrator_HandlerLeft_IsCachedAndNotRerun()
    {
        Assert.SkipWhen(true, "MySQL support requires Pomelo.EntityFrameworkCore.MySql v10.0.0 for EF Core 10 compatibility");

        await using var context = _fixture.CreateDbContext<TestEFDbContext>();
        await context.Database.EnsureCreatedAsync();
        var store = new InboxStoreEF(context);

        await global::Encina.IntegrationTests.Messaging.InboxRetryScenario.AssertHandlerLeftIsCachedAsync(
            store, new InboxMessageFactory(), () => context.SaveChangesAsync());
    }

    [Fact]
    public async Task AddAsync_WithRealDatabase_ShouldPersistMessage()
    {
        Assert.SkipWhen(true, "MySQL support requires Pomelo.EntityFrameworkCore.MySql v10.0.0 for EF Core 10 compatibility");

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
        Assert.SkipWhen(true, "MySQL support requires Pomelo.EntityFrameworkCore.MySql v10.0.0 for EF Core 10 compatibility");

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
        var option = result.ShouldBeRight();
        option.IsSome.ShouldBeTrue();
        option.IfSome(msg => msg.MessageId.ShouldBe(messageId));
    }

    [Fact]
    public async Task MarkAsProcessedAsync_ShouldUpdateTimestamp()
    {
        Assert.SkipWhen(true, "MySQL support requires Pomelo.EntityFrameworkCore.MySql v10.0.0 for EF Core 10 compatibility");

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
