using Encina.Security.Audit;
using FluentAssertions;

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

        act.Should().NotThrow();
    }

    [Fact]
    public void Constructor_NullEntityId_DoesNotThrow()
    {
        var act = () => new SensitiveDataAccessedNotification(
            "Patient", null, "user-1", DateTimeOffset.UtcNow);

        act.Should().NotThrow();
    }

    [Fact]
    public void Constructor_NullUserId_DoesNotThrow()
    {
        var act = () => new SensitiveDataAccessedNotification(
            "Patient", "PAT-001", null, DateTimeOffset.UtcNow);

        act.Should().NotThrow();
    }

    [Fact]
    public void Properties_ReturnCorrectValues()
    {
        var now = DateTimeOffset.UtcNow;
        var notification = new SensitiveDataAccessedNotification(
            "FinancialRecord", "FR-100", "user-42", now);

        notification.EntityType.Should().Be("FinancialRecord");
        notification.EntityId.Should().Be("FR-100");
        notification.UserId.Should().Be("user-42");
        notification.AccessedAtUtc.Should().Be(now);
    }

    [Fact]
    public void Notification_ImplementsINotification()
    {
        var notification = new SensitiveDataAccessedNotification(
            "Patient", null, null, DateTimeOffset.UtcNow);

        notification.Should().BeAssignableTo<INotification>();
    }
}
