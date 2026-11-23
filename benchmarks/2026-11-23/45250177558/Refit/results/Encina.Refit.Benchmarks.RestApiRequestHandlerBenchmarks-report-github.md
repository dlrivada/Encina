```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                      | Mean          | Error         | StdDev     | Ratio  | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|---------------------------- |--------------:|--------------:|-----------:|-------:|--------:|-----:|-------:|----------:|------------:|
| EncinaRefitCall             |            NA |            NA |         NA |      ? |       ? |    ? |     NA |        NA |           ? |
| EncinaRefitCall_Sequential5 |      12.16 μs |      1.362 μs |   0.075 μs |  0.001 |    0.00 |    1 | 0.5341 |   8.83 KB |        0.93 |
| EncinaRefitCall_Batch10     |      30.89 μs |     33.217 μs |   1.821 μs |  0.001 |    0.00 |    2 | 1.0986 |  18.42 KB |        1.93 |
| DirectRefitCall_Baseline    |  22,650.59 μs |  8,309.761 μs | 455.486 μs |  1.000 |    0.02 |    3 |      - |   9.53 KB |        1.00 |
| DirectRefitCall_Batch10     | 168,740.70 μs |  7,717.287 μs | 423.011 μs |  7.452 |    0.13 |    4 |      - |  95.67 KB |       10.04 |
| DirectRefitCall_Sequential5 | 507,249.36 μs | 14,512.777 μs | 795.494 μs | 22.401 |    0.40 |    5 |      - |  48.28 KB |        5.07 |

Benchmarks with issues:
  RestApiRequestHandlerBenchmarks.EncinaRefitCall: ShortRun(IterationCount=3, LaunchCount=1, WarmupCount=3)
