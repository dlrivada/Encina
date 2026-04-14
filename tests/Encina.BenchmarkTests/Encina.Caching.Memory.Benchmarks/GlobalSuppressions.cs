using System.Diagnostics.CodeAnalysis;

[assembly: SuppressMessage(
    "Design",
    "CA1001:Types that own disposable fields should be disposable",
    Justification = "BenchmarkDotNet manages lifecycle via [GlobalCleanup]; IDisposable is unused.",
    Scope = "namespaceanddescendants",
    Target = "~N:Encina.Caching.Memory.Benchmarks.Benchmarks")]
