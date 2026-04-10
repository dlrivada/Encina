using BenchmarkDotNet.Running;

namespace Encina.Cdc.Debezium.Benchmarks;

/// <summary>
/// Entry point for Encina.Cdc.Debezium provider benchmarks.
/// </summary>
public static class Program
{
    /// <summary>
    /// Dispatches BenchmarkDotNet with command-line arguments so filters, jobs,
    /// and exporters can be selected at runtime.
    /// </summary>
    /// <param name="args">Command line arguments passed to BenchmarkDotNet.</param>
    public static void Main(string[] args)
    {
        BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);
    }
}
