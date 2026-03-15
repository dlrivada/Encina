using Encina.Compliance.Consent;
using Encina.Compliance.Consent.Aggregates;

using FsCheck;
using FsCheck.Xunit;

namespace Encina.PropertyTests.Compliance.Consent;

/// <summary>
/// Property-based tests for <see cref="ConsentAggregate"/> verifying
/// invariants across randomized inputs using FsCheck.
/// </summary>
public class ConsentAggregatePropertyTests
{
    private static readonly IReadOnlyDictionary<string, object?> EmptyMetadata = new Dictionary<string, object?>();

    #region Grant Invariants

    /// <summary>
    /// Invariant: Grant preserves all input properties in the resulting aggregate.
    /// </summary>
    [Property(MaxTest = 50)]
    public bool Grant_PreservesAllInputProperties(
        NonEmptyString subject,
        NonEmptyString purpose,
        NonEmptyString versionId,
        NonEmptyString source,
        NonEmptyString grantedBy)
    {
        var subjectId = subject.Get.Trim();
        var purposeVal = purpose.Get.Trim();
        var versionVal = versionId.Get.Trim();
        var sourceVal = source.Get.Trim();
        var grantedByVal = grantedBy.Get.Trim();

        if (string.IsNullOrWhiteSpace(subjectId) ||
            string.IsNullOrWhiteSpace(purposeVal) ||
            string.IsNullOrWhiteSpace(versionVal) ||
            string.IsNullOrWhiteSpace(sourceVal) ||
            string.IsNullOrWhiteSpace(grantedByVal))
        {
            return true; // Skip whitespace-only inputs (guard clauses handle these)
        }

        var id = Guid.NewGuid();
        var ipAddress = "192.168.1.1";
        var proofOfConsent = "hash-abc123";
        var now = DateTimeOffset.UtcNow;

        var aggregate = ConsentAggregate.Grant(
            id, subjectId, purposeVal, versionVal, sourceVal,
            ipAddress, proofOfConsent, EmptyMetadata, null, grantedByVal, now);

        return aggregate.Id == id &&
               aggregate.DataSubjectId == subjectId &&
               aggregate.Purpose == purposeVal &&
               aggregate.ConsentVersionId == versionVal &&
               aggregate.Source == sourceVal &&
               aggregate.IpAddress == ipAddress &&
               aggregate.ProofOfConsent == proofOfConsent;
    }

    /// <summary>
    /// Invariant: Newly granted consent is always in Active status.
    /// </summary>
    [Property(MaxTest = 50)]
    public bool Grant_AlwaysProducesActiveStatus(
        NonEmptyString subject,
        NonEmptyString purpose,
        NonEmptyString grantedBy)
    {
        var subjectId = subject.Get.Trim();
        var purposeVal = purpose.Get.Trim();
        var grantedByVal = grantedBy.Get.Trim();

        if (string.IsNullOrWhiteSpace(subjectId) ||
            string.IsNullOrWhiteSpace(purposeVal) ||
            string.IsNullOrWhiteSpace(grantedByVal))
        {
            return true;
        }

        var aggregate = ConsentAggregate.Grant(
            Guid.NewGuid(), subjectId, purposeVal, "v1", "web",
            null, null, EmptyMetadata, null, grantedByVal, DateTimeOffset.UtcNow);

        return aggregate.Status == ConsentStatus.Active;
    }

    #endregion

    #region Withdraw Invariants

    /// <summary>
    /// Invariant: Withdrawn consent is never in Active status.
    /// </summary>
    [Property(MaxTest = 50)]
    public bool Withdraw_NeverProducesActiveStatus(NonEmptyString withdrawnBy)
    {
        var withdrawnByVal = withdrawnBy.Get.Trim();

        if (string.IsNullOrWhiteSpace(withdrawnByVal))
        {
            return true;
        }

        var aggregate = CreateActiveAggregate();
        aggregate.Withdraw(withdrawnByVal, "no longer needed", DateTimeOffset.UtcNow);

        return aggregate.Status != ConsentStatus.Active;
    }

