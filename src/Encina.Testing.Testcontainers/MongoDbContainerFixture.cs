using Testcontainers.MongoDb;
using Xunit;

namespace Encina.Testing.Testcontainers;

/// <summary>
/// Pre-configured xUnit fixture for MongoDB using Testcontainers.
/// Provides a throwaway MongoDB instance for integration tests.
/// </summary>
/// <remarks>
/// <para>
/// This fixture manages the lifecycle of a MongoDB container by inheriting
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
/// public class OrderRepositoryTests : IClassFixture&lt;MongoDbContainerFixture&gt;
/// {
///     private readonly MongoDbContainerFixture _db;
///
///     public OrderRepositoryTests(MongoDbContainerFixture db) => _db = db;
///
///     [Fact]
///     public async Task CreateOrder_ShouldPersist()
///     {
///         var client = new MongoClient(_db.ConnectionString);
///         var database = client.GetDatabase("test");
///         // Test with real MongoDB...
///     }
/// }
/// </code>
/// </example>
public class MongoDbContainerFixture : ContainerFixtureBase<MongoDbContainer>
{
    /// <summary>
    /// Builds and configures the MongoDB container.
    /// </summary>
    /// <returns>A pre-configured MongoDB container.</returns>
    protected override MongoDbContainer BuildContainer()
    {
        return new MongoDbBuilder()
            .WithImage("mongo:8.1")
            .WithCleanUp(true)
            .Build();
    }
}
