using System.Data;
using MongoDB.Bson;
using MongoDB.Driver;
using NBomber.Contracts;
using NBomber.CSharp;

namespace Encina.NBomber.Scenarios.Database;

/// <summary>
/// Factory for creating Multi-Tenancy load test scenarios.
/// Tests tenant isolation and context switching under concurrent load.
/// </summary>
public sealed class TenancyScenarioFactory
{
    private const int DefaultTenantCount = 100;
    private readonly DatabaseScenarioContext _context;
    private readonly int _tenantCount;

    /// <summary>
    /// Initializes a new instance of the <see cref="TenancyScenarioFactory"/> class.
    /// </summary>
    /// <param name="context">The database scenario context.</param>
    /// <param name="tenantCount">The number of tenants to simulate (default: 100).</param>
    public TenancyScenarioFactory(DatabaseScenarioContext context, int tenantCount = DefaultTenantCount)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _tenantCount = tenantCount > 0 ? tenantCount : DefaultTenantCount;
    }

    /// <summary>
    /// Creates all Tenancy scenarios.
    /// </summary>
    /// <returns>A collection of tenancy load test scenarios.</returns>
    public IEnumerable<ScenarioProps> CreateScenarios()
    {
        yield return CreateTenantIsolationScenario();
        yield return CreateTenantContextSwitchingScenario();
    }

    /// <summary>
    /// Creates the tenant isolation scenario.
    /// Tests that tenant data is properly isolated under concurrent access.
    /// Each virtual user operates as a different tenant.
    /// </summary>
    /// <returns>The scenario configuration.</returns>
    public ScenarioProps CreateTenantIsolationScenario()
    {
        return Scenario.Create(
            name: $"tenancy-isolation-{_context.ProviderName}",
            run: async scenarioContext =>
            {
                // Assign a unique tenant for this iteration
                var tenantId = GetTenantId(scenarioContext.InvocationNumber);

                try
                {
                    // Set tenant context
                    InMemoryTenantProvider.SetTenant(tenantId);

                    // Verify tenant provider returns correct tenant
                    var tenantProvider = _context.ProviderFactory.CreateTenantProvider();
                    var currentTenantId = tenantProvider.GetCurrentTenantId();

                    if (currentTenantId != tenantId)
                    {
                        return Response.Fail(
                            $"Tenant isolation violation: expected {tenantId}, got {currentTenantId}",
                            statusCode: "isolation_violation");
                    }

                    // Execute a tenant-scoped operation
                    if (_context.DatabaseType == DatabaseType.MongoDB)
                    {
                        return await ExecuteMongoDbTenantOperationAsync(tenantId).ConfigureAwait(false);
                    }

                    return await ExecuteRelationalTenantOperationAsync(tenantId).ConfigureAwait(false);
                }
                finally
                {
                    InMemoryTenantProvider.ClearTenant();
                }
            })
            .WithoutWarmUp()
            .WithLoadSimulations(
                Simulation.KeepConstant(
                    copies: Math.Min(_tenantCount, 50), // One virtual user per tenant (max 50)
                    during: TimeSpan.FromMinutes(2)));
    }

    /// <summary>
    /// Creates the tenant context switching scenario.
    /// Tests rapid tenant context switches under high concurrency.
    /// Simulates scenarios where a single thread handles multiple tenant requests.
    /// </summary>
    /// <returns>The scenario configuration.</returns>
    public ScenarioProps CreateTenantContextSwitchingScenario()
    {
        return Scenario.Create(
            name: $"tenancy-context-switching-{_context.ProviderName}",
            run: async scenarioContext =>
            {
                // Rapidly switch between tenants within a single iteration
                const int switchCount = 5;
                var errors = new List<string>();

                for (var i = 0; i < switchCount; i++)
                {
                    var tenantId = GetTenantId(scenarioContext.InvocationNumber * switchCount + i);

                    try
                    {
                        InMemoryTenantProvider.SetTenant(tenantId);

                        var tenantProvider = _context.ProviderFactory.CreateTenantProvider();
                        var currentTenantId = tenantProvider.GetCurrentTenantId();

                        if (currentTenantId != tenantId)
                        {
                            errors.Add($"Switch {i}: expected {tenantId}, got {currentTenantId}");
                            continue;
                        }

                        // Quick operation to verify tenant is set
                        if (_context.DatabaseType == DatabaseType.MongoDB)
                        {
                            var (success, errorMessage) = await ExecuteMongoDbTenantQueryAsync(tenantId).ConfigureAwait(false);
                            if (!success)
                            {
                                errors.Add($"Switch {i}: {errorMessage}");
                            }
                        }
                        else
                        {
                            var (success, errorMessage) = await ExecuteRelationalTenantQueryAsync(tenantId).ConfigureAwait(false);
                            if (!success)
                            {
                                errors.Add($"Switch {i}: {errorMessage}");
                            }
                        }
                    }
                    finally
                    {
                        InMemoryTenantProvider.ClearTenant();
                    }
                }

                if (errors.Count > 0)
                {
                    return Response.Fail(string.Join("; ", errors), statusCode: "context_switch_error");
                }

                return Response.Ok();
            })
            .WithoutWarmUp()
            .WithLoadSimulations(
                Simulation.Inject(
                    rate: 100,
                    interval: TimeSpan.FromSeconds(1),
                    during: TimeSpan.FromMinutes(1)));
    }

    private string GetTenantId(long iteration)
    {
        var tenantNumber = (iteration % _tenantCount) + 1;
        return $"tenant-{tenantNumber}";
    }

    private async Task<IResponse> ExecuteRelationalTenantOperationAsync(string tenantId)
    {
        IDbConnection? connection = null;

        try
        {
            connection = _context.ProviderFactory.CreateConnection();

            if (connection.State != ConnectionState.Open)
            {
                await OpenConnectionAsync(connection).ConfigureAwait(false);
            }

            // Insert a tenant-scoped record
            using var command = connection.CreateCommand();
            command.CommandText = GetInsertTenantDataQuery();

            AddParameter(command, "@TenantId", tenantId);
            AddParameter(command, "@EntityId", _context.NextEntityId());
            AddParameter(command, "@CreatedAt", DateTime.UtcNow);

            await Task.Run(() => command.ExecuteNonQuery()).ConfigureAwait(false);

            return Response.Ok();
        }
        catch (Exception ex)
        {
            return Response.Fail(ex.Message, statusCode: "tenant_operation_error");
        }
        finally
        {
            if (_context.DatabaseType != DatabaseType.Sqlite && connection is not null)
            {
                await DisposeConnectionAsync(connection).ConfigureAwait(false);
            }
        }
    }

    private async Task<(bool Success, string? ErrorMessage)> ExecuteRelationalTenantQueryAsync(string tenantId)
    {
        IDbConnection? connection = null;

        try
        {
            connection = _context.ProviderFactory.CreateConnection();

            if (connection.State != ConnectionState.Open)
            {
                await OpenConnectionAsync(connection).ConfigureAwait(false);
            }

            // Query to verify tenant context (just select with tenant filter)
            using var command = connection.CreateCommand();
            command.CommandText = GetSelectTenantDataQuery();
            AddParameter(command, "@TenantId", tenantId);

            await Task.Run(() => command.ExecuteScalar()).ConfigureAwait(false);

            return (true, null);
        }
        catch (Exception ex)
        {
            return (false, ex.Message);
        }
        finally
        {
            if (_context.DatabaseType != DatabaseType.Sqlite && connection is not null)
            {
                await DisposeConnectionAsync(connection).ConfigureAwait(false);
            }
        }
    }

    private async Task<IResponse> ExecuteMongoDbTenantOperationAsync(string tenantId)
    {
        try
        {
            if (_context.ProviderFactory is not MongoDbProviderFactory mongoFactory)
            {
                return Response.Fail("Invalid provider type for MongoDB scenario", statusCode: "invalid_provider");
            }

            var database = mongoFactory.GetDatabase();
            var collection = database.GetCollection<BsonDocument>("load_test_tenants");

            var doc = new BsonDocument
            {
                { "tenant_id", tenantId },
                { "entity_id", _context.NextEntityId() },
                { "created_at", DateTime.UtcNow }
            };

            await collection.InsertOneAsync(doc).ConfigureAwait(false);

            return Response.Ok();
        }
        catch (Exception ex)
        {
            return Response.Fail(ex.Message, statusCode: "mongodb_tenant_error");
        }
    }

    private async Task<(bool Success, string? ErrorMessage)> ExecuteMongoDbTenantQueryAsync(string tenantId)
    {
        try
        {
            if (_context.ProviderFactory is not MongoDbProviderFactory mongoFactory)
            {
                return (false, "Invalid provider type for MongoDB scenario");
            }

            var database = mongoFactory.GetDatabase();
            var collection = database.GetCollection<BsonDocument>("load_test_tenants");

            var filter = Builders<BsonDocument>.Filter.Eq("tenant_id", tenantId);
            var count = await collection.CountDocumentsAsync(filter).ConfigureAwait(false);

            return (true, null);
        }
        catch (Exception ex)
        {
            return (false, ex.Message);
        }
    }

    private string GetInsertTenantDataQuery()
    {
        return _context.DatabaseType switch
        {
            DatabaseType.Sqlite => """
                INSERT INTO load_test_tenants (TenantId, EntityId, CreatedAt)
                VALUES (@TenantId, @EntityId, @CreatedAt);
                """,
            DatabaseType.SqlServer => """
                INSERT INTO load_test_tenants (TenantId, EntityId, CreatedAt)
                VALUES (@TenantId, @EntityId, @CreatedAt);
                """,
            DatabaseType.PostgreSQL => """
                INSERT INTO load_test_tenants (tenant_id, entity_id, created_at)
                VALUES (@TenantId, @EntityId, @CreatedAt);
                """,
            DatabaseType.MySQL => """
                INSERT INTO load_test_tenants (TenantId, EntityId, CreatedAt)
                VALUES (@TenantId, @EntityId, @CreatedAt);
                """,
            _ => throw new NotSupportedException($"Unsupported database type: {_context.DatabaseType}")
        };
    }

    private string GetSelectTenantDataQuery()
    {
        return _context.DatabaseType switch
        {
            DatabaseType.Sqlite => "SELECT COUNT(*) FROM load_test_tenants WHERE TenantId = @TenantId;",
            DatabaseType.SqlServer => "SELECT COUNT(*) FROM load_test_tenants WHERE TenantId = @TenantId;",
            DatabaseType.PostgreSQL => "SELECT COUNT(*) FROM load_test_tenants WHERE tenant_id = @TenantId;",
            DatabaseType.MySQL => "SELECT COUNT(*) FROM load_test_tenants WHERE TenantId = @TenantId;",
            _ => throw new NotSupportedException($"Unsupported database type: {_context.DatabaseType}")
        };
    }

    private static void AddParameter(IDbCommand command, string name, object value)
    {
        var parameter = command.CreateParameter();
        parameter.ParameterName = name;
        parameter.Value = value;
        command.Parameters.Add(parameter);
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
