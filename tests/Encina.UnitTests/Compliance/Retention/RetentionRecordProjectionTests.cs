using Encina.Compliance.Retention.Events;
using Encina.Compliance.Retention.Model;
using Encina.Compliance.Retention.ReadModels;
using Encina.Marten.Projections;
using Shouldly;

namespace Encina.UnitTests.Compliance.Retention;

/// <summary>
/// Unit tests for <see cref="RetentionRecordProjection"/>.
/// </summary>
public class RetentionRecordProjectionTests
{
    private readonly RetentionRecordProjection _sut = new();
    private readonly ProjectionContext _context = new();

    #region Helpers

    private static RetentionRecordTracked CreateRetentionRecordTrackedEvent(
        Guid? recordId = null,
        string entityId = "customer-42",
        string dataCategory = "customer-data",
        Guid? policyId = null,
        TimeSpan? retentionPeriod = null,
        DateTimeOffset? expiresAtUtc = null,
        DateTimeOffset? occurredAtUtc = null,
        string? tenantId = "tenant-1",
        string? moduleId = "module-1")
    {
        var occurred = occurredAtUtc ?? new DateTimeOffset(2026, 1, 10, 9, 0, 0, TimeSpan.Zero);
        var period = retentionPeriod ?? TimeSpan.FromDays(2555);

        return new RetentionRecordTracked(
            RecordId: recordId ?? Guid.NewGuid(),
            EntityId: entityId,
            DataCategory: dataCategory,
            PolicyId: policyId ?? Guid.NewGuid(),
            RetentionPeriod: period,
            ExpiresAtUtc: expiresAtUtc ?? occurred.Add(period),
            OccurredAtUtc: occurred,
            TenantId: tenantId,
            ModuleId: moduleId);
    }

    private RetentionRecordReadModel CreateActiveReadModel(
        Guid? recordId = null,
        DateTimeOffset? expiresAtUtc = null,
        int version = 1)
    {
        var tracked = CreateRetentionRecordTrackedEvent(
            recordId: recordId,
            expiresAtUtc: expiresAtUtc);
        var readModel = _sut.Create(tracked, _context);
        readModel.Version = version;
        return readModel;
    }

    #endregion

    #region ProjectionName

    [Fact]
    public void ProjectionName_ShouldReturnRetentionRecordProjection()
    {
        // Act
        var name = _sut.ProjectionName;

        // Assert
        name.ShouldBe("RetentionRecordProjection");
    }

    #endregion

    #region Create (RetentionRecordTracked)

    [Fact]
    public void Create_RetentionRecordTracked_ShouldMapAllFields()
    {
        // Arrange
        var recordId = Guid.NewGuid();
        var policyId = Guid.NewGuid();
        var period = TimeSpan.FromDays(730);
        var occurredAt = new DateTimeOffset(2026, 2, 1, 10, 0, 0, TimeSpan.Zero);
        var expiresAt = occurredAt.Add(period);

        var tracked = CreateRetentionRecordTrackedEvent(
            recordId: recordId,
            entityId: "order-9876",
            dataCategory: "financial-records",
            policyId: policyId,
            retentionPeriod: period,
            expiresAtUtc: expiresAt,
            occurredAtUtc: occurredAt,
            tenantId: "tenant-A",
            moduleId: "module-B");

        // Act
        var result = _sut.Create(tracked, _context);

        // Assert
        result.Id.ShouldBe(recordId);
        result.EntityId.ShouldBe("order-9876");
        result.DataCategory.ShouldBe("financial-records");
        result.PolicyId.ShouldBe(policyId);
        result.RetentionPeriod.ShouldBe(period);
        result.ExpiresAtUtc.ShouldBe(expiresAt);
        result.TenantId.ShouldBe("tenant-A");
        result.ModuleId.ShouldBe("module-B");
        result.CreatedAtUtc.ShouldBe(occurredAt);
        result.LastModifiedAtUtc.ShouldBe(occurredAt);
    }

