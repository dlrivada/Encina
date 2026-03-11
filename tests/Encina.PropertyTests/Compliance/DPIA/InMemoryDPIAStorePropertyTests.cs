using Encina.Compliance.DPIA;
using Encina.Compliance.DPIA.Model;

using FsCheck;
using FsCheck.Xunit;

using LanguageExt;

using Microsoft.Extensions.Logging.Abstractions;

using static LanguageExt.Prelude;

namespace Encina.PropertyTests.Compliance.DPIA;

/// <summary>
/// Property-based tests for <see cref="InMemoryDPIAStore"/> verifying store
/// invariants using FsCheck random data generation.
/// </summary>
public class InMemoryDPIAStorePropertyTests
{
    private static InMemoryDPIAStore CreateStore() =>
        new(NullLogger<InMemoryDPIAStore>.Instance);

    #region Store Roundtrip Invariants

    /// <summary>
    /// Invariant: Any saved assessment can always be retrieved by its RequestTypeName.
    /// </summary>
    [Property(MaxTest = 50)]
    public bool Save_ThenGetByRequestType_AlwaysReturnsStoredAssessment(NonEmptyString requestType)
    {
        var store = CreateStore();
        var assessment = CreateAssessment(requestType.Get);

        var saveResult = store.SaveAssessmentAsync(assessment).AsTask().Result;
        if (!saveResult.IsRight) return false;

        var getResult = store.GetAssessmentAsync(requestType.Get).AsTask().Result;
        if (!getResult.IsRight) return false;

        var option = (Option<DPIAAssessment>)getResult;
        return option.Match(
            Some: retrieved => retrieved.Id == assessment.Id
                && retrieved.RequestTypeName == assessment.RequestTypeName,
            None: () => false);
    }

    /// <summary>
    /// Invariant: Any saved assessment can always be retrieved by its Id.
    /// </summary>
    [Property(MaxTest = 50)]
    public bool Save_ThenGetById_AlwaysReturnsStoredAssessment(NonEmptyString requestType)
    {
        var store = CreateStore();
        var assessment = CreateAssessment(requestType.Get);

        store.SaveAssessmentAsync(assessment).AsTask().Wait();

        var getResult = store.GetAssessmentByIdAsync(assessment.Id).AsTask().Result;
        if (!getResult.IsRight) return false;

        var option = (Option<DPIAAssessment>)getResult;
        return option.Match(
            Some: retrieved => retrieved.Id == assessment.Id,
            None: () => false);
    }

    /// <summary>
    /// Invariant: Getting a non-existent assessment always returns None.
    /// </summary>
    [Property(MaxTest = 50)]
    public bool GetByRequestType_NonExistent_AlwaysReturnsNone(NonEmptyString requestType)
    {
        var store = CreateStore();

        var result = store.GetAssessmentAsync(requestType.Get).AsTask().Result;
        if (!result.IsRight) return false;

        var option = (Option<DPIAAssessment>)result;
        return option.IsNone;
    }

    /// <summary>
    /// Invariant: Saving with the same RequestTypeName always overwrites the previous.
    /// </summary>
    [Property(MaxTest = 50)]
    public bool Save_SameRequestType_AlwaysOverwrites(NonEmptyString requestType)
    {
        var store = CreateStore();
        var first = CreateAssessment(requestType.Get);
        var second = first with { Status = DPIAAssessmentStatus.Approved };

        store.SaveAssessmentAsync(first).AsTask().Wait();
        store.SaveAssessmentAsync(second).AsTask().Wait();

        var result = store.GetAssessmentAsync(requestType.Get).AsTask().Result;
        var option = (Option<DPIAAssessment>)result;
        return option.Match(
            Some: retrieved => retrieved.Status == DPIAAssessmentStatus.Approved,
            None: () => false);
    }

    /// <summary>
    /// Invariant: After delete, the assessment is no longer retrievable.
    /// </summary>
    [Property(MaxTest = 50)]
    public bool Delete_ThenGet_AlwaysReturnsNone(NonEmptyString requestType)
    {
        var store = CreateStore();
        var assessment = CreateAssessment(requestType.Get);

        store.SaveAssessmentAsync(assessment).AsTask().Wait();
        var deleteResult = store.DeleteAssessmentAsync(assessment.Id).AsTask().Result;
        if (!deleteResult.IsRight) return false;

        var result = store.GetAssessmentByIdAsync(assessment.Id).AsTask().Result;
        var option = (Option<DPIAAssessment>)result;
        return option.IsNone;
    }

    #endregion

    #region GetExpiredAssessments Invariants

    /// <summary>
    /// Invariant: GetExpiredAssessments never returns non-approved assessments.
    /// </summary>
    [Property(MaxTest = 50)]
    public bool GetExpired_NeverReturnsNonApproved(PositiveInt daysBehind)
    {
        var store = CreateStore();
        var now = DateTimeOffset.UtcNow;

        var draft = CreateAssessment("Ns.Draft") with
        {
            Status = DPIAAssessmentStatus.Draft,
            NextReviewAtUtc = now.AddDays(-daysBehind.Get)
        };

        store.SaveAssessmentAsync(draft).AsTask().Wait();

        var result = store.GetExpiredAssessmentsAsync(now).AsTask().Result;
        var list = result.Match(l => l, _ => (IReadOnlyList<DPIAAssessment>)[]);
        return !list.Any(a => a.RequestTypeName == "Ns.Draft");
    }

    /// <summary>
    /// Invariant: GetExpiredAssessments always returns approved assessments with past review dates.
    /// </summary>
    [Property(MaxTest = 50)]
    public bool GetExpired_AlwaysReturnsApprovedWithPastReview(PositiveInt daysBehind)
    {
        var store = CreateStore();
        var now = DateTimeOffset.UtcNow;

        var expired = CreateAssessment("Ns.Expired") with
        {
            Status = DPIAAssessmentStatus.Approved,
            NextReviewAtUtc = now.AddDays(-daysBehind.Get)
        };

        store.SaveAssessmentAsync(expired).AsTask().Wait();

        var result = store.GetExpiredAssessmentsAsync(now).AsTask().Result;
        var list = result.Match(l => l, _ => (IReadOnlyList<DPIAAssessment>)[]);
        return list.Any(a => a.RequestTypeName == "Ns.Expired");
    }

    #endregion

    #region Helpers

    private static DPIAAssessment CreateAssessment(string requestTypeName) => new()
    {
        Id = Guid.NewGuid(),
        RequestTypeName = requestTypeName,
        Status = DPIAAssessmentStatus.Draft,
        CreatedAtUtc = DateTimeOffset.UtcNow
    };

    #endregion
}
