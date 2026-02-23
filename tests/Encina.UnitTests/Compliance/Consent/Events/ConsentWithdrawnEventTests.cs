using Encina.Compliance.Consent;
using FluentAssertions;

namespace Encina.UnitTests.Compliance.Consent.Events;

/// <summary>
/// Unit tests for <see cref="ConsentWithdrawnEvent"/>.
/// </summary>
public class ConsentWithdrawnEventTests
{
    [Fact]
    public void Constructor_AllProperties_ShouldCreateInstance()
    {
        // Arrange
        var occurredAt = new DateTimeOffset(2026, 2, 23, 12, 0, 0, TimeSpan.Zero);

        // Act
        var evt = new ConsentWithdrawnEvent(
            SubjectId: "user-123",
            Purpose: ConsentPurposes.Marketing,
            OccurredAtUtc: occurredAt);

        // Assert
        evt.SubjectId.Should().Be("user-123");
        evt.Purpose.Should().Be(ConsentPurposes.Marketing);
        evt.OccurredAtUtc.Should().Be(occurredAt);
    }

    [Fact]
    public void ShouldImplementINotification()
    {
        // Act
        var evt = new ConsentWithdrawnEvent("user-1", ConsentPurposes.Marketing, DateTimeOffset.UtcNow);

        // Assert
        evt.Should().BeAssignableTo<INotification>();
    }

    [Fact]
    public void Equality_SameValues_ShouldBeEqual()
    {
        // Arrange
        var occurredAt = new DateTimeOffset(2026, 2, 23, 12, 0, 0, TimeSpan.Zero);

        var evt1 = new ConsentWithdrawnEvent("user-1", ConsentPurposes.Marketing, occurredAt);
        var evt2 = new ConsentWithdrawnEvent("user-1", ConsentPurposes.Marketing, occurredAt);

        // Assert
        evt1.Should().Be(evt2);
    }

    [Fact]
    public void Equality_DifferentValues_ShouldNotBeEqual()
    {
        // Arrange
        var occurredAt = new DateTimeOffset(2026, 2, 23, 12, 0, 0, TimeSpan.Zero);

        var evt1 = new ConsentWithdrawnEvent("user-1", ConsentPurposes.Marketing, occurredAt);
        var evt2 = new ConsentWithdrawnEvent("user-1", ConsentPurposes.Analytics, occurredAt);

        // Assert
        evt1.Should().NotBe(evt2);
    }
}