    [Fact]
    public void Create_RetentionRecordTracked_ShouldSetStatusToActive()
    {
        // Arrange
        var tracked = CreateRetentionRecordTrackedEvent();

        // Act
        var result = _sut.Create(tracked, _context);

        // Assert
        result.Status.ShouldBe(RetentionStatus.Active);
    }

    [Fact]
    public void Create_RetentionRecordTracked_ShouldSetVersionToOne()
    {
        // Arrange
        var tracked = CreateRetentionRecordTrackedEvent();

        // Act
        var result = _sut.Create(tracked, _context);

        // Assert
        result.Version.ShouldBe(1);
    }

    [Fact]
    public void Create_RetentionRecordTracked_ShouldLeaveTerminalFieldsNull()
    {
        // Arrange
        var tracked = CreateRetentionRecordTrackedEvent();

        // Act
        var result = _sut.Create(tracked, _context);

        // Assert
        result.LegalHoldId.ShouldBeNull();
        result.DeletedAtUtc.ShouldBeNull();
        result.AnonymizedAtUtc.ShouldBeNull();
    }

    #endregion

    #region Apply (RetentionRecordExpired)

    [Fact]
    public void Apply_RetentionRecordExpired_ShouldSetExpired()
    {
        // Arrange
        var readModel = CreateActiveReadModel();
        var expiredAt = new DateTimeOffset(2033, 1, 10, 9, 0, 0, TimeSpan.Zero);

        var expired = new RetentionRecordExpired(
            RecordId: readModel.Id,
            EntityId: readModel.EntityId,
            DataCategory: readModel.DataCategory,
            OccurredAtUtc: expiredAt);

        // Act
        var result = _sut.Apply(expired, readModel, _context);

        // Assert
        result.Status.ShouldBe(RetentionStatus.Expired);
        result.LastModifiedAtUtc.ShouldBe(expiredAt);
    }

    [Fact]
    public void Apply_RetentionRecordExpired_ShouldIncrementVersion()
    {
        // Arrange
        var readModel = CreateActiveReadModel(version: 1);
        var expired = new RetentionRecordExpired(
            RecordId: readModel.Id,
            EntityId: readModel.EntityId,
            DataCategory: readModel.DataCategory,
            OccurredAtUtc: DateTimeOffset.UtcNow);

        // Act
        var result = _sut.Apply(expired, readModel, _context);

        // Assert
        result.Version.ShouldBe(2);
    }

    #endregion

    #region Apply (RetentionRecordHeld)

    [Fact]
    public void Apply_RetentionRecordHeld_ShouldSetUnderLegalHold()
    {
        // Arrange
        var readModel = CreateActiveReadModel();
        var holdId = Guid.NewGuid();
        var heldAt = new DateTimeOffset(2026, 3, 15, 14, 0, 0, TimeSpan.Zero);

        var held = new RetentionRecordHeld(
            RecordId: readModel.Id,
            EntityId: readModel.EntityId,
            LegalHoldId: holdId,
            OccurredAtUtc: heldAt);

        // Act
        var result = _sut.Apply(held, readModel, _context);

        // Assert
        result.Status.ShouldBe(RetentionStatus.UnderLegalHold);
        result.LegalHoldId.ShouldBe(holdId);
        result.LastModifiedAtUtc.ShouldBe(heldAt);
    }

    [Fact]
    public void Apply_RetentionRecordHeld_ShouldIncrementVersion()
    {
        // Arrange
        var readModel = CreateActiveReadModel(version: 1);
        var held = new RetentionRecordHeld(
            RecordId: readModel.Id,
            EntityId: readModel.EntityId,
            LegalHoldId: Guid.NewGuid(),
            OccurredAtUtc: DateTimeOffset.UtcNow);

        // Act
        var result = _sut.Apply(held, readModel, _context);

        // Assert
        result.Version.ShouldBe(2);
    }

    #endregion

    #region Apply (RetentionRecordReleased)

