using Encina.Compliance.Retention.Aggregates;
using Encina.Compliance.Retention.Events;
using Encina.Compliance.Retention.Model;
using Shouldly;

namespace Encina.UnitTests.Compliance.Retention;

/// <summary>
/// Unit tests for <see cref="RetentionRecordAggregate"/>.
/// </summary>
public class RetentionRecordAggregateTests
{
    private static readonly Guid DefaultId = Guid.NewGuid();
    private static readonly Guid DefaultPolicyId = Guid.NewGuid();
    private static readonly Guid DefaultHoldId = Guid.NewGuid();
    private static readonly DateTimeOffset Now = new(2026, 3, 15, 10, 0, 0, TimeSpan.Zero);
    private static readonly TimeSpan DefaultRetentionPeriod = TimeSpan.FromDays(365);

    #region Track Tests

    [Fact]
    public void Track_ValidParameters_ShouldSetAllProperties()
    {
        // Arrange
        var entityId = "customer-42";
        var dataCategory = "customer-data";
        var expiresAt = Now.Add(DefaultRetentionPeriod);
        var tenantId = "tenant-1";
        var moduleId = "module-crm";

        // Act
        var record = RetentionRecordAggregate.Track(
            DefaultId, entityId, dataCategory, DefaultPolicyId,
            DefaultRetentionPeriod, expiresAt, Now, tenantId, moduleId);

        // Assert
        record.Id.ShouldBe(DefaultId);
        record.EntityId.ShouldBe(entityId);
        record.DataCategory.ShouldBe(dataCategory);
        record.PolicyId.ShouldBe(DefaultPolicyId);
        record.RetentionPeriod.ShouldBe(DefaultRetentionPeriod);
        record.Status.ShouldBe(RetentionStatus.Active);
        record.ExpiresAtUtc.ShouldBe(expiresAt);
        record.TenantId.ShouldBe(tenantId);
        record.ModuleId.ShouldBe(moduleId);
        record.LegalHoldId.ShouldBeNull();
    }

    [Fact]
    public void Track_ValidParameters_ShouldRaiseRetentionRecordTrackedEvent()
    {
        // Act
        var record = CreateTrackedRecord();

        // Assert
        record.UncommittedEvents.Count.ShouldBe(1);
        record.UncommittedEvents[0].ShouldBeOfType<RetentionRecordTracked>();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Track_NullOrWhitespaceEntityId_ShouldThrowArgumentException(string? entityId)
    {
        // Act
        var act = () => RetentionRecordAggregate.Track(
            DefaultId, entityId!, "customer-data", DefaultPolicyId,
            DefaultRetentionPeriod, Now.Add(DefaultRetentionPeriod), Now);

        // Assert
        Should.Throw<ArgumentException>(act);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Track_NullOrWhitespaceDataCategory_ShouldThrowArgumentException(string? dataCategory)
    {
        // Act
        var act = () => RetentionRecordAggregate.Track(
            DefaultId, "customer-42", dataCategory!, DefaultPolicyId,
            DefaultRetentionPeriod, Now.Add(DefaultRetentionPeriod), Now);

        // Assert
        Should.Throw<ArgumentException>(act);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-365)]
    public void Track_ZeroOrNegativeRetentionPeriod_ShouldNotThrow_BecauseExpiresAtUtcIsPassedExplicitly(int days)
    {
        // The Track factory receives an explicit expiresAtUtc — validation of retentionPeriod is
        // the caller's responsibility. This test documents that Track itself does not validate the period.
        var act = () => RetentionRecordAggregate.Track(
            DefaultId, "customer-42", "customer-data", DefaultPolicyId,
            TimeSpan.FromDays(days), Now.AddDays(-1), Now);

        // No guard on retentionPeriod in Track; assertion matches actual source behavior.
        Should.NotThrow(act);
    }

    #endregion

    #region MarkExpired Tests

    [Fact]
    public void MarkExpired_ActiveRecord_ShouldSetStatusToExpired()
    {
        // Arrange
        var record = CreateTrackedRecord();

        // Act
        record.MarkExpired(Now.AddDays(366));

        // Assert
        record.Status.ShouldBe(RetentionStatus.Expired);
    }

    [Fact]
    public void MarkExpired_ActiveRecord_ShouldRaiseRetentionRecordExpiredEvent()
    {
        // Arrange
        var record = CreateTrackedRecord();

        // Act
        record.MarkExpired(Now.AddDays(366));

        // Assert
        record.UncommittedEvents.Count.ShouldBe(2);
        record.UncommittedEvents[1].ShouldBeOfType<RetentionRecordExpired>();
    }

    [Fact]
    public void MarkExpired_AlreadyExpiredRecord_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var record = CreateExpiredRecord();

        // Act
        var act = () => record.MarkExpired(Now.AddDays(367));

        // Assert
        Should.Throw<InvalidOperationException>(act);
    }

