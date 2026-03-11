#pragma warning disable CA2012 // Use ValueTasks correctly

using System.Text.Json;

using Encina.Compliance.DPIA;
using Encina.Compliance.DPIA.Model;

using FluentAssertions;

namespace Encina.UnitTests.Compliance.DPIA;

/// <summary>
/// Unit tests for <see cref="DPIAAssessmentMapper"/>.
/// </summary>
public class DPIAAssessmentMapperTests
{
    #region ToEntity Tests

    [Fact]
    public void ToEntity_ValidAssessment_MapsAllProperties()
    {
        var id = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;
        var assessment = new DPIAAssessment
        {
            Id = id,
            RequestTypeName = "Ns.TestCommand",
            Status = DPIAAssessmentStatus.Approved,
            ProcessingType = "AutomatedDecisionMaking",
            Reason = "High risk",
            CreatedAtUtc = now,
            ApprovedAtUtc = now.AddHours(1),
            NextReviewAtUtc = now.AddDays(365),
            TenantId = "tenant-1",
            ModuleId = "module-1"
        };

        var entity = DPIAAssessmentMapper.ToEntity(assessment);

        entity.Id.Should().Be(id.ToString("D"));
        entity.RequestTypeName.Should().Be("Ns.TestCommand");
        entity.StatusValue.Should().Be((int)DPIAAssessmentStatus.Approved);
        entity.ProcessingType.Should().Be("AutomatedDecisionMaking");
        entity.Reason.Should().Be("High risk");
        entity.CreatedAtUtc.Should().Be(now);
        entity.ApprovedAtUtc.Should().Be(now.AddHours(1));
        entity.NextReviewAtUtc.Should().Be(now.AddDays(365));
        entity.TenantId.Should().Be("tenant-1");
        entity.ModuleId.Should().Be("module-1");
    }

    [Fact]
    public void ToEntity_WithResult_SerializesToJson()
    {
        var assessment = CreateMinimalAssessment();
        var result = new DPIAResult
        {
            OverallRisk = RiskLevel.High,
            IdentifiedRisks = [new RiskItem("Profiling", RiskLevel.High, "Test risk", null)],
            ProposedMitigations = [],
            RequiresPriorConsultation = false,
            AssessedAtUtc = DateTimeOffset.UtcNow
        };
        var assessmentWithResult = assessment with { Result = result };

        var entity = DPIAAssessmentMapper.ToEntity(assessmentWithResult);

        entity.ResultJson.Should().NotBeNullOrEmpty();
        var deserialized = JsonSerializer.Deserialize<DPIAResult>(entity.ResultJson!);
        deserialized.Should().NotBeNull();
        deserialized!.OverallRisk.Should().Be(RiskLevel.High);
    }

    [Fact]
    public void ToEntity_WithDPOConsultation_SerializesToJson()
    {
        var assessment = CreateMinimalAssessment();
        var consultation = new DPOConsultation
        {
            Id = Guid.NewGuid(),
            DPOName = "Jane Doe",
            DPOEmail = "dpo@company.com",
            RequestedAtUtc = DateTimeOffset.UtcNow,
            Decision = DPOConsultationDecision.Pending
        };
        var assessmentWithConsultation = assessment with { DPOConsultation = consultation };

        var entity = DPIAAssessmentMapper.ToEntity(assessmentWithConsultation);

        entity.DPOConsultationJson.Should().NotBeNullOrEmpty();
        var deserialized = JsonSerializer.Deserialize<DPOConsultation>(entity.DPOConsultationJson!);
        deserialized.Should().NotBeNull();
        deserialized!.DPOEmail.Should().Be("dpo@company.com");
    }

    [Fact]
    public void ToEntity_NullResult_SetsNullJson()
    {
        var assessment = CreateMinimalAssessment();

        var entity = DPIAAssessmentMapper.ToEntity(assessment);

        entity.ResultJson.Should().BeNull();
        entity.DPOConsultationJson.Should().BeNull();
    }

    [Fact]
    public void ToEntity_NullAssessment_ThrowsArgumentNullException()
    {
        var act = () => DPIAAssessmentMapper.ToEntity(null!);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("assessment");
    }

    #endregion

    #region ToDomain Tests

    [Fact]
    public void ToDomain_ValidEntity_MapsAllProperties()
    {
        var id = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;
        var entity = new DPIAAssessmentEntity
        {
            Id = id.ToString("D"),
            RequestTypeName = "Ns.TestCommand",
            StatusValue = (int)DPIAAssessmentStatus.Approved,
            ProcessingType = "Profiling",
            Reason = "Required",
            CreatedAtUtc = now,
            ApprovedAtUtc = now.AddHours(1),
            NextReviewAtUtc = now.AddDays(365),
            TenantId = "t1",
            ModuleId = "m1"
        };

        var domain = DPIAAssessmentMapper.ToDomain(entity);

        domain.Should().NotBeNull();
        domain!.Id.Should().Be(id);
        domain.RequestTypeName.Should().Be("Ns.TestCommand");
        domain.Status.Should().Be(DPIAAssessmentStatus.Approved);
        domain.ProcessingType.Should().Be("Profiling");
        domain.Reason.Should().Be("Required");
        domain.CreatedAtUtc.Should().Be(now);
        domain.ApprovedAtUtc.Should().Be(now.AddHours(1));
        domain.NextReviewAtUtc.Should().Be(now.AddDays(365));
        domain.TenantId.Should().Be("t1");
        domain.ModuleId.Should().Be("m1");
    }

