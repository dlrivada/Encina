using System.Collections.Concurrent;
using Encina.Sharding;
using LanguageExt;
using MongoDB.Driver;

namespace Encina.MongoDB.Sharding;

/// <summary>
/// Implementation of <see cref="IShardedMongoCollectionFactory"/> that supports both
/// native <c>mongos</c> routing and application-level sharding.
/// </summary>
/// <remarks>
/// <para>
/// In native sharding mode, all collections come from the default <see cref="IMongoClient"/>
/// registered in DI (connected to <c>mongos</c>). MongoDB handles routing transparently.
/// </para>
/// <para>
/// In application-level sharding mode, separate <see cref="IMongoClient"/> instances are
/// created per shard using the connection strings from <see cref="ShardTopology"/>.
/// Clients are cached in a thread-safe dictionary for reuse.
/// </para>
/// </remarks>
public sealed class ShardedMongoCollectionFactory : IShardedMongoCollectionFactory, IDisposable
{
    private readonly IMongoClient _defaultClient;
    private readonly string _databaseName;
    private readonly bool _useNativeSharding;
    private readonly ShardTopology? _topology;
    private readonly ConcurrentDictionary<string, IMongoClient> _shardClients = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, string> _shardDatabaseNames = new(StringComparer.OrdinalIgnoreCase);
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="ShardedMongoCollectionFactory"/> class
    /// for native <c>mongos</c> sharding mode.
    /// </summary>
    /// <param name="defaultClient">The default MongoDB client connected to <c>mongos</c>.</param>
    /// <param name="databaseName">The database name.</param>
    public ShardedMongoCollectionFactory(
        IMongoClient defaultClient,
        string databaseName)
    {
        ArgumentNullException.ThrowIfNull(defaultClient);
        ArgumentException.ThrowIfNullOrWhiteSpace(databaseName);

        _defaultClient = defaultClient;
        _databaseName = databaseName;
        _useNativeSharding = true;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ShardedMongoCollectionFactory"/> class
    /// for application-level sharding mode.
    /// </summary>
    /// <param name="defaultClient">The default MongoDB client (fallback).</param>
    /// <param name="databaseName">The database name.</param>
    /// <param name="topology">The shard topology with connection strings per shard.</param>
    public ShardedMongoCollectionFactory(
        IMongoClient defaultClient,
        string databaseName,
        ShardTopology topology)
    {
        ArgumentNullException.ThrowIfNull(defaultClient);
        ArgumentException.ThrowIfNullOrWhiteSpace(databaseName);
        ArgumentNullException.ThrowIfNull(topology);

        _defaultClient = defaultClient;
        _databaseName = databaseName;
        _topology = topology;
        _useNativeSharding = false;
    }

    /// <inheritdoc />
    public Either<EncinaError, IMongoCollection<TEntity>> GetCollectionForShard<TEntity>(
        string shardId,
        string collectionName)
        where TEntity : class
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(shardId);
        ArgumentException.ThrowIfNullOrWhiteSpace(collectionName);

        ObjectDisposedException.ThrowIf(_disposed, this);

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

    /// <inheritdoc />
    public Either<EncinaError, IMongoCollection<TEntity>> GetDefaultCollection<TEntity>(
        string collectionName)
        where TEntity : class
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(collectionName);

        ObjectDisposedException.ThrowIf(_disposed, this);

        return Either<EncinaError, IMongoCollection<TEntity>>.Right(
            _defaultClient.GetDatabase(_databaseName).GetCollection<TEntity>(collectionName));
    }

    /// <inheritdoc />
    public Either<EncinaError, IReadOnlyDictionary<string, IMongoCollection<TEntity>>> GetAllCollections<TEntity>(
        string collectionName)
        where TEntity : class
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(collectionName);

        ObjectDisposedException.ThrowIf(_disposed, this);

        if (_useNativeSharding)
        {
            // In native mode, return a single collection via the default client
            var collection = _defaultClient.GetDatabase(_databaseName).GetCollection<TEntity>(collectionName);
            var result = new Dictionary<string, IMongoCollection<TEntity>>
            {
                ["mongos"] = collection
            };
            return Either<EncinaError, IReadOnlyDictionary<string, IMongoCollection<TEntity>>>.Right(result);
        }

        // Application-level mode: create collections for all active shards
        var collections = new Dictionary<string, IMongoCollection<TEntity>>(StringComparer.OrdinalIgnoreCase);

        foreach (var shardId in _topology!.ActiveShardIds)
        {
            var clientResult = GetOrCreateShardClient(shardId);

            if (clientResult.IsLeft)
            {
                return (EncinaError)clientResult;
            }

            clientResult.IfRight(client =>
            {
                var databaseName = _shardDatabaseNames.TryGetValue(shardId, out var shardDatabaseName)
                    ? shardDatabaseName
                    : _databaseName;

                collections[shardId] = client.GetDatabase(databaseName).GetCollection<TEntity>(collectionName);
            });
        }

        return Either<EncinaError, IReadOnlyDictionary<string, IMongoCollection<TEntity>>>.Right(collections);
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
}
