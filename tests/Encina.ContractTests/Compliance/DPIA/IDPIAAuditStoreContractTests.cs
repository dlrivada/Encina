#pragma warning disable CA1859 // Contract tests intentionally use interface types

using Encina.Compliance.DPIA;
using Encina.Compliance.DPIA.Model;

using LanguageExt;

using Microsoft.Extensions.Logging.Abstractions;

namespace Encina.ContractTests.Compliance.DPIA;

#region Abstract Base Class

/// <summary>
/// Abstract contract tests for <see cref="IDPIAAuditStore"/> verifying all implementations
/// behave consistently regardless of backing store technology.
/// </summary>
[Trait("Category", "Contract")]
public abstract class DPIAAuditStoreContractTestsBase
{
    protected abstract IDPIAAuditStore CreateStore();

    #region RecordAuditEntryAsync Contract

    [Fact]
    public async Task Contract_RecordAuditEntry_ThenGetAuditTrail_ContainsEntry()
    {
        var store = CreateStore();
        var assessmentId = Guid.NewGuid();
        var entry = CreateAuditEntry(assessmentId, "Created");

        var recordResult = await store.RecordAuditEntryAsync(entry);
        recordResult.IsRight.ShouldBeTrue("Record should succeed");

        var trailResult = await store.GetAuditTrailAsync(assessmentId);
        trailResult.IsRight.ShouldBeTrue("GetAuditTrail should succeed");

        var trail = trailResult.Match(l => l, _ => (IReadOnlyList<DPIAAuditEntry>)[]);
        trail.Count.ShouldBe(1);
        trail[0].Id.ShouldBe(entry.Id);
        trail[0].AssessmentId.ShouldBe(assessmentId);
        trail[0].Action.ShouldBe("Created");
    }

    [Fact]
    public async Task Contract_RecordMultipleEntries_AllRetrieved()
    {
        var store = CreateStore();
        var assessmentId = Guid.NewGuid();

        await store.RecordAuditEntryAsync(CreateAuditEntry(assessmentId, "Created"));
        await store.RecordAuditEntryAsync(CreateAuditEntry(assessmentId, "Assessed"));
        await store.RecordAuditEntryAsync(CreateAuditEntry(assessmentId, "Approved"));

        var trailResult = await store.GetAuditTrailAsync(assessmentId);
        var trail = trailResult.Match(l => l, _ => (IReadOnlyList<DPIAAuditEntry>)[]);
        trail.Count.ShouldBe(3);
    }

    #endregion

    #region GetAuditTrailAsync Contract

    [Fact]
    public async Task Contract_GetAuditTrail_NonExistentAssessment_ReturnsEmptyList()
    {
        var store = CreateStore();

        var result = await store.GetAuditTrailAsync(Guid.NewGuid());
        result.IsRight.ShouldBeTrue("Getting trail for non-existent should succeed");

        var trail = result.Match(l => l, _ => (IReadOnlyList<DPIAAuditEntry>)[]);
        trail.Count.ShouldBe(0);
    }

    [Fact]
    public async Task Contract_GetAuditTrail_IsolatedByAssessmentId()
    {
        var store = CreateStore();
        var assessment1 = Guid.NewGuid();
        var assessment2 = Guid.NewGuid();

        await store.RecordAuditEntryAsync(CreateAuditEntry(assessment1, "Created"));
        await store.RecordAuditEntryAsync(CreateAuditEntry(assessment2, "Created"));
        await store.RecordAuditEntryAsync(CreateAuditEntry(assessment1, "Approved"));

        var trail1 = (await store.GetAuditTrailAsync(assessment1))
            .Match(l => l, _ => (IReadOnlyList<DPIAAuditEntry>)[]);
        var trail2 = (await store.GetAuditTrailAsync(assessment2))
            .Match(l => l, _ => (IReadOnlyList<DPIAAuditEntry>)[]);

        trail1.Count.ShouldBe(2);
        trail2.Count.ShouldBe(1);
    }

    #endregion

    #region Helpers

    protected static DPIAAuditEntry CreateAuditEntry(Guid assessmentId, string action) => new()
    {
        Id = Guid.NewGuid(),
        AssessmentId = assessmentId,
        Action = action,
        OccurredAtUtc = DateTimeOffset.UtcNow,
        PerformedBy = "test-user",
        Details = $"Test audit entry: {action}"
    };

    #endregion
}

#endregion

#region InMemory Concrete Implementation

/// <summary>
/// Contract verification for the in-memory <see cref="IDPIAAuditStore"/> implementation.
/// </summary>
[Trait("Category", "Contract")]
public sealed class InMemoryDPIAAuditStoreContractTests : DPIAAuditStoreContractTestsBase
{
    protected override IDPIAAuditStore CreateStore() =>
        new InMemoryDPIAAuditStore(NullLogger<InMemoryDPIAAuditStore>.Instance);
}

#endregion
