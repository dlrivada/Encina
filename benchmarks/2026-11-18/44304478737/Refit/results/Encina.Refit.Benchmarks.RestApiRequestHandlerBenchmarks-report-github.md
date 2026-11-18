```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 3.24GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                      | Mean          | Error          | StdDev       | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|---------------------------- |--------------:|---------------:|-------------:|------:|--------:|-----:|-------:|----------:|------------:|
| EncinaRefitCall             |            NA |             NA |           NA |     ? |       ? |    ? |     NA |        NA |           ? |
| EncinaRefitCall_Sequential5 |      12.22 μs |       1.471 μs |     0.081 μs | 0.000 |    0.00 |    1 | 0.5341 |   8.83 KB |        0.93 |
| EncinaRefitCall_Batch10     |      27.70 μs |       5.815 μs |     0.319 μs | 0.001 |    0.00 |    2 | 1.0986 |  18.42 KB |        1.95 |
| DirectRefitCall_Baseline    |  24,993.33 μs |  16,971.292 μs |   930.254 μs | 1.001 |    0.05 |    3 |      - |   9.47 KB |        1.00 |
| DirectRefitCall_Batch10     |  33,480.63 μs | 120,184.169 μs | 6,587.700 μs | 1.341 |    0.23 |    4 |      - |  94.08 KB |        9.94 |
| DirectRefitCall_Sequential5 | 108,161.40 μs |  58,433.312 μs | 3,202.927 μs | 4.332 |    0.18 |    5 |      - |  46.33 KB |        4.89 |

Benchmarks with issues:
  RestApiRequestHandlerBenchmarks.EncinaRefitCall: ShortRun(IterationCount=3, LaunchCount=1, WarmupCount=3)
