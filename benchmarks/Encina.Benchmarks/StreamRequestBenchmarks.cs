using System.Runtime.CompilerServices;
using BenchmarkDotNet.Attributes;
using LanguageExt;
using Microsoft.Extensions.DependencyInjection;
using static LanguageExt.Prelude;

#pragma warning disable CA1822 // Mark members as static - Benchmarks cannot be static

namespace Encina.Benchmarks;

/// <summary>
/// Performance benchmarks for Stream Requests.
/// Measures throughput, memory allocation, and overhead of stream processing.
/// </summary>
[MemoryDiagnoser]
public class StreamRequestBenchmarks
{
    private IServiceProvider _provider = default!;

    [GlobalSetup]
    public void Setup()
    {
        var services = new ServiceCollection();
        services.AddEncina();

        // Register stream handlers
        services.AddTransient<IStreamRequestHandler<SmallStreamRequest, int>, SmallStreamHandler>();
        services.AddTransient<IStreamRequestHandler<MediumStreamRequest, int>, MediumStreamHandler>();
        services.AddTransient<IStreamRequestHandler<LargeStreamRequest, int>, LargeStreamHandler>();
        services.AddTransient<IStreamRequestHandler<StreamWithBehaviorRequest, int>, SimpleStreamHandler>();

        // Register behaviors
        services.AddTransient<IStreamPipelineBehavior<StreamWithBehaviorRequest, int>, MultiplyByTwoBehavior>();
        services.AddTransient<IStreamPipelineBehavior<StreamWithBehaviorRequest, int>, FilterEvenBehavior>();

        _provider = services.BuildServiceProvider();
    }

    [Benchmark(Baseline = true)]
    public async Task<int> Stream_SmallDataset_10Items()
    {
        using var scope = _provider.CreateScope();
        var Encina = scope.ServiceProvider.GetRequiredService<IEncina>();
        var request = new SmallStreamRequest();

        var count = 0;
        await foreach (var item in Encina.Stream(request))
        {
            _ = item.Match(
                Left: _ => count,
                Right: _ => ++count);
        }

        return count;
    }

    [Benchmark]
    public async Task<int> Stream_MediumDataset_100Items()
    {
        using var scope = _provider.CreateScope();
        var Encina = scope.ServiceProvider.GetRequiredService<IEncina>();
        var request = new MediumStreamRequest();

        var count = 0;
        await foreach (var item in Encina.Stream(request))
        {
            _ = item.Match(
                Left: _ => count,
                Right: _ => ++count);
        }

        return count;
    }

    [Benchmark]
    public async Task<int> Stream_LargeDataset_1000Items()
    {
        using var scope = _provider.CreateScope();
        var Encina = scope.ServiceProvider.GetRequiredService<IEncina>();
        var request = new LargeStreamRequest();

        var count = 0;
        await foreach (var item in Encina.Stream(request))
        {
            _ = item.Match(
                Left: _ => count,
                Right: _ => ++count);
        }

        return count;
    }

    [Benchmark]
    public async Task<int> Stream_WithPipelineBehaviors()
    {
        using var scope = _provider.CreateScope();
        var Encina = scope.ServiceProvider.GetRequiredService<IEncina>();
        var request = new StreamWithBehaviorRequest();

        var count = 0;
        await foreach (var item in Encina.Stream(request))
        {
            _ = item.Match(
                Left: _ => count,
                Right: _ => ++count);
        }

        return count;
    }

    [Benchmark]
    public async Task<int> Stream_MaterializeToList_100Items()
    {
        using var scope = _provider.CreateScope();
        var Encina = scope.ServiceProvider.GetRequiredService<IEncina>();
        var request = new MediumStreamRequest();

        var results = new List<int>();
        await foreach (var item in Encina.Stream(request))
        {
            item.IfRight(results.Add);
        }

        return results.Count;
    }

    [Benchmark]
    public async Task<int> Stream_CountOnly_NoMaterialization()
    {
        using var scope = _provider.CreateScope();
        var Encina = scope.ServiceProvider.GetRequiredService<IEncina>();
        var request = new MediumStreamRequest();

        var count = 0;
        await foreach (var item in Encina.Stream(request))
        {
            _ = item.Match(
                Left: _ => count,
                Right: _ => ++count);
        }

        return count;
    }

    [Benchmark]
    public async Task<int> Stream_WithCancellation_EarlyExit()
    {
        using var scope = _provider.CreateScope();
        var Encina = scope.ServiceProvider.GetRequiredService<IEncina>();
        var request = new LargeStreamRequest();
        using var cts = new CancellationTokenSource();

        var count = 0;
        try
        {
            await foreach (var item in Encina.Stream(request, cts.Token))
            {
                _ = item.Match(
                    Left: _ => count,
                    Right: _ => ++count);

                if (count == 100)
                {
                    await cts.CancelAsync();
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Expected
        }

        return count;
    }

    [Benchmark]
    public async Task<int> Stream_DirectHandlerInvocation_NoEncina()
    {
        var handler = new MediumStreamHandler();
        var request = new MediumStreamRequest();

        var count = 0;
        await foreach (var item in handler.Handle(request, CancellationToken.None))
        {
            _ = item.Match(
                Left: _ => count,
                Right: _ => ++count);
        }

        return count;
    }

    #region Test Data

    private sealed record SmallStreamRequest : IStreamRequest<int>;
    private sealed record MediumStreamRequest : IStreamRequest<int>;
    private sealed record LargeStreamRequest : IStreamRequest<int>;
    private sealed record StreamWithBehaviorRequest : IStreamRequest<int>;

    private sealed class SmallStreamHandler : IStreamRequestHandler<SmallStreamRequest, int>
    {
        public async IAsyncEnumerable<Either<EncinaError, int>> Handle(
            SmallStreamRequest request,
            [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            for (var i = 1; i <= 10; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                yield return Right<EncinaError, int>(i);
            }

            await Task.CompletedTask;
        }
    }

    private sealed class MediumStreamHandler : IStreamRequestHandler<MediumStreamRequest, int>
    {
        public async IAsyncEnumerable<Either<EncinaError, int>> Handle(
            MediumStreamRequest request,
            [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            for (var i = 1; i <= 100; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                yield return Right<EncinaError, int>(i);
            }

            await Task.CompletedTask;
        }
    }

    private sealed class LargeStreamHandler : IStreamRequestHandler<LargeStreamRequest, int>
    {
        public async IAsyncEnumerable<Either<EncinaError, int>> Handle(
            LargeStreamRequest request,
            [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            for (var i = 1; i <= 1000; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                yield return Right<EncinaError, int>(i);
            }

            await Task.CompletedTask;
        }
    }

    private sealed class SimpleStreamHandler : IStreamRequestHandler<StreamWithBehaviorRequest, int>
    {
        public async IAsyncEnumerable<Either<EncinaError, int>> Handle(
            StreamWithBehaviorRequest request,
            [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            for (var i = 1; i <= 100; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                yield return Right<EncinaError, int>(i);
            }

            await Task.CompletedTask;
        }
    }

    private sealed class MultiplyByTwoBehavior : IStreamPipelineBehavior<StreamWithBehaviorRequest, int>
    {
        public async IAsyncEnumerable<Either<EncinaError, int>> Handle(
            StreamWithBehaviorRequest request,
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

    private sealed class FilterEvenBehavior : IStreamPipelineBehavior<StreamWithBehaviorRequest, int>
    {
        public async IAsyncEnumerable<Either<EncinaError, int>> Handle(
            StreamWithBehaviorRequest request,
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
