using MongoDB.Driver;
using Testcontainers.MongoDb;
using Xunit;

namespace Encina.TestInfrastructure.Fixtures;

/// <summary>
/// MongoDB fixture using Testcontainers.
/// Provides a throwaway MongoDB instance for integration tests.
/// </summary>
public sealed class MongoDbFixture : IAsyncLifetime
{
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
    public async Task InitializeAsync()
    {
        try
        {
            _container = new MongoDbBuilder()
                .WithImage("mongo:7")
                .WithCleanUp(true)
                .Build();

            await _container.StartAsync();

            Client = new MongoClient(ConnectionString);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to start MongoDB container: {ex.Message}");
            // Container might not be available in CI without Docker
        }
    }

    /// <inheritdoc/>
    public async Task DisposeAsync()
    {
        if (_container is not null)
        {
            await _container.StopAsync();
            await _container.DisposeAsync();
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
