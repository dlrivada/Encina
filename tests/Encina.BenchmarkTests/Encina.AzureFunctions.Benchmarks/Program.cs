using System.Reflection;
using BenchmarkDotNet.Running;

// Use BenchmarkSwitcher to honor --filter, --list, --job and other CLI args.
// See CLAUDE.md "BenchmarkDotNet Guidelines" and ADR-025.
BenchmarkSwitcher.FromAssembly(Assembly.GetExecutingAssembly()).Run(args);
