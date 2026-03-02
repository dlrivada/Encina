using Encina.Compliance.DataResidency.Attributes;

using FluentAssertions;

namespace Encina.UnitTests.Compliance.DataResidency;

public class DataResidencyAttributeTests
{
    [Fact]
    public void Constructor_WithRegionCodes_ShouldSetAllowedRegionCodes()
    {
        // Act
        var attr = new DataResidencyAttribute("DE", "FR", "IT");

        // Assert
        attr.AllowedRegionCodes.Should().HaveCount(3);
        attr.AllowedRegionCodes.Should().Contain("DE");
        attr.AllowedRegionCodes.Should().Contain("FR");
        attr.AllowedRegionCodes.Should().Contain("IT");
    }

    [Fact]
    public void Constructor_WithSingleRegionCode_ShouldSetSingleCode()
    {
        // Act
        var attr = new DataResidencyAttribute("DE");

        // Assert
        attr.AllowedRegionCodes.Should().ContainSingle().Which.Should().Be("DE");
    }

    [Fact]
    public void DataCategory_ShouldDefaultToNull()
    {
        // Act
        var attr = new DataResidencyAttribute("DE");

        // Assert
        attr.DataCategory.Should().BeNull();
    }

    [Fact]
    public void DataCategory_WhenSet_ShouldReturnValue()
    {
        // Act
        var attr = new DataResidencyAttribute("DE") { DataCategory = "healthcare-data" };

        // Assert
        attr.DataCategory.Should().Be("healthcare-data");
    }

    [Fact]
    public void RequireAdequacyDecision_ShouldDefaultToFalse()
    {
        // Act
        var attr = new DataResidencyAttribute("DE");

        // Assert
        attr.RequireAdequacyDecision.Should().BeFalse();
    }

    [Fact]
    public void RequireAdequacyDecision_WhenSet_ShouldReturnTrue()
    {
        // Act
        var attr = new DataResidencyAttribute("DE") { RequireAdequacyDecision = true };

        // Assert
        attr.RequireAdequacyDecision.Should().BeTrue();
    }

    [Fact]
    public void Attribute_ShouldTargetClassOnly()
    {
        // Arrange
        var attributeUsage = typeof(DataResidencyAttribute)
            .GetCustomAttributes(typeof(AttributeUsageAttribute), false)
            .Cast<AttributeUsageAttribute>()
            .FirstOrDefault();

        // Assert
        attributeUsage.Should().NotBeNull();
        attributeUsage!.ValidOn.Should().HaveFlag(AttributeTargets.Class);
    }
}
