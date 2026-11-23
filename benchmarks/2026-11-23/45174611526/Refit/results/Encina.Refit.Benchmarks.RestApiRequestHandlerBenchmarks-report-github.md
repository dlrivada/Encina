```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
Intel Xeon 6975P-C 2.84GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                      | Mean          | Error          | StdDev         | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|---------------------------- |--------------:|---------------:|---------------:|------:|--------:|-----:|-------:|----------:|------------:|
| EncinaRefitCall             |            NA |             NA |             NA |     ? |       ? |    ? |     NA |        NA |           ? |
| EncinaRefitCall_Sequential5 |      7.506 μs |       4.344 μs |      0.2381 μs | 0.001 |    0.00 |    1 | 0.1068 |   8.83 KB |        0.94 |
| EncinaRefitCall_Batch10     |     17.795 μs |       5.333 μs |      0.2923 μs | 0.002 |    0.00 |    2 | 0.2136 |  18.42 KB |        1.95 |
| DirectRefitCall_Baseline    | 10,301.963 μs |  11,922.876 μs |    653.5331 μs | 1.003 |    0.08 |    3 |      - |   9.43 KB |        1.00 |
| DirectRefitCall_Batch10     | 20,431.847 μs |  15,536.468 μs |    851.6062 μs | 1.989 |    0.13 |    4 |      - |  93.51 KB |        9.92 |
| DirectRefitCall_Sequential5 | 74,730.185 μs | 233,088.161 μs | 12,776.3487 μs | 7.273 |    1.15 |    5 |      - |  45.88 KB |        4.87 |

Benchmarks with issues:
  RestApiRequestHandlerBenchmarks.EncinaRefitCall: ShortRun(IterationCount=3, LaunchCount=1, WarmupCount=3)
