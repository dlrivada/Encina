```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.70GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                      | Mean         | Error         | StdDev       | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|---------------------------- |-------------:|--------------:|-------------:|------:|--------:|-----:|-------:|----------:|------------:|
| EncinaRefitCall             |           NA |            NA |           NA |     ? |       ? |    ? |     NA |        NA |           ? |
| EncinaRefitCall_Sequential5 |     12.03 μs |      0.387 μs |     0.021 μs | 0.002 |    0.00 |    1 | 0.5341 |   8.75 KB |        0.93 |
| EncinaRefitCall_Batch10     |     28.34 μs |     18.315 μs |     1.004 μs | 0.004 |    0.00 |    2 | 1.0986 |  18.27 KB |        1.94 |
| DirectRefitCall_Baseline    |  7,748.98 μs |  2,083.204 μs |   114.187 μs | 1.000 |    0.02 |    3 |      - |   9.43 KB |        1.00 |
| DirectRefitCall_Batch10     | 11,740.61 μs |  7,365.320 μs |   403.718 μs | 1.515 |    0.05 |    4 |      - |  93.33 KB |        9.90 |
| DirectRefitCall_Sequential5 | 41,935.63 μs | 22,112.532 μs | 1,212.063 μs | 5.413 |    0.15 |    5 |      - |  45.63 KB |        4.84 |

Benchmarks with issues:
  RestApiRequestHandlerBenchmarks.EncinaRefitCall: ShortRun(IterationCount=3, LaunchCount=1, WarmupCount=3)
