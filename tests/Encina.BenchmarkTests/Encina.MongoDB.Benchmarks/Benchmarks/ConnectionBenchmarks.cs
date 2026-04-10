using BenchmarkDotNet.Attributes;
using Encina.MongoDB.Benchmarks.Infrastructure;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Encina.MongoDB.Benchmarks.Benchmarks;

/// <summary>
/// Measures the baseline cost of opening a <see cref="MongoClient"/> handle and executing a
/// trivial <c>ping</c> command against a live MongoDB container. This captures the per-request
/// floor that every repository/store operation in this assembly is paid on top of.
/// </summary>
/// <remarks>
/// Unlike <c>SqlConnection</c> or <c>NpgsqlConnection</c>, <see cref="MongoClient"/> is designed
/// to be cached and reused — it owns a connection pool internally. The baseline reuses a single
/// client across iterations to reflect realistic usage.
/// </remarks>
[MemoryDiagnoser]
public class ConnectionBenchmarks
{
    private MongoDbBenchmarkContainer _container = null!;
    private IMongoDatabase _database = null!;

    /// <summary>
    /// Boots a MongoDB container once before the first iteration and opens a reusable client.
    /// </summary>
    [GlobalSetup]
    public void GlobalSetup()
    {
        _container = new MongoDbBenchmarkContainer();
        _container.Start();

        var client = new MongoClient(_container.ConnectionString);
        _database = client.GetDatabase("encina_bench");
    }

    /// <summary>
    /// Tears down the MongoDB container after the last iteration.
    /// </summary>
    [GlobalCleanup]
    public void GlobalCleanup()
    {
        _container?.Stop();
    }

    /// <summary>
    /// Baseline: dispatch a <c>ping</c> command via the pre-opened client. Captures the
    /// per-operation cost of a round-trip command without any document materialization.
    /// </summary>
    /// <returns>The <c>ping</c> server response.</returns>
    [Benchmark(Baseline = true)]
    [BenchmarkCategory("DocRef:bench:mongodb/connection-ping")]
    public BsonDocument PingCommand()
    {
        return _database.RunCommand<BsonDocument>(new BsonDocument("ping", 1));
    }
}
