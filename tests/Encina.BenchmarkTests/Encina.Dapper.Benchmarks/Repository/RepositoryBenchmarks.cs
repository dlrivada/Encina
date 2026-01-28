using System.Linq.Expressions;
using BenchmarkDotNet.Attributes;
using Dapper;
using Encina.Dapper.Benchmarks.Infrastructure;
using Encina.Dapper.Sqlite.Repository;
using Encina.Dapper.Sqlite.TypeHandlers;
using Encina.DomainModeling;
using Microsoft.Data.Sqlite;

namespace Encina.Dapper.Benchmarks.Repository;

/// <summary>
/// Benchmarks for Dapper FunctionalRepository CRUD operations.
/// Compares repository abstraction vs raw Dapper queries.
/// </summary>
[MemoryDiagnoser]
[RankColumn]
public class RepositoryBenchmarks
{
    private SqliteConnection _connection = null!;
    private FunctionalRepositoryDapper<BenchmarkRepositoryEntity, Guid> _repository = null!;
    private BenchmarkRepositoryEntityMapping _mapping = null!;
    private List<BenchmarkRepositoryEntity> _seededEntities = null!;
    private Guid _existingEntityId;
    private BenchmarkRepositoryEntity _entityToUpdate = null!;
    private int _addCounter;

    /// <summary>
    /// Batch size for bulk operations.
    /// </summary>
    [Params(10, 100, 1000)]
    public int BatchSize { get; set; }

    /// <summary>
    /// Global setup - creates database and populates with test data.
    /// </summary>
    [GlobalSetup]
    public void GlobalSetup()
    {
        _connection = DapperConnectionFactory.CreateSharedMemorySqliteConnection("repository_dapper_benchmark");
        DapperSchemaBuilder.CreateBenchmarkEntityTable(_connection, DatabaseProvider.Sqlite);

        // Ensure type handlers are registered
        GuidTypeHandler.EnsureRegistered();

        _mapping = new BenchmarkRepositoryEntityMapping();
        _repository = new FunctionalRepositoryDapper<BenchmarkRepositoryEntity, Guid>(_connection, _mapping);

        // Seed initial data
        _seededEntities = BenchmarkEntityFactory.CreateRepositoryEntities(2000);
        foreach (var entity in _seededEntities)
        {
            InsertEntityDirect(entity);
        }

        _existingEntityId = _seededEntities[0].Id;
        _entityToUpdate = _seededEntities[1];
        _addCounter = 0;
    }

    /// <summary>
    /// Iteration setup - prepares entities for each iteration.
    /// </summary>
    [IterationSetup]
    public void IterationSetup()
    {
        _addCounter++;
    }

    /// <summary>
    /// Global cleanup - disposes resources.
    /// </summary>
    [GlobalCleanup]
    public void GlobalCleanup()
    {
        _connection.Dispose();
    }

    #region Read Operations

    /// <summary>
    /// Benchmarks GetByIdAsync via repository.
    /// </summary>
    [Benchmark(Baseline = true)]
    public async Task<BenchmarkRepositoryEntity?> Repository_GetByIdAsync()
    {
        var result = await _repository.GetByIdAsync(_existingEntityId);
        return result.Match(entity => entity, _ => null);
    }

    /// <summary>
    /// Benchmarks GetById via raw Dapper (baseline comparison).
    /// </summary>
    [Benchmark]
    public async Task<BenchmarkRepositoryEntity?> RawDapper_GetById()
    {
        var sql = @"SELECT ""Id"", ""Name"", ""Description"", ""Price"", ""Quantity"", ""IsActive"", ""Category"", ""CreatedAtUtc"", ""UpdatedAtUtc""
                    FROM ""BenchmarkEntities"" WHERE ""Id"" = @Id";
        return await _connection.QuerySingleOrDefaultAsync<BenchmarkRepositoryEntity>(sql, new { Id = _existingEntityId });
    }

    /// <summary>
    /// Benchmarks ListAsync (all entities) via repository.
    /// </summary>
    [Benchmark]
    public async Task<int> Repository_ListAsync()
    {
        var result = await _repository.ListAsync();
        return result.Match(list => list.Count, _ => 0);
    }

