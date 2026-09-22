```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                      | Mean           | Error          | StdDev        | Ratio  | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|---------------------------- |---------------:|---------------:|--------------:|-------:|--------:|-----:|-------:|----------:|------------:|
| EncinaRefitCall             |             NA |             NA |            NA |      ? |       ? |    ? |     NA |        NA |           ? |
| EncinaRefitCall_Sequential5 |       4.197 μs |      0.4470 μs |     0.0245 μs |  0.000 |    0.00 |    1 | 0.2975 |   4.92 KB |        0.50 |
| EncinaRefitCall_Batch10     |      15.825 μs |     18.4748 μs |     1.0127 μs |  0.001 |    0.00 |    2 | 0.6409 |  10.61 KB |        1.08 |
| DirectRefitCall_Baseline    |  13,768.866 μs | 14,714.8173 μs |   806.5688 μs |  1.002 |    0.07 |    3 |      - |   9.84 KB |        1.00 |
| DirectRefitCall_Batch10     |  40,026.528 μs | 68,723.6138 μs | 3,766.9732 μs |  2.914 |    0.28 |    4 |      - |  97.82 KB |        9.94 |
| DirectRefitCall_Sequential5 | 142,690.653 μs | 21,872.7971 μs | 1,198.9218 μs | 10.388 |    0.55 |    5 |      - |  48.24 KB |        4.90 |

Benchmarks with issues:
  RestApiRequestHandlerBenchmarks.EncinaRefitCall: ShortRun(IterationCount=3, LaunchCount=1, WarmupCount=3)
