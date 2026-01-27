using System.Data;
using MongoDB.Bson;
using MongoDB.Driver;
using NBomber.Contracts;
using NBomber.CSharp;

namespace Encina.NBomber.Scenarios.Database;

/// <summary>
/// Factory for creating Unit of Work load test scenarios.
/// Tests transaction management, rollback behavior, and connection pool pressure.
/// </summary>
public sealed class UnitOfWorkScenarioFactory
{
    private readonly DatabaseScenarioContext _context;

    /// <summary>
    /// Initializes a new instance of the <see cref="UnitOfWorkScenarioFactory"/> class.
    /// </summary>
    /// <param name="context">The database scenario context.</param>
    public UnitOfWorkScenarioFactory(DatabaseScenarioContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    /// <summary>
    /// Creates all Unit of Work scenarios.
    /// </summary>
    /// <returns>A collection of Unit of Work load test scenarios.</returns>
    public IEnumerable<ScenarioProps> CreateScenarios()
    {
        yield return CreateConcurrentTransactionsScenario();
        yield return CreateRollbackUnderLoadScenario();
        yield return CreateConnectionPoolPressureScenario();
    }

    /// <summary>
    /// Creates the concurrent transactions scenario.
    /// Tests multiple transactions executing simultaneously without conflicts.
    /// </summary>
    /// <returns>The scenario configuration.</returns>
    public ScenarioProps CreateConcurrentTransactionsScenario()
    {
        return Scenario.Create(
            name: $"uow-concurrent-transactions-{_context.ProviderName}",
            run: async scenarioContext =>
            {
                if (_context.DatabaseType == DatabaseType.MongoDB)
                {
                    return await ExecuteMongoDbTransactionAsync().ConfigureAwait(false);
                }

                return await ExecuteRelationalTransactionAsync().ConfigureAwait(false);
            })
            .WithoutWarmUp()
            .WithLoadSimulations(
                Simulation.Inject(
                    rate: 100,
                    interval: TimeSpan.FromSeconds(1),
                    during: TimeSpan.FromMinutes(1)));
    }

    /// <summary>
    /// Creates the rollback under load scenario.
    /// Tests that rollbacks work correctly under high concurrency.
    /// </summary>
    /// <returns>The scenario configuration.</returns>
    public ScenarioProps CreateRollbackUnderLoadScenario()
    {
        return Scenario.Create(
            name: $"uow-rollback-under-load-{_context.ProviderName}",
            run: async scenarioContext =>
            {
                // Simulate a transaction that will be rolled back
                var shouldRollback = scenarioContext.InvocationNumber % 3 == 0;

                if (_context.DatabaseType == DatabaseType.MongoDB)
                {
                    return await ExecuteMongoDbRollbackScenarioAsync(shouldRollback).ConfigureAwait(false);
                }

                return await ExecuteRelationalRollbackScenarioAsync(shouldRollback).ConfigureAwait(false);
            })
            .WithoutWarmUp()
            .WithLoadSimulations(
                Simulation.Inject(
                    rate: 50,
                    interval: TimeSpan.FromSeconds(1),
                    during: TimeSpan.FromMinutes(1)));
    }

    /// <summary>
    /// Creates the connection pool pressure scenario.
    /// Tests behavior when connection pool is under heavy load.
    /// </summary>
    /// <returns>The scenario configuration.</returns>
    public ScenarioProps CreateConnectionPoolPressureScenario()
    {
        return Scenario.Create(
            name: $"uow-connection-pool-pressure-{_context.ProviderName}",
            run: async scenarioContext =>
            {
                if (_context.DatabaseType == DatabaseType.MongoDB)
                {
                    // MongoDB doesn't use traditional connection pooling
                    return await ExecuteMongoDbQueryAsync().ConfigureAwait(false);
                }

                return await ExecuteConnectionPoolPressureAsync().ConfigureAwait(false);
            })
            .WithoutWarmUp()
            .WithLoadSimulations(
                Simulation.Inject(
                    rate: 200, // High rate to stress the pool
                    interval: TimeSpan.FromSeconds(1),
                    during: TimeSpan.FromMinutes(2)));
    }

    private async Task<IResponse> ExecuteRelationalTransactionAsync()
    {
        IDbConnection? connection = null;
        IDbTransaction? transaction = null;

        try
        {
            connection = _context.ProviderFactory.CreateConnection();

            if (connection.State != ConnectionState.Open)
            {
                await OpenConnectionAsync(connection).ConfigureAwait(false);
            }

            transaction = connection.BeginTransaction();

            // Simulate some work within the transaction
            using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = GetSelectOneQuery();
            await Task.Run(() => command.ExecuteScalar()).ConfigureAwait(false);

            transaction.Commit();
            return Response.Ok();
        }
        catch (Exception ex)
        {
            return Response.Fail(ex.Message, statusCode: "transaction_error");
        }
        finally
        {
            transaction?.Dispose();
            // Don't dispose SQLite in-memory connections
            if (_context.DatabaseType != DatabaseType.Sqlite && connection is not null)
            {
                await DisposeConnectionAsync(connection).ConfigureAwait(false);
            }
        }
    }

    private async Task<IResponse> ExecuteRelationalRollbackScenarioAsync(bool shouldRollback)
    {
        IDbConnection? connection = null;
        IDbTransaction? transaction = null;

        try
        {
            connection = _context.ProviderFactory.CreateConnection();

            if (connection.State != ConnectionState.Open)
            {
                await OpenConnectionAsync(connection).ConfigureAwait(false);
            }

            transaction = connection.BeginTransaction();

            // Simulate some work within the transaction
            using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = GetSelectOneQuery();
            await Task.Run(() => command.ExecuteScalar()).ConfigureAwait(false);

            if (shouldRollback)
            {
                transaction.Rollback();
                return Response.Ok(statusCode: "rollback");
            }

            transaction.Commit();
            return Response.Ok(statusCode: "commit");
        }
        catch (Exception ex)
        {
            try
            {
                transaction?.Rollback();
            }
            catch
            {
                // Ignore rollback errors
            }

            return Response.Fail(ex.Message, statusCode: "transaction_error");
        }
        finally
        {
            transaction?.Dispose();
            if (_context.DatabaseType != DatabaseType.Sqlite && connection is not null)
            {
                await DisposeConnectionAsync(connection).ConfigureAwait(false);
            }
        }
    }

    private async Task<IResponse> ExecuteConnectionPoolPressureAsync()
    {
        IDbConnection? connection = null;

        try
        {
            connection = _context.ProviderFactory.CreateConnection();

            if (connection.State != ConnectionState.Open)
            {
                await OpenConnectionAsync(connection).ConfigureAwait(false);
            }

            // Execute a simple query to exercise the connection
            using var command = connection.CreateCommand();
            command.CommandText = GetSelectOneQuery();
            await Task.Run(() => command.ExecuteScalar()).ConfigureAwait(false);

            // Small delay to simulate realistic connection hold time
            await Task.Delay(Random.Shared.Next(1, 10)).ConfigureAwait(false);

            return Response.Ok();
        }
        catch (Exception ex)
        {
            return Response.Fail(ex.Message, statusCode: "pool_exhausted");
        }
        finally
        {
            if (_context.DatabaseType != DatabaseType.Sqlite && connection is not null)
            {
                await DisposeConnectionAsync(connection).ConfigureAwait(false);
            }
        }
    }

    private async Task<IResponse> ExecuteMongoDbTransactionAsync()
    {
        try
        {
            if (_context.ProviderFactory is not MongoDbProviderFactory mongoFactory)
            {
                return Response.Fail("Invalid provider type for MongoDB scenario", statusCode: "invalid_provider");
            }

            var client = mongoFactory.GetMongoClient();
            using var session = await client.StartSessionAsync().ConfigureAwait(false);

            session.StartTransaction();

            try
            {
                // Simulate some work within the transaction
                var database = mongoFactory.GetDatabase();
                var collection = database.GetCollection<BsonDocument>("load_test_uow");

                var doc = new BsonDocument
                {
                    { "test_id", _context.NextEntityId() },
                    { "timestamp", DateTime.UtcNow }
                };

                await collection.InsertOneAsync(session, doc).ConfigureAwait(false);

                await session.CommitTransactionAsync().ConfigureAwait(false);
                return Response.Ok();
            }
            catch
            {
                await session.AbortTransactionAsync().ConfigureAwait(false);
                throw;
            }
        }
        catch (Exception ex)
        {
            return Response.Fail(ex.Message, statusCode: "mongodb_transaction_error");
        }
    }

    private async Task<IResponse> ExecuteMongoDbRollbackScenarioAsync(bool shouldRollback)
    {
        try
        {
            if (_context.ProviderFactory is not MongoDbProviderFactory mongoFactory)
            {
                return Response.Fail("Invalid provider type for MongoDB scenario", statusCode: "invalid_provider");
            }

            var client = mongoFactory.GetMongoClient();
            using var session = await client.StartSessionAsync().ConfigureAwait(false);

            session.StartTransaction();

            try
            {
                var database = mongoFactory.GetDatabase();
                var collection = database.GetCollection<BsonDocument>("load_test_uow");

                var doc = new BsonDocument
                {
                    { "test_id", _context.NextEntityId() },
                    { "timestamp", DateTime.UtcNow }
                };

                await collection.InsertOneAsync(session, doc).ConfigureAwait(false);

                if (shouldRollback)
                {
                    await session.AbortTransactionAsync().ConfigureAwait(false);
                    return Response.Ok(statusCode: "rollback");
                }

                await session.CommitTransactionAsync().ConfigureAwait(false);
                return Response.Ok(statusCode: "commit");
            }
            catch
            {
                await session.AbortTransactionAsync().ConfigureAwait(false);
                throw;
            }
        }
        catch (Exception ex)
        {
            return Response.Fail(ex.Message, statusCode: "mongodb_transaction_error");
        }
    }

    private async Task<IResponse> ExecuteMongoDbQueryAsync()
    {
        try
        {
            if (_context.ProviderFactory is not MongoDbProviderFactory mongoFactory)
            {
                return Response.Fail("Invalid provider type for MongoDB scenario", statusCode: "invalid_provider");
            }

            var database = mongoFactory.GetDatabase();
            var collection = database.GetCollection<BsonDocument>("load_test_uow");

            // Simple count query to exercise the connection
            var count = await collection.CountDocumentsAsync(
                Builders<BsonDocument>.Filter.Empty).ConfigureAwait(false);

            return Response.Ok();
        }
        catch (Exception ex)
        {
            return Response.Fail(ex.Message, statusCode: "mongodb_query_error");
        }
    }

    private string GetSelectOneQuery()
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
