using System.Collections;
using System.Data;
using System.Data.Common;
using BenchmarkDotNet.Attributes;
using Encina.Caching;
using Encina.Caching.Memory;
using Encina.EntityFrameworkCore.Caching;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
using EncinaMemoryCacheOptions = Encina.Caching.Memory.MemoryCacheOptions;
using MsMemoryCache = Microsoft.Extensions.Caching.Memory.MemoryCache;
using MsMemoryCacheOptions = Microsoft.Extensions.Caching.Memory.MemoryCacheOptions;

namespace Encina.Benchmarks.EntityFrameworkCore;

/// <summary>
/// Benchmarks for the EF Core query caching interceptor components.
/// Measures key generation overhead, cache lookup cost, and CachedDataReader performance.
/// Target: interceptor overhead should be &lt;1ms for cache hits.
/// </summary>
[MemoryDiagnoser]
[SimpleJob(warmupCount: 3, iterationCount: 10)]
public class QueryCacheInterceptorBenchmarks : IDisposable
{
    private DefaultQueryCacheKeyGenerator _keyGenerator = null!;
    private MemoryCacheProvider _cacheProvider = null!;
    private MsMemoryCache _memoryCache = null!;
    private DbCommand _simpleCommand = null!;
    private DbCommand _complexCommand = null!;
    private DbContext _mockContext = null!;
    private CachedQueryResult _smallResult = null!;
    private CachedQueryResult _largeResult = null!;
    private bool _disposed;

    [GlobalSetup]
    public void Setup()
    {
        // Cache provider setup
        var memoryCacheOptions = Options.Create(new MsMemoryCacheOptions());
        _memoryCache = new MsMemoryCache(memoryCacheOptions);
        var cacheOptions = Options.Create(new EncinaMemoryCacheOptions
        {
            DefaultExpiration = TimeSpan.FromMinutes(5)
        });
        _cacheProvider = new MemoryCacheProvider(
            _memoryCache, cacheOptions, NullLogger<MemoryCacheProvider>.Instance);

        // Key generator setup
        var queryOptions = Options.Create(new QueryCacheOptions { KeyPrefix = "bench" });
        _keyGenerator = new DefaultQueryCacheKeyGenerator(queryOptions);

        // Mock DbContext with empty model (no entity type resolution)
        _mockContext = Substitute.For<DbContext>();
        var model = Substitute.For<Microsoft.EntityFrameworkCore.Metadata.IModel>();
        model.GetEntityTypes()
            .Returns(Array.Empty<Microsoft.EntityFrameworkCore.Metadata.IEntityType>());
        _mockContext.Model.Returns(model);

        // Simple command (single table, one parameter)
        _simpleCommand = CreateCommand(
            "SELECT [o].[Id], [o].[Name], [o].[Price] FROM [Orders] AS [o] WHERE [o].[Id] = @p0",
            ("@p0", 42));

        // Complex command (multiple tables, multiple parameters)
        _complexCommand = CreateCommand(
            "SELECT [o].[Id], [o].[Name], [c].[CategoryName] " +
            "FROM [Orders] AS [o] " +
            "INNER JOIN [Customers] AS [c] ON [o].[CustomerId] = [c].[Id] " +
            "LEFT JOIN [OrderItems] AS [oi] ON [o].[Id] = [oi].[OrderId] " +
            "WHERE [o].[Status] = @p0 AND [o].[CreatedAt] > @p1 AND [c].[Region] = @p2",
            ("@p0", "Active"), ("@p1", DateTime.UtcNow.AddDays(-30)), ("@p2", "US"));

        // Small cached result (5 rows, 3 columns)
        _smallResult = CreateCachedResult(5, 3);

        // Large cached result (1000 rows, 10 columns)
        _largeResult = CreateCachedResult(1000, 10);

        // Pre-populate cache for cache-hit benchmarks
        var key = _keyGenerator.Generate(_simpleCommand, _mockContext);
        _cacheProvider.SetAsync(key.Key, _smallResult, TimeSpan.FromMinutes(5), CancellationToken.None)
            .GetAwaiter().GetResult();
    }

    [GlobalCleanup]
    public void Cleanup() => Dispose();

    public void Dispose()
    {
        if (!_disposed)
        {
            _memoryCache?.Dispose();
            _disposed = true;
        }

        GC.SuppressFinalize(this);
    }

    #region Key Generation Benchmarks

    [Benchmark(Baseline = true, Description = "Key generation (simple query)")]
    public QueryCacheKey KeyGeneration_SimpleQuery()
    {
        return _keyGenerator.Generate(_simpleCommand, _mockContext);
    }

    [Benchmark(Description = "Key generation (complex JOIN query)")]
    public QueryCacheKey KeyGeneration_ComplexQuery()
    {
        return _keyGenerator.Generate(_complexCommand, _mockContext);
    }

