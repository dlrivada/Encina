using Encina.Compliance.ProcessorAgreements.Model;

namespace Encina.GuardTests.Compliance.ProcessorAgreements;

/// <summary>
/// Guard tests that exercise model record instantiation for ProcessorAgreements.
/// Creates instances of all model types and verifies required properties are preserved,
/// covering executable lines in record constructors and computed properties.
/// </summary>
public sealed class ProcessorAgreementModelFactoryGuardTests
{
    private static readonly DateTimeOffset Now = DateTimeOffset.UtcNow;

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

    private static DPAMandatoryTerms CreatePartiallyCompliantTerms() => new()
    {
        ProcessOnDocumentedInstructions = true,
        ConfidentialityObligations = false,
        SecurityMeasures = true,
        SubProcessorRequirements = false,
        DataSubjectRightsAssistance = true,
        ComplianceAssistance = false,
        DataDeletionOrReturn = true,
        AuditRights = false
    };

    #region DataProcessingAgreement

    [Fact]
    public void DataProcessingAgreement_AllRequiredProperties_PreservesValues()
    {
        var terms = CreateFullyCompliantTerms();
        var purposes = new List<string> { "Analytics", "Marketing" };

        var dpa = new DataProcessingAgreement
        {
            Id = "dpa-001",
            ProcessorId = "proc-001",
            Status = DPAStatus.Active,
            SignedAtUtc = Now.AddDays(-30),
            ExpiresAtUtc = Now.AddDays(335),
            MandatoryTerms = terms,
            HasSCCs = true,
            ProcessingPurposes = purposes,
            TenantId = "tenant-1",
            ModuleId = "module-1",
            CreatedAtUtc = Now.AddDays(-30),
            LastUpdatedAtUtc = Now
        };

        dpa.Id.ShouldBe("dpa-001");
        dpa.ProcessorId.ShouldBe("proc-001");
        dpa.Status.ShouldBe(DPAStatus.Active);
        dpa.SignedAtUtc.ShouldBe(Now.AddDays(-30));
        dpa.ExpiresAtUtc.ShouldBe(Now.AddDays(335));
        dpa.MandatoryTerms.ShouldBe(terms);
        dpa.HasSCCs.ShouldBeTrue();
        dpa.ProcessingPurposes.ShouldBe(purposes);
        dpa.TenantId.ShouldBe("tenant-1");
        dpa.ModuleId.ShouldBe("module-1");
        dpa.CreatedAtUtc.ShouldBe(Now.AddDays(-30));
        dpa.LastUpdatedAtUtc.ShouldBe(Now);
    }

    [Fact]
    public void DataProcessingAgreement_IsActive_ActiveAndNotExpired_ReturnsTrue()
    {
        var dpa = new DataProcessingAgreement
        {
            Id = "dpa-002",
            ProcessorId = "proc-002",
            Status = DPAStatus.Active,
            SignedAtUtc = Now.AddDays(-10),
            ExpiresAtUtc = Now.AddDays(100),
            MandatoryTerms = CreateFullyCompliantTerms(),
            HasSCCs = false,
            ProcessingPurposes = ["Processing"],
            CreatedAtUtc = Now.AddDays(-10),
            LastUpdatedAtUtc = Now
        };

        dpa.IsActive(Now).ShouldBeTrue();
    }

    [Fact]
    public void DataProcessingAgreement_IsActive_ExpiredDate_ReturnsFalse()
    {
        var dpa = new DataProcessingAgreement
        {
            Id = "dpa-003",
            ProcessorId = "proc-003",
            Status = DPAStatus.Active,
            SignedAtUtc = Now.AddDays(-100),
            ExpiresAtUtc = Now.AddDays(-1),
            MandatoryTerms = CreateFullyCompliantTerms(),
            HasSCCs = false,
            ProcessingPurposes = ["Processing"],
            CreatedAtUtc = Now.AddDays(-100),
            LastUpdatedAtUtc = Now
        };

        dpa.IsActive(Now).ShouldBeFalse();
    }

    [Fact]
    public void DataProcessingAgreement_IsActive_NullExpiresAndActive_ReturnsTrue()
    {
        var dpa = new DataProcessingAgreement
        {
            Id = "dpa-004",
            ProcessorId = "proc-004",
            Status = DPAStatus.Active,
            SignedAtUtc = Now.AddDays(-5),
            ExpiresAtUtc = null,
            MandatoryTerms = CreateFullyCompliantTerms(),
            HasSCCs = false,
            ProcessingPurposes = ["Storage"],
            CreatedAtUtc = Now.AddDays(-5),
            LastUpdatedAtUtc = Now
        };

        dpa.IsActive(Now).ShouldBeTrue();
    }

