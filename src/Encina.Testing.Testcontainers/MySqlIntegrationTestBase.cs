using Encina.Testing.Respawn;
using Xunit;

namespace Encina.Testing.Testcontainers;

/// <summary>
/// Base class for MySQL integration tests with automatic database cleanup.
/// </summary>
/// <remarks>
/// <para>
/// This class provides a pre-configured MySQL integration testing setup by:
/// </para>
/// <list type="bullet">
/// <item>Using <see cref="MySqlContainerFixture"/> to spin up a real MySQL instance in Docker</item>
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
/// public class OrderRepositoryTests : MySqlIntegrationTestBase
/// {
///     public OrderRepositoryTests(MySqlContainerFixture fixture) : base(fixture) { }
///
///     [Fact]
///     public async Task CreateOrder_ShouldPersist()
///     {
///         // Database is clean at the start of each test
///         await using var connection = new MySqlConnection(ConnectionString);
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
public abstract class MySqlIntegrationTestBase
    : DatabaseIntegrationTestBase<MySqlContainerFixture>, IClassFixture<MySqlContainerFixture>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MySqlIntegrationTestBase"/> class.
    /// </summary>
    /// <param name="fixture">The MySQL container fixture provided by xUnit.</param>
    protected MySqlIntegrationTestBase(MySqlContainerFixture fixture)
        : base(fixture)
    {
    }

    /// <summary>
    /// Gets the MySQL container for advanced operations.
    /// </summary>
    protected MySqlContainerFixture MySqlFixture => Fixture;
}
