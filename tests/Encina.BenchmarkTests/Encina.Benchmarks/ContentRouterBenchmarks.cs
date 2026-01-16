using BenchmarkDotNet.Attributes;
using Encina.Messaging.ContentRouter;
using LanguageExt;
using Microsoft.Extensions.Logging.Abstractions;
using static LanguageExt.Prelude;

#pragma warning disable CA1822 // Mark members as static - BenchmarkDotNet requires instance methods

namespace Encina.Benchmarks;

/// <summary>
/// Benchmarks for Content-Based Router performance.
/// </summary>
[MemoryDiagnoser]
[SimpleJob(warmupCount: 3, iterationCount: 10)]
public class ContentRouterBenchmarks
{
    private Messaging.ContentRouter.ContentRouter _router = null!;
    private BuiltContentRouterDefinition<TestOrder, string> _simpleDefinition = null!;
    private BuiltContentRouterDefinition<TestOrder, string> _complexDefinition = null!;
    private BuiltContentRouterDefinition<TestOrder, string> _manyRoutesDefinition = null!;
    private TestOrder _lowOrder = null!;
    private TestOrder _mediumOrder = null!;
    private TestOrder _highOrder = null!;

    [GlobalSetup]
    public void Setup()
    {
        var options = new ContentRouterOptions();
        var logger = NullLogger<Messaging.ContentRouter.ContentRouter>.Instance;
        _router = new Messaging.ContentRouter.ContentRouter(options, logger);

        // Simple definition with one route
        _simpleDefinition = ContentRouterBuilder.Create<TestOrder, string>()
            .When(o => o.Total > 0)
            .RouteTo(o => Right<EncinaError, string>("processed"))
            .Build();

        // Complex definition with multiple conditions
        _complexDefinition = ContentRouterBuilder.Create<TestOrder, string>()
            .When("Premium", o => o.CustomerType == "Premium" && o.Total > 500)
            .WithPriority(1)
            .RouteTo(o => Right<EncinaError, string>("premium"))
            .When("HighValue", o => o.Total > 1000)
            .WithPriority(2)
            .RouteTo(o => Right<EncinaError, string>("high-value"))
            .When("International", o => o.IsInternational)
            .WithPriority(3)
            .RouteTo(o => Right<EncinaError, string>("international"))
            .Default(o => Right<EncinaError, string>("standard"))
            .Build();

        // Many routes definition
        var builder = ContentRouterBuilder.Create<TestOrder, string>();
        for (var i = 0; i < 50; i++)
        {
            var threshold = i * 20;
            builder = builder.When($"Route_{i}", o => o.Total > threshold)
                .RouteTo(o => Right<EncinaError, string>($"Route_{i}"));
        }
        _manyRoutesDefinition = builder.Build();

        // Test orders
        _lowOrder = new TestOrder { Total = 50, CustomerType = "Standard" };
        _mediumOrder = new TestOrder { Total = 500, CustomerType = "Premium" };
        _highOrder = new TestOrder { Total = 1500, CustomerType = "VIP", IsInternational = true };
    }

    [Benchmark(Baseline = true)]
    public async Task<Either<EncinaError, ContentRouterResult<string>>> SimpleRoute_SingleCondition()
    {
        return await _router.RouteAsync(_simpleDefinition, _lowOrder);
    }

    [Benchmark]
    public async Task<Either<EncinaError, ContentRouterResult<string>>> ComplexRoute_FirstMatch()
    {
        return await _router.RouteAsync(_complexDefinition, _mediumOrder);
    }

    [Benchmark]
    public async Task<Either<EncinaError, ContentRouterResult<string>>> ComplexRoute_DefaultFallback()
    {
        return await _router.RouteAsync(_complexDefinition, _lowOrder);
    }

    [Benchmark]
    public async Task<Either<EncinaError, ContentRouterResult<string>>> ManyRoutes_FirstMatch()
    {
        return await _router.RouteAsync(_manyRoutesDefinition, _lowOrder);
    }

    [Benchmark]
    public async Task<Either<EncinaError, ContentRouterResult<string>>> ManyRoutes_LateMatch()
    {
        return await _router.RouteAsync(_manyRoutesDefinition, _highOrder);
    }

    [Benchmark]
    public BuiltContentRouterDefinition<TestOrder, string> BuildDefinition_Simple()
    {
        return ContentRouterBuilder.Create<TestOrder, string>()
            .When(o => o.Total > 100)
            .RouteTo(o => Right<EncinaError, string>("high"))
            .Build();
    }

    [Benchmark]
    public BuiltContentRouterDefinition<TestOrder, string> BuildDefinition_Complex()
    {
        return ContentRouterBuilder.Create<TestOrder, string>()
            .When("High", o => o.Total > 1000)
            .WithPriority(1)
            .RouteTo(o => Right<EncinaError, string>("high"))
            .When("Medium", o => o.Total > 500)
            .WithPriority(2)
            .RouteTo(o => Right<EncinaError, string>("medium"))
            .When("Low", o => o.Total > 100)
            .WithPriority(3)
            .RouteTo(o => Right<EncinaError, string>("low"))
            .Default(o => Right<EncinaError, string>("default"))
            .Build();
    }

    public class TestOrder
    {
        public decimal Total { get; set; }
        public string? CustomerType { get; set; }
        public bool IsInternational { get; set; }
    }
}
