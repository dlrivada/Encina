using Encina.Compliance.Retention.Events;
using Encina.Compliance.Retention.Model;
using Encina.Compliance.Retention.ReadModels;
using Encina.Marten.Projections;
using FluentAssertions;

namespace Encina.UnitTests.Compliance.Retention;

/// <summary>
/// Unit tests for <see cref="RetentionPolicyProjection"/>.
/// </summary>
public class RetentionPolicyProjectionTests
{
    private readonly RetentionPolicyProjection _sut = new();
    private readonly ProjectionContext _context = new();

    #region Helpers

    private static RetentionPolicyCreated CreateRetentionPolicyCreatedEvent(
        Guid? policyId = null,
        string dataCategory = "customer-data",
        TimeSpan? retentionPeriod = null,
        bool autoDelete = true,
        RetentionPolicyType policyType = RetentionPolicyType.TimeBased,
        string? reason = "GDPR Art. 5(1)(e) storage limitation",
        string? legalBasis = "Tax Code §147",
        DateTimeOffset? occurredAtUtc = null,
        string? tenantId = "tenant-1",
        string? moduleId = "module-1")
    {
        return new RetentionPolicyCreated(
            PolicyId: policyId ?? Guid.NewGuid(),
            DataCategory: dataCategory,
            RetentionPeriod: retentionPeriod ?? TimeSpan.FromDays(2555),
            AutoDelete: autoDelete,
            PolicyType: policyType,
            Reason: reason,
            LegalBasis: legalBasis,
            OccurredAtUtc: occurredAtUtc ?? new DateTimeOffset(2026, 1, 10, 9, 0, 0, TimeSpan.Zero),
            TenantId: tenantId,
            ModuleId: moduleId);
    }

    private RetentionPolicyReadModel CreateActiveReadModel(Guid? policyId = null, int version = 1)
    {
        var created = CreateRetentionPolicyCreatedEvent(policyId: policyId);
        var readModel = _sut.Create(created, _context);
        readModel.Version = version;
        return readModel;
    }

    #endregion

    #region ProjectionName

    [Fact]
    public void ProjectionName_ShouldReturnRetentionPolicyProjection()
    {
        // Act
        var name = _sut.ProjectionName;

        // Assert
        name.Should().Be("RetentionPolicyProjection");
    }

    #endregion

    #region Create (RetentionPolicyCreated)

    [Fact]
    public void Create_RetentionPolicyCreated_ShouldMapAllFields()
    {
        // Arrange
        var policyId = Guid.NewGuid();
        var period = TimeSpan.FromDays(2555);
        var occurredAt = new DateTimeOffset(2026, 1, 10, 9, 0, 0, TimeSpan.Zero);

        var created = CreateRetentionPolicyCreatedEvent(
            policyId: policyId,
            dataCategory: "financial-records",
            retentionPeriod: period,
            autoDelete: true,
            policyType: RetentionPolicyType.EventBased,
            reason: "Legal requirement: 7 years after contract end",
            legalBasis: "Tax Code §147",
            occurredAtUtc: occurredAt,
            tenantId: "tenant-A",
            moduleId: "module-B");

        // Act
        var result = _sut.Create(created, _context);

        // Assert
        result.Id.Should().Be(policyId);
        result.DataCategory.Should().Be("financial-records");
        result.RetentionPeriod.Should().Be(period);
        result.AutoDelete.Should().BeTrue();
        result.PolicyType.Should().Be(RetentionPolicyType.EventBased);
        result.Reason.Should().Be("Legal requirement: 7 years after contract end");
        result.LegalBasis.Should().Be("Tax Code §147");
        result.TenantId.Should().Be("tenant-A");
        result.ModuleId.Should().Be("module-B");
        result.CreatedAtUtc.Should().Be(occurredAt);
        result.LastModifiedAtUtc.Should().Be(occurredAt);
    }

    [Fact]
    public void Create_RetentionPolicyCreated_ShouldSetIsActiveToTrue()
    {
        // Arrange
        var created = CreateRetentionPolicyCreatedEvent();

        // Act
        var result = _sut.Create(created, _context);

        // Assert
        result.IsActive.Should().BeTrue();
    }

    [Fact]
    public void Create_RetentionPolicyCreated_ShouldSetVersionToOne()
    {
        // Arrange
        var created = CreateRetentionPolicyCreatedEvent();

        // Act
        var result = _sut.Create(created, _context);

        // Assert
        result.Version.Should().Be(1);
    }

    [Fact]
    public void Create_RetentionPolicyCreated_ShouldLeaveDeactivationReasonNull()
    {
        // Arrange
        var created = CreateRetentionPolicyCreatedEvent();

        // Act
        var result = _sut.Create(created, _context);

        // Assert
        result.DeactivationReason.Should().BeNull();
    }

    [Fact]
    public void Create_RetentionPolicyCreated_WithNullOptionalFields_ShouldMapNulls()
    {
        // Arrange
        var created = CreateRetentionPolicyCreatedEvent(
            reason: null,
            legalBasis: null,
            tenantId: null,
            moduleId: null);

        // Act
        var result = _sut.Create(created, _context);

        // Assert
        result.Reason.Should().BeNull();
        result.LegalBasis.Should().BeNull();
        result.TenantId.Should().BeNull();
        result.ModuleId.Should().BeNull();
    }

    #endregion

