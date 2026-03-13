#pragma warning disable CA2012

using Encina.Compliance.ProcessorAgreements;
using Encina.Compliance.ProcessorAgreements.Model;

using FluentAssertions;

namespace Encina.UnitTests.Compliance.ProcessorAgreements;

/// <summary>
/// Unit tests for <see cref="DataProcessingAgreementMapper"/> static mapping methods.
/// </summary>
public class DataProcessingAgreementMapperTests
{
    private static readonly string[] ExpectedProcessingPurposes = ["Data hosting", "Backup services"];

    #region ToEntity Tests

    [Fact]
    public void ToEntity_ValidAgreement_MapsAllProperties()
    {
        // Arrange
        var agreement = CreateAgreement();

        // Act
        var entity = DataProcessingAgreementMapper.ToEntity(agreement);

        // Assert
        entity.Id.Should().Be(agreement.Id);
        entity.ProcessorId.Should().Be(agreement.ProcessorId);
        entity.StatusValue.Should().Be((int)agreement.Status);
        entity.SignedAtUtc.Should().Be(agreement.SignedAtUtc);
        entity.ExpiresAtUtc.Should().Be(agreement.ExpiresAtUtc);
        entity.HasSCCs.Should().Be(agreement.HasSCCs);
        entity.ProcessingPurposesJson.Should().Contain("Payment processing");
        entity.ProcessingPurposesJson.Should().Contain("Fraud detection");
        entity.ProcessOnDocumentedInstructions.Should().Be(agreement.MandatoryTerms.ProcessOnDocumentedInstructions);
        entity.ConfidentialityObligations.Should().Be(agreement.MandatoryTerms.ConfidentialityObligations);
        entity.SecurityMeasures.Should().Be(agreement.MandatoryTerms.SecurityMeasures);
        entity.SubProcessorRequirements.Should().Be(agreement.MandatoryTerms.SubProcessorRequirements);
        entity.DataSubjectRightsAssistance.Should().Be(agreement.MandatoryTerms.DataSubjectRightsAssistance);
        entity.ComplianceAssistance.Should().Be(agreement.MandatoryTerms.ComplianceAssistance);
        entity.DataDeletionOrReturn.Should().Be(agreement.MandatoryTerms.DataDeletionOrReturn);
        entity.AuditRights.Should().Be(agreement.MandatoryTerms.AuditRights);
        entity.TenantId.Should().Be(agreement.TenantId);
        entity.ModuleId.Should().Be(agreement.ModuleId);
        entity.CreatedAtUtc.Should().Be(agreement.CreatedAtUtc);
        entity.LastUpdatedAtUtc.Should().Be(agreement.LastUpdatedAtUtc);
    }