    [Fact]
    public void DataProcessingAgreement_IsActive_TerminatedStatus_ReturnsFalse()
    {
        var dpa = new DataProcessingAgreement
        {
            Id = "dpa-005",
            ProcessorId = "proc-005",
            Status = DPAStatus.Terminated,
            SignedAtUtc = Now.AddDays(-60),
            ExpiresAtUtc = Now.AddDays(300),
            MandatoryTerms = CreateFullyCompliantTerms(),
            HasSCCs = true,
            ProcessingPurposes = ["Analytics"],
            CreatedAtUtc = Now.AddDays(-60),
            LastUpdatedAtUtc = Now
        };

        dpa.IsActive(Now).ShouldBeFalse();
    }

    #endregion

    #region Processor

    [Fact]
    public void Processor_AllRequiredProperties_PreservesValues()
    {
        var processor = new Processor
        {
            Id = "proc-100",
            Name = "Stripe",
            Country = "US",
            ContactEmail = "dpo@stripe.com",
            ParentProcessorId = null,
            Depth = 0,
            SubProcessorAuthorizationType = SubProcessorAuthorizationType.Specific,
            TenantId = "tenant-2",
            ModuleId = "module-2",
            CreatedAtUtc = Now.AddDays(-90),
            LastUpdatedAtUtc = Now.AddDays(-1)
        };

        processor.Id.ShouldBe("proc-100");
        processor.Name.ShouldBe("Stripe");
        processor.Country.ShouldBe("US");
        processor.ContactEmail.ShouldBe("dpo@stripe.com");
        processor.ParentProcessorId.ShouldBeNull();
        processor.Depth.ShouldBe(0);
        processor.SubProcessorAuthorizationType.ShouldBe(SubProcessorAuthorizationType.Specific);
        processor.TenantId.ShouldBe("tenant-2");
        processor.ModuleId.ShouldBe("module-2");
        processor.CreatedAtUtc.ShouldBe(Now.AddDays(-90));
        processor.LastUpdatedAtUtc.ShouldBe(Now.AddDays(-1));
    }

    [Fact]
    public void Processor_SubProcessor_PreservesParentAndDepth()
    {
        var subProcessor = new Processor
        {
            Id = "sub-001",
            Name = "AWS Lambda",
            Country = "IE",
            ContactEmail = null,
            ParentProcessorId = "proc-100",
            Depth = 1,
            SubProcessorAuthorizationType = SubProcessorAuthorizationType.General,
            CreatedAtUtc = Now,
            LastUpdatedAtUtc = Now
        };

        subProcessor.ParentProcessorId.ShouldBe("proc-100");
        subProcessor.Depth.ShouldBe(1);
        subProcessor.SubProcessorAuthorizationType.ShouldBe(SubProcessorAuthorizationType.General);
        subProcessor.ContactEmail.ShouldBeNull();
    }

    #endregion

    #region DPAValidationResult

    [Fact]
    public void DPAValidationResult_ValidResult_PreservesAllProperties()
    {
        var result = new DPAValidationResult
        {
            ProcessorId = "proc-200",
            DPAId = "dpa-200",
            IsValid = true,
            Status = DPAStatus.Active,
            MissingTerms = [],
            Warnings = [],
            DaysUntilExpiration = 120,
            ValidatedAtUtc = Now
        };

        result.ProcessorId.ShouldBe("proc-200");
        result.DPAId.ShouldBe("dpa-200");
        result.IsValid.ShouldBeTrue();
        result.Status.ShouldBe(DPAStatus.Active);
        result.MissingTerms.ShouldBeEmpty();
        result.Warnings.ShouldBeEmpty();
        result.DaysUntilExpiration.ShouldBe(120);
        result.ValidatedAtUtc.ShouldBe(Now);
    }

