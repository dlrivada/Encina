using BenchmarkDotNet.Running;

namespace Encina.EntityFrameworkCore.Benchmarks;

/// <summary>
/// Entry point for EntityFrameworkCore benchmarks.
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
