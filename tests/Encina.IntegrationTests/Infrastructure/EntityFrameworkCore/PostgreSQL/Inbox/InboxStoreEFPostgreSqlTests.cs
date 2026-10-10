using Encina.EntityFrameworkCore.Inbox;
using Encina.TestInfrastructure.Extensions;
using Encina.TestInfrastructure.Fixtures.EntityFrameworkCore;
using LanguageExt;
using Microsoft.EntityFrameworkCore;
using Shouldly;
using Xunit;

namespace Encina.IntegrationTests.Infrastructure.EntityFrameworkCore.PostgreSQL.Inbox;

/// <summary>
/// PostgreSQL-specific integration tests for <see cref="InboxStoreEF"/>.
/// Uses real PostgreSQL database via Testcontainers.
/// </summary>
[Trait("Category", "Integration")]
[Trait("Database", "PostgreSQL")]
[Collection("EFCore-PostgreSQL")]
public sealed class InboxStoreEFPostgreSqlTests : IAsyncLifetime
{
    private readonly EFCorePostgreSqlFixture _fixture;

    public InboxStoreEFPostgreSqlTests(EFCorePostgreSqlFixture fixture)
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
    public Task Pipeline_ThrowingHandler_PersistsRetryCountAndRunsMaxRetriesTimes(int maxRetries, bool transactional) =>
        global::Encina.IntegrationTests.Messaging.InboxPipelineScenario.AssertThrowingHandlerRunsMaxRetriesTimesAsync(
            Harness(), maxRetries, transactional);

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public Task Pipeline_HandlerLeft_IsCachedEvenWhenTheTransactionRollsBack(bool transactional) =>
        global::Encina.IntegrationTests.Messaging.InboxPipelineScenario.AssertHandlerLeftIsCachedAfterRollbackAsync(
            Harness(), transactional);

    [Fact]
    public Task Pipeline_SuccessfulHandler_IsCachedAndCommitted() =>
        global::Encina.IntegrationTests.Messaging.InboxPipelineScenario.AssertSuccessIsCachedAsync(
            Harness(), transactional: true);

    [Fact]
    public Task Pipeline_FailedBusinessCommit_LeavesMessageUnprocessed() =>
        global::Encina.IntegrationTests.Messaging.InboxPipelineScenario.AssertFailedBusinessCommitLeavesMessageUnprocessedAsync(
            Harness());

    private global::Encina.IntegrationTests.Messaging.InboxPipelineScenario.Harness Harness() =>
        global::Encina.IntegrationTests.Messaging.InboxPipelineHarnesses.EfPostgreSql<TestPostgreSqlDbContext>(_fixture);

    [Theory]
    [InlineData(1)]
    [InlineData(3)]
    public async Task Orchestrator_HandlerAlwaysThrows_RunsHandlerMaxRetriesTimesThenRejects(int maxRetries)
    {
        await using var context = _fixture.CreateDbContext<TestPostgreSqlDbContext>();
        var store = new InboxStoreEF(context);

        await global::Encina.IntegrationTests.Messaging.InboxRetryScenario.AssertHandlerRunsMaxRetriesTimesAsync(
            store, new InboxMessageFactory(), maxRetries, () => context.SaveChangesAsync());
    }

    [Fact]
    public async Task Orchestrator_HandlerLeft_IsCachedAndNotRerun()
    {
        await using var context = _fixture.CreateDbContext<TestPostgreSqlDbContext>();
        var store = new InboxStoreEF(context);

        await global::Encina.IntegrationTests.Messaging.InboxRetryScenario.AssertHandlerLeftIsCachedAsync(
            store, new InboxMessageFactory(), () => context.SaveChangesAsync());
    }

    [Fact]
    public async Task AddAsync_WithRealDatabase_ShouldPersistMessage()
    {
        // Arrange
        await using var context = _fixture.CreateDbContext<TestPostgreSqlDbContext>();
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
        await using var verifyContext = _fixture.CreateDbContext<TestPostgreSqlDbContext>();
        var stored = await verifyContext.Set<InboxMessage>().FindAsync(message.MessageId);
        stored.ShouldNotBeNull();
        stored!.RequestType.ShouldBe("TestRequest");
    }

    [Fact]
    public async Task GetMessageAsync_ExistingMessage_ShouldReturnMessage()
    {
        // Arrange
        await using var context = _fixture.CreateDbContext<TestPostgreSqlDbContext>();
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
        await using var context = _fixture.CreateDbContext<TestPostgreSqlDbContext>();
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
        await using var verifyContext = _fixture.CreateDbContext<TestPostgreSqlDbContext>();
        var updated = await verifyContext.Set<InboxMessage>().FindAsync(messageId);
        updated!.ProcessedAtUtc.ShouldNotBeNull();
        updated.Response.ShouldBe("{\"result\":\"success\"}");
    }
}
