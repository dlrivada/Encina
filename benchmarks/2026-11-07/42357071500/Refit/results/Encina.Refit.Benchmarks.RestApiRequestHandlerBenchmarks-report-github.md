```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
INTEL XEON PLATINUM 8573C 3.10GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                      | Mean          | Error          | StdDev        | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|---------------------------- |--------------:|---------------:|--------------:|------:|--------:|-----:|-------:|----------:|------------:|
| EncinaRefitCall             |            NA |             NA |            NA |     ? |       ? |    ? |     NA |        NA |           ? |
| EncinaRefitCall_Sequential5 |      9.717 μs |       6.681 μs |     0.3662 μs | 0.001 |    0.00 |    1 | 0.1068 |   8.79 KB |        0.93 |
| EncinaRefitCall_Batch10     |     26.214 μs |      41.300 μs |     2.2638 μs | 0.002 |    0.00 |    2 | 0.2136 |  18.34 KB |        1.95 |
| DirectRefitCall_Baseline    | 14,999.093 μs |   4,015.199 μs |   220.0866 μs | 1.000 |    0.02 |    3 |      - |   9.42 KB |        1.00 |
| DirectRefitCall_Batch10     | 17,238.854 μs |   3,841.057 μs |   210.5413 μs | 1.149 |    0.02 |    3 |      - |  93.44 KB |        9.92 |
| DirectRefitCall_Sequential5 | 76,561.996 μs | 162,129.025 μs | 8,886.8390 μs | 5.105 |    0.52 |    4 |      - |  45.77 KB |        4.86 |

Benchmarks with issues:
  RestApiRequestHandlerBenchmarks.EncinaRefitCall: ShortRun(IterationCount=3, LaunchCount=1, WarmupCount=3)
