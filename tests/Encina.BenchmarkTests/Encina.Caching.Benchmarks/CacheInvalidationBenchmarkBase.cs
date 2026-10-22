using BenchmarkDotNet.Attributes;
using Encina.Caching.Memory;
using LanguageExt;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using MsMemoryCache = Microsoft.Extensions.Caching.Memory.MemoryCache;
using MsMemoryCacheOptions = Microsoft.Extensions.Caching.Memory.MemoryCacheOptions;

namespace Encina.Caching.Benchmarks;

/// <summary>
/// Shared setup and fields for the <c>CacheInvalidationPipelineBehavior</c> benchmarks with real caching.
/// </summary>
/// <remarks>
/// The invalidation benchmarks are split into several classes so that each one stays well under the
/// 60-minute job limit of the scheduled Benchmarks matrix (#2554); each concrete class becomes its
/// own matrix job. This base carries no benchmark attributes of its own, so the class discovery
/// and BenchmarkDotNet do not treat it as a benchmark class.
/// </remarks>
public abstract class CacheInvalidationBenchmarkBase : IDisposable
{
    private MsMemoryCache _memoryCache = null!;
    private bool _disposed;

    protected CacheInvalidationPipelineBehavior<InvalidatingCommand, InvalidationResult> Behavior { get; private set; } = null!;
    protected MemoryCacheProvider CacheProvider { get; private set; } = null!;
    protected IRequestContext Context { get; private set; } = null!;
    protected InvalidatingCommand Command { get; private set; } = null!;
    protected RequestHandlerCallback<InvalidationResult> Handler { get; private set; } = null!;

    [GlobalSetup]
    public void Setup()
    {
        var memoryCacheOptions = Options.Create(new MsMemoryCacheOptions());
        _memoryCache = new MsMemoryCache(memoryCacheOptions);

        var cacheOptions = Options.Create(new MemoryCacheOptions
        {
            DefaultExpiration = TimeSpan.FromMinutes(5)
        });
        CacheProvider = new MemoryCacheProvider(_memoryCache, cacheOptions, NullLogger<MemoryCacheProvider>.Instance);

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
        Behavior = new CacheInvalidationPipelineBehavior<InvalidatingCommand, InvalidationResult>(
            CacheProvider,
            keyGenerator,
            behaviorOptions,
            NullLogger<CacheInvalidationPipelineBehavior<InvalidatingCommand, InvalidationResult>>.Instance);

        Context = new BenchmarkRequestContext();
        Command = new InvalidatingCommand(Guid.NewGuid(), "Update");

        Handler = () => ValueTask.FromResult(Either<EncinaError, InvalidationResult>.Right(new InvalidationResult(true)));
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
