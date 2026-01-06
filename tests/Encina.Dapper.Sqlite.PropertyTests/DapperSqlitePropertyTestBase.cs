using Encina.TestInfrastructure.Extensions;
using Encina.TestInfrastructure.Fixtures;
using Microsoft.Extensions.Time.Testing;

namespace Encina.Dapper.Sqlite.Tests;

/// <summary>
/// Abstract base class for Dapper SQLite property tests.
/// Provides shared FakeTimeProvider, deterministic time, and IAsyncLifetime implementation.
/// </summary>
/// <typeparam name="TStore">The type of store being tested.</typeparam>
public abstract class DapperSqlitePropertyTestBase<TStore> : IAsyncLifetime
    where TStore : class
{
    /// <summary>
    /// Fixed base time for deterministic tests: 2025-01-05 12:00:00 UTC.
    /// </summary>
    private static readonly DateTimeOffset FixedBaseTime = new(2025, 1, 5, 12, 0, 0, TimeSpan.Zero);

    private readonly Lazy<TStore> _lazyStore;

    /// <summary>
    /// Gets the SQLite fixture for database operations.
    /// </summary>
    protected SqliteFixture Fixture { get; }

    /// <summary>
    /// Gets the FakeTimeProvider for deterministic time control.
    /// </summary>
    protected FakeTimeProvider FakeTimeProvider { get; }

    /// <summary>
    /// Gets the current UTC time from FakeTimeProvider.
    /// </summary>
    protected DateTime Now { get; }

    /// <summary>
    /// Gets the store instance for testing. Lazily initialized to avoid virtual call in constructor.
    /// </summary>
    protected TStore Store => _lazyStore.Value;

    /// <summary>
    /// Initializes the base class with shared setup.
    /// </summary>
    /// <param name="fixture">The SQLite fixture.</param>
    protected DapperSqlitePropertyTestBase(SqliteFixture fixture)
    {
        Fixture = fixture;
        DapperTypeHandlers.RegisterSqliteHandlers();

        // Use deterministic time for all tests
        FakeTimeProvider = new FakeTimeProvider(FixedBaseTime);
        Now = FakeTimeProvider.GetUtcNow().UtcDateTime;

        // Lazy initialization to avoid virtual call in constructor
        _lazyStore = new Lazy<TStore>(() => CreateStore(FakeTimeProvider));
    }

    /// <summary>
    /// Creates the store instance with the specified TimeProvider.
    /// Implemented by derived classes to create their specific store type.
    /// </summary>
    /// <param name="timeProvider">The TimeProvider to inject into the store.</param>
    /// <returns>A new store instance.</returns>
    protected abstract TStore CreateStore(TimeProvider timeProvider);

    /// <summary>
    /// Clears all data before each test to ensure clean state.
    /// </summary>
    public Task InitializeAsync() => Fixture.ClearAllDataAsync();

    /// <summary>
    /// No cleanup required after tests.
    /// </summary>
    public Task DisposeAsync() => Task.CompletedTask;
}