    /// <summary>
    /// Invariant: Withdrawing from Active status produces Withdrawn status.
    /// </summary>
    [Property(MaxTest = 50)]
    public bool Withdraw_FromActive_ProducesWithdrawnStatus(NonEmptyString withdrawnBy)
    {
        var withdrawnByVal = withdrawnBy.Get.Trim();

        if (string.IsNullOrWhiteSpace(withdrawnByVal))
        {
            return true;
        }

        var aggregate = CreateActiveAggregate();
        aggregate.Withdraw(withdrawnByVal, null, DateTimeOffset.UtcNow);

        return aggregate.Status == ConsentStatus.Withdrawn;
    }

    #endregion

    #region Expire Invariants

    /// <summary>
    /// Invariant: Expired consent is never in Active status.
    /// </summary>
    [Property(MaxTest = 50)]
    public bool Expire_NeverProducesActiveStatus(PositiveInt daysOffset)
    {
        var expiresAt = DateTimeOffset.UtcNow.AddDays(daysOffset.Get);
        var aggregate = ConsentAggregate.Grant(
            Guid.NewGuid(), "user-1", "marketing", "v1", "web",
            null, null, EmptyMetadata, expiresAt, "admin", DateTimeOffset.UtcNow);

        aggregate.Expire(DateTimeOffset.UtcNow);

        return aggregate.Status != ConsentStatus.Active;
    }

    /// <summary>
    /// Invariant: Expiring from Active status produces Expired status.
    /// </summary>
    [Property(MaxTest = 50)]
    public bool Expire_FromActive_ProducesExpiredStatus(PositiveInt daysOffset)
    {
        var expiresAt = DateTimeOffset.UtcNow.AddDays(daysOffset.Get);
        var aggregate = ConsentAggregate.Grant(
            Guid.NewGuid(), "user-1", "marketing", "v1", "web",
            null, null, EmptyMetadata, expiresAt, "admin", DateTimeOffset.UtcNow);

        aggregate.Expire(DateTimeOffset.UtcNow);

        return aggregate.Status == ConsentStatus.Expired;
    }

    #endregion

    #region ExpiresAtUtc Invariants

    /// <summary>
    /// Invariant: Active consent with ExpiresAtUtc in the future is valid for processing.
    /// </summary>
    [Property(MaxTest = 50)]
    public bool Active_BeforeExpiry_ExpiresAtUtcIsInFuture(PositiveInt daysUntilExpiry)
    {
        var now = DateTimeOffset.UtcNow;
        var expiresAt = now.AddDays(daysUntilExpiry.Get);

        var aggregate = ConsentAggregate.Grant(
            Guid.NewGuid(), "user-1", "marketing", "v1", "web",
            null, null, EmptyMetadata, expiresAt, "admin", now);

        return aggregate.Status == ConsentStatus.Active &&
               aggregate.ExpiresAtUtc.HasValue &&
               aggregate.ExpiresAtUtc.Value > now;
    }

    #endregion

    #region ChangeVersion Invariants

    /// <summary>
    /// Invariant: ChangeVersion with requiresReconsent=true always produces RequiresReconsent status.
    /// </summary>
    [Property(MaxTest = 50)]
    public bool ChangeVersion_RequiresReconsent_AlwaysProducesRequiresReconsentStatus(
        NonEmptyString newVersionId,
        NonEmptyString description,
        NonEmptyString changedBy)
    {
        var versionVal = newVersionId.Get.Trim();
        var descVal = description.Get.Trim();
        var changedByVal = changedBy.Get.Trim();

        if (string.IsNullOrWhiteSpace(versionVal) ||
            string.IsNullOrWhiteSpace(descVal) ||
            string.IsNullOrWhiteSpace(changedByVal))
        {
            return true;
        }

        var aggregate = CreateActiveAggregate();
        aggregate.ChangeVersion(versionVal, descVal, requiresReconsent: true, changedByVal, DateTimeOffset.UtcNow);

        return aggregate.Status == ConsentStatus.RequiresReconsent;
    }

