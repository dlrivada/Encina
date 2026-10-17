using Encina.Security.Audit;
using Shouldly;

namespace Encina.UnitTests.Security.Audit;

/// <summary>
/// A token cancelled before the call makes every in-memory audit store method return a <c>Left</c> without touching its entries (#2135 coverage of the early-return paths).
/// </summary>
public class InMemoryAuditStoresCancellationTests
{
    private static CancellationToken Cancelled => new(canceled: true);

    [Fact]
    public async Task OperationStore_EveryMethod_PreCancelledToken_ReturnsLeft()
    {
        var store = new InMemoryOperationAuditStore();
        var token = Cancelled;

        (await store.RecordAsync(new OperationAuditEntry
        {
            Id = Guid.NewGuid(),
            CorrelationId = "corr",
            Action = "Create",
            EntityType = "Order",
            Outcome = AuditOutcome.Success,
            TimestampUtc = DateTime.UtcNow,
            StartedAtUtc = DateTimeOffset.UtcNow,
            CompletedAtUtc = DateTimeOffset.UtcNow
        }, token)).IsLeft.ShouldBeTrue();
        (await store.GetByEntityAsync("Order", null, token)).IsLeft.ShouldBeTrue();
        (await store.GetByUserAsync("user", null, null, token)).IsLeft.ShouldBeTrue();
        (await store.GetByCorrelationIdAsync("corr", token)).IsLeft.ShouldBeTrue();
        (await store.QueryAsync(new OperationAuditQuery(), token)).IsLeft.ShouldBeTrue();
        (await store.PurgeEntriesAsync(DateTime.UtcNow, token)).IsLeft.ShouldBeTrue();
    }

    [Fact]
    public async Task ReadStore_EveryMethod_PreCancelledToken_ReturnsLeft()
    {
        var store = new InMemoryReadAuditStore();
        var token = Cancelled;

        (await store.GetAccessHistoryAsync("Patient", "PAT-1", token)).IsLeft.ShouldBeTrue();
        (await store.GetUserAccessHistoryAsync("user", DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow, token)).IsLeft.ShouldBeTrue();
        (await store.QueryAsync(new ReadAuditQuery(), token)).IsLeft.ShouldBeTrue();
        (await store.PurgeEntriesAsync(DateTimeOffset.UtcNow, token)).IsLeft.ShouldBeTrue();
    }
}
