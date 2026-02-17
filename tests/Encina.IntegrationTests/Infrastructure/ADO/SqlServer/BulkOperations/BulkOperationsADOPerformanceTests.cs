using System.Data;
using System.Diagnostics;
using Encina.ADO.SqlServer.BulkOperations;
using Encina.TestInfrastructure.Fixtures;
using Microsoft.Data.SqlClient;
using Xunit;
using Xunit;

namespace Encina.IntegrationTests.Infrastructure.ADO.SqlServer.BulkOperations;

/// <summary>
/// Performance comparison tests for ADO.NET Bulk Operations vs standard loop operations.
/// These tests measure actual performance differences using real SQL Server via Testcontainers.
/// </summary>
[Trait("Category", "Performance")]
[Trait("Provider", "ADO.SqlServer")]
[Trait("Feature", "BulkOperations")]
#pragma warning disable CA1001 // IAsyncLifetime handles disposal via DisposeAsync
[Collection("ADO-SqlServer")]
public sealed class BulkOperationsADOPerformanceTests : IAsyncLifetime
#pragma warning restore CA1001
{
    private readonly SqlServerFixture _fixture;
    private readonly ITestOutputHelper _output;
    private IDbConnection _connection = null!;
    private BulkOperationsADO<ADOPerformanceEntity, Guid> _bulkOps = null!;

    public BulkOperationsADOPerformanceTests(SqlServerFixture fixture, ITestOutputHelper output)
    {
        _fixture = fixture;
        _output = output;
    }

    public async ValueTask InitializeAsync()
    {
        _connection = _fixture.CreateConnection();
        await EnsureConnectionOpenAsync();

        // Create table
        using var cmd = ((SqlConnection)_connection).CreateCommand();
        cmd.CommandText = """
            IF OBJECT_ID('ADOPerformanceEntities', 'U') IS NULL
            CREATE TABLE ADOPerformanceEntities (
                Id UNIQUEIDENTIFIER PRIMARY KEY,
                Name NVARCHAR(200) NOT NULL,
                Amount DECIMAL(18,2) NOT NULL,
                IsActive BIT NOT NULL,
                CreatedAtUtc DATETIME2 NOT NULL
            )
            """;
        await cmd.ExecuteNonQueryAsync();

        // Create TVP types
        cmd.CommandText = """
            IF TYPE_ID('dbo.ADOPerformanceEntitiesType_Update') IS NULL
            CREATE TYPE dbo.ADOPerformanceEntitiesType_Update AS TABLE (
                Id UNIQUEIDENTIFIER,
                Name NVARCHAR(200),
                Amount DECIMAL(18,2),
                IsActive BIT
            )
            """;
        await cmd.ExecuteNonQueryAsync();

        cmd.CommandText = """
            IF TYPE_ID('dbo.ADOPerformanceEntitiesType_Ids') IS NULL
            CREATE TYPE dbo.ADOPerformanceEntitiesType_Ids AS TABLE (
                Id UNIQUEIDENTIFIER
            )
            """;
        await cmd.ExecuteNonQueryAsync();

        var mapping = new ADOPerformanceEntityMapping();
        _bulkOps = new BulkOperationsADO<ADOPerformanceEntity, Guid>(_connection, mapping);
    }

    public async ValueTask DisposeAsync()
    {
        await EnsureConnectionOpenAsync();

        if (_connection.State == ConnectionState.Open)
        {
            using var cmd = ((SqlConnection)_connection).CreateCommand();
            cmd.CommandText = "IF OBJECT_ID('ADOPerformanceEntities', 'U') IS NOT NULL DROP TABLE ADOPerformanceEntities";
            await cmd.ExecuteNonQueryAsync();
        }
        _connection?.Dispose();
    }

    private async Task TruncateTableAsync()
    {
        await EnsureConnectionOpenAsync();

        using var cmd = ((SqlConnection)_connection).CreateCommand();
        cmd.CommandText = "IF OBJECT_ID('ADOPerformanceEntities', 'U') IS NOT NULL TRUNCATE TABLE ADOPerformanceEntities";
        await cmd.ExecuteNonQueryAsync();
    }

    private async Task EnsureConnectionOpenAsync()
    {
        if (_connection is SqlConnection sqlConnection && sqlConnection.State != ConnectionState.Open)
        {
            await sqlConnection.OpenAsync();
        }
    }

    private static List<ADOPerformanceEntity> CreateEntities(int count)
    {
        return Enumerable.Range(0, count)
            .Select(i => new ADOPerformanceEntity
            {
                Id = Guid.NewGuid(),
                Name = $"Entity_{i:D6}",
                Amount = (i + 1) * 10.50m,
                IsActive = i % 2 == 0,
                CreatedAtUtc = DateTime.UtcNow
            })
            .ToList();
    }

    [Fact]
    public async Task PerformanceSummary_AllOperations()
    {
        const int entityCount = 1000;

        _output.WriteLine("===========================================");
        _output.WriteLine("ADO.NET BULK OPERATIONS PERFORMANCE SUMMARY");
        _output.WriteLine($"Entity Count: {entityCount:N0}");
        _output.WriteLine("===========================================");
        _output.WriteLine("");

        // INSERT
        var insertEntities = CreateEntities(entityCount);
        await TruncateTableAsync();

        // Warm up
        var warmupEntities = CreateEntities(10);
        await _bulkOps.BulkInsertAsync(warmupEntities);
        await TruncateTableAsync();

        var bulkInsertSw = Stopwatch.StartNew();
        var bulkInsertResult = await _bulkOps.BulkInsertAsync(insertEntities);
        bulkInsertSw.Stop();

        if (bulkInsertResult.IsLeft)
        {
            bulkInsertResult.IfLeft(e => _output.WriteLine($"Bulk insert failed: {e.Message}"));
            return;
        }

        await TruncateTableAsync();

        // Loop insert with individual commands
        var loopInsertSw = Stopwatch.StartNew();
        foreach (var entity in insertEntities)
        {
            using var cmd = ((SqlConnection)_connection).CreateCommand();
            cmd.CommandText = """
                INSERT INTO ADOPerformanceEntities (Id, Name, Amount, IsActive, CreatedAtUtc)
                VALUES (@Id, @Name, @Amount, @IsActive, @CreatedAtUtc)
                """;
            cmd.Parameters.AddWithValue("@Id", entity.Id);
            cmd.Parameters.AddWithValue("@Name", entity.Name);
            cmd.Parameters.AddWithValue("@Amount", entity.Amount);
            cmd.Parameters.AddWithValue("@IsActive", entity.IsActive);
            cmd.Parameters.AddWithValue("@CreatedAtUtc", entity.CreatedAtUtc);
            await cmd.ExecuteNonQueryAsync();
        }
        loopInsertSw.Stop();

        var insertImprovement = (double)loopInsertSw.ElapsedMilliseconds / Math.Max(1, bulkInsertSw.ElapsedMilliseconds);

        _output.WriteLine($"INSERT:");
        _output.WriteLine($"  Bulk:  {bulkInsertSw.ElapsedMilliseconds:N0} ms");
        _output.WriteLine($"  Loop:  {loopInsertSw.ElapsedMilliseconds:N0} ms");
        _output.WriteLine($"  Improvement: {insertImprovement:F1}x");
        _output.WriteLine("");

        // UPDATE
        // Insert fresh data for update test
        await TruncateTableAsync();
        var updateEntities = CreateEntities(entityCount);
        await _bulkOps.BulkInsertAsync(updateEntities);

        // Modify for bulk update
        foreach (var entity in updateEntities)
        {
            entity.Name = $"BulkUpdated_{entity.Name}";
            entity.Amount += 100;
        }

        var bulkUpdateSw = Stopwatch.StartNew();
        var bulkUpdateResult = await _bulkOps.BulkUpdateAsync(updateEntities);
        bulkUpdateSw.Stop();

        if (bulkUpdateResult.IsLeft)
        {
            bulkUpdateResult.IfLeft(e => _output.WriteLine($"Bulk update failed: {e.Message}"));
            // Continue anyway to get partial data
        }
        else
        {
            // Modify again for loop test
            foreach (var entity in updateEntities)
            {
                entity.Name = $"LoopUpdated_{entity.Name}";
            }

            var loopUpdateSw = Stopwatch.StartNew();
            foreach (var entity in updateEntities)
            {
                using var cmd = ((SqlConnection)_connection).CreateCommand();
                cmd.CommandText = """
                    UPDATE ADOPerformanceEntities
                    SET Name = @Name, Amount = @Amount, IsActive = @IsActive
                    WHERE Id = @Id
                    """;
                cmd.Parameters.AddWithValue("@Id", entity.Id);
                cmd.Parameters.AddWithValue("@Name", entity.Name);
                cmd.Parameters.AddWithValue("@Amount", entity.Amount);
                cmd.Parameters.AddWithValue("@IsActive", entity.IsActive);
                await cmd.ExecuteNonQueryAsync();
            }
            loopUpdateSw.Stop();

            var updateImprovement = (double)loopUpdateSw.ElapsedMilliseconds / Math.Max(1, bulkUpdateSw.ElapsedMilliseconds);

            _output.WriteLine($"UPDATE:");
            _output.WriteLine($"  Bulk:  {bulkUpdateSw.ElapsedMilliseconds:N0} ms");
            _output.WriteLine($"  Loop:  {loopUpdateSw.ElapsedMilliseconds:N0} ms");
            _output.WriteLine($"  Improvement: {updateImprovement:F1}x");
            _output.WriteLine("");
        }

        // DELETE
        var bulkDeleteSw = Stopwatch.StartNew();
        var bulkDeleteResult = await _bulkOps.BulkDeleteAsync(updateEntities);
        bulkDeleteSw.Stop();

        if (bulkDeleteResult.IsLeft)
        {
            bulkDeleteResult.IfLeft(e => _output.WriteLine($"Bulk delete failed: {e.Message}"));
        }
        else
        {
            // Re-insert for loop delete
            await _bulkOps.BulkInsertAsync(updateEntities);

            var loopDeleteSw = Stopwatch.StartNew();
            foreach (var entity in updateEntities)
            {
                using var cmd = ((SqlConnection)_connection).CreateCommand();
                cmd.CommandText = "DELETE FROM ADOPerformanceEntities WHERE Id = @Id";
                cmd.Parameters.AddWithValue("@Id", entity.Id);
                await cmd.ExecuteNonQueryAsync();
            }
            loopDeleteSw.Stop();

            var deleteImprovement = (double)loopDeleteSw.ElapsedMilliseconds / Math.Max(1, bulkDeleteSw.ElapsedMilliseconds);

            _output.WriteLine($"DELETE:");
            _output.WriteLine($"  Bulk:  {bulkDeleteSw.ElapsedMilliseconds:N0} ms");
            _output.WriteLine($"  Loop:  {loopDeleteSw.ElapsedMilliseconds:N0} ms");
            _output.WriteLine($"  Improvement: {deleteImprovement:F1}x");
            _output.WriteLine("");
        }

        _output.WriteLine("===========================================");
        _output.WriteLine("These numbers can be used to update documentation");
        _output.WriteLine("===========================================");
    }
}

#region Test Entity and Mapping

/// <summary>
/// Test entity for ADO.NET bulk operations performance tests.
/// </summary>
public class ADOPerformanceEntity
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}

/// <summary>
/// Mapping for ADO.NET bulk operations performance tests.
/// </summary>
public class ADOPerformanceEntityMapping : global::Encina.ADO.SqlServer.Repository.IEntityMapping<ADOPerformanceEntity, Guid>
{
    public string TableName => "ADOPerformanceEntities";
    public string IdColumnName => "Id";

    public IReadOnlyDictionary<string, string> ColumnMappings { get; } = new Dictionary<string, string>
    {
        ["Id"] = "Id",
        ["Name"] = "Name",
        ["Amount"] = "Amount",
        ["IsActive"] = "IsActive",
        ["CreatedAtUtc"] = "CreatedAtUtc"
    };

    public Guid GetId(ADOPerformanceEntity entity) => entity.Id;

    public IReadOnlySet<string> InsertExcludedProperties { get; } = new System.Collections.Generic.HashSet<string>();
    public IReadOnlySet<string> UpdateExcludedProperties { get; } = new System.Collections.Generic.HashSet<string> { "Id", "CreatedAtUtc" };
}

#endregion