    [Benchmark(Description = "Key generation with tenant")]
    public QueryCacheKey KeyGeneration_WithTenant()
    {
        var rc = Substitute.For<IRequestContext>();
        rc.TenantId.Returns("tenant-001");
        return _keyGenerator.Generate(_simpleCommand, _mockContext, rc);
    }

    #endregion

    #region Cache Lookup Benchmarks

    [Benchmark(Description = "Cache hit (memory)")]
    public async Task<CachedQueryResult?> CacheLookup_Hit()
    {
        var key = _keyGenerator.Generate(_simpleCommand, _mockContext);
        return await _cacheProvider.GetAsync<CachedQueryResult>(key.Key, CancellationToken.None);
    }

    [Benchmark(Description = "Cache miss (memory)")]
    public async Task<CachedQueryResult?> CacheLookup_Miss()
    {
        return await _cacheProvider.GetAsync<CachedQueryResult>(
            $"bench:missing:{Guid.NewGuid():N}", CancellationToken.None);
    }

    #endregion

    #region CachedDataReader Benchmarks

    [Benchmark(Description = "CachedDataReader read (5 rows)")]
    public int CachedDataReader_SmallResult()
    {
        using var reader = new CachedDataReader(_smallResult);
        var count = 0;
        while (reader.Read())
        {
            _ = reader.GetValue(0);
            _ = reader.GetValue(1);
            _ = reader.GetValue(2);
            count++;
        }

        return count;
    }

    [Benchmark(Description = "CachedDataReader read (1000 rows)")]
    public int CachedDataReader_LargeResult()
    {
        using var reader = new CachedDataReader(_largeResult);
        var count = 0;
        while (reader.Read())
        {
            _ = reader.GetValue(0);
            count++;
        }

        return count;
    }

    #endregion

    #region Helpers

    private static DbCommand CreateCommand(string sql, params (string Name, object Value)[] parameters)
    {
        var command = Substitute.For<DbCommand>();
        command.CommandText.Returns(sql);

        var paramCollection = new BenchmarkDbParameterCollection();
        foreach (var (name, value) in parameters)
        {
            var param = Substitute.For<DbParameter>();
            param.ParameterName.Returns(name);
            param.Value.Returns(value);
            paramCollection.Add(param);
        }

        command.Parameters.Returns(paramCollection);
        return command;
    }

    private static CachedQueryResult CreateCachedResult(int rowCount, int columnCount)
    {
        var columns = Enumerable.Range(0, columnCount)
            .Select(i => new CachedColumnSchema(
                $"Col{i}", i, "nvarchar", typeof(string).AssemblyQualifiedName!, false))
            .ToList();

        var rows = Enumerable.Range(0, rowCount)
            .Select(r => Enumerable.Range(0, columnCount)
                .Select(c => (object?)$"Value_{r}_{c}")
                .ToArray())
            .ToList();

        return new CachedQueryResult
        {
            Columns = columns,
            Rows = rows,
            CachedAtUtc = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Minimal DbParameterCollection for benchmarking without full mocking overhead.
    /// </summary>
    private sealed class BenchmarkDbParameterCollection : DbParameterCollection
    {
        private readonly List<DbParameter> _parameters = [];

        public override int Count => _parameters.Count;
        public override object SyncRoot => ((ICollection)_parameters).SyncRoot;

        public override int Add(object value)
        {
            _parameters.Add((DbParameter)value);
            return _parameters.Count - 1;
        }

        public override void Clear() => _parameters.Clear();
        public override bool Contains(object value) => _parameters.Contains((DbParameter)value);
        public override bool Contains(string value) => _parameters.Any(p => p.ParameterName == value);
        public override int IndexOf(object value) => _parameters.IndexOf((DbParameter)value);
        public override int IndexOf(string parameterName) => _parameters.FindIndex(p => p.ParameterName == parameterName);
        public override void Insert(int index, object value) => _parameters.Insert(index, (DbParameter)value);
        public override void Remove(object value) => _parameters.Remove((DbParameter)value);
        public override void RemoveAt(int index) => _parameters.RemoveAt(index);
        public override void RemoveAt(string parameterName) => _parameters.RemoveAll(p => p.ParameterName == parameterName);
        public override void AddRange(Array values) { foreach (DbParameter v in values) _parameters.Add(v); }
        public override void CopyTo(Array array, int index) => ((ICollection)_parameters).CopyTo(array, index);
        public override IEnumerator GetEnumerator() => _parameters.GetEnumerator();
        protected override DbParameter GetParameter(int index) => _parameters[index];
        protected override DbParameter GetParameter(string parameterName) => _parameters.First(p => p.ParameterName == parameterName);
        protected override void SetParameter(int index, DbParameter value) => _parameters[index] = value;
        protected override void SetParameter(string parameterName, DbParameter value)
        {
            var idx = IndexOf(parameterName);
            if (idx >= 0) _parameters[idx] = value;
        }
    }

    #endregion
}
