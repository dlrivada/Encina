using Encina.Compliance.PrivacyByDesign;

using Shouldly;

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
        attribute.Purpose.ShouldBe("Order Processing");
    }

    [Fact]
    public void Constructor_WithDifferentPurpose_ShouldStorePurpose()
    {
        // Act
        var attribute = new PurposeLimitationAttribute("Marketing Analytics");

        // Assert
        attribute.Purpose.ShouldBe("Marketing Analytics");
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
        usage.ValidOn.ShouldBe(AttributeTargets.Property);
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
        usage.AllowMultiple.ShouldBeFalse();
    }
}
