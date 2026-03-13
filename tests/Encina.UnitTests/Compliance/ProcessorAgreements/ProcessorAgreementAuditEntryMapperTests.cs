#pragma warning disable CA2012

using Encina.Compliance.ProcessorAgreements;
using Encina.Compliance.ProcessorAgreements.Model;

using FluentAssertions;

namespace Encina.UnitTests.Compliance.ProcessorAgreements;

/// <summary>
/// Unit tests for <see cref="ProcessorAgreementAuditEntryMapper"/> static mapping methods.
/// </summary>
public class ProcessorAgreementAuditEntryMapperTests
{
    #region ToEntity Tests

    [Fact]
    public void ToEntity_ValidEntry_MapsAllProperties()
    {
        // Arrange
        var entry = CreateEntry();

        // Act
        var entity = ProcessorAgreementAuditEntryMapper.ToEntity(entry);

        // Assert
        entity.Id.Should().Be(entry.Id);
        entity.ProcessorId.Should().Be(entry.ProcessorId);
        entity.DPAId.Should().Be(entry.DPAId);
        entity.Action.Should().Be(entry.Action);
        entity.Detail.Should().Be(entry.Detail);
        entity.PerformedByUserId.Should().Be(entry.PerformedByUserId);
        entity.OccurredAtUtc.Should().Be(entry.OccurredAtUtc);
        entity.TenantId.Should().Be(entry.TenantId);
        entity.ModuleId.Should().Be(entry.ModuleId);
    }

    [Fact]
    public void ToEntity_NullEntry_ThrowsArgumentNullException()
    {
        // Act
        var act = () => ProcessorAgreementAuditEntryMapper.ToEntity(null!);

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
        var domain = ProcessorAgreementAuditEntryMapper.ToDomain(entity);

        // Assert
        domain.Id.Should().Be(entity.Id);
        domain.ProcessorId.Should().Be(entity.ProcessorId);
        domain.DPAId.Should().Be(entity.DPAId);
        domain.Action.Should().Be(entity.Action);
        domain.Detail.Should().Be(entity.Detail);
        domain.PerformedByUserId.Should().Be(entity.PerformedByUserId);
        domain.OccurredAtUtc.Should().Be(entity.OccurredAtUtc);
        domain.TenantId.Should().Be(entity.TenantId);
        domain.ModuleId.Should().Be(entity.ModuleId);
    }

    [Fact]
    public void ToDomain_NullEntity_ThrowsArgumentNullException()
    {
        // Act
        var act = () => ProcessorAgreementAuditEntryMapper.ToDomain(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    #endregion

    #region Roundtrip Tests

    [Fact]
    public void Roundtrip_ToEntityThenToDomain_PreservesValues()
    {
        // Arrange
        var original = CreateEntry();

        // Act
        var entity = ProcessorAgreementAuditEntryMapper.ToEntity(original);
        var roundtripped = ProcessorAgreementAuditEntryMapper.ToDomain(entity);

        // Assert
        roundtripped.Id.Should().Be(original.Id);
        roundtripped.ProcessorId.Should().Be(original.ProcessorId);
        roundtripped.DPAId.Should().Be(original.DPAId);
        roundtripped.Action.Should().Be(original.Action);
        roundtripped.Detail.Should().Be(original.Detail);
        roundtripped.PerformedByUserId.Should().Be(original.PerformedByUserId);
        roundtripped.OccurredAtUtc.Should().Be(original.OccurredAtUtc);
        roundtripped.TenantId.Should().Be(original.TenantId);
        roundtripped.ModuleId.Should().Be(original.ModuleId);
    }

    #endregion

    private static ProcessorAgreementAuditEntry CreateEntry() => new()
    {
        Id = "audit-001",
        ProcessorId = "proc-stripe",
        DPAId = "dpa-001",
        Action = "DPASigned",
        Detail = "Data Processing Agreement signed with Stripe",
        PerformedByUserId = "admin@company.com",
        OccurredAtUtc = new DateTimeOffset(2026, 1, 15, 10, 0, 0, TimeSpan.Zero),
        TenantId = "tenant-abc",
        ModuleId = "module-payments"
    };

    private static ProcessorAgreementAuditEntryEntity CreateEntity() => new()
    {
        Id = "audit-entity-001",
        ProcessorId = "proc-aws",
        DPAId = null,
        Action = "Registered",
        Detail = null,
        PerformedByUserId = null,
        OccurredAtUtc = new DateTimeOffset(2026, 3, 1, 8, 0, 0, TimeSpan.Zero),
        TenantId = null,
        ModuleId = null
    };
}
