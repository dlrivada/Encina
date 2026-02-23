using Encina.Compliance.Consent;
using FluentAssertions;

namespace Encina.UnitTests.Compliance.Consent.Model;

/// <summary>
/// Unit tests for <see cref="ConsentRecord"/>.
/// </summary>
public class ConsentRecordTests
{
    #region Construction Tests

    [Fact]
    public void ConsentRecord_WithAllRequiredProperties_ShouldCreateInstance()
    {
        // Arrange & Act
        var id = Guid.NewGuid();
        var givenAt = DateTimeOffset.UtcNow;
        var record = new ConsentRecord
        {
            Id = id,
            SubjectId = "user-123",
            Purpose = ConsentPurposes.Marketing,
            Status = ConsentStatus.Active,
            ConsentVersionId = "marketing-v1",
            GivenAtUtc = givenAt,
            Source = "web-form",
            Metadata = new Dictionary<string, object?>()
        };

        // Assert
        record.Id.Should().Be(id);
        record.SubjectId.Should().Be("user-123");
        record.Purpose.Should().Be(ConsentPurposes.Marketing);
        record.Status.Should().Be(ConsentStatus.Active);
        record.ConsentVersionId.Should().Be("marketing-v1");
        record.GivenAtUtc.Should().Be(givenAt);
        record.Source.Should().Be("web-form");
        record.Metadata.Should().BeEmpty();
    }

    [Fact]
    public void ConsentRecord_OptionalProperties_ShouldDefaultToNull()
    {
        // Arrange & Act
        var record = CreateActiveRecord();

        // Assert
        record.WithdrawnAtUtc.Should().BeNull();
        record.ExpiresAtUtc.Should().BeNull();
        record.IpAddress.Should().BeNull();
        record.ProofOfConsent.Should().BeNull();
    }

    [Fact]
    public void ConsentRecord_WithOptionalProperties_ShouldRetainValues()
    {
        // Arrange
        var withdrawnAt = DateTimeOffset.UtcNow.AddDays(-1);
        var expiresAt = DateTimeOffset.UtcNow.AddDays(365);

        // Act
        var record = new ConsentRecord
        {
            Id = Guid.NewGuid(),
            SubjectId = "user-456",
            Purpose = ConsentPurposes.Analytics,
            Status = ConsentStatus.Withdrawn,
            ConsentVersionId = "analytics-v2",
            GivenAtUtc = DateTimeOffset.UtcNow.AddDays(-30),
            WithdrawnAtUtc = withdrawnAt,
            ExpiresAtUtc = expiresAt,
            Source = "mobile-app",
            IpAddress = "192.168.1.1",
            ProofOfConsent = "sha256:abc123",
            Metadata = new Dictionary<string, object?> { ["browser"] = "Chrome" }
        };

        // Assert
        record.WithdrawnAtUtc.Should().Be(withdrawnAt);
        record.ExpiresAtUtc.Should().Be(expiresAt);
        record.IpAddress.Should().Be("192.168.1.1");
        record.ProofOfConsent.Should().Be("sha256:abc123");
        record.Metadata.Should().ContainKey("browser");
    }

    #endregion

    #region Record Immutability Tests

    [Fact]
    public void ConsentRecord_WithExpression_ShouldCreateNewInstance()
    {
        // Arrange
        var original = CreateActiveRecord();

        // Act
        var modified = original with { Status = ConsentStatus.Withdrawn };

        // Assert
        original.Status.Should().Be(ConsentStatus.Active);
        modified.Status.Should().Be(ConsentStatus.Withdrawn);
        modified.SubjectId.Should().Be(original.SubjectId);
    }

