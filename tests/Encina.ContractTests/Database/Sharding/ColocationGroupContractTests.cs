using System.Reflection;
using Encina.Sharding;
using Encina.Sharding.Colocation;
using Encina.Sharding.Routing;
using Shouldly;

namespace Encina.ContractTests.Database.Sharding;

/// <summary>
/// Contract tests verifying that all shard router implementations support
/// <see cref="IShardRouter.GetColocationGroup"/> consistently and that
/// co-location abstractions satisfy their contracts.
/// </summary>
[Trait("Category", "Contract")]
public sealed class ColocationGroupContractTests
{
    // ────────────────────────────────────────────────────────────
    //  Shared test infrastructure
    // ────────────────────────────────────────────────────────────

    private static ShardTopology CreateTopology() =>
        new([
            new ShardInfo("shard-1", "Server=shard1"),
            new ShardInfo("shard-2", "Server=shard2")
        ]);

    private static ColocationGroupRegistry CreateRegistryWithGroup()
    {
        var registry = new ColocationGroupRegistry();
        var group = new ColocationGroup(typeof(Order), [typeof(OrderItem)], "CustomerId");
        registry.RegisterGroup(group);
        return registry;
    }

    // ────────────────────────────────────────────────────────────
    //  All routers implement GetColocationGroup
    // ────────────────────────────────────────────────────────────

    [Fact]
    public void Contract_HashShardRouter_ImplementsGetColocationGroup()
    {
        VerifyRouterHasGetColocationGroup(typeof(HashShardRouter));
    }

    [Fact]
    public void Contract_RangeShardRouter_ImplementsGetColocationGroup()
    {
        VerifyRouterHasGetColocationGroup(typeof(RangeShardRouter));
    }

    [Fact]
    public void Contract_DirectoryShardRouter_ImplementsGetColocationGroup()
    {
        VerifyRouterHasGetColocationGroup(typeof(DirectoryShardRouter));
    }

    [Fact]
    public void Contract_GeoShardRouter_ImplementsGetColocationGroup()
    {
        VerifyRouterHasGetColocationGroup(typeof(GeoShardRouter));
    }

    [Fact]
    public void Contract_CompoundShardRouter_ImplementsGetColocationGroup()
    {
        VerifyRouterHasGetColocationGroup(typeof(CompoundShardRouter));
    }

    // ────────────────────────────────────────────────────────────
    //  All routers accept optional ColocationGroupRegistry
    // ────────────────────────────────────────────────────────────

    [Fact]
    public void Contract_HashShardRouter_AcceptsColocationRegistryInConstructor()
    {
        VerifyConstructorAcceptsColocationRegistry(typeof(HashShardRouter));
    }

    [Fact]
    public void Contract_RangeShardRouter_AcceptsColocationRegistryInConstructor()
    {
        VerifyConstructorAcceptsColocationRegistry(typeof(RangeShardRouter));
    }

    [Fact]
    public void Contract_DirectoryShardRouter_AcceptsColocationRegistryInConstructor()
    {
        VerifyConstructorAcceptsColocationRegistry(typeof(DirectoryShardRouter));
    }

    [Fact]
    public void Contract_GeoShardRouter_AcceptsColocationRegistryInConstructor()
    {
        VerifyConstructorAcceptsColocationRegistry(typeof(GeoShardRouter));
    }

    [Fact]
    public void Contract_CompoundShardRouter_AcceptsColocationRegistryInConstructor()
    {
        VerifyConstructorAcceptsColocationRegistry(typeof(CompoundShardRouter));
    }

    // ────────────────────────────────────────────────────────────
    //  HashShardRouter returns correct co-location group
    // ────────────────────────────────────────────────────────────

    [Fact]
    public void Contract_HashShardRouter_GetColocationGroup_ReturnsGroupForColocatedEntity()
    {
        // Arrange
        var registry = CreateRegistryWithGroup();
        var router = new HashShardRouter(CreateTopology(), colocationRegistry: registry);

        // Act
        var group = router.GetColocationGroup(typeof(OrderItem));

        // Assert
        group.ShouldNotBeNull();
        group.RootEntity.ShouldBe(typeof(Order));
    }

    [Fact]
    public void Contract_HashShardRouter_GetColocationGroup_ReturnsNullForNonColocatedEntity()
    {
        // Arrange
        var registry = CreateRegistryWithGroup();
        var router = new HashShardRouter(CreateTopology(), colocationRegistry: registry);

        // Act
        var group = router.GetColocationGroup(typeof(string));

        // Assert
        group.ShouldBeNull();
    }

