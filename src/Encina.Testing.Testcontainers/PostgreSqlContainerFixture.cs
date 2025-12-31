using Testcontainers.PostgreSql;
using Xunit;

namespace Encina.Testing.Testcontainers;

/// <summary>
/// Pre-configured xUnit fixture for PostgreSQL using Testcontainers.
/// Provides a throwaway PostgreSQL instance for integration tests.
/// </summary>
/// <remarks>
/// <para>
/// This fixture manages the lifecycle of a PostgreSQL container by inheriting
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
/// public class OrderRepositoryTests : IClassFixture&lt;PostgreSqlContainerFixture&gt;
/// {
///     private readonly PostgreSqlContainerFixture _db;
///
///     public OrderRepositoryTests(PostgreSqlContainerFixture db) => _db = db;
///
///     [Fact]
///     public async Task CreateOrder_ShouldPersist()
///     {
///         await using var connection = new NpgsqlConnection(_db.ConnectionString);
///         await connection.OpenAsync();
///         // Test with real PostgreSQL...
///     }
/// }
/// </code>
/// </example>
public class PostgreSqlContainerFixture : ContainerFixtureBase<PostgreSqlContainer>
{
    /// <summary>
    /// Builds and configures the PostgreSQL container.
    /// </summary>
    /// <returns>A pre-configured PostgreSQL container.</returns>
    protected override PostgreSqlContainer BuildContainer()
    {
        return new PostgreSqlBuilder()
            .WithImage("postgres:17-alpine")
            .WithDatabase("encina_test")
            .WithUsername("encina")
            .WithPassword("StrongP@ssw0rd!")
            .WithCleanUp(true)
            .Build();
    }
}