    [Fact]
    public void ToDomain_InvalidGuid_ReturnsNull()
    {
        var entity = new DPIAAssessmentEntity
        {
            Id = "not-a-guid",
            RequestTypeName = "Ns.Test",
            StatusValue = 0,
            CreatedAtUtc = DateTimeOffset.UtcNow
        };

        var domain = DPIAAssessmentMapper.ToDomain(entity);

        domain.Should().BeNull();
    }

    [Fact]
    public void ToDomain_InvalidStatusValue_ReturnsNull()
    {
        var entity = new DPIAAssessmentEntity
        {
            Id = Guid.NewGuid().ToString("D"),
            RequestTypeName = "Ns.Test",
            StatusValue = 999,
            CreatedAtUtc = DateTimeOffset.UtcNow
        };

        var domain = DPIAAssessmentMapper.ToDomain(entity);

        domain.Should().BeNull();
    }

    [Fact]
    public void ToDomain_InvalidResultJson_ReturnsNull()
    {
        var entity = new DPIAAssessmentEntity
        {
            Id = Guid.NewGuid().ToString("D"),
            RequestTypeName = "Ns.Test",
            StatusValue = (int)DPIAAssessmentStatus.Draft,
            ResultJson = "{ invalid json",
            CreatedAtUtc = DateTimeOffset.UtcNow
        };

        var domain = DPIAAssessmentMapper.ToDomain(entity);

        domain.Should().BeNull();
    }

    [Fact]
    public void ToDomain_InvalidConsultationJson_ReturnsNull()
    {
        var entity = new DPIAAssessmentEntity
        {
            Id = Guid.NewGuid().ToString("D"),
            RequestTypeName = "Ns.Test",
            StatusValue = (int)DPIAAssessmentStatus.Draft,
            DPOConsultationJson = "not json at all",
            CreatedAtUtc = DateTimeOffset.UtcNow
        };

        var domain = DPIAAssessmentMapper.ToDomain(entity);

        domain.Should().BeNull();
    }

    [Fact]
    public void ToDomain_ValidResultJson_DeserializesResult()
    {
        var result = new DPIAResult
        {
            OverallRisk = RiskLevel.Medium,
            IdentifiedRisks = [],
            ProposedMitigations = [],
            RequiresPriorConsultation = false,
            AssessedAtUtc = DateTimeOffset.UtcNow
        };

        var entity = new DPIAAssessmentEntity
        {
            Id = Guid.NewGuid().ToString("D"),
            RequestTypeName = "Ns.Test",
            StatusValue = (int)DPIAAssessmentStatus.Approved,
            ResultJson = JsonSerializer.Serialize(result),
            CreatedAtUtc = DateTimeOffset.UtcNow
        };

        var domain = DPIAAssessmentMapper.ToDomain(entity);

        domain.Should().NotBeNull();
        domain!.Result.Should().NotBeNull();
        domain.Result!.OverallRisk.Should().Be(RiskLevel.Medium);
    }

    [Fact]
    public void ToDomain_NullEntity_ThrowsArgumentNullException()
    {
        var act = () => DPIAAssessmentMapper.ToDomain(null!);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("entity");
    }

    #endregion

    #region Round-Trip Tests

    [Fact]
    public void RoundTrip_ToEntityThenToDomain_PreservesValues()
    {
        var id = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;
        var original = new DPIAAssessment
        {
            Id = id,
            RequestTypeName = "Ns.RoundTripCommand",
            Status = DPIAAssessmentStatus.Approved,
            ProcessingType = "Profiling",
            Reason = "Annual review",
            CreatedAtUtc = now,
            ApprovedAtUtc = now.AddHours(2),
            NextReviewAtUtc = now.AddDays(180),
            TenantId = "tenant-x",
            ModuleId = "module-y"
        };

        var entity = DPIAAssessmentMapper.ToEntity(original);
        var roundTripped = DPIAAssessmentMapper.ToDomain(entity);

        roundTripped.Should().NotBeNull();
        roundTripped!.Id.Should().Be(original.Id);
        roundTripped.RequestTypeName.Should().Be(original.RequestTypeName);
        roundTripped.Status.Should().Be(original.Status);
        roundTripped.ProcessingType.Should().Be(original.ProcessingType);
        roundTripped.Reason.Should().Be(original.Reason);
        roundTripped.TenantId.Should().Be(original.TenantId);
        roundTripped.ModuleId.Should().Be(original.ModuleId);
    }

    #endregion

    #region Helpers

    private static DPIAAssessment CreateMinimalAssessment() => new()
    {
        Id = Guid.NewGuid(),
        RequestTypeName = "Ns.TestCommand",
        Status = DPIAAssessmentStatus.Draft,
        CreatedAtUtc = DateTimeOffset.UtcNow
    };

    #endregion
}
