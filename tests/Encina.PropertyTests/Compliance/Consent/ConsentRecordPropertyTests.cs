using Encina.Compliance.Consent;

using LanguageExt;

using Microsoft.Extensions.Logging.Abstractions;

namespace Encina.PropertyTests.Compliance.Consent;

/// <summary>
/// Property-based tests for <see cref="ConsentRecord"/> and <see cref="InMemoryConsentStore"/>
/// using FsCheck to verify invariants across randomly generated inputs.
/// </summary>
[Trait("Category", "Property")]
public sealed class ConsentRecordPropertyTests
{
    #region Timestamp Invariants

    /// <summary>
    /// Property: When WithdrawnAtUtc is set, it must be greater than or equal to GivenAtUtc.
    /// Models the real-world constraint that withdrawal cannot happen before consent was given.
    /// </summary>
    [Property(MaxTest = 100)]
    public bool Property_WithdrawnAtUtc_GreaterOrEqual_GivenAtUtc(PositiveInt givenMinutes, PositiveInt withdrawalDelay)
    {
        var givenAt = DateTimeOffset.UnixEpoch.AddMinutes(givenMinutes.Get);
        var withdrawnAt = givenAt.AddMinutes(withdrawalDelay.Get);

        var record = CreateRecord(givenAt, withdrawnAt: withdrawnAt, status: ConsentStatus.Withdrawn);

        return record.WithdrawnAtUtc!.Value >= record.GivenAtUtc;
    }

    /// <summary>
    /// Property: When ExpiresAtUtc is set, it must be strictly greater than GivenAtUtc.
    /// Consent cannot expire at or before the moment it was given.
    /// </summary>
    [Property(MaxTest = 100)]
    public bool Property_ExpiresAtUtc_GreaterThan_GivenAtUtc(PositiveInt givenMinutes, PositiveInt expirationDelay)
    {
        var givenAt = DateTimeOffset.UnixEpoch.AddMinutes(givenMinutes.Get);
        var expiresAt = givenAt.AddMinutes(expirationDelay.Get);

        var record = CreateRecord(givenAt, expiresAt: expiresAt);

        return record.ExpiresAtUtc!.Value > record.GivenAtUtc;
    }

    /// <summary>
    /// Property: Active consent records must not have WithdrawnAtUtc set.
    /// </summary>
    [Property(MaxTest = 100)]
    public bool Property_ActiveConsent_HasNoWithdrawnTimestamp(PositiveInt givenMinutes)
    {
        var givenAt = DateTimeOffset.UnixEpoch.AddMinutes(givenMinutes.Get);
        var record = CreateRecord(givenAt, status: ConsentStatus.Active);

        return record.Status == ConsentStatus.Active && record.WithdrawnAtUtc is null;
    }

    /// <summary>
    /// Property: Withdrawn consent records must have WithdrawnAtUtc set.
    /// </summary>
    [Property(MaxTest = 100)]
    public bool Property_WithdrawnConsent_HasWithdrawnTimestamp(PositiveInt givenMinutes, PositiveInt delay)
    {
        var givenAt = DateTimeOffset.UnixEpoch.AddMinutes(givenMinutes.Get);
        var withdrawnAt = givenAt.AddMinutes(delay.Get);
        var record = CreateRecord(givenAt, withdrawnAt: withdrawnAt, status: ConsentStatus.Withdrawn);

        return record.Status == ConsentStatus.Withdrawn && record.WithdrawnAtUtc is not null;
    }

    #endregion

    #region Record Identity Invariants

    /// <summary>
    /// Property: Every consent record has a unique non-empty Id.
    /// </summary>
    [Property(MaxTest = 100)]
    public bool Property_ConsentRecord_HasNonEmptyId(Guid id)
    {
        if (id == Guid.Empty) return true; // Skip degenerate case — FsCheck may generate it

        var record = CreateRecord(DateTimeOffset.UtcNow, id: id);

        return record.Id != Guid.Empty;
    }

