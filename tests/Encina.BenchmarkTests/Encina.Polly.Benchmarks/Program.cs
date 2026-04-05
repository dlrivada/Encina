using System.Reflection;
using BenchmarkDotNet.Running;

namespace Encina.Polly.Benchmarks;

public class Program
{
    public static void Main(string[] args)
    {
        // Use BenchmarkSwitcher to honor --filter, --list, --job and other CLI args.
        // See CLAUDE.md "BenchmarkDotNet Guidelines" and ADR-025.
        BenchmarkSwitcher.FromAssembly(Assembly.GetExecutingAssembly()).Run(args);
    }
}
