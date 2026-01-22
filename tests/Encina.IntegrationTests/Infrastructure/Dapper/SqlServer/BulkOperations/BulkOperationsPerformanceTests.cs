using System.Data;
using System.Diagnostics;
using System.Globalization;
using Dapper;
using Encina.Dapper.SqlServer.BulkOperations;
using Encina.Dapper.SqlServer.Repository;
using Encina.TestInfrastructure.Fixtures;
using Microsoft.Data.SqlClient;
using Xunit;
using Xunit.Abstractions;

using HashSet = System.Collections.Generic.HashSet<string>;

namespace Encina.IntegrationTests.Infrastructure.Dapper.SqlServer.BulkOperations;

/// <summary>
/// Performance comparison tests for Bulk Operations vs standard loop inserts.
/// These tests measure actual performance differences to validate documentation claims.
/// </summary>
/// <remarks>
/// Run with: dotnet test --filter "FullyQualifiedName~BulkOperationsPerformanceTests" --configuration Release
/// </remarks>
[Trait("Category", "Performance")]
[Trait("Provider", "Dapper.SqlServer")]
[Trait("Feature", "BulkOperations")]
public sealed class BulkOperationsPerformanceTests : IClassFixture<SqlServerFixture>, IAsyncLifetime
{
    private readonly SqlServerFixture _fixture;
    private readonly ITestOutputHelper _output;
    private IDbConnection _connection = null!;
    private BulkOperationsDapper<PerformanceTestOrder, Guid> _bulkOps = null!;

    public BulkOperationsPerformanceTests(SqlServerFixture fixture, ITestOutputHelper output)
    {
        _fixture = fixture;
        _output = output;
    }

    public async Task InitializeAsync()
    {
        _connection = _fixture.CreateConnection();
        await CreatePerformanceTestTableAsync();

        var mapping = new PerformanceTestOrderMapping();
        _bulkOps = new BulkOperationsDapper<PerformanceTestOrder, Guid>(_connection, mapping);
    }

    public async Task DisposeAsync()
    {
        await DropPerformanceTestTableAsync();
        _connection?.Dispose();
    }

    private async Task CreatePerformanceTestTableAsync()
    {
        const string sql = """
            IF OBJECT_ID('PerformanceTestOrders', 'U') IS NOT NULL
                DROP TABLE PerformanceTestOrders;

            CREATE TABLE PerformanceTestOrders (
                Id UNIQUEIDENTIFIER PRIMARY KEY,
                CustomerName NVARCHAR(200) NOT NULL,
                Amount DECIMAL(18,2) NOT NULL,
                IsActive BIT NOT NULL,
                CreatedAtUtc DATETIME2 NOT NULL
            );
            """;

        using var command = ((SqlConnection)_connection).CreateCommand();
        command.CommandText = sql;
        await command.ExecuteNonQueryAsync();
    }

    private async Task DropPerformanceTestTableAsync()
    {
        const string sql = "IF OBJECT_ID('PerformanceTestOrders', 'U') IS NOT NULL DROP TABLE PerformanceTestOrders;";

        if (_connection.State == ConnectionState.Open)
        {
            using var command = ((SqlConnection)_connection).CreateCommand();
            command.CommandText = sql;
            await command.ExecuteNonQueryAsync();
        }
    }

    private async Task TruncateTableAsync()
    {
        using var command = ((SqlConnection)_connection).CreateCommand();
        command.CommandText = "TRUNCATE TABLE PerformanceTestOrders";
        await command.ExecuteNonQueryAsync();
    }

    private static List<PerformanceTestOrder> CreateOrders(int count)
    {
        return Enumerable.Range(0, count)
            .Select(i => new PerformanceTestOrder
            {
                Id = Guid.NewGuid(),
                CustomerName = $"Customer_{i:D6}",
                Amount = (i + 1) * 10.50m,
                IsActive = i % 2 == 0,
                CreatedAtUtc = DateTime.UtcNow
            })
            .ToList();
    }

    #region Insert Performance Tests