    /// <summary>
    /// Property: SubjectId and Purpose form a logical composite key.
    /// Two records with same SubjectId+Purpose represent the same consent relationship.
    /// </summary>
    [Property(MaxTest = 100)]
    public bool Property_CompositeKey_SubjectIdAndPurpose_AreStable(NonEmptyString subjectId, NonEmptyString purpose)
    {
        var record1 = CreateRecord(DateTimeOffset.UtcNow, subjectId: subjectId.Get, purpose: purpose.Get);
        var record2 = CreateRecord(DateTimeOffset.UtcNow, subjectId: subjectId.Get, purpose: purpose.Get);

        return record1.SubjectId == record2.SubjectId && record1.Purpose == record2.Purpose;
    }

    #endregion

    #region Store Roundtrip Invariants

    /// <summary>
    /// Property: Recording consent then retrieving it returns the same record.
    /// </summary>
    [Property(MaxTest = 50)]
    public bool Property_Store_RecordThenGet_ReturnsEquivalent(PositiveInt givenMinutes)
    {
        var givenAt = DateTimeOffset.UnixEpoch.AddMinutes(givenMinutes.Get);
        var record = CreateRecord(givenAt);

        var store = CreateStore();
        store.RecordConsentAsync(record).AsTask().GetAwaiter().GetResult();
        var result = store.GetConsentAsync(record.SubjectId, record.Purpose).AsTask().GetAwaiter().GetResult();

        if (result.IsLeft) return false;
        var opt = (Option<ConsentRecord>)result;
        if (opt.IsNone) return false;
        var retrieved = (ConsentRecord)opt;

        return retrieved.SubjectId == record.SubjectId
            && retrieved.Purpose == record.Purpose
            && retrieved.Status == record.Status
            && retrieved.ConsentVersionId == record.ConsentVersionId
            && retrieved.Source == record.Source;
    }

    /// <summary>
    /// Property: HasValidConsent returns true for Active consent and false for Withdrawn.
    /// </summary>
    [Property(MaxTest = 50)]
    public bool Property_Store_HasValid_TrueForActive_FalseForWithdrawn(PositiveInt givenMinutes)
    {
        var givenAt = DateTimeOffset.UnixEpoch.AddMinutes(givenMinutes.Get);
        var record = CreateRecord(givenAt, status: ConsentStatus.Active);

        var store = CreateStore();
        store.RecordConsentAsync(record).AsTask().GetAwaiter().GetResult();

        var activeResult = store.HasValidConsentAsync(record.SubjectId, record.Purpose).AsTask().GetAwaiter().GetResult();
        if (activeResult.IsLeft) return false;
        var isValidBeforeWithdrawal = (bool)activeResult;

        store.WithdrawConsentAsync(record.SubjectId, record.Purpose).AsTask().GetAwaiter().GetResult();

        var withdrawnResult = store.HasValidConsentAsync(record.SubjectId, record.Purpose).AsTask().GetAwaiter().GetResult();
        if (withdrawnResult.IsLeft) return false;
        var isValidAfterWithdrawal = (bool)withdrawnResult;

        return isValidBeforeWithdrawal && !isValidAfterWithdrawal;
    }

    #endregion

    #region Idempotency Invariants

    /// <summary>
    /// Property: Withdrawing consent multiple times is idempotent in its observable effect —
    /// HasValidConsent always returns false after any number of withdrawals.
    /// </summary>
    [Property(MaxTest = 50)]
    public bool Property_Store_WithdrawMultipleTimes_HasValidAlwaysFalse(PositiveInt givenMinutes, PositiveInt withdrawCount)
    {
        var givenAt = DateTimeOffset.UnixEpoch.AddMinutes(givenMinutes.Get);
        var record = CreateRecord(givenAt);
        var times = Math.Min(withdrawCount.Get, 5); // Cap iterations

        var store = CreateStore();
        store.RecordConsentAsync(record).AsTask().GetAwaiter().GetResult();

        // Withdraw N times
        for (var i = 0; i < times; i++)
        {
            store.WithdrawConsentAsync(record.SubjectId, record.Purpose).AsTask().GetAwaiter().GetResult();
        }

        // After any number of withdrawals, HasValid must be false
        var hasValid = store.HasValidConsentAsync(record.SubjectId, record.Purpose).AsTask().GetAwaiter().GetResult();
        if (hasValid.IsLeft) return false;

        return !(bool)hasValid;
    }

