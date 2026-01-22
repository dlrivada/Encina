using System.Data;
using BenchmarkDotNet.Attributes;
using Encina.ADO.Sqlite.BulkOperations;
using Encina.Dapper.Sqlite.BulkOperations;
using Encina.DomainModeling;
using Encina.EntityFrameworkCore.BulkOperations;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using AdoMapping = Encina.ADO.Sqlite.Repository;
using DapperMapping = Encina.Dapper.Sqlite.Repository;

namespace Encina.Benchmarks.BulkOperations;

/// <summary>
/// Benchmarks comparing BulkInsertAsync across ADO.NET, Dapper, and EF Core providers.
/// Uses SQLite in-memory database for consistent benchmark results without external dependencies.
/// </summary>
[MemoryDiagnoser]
[RankColumn]
#pragma warning disable CA1001 // BenchmarkDotNet handles disposal via GlobalCleanup
public class SqliteBulkInsertComparisonBenchmarks
#pragma warning restore CA1001
{
    private SqliteConnection _connection = null!;
    private BulkBenchmarkDbContext _dbContext = null!;
    private BulkOperationsSqlite<SqliteBenchmarkEntity, Guid> _adoBulkOps = null!;
    private BulkOperationsDapper<SqliteBenchmarkEntity, Guid> _dapperBulkOps = null!;
    private BulkOperationsEF<BenchmarkEntity> _efBulkOps = null!;
    private List<SqliteBenchmarkEntity> _sqliteEntities = null!;
    private List<BenchmarkEntity> _efEntities = null!;

    /// <summary>
    /// Number of entities to insert in the benchmark.
    /// </summary>
    [Params(100, 1000, 5000)]
    public int EntityCount { get; set; }

    [GlobalSetup]
    public void GlobalSetup()
    {
        // Setup SQLite connection for ADO.NET and Dapper
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();
        CreateTable(_connection);

        var adoMapping = new SqliteBenchmarkEntityMapping();
        var dapperMapping = new SqliteBenchmarkEntityDapperMapping();

        _adoBulkOps = new BulkOperationsSqlite<SqliteBenchmarkEntity, Guid>(_connection, adoMapping);
        _dapperBulkOps = new BulkOperationsDapper<SqliteBenchmarkEntity, Guid>(_connection, dapperMapping);

        // Setup EF Core DbContext
        var options = new DbContextOptionsBuilder<BulkBenchmarkDbContext>()
            .UseSqlite("Data Source=:memory:")
            .Options;

        _dbContext = new BulkBenchmarkDbContext(options);
        _dbContext.Database.OpenConnection();
        _dbContext.Database.EnsureCreated();

        _efBulkOps = new BulkOperationsEF<BenchmarkEntity>(_dbContext);
    }

    [GlobalCleanup]
    public void GlobalCleanup()
    {
        _connection?.Dispose();
        _dbContext?.Dispose();
    }

    [IterationSetup]
    public void IterationSetup()
    {
        // Clear tables before each iteration
        using var clearCmd = _connection.CreateCommand();
        clearCmd.CommandText = "DELETE FROM BenchmarkEntities";
        clearCmd.ExecuteNonQuery();

        _dbContext.BenchmarkEntities.ExecuteDelete();

        // Create fresh entities for this iteration
        _sqliteEntities = Enumerable.Range(0, EntityCount)
            .Select(i => new SqliteBenchmarkEntity
            {
                Id = Guid.NewGuid(),
                Name = $"Entity_{i}",
                Amount = i * 10.5m,
                IsActive = i % 2 == 0,
                CreatedAtUtc = DateTime.UtcNow
            })
            .ToList();

        _efEntities = Enumerable.Range(0, EntityCount)
            .Select(i => new BenchmarkEntity
            {
                Id = Guid.NewGuid(),
                Name = $"Entity_{i}",
                Amount = i * 10.5m,
                IsActive = i % 2 == 0,
                CreatedAtUtc = DateTime.UtcNow
            })
            .ToList();
    }

    [Benchmark(Description = "ADO.NET BulkInsert")]
    public async Task AdoNet_BulkInsert()
    {
        await _adoBulkOps.BulkInsertAsync(_sqliteEntities);
    }

    [Benchmark(Description = "Dapper BulkInsert")]
    public async Task Dapper_BulkInsert()
    {
        await _dapperBulkOps.BulkInsertAsync(_sqliteEntities);
    }

    [Benchmark(Baseline = true, Description = "EF Core BulkInsert")]
    public async Task EfCore_BulkInsert()
    {
        await _efBulkOps.BulkInsertAsync(_efEntities);
    }

    private static void CreateTable(SqliteConnection connection)
    {
        using var cmd = connection.CreateCommand();
        cmd.CommandText = """
            CREATE TABLE IF NOT EXISTS BenchmarkEntities (
                Id TEXT PRIMARY KEY,
                Name TEXT NOT NULL,
                Amount REAL NOT NULL,
                IsActive INTEGER NOT NULL,
                CreatedAtUtc TEXT NOT NULL
            )
            """;
        cmd.ExecuteNonQuery();
    }
}

