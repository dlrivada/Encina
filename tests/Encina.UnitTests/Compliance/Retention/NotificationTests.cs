using Encina.Compliance.Retention;
using Encina.Compliance.Retention.Model;

using Shouldly;

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

        notification.EntityId.ShouldBe("entity-1");
        notification.DataCategory.ShouldBe("user-data");
        notification.DeletedAtUtc.ShouldBe(deletedAt);
        notification.PolicyId.ShouldBe("policy-1");
    }

    [Fact]
    public void DataDeletedNotification_ImplementsINotification()
    {
        var notification = new DataDeletedNotification("e", "c", DateTimeOffset.UtcNow, null);

        notification.ShouldBeAssignableTo<INotification>();
    }

    [Fact]
    public void DataDeletedNotification_NullPolicyId_IsAllowed()
    {
        var notification = new DataDeletedNotification("e", "c", DateTimeOffset.UtcNow, null);

        notification.PolicyId.ShouldBeNull();
    }

    [Fact]
    public void DataExpiringNotification_SetsAllProperties()
    {
        var expiresAt = DateTimeOffset.UtcNow.AddDays(7);
        var occurredAt = DateTimeOffset.UtcNow;
        var notification = new DataExpiringNotification("entity-2", "session-data", expiresAt, 7, occurredAt);

        notification.EntityId.ShouldBe("entity-2");
        notification.DataCategory.ShouldBe("session-data");
        notification.ExpiresAtUtc.ShouldBe(expiresAt);
        notification.DaysUntilExpiration.ShouldBe(7);
        notification.OccurredAtUtc.ShouldBe(occurredAt);
    }

    [Fact]
    public void DataExpiringNotification_ImplementsINotification()
    {
        var notification = new DataExpiringNotification("e", "c", DateTimeOffset.UtcNow, 1, DateTimeOffset.UtcNow);

        notification.ShouldBeAssignableTo<INotification>();
    }

    [Fact]
    public void LegalHoldAppliedNotification_SetsAllProperties()
    {
        var appliedAt = DateTimeOffset.UtcNow;
        var notification = new LegalHoldAppliedNotification("hold-1", "entity-3", "Litigation", appliedAt);

        notification.HoldId.ShouldBe("hold-1");
        notification.EntityId.ShouldBe("entity-3");
        notification.Reason.ShouldBe("Litigation");
        notification.AppliedAtUtc.ShouldBe(appliedAt);
    }

    [Fact]
    public void LegalHoldAppliedNotification_ImplementsINotification()
    {
        var notification = new LegalHoldAppliedNotification("h", "e", "r", DateTimeOffset.UtcNow);

        notification.ShouldBeAssignableTo<INotification>();
    }

    [Fact]
    public void LegalHoldReleasedNotification_SetsAllProperties()
    {
        var releasedAt = DateTimeOffset.UtcNow;
        var notification = new LegalHoldReleasedNotification("hold-2", "entity-4", releasedAt);

        notification.HoldId.ShouldBe("hold-2");
        notification.EntityId.ShouldBe("entity-4");
        notification.ReleasedAtUtc.ShouldBe(releasedAt);
    }

    [Fact]
    public void LegalHoldReleasedNotification_ImplementsINotification()
    {
        var notification = new LegalHoldReleasedNotification("h", "e", DateTimeOffset.UtcNow);

        notification.ShouldBeAssignableTo<INotification>();
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

        notification.Result.ShouldBe(result);
        notification.OccurredAtUtc.ShouldBe(occurredAt);
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

        notification.ShouldBeAssignableTo<INotification>();
    }
}
