using Encina.Compliance.Consent;
using FluentAssertions;

namespace Encina.UnitTests.Compliance.Consent.Model;

/// <summary>
/// Unit tests for <see cref="ConsentAuditEntry"/> and <see cref="ConsentAuditAction"/>.
/// </summary>
public class ConsentAuditEntryTests
{
    #region ConsentAuditAction Enum

    [Fact]
    public void ConsentAuditAction_ShouldHaveFourValues()
    {
        Enum.GetValues<ConsentAuditAction>().Should().HaveCount(4);
    }

    [Theory]
    [InlineData(ConsentAuditAction.Granted, 0)]
    [InlineData(ConsentAuditAction.Withdrawn, 1)]
    [InlineData(ConsentAuditAction.Expired, 2)]
    [InlineData(ConsentAuditAction.VersionChanged, 3)]
    public void ConsentAuditAction_ShouldHaveExpectedIntValues(ConsentAuditAction action, int expected)
    {
        ((int)action).Should().Be(expected);
    }

    #endregion

    #region ConsentAuditEntry Record

    [Fact]
    public void ConsentAuditEntry_WithAllProperties_ShouldCreateInstance()
    {
        // Arrange
        var id = Guid.NewGuid();
        var occurredAt = DateTimeOffset.UtcNow;

        // Act
        var entry = new ConsentAuditEntry
        {
            Id = id,
            SubjectId = "user-123",
            Purpose = ConsentPurposes.Marketing,
            Action = ConsentAuditAction.Granted,
            OccurredAtUtc = occurredAt,
            PerformedBy = "user-123",
            IpAddress = "10.0.0.1",
            Metadata = new Dictionary<string, object?> { ["source"] = "web-form" }
        };

        // Assert
        entry.Id.Should().Be(id);
        entry.SubjectId.Should().Be("user-123");
        entry.Purpose.Should().Be(ConsentPurposes.Marketing);
        entry.Action.Should().Be(ConsentAuditAction.Granted);
        entry.OccurredAtUtc.Should().Be(occurredAt);
        entry.PerformedBy.Should().Be("user-123");
        entry.IpAddress.Should().Be("10.0.0.1");
        entry.Metadata.Should().ContainKey("source");
    }

    [Fact]
    public void ConsentAuditEntry_IpAddress_ShouldBeOptional()
    {
        // Arrange & Act
        var entry = new ConsentAuditEntry
        {
            Id = Guid.NewGuid(),
            SubjectId = "user-456",
            Purpose = ConsentPurposes.Analytics,
            Action = ConsentAuditAction.Withdrawn,
            OccurredAtUtc = DateTimeOffset.UtcNow,
            PerformedBy = "admin",
            Metadata = new Dictionary<string, object?>()
        };

        // Assert
        entry.IpAddress.Should().BeNull();
    }

    #endregion

    #region ConsentPurposes Constants

    [Fact]
    public void ConsentPurposes_ShouldHaveStandardConstants()
    {
        ConsentPurposes.Marketing.Should().Be("marketing");
        ConsentPurposes.Analytics.Should().Be("analytics");
        ConsentPurposes.Personalization.Should().Be("personalization");
        ConsentPurposes.ThirdPartySharing.Should().Be("third-party-sharing");
        ConsentPurposes.Profiling.Should().Be("profiling");
        ConsentPurposes.Newsletter.Should().Be("newsletter");
        ConsentPurposes.LocationTracking.Should().Be("location-tracking");
        ConsentPurposes.CrossBorderTransfer.Should().Be("cross-border-transfer");
    }

    #endregion
}