    [Theory]
    [InlineData(100)]
    [InlineData(1000)]
    [InlineData(5000)]
    [InlineData(10000)]
    public async Task Insert_BulkVsLoop_PerformanceComparison(int entityCount)
    {
        var orders = CreateOrders(entityCount);

        // Warm up
        await TruncateTableAsync();
        await _bulkOps.BulkInsertAsync(orders.Take(10).ToList());
        await TruncateTableAsync();

        // Test 1: Bulk Insert
        var bulkStopwatch = Stopwatch.StartNew();
        var bulkResult = await _bulkOps.BulkInsertAsync(orders);
        bulkStopwatch.Stop();

        Assert.True(bulkResult.IsRight, "Bulk insert failed");
        var bulkTimeMs = bulkStopwatch.ElapsedMilliseconds;

        await TruncateTableAsync();

        // Test 2: Loop Insert (standard Dapper)
        var loopStopwatch = Stopwatch.StartNew();
        foreach (var order in orders)
        {
            const string insertSql = """
                INSERT INTO PerformanceTestOrders (Id, CustomerName, Amount, IsActive, CreatedAtUtc)
                VALUES (@Id, @CustomerName, @Amount, @IsActive, @CreatedAtUtc)
                """;
            await _connection.ExecuteAsync(insertSql, order);
        }
        loopStopwatch.Stop();
        var loopTimeMs = loopStopwatch.ElapsedMilliseconds;

        // Calculate improvement
        var improvement = loopTimeMs > 0 ? (double)loopTimeMs / Math.Max(1, bulkTimeMs) : 1.0;

        _output.WriteLine($"=== INSERT Performance ({entityCount:N0} entities) ===");
        _output.WriteLine($"Bulk Insert:  {bulkTimeMs:N0} ms");
        _output.WriteLine($"Loop Insert:  {loopTimeMs:N0} ms");
        _output.WriteLine($"Improvement:  {improvement:F1}x faster");
        _output.WriteLine("");

        // Assert bulk is faster (at least for > 100 entities)
        if (entityCount >= 1000)
        {
            Assert.True(bulkTimeMs < loopTimeMs, $"Bulk insert should be faster: bulk={bulkTimeMs}ms, loop={loopTimeMs}ms");
        }
    }

    #endregion

    #region Update Performance Tests

    [Theory]
    [InlineData(100)]
    [InlineData(1000)]
    [InlineData(5000)]
    public async Task Update_BulkVsLoop_PerformanceComparison(int entityCount)
    {
        // Setup: Create TVP type for updates
        await CreateUpdateTvpTypeAsync();

        var orders = CreateOrders(entityCount);

        // Insert data first
        await _bulkOps.BulkInsertAsync(orders);

        // Modify all orders
        foreach (var order in orders)
        {
            order.CustomerName = $"Updated_{order.CustomerName}";
            order.Amount += 100;
        }

        // Test 1: Bulk Update
        var bulkStopwatch = Stopwatch.StartNew();
        var bulkResult = await _bulkOps.BulkUpdateAsync(orders);
        bulkStopwatch.Stop();

        Assert.True(bulkResult.IsRight, $"Bulk update failed: {bulkResult.Match(r => "", e => e.Message)}");
        var bulkTimeMs = bulkStopwatch.ElapsedMilliseconds;

        // Modify again for loop test
        foreach (var order in orders)
        {
            order.CustomerName = $"LoopUpdated_{order.CustomerName}";
        }

        // Test 2: Loop Update
        var loopStopwatch = Stopwatch.StartNew();
        foreach (var order in orders)
        {
            const string updateSql = """
                UPDATE PerformanceTestOrders
                SET CustomerName = @CustomerName, Amount = @Amount, IsActive = @IsActive
                WHERE Id = @Id
                """;
            await _connection.ExecuteAsync(updateSql, order);
        }
        loopStopwatch.Stop();
        var loopTimeMs = loopStopwatch.ElapsedMilliseconds;

        var improvement = loopTimeMs > 0 ? (double)loopTimeMs / Math.Max(1, bulkTimeMs) : 1.0;

        _output.WriteLine($"=== UPDATE Performance ({entityCount:N0} entities) ===");
        _output.WriteLine($"Bulk Update:  {bulkTimeMs:N0} ms");
        _output.WriteLine($"Loop Update:  {loopTimeMs:N0} ms");
        _output.WriteLine($"Improvement:  {improvement:F1}x faster");
        _output.WriteLine("");
    }

