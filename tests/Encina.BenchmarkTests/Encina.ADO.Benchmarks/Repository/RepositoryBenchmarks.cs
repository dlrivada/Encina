using System.Data;
using System.Globalization;
using System.Linq.Expressions;
using BenchmarkDotNet.Attributes;
using Encina.ADO.Benchmarks.Infrastructure;
using Encina.ADO.Sqlite.Repository;
using Encina.DomainModeling;
using Microsoft.Data.Sqlite;

namespace Encina.ADO.Benchmarks.Repository;

/// <summary>
/// Benchmarks for ADO.NET FunctionalRepository CRUD operations.
/// Compares repository abstraction vs raw ADO.NET queries.
/// </summary>
[MemoryDiagnoser]
[RankColumn]
public class RepositoryBenchmarks
{
    private SqliteConnection _connection = null!;
    private FunctionalRepositoryADO<BenchmarkRepositoryEntity, Guid> _repository = null!;
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
        _connection = AdoConnectionFactory.CreateSharedMemorySqliteConnection("repository_ado_benchmark");
        AdoSchemaBuilder.CreateBenchmarkEntityTable(_connection, DatabaseProvider.Sqlite);

        _mapping = new BenchmarkRepositoryEntityMapping();
        _repository = new FunctionalRepositoryADO<BenchmarkRepositoryEntity, Guid>(_connection, _mapping);

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
    /// Benchmarks GetById via raw ADO.NET (baseline comparison).
    /// </summary>
    [Benchmark]
    public async Task<BenchmarkRepositoryEntity?> RawAdo_GetById()
    {
        using var command = _connection.CreateCommand();
        command.CommandText = @"SELECT ""Id"", ""Name"", ""Description"", ""Price"", ""Quantity"", ""IsActive"", ""Category"", ""CreatedAtUtc"", ""UpdatedAtUtc""
                                FROM ""BenchmarkEntities"" WHERE ""Id"" = @Id";
        command.Parameters.AddWithValue("@Id", _existingEntityId.ToString());

        using var reader = await command.ExecuteReaderAsync();
        if (await reader.ReadAsync())
        {
            return MapFromReader(reader);
        }
        return null;
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
    /// Benchmarks ListAsync via raw ADO.NET.
    /// </summary>
    [Benchmark]
    public async Task<int> RawAdo_ListAll()
    {
        using var command = _connection.CreateCommand();
        command.CommandText = @"SELECT ""Id"", ""Name"", ""Description"", ""Price"", ""Quantity"", ""IsActive"", ""Category"", ""CreatedAtUtc"", ""UpdatedAtUtc""
                                FROM ""BenchmarkEntities""";

        var entities = new List<BenchmarkRepositoryEntity>();
        using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            entities.Add(MapFromReader(reader));
        }
        return entities.Count;
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
    /// Benchmarks filtered query via raw ADO.NET.
    /// </summary>
    [Benchmark]
    public async Task<int> RawAdo_FilteredQuery()
    {
        using var command = _connection.CreateCommand();
        command.CommandText = @"SELECT ""Id"", ""Name"", ""Description"", ""Price"", ""Quantity"", ""IsActive"", ""Category"", ""CreatedAtUtc"", ""UpdatedAtUtc""
                                FROM ""BenchmarkEntities"" WHERE ""IsActive"" = @IsActive AND ""Category"" = @Category";
        command.Parameters.AddWithValue("@IsActive", 1);
        command.Parameters.AddWithValue("@Category", "Electronics");

        var entities = new List<BenchmarkRepositoryEntity>();
        using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            entities.Add(MapFromReader(reader));
        }
        return entities.Count;
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
    /// Benchmarks Count via raw ADO.NET.
    /// </summary>
    [Benchmark]
    public async Task<int> RawAdo_Count()
    {
        using var command = _connection.CreateCommand();
        command.CommandText = @"SELECT COUNT(*) FROM ""BenchmarkEntities"" WHERE ""IsActive"" = @IsActive";
        command.Parameters.AddWithValue("@IsActive", 1);

        var result = await command.ExecuteScalarAsync();
        return Convert.ToInt32(result, CultureInfo.InvariantCulture);
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
    /// Benchmarks Insert via raw ADO.NET.
    /// </summary>
    [Benchmark]
    public async Task<int> RawAdo_Insert()
    {
        var entity = BenchmarkEntityFactory.CreateRepositoryEntity();
        using var command = _connection.CreateCommand();
        command.CommandText = @"INSERT INTO ""BenchmarkEntities"" (""Id"", ""Name"", ""Description"", ""Price"", ""Quantity"", ""IsActive"", ""Category"", ""CreatedAtUtc"", ""UpdatedAtUtc"")
                                VALUES (@Id, @Name, @Description, @Price, @Quantity, @IsActive, @Category, @CreatedAtUtc, @UpdatedAtUtc)";
        AddEntityParameters(command, entity);
        return await command.ExecuteNonQueryAsync();
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
    /// Benchmarks Update via raw ADO.NET.
    /// </summary>
    [Benchmark]
    public async Task<int> RawAdo_Update()
    {
        using var command = _connection.CreateCommand();
        command.CommandText = @"UPDATE ""BenchmarkEntities"" SET ""Name"" = @Name, ""Description"" = @Description, ""Price"" = @Price,
                                ""Quantity"" = @Quantity, ""IsActive"" = @IsActive, ""Category"" = @Category, ""UpdatedAtUtc"" = @UpdatedAtUtc
                                WHERE ""Id"" = @Id";
        command.Parameters.AddWithValue("@Id", _entityToUpdate.Id.ToString());
        command.Parameters.AddWithValue("@Name", _entityToUpdate.Name);
        command.Parameters.AddWithValue("@Description", _entityToUpdate.Description ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@Price", _entityToUpdate.Price);
        command.Parameters.AddWithValue("@Quantity", _entityToUpdate.Quantity + 1);
        command.Parameters.AddWithValue("@IsActive", _entityToUpdate.IsActive ? 1 : 0);
        command.Parameters.AddWithValue("@Category", _entityToUpdate.Category);
        command.Parameters.AddWithValue("@UpdatedAtUtc", DateTime.UtcNow.ToString("O"));

        return await command.ExecuteNonQueryAsync();
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
    /// Benchmarks Delete via raw ADO.NET.
    /// </summary>
    [Benchmark]
    public async Task<int> RawAdo_Delete()
    {
        // Create and delete a new entity each time
        var entity = BenchmarkEntityFactory.CreateRepositoryEntity();
        InsertEntityDirect(entity);

        using var command = _connection.CreateCommand();
        command.CommandText = @"DELETE FROM ""BenchmarkEntities"" WHERE ""Id"" = @Id";
        command.Parameters.AddWithValue("@Id", entity.Id.ToString());
        return await command.ExecuteNonQueryAsync();
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
    /// Benchmarks bulk insert via raw ADO.NET.
    /// </summary>
    [Benchmark]
    public async Task<int> RawAdo_BulkInsert()
    {
        var entities = BenchmarkEntityFactory.CreateRepositoryEntities(BatchSize);
        var count = 0;

        foreach (var entity in entities)
        {
            using var command = _connection.CreateCommand();
            command.CommandText = @"INSERT INTO ""BenchmarkEntities"" (""Id"", ""Name"", ""Description"", ""Price"", ""Quantity"", ""IsActive"", ""Category"", ""CreatedAtUtc"", ""UpdatedAtUtc"")
                                    VALUES (@Id, @Name, @Description, @Price, @Quantity, @IsActive, @Category, @CreatedAtUtc, @UpdatedAtUtc)";
            AddEntityParameters(command, entity);
            count += await command.ExecuteNonQueryAsync();
        }

        return count;
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
    /// Benchmarks bulk update via raw ADO.NET.
    /// </summary>
    [Benchmark]
    public async Task<int> RawAdo_BulkUpdate()
    {
        var entitiesToUpdate = _seededEntities.Take(BatchSize).ToList();
        var count = 0;

        foreach (var entity in entitiesToUpdate)
        {
            using var command = _connection.CreateCommand();
            command.CommandText = @"UPDATE ""BenchmarkEntities"" SET ""Name"" = @Name, ""Description"" = @Description, ""Price"" = @Price,
                                    ""Quantity"" = @Quantity, ""IsActive"" = @IsActive, ""Category"" = @Category, ""UpdatedAtUtc"" = @UpdatedAtUtc
                                    WHERE ""Id"" = @Id";
            command.Parameters.AddWithValue("@Id", entity.Id.ToString());
            command.Parameters.AddWithValue("@Name", entity.Name);
            command.Parameters.AddWithValue("@Description", entity.Description ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@Price", entity.Price);
            command.Parameters.AddWithValue("@Quantity", entity.Quantity);
            command.Parameters.AddWithValue("@IsActive", entity.IsActive ? 1 : 0);
            command.Parameters.AddWithValue("@Category", entity.Category);
            command.Parameters.AddWithValue("@UpdatedAtUtc", DateTime.UtcNow.ToString("O"));

            count += await command.ExecuteNonQueryAsync();
        }

        return count;
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
    /// Benchmarks bulk delete via raw ADO.NET.
    /// </summary>
    [Benchmark]
    public async Task<int> RawAdo_BulkDelete()
    {
        // Create test entities to delete
        var entities = BenchmarkEntityFactory.CreateRepositoryEntities(BatchSize);
        foreach (var entity in entities)
        {
            entity.Category = "ToDeleteRaw";
            InsertEntityDirect(entity);
        }

        using var command = _connection.CreateCommand();
        command.CommandText = @"DELETE FROM ""BenchmarkEntities"" WHERE ""Category"" = @Category";
        command.Parameters.AddWithValue("@Category", "ToDeleteRaw");
        return await command.ExecuteNonQueryAsync();
    }

    #endregion

    #region Helper Methods

    private void InsertEntityDirect(BenchmarkRepositoryEntity entity)
    {
        using var command = _connection.CreateCommand();
        command.CommandText = @"INSERT INTO ""BenchmarkEntities"" (""Id"", ""Name"", ""Description"", ""Price"", ""Quantity"", ""IsActive"", ""Category"", ""CreatedAtUtc"", ""UpdatedAtUtc"")
                                VALUES (@Id, @Name, @Description, @Price, @Quantity, @IsActive, @Category, @CreatedAtUtc, @UpdatedAtUtc)";
        AddEntityParameters(command, entity);
        command.ExecuteNonQuery();
    }

    private static void AddEntityParameters(IDbCommand command, BenchmarkRepositoryEntity entity)
    {
        var idParam = command.CreateParameter();
        idParam.ParameterName = "@Id";
        idParam.Value = entity.Id.ToString();
        command.Parameters.Add(idParam);

        var nameParam = command.CreateParameter();
        nameParam.ParameterName = "@Name";
        nameParam.Value = entity.Name;
        command.Parameters.Add(nameParam);

        var descParam = command.CreateParameter();
        descParam.ParameterName = "@Description";
        descParam.Value = entity.Description ?? (object)DBNull.Value;
        command.Parameters.Add(descParam);

        var priceParam = command.CreateParameter();
        priceParam.ParameterName = "@Price";
        priceParam.Value = entity.Price;
        command.Parameters.Add(priceParam);

        var qtyParam = command.CreateParameter();
        qtyParam.ParameterName = "@Quantity";
        qtyParam.Value = entity.Quantity;
        command.Parameters.Add(qtyParam);

        var activeParam = command.CreateParameter();
        activeParam.ParameterName = "@IsActive";
        activeParam.Value = entity.IsActive ? 1 : 0;
        command.Parameters.Add(activeParam);

        var categoryParam = command.CreateParameter();
        categoryParam.ParameterName = "@Category";
        categoryParam.Value = entity.Category;
        command.Parameters.Add(categoryParam);

        var createdParam = command.CreateParameter();
        createdParam.ParameterName = "@CreatedAtUtc";
        createdParam.Value = entity.CreatedAtUtc.ToString("O");
        command.Parameters.Add(createdParam);

        var updatedParam = command.CreateParameter();
        updatedParam.ParameterName = "@UpdatedAtUtc";
        updatedParam.Value = entity.UpdatedAtUtc?.ToString("O") ?? (object)DBNull.Value;
        command.Parameters.Add(updatedParam);
    }

    private static BenchmarkRepositoryEntity MapFromReader(SqliteDataReader reader)
    {
        return new BenchmarkRepositoryEntity
        {
            Id = Guid.Parse(reader.GetString(0)),
            Name = reader.GetString(1),
            Description = reader.IsDBNull(2) ? null : reader.GetString(2),
            Price = reader.GetDecimal(3),
            Quantity = reader.GetInt32(4),
            IsActive = reader.GetInt32(5) == 1,
            Category = reader.GetString(6),
            CreatedAtUtc = DateTime.Parse(reader.GetString(7), CultureInfo.InvariantCulture),
            UpdatedAtUtc = reader.IsDBNull(8) ? null : DateTime.Parse(reader.GetString(8), CultureInfo.InvariantCulture)
        };
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
