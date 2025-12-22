using BenchmarkDotNet.Attributes;

namespace Encina.AspNetCore.Benchmarks;

/// <summary>
/// Benchmarks for <see cref="RequestContextAccessor"/>.
/// Measures performance of AsyncLocal-based context storage.
/// </summary>
[MemoryDiagnoser]
[MarkdownExporter]
public class RequestContextAccessorBenchmarks
{
    private RequestContextAccessor _accessor = null!;
    private IRequestContext _context = null!;

    [GlobalSetup]
    public void Setup()
    {
        _accessor = new RequestContextAccessor();
        _context = RequestContext.CreateForTest(
            correlationId: "benchmark-correlation",
            userId: "benchmark-user",
            tenantId: "benchmark-tenant");
    }

    [Benchmark(Baseline = true)]
    public void SetContext()
    {
        _accessor.RequestContext = _context;
    }

    [Benchmark]
    public IRequestContext? GetContext()
    {
        return _accessor.RequestContext;
    }

    [Benchmark]
    public void SetAndGetContext()
    {
        _accessor.RequestContext = _context;
        var retrieved = _accessor.RequestContext;
    }

    [Benchmark]
    public async Task SetGetAcrossAwait()
    {
        _accessor.RequestContext = _context;
        await Task.Yield();
        var retrieved = _accessor.RequestContext;
    }

    [Benchmark]
    public void SetNullContext()
    {
        _accessor.RequestContext = null;
    }

    [Benchmark]
    public void CreateNewAccessor()
    {
        var accessor = new RequestContextAccessor();
    }
}
