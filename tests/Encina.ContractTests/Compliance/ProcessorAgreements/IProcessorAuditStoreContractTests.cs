#pragma warning disable CA1859 // Contract tests intentionally use interface types

using Encina.Compliance.ProcessorAgreements;
using Encina.Compliance.ProcessorAgreements.Model;

using LanguageExt;

namespace Encina.ContractTests.Compliance.ProcessorAgreements;

#region Abstract Base Class

/// <summary>
/// Abstract contract tests for <see cref="IProcessorAuditStore"/> verifying all implementations
/// behave consistently regardless of backing store technology.
/// </summary>
[Trait("Category", "Contract")]
public abstract class ProcessorAuditStoreContractTestsBase
{
    protected abstract IProcessorAuditStore CreateStore();

    #region RecordAsync Contract

    [Fact]
    public async Task Contract_RecordEntry_ThenGetAuditTrail_ContainsEntry()
    {
        var store = CreateStore();
        var entry = CreateAuditEntry("proc-audit1", "Registered");

        var recordResult = await store.RecordAsync(entry);
        recordResult.IsRight.ShouldBeTrue("Record should succeed");

        var trailResult = await store.GetAuditTrailAsync("proc-audit1");
        trailResult.IsRight.ShouldBeTrue("GetAuditTrail should succeed");

        var trail = trailResult.Match(l => l, _ => (IReadOnlyList<ProcessorAgreementAuditEntry>)[]);
        trail.Count.ShouldBe(1);
        trail[0].Id.ShouldBe(entry.Id);
        trail[0].ProcessorId.ShouldBe("proc-audit1");
        trail[0].Action.ShouldBe("Registered");
    }

    [Fact]
    public async Task Contract_RecordMultipleEntries_AllRetrieved()
    {
        var store = CreateStore();

        await store.RecordAsync(CreateAuditEntry("proc-multi", "Registered"));
        await store.RecordAsync(CreateAuditEntry("proc-multi", "DPASigned"));
        await store.RecordAsync(CreateAuditEntry("proc-multi", "SubProcessorAdded"));

        var trailResult = await store.GetAuditTrailAsync("proc-multi");
        var trail = trailResult.Match(l => l, _ => (IReadOnlyList<ProcessorAgreementAuditEntry>)[]);
        trail.Count.ShouldBe(3);
    }

    #endregion

    #region GetAuditTrailAsync Contract

    [Fact]
    public async Task Contract_GetAuditTrail_NonExistentProcessor_ReturnsEmptyList()
    {
        var store = CreateStore();

        var result = await store.GetAuditTrailAsync("non-existent-processor");
        result.IsRight.ShouldBeTrue("Getting trail for non-existent should succeed");

        var trail = result.Match(l => l, _ => (IReadOnlyList<ProcessorAgreementAuditEntry>)[]);
        trail.Count.ShouldBe(0);
    }

    [Fact]
    public async Task Contract_GetAuditTrail_IsolatedByProcessorId()
    {
        var store = CreateStore();

        await store.RecordAsync(CreateAuditEntry("proc-iso1", "Registered"));
        await store.RecordAsync(CreateAuditEntry("proc-iso2", "Registered"));
        await store.RecordAsync(CreateAuditEntry("proc-iso1", "DPASigned"));

        var trail1 = (await store.GetAuditTrailAsync("proc-iso1"))
            .Match(l => l, _ => (IReadOnlyList<ProcessorAgreementAuditEntry>)[]);
        var trail2 = (await store.GetAuditTrailAsync("proc-iso2"))
            .Match(l => l, _ => (IReadOnlyList<ProcessorAgreementAuditEntry>)[]);

        trail1.Count.ShouldBe(2);
        trail2.Count.ShouldBe(1);
    }

    [Fact]
    public async Task Contract_GetAuditTrail_OrderedByOccurredAtUtc()
    {
        var store = CreateStore();
        var baseTime = DateTimeOffset.UtcNow;

        var entry1 = CreateAuditEntry("proc-order", "Registered") with
        {
            OccurredAtUtc = baseTime.AddMinutes(-10)
        };
        var entry2 = CreateAuditEntry("proc-order", "DPASigned") with
        {
            OccurredAtUtc = baseTime.AddMinutes(-5)
        };
        var entry3 = CreateAuditEntry("proc-order", "SubProcessorAdded") with
        {
            OccurredAtUtc = baseTime
        };

        // Insert in non-chronological order to verify sorting.
        await store.RecordAsync(entry3);
        await store.RecordAsync(entry1);
        await store.RecordAsync(entry2);

        var trailResult = await store.GetAuditTrailAsync("proc-order");
        var trail = trailResult.Match(l => l, _ => (IReadOnlyList<ProcessorAgreementAuditEntry>)[]);

        trail.Count.ShouldBe(3);
        trail[0].Action.ShouldBe("Registered");
        trail[1].Action.ShouldBe("DPASigned");
        trail[2].Action.ShouldBe("SubProcessorAdded");
    }

    #endregion

    #region Helpers

    protected static ProcessorAgreementAuditEntry CreateAuditEntry(
        string processorId, string action) => new()
        {
            Id = Guid.NewGuid().ToString(),
            ProcessorId = processorId,
            Action = action,
            Detail = $"Test audit entry: {action}",
            PerformedByUserId = "test-user",
            OccurredAtUtc = DateTimeOffset.UtcNow
        };

    #endregion
}

#endregion
