using Encina.Compliance.PrivacyByDesign;

using Shouldly;

namespace Encina.UnitTests.Compliance.PrivacyByDesign;

/// <summary>
/// Unit tests for <see cref="EnforceDataMinimizationAttribute"/>.
/// </summary>
public class EnforceDataMinimizationAttributeTests
{
    [Fact]
    public void DefaultConstructor_ShouldSetPurposeToNull()
    {
        // Act
        var attribute = new EnforceDataMinimizationAttribute();

        // Assert
        attribute.Purpose.ShouldBeNull();
    }

    [Fact]
    public void Purpose_WhenSet_ShouldStoreValue()
    {
        // Arrange
        var attribute = new EnforceDataMinimizationAttribute();

        // Act
        attribute.Purpose = "Order Processing";

        // Assert
        attribute.Purpose.ShouldBe("Order Processing");
    }

    [Fact]
    public void AttributeUsage_ShouldTargetClassOnly()
    {
        // Arrange
        var usage = typeof(EnforceDataMinimizationAttribute)
            .GetCustomAttributes(typeof(AttributeUsageAttribute), false)
            .Cast<AttributeUsageAttribute>()
            .Single();

        // Assert
        usage.ValidOn.ShouldBe(AttributeTargets.Class);
    }

    [Fact]
    public void AttributeUsage_ShouldNotAllowMultiple()
    {
        // Arrange
        var usage = typeof(EnforceDataMinimizationAttribute)
            .GetCustomAttributes(typeof(AttributeUsageAttribute), false)
            .Cast<AttributeUsageAttribute>()
            .Single();

        // Assert
        usage.AllowMultiple.ShouldBeFalse();
    }

    [Fact]
    public void AttributeUsage_ShouldBeInherited()
    {
        // Arrange
        var usage = typeof(EnforceDataMinimizationAttribute)
            .GetCustomAttributes(typeof(AttributeUsageAttribute), false)
            .Cast<AttributeUsageAttribute>()
            .Single();

        // Assert
        usage.Inherited.ShouldBeTrue();
    }
}
