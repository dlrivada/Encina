using Encina.Sharding;
using Encina.Sharding.Routing;

namespace Encina.UnitTests.Core.Sharding.Routing;

/// <summary>
/// Unit tests for <see cref="GeoShardRouter"/>.
/// </summary>
public sealed class GeoShardRouterTests
{
    private static ShardTopology CreateTopology(params ShardInfo[] shards) =>
        new(shards);

    private static ShardTopology CreateDefaultTopology() =>
        CreateTopology(
            new ShardInfo("shard-us", "Server=us;Database=app"),
            new ShardInfo("shard-eu", "Server=eu;Database=app"),
            new ShardInfo("shard-ap", "Server=ap;Database=app"));

    /// <summary>
    /// Region resolver that extracts the substring before the first hyphen.
    /// For example, "us-east-customer-123" resolves to "us".
    /// </summary>
    private static Func<string, string> PrefixResolver =>
        key => key.Contains('-') ? key[..key.IndexOf('-')] : key;

    #region Constructor

    [Fact]
    public void Constructor_ValidInputs_ShouldCreateRouter()
    {
        // Arrange
        var topology = CreateDefaultTopology();
        var regions = new[]
        {
            new GeoRegion("us-east", "shard-us"),
            new GeoRegion("eu-west", "shard-eu")
        };

        // Act
        var router = new GeoShardRouter(topology, regions, PrefixResolver);

        // Assert
        router.ShouldNotBeNull();
        router.GetAllShardIds().Count.ShouldBe(3);
    }

