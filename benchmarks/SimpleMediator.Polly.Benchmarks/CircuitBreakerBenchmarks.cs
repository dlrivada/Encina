using BenchmarkDotNet.Attributes;
using LanguageExt;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using static LanguageExt.Prelude;

namespace SimpleMediator.Polly.Benchmarks;

[MemoryDiagnoser]
[SimpleJob(warmupCount: 3, iterationCount: 5)]
public class CircuitBreakerBenchmarks
{
    private IMediator _mediatorWithCircuitBreaker = null!;
    private IMediator _mediatorWithoutCircuitBreaker = null!;

    [GlobalSetup]
    public void Setup()
    {
        // With Circuit Breaker
        var servicesWithCB = new ServiceCollection();
        servicesWithCB.AddSimpleMediator(config => { });
        servicesWithCB.AddSimpleMediatorPolly();
        servicesWithCB.AddLogging();
        servicesWithCB.AddTransient<CircuitBreakerRequestHandler>();
        servicesWithCB.AddTransient<NoCircuitBreakerRequestHandler>();
        _mediatorWithCircuitBreaker = servicesWithCB.BuildServiceProvider().GetRequiredService<IMediator>();

        // Without Circuit Breaker
        var servicesWithoutCB = new ServiceCollection();
        servicesWithoutCB.AddSimpleMediator(config => { });
        servicesWithoutCB.AddLogging();
        servicesWithoutCB.AddTransient<NoCircuitBreakerRequestHandler>();
        _mediatorWithoutCircuitBreaker = servicesWithoutCB.BuildServiceProvider().GetRequiredService<IMediator>();
    }

    [Benchmark(Baseline = true)]
    public async Task NoCircuitBreakerAttribute_Baseline()
    {
        var request = new NoCircuitBreakerRequest();
        _ = await _mediatorWithoutCircuitBreaker.Send(request);
    }

    [Benchmark]
    public async Task WithCircuitBreakerAttribute_ClosedState()
    {
        var request = new CircuitBreakerRequest();
        _ = await _mediatorWithCircuitBreaker.Send(request);
    }

    // Test types
    [CircuitBreaker(FailureThreshold = 5, MinimumThroughput = 10)]
    private sealed record CircuitBreakerRequest : IRequest<string>;

    private record NoCircuitBreakerRequest : IRequest<string>;

    private sealed class CircuitBreakerRequestHandler : IRequestHandler<CircuitBreakerRequest, string>
    {
        public Task<Either<MediatorError, string>> Handle(CircuitBreakerRequest request, CancellationToken cancellationToken)
        {
            return Task.FromResult(Right<MediatorError, string>("Success"));
        }
    }

    private sealed class NoCircuitBreakerRequestHandler : IRequestHandler<NoCircuitBreakerRequest, string>
    {
        public Task<Either<MediatorError, string>> Handle(NoCircuitBreakerRequest request, CancellationToken cancellationToken)
        {
            return Task.FromResult(Right<MediatorError, string>("Success"));
        }
    }
}
