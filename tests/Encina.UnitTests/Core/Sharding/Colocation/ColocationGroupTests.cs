using Encina.Sharding.Colocation;

namespace Encina.UnitTests.Core.Sharding.Colocation;

/// <summary>
/// Unit tests for <see cref="ColocationGroup"/> record.
/// </summary>
public sealed class ColocationGroupTests
{
    // ────────────────────────────────────────────────────────────
    //  Construction
    // ────────────────────────────────────────────────────────────

    [Fact]
    public void Constructor_ValidParameters_StoresCorrectly()
    {
        // Arrange
        var colocated = new List<Type> { typeof(OrderItem) };

        // Act
        var group = new ColocationGroup(typeof(Order), colocated, "CustomerId");

        // Assert
        group.RootEntity.ShouldBe(typeof(Order));
        group.ColocatedEntities.ShouldBe(colocated);
        group.SharedShardKeyProperty.ShouldBe("CustomerId");
    }

    // ────────────────────────────────────────────────────────────
    //  IColocationGroup Interface
    // ────────────────────────────────────────────────────────────

    [Fact]
    public void ImplementsIColocationGroup()
    {
        // Arrange & Act
        IColocationGroup group = new ColocationGroup(typeof(Order), [], "Key");

        // Assert
        group.ShouldNotBeNull();
    }

    // ────────────────────────────────────────────────────────────
    //  Record Equality
    // ────────────────────────────────────────────────────────────

    [Fact]
    public void Equality_SameValues_AreEqual()
    {
        // Arrange
        var colocated = new List<Type> { typeof(OrderItem) };
        var group1 = new ColocationGroup(typeof(Order), colocated, "CustomerId");
        var group2 = new ColocationGroup(typeof(Order), colocated, "CustomerId");

        // Assert
        group1.ShouldBe(group2);
    }

    [Fact]
    public void Equality_DifferentRootEntity_AreNotEqual()
    {
        // Arrange
        var colocated = new List<Type> { typeof(OrderItem) };
        var group1 = new ColocationGroup(typeof(Order), colocated, "CustomerId");
        var group2 = new ColocationGroup(typeof(Customer), colocated, "CustomerId");

        // Assert
        group1.ShouldNotBe(group2);
    }

    // ────────────────────────────────────────────────────────────
    //  Test entity stubs
    // ────────────────────────────────────────────────────────────

    private sealed class Order;
    private sealed class OrderItem;
    private sealed class Customer;
}
