using Encina.Compliance.DataSubjectRights;

namespace Encina.PropertyTests.Compliance.DataSubjectRights;

/// <summary>
/// Property-based tests for <see cref="DSRRequest"/> verifying domain invariants
/// using FsCheck random data generation.
/// </summary>
public class DSRRequestPropertyTests
{
    #region Deadline Invariants

    /// <summary>
    /// Invariant: DeadlineAtUtc is always exactly 30 days after ReceivedAtUtc.
    /// Per GDPR Article 12(3), the controller must respond within one month of receipt.
    /// </summary>
    [Property(MaxTest = 100)]
    public bool Property_Create_DeadlineAlways30DaysAfterReceivedAt(NonEmptyString id, NonEmptyString subjectId)
    {
        var receivedAt = DateTimeOffset.UtcNow;
        var request = DSRRequest.Create(id.Get, subjectId.Get, DataSubjectRight.Access, receivedAt);

        return request.DeadlineAtUtc == receivedAt.AddDays(30);
    }

    /// <summary>
    /// Invariant: Created requests always have Received status.
    /// </summary>
    [Property(MaxTest = 50)]
    public bool Property_Create_StatusAlwaysReceived(NonEmptyString id, NonEmptyString subjectId)
    {
        var request = DSRRequest.Create(id.Get, subjectId.Get, DataSubjectRight.Erasure, DateTimeOffset.UtcNow);

        return request.Status == DSRRequestStatus.Received;
    }

    /// <summary>
    /// Invariant: Created requests never have completed, extension, rejection or verification timestamps.
    /// </summary>
    [Property(MaxTest = 50)]
    public bool Property_Create_OptionalTimestampsAlwaysNull(NonEmptyString id, NonEmptyString subjectId)
    {
        var request = DSRRequest.Create(id.Get, subjectId.Get, DataSubjectRight.Access, DateTimeOffset.UtcNow);

        return request.CompletedAtUtc is null
            && request.ExtendedDeadlineAtUtc is null
            && request.VerifiedAtUtc is null
            && request.ExtensionReason is null
            && request.RejectionReason is null
            && request.ProcessedByUserId is null;
    }

    /// <summary>
    /// Invariant: Id and SubjectId are always preserved as provided.
    /// </summary>
    [Property(MaxTest = 50)]
    public bool Property_Create_IdAndSubjectIdAlwaysPreserved(NonEmptyString id, NonEmptyString subjectId)
    {
        var request = DSRRequest.Create(id.Get, subjectId.Get, DataSubjectRight.Portability, DateTimeOffset.UtcNow);

        return request.Id == id.Get && request.SubjectId == subjectId.Get;
    }

    #endregion

    #region Record Immutability Invariants

    /// <summary>
    /// Invariant: Using 'with' creates a new record, leaving the original unchanged.
    /// </summary>
    [Property(MaxTest = 50)]
    public bool Property_WithExpression_OriginalUnchanged(NonEmptyString id, NonEmptyString subjectId)
    {
        var original = DSRRequest.Create(id.Get, subjectId.Get, DataSubjectRight.Access, DateTimeOffset.UtcNow);
        var modified = original with { Status = DSRRequestStatus.Completed };

        return original.Status == DSRRequestStatus.Received
            && modified.Status == DSRRequestStatus.Completed;
    }

    #endregion
}
