using Encina.Compliance.ProcessorAgreements.Aggregates;
using Encina.Compliance.ProcessorAgreements.Model;

using FsCheck;
using FsCheck.Xunit;

namespace Encina.PropertyTests.Compliance.ProcessorAgreements;

/// <summary>
/// Property-based tests for <see cref="DPAAggregate"/> verifying domain invariants
/// using FsCheck random data generation.
/// </summary>
public class DPAAggregatePropertyTests
{
    private static readonly DateTimeOffset Now = new(2026, 3, 16, 12, 0, 0, TimeSpan.Zero);

    /// <summary>
    /// Invariant: Execute always sets Status to Active.
    /// </summary>
    [Property(MaxTest = 50)]
    public bool Execute_Always_SetsActiveStatus(bool hasSCCs)
    {
        var aggregate = CreateExecutedAggregate(hasSCCs: hasSCCs);
        return aggregate.Status == DPAStatus.Active;
    }

    /// <summary>
    /// Invariant: Execute always sets ProcessorId to the provided value.
    /// </summary>
    [Property(MaxTest = 50)]
    public bool Execute_Always_SetsProcessorId(Guid processorId)
    {
        var aggregate = DPAAggregate.Execute(
            Guid.NewGuid(), processorId, CreateFullyCompliantTerms(), true,
            ["Data storage"], Now, Now.AddYears(1), Now);

        return aggregate.ProcessorId == processorId;
    }

    /// <summary>
    /// Invariant: An executed DPA with a future expiration is always active before expiry.
    /// </summary>
    [Property(MaxTest = 50)]
    public bool Execute_WithExpiration_IsActiveReturnsTrueBeforeExpiry(PositiveInt daysAhead)
    {
        var expiresAt = Now.AddDays(daysAhead.Get);
        var aggregate = CreateExecutedAggregate(expiresAtUtc: expiresAt);

        return aggregate.IsActive(Now);
    }

    /// <summary>
    /// Invariant: An executed DPA with a past expiration is not active after expiry.
    /// </summary>
    [Property(MaxTest = 50)]
    public bool Execute_WithExpiration_IsActiveReturnsFalseAfterExpiry(PositiveInt daysBefore)
    {
        var expiresAt = Now.AddDays(-daysBefore.Get);
        var aggregate = CreateExecutedAggregate(expiresAtUtc: expiresAt);

        return !aggregate.IsActive(Now);
    }

    /// <summary>
    /// Invariant: Terminate always sets Status to Terminated.
    /// </summary>
    [Property(MaxTest = 50)]
    public bool Terminate_Always_SetsTerminatedStatus(NonEmptyString reason)
    {
        if (string.IsNullOrWhiteSpace(reason.Get)) return true;

        var aggregate = CreateExecutedAggregate();
        aggregate.Terminate(reason.Get, Now.AddHours(1));

        return aggregate.Status == DPAStatus.Terminated;
    }

    /// <summary>
    /// Invariant: MarkExpired always sets Status to Expired.
    /// </summary>
    [Property(MaxTest = 50)]
    public bool MarkExpired_Always_SetsExpiredStatus(bool hasSCCs)
    {
        var aggregate = CreateExecutedAggregate(hasSCCs: hasSCCs);
        aggregate.MarkExpired(Now.AddHours(1));

        return aggregate.Status == DPAStatus.Expired;
    }

    /// <summary>
    /// Invariant: When all mandatory terms are true, IsFullyCompliant is true.
    /// </summary>
    [Property(MaxTest = 50)]
    public bool MandatoryTerms_AllTrue_IsFullyCompliant(bool hasSCCs)
    {
        var terms = CreateFullyCompliantTerms();
        var aggregate = CreateExecutedAggregate(terms: terms, hasSCCs: hasSCCs);

        return aggregate.MandatoryTerms.IsFullyCompliant;
    }

    #region Helpers

    private static DPAAggregate CreateExecutedAggregate(
        DPAMandatoryTerms? terms = null,
        bool hasSCCs = true,
        DateTimeOffset? expiresAtUtc = null)
    {
        return DPAAggregate.Execute(
            Guid.NewGuid(),
            Guid.NewGuid(),
            terms ?? CreateFullyCompliantTerms(),
            hasSCCs,
            ["Data storage"],
            Now,
            expiresAtUtc ?? Now.AddYears(1),
            Now);
    }

    private static DPAMandatoryTerms CreateFullyCompliantTerms() => new()
    {
        ProcessOnDocumentedInstructions = true,
        ConfidentialityObligations = true,
        SecurityMeasures = true,
        SubProcessorRequirements = true,
        DataSubjectRightsAssistance = true,
        ComplianceAssistance = true,
        DataDeletionOrReturn = true,
        AuditRights = true
    };

    #endregion
}
