using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using BenchmarkDotNet.Attributes;
using Encina.Benchmarks.EntityFrameworkCore.Infrastructure;
using Encina.DomainModeling;
using Encina.EntityFrameworkCore.Repository;
using LanguageExt;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Encina.Benchmarks.EntityFrameworkCore;

/// <summary>
/// Benchmarks for <see cref="FunctionalRepositoryEF{TEntity, TId}"/> measuring
/// CRUD operations, tracking behavior, and exception mapping overhead.
/// </summary>
/// <remarks>
/// <para>
/// These benchmarks use SQLite in-memory database for realistic persistence behavior
/// while measuring the repository abstraction overhead.
/// </para>
/// <para>
/// <b>CA1001 Suppression:</b> BenchmarkDotNet manages lifecycle via [GlobalSetup]/[GlobalCleanup].
/// </para>
/// </remarks>
[MemoryDiagnoser]
[MarkdownExporter]
[RankColumn]
#pragma warning disable CA1001 // BenchmarkDotNet handles disposal via GlobalCleanup
public class FunctionalRepositoryBenchmarks
#pragma warning restore CA1001
{
    private SqliteConnection _connection = null!;
    private EntityFrameworkBenchmarkDbContext _dbContext = null!;
    private FunctionalRepositoryEF<BenchmarkEntity, Guid> _repository = null!;

    // Pre-created test data
    private Guid _existingEntityId;
    private List<BenchmarkEntity> _batchEntities = null!;

    /// <summary>
    /// Batch size for AddRangeAsync benchmarks.
    /// </summary>
    [Params(10, 100, 1000)]
    public int BatchSize { get; set; }

    [GlobalSetup]
    public void GlobalSetup()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();

        _dbContext = EntityFrameworkBenchmarkDbContext.CreateSqlite(_connection);
        _dbContext.Database.EnsureCreated();

        _repository = new FunctionalRepositoryEF<BenchmarkEntity, Guid>(_dbContext);

        // Pre-seed one entity for GetByIdAsync benchmarks
        var seedEntity = TestData.CreateEntity(0);
        _existingEntityId = seedEntity.Id;
        _dbContext.BenchmarkEntities.Add(seedEntity);
        _dbContext.SaveChanges();
        _dbContext.ChangeTracker.Clear();

        // Pre-create batch entities for AddRangeAsync
        _batchEntities = TestData.CreateEntities(1000);
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
        // Clear entities except the seed entity
        _dbContext.Database.ExecuteSqlRaw("DELETE FROM BenchmarkEntities WHERE Id != {0}", _existingEntityId);
        _dbContext.ChangeTracker.Clear();
    }

    #region GetByIdAsync Benchmarks

    /// <summary>
    /// GetByIdAsync with entity in database (cache miss, then identity map hit).
    /// Measures FindAsync overhead including identity map lookup.
    /// </summary>
    [Benchmark(Baseline = true, Description = "GetByIdAsync (existing entity)")]
    public async Task<Either<EncinaError, BenchmarkEntity>> GetByIdAsync_ExistingEntity()
    {
        return await _repository.GetByIdAsync(_existingEntityId);
    }

    /// <summary>
    /// GetByIdAsync with non-existent entity (NotFound path).
    /// Measures the cost of the NotFound error creation.
    /// </summary>
    [Benchmark(Description = "GetByIdAsync (not found)")]
    public async Task<Either<EncinaError, BenchmarkEntity>> GetByIdAsync_NotFound()
    {
        return await _repository.GetByIdAsync(Guid.NewGuid());
    }

    /// <summary>
    /// GetByIdAsync with identity map hit (entity already tracked).
    /// Measures the fast path when entity is already in change tracker.
    /// </summary>
    [Benchmark(Description = "GetByIdAsync (identity map hit)")]
    public async Task<Either<EncinaError, BenchmarkEntity>> GetByIdAsync_IdentityMapHit()
    {
        // First call loads into identity map
        _ = await _repository.GetByIdAsync(_existingEntityId);
        // Second call should hit identity map
        return await _repository.GetByIdAsync(_existingEntityId);
    }

    #endregion

    #region ListAsync Benchmarks

    /// <summary>
    /// ListAsync with AsNoTracking (default behavior).
    /// </summary>
    [Benchmark(Description = "ListAsync (AsNoTracking)")]
    public async Task<Either<EncinaError, IReadOnlyList<BenchmarkEntity>>> ListAsync_NoTracking()
    {
        return await _repository.ListAsync();
    }

    /// <summary>
    /// ListAsync with specification filter.
    /// Measures SpecificationEvaluator integration overhead.
    /// </summary>
    [Benchmark(Description = "ListAsync (with specification)")]
    public async Task<Either<EncinaError, IReadOnlyList<BenchmarkEntity>>> ListAsync_WithSpecification()
    {
        var spec = new ActiveEntitiesSpec();
        return await _repository.ListAsync(spec);
    }

    /// <summary>
    /// Direct DbSet query for comparison (baseline without repository abstraction).
    /// </summary>
    [Benchmark(Description = "Direct DbSet.ToListAsync (baseline)")]
    public async Task<List<BenchmarkEntity>> DirectDbSet_ToList()
    {
        return await _dbContext.BenchmarkEntities.AsNoTracking().ToListAsync();
    }

    #endregion

    #region AddAsync Benchmarks

    /// <summary>
    /// AddAsync single entity with SaveChanges.
    /// Measures per-entity overhead.
    /// </summary>
    [Benchmark(Description = "AddAsync (single entity)")]
    public async Task<Either<EncinaError, BenchmarkEntity>> AddAsync_SingleEntity()
    {
        var entity = TestData.CreateEntity(Random.Shared.Next());
        return await _repository.AddAsync(entity);
    }

    /// <summary>
    /// Direct DbContext.Add + SaveChanges for comparison.
    /// </summary>
    [Benchmark(Description = "Direct Add + SaveChanges (baseline)")]
    public async Task<BenchmarkEntity> DirectAdd_WithSaveChanges()
    {
        var entity = TestData.CreateEntity(Random.Shared.Next());
        await _dbContext.BenchmarkEntities.AddAsync(entity);
        await _dbContext.SaveChangesAsync();
        return entity;
    }

    #endregion

    #region AddRangeAsync Benchmarks

    /// <summary>
    /// AddRangeAsync with parameterized batch sizes.
    /// Demonstrates batching benefits.
    /// </summary>
    [Benchmark(Description = "AddRangeAsync (batch)")]
    public async Task<Either<EncinaError, IReadOnlyList<BenchmarkEntity>>> AddRangeAsync_Batch()
    {
        var entities = _batchEntities.Take(BatchSize).Select(e => new BenchmarkEntity
        {
            Id = Guid.NewGuid(),
            Name = e.Name,
            Value = e.Value,
            CreatedAtUtc = e.CreatedAtUtc,
            Category = e.Category,
            IsActive = e.IsActive
        }).ToList();

        return await _repository.AddRangeAsync(entities);
    }

    /// <summary>
    /// Per-entity overhead calculation: AddAsync in a loop vs AddRangeAsync.
    /// </summary>
    [Benchmark(Description = "AddAsync in loop (for comparison)")]
    public async Task AddAsync_Loop()
    {
        var entities = _batchEntities.Take(Math.Min(BatchSize, 10)).ToList(); // Limit to 10 for loop comparison

        foreach (var template in entities)
        {
            var entity = new BenchmarkEntity
            {
                Id = Guid.NewGuid(),
                Name = template.Name,
                Value = template.Value,
                CreatedAtUtc = template.CreatedAtUtc,
                Category = template.Category,
                IsActive = template.IsActive
            };
            await _repository.AddAsync(entity);
        }
    }

    #endregion

    #region DeleteRangeAsync Benchmarks

    /// <summary>
    /// DeleteRangeAsync using ExecuteDeleteAsync (bulk delete).
    /// </summary>
    [Benchmark(Description = "DeleteRangeAsync (ExecuteDelete)")]
    public async Task<Either<EncinaError, int>> DeleteRangeAsync_BulkDelete()
    {
        // Setup: Add entities to delete
        var entities = TestData.CreateEntitiesForCategory("ToDelete", 10);
        await _dbContext.BenchmarkEntities.AddRangeAsync(entities);
        await _dbContext.SaveChangesAsync();
        _dbContext.ChangeTracker.Clear();

        // Benchmark: Bulk delete
        var spec = new CategorySpec("ToDelete");
        return await _repository.DeleteRangeAsync(spec);
    }

    #endregion

    #region IsDuplicateKeyException Benchmarks

    /// <summary>
    /// IsDuplicateKeyException string matching overhead.
    /// Tests the cold path when a duplicate key exception occurs.
    /// </summary>
    [Benchmark(Description = "AddAsync (duplicate key exception)")]
    public async Task<Either<EncinaError, BenchmarkEntity>> AddAsync_DuplicateKey()
    {
        // Try to add entity with existing ID (should fail)
        var duplicateEntity = TestData.CreateEntityWithId(_existingEntityId, 999);
        return await _repository.AddAsync(duplicateEntity);
    }

    #endregion

    #region Test Specification Classes

    /// <summary>
    /// Specification for active entities.
    /// </summary>
    private sealed class ActiveEntitiesSpec : QuerySpecification<BenchmarkEntity>
    {
        public ActiveEntitiesSpec()
        {
            AddCriteria(e => e.IsActive);
        }
    }

    /// <summary>
    /// Specification for entities in a specific category.
    /// </summary>
    private sealed class CategorySpec : QuerySpecification<BenchmarkEntity>
    {
        public CategorySpec(string category)
        {
            AddCriteria(e => e.Category == category);
        }
    }

    #endregion
}
