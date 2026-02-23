using Encina.Compliance.Consent;
using FluentAssertions;

namespace Encina.UnitTests.Compliance.Consent.Events;

/// <summary>
/// Unit tests for <see cref="ConsentVersionChangedEvent"/>.
/// </summary>
public class ConsentVersionChangedEventTests
{
    [Fact]
    public void Constructor_AllProperties_ShouldCreateInstance()
    {
        // Arrange
        var occurredAt = new DateTimeOffset(2026, 2, 23, 12, 0, 0, TimeSpan.Zero);

        // Act
        var evt = new ConsentVersionChangedEvent(
            Purpose: ConsentPurposes.Marketing,
            OccurredAtUtc: occurredAt,
            NewVersionId: "v3",
            RequiresExplicitReconsent: true);

        // Assert
        evt.Purpose.Should().Be(ConsentPurposes.Marketing);
        evt.OccurredAtUtc.Should().Be(occurredAt);
        evt.NewVersionId.Should().Be("v3");
        evt.RequiresExplicitReconsent.Should().BeTrue();
    }

    [Fact]
    public void Constructor_NoReconsent_ShouldBeFalse()
    {
        // Act
        var evt = new ConsentVersionChangedEvent(
            Purpose: ConsentPurposes.Analytics,
            OccurredAtUtc: DateTimeOffset.UtcNow,
            NewVersionId: "v2",
            RequiresExplicitReconsent: false);

        // Assert
        evt.RequiresExplicitReconsent.Should().BeFalse();
    }

    [Fact]
    public void ShouldImplementINotification()
    {
        // Act
        var evt = new ConsentVersionChangedEvent(
            ConsentPurposes.Marketing, DateTimeOffset.UtcNow, "v1", false);

        // Assert
        evt.Should().BeAssignableTo<INotification>();
    }

    [Fact]
    public void Equality_SameValues_ShouldBeEqual()
    {
        // Arrange
        var occurredAt = new DateTimeOffset(2026, 2, 23, 12, 0, 0, TimeSpan.Zero);

        var evt1 = new ConsentVersionChangedEvent(ConsentPurposes.Marketing, occurredAt, "v3", true);
        var evt2 = new ConsentVersionChangedEvent(ConsentPurposes.Marketing, occurredAt, "v3", true);

        // Assert
        evt1.Should().Be(evt2);
    }

    [Fact]
    public void Equality_DifferentValues_ShouldNotBeEqual()
    {
        // Arrange
        var occurredAt = new DateTimeOffset(2026, 2, 23, 12, 0, 0, TimeSpan.Zero);

        var evt1 = new ConsentVersionChangedEvent(ConsentPurposes.Marketing, occurredAt, "v3", true);
        var evt2 = new ConsentVersionChangedEvent(ConsentPurposes.Marketing, occurredAt, "v3", false);

        // Assert
        evt1.Should().NotBe(evt2);
    }
}