    [Fact]
    public void Apply_RetentionRecordReleased_WhenExpired_ShouldSetExpired()
    {
        // Arrange
        var expiresAt = new DateTimeOffset(2033, 1, 10, 9, 0, 0, TimeSpan.Zero);
        var readModel = CreateActiveReadModel(expiresAtUtc: expiresAt);
        readModel.Status = RetentionStatus.UnderLegalHold;
        readModel.LegalHoldId = Guid.NewGuid();

        // Released after expiration date
        var releasedAt = expiresAt.AddDays(30);
        var released = new RetentionRecordReleased(
            RecordId: readModel.Id,
            EntityId: readModel.EntityId,
            LegalHoldId: readModel.LegalHoldId!.Value,
            OccurredAtUtc: releasedAt);

        // Act
        var result = _sut.Apply(released, readModel, _context);

        // Assert
        result.Status.ShouldBe(RetentionStatus.Expired);
        result.LegalHoldId.ShouldBeNull();
        result.LastModifiedAtUtc.ShouldBe(releasedAt);
    }

    [Fact]
    public void Apply_RetentionRecordReleased_WhenNotExpired_ShouldSetActive()
    {
        // Arrange
        var expiresAt = new DateTimeOffset(2033, 1, 10, 9, 0, 0, TimeSpan.Zero);
        var readModel = CreateActiveReadModel(expiresAtUtc: expiresAt);
        readModel.Status = RetentionStatus.UnderLegalHold;
        readModel.LegalHoldId = Guid.NewGuid();

        // Released before expiration date
        var releasedAt = expiresAt.AddDays(-180);
        var released = new RetentionRecordReleased(
            RecordId: readModel.Id,
            EntityId: readModel.EntityId,
            LegalHoldId: readModel.LegalHoldId!.Value,
            OccurredAtUtc: releasedAt);

        // Act
        var result = _sut.Apply(released, readModel, _context);

        // Assert
        result.Status.ShouldBe(RetentionStatus.Active);
        result.LegalHoldId.ShouldBeNull();
        result.LastModifiedAtUtc.ShouldBe(releasedAt);
    }

    [Fact]
    public void Apply_RetentionRecordReleased_AtExactExpiryMoment_ShouldSetExpired()
    {
        // Arrange
        var expiresAt = new DateTimeOffset(2033, 1, 10, 9, 0, 0, TimeSpan.Zero);
        var readModel = CreateActiveReadModel(expiresAtUtc: expiresAt);
        readModel.Status = RetentionStatus.UnderLegalHold;
        readModel.LegalHoldId = Guid.NewGuid();

        var released = new RetentionRecordReleased(
            RecordId: readModel.Id,
            EntityId: readModel.EntityId,
            LegalHoldId: readModel.LegalHoldId!.Value,
            OccurredAtUtc: expiresAt);

        // Act
        var result = _sut.Apply(released, readModel, _context);

        // Assert
        result.Status.ShouldBe(RetentionStatus.Expired);
    }

    [Fact]
    public void Apply_RetentionRecordReleased_ShouldIncrementVersion()
    {
        // Arrange
        var readModel = CreateActiveReadModel(version: 2);
        readModel.Status = RetentionStatus.UnderLegalHold;
        readModel.LegalHoldId = Guid.NewGuid();

        var released = new RetentionRecordReleased(
            RecordId: readModel.Id,
            EntityId: readModel.EntityId,
            LegalHoldId: readModel.LegalHoldId!.Value,
            OccurredAtUtc: DateTimeOffset.UtcNow.AddDays(-1));

        // Act
        var result = _sut.Apply(released, readModel, _context);

        // Assert
        result.Version.ShouldBe(3);
    }

    #endregion

    #region Apply (DataDeleted)

