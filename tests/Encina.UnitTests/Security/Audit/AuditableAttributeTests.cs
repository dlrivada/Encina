using System.Reflection;
using Encina.Security.Audit;
using Shouldly;

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
        attribute.EntityType.ShouldBeNull();
        attribute.Action.ShouldBeNull();
        attribute.IncludePayload.ShouldBeNull();
        attribute.SensitivityLevel.ShouldBeNull();
        attribute.Skip.ShouldBeFalse();
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
        attribute.EntityType.ShouldBe("Order");
        attribute.Action.ShouldBe("Create");
        attribute.IncludePayload.ShouldBeTrue();
        attribute.IncludePayloadValue.ShouldBeTrue();
        attribute.SensitivityLevel.ShouldBe("High");
        attribute.Skip.ShouldBeFalse();
    }

    [Fact]
    public void Skip_WhenSetToTrue_ShouldPreventsAuditing()
    {
        // Act
        var attribute = new AuditableAttribute { Skip = true };

        // Assert
        attribute.Skip.ShouldBeTrue();
    }

    [Fact]
    public void Attribute_ShouldBeApplicableToClasses()
    {
        // Arrange
        var attributeUsage = typeof(AuditableAttribute).GetCustomAttribute<AttributeUsageAttribute>();

        // Assert
        attributeUsage.ShouldNotBeNull();
        attributeUsage!.ValidOn.ShouldHaveFlag(AttributeTargets.Class);
    }

    [Fact]
    public void Attribute_ShouldNotAllowMultiple()
    {
        // Arrange
        var attributeUsage = typeof(AuditableAttribute).GetCustomAttribute<AttributeUsageAttribute>();

        // Assert
        attributeUsage.ShouldNotBeNull();
        attributeUsage!.AllowMultiple.ShouldBeFalse();
    }

    [Fact]
    public void Attribute_ShouldNotBeInherited()
    {
        // Arrange
        var attributeUsage = typeof(AuditableAttribute).GetCustomAttribute<AttributeUsageAttribute>();

        // Assert
        attributeUsage.ShouldNotBeNull();
        attributeUsage!.Inherited.ShouldBeFalse();
    }

    [Fact]
    public void Attribute_CanBeRetrievedFromDecoratedClass()
    {
        // Act
        var attribute = typeof(TestAuditableCommand).GetCustomAttribute<AuditableAttribute>();

        // Assert
        attribute.ShouldNotBeNull();
        attribute!.EntityType.ShouldBe("TestEntity");
        attribute.Action.ShouldBe("TestAction");
    }

    [Fact]
    public void Attribute_WithSkip_CanBeRetrievedFromDecoratedClass()
    {
        // Act
        var attribute = typeof(SkippedAuditCommand).GetCustomAttribute<AuditableAttribute>();

        // Assert
        attribute.ShouldNotBeNull();
        attribute!.Skip.ShouldBeTrue();
    }

    [Fact]
    public void IncludePayload_WhenNull_ShouldIndicateUseDefault()
    {
        // Act
        var attribute = new AuditableAttribute();

        // Assert - null means "use the default from AuditOptions"
        attribute.IncludePayload.ShouldBeNull();
    }

    [Fact]
    public void IncludePayload_WhenFalse_ShouldExplicitlyDisable()
    {
        // Act
        var attribute = new AuditableAttribute { IncludePayloadValue = false };

        // Assert
        attribute.IncludePayload.ShouldBeFalse();
        attribute.IncludePayloadValue.ShouldBeFalse();
    }

    [Fact]
    public void IncludePayloadValue_ShouldBeUsableInAttributes()
    {
        // Act
        var attribute = typeof(NoPayloadCommand).GetCustomAttribute<AuditableAttribute>();

        // Assert
        attribute.ShouldNotBeNull();
        attribute!.IncludePayload.ShouldBeFalse();
    }

    [Auditable(IncludePayloadValue = false)]
    private sealed class NoPayloadCommand { }

    [Auditable(EntityType = "TestEntity", Action = "TestAction")]
    private sealed class TestAuditableCommand { }

    [Auditable(Skip = true)]
    private sealed class SkippedAuditCommand { }
}
