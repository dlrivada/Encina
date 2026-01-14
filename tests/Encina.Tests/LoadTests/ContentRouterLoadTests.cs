using System.Diagnostics;

using Encina.Messaging.ContentRouter;

using LanguageExt;

using Microsoft.Extensions.Logging;

using NSubstitute;

using Shouldly;

using static LanguageExt.Prelude;

namespace Encina.Tests.LoadTests;

/// <summary>
/// Load tests for Content-Based Router to verify performance under stress.
/// </summary>
[Trait("Category", "Load")]
public sealed class ContentRouterLoadTests
{
    private readonly ContentRouterOptions _options = new();
    private readonly ILogger<Messaging.ContentRouter.ContentRouter> _logger =
        Substitute.For<ILogger<Messaging.ContentRouter.ContentRouter>>();
    private readonly Xunit.Abstractions.ITestOutputHelper _output;

    public ContentRouterLoadTests(Xunit.Abstractions.ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public async Task HighConcurrency_1000Requests_AllSucceed()
    {
        // Arrange
        var router = new Messaging.ContentRouter.ContentRouter(_options, _logger);
        var definition = ContentRouterBuilder.Create<TestOrder, string>()
            .When(o => o.Total > 100)
            .RouteTo(o => Right<EncinaError, string>("high"))
            .When(o => o.Total > 50)
            .RouteTo(o => Right<EncinaError, string>("medium"))
            .Default(o => Right<EncinaError, string>("low"))
            .Build();

        const int requestCount = 1000;
        var random = new Random(42); // Deterministic seed for reproducibility // DevSkim: ignore DS148264

        // Act
        var stopwatch = Stopwatch.StartNew();
        var tasks = Enumerable.Range(0, requestCount)
            .Select(async i =>
            {
                var order = new TestOrder { Total = random.Next(1, 200) };
                return await router.RouteAsync(definition, order);
            })
            .ToList();

        var results = await Task.WhenAll(tasks);
        stopwatch.Stop();

        // Assert
        results.ShouldAllBeSuccess();
        results.Length.ShouldBe(requestCount);

        // Performance assertion: should complete in reasonable time
        stopwatch.Elapsed.TotalSeconds.ShouldBeLessThan(30);
    }

    [Fact]
    public async Task HighThroughput_SequentialRequests_MaintainsPerformance()
    {
        // Arrange
        var router = new Messaging.ContentRouter.ContentRouter(_options, _logger);
        var definition = ContentRouterBuilder.Create<TestOrder, string>()
            .When(o => o.Total > 0)
            .RouteTo(o => Right<EncinaError, string>($"Total: {o.Total}"))
            .Build();

        const int requestCount = 10000;
        var successCount = 0;

        // Act
        var stopwatch = Stopwatch.StartNew();
        for (var i = 0; i < requestCount; i++)
        {
            var order = new TestOrder { Total = i + 1 };
            var result = await router.RouteAsync(definition, order);
            if (result.IsRight) successCount++;
        }
        stopwatch.Stop();

        // Assert
        successCount.ShouldBe(requestCount);

        // Performance: should process > 1000 requests/second
        var requestsPerSecond = requestCount / stopwatch.Elapsed.TotalSeconds;
        requestsPerSecond.ShouldBeGreaterThan(1000);
    }

    [Fact]
    public async Task ManyRoutes_100Routes_EvaluatesEfficiently()
    {
        // Arrange
        var router = new Messaging.ContentRouter.ContentRouter(_options, _logger);

        var builder = ContentRouterBuilder.Create<TestOrder, string>();
        for (var i = 0; i < 100; i++)
        {
            var threshold = i * 10;
            builder = builder.When($"Route_{i}", o => o.Total > threshold)
                .RouteTo(o => Right<EncinaError, string>($"Route_{i}"));
        }
        var definition = builder.Build();

        const int requestCount = 1000;

        // Act
        var stopwatch = Stopwatch.StartNew();
        var tasks = Enumerable.Range(0, requestCount)
            .Select(async i =>
            {
                var order = new TestOrder { Total = (i % 100) * 10 + 5 };
                return await router.RouteAsync(definition, order);
            })
            .ToList();

        var results = await Task.WhenAll(tasks);
        stopwatch.Stop();

        // Assert
        results.ShouldAllBeSuccess();

        // Should complete in reasonable time even with 100 routes
        stopwatch.Elapsed.TotalSeconds.ShouldBeLessThan(30);
    }

    [Fact]
    public async Task ParallelEvaluation_WithMultipleMatches_HandlesLoad()
    {
        // Arrange
        var options = new ContentRouterOptions
        {
            AllowMultipleMatches = true,
            EvaluateInParallel = true,
            MaxDegreeOfParallelism = Environment.ProcessorCount
        };
        var router = new Messaging.ContentRouter.ContentRouter(options, _logger);

        var builder = ContentRouterBuilder.Create<TestOrder, string>();
        for (var i = 0; i < 10; i++)
        {
            builder = builder.When($"Route_{i}", o => true)
                .RouteTo(async (o, ct) =>
                {
                    await Task.Delay(1, ct); // Simulate some work
                    return Right<EncinaError, string>($"Route_{i}");
                });
        }
        var definition = builder.Build();

        const int requestCount = 100;

        // Act
        var stopwatch = Stopwatch.StartNew();
        var tasks = Enumerable.Range(0, requestCount)
            .Select(async i =>
            {
                var order = new TestOrder { Total = i + 1 };
                return await router.RouteAsync(definition, order);
            })
            .ToList();

        var results = await Task.WhenAll(tasks);
        stopwatch.Stop();

        // Assert
        // Each result should have 10 route results
        foreach (var result in results)
        {
            var routerResult = result.ShouldBeSuccess();
            routerResult.MatchedRouteCount.ShouldBe(10);
        }
    }

    [Fact]
    public async Task StressTest_ContinuousLoad_MaintainsStability()
    {
        // Arrange
        var router = new Messaging.ContentRouter.ContentRouter(_options, _logger);
        var definition = ContentRouterBuilder.Create<TestOrder, string>()
            .When(o => o.Total > 0)
            .RouteTo(o => Right<EncinaError, string>("processed"))
            .Build();

        var successCount = 0;
        var errorCount = 0;
        var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5)); // Run for 5 seconds

