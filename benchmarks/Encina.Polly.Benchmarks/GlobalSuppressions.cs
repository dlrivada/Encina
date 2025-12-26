using System.Diagnostics.CodeAnalysis;

// Suppress sealing warnings for benchmark test types that are used only in benchmarks
[assembly: SuppressMessage("Performance", "CA1852:Seal internal types", Justification = "Benchmark test types don't need sealing")]