    [Fact]
    public void DPAValidationResult_InvalidWithMissingTerms_PreservesWarnings()
    {
        var missingTerms = new List<string> { "SecurityMeasures", "AuditRights" };
        var warnings = new List<string> { "DPA expires in 10 days" };

        var result = new DPAValidationResult
        {
            ProcessorId = "proc-201",
            DPAId = null,
            IsValid = false,
            Status = null,
            MissingTerms = missingTerms,
            Warnings = warnings,
            DaysUntilExpiration = null,
            ValidatedAtUtc = Now
        };

        result.IsValid.ShouldBeFalse();
        result.DPAId.ShouldBeNull();
        result.Status.ShouldBeNull();
        result.MissingTerms.Count.ShouldBe(2);
        result.Warnings.Count.ShouldBe(1);
        result.DaysUntilExpiration.ShouldBeNull();
    }

    #endregion

    #region ProcessorAgreementAuditEntry

    [Fact]
    public void ProcessorAgreementAuditEntry_AllProperties_PreservesValues()
    {
        var entry = new ProcessorAgreementAuditEntry
        {
            Id = "audit-001",
            ProcessorId = "proc-300",
            DPAId = "dpa-300",
            Action = "DPASigned",
            Detail = "Agreement signed with full terms",
            PerformedByUserId = "user-42",
            OccurredAtUtc = Now,
            TenantId = "tenant-5",
            ModuleId = "module-5"
        };

        entry.Id.ShouldBe("audit-001");
        entry.ProcessorId.ShouldBe("proc-300");
        entry.DPAId.ShouldBe("dpa-300");
        entry.Action.ShouldBe("DPASigned");
        entry.Detail.ShouldBe("Agreement signed with full terms");
        entry.PerformedByUserId.ShouldBe("user-42");
        entry.OccurredAtUtc.ShouldBe(Now);
        entry.TenantId.ShouldBe("tenant-5");
        entry.ModuleId.ShouldBe("module-5");
    }

    [Fact]
    public void ProcessorAgreementAuditEntry_SystemAction_NullableFieldsAreNull()
    {
        var entry = new ProcessorAgreementAuditEntry
        {
            Id = "audit-002",
            ProcessorId = "proc-301",
            DPAId = null,
            Action = "Registered",
            Detail = null,
            PerformedByUserId = null,
            OccurredAtUtc = Now,
            TenantId = null,
            ModuleId = null
        };

        entry.DPAId.ShouldBeNull();
        entry.Detail.ShouldBeNull();
        entry.PerformedByUserId.ShouldBeNull();
        entry.TenantId.ShouldBeNull();
        entry.ModuleId.ShouldBeNull();
    }

    #endregion

    #region DPAMandatoryTerms

    [Fact]
    public void DPAMandatoryTerms_FullyCompliant_IsFullyCompliantReturnsTrue()
    {
        var terms = CreateFullyCompliantTerms();

        terms.IsFullyCompliant.ShouldBeTrue();
        terms.MissingTerms.ShouldBeEmpty();
    }

    [Fact]
    public void DPAMandatoryTerms_PartiallyCompliant_ReturnsMissingTermNames()
    {
        var terms = CreatePartiallyCompliantTerms();

        terms.IsFullyCompliant.ShouldBeFalse();

        var missing = terms.MissingTerms;
        missing.ShouldContain(nameof(DPAMandatoryTerms.ConfidentialityObligations));
        missing.ShouldContain(nameof(DPAMandatoryTerms.SubProcessorRequirements));
        missing.ShouldContain(nameof(DPAMandatoryTerms.ComplianceAssistance));
        missing.ShouldContain(nameof(DPAMandatoryTerms.AuditRights));
        missing.Count.ShouldBe(4);
    }

    [Fact]
    public void DPAMandatoryTerms_AllFalse_ReturnsAllEightMissing()
    {
        var terms = new DPAMandatoryTerms
        {
            ProcessOnDocumentedInstructions = false,
            ConfidentialityObligations = false,
            SecurityMeasures = false,
            SubProcessorRequirements = false,
            DataSubjectRightsAssistance = false,
            ComplianceAssistance = false,
            DataDeletionOrReturn = false,
            AuditRights = false
        };

        terms.IsFullyCompliant.ShouldBeFalse();
        terms.MissingTerms.Count.ShouldBe(8);
        terms.MissingTerms.ShouldContain(nameof(DPAMandatoryTerms.ProcessOnDocumentedInstructions));
        terms.MissingTerms.ShouldContain(nameof(DPAMandatoryTerms.DataDeletionOrReturn));
    }

    #endregion
}
