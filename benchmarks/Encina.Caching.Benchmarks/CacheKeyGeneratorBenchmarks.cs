using BenchmarkDotNet.Attributes;
using Microsoft.Extensions.Options;

namespace Encina.Caching.Benchmarks;

/// <summary>
/// Benchmarks for cache key generation performance.
/// </summary>
[MemoryDiagnoser]
[SimpleJob(warmupCount: 3, iterationCount: 10)]
public class CacheKeyGeneratorBenchmarks
{
    private static readonly string[] DefaultTags = ["tag1", "tag2", "tag3"];

    private DefaultCacheKeyGenerator _keyGenerator = null!;
    private IRequestContext _context = null!;
    private SimpleQuery _simpleQuery = null!;
    private ComplexQuery _complexQuery = null!;
    private TemplatedQuery _templatedQuery = null!;

    [GlobalSetup]
    public void Setup()
    {
        var options = Options.Create(new CachingOptions { KeyPrefix = "cache" });
        _keyGenerator = new DefaultCacheKeyGenerator(options);
        _context = new BenchmarkRequestContext();
        _simpleQuery = new SimpleQuery(42);
        _complexQuery = new ComplexQuery(
            Guid.NewGuid(),
            "Test Product",
            99.99m,
            DefaultTags);
        _templatedQuery = new TemplatedQuery(Guid.NewGuid());
    }

    [Benchmark(Baseline = true)]
    public string GenerateKey_SimpleQuery()
    {
        return _keyGenerator.GenerateKey<SimpleQuery, string>(_simpleQuery, _context);
    }

    [Benchmark]
    public string GenerateKey_ComplexQuery()
    {
        return _keyGenerator.GenerateKey<ComplexQuery, ProductResult>(_complexQuery, _context);
    }

    [Benchmark]
    public string GenerateKey_WithTemplate()
    {
        return _keyGenerator.GenerateKey<TemplatedQuery, string>(_templatedQuery, _context);
    }

    [Benchmark]
    public string GeneratePattern()
    {
        return _keyGenerator.GeneratePattern<SimpleQuery>(_context);
    }

    [Benchmark]
    public string GeneratePattern_WithTemplate()
    {
        return _keyGenerator.GeneratePatternFromTemplate("product:{ProductId}:*", _templatedQuery, _context);
    }
}

// Test request types
[Cache(DurationSeconds = 300)]
public sealed record SimpleQuery(int Id) : IRequest<string>;

[Cache(DurationSeconds = 600)]
public sealed record ComplexQuery(
    Guid ProductId,
    string Name,
    decimal Price,
    string[] Tags) : IRequest<ProductResult>;

[Cache(DurationSeconds = 300, KeyTemplate = "product:{ProductId}:details")]
public sealed record TemplatedQuery(Guid ProductId) : IRequest<string>;

public sealed record ProductResult(Guid Id, string Name, decimal Price);

public sealed class BenchmarkRequestContext : IRequestContext
{
    public string? TenantId => "benchmark-tenant";
    public string? UserId => "benchmark-user";
    public string CorrelationId => "bench-corr-001";
    public string? IdempotencyKey => null;
    public DateTimeOffset Timestamp => DateTimeOffset.UtcNow;
    public IReadOnlyDictionary<string, object?> Metadata => _metadata;

    private readonly Dictionary<string, object?> _metadata = new();

    public IRequestContext WithMetadata(string key, object? value)
    {
        var clone = new BenchmarkRequestContext();
        foreach (var kvp in _metadata)
        {
            clone._metadata[kvp.Key] = kvp.Value;
        }
        clone._metadata[key] = value;
        return clone;
    }

    public IRequestContext WithUserId(string? userId) => this;
    public IRequestContext WithIdempotencyKey(string? idempotencyKey) => this;
    public IRequestContext WithTenantId(string? tenantId) => this;
}
