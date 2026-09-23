using Encina.EntityFrameworkCore.Outbox;
using Encina.Testing.Shouldly;
using Microsoft.EntityFrameworkCore;

namespace Encina.UnitTests.EntityFrameworkCore.Outbox;

/// <summary>
/// Unit tests for the exhausted-message members of <see cref="OutboxStoreEF"/> (#1150), on the EF Core
/// in-memory provider.
/// </summary>
public sealed class OutboxStoreEFExhaustionTests : IDisposable
{
    private const int MaxRetries = 3;

    private readonly TestDbContext _dbContext;
    private readonly OutboxStoreEF _store;

    public OutboxStoreEFExhaustionTests()
    {
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _dbContext = new TestDbContext(options);
        _store = new OutboxStoreEF(_dbContext);
    }

    [Fact]
    public async Task GetPendingCountAsync_CountsUnprocessedMessagesWithRetriesLeft()
    {
        await SeedAsync();

        var count = (await _store.GetPendingCountAsync(MaxRetries)).ShouldBeRight();

        // fresh + scheduled-for-later-retry; processed and exhausted are excluded
        count.ShouldBe(2);
    }

    [Fact]
    public async Task GetExhaustedCountAsync_CountsUnprocessedMessagesWithoutRetriesLeft()
    {
        await SeedAsync();

        var count = (await _store.GetExhaustedCountAsync(MaxRetries)).ShouldBeRight();

        count.ShouldBe(2);
    }

    [Fact]
    public async Task RequeueExhaustedAsync_All_ResetsEveryExhaustedMessage()
    {
        var seeded = await SeedAsync();

        var requeued = (await _store.RequeueExhaustedAsync(MaxRetries, null)).ShouldBeRight();
        (await _store.SaveChangesAsync()).ShouldBeRight();

        requeued.ShouldBe(2);
        (await _store.GetExhaustedCountAsync(MaxRetries)).ShouldBeRight().ShouldBe(0);
        (await _store.GetPendingCountAsync(MaxRetries)).ShouldBeRight().ShouldBe(4);

        var message = await _dbContext.OutboxMessages.SingleAsync(m => m.Id == seeded.Exhausted1);
        message.RetryCount.ShouldBe(0);
        message.NextRetryAtUtc.ShouldBeNull();
        message.ErrorMessage.ShouldBeNull();
        message.ProcessedAtUtc.ShouldBeNull();
    }

    [Fact]
    public async Task RequeueExhaustedAsync_ByIds_ResetsOnlyRequestedExhaustedMessages()
    {
        var seeded = await SeedAsync();

        var requeued = (await _store.RequeueExhaustedAsync(
            MaxRetries,
            [seeded.Exhausted1, seeded.Fresh, seeded.Processed, Guid.NewGuid()])).ShouldBeRight();
        (await _store.SaveChangesAsync()).ShouldBeRight();

        requeued.ShouldBe(1);
        (await _dbContext.OutboxMessages.SingleAsync(m => m.Id == seeded.Exhausted1)).RetryCount.ShouldBe(0);
        (await _dbContext.OutboxMessages.SingleAsync(m => m.Id == seeded.Exhausted2)).RetryCount.ShouldBe(MaxRetries + 2);
        (await _dbContext.OutboxMessages.SingleAsync(m => m.Id == seeded.Processed)).RetryCount.ShouldBe(MaxRetries);
    }

    [Fact]
    public async Task RequeueExhaustedAsync_NothingExhausted_ReturnsZero()
    {
        (await _store.RequeueExhaustedAsync(MaxRetries, null)).ShouldBeRight().ShouldBe(0);
    }

    [Fact]
    public async Task RequeueExhaustedAsync_RequiresSaveChangesToPersist()
    {
        var seeded = await SeedAsync();

        (await _store.RequeueExhaustedAsync(MaxRetries, [seeded.Exhausted1])).ShouldBeRight().ShouldBe(1);

        _dbContext.ChangeTracker.HasChanges().ShouldBeTrue();
    }

    [Fact]
    public async Task CountAndRequeue_DisposedContext_ReturnLeft()
    {
        await _dbContext.DisposeAsync();

        (await _store.GetPendingCountAsync(MaxRetries)).IsLeft.ShouldBeTrue();
        (await _store.GetExhaustedCountAsync(MaxRetries)).IsLeft.ShouldBeTrue();
        (await _store.RequeueExhaustedAsync(MaxRetries, null)).IsLeft.ShouldBeTrue();
    }

    public void Dispose() => _dbContext.Dispose();

    private async Task<SeededIds> SeedAsync()
    {
        var now = DateTime.UtcNow;
        var ids = new SeededIds(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());

        _dbContext.OutboxMessages.AddRange(
            NewMessage(ids.Fresh, now, retryCount: 0),
            NewMessage(ids.Scheduled, now, retryCount: 1, nextRetryAtUtc: now.AddHours(1)),
            NewMessage(ids.Exhausted1, now, retryCount: MaxRetries),
            NewMessage(ids.Exhausted2, now, retryCount: MaxRetries + 2),
            NewMessage(ids.Processed, now, retryCount: MaxRetries, processedAtUtc: now));
        await _dbContext.SaveChangesAsync();
        _dbContext.ChangeTracker.Clear();

        return ids;
    }

    private static OutboxMessage NewMessage(
        Guid id,
        DateTime createdAtUtc,
        int retryCount,
        DateTime? nextRetryAtUtc = null,
        DateTime? processedAtUtc = null) => new()
        {
            Id = id,
            NotificationType = "TestNotification",
            Content = "{}",
            CreatedAtUtc = createdAtUtc,
            RetryCount = retryCount,
            NextRetryAtUtc = nextRetryAtUtc,
            ProcessedAtUtc = processedAtUtc,
            ErrorMessage = retryCount > 0 && processedAtUtc is null ? "Rejected by handler" : null
        };

    private sealed record SeededIds(Guid Fresh, Guid Scheduled, Guid Exhausted1, Guid Exhausted2, Guid Processed);
}
