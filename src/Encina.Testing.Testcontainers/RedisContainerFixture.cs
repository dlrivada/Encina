using Testcontainers.Redis;
using Xunit;

namespace Encina.Testing.Testcontainers;

/// <summary>
/// Pre-configured xUnit fixture for Redis using Testcontainers.
/// Provides a throwaway Redis instance for integration tests.
/// </summary>
/// <remarks>
/// <para>
/// This fixture manages the lifecycle of a Redis container by inheriting
/// common functionality from <see cref="ContainerFixtureBase{TContainer}"/>.
/// </para>
/// <para>
/// Use this fixture with xUnit's <c>IClassFixture&lt;T&gt;</c> pattern for shared
/// container across tests in a class, or <c>ICollectionFixture&lt;T&gt;</c> for
/// shared container across multiple test classes.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// public class CacheTests : IClassFixture&lt;RedisContainerFixture&gt;
/// {
///     private readonly RedisContainerFixture _redis;
///
///     public CacheTests(RedisContainerFixture redis) => _redis = redis;
///
///     [Fact]
///     public async Task SetValue_ShouldPersist()
///     {
///         var redis = ConnectionMultiplexer.Connect(_redis.ConnectionString);
///         var db = redis.GetDatabase();
///         // Test with real Redis...
///     }
/// }
/// </code>
/// </example>
public class RedisContainerFixture : ContainerFixtureBase<RedisContainer>
{
    /// <summary>
    /// Builds and configures the Redis container.
    /// </summary>
    /// <returns>A pre-configured Redis container.</returns>
    protected override RedisContainer BuildContainer()
    {
        return new RedisBuilder()
            .WithImage("redis:7-alpine")
            .WithCleanUp(true)
            .Build();
    }
}
