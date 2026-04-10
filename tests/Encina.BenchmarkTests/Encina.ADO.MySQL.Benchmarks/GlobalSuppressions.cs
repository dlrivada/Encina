using System.Diagnostics.CodeAnalysis;

// BenchmarkDotNet manages the benchmark class lifecycle via [GlobalSetup] / [GlobalCleanup].
// Classes own disposable fields (container, connection) that are released in [GlobalCleanup],
// so making the benchmark classes themselves IDisposable adds no value and would conflict with
// BDN's reflection-based instantiation. Suppress CA1001 for the entire benchmarks folder.
[assembly: SuppressMessage(
    "Design",
    "CA1001:Types that own disposable fields should be disposable",
    Justification = "BenchmarkDotNet manages lifecycle via [GlobalCleanup]; IDisposable is unused.",
    Scope = "namespaceanddescendants",
    Target = "~N:Encina.ADO.MySQL.Benchmarks.Benchmarks")]
