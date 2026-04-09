#pragma warning disable CA2012

using Encina.Compliance.CrossBorderTransfer.Model;
using Encina.Compliance.CrossBorderTransfer.ReadModels;

namespace Encina.GuardTests.Compliance.CrossBorderTransfer;

/// <summary>
/// Guard tests for cross-border transfer model types verifying correct instantiation
/// via factory methods and required property initialization.
/// </summary>
public class CrossBorderTransferModelGuardTests
{
    #region TransferValidationOutcome.Allow

    [Fact]
    public void TransferValidationOutcome_Allow_ReturnsAllowedOutcome()
    {
        var outcome = TransferValidationOutcome.Allow(TransferBasis.AdequacyDecision);

        outcome.IsAllowed.ShouldBeTrue();
        outcome.Basis.ShouldBe(TransferBasis.AdequacyDecision);
        outcome.SupplementaryMeasuresRequired.ShouldBeEmpty();
        outcome.TIARequired.ShouldBeFalse();
        outcome.BlockReason.ShouldBeNull();
        outcome.Warnings.ShouldBeEmpty();
    }

    [Fact]
    public void TransferValidationOutcome_Allow_WithAllParameters_SetsCorrectly()
    {
        var measures = new List<string> { "Encryption" };
        var warnings = new List<string> { "SCC expiring soon" };

        var outcome = TransferValidationOutcome.Allow(
            TransferBasis.SCCs,
            supplementaryMeasuresRequired: measures,
            tiaRequired: true,
            sccModuleRequired: SCCModule.ControllerToProcessor,
            warnings: warnings);

        outcome.IsAllowed.ShouldBeTrue();
        outcome.Basis.ShouldBe(TransferBasis.SCCs);
        outcome.SupplementaryMeasuresRequired.Count.ShouldBe(1);
        outcome.TIARequired.ShouldBeTrue();
        outcome.SCCModuleRequired.ShouldBe(SCCModule.ControllerToProcessor);
        outcome.Warnings.Count.ShouldBe(1);
    }

    #endregion

    #region TransferValidationOutcome.Block

    [Fact]
    public void TransferValidationOutcome_Block_ReturnsBlockedOutcome()
    {
        var outcome = TransferValidationOutcome.Block("No valid mechanism");

        outcome.IsAllowed.ShouldBeFalse();
        outcome.Basis.ShouldBe(TransferBasis.Blocked);
        outcome.BlockReason.ShouldBe("No valid mechanism");
        outcome.SupplementaryMeasuresRequired.ShouldBeEmpty();
        outcome.TIARequired.ShouldBeFalse();
        outcome.Warnings.ShouldBeEmpty();
    }

    [Fact]
    public void TransferValidationOutcome_Block_WithWarnings_SetsWarnings()
    {
        var warnings = new List<string> { "Previous SCC expired" };
        var outcome = TransferValidationOutcome.Block("Blocked", warnings);

        outcome.Warnings.Count.ShouldBe(1);
    }

    #endregion

    #region TransferRequest

    [Fact]
    public void TransferRequest_RequiredProperties_SetCorrectly()
    {
        var request = new TransferRequest
        {
            SourceCountryCode = "DE",
            DestinationCountryCode = "US",
            DataCategory = "personal-data"
        };

        request.SourceCountryCode.ShouldBe("DE");
        request.DestinationCountryCode.ShouldBe("US");
        request.DataCategory.ShouldBe("personal-data");
        request.ProcessorId.ShouldBeNull();
        request.TenantId.ShouldBeNull();
        request.ModuleId.ShouldBeNull();
    }

    [Fact]
    public void TransferRequest_WithOptionalProperties_SetsCorrectly()
    {
        var request = new TransferRequest
        {
            SourceCountryCode = "DE",
            DestinationCountryCode = "US",
            DataCategory = "health-data",
            ProcessorId = "proc-1",
            TenantId = "tenant-1",
            ModuleId = "module-1"
        };

        request.ProcessorId.ShouldBe("proc-1");
        request.TenantId.ShouldBe("tenant-1");
        request.ModuleId.ShouldBe("module-1");
    }

    #endregion

    #region TIARiskAssessment

    [Fact]
    public void TIARiskAssessment_ConstructsCorrectly()
    {
        var factors = new List<string> { "No adequacy decision" };
        var recommendations = new List<string> { "Use SCCs" };

        var assessment = new TIARiskAssessment(0.6, factors, recommendations);

        assessment.Score.ShouldBe(0.6);
        assessment.Factors.Count.ShouldBe(1);
        assessment.Recommendations.Count.ShouldBe(1);
    }

