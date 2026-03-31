using Encina.EntityFrameworkCore;
using Encina.EntityFrameworkCore.Inbox;
using Encina.Testing.Shouldly;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Time.Testing;
using Shouldly;
using Xunit;

namespace Encina.UnitTests.EntityFrameworkCore.Inbox;

/// <summary>
/// Tests for <see cref="InboxStoreEF"/> exercising custom TimeProvider injection
/// and full lifecycle flows that cover timestamp-dependent code paths.
/// </summary>
[Trait("Category", "Unit")]
public class InboxStoreEFTimeProviderTests : IDisposable
{
    private readonly TestDbContext _dbContext;
    private readonly FakeTimeProvider _timeProvider;
    private readonly InboxStoreEF _store;

    public InboxStoreEFTimeProviderTests()
    {
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _dbContext = new TestDbContext(options);
        _timeProvider = new FakeTimeProvider(new DateTimeOffset(2026, 3, 15, 12, 0, 0, TimeSpan.Zero));
        _store = new InboxStoreEF(_dbContext, _timeProvider);
    }

    [Fact]
    public async Task MarkAsProcessedAsync_UsesInjectedTimeProvider()
    {
        // Arrange
        var message = new InboxMessage
        {
            MessageId = "tp-process-1",
            RequestType = "TestRequest",
            ReceivedAtUtc = _timeProvider.GetUtcNow().UtcDateTime.AddMinutes(-10),
            ExpiresAtUtc = _timeProvider.GetUtcNow().UtcDateTime.AddDays(30),
            RetryCount = 0
        };

        await _dbContext.InboxMessages.AddAsync(message);
        await _dbContext.SaveChangesAsync();

        // Act
        (await _store.MarkAsProcessedAsync("tp-process-1", "{\"ok\":true}")).ShouldBeRight();
        (await _store.SaveChangesAsync()).ShouldBeRight();

        // Assert - the processed timestamp should match the FakeTimeProvider
        var updated = await _dbContext.InboxMessages.FindAsync("tp-process-1");
        updated!.ProcessedAtUtc.ShouldNotBeNull();
        updated.ProcessedAtUtc!.Value.ShouldBe(
            _timeProvider.GetUtcNow().UtcDateTime,
            TimeSpan.FromSeconds(1));
    }

    [Fact]
    public async Task MarkAsProcessedAsync_ClearsPreExistingErrorMessage()
    {
        // Arrange - message that previously had an error
        var message = new InboxMessage
        {
            MessageId = "tp-clear-error",
            RequestType = "TestRequest",
            ReceivedAtUtc = _timeProvider.GetUtcNow().UtcDateTime.AddMinutes(-20),
            ExpiresAtUtc = _timeProvider.GetUtcNow().UtcDateTime.AddDays(30),
            RetryCount = 2,
            ErrorMessage = "Previous transient error",
            NextRetryAtUtc = _timeProvider.GetUtcNow().UtcDateTime.AddMinutes(-5)
        };

        await _dbContext.InboxMessages.AddAsync(message);
        await _dbContext.SaveChangesAsync();

        // Act - successfully process it now
        (await _store.MarkAsProcessedAsync("tp-clear-error", "{\"result\":\"ok\"}")).ShouldBeRight();
        (await _store.SaveChangesAsync()).ShouldBeRight();

        // Assert - ErrorMessage should be cleared
        var updated = await _dbContext.InboxMessages.FindAsync("tp-clear-error");
        updated!.ErrorMessage.ShouldBeNull();
        updated.Response.ShouldBe("{\"result\":\"ok\"}");
        updated.ProcessedAtUtc.ShouldNotBeNull();
    }

    [Fact]
    public async Task GetExpiredMessagesAsync_UsesInjectedTimeProvider()
    {
        // Arrange
        var baseTime = _timeProvider.GetUtcNow().UtcDateTime;

        var expired = new InboxMessage
        {
            MessageId = "tp-expired",
            RequestType = "TestRequest",
            ReceivedAtUtc = baseTime.AddDays(-10),
            ExpiresAtUtc = baseTime.AddDays(-1), // expired yesterday
            RetryCount = 0
        };

        var notExpired = new InboxMessage
        {
            MessageId = "tp-not-expired",
            RequestType = "TestRequest",
            ReceivedAtUtc = baseTime,
            ExpiresAtUtc = baseTime.AddDays(10), // expires in the future
            RetryCount = 0
        };

        await _dbContext.InboxMessages.AddRangeAsync(expired, notExpired);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = (await _store.GetExpiredMessagesAsync(batchSize: 10)).ShouldBeRight();

        // Assert
        result.Count().ShouldBe(1);
        result.First().MessageId.ShouldBe("tp-expired");
    }

