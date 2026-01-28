using System.Diagnostics.CodeAnalysis;
using BenchmarkDotNet.Attributes;
using Encina.Benchmarks.EntityFrameworkCore.Infrastructure;
using Encina.DomainModeling;
using Encina.EntityFrameworkCore.UnitOfWork;
using LanguageExt;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Encina.Benchmarks.EntityFrameworkCore;

/// <summary>
/// Benchmarks for <see cref="UnitOfWorkEF"/> measuring repository caching,
/// transaction lifecycle, and SaveChanges overhead.
/// </summary>
/// <remarks>
/// <para>
/// These benchmarks focus on the Unit of Work coordination overhead,
/// including repository caching (ConcurrentDictionary) and transaction management.
/// </para>
/// <para>
/// <b>CA1001 Suppression:</b> BenchmarkDotNet manages lifecycle via [GlobalSetup]/[GlobalCleanup].
/// </para>
/// </remarks>
[MemoryDiagnoser]
[MarkdownExporter]
[RankColumn]
#pragma warning disable CA1001 // BenchmarkDotNet handles disposal via GlobalCleanup
public class UnitOfWorkBenchmarks
#pragma warning restore CA1001
{
    private SqliteConnection _connection = null!;
    private EntityFrameworkBenchmarkDbContext _dbContext = null!;
    private IServiceProvider _serviceProvider = null!;
    private UnitOfWorkEF _unitOfWork = null!;

    /// <summary>
    /// Number of tracked entities for SaveChangesAsync benchmarks.
    /// </summary>
    [Params(1, 10, 100)]
    public int TrackedEntityCount { get; set; }

    [GlobalSetup]
    public void GlobalSetup()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();

        _dbContext = EntityFrameworkBenchmarkDbContext.CreateSqlite(_connection);
        _dbContext.Database.EnsureCreated();

        // Create a minimal service provider for UnitOfWork
        var services = new ServiceCollection();
        services.AddSingleton<DbContext>(_dbContext);
        _serviceProvider = services.BuildServiceProvider();

        _unitOfWork = new UnitOfWorkEF(_dbContext, _serviceProvider);
    }

    [GlobalCleanup]
    public void GlobalCleanup()
    {
        _unitOfWork?.DisposeAsync().AsTask().GetAwaiter().GetResult();
        _dbContext?.Dispose();
        _connection?.Dispose();
    }

    [IterationSetup]
    public void IterationSetup()
    {
        // Clear database and recreate UnitOfWork for clean state
        _dbContext.Database.ExecuteSqlRaw("DELETE FROM BenchmarkEntities");
        _dbContext.ChangeTracker.Clear();

        // Recreate UnitOfWork for each iteration to reset repository cache
        _unitOfWork?.DisposeAsync().AsTask().GetAwaiter().GetResult();
        _unitOfWork = new UnitOfWorkEF(_dbContext, _serviceProvider);
    }

    #region Repository Caching Benchmarks

    /// <summary>
    /// Repository access - cache miss (first access).
    /// Measures ConcurrentDictionary.GetOrAdd with factory execution.
    /// </summary>
    [Benchmark(Baseline = true, Description = "Repository<T>() - cache miss")]
    [SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "BenchmarkDotNet requires instance methods")]
    public async Task<IFunctionalRepository<BenchmarkEntity, Guid>> Repository_CacheMiss()
    {
        // Create fresh UnitOfWork to ensure cache miss
        var freshUow = new UnitOfWorkEF(_dbContext, _serviceProvider);
        try
        {
            return freshUow.Repository<BenchmarkEntity, Guid>();
        }
        finally
        {
            await freshUow.DisposeAsync();
        }
    }

    /// <summary>
    /// Repository access - cache hit (repeated access for same entity type).
    /// Measures ConcurrentDictionary lookup performance.
    /// </summary>
    [Benchmark(Description = "Repository<T>() - cache hit")]
    public IFunctionalRepository<BenchmarkEntity, Guid> Repository_CacheHit()
    {
        // First call populates cache
        _ = _unitOfWork.Repository<BenchmarkEntity, Guid>();
        // Second call hits cache
        return _unitOfWork.Repository<BenchmarkEntity, Guid>();
    }

    /// <summary>
    /// Multiple repository types - measures cache behavior with different entity types.
    /// </summary>
    [Benchmark(Description = "Repository<T>() - multiple types")]
    [SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "BenchmarkDotNet requires instance methods")]
    public async Task Repository_MultipleTypes()
    {
        var freshUow = new UnitOfWorkEF(_dbContext, _serviceProvider);
        try
        {
            // Access multiple repository types
            _ = freshUow.Repository<BenchmarkEntity, Guid>();
            // Would add more types here if we had them
            // For now, just re-access to show cache behavior
            _ = freshUow.Repository<BenchmarkEntity, Guid>();
        }
        finally
        {
            await freshUow.DisposeAsync();
        }
    }

    #endregion

    #region Transaction Lifecycle Benchmarks

    /// <summary>
    /// BeginTransactionAsync overhead.
    /// </summary>
    [Benchmark(Description = "BeginTransactionAsync")]
    public async Task<Either<EncinaError, Unit>> BeginTransactionAsync_Overhead()
    {
        return await _unitOfWork.BeginTransactionAsync();
    }

    /// <summary>
    /// CommitAsync overhead (requires active transaction).
    /// </summary>
    [Benchmark(Description = "CommitAsync (with active transaction)")]
    public async Task<Either<EncinaError, Unit>> CommitAsync_Overhead()
    {
        await _unitOfWork.BeginTransactionAsync();
        return await _unitOfWork.CommitAsync();
    }

    /// <summary>
    /// Full transaction lifecycle: Begin + Commit.
    /// </summary>
    [Benchmark(Description = "Full transaction (Begin + Commit)")]
    public async Task FullTransaction_BeginCommit()
    {
        await _unitOfWork.BeginTransactionAsync();
        await _unitOfWork.CommitAsync();
    }

    /// <summary>
    /// Full transaction lifecycle: Begin + Rollback.
    /// </summary>
    [Benchmark(Description = "Full transaction (Begin + Rollback)")]
    public async Task FullTransaction_BeginRollback()
    {
        await _unitOfWork.BeginTransactionAsync();
        await _unitOfWork.RollbackAsync();
    }

    #endregion

    #region SaveChangesAsync Benchmarks

    /// <summary>
    /// SaveChangesAsync with varying numbers of tracked entities.
    /// </summary>
    [Benchmark(Description = "SaveChangesAsync (parameterized)")]
    public async Task<Either<EncinaError, int>> SaveChangesAsync_Parameterized()
    {
        // Add entities to track
        var entities = TestData.CreateEntities(TrackedEntityCount);
        foreach (var entity in entities)
        {
            entity.Id = Guid.NewGuid(); // Ensure unique IDs
        }
        await _dbContext.BenchmarkEntities.AddRangeAsync(entities);

        // Benchmark: SaveChanges
        return await _unitOfWork.SaveChangesAsync();
    }

    /// <summary>
    /// SaveChangesAsync with no changes (empty change tracker).
    /// Measures the baseline overhead when nothing needs to be saved.
    /// </summary>
    [Benchmark(Description = "SaveChangesAsync (no changes)")]
    public async Task<Either<EncinaError, int>> SaveChangesAsync_NoChanges()
    {
        return await _unitOfWork.SaveChangesAsync();
    }

    #endregion

    #region ChangeTracker.Clear Benchmarks

    /// <summary>
    /// ChangeTracker.Clear with varying numbers of tracked entities.
    /// Measures cleanup cost.
    /// </summary>
    [Benchmark(Description = "ChangeTracker.Clear (parameterized)")]
    public void ChangeTrackerClear_Parameterized()
    {
        // Add entities to track (without saving)
        var entities = TestData.CreateEntities(TrackedEntityCount);
        _dbContext.BenchmarkEntities.AddRange(entities);

        // Benchmark: Clear tracked entities
        _dbContext.ChangeTracker.Clear();
    }

    #endregion

    #region Concurrent Repository Access Benchmarks

    /// <summary>
    /// Concurrent repository access using Task.WhenAll.
    /// Stresses ConcurrentDictionary thread safety.
    /// </summary>
    [Benchmark(Description = "Concurrent Repository access")]
    [SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "BenchmarkDotNet requires instance methods")]
    public async Task ConcurrentRepositoryAccess()
    {
        var freshUow = new UnitOfWorkEF(_dbContext, _serviceProvider);
        try
        {
            var tasks = Enumerable.Range(0, 10)
                .Select(_ => Task.Run(() => freshUow.Repository<BenchmarkEntity, Guid>()))
                .ToArray();

            await Task.WhenAll(tasks);
        }
        finally
        {
            await freshUow.DisposeAsync();
        }
    }

    #endregion
}