/// <summary>
/// Benchmarks comparing BulkUpdateAsync across ADO.NET, Dapper, and EF Core providers.
/// </summary>
[MemoryDiagnoser]
[RankColumn]
#pragma warning disable CA1001 // BenchmarkDotNet handles disposal via GlobalCleanup
public class SqliteBulkUpdateComparisonBenchmarks
#pragma warning restore CA1001
{
    private SqliteConnection _connection = null!;
    private BulkBenchmarkDbContext _dbContext = null!;
    private BulkOperationsSqlite<SqliteBenchmarkEntity, Guid> _adoBulkOps = null!;
    private BulkOperationsDapper<SqliteBenchmarkEntity, Guid> _dapperBulkOps = null!;
    private BulkOperationsEF<BenchmarkEntity> _efBulkOps = null!;
    private List<SqliteBenchmarkEntity> _sqliteEntities = null!;
    private List<BenchmarkEntity> _efEntities = null!;

    /// <summary>
    /// Number of entities to update in the benchmark.
    /// </summary>
    [Params(100, 500, 1000)]
    public int EntityCount { get; set; }

    [GlobalSetup]
    public void GlobalSetup()
    {
        // Setup SQLite connection for ADO.NET and Dapper
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();
        CreateTable(_connection);

        var adoMapping = new SqliteBenchmarkEntityMapping();
        var dapperMapping = new SqliteBenchmarkEntityDapperMapping();

        _adoBulkOps = new BulkOperationsSqlite<SqliteBenchmarkEntity, Guid>(_connection, adoMapping);
        _dapperBulkOps = new BulkOperationsDapper<SqliteBenchmarkEntity, Guid>(_connection, dapperMapping);

        // Setup EF Core DbContext
        var options = new DbContextOptionsBuilder<BulkBenchmarkDbContext>()
            .UseSqlite("Data Source=:memory:")
            .Options;

        _dbContext = new BulkBenchmarkDbContext(options);
        _dbContext.Database.OpenConnection();
        _dbContext.Database.EnsureCreated();

        _efBulkOps = new BulkOperationsEF<BenchmarkEntity>(_dbContext);
    }

    [GlobalCleanup]
    public void GlobalCleanup()
    {
        _connection?.Dispose();
        _dbContext?.Dispose();
    }

    [IterationSetup]
    public void IterationSetup()
    {
        // Clear and seed data for ADO.NET/Dapper
        using var clearCmd = _connection.CreateCommand();
        clearCmd.CommandText = "DELETE FROM BenchmarkEntities";
        clearCmd.ExecuteNonQuery();

        _sqliteEntities = Enumerable.Range(0, EntityCount)
            .Select(i => new SqliteBenchmarkEntity
            {
                Id = Guid.NewGuid(),
                Name = $"Entity_{i}",
                Amount = i * 10.5m,
                IsActive = i % 2 == 0,
                CreatedAtUtc = DateTime.UtcNow
            })
            .ToList();

        // Insert initial data
        foreach (var entity in _sqliteEntities)
        {
            using var insertCmd = _connection.CreateCommand();
            insertCmd.CommandText = """
                INSERT INTO BenchmarkEntities (Id, Name, Amount, IsActive, CreatedAtUtc)
                VALUES (@Id, @Name, @Amount, @IsActive, @CreatedAtUtc)
                """;
            insertCmd.Parameters.AddWithValue("@Id", entity.Id.ToString());
            insertCmd.Parameters.AddWithValue("@Name", entity.Name);
            insertCmd.Parameters.AddWithValue("@Amount", entity.Amount);
            insertCmd.Parameters.AddWithValue("@IsActive", entity.IsActive ? 1 : 0);
            insertCmd.Parameters.AddWithValue("@CreatedAtUtc", entity.CreatedAtUtc.ToString("o"));
            insertCmd.ExecuteNonQuery();
        }

        // Seed EF Core data
        _dbContext.BenchmarkEntities.ExecuteDelete();

        _efEntities = Enumerable.Range(0, EntityCount)
            .Select(i => new BenchmarkEntity
            {
                Id = Guid.NewGuid(),
                Name = $"Entity_{i}",
                Amount = i * 10.5m,
                IsActive = i % 2 == 0,
                CreatedAtUtc = DateTime.UtcNow
            })
            .ToList();

        _dbContext.BenchmarkEntities.AddRange(_efEntities);
        _dbContext.SaveChanges();
        _dbContext.ChangeTracker.Clear();

        // Modify entities for update
        foreach (var entity in _sqliteEntities)
        {
            entity.Name = $"Updated_{entity.Name}";
            entity.Amount *= 2;
        }

        foreach (var entity in _efEntities)
        {
            entity.Name = $"Updated_{entity.Name}";
            entity.Amount *= 2;
        }
    }

    [Benchmark(Description = "ADO.NET BulkUpdate")]
    public async Task AdoNet_BulkUpdate()
    {
        await _adoBulkOps.BulkUpdateAsync(_sqliteEntities);
    }

    [Benchmark(Description = "Dapper BulkUpdate")]
    public async Task Dapper_BulkUpdate()
    {
        await _dapperBulkOps.BulkUpdateAsync(_sqliteEntities);
    }

    [Benchmark(Baseline = true, Description = "EF Core BulkUpdate")]
    public async Task EfCore_BulkUpdate()
    {
        await _efBulkOps.BulkUpdateAsync(_efEntities);
    }

    private static void CreateTable(SqliteConnection connection)
    {
        using var cmd = connection.CreateCommand();
        cmd.CommandText = """
            CREATE TABLE IF NOT EXISTS BenchmarkEntities (
                Id TEXT PRIMARY KEY,
                Name TEXT NOT NULL,
                Amount REAL NOT NULL,
                IsActive INTEGER NOT NULL,
                CreatedAtUtc TEXT NOT NULL
            )
            """;
        cmd.ExecuteNonQuery();
    }
}