    [Fact]
    public void ToEntity_NullAgreement_ThrowsArgumentNullException()
    {
        // Act
        var act = () => DataProcessingAgreementMapper.ToEntity(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    #endregion

    #region ToDomain Tests

    [Fact]
    public void ToDomain_ValidEntity_MapsAllProperties()
    {
        // Arrange
        var entity = CreateEntity();

        // Act
        var agreement = DataProcessingAgreementMapper.ToDomain(entity);

        // Assert
        agreement.Should().NotBeNull();
        agreement!.Id.Should().Be(entity.Id);
        agreement.ProcessorId.Should().Be(entity.ProcessorId);
        agreement.Status.Should().Be((DPAStatus)entity.StatusValue);
        agreement.SignedAtUtc.Should().Be(entity.SignedAtUtc);
        agreement.ExpiresAtUtc.Should().Be(entity.ExpiresAtUtc);
        agreement.HasSCCs.Should().Be(entity.HasSCCs);
        agreement.ProcessingPurposes.Should().BeEquivalentTo(ExpectedProcessingPurposes);
        agreement.MandatoryTerms.ProcessOnDocumentedInstructions.Should().Be(entity.ProcessOnDocumentedInstructions);
        agreement.MandatoryTerms.ConfidentialityObligations.Should().Be(entity.ConfidentialityObligations);
        agreement.MandatoryTerms.SecurityMeasures.Should().Be(entity.SecurityMeasures);
        agreement.MandatoryTerms.SubProcessorRequirements.Should().Be(entity.SubProcessorRequirements);
        agreement.MandatoryTerms.DataSubjectRightsAssistance.Should().Be(entity.DataSubjectRightsAssistance);
        agreement.MandatoryTerms.ComplianceAssistance.Should().Be(entity.ComplianceAssistance);
        agreement.MandatoryTerms.DataDeletionOrReturn.Should().Be(entity.DataDeletionOrReturn);
        agreement.MandatoryTerms.AuditRights.Should().Be(entity.AuditRights);
        agreement.TenantId.Should().Be(entity.TenantId);
        agreement.ModuleId.Should().Be(entity.ModuleId);
        agreement.CreatedAtUtc.Should().Be(entity.CreatedAtUtc);
        agreement.LastUpdatedAtUtc.Should().Be(entity.LastUpdatedAtUtc);
    }

    [Fact]
    public void ToDomain_InvalidStatusValue_ReturnsNull()
    {
        // Arrange
        var entity = CreateEntity();
        entity.StatusValue = 99;

        // Act
        var agreement = DataProcessingAgreementMapper.ToDomain(entity);

        // Assert
        agreement.Should().BeNull();
    }

    [Fact]
    public void ToDomain_InvalidJson_ReturnsNull()
    {
        // Arrange
        var entity = CreateEntity();
        entity.ProcessingPurposesJson = "not-json{";

        // Act
        var agreement = DataProcessingAgreementMapper.ToDomain(entity);

        // Assert
        agreement.Should().BeNull();
    }

    [Fact]
    public void ToDomain_EmptyJson_ReturnsEmptyList()
    {
        // Arrange
        var entity = CreateEntity();
        entity.ProcessingPurposesJson = "";

        // Act
        var agreement = DataProcessingAgreementMapper.ToDomain(entity);

        // Assert
        agreement.Should().NotBeNull();
        agreement!.ProcessingPurposes.Should().BeEmpty();
    }

    [Fact]
    public void ToDomain_NullEntity_ThrowsArgumentNullException()
    {
        // Act
        var act = () => DataProcessingAgreementMapper.ToDomain(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    #endregion

    #region Roundtrip Tests

    [Fact]
    public void Roundtrip_ToEntityThenToDomain_PreservesValues()
    {
        // Arrange
        var original = CreateAgreement();

        // Act
        var entity = DataProcessingAgreementMapper.ToEntity(original);
        var roundtripped = DataProcessingAgreementMapper.ToDomain(entity);

        // Assert
        roundtripped.Should().NotBeNull();
        roundtripped!.Id.Should().Be(original.Id);
        roundtripped.ProcessorId.Should().Be(original.ProcessorId);
        roundtripped.Status.Should().Be(original.Status);
        roundtripped.SignedAtUtc.Should().Be(original.SignedAtUtc);
        roundtripped.ExpiresAtUtc.Should().Be(original.ExpiresAtUtc);
        roundtripped.HasSCCs.Should().Be(original.HasSCCs);
        roundtripped.ProcessingPurposes.Should().BeEquivalentTo(original.ProcessingPurposes);
        roundtripped.MandatoryTerms.ProcessOnDocumentedInstructions.Should().Be(original.MandatoryTerms.ProcessOnDocumentedInstructions);
        roundtripped.MandatoryTerms.ConfidentialityObligations.Should().Be(original.MandatoryTerms.ConfidentialityObligations);
        roundtripped.MandatoryTerms.SecurityMeasures.Should().Be(original.MandatoryTerms.SecurityMeasures);
        roundtripped.MandatoryTerms.SubProcessorRequirements.Should().Be(original.MandatoryTerms.SubProcessorRequirements);
        roundtripped.MandatoryTerms.DataSubjectRightsAssistance.Should().Be(original.MandatoryTerms.DataSubjectRightsAssistance);
        roundtripped.MandatoryTerms.ComplianceAssistance.Should().Be(original.MandatoryTerms.ComplianceAssistance);
        roundtripped.MandatoryTerms.DataDeletionOrReturn.Should().Be(original.MandatoryTerms.DataDeletionOrReturn);
        roundtripped.MandatoryTerms.AuditRights.Should().Be(original.MandatoryTerms.AuditRights);
        roundtripped.TenantId.Should().Be(original.TenantId);
        roundtripped.ModuleId.Should().Be(original.ModuleId);
        roundtripped.CreatedAtUtc.Should().Be(original.CreatedAtUtc);
        roundtripped.LastUpdatedAtUtc.Should().Be(original.LastUpdatedAtUtc);
    }

    #endregion

    private static DataProcessingAgreement CreateAgreement() => new()
    {
        Id = "dpa-001",
        ProcessorId = "proc-stripe",
        Status = DPAStatus.Active,
        SignedAtUtc = new DateTimeOffset(2026, 1, 10, 9, 0, 0, TimeSpan.Zero),
        ExpiresAtUtc = new DateTimeOffset(2027, 1, 10, 9, 0, 0, TimeSpan.Zero),
        HasSCCs = true,
        ProcessingPurposes = ["Payment processing", "Fraud detection"],
        MandatoryTerms = new DPAMandatoryTerms
        {
            ProcessOnDocumentedInstructions = true,
            ConfidentialityObligations = true,
            SecurityMeasures = true,
            SubProcessorRequirements = true,
            DataSubjectRightsAssistance = true,
            ComplianceAssistance = false,
            DataDeletionOrReturn = true,
            AuditRights = true
        },
        TenantId = "tenant-abc",
        ModuleId = "module-payments",
        CreatedAtUtc = new DateTimeOffset(2026, 1, 10, 9, 0, 0, TimeSpan.Zero),
        LastUpdatedAtUtc = new DateTimeOffset(2026, 2, 15, 11, 30, 0, TimeSpan.Zero)
    };

    private static DataProcessingAgreementEntity CreateEntity() => new()
    {
        Id = "dpa-entity-001",
        ProcessorId = "proc-aws",
        StatusValue = (int)DPAStatus.Active,
        SignedAtUtc = new DateTimeOffset(2026, 3, 1, 8, 0, 0, TimeSpan.Zero),
        ExpiresAtUtc = new DateTimeOffset(2027, 3, 1, 8, 0, 0, TimeSpan.Zero),
        HasSCCs = false,
        ProcessingPurposesJson = "[\"Data hosting\",\"Backup services\"]",
        ProcessOnDocumentedInstructions = true,
        ConfidentialityObligations = true,
        SecurityMeasures = false,
        SubProcessorRequirements = true,
        DataSubjectRightsAssistance = false,
        ComplianceAssistance = true,
        DataDeletionOrReturn = true,
        AuditRights = false,
        TenantId = "tenant-xyz",
        ModuleId = null,
        CreatedAtUtc = new DateTimeOffset(2026, 3, 1, 8, 0, 0, TimeSpan.Zero),
        LastUpdatedAtUtc = new DateTimeOffset(2026, 3, 5, 12, 0, 0, TimeSpan.Zero)
    };
}
