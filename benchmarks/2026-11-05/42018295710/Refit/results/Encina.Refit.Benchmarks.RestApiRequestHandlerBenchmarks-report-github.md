```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
Intel Xeon 6973P-C 3.73GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                      | Mean          | Error          | StdDev        | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|---------------------------- |--------------:|---------------:|--------------:|------:|--------:|-----:|-------:|----------:|------------:|
| EncinaRefitCall             |            NA |             NA |            NA |     ? |       ? |    ? |     NA |        NA |           ? |
| EncinaRefitCall_Sequential5 |      7.207 μs |      0.8167 μs |     0.0448 μs | 0.000 |    0.00 |    1 | 0.1068 |   8.79 KB |        0.93 |
| EncinaRefitCall_Batch10     |     18.821 μs |     17.1614 μs |     0.9407 μs | 0.001 |    0.00 |    2 | 0.2136 |  18.34 KB |        1.94 |
| DirectRefitCall_Baseline    | 16,465.490 μs |  4,207.5241 μs |   230.6286 μs | 1.000 |    0.02 |    3 |      - |   9.48 KB |        1.00 |
| DirectRefitCall_Batch10     | 27,849.742 μs | 36,712.0619 μs | 2,012.3120 μs | 1.692 |    0.11 |    4 |      - |   93.5 KB |        9.87 |
| DirectRefitCall_Sequential5 | 87,409.464 μs | 54,153.5775 μs | 2,968.3403 μs | 5.309 |    0.17 |    5 |      - |  45.83 KB |        4.84 |

Benchmarks with issues:
  RestApiRequestHandlerBenchmarks.EncinaRefitCall: ShortRun(IterationCount=3, LaunchCount=1, WarmupCount=3)
