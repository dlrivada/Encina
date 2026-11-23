```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
Intel Xeon 6975P-C 2.70GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                      | Mean           | Error          | StdDev        | Ratio  | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|---------------------------- |---------------:|---------------:|--------------:|-------:|--------:|-----:|-------:|----------:|------------:|
| EncinaRefitCall             |             NA |             NA |            NA |      ? |       ? |    ? |     NA |        NA |           ? |
| EncinaRefitCall_Sequential5 |       7.669 μs |       1.315 μs |     0.0721 μs |  0.001 |    0.00 |    1 | 0.1068 |   8.83 KB |        0.94 |
| EncinaRefitCall_Batch10     |      18.094 μs |       1.905 μs |     0.1044 μs |  0.001 |    0.00 |    2 | 0.2136 |  18.42 KB |        1.95 |
| DirectRefitCall_Baseline    |  14,344.817 μs |  11,229.333 μs |   615.5176 μs |  1.001 |    0.05 |    3 |      - |   9.43 KB |        1.00 |
| DirectRefitCall_Batch10     |  73,494.208 μs |  56,007.544 μs | 3,069.9625 μs |  5.130 |    0.27 |    4 |      - |  94.02 KB |        9.97 |
| DirectRefitCall_Sequential5 | 349,855.460 μs | 115,360.974 μs | 6,323.3243 μs | 24.420 |    1.00 |    5 |      - |  48.26 KB |        5.12 |

Benchmarks with issues:
  RestApiRequestHandlerBenchmarks.EncinaRefitCall: ShortRun(IterationCount=3, LaunchCount=1, WarmupCount=3)