    #region Apply (RetentionPolicyUpdated)

    [Fact]
    public void Apply_RetentionPolicyUpdated_ShouldUpdateFields()
    {
        // Arrange
        var readModel = CreateActiveReadModel();
        var newPeriod = TimeSpan.FromDays(3650);
        var updatedAt = new DateTimeOffset(2026, 6, 15, 12, 0, 0, TimeSpan.Zero);

        var updated = new RetentionPolicyUpdated(
            PolicyId: readModel.Id,
            RetentionPeriod: newPeriod,
            AutoDelete: false,
            Reason: "Revised legal requirement: 10 years",
            LegalBasis: "Regulation (EU) 2022/1234",
            OccurredAtUtc: updatedAt);

        // Act
        var result = _sut.Apply(updated, readModel, _context);

        // Assert
        result.RetentionPeriod.Should().Be(newPeriod);
        result.AutoDelete.Should().BeFalse();
        result.Reason.Should().Be("Revised legal requirement: 10 years");
        result.LegalBasis.Should().Be("Regulation (EU) 2022/1234");
        result.LastModifiedAtUtc.Should().Be(updatedAt);
    }

    [Fact]
    public void Apply_RetentionPolicyUpdated_ShouldIncrementVersion()
    {
        // Arrange
        var readModel = CreateActiveReadModel(version: 1);
        var updated = new RetentionPolicyUpdated(
            PolicyId: readModel.Id,
            RetentionPeriod: TimeSpan.FromDays(365),
            AutoDelete: true,
            Reason: null,
            LegalBasis: null,
            OccurredAtUtc: DateTimeOffset.UtcNow);

        // Act
        var result = _sut.Apply(updated, readModel, _context);

        // Assert
        result.Version.Should().Be(2);
    }

    [Fact]
    public void Apply_RetentionPolicyUpdated_ShouldNotChangeIsActive()
    {
        // Arrange
        var readModel = CreateActiveReadModel();
        var updated = new RetentionPolicyUpdated(
            PolicyId: readModel.Id,
            RetentionPeriod: TimeSpan.FromDays(180),
            AutoDelete: false,
            Reason: null,
            LegalBasis: null,
            OccurredAtUtc: DateTimeOffset.UtcNow);

        // Act
        var result = _sut.Apply(updated, readModel, _context);

        // Assert
        result.IsActive.Should().BeTrue();
    }

    [Fact]
    public void Apply_RetentionPolicyUpdated_WithNullOptionalFields_ShouldClearThem()
    {
        // Arrange
        var readModel = CreateActiveReadModel();
        var updated = new RetentionPolicyUpdated(
            PolicyId: readModel.Id,
            RetentionPeriod: TimeSpan.FromDays(730),
            AutoDelete: true,
            Reason: null,
            LegalBasis: null,
            OccurredAtUtc: DateTimeOffset.UtcNow);

        // Act
        var result = _sut.Apply(updated, readModel, _context);

        // Assert
        result.Reason.Should().BeNull();
        result.LegalBasis.Should().BeNull();
    }

    #endregion

    #region Apply (RetentionPolicyDeactivated)

    [Fact]
    public void Apply_RetentionPolicyDeactivated_ShouldSetInactive()
    {
        // Arrange
        var readModel = CreateActiveReadModel();
        var deactivatedAt = new DateTimeOffset(2026, 9, 1, 8, 30, 0, TimeSpan.Zero);

        var deactivated = new RetentionPolicyDeactivated(
            PolicyId: readModel.Id,
            Reason: "Policy superseded by updated regulatory guidance",
            OccurredAtUtc: deactivatedAt);

        // Act
        var result = _sut.Apply(deactivated, readModel, _context);

        // Assert
        result.IsActive.Should().BeFalse();
        result.DeactivationReason.Should().Be("Policy superseded by updated regulatory guidance");
        result.LastModifiedAtUtc.Should().Be(deactivatedAt);
    }

    [Fact]
    public void Apply_RetentionPolicyDeactivated_ShouldIncrementVersion()
    {
        // Arrange
        var readModel = CreateActiveReadModel(version: 2);
        var deactivated = new RetentionPolicyDeactivated(
            PolicyId: readModel.Id,
            Reason: "Data category no longer collected",
            OccurredAtUtc: DateTimeOffset.UtcNow);

        // Act
        var result = _sut.Apply(deactivated, readModel, _context);

        // Assert
        result.Version.Should().Be(3);
    }

    [Fact]
    public void Apply_RetentionPolicyDeactivated_ShouldPreserveUnchangedFields()
    {
        // Arrange
        var policyId = Guid.NewGuid();
        var created = CreateRetentionPolicyCreatedEvent(
            policyId: policyId,
            dataCategory: "marketing-data",
            tenantId: "tenant-X",
            moduleId: "module-Y");

        var readModel = _sut.Create(created, _context);
        var deactivated = new RetentionPolicyDeactivated(
            PolicyId: policyId,
            Reason: "No longer needed",
            OccurredAtUtc: DateTimeOffset.UtcNow);

        // Act
        var result = _sut.Apply(deactivated, readModel, _context);

        // Assert
        result.Id.Should().Be(policyId);
        result.DataCategory.Should().Be("marketing-data");
        result.TenantId.Should().Be("tenant-X");
        result.ModuleId.Should().Be("module-Y");
    }

    #endregion
}
