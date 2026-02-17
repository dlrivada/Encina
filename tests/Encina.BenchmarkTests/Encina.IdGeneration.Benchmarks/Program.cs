using BenchmarkDotNet.Running;

namespace Encina.IdGeneration.Benchmarks;

/// <summary>
/// Entry point for ID generation benchmarks.
/// </summary>
public class Program
{
    /// <summary>
    /// Main entry point for running benchmarks.
    /// </summary>
    /// <param name="args">Command line arguments passed to BenchmarkDotNet.</param>
    public static void Main(string[] args)
    {
        BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);
    }
}
