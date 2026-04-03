using Encina.Compliance.Retention;
using Encina.Compliance.Retention.Model;

using FluentAssertions;

namespace Encina.UnitTests.Compliance.Retention;

/// <summary>
/// Unit tests for retention notification records verifying immutability and correct initialization.
/// </summary>
public sealed class NotificationTests
{
    [Fact]
    public void DataDeletedNotification_SetsAllProperties()
    {
        var deletedAt = DateTimeOffset.UtcNow;
        var notification = new DataDeletedNotification("entity-1", "user-data", deletedAt, "policy-1");

        notification.EntityId.Should().Be("entity-1");
        notification.DataCategory.Should().Be("user-data");
        notification.DeletedAtUtc.Should().Be(deletedAt);
        notification.PolicyId.Should().Be("policy-1");
    }

    [Fact]
    public void DataDeletedNotification_ImplementsINotification()
    {
        var notification = new DataDeletedNotification("e", "c", DateTimeOffset.UtcNow, null);

        notification.Should().BeAssignableTo<INotification>();
    }

    [Fact]
    public void DataDeletedNotification_NullPolicyId_IsAllowed()
    {
        var notification = new DataDeletedNotification("e", "c", DateTimeOffset.UtcNow, null);

        notification.PolicyId.Should().BeNull();
    }

    [Fact]
    public void DataExpiringNotification_SetsAllProperties()
    {
        var expiresAt = DateTimeOffset.UtcNow.AddDays(7);
        var occurredAt = DateTimeOffset.UtcNow;
        var notification = new DataExpiringNotification("entity-2", "session-data", expiresAt, 7, occurredAt);

        notification.EntityId.Should().Be("entity-2");
        notification.DataCategory.Should().Be("session-data");
        notification.ExpiresAtUtc.Should().Be(expiresAt);
        notification.DaysUntilExpiration.Should().Be(7);
        notification.OccurredAtUtc.Should().Be(occurredAt);
    }

    [Fact]
    public void DataExpiringNotification_ImplementsINotification()
    {
        var notification = new DataExpiringNotification("e", "c", DateTimeOffset.UtcNow, 1, DateTimeOffset.UtcNow);

        notification.Should().BeAssignableTo<INotification>();
    }

    [Fact]
    public void LegalHoldAppliedNotification_SetsAllProperties()
    {
        var appliedAt = DateTimeOffset.UtcNow;
        var notification = new LegalHoldAppliedNotification("hold-1", "entity-3", "Litigation", appliedAt);

        notification.HoldId.Should().Be("hold-1");
        notification.EntityId.Should().Be("entity-3");
        notification.Reason.Should().Be("Litigation");
        notification.AppliedAtUtc.Should().Be(appliedAt);
    }

    [Fact]
    public void LegalHoldAppliedNotification_ImplementsINotification()
    {
        var notification = new LegalHoldAppliedNotification("h", "e", "r", DateTimeOffset.UtcNow);

        notification.Should().BeAssignableTo<INotification>();
    }

    [Fact]
    public void LegalHoldReleasedNotification_SetsAllProperties()
    {
        var releasedAt = DateTimeOffset.UtcNow;
        var notification = new LegalHoldReleasedNotification("hold-2", "entity-4", releasedAt);

        notification.HoldId.Should().Be("hold-2");
        notification.EntityId.Should().Be("entity-4");
        notification.ReleasedAtUtc.Should().Be(releasedAt);
    }

    [Fact]
    public void LegalHoldReleasedNotification_ImplementsINotification()
    {
        var notification = new LegalHoldReleasedNotification("h", "e", DateTimeOffset.UtcNow);

        notification.Should().BeAssignableTo<INotification>();
    }

    [Fact]
    public void RetentionEnforcementCompletedNotification_SetsAllProperties()
    {
        var result = new DeletionResult
        {
            TotalRecordsEvaluated = 10,
            RecordsDeleted = 5,
            RecordsRetained = 2,
            RecordsFailed = 1,
            RecordsUnderHold = 2,
            Details = [],
            ExecutedAtUtc = DateTimeOffset.UtcNow
        };
        var occurredAt = DateTimeOffset.UtcNow;
        var notification = new RetentionEnforcementCompletedNotification(result, occurredAt);

        notification.Result.Should().Be(result);
        notification.OccurredAtUtc.Should().Be(occurredAt);
    }

    [Fact]
    public void RetentionEnforcementCompletedNotification_ImplementsINotification()
    {
        var result = new DeletionResult
        {
            TotalRecordsEvaluated = 0,
            RecordsDeleted = 0,
            RecordsRetained = 0,
            RecordsFailed = 0,
            RecordsUnderHold = 0,
            Details = [],
            ExecutedAtUtc = DateTimeOffset.UtcNow
        };
        var notification = new RetentionEnforcementCompletedNotification(result, DateTimeOffset.UtcNow);

        notification.Should().BeAssignableTo<INotification>();
    }
}
