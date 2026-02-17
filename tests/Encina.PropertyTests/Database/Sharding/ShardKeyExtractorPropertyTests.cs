using Encina.Sharding;

using FsCheck;
using FsCheck.Xunit;

namespace Encina.PropertyTests.Database.Sharding;

/// <summary>
/// Property-based tests for <see cref="ShardKeyExtractor"/> invariants.
/// Verifies IShardable precedence, attribute extraction, and error handling.
/// </summary>
[Trait("Category", "Property")]
public sealed class ShardKeyExtractorPropertyTests
{
    #region IShardable Extraction

    [Property(MaxTest = 100)]
    public bool Property_Extract_IShardable_AlwaysReturnsKey(NonEmptyString key)
    {
        var entity = new ShardableEntity(key.Get);

        var result = ShardKeyExtractor.Extract(entity);

        if (!result.IsRight) return false;

        string extracted = string.Empty;
        _ = result.IfRight(s => extracted = s);

        return extracted == key.Get;
    }

    [Property(MaxTest = 100)]
    public bool Property_Extract_IShardable_Deterministic(NonEmptyString key)
    {
        var entity = new ShardableEntity(key.Get);

        var result1 = ShardKeyExtractor.Extract(entity);
        var result2 = ShardKeyExtractor.Extract(entity);

        return result1.IsRight && result2.IsRight && result1 == result2;
    }

    [Fact]
    [Trait("Category", "Property")]
    public void Property_Extract_IShardable_NullKeyReturnsError()
    {
        var entity = new ShardableEntity(null!);

        var result = ShardKeyExtractor.Extract(entity);

        Assert.True(result.IsLeft);
    }

    [Fact]
    [Trait("Category", "Property")]
    public void Property_Extract_IShardable_EmptyKeyReturnsError()
    {
        var entity = new ShardableEntity("");

        var result = ShardKeyExtractor.Extract(entity);

        Assert.True(result.IsLeft);
    }

    #endregion

    #region ShardKeyAttribute Extraction

    [Property(MaxTest = 100)]
    public bool Property_Extract_Attribute_AlwaysReturnsPropertyValue(NonEmptyString key)
    {
        var entity = new AttributeEntity { TenantId = key.Get };

        var result = ShardKeyExtractor.Extract(entity);

        if (!result.IsRight) return false;

        string extracted = string.Empty;
        _ = result.IfRight(s => extracted = s);

        return extracted == key.Get;
    }

    [Fact]
    [Trait("Category", "Property")]
    public void Property_Extract_Attribute_NullPropertyReturnsError()
    {
        var entity = new AttributeEntity { TenantId = null! };

        var result = ShardKeyExtractor.Extract(entity);

        Assert.True(result.IsLeft);
    }

    #endregion

    #region IShardable Precedence over Attribute

    [Property(MaxTest = 50)]
    public bool Property_Extract_IShardable_TakesPrecedenceOverAttribute(
        NonEmptyString shardableKey,
        NonEmptyString attributeKey)
    {
        var entity = new DualEntity
        {
            ShardKey = shardableKey.Get,
            AttributeKey = attributeKey.Get
        };

        var result = ShardKeyExtractor.Extract(entity);

        if (!result.IsRight) return false;

        string extracted = string.Empty;
        _ = result.IfRight(s => extracted = s);

        // IShardable should take precedence
        return extracted == shardableKey.Get;
    }

    #endregion

    #region No Configuration

    [Fact]
    [Trait("Category", "Property")]
    public void Property_Extract_NoShardKeyConfig_ReturnsError()
    {
        var entity = new PlainEntity { Name = "test" };

        var result = ShardKeyExtractor.Extract(entity);

        Assert.True(result.IsLeft);
    }

    #endregion

    #region Type Conversion

    [Property(MaxTest = 50)]
    public bool Property_Extract_Attribute_IntProperty_ConvertsViaToString(int value)
    {
        var entity = new IntKeyEntity { ShardNumber = value };

        var result = ShardKeyExtractor.Extract(entity);

        if (!result.IsRight) return false;

        string extracted = string.Empty;
        _ = result.IfRight(s => extracted = s);

        return extracted == value.ToString(System.Globalization.CultureInfo.InvariantCulture);
    }

    #endregion

    #region Test Entities

    private sealed class ShardableEntity : IShardable
    {
        private readonly string _key;
        public ShardableEntity(string key) => _key = key;
        public string GetShardKey() => _key;
    }

    private sealed class AttributeEntity
    {
        [ShardKey]
        public string TenantId { get; set; } = default!;
    }

    private sealed class DualEntity : IShardable
    {
        public string ShardKey { get; set; } = default!;

        [ShardKey]
        public string AttributeKey { get; set; } = default!;

        public string GetShardKey() => ShardKey;
    }

    private sealed class PlainEntity
    {
        public string Name { get; set; } = default!;
    }

    private sealed class IntKeyEntity
    {
        [ShardKey]
        public int ShardNumber { get; set; }
    }

    #endregion
}
