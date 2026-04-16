using Encina.Compliance.DataResidency.Attributes;

using Shouldly;

namespace Encina.UnitTests.Compliance.DataResidency;

public class DataResidencyAttributeTests
{
    [Fact]
    public void Constructor_WithRegionCodes_ShouldSetAllowedRegionCodes()
    {
        // Act
        var attr = new DataResidencyAttribute("DE", "FR", "IT");

        // Assert
        attr.AllowedRegionCodes.Length.ShouldBe(3);
        attr.AllowedRegionCodes.ShouldContain("DE");
        attr.AllowedRegionCodes.ShouldContain("FR");
        attr.AllowedRegionCodes.ShouldContain("IT");
    }

    [Fact]
    public void Constructor_WithSingleRegionCode_ShouldSetSingleCode()
    {
        // Act
        var attr = new DataResidencyAttribute("DE");

        // Assert
        attr.AllowedRegionCodes.ShouldHaveSingleItem().ShouldBe("DE");
    }

    [Fact]
    public void DataCategory_ShouldDefaultToNull()
    {
        // Act
        var attr = new DataResidencyAttribute("DE");

        // Assert
        attr.DataCategory.ShouldBeNull();
    }

    [Fact]
    public void DataCategory_WhenSet_ShouldReturnValue()
    {
        // Act
        var attr = new DataResidencyAttribute("DE") { DataCategory = "healthcare-data" };

        // Assert
        attr.DataCategory.ShouldBe("healthcare-data");
    }

    [Fact]
    public void RequireAdequacyDecision_ShouldDefaultToFalse()
    {
        // Act
        var attr = new DataResidencyAttribute("DE");

        // Assert
        attr.RequireAdequacyDecision.ShouldBeFalse();
    }

    [Fact]
    public void RequireAdequacyDecision_WhenSet_ShouldReturnTrue()
    {
        // Act
        var attr = new DataResidencyAttribute("DE") { RequireAdequacyDecision = true };

        // Assert
        attr.RequireAdequacyDecision.ShouldBeTrue();
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
        attributeUsage.ShouldNotBeNull();
        (attributeUsage!.ValidOn & AttributeTargets.Class).ShouldBe(AttributeTargets.Class);
    }
}
