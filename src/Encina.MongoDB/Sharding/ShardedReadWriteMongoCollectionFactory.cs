using System.Collections.Concurrent;
using Encina.Messaging.ReadWriteSeparation;
using Encina.MongoDB.ReadWriteSeparation;
using Encina.Sharding;
using LanguageExt;
using MongoDB.Driver;

namespace Encina.MongoDB.Sharding;

/// <summary>
/// MongoDB implementation of <see cref="IShardedReadWriteMongoCollectionFactory"/> that combines
/// shard routing with read/write separation via MongoDB read preferences.
/// </summary>
/// <remarks>
/// <para>
/// This factory supports both sharding modes:
/// <list type="bullet">
///   <item>
///     <description>
///       <b>Native <c>mongos</c> sharding</b>: Uses the default <see cref="IMongoClient"/>
///       with appropriate read preferences. MongoDB handles shard routing transparently.
///     </description>
///   </item>
///   <item>
///     <description>
///       <b>Application-level sharding</b>: Creates per-shard <see cref="IMongoClient"/> instances
///       from the <see cref="ShardTopology"/> connection strings, then applies read preferences.
///     </description>
///   </item>
/// </list>
/// </para>
/// <para>
/// Read operations use the configured read preference (default:
/// <see cref="ReadPreference.SecondaryPreferred"/>), while write operations always use
/// <see cref="ReadPreference.Primary"/>. The context-aware method reads the ambient
/// <see cref="DatabaseRoutingContext"/> to determine which preference to apply.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Explicit read — uses SecondaryPreferred on shard-0
/// var readResult = factory.GetReadCollectionForShard&lt;Order&gt;("shard-0", "orders");
///
/// // Explicit write — uses Primary on shard-0
/// var writeResult = factory.GetWriteCollectionForShard&lt;Order&gt;("shard-0", "orders");
///
/// // Context-aware — uses DatabaseRoutingContext.CurrentIntent
/// DatabaseRoutingContext.CurrentIntent = DatabaseIntent.Read;
/// var autoResult = factory.GetCollectionForShard&lt;Order&gt;("shard-0", "orders");
/// </code>
/// </example>
public sealed class ShardedReadWriteMongoCollectionFactory : IShardedReadWriteMongoCollectionFactory, IDisposable
{
    private readonly IMongoClient _defaultClient;
    private readonly string _databaseName;
    private readonly bool _useNativeSharding;
    private readonly ShardTopology? _topology;
    private readonly ReadPreference _readPreference;
    private readonly ReadConcern _readConcern;
    private readonly ConcurrentDictionary<string, IMongoClient> _shardClients = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, string> _shardDatabaseNames = new(StringComparer.OrdinalIgnoreCase);
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="ShardedReadWriteMongoCollectionFactory"/> class
    /// for native <c>mongos</c> sharding mode.
    /// </summary>
    /// <param name="defaultClient">The default MongoDB client connected to <c>mongos</c>.</param>
    /// <param name="databaseName">The database name.</param>
    /// <param name="rwOptions">The read/write separation options.</param>
    public ShardedReadWriteMongoCollectionFactory(
        IMongoClient defaultClient,
        string databaseName,
        MongoReadWriteSeparationOptions rwOptions)
    {
        ArgumentNullException.ThrowIfNull(defaultClient);
        ArgumentException.ThrowIfNullOrWhiteSpace(databaseName);
        ArgumentNullException.ThrowIfNull(rwOptions);

        _defaultClient = defaultClient;
        _databaseName = databaseName;
        _useNativeSharding = true;

        _readPreference = ConvertReadPreference(rwOptions.ReadPreference, rwOptions.MaxStaleness);
        _readConcern = ConvertReadConcern(rwOptions.ReadConcern);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ShardedReadWriteMongoCollectionFactory"/> class
    /// for application-level sharding mode.
    /// </summary>
    /// <param name="defaultClient">The default MongoDB client (fallback).</param>
    /// <param name="databaseName">The database name.</param>
    /// <param name="topology">The shard topology with connection strings per shard.</param>
    /// <param name="rwOptions">The read/write separation options.</param>
    public ShardedReadWriteMongoCollectionFactory(
        IMongoClient defaultClient,
        string databaseName,
        ShardTopology topology,
        MongoReadWriteSeparationOptions rwOptions)
    {
        ArgumentNullException.ThrowIfNull(defaultClient);
        ArgumentException.ThrowIfNullOrWhiteSpace(databaseName);
        ArgumentNullException.ThrowIfNull(topology);
        ArgumentNullException.ThrowIfNull(rwOptions);

        _defaultClient = defaultClient;
        _databaseName = databaseName;
        _topology = topology;
        _useNativeSharding = false;

        _readPreference = ConvertReadPreference(rwOptions.ReadPreference, rwOptions.MaxStaleness);
        _readConcern = ConvertReadConcern(rwOptions.ReadConcern);
    }

    /// <inheritdoc />
    public Either<EncinaError, IMongoCollection<TEntity>> GetReadCollectionForShard<TEntity>(
        string shardId,
        string collectionName)
        where TEntity : class
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(shardId);
        ArgumentException.ThrowIfNullOrWhiteSpace(collectionName);
        ObjectDisposedException.ThrowIf(_disposed, this);

        return GetBaseCollectionForShard<TEntity>(shardId, collectionName)
            .Map(collection => collection
                .WithReadPreference(_readPreference)
                .WithReadConcern(_readConcern));
    }

