using System.Runtime.CompilerServices;
using LanguageExt;
using Microsoft.Extensions.DependencyInjection;
using NBomber.CSharp;
using Shouldly;
using Xunit.Abstractions;
using static LanguageExt.Prelude;

namespace Encina.Tests.LoadTests;

/// <summary>
/// Load tests for Stream Requests using NBomber.
/// Verifies performance, concurrency, and throughput under stress conditions.
/// </summary>
/// <remarks>
/// TEMPORARILY DISABLED: These tests cause CLR crashes and Windows restarts
/// under high concurrency. See GitHub Issue #5 for details.
/// Root cause appears to be NBomber + IAsyncEnumerable interaction causing
/// thread pool exhaustion or memory pressure that crashes the CLR.
/// </remarks>
[Trait("Category", "Load")]
public sealed class StreamRequestLoadTests
{
    private const string SkipReason = "Issue #5: CLR crash under high concurrency - temporarily disabled";

    private readonly ITestOutputHelper _output;

    public StreamRequestLoadTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact(Skip = SkipReason)]
    public void HighConcurrency_MultipleStreams_ShouldHandleLoad()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddEncina();
        services.AddTransient<IStreamRequestHandler<LoadStreamRequest, int>, LoadStreamHandler>();
        var provider = services.BuildServiceProvider();

        // Act - Load test with NBomber
        var scenario = Scenario.Create("concurrent_streams", async context =>
        {
            using var scope = provider.CreateScope();
            var Encina = scope.ServiceProvider.GetRequiredService<IEncina>();
            var request = new LoadStreamRequest(ItemCount: 100);

            var count = 0;
            await foreach (var item in Encina.Stream(request))
            {
                item.IfRight(_ => count++);
            }

            return count == 100 ? Response.Ok() : Response.Fail<int>(statusCode: "incomplete");
        })
        .WithLoadSimulations(
            Simulation.Inject(rate: 50, interval: TimeSpan.FromSeconds(1), during: TimeSpan.FromSeconds(10))
        );

        var stats = NBomberRunner
            .RegisterScenarios(scenario)
            .WithoutReports()
            .Run();

        // Assert
        var scen = stats.ScenarioStats[0];
        _output.WriteLine($"Total streams: {scen.Ok.Request.Count + scen.Fail.Request.Count}");
        _output.WriteLine($"OK: {scen.Ok.Request.Count}, Fail: {scen.Fail.Request.Count}");
        _output.WriteLine($"RPS: {scen.Ok.Request.RPS}");

        scen.Ok.Request.Count.ShouldBeGreaterThan(450, "at least 90% success rate");
        scen.Fail.Request.Count.ShouldBeLessThan(50, "less than 10% failures");
    }

    [Fact(Skip = SkipReason)]
    public void HighThroughput_LargeStreamProcessing_ShouldMaintainPerformance()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddEncina();
        services.AddTransient<IStreamRequestHandler<LoadStreamRequest, int>, LoadStreamHandler>();
        var provider = services.BuildServiceProvider();

        // Act - Process large streams under load
        var scenario = Scenario.Create("large_stream_throughput", async context =>
        {
            using var scope = provider.CreateScope();
            var Encina = scope.ServiceProvider.GetRequiredService<IEncina>();
            var request = new LoadStreamRequest(ItemCount: 1000);

            var count = 0;
            await foreach (var item in Encina.Stream(request))
            {
                item.IfRight(_ => count++);
            }

            return count == 1000 ? Response.Ok() : Response.Fail<int>(statusCode: "incomplete");
        })
        .WithLoadSimulations(
            Simulation.Inject(rate: 10, interval: TimeSpan.FromSeconds(1), during: TimeSpan.FromSeconds(10))
        );

        var stats = NBomberRunner
            .RegisterScenarios(scenario)
            .WithoutReports()
            .Run();

        // Assert
        var scen = stats.ScenarioStats[0];
        _output.WriteLine($"Total large streams: {scen.Ok.Request.Count + scen.Fail.Request.Count}");
        _output.WriteLine($"OK: {scen.Ok.Request.Count}, Fail: {scen.Fail.Request.Count}");
        _output.WriteLine($"RPS: {scen.Ok.Request.RPS}");

        scen.Ok.Request.Count.ShouldBeGreaterThan(90, "at least 90% success rate");
    }

    [Fact(Skip = SkipReason)]
    public void Stress_WithBehaviors_ShouldHandlePipelineLoad()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddEncina();
        services.AddTransient<IStreamRequestHandler<LoadStreamRequest, int>, LoadStreamHandler>();
        services.AddTransient<IStreamPipelineBehavior<LoadStreamRequest, int>, MultiplyBehavior>();
        services.AddTransient<IStreamPipelineBehavior<LoadStreamRequest, int>, FilterBehavior>();
        var provider = services.BuildServiceProvider();

        // Act - Stress test with pipeline behaviors
        var scenario = Scenario.Create("stream_with_behaviors", async context =>
        {
            using var scope = provider.CreateScope();
            var Encina = scope.ServiceProvider.GetRequiredService<IEncina>();
            var request = new LoadStreamRequest(ItemCount: 50);

            var count = 0;
            await foreach (var item in Encina.Stream(request))
            {
                item.IfRight(_ => count++);
            }

            // FilterBehavior keeps even numbers (after multiply), so count should be 25
            return count == 25 ? Response.Ok() : Response.Fail<int>(statusCode: $"expected_25_got_{count}");
        })
        .WithLoadSimulations(
            Simulation.Inject(rate: 100, interval: TimeSpan.FromSeconds(1), during: TimeSpan.FromSeconds(10))
        );

        var stats = NBomberRunner
            .RegisterScenarios(scenario)
            .WithoutReports()
            .Run();

        // Assert
        var scen = stats.ScenarioStats[0];
        _output.WriteLine($"Total pipeline executions: {scen.Ok.Request.Count + scen.Fail.Request.Count}");
        _output.WriteLine($"OK: {scen.Ok.Request.Count}, Fail: {scen.Fail.Request.Count}");

        scen.Ok.Request.Count.ShouldBeGreaterThan(900, "pipeline should handle high concurrency");
    }

    [Fact(Skip = SkipReason)]
    public void Endurance_ContinuousStreaming_ShouldNotDegrade()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddEncina();
        services.AddTransient<IStreamRequestHandler<LoadStreamRequest, int>, LoadStreamHandler>();
        var provider = services.BuildServiceProvider();

        // Act - Endurance test with constant load
        var scenario = Scenario.Create("endurance_streaming", async context =>
        {
            using var scope = provider.CreateScope();
            var Encina = scope.ServiceProvider.GetRequiredService<IEncina>();
            var request = new LoadStreamRequest(ItemCount: 200);

            var count = 0;
            await foreach (var item in Encina.Stream(request))
            {
                item.IfRight(_ => count++);
            }

            return count == 200 ? Response.Ok() : Response.Fail<int>(statusCode: "incomplete");
        })
        .WithLoadSimulations(
            Simulation.KeepConstant(copies: 20, during: TimeSpan.FromSeconds(30))
        );

        var stats = NBomberRunner
            .RegisterScenarios(scenario)
            .WithoutReports()
            .Run();

        // Assert
        var scen = stats.ScenarioStats[0];
        _output.WriteLine($"Total endurance runs: {scen.Ok.Request.Count + scen.Fail.Request.Count}");
        _output.WriteLine($"OK: {scen.Ok.Request.Count}, Fail: {scen.Fail.Request.Count}");
        _output.WriteLine($"RPS: {scen.Ok.Request.RPS}");

        scen.Ok.Request.Count.ShouldBeGreaterThan(0, "should complete at least some requests");
    }

    [Fact(Skip = SkipReason)]
    public void ErrorHandling_StreamsWithErrors_ShouldMaintainThroughput()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddEncina();
        services.AddTransient<IStreamRequestHandler<ErrorStreamRequest, int>, ErrorStreamHandler>();
        var provider = services.BuildServiceProvider();

        // Act - Load test with error-producing streams
        var scenario = Scenario.Create("error_stream_handling", async context =>
        {
            using var scope = provider.CreateScope();
            var Encina = scope.ServiceProvider.GetRequiredService<IEncina>();
            var request = new ErrorStreamRequest(TotalItems: 100, ErrorInterval: 10);

            var successCount = 0;
            var errorCount = 0;

            await foreach (var item in Encina.Stream(request))
            {
                _ = item.Match(
                    Left: _ => errorCount++,
                    Right: _ => successCount++
                );
            }

            // Should have 90 successes and 10 errors
            return (successCount == 90 && errorCount == 10)
                ? Response.Ok()
                : Response.Fail<int>(statusCode: $"success_{successCount}_errors_{errorCount}");
        })
        .WithLoadSimulations(
            Simulation.Inject(rate: 50, interval: TimeSpan.FromSeconds(1), during: TimeSpan.FromSeconds(10))
        );

        var stats = NBomberRunner
            .RegisterScenarios(scenario)
            .WithoutReports()
            .Run();

        // Assert
        var scen = stats.ScenarioStats[0];
        _output.WriteLine($"Total error streams: {scen.Ok.Request.Count + scen.Fail.Request.Count}");
        _output.WriteLine($"OK: {scen.Ok.Request.Count}, Fail: {scen.Fail.Request.Count}");

        scen.Ok.Request.Count.ShouldBeGreaterThan(450, "error handling should not impact throughput");
    }

    [Fact(Skip = SkipReason)]
    public void Cancellation_UnderLoad_ShouldHandleGracefully()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddEncina();
        services.AddTransient<IStreamRequestHandler<LoadStreamRequest, int>, LoadStreamHandler>();
        var provider = services.BuildServiceProvider();

        // Act - Load test with cancellation
        var scenario = Scenario.Create("cancellation_handling", async context =>
        {
            using var scope = provider.CreateScope();
            var Encina = scope.ServiceProvider.GetRequiredService<IEncina>();
            var request = new LoadStreamRequest(ItemCount: 1000);
            using var cts = new CancellationTokenSource();

            var count = 0;
            try
            {
                await foreach (var item in Encina.Stream(request, cts.Token))
                {
                    item.IfRight(_ => count++);

                    // Cancel after 50 items
                    if (count == 50)
                    {
                        await cts.CancelAsync();
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // Expected
            }

            // Should have processed around 50 items before cancellation
            return count is >= 50 and < 100
                ? Response.Ok()
                : Response.Fail<int>(statusCode: $"processed_{count}");
        })
        .WithLoadSimulations(
            Simulation.Inject(rate: 30, interval: TimeSpan.FromSeconds(1), during: TimeSpan.FromSeconds(10))
        );

        var stats = NBomberRunner
            .RegisterScenarios(scenario)
            .WithoutReports()
            .Run();

        // Assert
        var scen = stats.ScenarioStats[0];
        _output.WriteLine($"Total cancellation tests: {scen.Ok.Request.Count + scen.Fail.Request.Count}");
        _output.WriteLine($"OK: {scen.Ok.Request.Count}, Fail: {scen.Fail.Request.Count}");

        scen.Ok.Request.Count.ShouldBeGreaterThan(250, "cancellation should work reliably under load");
    }

    [Fact(Skip = SkipReason)]
    public void MemoryPressure_ManySmallStreams_ShouldNotLeak()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddEncina();
        services.AddTransient<IStreamRequestHandler<LoadStreamRequest, int>, LoadStreamHandler>();
        var provider = services.BuildServiceProvider();

        // Act - Memory pressure test with many small streams
        var scenario = Scenario.Create("memory_pressure", async context =>
        {
            using var scope = provider.CreateScope();
            var Encina = scope.ServiceProvider.GetRequiredService<IEncina>();
            var request = new LoadStreamRequest(ItemCount: 10);

            var count = 0;
            await foreach (var item in Encina.Stream(request))
            {
                item.IfRight(_ => count++);
            }

            return count == 10 ? Response.Ok() : Response.Fail<int>(statusCode: "incomplete");
        })
        .WithLoadSimulations(
            Simulation.Inject(rate: 200, interval: TimeSpan.FromSeconds(1), during: TimeSpan.FromSeconds(10))
        );

        var stats = NBomberRunner
            .RegisterScenarios(scenario)
            .WithoutReports()
            .Run();

        // Assert
        var scen = stats.ScenarioStats[0];
        _output.WriteLine($"Total small streams: {scen.Ok.Request.Count + scen.Fail.Request.Count}");
        _output.WriteLine($"OK: {scen.Ok.Request.Count}, Fail: {scen.Fail.Request.Count}");
        _output.WriteLine($"RPS: {scen.Ok.Request.RPS}");

        scen.Ok.Request.Count.ShouldBeGreaterThan(1800, "should handle many small streams efficiently");
    }

    [Fact(Skip = SkipReason)]
    public void BurstLoad_SuddenSpike_ShouldRecover()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddEncina();
        services.AddTransient<IStreamRequestHandler<LoadStreamRequest, int>, LoadStreamHandler>();
        var provider = services.BuildServiceProvider();

        // Act - Burst load test
        var scenario = Scenario.Create("burst_load", async context =>
        {
            using var scope = provider.CreateScope();
            var Encina = scope.ServiceProvider.GetRequiredService<IEncina>();
            var request = new LoadStreamRequest(ItemCount: 100);

            var count = 0;
            await foreach (var item in Encina.Stream(request))
            {
                item.IfRight(_ => count++);
            }

            return count == 100 ? Response.Ok() : Response.Fail<int>(statusCode: "incomplete");
        })
        .WithLoadSimulations(
            // Ramp up simulation: sudden spike
            Simulation.RampingInject(rate: 0, interval: TimeSpan.FromSeconds(1), during: TimeSpan.FromSeconds(2)),
            Simulation.RampingInject(rate: 100, interval: TimeSpan.FromSeconds(1), during: TimeSpan.FromSeconds(5)),
            Simulation.RampingInject(rate: 0, interval: TimeSpan.FromSeconds(1), during: TimeSpan.FromSeconds(3))
        );

        var stats = NBomberRunner
            .RegisterScenarios(scenario)
            .WithoutReports()
            .Run();

        // Assert
        var scen = stats.ScenarioStats[0];
        _output.WriteLine($"Total burst requests: {scen.Ok.Request.Count + scen.Fail.Request.Count}");
        _output.WriteLine($"OK: {scen.Ok.Request.Count}, Fail: {scen.Fail.Request.Count}");

        scen.Ok.Request.Count.ShouldBeGreaterThan(400, "should handle burst load gracefully");
    }

    #region Test Data

    private sealed record LoadStreamRequest(int ItemCount) : IStreamRequest<int>;

    private sealed record ErrorStreamRequest(int TotalItems, int ErrorInterval) : IStreamRequest<int>;

    private sealed class LoadStreamHandler : IStreamRequestHandler<LoadStreamRequest, int>
    {
        public async IAsyncEnumerable<Either<EncinaError, int>> Handle(
            LoadStreamRequest request,
            [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            for (var i = 1; i <= request.ItemCount; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                yield return Right<EncinaError, int>(i);
            }

            await Task.CompletedTask;
        }
    }

    private sealed class ErrorStreamHandler : IStreamRequestHandler<ErrorStreamRequest, int>
    {
        public async IAsyncEnumerable<Either<EncinaError, int>> Handle(
            ErrorStreamRequest request,
            [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            for (var i = 1; i <= request.TotalItems; i++)
            {
                if (i % request.ErrorInterval == 0)
                {
                    yield return Left<EncinaError, int>(
                        EncinaErrors.Create("LOAD_ERROR", $"Error at item {i}"));
                }
                else
                {
                    yield return Right<EncinaError, int>(i);
                }
            }

            await Task.CompletedTask;
        }
    }

    private sealed class MultiplyBehavior : IStreamPipelineBehavior<LoadStreamRequest, int>
    {
        public async IAsyncEnumerable<Either<EncinaError, int>> Handle(
            LoadStreamRequest request,
            IRequestContext context,
            StreamHandlerCallback<int> nextStep,
            [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            await foreach (var item in nextStep().WithCancellation(cancellationToken))
            {
                yield return item.Map(v => v * 2);
            }
        }
    }

    private sealed class FilterBehavior : IStreamPipelineBehavior<LoadStreamRequest, int>
    {
        public async IAsyncEnumerable<Either<EncinaError, int>> Handle(
            LoadStreamRequest request,
            IRequestContext context,
            StreamHandlerCallback<int> nextStep,
            [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            await foreach (var item in nextStep().WithCancellation(cancellationToken))
            {
                var shouldYield = item.Match(
                    Left: _ => true,
                    Right: value => value % 2 == 0);

                if (shouldYield)
                {
                    yield return item;
                }
            }
        }
    }

    #endregion
}
