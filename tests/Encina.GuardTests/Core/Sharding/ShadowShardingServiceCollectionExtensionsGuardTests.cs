using Encina.Sharding.Shadow;

namespace Encina.GuardTests.Core.Sharding;

/// <summary>
/// Guard clause tests for <see cref="ShadowShardingServiceCollectionExtensions"/>.
/// </summary>
public sealed class ShadowShardingServiceCollectionExtensionsGuardTests
{
    [Fact]
    public void AddShadowSharding_NullServices_ThrowsArgumentNullException()
    {
        Should.Throw<ArgumentNullException>(() =>
            ShadowShardingServiceCollectionExtensions.AddShadowSharding(null!, new ShadowShardingOptions()));
    }

    [Fact]
    public void AddShadowSharding_NullOptions_ThrowsArgumentNullException()
    {
        Should.Throw<ArgumentNullException>(() =>
            ShadowShardingServiceCollectionExtensions.AddShadowSharding(new ServiceCollection(), null!));
    }
}