/// <summary>
/// Benchmarks comparing BulkDeleteAsync across ADO.NET, Dapper, and EF Core providers.
/// </summary>
[MemoryDiagnoser]
[RankColumn]
#pragma warning disable CA1001 // BenchmarkDotNet handles disposal via GlobalCleanup
public class SqliteBulkDeleteComparisonBenchmarks
#pragma warning restore CA1001
{
    private SqliteConnection _connection = null!;
    private BulkBenchmarkDbContext _dbContext = null!;
    private BulkOperationsSqlite<SqliteBenchmarkEntity, Guid> _adoBulkOps = null!;
    private BulkOperationsDapper<SqliteBenchmarkEntity, Guid> _dapperBulkOps = null!;
    private BulkOperationsEF<BenchmarkEntity> _efBulkOps = null!;
    private List<SqliteBenchmarkEntity> _sqliteEntities = null!;
    private List<BenchmarkEntity> _efEntities = null!;

    /// <summary>
    /// Number of entities to delete in the benchmark.
    /// </summary>
    [Params(100, 500, 1000)]
    public int EntityCount { get; set; }

    [GlobalSetup]
    public void GlobalSetup()
    {
        // Setup SQLite connection for ADO.NET and Dapper
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();
        CreateTable(_connection);

        var adoMapping = new SqliteBenchmarkEntityMapping();
        var dapperMapping = new SqliteBenchmarkEntityDapperMapping();

        _adoBulkOps = new BulkOperationsSqlite<SqliteBenchmarkEntity, Guid>(_connection, adoMapping);
        _dapperBulkOps = new BulkOperationsDapper<SqliteBenchmarkEntity, Guid>(_connection, dapperMapping);

        // Setup EF Core DbContext
        var options = new DbContextOptionsBuilder<BulkBenchmarkDbContext>()
            .UseSqlite("Data Source=:memory:")
            .Options;

        _dbContext = new BulkBenchmarkDbContext(options);
        _dbContext.Database.OpenConnection();
        _dbContext.Database.EnsureCreated();

        _efBulkOps = new BulkOperationsEF<BenchmarkEntity>(_dbContext);
    }

    [GlobalCleanup]
    public void GlobalCleanup()
    {
        _connection?.Dispose();
        _dbContext?.Dispose();
    }

    [IterationSetup]
    public void IterationSetup()
    {
        // Clear and seed data for ADO.NET/Dapper
        using var clearCmd = _connection.CreateCommand();
        clearCmd.CommandText = "DELETE FROM BenchmarkEntities";
        clearCmd.ExecuteNonQuery();

        _sqliteEntities = Enumerable.Range(0, EntityCount)
            .Select(i => new SqliteBenchmarkEntity
            {
                Id = Guid.NewGuid(),
                Name = $"Entity_{i}",
                Amount = i * 10.5m,
                IsActive = i % 2 == 0,
                CreatedAtUtc = DateTime.UtcNow
            })
            .ToList();

        // Insert initial data
        foreach (var entity in _sqliteEntities)
        {
            using var insertCmd = _connection.CreateCommand();
            insertCmd.CommandText = """
                INSERT INTO BenchmarkEntities (Id, Name, Amount, IsActive, CreatedAtUtc)
                VALUES (@Id, @Name, @Amount, @IsActive, @CreatedAtUtc)
                """;
            insertCmd.Parameters.AddWithValue("@Id", entity.Id.ToString());
            insertCmd.Parameters.AddWithValue("@Name", entity.Name);
            insertCmd.Parameters.AddWithValue("@Amount", entity.Amount);
            insertCmd.Parameters.AddWithValue("@IsActive", entity.IsActive ? 1 : 0);
            insertCmd.Parameters.AddWithValue("@CreatedAtUtc", entity.CreatedAtUtc.ToString("o"));
            insertCmd.ExecuteNonQuery();
        }

        // Seed EF Core data
        _dbContext.BenchmarkEntities.ExecuteDelete();

        _efEntities = Enumerable.Range(0, EntityCount)
            .Select(i => new BenchmarkEntity
            {
                Id = Guid.NewGuid(),
                Name = $"Entity_{i}",
                Amount = i * 10.5m,
                IsActive = i % 2 == 0,
                CreatedAtUtc = DateTime.UtcNow
            })
            .ToList();

        _dbContext.BenchmarkEntities.AddRange(_efEntities);
        _dbContext.SaveChanges();
        _dbContext.ChangeTracker.Clear();
    }

    [Benchmark(Description = "ADO.NET BulkDelete")]
    public async Task AdoNet_BulkDelete()
    {
        await _adoBulkOps.BulkDeleteAsync(_sqliteEntities);
    }

    [Benchmark(Description = "Dapper BulkDelete")]
    public async Task Dapper_BulkDelete()
    {
        await _dapperBulkOps.BulkDeleteAsync(_sqliteEntities);
    }

    [Benchmark(Baseline = true, Description = "EF Core BulkDelete")]
    public async Task EfCore_BulkDelete()
    {
        await _efBulkOps.BulkDeleteAsync(_efEntities);
    }

    private static void CreateTable(SqliteConnection connection)
    {
        using var cmd = connection.CreateCommand();
        cmd.CommandText = """
            CREATE TABLE IF NOT EXISTS BenchmarkEntities (
                Id TEXT PRIMARY KEY,
                Name TEXT NOT NULL,
                Amount REAL NOT NULL,
                IsActive INTEGER NOT NULL,
                CreatedAtUtc TEXT NOT NULL
            )
            """;
        cmd.ExecuteNonQuery();
    }
}

