```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 4.19GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                      | Mean           | Error         | StdDev        | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|---------------------------- |---------------:|--------------:|--------------:|------:|--------:|-----:|-------:|----------:|------------:|
| EncinaRefitCall             |             NA |            NA |            NA |     ? |       ? |    ? |     NA |        NA |           ? |
| EncinaRefitCall_Sequential5 |       8.094 μs |      6.029 μs |     0.3305 μs | 0.000 |    0.00 |    1 | 0.5341 |   8.79 KB |        0.93 |
| EncinaRefitCall_Batch10     |      20.732 μs |     31.357 μs |     1.7188 μs | 0.001 |    0.00 |    2 | 1.0986 |  18.34 KB |        1.94 |
| DirectRefitCall_Baseline    |  28,276.855 μs |  7,589.270 μs |   415.9935 μs | 1.000 |    0.02 |    3 |      - |   9.48 KB |        1.00 |
| DirectRefitCall_Batch10     |  59,631.603 μs | 43,032.309 μs | 2,358.7461 μs | 2.109 |    0.08 |    4 |      - |  93.69 KB |        9.89 |
| DirectRefitCall_Sequential5 | 240,299.297 μs | 54,598.237 μs | 2,992.7136 μs | 8.499 |    0.14 |    5 |      - |  46.35 KB |        4.89 |

Benchmarks with issues:
  RestApiRequestHandlerBenchmarks.EncinaRefitCall: ShortRun(IterationCount=3, LaunchCount=1, WarmupCount=3)
