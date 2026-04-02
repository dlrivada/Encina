using Encina.MongoDB.Sharding;
using Shouldly;

namespace Encina.UnitTests.MongoDB.Sharding;

/// <summary>
/// Unit tests for <see cref="MongoDbShardingOptions{TEntity, TId}"/>.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Provider", "MongoDB")]
public sealed class MongoDbShardingOptionsTests
{
    #region Default Values

    [Fact]
    public void DefaultValues_AreCorrect()
    {
        var options = new MongoDbShardingOptions<TestEntity, Guid>();

        options.UseNativeSharding.ShouldBeTrue();
        options.ShardKeyField.ShouldBeNull();
        options.CollectionName.ShouldBeNull();
        options.IdProperty.ShouldBeNull();
        options.DatabaseName.ShouldBeNull();
    }

    #endregion

    #region Properties

    [Fact]
    public void Properties_CanBeSet()
    {
        var options = new MongoDbShardingOptions<TestEntity, Guid>
        {
            UseNativeSharding = false,
            ShardKeyField = "customerId",
            CollectionName = "custom_orders",
            DatabaseName = "orders_db",
            IdProperty = e => e.Id,
        };

        options.UseNativeSharding.ShouldBeFalse();
        options.ShardKeyField.ShouldBe("customerId");
        options.CollectionName.ShouldBe("custom_orders");
        options.DatabaseName.ShouldBe("orders_db");
        options.IdProperty.ShouldNotBeNull();
    }

    #endregion

    #region GetEffectiveCollectionName

    [Fact]
    public void GetEffectiveCollectionName_WhenExplicitlySet_ReturnsConfiguredName()
    {
        var options = new MongoDbShardingOptions<TestEntity, Guid>
        {
            CollectionName = "my_collection"
        };

        options.GetEffectiveCollectionName().ShouldBe("my_collection");
    }

    [Fact]
    public void GetEffectiveCollectionName_WhenNotSet_ReturnsLowercaseEntityNameWithS()
    {
        var options = new MongoDbShardingOptions<TestEntity, Guid>();

        options.GetEffectiveCollectionName().ShouldBe("testentitys");
    }

    [Fact]
    public void GetEffectiveCollectionName_WhenWhitespace_ReturnsLowercaseEntityNameWithS()
    {
        var options = new MongoDbShardingOptions<TestEntity, Guid>
        {
            CollectionName = "   "
        };

        options.GetEffectiveCollectionName().ShouldBe("testentitys");
    }

    #endregion

    #region Validate

    [Fact]
    public void Validate_WithIdProperty_DoesNotThrow()
    {
        var options = new MongoDbShardingOptions<TestEntity, Guid>
        {
            IdProperty = e => e.Id
        };

        Should.NotThrow(() => options.Validate());
    }

    [Fact]
    public void Validate_WithoutIdProperty_ThrowsInvalidOperationException()
    {
        var options = new MongoDbShardingOptions<TestEntity, Guid>();

        var ex = Should.Throw<InvalidOperationException>(() => options.Validate());
        ex.Message.ShouldContain("IdProperty must be configured");
        ex.Message.ShouldContain("TestEntity");
    }

    #endregion

    #region Test entities

    public sealed class TestEntity
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    #endregion
}
