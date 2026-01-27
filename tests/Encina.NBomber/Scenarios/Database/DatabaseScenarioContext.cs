namespace Encina.NBomber.Scenarios.Database;

/// <summary>
/// Shared context for database load testing scenarios.
/// Contains the provider factory, connection strings, and shared state.
/// </summary>
/// <param name="ProviderFactory">The database provider factory instance.</param>
/// <param name="ProviderName">The provider name (e.g., "efcore-postgresql").</param>
/// <param name="ConnectionString">The database connection string.</param>
public sealed record DatabaseScenarioContext(
    IDatabaseProviderFactory ProviderFactory,
    string ProviderName,
    string ConnectionString)
{
    /// <summary>
    /// Gets the provider category (ADO, Dapper, EFCore, MongoDB).
    /// </summary>
    public ProviderCategory Category => ProviderFactory.Category;

    /// <summary>
    /// Gets the database type (Sqlite, SqlServer, PostgreSQL, MySQL, MongoDB).
    /// </summary>
    public DatabaseType DatabaseType => ProviderFactory.DatabaseType;

    /// <summary>
    /// Gets a value indicating whether the provider supports Read/Write separation.
    /// </summary>
    public bool SupportsReadWriteSeparation => ProviderFactory.SupportsReadWriteSeparation;

    /// <summary>
    /// Thread-safe entity ID generation.
    /// </summary>
    private long _entitySequence;

    /// <summary>
    /// Generates the next unique entity ID for load testing.
    /// </summary>
    /// <returns>A unique entity ID.</returns>
    public long NextEntityId() => Interlocked.Increment(ref _entitySequence);

    /// <summary>
    /// Generates a unique GUID based on the entity sequence.
    /// </summary>
    /// <returns>A deterministic GUID based on sequence.</returns>
    public Guid NextEntityGuid()
    {
        var id = NextEntityId();
        // Create a deterministic GUID from the sequence for reproducibility
        var bytes = new byte[16];
        BitConverter.GetBytes(id).CopyTo(bytes, 0);
        return new Guid(bytes);
    }

    /// <summary>
    /// Gets a tenant ID for multi-tenancy scenarios based on iteration.
    /// </summary>
    /// <param name="tenantCount">Total number of tenants to simulate.</param>
    /// <returns>A tenant ID string.</returns>
    public string GetTenantId(int tenantCount = 10)
    {
        var id = NextEntityId();
        var tenantNumber = (id % tenantCount) + 1;
        return $"tenant-{tenantNumber}";
    }
}
