using Encina.Compliance.PrivacyByDesign;

using FluentAssertions;

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
        attribute.DefaultValue.Should().BeNull();
    }

    [Fact]
    public void Constructor_WithBoolFalse_ShouldStoreValue()
    {
        // Act
        var attribute = new PrivacyDefaultAttribute(false);

        // Assert
        attribute.DefaultValue.Should().Be(false);
    }

    [Fact]
    public void Constructor_WithBoolTrue_ShouldStoreValue()
    {
        // Act
        var attribute = new PrivacyDefaultAttribute(true);

        // Assert
        attribute.DefaultValue.Should().Be(true);
    }

    [Fact]
    public void Constructor_WithString_ShouldStoreValue()
    {
        // Act
        var attribute = new PrivacyDefaultAttribute("default-value");

        // Assert
        attribute.DefaultValue.Should().Be("default-value");
    }

    [Fact]
    public void Constructor_WithInt_ShouldStoreValue()
    {
        // Act
        var attribute = new PrivacyDefaultAttribute(30);

        // Assert
        attribute.DefaultValue.Should().Be(30);
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
        usage.ValidOn.Should().Be(AttributeTargets.Property);
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
        usage.AllowMultiple.Should().BeFalse();
    }
}
