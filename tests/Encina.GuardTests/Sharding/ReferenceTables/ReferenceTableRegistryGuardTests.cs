using System.Diagnostics.CodeAnalysis;
using Encina.Sharding.ReferenceTables;

namespace Encina.GuardTests.Sharding.ReferenceTables;

/// <summary>
/// Guard clause tests for <see cref="ReferenceTableRegistry"/>.
/// </summary>
public sealed class ReferenceTableRegistryGuardTests
{
    [Fact]
    public void Constructor_NullConfigurations_ThrowsArgumentNullException()
    {
        // Act
        var act = () => new ReferenceTableRegistry(null!);

        // Assert
        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("configurations");
    }

    [Fact]
    public void Constructor_DuplicateEntityTypes_ThrowsArgumentException()
    {
        // Arrange
        var configs = new[]
        {
            new ReferenceTableConfiguration(typeof(string), new ReferenceTableOptions()),
            new ReferenceTableConfiguration(typeof(string), new ReferenceTableOptions()),
        };

        // Act
        var act = () => new ReferenceTableRegistry(configs);

        // Assert
        Should.Throw<ArgumentException>(act);
    }

    [Fact]
    [SuppressMessage("CodeQuality", "CA2263:Prefer generic overload when type is known", Justification = "Testing the non-generic overload with null Type parameter")]
    public void IsRegistered_NullType_ThrowsArgumentNullException()
    {
        // Arrange
        var registry = new ReferenceTableRegistry([]);
        Type nullType = null!;

        // Act
        Action act = () => registry.IsRegistered(nullType);

        // Assert
        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("entityType");
    }

    [Fact]
    [SuppressMessage("CodeQuality", "CA2263:Prefer generic overload when type is known", Justification = "Testing the non-generic overload with null Type parameter")]
    public void GetConfiguration_NullType_ThrowsArgumentNullException()
    {
        // Arrange
        var registry = new ReferenceTableRegistry([]);
        Type nullType = null!;

        // Act
        Action act = () => registry.GetConfiguration(nullType);

        // Assert
        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("entityType");
    }

    [Fact]
    [SuppressMessage("CodeQuality", "CA2263:Prefer generic overload when type is known", Justification = "Testing the non-generic overload with unregistered Type parameter")]
    public void GetConfiguration_UnregisteredType_ThrowsInvalidOperationException()
    {
        // Arrange
        var registry = new ReferenceTableRegistry([]);

        // Act
        Action act = () => registry.GetConfiguration(typeof(string));

        // Assert
        Should.Throw<InvalidOperationException>(act);
    }

    [Fact]
    public void TryGetConfiguration_NullType_ThrowsArgumentNullException()
    {
        // Arrange
        var registry = new ReferenceTableRegistry([]);
        Type nullType = null!;

        // Act
        Action act = () => registry.TryGetConfiguration(nullType, out _);

        // Assert
        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("entityType");
    }
}