/// <summary>
/// Entity used for SQLite bulk operation benchmarks (ADO.NET and Dapper).
/// </summary>
public sealed class SqliteBenchmarkEntity
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}

/// <summary>
/// Entity mapping for ADO.NET SQLite bulk operations.
/// </summary>
public sealed class SqliteBenchmarkEntityMapping : AdoMapping.IEntityMapping<SqliteBenchmarkEntity, Guid>
{
    public string TableName => "BenchmarkEntities";
    public string IdColumnName => "Id";

    public IReadOnlyDictionary<string, string> ColumnMappings { get; } = new Dictionary<string, string>
    {
        [nameof(SqliteBenchmarkEntity.Id)] = "Id",
        [nameof(SqliteBenchmarkEntity.Name)] = "Name",
        [nameof(SqliteBenchmarkEntity.Amount)] = "Amount",
        [nameof(SqliteBenchmarkEntity.IsActive)] = "IsActive",
        [nameof(SqliteBenchmarkEntity.CreatedAtUtc)] = "CreatedAtUtc"
    };

    public Guid GetId(SqliteBenchmarkEntity entity) => entity.Id;
    public IReadOnlySet<string> InsertExcludedProperties { get; } = new HashSet<string>();
    public IReadOnlySet<string> UpdateExcludedProperties { get; } = new HashSet<string> { nameof(SqliteBenchmarkEntity.Id) };
}

/// <summary>
/// Entity mapping for Dapper SQLite bulk operations.
/// </summary>
public sealed class SqliteBenchmarkEntityDapperMapping : DapperMapping.IEntityMapping<SqliteBenchmarkEntity, Guid>
{
    public string TableName => "BenchmarkEntities";
    public string IdColumnName => "Id";

    public IReadOnlyDictionary<string, string> ColumnMappings { get; } = new Dictionary<string, string>
    {
        [nameof(SqliteBenchmarkEntity.Id)] = "Id",
        [nameof(SqliteBenchmarkEntity.Name)] = "Name",
        [nameof(SqliteBenchmarkEntity.Amount)] = "Amount",
        [nameof(SqliteBenchmarkEntity.IsActive)] = "IsActive",
        [nameof(SqliteBenchmarkEntity.CreatedAtUtc)] = "CreatedAtUtc"
    };

    public Guid GetId(SqliteBenchmarkEntity entity) => entity.Id;
    public IReadOnlySet<string> InsertExcludedProperties { get; } = new HashSet<string>();
    public IReadOnlySet<string> UpdateExcludedProperties { get; } = new HashSet<string> { nameof(SqliteBenchmarkEntity.Id) };
}
