#pragma warning disable CA2012 // Use ValueTasks correctly

using Encina.Compliance.DPIA;
using Encina.Compliance.DPIA.Model;

using FluentAssertions;

namespace Encina.UnitTests.Compliance.DPIA;

/// <summary>
/// Unit tests for <see cref="DPIAAuditEntryMapper"/>.
/// </summary>
public class DPIAAuditEntryMapperTests
{
    #region ToEntity Tests

    [Fact]
    public void ToEntity_ValidEntry_MapsAllProperties()
    {
        var id = Guid.NewGuid();
        var assessmentId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;

        var entry = new DPIAAuditEntry
        {
            Id = id,
            AssessmentId = assessmentId,
            Action = "AssessmentCompleted",
            PerformedBy = "System",
            OccurredAtUtc = now,
            Details = "Risk assessment completed.",
            TenantId = "tenant-1",
            ModuleId = "module-1"
        };

        var entity = DPIAAuditEntryMapper.ToEntity(entry);

        entity.Id.Should().Be(id.ToString("D"));
        entity.AssessmentId.Should().Be(assessmentId.ToString("D"));
        entity.Action.Should().Be("AssessmentCompleted");
        entity.PerformedBy.Should().Be("System");
        entity.OccurredAtUtc.Should().Be(now);
        entity.Details.Should().Be("Risk assessment completed.");
        entity.TenantId.Should().Be("tenant-1");
        entity.ModuleId.Should().Be("module-1");
    }

    [Fact]
    public void ToEntity_NullEntry_ThrowsArgumentNullException()
    {
        var act = () => DPIAAuditEntryMapper.ToEntity(null!);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("entry");
    }

    #endregion

    #region ToDomain Tests

    [Fact]
    public void ToDomain_ValidEntity_MapsAllProperties()
    {
        var id = Guid.NewGuid();
        var assessmentId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;

        var entity = new DPIAAuditEntryEntity
        {
            Id = id.ToString("D"),
            AssessmentId = assessmentId.ToString("D"),
            Action = "DPOConsultationRequested",
            PerformedBy = "Admin",
            OccurredAtUtc = now,
            Details = "DPO consulted.",
            TenantId = "t1",
            ModuleId = "m1"
        };

        var domain = DPIAAuditEntryMapper.ToDomain(entity);

        domain.Should().NotBeNull();
        domain!.Id.Should().Be(id);
        domain.AssessmentId.Should().Be(assessmentId);
        domain.Action.Should().Be("DPOConsultationRequested");
        domain.PerformedBy.Should().Be("Admin");
        domain.OccurredAtUtc.Should().Be(now);
        domain.Details.Should().Be("DPO consulted.");
        domain.TenantId.Should().Be("t1");
        domain.ModuleId.Should().Be("m1");
    }

    [Fact]
    public void ToDomain_InvalidId_ReturnsNull()
    {
        var entity = new DPIAAuditEntryEntity
        {
            Id = "not-a-guid",
            AssessmentId = Guid.NewGuid().ToString("D"),
            Action = "Test",
            OccurredAtUtc = DateTimeOffset.UtcNow
        };

        var domain = DPIAAuditEntryMapper.ToDomain(entity);

        domain.Should().BeNull();
    }

    [Fact]
    public void ToDomain_InvalidAssessmentId_ReturnsNull()
    {
        var entity = new DPIAAuditEntryEntity
        {
            Id = Guid.NewGuid().ToString("D"),
            AssessmentId = "bad-guid",
            Action = "Test",
            OccurredAtUtc = DateTimeOffset.UtcNow
        };

        var domain = DPIAAuditEntryMapper.ToDomain(entity);

        domain.Should().BeNull();
    }

    [Fact]
    public void ToDomain_NullEntity_ThrowsArgumentNullException()
    {
        var act = () => DPIAAuditEntryMapper.ToDomain(null!);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("entity");
    }

    #endregion

    #region Round-Trip Tests

    [Fact]
    public void RoundTrip_ToEntityThenToDomain_PreservesValues()
    {
        var original = new DPIAAuditEntry
        {
            Id = Guid.NewGuid(),
            AssessmentId = Guid.NewGuid(),
            Action = "StatusChanged",
            PerformedBy = "DPO",
            OccurredAtUtc = DateTimeOffset.UtcNow,
            Details = "Approved after review.",
            TenantId = "t-round",
            ModuleId = "m-round"
        };

        var entity = DPIAAuditEntryMapper.ToEntity(original);
        var roundTripped = DPIAAuditEntryMapper.ToDomain(entity);

        roundTripped.Should().NotBeNull();
        roundTripped!.Id.Should().Be(original.Id);
        roundTripped.AssessmentId.Should().Be(original.AssessmentId);
        roundTripped.Action.Should().Be(original.Action);
        roundTripped.PerformedBy.Should().Be(original.PerformedBy);
        roundTripped.Details.Should().Be(original.Details);
        roundTripped.TenantId.Should().Be(original.TenantId);
        roundTripped.ModuleId.Should().Be(original.ModuleId);
    }

    #endregion
}