    [Fact]
    public void TIARiskAssessment_ZeroScore_ConstructsCorrectly()
    {
        var assessment = new TIARiskAssessment(0.0, [], []);

        assessment.Score.ShouldBe(0.0);
        assessment.Factors.ShouldBeEmpty();
        assessment.Recommendations.ShouldBeEmpty();
    }

    #endregion

    #region SCCValidationResult

    [Fact]
    public void SCCValidationResult_ValidResult_SetsCorrectly()
    {
        var result = new SCCValidationResult
        {
            IsValid = true,
            AgreementId = Guid.NewGuid(),
            Module = SCCModule.ControllerToProcessor,
            Version = "2021/914",
            MissingMeasures = [],
            Issues = []
        };

        result.IsValid.ShouldBeTrue();
        result.AgreementId.ShouldNotBeNull();
        result.Module.ShouldBe(SCCModule.ControllerToProcessor);
        result.Version.ShouldBe("2021/914");
        result.MissingMeasures.ShouldBeEmpty();
        result.Issues.ShouldBeEmpty();
    }

    [Fact]
    public void SCCValidationResult_InvalidResult_SetsCorrectly()
    {
        var result = new SCCValidationResult
        {
            IsValid = false,
            MissingMeasures = ["Encryption"],
            Issues = ["Agreement expired"]
        };

        result.IsValid.ShouldBeFalse();
        result.AgreementId.ShouldBeNull();
        result.MissingMeasures.Count.ShouldBe(1);
        result.Issues.Count.ShouldBe(1);
    }

    #endregion

    #region SupplementaryMeasure

    [Fact]
    public void SupplementaryMeasure_RequiredProperties_SetCorrectly()
    {
        var measureId = Guid.NewGuid();
        var measure = new SupplementaryMeasure
        {
            Id = measureId,
            Type = SupplementaryMeasureType.Technical,
            Description = "End-to-end encryption",
            IsImplemented = false
        };

        measure.Id.ShouldBe(measureId);
        measure.Type.ShouldBe(SupplementaryMeasureType.Technical);
        measure.Description.ShouldBe("End-to-end encryption");
        measure.IsImplemented.ShouldBeFalse();
        measure.ImplementedAtUtc.ShouldBeNull();
    }

    [Fact]
    public void SupplementaryMeasure_Implemented_SetsTimestamp()
    {
        var now = DateTimeOffset.UtcNow;
        var measure = new SupplementaryMeasure
        {
            Id = Guid.NewGuid(),
            Type = SupplementaryMeasureType.Contractual,
            Description = "Audit rights clause",
            IsImplemented = true,
            ImplementedAtUtc = now
        };

        measure.IsImplemented.ShouldBeTrue();
        measure.ImplementedAtUtc.ShouldBe(now);
    }

    #endregion

    #region TIAReadModel

    [Fact]
    public void TIAReadModel_RequiredProperties_SetCorrectly()
    {
        var id = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;

        var readModel = new TIAReadModel
        {
            Id = id,
            SourceCountryCode = "DE",
            DestinationCountryCode = "US",
            DataCategory = "personal-data",
            Status = TIAStatus.Draft,
            RequiredSupplementaryMeasures = [],
            CreatedAtUtc = now,
            LastModifiedAtUtc = now
        };

        readModel.Id.ShouldBe(id);
        readModel.SourceCountryCode.ShouldBe("DE");
        readModel.DestinationCountryCode.ShouldBe("US");
        readModel.DataCategory.ShouldBe("personal-data");
        readModel.Status.ShouldBe(TIAStatus.Draft);
        readModel.RiskScore.ShouldBeNull();
        readModel.Findings.ShouldBeNull();
        readModel.AssessorId.ShouldBeNull();
    }

    #endregion

    #region SCCAgreementReadModel

    [Fact]
    public void SCCAgreementReadModel_RequiredProperties_SetCorrectly()
    {
        var readModel = new SCCAgreementReadModel
        {
            Id = Guid.NewGuid(),
            ProcessorId = "proc-1",
            Module = SCCModule.ControllerToProcessor,
            Version = "2021/914",
            ExecutedAtUtc = DateTimeOffset.UtcNow,
            IsRevoked = false,
            SupplementaryMeasures = []
        };

        readModel.ProcessorId.ShouldBe("proc-1");
        readModel.Module.ShouldBe(SCCModule.ControllerToProcessor);
        readModel.IsRevoked.ShouldBeFalse();
    }