    /// <summary>
    /// Invariant: ChangeVersion with requiresReconsent=false keeps Active status.
    /// </summary>
    [Property(MaxTest = 50)]
    public bool ChangeVersion_DoesNotRequireReconsent_KeepsActiveStatus(
        NonEmptyString newVersionId,
        NonEmptyString description,
        NonEmptyString changedBy)
    {
        var versionVal = newVersionId.Get.Trim();
        var descVal = description.Get.Trim();
        var changedByVal = changedBy.Get.Trim();

        if (string.IsNullOrWhiteSpace(versionVal) ||
            string.IsNullOrWhiteSpace(descVal) ||
            string.IsNullOrWhiteSpace(changedByVal))
        {
            return true;
        }

        var aggregate = CreateActiveAggregate();
        aggregate.ChangeVersion(versionVal, descVal, requiresReconsent: false, changedByVal, DateTimeOffset.UtcNow);

        return aggregate.Status == ConsentStatus.Active;
    }

    #endregion

    #region Reconsent Invariants

    /// <summary>
    /// Invariant: Providing reconsent always produces Active status.
    /// </summary>
    [Property(MaxTest = 50)]
    public bool ProvideReconsent_AlwaysProducesActiveStatus(
        NonEmptyString newVersionId,
        NonEmptyString source,
        NonEmptyString grantedBy)
    {
        var versionVal = newVersionId.Get.Trim();
        var sourceVal = source.Get.Trim();
        var grantedByVal = grantedBy.Get.Trim();

        if (string.IsNullOrWhiteSpace(versionVal) ||
            string.IsNullOrWhiteSpace(sourceVal) ||
            string.IsNullOrWhiteSpace(grantedByVal))
        {
            return true;
        }

        var aggregate = CreateRequiresReconsentAggregate();
        aggregate.ProvideReconsent(
            versionVal, sourceVal, null, null, EmptyMetadata, null, grantedByVal, DateTimeOffset.UtcNow);

        return aggregate.Status == ConsentStatus.Active;
    }

    /// <summary>
    /// Invariant: Reconsent clears withdrawal data (WithdrawnAtUtc and WithdrawalReason become null).
    /// </summary>
    [Property(MaxTest = 50)]
    public bool ProvideReconsent_ClearsWithdrawalData(
        NonEmptyString source,
        NonEmptyString grantedBy)
    {
        var sourceVal = source.Get.Trim();
        var grantedByVal = grantedBy.Get.Trim();

        if (string.IsNullOrWhiteSpace(sourceVal) ||
            string.IsNullOrWhiteSpace(grantedByVal))
        {
            return true;
        }

        var aggregate = CreateRequiresReconsentAggregate();
        aggregate.ProvideReconsent(
            "v3", sourceVal, null, null, EmptyMetadata, null, grantedByVal, DateTimeOffset.UtcNow);

        return aggregate.WithdrawnAtUtc is null &&
               aggregate.WithdrawalReason is null;
    }

    #endregion

    #region Helpers

    private static ConsentAggregate CreateActiveAggregate()
    {
        return ConsentAggregate.Grant(
            Guid.NewGuid(), "user-1", "marketing", "v1", "web",
            null, null, EmptyMetadata, null, "admin", DateTimeOffset.UtcNow);
    }

    private static ConsentAggregate CreateRequiresReconsentAggregate()
    {
        var aggregate = CreateActiveAggregate();
        aggregate.ChangeVersion("v2", "Updated terms", requiresReconsent: true, "admin", DateTimeOffset.UtcNow);
        return aggregate;
    }

    #endregion
}
