using Encina.Sharding;

namespace Encina.UnitTests.Core.Sharding;

/// <summary>
/// Unit tests for <see cref="ShardKeyExtractor"/>.
/// </summary>
public sealed class ShardKeyExtractorTests
{
    // ────────────────────────────────────────────────────────────
    //  IShardable extraction
    // ────────────────────────────────────────────────────────────

    [Fact]
    public void Extract_EntityImplementsIShardable_ReturnsShardKey()
    {
        // Arrange
        var entity = new ShardableEntity("customer-123");

        // Act
        var result = ShardKeyExtractor.Extract(entity);

        // Assert
        result.IsRight.ShouldBeTrue();
        _ = result.IfRight(key => key.ShouldBe("customer-123"));
    }

    [Fact]
    public void Extract_IShardableReturnsEmpty_ReturnsLeft()
    {
        // Arrange
        var entity = new ShardableEntity("");

        // Act
        var result = ShardKeyExtractor.Extract(entity);

        // Assert
        result.IsLeft.ShouldBeTrue();
    }

    [Fact]
    public void Extract_IShardableReturnsNull_ReturnsLeft()
    {
        // Arrange
        var entity = new ShardableEntity(null!);

        // Act
        var result = ShardKeyExtractor.Extract(entity);

        // Assert
        result.IsLeft.ShouldBeTrue();
    }

    // ────────────────────────────────────────────────────────────
    //  ShardKeyAttribute extraction
    // ────────────────────────────────────────────────────────────

    [Fact]
    public void Extract_EntityWithShardKeyAttribute_ReturnsPropertyValue()
    {
        // Arrange
        var entity = new AttributeEntity { TenantId = "tenant-abc" };

        // Act
        var result = ShardKeyExtractor.Extract(entity);

        // Assert
        result.IsRight.ShouldBeTrue();
        _ = result.IfRight(key => key.ShouldBe("tenant-abc"));
    }

    [Fact]
    public void Extract_AttributePropertyIsNull_ReturnsLeft()
    {
        // Arrange
        var entity = new AttributeEntity { TenantId = null! };

        // Act
        var result = ShardKeyExtractor.Extract(entity);

        // Assert
        result.IsLeft.ShouldBeTrue();
    }

    [Fact]
    public void Extract_AttributePropertyToStringReturnsEmpty_ReturnsLeft()
    {
        // Arrange
        var entity = new AttributeEntityWithIntKey { ShardKey = 0 };

        // Act
        var result = ShardKeyExtractor.Extract(entity);

        // Assert
        // int.ToString() returns "0" which is not empty, so this should succeed
        result.IsRight.ShouldBeTrue();
        _ = result.IfRight(key => key.ShouldBe("0"));
    }

    // ────────────────────────────────────────────────────────────
    //  No shard key configured
    // ────────────────────────────────────────────────────────────

    [Fact]
    public void Extract_EntityWithoutShardKey_ReturnsLeft()
    {
        // Arrange
        var entity = new NoShardKeyEntity { Id = 42 };

        // Act
        var result = ShardKeyExtractor.Extract(entity);

        // Assert
        result.IsLeft.ShouldBeTrue();
    }

    // ────────────────────────────────────────────────────────────
    //  Null entity
    // ────────────────────────────────────────────────────────────

    [Fact]
    public void Extract_NullEntity_ThrowsArgumentNullException()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() => ShardKeyExtractor.Extract<ShardableEntity>(null!));
    }

    // ────────────────────────────────────────────────────────────
    //  IShardable takes precedence over attribute
    // ────────────────────────────────────────────────────────────

    [Fact]
    public void Extract_EntityImplementsBothIShardableAndAttribute_UsesIShardable()
    {
        // Arrange
        var entity = new BothShardableAndAttribute
        {
            AttributeKey = "from-attribute",
            ShardableKey = "from-interface"
        };

        // Act
        var result = ShardKeyExtractor.Extract(entity);

        // Assert
        result.IsRight.ShouldBeTrue();
        _ = result.IfRight(key => key.ShouldBe("from-interface"));
    }

    // ────────────────────────────────────────────────────────────
    //  Reflection caching (determinism)
    // ────────────────────────────────────────────────────────────

    [Fact]
    public void Extract_CalledMultipleTimes_ReturnsSameResult()
    {
        // Arrange
        var entity = new AttributeEntity { TenantId = "tenant-xyz" };

        // Act
        var result1 = ShardKeyExtractor.Extract(entity);
        var result2 = ShardKeyExtractor.Extract(entity);

        // Assert
        result1.IsRight.ShouldBeTrue();
        result2.IsRight.ShouldBeTrue();

        var key1 = string.Empty;
        var key2 = string.Empty;
        _ = result1.IfRight(k => key1 = k);
        _ = result2.IfRight(k => key2 = k);
        key1.ShouldBe(key2);
    }

    // ────────────────────────────────────────────────────────────
    //  Test entities
    // ────────────────────────────────────────────────────────────

    private sealed class ShardableEntity(string shardKey) : IShardable
    {
        public string GetShardKey() => shardKey;
    }

    private sealed class AttributeEntity
    {
        [ShardKey]
        public string TenantId { get; set; } = default!;
    }

    private sealed class AttributeEntityWithIntKey
    {
        [ShardKey]
        public int ShardKey { get; set; }
    }

    private sealed class NoShardKeyEntity
    {
        public int Id { get; set; }
    }

    private sealed class BothShardableAndAttribute : IShardable
    {
        [ShardKey]
        public string AttributeKey { get; set; } = default!;

        public string ShardableKey { get; set; } = default!;

        public string GetShardKey() => ShardableKey;
    }
}
