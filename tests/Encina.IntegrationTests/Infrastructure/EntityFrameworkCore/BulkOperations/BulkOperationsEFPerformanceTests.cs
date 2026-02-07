using System.Diagnostics;
using Encina.EntityFrameworkCore.BulkOperations;
using Encina.TestInfrastructure.Fixtures;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Xunit;
using Xunit.Abstractions;

namespace Encina.IntegrationTests.Infrastructure.EntityFrameworkCore.BulkOperations;

/// <summary>
/// Performance comparison tests for EF Core Bulk Operations vs standard SaveChanges.
/// These tests measure actual performance differences using real SQL Server via Testcontainers.
/// </summary>
[Trait("Category", "Performance")]
[Trait("Provider", "EntityFrameworkCore")]
[Trait("Feature", "BulkOperations")]
#pragma warning disable CA1001 // IAsyncLifetime handles disposal via DisposeAsync
[Collection("ADO-SqlServer")]
public sealed class BulkOperationsEFPerformanceTests : IAsyncLifetime
#pragma warning restore CA1001
{
    private readonly SqlServerFixture _fixture;
    private readonly ITestOutputHelper _output;
    private EFPerformanceDbContext _dbContext = null!;
    private BulkOperationsEF<EFPerformanceEntity> _bulkOps = null!;

    public BulkOperationsEFPerformanceTests(SqlServerFixture fixture, ITestOutputHelper output)
    {
        _fixture = fixture;
        _output = output;
    }

    public async Task InitializeAsync()
    {
        var options = new DbContextOptionsBuilder<EFPerformanceDbContext>()
            .UseSqlServer(_fixture.ConnectionString)
            .Options;

        _dbContext = new EFPerformanceDbContext(options);

        // Create table manually since EnsureCreated doesn't work well with master DB
        await _dbContext.Database.ExecuteSqlRawAsync("""
            IF OBJECT_ID('EFPerformanceEntities', 'U') IS NULL
            CREATE TABLE EFPerformanceEntities (
                Id UNIQUEIDENTIFIER PRIMARY KEY,
                Name NVARCHAR(200) NOT NULL,
                Amount DECIMAL(18,2) NOT NULL,
                IsActive BIT NOT NULL,
                CreatedAtUtc DATETIME2 NOT NULL
            )
            """);

        // Create TVP types for Update and Delete operations
        // Column order must match EF Core property order (discovered via GetProperties):
        // Id, Amount, CreatedAtUtc, IsActive, Name
        await _dbContext.Database.ExecuteSqlRawAsync("""
            IF TYPE_ID('dbo.EFPerformanceEntitiesType_Update') IS NULL
            CREATE TYPE dbo.EFPerformanceEntitiesType_Update AS TABLE (
                Id UNIQUEIDENTIFIER,
                Amount DECIMAL(18,2),
                CreatedAtUtc DATETIME2,
                IsActive BIT,
                Name NVARCHAR(200)
            )
            """);

        await _dbContext.Database.ExecuteSqlRawAsync("""
            IF TYPE_ID('dbo.EFPerformanceEntitiesType_Ids') IS NULL
            CREATE TYPE dbo.EFPerformanceEntitiesType_Ids AS TABLE (
                Id UNIQUEIDENTIFIER
            )
            """);

        _bulkOps = new BulkOperationsEF<EFPerformanceEntity>(_dbContext);
    }

    public async Task DisposeAsync()
    {
        await _dbContext.Database.ExecuteSqlRawAsync(
            "IF OBJECT_ID('EFPerformanceEntities', 'U') IS NOT NULL DROP TABLE EFPerformanceEntities");
        await _dbContext.DisposeAsync();
    }

    private async Task TruncateTableAsync()
    {
        await _dbContext.Database.ExecuteSqlRawAsync(
            "IF OBJECT_ID('EFPerformanceEntities', 'U') IS NOT NULL TRUNCATE TABLE EFPerformanceEntities");
    }

    private static List<EFPerformanceEntity> CreateEntities(int count)
    {
        return Enumerable.Range(0, count)
            .Select(i => new EFPerformanceEntity
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
        _output.WriteLine("EF CORE BULK OPERATIONS PERFORMANCE SUMMARY");
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
        _dbContext.ChangeTracker.Clear();

        // Loop insert with SaveChanges per entity (worst case)
        var loopInsertSw = Stopwatch.StartNew();
        foreach (var entity in insertEntities)
        {
            entity.Id = Guid.NewGuid(); // New IDs for fresh insert
            _dbContext.EFPerformanceEntities.Add(entity);
            await _dbContext.SaveChangesAsync();
            _dbContext.ChangeTracker.Clear();
        }
        loopInsertSw.Stop();

        var insertImprovement = (double)loopInsertSw.ElapsedMilliseconds / Math.Max(1, bulkInsertSw.ElapsedMilliseconds);

        _output.WriteLine($"INSERT:");
        _output.WriteLine($"  Bulk:  {bulkInsertSw.ElapsedMilliseconds:N0} ms");
        _output.WriteLine($"  Loop:  {loopInsertSw.ElapsedMilliseconds:N0} ms");
        _output.WriteLine($"  Improvement: {insertImprovement:F1}x");
        _output.WriteLine("");

        // UPDATE
        await TruncateTableAsync();
        _dbContext.ChangeTracker.Clear();

        // Insert fresh data for update test
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
            return;
        }

        // Reload and update with loop
        _dbContext.ChangeTracker.Clear();
        var loadedEntities = await _dbContext.EFPerformanceEntities.ToListAsync();

        var loopUpdateSw = Stopwatch.StartNew();
        foreach (var entity in loadedEntities)
        {
            entity.Name = $"LoopUpdated_{entity.Name}";
            entity.Amount += 50;
            await _dbContext.SaveChangesAsync();
        }
        loopUpdateSw.Stop();

        var updateImprovement = (double)loopUpdateSw.ElapsedMilliseconds / Math.Max(1, bulkUpdateSw.ElapsedMilliseconds);

        _output.WriteLine($"UPDATE:");
        _output.WriteLine($"  Bulk:  {bulkUpdateSw.ElapsedMilliseconds:N0} ms");
        _output.WriteLine($"  Loop:  {loopUpdateSw.ElapsedMilliseconds:N0} ms");
        _output.WriteLine($"  Improvement: {updateImprovement:F1}x");
        _output.WriteLine("");

        // DELETE
        _dbContext.ChangeTracker.Clear();
        var deleteEntities = await _dbContext.EFPerformanceEntities.ToListAsync();

        var bulkDeleteSw = Stopwatch.StartNew();
        var bulkDeleteResult = await _bulkOps.BulkDeleteAsync(deleteEntities);
        bulkDeleteSw.Stop();

        if (bulkDeleteResult.IsLeft)
        {
            bulkDeleteResult.IfLeft(e => _output.WriteLine($"Bulk delete failed: {e.Message}"));
            return;
        }

        // Re-insert for loop delete
        await _bulkOps.BulkInsertAsync(deleteEntities);
        _dbContext.ChangeTracker.Clear();
        var toDelete = await _dbContext.EFPerformanceEntities.ToListAsync();

        var loopDeleteSw = Stopwatch.StartNew();
        foreach (var entity in toDelete)
        {
            _dbContext.EFPerformanceEntities.Remove(entity);
            await _dbContext.SaveChangesAsync();
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
}

#region Test Entity and DbContext

/// <summary>
/// Test entity for EF Core bulk operations performance tests.
/// </summary>
public class EFPerformanceEntity
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}

/// <summary>
/// DbContext for EF Core bulk operations performance tests.
/// </summary>
public class EFPerformanceDbContext(DbContextOptions<EFPerformanceDbContext> options) : DbContext(options)
{
    public DbSet<EFPerformanceEntity> EFPerformanceEntities => Set<EFPerformanceEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<EFPerformanceEntity>(entity =>
        {
            entity.ToTable("EFPerformanceEntities");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedNever(); // Manual GUID, not auto-generated
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Amount).HasPrecision(18, 2);
            entity.Property(e => e.CreatedAtUtc).IsRequired();
        });
    }
}

#endregion
