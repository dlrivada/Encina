using Encina.Messaging.ReadWriteSeparation;
using Shouldly;

namespace Encina.UnitTests.Messaging.ReadWriteSeparation;

/// <summary>
/// Unit tests for <see cref="ForceWriteDatabaseAttribute"/>.
/// </summary>
public sealed class ForceWriteDatabaseAttributeTests
{
    [Fact]
    public void Constructor_CreatesAttribute()
    {
        // Act
        var attribute = new ForceWriteDatabaseAttribute();

        // Assert
        attribute.ShouldNotBeNull();
    }

    [Fact]
    public void Reason_InitiallyIsNull()
    {
        // Arrange
        var attribute = new ForceWriteDatabaseAttribute();

        // Assert
        attribute.Reason.ShouldBeNull();
    }

    [Fact]
    public void Reason_CanBeSetViaInit()
    {
        // Act
        var attribute = new ForceWriteDatabaseAttribute
        {
            Reason = "Read-after-write consistency required"
        };

        // Assert
        attribute.Reason.ShouldBe("Read-after-write consistency required");
    }

    [Fact]
    public void Attribute_CanBeAppliedToClass()
    {
        // Act
        var type = typeof(TestQueryWithAttribute);
        var attribute = type.GetCustomAttributes(typeof(ForceWriteDatabaseAttribute), false)
            .FirstOrDefault() as ForceWriteDatabaseAttribute;

        // Assert
        attribute.ShouldNotBeNull();
    }

    [Fact]
    public void Attribute_ReasonIsRetrievableFromReflection()
    {
        // Act
        var type = typeof(TestQueryWithReason);
        var attribute = type.GetCustomAttributes(typeof(ForceWriteDatabaseAttribute), false)
            .FirstOrDefault() as ForceWriteDatabaseAttribute;

        // Assert
        attribute.ShouldNotBeNull();
        attribute.Reason.ShouldBe("Must verify latest balance");
    }

    [Fact]
    public void Attribute_IsNotInherited()
    {
        // Act
        var baseType = typeof(BaseQueryWithAttribute);
        var derivedType = typeof(DerivedQueryWithoutAttribute);

        var baseAttribute = baseType.GetCustomAttributes(typeof(ForceWriteDatabaseAttribute), false)
            .FirstOrDefault();
        var derivedAttribute = derivedType.GetCustomAttributes(typeof(ForceWriteDatabaseAttribute), false)
            .FirstOrDefault();

        // Assert
        baseAttribute.ShouldNotBeNull();
        derivedAttribute.ShouldBeNull(); // Not inherited because Inherited = false
    }

    [Fact]
    public void Attribute_TargetsClassOnly()
    {
        // Arrange
        var attributeUsage = typeof(ForceWriteDatabaseAttribute)
            .GetCustomAttributes(typeof(AttributeUsageAttribute), false)
            .FirstOrDefault() as AttributeUsageAttribute;

        // Assert
        attributeUsage.ShouldNotBeNull();
        attributeUsage.ValidOn.ShouldBe(AttributeTargets.Class);
        attributeUsage.AllowMultiple.ShouldBeFalse();
        attributeUsage.Inherited.ShouldBeFalse();
    }

    // Test classes for attribute testing
    [ForceWriteDatabase]
    private sealed class TestQueryWithAttribute { }

    [ForceWriteDatabase(Reason = "Must verify latest balance")]
    private sealed class TestQueryWithReason { }

    [ForceWriteDatabase]
    private class BaseQueryWithAttribute { }

    private sealed class DerivedQueryWithoutAttribute : BaseQueryWithAttribute { }
}
