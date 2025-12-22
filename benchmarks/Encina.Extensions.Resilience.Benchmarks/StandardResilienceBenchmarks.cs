using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Order;
using LanguageExt;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Polly;
using Encina.Extensions.Resilience;

namespace Encina.Extensions.Resilience.Benchmarks;

/// <summary>
/// Benchmarks for Encina with Standard Resilience Handler.
/// Measures performance impact of resilience strategies.
/// </summary>
[MemoryDiagnoser]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
[RankColumn]
public class StandardResilienceBenchmarks
{
    private IEncina _EncinaNoResilience = null!;
    private IEncina _EncinaWithResilience = null!;
    private IEncina _EncinaWithRetry = null!;
    private BenchmarkRequest _request = null!;

    [GlobalSetup]
    public void Setup()
    {
        // Setup Encina without resilience (baseline)
        var servicesBaseline = new ServiceCollection();
        servicesBaseline.AddLogging(builder => builder.SetMinimumLevel(LogLevel.Warning));
        servicesBaseline.AddEncina(typeof(StandardResilienceBenchmarks).Assembly);
        var providerBaseline = servicesBaseline.BuildServiceProvider();
        _EncinaNoResilience = providerBaseline.GetRequiredService<IEncina>();

        // Setup Encina with standard resilience
        var servicesResilience = new ServiceCollection();
        servicesResilience.AddLogging(builder => builder.SetMinimumLevel(LogLevel.Warning));
        servicesResilience.AddEncina(typeof(StandardResilienceBenchmarks).Assembly);
        servicesResilience.AddEncinaStandardResilience();
        var providerResilience = servicesResilience.BuildServiceProvider();
        _EncinaWithResilience = providerResilience.GetRequiredService<IEncina>();

        // Setup Encina with retry-focused resilience
        var servicesRetry = new ServiceCollection();
        servicesRetry.AddLogging(builder => builder.SetMinimumLevel(LogLevel.Warning));
        servicesRetry.AddEncina(typeof(StandardResilienceBenchmarks).Assembly);
        servicesRetry.AddScoped<RetryingHandler>();
        servicesRetry.AddScoped<IRequestHandler<BenchmarkRequest, BenchmarkResponse>>(sp =>
            sp.GetRequiredService<RetryingHandler>());
        servicesRetry.AddEncinaStandardResilience(options =>
        {
            options.Retry.MaxRetryAttempts = 3;
            options.Retry.Delay = TimeSpan.FromMilliseconds(1);
            options.Retry.BackoffType = DelayBackoffType.Constant;
        });
        var providerRetry = servicesRetry.BuildServiceProvider();
        _EncinaWithRetry = providerRetry.GetRequiredService<IEncina>();

        _request = new BenchmarkRequest { Value = 42 };
    }

    [Benchmark(Baseline = true)]
    public async Task<Either<EncinaError, BenchmarkResponse>> NoResilience_Baseline()
    {
        return await _EncinaNoResilience.Send(_request);
    }

    [Benchmark]
    public async Task<Either<EncinaError, BenchmarkResponse>> StandardResilience_Success()
    {
        return await _EncinaWithResilience.Send(_request);
    }

    [Benchmark]
    public async Task<Either<EncinaError, BenchmarkResponse>> StandardResilience_WithRetry()
    {
        return await _EncinaWithRetry.Send(_request);
    }

    [Benchmark]
    public async Task<BenchmarkResponse> StandardResilience_MultipleSequentialRequests()
    {
        BenchmarkResponse? response = null;
        for (int i = 0; i < 10; i++)
        {
            var result = await _EncinaWithResilience.Send(_request);
            response = result.Match(
                Right: r => r,
                Left: _ => throw new InvalidOperationException("Should not fail")
            );
        }
        return response!;
    }

    [Benchmark]
    public async Task<BenchmarkResponse[]> StandardResilience_ConcurrentRequests()
    {
        var tasks = Enumerable.Range(0, 10).Select(async _ =>
        {
            var result = await _EncinaWithResilience.Send(_request);
            return result.Match(
                Right: r => r,
                Left: _ => throw new InvalidOperationException("Should not fail")
            );
        });

        return await Task.WhenAll(tasks);
    }
}

// Benchmark request/response types
public record BenchmarkRequest : IRequest<BenchmarkResponse>
{
    public int Value { get; init; }
}

public record BenchmarkResponse
{
    public int Result { get; init; }
}

// Benchmark handlers
public class BenchmarkRequestHandler : IRequestHandler<BenchmarkRequest, BenchmarkResponse>
{
    public ValueTask<Either<EncinaError, BenchmarkResponse>> Handle(
        BenchmarkRequest request,
        IRequestContext context,
        CancellationToken cancellationToken)
    {
        return ValueTask.FromResult<Either<EncinaError, BenchmarkResponse>>(
            new BenchmarkResponse { Result = request.Value * 2 });
    }
}

public class RetryingHandler : IRequestHandler<BenchmarkRequest, BenchmarkResponse>
{
    private int _attemptCount = 0;

    public ValueTask<Either<EncinaError, BenchmarkResponse>> Handle(
        BenchmarkRequest request,
        IRequestContext context,
        CancellationToken cancellationToken)
    {
        _attemptCount++;

        // Fail first 2 attempts to trigger retry
        if (_attemptCount < 3)
        {
            _attemptCount = 0; // Reset for next benchmark iteration
            return ValueTask.FromResult<Either<EncinaError, BenchmarkResponse>>(
                EncinaError.New("Retry needed"));
        }

        _attemptCount = 0; // Reset for next benchmark iteration
        return ValueTask.FromResult<Either<EncinaError, BenchmarkResponse>>(
            new BenchmarkResponse { Result = request.Value * 2 });
    }
}
