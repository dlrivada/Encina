#pragma warning disable CA2012 // Use ValueTasks correctly

using Encina.Compliance.ProcessorAgreements.Model;

using Shouldly;

namespace Encina.UnitTests.Compliance.ProcessorAgreements;

/// <summary>
/// Unit tests for <see cref="DataProcessingAgreement"/>.
/// </summary>
public class DataProcessingAgreementTests
{
    private static readonly DateTimeOffset Now = new(2026, 3, 13, 12, 0, 0, TimeSpan.Zero);

    #region Required Properties

    [Fact]
    public void DataProcessingAgreement_ShouldHaveAllRequiredProperties()
    {
        var terms = CreateAllTrueTerms();
        var dpa = new DataProcessingAgreement
        {
            Id = "dpa-001",
            ProcessorId = "proc-001",
            Status = DPAStatus.Active,
            SignedAtUtc = Now.AddDays(-30),
            MandatoryTerms = terms,
            HasSCCs = true,
            ProcessingPurposes = ["Payment processing", "Analytics"],
            CreatedAtUtc = Now.AddDays(-30),
            LastUpdatedAtUtc = Now
        };

        dpa.Id.ShouldBe("dpa-001");
        dpa.ProcessorId.ShouldBe("proc-001");
        dpa.Status.ShouldBe(DPAStatus.Active);
        dpa.SignedAtUtc.ShouldBe(Now.AddDays(-30));
        dpa.MandatoryTerms.ShouldBe(terms);
        dpa.HasSCCs.ShouldBeTrue();
        dpa.ProcessingPurposes.Count.ShouldBe(2);
        dpa.CreatedAtUtc.ShouldBe(Now.AddDays(-30));
        dpa.LastUpdatedAtUtc.ShouldBe(Now);
    }

    #endregion

    #region Optional Properties

    [Fact]
    public void DataProcessingAgreement_OptionalProperties_ShouldDefaultToNull()
    {
        var dpa = CreateActiveDPA();

        dpa.ExpiresAtUtc.ShouldBeNull();
        dpa.TenantId.ShouldBeNull();
        dpa.ModuleId.ShouldBeNull();
    }

    [Fact]
    public void DataProcessingAgreement_OptionalProperties_ShouldAcceptValues()
    {
        var dpa = CreateActiveDPA() with
        {
            ExpiresAtUtc = Now.AddYears(1),
            TenantId = "tenant-xyz",
            ModuleId = "module-hr"
        };

        dpa.ExpiresAtUtc.ShouldBe(Now.AddYears(1));
        dpa.TenantId.ShouldBe("tenant-xyz");
        dpa.ModuleId.ShouldBe("module-hr");
    }

    #endregion

    #region DPAStatus Enum

    [Fact]
    public void DPAStatus_ShouldHaveExpectedValues()
    {
        ((int)DPAStatus.Active).ShouldBe(0);
        ((int)DPAStatus.Expired).ShouldBe(1);
        ((int)DPAStatus.PendingRenewal).ShouldBe(2);
        ((int)DPAStatus.Terminated).ShouldBe(3);
    }

    #endregion

    #region IsActive Method

    [Fact]
    public void IsActive_ActiveStatusAndNotExpired_ShouldReturnTrue()
    {
        var dpa = CreateActiveDPA() with { ExpiresAtUtc = Now.AddDays(30) };

        dpa.IsActive(Now).ShouldBeTrue();
    }

    [Fact]
    public void IsActive_ActiveStatusAndNoExpiration_ShouldReturnTrue()
    {
        var dpa = CreateActiveDPA();

        dpa.ExpiresAtUtc.ShouldBeNull();
        dpa.IsActive(Now).ShouldBeTrue();
    }

    [Fact]
    public void IsActive_ActiveStatusButExpired_ShouldReturnFalse()
    {
        var dpa = CreateActiveDPA() with { ExpiresAtUtc = Now.AddDays(-1) };

        dpa.IsActive(Now).ShouldBeFalse();
    }

    [Fact]
    public void IsActive_ExpiredStatus_ShouldReturnFalse()
    {
        var dpa = CreateActiveDPA() with { Status = DPAStatus.Expired };

        dpa.IsActive(Now).ShouldBeFalse();
    }

    [Fact]
    public void IsActive_TerminatedStatus_ShouldReturnFalse()
    {
        var dpa = CreateActiveDPA() with { Status = DPAStatus.Terminated };

        dpa.IsActive(Now).ShouldBeFalse();
    }

    [Fact]
    public void IsActive_PendingRenewalStatus_ShouldReturnFalse()
    {
        var dpa = CreateActiveDPA() with { Status = DPAStatus.PendingRenewal };

        dpa.IsActive(Now).ShouldBeFalse();
    }

    #endregion

    #region Helpers

    private static DPAMandatoryTerms CreateAllTrueTerms() => new()
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

    private static DataProcessingAgreement CreateActiveDPA() => new()
    {
        Id = "dpa-001",
        ProcessorId = "proc-001",
        Status = DPAStatus.Active,
        SignedAtUtc = Now.AddDays(-30),
        MandatoryTerms = CreateAllTrueTerms(),
        HasSCCs = false,
        ProcessingPurposes = ["Data hosting"],
        CreatedAtUtc = Now.AddDays(-30),
        LastUpdatedAtUtc = Now
    };

    #endregion
}
