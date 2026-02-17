using MongoDB.Bson;
using MongoDB.Driver;
using Testcontainers.MongoDb;
using Xunit;

namespace Encina.TestInfrastructure.Fixtures;

/// <summary>
/// MongoDB replica set fixture using Testcontainers.
/// Provides a throwaway MongoDB instance in replica set mode for integration tests
/// that require features like read preferences, transactions, or change streams.
/// </summary>
/// <remarks>
/// <para>
/// This fixture is specifically designed for Read/Write Separation tests that require
/// a replica set to validate read preference configuration and routing behavior.
/// </para>
/// <para>
/// Uses single-node replica set mode (<c>rs0</c>) which is sufficient for testing
/// read preference semantics without the complexity of multi-node deployment.
/// </para>
/// </remarks>
public sealed class MongoDbReplicaSetFixture : IAsyncLifetime
{
    private MongoDbContainer? _container;

    /// <summary>
    /// Gets the MongoDB client configured for the replica set.
    /// </summary>
    public IMongoClient? Client { get; private set; }

    /// <summary>
    /// Gets the connection string for the MongoDB replica set container.
    /// </summary>
    public string ConnectionString => _container?.GetConnectionString() ?? string.Empty;

    /// <summary>
    /// Gets a value indicating whether the MongoDB replica set container is available.
    /// </summary>
    public bool IsAvailable => _container is not null && Client is not null && _isReplicaSetInitialized;

    /// <summary>
    /// Gets the test database name.
    /// </summary>
    public static string DatabaseName => "encina_test_rs";

    /// <summary>
    /// Gets the replica set name.
    /// </summary>
    public static string ReplicaSetName => "rs0";

    /// <summary>
    /// Gets the test database.
    /// </summary>
    public IMongoDatabase? Database => Client?.GetDatabase(DatabaseName);

    private bool _isReplicaSetInitialized;

    /// <inheritdoc/>
    public async ValueTask InitializeAsync()
    {
        _container = new MongoDbBuilder()
            .WithImage("mongo:7")
            .WithReplicaSet(ReplicaSetName)
            .WithCleanUp(true)
            .Build();

        await _container.StartAsync();

        Client = new MongoClient(ConnectionString);

        // Verify replica set is properly initialized
        _isReplicaSetInitialized = await VerifyReplicaSetInitializedAsync();

        if (!_isReplicaSetInitialized)
        {
            throw new InvalidOperationException("MongoDB replica set verification failed - no PRIMARY member found");
        }
    }

    /// <inheritdoc/>
    public async ValueTask DisposeAsync()
    {
        if (_container is not null)
        {
            await _container.StopAsync();
            await _container.DisposeAsync();
        }
    }

    /// <summary>
    /// Verifies that the replica set is properly initialized and ready for testing.
    /// </summary>
    /// <returns><c>true</c> if the replica set is initialized and has a primary; otherwise, <c>false</c>.</returns>
    public async Task<bool> VerifyReplicaSetInitializedAsync()
    {
        if (Client is null)
        {
            return false;
        }

        try
        {
            var adminDb = Client.GetDatabase("admin");
            var command = new BsonDocument("replSetGetStatus", 1);
            var result = await adminDb.RunCommandAsync<BsonDocument>(command);

            // Check if we have a primary member
            if (result.Contains("members"))
            {
                var members = result["members"].AsBsonArray;
                foreach (var member in members)
                {
                    var memberDoc = member.AsBsonDocument;
                    if (memberDoc.Contains("stateStr") && memberDoc["stateStr"].AsString == "PRIMARY")
                    {
                        return true;
                    }
                }
            }

            return false;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to verify replica set status: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Gets the replica set status information.
    /// </summary>
    /// <returns>A <see cref="BsonDocument"/> containing the replica set status, or <c>null</c> if unavailable.</returns>
    public async Task<BsonDocument?> GetReplicaSetStatusAsync()
    {
        if (Client is null)
        {
            return null;
        }

        try
        {
            var adminDb = Client.GetDatabase("admin");
            var command = new BsonDocument("replSetGetStatus", 1);
            return await adminDb.RunCommandAsync<BsonDocument>(command);
        }
        catch
        {
            return null;
        }
    }
}