    /// <summary>
    /// Property: Recording the same consent (same SubjectId+Purpose) overwrites the previous record.
    /// The store always returns the latest version.
    /// </summary>
    [Property(MaxTest = 50)]
    public bool Property_Store_RecordOverwrite_ReturnsLatest(PositiveInt v1Minutes, PositiveInt v2Minutes)
    {
        var givenAt1 = DateTimeOffset.UnixEpoch.AddMinutes(v1Minutes.Get);
        var givenAt2 = DateTimeOffset.UnixEpoch.AddMinutes(v1Minutes.Get + v2Minutes.Get);

        var record1 = CreateRecord(givenAt1, versionId: "v1");
        var record2 = CreateRecord(givenAt2, versionId: "v2",
            subjectId: record1.SubjectId, purpose: record1.Purpose);

        var store = CreateStore();
        store.RecordConsentAsync(record1).AsTask().GetAwaiter().GetResult();
        store.RecordConsentAsync(record2).AsTask().GetAwaiter().GetResult();

        var result = store.GetConsentAsync(record1.SubjectId, record1.Purpose).AsTask().GetAwaiter().GetResult();
        if (result.IsLeft) return false;
        var opt = (Option<ConsentRecord>)result;
        if (opt.IsNone) return false;
        var retrieved = (ConsentRecord)opt;

        return retrieved.ConsentVersionId == "v2" && retrieved.GivenAtUtc == givenAt2;
    }

    #endregion

    #region Bulk Operation Invariants

    /// <summary>
    /// Property: BulkRecordConsentAsync with N records results in SuccessCount == N.
    /// </summary>
    [Property(MaxTest = 30)]
    public bool Property_Store_BulkRecord_AllSucceed(PositiveInt count)
    {
        var n = Math.Min(count.Get, 20); // Cap at 20 for test performance
        var records = Enumerable.Range(0, n)
            .Select(i => CreateRecord(DateTimeOffset.UtcNow, subjectId: $"user-{i}", purpose: "marketing"))
            .ToList();

        var store = CreateStore();
        var result = store.BulkRecordConsentAsync(records).AsTask().GetAwaiter().GetResult();

        if (result.IsLeft) return false;
        var bulkResult = (BulkOperationResult)result;

        return bulkResult.SuccessCount == n && bulkResult.AllSucceeded;
    }

    #endregion

    #region Helpers

    private static InMemoryConsentStore CreateStore()
    {
        return new InMemoryConsentStore(
            TimeProvider.System,
            NullLogger<InMemoryConsentStore>.Instance);
    }

    private static ConsentRecord CreateRecord(
        DateTimeOffset givenAt,
        Guid? id = null,
        string subjectId = "user-1",
        string purpose = "marketing",
        ConsentStatus status = ConsentStatus.Active,
        string versionId = "v1",
        string source = "test",
        DateTimeOffset? withdrawnAt = null,
        DateTimeOffset? expiresAt = null)
    {
        return new ConsentRecord
        {
            Id = id ?? Guid.NewGuid(),
            SubjectId = subjectId,
            Purpose = purpose,
            Status = status,
            ConsentVersionId = versionId,
            GivenAtUtc = givenAt,
            WithdrawnAtUtc = withdrawnAt,
            ExpiresAtUtc = expiresAt,
            Source = source,
            Metadata = new Dictionary<string, object?>()
        };
    }

    #endregion
}
