using Encina.Compliance.Retention.Events;
using Encina.Compliance.Retention.ReadModels;
using Encina.Marten.Projections;
using FluentAssertions;

namespace Encina.UnitTests.Compliance.Retention;

/// <summary>
/// Unit tests for <see cref="LegalHoldProjection"/>.
/// </summary>
public class LegalHoldProjectionTests
{
    private readonly LegalHoldProjection _sut = new();
    private readonly ProjectionContext _context = new();

    #region Helpers

    private static LegalHoldPlaced CreateLegalHoldPlacedEvent(
        Guid? holdId = null,
        string entityId = "customer-42",
        string reason = "Ongoing litigation - Case #12345",
        string appliedByUserId = "legal-counsel",
        DateTimeOffset? appliedAtUtc = null,
        string? tenantId = "tenant-1",
        string? moduleId = "module-1")
    {
        return new LegalHoldPlaced(
            HoldId: holdId ?? Guid.NewGuid(),
            EntityId: entityId,
            Reason: reason,
            AppliedByUserId: appliedByUserId,
            AppliedAtUtc: appliedAtUtc ?? new DateTimeOffset(2026, 3, 10, 11, 0, 0, TimeSpan.Zero),
            TenantId: tenantId,
            ModuleId: moduleId);
    }

    private LegalHoldReadModel CreateActiveReadModel(Guid? holdId = null, int version = 1)
    {
        var placed = CreateLegalHoldPlacedEvent(holdId: holdId);
        var readModel = _sut.Create(placed, _context);
        readModel.Version = version;
        return readModel;
    }

    #endregion

    #region ProjectionName

    [Fact]
    public void ProjectionName_ShouldReturnLegalHoldProjection()
    {
        // Act
        var name = _sut.ProjectionName;

        // Assert
        name.Should().Be("LegalHoldProjection");
    }

    #endregion

    #region Create (LegalHoldPlaced)

    [Fact]
    public void Create_LegalHoldPlaced_ShouldMapAllFields()
    {
        // Arrange
        var holdId = Guid.NewGuid();
        var appliedAt = new DateTimeOffset(2026, 3, 10, 11, 0, 0, TimeSpan.Zero);

        var placed = CreateLegalHoldPlacedEvent(
            holdId: holdId,
            entityId: "employee-7",
            reason: "Regulatory investigation - Ref. INV-2026-001",
            appliedByUserId: "chief-legal-officer",
            appliedAtUtc: appliedAt,
            tenantId: "tenant-A",
            moduleId: "module-B");

        // Act
        var result = _sut.Create(placed, _context);

        // Assert
        result.Id.Should().Be(holdId);
        result.EntityId.Should().Be("employee-7");
        result.Reason.Should().Be("Regulatory investigation - Ref. INV-2026-001");
        result.AppliedByUserId.Should().Be("chief-legal-officer");
        result.AppliedAtUtc.Should().Be(appliedAt);
        result.TenantId.Should().Be("tenant-A");
        result.ModuleId.Should().Be("module-B");
        result.LastModifiedAtUtc.Should().Be(appliedAt);
    }

    [Fact]
    public void Create_LegalHoldPlaced_ShouldSetIsActiveToTrue()
    {
        // Arrange
        var placed = CreateLegalHoldPlacedEvent();

        // Act
        var result = _sut.Create(placed, _context);

        // Assert
        result.IsActive.Should().BeTrue();
    }

    [Fact]
    public void Create_LegalHoldPlaced_ShouldSetVersionToOne()
    {
        // Arrange
        var placed = CreateLegalHoldPlacedEvent();

        // Act
        var result = _sut.Create(placed, _context);

        // Assert
        result.Version.Should().Be(1);
    }

    [Fact]
    public void Create_LegalHoldPlaced_ShouldLeaveReleaseFieldsNull()
    {
        // Arrange
        var placed = CreateLegalHoldPlacedEvent();

        // Act
        var result = _sut.Create(placed, _context);

        // Assert
        result.ReleasedByUserId.Should().BeNull();
        result.ReleasedAtUtc.Should().BeNull();
    }

    [Fact]
    public void Create_LegalHoldPlaced_WithNullTenantAndModule_ShouldMapNulls()
    {
        // Arrange
        var placed = CreateLegalHoldPlacedEvent(tenantId: null, moduleId: null);

        // Act
        var result = _sut.Create(placed, _context);

        // Assert
        result.TenantId.Should().BeNull();
        result.ModuleId.Should().BeNull();
    }

    #endregion

    #region Apply (LegalHoldLifted)

    [Fact]
    public void Apply_LegalHoldLifted_ShouldSetInactive()
    {
        // Arrange
        var readModel = CreateActiveReadModel();
        var releasedAt = new DateTimeOffset(2026, 9, 20, 16, 0, 0, TimeSpan.Zero);

        var lifted = new LegalHoldLifted(
            HoldId: readModel.Id,
            EntityId: readModel.EntityId,
            ReleasedByUserId: "senior-counsel",
            ReleasedAtUtc: releasedAt);

        // Act
        var result = _sut.Apply(lifted, readModel, _context);

        // Assert
        result.IsActive.Should().BeFalse();
        result.ReleasedByUserId.Should().Be("senior-counsel");
        result.ReleasedAtUtc.Should().Be(releasedAt);
        result.LastModifiedAtUtc.Should().Be(releasedAt);
    }

    [Fact]
    public void Apply_LegalHoldLifted_ShouldIncrementVersion()
    {
        // Arrange
        var readModel = CreateActiveReadModel(version: 1);
        var lifted = new LegalHoldLifted(
            HoldId: readModel.Id,
            EntityId: readModel.EntityId,
            ReleasedByUserId: "dpo",
            ReleasedAtUtc: DateTimeOffset.UtcNow);

        // Act
        var result = _sut.Apply(lifted, readModel, _context);

        // Assert
        result.Version.Should().Be(2);
    }

    [Fact]
    public void Apply_LegalHoldLifted_ShouldPreserveUnchangedFields()
    {
        // Arrange
        var holdId = Guid.NewGuid();
        var placed = CreateLegalHoldPlacedEvent(
            holdId: holdId,
            entityId: "contract-999",
            reason: "Dispute resolution",
            appliedByUserId: "legal-team",
            tenantId: "tenant-Z",
            moduleId: "module-W");

        var readModel = _sut.Create(placed, _context);
        var lifted = new LegalHoldLifted(
            HoldId: holdId,
            EntityId: "contract-999",
            ReleasedByUserId: "legal-team",
            ReleasedAtUtc: DateTimeOffset.UtcNow);

        // Act
        var result = _sut.Apply(lifted, readModel, _context);

        // Assert
        result.Id.Should().Be(holdId);
        result.EntityId.Should().Be("contract-999");
        result.Reason.Should().Be("Dispute resolution");
        result.AppliedByUserId.Should().Be("legal-team");
        result.TenantId.Should().Be("tenant-Z");
        result.ModuleId.Should().Be("module-W");
        result.AppliedAtUtc.Should().Be(placed.AppliedAtUtc);
    }

    #endregion
}
