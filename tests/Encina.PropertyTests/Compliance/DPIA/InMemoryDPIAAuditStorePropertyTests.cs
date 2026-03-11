using Encina.Compliance.DPIA;
using Encina.Compliance.DPIA.Model;

using FsCheck;
using FsCheck.Xunit;

using LanguageExt;

using Microsoft.Extensions.Logging.Abstractions;

namespace Encina.PropertyTests.Compliance.DPIA;

/// <summary>
/// Property-based tests for <see cref="InMemoryDPIAAuditStore"/> verifying store
/// invariants using FsCheck random data generation.
/// </summary>
public class InMemoryDPIAAuditStorePropertyTests
{
    private static InMemoryDPIAAuditStore CreateStore() =>
        new(NullLogger<InMemoryDPIAAuditStore>.Instance);

    #region Store Roundtrip Invariants

    /// <summary>
    /// Invariant: Any recorded audit entry is always retrievable in the assessment's trail.
    /// </summary>
    [Property(MaxTest = 50)]
    public bool Record_ThenGetTrail_AlwaysContainsEntry(NonEmptyString action)
    {
        var store = CreateStore();
        var assessmentId = Guid.NewGuid();
        var entry = CreateEntry(assessmentId, action.Get);

        var recordResult = store.RecordAuditEntryAsync(entry).AsTask().Result;
        if (!recordResult.IsRight) return false;

        var trailResult = store.GetAuditTrailAsync(assessmentId).AsTask().Result;
        var trail = trailResult.Match(l => l, _ => (IReadOnlyList<DPIAAuditEntry>)[]);

        return trail.Any(e => e.Id == entry.Id && e.Action == action.Get);
    }

    /// <summary>
    /// Invariant: Audit trails for different assessments are always isolated.
    /// </summary>
    [Property(MaxTest = 50)]
    public bool GetTrail_AlwaysIsolatedByAssessmentId(NonEmptyString action1, NonEmptyString action2)
    {
        var store = CreateStore();
        var id1 = Guid.NewGuid();
        var id2 = Guid.NewGuid();

        store.RecordAuditEntryAsync(CreateEntry(id1, action1.Get)).AsTask().Wait();
        store.RecordAuditEntryAsync(CreateEntry(id2, action2.Get)).AsTask().Wait();

        var trail1 = store.GetAuditTrailAsync(id1).AsTask().Result
            .Match(l => l, _ => (IReadOnlyList<DPIAAuditEntry>)[]);
        var trail2 = store.GetAuditTrailAsync(id2).AsTask().Result
            .Match(l => l, _ => (IReadOnlyList<DPIAAuditEntry>)[]);

        return trail1.Count == 1 && trail2.Count == 1
            && trail1.All(e => e.AssessmentId == id1)
            && trail2.All(e => e.AssessmentId == id2);
    }

    /// <summary>
    /// Invariant: Trail for a non-existent assessment is always empty.
    /// </summary>
    [Property(MaxTest = 50)]
    public bool GetTrail_NonExistent_AlwaysEmpty()
    {
        var store = CreateStore();

        var result = store.GetAuditTrailAsync(Guid.NewGuid()).AsTask().Result;
        var trail = result.Match(l => l, _ => (IReadOnlyList<DPIAAuditEntry>)[]);

        return trail.Count == 0;
    }

    #endregion

    #region Helpers

    private static DPIAAuditEntry CreateEntry(Guid assessmentId, string action) => new()
    {
        Id = Guid.NewGuid(),
        AssessmentId = assessmentId,
        Action = action,
        OccurredAtUtc = DateTimeOffset.UtcNow,
        PerformedBy = "test-user",
        Details = $"Test: {action}"
    };

    #endregion
}
