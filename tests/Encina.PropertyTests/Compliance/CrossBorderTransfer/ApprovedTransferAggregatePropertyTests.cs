using Encina.Compliance.CrossBorderTransfer.Aggregates;
using Encina.Compliance.CrossBorderTransfer.Model;

using FsCheck;
using FsCheck.Xunit;

namespace Encina.PropertyTests.Compliance.CrossBorderTransfer;

/// <summary>
/// Property-based tests for <see cref="ApprovedTransferAggregate"/> verifying
/// invariants across randomized inputs using FsCheck.
/// </summary>
public class ApprovedTransferAggregatePropertyTests
{
    #region Factory Invariants

    /// <summary>
    /// Invariant: Approve preserves all input properties in the resulting aggregate.
    /// </summary>
    [Property(MaxTest = 50)]
    public bool Approve_PreservesAllInputProperties(
        NonEmptyString source,
        NonEmptyString dest,
        NonEmptyString category,
        NonEmptyString approver)
    {
        var sourceCode = source.Get.Trim();
        var destCode = dest.Get.Trim();
        var dataCategory = category.Get.Trim();
        var approvedBy = approver.Get.Trim();

        if (string.IsNullOrWhiteSpace(sourceCode) ||
            string.IsNullOrWhiteSpace(destCode) ||
            string.IsNullOrWhiteSpace(dataCategory) ||
            string.IsNullOrWhiteSpace(approvedBy))
        {
            return true; // Skip whitespace-only inputs (guard clauses handle these)
        }

        var id = Guid.NewGuid();
        var aggregate = ApprovedTransferAggregate.Approve(
            id, sourceCode, destCode, dataCategory,
            TransferBasis.AdequacyDecision, approvedBy: approvedBy);

        return aggregate.Id == id &&
               aggregate.SourceCountryCode == sourceCode &&
               aggregate.DestinationCountryCode == destCode &&
               aggregate.DataCategory == dataCategory &&
               aggregate.ApprovedBy == approvedBy &&
               aggregate.Basis == TransferBasis.AdequacyDecision &&
               !aggregate.IsRevoked &&
               !aggregate.IsExpired;
    }

    #endregion

    #region IsValid Invariants

    /// <summary>
    /// Invariant: A revoked transfer is never valid, regardless of time.
    /// </summary>
    [Property(MaxTest = 50)]
    public bool IsValid_RevokedTransfer_AlwaysFalse(PositiveInt minutesOffset)
    {
        var aggregate = ApprovedTransferAggregate.Approve(
            Guid.NewGuid(), "DE", "US", "personal-data",
            TransferBasis.SCCs, approvedBy: "admin");

        aggregate.Revoke("Test revocation", "admin");

        var checkTime = DateTimeOffset.UtcNow.AddMinutes(minutesOffset.Get);
        return !aggregate.IsValid(checkTime);
    }

    /// <summary>
    /// Invariant: A transfer checked before its expiration is always valid
    /// (when not revoked or expired via Expire()).
    /// </summary>
    [Property(MaxTest = 50)]
    public bool IsValid_BeforeExpiration_AlwaysTrue(PositiveInt minutesBefore)
    {
        var expiresAt = DateTimeOffset.UtcNow.AddDays(365);
        var aggregate = ApprovedTransferAggregate.Approve(
            Guid.NewGuid(), "DE", "JP", "personal-data",
            TransferBasis.AdequacyDecision,
            approvedBy: "admin",
            expiresAtUtc: expiresAt);

        var checkTime = expiresAt.AddMinutes(-minutesBefore.Get);
        return aggregate.IsValid(checkTime);
    }

    /// <summary>
    /// Invariant: A transfer checked at or after its expiration is always invalid.
    /// </summary>
    [Property(MaxTest = 50)]
    public bool IsValid_AfterExpiration_AlwaysFalse(PositiveInt minutesAfter)
    {
        var expiresAt = DateTimeOffset.UtcNow.AddDays(-1);
        var aggregate = ApprovedTransferAggregate.Approve(
            Guid.NewGuid(), "DE", "US", "personal-data",
            TransferBasis.SCCs,
            approvedBy: "admin",
            expiresAtUtc: expiresAt);

        var checkTime = expiresAt.AddMinutes(minutesAfter.Get);
        return !aggregate.IsValid(checkTime);
    }

    #endregion

    #region Renew Invariants

    /// <summary>
    /// Invariant: Renewing a transfer updates ExpiresAtUtc to the new date.
    /// </summary>
    [Property(MaxTest = 50)]
    public bool Renew_UpdatesExpiration_ToNewDate(PositiveInt daysOffset)
    {
        var aggregate = ApprovedTransferAggregate.Approve(
            Guid.NewGuid(), "DE", "US", "personal-data",
            TransferBasis.SCCs,
            approvedBy: "admin",
            expiresAtUtc: DateTimeOffset.UtcNow.AddDays(30));

        var newExpiration = DateTimeOffset.UtcNow.AddDays(daysOffset.Get);
        aggregate.Renew(newExpiration, "admin");

        return aggregate.ExpiresAtUtc.HasValue &&
               aggregate.ExpiresAtUtc.Value == newExpiration &&
               !aggregate.IsExpired;
    }

    #endregion
}
