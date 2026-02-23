using Encina.Compliance.Consent;
using FluentAssertions;

namespace Encina.UnitTests.Compliance.Consent.Events;

/// <summary>
/// Unit tests for <see cref="ConsentExpiredEvent"/>.
/// </summary>
public class ConsentExpiredEventTests
{
    [Fact]
    public void Constructor_AllProperties_ShouldCreateInstance()
    {
        // Arrange
        var occurredAt = new DateTimeOffset(2026, 2, 23, 12, 0, 0, TimeSpan.Zero);
        var expiredAt = occurredAt.AddDays(-1);

        // Act
        var evt = new ConsentExpiredEvent(
            SubjectId: "user-123",
            Purpose: ConsentPurposes.Marketing,
            OccurredAtUtc: occurredAt,
            ExpiredAtUtc: expiredAt);

        // Assert
        evt.SubjectId.Should().Be("user-123");
        evt.Purpose.Should().Be(ConsentPurposes.Marketing);
        evt.OccurredAtUtc.Should().Be(occurredAt);
        evt.ExpiredAtUtc.Should().Be(expiredAt);
    }

    [Fact]
    public void ShouldImplementINotification()
    {
        // Act
        var evt = new ConsentExpiredEvent(
            "user-1", ConsentPurposes.Marketing, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddDays(-1));

        // Assert
        evt.Should().BeAssignableTo<INotification>();
    }

    [Fact]
    public void Equality_SameValues_ShouldBeEqual()
    {
        // Arrange
        var occurredAt = new DateTimeOffset(2026, 2, 23, 12, 0, 0, TimeSpan.Zero);
        var expiredAt = occurredAt.AddHours(-2);

        var evt1 = new ConsentExpiredEvent("user-1", ConsentPurposes.Analytics, occurredAt, expiredAt);
        var evt2 = new ConsentExpiredEvent("user-1", ConsentPurposes.Analytics, occurredAt, expiredAt);

        // Assert
        evt1.Should().Be(evt2);
    }

    [Fact]
    public void Equality_DifferentValues_ShouldNotBeEqual()
    {
        // Arrange
        var occurredAt = new DateTimeOffset(2026, 2, 23, 12, 0, 0, TimeSpan.Zero);

        var evt1 = new ConsentExpiredEvent("user-1", ConsentPurposes.Marketing, occurredAt, occurredAt.AddDays(-1));
        var evt2 = new ConsentExpiredEvent("user-1", ConsentPurposes.Marketing, occurredAt, occurredAt.AddDays(-2));

        // Assert
        evt1.Should().NotBe(evt2);
    }
}
