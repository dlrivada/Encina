using BenchmarkDotNet.Attributes;
using LanguageExt;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using static LanguageExt.Prelude;

namespace Encina.Polly.Benchmarks;

[MemoryDiagnoser]
[SimpleJob(warmupCount: 3, iterationCount: 5)]
public class RetryBenchmarks
{
    private IEncina _EncinaWithRetry = null!;
    private IEncina _EncinaWithoutRetry = null!;

    [GlobalSetup]
    public void Setup()
    {
        // With Retry
        var servicesWithRetry = new ServiceCollection();
        servicesWithRetry.AddEncina(config => { });
        servicesWithRetry.AddEncinaPolly();
        servicesWithRetry.AddLogging();
        servicesWithRetry.AddTransient<RetryRequestHandler>();
        servicesWithRetry.AddTransient<NoRetryRequestHandler>();
        _EncinaWithRetry = servicesWithRetry.BuildServiceProvider().GetRequiredService<IEncina>();

        // Without Retry
        var servicesWithoutRetry = new ServiceCollection();
        servicesWithoutRetry.AddEncina(config => { });
        servicesWithoutRetry.AddLogging();
        servicesWithoutRetry.AddTransient<NoRetryRequestHandler>();
        _EncinaWithoutRetry = servicesWithoutRetry.BuildServiceProvider().GetRequiredService<IEncina>();
    }

    [Benchmark(Baseline = true)]
    public async Task NoRetryAttribute_Baseline()
    {
        var request = new NoRetryRequest();
        _ = await _EncinaWithoutRetry.Send(request);
    }

    [Benchmark]
    public async Task WithRetryAttribute_NoActualRetries()
    {
        var request = new RetryRequest();
        _ = await _EncinaWithRetry.Send(request);
    }

    // Test types
    [Retry(MaxAttempts = 3, BaseDelayMs = 10)]
    private sealed record RetryRequest : IRequest<string>;

    private record NoRetryRequest : IRequest<string>;

    private sealed class RetryRequestHandler : IRequestHandler<RetryRequest, string>
    {
        public Task<Either<EncinaError, string>> Handle(RetryRequest request, CancellationToken cancellationToken)
        {
            return Task.FromResult(Right<EncinaError, string>("Success"));
        }
    }

    private sealed class NoRetryRequestHandler : IRequestHandler<NoRetryRequest, string>
    {
        public Task<Either<EncinaError, string>> Handle(NoRetryRequest request, CancellationToken cancellationToken)
        {
            return Task.FromResult(Right<EncinaError, string>("Success"));
        }
    }
}
