using BenchmarkDotNet.Attributes;
using Encina;
using Encina.Caching;
using Encina.Caching.Memory;
using LanguageExt;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using MsMemoryCache = Microsoft.Extensions.Caching.Memory.MemoryCache;
using MsMemoryCacheOptions = Microsoft.Extensions.Caching.Memory.MemoryCacheOptions;

namespace Encina.Caching.Benchmarks;

/// <summary>
/// Benchmarks for QueryCachingPipelineBehavior with real caching.
/// </summary>
[MemoryDiagnoser]
[SimpleJob(warmupCount: 3, iterationCount: 10)]
public class QueryCachingPipelineBenchmarks : IDisposable
{
    private QueryCachingPipelineBehavior<CachableQuery, QueryResult> _behavior = null!;
    private MsMemoryCache _memoryCache = null!;
    private MemoryCacheProvider _cacheProvider = null!;
    private bool _disposed;
    private IRequestContext _context = null!;
    private CachableQuery _query = null!;
    private string _cachedKey = null!;
    private RequestHandlerCallback<QueryResult> _handler = null!;

    [GlobalSetup]
    public void Setup()
    {
        var memoryCacheOptions = Options.Create(new MsMemoryCacheOptions());
        _memoryCache = new MsMemoryCache(memoryCacheOptions);

        var cacheOptions = Options.Create(new MemoryCacheOptions
        {
            DefaultExpiration = TimeSpan.FromMinutes(5)
        });
        _cacheProvider = new MemoryCacheProvider(_memoryCache, cacheOptions, NullLogger<MemoryCacheProvider>.Instance);

        var keyGeneratorOptions = Options.Create(new CachingOptions
        {
            EnableQueryCaching = true,
            KeyPrefix = "bench"
        });
        var keyGenerator = new DefaultCacheKeyGenerator(keyGeneratorOptions);

        var behaviorOptions = Options.Create(new CachingOptions
        {
            EnableQueryCaching = true
        });
        _behavior = new QueryCachingPipelineBehavior<CachableQuery, QueryResult>(
            _cacheProvider,
            keyGenerator,
            behaviorOptions,
            NullLogger<QueryCachingPipelineBehavior<CachableQuery, QueryResult>>.Instance);

        _context = new BenchmarkRequestContext();
        _query = new CachableQuery(Guid.NewGuid(), "Test Query");
        _cachedKey = keyGenerator.GenerateKey<CachableQuery, QueryResult>(_query, _context);

        // Set up the handler
        _handler = () => ValueTask.FromResult(Either<MediatorError, QueryResult>.Right(new QueryResult(_query.Id, "Result", 42)));
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        Dispose();
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _memoryCache?.Dispose();
            _disposed = true;
        }

        GC.SuppressFinalize(this);
    }

    [Benchmark(Baseline = true)]
    public async Task<Either<MediatorError, QueryResult>> Pipeline_CacheMiss()
    {
        // Create unique query to ensure cache miss
        var query = new CachableQuery(Guid.NewGuid(), "New Query");
        return await _behavior.Handle(query, _context, _handler, CancellationToken.None);
    }

    [Benchmark]
    public async Task<Either<MediatorError, QueryResult>> Pipeline_CacheHit()
    {
        // First call to populate cache
        await _behavior.Handle(_query, _context, _handler, CancellationToken.None);

        // Second call should be cache hit
        return await _behavior.Handle(_query, _context, _handler, CancellationToken.None);
    }

    [Benchmark]
    [Arguments(10)]
    [Arguments(50)]
    [Arguments(100)]
    public async Task Pipeline_ConcurrentAccess(int concurrencyLevel)
    {
        var tasks = new Task<Either<MediatorError, QueryResult>>[concurrencyLevel];

        // Half cache hits, half cache misses
        for (var i = 0; i < concurrencyLevel; i++)
        {
            var query = i % 2 == 0 ? _query : new CachableQuery(Guid.NewGuid(), $"Query-{i}");
            tasks[i] = _behavior.Handle(query, _context, _handler, CancellationToken.None).AsTask();
        }

        await Task.WhenAll(tasks);
    }

    [Benchmark]
    public async Task Pipeline_SequentialDifferentQueries()
    {
        for (var i = 0; i < 10; i++)
        {
            var query = new CachableQuery(Guid.NewGuid(), $"Query-{i}");
            await _behavior.Handle(query, _context, _handler, CancellationToken.None);
        }
    }

    [Benchmark]
    public async Task Pipeline_SequentialSameQuery()
    {
        for (var i = 0; i < 10; i++)
        {
            await _behavior.Handle(_query, _context, _handler, CancellationToken.None);
        }
    }
}

/// <summary>
/// A cacheable query for benchmarking.
/// </summary>
[Cache(DurationSeconds = 300)]
public sealed record CachableQuery(Guid Id, string Name) : IRequest<QueryResult>;

/// <summary>
/// Query result for benchmarking.
/// </summary>
public sealed record QueryResult(Guid Id, string Name, int Value);
