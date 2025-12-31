using Encina.Testing.Respawn;
using Xunit;

namespace Encina.Testing.Testcontainers;

/// <summary>
/// Base class for PostgreSQL integration tests with automatic database cleanup.
/// </summary>
/// <remarks>
/// <para>
/// This class provides a pre-configured PostgreSQL integration testing setup by:
/// </para>
/// <list type="bullet">
/// <item>Using <see cref="PostgreSqlContainerFixture"/> to spin up a real PostgreSQL instance in Docker</item>
/// <item>Using Respawn to efficiently clean up data between tests</item>
/// <item>Implementing xUnit's <see cref="IAsyncLifetime"/> for proper lifecycle management</item>
/// </list>
/// <para>
/// The database is reset before each test runs, ensuring test isolation.
/// Use this class with xUnit's <see cref="IClassFixture{TFixture}"/> to share the container
/// across all tests in the class.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// public class OrderRepositoryTests : PostgreSqlIntegrationTestBase
/// {
///     public OrderRepositoryTests(PostgreSqlContainerFixture fixture) : base(fixture) { }
///
///     [Fact]
///     public async Task CreateOrder_ShouldPersist()
///     {
///         // Database is clean at the start of each test
///         await using var connection = new NpgsqlConnection(ConnectionString);
///         await connection.OpenAsync();
///
///         // Insert test data and verify
///         await connection.ExecuteAsync("INSERT INTO orders (id, name) VALUES (1, 'Test')");
///         var count = await connection.ExecuteScalarAsync&lt;int&gt;("SELECT COUNT(*) FROM orders");
///         Assert.Equal(1, count);
///     }
/// }
/// </code>
/// </example>
public abstract class PostgreSqlIntegrationTestBase
    : DatabaseIntegrationTestBase<PostgreSqlContainerFixture>, IClassFixture<PostgreSqlContainerFixture>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PostgreSqlIntegrationTestBase"/> class.
    /// </summary>
    /// <param name="fixture">The PostgreSQL container fixture provided by xUnit.</param>
    protected PostgreSqlIntegrationTestBase(PostgreSqlContainerFixture fixture)
        : base(fixture)
    {
    }

    /// <summary>
    /// Gets the PostgreSQL container for advanced operations.
    /// </summary>
    protected PostgreSqlContainerFixture PostgreSqlFixture => Fixture;
}
