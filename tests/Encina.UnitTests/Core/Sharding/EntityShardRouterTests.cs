using Encina.Sharding;

namespace Encina.UnitTests.Core.Sharding;

/// <summary>
/// Unit tests for <see cref="EntityShardRouter{TEntity}"/> (internal class, visible via InternalsVisibleTo).
/// </summary>
public sealed class EntityShardRouterTests
{
    // ────────────────────────────────────────────────────────────
    //  Constructor
    // ────────────────────────────────────────────────────────────

    [Fact]
    public void Constructor_NullInner_ThrowsArgumentNullException()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() => new EntityShardRouter<ShardableOrder>(null!));
    }

    [Fact]
    public void Constructor_ValidInner_DoesNotThrow()
    {
        // Arrange
        var inner = Substitute.For<IShardRouter>();

        // Act & Assert
        Should.NotThrow(() => new EntityShardRouter<ShardableOrder>(inner));
    }

    // ────────────────────────────────────────────────────────────
    //  GetShardId(TEntity)
    // ────────────────────────────────────────────────────────────

    [Fact]
    public void GetShardId_ShardableEntity_ExtractsKeyAndDelegatesToInner()
    {
        // Arrange
        var inner = Substitute.For<IShardRouter>();
        inner.GetShardId("customer-456")
            .Returns(LanguageExt.Prelude.Right<EncinaError, string>("shard-2"));
        var router = new EntityShardRouter<ShardableOrder>(inner);
        var entity = new ShardableOrder { CustomerId = "customer-456" };

        // Act
        var result = router.GetShardId(entity);

        // Assert
        result.IsRight.ShouldBeTrue();
        _ = result.IfRight(id => id.ShouldBe("shard-2"));
        inner.Received(1).GetShardId("customer-456");
    }

    [Fact]
    public void GetShardId_AttributeEntity_ExtractsKeyAndDelegatesToInner()
    {
        // Arrange
        var inner = Substitute.For<IShardRouter>();
        inner.GetShardId("tenant-abc")
            .Returns(LanguageExt.Prelude.Right<EncinaError, string>("shard-1"));
        var router = new EntityShardRouter<AttributeOrder>(inner);
        var entity = new AttributeOrder { TenantId = "tenant-abc" };

        // Act
        var result = router.GetShardId(entity);

        // Assert
        result.IsRight.ShouldBeTrue();
        _ = result.IfRight(id => id.ShouldBe("shard-1"));
    }

    [Fact]
    public void GetShardId_EntityWithNoShardKey_ReturnsLeft()
    {
        // Arrange
        var inner = Substitute.For<IShardRouter>();
        var router = new EntityShardRouter<NoKeyEntity>(inner);
        var entity = new NoKeyEntity { Id = 42 };

        // Act
        var result = router.GetShardId(entity);

        // Assert
        result.IsLeft.ShouldBeTrue();
        inner.DidNotReceive().GetShardId(Arg.Any<string>());
    }

    [Fact]
    public void GetShardId_NullEntity_ThrowsArgumentNullException()
    {
        // Arrange
        var inner = Substitute.For<IShardRouter>();
        var router = new EntityShardRouter<ShardableOrder>(inner);

        // Act & Assert
        Should.Throw<ArgumentNullException>(() => router.GetShardId((ShardableOrder)null!));
    }

    [Fact]
    public void GetShardId_InnerRouterReturnsError_PropagatesError()
    {
        // Arrange
        var inner = Substitute.For<IShardRouter>();
        var error = EncinaErrors.Create(ShardingErrorCodes.ShardNotFound, "Not found");
        inner.GetShardId("customer-789")
            .Returns(LanguageExt.Prelude.Left<EncinaError, string>(error));
        var router = new EntityShardRouter<ShardableOrder>(inner);
        var entity = new ShardableOrder { CustomerId = "customer-789" };

        // Act
        var result = router.GetShardId(entity);

        // Assert
        result.IsLeft.ShouldBeTrue();
    }

    // ────────────────────────────────────────────────────────────
    //  GetShardId(string) delegation
    // ────────────────────────────────────────────────────────────

    [Fact]
    public void GetShardId_StringKey_DelegatesToInner()
    {
        // Arrange
        var inner = Substitute.For<IShardRouter>();
        inner.GetShardId("key-123")
            .Returns(LanguageExt.Prelude.Right<EncinaError, string>("shard-1"));
        var router = new EntityShardRouter<ShardableOrder>(inner);

        // Act
        var result = router.GetShardId("key-123");

        // Assert
        result.IsRight.ShouldBeTrue();
        inner.Received(1).GetShardId("key-123");
    }

    // ────────────────────────────────────────────────────────────
    //  GetAllShardIds delegation
    // ────────────────────────────────────────────────────────────

    [Fact]
    public void GetAllShardIds_DelegatesToInner()
    {
        // Arrange
        var inner = Substitute.For<IShardRouter>();
        inner.GetAllShardIds().Returns(new List<string> { "shard-1", "shard-2" });
        var router = new EntityShardRouter<ShardableOrder>(inner);

        // Act
        var result = router.GetAllShardIds();

        // Assert
        result.Count.ShouldBe(2);
        inner.Received(1).GetAllShardIds();
    }

    // ────────────────────────────────────────────────────────────
    //  GetShardConnectionString delegation
    // ────────────────────────────────────────────────────────────

    [Fact]
    public void GetShardConnectionString_DelegatesToInner()
    {
        // Arrange
        var inner = Substitute.For<IShardRouter>();
        inner.GetShardConnectionString("shard-1")
            .Returns(LanguageExt.Prelude.Right<EncinaError, string>("conn1"));
        var router = new EntityShardRouter<ShardableOrder>(inner);

        // Act
        var result = router.GetShardConnectionString("shard-1");

        // Assert
        result.IsRight.ShouldBeTrue();
        inner.Received(1).GetShardConnectionString("shard-1");
    }

    // ────────────────────────────────────────────────────────────
    //  Test entities
    // ────────────────────────────────────────────────────────────

    private sealed class ShardableOrder : IShardable
    {
        public string CustomerId { get; set; } = default!;
        public string GetShardKey() => CustomerId;
    }

    private sealed class AttributeOrder
    {
        [ShardKey]
        public string TenantId { get; set; } = default!;
    }

    private sealed class NoKeyEntity
    {
        public int Id { get; set; }
    }
}
