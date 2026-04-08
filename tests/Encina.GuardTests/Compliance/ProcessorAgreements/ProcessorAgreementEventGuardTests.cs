using Encina.Compliance.ProcessorAgreements.Events;
using Encina.Compliance.ProcessorAgreements.Model;

namespace Encina.GuardTests.Compliance.ProcessorAgreements;

/// <summary>
/// Guard tests that exercise event record instantiation for ProcessorAgreements.
/// Creates instances of all DPA and Processor events and verifies properties are preserved.
/// </summary>
public sealed class ProcessorAgreementEventGuardTests
{
    private static readonly DateTimeOffset Now = DateTimeOffset.UtcNow;
    private static readonly Guid DpaId = Guid.NewGuid();
    private static readonly Guid ProcessorId = Guid.NewGuid();
    private static readonly Guid SubProcessorId = Guid.NewGuid();

    private static DPAMandatoryTerms CreateTerms() => new()
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

    #region DPA Events

    [Fact]
    public void DPAExecuted_AllProperties_PreservesValues()
    {
        var terms = CreateTerms();
        var purposes = new List<string> { "Analytics", "Storage" };

        var evt = new DPAExecuted(
            DPAId: DpaId,
            ProcessorId: ProcessorId,
            MandatoryTerms: terms,
            HasSCCs: true,
            ProcessingPurposes: purposes,
            SignedAtUtc: Now.AddDays(-1),
            ExpiresAtUtc: Now.AddYears(1),
            OccurredAtUtc: Now,
            TenantId: "tenant-1",
            ModuleId: "module-1");

        evt.DPAId.ShouldBe(DpaId);
        evt.ProcessorId.ShouldBe(ProcessorId);
        evt.MandatoryTerms.ShouldBe(terms);
        evt.HasSCCs.ShouldBeTrue();
        evt.ProcessingPurposes.ShouldBe(purposes);
        evt.SignedAtUtc.ShouldBe(Now.AddDays(-1));
        evt.ExpiresAtUtc.ShouldBe(Now.AddYears(1));
        evt.OccurredAtUtc.ShouldBe(Now);
        evt.TenantId.ShouldBe("tenant-1");
        evt.ModuleId.ShouldBe("module-1");
    }

    [Fact]
    public void DPAExecuted_NullOptionalFields_PreservesNulls()
    {
        var evt = new DPAExecuted(
            DPAId: DpaId,
            ProcessorId: ProcessorId,
            MandatoryTerms: CreateTerms(),
            HasSCCs: false,
            ProcessingPurposes: [],
            SignedAtUtc: Now,
            ExpiresAtUtc: null,
            OccurredAtUtc: Now,
            TenantId: null,
            ModuleId: null);

        evt.ExpiresAtUtc.ShouldBeNull();
        evt.TenantId.ShouldBeNull();
        evt.ModuleId.ShouldBeNull();
    }

    [Fact]
    public void DPAAmended_AllProperties_PreservesValues()
    {
        var terms = CreateTerms();
        var purposes = new List<string> { "Marketing" };

        var evt = new DPAAmended(
            DPAId: DpaId,
            UpdatedTerms: terms,
            HasSCCs: false,
            ProcessingPurposes: purposes,
            AmendmentReason: "Updated processing purposes",
            OccurredAtUtc: Now);

        evt.DPAId.ShouldBe(DpaId);
        evt.UpdatedTerms.ShouldBe(terms);
        evt.HasSCCs.ShouldBeFalse();
        evt.ProcessingPurposes.ShouldBe(purposes);
        evt.AmendmentReason.ShouldBe("Updated processing purposes");
        evt.OccurredAtUtc.ShouldBe(Now);
    }

    [Fact]
    public void DPAAudited_AllProperties_PreservesValues()
    {
        var evt = new DPAAudited(
            DPAId: DpaId,
            AuditorId: "auditor-42",
            AuditFindings: "All terms present and compliant",
            OccurredAtUtc: Now);

        evt.DPAId.ShouldBe(DpaId);
        evt.AuditorId.ShouldBe("auditor-42");
        evt.AuditFindings.ShouldBe("All terms present and compliant");
        evt.OccurredAtUtc.ShouldBe(Now);
    }

    [Fact]
    public void DPARenewed_AllProperties_PreservesValues()
    {
        var newExpires = Now.AddYears(2);

        var evt = new DPARenewed(
            DPAId: DpaId,
            NewExpiresAtUtc: newExpires,
            OccurredAtUtc: Now);

        evt.DPAId.ShouldBe(DpaId);
        evt.NewExpiresAtUtc.ShouldBe(newExpires);
        evt.OccurredAtUtc.ShouldBe(Now);
    }

    [Fact]
    public void DPATerminated_AllProperties_PreservesValues()
    {
        var evt = new DPATerminated(
            DPAId: DpaId,
            Reason: "Processor relocated outside EEA",
            OccurredAtUtc: Now);

        evt.DPAId.ShouldBe(DpaId);
        evt.Reason.ShouldBe("Processor relocated outside EEA");
        evt.OccurredAtUtc.ShouldBe(Now);
    }