    private async Task CreateUpdateTvpTypeAsync()
    {
        const string sql = """
            IF TYPE_ID('dbo.PerformanceTestOrdersType_Update') IS NULL
            BEGIN
                CREATE TYPE dbo.PerformanceTestOrdersType_Update AS TABLE (
                    Id UNIQUEIDENTIFIER,
                    CustomerName NVARCHAR(200),
                    Amount DECIMAL(18,2),
                    IsActive BIT
                );
            END
            """;

        using var command = ((SqlConnection)_connection).CreateCommand();
        command.CommandText = sql;
        await command.ExecuteNonQueryAsync();
    }

    #endregion

    #region Delete Performance Tests

    [Theory]
    [InlineData(100)]
    [InlineData(1000)]
    [InlineData(5000)]
    public async Task Delete_BulkVsLoop_PerformanceComparison(int entityCount)
    {
        // Setup: Create TVP type for deletes
        await CreateDeleteTvpTypeAsync();

        // Test 1: Setup for Bulk Delete
        var orders1 = CreateOrders(entityCount);
        await _bulkOps.BulkInsertAsync(orders1);

        var bulkStopwatch = Stopwatch.StartNew();
        var bulkResult = await _bulkOps.BulkDeleteAsync(orders1);
        bulkStopwatch.Stop();

        Assert.True(bulkResult.IsRight, $"Bulk delete failed: {bulkResult.Match(r => "", e => e.Message)}");
        var bulkTimeMs = bulkStopwatch.ElapsedMilliseconds;

        // Test 2: Setup for Loop Delete
        var orders2 = CreateOrders(entityCount);
        await _bulkOps.BulkInsertAsync(orders2);

        var loopStopwatch = Stopwatch.StartNew();
        foreach (var order in orders2)
        {
            const string deleteSql = "DELETE FROM PerformanceTestOrders WHERE Id = @Id";
            await _connection.ExecuteAsync(deleteSql, new { order.Id });
        }
        loopStopwatch.Stop();
        var loopTimeMs = loopStopwatch.ElapsedMilliseconds;

        var improvement = loopTimeMs > 0 ? (double)loopTimeMs / Math.Max(1, bulkTimeMs) : 1.0;

        _output.WriteLine($"=== DELETE Performance ({entityCount:N0} entities) ===");
        _output.WriteLine($"Bulk Delete:  {bulkTimeMs:N0} ms");
        _output.WriteLine($"Loop Delete:  {loopTimeMs:N0} ms");
        _output.WriteLine($"Improvement:  {improvement:F1}x faster");
        _output.WriteLine("");
    }

    private async Task CreateDeleteTvpTypeAsync()
    {
        const string sql = """
            IF TYPE_ID('dbo.PerformanceTestOrdersType_Ids') IS NULL
            BEGIN
                CREATE TYPE dbo.PerformanceTestOrdersType_Ids AS TABLE (
                    Id UNIQUEIDENTIFIER
                );
            END
            """;

        using var command = ((SqlConnection)_connection).CreateCommand();
        command.CommandText = sql;
        await command.ExecuteNonQueryAsync();
    }

    #endregion

    #region Summary Test