    [Fact]
    public void Apply_DataDeleted_ShouldSetDeleted()
    {
        // Arrange
        var readModel = CreateActiveReadModel();
        readModel.Status = RetentionStatus.Expired;
        var deletedAt = new DateTimeOffset(2033, 2, 1, 0, 0, 0, TimeSpan.Zero);

        var deleted = new DataDeleted(
            RecordId: readModel.Id,
            EntityId: readModel.EntityId,
            DataCategory: readModel.DataCategory,
            PolicyId: readModel.PolicyId,
            DeletedAtUtc: deletedAt);

        // Act
        var result = _sut.Apply(deleted, readModel, _context);

        // Assert
        result.Status.ShouldBe(RetentionStatus.Deleted);
        result.DeletedAtUtc.ShouldBe(deletedAt);
        result.LastModifiedAtUtc.ShouldBe(deletedAt);
    }

    [Fact]
    public void Apply_DataDeleted_ShouldIncrementVersion()
    {
        // Arrange
        var readModel = CreateActiveReadModel(version: 2);
        var deleted = new DataDeleted(
            RecordId: readModel.Id,
            EntityId: readModel.EntityId,
            DataCategory: readModel.DataCategory,
            PolicyId: readModel.PolicyId,
            DeletedAtUtc: DateTimeOffset.UtcNow);

        // Act
        var result = _sut.Apply(deleted, readModel, _context);

        // Assert
        result.Version.ShouldBe(3);
    }

    [Fact]
    public void Apply_DataDeleted_ShouldNotSetAnonymizedAtUtc()
    {
        // Arrange
        var readModel = CreateActiveReadModel();
        var deleted = new DataDeleted(
            RecordId: readModel.Id,
            EntityId: readModel.EntityId,
            DataCategory: readModel.DataCategory,
            PolicyId: readModel.PolicyId,
            DeletedAtUtc: DateTimeOffset.UtcNow);

        // Act
        var result = _sut.Apply(deleted, readModel, _context);

        // Assert
        result.AnonymizedAtUtc.ShouldBeNull();
    }

    #endregion

    #region Apply (DataAnonymized)

    [Fact]
    public void Apply_DataAnonymized_ShouldSetDeleted()
    {
        // Arrange
        var readModel = CreateActiveReadModel();
        readModel.Status = RetentionStatus.Expired;
        var anonymizedAt = new DateTimeOffset(2033, 3, 5, 12, 0, 0, TimeSpan.Zero);

        var anonymized = new DataAnonymized(
            RecordId: readModel.Id,
            EntityId: readModel.EntityId,
            DataCategory: readModel.DataCategory,
            PolicyId: readModel.PolicyId,
            AnonymizedAtUtc: anonymizedAt);

        // Act
        var result = _sut.Apply(anonymized, readModel, _context);

        // Assert
        result.Status.ShouldBe(RetentionStatus.Deleted);
        result.AnonymizedAtUtc.ShouldBe(anonymizedAt);
        result.LastModifiedAtUtc.ShouldBe(anonymizedAt);
    }

    [Fact]
    public void Apply_DataAnonymized_ShouldIncrementVersion()
    {
        // Arrange
        var readModel = CreateActiveReadModel(version: 3);
        var anonymized = new DataAnonymized(
            RecordId: readModel.Id,
            EntityId: readModel.EntityId,
            DataCategory: readModel.DataCategory,
            PolicyId: readModel.PolicyId,
            AnonymizedAtUtc: DateTimeOffset.UtcNow);

        // Act
        var result = _sut.Apply(anonymized, readModel, _context);

        // Assert
        result.Version.ShouldBe(4);
    }

    [Fact]
    public void Apply_DataAnonymized_ShouldNotSetDeletedAtUtc()
    {
        // Arrange
        var readModel = CreateActiveReadModel();
        var anonymized = new DataAnonymized(
            RecordId: readModel.Id,
            EntityId: readModel.EntityId,
            DataCategory: readModel.DataCategory,
            PolicyId: readModel.PolicyId,
            AnonymizedAtUtc: DateTimeOffset.UtcNow);

        // Act
        var result = _sut.Apply(anonymized, readModel, _context);

        // Assert
        result.DeletedAtUtc.ShouldBeNull();
    }

    #endregion
}
