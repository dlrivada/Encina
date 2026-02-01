using System.Reflection;
using Encina.Security.Audit;
using FluentAssertions;

namespace Encina.UnitTests.Security.Audit;

/// <summary>
/// Unit tests for <see cref="AuditableAttribute"/>.
/// </summary>
public class AuditableAttributeTests
{
    [Fact]
    public void DefaultValues_ShouldBeNull()
    {
        // Act
        var attribute = new AuditableAttribute();

        // Assert
        attribute.EntityType.Should().BeNull();
        attribute.Action.Should().BeNull();
        attribute.IncludePayload.Should().BeNull();
        attribute.SensitivityLevel.Should().BeNull();
        attribute.Skip.Should().BeFalse();
    }

    [Fact]
    public void Properties_ShouldBeSettable()
    {
        // Act
        var attribute = new AuditableAttribute
        {
            EntityType = "Order",
            Action = "Create",
            IncludePayloadValue = true,
            SensitivityLevel = "High",
            Skip = false
        };

        // Assert
        attribute.EntityType.Should().Be("Order");
        attribute.Action.Should().Be("Create");
        attribute.IncludePayload.Should().BeTrue();
        attribute.IncludePayloadValue.Should().BeTrue();
        attribute.SensitivityLevel.Should().Be("High");
        attribute.Skip.Should().BeFalse();
    }

    [Fact]
    public void Skip_WhenSetToTrue_ShouldPreventsAuditing()
    {
        // Act
        var attribute = new AuditableAttribute { Skip = true };

        // Assert
        attribute.Skip.Should().BeTrue();
    }

    [Fact]
    public void Attribute_ShouldBeApplicableToClasses()
    {
        // Arrange
        var attributeUsage = typeof(AuditableAttribute).GetCustomAttribute<AttributeUsageAttribute>();

        // Assert
        attributeUsage.Should().NotBeNull();
        attributeUsage!.ValidOn.Should().HaveFlag(AttributeTargets.Class);
    }

    [Fact]
    public void Attribute_ShouldNotAllowMultiple()
    {
        // Arrange
        var attributeUsage = typeof(AuditableAttribute).GetCustomAttribute<AttributeUsageAttribute>();

        // Assert
        attributeUsage.Should().NotBeNull();
        attributeUsage!.AllowMultiple.Should().BeFalse();
    }

    [Fact]
    public void Attribute_ShouldNotBeInherited()
    {
        // Arrange
        var attributeUsage = typeof(AuditableAttribute).GetCustomAttribute<AttributeUsageAttribute>();

        // Assert
        attributeUsage.Should().NotBeNull();
        attributeUsage!.Inherited.Should().BeFalse();
    }

    [Fact]
    public void Attribute_CanBeRetrievedFromDecoratedClass()
    {
        // Act
        var attribute = typeof(TestAuditableCommand).GetCustomAttribute<AuditableAttribute>();

        // Assert
        attribute.Should().NotBeNull();
        attribute!.EntityType.Should().Be("TestEntity");
        attribute.Action.Should().Be("TestAction");
    }

    [Fact]
    public void Attribute_WithSkip_CanBeRetrievedFromDecoratedClass()
    {
        // Act
        var attribute = typeof(SkippedAuditCommand).GetCustomAttribute<AuditableAttribute>();

        // Assert
        attribute.Should().NotBeNull();
        attribute!.Skip.Should().BeTrue();
    }

    [Fact]
    public void IncludePayload_WhenNull_ShouldIndicateUseDefault()
    {
        // Act
        var attribute = new AuditableAttribute();

        // Assert - null means "use the default from AuditOptions"
        attribute.IncludePayload.Should().BeNull();
    }

    [Fact]
    public void IncludePayload_WhenFalse_ShouldExplicitlyDisable()
    {
        // Act
        var attribute = new AuditableAttribute { IncludePayloadValue = false };

        // Assert
        attribute.IncludePayload.Should().BeFalse();
        attribute.IncludePayloadValue.Should().BeFalse();
    }

    [Fact]
    public void IncludePayloadValue_ShouldBeUsableInAttributes()
    {
        // Act
        var attribute = typeof(NoPayloadCommand).GetCustomAttribute<AuditableAttribute>();

        // Assert
        attribute.Should().NotBeNull();
        attribute!.IncludePayload.Should().BeFalse();
    }

    [Auditable(IncludePayloadValue = false)]
    private sealed class NoPayloadCommand { }

    [Auditable(EntityType = "TestEntity", Action = "TestAction")]
    private sealed class TestAuditableCommand { }

    [Auditable(Skip = true)]
    private sealed class SkippedAuditCommand { }
}
