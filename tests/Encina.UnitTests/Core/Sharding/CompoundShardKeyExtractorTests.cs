using Encina.Sharding;

namespace Encina.UnitTests.Core.Sharding;

/// <summary>
/// Unit tests for <see cref="CompoundShardKeyExtractor"/>.
/// </summary>
public sealed class CompoundShardKeyExtractorTests
{
    // ────────────────────────────────────────────────────────────
    //  Priority 1: ICompoundShardable
    // ────────────────────────────────────────────────────────────

    [Fact]
    public void Extract_ICompoundShardable_ReturnsCompoundKey()
    {
        var entity = new CompoundShardableEntity("us-east", "customer-123");

        var result = CompoundShardKeyExtractor.Extract(entity);

        result.IsRight.ShouldBeTrue();
        _ = result.IfRight(key =>
        {
            key.ComponentCount.ShouldBe(2);
            key.PrimaryComponent.ShouldBe("us-east");
            key.Components[1].ShouldBe("customer-123");
        });
    }

    [Fact]
    public void Extract_ICompoundShardable_ReturnsNullKey_ReturnsLeft()
    {
        var entity = new NullCompoundShardableEntity();

        var result = CompoundShardKeyExtractor.Extract(entity);

        result.IsLeft.ShouldBeTrue();
    }

    [Fact]
    public void Extract_ICompoundShardable_EmptyComponent_ReturnsLeft()
    {
        var entity = new CompoundShardableEntity("us-east", "");

        var result = CompoundShardKeyExtractor.Extract(entity);

        result.IsLeft.ShouldBeTrue();
    }

    // ────────────────────────────────────────────────────────────
    //  Priority 2: Multiple [ShardKey] attributes
    // ────────────────────────────────────────────────────────────

    [Fact]
    public void Extract_MultipleShardKeyAttributes_OrderedByOrder_ReturnsCompoundKey()
    {
        var entity = new MultiAttributeEntity { Region = "eu-west", CustomerId = "cust-456" };

        var result = CompoundShardKeyExtractor.Extract(entity);

        result.IsRight.ShouldBeTrue();
        _ = result.IfRight(key =>
        {
            key.ComponentCount.ShouldBe(2);
            key.PrimaryComponent.ShouldBe("eu-west");
            key.Components[1].ShouldBe("cust-456");
        });
    }

    [Fact]
    public void Extract_MultipleShardKeyAttributes_ThreeComponents_ReturnsCorrectOrder()
    {
        var entity = new ThreeAttributeEntity
        {
            Region = "ap-south",
            CustomerId = "cust-789",
            ProductCategory = "electronics"
        };

        var result = CompoundShardKeyExtractor.Extract(entity);

        result.IsRight.ShouldBeTrue();
        _ = result.IfRight(key =>
        {
            key.ComponentCount.ShouldBe(3);
            key.Components[0].ShouldBe("ap-south");
            key.Components[1].ShouldBe("cust-789");
            key.Components[2].ShouldBe("electronics");
        });
    }

    [Fact]
    public void Extract_MultipleShardKeyAttributes_DuplicateOrder_ReturnsLeft()
    {
        var entity = new DuplicateOrderEntity { Region = "us-east", CustomerId = "cust-123" };

        var result = CompoundShardKeyExtractor.Extract(entity);

        result.IsLeft.ShouldBeTrue();
    }

    [Fact]
    public void Extract_MultipleShardKeyAttributes_NullComponent_ReturnsLeft()
    {
        var entity = new MultiAttributeEntity { Region = null!, CustomerId = "cust-456" };

        var result = CompoundShardKeyExtractor.Extract(entity);

        result.IsLeft.ShouldBeTrue();
    }

    [Fact]
    public void Extract_MultipleShardKeyAttributes_EmptyComponent_ReturnsLeft()
    {
        var entity = new MultiAttributeEntity { Region = "", CustomerId = "cust-456" };

        var result = CompoundShardKeyExtractor.Extract(entity);

        result.IsLeft.ShouldBeTrue();
    }

    // ────────────────────────────────────────────────────────────
    //  Priority 3: IShardable (backward compatibility)
    // ────────────────────────────────────────────────────────────

    [Fact]
    public void Extract_IShardable_ReturnsSingleComponentCompoundKey()
    {
        var entity = new SimpleShardableEntity("tenant-abc");

        var result = CompoundShardKeyExtractor.Extract(entity);

        result.IsRight.ShouldBeTrue();
        _ = result.IfRight(key =>
        {
            key.ComponentCount.ShouldBe(1);
            key.PrimaryComponent.ShouldBe("tenant-abc");
            key.HasSecondaryComponents.ShouldBeFalse();
        });
    }

    [Fact]
    public void Extract_IShardable_EmptyKey_ReturnsLeft()
    {
        var entity = new SimpleShardableEntity("");

        var result = CompoundShardKeyExtractor.Extract(entity);

        result.IsLeft.ShouldBeTrue();
    }

    [Fact]
    public void Extract_IShardable_NullKey_ReturnsLeft()
    {
        var entity = new SimpleShardableEntity(null!);

        var result = CompoundShardKeyExtractor.Extract(entity);

        result.IsLeft.ShouldBeTrue();
    }

    // ────────────────────────────────────────────────────────────
    //  Priority 4: Single [ShardKey] attribute
    // ────────────────────────────────────────────────────────────

    [Fact]
    public void Extract_SingleShardKeyAttribute_ReturnsSingleComponentCompoundKey()
    {
        var entity = new SingleAttributeEntity { TenantId = "tenant-xyz" };

        var result = CompoundShardKeyExtractor.Extract(entity);

        result.IsRight.ShouldBeTrue();
        _ = result.IfRight(key =>
        {
            key.ComponentCount.ShouldBe(1);
            key.PrimaryComponent.ShouldBe("tenant-xyz");
        });
    }

