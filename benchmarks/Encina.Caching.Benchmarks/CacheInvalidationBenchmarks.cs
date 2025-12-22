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
/// Benchmarks for CacheInvalidationPipelineBehavior with real caching.
/// </summary>
[MemoryDiagnoser]
[SimpleJob(warmupCount: 3, iterationCount: 10)]
public class CacheInvalidationBenchmarks : IDisposable
{
    private CacheInvalidationPipelineBehavior<InvalidatingCommand, InvalidationResult> _behavior = null!;
    private MsMemoryCache _memoryCache = null!;
    private MemoryCacheProvider _cacheProvider = null!;
    private bool _disposed;
    private IRequestContext _context = null!;
    private InvalidatingCommand _command = null!;
    private RequestHandlerCallback<InvalidationResult> _handler = null!;

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
            EnableCacheInvalidation = true
        });
        _behavior = new CacheInvalidationPipelineBehavior<InvalidatingCommand, InvalidationResult>(
            _cacheProvider,
            keyGenerator,
            behaviorOptions,
            NullLogger<CacheInvalidationPipelineBehavior<InvalidatingCommand, InvalidationResult>>.Instance);

        _context = new BenchmarkRequestContext();
        _command = new InvalidatingCommand(Guid.NewGuid(), "Update");

        _handler = () => ValueTask.FromResult(Either<EncinaError, InvalidationResult>.Right(new InvalidationResult(true)));
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
    public async Task<Either<EncinaError, InvalidationResult>> Invalidation_NoMatchingKeys()
    {
        return await _behavior.Handle(_command, _context, _handler, CancellationToken.None);
    }

    [Benchmark]
    public async Task<Either<EncinaError, InvalidationResult>> Invalidation_WithMatchingKeys()
    {
        // Pre-populate cache with keys that match the invalidation pattern
        var productId = _command.ProductId;
        await _cacheProvider.SetAsync($"bench:product:{productId}:details", "details", TimeSpan.FromMinutes(5), CancellationToken.None);
        await _cacheProvider.SetAsync($"bench:product:{productId}:pricing", "pricing", TimeSpan.FromMinutes(5), CancellationToken.None);
        await _cacheProvider.SetAsync($"bench:product:{productId}:inventory", "inventory", TimeSpan.FromMinutes(5), CancellationToken.None);

        return await _behavior.Handle(_command, _context, _handler, CancellationToken.None);
    }

    [Benchmark]
    [Arguments(5)]
    [Arguments(10)]
    [Arguments(25)]
    public async Task Invalidation_MultipleKeys(int keyCount)
    {
        var productId = Guid.NewGuid();
        var command = new InvalidatingCommand(productId, "MultiInvalidate");

        // Pre-populate cache
        for (var i = 0; i < keyCount; i++)
        {
            await _cacheProvider.SetAsync(
                $"bench:product:{productId}:data-{i}",
                $"value-{i}",
                TimeSpan.FromMinutes(5),
                CancellationToken.None);
        }

        await _behavior.Handle(command, _context, _handler, CancellationToken.None);
    }

    [Benchmark]
    public async Task Invalidation_SequentialCommands()
    {
        for (var i = 0; i < 10; i++)
        {
            var command = new InvalidatingCommand(Guid.NewGuid(), $"Command-{i}");
            await _behavior.Handle(command, _context, _handler, CancellationToken.None);
        }
    }

    [Benchmark]
    [Arguments(10)]
    [Arguments(50)]
    public async Task Invalidation_ConcurrentCommands(int concurrencyLevel)
    {
        var tasks = new Task<Either<EncinaError, InvalidationResult>>[concurrencyLevel];

        for (var i = 0; i < concurrencyLevel; i++)
        {
            var command = new InvalidatingCommand(Guid.NewGuid(), $"Concurrent-{i}");
            tasks[i] = _behavior.Handle(command, _context, _handler, CancellationToken.None).AsTask();
        }

        await Task.WhenAll(tasks);
    }
}

/// <summary>
/// A command that triggers cache invalidation.
/// </summary>
[InvalidatesCache(KeyPattern = "product:{ProductId}:*")]
public sealed record InvalidatingCommand(Guid ProductId, string Action) : IRequest<InvalidationResult>;

/// <summary>
/// Result of an invalidation command.
/// </summary>
public sealed record InvalidationResult(bool Success);