    [Fact]
    public async Task PerformanceSummary_AllOperations()
    {
        // This test runs all operations at a fixed size and produces a summary
        const int entityCount = 1000;

        await CreateUpdateTvpTypeAsync();
        await CreateDeleteTvpTypeAsync();

        _output.WriteLine("===========================================");
        _output.WriteLine($"BULK OPERATIONS PERFORMANCE SUMMARY");
        _output.WriteLine($"Entity Count: {entityCount:N0}");
        _output.WriteLine("===========================================");
        _output.WriteLine("");

        // INSERT
        var insertOrders = CreateOrders(entityCount);
        await TruncateTableAsync();

        var bulkInsertSw = Stopwatch.StartNew();
        await _bulkOps.BulkInsertAsync(insertOrders);
        bulkInsertSw.Stop();

        await TruncateTableAsync();

        var loopInsertSw = Stopwatch.StartNew();
        foreach (var order in insertOrders)
        {
            await _connection.ExecuteAsync(
                "INSERT INTO PerformanceTestOrders (Id, CustomerName, Amount, IsActive, CreatedAtUtc) VALUES (@Id, @CustomerName, @Amount, @IsActive, @CreatedAtUtc)",
                order);
        }
        loopInsertSw.Stop();

        var insertImprovement = (double)loopInsertSw.ElapsedMilliseconds / Math.Max(1, bulkInsertSw.ElapsedMilliseconds);

        _output.WriteLine($"INSERT:");
        _output.WriteLine($"  Bulk:  {bulkInsertSw.ElapsedMilliseconds:N0} ms");
        _output.WriteLine($"  Loop:  {loopInsertSw.ElapsedMilliseconds:N0} ms");
        _output.WriteLine($"  Improvement: {insertImprovement:F1}x");
        _output.WriteLine("");

        // UPDATE (reuse inserted data)
        foreach (var order in insertOrders)
        {
            order.CustomerName = $"Updated_{order.CustomerName}";
        }

        var bulkUpdateSw = Stopwatch.StartNew();
        await _bulkOps.BulkUpdateAsync(insertOrders);
        bulkUpdateSw.Stop();

        foreach (var order in insertOrders)
        {
            order.CustomerName = $"Loop_{order.CustomerName}";
        }

        var loopUpdateSw = Stopwatch.StartNew();
        foreach (var order in insertOrders)
        {
            await _connection.ExecuteAsync(
                "UPDATE PerformanceTestOrders SET CustomerName = @CustomerName, Amount = @Amount, IsActive = @IsActive WHERE Id = @Id",
                order);
        }
        loopUpdateSw.Stop();

        var updateImprovement = (double)loopUpdateSw.ElapsedMilliseconds / Math.Max(1, bulkUpdateSw.ElapsedMilliseconds);

        _output.WriteLine($"UPDATE:");
        _output.WriteLine($"  Bulk:  {bulkUpdateSw.ElapsedMilliseconds:N0} ms");
        _output.WriteLine($"  Loop:  {loopUpdateSw.ElapsedMilliseconds:N0} ms");
        _output.WriteLine($"  Improvement: {updateImprovement:F1}x");
        _output.WriteLine("");

        // DELETE
        var bulkDeleteSw = Stopwatch.StartNew();
        await _bulkOps.BulkDeleteAsync(insertOrders);
        bulkDeleteSw.Stop();

        // Re-insert for loop delete
        await _bulkOps.BulkInsertAsync(insertOrders);

        var loopDeleteSw = Stopwatch.StartNew();
        foreach (var order in insertOrders)
        {
            await _connection.ExecuteAsync("DELETE FROM PerformanceTestOrders WHERE Id = @Id", new { order.Id });
        }
        loopDeleteSw.Stop();

        var deleteImprovement = (double)loopDeleteSw.ElapsedMilliseconds / Math.Max(1, bulkDeleteSw.ElapsedMilliseconds);

        _output.WriteLine($"DELETE:");
        _output.WriteLine($"  Bulk:  {bulkDeleteSw.ElapsedMilliseconds:N0} ms");
        _output.WriteLine($"  Loop:  {loopDeleteSw.ElapsedMilliseconds:N0} ms");
        _output.WriteLine($"  Improvement: {deleteImprovement:F1}x");
        _output.WriteLine("");

        _output.WriteLine("===========================================");
        _output.WriteLine("These numbers can be used to update documentation");
        _output.WriteLine("===========================================");
    }

    #endregion
}

#region Test Entity and Mapping

public class PerformanceTestOrder
{
    public Guid Id { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}

public class PerformanceTestOrderMapping : IEntityMapping<PerformanceTestOrder, Guid>
{
    public string TableName => "PerformanceTestOrders";
    public string IdColumnName => "Id";

    public IReadOnlyDictionary<string, string> ColumnMappings { get; } = new Dictionary<string, string>
    {
        ["Id"] = "Id",
        ["CustomerName"] = "CustomerName",
        ["Amount"] = "Amount",
        ["IsActive"] = "IsActive",
        ["CreatedAtUtc"] = "CreatedAtUtc"
    };

    public Guid GetId(PerformanceTestOrder entity) => entity.Id;

    public IReadOnlySet<string> InsertExcludedProperties { get; } = new HashSet();
    public IReadOnlySet<string> UpdateExcludedProperties { get; } = new HashSet { "Id", "CreatedAtUtc" };
}

#endregion