    [Fact]
    public async Task GetExpiredMessagesAsync_AfterTimeAdvance_FindsNewlyExpired()
    {
        // Arrange - message that expires in 1 hour from fake time
        var baseTime = _timeProvider.GetUtcNow().UtcDateTime;

        var soonExpiring = new InboxMessage
        {
            MessageId = "tp-soon-expire",
            RequestType = "TestRequest",
            ReceivedAtUtc = baseTime.AddDays(-5),
            ExpiresAtUtc = baseTime.AddHours(1), // expires in 1 hour
            RetryCount = 0
        };

        await _dbContext.InboxMessages.AddAsync(soonExpiring);
        await _dbContext.SaveChangesAsync();

        // Act 1 - not expired yet
        var beforeAdvance = (await _store.GetExpiredMessagesAsync(batchSize: 10)).ShouldBeRight();
        beforeAdvance.ShouldBeEmpty();

        // Advance time by 2 hours
        _timeProvider.Advance(TimeSpan.FromHours(2));

        // Act 2 - now expired
        var afterAdvance = (await _store.GetExpiredMessagesAsync(batchSize: 10)).ShouldBeRight();
        afterAdvance.Count().ShouldBe(1);
        afterAdvance.First().MessageId.ShouldBe("tp-soon-expire");
    }

    [Fact]
    public async Task FullLifecycle_AddProcessAndVerify()
    {
        // Arrange
        var message = new InboxMessage
        {
            MessageId = "lifecycle-1",
            RequestType = "OrderPlacedCommand",
            ReceivedAtUtc = _timeProvider.GetUtcNow().UtcDateTime,
            ExpiresAtUtc = _timeProvider.GetUtcNow().UtcDateTime.AddDays(7),
            RetryCount = 0
        };

        // Act - Add
        (await _store.AddAsync(message)).ShouldBeRight();
        (await _store.SaveChangesAsync()).ShouldBeRight();

        // Act - Verify exists
        var found = (await _store.GetMessageAsync("lifecycle-1")).ShouldBeRight();
        found.IsSome.ShouldBeTrue();

        // Act - Mark as failed first
        (await _store.MarkAsFailedAsync("lifecycle-1", "Timeout", _timeProvider.GetUtcNow().UtcDateTime.AddMinutes(5))).ShouldBeRight();
        (await _store.SaveChangesAsync()).ShouldBeRight();

        var afterFail = await _dbContext.InboxMessages.FindAsync("lifecycle-1");
        afterFail!.RetryCount.ShouldBe(1);
        afterFail.ErrorMessage.ShouldBe("Timeout");

        // Act - Increment retry
        (await _store.IncrementRetryCountAsync("lifecycle-1")).ShouldBeRight();
        (await _store.SaveChangesAsync()).ShouldBeRight();

        var afterIncrement = await _dbContext.InboxMessages.FindAsync("lifecycle-1");
        afterIncrement!.RetryCount.ShouldBe(2);

        // Act - Finally process successfully
        (await _store.MarkAsProcessedAsync("lifecycle-1", "{\"orderId\":\"123\"}")).ShouldBeRight();
        (await _store.SaveChangesAsync()).ShouldBeRight();

        var final = await _dbContext.InboxMessages.FindAsync("lifecycle-1");
        final!.IsProcessed.ShouldBeTrue();
        final.ErrorMessage.ShouldBeNull();
        final.Response.ShouldBe("{\"orderId\":\"123\"}");
    }

    [Fact]
    public async Task RemoveExpiredMessagesAsync_OnlyRemovesSpecifiedIds()
    {
        // Arrange
        var msg1 = new InboxMessage
        {
            MessageId = "remove-target-1",
            RequestType = "TestRequest",
            ReceivedAtUtc = _timeProvider.GetUtcNow().UtcDateTime.AddDays(-10),
            ExpiresAtUtc = _timeProvider.GetUtcNow().UtcDateTime.AddDays(-1),
            RetryCount = 0
        };

        var msg2 = new InboxMessage
        {
            MessageId = "remove-target-2",
            RequestType = "TestRequest",
            ReceivedAtUtc = _timeProvider.GetUtcNow().UtcDateTime.AddDays(-10),
            ExpiresAtUtc = _timeProvider.GetUtcNow().UtcDateTime.AddDays(-1),
            RetryCount = 0
        };

        var msgKeep = new InboxMessage
        {
            MessageId = "keep-this",
            RequestType = "TestRequest",
            ReceivedAtUtc = _timeProvider.GetUtcNow().UtcDateTime,
            ExpiresAtUtc = _timeProvider.GetUtcNow().UtcDateTime.AddDays(30),
            RetryCount = 0
        };

        await _dbContext.InboxMessages.AddRangeAsync(msg1, msg2, msgKeep);
        await _dbContext.SaveChangesAsync();

        // Act - remove only the two targets
        (await _store.RemoveExpiredMessagesAsync(["remove-target-1", "remove-target-2"])).ShouldBeRight();
        (await _store.SaveChangesAsync()).ShouldBeRight();

        // Assert
        var remaining = await _dbContext.InboxMessages.ToListAsync();
        remaining.Count.ShouldBe(1);
        remaining[0].MessageId.ShouldBe("keep-this");
    }

    public void Dispose()
    {
        _dbContext.Dispose();
        GC.SuppressFinalize(this);
    }
}
