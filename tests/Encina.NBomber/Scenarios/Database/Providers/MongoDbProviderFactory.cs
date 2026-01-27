using System.Data;
using Encina.Tenancy;
using Encina.TestInfrastructure.Fixtures;
using MongoDB.Driver;

namespace Encina.NBomber.Scenarios.Database;

/// <summary>
/// MongoDB provider factory using Testcontainers with replica set support.
/// MongoDB requires a replica set for transaction support.
/// </summary>
public sealed class MongoDbProviderFactory : DatabaseProviderFactoryBase
{
    private MongoDbReplicaSetFixture? _fixture;

    /// <inheritdoc />
    public override string ProviderName => "mongodb";

    /// <inheritdoc />
    public override ProviderCategory Category => ProviderCategory.MongoDB;

    /// <inheritdoc />
    public override DatabaseType DatabaseType => DatabaseType.MongoDB;

    /// <inheritdoc />
    public override bool SupportsReadWriteSeparation => false;

    /// <inheritdoc />
    public override string ConnectionString => _fixture?.ConnectionString ?? string.Empty;

    /// <inheritdoc />
    protected override async Task InitializeCoreAsync(CancellationToken cancellationToken)
    {
        _fixture = new MongoDbReplicaSetFixture();
        await _fixture.InitializeAsync().ConfigureAwait(false);
    }

    /// <inheritdoc />
    public override IDbConnection CreateConnection()
    {
        // MongoDB doesn't use IDbConnection - this is a limitation of the abstraction
        // Return a dummy connection that throws if used incorrectly
        throw new NotSupportedException("MongoDB does not support IDbConnection. Use the MongoDB driver directly.");
    }

    /// <inheritdoc />
    public override object? CreateUnitOfWork() => null;

    /// <inheritdoc />
    public override ITenantProvider CreateTenantProvider() => new InMemoryTenantProvider();

    /// <inheritdoc />
    public override object? CreateReadWriteSelector()
    {
        // MongoDB doesn't support read/write separation in the same way as relational databases
        return null;
    }

    /// <inheritdoc />
    public override async Task ClearDataAsync(CancellationToken cancellationToken = default)
    {
        EnsureInitialized();
        // MongoDB fixture doesn't have ClearAllDataAsync - drop all collections
        var database = _fixture!.Database;
        if (database is not null)
        {
            var collectionNames = await database.ListCollectionNamesAsync(cancellationToken: cancellationToken)
                .ConfigureAwait(false);

            await collectionNames.ForEachAsync(async collectionName =>
            {
                await database.DropCollectionAsync(collectionName, cancellationToken).ConfigureAwait(false);
            }, cancellationToken).ConfigureAwait(false);
        }
    }

    /// <inheritdoc />
    protected override async ValueTask DisposeCoreAsync()
    {
        if (_fixture is not null)
        {
            await _fixture.DisposeAsync().ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Gets the MongoDB client for direct MongoDB operations.
    /// </summary>
    /// <returns>The MongoDB client.</returns>
    /// <exception cref="InvalidOperationException">Thrown if the client is not available.</exception>
    public IMongoClient GetMongoClient()
    {
        EnsureInitialized();
        return _fixture!.Client ?? throw new InvalidOperationException("MongoDB client is not available.");
    }

    /// <summary>
    /// Gets the MongoDB database for direct MongoDB operations.
    /// </summary>
    /// <returns>The MongoDB database.</returns>
    /// <exception cref="InvalidOperationException">Thrown if the database is not available.</exception>
    public IMongoDatabase GetDatabase()
    {
        EnsureInitialized();
        return _fixture!.Database ?? throw new InvalidOperationException("MongoDB database is not available.");
    }
}