    /// <inheritdoc />
    public Either<EncinaError, IMongoCollection<TEntity>> GetWriteCollectionForShard<TEntity>(
        string shardId,
        string collectionName)
        where TEntity : class
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(shardId);
        ArgumentException.ThrowIfNullOrWhiteSpace(collectionName);
        ObjectDisposedException.ThrowIf(_disposed, this);

        return GetBaseCollectionForShard<TEntity>(shardId, collectionName)
            .Map(collection => collection
                .WithReadPreference(ReadPreference.Primary));
    }

    /// <inheritdoc />
    public Either<EncinaError, IMongoCollection<TEntity>> GetCollectionForShard<TEntity>(
        string shardId,
        string collectionName)
        where TEntity : class
    {
        return DatabaseRoutingContext.IsReadIntent
            ? GetReadCollectionForShard<TEntity>(shardId, collectionName)
            : GetWriteCollectionForShard<TEntity>(shardId, collectionName);
    }

    /// <inheritdoc />
    public Either<EncinaError, IReadOnlyDictionary<string, IMongoCollection<TEntity>>> GetAllReadCollections<TEntity>(
        string collectionName)
        where TEntity : class
    {
        return GetAllCollectionsInternal<TEntity>(
            collectionName,
            (sid, cn) => GetReadCollectionForShard<TEntity>(sid, cn));
    }

    /// <inheritdoc />
    public Either<EncinaError, IReadOnlyDictionary<string, IMongoCollection<TEntity>>> GetAllWriteCollections<TEntity>(
        string collectionName)
        where TEntity : class
    {
        return GetAllCollectionsInternal<TEntity>(
            collectionName,
            (sid, cn) => GetWriteCollectionForShard<TEntity>(sid, cn));
    }

    /// <summary>
    /// Disposes the factory and all cached shard clients.
    /// </summary>
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        foreach (var client in _shardClients.Values)
        {
            (client as IDisposable)?.Dispose();
        }

        _shardClients.Clear();
        _shardDatabaseNames.Clear();
        _disposed = true;
    }

    private Either<EncinaError, IMongoCollection<TEntity>> GetBaseCollectionForShard<TEntity>(
        string shardId,
        string collectionName)
        where TEntity : class
    {
        if (_useNativeSharding)
        {
            // In native mode, mongos handles routing — always use default client
            return Either<EncinaError, IMongoCollection<TEntity>>.Right(
                _defaultClient.GetDatabase(_databaseName).GetCollection<TEntity>(collectionName));
        }

        // Application-level mode: get or create a client for this shard
        return GetOrCreateShardClient(shardId)
            .Map(client =>
            {
                var databaseName = _shardDatabaseNames.TryGetValue(shardId, out var shardDatabaseName)
                    ? shardDatabaseName
                    : _databaseName;

                return client.GetDatabase(databaseName).GetCollection<TEntity>(collectionName);
            });
    }

    private Either<EncinaError, IReadOnlyDictionary<string, IMongoCollection<TEntity>>>
        GetAllCollectionsInternal<TEntity>(
        string collectionName,
        Func<string, string, Either<EncinaError, IMongoCollection<TEntity>>> getCollectionForShard)
        where TEntity : class
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(collectionName);
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (_useNativeSharding)
        {
            // In native mode, return a single collection via the default client
            var result = getCollectionForShard("mongos", collectionName);
            return result.Map(static collection =>
                (IReadOnlyDictionary<string, IMongoCollection<TEntity>>)
                new Dictionary<string, IMongoCollection<TEntity>> { ["mongos"] = collection });
        }

        var collections = new Dictionary<string, IMongoCollection<TEntity>>(StringComparer.OrdinalIgnoreCase);

        foreach (var shardId in _topology!.ActiveShardIds)
        {
            var collectionResult = getCollectionForShard(shardId, collectionName);

            if (collectionResult.IsLeft)
            {
                return (EncinaError)collectionResult;
            }

            collectionResult.IfRight(collection => collections[shardId] = collection);
        }

        return Either<EncinaError, IReadOnlyDictionary<string, IMongoCollection<TEntity>>>.Right(collections);
    }

    private Either<EncinaError, IMongoClient> GetOrCreateShardClient(string shardId)
    {
        if (_shardClients.TryGetValue(shardId, out var existingClient))
        {
            return Either<EncinaError, IMongoClient>.Right(existingClient);
        }

        return _topology!.GetConnectionString(shardId)
            .Map(connectionString =>
            {
                var databaseName = ExtractDatabaseName(connectionString) ?? _databaseName;
                _shardDatabaseNames[shardId] = databaseName;
                var client = _shardClients.GetOrAdd(shardId, _ => new MongoClient(connectionString));
                return client;
            });
    }

    private static string? ExtractDatabaseName(string connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            return null;
        }

        try
        {
            var schemeSeparatorIndex = connectionString.IndexOf("://", StringComparison.Ordinal);
            var authorityStart = schemeSeparatorIndex >= 0 ? schemeSeparatorIndex + 3 : 0;
            var pathStart = connectionString.IndexOf('/', authorityStart);

            if (pathStart < 0 || pathStart + 1 >= connectionString.Length)
            {
                return null;
            }

            var queryStart = connectionString.IndexOf('?', pathStart);
            var rawDatabaseName = queryStart >= 0
                ? connectionString[(pathStart + 1)..queryStart]
                : connectionString[(pathStart + 1)..];

            if (string.IsNullOrWhiteSpace(rawDatabaseName))
            {
                return null;
            }

            return Uri.UnescapeDataString(rawDatabaseName);
        }
        catch
        {
            return null;
        }
    }

    private static ReadPreference ConvertReadPreference(MongoReadPreference preference, TimeSpan? maxStaleness)
    {
        ReadPreference result = preference switch
        {
            MongoReadPreference.Primary => ReadPreference.Primary,
            MongoReadPreference.PrimaryPreferred => ReadPreference.PrimaryPreferred,
            MongoReadPreference.Secondary => ReadPreference.Secondary,
            MongoReadPreference.SecondaryPreferred => ReadPreference.SecondaryPreferred,
            MongoReadPreference.Nearest => ReadPreference.Nearest,
            _ => ReadPreference.SecondaryPreferred
        };

        if (maxStaleness.HasValue && result.ReadPreferenceMode != ReadPreferenceMode.Primary)
        {
            result = result.With(maxStaleness: maxStaleness.Value);
        }

        return result;
    }

    private static ReadConcern ConvertReadConcern(MongoReadConcern concern)
    {
        return concern switch
        {
            MongoReadConcern.Default => ReadConcern.Default,
            MongoReadConcern.Local => ReadConcern.Local,
            MongoReadConcern.Majority => ReadConcern.Majority,
            MongoReadConcern.Linearizable => ReadConcern.Linearizable,
            MongoReadConcern.Available => ReadConcern.Available,
            MongoReadConcern.Snapshot => ReadConcern.Snapshot,
            _ => ReadConcern.Majority
        };
    }
}
