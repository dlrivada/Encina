using Encina.Sharding.Colocation;

namespace Encina.UnitTests.Core.Sharding.Colocation;

/// <summary>
/// Unit tests for <see cref="ColocationGroupRegistry"/>.
/// </summary>
public sealed class ColocationGroupRegistryTests
{
    // ────────────────────────────────────────────────────────────
    //  RegisterGroup
    // ────────────────────────────────────────────────────────────

    [Fact]
    public void RegisterGroup_ValidGroup_SuccessfullyRegisters()
    {
        // Arrange
        var registry = new ColocationGroupRegistry();
        var group = CreateGroup<Order>(typeof(OrderItem));

        // Act
        registry.RegisterGroup(group);

        // Assert
        registry.GetAllGroups().Count.ShouldBe(1);
    }

    [Fact]
    public void RegisterGroup_MultipleGroups_AllRegistered()
    {
        // Arrange
        var registry = new ColocationGroupRegistry();
        var group1 = CreateGroup<Order>(typeof(OrderItem));
        var group2 = CreateGroup<Customer>(typeof(CustomerAddress));

        // Act
        registry.RegisterGroup(group1);
        registry.RegisterGroup(group2);

        // Assert
        registry.GetAllGroups().Count.ShouldBe(2);
    }

    [Fact]
    public void RegisterGroup_SameGroupTwice_DoesNotThrow()
    {
        // Arrange
        var registry = new ColocationGroupRegistry();
        var group = CreateGroup<Order>(typeof(OrderItem));

        // Act
        registry.RegisterGroup(group);

        // Assert — registering the same instance again should not throw
        Should.NotThrow(() => registry.RegisterGroup(group));
    }

    [Fact]
    public void RegisterGroup_DifferentGroupWithSameRootEntity_ThrowsInvalidOperationException()
    {
        // Arrange
        var registry = new ColocationGroupRegistry();
        var group1 = CreateGroup<Order>(typeof(OrderItem));
        var group2 = new ColocationGroup(typeof(Order), [typeof(OrderPayment)], "CustomerId");

        registry.RegisterGroup(group1);

        // Act & Assert
        Should.Throw<InvalidOperationException>(() => registry.RegisterGroup(group2));
    }

    [Fact]
    public void RegisterGroup_ColocatedEntityAlreadyInDifferentGroup_ThrowsInvalidOperationException()
    {
        // Arrange
        var registry = new ColocationGroupRegistry();
        var group1 = CreateGroup<Order>(typeof(SharedEntity));
        var group2 = CreateGroup<Customer>(typeof(SharedEntity));

        registry.RegisterGroup(group1);

        // Act & Assert
        Should.Throw<InvalidOperationException>(() => registry.RegisterGroup(group2));
    }

    // ────────────────────────────────────────────────────────────
    //  RegisterColocatedEntity
    // ────────────────────────────────────────────────────────────

    [Fact]
    public void RegisterColocatedEntity_CreatesNewGroup()
    {
        // Arrange
        var registry = new ColocationGroupRegistry();

        // Act
        registry.RegisterColocatedEntity(typeof(Order), typeof(OrderItem));

        // Assert
        registry.TryGetGroup(typeof(Order), out var group).ShouldBeTrue();
        group.ShouldNotBeNull();
        group.RootEntity.ShouldBe(typeof(Order));
    }

    [Fact]
    public void RegisterColocatedEntity_ExistingGroup_AddsEntity()
    {
        // Arrange
        var registry = new ColocationGroupRegistry();
        registry.RegisterColocatedEntity(typeof(Order), typeof(OrderItem));

        // Act
        registry.RegisterColocatedEntity(typeof(Order), typeof(OrderPayment));

        // Assert
        registry.TryGetGroup(typeof(Order), out var group).ShouldBeTrue();
        group!.ColocatedEntities.ShouldContain(typeof(OrderItem));
        group.ColocatedEntities.ShouldContain(typeof(OrderPayment));
    }