    [Fact]
    public void ConsentRecord_WithExpression_ShouldPreserveOtherProperties()
    {
        // Arrange
        var original = CreateActiveRecord();
        var withdrawnAt = DateTimeOffset.UtcNow;

        // Act
        var withdrawn = original with
        {
            Status = ConsentStatus.Withdrawn,
            WithdrawnAtUtc = withdrawnAt
        };

        // Assert
        withdrawn.Id.Should().Be(original.Id);
        withdrawn.SubjectId.Should().Be(original.SubjectId);
        withdrawn.Purpose.Should().Be(original.Purpose);
        withdrawn.ConsentVersionId.Should().Be(original.ConsentVersionId);
        withdrawn.GivenAtUtc.Should().Be(original.GivenAtUtc);
        withdrawn.Source.Should().Be(original.Source);
        withdrawn.Status.Should().Be(ConsentStatus.Withdrawn);
        withdrawn.WithdrawnAtUtc.Should().Be(withdrawnAt);
    }

    #endregion

    #region Equality Tests

    [Fact]
    public void ConsentRecord_SameValues_ShouldBeEqual()
    {
        // Arrange
        var id = Guid.NewGuid();
        var givenAt = DateTimeOffset.UtcNow;

        var record1 = new ConsentRecord
        {
            Id = id,
            SubjectId = "user-1",
            Purpose = "marketing",
            Status = ConsentStatus.Active,
            ConsentVersionId = "v1",
            GivenAtUtc = givenAt,
            Source = "web",
            Metadata = new Dictionary<string, object?>()
        };

        var record2 = new ConsentRecord
        {
            Id = id,
            SubjectId = "user-1",
            Purpose = "marketing",
            Status = ConsentStatus.Active,
            ConsentVersionId = "v1",
            GivenAtUtc = givenAt,
            Source = "web",
            Metadata = new Dictionary<string, object?>()
        };

        // Assert - Records use reference equality for collections
        record1.Should().NotBeSameAs(record2);
    }

    [Fact]
    public void ConsentRecord_DifferentId_ShouldNotBeEqual()
    {
        // Arrange
        var givenAt = DateTimeOffset.UtcNow;

        var record1 = new ConsentRecord
        {
            Id = Guid.NewGuid(),
            SubjectId = "user-1",
            Purpose = "marketing",
            Status = ConsentStatus.Active,
            ConsentVersionId = "v1",
            GivenAtUtc = givenAt,
            Source = "web",
            Metadata = new Dictionary<string, object?>()
        };

        var record2 = new ConsentRecord
        {
            Id = Guid.NewGuid(),
            SubjectId = "user-1",
            Purpose = "marketing",
            Status = ConsentStatus.Active,
            ConsentVersionId = "v1",
            GivenAtUtc = givenAt,
            Source = "web",
            Metadata = new Dictionary<string, object?>()
        };

        // Assert
        record1.Should().NotBe(record2);
    }

    #endregion

    #region Metadata Tests

    [Fact]
    public void ConsentRecord_Metadata_ShouldSupportMultipleValues()
    {
        // Arrange & Act
        var record = new ConsentRecord
        {
            Id = Guid.NewGuid(),
            SubjectId = "user-789",
            Purpose = ConsentPurposes.Personalization,
            Status = ConsentStatus.Active,
            ConsentVersionId = "v1",
            GivenAtUtc = DateTimeOffset.UtcNow,
            Source = "api",
            Metadata = new Dictionary<string, object?>
            {
                ["browser"] = "Firefox",
                ["version"] = "120",
                ["optionalField"] = null
            }
        };

        // Assert
        record.Metadata.Should().HaveCount(3);
        record.Metadata["browser"].Should().Be("Firefox");
        record.Metadata["version"].Should().Be("120");
        record.Metadata["optionalField"].Should().BeNull();
    }

    #endregion

    #region Helpers

    private static ConsentRecord CreateActiveRecord() => new()
    {
        Id = Guid.NewGuid(),
        SubjectId = "user-test",
        Purpose = ConsentPurposes.Marketing,
        Status = ConsentStatus.Active,
        ConsentVersionId = "marketing-v1",
        GivenAtUtc = DateTimeOffset.UtcNow,
        Source = "test",
        Metadata = new Dictionary<string, object?>()
    };

    #endregion
}
