using Testcontainers.MySql;
using Xunit;

namespace Encina.Testing.Testcontainers;

/// <summary>
/// Pre-configured xUnit fixture for MySQL using Testcontainers.
/// Provides a throwaway MySQL instance for integration tests.
/// </summary>
/// <remarks>
/// <para>
/// This fixture manages the lifecycle of a MySQL container by inheriting
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
/// public class OrderRepositoryTests : IClassFixture&lt;MySqlContainerFixture&gt;
/// {
///     private readonly MySqlContainerFixture _db;
///
///     public OrderRepositoryTests(MySqlContainerFixture db) => _db = db;
///
///     [Fact]
///     public async Task CreateOrder_ShouldPersist()
///     {
///         await using var connection = new MySqlConnection(_db.ConnectionString);
///         await connection.OpenAsync();
///         // Test with real MySQL...
///     }
/// }
/// </code>
/// </example>
public class MySqlContainerFixture : ContainerFixtureBase<MySqlContainer>
{
    /// <summary>
    /// Builds and configures the MySQL container.
    /// </summary>
    /// <returns>A pre-configured MySQL container.</returns>
    protected override MySqlContainer BuildContainer()
    {
        return new MySqlBuilder()
            .WithImage("mysql:9.1")
            .WithDatabase("encina_test")
            .WithUsername("encina")
            .WithPassword("StrongP@ssw0rd!")
            .WithCleanUp(true)
            .Build();
    }
}
