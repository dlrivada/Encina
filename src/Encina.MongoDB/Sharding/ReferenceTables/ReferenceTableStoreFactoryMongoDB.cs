using System.Collections.Concurrent;
using Encina.Sharding.ReferenceTables;
using MongoDB.Driver;

namespace Encina.MongoDB.Sharding.ReferenceTables;

/// <summary>
/// MongoDB factory that creates <see cref="ReferenceTableStoreMongoDB"/> instances
/// bound to a specific shard connection string.
/// </summary>
/// <remarks>
/// <para>
/// The factory caches <see cref="IMongoClient"/> instances per connection string using a
/// <see cref="ConcurrentDictionary{TKey, TValue}"/> for thread-safe reuse, matching
/// the pattern used by <see cref="ShardedMongoCollectionFactory"/>.
/// </para>
/// <para>
/// The database name is extracted from the connection string via
/// <see cref="MongoUrl.DatabaseName"/>. If not specified in the connection string,
/// it defaults to <c>"encina"</c>.
/// </para>
/// </remarks>
public sealed class ReferenceTableStoreFactoryMongoDB : IReferenceTableStoreFactory, IDisposable
{
    private const string DefaultDatabaseName = "encina";

    private readonly ConcurrentDictionary<string, IMongoClient> _clientCache = new(StringComparer.Ordinal);
    private bool _disposed;

    /// <inheritdoc />
    public IReferenceTableStore CreateForShard(string connectionString)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);

        ObjectDisposedException.ThrowIf(_disposed, this);

        var client = _clientCache.GetOrAdd(connectionString, static cs => new MongoClient(cs));
        var mongoUrl = new MongoUrl(connectionString);
        var databaseName = mongoUrl.DatabaseName ?? DefaultDatabaseName;

        return new ReferenceTableStoreMongoDB(client.GetDatabase(databaseName));
    }

    /// <inheritdoc cref="IDisposable.Dispose" />
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _clientCache.Clear();
    }
}
