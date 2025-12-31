using Encina.Testing.Respawn;
using Xunit;

namespace Encina.Testing.Testcontainers;

/// <summary>
/// Base class for SQL Server integration tests with automatic database cleanup.
/// </summary>
/// <remarks>
/// <para>
/// This class provides a pre-configured SQL Server integration testing setup by:
/// </para>
/// <list type="bullet">
/// <item>Using <see cref="SqlServerContainerFixture"/> to spin up a real SQL Server instance in Docker</item>
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
/// public class OrderRepositoryTests : SqlServerIntegrationTestBase
/// {
///     public OrderRepositoryTests(SqlServerContainerFixture fixture) : base(fixture) { }
///
///     [Fact]
///     public async Task CreateOrder_ShouldPersist()
///     {
///         // Database is clean at the start of each test
///         await using var connection = new SqlConnection(ConnectionString);
///         await connection.OpenAsync();
///
///         // Insert test data and verify
///         await connection.ExecuteAsync("INSERT INTO Orders (Id, Name) VALUES (1, 'Test')");
///         var count = await connection.ExecuteScalarAsync&lt;int&gt;("SELECT COUNT(*) FROM Orders");
///         Assert.Equal(1, count);
///     }
/// }
/// </code>
/// </example>
public abstract class SqlServerIntegrationTestBase
    : DatabaseIntegrationTestBase<SqlServerContainerFixture>, IClassFixture<SqlServerContainerFixture>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SqlServerIntegrationTestBase"/> class.
    /// </summary>
    /// <param name="fixture">The SQL Server container fixture provided by xUnit.</param>
    protected SqlServerIntegrationTestBase(SqlServerContainerFixture fixture)
        : base(fixture)
    {
    }

    /// <summary>
    /// Gets the SQL Server container for advanced operations.
    /// </summary>
    protected SqlServerContainerFixture SqlServerFixture => Fixture;
}