    [Fact]
    public void Constructor_NullTopology_ShouldThrowArgumentNullException()
    {
        // Arrange
        var regions = new[] { new GeoRegion("us-east", "shard-us") };

        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            new GeoShardRouter(null!, regions, PrefixResolver));
    }

    [Fact]
    public void Constructor_NullRegions_ShouldThrowArgumentNullException()
    {
        // Arrange
        var topology = CreateDefaultTopology();

        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            new GeoShardRouter(topology, null!, PrefixResolver));
    }

    [Fact]
    public void Constructor_NullRegionResolver_ShouldThrowArgumentNullException()
    {
        // Arrange
        var topology = CreateDefaultTopology();
        var regions = new[] { new GeoRegion("us-east", "shard-us") };

        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            new GeoShardRouter(topology, regions, null!));
    }

    [Fact]
    public void Constructor_NullOptions_ShouldUseDefaults()
    {
        // Arrange
        var topology = CreateDefaultTopology();
        var regions = new[] { new GeoRegion("us-east", "shard-us") };
        Func<string, string> resolver = _ => "unknown-region";

        // Act
        var router = new GeoShardRouter(topology, regions, resolver, options: null);

        // Assert - default options: no RequireExactMatch, no DefaultRegion
        var result = router.GetShardId("any-key");
        result.IsLeft.ShouldBeTrue();
    }

    [Fact]
    public void Constructor_CustomOptions_ShouldRespectDefaultRegion()
    {
        // Arrange
        var topology = CreateDefaultTopology();
        var regions = new[]
        {
            new GeoRegion("us-east", "shard-us"),
            new GeoRegion("default-region", "shard-eu")
        };
        var options = new GeoShardRouterOptions { DefaultRegion = "default-region" };
        Func<string, string> resolver = _ => "unknown-region";

        // Act
        var router = new GeoShardRouter(topology, regions, resolver, options);
        var result = router.GetShardId("any-key");

        // Assert - should fall back to default region's shard
        result.IsRight.ShouldBeTrue();
        _ = result.Match(
            Right: shardId => shardId.ShouldBe("shard-eu"),
            Left: _ => throw new InvalidOperationException("Expected Right"));
    }

    #endregion

    #region GetShardId - Direct Match

    [Fact]
    public void GetShardId_KnownRegion_ShouldReturnRegionShard()
    {
        // Arrange
        var topology = CreateDefaultTopology();
        var regions = new[]
        {
            new GeoRegion("us-east", "shard-us"),
            new GeoRegion("eu-west", "shard-eu")
        };
        Func<string, string> resolver = _ => "us-east";
        var router = new GeoShardRouter(topology, regions, resolver);

        // Act
        var result = router.GetShardId("customer-123");

        // Assert
        result.IsRight.ShouldBeTrue();
        _ = result.Match(
            Right: shardId => shardId.ShouldBe("shard-us"),
            Left: _ => throw new InvalidOperationException("Expected Right"));
    }

    [Fact]
    public void GetShardId_SecondRegion_ShouldReturnCorrectShard()
    {
        // Arrange
        var topology = CreateDefaultTopology();
        var regions = new[]
        {
            new GeoRegion("us-east", "shard-us"),
            new GeoRegion("eu-west", "shard-eu"),
            new GeoRegion("ap-south", "shard-ap")
        };
        Func<string, string> resolver = _ => "ap-south";
        var router = new GeoShardRouter(topology, regions, resolver);

        // Act
        var result = router.GetShardId("order-456");

        // Assert
        result.IsRight.ShouldBeTrue();
        _ = result.Match(
            Right: shardId => shardId.ShouldBe("shard-ap"),
            Left: _ => throw new InvalidOperationException("Expected Right"));
    }

    #endregion

    #region GetShardId - Case Insensitive

    [Theory]
    [InlineData("US-EAST")]
    [InlineData("Us-East")]
    [InlineData("us-EAST")]
    [InlineData("uS-eAsT")]
    public void GetShardId_CaseInsensitiveRegion_ShouldMatchRegardlessOfCase(string resolvedRegion)
    {
        // Arrange
        var topology = CreateDefaultTopology();
        var regions = new[] { new GeoRegion("us-east", "shard-us") };
        Func<string, string> resolver = _ => resolvedRegion;
        var router = new GeoShardRouter(topology, regions, resolver);

        // Act
        var result = router.GetShardId("any-key");

        // Assert
        result.IsRight.ShouldBeTrue();
        _ = result.Match(
            Right: shardId => shardId.ShouldBe("shard-us"),
            Left: _ => throw new InvalidOperationException("Expected Right"));
    }

    #endregion

    #region GetShardId - Fallback Chain

    [Fact]
    public void GetShardId_UnknownRegion_FallsToDefaultRegion()
    {
        // Arrange
        // When resolver returns a region that is NOT in the dictionary, the fallback
        // loop cannot traverse because the initial TryGetValue for that unknown region
        // also fails. The router then falls through to TryDefaultRegion.
        var topology = CreateDefaultTopology();
        var regions = new[]
        {
            new GeoRegion("us-east", "shard-us", FallbackRegionCode: "us-west"),
            new GeoRegion("us-west", "shard-us")
        };
        var options = new GeoShardRouterOptions { DefaultRegion = "us-west" };
        Func<string, string> resolver = _ => "us-central"; // Not in regions
        var router = new GeoShardRouter(topology, regions, resolver, options);

        // Act
        var result = router.GetShardId("any-key");

        // Assert
        result.IsRight.ShouldBeTrue();
        _ = result.Match(
            Right: shardId => shardId.ShouldBe("shard-us"),
            Left: _ => throw new InvalidOperationException("Expected Right"));
    }

    [Fact]
    public void GetShardId_DirectMatch_IgnoresFallbackChain()
    {
        // Arrange
        // When a region IS found, its shard is returned directly.
        // The FallbackRegionCode is not consulted for direct matches.
        var topology = CreateDefaultTopology();
        var regions = new[]
        {
            new GeoRegion("eu-west", "shard-eu", FallbackRegionCode: "us-east"),
            new GeoRegion("us-east", "shard-us")
        };
        Func<string, string> resolver = _ => "eu-west";
        var router = new GeoShardRouter(topology, regions, resolver);

        // Act
        var result = router.GetShardId("any-key");

        // Assert - should return eu-west's shard directly, not follow fallback to us-east
        result.IsRight.ShouldBeTrue();
        _ = result.Match(
            Right: shardId => shardId.ShouldBe("shard-eu"),
            Left: _ => throw new InvalidOperationException("Expected Right"));
    }

    #endregion

    #region GetShardId - Cycle Detection

    [Fact]
    public void GetShardId_FallbackCycleInRegions_ShouldNotInfiniteLoop()
    {
        // Arrange
        // Even with circular fallback references, the router should terminate.
        var topology = CreateDefaultTopology();
        var regions = new[]
        {
            new GeoRegion("region-a", "shard-us", FallbackRegionCode: "region-b"),
            new GeoRegion("region-b", "shard-eu", FallbackRegionCode: "region-a")
        };
        Func<string, string> resolver = _ => "unknown";
        var router = new GeoShardRouter(topology, regions, resolver);

        // Act - should terminate and return Left error
        var result = router.GetShardId("some-key");

        // Assert
        result.IsLeft.ShouldBeTrue();
        _ = result.Match(
            Right: _ => throw new InvalidOperationException("Expected Left"),
            Left: error =>
            {
                error.GetCode().IsSome.ShouldBeTrue();
                error.GetCode().IfSome(c => c.ShouldBe(ShardingErrorCodes.RegionNotFound));
                return Unit.Default;
            });
    }

    [Fact]
    public void GetShardId_SelfReferencingFallback_ShouldNotInfiniteLoop()
    {
        // Arrange
        var topology = CreateDefaultTopology();
        var regions = new[]
        {
            new GeoRegion("region-a", "shard-us", FallbackRegionCode: "region-a")
        };
        Func<string, string> resolver = _ => "unknown";
        var router = new GeoShardRouter(topology, regions, resolver);

        // Act
        var result = router.GetShardId("some-key");

        // Assert
        result.IsLeft.ShouldBeTrue();
    }

    #endregion

    #region GetShardId - RequireExactMatch

    [Fact]
    public void GetShardId_RequireExactMatch_UnknownRegion_ShouldReturnError()
    {
        // Arrange
        var topology = CreateDefaultTopology();
        var regions = new[]
        {
            new GeoRegion("us-east", "shard-us"),
            new GeoRegion("default-region", "shard-eu")
        };
        var options = new GeoShardRouterOptions
        {
            RequireExactMatch = true,
            DefaultRegion = "default-region" // Should NOT be used when RequireExactMatch is true
        };
        Func<string, string> resolver = _ => "unknown-region";
        var router = new GeoShardRouter(topology, regions, resolver, options);

        // Act
        var result = router.GetShardId("some-key");

        // Assert
        result.IsLeft.ShouldBeTrue();
        _ = result.Match(
            Right: _ => throw new InvalidOperationException("Expected Left"),
            Left: error =>
            {
                error.GetCode().IsSome.ShouldBeTrue();
                error.GetCode().IfSome(c => c.ShouldBe(ShardingErrorCodes.RegionNotFound));
                error.Message.ShouldContain("exact match is required");
                return Unit.Default;
            });
    }

    [Fact]
    public void GetShardId_RequireExactMatch_KnownRegion_ShouldReturnShard()
    {
        // Arrange
        var topology = CreateDefaultTopology();
        var regions = new[] { new GeoRegion("us-east", "shard-us") };
        var options = new GeoShardRouterOptions { RequireExactMatch = true };
        Func<string, string> resolver = _ => "us-east";
        var router = new GeoShardRouter(topology, regions, resolver, options);

        // Act
        var result = router.GetShardId("some-key");

        // Assert
        result.IsRight.ShouldBeTrue();
        _ = result.Match(
            Right: shardId => shardId.ShouldBe("shard-us"),
            Left: _ => throw new InvalidOperationException("Expected Right"));
    }

    #endregion

    #region GetShardId - Default Region

    [Fact]
    public void GetShardId_DefaultRegionConfigured_UnknownRegion_ShouldUseDefault()
    {
        // Arrange
        var topology = CreateDefaultTopology();
        var regions = new[]
        {
            new GeoRegion("us-east", "shard-us"),
            new GeoRegion("fallback", "shard-eu")
        };
        var options = new GeoShardRouterOptions { DefaultRegion = "fallback" };
        Func<string, string> resolver = _ => "unknown";
        var router = new GeoShardRouter(topology, regions, resolver, options);

        // Act
        var result = router.GetShardId("some-key");

        // Assert
        result.IsRight.ShouldBeTrue();
        _ = result.Match(
            Right: shardId => shardId.ShouldBe("shard-eu"),
            Left: _ => throw new InvalidOperationException("Expected Right"));
    }

    [Fact]
    public void GetShardId_DefaultRegionNotInDictionary_ShouldReturnError()
    {
        // Arrange
        var topology = CreateDefaultTopology();
        var regions = new[] { new GeoRegion("us-east", "shard-us") };
        var options = new GeoShardRouterOptions { DefaultRegion = "non-existent-default" };
        Func<string, string> resolver = _ => "unknown";
        var router = new GeoShardRouter(topology, regions, resolver, options);

        // Act
        var result = router.GetShardId("some-key");

        // Assert
        result.IsLeft.ShouldBeTrue();
        _ = result.Match(
            Right: _ => throw new InvalidOperationException("Expected Left"),
            Left: error =>
            {
                error.GetCode().IsSome.ShouldBeTrue();
                error.GetCode().IfSome(c => c.ShouldBe(ShardingErrorCodes.RegionNotFound));
                return Unit.Default;
            });
    }

    [Fact]
    public void GetShardId_ResolverReturnsEmpty_DefaultConfigured_ShouldUseDefault()
    {
        // Arrange
        var topology = CreateDefaultTopology();
        var regions = new[]
        {
            new GeoRegion("us-east", "shard-us"),
            new GeoRegion("default-region", "shard-ap")
        };
        var options = new GeoShardRouterOptions { DefaultRegion = "default-region" };
        Func<string, string> resolver = _ => string.Empty;
        var router = new GeoShardRouter(topology, regions, resolver, options);

        // Act
        var result = router.GetShardId("some-key");

        // Assert
        result.IsRight.ShouldBeTrue();
        _ = result.Match(
            Right: shardId => shardId.ShouldBe("shard-ap"),
            Left: _ => throw new InvalidOperationException("Expected Right"));
    }

    [Fact]
    public void GetShardId_ResolverReturnsNull_DefaultConfigured_ShouldUseDefault()
    {
        // Arrange
        var topology = CreateDefaultTopology();
        var regions = new[]
        {
            new GeoRegion("us-east", "shard-us"),
            new GeoRegion("default-region", "shard-ap")
        };
        var options = new GeoShardRouterOptions { DefaultRegion = "default-region" };
        Func<string, string> resolver = _ => null!;
        var router = new GeoShardRouter(topology, regions, resolver, options);

        // Act
        var result = router.GetShardId("some-key");

        // Assert
        result.IsRight.ShouldBeTrue();
        _ = result.Match(
            Right: shardId => shardId.ShouldBe("shard-ap"),
            Left: _ => throw new InvalidOperationException("Expected Right"));
    }

    #endregion

    #region GetShardId - No Match

    [Fact]
    public void GetShardId_UnknownRegion_NoFallback_NoDefault_ShouldReturnError()
    {
        // Arrange
        var topology = CreateDefaultTopology();
        var regions = new[] { new GeoRegion("us-east", "shard-us") };
        Func<string, string> resolver = _ => "unknown-region";
        var router = new GeoShardRouter(topology, regions, resolver);

        // Act
        var result = router.GetShardId("some-key");

        // Assert
        result.IsLeft.ShouldBeTrue();
        _ = result.Match(
            Right: _ => throw new InvalidOperationException("Expected Left"),
            Left: error =>
            {
                error.GetCode().IsSome.ShouldBeTrue();
                error.GetCode().IfSome(c => c.ShouldBe(ShardingErrorCodes.RegionNotFound));
                error.Message.ShouldContain("unknown-region");
                error.Message.ShouldContain("not found");
                return Unit.Default;
            });
    }

    [Fact]
    public void GetShardId_ResolverReturnsEmpty_NoDefault_ShouldReturnError()
    {
        // Arrange
        var topology = CreateDefaultTopology();
        var regions = new[] { new GeoRegion("us-east", "shard-us") };
        Func<string, string> resolver = _ => string.Empty;
        var router = new GeoShardRouter(topology, regions, resolver);

        // Act
        var result = router.GetShardId("some-key");

        // Assert
        result.IsLeft.ShouldBeTrue();
        _ = result.Match(
            Right: _ => throw new InvalidOperationException("Expected Left"),
            Left: error =>
            {
                error.GetCode().IsSome.ShouldBeTrue();
                error.GetCode().IfSome(c => c.ShouldBe(ShardingErrorCodes.RegionNotFound));
                error.Message.ShouldContain("no default region");
                return Unit.Default;
            });
    }

    #endregion

    #region GetShardId - Null Key

    [Fact]
    public void GetShardId_NullKey_ShouldThrowArgumentNullException()
    {
        // Arrange
        var topology = CreateDefaultTopology();
        var regions = new[] { new GeoRegion("us-east", "shard-us") };
        var router = new GeoShardRouter(topology, regions, PrefixResolver);

        // Act & Assert
        Should.Throw<ArgumentNullException>(() => router.GetShardId(null!));
    }

    #endregion

    #region GetShardId - Multiple Regions Same Shard

    [Fact]
    public void GetShardId_MultipleRegionsSameShard_ShouldAllRouteToSameShard()
    {
        // Arrange
        var topology = CreateTopology(
            new ShardInfo("shard-us", "Server=us;Database=app"));
        var regions = new[]
        {
            new GeoRegion("us-east", "shard-us"),
            new GeoRegion("us-west", "shard-us"),
            new GeoRegion("us-central", "shard-us")
        };
        var router = new GeoShardRouter(topology, regions, key => key);

        // Act & Assert
        var resultEast = router.GetShardId("us-east");
        resultEast.IsRight.ShouldBeTrue();
        _ = resultEast.Match(
            Right: s => s.ShouldBe("shard-us"),
            Left: _ => throw new InvalidOperationException("Expected Right"));

        var resultWest = router.GetShardId("us-west");
        resultWest.IsRight.ShouldBeTrue();
        _ = resultWest.Match(
            Right: s => s.ShouldBe("shard-us"),
            Left: _ => throw new InvalidOperationException("Expected Right"));

        var resultCentral = router.GetShardId("us-central");
        resultCentral.IsRight.ShouldBeTrue();
        _ = resultCentral.Match(
            Right: s => s.ShouldBe("shard-us"),
            Left: _ => throw new InvalidOperationException("Expected Right"));
    }

    #endregion

    #region GetShardId - Empty Regions

    [Fact]
    public void GetShardId_EmptyRegions_ShouldAlwaysReturnError()
    {
        // Arrange
        var topology = CreateDefaultTopology();
        var regions = Array.Empty<GeoRegion>();
        Func<string, string> resolver = _ => "any-region";
        var router = new GeoShardRouter(topology, regions, resolver);

        // Act
        var result = router.GetShardId("any-key");

        // Assert
        result.IsLeft.ShouldBeTrue();
        _ = result.Match(
            Right: _ => throw new InvalidOperationException("Expected Left"),
            Left: error =>
            {
                error.GetCode().IsSome.ShouldBeTrue();
                error.GetCode().IfSome(c => c.ShouldBe(ShardingErrorCodes.RegionNotFound));
                return Unit.Default;
            });
    }

    [Fact]
    public void GetShardId_EmptyRegions_WithDefaultConfigured_ShouldStillReturnError()
    {
        // Arrange - default region is set but no regions exist, so default lookup fails
        var topology = CreateDefaultTopology();
        var regions = Array.Empty<GeoRegion>();
        var options = new GeoShardRouterOptions { DefaultRegion = "us-east" };
        Func<string, string> resolver = _ => "any-region";
        var router = new GeoShardRouter(topology, regions, resolver, options);

        // Act
        var result = router.GetShardId("any-key");

        // Assert
        result.IsLeft.ShouldBeTrue();
    }

    #endregion

    #region GetShardId - Custom Region Resolver

    [Fact]
    public void GetShardId_CustomResolver_ShouldExtractRegionFromComplexKey()
    {
        // Arrange
        var topology = CreateDefaultTopology();
        var regions = new[] { new GeoRegion("eu-west", "shard-eu") };
        // Resolver that extracts the segment after the last colon
        Func<string, string> resolver = key =>
        {
            var lastColon = key.LastIndexOf(':');
            return lastColon >= 0 ? key[(lastColon + 1)..] : key;
        };
        var router = new GeoShardRouter(topology, regions, resolver);

        // Act
        var result = router.GetShardId("customer:12345:eu-west");

        // Assert
        result.IsRight.ShouldBeTrue();
        _ = result.Match(
            Right: shardId => shardId.ShouldBe("shard-eu"),
            Left: _ => throw new InvalidOperationException("Expected Right"));
    }

    [Fact]
    public void GetShardId_ResolverUsesKeyCorrectly_ShouldPassFullKeyToResolver()
    {
        // Arrange
        var topology = CreateDefaultTopology();
        var regions = new[] { new GeoRegion("us-east", "shard-us") };
        string? capturedKey = null;
        Func<string, string> resolver = key =>
        {
            capturedKey = key;
            return "us-east";
        };
        var router = new GeoShardRouter(topology, regions, resolver);

        // Act
        var result = router.GetShardId("my-full-shard-key");

        // Assert
        result.IsRight.ShouldBeTrue();
        capturedKey.ShouldBe("my-full-shard-key");
    }

    #endregion

    #region GetAllShardIds

    [Fact]
    public void GetAllShardIds_ShouldReturnAllTopologyShards()
    {
        // Arrange
        var topology = CreateDefaultTopology();
        var regions = new[] { new GeoRegion("us-east", "shard-us") };
        var router = new GeoShardRouter(topology, regions, PrefixResolver);

        // Act
        var allShardIds = router.GetAllShardIds();

        // Assert
        allShardIds.Count.ShouldBe(3);
        allShardIds.ShouldContain("shard-us");
        allShardIds.ShouldContain("shard-eu");
        allShardIds.ShouldContain("shard-ap");
    }

    [Fact]
    public void GetAllShardIds_SingleShard_ShouldReturnOne()
    {
        // Arrange
        var topology = CreateTopology(new ShardInfo("shard-1", "Server=1;Database=app"));
        var regions = new[] { new GeoRegion("region-a", "shard-1") };
        var router = new GeoShardRouter(topology, regions, PrefixResolver);

        // Act
        var allShardIds = router.GetAllShardIds();

        // Assert
        allShardIds.Count.ShouldBe(1);
        allShardIds.ShouldContain("shard-1");
    }

    #endregion

    #region GetShardConnectionString

    [Fact]
    public void GetShardConnectionString_ValidShardId_ShouldReturnConnectionString()
    {
        // Arrange
        var topology = CreateDefaultTopology();
        var regions = new[] { new GeoRegion("us-east", "shard-us") };
        var router = new GeoShardRouter(topology, regions, PrefixResolver);

        // Act
        var result = router.GetShardConnectionString("shard-us");

        // Assert
        result.IsRight.ShouldBeTrue();
        _ = result.Match(
            Right: cs => cs.ShouldBe("Server=us;Database=app"),
            Left: _ => throw new InvalidOperationException("Expected Right"));
    }

    [Fact]
    public void GetShardConnectionString_UnknownShardId_ShouldReturnError()
    {
        // Arrange
        var topology = CreateDefaultTopology();
        var regions = new[] { new GeoRegion("us-east", "shard-us") };
        var router = new GeoShardRouter(topology, regions, PrefixResolver);

        // Act
        var result = router.GetShardConnectionString("shard-nonexistent");

        // Assert
        result.IsLeft.ShouldBeTrue();
        _ = result.Match(
            Right: _ => throw new InvalidOperationException("Expected Left"),
            Left: error =>
            {
                error.GetCode().IsSome.ShouldBeTrue();
                error.GetCode().IfSome(c => c.ShouldBe(ShardingErrorCodes.ShardNotFound));
                return Unit.Default;
            });
    }

    [Fact]
    public void GetShardConnectionString_NullShardId_ShouldThrowArgumentNullException()
    {
        // Arrange
        var topology = CreateDefaultTopology();
        var regions = new[] { new GeoRegion("us-east", "shard-us") };
        var router = new GeoShardRouter(topology, regions, PrefixResolver);

        // Act & Assert
        Should.Throw<ArgumentNullException>(() => router.GetShardConnectionString(null!));
    }

    #endregion

    #region GeoRegion Record Validation

    [Fact]
    public void GeoRegion_NullRegionCode_ShouldThrowArgumentException()
    {
        // Act & Assert
        Should.Throw<ArgumentException>(() => new GeoRegion(null!, "shard-1"));
    }

    [Fact]
    public void GeoRegion_WhitespaceRegionCode_ShouldThrowArgumentException()
    {
        // Act & Assert
        Should.Throw<ArgumentException>(() => new GeoRegion("   ", "shard-1"));
    }

    [Fact]
    public void GeoRegion_NullShardId_ShouldThrowArgumentException()
    {
        // Act & Assert
        Should.Throw<ArgumentException>(() => new GeoRegion("us-east", null!));
    }

    [Fact]
    public void GeoRegion_WhitespaceShardId_ShouldThrowArgumentException()
    {
        // Act & Assert
        Should.Throw<ArgumentException>(() => new GeoRegion("us-east", "   "));
    }

    [Fact]
    public void GeoRegion_ValidInputs_ShouldSetProperties()
    {
        // Act
        var region = new GeoRegion("us-east", "shard-us", "eu-west");

        // Assert
        region.RegionCode.ShouldBe("us-east");
        region.ShardId.ShouldBe("shard-us");
        region.FallbackRegionCode.ShouldBe("eu-west");
    }

    [Fact]
    public void GeoRegion_NoFallback_ShouldHaveNullFallback()
    {
        // Act
        var region = new GeoRegion("us-east", "shard-us");

        // Assert
        region.FallbackRegionCode.ShouldBeNull();
    }

    #endregion

    #region GeoShardRouterOptions

    [Fact]
    public void GeoShardRouterOptions_Defaults_ShouldHaveExpectedValues()
    {
        // Act
        var options = new GeoShardRouterOptions();

        // Assert
        options.DefaultRegion.ShouldBeNull();
        options.RequireExactMatch.ShouldBeFalse();
    }

    #endregion
}
