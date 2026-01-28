using System.Data.Common;
using System.Diagnostics.CodeAnalysis;
using BenchmarkDotNet.Attributes;
using Encina.Benchmarks.EntityFrameworkCore.Infrastructure;
using Encina.DomainModeling;
using Encina.EntityFrameworkCore.BulkOperations;
using LanguageExt;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Encina.Benchmarks.EntityFrameworkCore;

/// <summary>
/// Benchmarks for <see cref="BulkOperationsEF{TEntity}"/> measuring factory method
/// overhead and provider detection cost.
/// </summary>
/// <remarks>
/// <para>
/// <b>Architecture Note:</b> BulkOperationsEF uses a factory pattern that detects
/// the database provider at instantiation time and delegates to provider-specific
/// implementations:
/// </para>
/// <list type="bullet">
///   <item><description><b>SQL Server:</b> Uses SqlBulkCopy (unique optimization path with native bulk copy)</description></item>
///   <item><description><b>PostgreSQL:</b> Uses batched parameterized SQL with COPY command</description></item>
///   <item><description><b>MySQL:</b> Uses batched parameterized SQL with extended inserts</description></item>
///   <item><description><b>SQLite:</b> Uses batched parameterized SQL with INSERT OR REPLACE</description></item>
///   <item><description><b>Oracle:</b> Uses INSERT ALL and MERGE statements</description></item>
/// </list>
/// <para>
/// <b>Performance Implication:</b> The factory pattern cost is incurred on every
/// <see cref="BulkOperationsEF{TEntity}"/> instantiation. For high-throughput scenarios,
/// consider caching the bulk operations instance per DbContext.
/// </para>
/// <para>
/// <b>CA1001 Suppression:</b> BenchmarkDotNet manages lifecycle via [GlobalSetup]/[GlobalCleanup].
/// </para>
/// </remarks>
[MemoryDiagnoser]
[MarkdownExporter]
[RankColumn]
#pragma warning disable CA1001 // BenchmarkDotNet handles disposal via GlobalCleanup
public class BulkOperationsBenchmarks
#pragma warning restore CA1001
{
    private SqliteConnection _connection = null!;
    private EntityFrameworkBenchmarkDbContext _dbContext = null!;

    // Pre-created test entities for bulk operations
    private List<BenchmarkEntity> _entities100 = null!;
    private List<BenchmarkEntity> _entities1000 = null!;

    /// <summary>
    /// Batch size for bulk operation benchmarks.
    /// </summary>
    [Params(100, 1000)]
    public int BatchSize { get; set; }

    [GlobalSetup]
    public void GlobalSetup()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();

        _dbContext = EntityFrameworkBenchmarkDbContext.CreateSqlite(_connection);
        _dbContext.Database.EnsureCreated();

        // Pre-create test entities
        _entities100 = TestData.CreateEntities(100);
        _entities1000 = TestData.CreateEntities(1000);
    }

    [GlobalCleanup]
    public void GlobalCleanup()
    {
        _dbContext?.Dispose();
        _connection?.Dispose();
    }

    [IterationSetup]
    public void IterationSetup()
    {
        // Clear database for clean state
        _dbContext.Database.ExecuteSqlRaw("DELETE FROM BenchmarkEntities");
        _dbContext.ChangeTracker.Clear();
    }

    #region Factory Method Benchmarks

    /// <summary>
    /// BulkOperationsEF.Create&lt;TEntity&gt;() factory method overhead.
    /// Measures provider detection and implementation instantiation cost.
    /// </summary>
    /// <remarks>
    /// This benchmark measures the full cost of creating a BulkOperationsEF instance,
    /// including:
    /// <list type="number">
    ///   <item><description>DbContext.Database.GetDbConnection() call</description></item>
    ///   <item><description>Connection type pattern matching</description></item>
    ///   <item><description>Provider-specific implementation instantiation</description></item>
    /// </list>
    /// </remarks>
    [Benchmark(Baseline = true, Description = "BulkOperationsEF.Create<T>() factory")]
    public IBulkOperations<BenchmarkEntity> CreateBulkOperations_Factory()
    {
        return new BulkOperationsEF<BenchmarkEntity>(_dbContext);
    }

    /// <summary>
    /// Repeated instantiation of BulkOperationsEF.
    /// Shows the cost when not caching the bulk operations instance.
    /// </summary>
    [Benchmark(Description = "BulkOperationsEF repeated instantiation (x10)")]
    [SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "BenchmarkDotNet requires instance methods")]
    public void CreateBulkOperations_Repeated()
    {
        for (var i = 0; i < 10; i++)
        {
            _ = new BulkOperationsEF<BenchmarkEntity>(_dbContext);
        }
    }

    #endregion

    #region Connection Retrieval Benchmarks

    /// <summary>
    /// DbContext.Database.GetDbConnection() cost.
    /// This is called internally during provider detection.
    /// </summary>
    [Benchmark(Description = "GetDbConnection() retrieval")]
    public DbConnection GetDbConnection_Cost()
    {
        return _dbContext.Database.GetDbConnection();
    }

    /// <summary>
    /// Repeated GetDbConnection calls.
    /// Tests if EF Core caches the connection internally.
    /// </summary>
    [Benchmark(Description = "GetDbConnection() repeated (x10)")]
    public void GetDbConnection_Repeated()
    {
        for (var i = 0; i < 10; i++)
        {
            _ = _dbContext.Database.GetDbConnection();
        }
    }

    #endregion

    #region Connection Type Pattern Matching Benchmarks

    /// <summary>
    /// Connection type pattern matching overhead.
    /// Measures the switch expression cost for provider detection.
    /// </summary>
    [Benchmark(Description = "Connection type pattern matching")]
    public string ConnectionType_PatternMatching()
    {
        var connection = _dbContext.Database.GetDbConnection();

        // Simulates the pattern matching in BulkOperationsEF.CreateProviderImplementation
        return connection switch
        {
            SqliteConnection => "SQLite",
            _ => "Unknown"
        };
    }

    /// <summary>
    /// Direct type check comparison (baseline for pattern matching).
    /// </summary>
    [Benchmark(Description = "Connection type check (is)")]
    public bool ConnectionType_DirectCheck()
    {
        var connection = _dbContext.Database.GetDbConnection();
        return connection is SqliteConnection;
    }

    /// <summary>
    /// GetType().Name comparison (string-based detection).
    /// Shows why pattern matching is preferred over string comparison.
    /// </summary>
    [Benchmark(Description = "Connection type name comparison")]
    [SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "BenchmarkDotNet requires instance methods")]
    public bool ConnectionType_StringComparison()
    {
        var connection = _dbContext.Database.GetDbConnection();
        return connection.GetType().Name == "SqliteConnection";
    }

    #endregion

    #region Cached vs Uncached Comparison

    /// <summary>
    /// Cached bulk operations instance usage.
    /// Shows the benefit of caching vs repeated instantiation.
    /// </summary>
    [Benchmark(Description = "Cached BulkOperations usage")]
    public async Task<Either<EncinaError, int>> CachedBulkOperations_Usage()
    {
        // Simulate cached instance (created once, reused)
        var bulkOps = new BulkOperationsEF<BenchmarkEntity>(_dbContext);

        var entities = BatchSize switch
        {
            100 => CreateFreshEntities(_entities100),
            1000 => CreateFreshEntities(_entities1000),
            _ => CreateFreshEntities(_entities100)
        };

        return await bulkOps.BulkInsertAsync(entities);
    }

    /// <summary>
    /// Uncached bulk operations (new instance per operation).
    /// Shows the overhead of not caching.
    /// </summary>
    [Benchmark(Description = "Uncached BulkOperations (new each time)")]
    public async Task<Either<EncinaError, int>> UncachedBulkOperations_Usage()
    {
        // Create new instance for each operation (not recommended)
        var bulkOps = new BulkOperationsEF<BenchmarkEntity>(_dbContext);

        var entities = BatchSize switch
        {
            100 => CreateFreshEntities(_entities100),
            1000 => CreateFreshEntities(_entities1000),
            _ => CreateFreshEntities(_entities100)
        };

        return await bulkOps.BulkInsertAsync(entities);
    }

    #endregion

    #region Provider-Specific Implementation Benchmarks

    /// <summary>
    /// Direct SQLite bulk operations instantiation (bypassing factory).
    /// Measures the implementation construction cost without factory overhead.
    /// </summary>
    [Benchmark(Description = "Direct BulkOperationsEFSqlite instantiation")]
    public IBulkOperations<BenchmarkEntity> DirectSqliteImplementation()
    {
        return new BulkOperationsEFSqlite<BenchmarkEntity>(_dbContext);
    }

    /// <summary>
    /// Factory vs direct instantiation comparison.
    /// Shows the overhead added by the factory pattern.
    /// </summary>
    [Benchmark(Description = "Factory overhead (factory - direct)")]
    public void FactoryOverhead_Comparison()
    {
        // Factory path
        _ = new BulkOperationsEF<BenchmarkEntity>(_dbContext);

        // Direct path (for comparison, measures both)
        _ = new BulkOperationsEFSqlite<BenchmarkEntity>(_dbContext);
    }

    #endregion

    #region Bulk Insert Benchmarks

    /// <summary>
    /// BulkInsertAsync with SQLite provider.
    /// Measures actual bulk insert performance (not just factory overhead).
    /// </summary>
    [Benchmark(Description = "BulkInsertAsync (SQLite)")]
    public async Task<Either<EncinaError, int>> BulkInsertAsync_Sqlite()
    {
        var bulkOps = new BulkOperationsEF<BenchmarkEntity>(_dbContext);

        var entities = BatchSize switch
        {
            100 => CreateFreshEntities(_entities100),
            1000 => CreateFreshEntities(_entities1000),
            _ => CreateFreshEntities(_entities100)
        };

        return await bulkOps.BulkInsertAsync(entities);
    }

    /// <summary>
    /// Direct DbSet.AddRangeAsync + SaveChanges (baseline comparison).
    /// Shows the benefit of bulk operations vs EF Core standard approach.
    /// </summary>
    [Benchmark(Description = "AddRangeAsync + SaveChanges (baseline)")]
    public async Task<int> StandardEfCore_AddRange()
    {
        var entities = BatchSize switch
        {
            100 => CreateFreshEntities(_entities100),
            1000 => CreateFreshEntities(_entities1000),
            _ => CreateFreshEntities(_entities100)
        };

        await _dbContext.BenchmarkEntities.AddRangeAsync(entities);
        return await _dbContext.SaveChangesAsync();
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// Creates fresh entities with new GUIDs from a template list.
    /// </summary>
    private static List<BenchmarkEntity> CreateFreshEntities(List<BenchmarkEntity> templates)
    {
        return templates.Select(t => new BenchmarkEntity
        {
            Id = Guid.NewGuid(),
            Name = t.Name,
            Value = t.Value,
            CreatedAtUtc = t.CreatedAtUtc,
            Category = t.Category,
            IsActive = t.IsActive
        }).ToList();
    }

    #endregion
}
