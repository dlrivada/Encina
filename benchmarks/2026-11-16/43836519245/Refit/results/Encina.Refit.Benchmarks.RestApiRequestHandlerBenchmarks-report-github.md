```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
Intel Xeon 6975P-C 2.70GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                      | Mean          | Error         | StdDev        | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|---------------------------- |--------------:|--------------:|--------------:|------:|--------:|-----:|-------:|----------:|------------:|
| EncinaRefitCall             |            NA |            NA |            NA |     ? |       ? |    ? |     NA |        NA |           ? |
| EncinaRefitCall_Sequential5 |      7.666 μs |      1.205 μs |     0.0660 μs | 0.001 |    0.00 |    1 | 0.1068 |   8.83 KB |        0.94 |
| EncinaRefitCall_Batch10     |     18.225 μs |      7.041 μs |     0.3860 μs | 0.002 |    0.00 |    2 | 0.2136 |  18.42 KB |        1.95 |
| DirectRefitCall_Baseline    |  8,451.514 μs |  6,726.070 μs |   368.6786 μs | 1.001 |    0.05 |    3 |      - |   9.43 KB |        1.00 |
| DirectRefitCall_Batch10     | 15,486.084 μs | 57,495.880 μs | 3,151.5432 μs | 1.835 |    0.33 |    4 |      - |  93.62 KB |        9.92 |
| DirectRefitCall_Sequential5 | 40,967.553 μs | 46,348.427 μs | 2,540.5137 μs | 4.854 |    0.32 |    5 |      - |  47.89 KB |        5.08 |

Benchmarks with issues:
  RestApiRequestHandlerBenchmarks.EncinaRefitCall: ShortRun(IterationCount=3, LaunchCount=1, WarmupCount=3)
