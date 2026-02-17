using Encina.Sharding;
using Encina.Sharding.Colocation;
using Encina.Sharding.Routing;
using FsCheck;
using FsCheck.Xunit;

namespace Encina.PropertyTests.Database.Sharding;

/// <summary>
/// Property-based tests for <see cref="ColocationGroupRegistry"/> and co-location routing invariants.
/// Verifies determinism, membership consistency, and uniqueness constraints across generated inputs.
/// </summary>
[Trait("Category", "Property")]
public sealed class ColocationGroupRegistryPropertyTests
{
    // ────────────────────────────────────────────────────────────
    //  Determinism: same entity type always resolves to same group
    // ────────────────────────────────────────────────────────────

    [Property(MaxTest = 100)]
    public bool Property_TryGetGroup_IsDeterministic_ForRootEntity(byte seed)
    {
        // Arrange — use seed to vary but keep deterministic per test
        var registry = new ColocationGroupRegistry();
        var group = new ColocationGroup(typeof(Order), [typeof(OrderItem)], "CustomerId");
        registry.RegisterGroup(group);

        // Act — call twice
        var result1 = registry.TryGetGroup(typeof(Order), out var group1);
        var result2 = registry.TryGetGroup(typeof(Order), out var group2);

        // Assert
        return result1 && result2 && ReferenceEquals(group1, group2);
    }

    [Property(MaxTest = 100)]
    public bool Property_TryGetGroup_IsDeterministic_ForColocatedEntity(byte seed)
    {
        // Arrange
        var registry = new ColocationGroupRegistry();
        var group = new ColocationGroup(typeof(Order), [typeof(OrderItem)], "CustomerId");
        registry.RegisterGroup(group);

        // Act — call twice for co-located entity
        var result1 = registry.TryGetGroup(typeof(OrderItem), out var group1);
        var result2 = registry.TryGetGroup(typeof(OrderItem), out var group2);

        // Assert
        return result1 && result2 && ReferenceEquals(group1, group2);
    }

    // ────────────────────────────────────────────────────────────
    //  Membership consistency: root and co-located resolve to same group
    // ────────────────────────────────────────────────────────────

    [Property(MaxTest = 50)]
    public bool Property_RootAndColocatedEntities_ResolveSameGroup(byte seed)
    {
        // Arrange
        var registry = new ColocationGroupRegistry();
        var group = new ColocationGroup(typeof(Order), [typeof(OrderItem), typeof(OrderPayment)], "CustId");
        registry.RegisterGroup(group);

        // Act
        registry.TryGetGroup(typeof(Order), out var rootGroup);
        registry.TryGetGroup(typeof(OrderItem), out var itemGroup);
        registry.TryGetGroup(typeof(OrderPayment), out var paymentGroup);

        // Assert — all point to the same group instance
        return ReferenceEquals(rootGroup, itemGroup) && ReferenceEquals(itemGroup, paymentGroup);
    }

    // ────────────────────────────────────────────────────────────
    //  Uniqueness: an entity can belong to at most one group
    // ────────────────────────────────────────────────────────────

    [Fact]
    [Trait("Category", "Property")]
    public void Property_Entity_CannotBeInMultipleGroups()
    {
        // Arrange
        var registry = new ColocationGroupRegistry();
        var group1 = new ColocationGroup(typeof(Order), [typeof(SharedEntity)], "Key");
        var group2 = new ColocationGroup(typeof(Customer), [typeof(SharedEntity)], "Key");

        registry.RegisterGroup(group1);

        // Act & Assert — second registration must fail
        Assert.Throws<InvalidOperationException>(() => registry.RegisterGroup(group2));
    }

    // ────────────────────────────────────────────────────────────
    //  Idempotence: registering same group twice is safe
    // ────────────────────────────────────────────────────────────

    [Property(MaxTest = 50)]
    public bool Property_RegisterGroup_IsIdempotent(byte seed)
    {
        // Arrange
        var registry = new ColocationGroupRegistry();
        var group = new ColocationGroup(typeof(Order), [typeof(OrderItem)], "CustId");

        // Act — register same instance twice
        registry.RegisterGroup(group);
        registry.RegisterGroup(group);

        // Assert — group count should still be 1
        return registry.GetAllGroups().Count == 1;
    }

