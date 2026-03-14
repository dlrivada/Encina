using Encina.Compliance.PrivacyByDesign;

using FluentAssertions;

namespace Encina.UnitTests.Compliance.PrivacyByDesign;

/// <summary>
/// Unit tests for <see cref="PurposeLimitationAttribute"/>.
/// </summary>
public class PurposeLimitationAttributeTests
{
    [Fact]
    public void Constructor_ShouldStorePurpose()
    {
        // Act
        var attribute = new PurposeLimitationAttribute("Order Processing");

        // Assert
        attribute.Purpose.Should().Be("Order Processing");
    }

    [Fact]
    public void Constructor_WithDifferentPurpose_ShouldStorePurpose()
    {
        // Act
        var attribute = new PurposeLimitationAttribute("Marketing Analytics");

        // Assert
        attribute.Purpose.Should().Be("Marketing Analytics");
    }

    [Fact]
    public void AttributeUsage_ShouldTargetPropertyOnly()
    {
        // Arrange
        var usage = typeof(PurposeLimitationAttribute)
            .GetCustomAttributes(typeof(AttributeUsageAttribute), false)
            .Cast<AttributeUsageAttribute>()
            .Single();

        // Assert
        usage.ValidOn.Should().Be(AttributeTargets.Property);
    }

    [Fact]
    public void AttributeUsage_ShouldNotAllowMultiple()
    {
        // Arrange
        var usage = typeof(PurposeLimitationAttribute)
            .GetCustomAttributes(typeof(AttributeUsageAttribute), false)
            .Cast<AttributeUsageAttribute>()
            .Single();

        // Assert
        usage.AllowMultiple.Should().BeFalse();
    }
}
