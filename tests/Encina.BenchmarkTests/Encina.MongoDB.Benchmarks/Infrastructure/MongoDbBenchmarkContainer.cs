using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using Testcontainers.MongoDb;

namespace Encina.MongoDB.Benchmarks.Infrastructure;

/// <summary>
/// Spins up a throwaway MongoDB container for benchmark execution.
/// </summary>
/// <remarks>
/// <para>
/// BenchmarkDotNet's <c>[GlobalSetup]</c> and <c>[GlobalCleanup]</c> hooks are synchronous,
/// so this helper exposes blocking <see cref="Start"/> / <see cref="Stop"/> wrappers around
/// Testcontainers' async API. The image mirrors
/// <c>tests/Encina.TestInfrastructure/Fixtures/MongoDbFixture.cs</c> for parity with
/// integration tests (<c>mongo:7</c>, standalone — no replica set).
/// </para>
/// <para>
/// Registers the standard GUID representation on first use so that
/// <see cref="MongoDB.Bson.Serialization.Attributes.BsonRepresentationAttribute"/> with
/// <see cref="BsonType.String"/> works for entity IDs.
/// </para>
/// </remarks>
public sealed class MongoDbBenchmarkContainer : IAsyncDisposable
{
    private static readonly object s_serializerLock = new();
    private static bool s_serializerRegistered;

    private readonly MongoDbContainer _container;

    /// <summary>
    /// Initializes a new container definition. The underlying Docker container is not started
    /// until <see cref="Start"/> is called.
    /// </summary>
    public MongoDbBenchmarkContainer()
    {
        _container = new MongoDbBuilder("mongo:7")
            .WithCleanUp(true)
            .Build();
    }

    /// <summary>
    /// Gets the connection string for the running container. Only valid after <see cref="Start"/>.
    /// </summary>
    public string ConnectionString => _container.GetConnectionString();

    /// <summary>
    /// Starts the container synchronously and registers the default GUID serializer. Safe to
    /// call from <c>[GlobalSetup]</c>.
    /// </summary>
    public void Start()
    {
        RegisterGuidSerializer();
        _container.StartAsync().GetAwaiter().GetResult();
    }

    /// <summary>
    /// Stops and removes the container synchronously. Safe to call from <c>[GlobalCleanup]</c>.
    /// </summary>
    public void Stop()
    {
        _container.DisposeAsync().AsTask().GetAwaiter().GetResult();
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        await _container.DisposeAsync().ConfigureAwait(false);
    }

    private static void RegisterGuidSerializer()
    {
        lock (s_serializerLock)
        {
            if (s_serializerRegistered)
            {
                return;
            }

            try
            {
                BsonSerializer.RegisterSerializer(new GuidSerializer(GuidRepresentation.Standard));
                s_serializerRegistered = true;
            }
            catch (BsonSerializationException)
            {
                // Already registered — ignore.
                s_serializerRegistered = true;
            }
        }
    }
}