    // ────────────────────────────────────────────────────────────
    //  GetAllGroups returns distinct groups only
    // ────────────────────────────────────────────────────────────

    [Property(MaxTest = 50)]
    public bool Property_GetAllGroups_ReturnsDistinctGroups(byte seed)
    {
        // Arrange
        var registry = new ColocationGroupRegistry();
        var group1 = new ColocationGroup(typeof(Order), [typeof(OrderItem)], "CustId");
        var group2 = new ColocationGroup(typeof(Customer), [typeof(CustomerAddress)], "CustId");

        registry.RegisterGroup(group1);
        registry.RegisterGroup(group2);

        // Act
        var allGroups = registry.GetAllGroups();

        // Assert — exactly 2 distinct groups
        return allGroups.Count == 2 && allGroups.Distinct().Count() == 2;
    }

    // ────────────────────────────────────────────────────────────
    //  Unregistered entity always returns false
    // ────────────────────────────────────────────────────────────

    [Property(MaxTest = 50)]
    public bool Property_TryGetGroup_UnregisteredEntity_ReturnsFalse(byte seed)
    {
        // Arrange
        var registry = new ColocationGroupRegistry();
        var group = new ColocationGroup(typeof(Order), [typeof(OrderItem)], "CustId");
        registry.RegisterGroup(group);

        // Act — query a type that was never registered
        var result = registry.TryGetGroup(typeof(string), out var found);

        // Assert
        return !result && found is null;
    }

    // ────────────────────────────────────────────────────────────
    //  Router co-location consistency: same shard key routes
    //  root and co-located entities to the same group
    // ────────────────────────────────────────────────────────────

    [Property(MaxTest = 100)]
    public bool Property_Router_ColocatedEntities_ResolveToSameGroup(NonEmptyString shardKey)
    {
        // Arrange
        var registry = new ColocationGroupRegistry();
        var group = new ColocationGroup(typeof(Order), [typeof(OrderItem)], "CustomerId");
        registry.RegisterGroup(group);

        var topology = new ShardTopology([
            new ShardInfo("shard-1", "Server=s1"),
            new ShardInfo("shard-2", "Server=s2")
        ]);
        var router = new HashShardRouter(topology, colocationRegistry: registry);

        // Act
        var rootGroup = router.GetColocationGroup(typeof(Order));
        var itemGroup = router.GetColocationGroup(typeof(OrderItem));

        // Assert — both resolve to the same group
        return rootGroup is not null
               && itemGroup is not null
               && ReferenceEquals(rootGroup, itemGroup);
    }

    // ────────────────────────────────────────────────────────────
    //  RegisterColocatedEntity: incremental registration is consistent
    // ────────────────────────────────────────────────────────────

    [Property(MaxTest = 50)]
    public bool Property_RegisterColocatedEntity_IncrementalRegistration_IsConsistent(byte seed)
    {
        // Arrange
        var registry = new ColocationGroupRegistry();

        // Act — register entities incrementally
        registry.RegisterColocatedEntity(typeof(Order), typeof(OrderItem));
        registry.RegisterColocatedEntity(typeof(Order), typeof(OrderPayment));

        registry.TryGetGroup(typeof(Order), out var rootGroup);
        registry.TryGetGroup(typeof(OrderItem), out var itemGroup);
        registry.TryGetGroup(typeof(OrderPayment), out var paymentGroup);

        // Assert — all resolve to the same group
        return rootGroup is not null
               && ReferenceEquals(rootGroup, itemGroup)
               && ReferenceEquals(rootGroup, paymentGroup);
    }

    // ────────────────────────────────────────────────────────────
    //  Test entity stubs
    // ────────────────────────────────────────────────────────────

    private sealed class Order;
    private sealed class OrderItem;
    private sealed class OrderPayment;
    private sealed class Customer;
    private sealed class CustomerAddress;
    private sealed class SharedEntity;
}
