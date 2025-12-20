using BenchmarkDotNet.Running;

namespace SimpleMediator.Polly.Benchmarks;

public class Program
{
    public static void Main(string[] args)
    {
        BenchmarkRunner.Run<RetryBenchmarks>(args: args);
        BenchmarkRunner.Run<CircuitBreakerBenchmarks>(args: args);
    }
}
