using Encina.Compliance.DataResidency.Attributes;

using FluentAssertions;

namespace Encina.UnitTests.Compliance.DataResidency;

public class NoCrossBorderTransferAttributeTests
{
    [Fact]
    public void Constructor_ShouldCreateInstanceWithDefaults()
    {
        // Act
        var attr = new NoCrossBorderTransferAttribute();

        // Assert
        attr.DataCategory.Should().BeNull();
        attr.Reason.Should().BeNull();
    }

    [Fact]
    public void DataCategory_WhenSet_ShouldReturnValue()
    {
        // Act
        var attr = new NoCrossBorderTransferAttribute { DataCategory = "personal-data" };

        // Assert
        attr.DataCategory.Should().Be("personal-data");
    }

    [Fact]
    public void Reason_WhenSet_ShouldReturnValue()
    {
        // Act
        var attr = new NoCrossBorderTransferAttribute { Reason = "Data sovereignty requirement" };

        // Assert
        attr.Reason.Should().Be("Data sovereignty requirement");
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
        attributeUsage.Should().NotBeNull();
        attributeUsage!.ValidOn.Should().HaveFlag(AttributeTargets.Class);
    }
}
