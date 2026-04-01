using Encina.Sharding;
using Encina.Sharding.Configuration;
using Encina.Sharding.Routing;

namespace Encina.GuardTests.Core.Sharding;

/// <summary>
/// Guard clause tests for <see cref="ShardingOptions{TEntity}"/>.
/// Verifies null/whitespace parameter handling, duplicate detection, and validation.
/// </summary>
public sealed class ShardingOptionsGuardTests
{
    #region AddShard Guards

    /// <summary>
    /// Verifies that AddShard throws when shard ID is null.
    /// </summary>
    [Fact]
    public void AddShard_NullShardId_ThrowsArgumentNullException()
    {
        var options = new ShardingOptions<TestShardEntity>();

        Should.Throw<ArgumentNullException>(() =>
            options.AddShard(null!, "Server=s0;Database=db;"));
    }

    /// <summary>
    /// Verifies that AddShard throws when connection string is null.
    /// </summary>
    [Fact]
    public void AddShard_NullConnectionString_ThrowsArgumentNullException()
    {
        var options = new ShardingOptions<TestShardEntity>();

        Should.Throw<ArgumentNullException>(() =>
            options.AddShard("shard-0", null!));
    }

    /// <summary>
    /// Verifies that AddShard with valid arguments succeeds and returns the options for chaining.
    /// </summary>
    [Fact]
    public void AddShard_ValidArguments_ReturnsSameInstance()
    {
        var options = new ShardingOptions<TestShardEntity>();

        var result = options.AddShard("shard-0", "Server=s0;Database=db;");

        result.ShouldBeSameAs(options);
    }

    /// <summary>
    /// Verifies that AddShard overwrites when the same shard ID is added again.
    /// </summary>
    [Fact]
    public void AddShard_DuplicateShardId_OverwritesShard()
    {
        var options = new ShardingOptions<TestShardEntity>();

        options.AddShard("shard-0", "Server=first;Database=db;");
        options.AddShard("shard-0", "Server=second;Database=db;");

        options.Shards.Count.ShouldBe(1);
    }

    #endregion

    #region WithShadowSharding Guards

    /// <summary>
    /// Verifies that WithShadowSharding with null configure succeeds (optional parameter).
    /// </summary>
    [Fact]
    public void WithShadowSharding_NullConfigure_Succeeds()
    {
        var options = new ShardingOptions<TestShardEntity>();

        Should.NotThrow(() => options.WithShadowSharding(null));

        options.UseShadowSharding.ShouldBeTrue();
    }

    /// <summary>
    /// Verifies that WithShadowSharding sets the flag and returns instance.
    /// </summary>
    [Fact]
    public void WithShadowSharding_ValidConfigure_SetsFlagAndReturns()
    {
        var options = new ShardingOptions<TestShardEntity>();

        var result = options.WithShadowSharding(shadow => { });

        result.ShouldBeSameAs(options);
        options.UseShadowSharding.ShouldBeTrue();
        options.ShadowSharding.ShouldNotBeNull();
    }

    #endregion

    #region WithResharding Guards

    /// <summary>
    /// Verifies that WithResharding with null configure succeeds (optional parameter).
    /// </summary>
    [Fact]
    public void WithResharding_NullConfigure_Succeeds()
    {
        var options = new ShardingOptions<TestShardEntity>();

        Should.NotThrow(() => options.WithResharding(null));

        options.UseResharding.ShouldBeTrue();
    }

    /// <summary>
    /// Verifies that WithResharding sets the flag and returns instance.
    /// </summary>
    [Fact]
    public void WithResharding_ValidConfigure_SetsFlagAndReturns()
    {
        var options = new ShardingOptions<TestShardEntity>();

        var result = options.WithResharding(builder => { });

        result.ShouldBeSameAs(options);
        options.UseResharding.ShouldBeTrue();
    }

    #endregion

    #region BuildRouter Guards

