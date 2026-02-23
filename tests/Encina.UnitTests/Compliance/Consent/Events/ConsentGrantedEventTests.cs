using Encina.Compliance.Consent;
using FluentAssertions;

namespace Encina.UnitTests.Compliance.Consent.Events;

/// <summary>
/// Unit tests for <see cref="ConsentGrantedEvent"/>.
/// </summary>
public class ConsentGrantedEventTests
{
    [Fact]
    public void Constructor_AllProperties_ShouldCreateInstance()
    {
        // Arrange
        var occurredAt = new DateTimeOffset(2026, 2, 23, 12, 0, 0, TimeSpan.Zero);
        var expiresAt = occurredAt.AddDays(365);

        // Act
        var evt = new ConsentGrantedEvent(
            SubjectId: "user-123",
            Purpose: ConsentPurposes.Marketing,
            OccurredAtUtc: occurredAt,
            ConsentVersionId: "v1",
            Source: "web-form",
            ExpiresAtUtc: expiresAt);

        // Assert
        evt.SubjectId.Should().Be("user-123");
        evt.Purpose.Should().Be(ConsentPurposes.Marketing);
        evt.OccurredAtUtc.Should().Be(occurredAt);
        evt.ConsentVersionId.Should().Be("v1");
        evt.Source.Should().Be("web-form");
        evt.ExpiresAtUtc.Should().Be(expiresAt);
    }

    [Fact]
    public void Constructor_NullExpiresAt_ShouldBeNull()
    {
        // Act
        var evt = new ConsentGrantedEvent(
            SubjectId: "user-456",
            Purpose: ConsentPurposes.Analytics,
            OccurredAtUtc: DateTimeOffset.UtcNow,
            ConsentVersionId: "v2",
            Source: "api",
            ExpiresAtUtc: null);

        // Assert
        evt.ExpiresAtUtc.Should().BeNull();
    }

    [Fact]
    public void ShouldImplementINotification()
    {
        // Act
        var evt = new ConsentGrantedEvent(
            "user-1", ConsentPurposes.Marketing, DateTimeOffset.UtcNow, "v1", "test", null);

        // Assert
        evt.Should().BeAssignableTo<INotification>();
    }

    [Fact]
    public void Equality_SameValues_ShouldBeEqual()
    {
        // Arrange
        var occurredAt = new DateTimeOffset(2026, 2, 23, 12, 0, 0, TimeSpan.Zero);

        var evt1 = new ConsentGrantedEvent("user-1", ConsentPurposes.Marketing, occurredAt, "v1", "web", null);
        var evt2 = new ConsentGrantedEvent("user-1", ConsentPurposes.Marketing, occurredAt, "v1", "web", null);

        // Assert
        evt1.Should().Be(evt2);
    }

    [Fact]
    public void Equality_DifferentValues_ShouldNotBeEqual()
    {
        // Arrange
        var occurredAt = new DateTimeOffset(2026, 2, 23, 12, 0, 0, TimeSpan.Zero);

        var evt1 = new ConsentGrantedEvent("user-1", ConsentPurposes.Marketing, occurredAt, "v1", "web", null);
        var evt2 = new ConsentGrantedEvent("user-2", ConsentPurposes.Marketing, occurredAt, "v1", "web", null);

        // Assert
        evt1.Should().NotBe(evt2);
    }
}
