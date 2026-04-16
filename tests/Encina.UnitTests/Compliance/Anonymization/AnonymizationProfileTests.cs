using Encina.Compliance.Anonymization.Model;
using Shouldly;

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
        profile.Id.ShouldNotBeNullOrEmpty();
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
        profile.Name.ShouldBe("test-profile");
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
        profile.FieldRules.Count.ShouldBe(2);
        profile.FieldRules.ShouldBe(rules);
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
        profile.Description.ShouldBe("Profile for analytics data export");
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
        profile.Description.ShouldBeNull();
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
        profile.CreatedAtUtc.ShouldBeGreaterThanOrEqualTo(before);
        profile.CreatedAtUtc.ShouldBeLessThanOrEqualTo(after);
    }

    #endregion
}
