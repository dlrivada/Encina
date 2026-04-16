using Encina.Compliance.DataResidency.Attributes;

using Shouldly;

namespace Encina.UnitTests.Compliance.DataResidency;

public class NoCrossBorderTransferAttributeTests
{
    [Fact]
    public void Constructor_ShouldCreateInstanceWithDefaults()
    {
        // Act
        var attr = new NoCrossBorderTransferAttribute();

        // Assert
        attr.DataCategory.ShouldBeNull();
        attr.Reason.ShouldBeNull();
    }

    [Fact]
    public void DataCategory_WhenSet_ShouldReturnValue()
    {
        // Act
        var attr = new NoCrossBorderTransferAttribute { DataCategory = "personal-data" };

        // Assert
        attr.DataCategory.ShouldBe("personal-data");
    }

    [Fact]
    public void Reason_WhenSet_ShouldReturnValue()
    {
        // Act
        var attr = new NoCrossBorderTransferAttribute { Reason = "Data sovereignty requirement" };

        // Assert
        attr.Reason.ShouldBe("Data sovereignty requirement");
    }

    [Fact]
    public void Attribute_ShouldTargetClassOnly()
    {
        // Arrange
        var attributeUsage = typeof(NoCrossBorderTransferAttribute)
            .GetCustomAttributes(typeof(AttributeUsageAttribute), false)
            .Cast<AttributeUsageAttribute>()
            .FirstOrDefault();

        // Assert
        attributeUsage.ShouldNotBeNull();
        (attributeUsage!.ValidOn & AttributeTargets.Class).ShouldBe(AttributeTargets.Class);
    }
}
