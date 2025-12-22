using BenchmarkDotNet.Attributes;
using LanguageExt;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using static LanguageExt.Prelude;

namespace Encina.Polly.Benchmarks;

[MemoryDiagnoser]
[SimpleJob(warmupCount: 3, iterationCount: 5)]
public class CircuitBreakerBenchmarks
{
    private IEncina _EncinaWithCircuitBreaker = null!;
    private IEncina _EncinaWithoutCircuitBreaker = null!;

    [GlobalSetup]
    public void Setup()
    {
        // With Circuit Breaker
        var servicesWithCB = new ServiceCollection();
        servicesWithCB.AddEncina(config => { });
        servicesWithCB.AddEncinaPolly();
        servicesWithCB.AddLogging();
        servicesWithCB.AddTransient<CircuitBreakerRequestHandler>();
        servicesWithCB.AddTransient<NoCircuitBreakerRequestHandler>();
        _EncinaWithCircuitBreaker = servicesWithCB.BuildServiceProvider().GetRequiredService<IEncina>();

        // Without Circuit Breaker
        var servicesWithoutCB = new ServiceCollection();
        servicesWithoutCB.AddEncina(config => { });
        servicesWithoutCB.AddLogging();
        servicesWithoutCB.AddTransient<NoCircuitBreakerRequestHandler>();
        _EncinaWithoutCircuitBreaker = servicesWithoutCB.BuildServiceProvider().GetRequiredService<IEncina>();
    }

    [Benchmark(Baseline = true)]
    public async Task NoCircuitBreakerAttribute_Baseline()
    {
        var request = new NoCircuitBreakerRequest();
        _ = await _EncinaWithoutCircuitBreaker.Send(request);
    }

    [Benchmark]
    public async Task WithCircuitBreakerAttribute_ClosedState()
    {
        var request = new CircuitBreakerRequest();
        _ = await _EncinaWithCircuitBreaker.Send(request);
    }

    // Test types
    [CircuitBreaker(FailureThreshold = 5, MinimumThroughput = 10)]
    private sealed record CircuitBreakerRequest : IRequest<string>;

    private record NoCircuitBreakerRequest : IRequest<string>;

    private sealed class CircuitBreakerRequestHandler : IRequestHandler<CircuitBreakerRequest, string>
    {
        public Task<Either<EncinaError, string>> Handle(CircuitBreakerRequest request, CancellationToken cancellationToken)
        {
            return Task.FromResult(Right<EncinaError, string>("Success"));
        }
    }

    private sealed class NoCircuitBreakerRequestHandler : IRequestHandler<NoCircuitBreakerRequest, string>
    {
        public Task<Either<EncinaError, string>> Handle(NoCircuitBreakerRequest request, CancellationToken cancellationToken)
        {
            return Task.FromResult(Right<EncinaError, string>("Success"));
        }
    }
}
