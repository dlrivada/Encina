using Encina.Security.Audit;
using Shouldly;

namespace Encina.GuardTests.Security.Audit;

/// <summary>
/// Guard clause tests for <see cref="SensitiveDataAccessedNotification"/>.
/// Verifies construction and property access for the notification record.
/// </summary>
public class SensitiveDataAccessedNotificationGuardTests
{
    [Fact]
    public void Constructor_ValidParameters_DoesNotThrow()
    {
        var act = () => new SensitiveDataAccessedNotification(
            "Patient", "PAT-001", "user-1", DateTimeOffset.UtcNow);

        Should.NotThrow(act);
    }

    [Fact]
    public void Constructor_NullEntityId_DoesNotThrow()
    {
        var act = () => new SensitiveDataAccessedNotification(
            "Patient", null, "user-1", DateTimeOffset.UtcNow);

        Should.NotThrow(act);
    }

    [Fact]
    public void Constructor_NullUserId_DoesNotThrow()
    {
        var act = () => new SensitiveDataAccessedNotification(
            "Patient", "PAT-001", null, DateTimeOffset.UtcNow);

        Should.NotThrow(act);
    }

    [Fact]
    public void Properties_ReturnCorrectValues()
    {
        var now = DateTimeOffset.UtcNow;
        var notification = new SensitiveDataAccessedNotification(
            "FinancialRecord", "FR-100", "user-42", now);

        notification.EntityType.ShouldBe("FinancialRecord");
        notification.EntityId.ShouldBe("FR-100");
        notification.UserId.ShouldBe("user-42");
        notification.AccessedAtUtc.ShouldBe(now);
    }

    [Fact]
    public void Notification_ImplementsINotification()
    {
        var notification = new SensitiveDataAccessedNotification(
            "Patient", null, null, DateTimeOffset.UtcNow);

        notification.ShouldBeAssignableTo<INotification>();
    }
}
