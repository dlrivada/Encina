using Encina.AspNetCore.Authorization;
using Shouldly;
using Xunit;

namespace Encina.UnitTests.AspNetCore.Authorization;

public class ResourceAuthorizeAttributeTests
{
    [Fact]
    public void Constructor_ValidPolicy_SetsProperty()
    {
        // Arrange & Act
        var attribute = new ResourceAuthorizeAttribute("CanEditOrder");

        // Assert
        attribute.Policy.ShouldBe("CanEditOrder");
    }

    [Fact]
    public void Constructor_NullPolicy_ThrowsArgumentException()
    {
        // Act & Assert
        Should.Throw<ArgumentException>(() => new ResourceAuthorizeAttribute(null!));
    }

    [Fact]
    public void Constructor_EmptyPolicy_ThrowsArgumentException()
    {
        // Act & Assert
        Should.Throw<ArgumentException>(() => new ResourceAuthorizeAttribute(""));
    }

    [Fact]
    public void Constructor_WhitespacePolicy_ThrowsArgumentException()
    {
        // Act & Assert
        Should.Throw<ArgumentException>(() => new ResourceAuthorizeAttribute("   "));
    }

    [Fact]
    public void AttributeUsage_TargetsClassOnly()
    {
        // Arrange
        var attributeUsage = (AttributeUsageAttribute)Attribute.GetCustomAttribute(
            typeof(ResourceAuthorizeAttribute),
            typeof(AttributeUsageAttribute))!;

        // Assert
        attributeUsage.ShouldNotBeNull();
        attributeUsage.ValidOn.ShouldBe(AttributeTargets.Class);
    }

    [Fact]
    public void AttributeUsage_DoesNotAllowMultiple()
    {
        // Arrange
        var attributeUsage = (AttributeUsageAttribute)Attribute.GetCustomAttribute(
            typeof(ResourceAuthorizeAttribute),
            typeof(AttributeUsageAttribute))!;

        // Assert
        attributeUsage.AllowMultiple.ShouldBeFalse();
    }

    [Fact]
    public void AttributeUsage_IsInherited()
    {
        // Arrange
        var attributeUsage = (AttributeUsageAttribute)Attribute.GetCustomAttribute(
            typeof(ResourceAuthorizeAttribute),
            typeof(AttributeUsageAttribute))!;

        // Assert
        attributeUsage.Inherited.ShouldBeTrue();
    }

    [Fact]
    public void Attribute_CanBeAppliedToRecordType()
    {
        // Arrange & Act
        var attribute = typeof(TestCommand)
            .GetCustomAttributes(typeof(ResourceAuthorizeAttribute), inherit: true)
            .Cast<ResourceAuthorizeAttribute>()
            .FirstOrDefault();

        // Assert
        attribute.ShouldNotBeNull();
        attribute.Policy.ShouldBe("TestPolicy");
    }

    [Fact]
    public void Attribute_IsInheritedByDerivedTypes()
    {
        // Arrange & Act
        var attribute = typeof(DerivedCommand)
            .GetCustomAttributes(typeof(ResourceAuthorizeAttribute), inherit: true)
            .Cast<ResourceAuthorizeAttribute>()
            .FirstOrDefault();

        // Assert
        attribute.ShouldNotBeNull();
        attribute.Policy.ShouldBe("BasePolicy");
    }

    // Test types
    [ResourceAuthorize("TestPolicy")]
    private sealed record TestCommand;

    [ResourceAuthorize("BasePolicy")]
    private record BaseCommand;

    private sealed record DerivedCommand : BaseCommand;
}
