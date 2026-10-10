```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
INTEL XEON PLATINUM 8573C 3.51GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                      | Mean          | Error          | StdDev      | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|---------------------------- |--------------:|---------------:|------------:|------:|--------:|-----:|-------:|----------:|------------:|
| EncinaRefitCall             |            NA |             NA |          NA |     ? |       ? |    ? |     NA |        NA |           ? |
| EncinaRefitCall_Sequential5 |      8.654 μs |      0.7372 μs |   0.0404 μs | 0.001 |    0.00 |    1 | 0.1068 |   8.79 KB |        0.93 |
| EncinaRefitCall_Batch10     |     30.286 μs |     47.1465 μs |   2.5843 μs | 0.002 |    0.00 |    2 | 0.1831 |  18.34 KB |        1.94 |
| DirectRefitCall_Baseline    | 14,345.715 μs | 12,295.4624 μs | 673.9558 μs | 1.002 |    0.06 |    3 |      - |   9.44 KB |        1.00 |
| DirectRefitCall_Batch10     | 18,278.111 μs |  2,475.8405 μs | 135.7092 μs | 1.276 |    0.05 |    4 |      - |  93.51 KB |        9.90 |
| DirectRefitCall_Sequential5 | 74,617.977 μs | 16,498.7275 μs | 904.3509 μs | 5.209 |    0.22 |    5 |      - |  45.98 KB |        4.87 |

Benchmarks with issues:
  RestApiRequestHandlerBenchmarks.EncinaRefitCall: ShortRun(IterationCount=3, LaunchCount=1, WarmupCount=3)
