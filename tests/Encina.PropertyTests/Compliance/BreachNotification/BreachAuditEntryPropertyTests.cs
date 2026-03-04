using Encina.Compliance.BreachNotification.Model;

using FsCheck;
using FsCheck.Xunit;

namespace Encina.PropertyTests.Compliance.BreachNotification;

/// <summary>
/// Property-based tests for <see cref="BreachAuditEntry"/> verifying domain invariants
/// using FsCheck random data generation.
/// </summary>
public class BreachAuditEntryPropertyTests
{
    #region Create Factory Invariants

    /// <summary>
    /// Invariant: BreachAuditEntry.Create always generates a 32-character hex Id (GUID without hyphens, N format).
    /// </summary>
    [Property(MaxTest = 50)]
    public bool Create_AlwaysGenerates32CharHexId(NonEmptyString breachId, NonEmptyString action)
    {
        var entry = BreachAuditEntry.Create(
            breachId: breachId.Get,
            action: action.Get);

        return !string.IsNullOrEmpty(entry.Id)
            && entry.Id.Length == 32
            && !entry.Id.Contains('-');
    }

    /// <summary>
    /// Invariant: BreachAuditEntry.Create preserves the BreachId parameter.
    /// </summary>
    [Property(MaxTest = 50)]
    public bool Create_PreservesBreachId(NonEmptyString breachId, NonEmptyString action)
    {
        var entry = BreachAuditEntry.Create(
            breachId: breachId.Get,
            action: action.Get);

        return entry.BreachId == breachId.Get;
    }

    /// <summary>
    /// Invariant: BreachAuditEntry.Create preserves the Action parameter.
    /// </summary>
    [Property(MaxTest = 50)]
    public bool Create_PreservesAction(NonEmptyString breachId, NonEmptyString action)
    {
        var entry = BreachAuditEntry.Create(
            breachId: breachId.Get,
            action: action.Get);

        return entry.Action == action.Get;
    }

    /// <summary>
    /// Invariant: BreachAuditEntry.Create always sets OccurredAtUtc to a UTC timestamp.
    /// </summary>
    [Property(MaxTest = 50)]
    public bool Create_TimestampAlwaysUtc(NonEmptyString breachId, NonEmptyString action)
    {
        var before = DateTimeOffset.UtcNow;
        var entry = BreachAuditEntry.Create(
            breachId: breachId.Get,
            action: action.Get);
        var after = DateTimeOffset.UtcNow;

        return entry.OccurredAtUtc.Offset == TimeSpan.Zero
            && entry.OccurredAtUtc >= before
            && entry.OccurredAtUtc <= after;
    }

    /// <summary>
    /// Invariant: BreachAuditEntry.Create preserves the optional Detail parameter.
    /// </summary>
    [Property(MaxTest = 50)]
    public bool Create_PreservesDetail(NonEmptyString breachId, NonEmptyString detail)
    {
        var entry = BreachAuditEntry.Create(
            breachId: breachId.Get,
            action: "BreachDetected",
            detail: detail.Get);

        return entry.Detail == detail.Get;
    }

    /// <summary>
    /// Invariant: BreachAuditEntry.Create preserves the optional PerformedByUserId parameter.
    /// </summary>
    [Property(MaxTest = 50)]
    public bool Create_PreservesPerformedByUserId(NonEmptyString breachId, NonEmptyString userId)
    {
        var entry = BreachAuditEntry.Create(
            breachId: breachId.Get,
            action: "AuthorityNotified",
            performedByUserId: userId.Get);

        return entry.PerformedByUserId == userId.Get;
    }

    /// <summary>
    /// Invariant: Create with different calls produces unique Ids.
    /// </summary>
    [Property(MaxTest = 50)]
    public bool Create_DifferentCallsProduceUniqueIds(NonEmptyString breachId, NonEmptyString action)
    {
        var entry1 = BreachAuditEntry.Create(
            breachId: breachId.Get,
            action: action.Get);

        var entry2 = BreachAuditEntry.Create(
            breachId: breachId.Get,
            action: action.Get);

        return entry1.Id != entry2.Id;
    }

    #endregion
}