    [Fact]
    public void Contract_HashShardRouter_GetColocationGroup_WithoutRegistry_ReturnsNull()
    {
        // Arrange — no registry
        var router = new HashShardRouter(CreateTopology());

        // Act
        var group = router.GetColocationGroup(typeof(Order));

        // Assert
        group.ShouldBeNull();
    }

    // ────────────────────────────────────────────────────────────
    //  RangeShardRouter returns correct co-location group
    // ────────────────────────────────────────────────────────────

    [Fact]
    public void Contract_RangeShardRouter_GetColocationGroup_ReturnsGroupForRootEntity()
    {
        // Arrange
        var registry = CreateRegistryWithGroup();
        var ranges = new[]
        {
            new ShardRange("A", "M", "shard-1"),
            new ShardRange("N", null, "shard-2")
        };
        var router = new RangeShardRouter(CreateTopology(), ranges, colocationRegistry: registry);

        // Act
        var group = router.GetColocationGroup(typeof(Order));

        // Assert
        group.ShouldNotBeNull();
        group.RootEntity.ShouldBe(typeof(Order));
    }

    [Fact]
    public void Contract_RangeShardRouter_GetColocationGroup_ReturnsNullForNonColocatedEntity()
    {
        // Arrange
        var registry = CreateRegistryWithGroup();
        var ranges = new[]
        {
            new ShardRange("A", "M", "shard-1"),
            new ShardRange("N", null, "shard-2")
        };
        var router = new RangeShardRouter(CreateTopology(), ranges, colocationRegistry: registry);

        // Act
        var group = router.GetColocationGroup(typeof(string));

        // Assert
        group.ShouldBeNull();
    }

    // ────────────────────────────────────────────────────────────
    //  DirectoryShardRouter returns correct co-location group
    // ────────────────────────────────────────────────────────────

    [Fact]
    public void Contract_DirectoryShardRouter_GetColocationGroup_ReturnsGroupForColocatedEntity()
    {
        // Arrange
        var registry = CreateRegistryWithGroup();
        var store = new InMemoryShardDirectoryStore();
        var router = new DirectoryShardRouter(CreateTopology(), store, colocationRegistry: registry);

        // Act
        var group = router.GetColocationGroup(typeof(OrderItem));

        // Assert
        group.ShouldNotBeNull();
        group.RootEntity.ShouldBe(typeof(Order));
    }

    // ────────────────────────────────────────────────────────────
    //  GeoShardRouter returns correct co-location group
    // ────────────────────────────────────────────────────────────

    [Fact]
    public void Contract_GeoShardRouter_GetColocationGroup_ReturnsGroupForColocatedEntity()
    {
        // Arrange
        var registry = CreateRegistryWithGroup();
        var regions = new[]
        {
            new GeoRegion("US", "shard-1"),
            new GeoRegion("EU", "shard-2")
        };
        var router = new GeoShardRouter(
            CreateTopology(), regions, _ => "US", colocationRegistry: registry);

        // Act
        var group = router.GetColocationGroup(typeof(OrderItem));

        // Assert
        group.ShouldNotBeNull();
        group.RootEntity.ShouldBe(typeof(Order));
    }

    // ────────────────────────────────────────────────────────────
    //  CompoundShardRouter returns correct co-location group
    // ────────────────────────────────────────────────────────────

    [Fact]
    public void Contract_CompoundShardRouter_GetColocationGroup_ReturnsGroupForColocatedEntity()
    {
        // Arrange
        var registry = CreateRegistryWithGroup();
        var innerRouter = new HashShardRouter(CreateTopology());
        var options = new CompoundShardRouterOptions
        {
            ComponentRouters = { [0] = innerRouter }
        };
        var router = new CompoundShardRouter(CreateTopology(), options, colocationRegistry: registry);

        // Act
        var group = router.GetColocationGroup(typeof(OrderItem));

        // Assert
        group.ShouldNotBeNull();
        group.RootEntity.ShouldBe(typeof(Order));
    }

    // ────────────────────────────────────────────────────────────
    //  ColocationGroup record contract
    // ────────────────────────────────────────────────────────────

    [Fact]
    public void Contract_ColocationGroup_IsSealed()
    {
        typeof(ColocationGroup).IsSealed.ShouldBeTrue(
            "ColocationGroup must be sealed to prevent uncontrolled subclassing.");
    }

