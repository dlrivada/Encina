```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 3.16GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                      | Mean         | Error         | StdDev     | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|---------------------------- |-------------:|--------------:|-----------:|------:|--------:|-----:|-------:|----------:|------------:|
| EncinaRefitCall             |           NA |            NA |         NA |     ? |       ? |    ? |     NA |        NA |           ? |
| EncinaRefitCall_Sequential5 |     12.66 μs |      1.367 μs |   0.075 μs | 0.002 |    0.00 |    1 | 0.5341 |   8.83 KB |        0.94 |
| EncinaRefitCall_Batch10     |     29.77 μs |     40.489 μs |   2.219 μs | 0.004 |    0.00 |    2 | 1.0986 |  18.42 KB |        1.96 |
| DirectRefitCall_Baseline    |  7,701.93 μs |  6,191.575 μs | 339.381 μs | 1.001 |    0.05 |    3 |      - |   9.41 KB |        1.00 |
| DirectRefitCall_Batch10     | 10,977.09 μs | 14,262.926 μs | 781.799 μs | 1.427 |    0.10 |    4 |      - |  93.36 KB |        9.92 |
| DirectRefitCall_Sequential5 | 34,994.88 μs | 11,883.705 μs | 651.386 μs | 4.549 |    0.19 |    5 |      - |  45.67 KB |        4.85 |

Benchmarks with issues:
  RestApiRequestHandlerBenchmarks.EncinaRefitCall: ShortRun(IterationCount=3, LaunchCount=1, WarmupCount=3)
