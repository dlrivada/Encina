using Encina.Modules.Isolation;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace Encina.MongoDB.Modules;

/// <summary>
/// MongoDB implementation of <see cref="IModuleAwareMongoCollectionFactory"/> for module-aware collection routing.
/// </summary>
/// <remarks>
/// <para>
/// This factory creates MongoDB collections based on the current module context
/// and module isolation strategy:
/// </para>
/// <list type="bullet">
/// <item>
/// <term>SharedDatabase</term>
/// <description>Uses the default database (module isolation is not enforced at database level)</description>
/// </item>
/// <item>
/// <term>DatabasePerModule</term>
/// <description>Uses a module-specific database based on the configured pattern</description>
/// </item>
/// </list>
/// </remarks>
/// <example>
/// <code>
/// // Registration
/// services.AddEncinaMongoDBWithModuleIsolation(config =&gt; { }, isolation =&gt;
/// {
///     isolation.EnableDatabasePerModule = true;
///     isolation.DatabaseNamePattern = "{baseName}_{moduleName}";
/// });
///
/// // Usage
/// public class OrderService(IModuleAwareMongoCollectionFactory collectionFactory)
/// {
///     public async Task&lt;IMongoCollection&lt;Order&gt;&gt; GetCollectionAsync(CancellationToken ct)
///     {
///         return await collectionFactory.GetCollectionAsync&lt;Order&gt;("orders", ct);
///     }
/// }
/// </code>
/// </example>
public sealed class ModuleAwareMongoCollectionFactory : IModuleAwareMongoCollectionFactory
{
    private readonly IMongoClient _mongoClient;
    private readonly IModuleExecutionContext _moduleContext;
    private readonly EncinaMongoDbOptions _mongoOptions;
    private readonly MongoDbModuleIsolationOptions _isolationOptions;
    private readonly ILogger<ModuleAwareMongoCollectionFactory> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ModuleAwareMongoCollectionFactory"/> class.
    /// </summary>
    /// <param name="mongoClient">The MongoDB client.</param>
    /// <param name="moduleContext">The module execution context for current module.</param>
    /// <param name="mongoOptions">The MongoDB configuration options.</param>
    /// <param name="isolationOptions">The module isolation configuration options.</param>
    /// <param name="logger">The logger.</param>
    public ModuleAwareMongoCollectionFactory(
        IMongoClient mongoClient,
        IModuleExecutionContext moduleContext,
        IOptions<EncinaMongoDbOptions> mongoOptions,
        IOptions<MongoDbModuleIsolationOptions> isolationOptions,
        ILogger<ModuleAwareMongoCollectionFactory> logger)
    {
        ArgumentNullException.ThrowIfNull(mongoClient);
        ArgumentNullException.ThrowIfNull(moduleContext);
        ArgumentNullException.ThrowIfNull(mongoOptions);
        ArgumentNullException.ThrowIfNull(isolationOptions);
        ArgumentNullException.ThrowIfNull(logger);

        _mongoClient = mongoClient;
        _moduleContext = moduleContext;
        _mongoOptions = mongoOptions.Value;
        _isolationOptions = isolationOptions.Value;
        _logger = logger;
    }

    /// <inheritdoc/>
    public ValueTask<IMongoCollection<TEntity>> GetCollectionAsync<TEntity>(
        string collectionName,
        CancellationToken cancellationToken = default)
        where TEntity : class
    {
        ArgumentException.ThrowIfNullOrEmpty(collectionName);

        var moduleName = _moduleContext.CurrentModule;

        // No module context
        if (string.IsNullOrEmpty(moduleName))
        {
            if (_isolationOptions.ThrowOnMissingModuleContext)
            {
                throw new InvalidOperationException(
                    "No module context is available. Module isolation requires a module context " +
                    "to be set via IModuleExecutionContext before accessing collections.");
            }

            if (_isolationOptions.LogWarningOnFallback)
            {
                Log.NoModuleContextFallingBackToBaseDatabase(_logger, collectionName);
            }

            var defaultDatabase = _mongoClient.GetDatabase(GetDefaultDatabaseName());
            return ValueTask.FromResult(defaultDatabase.GetCollection<TEntity>(collectionName));
        }

        var databaseName = GetDatabaseNameForModule(moduleName);
        var database = _mongoClient.GetDatabase(databaseName);
        return ValueTask.FromResult(database.GetCollection<TEntity>(collectionName));
    }

    /// <inheritdoc/>
    public ValueTask<IMongoCollection<TEntity>> GetCollectionForModuleAsync<TEntity>(
        string collectionName,
        string moduleName,
        CancellationToken cancellationToken = default)
        where TEntity : class
    {
        ArgumentException.ThrowIfNullOrEmpty(collectionName);
        ArgumentException.ThrowIfNullOrEmpty(moduleName);

        var databaseName = GetDatabaseNameForModule(moduleName);
        var database = _mongoClient.GetDatabase(databaseName);
        return ValueTask.FromResult(database.GetCollection<TEntity>(collectionName));
    }

    /// <inheritdoc/>
    public ValueTask<string> GetDatabaseNameAsync(CancellationToken cancellationToken = default)
    {
        var moduleName = _moduleContext.CurrentModule;

        // No module context - use default
        if (string.IsNullOrEmpty(moduleName))
        {
            if (_isolationOptions.ThrowOnMissingModuleContext)
            {
                throw new InvalidOperationException(
                    "No module context is available. Module isolation requires a module context " +
                    "to be set via IModuleExecutionContext.");
            }

            return ValueTask.FromResult(GetDefaultDatabaseName());
        }

        return ValueTask.FromResult(GetDatabaseNameForModule(moduleName));
    }

    /// <inheritdoc/>
    public string GetDatabaseNameForModule(string moduleName)
    {
        ArgumentException.ThrowIfNullOrEmpty(moduleName);

        // If database-per-module is not enabled, always use default
        if (!_isolationOptions.EnableDatabasePerModule)
        {
            return GetDefaultDatabaseName();
        }

        return _isolationOptions.GetDatabaseName(_mongoOptions.DatabaseName, moduleName);
    }

    private string GetDefaultDatabaseName()
    {
        if (string.IsNullOrEmpty(_mongoOptions.DatabaseName))
        {
            throw new InvalidOperationException(
                "No default database name configured. " +
                "Set EncinaMongoDbOptions.DatabaseName in your configuration.");
        }

        return _mongoOptions.DatabaseName;
    }
}
