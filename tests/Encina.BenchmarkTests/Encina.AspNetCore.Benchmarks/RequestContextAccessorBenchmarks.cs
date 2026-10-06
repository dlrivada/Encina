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
    private IRequestContext _tenantChangedContext = null!;

    [GlobalSetup]
    public void Setup()
    {
        _accessor = new RequestContextAccessor();
        _context = RequestContext.CreateForTest(
            correlationId: "benchmark-correlation",
            tenantId: "benchmark-tenant");
        _tenantChangedContext = _context.WithTenantId("benchmark-other-tenant");
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
    public IRequestContext? SetAndGetContext()
    {
        _accessor.RequestContext = _context;
        return _accessor.RequestContext;
    }

    [BenchmarkCategory("DocRef:bench:aspnetcore/context-async")]
    [Benchmark]
    public async Task<IRequestContext?> SetGetAcrossAwait()
    {
        _accessor.RequestContext = _context;
        await Task.Yield();
        return _accessor.RequestContext;
    }

    /// <summary>
    /// The setter never clears (#1705 Phase 2): what replaced the former null set is an
    /// identity- and origin-preserving tenant change, the path tenant resolution takes.
    /// </summary>
    [Benchmark]
    public void SetTenantChangedContext()
    {
        _accessor.RequestContext = _tenantChangedContext;
    }

    [BenchmarkCategory("DocRef:bench:aspnetcore/context-create")]
    [Benchmark]
    public IRequestContextAccessor CreateNewAccessor()
    {
        return new RequestContextAccessor();
    }
}
