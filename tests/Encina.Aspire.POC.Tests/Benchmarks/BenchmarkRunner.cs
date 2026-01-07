using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Running;

namespace Encina.Aspire.POC.Tests.Benchmarks;

/// <summary>
/// Entry point for running benchmarks from command line.
/// </summary>
/// <remarks>
/// <para>
/// Run benchmarks with:
/// <code>
/// dotnet run -c Release --project tests/Encina.Aspire.POC.Tests -- --filter "*ContainerStartup*"
/// </code>
/// </para>
/// <para>
/// For quick testing (non-scientific):
/// <code>
/// dotnet run -c Release --project tests/Encina.Aspire.POC.Tests -- --job short
/// </code>
/// </para>
/// </remarks>
public static class BenchmarkRunner
{
    /// <summary>
    /// Runs benchmarks when project is executed directly.
    /// </summary>
    /// <param name="args">Command-line arguments passed to BenchmarkDotNet.</param>
    public static void RunBenchmarks(string[] args)
    {
        var config = DefaultConfig.Instance;

        BenchmarkSwitcher
            .FromAssembly(typeof(BenchmarkRunner).Assembly)
            .Run(args, config);
    }
}