        // Act
        // Stress test pattern: aggregate success/error counts across all workers rather than
        // failing immediately. This allows measuring throughput and stability over the test
        // duration. Final assertions verify zero errors occurred across all iterations.
        var workers = Enumerable.Range(0, 10)
            .Select(async workerId =>
            {
                var localSuccess = 0;
                var localError = 0;
                var random = new Random(workerId); // Deterministic seed per worker // DevSkim: ignore DS148264

                while (!cts.Token.IsCancellationRequested)
                {
                    try
                    {
                        var order = new TestOrder { Total = random.Next(1, 1000) };
                        var result = await router.RouteAsync(definition, order, cts.Token);

                        if (result.IsRight)
                        {
                            localSuccess++;
                        }
                        else
                        {
                            // Log error details for diagnostics when investigating failures
                            // Using ITestOutputHelper for xUnit integration and test log visibility
                            result.IfLeft(error =>
                                _output.WriteLine(
                                    $"Worker {workerId} error: {error.Message}"));
                            localError++;
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        break;
                    }
                }

                return (Success: localSuccess, Error: localError);
            })
            .ToList();

        var workerResults = await Task.WhenAll(workers);
        successCount = workerResults.Sum(r => r.Success);
        errorCount = workerResults.Sum(r => r.Error);

        // Assert
        successCount.ShouldBeGreaterThan(0);
        errorCount.ShouldBe(0);

        // Should process at least some requests per second across all workers
        var requestsPerSecond = successCount / 5.0; // 5 seconds test
        requestsPerSecond.ShouldBeGreaterThan(100); // At least 100 req/s
    }

    [Fact]
    public async Task MemoryPressure_LargePayloads_HandlesCorrectly()
    {
        // Arrange
        var router = new Messaging.ContentRouter.ContentRouter(_options, _logger);
        var definition = ContentRouterBuilder.Create<LargeOrder, string>()
            .When(o => o.Items.Count > 0)
            .RouteTo(o => Right<EncinaError, string>($"Items: {o.Items.Count}"))
            .Build();

        const int requestCount = 100;

        // Act
        var tasks = Enumerable.Range(0, requestCount)
            .Select(async i =>
            {
                var order = new LargeOrder
                {
                    Items = Enumerable.Range(0, 1000)
                        .Select(j => $"Item_{i}_{j}")
                        .ToList()
                };
                return await router.RouteAsync(definition, order);
            })
            .ToList();

        var results = await Task.WhenAll(tasks);

        // Assert
        results.ShouldAllBeSuccess();
    }

    public class TestOrder
    {
        public decimal Total { get; set; }
    }

    public class LargeOrder
    {
        public List<string> Items { get; set; } = [];
    }
}
