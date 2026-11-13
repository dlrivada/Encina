```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
Intel Xeon Platinum 8370C CPU 2.80GHz (Max: 2.79GHz), 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                      | Mean          | Error         | StdDev        | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|---------------------------- |--------------:|--------------:|--------------:|------:|--------:|-----:|-------:|----------:|------------:|
| EncinaRefitCall             |            NA |            NA |            NA |     ? |       ? |    ? |     NA |        NA |           ? |
| EncinaRefitCall_Sequential5 |      9.619 μs |      1.386 μs |     0.0760 μs | 0.001 |    0.00 |    1 | 0.3510 |   8.83 KB |        0.94 |
| EncinaRefitCall_Batch10     |     30.967 μs |    173.853 μs |     9.5295 μs | 0.002 |    0.00 |    2 | 0.7324 |  18.42 KB |        1.95 |
| DirectRefitCall_Baseline    | 13,740.583 μs | 13,992.392 μs |   766.9702 μs | 1.002 |    0.07 |    3 |      - |   9.43 KB |        1.00 |
| DirectRefitCall_Batch10     | 22,243.632 μs | 41,228.975 μs | 2,259.8993 μs | 1.622 |    0.16 |    4 |      - |  93.41 KB |        9.90 |
| DirectRefitCall_Sequential5 | 70,737.123 μs | 21,317.820 μs | 1,168.5016 μs | 5.158 |    0.25 |    5 |      - |  45.78 KB |        4.85 |

Benchmarks with issues:
  RestApiRequestHandlerBenchmarks.EncinaRefitCall: ShortRun(IterationCount=3, LaunchCount=1, WarmupCount=3)