    [Fact]
    public void DPAExpired_AllProperties_PreservesValues()
    {
        var evt = new DPAExpired(
            DPAId: DpaId,
            OccurredAtUtc: Now);

        evt.DPAId.ShouldBe(DpaId);
        evt.OccurredAtUtc.ShouldBe(Now);
    }

    [Fact]
    public void DPAMarkedPendingRenewal_AllProperties_PreservesValues()
    {
        var evt = new DPAMarkedPendingRenewal(
            DPAId: DpaId,
            OccurredAtUtc: Now);

        evt.DPAId.ShouldBe(DpaId);
        evt.OccurredAtUtc.ShouldBe(Now);
    }

    #endregion

    #region Processor Events

    [Fact]
    public void ProcessorRegistered_AllProperties_PreservesValues()
    {
        var evt = new ProcessorRegistered(
            ProcessorId: ProcessorId,
            Name: "Stripe",
            Country: "US",
            ContactEmail: "dpo@stripe.com",
            ParentProcessorId: null,
            Depth: 0,
            AuthorizationType: SubProcessorAuthorizationType.Specific,
            OccurredAtUtc: Now,
            TenantId: "tenant-1",
            ModuleId: "module-1");

        evt.ProcessorId.ShouldBe(ProcessorId);
        evt.Name.ShouldBe("Stripe");
        evt.Country.ShouldBe("US");
        evt.ContactEmail.ShouldBe("dpo@stripe.com");
        evt.ParentProcessorId.ShouldBeNull();
        evt.Depth.ShouldBe(0);
        evt.AuthorizationType.ShouldBe(SubProcessorAuthorizationType.Specific);
        evt.OccurredAtUtc.ShouldBe(Now);
        evt.TenantId.ShouldBe("tenant-1");
        evt.ModuleId.ShouldBe("module-1");
    }

    [Fact]
    public void ProcessorRegistered_SubProcessor_PreservesParentAndDepth()
    {
        var evt = new ProcessorRegistered(
            ProcessorId: SubProcessorId,
            Name: "AWS Lambda",
            Country: "IE",
            ContactEmail: null,
            ParentProcessorId: ProcessorId,
            Depth: 1,
            AuthorizationType: SubProcessorAuthorizationType.General,
            OccurredAtUtc: Now,
            TenantId: null,
            ModuleId: null);

        evt.ParentProcessorId.ShouldBe(ProcessorId);
        evt.Depth.ShouldBe(1);
        evt.AuthorizationType.ShouldBe(SubProcessorAuthorizationType.General);
        evt.ContactEmail.ShouldBeNull();
    }

    [Fact]
    public void ProcessorUpdated_AllProperties_PreservesValues()
    {
        var evt = new ProcessorUpdated(
            ProcessorId: ProcessorId,
            Name: "Stripe Inc.",
            Country: "IE",
            ContactEmail: "privacy@stripe.com",
            AuthorizationType: SubProcessorAuthorizationType.General,
            OccurredAtUtc: Now);

        evt.ProcessorId.ShouldBe(ProcessorId);
        evt.Name.ShouldBe("Stripe Inc.");
        evt.Country.ShouldBe("IE");
        evt.ContactEmail.ShouldBe("privacy@stripe.com");
        evt.AuthorizationType.ShouldBe(SubProcessorAuthorizationType.General);
        evt.OccurredAtUtc.ShouldBe(Now);
    }

    [Fact]
    public void ProcessorRemoved_AllProperties_PreservesValues()
    {
        var evt = new ProcessorRemoved(
            ProcessorId: ProcessorId,
            Reason: "Contract ended",
            OccurredAtUtc: Now);

        evt.ProcessorId.ShouldBe(ProcessorId);
        evt.Reason.ShouldBe("Contract ended");
        evt.OccurredAtUtc.ShouldBe(Now);
    }

    [Fact]
    public void SubProcessorAdded_AllProperties_PreservesValues()
    {
        var evt = new SubProcessorAdded(
            ProcessorId: ProcessorId,
            SubProcessorId: SubProcessorId,
            SubProcessorName: "CloudFlare",
            Depth: 2,
            OccurredAtUtc: Now);

        evt.ProcessorId.ShouldBe(ProcessorId);
        evt.SubProcessorId.ShouldBe(SubProcessorId);
        evt.SubProcessorName.ShouldBe("CloudFlare");
        evt.Depth.ShouldBe(2);
        evt.OccurredAtUtc.ShouldBe(Now);
    }

    [Fact]
    public void SubProcessorRemoved_AllProperties_PreservesValues()
    {
        var evt = new SubProcessorRemoved(
            ProcessorId: ProcessorId,
            SubProcessorId: SubProcessorId,
            Reason: "No longer needed",
            OccurredAtUtc: Now);

        evt.ProcessorId.ShouldBe(ProcessorId);
        evt.SubProcessorId.ShouldBe(SubProcessorId);
        evt.Reason.ShouldBe("No longer needed");
        evt.OccurredAtUtc.ShouldBe(Now);
    }

    #endregion
}
