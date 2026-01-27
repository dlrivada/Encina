namespace Encina.NBomber.Scenarios.Database;

/// <summary>
/// Utility class for warming up database providers before running load tests.
/// Pre-seeds test data and primes connection pools.
/// </summary>
public static class DatabaseWarmup
{
    /// <summary>
    /// Initializes a database provider and optionally pre-seeds test data.
    /// </summary>
    /// <param name="providerName">The provider name to initialize.</param>
    /// <param name="seedData">Whether to seed initial test data.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The initialized scenario context.</returns>
    public static async Task<DatabaseScenarioContext> InitializeProviderAsync(
        string providerName,
        bool seedData = true,
        CancellationToken cancellationToken = default)
    {
        Console.WriteLine($"Initializing provider: {providerName}...");

        var factory = DatabaseProviderRegistry.Create(providerName);
        await factory.InitializeAsync(cancellationToken).ConfigureAwait(false);

        var context = new DatabaseScenarioContext(
            factory,
            providerName,
            factory.ConnectionString);

        if (seedData)
        {
            await SeedTestDataAsync(context, cancellationToken).ConfigureAwait(false);
        }

        // Prime connection pool with a few test queries
        await PrimeConnectionPoolAsync(context, cancellationToken).ConfigureAwait(false);

        Console.WriteLine($"Provider {providerName} initialized. Connection string: {TruncateConnectionString(factory.ConnectionString)}");

        return context;
    }

    /// <summary>
    /// Initializes multiple database providers in parallel.
    /// </summary>
    /// <param name="providerNames">List of provider names to initialize.</param>
    /// <param name="seedData">Whether to seed initial test data.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Dictionary of provider names to their contexts.</returns>
    public static async Task<Dictionary<string, DatabaseScenarioContext>> InitializeProvidersAsync(
        IEnumerable<string> providerNames,
        bool seedData = true,
        CancellationToken cancellationToken = default)
    {
        var results = new Dictionary<string, DatabaseScenarioContext>(StringComparer.OrdinalIgnoreCase);

        // Initialize providers sequentially to avoid resource contention
        foreach (var providerName in providerNames)
        {
            try
            {
                var context = await InitializeProviderAsync(providerName, seedData, cancellationToken)
                    .ConfigureAwait(false);
                results[providerName] = context;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to initialize provider {providerName}: {ex.Message}");
            }
        }

        return results;
    }

    /// <summary>
    /// Disposes all provider contexts.
    /// </summary>
    /// <param name="contexts">The contexts to dispose.</param>
    public static async Task DisposeProvidersAsync(IEnumerable<DatabaseScenarioContext> contexts)
    {
        foreach (var context in contexts)
        {
            try
            {
                await context.ProviderFactory.DisposeAsync().ConfigureAwait(false);
                Console.WriteLine($"Disposed provider: {context.ProviderName}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error disposing provider {context.ProviderName}: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// Seeds initial test data for load testing scenarios.
    /// </summary>
    private static async Task SeedTestDataAsync(
        DatabaseScenarioContext context,
        CancellationToken cancellationToken)
    {
        // For now, just clear existing data to ensure clean state
        await context.ProviderFactory.ClearDataAsync(cancellationToken).ConfigureAwait(false);

        // Future: Add seed data specific to each test scenario
        // - For UoW tests: Create some initial records to update
        // - For Tenancy tests: Create tenant configuration
        // - For ReadWrite tests: Seed read-heavy data
    }

    /// <summary>
    /// Primes the connection pool by executing a few test queries.
    /// </summary>
    private static async Task PrimeConnectionPoolAsync(
        DatabaseScenarioContext context,
        CancellationToken cancellationToken)
    {
        // MongoDB doesn't use IDbConnection
        if (context.DatabaseType == DatabaseType.MongoDB)
        {
            // MongoDB warmup - just verify the connection
            if (context.ProviderFactory is MongoDbProviderFactory mongoFactory)
            {
                var db = mongoFactory.GetDatabase();
                await db.ListCollectionNamesAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
            }

            return;
        }

        // For relational databases, execute a few simple queries to warm up the pool
        const int warmupQueries = 5;
        for (var i = 0; i < warmupQueries; i++)
        {
            try
            {
                using var connection = context.ProviderFactory.CreateConnection();
                using var command = connection.CreateCommand();
                command.CommandText = GetWarmupQuery(context.DatabaseType);
                await Task.Run(() => command.ExecuteScalar(), cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Warmup query {i + 1} failed: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// Gets a simple warmup query for the database type.
    /// </summary>
    private static string GetWarmupQuery(DatabaseType databaseType)
    {
        return databaseType switch
        {
            DatabaseType.Sqlite => "SELECT 1;",
            DatabaseType.SqlServer => "SELECT 1;",
            DatabaseType.PostgreSQL => "SELECT 1;",
            DatabaseType.MySQL => "SELECT 1;",
            _ => "SELECT 1;"
        };
    }

    /// <summary>
    /// Truncates a connection string for display (hides password).
    /// </summary>
    private static string TruncateConnectionString(string connectionString)
    {
        if (string.IsNullOrEmpty(connectionString))
        {
            return "(empty)";
        }

        // Hide password in connection string for logging
        var parts = connectionString.Split(';');
        var filtered = parts
            .Select(p =>
            {
                if (p.Contains("password", StringComparison.OrdinalIgnoreCase) ||
                    p.Contains("pwd", StringComparison.OrdinalIgnoreCase))
                {
                    var idx = p.IndexOf('=');
                    if (idx > 0)
                    {
                        return p[..(idx + 1)] + "***";
                    }
                }

                return p;
            })
            .ToArray();

        var result = string.Join(";", filtered);
        return result.Length > 100 ? result[..100] + "..." : result;
    }
}
