using Encina.Compliance.ProcessorAgreements.Model;

using FsCheck;
using FsCheck.Xunit;

namespace Encina.PropertyTests.Compliance.ProcessorAgreements;

/// <summary>
/// Property-based tests for <see cref="DataProcessingAgreement"/> verifying domain invariants
/// using FsCheck random data generation.
/// </summary>
public class DataProcessingAgreementPropertyTests
{
    #region Status Invariants

    /// <summary>
    /// Invariant: Status is always a valid DPAStatus enum value.
    /// </summary>
    [Property(MaxTest = 50)]
    public bool Status_AlwaysValidEnum()
    {
        var validStatuses = Enum.GetValues<DPAStatus>();

        return validStatuses.All(status =>
        {
            var dpa = CreateDPA(status: status);
            return Enum.IsDefined(dpa.Status);
        });
    }

    /// <summary>
    /// Invariant: Status preserves the assigned value.
    /// </summary>
    [Property(MaxTest = 50)]
    public bool Status_SetValue_AlwaysPreserved()
    {
        var statuses = Enum.GetValues<DPAStatus>();
        return statuses.All(status =>
        {
            var dpa = CreateDPA(status: status);
            return dpa.Status == status;
        });
    }

    #endregion

    #region MandatoryTerms Invariants

    /// <summary>
    /// Invariant: MandatoryTerms is never null on a valid DPA.
    /// </summary>
    [Property(MaxTest = 50)]
    public bool MandatoryTerms_NeverNull(
        bool a, bool b, bool c, bool d, bool e, bool f, bool g, bool h)
    {
        var terms = new DPAMandatoryTerms
        {
            ProcessOnDocumentedInstructions = a,
            ConfidentialityObligations = b,
            SecurityMeasures = c,
            SubProcessorRequirements = d,
            DataSubjectRightsAssistance = e,
            ComplianceAssistance = f,
            DataDeletionOrReturn = g,
            AuditRights = h
        };

        var dpa = CreateDPA(mandatoryTerms: terms);
        return dpa.MandatoryTerms is not null;
    }

    #endregion

    #region ProcessingPurposes Invariants

    /// <summary>
    /// Invariant: ProcessingPurposes is never null on a valid DPA.
    /// </summary>
    [Property(MaxTest = 50)]
    public bool ProcessingPurposes_NeverNull(NonEmptyString purpose)
    {
        var dpa = CreateDPA(processingPurposes: [purpose.Get]);
        return dpa.ProcessingPurposes is not null;
    }

    /// <summary>
    /// Invariant: ProcessingPurposes preserves all assigned values.
    /// </summary>
    [Property(MaxTest = 50)]
    public bool ProcessingPurposes_PreservesValues(NonEmptyString purpose1, NonEmptyString purpose2)
    {
        var purposes = new List<string> { purpose1.Get, purpose2.Get };
        var dpa = CreateDPA(processingPurposes: purposes);
        return dpa.ProcessingPurposes.Count == purposes.Count
            && dpa.ProcessingPurposes.SequenceEqual(purposes);
    }

    #endregion

    #region IsActive Invariants

    /// <summary>
    /// Invariant: An active DPA with no expiration is always active.
    /// </summary>
    [Property(MaxTest = 50)]
    public bool IsActive_ActiveStatusNoExpiration_AlwaysTrue()
    {
        var dpa = CreateDPA(status: DPAStatus.Active, expiresAtUtc: null);
        return dpa.IsActive(DateTimeOffset.UtcNow);
    }

    /// <summary>
    /// Invariant: An active DPA with future expiration is always active.
    /// </summary>
    [Property(MaxTest = 50)]
    public bool IsActive_ActiveStatusFutureExpiration_AlwaysTrue(PositiveInt daysAhead)
    {
        var now = DateTimeOffset.UtcNow;
        var dpa = CreateDPA(status: DPAStatus.Active, expiresAtUtc: now.AddDays(daysAhead.Get));
        return dpa.IsActive(now);
    }

    /// <summary>
    /// Invariant: An active DPA with past expiration is never active.
    /// </summary>
    [Property(MaxTest = 50)]
    public bool IsActive_ActiveStatusPastExpiration_AlwaysFalse(PositiveInt daysBehind)
    {
        var now = DateTimeOffset.UtcNow;
        var dpa = CreateDPA(status: DPAStatus.Active, expiresAtUtc: now.AddDays(-daysBehind.Get));
        return !dpa.IsActive(now);
    }

    /// <summary>
    /// Invariant: A non-active status DPA is never active regardless of expiration.
    /// </summary>
    [Property(MaxTest = 50)]
    public bool IsActive_NonActiveStatus_AlwaysFalse(PositiveInt daysAhead)
    {
        var now = DateTimeOffset.UtcNow;
        var nonActiveStatuses = new[]
        {
            DPAStatus.Expired,
            DPAStatus.PendingRenewal,
            DPAStatus.Terminated
        };

        return nonActiveStatuses.All(status =>
        {
            var dpa = CreateDPA(status: status, expiresAtUtc: now.AddDays(daysAhead.Get));
            return !dpa.IsActive(now);
        });
    }

    #endregion

    #region Record Equality Invariants

    /// <summary>
    /// Invariant: 'with' expression creates a new instance preserving other properties.
    /// </summary>
    [Property(MaxTest = 50)]
    public bool WithExpression_PreservesOtherProperties()
    {
        var original = CreateDPA(status: DPAStatus.Active);
        var modified = original with { Status = DPAStatus.Terminated };

        return modified.Id == original.Id
            && modified.ProcessorId == original.ProcessorId
            && modified.MandatoryTerms == original.MandatoryTerms
            && modified.SignedAtUtc == original.SignedAtUtc
            && modified.Status == DPAStatus.Terminated;
    }

    #endregion

    #region Helpers

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

    private static DataProcessingAgreement CreateDPA(
        DPAStatus status = DPAStatus.Active,
        DateTimeOffset? expiresAtUtc = null,
        DPAMandatoryTerms? mandatoryTerms = null,
        IReadOnlyList<string>? processingPurposes = null)
    {
        var now = DateTimeOffset.UtcNow;
        return new DataProcessingAgreement
        {
            Id = Guid.NewGuid().ToString(),
            ProcessorId = Guid.NewGuid().ToString(),
            Status = status,
            SignedAtUtc = now.AddDays(-30),
            ExpiresAtUtc = expiresAtUtc,
            MandatoryTerms = mandatoryTerms ?? CreateFullyCompliantTerms(),
            HasSCCs = false,
            ProcessingPurposes = processingPurposes ?? ["Payment processing"],
            CreatedAtUtc = now,
            LastUpdatedAtUtc = now
        };
    }

    #endregion
}
