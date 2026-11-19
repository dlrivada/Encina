```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
INTEL XEON PLATINUM 8573C 3.50GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                      | Mean           | Error         | StdDev        | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|---------------------------- |---------------:|--------------:|--------------:|------:|--------:|-----:|-------:|----------:|------------:|
| DirectRefitCall_Baseline    |             NA |            NA |            NA |     ? |       ? |    ? |     NA |        NA |           ? |
| EncinaRefitCall             |             NA |            NA |            NA |     ? |       ? |    ? |     NA |        NA |           ? |
| DirectRefitCall_Batch10     |             NA |            NA |            NA |     ? |       ? |    ? |     NA |        NA |           ? |
| EncinaRefitCall_Sequential5 |       9.005 μs |      5.547 μs |     0.3040 μs |     ? |       ? |    1 | 0.1068 |   8.83 KB |           ? |
| EncinaRefitCall_Batch10     |      24.599 μs |    101.923 μs |     5.5867 μs |     ? |       ? |    2 | 0.2136 |  18.42 KB |           ? |
| DirectRefitCall_Sequential5 | 124,478.797 μs | 33,128.511 μs | 1,815.8855 μs |     ? |       ? |    3 |      - |  46.14 KB |           ? |

Benchmarks with issues:
  RestApiRequestHandlerBenchmarks.DirectRefitCall_Baseline: ShortRun(IterationCount=3, LaunchCount=1, WarmupCount=3)
  RestApiRequestHandlerBenchmarks.EncinaRefitCall: ShortRun(IterationCount=3, LaunchCount=1, WarmupCount=3)
  RestApiRequestHandlerBenchmarks.DirectRefitCall_Batch10: ShortRun(IterationCount=3, LaunchCount=1, WarmupCount=3)