    /// <summary>
    /// Verifies that BuildRouter throws when no routing strategy is configured.
    /// </summary>
    [Fact]
    public void BuildRouter_NoRoutingStrategy_ThrowsInvalidOperationException()
    {
        var options = new ShardingOptions<TestShardEntity>();
        options.AddShard("shard-0", "Server=s0;Database=db;");

        var topology = options.BuildTopology();

        Should.Throw<InvalidOperationException>(() =>
            options.BuildRouter(topology));
    }

    #endregion

    #region AddColocatedEntity Guards

    /// <summary>
    /// Verifies that AddColocatedEntity returns the same instance for chaining.
    /// </summary>
    [Fact]
    public void AddColocatedEntity_ReturnsInstance()
    {
        var options = new ShardingOptions<TestShardEntity>();

        var result = options.AddColocatedEntity<AnotherShardEntity>();

        result.ShouldBeSameAs(options);
    }

    /// <summary>
    /// Verifies that AddColocatedEntity does not add duplicates.
    /// </summary>
    [Fact]
    public void AddColocatedEntity_DuplicateType_NoDuplicate()
    {
        var options = new ShardingOptions<TestShardEntity>();

        options.AddColocatedEntity<AnotherShardEntity>();
        options.AddColocatedEntity<AnotherShardEntity>();

        options.ColocatedEntityTypes.Count.ShouldBe(1);
    }

    #endregion

    #region AddReferenceTable Guards

    /// <summary>
    /// Verifies that AddReferenceTable with duplicate type throws.
    /// </summary>
    [Fact]
    public void AddReferenceTable_DuplicateType_ThrowsInvalidOperationException()
    {
        var options = new ShardingOptions<TestShardEntity>();

        options.AddReferenceTable<ReferenceEntity>();

        Should.Throw<InvalidOperationException>(() =>
            options.AddReferenceTable<ReferenceEntity>());
    }

    /// <summary>
    /// Verifies that AddReferenceTable returns instance for chaining.
    /// </summary>
    [Fact]
    public void AddReferenceTable_ValidType_ReturnsInstance()
    {
        var options = new ShardingOptions<TestShardEntity>();

        var result = options.AddReferenceTable<ReferenceEntity>();

        result.ShouldBeSameAs(options);
    }

    /// <summary>
    /// Verifies that AddReferenceTable with null configure succeeds (optional).
    /// </summary>
    [Fact]
    public void AddReferenceTable_NullConfigure_Succeeds()
    {
        var options = new ShardingOptions<TestShardEntity>();

        Should.NotThrow(() => options.AddReferenceTable<ReferenceEntity>(null));
    }

    #endregion

    #region UseHashRouting Guards

    /// <summary>
    /// Verifies that UseHashRouting with null configure succeeds (optional parameter).
    /// </summary>
    [Fact]
    public void UseHashRouting_NullConfigure_Succeeds()
    {
        var options = new ShardingOptions<TestShardEntity>();

        Should.NotThrow(() => options.UseHashRouting(null));
    }

    /// <summary>
    /// Verifies that UseHashRouting returns instance for chaining.
    /// </summary>
    [Fact]
    public void UseHashRouting_ReturnsInstance()
    {
        var options = new ShardingOptions<TestShardEntity>();

        var result = options.UseHashRouting();

        result.ShouldBeSameAs(options);
    }

    #endregion

    #region ScatterGather Options

    /// <summary>
    /// Verifies that ScatterGather is initialized by default.
    /// </summary>
    [Fact]
    public void ScatterGather_DefaultInstance_IsNotNull()
    {
        var options = new ShardingOptions<TestShardEntity>();

        options.ScatterGather.ShouldNotBeNull();
    }

    #endregion

    #region Test Helpers

    private sealed class TestShardEntity : IShardable
    {
        public string GetShardKey() => "test-key";
    }

    private sealed class AnotherShardEntity : IShardable
    {
        public string GetShardKey() => "another-key";
    }

    private sealed class ReferenceEntity;

    #endregion
}