    [Fact]
    public void Extract_SingleShardKeyAttribute_NullValue_ReturnsLeft()
    {
        var entity = new SingleAttributeEntity { TenantId = null! };

        var result = CompoundShardKeyExtractor.Extract(entity);

        result.IsLeft.ShouldBeTrue();
    }

    // ────────────────────────────────────────────────────────────
    //  Priority ordering
    // ────────────────────────────────────────────────────────────

    [Fact]
    public void Extract_ICompoundShardable_TakesPrecedence_OverMultipleAttributes()
    {
        var entity = new CompoundShardableWithAttributes
        {
            Region = "from-attribute",
            CustomerId = "from-attribute",
            CompoundKeyRegion = "from-interface",
            CompoundKeyCustomer = "from-interface"
        };

        var result = CompoundShardKeyExtractor.Extract(entity);

        result.IsRight.ShouldBeTrue();
        _ = result.IfRight(key =>
        {
            key.PrimaryComponent.ShouldBe("from-interface");
            key.Components[1].ShouldBe("from-interface");
        });
    }

    [Fact]
    public void Extract_MultipleAttributes_TakesPrecedence_OverIShardable()
    {
        var entity = new MultiAttributeAndShardable
        {
            Region = "from-attributes",
            CustomerId = "from-attributes",
            ShardableKey = "from-ishardable"
        };

        var result = CompoundShardKeyExtractor.Extract(entity);

        result.IsRight.ShouldBeTrue();
        _ = result.IfRight(key =>
        {
            key.ComponentCount.ShouldBe(2);
            key.PrimaryComponent.ShouldBe("from-attributes");
        });
    }

    // ────────────────────────────────────────────────────────────
    //  No shard key configured
    // ────────────────────────────────────────────────────────────

    [Fact]
    public void Extract_NoShardKeyConfigured_ReturnsLeft()
    {
        var entity = new PlainEntity { Id = 42 };

        var result = CompoundShardKeyExtractor.Extract(entity);

        result.IsLeft.ShouldBeTrue();
    }

    // ────────────────────────────────────────────────────────────
    //  Null entity
    // ────────────────────────────────────────────────────────────

    [Fact]
    public void Extract_NullEntity_ThrowsArgumentNullException()
    {
        Should.Throw<ArgumentNullException>(() =>
            CompoundShardKeyExtractor.Extract<PlainEntity>(null!));
    }

    // ────────────────────────────────────────────────────────────
    //  Caching / Determinism
    // ────────────────────────────────────────────────────────────

    [Fact]
    public void Extract_CalledMultipleTimes_ReturnsSameResult()
    {
        var entity = new MultiAttributeEntity { Region = "us-east", CustomerId = "cust-1" };

        var result1 = CompoundShardKeyExtractor.Extract(entity);
        var result2 = CompoundShardKeyExtractor.Extract(entity);

        result1.IsRight.ShouldBeTrue();
        result2.IsRight.ShouldBeTrue();

        CompoundShardKey? key1 = null;
        CompoundShardKey? key2 = null;
        _ = result1.IfRight(k => key1 = k);
        _ = result2.IfRight(k => key2 = k);

        key1!.ToString().ShouldBe(key2!.ToString());
    }

    // ────────────────────────────────────────────────────────────
    //  Test entities
    // ────────────────────────────────────────────────────────────

    private sealed class CompoundShardableEntity(string region, string customerId) : ICompoundShardable
    {
        public CompoundShardKey GetCompoundShardKey() => new(region, customerId);
    }

    private sealed class NullCompoundShardableEntity : ICompoundShardable
    {
        public CompoundShardKey GetCompoundShardKey() => null!;
    }

    private sealed class MultiAttributeEntity
    {
        [ShardKey(Order = 0)]
        public string Region { get; set; } = default!;

        [ShardKey(Order = 1)]
        public string CustomerId { get; set; } = default!;
    }

    private sealed class ThreeAttributeEntity
    {
        [ShardKey(Order = 0)]
        public string Region { get; set; } = default!;

        [ShardKey(Order = 1)]
        public string CustomerId { get; set; } = default!;

        [ShardKey(Order = 2)]
        public string ProductCategory { get; set; } = default!;
    }

    private sealed class DuplicateOrderEntity
    {
        [ShardKey(Order = 0)]
        public string Region { get; set; } = default!;

        [ShardKey(Order = 0)]
        public string CustomerId { get; set; } = default!;
    }

    private sealed class SimpleShardableEntity(string shardKey) : IShardable
    {
        public string GetShardKey() => shardKey;
    }

    private sealed class SingleAttributeEntity
    {
        [ShardKey]
        public string TenantId { get; set; } = default!;
    }

    private sealed class CompoundShardableWithAttributes : ICompoundShardable
    {
        [ShardKey(Order = 0)]
        public string Region { get; set; } = default!;

        [ShardKey(Order = 1)]
        public string CustomerId { get; set; } = default!;

        public string CompoundKeyRegion { get; set; } = default!;
        public string CompoundKeyCustomer { get; set; } = default!;

        public CompoundShardKey GetCompoundShardKey() => new(CompoundKeyRegion, CompoundKeyCustomer);
    }

    private sealed class MultiAttributeAndShardable : IShardable
    {
        [ShardKey(Order = 0)]
        public string Region { get; set; } = default!;

        [ShardKey(Order = 1)]
        public string CustomerId { get; set; } = default!;

        public string ShardableKey { get; set; } = default!;

        public string GetShardKey() => ShardableKey;
    }

    private sealed class PlainEntity
    {
        public int Id { get; set; }
    }
}
