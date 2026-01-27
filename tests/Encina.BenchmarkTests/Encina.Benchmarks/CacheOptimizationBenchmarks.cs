using System.Collections.Concurrent;
using BenchmarkDotNet.Attributes;
using LanguageExt;
using Microsoft.Extensions.DependencyInjection;
using static LanguageExt.Prelude;

namespace Encina.Benchmarks;

/// <summary>
/// Benchmarks to measure the performance impact of delegate cache optimizations.
/// These benchmarks validate the TryGetValue-before-GetOrAdd pattern and type check caching.
/// </summary>
[MemoryDiagnoser]
public class CacheOptimizationBenchmarks
{
    private IServiceProvider _provider = default!;
    private IEncina _encina = default!;
    private SampleCommand _command = default!;
    private SampleQuery _query = default!;
    private SampleNotification _notification = default!;

    // For raw cache access benchmarks
    private ConcurrentDictionary<(Type, Type), object> _cache = default!;
    private (Type, Type) _cacheKey;
    private static readonly Func<(Type, Type), object> Factory = static _ => new object();

    [GlobalSetup]
    public void Setup()
    {
        var services = new ServiceCollection();
        // Don't scan the assembly as it would pick up processors from other benchmarks
        // that have dependencies we haven't registered (e.g., EncinaBenchmarks.CallRecorder)
        services.AddEncina();
        services.AddScoped<IRequestHandler<SampleCommand, int>, SampleCommandHandler>();
        services.AddScoped<IRequestHandler<SampleQuery, string>, SampleQueryHandler>();
        services.AddScoped<INotificationHandler<SampleNotification>, SampleNotificationHandler>();

        _provider = services.BuildServiceProvider();
        _encina = _provider.GetRequiredService<IEncina>();
        _command = new SampleCommand(Guid.NewGuid());
        _query = new SampleQuery("test");
        _notification = new SampleNotification(42);

        // Pre-warm caches
        _encina.Send(_command).AsTask().GetAwaiter().GetResult();
        _encina.Send(_query).AsTask().GetAwaiter().GetResult();
        _encina.Publish(_notification).AsTask().GetAwaiter().GetResult();

        // Setup for raw cache benchmarks
        _cache = new ConcurrentDictionary<(Type, Type), object>();
        _cacheKey = (typeof(SampleCommand), typeof(int));
        _cache[_cacheKey] = new object();
    }

    /// <summary>
    /// Baseline: Direct ConcurrentDictionary.GetOrAdd (always allocates delegate)
    /// </summary>
    [Benchmark(Baseline = true)]
    public object Cache_GetOrAdd_Direct()
    {
        return _cache.GetOrAdd(_cacheKey, Factory);
    }

    /// <summary>
    /// Optimized: TryGetValue first, then GetOrAdd only on miss
    /// </summary>
    [Benchmark]
    public object Cache_TryGetValue_ThenGetOrAdd()
    {
        if (_cache.TryGetValue(_cacheKey, out var value))
        {
            return value;
        }

        return _cache.GetOrAdd(_cacheKey, Factory);
    }

    /// <summary>
    /// Send command through full pipeline (tests handler cache optimization)
    /// </summary>
    [Benchmark]
    public async Task<int> Send_Command_CacheHit()
    {
        var result = await _encina.Send(_command).ConfigureAwait(false);
        return result.Match(
            Left: _ => -1,
            Right: v => v);
    }

    /// <summary>
    /// Send query through full pipeline (tests handler cache optimization)
    /// </summary>
    [Benchmark]
    public async Task<string> Send_Query_CacheHit()
    {
        var result = await _encina.Send(_query).ConfigureAwait(false);
        return result.Match(
            Left: _ => string.Empty,
            Right: v => v);
    }

    /// <summary>
    /// Publish notification (tests notification invoker cache optimization)
    /// </summary>
    [Benchmark]
    public async Task Publish_Notification_CacheHit()
    {
        await _encina.Publish(_notification).ConfigureAwait(false);
    }

    /// <summary>
    /// Type check with caching (simulates GetRequestKind optimization)
    /// </summary>
    [Benchmark]
#pragma warning disable CA1822 // Mark members as static - BenchmarkDotNet requires instance methods
    public string TypeCheck_Cached()
#pragma warning restore CA1822
    {
        return GetRequestKindCached(typeof(SampleCommand));
    }

    /// <summary>
    /// Type check without caching (baseline comparison)
    /// </summary>
    [Benchmark]
#pragma warning disable CA1822 // Mark members as static - BenchmarkDotNet requires instance methods
    public string TypeCheck_Direct()
#pragma warning restore CA1822
    {
        return GetRequestKindDirect(typeof(SampleCommand));
    }

    private static readonly ConcurrentDictionary<Type, string> TypeCheckCache = new();

    private static string GetRequestKindCached(Type requestType)
    {
        if (TypeCheckCache.TryGetValue(requestType, out var kind))
        {
            return kind;
        }

        kind = GetRequestKindDirect(requestType);
        TypeCheckCache.TryAdd(requestType, kind);
        return kind;
    }

    private static string GetRequestKindDirect(Type requestType)
    {
        if (typeof(ICommand).IsAssignableFrom(requestType))
        {
            return "command";
        }

        if (typeof(IQuery).IsAssignableFrom(requestType))
        {
            return "query";
        }

        return "request";
    }

    // Test types
    public sealed record SampleCommand(Guid Id) : ICommand<int>;
    public sealed record SampleQuery(string Name) : IQuery<string>;
    public sealed record SampleNotification(int Value) : INotification;

    public sealed class SampleCommandHandler : ICommandHandler<SampleCommand, int>
    {
        public Task<Either<EncinaError, int>> Handle(SampleCommand request, CancellationToken cancellationToken)
            => Task.FromResult(Right<EncinaError, int>(request.Id.GetHashCode()));
    }

    public sealed class SampleQueryHandler : IQueryHandler<SampleQuery, string>
    {
        public Task<Either<EncinaError, string>> Handle(SampleQuery request, CancellationToken cancellationToken)
            => Task.FromResult(Right<EncinaError, string>(request.Name));
    }

    public sealed class SampleNotificationHandler : INotificationHandler<SampleNotification>
    {
        public Task<Either<EncinaError, Unit>> Handle(SampleNotification notification, CancellationToken cancellationToken)
            => Task.FromResult(Right<EncinaError, Unit>(Unit.Default));
    }
}