    /// <summary>
    /// Benchmarks ListAsync via raw Dapper.
    /// </summary>
    [Benchmark]
    public async Task<int> RawDapper_ListAll()
    {
        var sql = @"SELECT ""Id"", ""Name"", ""Description"", ""Price"", ""Quantity"", ""IsActive"", ""Category"", ""CreatedAtUtc"", ""UpdatedAtUtc""
                    FROM ""BenchmarkEntities""";
        var entities = await _connection.QueryAsync<BenchmarkRepositoryEntity>(sql);
        return entities.Count();
    }

    /// <summary>
    /// Benchmarks ListAsync with specification via repository.
    /// </summary>
    [Benchmark]
    public async Task<int> Repository_ListWithSpecification()
    {
        var spec = new ActiveEntitiesInCategorySpec("Electronics");
        var result = await _repository.ListAsync(spec);
        return result.Match(list => list.Count, _ => 0);
    }

    /// <summary>
    /// Benchmarks filtered query via raw Dapper.
    /// </summary>
    [Benchmark]
    public async Task<int> RawDapper_FilteredQuery()
    {
        var sql = @"SELECT ""Id"", ""Name"", ""Description"", ""Price"", ""Quantity"", ""IsActive"", ""Category"", ""CreatedAtUtc"", ""UpdatedAtUtc""
                    FROM ""BenchmarkEntities"" WHERE ""IsActive"" = @IsActive AND ""Category"" = @Category";
        var entities = await _connection.QueryAsync<BenchmarkRepositoryEntity>(sql, new { IsActive = 1, Category = "Electronics" });
        return entities.Count();
    }

    /// <summary>
    /// Benchmarks FirstOrDefaultAsync via repository.
    /// </summary>
    [Benchmark]
    public async Task<BenchmarkRepositoryEntity?> Repository_FirstOrDefaultAsync()
    {
        var spec = new EntityByNameSpec("Product-1");
        var result = await _repository.FirstOrDefaultAsync(spec);
        return result.Match(entity => entity, _ => null);
    }

    /// <summary>
    /// Benchmarks CountAsync via repository.
    /// </summary>
    [Benchmark]
    public async Task<int> Repository_CountAsync()
    {
        var spec = new ActiveEntitiesSpec();
        var result = await _repository.CountAsync(spec);
        return result.Match(count => count, _ => 0);
    }

    /// <summary>
    /// Benchmarks Count via raw Dapper.
    /// </summary>
    [Benchmark]
    public async Task<int> RawDapper_Count()
    {
        var sql = @"SELECT COUNT(*) FROM ""BenchmarkEntities"" WHERE ""IsActive"" = @IsActive";
        return await _connection.ExecuteScalarAsync<int>(sql, new { IsActive = 1 });
    }

    /// <summary>
    /// Benchmarks AnyAsync via repository.
    /// </summary>
    [Benchmark]
    public async Task<bool> Repository_AnyAsync()
    {
        var spec = new ActiveEntitiesInCategorySpec("Books");
        var result = await _repository.AnyAsync(spec);
        return result.Match(any => any, _ => false);
    }

    #endregion

    #region Write Operations

    /// <summary>
    /// Benchmarks AddAsync via repository.
    /// </summary>
    [Benchmark]
    public async Task<BenchmarkRepositoryEntity?> Repository_AddAsync()
    {
        var entity = BenchmarkEntityFactory.CreateRepositoryEntity();
        var result = await _repository.AddAsync(entity);
        return result.Match(e => e, _ => null);
    }

    /// <summary>
    /// Benchmarks Insert via raw Dapper.
    /// </summary>
    [Benchmark]
    public async Task<int> RawDapper_Insert()
    {
        var entity = BenchmarkEntityFactory.CreateRepositoryEntity();
        var sql = @"INSERT INTO ""BenchmarkEntities"" (""Id"", ""Name"", ""Description"", ""Price"", ""Quantity"", ""IsActive"", ""Category"", ""CreatedAtUtc"", ""UpdatedAtUtc"")
                    VALUES (@Id, @Name, @Description, @Price, @Quantity, @IsActive, @Category, @CreatedAtUtc, @UpdatedAtUtc)";
        return await _connection.ExecuteAsync(sql, entity);
    }

    /// <summary>
    /// Benchmarks UpdateAsync via repository.
    /// </summary>
    [Benchmark]
    public async Task<BenchmarkRepositoryEntity?> Repository_UpdateAsync()
    {
        _entityToUpdate.UpdatedAtUtc = DateTime.UtcNow;
        _entityToUpdate.Quantity++;
        var result = await _repository.UpdateAsync(_entityToUpdate);
        return result.Match(e => e, _ => null);
    }

    /// <summary>
    /// Benchmarks Update via raw Dapper.
    /// </summary>
    [Benchmark]
    public async Task<int> RawDapper_Update()
    {
        var sql = @"UPDATE ""BenchmarkEntities"" SET ""Name"" = @Name, ""Description"" = @Description, ""Price"" = @Price,
                    ""Quantity"" = @Quantity, ""IsActive"" = @IsActive, ""Category"" = @Category, ""UpdatedAtUtc"" = @UpdatedAtUtc
                    WHERE ""Id"" = @Id";
        return await _connection.ExecuteAsync(sql, new
        {
            _entityToUpdate.Id,
            _entityToUpdate.Name,
            _entityToUpdate.Description,
            _entityToUpdate.Price,
            Quantity = _entityToUpdate.Quantity + 1,
            _entityToUpdate.IsActive,
            _entityToUpdate.Category,
            UpdatedAtUtc = DateTime.UtcNow
        });
    }

    /// <summary>
    /// Benchmarks DeleteAsync via repository.
    /// </summary>
    [Benchmark]
    public async Task Repository_DeleteAsync()
    {
        // Create and delete a new entity each time
        var entity = BenchmarkEntityFactory.CreateRepositoryEntity();
        InsertEntityDirect(entity);
        await _repository.DeleteAsync(entity.Id);
    }

    /// <summary>
    /// Benchmarks Delete via raw Dapper.
    /// </summary>
    [Benchmark]
    public async Task<int> RawDapper_Delete()
    {
        // Create and delete a new entity each time
        var entity = BenchmarkEntityFactory.CreateRepositoryEntity();
        InsertEntityDirect(entity);
        var sql = @"DELETE FROM ""BenchmarkEntities"" WHERE ""Id"" = @Id";
        return await _connection.ExecuteAsync(sql, new { entity.Id });
    }

    #endregion

    #region Bulk Operations

    /// <summary>
    /// Benchmarks AddRangeAsync via repository.
    /// </summary>
    [Benchmark]
    public async Task<int> Repository_AddRangeAsync()
    {
        var entities = BenchmarkEntityFactory.CreateRepositoryEntities(BatchSize);
        var result = await _repository.AddRangeAsync(entities);
        return result.Match(list => list.Count, _ => 0);
    }

    /// <summary>
    /// Benchmarks bulk insert via raw Dapper.
    /// </summary>
    [Benchmark]
    public async Task<int> RawDapper_BulkInsert()
    {
        var entities = BenchmarkEntityFactory.CreateRepositoryEntities(BatchSize);
        var sql = @"INSERT INTO ""BenchmarkEntities"" (""Id"", ""Name"", ""Description"", ""Price"", ""Quantity"", ""IsActive"", ""Category"", ""CreatedAtUtc"", ""UpdatedAtUtc"")
                    VALUES (@Id, @Name, @Description, @Price, @Quantity, @IsActive, @Category, @CreatedAtUtc, @UpdatedAtUtc)";
        return await _connection.ExecuteAsync(sql, entities);
    }

    /// <summary>
    /// Benchmarks UpdateRangeAsync via repository.
    /// </summary>
    [Benchmark]
    public async Task Repository_UpdateRangeAsync()
    {
        var entitiesToUpdate = _seededEntities.Take(BatchSize).ToList();
        foreach (var entity in entitiesToUpdate)
        {
            entity.UpdatedAtUtc = DateTime.UtcNow;
        }
        await _repository.UpdateRangeAsync(entitiesToUpdate);
    }

    /// <summary>
    /// Benchmarks bulk update via raw Dapper.
    /// </summary>
    [Benchmark]
    public async Task<int> RawDapper_BulkUpdate()
    {
        var entitiesToUpdate = _seededEntities.Take(BatchSize).Select(e => new
        {
            e.Id,
            e.Name,
            e.Description,
            e.Price,
            e.Quantity,
            e.IsActive,
            e.Category,
            UpdatedAtUtc = DateTime.UtcNow
        }).ToList();

        var sql = @"UPDATE ""BenchmarkEntities"" SET ""Name"" = @Name, ""Description"" = @Description, ""Price"" = @Price,
                    ""Quantity"" = @Quantity, ""IsActive"" = @IsActive, ""Category"" = @Category, ""UpdatedAtUtc"" = @UpdatedAtUtc
                    WHERE ""Id"" = @Id";
        return await _connection.ExecuteAsync(sql, entitiesToUpdate);
    }

    /// <summary>
    /// Benchmarks DeleteRangeAsync via repository with specification.
    /// </summary>
    [Benchmark]
    public async Task<int> Repository_DeleteRangeAsync()
    {
        // Create test entities to delete
        var entities = BenchmarkEntityFactory.CreateRepositoryEntities(BatchSize);
        foreach (var entity in entities)
        {
            entity.Category = "ToDelete";
            InsertEntityDirect(entity);
        }

        var spec = new EntitiesInCategorySpec("ToDelete");
        var result = await _repository.DeleteRangeAsync(spec);
        return result.Match(count => count, _ => 0);
    }

    /// <summary>
    /// Benchmarks bulk delete via raw Dapper.
    /// </summary>
    [Benchmark]
    public async Task<int> RawDapper_BulkDelete()
    {
        // Create test entities to delete
        var entities = BenchmarkEntityFactory.CreateRepositoryEntities(BatchSize);
        foreach (var entity in entities)
        {
            entity.Category = "ToDeleteRaw";
            InsertEntityDirect(entity);
        }

        var sql = @"DELETE FROM ""BenchmarkEntities"" WHERE ""Category"" = @Category";
        return await _connection.ExecuteAsync(sql, new { Category = "ToDeleteRaw" });
    }

    #endregion

    #region Helper Methods

    private void InsertEntityDirect(BenchmarkRepositoryEntity entity)
    {
        var sql = @"INSERT INTO ""BenchmarkEntities"" (""Id"", ""Name"", ""Description"", ""Price"", ""Quantity"", ""IsActive"", ""Category"", ""CreatedAtUtc"", ""UpdatedAtUtc"")
                    VALUES (@Id, @Name, @Description, @Price, @Quantity, @IsActive, @Category, @CreatedAtUtc, @UpdatedAtUtc)";
        _connection.Execute(sql, entity);
    }

    #endregion
}

#region Specifications

/// <summary>
/// Specification for active entities.
/// </summary>
public class ActiveEntitiesSpec : Specification<BenchmarkRepositoryEntity>
{
    /// <inheritdoc />
    public override Expression<Func<BenchmarkRepositoryEntity, bool>> ToExpression()
        => e => e.IsActive;
}

/// <summary>
/// Specification for active entities in a specific category.
/// </summary>
public class ActiveEntitiesInCategorySpec : Specification<BenchmarkRepositoryEntity>
{
    private readonly string _category;

    /// <summary>
    /// Initializes a new instance with the specified category.
    /// </summary>
    public ActiveEntitiesInCategorySpec(string category)
    {
        _category = category;
    }

    /// <inheritdoc />
    public override Expression<Func<BenchmarkRepositoryEntity, bool>> ToExpression()
        => e => e.IsActive && e.Category == _category;
}

/// <summary>
/// Specification for entities in a specific category.
/// </summary>
public class EntitiesInCategorySpec : Specification<BenchmarkRepositoryEntity>
{
    private readonly string _category;

    /// <summary>
    /// Initializes a new instance with the specified category.
    /// </summary>
    public EntitiesInCategorySpec(string category)
    {
        _category = category;
    }

    /// <inheritdoc />
    public override Expression<Func<BenchmarkRepositoryEntity, bool>> ToExpression()
        => e => e.Category == _category;
}

/// <summary>
/// Specification for entity by name.
/// </summary>
public class EntityByNameSpec : Specification<BenchmarkRepositoryEntity>
{
    private readonly string _name;

    /// <summary>
    /// Initializes a new instance with the specified name.
    /// </summary>
    public EntityByNameSpec(string name)
    {
        _name = name;
    }

    /// <inheritdoc />
    public override Expression<Func<BenchmarkRepositoryEntity, bool>> ToExpression()
        => e => e.Name == _name;
}

#endregion
