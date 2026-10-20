using Encina.EntityFrameworkCore.Inbox;
using Encina.TestInfrastructure.Extensions;
using Encina.TestInfrastructure.Fixtures.EntityFrameworkCore;
using LanguageExt;
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

    [Theory]
    [InlineData(1, true)]
    [InlineData(3, true)]
    [InlineData(3, false)]
    public async Task Pipeline_ThrowingHandler_PersistsRetryCountAndRunsMaxRetriesTimes(int maxRetries, bool transactional)
    {
        await EnsureSchemaAsync();
        await global::Encina.IntegrationTests.Messaging.InboxPipelineScenario.AssertThrowingHandlerRunsMaxRetriesTimesAsync(
            Harness(), maxRetries, transactional);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Pipeline_HandlerLeft_IsCachedEvenWhenTheTransactionRollsBack(bool transactional)
    {
        await EnsureSchemaAsync();
        await global::Encina.IntegrationTests.Messaging.InboxPipelineScenario.AssertHandlerLeftIsCachedAfterRollbackAsync(
            Harness(), transactional);
    }

    [Fact]
    public async Task Pipeline_SuccessfulHandler_IsCachedAndCommitted()
    {
        await EnsureSchemaAsync();
        await global::Encina.IntegrationTests.Messaging.InboxPipelineScenario.AssertSuccessIsCachedAsync(
            Harness(), transactional: true);
    }

    [Fact]
    public async Task Pipeline_FailedBusinessCommit_LeavesMessageUnprocessed()
    {
        await EnsureSchemaAsync();
        await global::Encina.IntegrationTests.Messaging.InboxPipelineScenario.AssertFailedBusinessCommitLeavesMessageUnprocessedAsync(
            Harness());
    }

    private global::Encina.IntegrationTests.Messaging.InboxPipelineScenario.Harness Harness() =>
        global::Encina.IntegrationTests.Messaging.InboxPipelineHarnesses.EfSqlServer<TestEFDbContext>(_fixture);

    private async Task EnsureSchemaAsync()
    {
        await using var context = _fixture.CreateDbContext<TestEFDbContext>();
        await context.Database.EnsureCreatedAsync();
    }

    [Theory]
    [InlineData(1)]
    [InlineData(3)]
    public async Task Orchestrator_HandlerAlwaysThrows_RunsHandlerMaxRetriesTimesThenRejects(int maxRetries)
    {
        await using var context = _fixture.CreateDbContext<TestEFDbContext>();
        await context.Database.EnsureCreatedAsync();
        var store = new InboxStoreEF(context);

        await global::Encina.IntegrationTests.Messaging.InboxRetryScenario.AssertHandlerRunsMaxRetriesTimesAsync(
            store, new InboxMessageFactory(), maxRetries, () => context.SaveChangesAsync());
    }

    [Fact]
    public async Task Orchestrator_HandlerLeft_IsCachedAndNotRerun()
    {
        await using var context = _fixture.CreateDbContext<TestEFDbContext>();
        await context.Database.EnsureCreatedAsync();
        var store = new InboxStoreEF(context);

        await global::Encina.IntegrationTests.Messaging.InboxRetryScenario.AssertHandlerLeftIsCachedAsync(
            store, new InboxMessageFactory(), () => context.SaveChangesAsync());
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
        (await store.AddAsync(message)).ShouldBeRight();
        (await store.SaveChangesAsync()).ShouldBeRight();

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
        var resultOption = (await store.GetMessageAsync(messageId)).ShouldBeRight();

        // Assert
        resultOption.IsSome.ShouldBeTrue();
        resultOption.IfSome(msg => msg.MessageId.ShouldBe(messageId));
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
        (await store.MarkAsProcessedAsync(messageId, "{\"result\":\"success\"}")).ShouldBeRight();
        (await store.SaveChangesAsync()).ShouldBeRight();

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
        var expiredMessages = (await store.GetExpiredMessagesAsync(batchSize: 10)).ShouldBeRight();

        // Assert
        var messageList = expiredMessages.ToList();
        messageList.Count.ShouldBe(1);
        messageList.ShouldContain(m => m.MessageId == expired.MessageId);
    }
}
