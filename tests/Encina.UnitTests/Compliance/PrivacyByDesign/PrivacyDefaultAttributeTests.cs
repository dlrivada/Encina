using Encina.Compliance.PrivacyByDesign;

using Shouldly;

namespace Encina.UnitTests.Compliance.PrivacyByDesign;

/// <summary>
/// Unit tests for <see cref="PrivacyDefaultAttribute"/>.
/// </summary>
public class PrivacyDefaultAttributeTests
{
    [Fact]
    public void Constructor_WithNull_ShouldStoreNull()
    {
        // Act
        var attribute = new PrivacyDefaultAttribute(null);

        // Assert
        attribute.DefaultValue.ShouldBeNull();
    }

    [Fact]
    public void Constructor_WithBoolFalse_ShouldStoreValue()
    {
        // Act
        var attribute = new PrivacyDefaultAttribute(false);

        // Assert
        attribute.DefaultValue.ShouldBe(false);
    }

    [Fact]
    public void Constructor_WithBoolTrue_ShouldStoreValue()
    {
        // Act
        var attribute = new PrivacyDefaultAttribute(true);

        // Assert
        attribute.DefaultValue.ShouldBe(true);
    }

    [Fact]
    public void Constructor_WithString_ShouldStoreValue()
    {
        // Act
        var attribute = new PrivacyDefaultAttribute("default-value");

        // Assert
        attribute.DefaultValue.ShouldBe("default-value");
    }

    [Fact]
    public void Constructor_WithInt_ShouldStoreValue()
    {
        // Act
        var attribute = new PrivacyDefaultAttribute(30);

        // Assert
        attribute.DefaultValue.ShouldBe(30);
    }

    [Fact]
    public void AttributeUsage_ShouldTargetPropertyOnly()
    {
        // Arrange
        var usage = typeof(PrivacyDefaultAttribute)
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
        var usage = typeof(PrivacyDefaultAttribute)
            .GetCustomAttributes(typeof(AttributeUsageAttribute), false)
            .Cast<AttributeUsageAttribute>()
            .Single();

        // Assert
        usage.AllowMultiple.ShouldBeFalse();
    }
}
