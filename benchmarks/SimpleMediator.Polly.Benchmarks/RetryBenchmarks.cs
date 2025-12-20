using BenchmarkDotNet.Attributes;
using LanguageExt;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using static LanguageExt.Prelude;

namespace SimpleMediator.Polly.Benchmarks;

[MemoryDiagnoser]
[SimpleJob(warmupCount: 3, iterationCount: 5)]
public class RetryBenchmarks
{
    private IMediator _mediatorWithRetry = null!;
    private IMediator _mediatorWithoutRetry = null!;

    [GlobalSetup]
    public void Setup()
    {
        // With Retry
        var servicesWithRetry = new ServiceCollection();
        servicesWithRetry.AddSimpleMediator(config => { });
        servicesWithRetry.AddSimpleMediatorPolly();
        servicesWithRetry.AddLogging();
        servicesWithRetry.AddTransient<RetryRequestHandler>();
        servicesWithRetry.AddTransient<NoRetryRequestHandler>();
        _mediatorWithRetry = servicesWithRetry.BuildServiceProvider().GetRequiredService<IMediator>();

        // Without Retry
        var servicesWithoutRetry = new ServiceCollection();
        servicesWithoutRetry.AddSimpleMediator(config => { });
        servicesWithoutRetry.AddLogging();
        servicesWithoutRetry.AddTransient<NoRetryRequestHandler>();
        _mediatorWithoutRetry = servicesWithoutRetry.BuildServiceProvider().GetRequiredService<IMediator>();
    }

    [Benchmark(Baseline = true)]
    public async Task NoRetryAttribute_Baseline()
    {
        var request = new NoRetryRequest();
        _ = await _mediatorWithoutRetry.Send(request);
    }

    [Benchmark]
    public async Task WithRetryAttribute_NoActualRetries()
    {
        var request = new RetryRequest();
        _ = await _mediatorWithRetry.Send(request);
    }

    // Test types
    [Retry(MaxAttempts = 3, BaseDelayMs = 10)]
    private sealed record RetryRequest : IRequest<string>;

    private record NoRetryRequest : IRequest<string>;

    private sealed class RetryRequestHandler : IRequestHandler<RetryRequest, string>
    {
        public Task<Either<MediatorError, string>> Handle(RetryRequest request, CancellationToken cancellationToken)
        {
            return Task.FromResult(Right<MediatorError, string>("Success"));
        }
    }

    private sealed class NoRetryRequestHandler : IRequestHandler<NoRetryRequest, string>
    {
        public Task<Either<MediatorError, string>> Handle(NoRetryRequest request, CancellationToken cancellationToken)
        {
            return Task.FromResult(Right<MediatorError, string>("Success"));
        }
    }
}