    [Fact]
    public void Contract_ColocationGroup_ImplementsIColocationGroup()
    {
        typeof(IColocationGroup).IsAssignableFrom(typeof(ColocationGroup)).ShouldBeTrue(
            "ColocationGroup must implement IColocationGroup.");
    }

    [Fact]
    public void Contract_ColocationGroupRegistry_IsSealed()
    {
        typeof(ColocationGroupRegistry).IsSealed.ShouldBeTrue(
            "ColocationGroupRegistry must be sealed.");
    }

    [Fact]
    public void Contract_ColocationGroupBuilder_IsSealed()
    {
        typeof(ColocationGroupBuilder).IsSealed.ShouldBeTrue(
            "ColocationGroupBuilder must be sealed.");
    }

    [Fact]
    public void Contract_ColocatedWithAttribute_IsSealed()
    {
        typeof(ColocatedWithAttribute).IsSealed.ShouldBeTrue(
            "ColocatedWithAttribute must be sealed.");
    }

    [Fact]
    public void Contract_ColocationViolationException_IsSealed()
    {
        typeof(ColocationViolationException).IsSealed.ShouldBeTrue(
            "ColocationViolationException must be sealed.");
    }

    // ────────────────────────────────────────────────────────────
    //  IColocationGroup contract surface
    // ────────────────────────────────────────────────────────────

    [Fact]
    public void Contract_IColocationGroup_HasRequiredProperties()
    {
        var type = typeof(IColocationGroup);
        var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);

        properties.Select(p => p.Name).ShouldContain("RootEntity");
        properties.Select(p => p.Name).ShouldContain("ColocatedEntities");
        properties.Select(p => p.Name).ShouldContain("SharedShardKeyProperty");
    }

    [Fact]
    public void Contract_IColocationGroup_RootEntity_ReturnsType()
    {
        var prop = typeof(IColocationGroup).GetProperty("RootEntity");
        prop.ShouldNotBeNull();
        prop.PropertyType.ShouldBe(typeof(Type));
    }

    [Fact]
    public void Contract_IColocationGroup_ColocatedEntities_ReturnsReadOnlyListOfType()
    {
        var prop = typeof(IColocationGroup).GetProperty("ColocatedEntities");
        prop.ShouldNotBeNull();
        prop.PropertyType.ShouldBe(typeof(IReadOnlyList<Type>));
    }

    // ────────────────────────────────────────────────────────────
    //  Routing consistency: co-located entities via same router
    //  return the same group
    // ────────────────────────────────────────────────────────────

    [Fact]
    public void Contract_Router_ColocatedEntities_ResolveSameGroup()
    {
        // Arrange
        var registry = CreateRegistryWithGroup();
        var router = new HashShardRouter(CreateTopology(), colocationRegistry: registry);

        // Act
        var rootGroup = router.GetColocationGroup(typeof(Order));
        var childGroup = router.GetColocationGroup(typeof(OrderItem));

        // Assert
        rootGroup.ShouldNotBeNull();
        childGroup.ShouldNotBeNull();
        rootGroup.ShouldBeSameAs(childGroup);
    }

    // ────────────────────────────────────────────────────────────
    //  Helpers
    // ────────────────────────────────────────────────────────────

    private static void VerifyRouterHasGetColocationGroup(Type routerType)
    {
        var method = routerType.GetMethod(
            "GetColocationGroup",
            BindingFlags.Public | BindingFlags.Instance,
            [typeof(Type)]);

        method.ShouldNotBeNull(
            $"{routerType.Name} must implement GetColocationGroup(Type entityType)");
        method.ReturnType.ShouldBe(typeof(IColocationGroup),
            $"{routerType.Name}.GetColocationGroup should return IColocationGroup?");
    }

    private static void VerifyConstructorAcceptsColocationRegistry(Type routerType)
    {
        var constructors = routerType.GetConstructors(BindingFlags.Public | BindingFlags.Instance);
        var hasRegistryParam = constructors.Any(c =>
            c.GetParameters().Any(p => p.ParameterType == typeof(ColocationGroupRegistry)));

        hasRegistryParam.ShouldBeTrue(
            $"{routerType.Name} constructor must accept an optional ColocationGroupRegistry parameter.");
    }

    // ────────────────────────────────────────────────────────────
    //  Test entity stubs
    // ────────────────────────────────────────────────────────────

    private sealed class Order;
    private sealed class OrderItem;
}
