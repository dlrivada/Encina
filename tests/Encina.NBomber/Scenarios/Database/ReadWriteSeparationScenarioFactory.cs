using System.Collections.Concurrent;
using System.Data;
using MongoDB.Bson;
using MongoDB.Driver;
using NBomber.Contracts;
using NBomber.CSharp;

namespace Encina.NBomber.Scenarios.Database;

/// <summary>
/// Factory for creating Read/Write Separation load test scenarios.
/// Tests replica distribution, round-robin validation, and least-connections algorithms.
/// </summary>
/// <remarks>
/// Note: These scenarios simulate read/write separation patterns.
/// In a real production environment, you would have actual read replicas.
/// For load testing, we simulate the pattern using the same connection.
/// </remarks>
public sealed class ReadWriteSeparationScenarioFactory
{
    private readonly DatabaseScenarioContext _context;
    private readonly ConcurrentDictionary<string, long> _replicaDistribution = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="ReadWriteSeparationScenarioFactory"/> class.
    /// </summary>
    /// <param name="context">The database scenario context.</param>
    public ReadWriteSeparationScenarioFactory(DatabaseScenarioContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    /// <summary>
    /// Creates all Read/Write Separation scenarios.
    /// </summary>
    /// <returns>A collection of read/write separation load test scenarios.</returns>
    public IEnumerable<ScenarioProps> CreateScenarios()
    {
        yield return CreateReplicaDistributionScenario();
        yield return CreateRoundRobinValidationScenario();
        yield return CreateLeastConnectionsValidationScenario();
    }

    /// <summary>
    /// Creates the replica distribution scenario.
    /// Tests that reads are distributed across multiple simulated replicas.
    /// </summary>
    /// <returns>The scenario configuration.</returns>
    public ScenarioProps CreateReplicaDistributionScenario()
    {
        // Clear previous distribution data
        _replicaDistribution.Clear();

        return Scenario.Create(
            name: $"readwrite-replica-distribution-{_context.ProviderName}",
            run: async scenarioContext =>
            {
                // Simulate selecting a replica (round-robin)
                var replicaCount = 3;
                var replicaIndex = (int)(scenarioContext.InvocationNumber % replicaCount);
                var replicaName = $"replica-{replicaIndex}";

                // Track distribution
                _replicaDistribution.AddOrUpdate(replicaName, 1, (_, count) => count + 1);

                if (_context.DatabaseType == DatabaseType.MongoDB)
                {
                    return await ExecuteMongoDbReadAsync(replicaName).ConfigureAwait(false);
                }

                return await ExecuteRelationalReadAsync(replicaName).ConfigureAwait(false);
            })
            .WithoutWarmUp()
            .WithLoadSimulations(
                Simulation.Inject(
                    rate: 150,
                    interval: TimeSpan.FromSeconds(1),
                    during: TimeSpan.FromMinutes(1)));
    }

    /// <summary>
    /// Creates the round-robin validation scenario.
    /// Validates that round-robin load balancing distributes requests evenly.
    /// </summary>
    /// <returns>The scenario configuration.</returns>
    public ScenarioProps CreateRoundRobinValidationScenario()
    {
        var roundRobinCounter = 0L;
        var replicaCount = 3;
        var distribution = new ConcurrentDictionary<int, long>();

        return Scenario.Create(
            name: $"readwrite-roundrobin-validation-{_context.ProviderName}",
            run: async scenarioContext =>
            {
                // Simulate round-robin selection
                var counter = Interlocked.Increment(ref roundRobinCounter);
                var replicaIndex = (int)((counter - 1) % replicaCount);

                // Track distribution for validation
                distribution.AddOrUpdate(replicaIndex, 1, (_, count) => count + 1);

                if (_context.DatabaseType == DatabaseType.MongoDB)
                {
                    return await ExecuteMongoDbReadWithReplicaAsync(replicaIndex).ConfigureAwait(false);
                }

                return await ExecuteRelationalReadWithReplicaAsync(replicaIndex).ConfigureAwait(false);
            })
            .WithInit(async scenarioContext =>
            {
                // Reset counters at scenario start
                Interlocked.Exchange(ref roundRobinCounter, 0);
                distribution.Clear();
            })
            .WithClean(async scenarioContext =>
            {
                // Log distribution results
                var total = distribution.Values.Sum();
                var expectedPerReplica = total / replicaCount;

                Console.WriteLine($"\n[{_context.ProviderName}] Round-Robin Distribution:");
                foreach (var kvp in distribution.OrderBy(k => k.Key))
                {
                    var percentage = total > 0 ? (kvp.Value * 100.0 / total) : 0;
                    var deviation = total > 0 ? Math.Abs(kvp.Value - expectedPerReplica) * 100.0 / expectedPerReplica : 0;
                    Console.WriteLine($"  Replica {kvp.Key}: {kvp.Value} requests ({percentage:F1}%), deviation: {deviation:F1}%");
                }
            })
            .WithoutWarmUp()
            .WithLoadSimulations(
                Simulation.Inject(
                    rate: 100,
                    interval: TimeSpan.FromSeconds(1),
                    during: TimeSpan.FromMinutes(1)));
    }

    /// <summary>
    /// Creates the least-connections validation scenario.
    /// Validates that least-connections algorithm routes to least busy replica.
    /// </summary>
    /// <returns>The scenario configuration.</returns>
    public ScenarioProps CreateLeastConnectionsValidationScenario()
    {
        var replicaCount = 3;
        var activeConnections = new long[replicaCount];
        var distribution = new ConcurrentDictionary<int, long>();

        return Scenario.Create(
            name: $"readwrite-leastconnections-validation-{_context.ProviderName}",
            run: async scenarioContext =>
            {
                // Find replica with least active connections
                var replicaIndex = FindLeastConnectionsReplica(activeConnections);

                // Increment active connections for this replica
                Interlocked.Increment(ref activeConnections[replicaIndex]);

                // Track distribution
                distribution.AddOrUpdate(replicaIndex, 1, (_, count) => count + 1);

                try
                {
                    // Simulate variable processing time to create realistic load distribution
                    var processingTime = Random.Shared.Next(5, 50);

                    if (_context.DatabaseType == DatabaseType.MongoDB)
                    {
                        var result = await ExecuteMongoDbReadWithDelayAsync(replicaIndex, processingTime).ConfigureAwait(false);
                        return result;
                    }

                    return await ExecuteRelationalReadWithDelayAsync(replicaIndex, processingTime).ConfigureAwait(false);
                }
                finally
                {
                    // Decrement active connections when done
                    Interlocked.Decrement(ref activeConnections[replicaIndex]);
                }
            })
            .WithInit(async scenarioContext =>
            {
                // Reset state
                for (var i = 0; i < activeConnections.Length; i++)
                {
                    activeConnections[i] = 0;
                }

                distribution.Clear();
            })
            .WithClean(async scenarioContext =>
            {
                // Log distribution results
                var total = distribution.Values.Sum();

                Console.WriteLine($"\n[{_context.ProviderName}] Least-Connections Distribution:");
                foreach (var kvp in distribution.OrderBy(k => k.Key))
                {
                    var percentage = total > 0 ? (kvp.Value * 100.0 / total) : 0;
                    Console.WriteLine($"  Replica {kvp.Key}: {kvp.Value} requests ({percentage:F1}%)");
                }
            })
            .WithoutWarmUp()
            .WithLoadSimulations(
                Simulation.Inject(
                    rate: 75,
                    interval: TimeSpan.FromSeconds(1),
                    during: TimeSpan.FromMinutes(1)));
    }

    private static int FindLeastConnectionsReplica(long[] activeConnections)
    {
        var minConnections = long.MaxValue;
        var selectedReplica = 0;

        for (var i = 0; i < activeConnections.Length; i++)
        {
            var connections = Interlocked.Read(ref activeConnections[i]);
            if (connections < minConnections)
            {
                minConnections = connections;
                selectedReplica = i;
            }
        }

        return selectedReplica;
    }

    private async Task<IResponse> ExecuteRelationalReadAsync(string replicaName)
    {
        IDbConnection? connection = null;

        try
        {
            connection = _context.ProviderFactory.CreateConnection();

            if (connection.State != ConnectionState.Open)
            {
                await OpenConnectionAsync(connection).ConfigureAwait(false);
            }

            using var command = connection.CreateCommand();
            command.CommandText = GetSelectQuery();
            await Task.Run(() => command.ExecuteScalar()).ConfigureAwait(false);

            return Response.Ok(statusCode: replicaName);
        }
        catch (Exception ex)
        {
            return Response.Fail(ex.Message, statusCode: "read_error");
        }
        finally
        {
            if (_context.DatabaseType != DatabaseType.Sqlite && connection is not null)
            {
                await DisposeConnectionAsync(connection).ConfigureAwait(false);
            }
        }
    }

    private async Task<IResponse> ExecuteRelationalReadWithReplicaAsync(int replicaIndex)
    {
        IDbConnection? connection = null;

        try
        {
            connection = _context.ProviderFactory.CreateConnection();

            if (connection.State != ConnectionState.Open)
            {
                await OpenConnectionAsync(connection).ConfigureAwait(false);
            }

            using var command = connection.CreateCommand();
            command.CommandText = GetSelectQuery();
            await Task.Run(() => command.ExecuteScalar()).ConfigureAwait(false);

            return Response.Ok(statusCode: $"replica-{replicaIndex}");
        }
        catch (Exception ex)
        {
            return Response.Fail(ex.Message, statusCode: "read_error");
        }
        finally
        {
            if (_context.DatabaseType != DatabaseType.Sqlite && connection is not null)
            {
                await DisposeConnectionAsync(connection).ConfigureAwait(false);
            }
        }
    }

    private async Task<IResponse> ExecuteRelationalReadWithDelayAsync(int replicaIndex, int delayMs)
    {
        IDbConnection? connection = null;

        try
        {
            connection = _context.ProviderFactory.CreateConnection();

            if (connection.State != ConnectionState.Open)
            {
                await OpenConnectionAsync(connection).ConfigureAwait(false);
            }

            using var command = connection.CreateCommand();
            command.CommandText = GetSelectQuery();
            await Task.Run(() => command.ExecuteScalar()).ConfigureAwait(false);

            // Simulate variable processing time
            await Task.Delay(delayMs).ConfigureAwait(false);

            return Response.Ok(statusCode: $"replica-{replicaIndex}");
        }
        catch (Exception ex)
        {
            return Response.Fail(ex.Message, statusCode: "read_error");
        }
        finally
        {
            if (_context.DatabaseType != DatabaseType.Sqlite && connection is not null)
            {
                await DisposeConnectionAsync(connection).ConfigureAwait(false);
            }
        }
    }

    private async Task<IResponse> ExecuteMongoDbReadAsync(string replicaName)
    {
        try
        {
            if (_context.ProviderFactory is not MongoDbProviderFactory mongoFactory)
            {
                return Response.Fail("Invalid provider type for MongoDB scenario", statusCode: "invalid_provider");
            }

            var database = mongoFactory.GetDatabase();
            var collection = database.GetCollection<BsonDocument>("load_test_readwrite");

            // Simple read operation
            var count = await collection.CountDocumentsAsync(
                Builders<BsonDocument>.Filter.Empty).ConfigureAwait(false);

            return Response.Ok(statusCode: replicaName);
        }
        catch (Exception ex)
        {
            return Response.Fail(ex.Message, statusCode: "mongodb_read_error");
        }
    }

    private async Task<IResponse> ExecuteMongoDbReadWithReplicaAsync(int replicaIndex)
    {
        try
        {
            if (_context.ProviderFactory is not MongoDbProviderFactory mongoFactory)
            {
                return Response.Fail("Invalid provider type for MongoDB scenario", statusCode: "invalid_provider");
            }

            var database = mongoFactory.GetDatabase();
            var collection = database.GetCollection<BsonDocument>("load_test_readwrite");

            var count = await collection.CountDocumentsAsync(
                Builders<BsonDocument>.Filter.Empty).ConfigureAwait(false);

            return Response.Ok(statusCode: $"replica-{replicaIndex}");
        }
        catch (Exception ex)
        {
            return Response.Fail(ex.Message, statusCode: "mongodb_read_error");
        }
    }

    private async Task<IResponse> ExecuteMongoDbReadWithDelayAsync(int replicaIndex, int delayMs)
    {
        try
        {
            if (_context.ProviderFactory is not MongoDbProviderFactory mongoFactory)
            {
                return Response.Fail("Invalid provider type for MongoDB scenario", statusCode: "invalid_provider");
            }

            var database = mongoFactory.GetDatabase();
            var collection = database.GetCollection<BsonDocument>("load_test_readwrite");

            var count = await collection.CountDocumentsAsync(
                Builders<BsonDocument>.Filter.Empty).ConfigureAwait(false);

            // Simulate variable processing time
            await Task.Delay(delayMs).ConfigureAwait(false);

            return Response.Ok(statusCode: $"replica-{replicaIndex}");
        }
        catch (Exception ex)
        {
            return Response.Fail(ex.Message, statusCode: "mongodb_read_error");
        }
    }

    private string GetSelectQuery()
    {
        return _context.DatabaseType switch
        {
            DatabaseType.Sqlite => "SELECT 1;",
            DatabaseType.SqlServer => "SELECT 1;",
            DatabaseType.PostgreSQL => "SELECT 1;",
            DatabaseType.MySQL => "SELECT 1;",
            _ => "SELECT 1;"
        };
    }

    private static async Task OpenConnectionAsync(IDbConnection connection)
    {
        switch (connection)
        {
            case Microsoft.Data.SqlClient.SqlConnection sqlConnection:
                await sqlConnection.OpenAsync().ConfigureAwait(false);
                break;
            case Npgsql.NpgsqlConnection npgsqlConnection:
                await npgsqlConnection.OpenAsync().ConfigureAwait(false);
                break;
            case MySqlConnector.MySqlConnection mysqlConnection:
                await mysqlConnection.OpenAsync().ConfigureAwait(false);
                break;
            case Microsoft.Data.Sqlite.SqliteConnection sqliteConnection:
                await sqliteConnection.OpenAsync().ConfigureAwait(false);
                break;
            default:
                await Task.Run(connection.Open).ConfigureAwait(false);
                break;
        }
    }

    private static async Task DisposeConnectionAsync(IDbConnection connection)
    {
        switch (connection)
        {
            case IAsyncDisposable asyncDisposable:
                await asyncDisposable.DisposeAsync().ConfigureAwait(false);
                break;
            default:
                connection.Dispose();
                break;
        }
    }
}
