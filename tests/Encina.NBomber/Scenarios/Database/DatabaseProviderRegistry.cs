namespace Encina.NBomber.Scenarios.Database;

/// <summary>
/// Registry that maps provider names to their factory implementations.
/// Supports all 13 Encina database providers.
/// </summary>
public static class DatabaseProviderRegistry
{
    /// <summary>
    /// All supported provider names.
    /// </summary>
    public static readonly IReadOnlyList<string> AllProviders = new[]
    {
        // ADO.NET providers (4)
        "ado-sqlite",
        "ado-sqlserver",
        "ado-postgresql",
        "ado-mysql",

        // Dapper providers (4)
        "dapper-sqlite",
        "dapper-sqlserver",
        "dapper-postgresql",
        "dapper-mysql",

        // EF Core providers (4)
        "efcore-sqlite",
        "efcore-sqlserver",
        "efcore-postgresql",
        "efcore-mysql",

        // MongoDB (1)
        "mongodb"
    };

    /// <summary>
    /// Providers that support Read/Write separation (excludes MongoDB).
    /// </summary>
    public static readonly IReadOnlyList<string> ReadWriteProviders = AllProviders
        .Where(p => !p.Equals("mongodb", StringComparison.OrdinalIgnoreCase))
        .ToList();

    private static readonly Dictionary<string, Func<IDatabaseProviderFactory>> _factories = new(StringComparer.OrdinalIgnoreCase)
    {
        // ADO.NET providers
        ["ado-sqlite"] = () => new AdoSqliteProviderFactory(),
        ["ado-sqlserver"] = () => new AdoSqlServerProviderFactory(),
        ["ado-postgresql"] = () => new AdoPostgreSqlProviderFactory(),
        ["ado-mysql"] = () => new AdoMySqlProviderFactory(),

        // Dapper providers
        ["dapper-sqlite"] = () => new DapperSqliteProviderFactory(),
        ["dapper-sqlserver"] = () => new DapperSqlServerProviderFactory(),
        ["dapper-postgresql"] = () => new DapperPostgreSqlProviderFactory(),
        ["dapper-mysql"] = () => new DapperMySqlProviderFactory(),

        // EF Core providers
        ["efcore-sqlite"] = () => new EFCoreSqliteProviderFactory(),
        ["efcore-sqlserver"] = () => new EFCoreSqlServerProviderFactory(),
        ["efcore-postgresql"] = () => new EFCorePostgreSqlProviderFactory(),
        ["efcore-mysql"] = () => new EFCoreMySqlProviderFactory(),

        // MongoDB
        ["mongodb"] = () => new MongoDbProviderFactory()
    };

    /// <summary>
    /// Creates a provider factory for the given provider name.
    /// </summary>
    /// <param name="providerName">The provider name (e.g., "efcore-postgresql").</param>
    /// <returns>A new provider factory instance.</returns>
    /// <exception cref="ArgumentException">Thrown when the provider name is not recognized.</exception>
    public static IDatabaseProviderFactory Create(string providerName)
    {
        if (string.IsNullOrWhiteSpace(providerName))
        {
            throw new ArgumentException("Provider name cannot be null or empty.", nameof(providerName));
        }

        if (!_factories.TryGetValue(providerName, out var factory))
        {
            var validProviders = string.Join(", ", AllProviders);
            throw new ArgumentException(
                $"Unknown provider '{providerName}'. Valid providers are: {validProviders}",
                nameof(providerName));
        }

        return factory();
    }

    /// <summary>
    /// Validates that all provider names in the list are recognized.
    /// </summary>
    /// <param name="providerNames">List of provider names to validate.</param>
    /// <returns>True if all providers are valid; otherwise, false.</returns>
    public static bool ValidateProviders(IEnumerable<string> providerNames)
    {
        return providerNames.All(name => _factories.ContainsKey(name));
    }

    /// <summary>
    /// Gets provider names matching a category filter.
    /// </summary>
    /// <param name="category">The provider category to filter by.</param>
    /// <returns>Provider names in the specified category.</returns>
    public static IEnumerable<string> GetProvidersByCategory(ProviderCategory category)
    {
        var prefix = category switch
        {
            ProviderCategory.ADO => "ado-",
            ProviderCategory.Dapper => "dapper-",
            ProviderCategory.EFCore => "efcore-",
            ProviderCategory.MongoDB => "mongodb",
            _ => throw new ArgumentOutOfRangeException(nameof(category))
        };

        return AllProviders.Where(p => p.StartsWith(prefix, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Gets provider names matching a database type filter.
    /// </summary>
    /// <param name="databaseType">The database type to filter by.</param>
    /// <returns>Provider names for the specified database type.</returns>
    public static IEnumerable<string> GetProvidersByDatabase(DatabaseType databaseType)
    {
        var suffix = databaseType switch
        {
            DatabaseType.Sqlite => "-sqlite",
            DatabaseType.SqlServer => "-sqlserver",
            DatabaseType.PostgreSQL => "-postgresql",
            DatabaseType.MySQL => "-mysql",
            DatabaseType.MongoDB => "mongodb",
            _ => throw new ArgumentOutOfRangeException(nameof(databaseType))
        };

        if (databaseType == DatabaseType.MongoDB)
        {
            return new[] { "mongodb" };
        }

        return AllProviders.Where(p => p.EndsWith(suffix, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Parses a comma-separated list of provider names.
    /// </summary>
    /// <param name="providerList">Comma-separated provider names (e.g., "efcore-postgresql,dapper-mysql").</param>
    /// <returns>List of parsed provider names.</returns>
    public static IReadOnlyList<string> ParseProviderList(string? providerList)
    {
        if (string.IsNullOrWhiteSpace(providerList))
        {
            return Array.Empty<string>();
        }

        return providerList
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(p => !string.IsNullOrWhiteSpace(p))
            .ToList();
    }
}