    [Fact]
    public void RegisterColocatedEntity_DuplicateEntity_DoesNotAddTwice()
    {
        // Arrange
        var registry = new ColocationGroupRegistry();
        registry.RegisterColocatedEntity(typeof(Order), typeof(OrderItem));

        // Act
        registry.RegisterColocatedEntity(typeof(Order), typeof(OrderItem));

        // Assert
        registry.TryGetGroup(typeof(Order), out var group).ShouldBeTrue();
        group!.ColocatedEntities.Count(t => t == typeof(OrderItem)).ShouldBe(1);
    }

    [Fact]
    public void RegisterColocatedEntity_EntityInDifferentGroup_ThrowsInvalidOperationException()
    {
        // Arrange
        var registry = new ColocationGroupRegistry();
        registry.RegisterColocatedEntity(typeof(Order), typeof(SharedEntity));

        // Act & Assert
        Should.Throw<InvalidOperationException>(
            () => registry.RegisterColocatedEntity(typeof(Customer), typeof(SharedEntity)));
    }

    // ────────────────────────────────────────────────────────────
    //  TryGetGroup
    // ────────────────────────────────────────────────────────────

    [Fact]
    public void TryGetGroup_RegisteredRootEntity_ReturnsTrue()
    {
        // Arrange
        var registry = new ColocationGroupRegistry();
        var group = CreateGroup<Order>(typeof(OrderItem));
        registry.RegisterGroup(group);

        // Act
        var result = registry.TryGetGroup(typeof(Order), out var found);

        // Assert
        result.ShouldBeTrue();
        found.ShouldBeSameAs(group);
    }

    [Fact]
    public void TryGetGroup_RegisteredColocatedEntity_ReturnsTrue()
    {
        // Arrange
        var registry = new ColocationGroupRegistry();
        var group = CreateGroup<Order>(typeof(OrderItem));
        registry.RegisterGroup(group);

        // Act
        var result = registry.TryGetGroup(typeof(OrderItem), out var found);

        // Assert
        result.ShouldBeTrue();
        found.ShouldBeSameAs(group);
    }

    [Fact]
    public void TryGetGroup_UnregisteredEntity_ReturnsFalse()
    {
        // Arrange
        var registry = new ColocationGroupRegistry();

        // Act
        var result = registry.TryGetGroup(typeof(string), out var found);

        // Assert
        result.ShouldBeFalse();
        found.ShouldBeNull();
    }

    // ────────────────────────────────────────────────────────────
    //  GetAllGroups
    // ────────────────────────────────────────────────────────────

    [Fact]
    public void GetAllGroups_Empty_ReturnsEmptyCollection()
    {
        // Arrange
        var registry = new ColocationGroupRegistry();

        // Act
        var groups = registry.GetAllGroups();

        // Assert
        groups.ShouldBeEmpty();
    }

    [Fact]
    public void GetAllGroups_MultipleGroups_ReturnsAll()
    {
        // Arrange
        var registry = new ColocationGroupRegistry();
        registry.RegisterGroup(CreateGroup<Order>(typeof(OrderItem)));
        registry.RegisterGroup(CreateGroup<Customer>(typeof(CustomerAddress)));

        // Act
        var groups = registry.GetAllGroups();

        // Assert
        groups.Count.ShouldBe(2);
    }

    [Fact]
    public void GetAllGroups_ReturnsDistinctGroups()
    {
        // Arrange
        var registry = new ColocationGroupRegistry();
        var group = CreateGroup<Order>(typeof(OrderItem));
        registry.RegisterGroup(group);

        // Act
        var groups = registry.GetAllGroups();

        // Assert — should not have duplicates even though root and co-located both map to same group
        groups.Count.ShouldBe(1);
    }

    // ────────────────────────────────────────────────────────────
    //  Helpers
    // ────────────────────────────────────────────────────────────

    private static ColocationGroup CreateGroup<TRoot>(params Type[] colocatedEntities)
        => new(typeof(TRoot), colocatedEntities.ToList(), "SharedKey");

    // Test entity stubs
    private sealed class Order;
    private sealed class OrderItem;
    private sealed class OrderPayment;
    private sealed class Customer;
    private sealed class CustomerAddress;
    private sealed class SharedEntity;
}
