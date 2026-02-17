using Encina.Sharding.Colocation;

namespace Encina.UnitTests.Core.Sharding.Colocation;

/// <summary>
/// Unit tests for <see cref="ColocatedWithAttribute"/>.
/// </summary>
public sealed class ColocatedWithAttributeTests
{
    // ────────────────────────────────────────────────────────────
    //  Constructor
    // ────────────────────────────────────────────────────────────

    [Fact]
    public void Constructor_ValidType_StoresRootEntityType()
    {
        // Arrange & Act
        var attribute = new ColocatedWithAttribute(typeof(string));

        // Assert
        attribute.RootEntityType.ShouldBe(typeof(string));
    }

    [Fact]
    public void Constructor_NullType_ThrowsArgumentNullException()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() => new ColocatedWithAttribute(null!));
    }

    // ────────────────────────────────────────────────────────────
    //  Attribute Usage
    // ────────────────────────────────────────────────────────────

    [Fact]
    public void AttributeUsage_TargetsClassOnly()
    {
        // Arrange
        var usage = typeof(ColocatedWithAttribute)
            .GetCustomAttributes(typeof(AttributeUsageAttribute), false)
            .OfType<AttributeUsageAttribute>()
            .Single();

        // Assert
        usage.ValidOn.ShouldBe(AttributeTargets.Class);
    }

    [Fact]
    public void AttributeUsage_AllowMultiple_IsFalse()
    {
        // Arrange
        var usage = typeof(ColocatedWithAttribute)
            .GetCustomAttributes(typeof(AttributeUsageAttribute), false)
            .OfType<AttributeUsageAttribute>()
            .Single();

        // Assert
        usage.AllowMultiple.ShouldBeFalse();
    }

    [Fact]
    public void AttributeUsage_Inherited_IsFalse()
    {
        // Arrange
        var usage = typeof(ColocatedWithAttribute)
            .GetCustomAttributes(typeof(AttributeUsageAttribute), false)
            .OfType<AttributeUsageAttribute>()
            .Single();

        // Assert
        usage.Inherited.ShouldBeFalse();
    }

    // ────────────────────────────────────────────────────────────
    //  RootEntityType Property
    // ────────────────────────────────────────────────────────────

    [Fact]
    public void RootEntityType_ReturnsTypePassedToConstructor()
    {
        // Arrange
        var expectedType = typeof(int);

        // Act
        var attribute = new ColocatedWithAttribute(expectedType);

        // Assert
        attribute.RootEntityType.ShouldBeSameAs(expectedType);
    }

    [Fact]
    public void RootEntityType_DifferentTypes_ReturnCorrectType()
    {
        // Arrange & Act
        var attr1 = new ColocatedWithAttribute(typeof(string));
        var attr2 = new ColocatedWithAttribute(typeof(int));

        // Assert
        attr1.RootEntityType.ShouldBe(typeof(string));
        attr2.RootEntityType.ShouldBe(typeof(int));
    }
}
