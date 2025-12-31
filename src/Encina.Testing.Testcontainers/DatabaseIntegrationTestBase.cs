using Encina.Testing.Respawn;
using Xunit;

namespace Encina.Testing.Testcontainers;

/// <summary>
/// Abstract base class for database integration tests that combines Testcontainers
/// with Respawn for automatic database cleanup between tests.
/// </summary>
/// <typeparam name="TFixture">The type of container fixture to use.</typeparam>
/// <remarks>
/// <para>
/// This class provides a complete integration testing setup by:
/// </para>
/// <list type="bullet">
/// <item>Using Testcontainers to spin up real database instances in Docker</item>
/// <item>Using Respawn to efficiently clean up data between tests</item>
/// <item>Implementing xUnit's <see cref="IAsyncLifetime"/> for proper lifecycle management</item>
/// </list>
/// <para>
/// The database is reset before each test runs, ensuring test isolation.
/// The container is shared across all tests in the class via xUnit's <see cref="IClassFixture{TFixture}"/>.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// public class OrderRepositoryTests : DatabaseIntegrationTestBase&lt;SqlServerContainerFixture&gt;
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
///         // Insert test data
///         await connection.ExecuteAsync("INSERT INTO Orders (Id, Name) VALUES (1, 'Test')");
///
///         // Verify
///         var count = await connection.ExecuteScalarAsync&lt;int&gt;("SELECT COUNT(*) FROM Orders");
///         Assert.Equal(1, count);
///     }
/// }
/// </code>
/// </example>
public abstract class DatabaseIntegrationTestBase<TFixture> : IAsyncLifetime
    where TFixture : class, IAsyncLifetime
{
    private readonly TFixture _fixture;
    private DatabaseRespawner? _respawner;

    /// <summary>
    /// Initializes a new instance of the <see cref="DatabaseIntegrationTestBase{TFixture}"/> class.
    /// </summary>
    /// <param name="fixture">The container fixture provided by xUnit's dependency injection.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="fixture"/> is null.
    /// </exception>
    protected DatabaseIntegrationTestBase(TFixture fixture)
    {
        ArgumentNullException.ThrowIfNull(fixture);
        _fixture = fixture;
    }

    /// <summary>
    /// Gets the container fixture.
    /// </summary>
    protected TFixture Fixture => _fixture;

    /// <summary>
    /// Gets the connection string for the database container.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the connection string cannot be retrieved from the fixture.
    /// </exception>
    protected string ConnectionString
    {
        get
        {
            return _fixture switch
            {
                SqlServerContainerFixture sqlServer => sqlServer.ConnectionString,
                PostgreSqlContainerFixture postgres => postgres.ConnectionString,
                MySqlContainerFixture mysql => mysql.ConnectionString,
                _ => GetConnectionStringFromReflection()
            };
        }
    }

    /// <summary>
    /// Gets the Respawn adapter type based on the fixture type.
    /// </summary>
    /// <exception cref="NotSupportedException">
    /// Thrown when the fixture type does not support Respawn cleanup.
    /// </exception>
    protected RespawnAdapter RespawnAdapter
    {
        get
        {
            return _fixture switch
            {
                SqlServerContainerFixture => RespawnAdapter.SqlServer,
                PostgreSqlContainerFixture => RespawnAdapter.PostgreSql,
                MySqlContainerFixture => RespawnAdapter.MySql,
                _ => throw new NotSupportedException(
                    $"Fixture type '{typeof(TFixture).Name}' does not have a corresponding Respawn adapter. " +
                    "Override GetRespawner() to provide a custom respawner.")
            };
        }
    }

    /// <summary>
    /// Gets the database respawner used for cleanup.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// Thrown when accessed before <see cref="InitializeAsync"/> has been called.
    /// </exception>
    protected DatabaseRespawner Respawner =>
        _respawner ?? throw new InvalidOperationException(
            "Respawner not initialized. Ensure InitializeAsync has completed.");

    /// <summary>
    /// Gets the options to configure Respawn behavior.
    /// Override this property to customize cleanup behavior.
    /// </summary>
    /// <remarks>
    /// By default, Encina messaging tables are excluded from cleanup.
    /// Override this property to change the behavior.
    /// </remarks>
    protected virtual RespawnOptions RespawnOptions => new()
    {
        ResetEncinaMessagingTables = false
    };

    /// <summary>
    /// Initializes the test by setting up the respawner and resetting the database.
    /// </summary>
    /// <remarks>
    /// This method is called by xUnit before each test method runs.
    /// It creates the respawner and resets the database to a clean state.
    /// </remarks>
    /// <returns>A task representing the asynchronous operation.</returns>
    public virtual async Task InitializeAsync()
    {
        _respawner = CreateRespawner();
        await _respawner.InitializeAsync();
        await _respawner.ResetAsync();
    }

    /// <summary>
    /// Cleans up after the test by disposing the respawner.
    /// </summary>
    /// <remarks>
    /// This method is called by xUnit after each test method completes.
    /// </remarks>
    /// <returns>A task representing the asynchronous operation.</returns>
    public virtual async Task DisposeAsync()
    {
        if (_respawner is not null)
        {
            await _respawner.DisposeAsync();
        }
    }

    /// <summary>
    /// Creates the database respawner for cleanup.
    /// Override this method to provide a custom respawner configuration.
    /// </summary>
    /// <returns>A configured database respawner.</returns>
    protected virtual DatabaseRespawner CreateRespawner()
    {
        return RespawnerFactory.Create(RespawnAdapter, ConnectionString, RespawnOptions);
    }

    /// <summary>
    /// Resets the database to a clean state.
    /// Call this method if you need to reset the database mid-test.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    protected async Task ResetDatabaseAsync(CancellationToken cancellationToken = default)
    {
        await Respawner.ResetAsync(cancellationToken);
    }

    /// <summary>
    /// Gets the connection string from the fixture using reflection.
    /// Used as a fallback for custom fixture types.
    /// </summary>
    private string GetConnectionStringFromReflection()
    {
        var property = typeof(TFixture).GetProperty("ConnectionString");
        if (property is null)
        {
            throw new InvalidOperationException(
                $"Fixture type '{typeof(TFixture).Name}' does not have a ConnectionString property. " +
                "Ensure the fixture exposes a public ConnectionString property.");
        }

        var value = property.GetValue(_fixture);
        if (value is not string connectionString)
        {
            throw new InvalidOperationException(
                $"ConnectionString property on '{typeof(TFixture).Name}' returned null or non-string value.");
        }

        return connectionString;
    }
}