    [Fact]
    public void MarkExpired_RecordUnderLegalHold_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var record = CreateHeldRecord();

        // Act
        var act = () => record.MarkExpired(Now.AddDays(400));

        // Assert
        Should.Throw<InvalidOperationException>(act);
    }

    #endregion

    #region Hold Tests

    [Fact]
    public void Hold_ActiveRecord_ShouldSetStatusToUnderLegalHold()
    {
        // Arrange
        var record = CreateTrackedRecord();

        // Act
        record.Hold(DefaultHoldId, Now.AddDays(10));

        // Assert
        record.Status.ShouldBe(RetentionStatus.UnderLegalHold);
    }

    [Fact]
    public void Hold_ActiveRecord_ShouldSetLegalHoldId()
    {
        // Arrange
        var record = CreateTrackedRecord();

        // Act
        record.Hold(DefaultHoldId, Now.AddDays(10));

        // Assert
        record.LegalHoldId.ShouldBe(DefaultHoldId);
    }

    [Fact]
    public void Hold_ActiveRecord_ShouldRaiseRetentionRecordHeldEvent()
    {
        // Arrange
        var record = CreateTrackedRecord();

        // Act
        record.Hold(DefaultHoldId, Now.AddDays(10));

        // Assert
        record.UncommittedEvents.Count.ShouldBe(2);
        record.UncommittedEvents[1].ShouldBeOfType<RetentionRecordHeld>();
    }

    [Fact]
    public void Hold_ExpiredRecord_ShouldSetStatusToUnderLegalHold()
    {
        // Arrange
        var record = CreateExpiredRecord();

        // Act
        record.Hold(DefaultHoldId, Now.AddDays(370));

        // Assert
        record.Status.ShouldBe(RetentionStatus.UnderLegalHold);
    }

    [Fact]
    public void Hold_DeletedRecord_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var record = CreateDeletedRecord();

        // Act
        var act = () => record.Hold(DefaultHoldId, Now.AddDays(400));

        // Assert
        Should.Throw<InvalidOperationException>(act);
    }

    #endregion

    #region Release Tests

    [Fact]
    public void Release_HeldRecord_WhenNotExpired_ShouldRestoreActiveStatus()
    {
        // Arrange — hold is placed before expiration; ExpiresAtUtc is far in the future
        // so Apply resolves to Active when DateTimeOffset.UtcNow < ExpiresAtUtc.
        var futureExpiry = DateTimeOffset.UtcNow.AddYears(10);
        var record = RetentionRecordAggregate.Track(
            DefaultId, "customer-42", "customer-data", DefaultPolicyId,
            DefaultRetentionPeriod, futureExpiry, Now);
        record.Hold(DefaultHoldId, Now.AddDays(10));

        // Act
        record.Release(DefaultHoldId, Now.AddDays(20));

        // Assert
        record.Status.ShouldBe(RetentionStatus.Active);
    }

    [Fact]
    public void Release_HeldRecord_ShouldClearLegalHoldId()
    {
        // Arrange
        var record = CreateHeldRecord();

        // Act
        record.Release(DefaultHoldId, Now.AddDays(380));

        // Assert
        record.LegalHoldId.ShouldBeNull();
    }

    [Fact]
    public void Release_HeldRecord_ShouldRaiseRetentionRecordReleasedEvent()
    {
        // Arrange
        var record = CreateHeldRecord();

        // Act
        record.Release(DefaultHoldId, Now.AddDays(380));

        // Assert
        var releasedEvent = record.UncommittedEvents
            .OfType<RetentionRecordReleased>()
            .ShouldHaveSingleItem();
        releasedEvent.LegalHoldId.ShouldBe(DefaultHoldId);
    }

    [Fact]
    public void Release_NotUnderLegalHold_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var record = CreateExpiredRecord();

        // Act
        var act = () => record.Release(DefaultHoldId, Now.AddDays(370));

        // Assert
        Should.Throw<InvalidOperationException>(act);
    }

    [Fact]
    public void Release_ActiveRecord_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var record = CreateTrackedRecord();

        // Act
        var act = () => record.Release(DefaultHoldId, Now.AddDays(5));

        // Assert
        Should.Throw<InvalidOperationException>(act);
    }

    #endregion

    #region MarkDeleted Tests

    [Fact]
    public void MarkDeleted_ExpiredRecord_ShouldSetStatusToDeleted()
    {
        // Arrange
        var record = CreateExpiredRecord();

        // Act
        record.MarkDeleted(Now.AddDays(367));

        // Assert
        record.Status.ShouldBe(RetentionStatus.Deleted);
    }

    [Fact]
    public void MarkDeleted_ExpiredRecord_ShouldRaiseDataDeletedEvent()
    {
        // Arrange
        var record = CreateExpiredRecord();

        // Act
        record.MarkDeleted(Now.AddDays(367));

        // Assert
        record.UncommittedEvents.OfType<DataDeleted>().ShouldHaveSingleItem();
    }

    [Fact]
    public void MarkDeleted_ActiveRecord_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var record = CreateTrackedRecord();

        // Act
        var act = () => record.MarkDeleted(Now.AddDays(5));

        // Assert
        Should.Throw<InvalidOperationException>(act);
    }

    [Fact]
    public void MarkDeleted_RecordUnderLegalHold_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var record = CreateHeldRecord();

        // Act
        var act = () => record.MarkDeleted(Now.AddDays(400));

        // Assert
        Should.Throw<InvalidOperationException>(act);
    }

    #endregion

    #region MarkAnonymized Tests

    [Fact]
    public void MarkAnonymized_ExpiredRecord_ShouldSetStatusToDeleted()
    {
        // Arrange
        var record = CreateExpiredRecord();

        // Act
        record.MarkAnonymized(Now.AddDays(367));

        // Assert
        record.Status.ShouldBe(RetentionStatus.Deleted);
    }

    [Fact]
    public void MarkAnonymized_ExpiredRecord_ShouldRaiseDataAnonymizedEvent()
    {
        // Arrange
        var record = CreateExpiredRecord();

        // Act
        record.MarkAnonymized(Now.AddDays(367));

        // Assert
        record.UncommittedEvents.OfType<DataAnonymized>().ShouldHaveSingleItem();
    }

    [Fact]
    public void MarkAnonymized_ActiveRecord_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var record = CreateTrackedRecord();

        // Act
        var act = () => record.MarkAnonymized(Now.AddDays(5));

        // Assert
        Should.Throw<InvalidOperationException>(act);
    }

    [Fact]
    public void MarkAnonymized_RecordUnderLegalHold_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var record = CreateHeldRecord();

        // Act
        var act = () => record.MarkAnonymized(Now.AddDays(400));

        // Assert
        Should.Throw<InvalidOperationException>(act);
    }

    #endregion

    #region Helpers

    private static RetentionRecordAggregate CreateTrackedRecord() =>
        RetentionRecordAggregate.Track(
            DefaultId, "customer-42", "customer-data", DefaultPolicyId,
            DefaultRetentionPeriod, Now.Add(DefaultRetentionPeriod), Now);

    private static RetentionRecordAggregate CreateExpiredRecord()
    {
        var record = CreateTrackedRecord();
        record.MarkExpired(Now.AddDays(366));
        return record;
    }

    private static RetentionRecordAggregate CreateHeldRecord()
    {
        var record = CreateExpiredRecord();
        record.Hold(DefaultHoldId, Now.AddDays(370));
        return record;
    }

    /// <summary>
    /// Creates a record that was held and then released. After release the Apply method sets
    /// status to <see cref="RetentionStatus.Expired"/> or <see cref="RetentionStatus.Active"/>
    /// based on <c>DateTimeOffset.UtcNow >= ExpiresAtUtc</c>. Since the record was already
    /// expired before the hold was placed, <c>ExpiresAtUtc</c> is in the past relative to
    /// wall-clock time, so status resolves to <see cref="RetentionStatus.Expired"/>.
    /// </summary>
    private static RetentionRecordAggregate CreateReleasedRecord()
    {
        // Use a past expiry so that after release Apply resolves to Expired.
        var pastExpiry = DateTimeOffset.UtcNow.AddDays(-1);
        var record = RetentionRecordAggregate.Track(
            DefaultId, "customer-42", "customer-data", DefaultPolicyId,
            DefaultRetentionPeriod, pastExpiry, Now.AddDays(-400));
        record.MarkExpired(Now.AddDays(-1));
        record.Hold(DefaultHoldId, Now.AddDays(-1));
        record.Release(DefaultHoldId, Now);
        return record;
    }

    private static RetentionRecordAggregate CreateDeletedRecord()
    {
        var record = CreateReleasedRecord();
        // After release from a hold placed on an already-expired record,
        // status is Expired — can proceed to delete.
        record.MarkDeleted(Now.AddDays(1));
        return record;
    }

    #endregion
}