    [Fact]
    public void SCCAgreementReadModel_IsValid_NotRevoked_ReturnsTrue()
    {
        var readModel = new SCCAgreementReadModel
        {
            Id = Guid.NewGuid(),
            ProcessorId = "proc-1",
            Module = SCCModule.ControllerToProcessor,
            Version = "2021/914",
            ExecutedAtUtc = DateTimeOffset.UtcNow,
            IsRevoked = false,
            SupplementaryMeasures = []
        };

        readModel.IsValid(DateTimeOffset.UtcNow).ShouldBeTrue();
    }

    [Fact]
    public void SCCAgreementReadModel_IsValid_Revoked_ReturnsFalse()
    {
        var readModel = new SCCAgreementReadModel
        {
            Id = Guid.NewGuid(),
            ProcessorId = "proc-1",
            Module = SCCModule.ControllerToProcessor,
            Version = "2021/914",
            ExecutedAtUtc = DateTimeOffset.UtcNow,
            IsRevoked = true,
            SupplementaryMeasures = []
        };

        readModel.IsValid(DateTimeOffset.UtcNow).ShouldBeFalse();
    }

    [Fact]
    public void SCCAgreementReadModel_IsValid_Expired_ReturnsFalse()
    {
        var readModel = new SCCAgreementReadModel
        {
            Id = Guid.NewGuid(),
            ProcessorId = "proc-1",
            Module = SCCModule.ControllerToProcessor,
            Version = "2021/914",
            ExecutedAtUtc = DateTimeOffset.UtcNow.AddDays(-60),
            ExpiresAtUtc = DateTimeOffset.UtcNow.AddDays(-1),
            IsRevoked = false,
            SupplementaryMeasures = []
        };

        readModel.IsValid(DateTimeOffset.UtcNow).ShouldBeFalse();
    }

    #endregion

    #region ApprovedTransferReadModel

    [Fact]
    public void ApprovedTransferReadModel_RequiredProperties_SetCorrectly()
    {
        var readModel = new ApprovedTransferReadModel
        {
            Id = Guid.NewGuid(),
            SourceCountryCode = "DE",
            DestinationCountryCode = "US",
            DataCategory = "personal-data",
            Basis = TransferBasis.SCCs,
            ApprovedBy = "admin1",
            IsRevoked = false
        };

        readModel.SourceCountryCode.ShouldBe("DE");
        readModel.Basis.ShouldBe(TransferBasis.SCCs);
        readModel.IsRevoked.ShouldBeFalse();
    }

    [Fact]
    public void ApprovedTransferReadModel_IsValid_Active_ReturnsTrue()
    {
        var readModel = new ApprovedTransferReadModel
        {
            Id = Guid.NewGuid(),
            SourceCountryCode = "DE",
            DestinationCountryCode = "US",
            DataCategory = "personal-data",
            Basis = TransferBasis.SCCs,
            ApprovedBy = "admin1",
            IsRevoked = false
        };

        readModel.IsValid(DateTimeOffset.UtcNow).ShouldBeTrue();
    }

    [Fact]
    public void ApprovedTransferReadModel_IsValid_Revoked_ReturnsFalse()
    {
        var readModel = new ApprovedTransferReadModel
        {
            Id = Guid.NewGuid(),
            SourceCountryCode = "DE",
            DestinationCountryCode = "US",
            DataCategory = "personal-data",
            Basis = TransferBasis.SCCs,
            ApprovedBy = "admin1",
            IsRevoked = true
        };

        readModel.IsValid(DateTimeOffset.UtcNow).ShouldBeFalse();
    }

    [Fact]
    public void ApprovedTransferReadModel_IsValid_PastExpiration_ReturnsFalse()
    {
        var readModel = new ApprovedTransferReadModel
        {
            Id = Guid.NewGuid(),
            SourceCountryCode = "DE",
            DestinationCountryCode = "US",
            DataCategory = "personal-data",
            Basis = TransferBasis.SCCs,
            ApprovedBy = "admin1",
            IsRevoked = false,
            ExpiresAtUtc = DateTimeOffset.UtcNow.AddDays(-1)
        };

        readModel.IsValid(DateTimeOffset.UtcNow).ShouldBeFalse();
    }

    #endregion
}
