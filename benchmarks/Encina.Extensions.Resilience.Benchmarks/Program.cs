using BenchmarkDotNet.Running;

namespace Encina.Extensions.Resilience.Benchmarks;

public class Program
{
    public static void Main(string[] args)
    {
        BenchmarkRunner.Run<StandardResilienceBenchmarks>(args);
    }
}
