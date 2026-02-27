using Encina.Compliance.DataSubjectRights;

#pragma warning disable CA1859 // Use concrete types when possible for improved performance

namespace Encina.ContractTests.Compliance.DataSubjectRights;

/// <summary>
/// Contract tests verifying that <see cref="IDSRAuditStore"/> implementations follow the
/// expected behavioral contract for DSR audit trail management.
/// </summary>
public abstract class DSRAuditStoreContractTestsBase
{
    /// <summary>
    /// Creates a new instance of the audit store being tested.
    /// </summary>
    protected abstract IDSRAuditStore CreateStore();

    #region RecordAsync Contract

    /// <summary>
    /// Contract: RecordAsync with a valid entry should succeed.
    /// </summary>
    [Fact]
    public async Task Contract_RecordAsync_ValidEntry_ShouldSucceed()
    {
        var store = CreateStore();
        var entry = CreateAuditEntry("req-001", "RequestReceived");

        var result = await store.RecordAsync(entry);

        result.IsRight.ShouldBeTrue();
    }

    /// <summary>
    /// Contract: RecordAsync should allow multiple entries for the same request.
    /// </summary>
    [Fact]
    public async Task Contract_RecordAsync_MultipleEntries_ShouldStoreAll()
    {
        var store = CreateStore();

        await store.RecordAsync(CreateAuditEntry("req-001", "RequestReceived"));
        await store.RecordAsync(CreateAuditEntry("req-001", "IdentityVerified"));
        await store.RecordAsync(CreateAuditEntry("req-001", "ErasureExecuted"));

        var result = await store.GetAuditTrailAsync("req-001");
        result.IsRight.ShouldBeTrue();
        var list = result.RightAsEnumerable().First();
        list.Count.ShouldBe(3);
    }

    #endregion

    #region GetAuditTrailAsync Contract

    /// <summary>
    /// Contract: GetAuditTrailAsync should return entries in chronological order.
    /// </summary>
    [Fact]
    public async Task Contract_GetAuditTrailAsync_ShouldReturnChronologicalOrder()
    {
        var store = CreateStore();

        var time1 = new DateTimeOffset(2026, 1, 1, 10, 0, 0, TimeSpan.Zero);
        var time2 = new DateTimeOffset(2026, 1, 1, 11, 0, 0, TimeSpan.Zero);
        var time3 = new DateTimeOffset(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);

        // Insert out of order
        await store.RecordAsync(CreateAuditEntry("req-001", "ErasureExecuted", occurredAtUtc: time3));
        await store.RecordAsync(CreateAuditEntry("req-001", "RequestReceived", occurredAtUtc: time1));
        await store.RecordAsync(CreateAuditEntry("req-001", "IdentityVerified", occurredAtUtc: time2));

        var result = await store.GetAuditTrailAsync("req-001");

        result.IsRight.ShouldBeTrue();
        var list = result.RightAsEnumerable().First();
        list[0].Action.ShouldBe("RequestReceived");
        list[1].Action.ShouldBe("IdentityVerified");
        list[2].Action.ShouldBe("ErasureExecuted");
    }

    /// <summary>
    /// Contract: GetAuditTrailAsync for non-existing request should return empty list.
    /// </summary>
    [Fact]
    public async Task Contract_GetAuditTrailAsync_NonExisting_ShouldReturnEmpty()
    {
        var store = CreateStore();

        var result = await store.GetAuditTrailAsync("non-existing");

        result.IsRight.ShouldBeTrue();
        var list = result.RightAsEnumerable().First();
        list.Count.ShouldBe(0);
    }

    /// <summary>
    /// Contract: GetAuditTrailAsync should isolate requests.
    /// </summary>
    [Fact]
    public async Task Contract_GetAuditTrailAsync_ShouldIsolateRequests()
    {
        var store = CreateStore();

        await store.RecordAsync(CreateAuditEntry("req-001", "RequestReceived"));
        await store.RecordAsync(CreateAuditEntry("req-001", "IdentityVerified"));
        await store.RecordAsync(CreateAuditEntry("req-002", "RequestReceived"));

        var result1 = await store.GetAuditTrailAsync("req-001");
        var result2 = await store.GetAuditTrailAsync("req-002");

        var list1 = result1.RightAsEnumerable().First();
        var list2 = result2.RightAsEnumerable().First();
        list1.Count.ShouldBe(2);
        list2.Count.ShouldBe(1);
    }

    /// <summary>
    /// Contract: GetAuditTrailAsync should preserve entry data.
    /// </summary>
    [Fact]
    public async Task Contract_GetAuditTrailAsync_ShouldPreserveEntryData()
    {
        var store = CreateStore();
        var entry = new DSRAuditEntry
        {
            Id = "audit-001",
            DSRRequestId = "req-001",
            Action = "ErasureExecuted",
            Detail = "Erased 12 fields across 3 entities",
            PerformedByUserId = "admin-456",
            OccurredAtUtc = new DateTimeOffset(2026, 2, 27, 12, 0, 0, TimeSpan.Zero)
        };
        await store.RecordAsync(entry);

        var result = await store.GetAuditTrailAsync("req-001");

        var list = result.RightAsEnumerable().First();
        list.Count.ShouldBe(1);
        var found = list[0];
        found.Id.ShouldBe("audit-001");
        found.DSRRequestId.ShouldBe("req-001");
        found.Action.ShouldBe("ErasureExecuted");
        found.Detail.ShouldBe("Erased 12 fields across 3 entities");
        found.PerformedByUserId.ShouldBe("admin-456");
    }

    #endregion

    #region Helpers

    private static DSRAuditEntry CreateAuditEntry(
        string requestId,
        string action,
        string? detail = null,
        DateTimeOffset? occurredAtUtc = null) =>
        new()
        {
            Id = Guid.NewGuid().ToString(),
            DSRRequestId = requestId,
            Action = action,
            Detail = detail,
            OccurredAtUtc = occurredAtUtc ?? DateTimeOffset.UtcNow
        };

    #endregion
}

/// <summary>
/// Contract tests for <see cref="InMemoryDSRAuditStore"/>.
/// </summary>
public sealed class InMemoryDSRAuditStoreContractTests : DSRAuditStoreContractTestsBase
{
    protected override IDSRAuditStore CreateStore() =>
        new InMemoryDSRAuditStore(Microsoft.Extensions.Logging.Abstractions.NullLogger<InMemoryDSRAuditStore>.Instance);
}
