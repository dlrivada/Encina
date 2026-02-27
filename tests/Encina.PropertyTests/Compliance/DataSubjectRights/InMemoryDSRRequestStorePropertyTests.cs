using Encina.Compliance.DataSubjectRights;

using LanguageExt;

namespace Encina.PropertyTests.Compliance.DataSubjectRights;

/// <summary>
/// Property-based tests for <see cref="InMemoryDSRRequestStore"/> verifying store
/// invariants using FsCheck random data generation.
/// </summary>
public class InMemoryDSRRequestStorePropertyTests
{
    #region Store Roundtrip Invariants

    /// <summary>
    /// Invariant: Any created request can be retrieved by its ID.
    /// </summary>
    [Property(MaxTest = 50)]
    public bool Property_CreateThenGet_AlwaysReturnsStoredRequest(NonEmptyString subjectId)
    {
        // Skip whitespace-only strings — the store rejects them by design (ArgumentException)
        if (string.IsNullOrWhiteSpace(subjectId.Get)) return true;

        var store = CreateStore();
        var id = Guid.NewGuid().ToString();
        var request = DSRRequest.Create(id, subjectId.Get, DataSubjectRight.Access, DateTimeOffset.UtcNow);

        var createResult = store.CreateAsync(request).AsTask().Result;
        if (!createResult.IsRight) return false;

        var getResult = store.GetByIdAsync(id).AsTask().Result;
        if (!getResult.IsRight) return false;

        var option = (Option<DSRRequest>)getResult;
        if (option.IsNone) return false;

        var found = (DSRRequest)option;
        return found.Id == id && found.SubjectId == subjectId.Get;
    }

    /// <summary>
    /// Invariant: GetBySubjectIdAsync always returns all requests for that subject.
    /// </summary>
    [Property(MaxTest = 30)]
    public bool Property_GetBySubjectId_ReturnsAllForSubject(PositiveInt count)
    {
        var actualCount = Math.Min(count.Get, 10); // Cap for performance
        var store = CreateStore();
        var subjectId = "subject-test";

        for (var i = 0; i < actualCount; i++)
        {
            var request = DSRRequest.Create(
                $"req-{i}",
                subjectId,
                DataSubjectRight.Access,
                DateTimeOffset.UtcNow);
            store.CreateAsync(request).AsTask().Wait();
        }

        var result = store.GetBySubjectIdAsync(subjectId).AsTask().Result;
        if (!result.IsRight) return false;

        var list = result.RightAsEnumerable().First();
        return list.Count == actualCount;
    }

    /// <summary>
    /// Invariant: Duplicate IDs always fail (idempotent protection).
    /// </summary>
    [Property(MaxTest = 50)]
    public bool Property_DuplicateCreate_AlwaysFails(NonEmptyString subjectId)
    {
        if (string.IsNullOrWhiteSpace(subjectId.Get)) return true;

        var store = CreateStore();
        var id = Guid.NewGuid().ToString();
        var request = DSRRequest.Create(id, subjectId.Get, DataSubjectRight.Access, DateTimeOffset.UtcNow);

        var first = store.CreateAsync(request).AsTask().Result;
        var second = store.CreateAsync(request).AsTask().Result;

        return first.IsRight && second.IsLeft;
    }

    #endregion

    #region Status Update Invariants

    /// <summary>
    /// Invariant: After UpdateStatusAsync, GetByIdAsync reflects the new status.
    /// </summary>
    [Property(MaxTest = 30)]
    public bool Property_UpdateStatus_AlwaysReflectedInGet(NonEmptyString subjectId)
    {
        if (string.IsNullOrWhiteSpace(subjectId.Get)) return true;

        var store = CreateStore();
        var id = Guid.NewGuid().ToString();
        var request = DSRRequest.Create(id, subjectId.Get, DataSubjectRight.Access, DateTimeOffset.UtcNow);
        store.CreateAsync(request).AsTask().Wait();

        var updateResult = store.UpdateStatusAsync(id, DSRRequestStatus.InProgress, null).AsTask().Result;
        if (!updateResult.IsRight) return false;

        var getResult = store.GetByIdAsync(id).AsTask().Result;
        var option = (Option<DSRRequest>)getResult;
        var found = (DSRRequest)option;

        return found.Status == DSRRequestStatus.InProgress;
    }

    #endregion

    #region HasActiveRestriction Invariants

    /// <summary>
    /// Invariant: HasActiveRestrictionAsync is true only when a Restriction right request is pending.
    /// </summary>
    [Property(MaxTest = 50)]
    public bool Property_HasActiveRestriction_OnlyForPendingRestriction(NonEmptyString subjectId)
    {
        if (string.IsNullOrWhiteSpace(subjectId.Get)) return true;

        var store = CreateStore();
        var id = Guid.NewGuid().ToString();

        // No requests → no restriction
        var before = store.HasActiveRestrictionAsync(subjectId.Get).AsTask().Result;
        if (!before.IsRight || (bool)before) return false;

        // Non-restriction request → no restriction
        var accessRequest = DSRRequest.Create(id, subjectId.Get, DataSubjectRight.Access, DateTimeOffset.UtcNow);
        store.CreateAsync(accessRequest).AsTask().Wait();
        var afterAccess = store.HasActiveRestrictionAsync(subjectId.Get).AsTask().Result;
        if (!afterAccess.IsRight || (bool)afterAccess) return false;

        // Restriction request → has restriction
        var restrictionId = Guid.NewGuid().ToString();
        var restrictionRequest = DSRRequest.Create(restrictionId, subjectId.Get, DataSubjectRight.Restriction, DateTimeOffset.UtcNow);
        store.CreateAsync(restrictionRequest).AsTask().Wait();
        var afterRestriction = store.HasActiveRestrictionAsync(subjectId.Get).AsTask().Result;
        if (!afterRestriction.IsRight || !(bool)afterRestriction) return false;

        // Completed restriction → no restriction
        store.UpdateStatusAsync(restrictionId, DSRRequestStatus.Completed, null).AsTask().Wait();
        var afterComplete = store.HasActiveRestrictionAsync(subjectId.Get).AsTask().Result;
        return afterComplete.IsRight && !(bool)afterComplete;
    }

    #endregion

    #region Helpers

    private static InMemoryDSRRequestStore CreateStore() =>
        new(TimeProvider.System, Microsoft.Extensions.Logging.Abstractions.NullLogger<InMemoryDSRRequestStore>.Instance);

    #endregion
}
