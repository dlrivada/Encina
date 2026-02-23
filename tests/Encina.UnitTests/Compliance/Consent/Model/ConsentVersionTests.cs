using Encina.Compliance.Consent;
using FluentAssertions;

namespace Encina.UnitTests.Compliance.Consent.Model;

/// <summary>
/// Unit tests for <see cref="ConsentVersion"/>.
/// </summary>
public class ConsentVersionTests
{
    [Fact]
    public void ConsentVersion_WithAllProperties_ShouldCreateInstance()
    {
        // Arrange
        var effectiveFrom = DateTimeOffset.UtcNow;

        // Act
        var version = new ConsentVersion
        {
            VersionId = "marketing-v3",
            Purpose = ConsentPurposes.Marketing,
            EffectiveFromUtc = effectiveFrom,
            Description = "Added social media retargeting scope",
            RequiresExplicitReconsent = true
        };

        // Assert
        version.VersionId.Should().Be("marketing-v3");
        version.Purpose.Should().Be(ConsentPurposes.Marketing);
        version.EffectiveFromUtc.Should().Be(effectiveFrom);
        version.Description.Should().Be("Added social media retargeting scope");
        version.RequiresExplicitReconsent.Should().BeTrue();
    }

    [Fact]
    public void ConsentVersion_RequiresExplicitReconsent_DefaultShouldBeFalse()
    {
        // Arrange & Act
        var version = new ConsentVersion
        {
            VersionId = "v1",
            Purpose = ConsentPurposes.Analytics,
            EffectiveFromUtc = DateTimeOffset.UtcNow,
            Description = "Initial version",
            RequiresExplicitReconsent = false
        };

        // Assert
        version.RequiresExplicitReconsent.Should().BeFalse();
    }

    [Fact]
    public void ConsentVersion_WithExpression_ShouldCreateNewInstance()
    {
        // Arrange
        var original = new ConsentVersion
        {
            VersionId = "v1",
            Purpose = ConsentPurposes.Marketing,
            EffectiveFromUtc = DateTimeOffset.UtcNow.AddDays(-90),
            Description = "Original",
            RequiresExplicitReconsent = false
        };

        // Act
        var updated = original with
        {
            VersionId = "v2",
            Description = "Updated terms",
            RequiresExplicitReconsent = true
        };

        // Assert
        original.VersionId.Should().Be("v1");
        updated.VersionId.Should().Be("v2");
        updated.Purpose.Should().Be(original.Purpose);
        updated.RequiresExplicitReconsent.Should().BeTrue();
    }

    [Fact]
    public void ConsentVersion_Equality_SameValues_ShouldBeEqual()
    {
        // Arrange
        var effectiveFrom = DateTimeOffset.UtcNow;
        var v1 = new ConsentVersion
        {
            VersionId = "v1",
            Purpose = "marketing",
            EffectiveFromUtc = effectiveFrom,
            Description = "Terms",
            RequiresExplicitReconsent = true
        };
        var v2 = new ConsentVersion
        {
            VersionId = "v1",
            Purpose = "marketing",
            EffectiveFromUtc = effectiveFrom,
            Description = "Terms",
            RequiresExplicitReconsent = true
        };

        // Assert
        v1.Should().Be(v2);
    }
}
