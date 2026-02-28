using Encina.Compliance.Anonymization.Model;
using FluentAssertions;

namespace Encina.UnitTests.Compliance.Anonymization;

/// <summary>
/// Unit tests for <see cref="AnonymizationProfile"/> factory method and record behavior.
/// </summary>
public class AnonymizationProfileTests
{
    #region Create Factory Method Tests

    [Fact]
    public void Create_ShouldGenerateNonEmptyId()
    {
        // Arrange
        var rules = new List<FieldAnonymizationRule>
        {
            new() { FieldName = "Name", Technique = AnonymizationTechnique.Suppression }
        };

        // Act
        var profile = AnonymizationProfile.Create("test-profile", rules);

        // Assert
        profile.Id.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void Create_ShouldSetName()
    {
        // Arrange
        var rules = new List<FieldAnonymizationRule>
        {
            new() { FieldName = "Name", Technique = AnonymizationTechnique.Suppression }
        };

        // Act
        var profile = AnonymizationProfile.Create("test-profile", rules);

        // Assert
        profile.Name.Should().Be("test-profile");
    }

    [Fact]
    public void Create_ShouldSetFieldRules()
    {
        // Arrange
        var rules = new List<FieldAnonymizationRule>
        {
            new() { FieldName = "Name", Technique = AnonymizationTechnique.Suppression },
            new() { FieldName = "Age", Technique = AnonymizationTechnique.Generalization }
        };

        // Act
        var profile = AnonymizationProfile.Create("test-profile", rules);

        // Assert
        profile.FieldRules.Should().HaveCount(2);
        profile.FieldRules.Should().BeEquivalentTo(rules);
    }

    [Fact]
    public void Create_WithDescription_ShouldSetDescription()
    {
        // Arrange
        var rules = new List<FieldAnonymizationRule>
        {
            new() { FieldName = "Name", Technique = AnonymizationTechnique.Suppression }
        };

        // Act
        var profile = AnonymizationProfile.Create(
            "analytics-export",
            rules,
            description: "Profile for analytics data export");

        // Assert
        profile.Description.Should().Be("Profile for analytics data export");
    }

    [Fact]
    public void Create_WithoutDescription_DescriptionShouldBeNull()
    {
        // Arrange
        var rules = new List<FieldAnonymizationRule>
        {
            new() { FieldName = "Name", Technique = AnonymizationTechnique.Suppression }
        };

        // Act
        var profile = AnonymizationProfile.Create("test-profile", rules);

        // Assert
        profile.Description.Should().BeNull();
    }

    [Fact]
    public void Create_ShouldSetCreatedAtUtcToNow()
    {
        // Arrange
        var rules = new List<FieldAnonymizationRule>
        {
            new() { FieldName = "Name", Technique = AnonymizationTechnique.Suppression }
        };
        var before = DateTimeOffset.UtcNow;

        // Act
        var profile = AnonymizationProfile.Create("test-profile", rules);

        var after = DateTimeOffset.UtcNow;

        // Assert
        profile.CreatedAtUtc.Should().BeOnOrAfter(before);
        profile.CreatedAtUtc.Should().BeOnOrBefore(after);
    }

    #endregion
}
