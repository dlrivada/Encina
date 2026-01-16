using BenchmarkDotNet.Running;

namespace Encina.Polly.Benchmarks;

public class Program
{
    public static void Main(string[] args)
    {
        BenchmarkRunner.Run<RetryBenchmarks>(args: args);
        BenchmarkRunner.Run<CircuitBreakerBenchmarks>(args: args);
        BenchmarkRunner.Run<RateLimitingBenchmarks>(args: args);
        BenchmarkRunner.Run<RateLimitingMultiKeyBenchmarks>(args: args);
    }
}
