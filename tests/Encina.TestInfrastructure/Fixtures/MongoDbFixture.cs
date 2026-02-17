using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver;
using Testcontainers.MongoDb;
using Xunit;

namespace Encina.TestInfrastructure.Fixtures;

/// <summary>
/// MongoDB fixture using Testcontainers.
/// Provides a throwaway MongoDB instance for integration tests.
/// </summary>
/// <remarks>
/// Note: This fixture runs MongoDB in standalone mode (no replica set).
/// Tests that require transactions will be skipped as transactions require
/// a replica set configuration. See test documentation for instructions
/// on running MongoDB with replica set for transaction tests.
/// </remarks>
public sealed class MongoDbFixture : IAsyncLifetime
{
    private static readonly object s_serializerLock = new();
    private static bool s_serializerRegistered;

    private MongoDbContainer? _container;

    /// <summary>
    /// Gets the MongoDB client.
    /// </summary>
    public IMongoClient? Client { get; private set; }

    /// <summary>
    /// Gets the connection string for the MongoDB container.
    /// </summary>
    public string ConnectionString => _container?.GetConnectionString() ?? string.Empty;

    /// <summary>
    /// Gets a value indicating whether the MongoDB container is available.
    /// </summary>
    public bool IsAvailable => _container is not null && Client is not null;

    /// <summary>
    /// Gets the test database name.
    /// </summary>
    public static string DatabaseName => "encina_test";

    /// <summary>
    /// Gets the test database.
    /// </summary>
    public IMongoDatabase? Database => Client?.GetDatabase(DatabaseName);

    /// <inheritdoc/>
    public async ValueTask InitializeAsync()
    {
        // Register GUID serializer once (thread-safe)
        RegisterGuidSerializer();

        // Start MongoDB (replica set for transactions not supported in standard Testcontainers)
        // Tests that require transactions will be skipped
        _container = new MongoDbBuilder()
            .WithImage("mongo:7")
            .WithCleanUp(true)
            .Build();

        await _container.StartAsync();

        Client = new MongoClient(ConnectionString);
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
    /// Registers the GUID serializer with Standard representation.
    /// This is necessary for MongoDB driver to serialize Guid properties.
    /// </summary>
    private static void RegisterGuidSerializer()
    {
        lock (s_serializerLock)
        {
            if (!s_serializerRegistered)
            {
                try
                {
                    BsonSerializer.RegisterSerializer(new GuidSerializer(GuidRepresentation.Standard));
                    s_serializerRegistered = true;
                }
                catch (BsonSerializationException)
                {
                    // Serializer already registered by another fixture
                    s_serializerRegistered = true;
                }
            }
        }
    }
}

/// <summary>
/// Collection definition for MongoDB integration tests.
/// </summary>
[CollectionDefinition(Name)]
#pragma warning disable CA1711 // Identifiers should not have incorrect suffix
public class MongoDbCollection : ICollectionFixture<MongoDbFixture>
#pragma warning restore CA1711
{
    /// <summary>
    /// The name of the collection.
    /// </summary>
    public const string Name = "MongoDB";
}
