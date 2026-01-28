using System.Diagnostics.CodeAnalysis;
using BenchmarkDotNet.Attributes;
using Encina.Benchmarks.EntityFrameworkCore.Infrastructure;
using Encina.DomainModeling;
using Encina.EntityFrameworkCore.Repository;
using Encina.EntityFrameworkCore.UnitOfWork;
using LanguageExt;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Encina.Benchmarks.EntityFrameworkCore;

/// <summary>
/// Benchmarks for <see cref="UnitOfWorkRepositoryEF{TEntity, TId}"/> measuring
/// deferred write operations (tracking without immediate SaveChanges).
/// </summary>
/// <remarks>
/// <para>
/// These benchmarks compare the Unit of Work pattern (deferred persistence)
/// vs the FunctionalRepository pattern (immediate SaveChanges).
/// </para>
/// <para>
/// Key difference: UnitOfWorkRepository.AddAsync only tracks the entity,
/// while FunctionalRepository.AddAsync tracks AND saves immediately.
/// </para>
/// <para>
/// <b>CA1001 Suppression:</b> BenchmarkDotNet manages lifecycle via [GlobalSetup]/[GlobalCleanup].
/// </para>
/// </remarks>
[MemoryDiagnoser]
[MarkdownExporter]
[RankColumn]
#pragma warning disable CA1001 // BenchmarkDotNet handles disposal via GlobalCleanup
public class UnitOfWorkRepositoryBenchmarks
#pragma warning restore CA1001
{
    private SqliteConnection _connection = null!;
    private EntityFrameworkBenchmarkDbContext _dbContext = null!;
    private IServiceProvider _serviceProvider = null!;
    private UnitOfWorkEF _unitOfWork = null!;
    private FunctionalRepositoryEF<BenchmarkEntity, Guid> _functionalRepository = null!;
    private IFunctionalRepository<BenchmarkEntity, Guid> _uowRepository = null!;

    // Pre-created test data
    private List<BenchmarkEntity> _batchEntities = null!;

    /// <summary>
    /// Number of entities for batch tracking benchmarks.
    /// </summary>
    [Params(1, 10, 100, 1000)]
    public int EntityCount { get; set; }

    [GlobalSetup]
    public void GlobalSetup()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();

        _dbContext = EntityFrameworkBenchmarkDbContext.CreateSqlite(_connection);
        _dbContext.Database.EnsureCreated();

        var services = new ServiceCollection();
        services.AddSingleton<DbContext>(_dbContext);
        _serviceProvider = services.BuildServiceProvider();

        _unitOfWork = new UnitOfWorkEF(_dbContext, _serviceProvider);
        _functionalRepository = new FunctionalRepositoryEF<BenchmarkEntity, Guid>(_dbContext);
        _uowRepository = _unitOfWork.Repository<BenchmarkEntity, Guid>();

        // Pre-create batch entities
        _batchEntities = TestData.CreateEntities(1000);
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
        // Clear database and change tracker
        _dbContext.Database.ExecuteSqlRaw("DELETE FROM BenchmarkEntities");
        _dbContext.ChangeTracker.Clear();
    }

    #region AddAsync Tracking Benchmarks

    /// <summary>
    /// UnitOfWorkRepository.AddAsync - entity tracked but NOT persisted.
    /// Measures pure tracking overhead without database I/O.
    /// </summary>
    [Benchmark(Baseline = true, Description = "UoW AddAsync (tracking only)")]
    public async Task<Either<EncinaError, BenchmarkEntity>> UoW_AddAsync_TrackingOnly()
    {
        var entity = CreateFreshEntity();
        return await _uowRepository.AddAsync(entity);
    }

    /// <summary>
    /// FunctionalRepository.AddAsync - entity tracked AND persisted.
    /// Measures overhead with database I/O for comparison.
    /// </summary>
    [Benchmark(Description = "Functional AddAsync (with SaveChanges)")]
    public async Task<Either<EncinaError, BenchmarkEntity>> Functional_AddAsync_WithSaveChanges()
    {
        var entity = CreateFreshEntity();
        return await _functionalRepository.AddAsync(entity);
    }

    /// <summary>
    /// Direct DbSet.Add without SaveChanges (baseline for tracking overhead).
    /// </summary>
    [Benchmark(Description = "Direct DbSet.Add (baseline tracking)")]
    public async Task DirectAdd_TrackingOnly()
    {
        var entity = CreateFreshEntity();
        await _dbContext.BenchmarkEntities.AddAsync(entity);
    }

    #endregion

    #region UpdateAsync Tracking Benchmarks

    /// <summary>
    /// UnitOfWorkRepository.UpdateAsync - marks entity as modified.
    /// Measures change tracking overhead.
    /// </summary>
    [Benchmark(Description = "UoW UpdateAsync (mark modified)")]
    public async Task<Either<EncinaError, BenchmarkEntity>> UoW_UpdateAsync_MarkModified()
    {
        // First add an entity
        var entity = CreateFreshEntity();
        await _dbContext.BenchmarkEntities.AddAsync(entity);
        await _dbContext.SaveChangesAsync();

        // Modify and update
        entity.Name = "Updated";
        return await _uowRepository.UpdateAsync(entity);
    }

    /// <summary>
    /// Direct DbSet.Update (baseline for update overhead).
    /// </summary>
    [Benchmark(Description = "Direct DbSet.Update (baseline)")]
    public async Task DirectUpdate_MarkModified()
    {
        // First add an entity
        var entity = CreateFreshEntity();
        await _dbContext.BenchmarkEntities.AddAsync(entity);
        await _dbContext.SaveChangesAsync();

        // Modify and update
        entity.Name = "Updated";
        _dbContext.BenchmarkEntities.Update(entity);
    }

    #endregion

    #region Batch Tracking Benchmarks

    /// <summary>
    /// Batch AddRangeAsync - entities tracked (parameterized count).
    /// Calculates per-entity tracking cost.
    /// </summary>
    [Benchmark(Description = "UoW AddRangeAsync (tracking batch)")]
    public async Task<Either<EncinaError, IReadOnlyList<BenchmarkEntity>>> UoW_AddRangeAsync_TrackingBatch()
    {
        var entities = CreateFreshEntities(EntityCount);
        return await _uowRepository.AddRangeAsync(entities);
    }

    /// <summary>
    /// Direct DbSet.AddRangeAsync (baseline for batch tracking).
    /// </summary>
    [Benchmark(Description = "Direct AddRangeAsync (baseline)")]
    public async Task DirectAddRange_TrackingBatch()
    {
        var entities = CreateFreshEntities(EntityCount);
        await _dbContext.BenchmarkEntities.AddRangeAsync(entities);
    }

    /// <summary>
    /// Batch tracking followed by SaveChanges.
    /// Measures deferred persistence pattern.
    /// </summary>
    [Benchmark(Description = "UoW AddRangeAsync + SaveChanges")]
    public async Task UoW_AddRangeAsync_ThenSave()
    {
        var entities = CreateFreshEntities(EntityCount);
        await _uowRepository.AddRangeAsync(entities);
        await _unitOfWork.SaveChangesAsync();
    }

    #endregion

    #region Pattern Comparison Benchmarks

    /// <summary>
    /// Deferred persistence pattern: Track multiple then save once.
    /// Unit of Work pattern - batch save.
    /// </summary>
    [Benchmark(Description = "Deferred pattern (track N, save once)")]
    public async Task DeferredPersistence_TrackManyThenSave()
    {
        // Track multiple entities
        for (var i = 0; i < Math.Min(EntityCount, 10); i++)
        {
            var entity = CreateFreshEntity();
            await _uowRepository.AddAsync(entity);
        }

        // Single SaveChanges
        await _unitOfWork.SaveChangesAsync();
    }

    /// <summary>
    /// Immediate persistence pattern: Save after each add.
    /// Traditional repository pattern - individual saves.
    /// </summary>
    [Benchmark(Description = "Immediate pattern (save each)")]
    public async Task ImmediatePersistence_SaveEach()
    {
        // Save each entity immediately
        for (var i = 0; i < Math.Min(EntityCount, 10); i++)
        {
            var entity = CreateFreshEntity();
            await _functionalRepository.AddAsync(entity);
        }
    }

    #endregion

    #region Per-Entity Tracking Cost Benchmarks

    /// <summary>
    /// Measures pure tracking overhead per entity for different batch sizes.
    /// </summary>
    [Benchmark(Description = "Per-entity tracking cost")]
    public async Task PerEntityTrackingCost()
    {
        var entities = CreateFreshEntities(EntityCount);

        foreach (var entity in entities)
        {
            await _dbContext.BenchmarkEntities.AddAsync(entity);
        }
    }

    /// <summary>
    /// ChangeTracker entry count after batch tracking.
    /// Verifies entities are tracked correctly.
    /// </summary>
    [Benchmark(Description = "ChangeTracker.Entries count")]
    [SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "BenchmarkDotNet requires instance methods")]
    public int ChangeTrackerEntriesCount()
    {
        var entities = CreateFreshEntities(EntityCount);
        _dbContext.BenchmarkEntities.AddRange(entities);

        return _dbContext.ChangeTracker.Entries().Count();
    }

    #endregion

    #region Helper Methods

    private static BenchmarkEntity CreateFreshEntity()
    {
        return new BenchmarkEntity
        {
            Id = Guid.NewGuid(),
            Name = $"Entity-{Guid.NewGuid():N}",
            Value = Random.Shared.Next(1, 10000),
            CreatedAtUtc = DateTime.UtcNow,
            Category = "Benchmark",
            IsActive = true
        };
    }

    private static List<BenchmarkEntity> CreateFreshEntities(int count)
    {
        var entities = new List<BenchmarkEntity>(count);
        for (var i = 0; i < count; i++)
        {
            entities.Add(CreateFreshEntity());
        }
        return entities;
    }

    #endregion
}
