using Encina.Security.Secrets.Caching;
using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;

namespace Encina.GuardTests.Security.Secrets.Caching;

/// <summary>
/// Guard clause tests for <see cref="SecretCachePubSubHostedService"/>.
/// Verifies that null arguments are properly rejected in the constructor.
/// </summary>
public sealed class SecretCachePubSubHostedServiceGuardTests
{
    #region Constructor Guards

    [Fact]
    public void Constructor_NullCache_ThrowsArgumentNullException()
    {
        var act = () => new SecretCachePubSubHostedService(
            null!,
            Substitute.For<IPubSubProvider>(),
            new SecretCachingOptions(),
            NullLogger<SecretCachePubSubHostedService>.Instance);

        Should.Throw<ArgumentNullException>(act)
            .ParamName.ShouldBe("cache");
    }

    [Fact]
    public void Constructor_NullOptions_ThrowsArgumentNullException()
    {
        var act = () => new SecretCachePubSubHostedService(
            Substitute.For<ICacheProvider>(),
            Substitute.For<IPubSubProvider>(),
            null!,
            NullLogger<SecretCachePubSubHostedService>.Instance);

        Should.Throw<ArgumentNullException>(act)
            .ParamName.ShouldBe("options");
    }

    [Fact]
    public void Constructor_NullLogger_ThrowsArgumentNullException()
    {
        var act = () => new SecretCachePubSubHostedService(
            Substitute.For<ICacheProvider>(),
            Substitute.For<IPubSubProvider>(),
            new SecretCachingOptions(),
            null!);

        Should.Throw<ArgumentNullException>(act)
            .ParamName.ShouldBe("logger");
    }

    [Fact]
    public void Constructor_NullPubSub_DoesNotThrow()
    {
        // PubSub is explicitly nullable — no guard expected
        var act = () => new SecretCachePubSubHostedService(
            Substitute.For<ICacheProvider>(),
            null,
            new SecretCachingOptions(),
            NullLogger<SecretCachePubSubHostedService>.Instance);

        Should.NotThrow(act);
    }

    [Fact]
    public void Constructor_EmptyInvalidationChannel_ThrowsArgumentException()
    {
        var options = new SecretCachingOptions { InvalidationChannel = "" };

        var act = () => new SecretCachePubSubHostedService(
            Substitute.For<ICacheProvider>(),
            Substitute.For<IPubSubProvider>(),
            options,
            NullLogger<SecretCachePubSubHostedService>.Instance);

        Should.Throw<ArgumentException>(act);
    }

    [Fact]
    public void Constructor_EmptyCacheKeyPrefix_ThrowsArgumentException()
    {
        var options = new SecretCachingOptions { CacheKeyPrefix = "" };

        var act = () => new SecretCachePubSubHostedService(
            Substitute.For<ICacheProvider>(),
            Substitute.For<IPubSubProvider>(),
            options,
            NullLogger<SecretCachePubSubHostedService>.Instance);

        Should.Throw<ArgumentException>(act);
    }

    #endregion
}
