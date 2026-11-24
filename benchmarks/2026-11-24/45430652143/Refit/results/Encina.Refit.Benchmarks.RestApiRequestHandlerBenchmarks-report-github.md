```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V74 3.69GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                      | Mean          | Error          | StdDev        | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|---------------------------- |--------------:|---------------:|--------------:|------:|--------:|-----:|-------:|----------:|------------:|
| EncinaRefitCall             |            NA |             NA |            NA |     ? |       ? |    ? |     NA |        NA |           ? |
| EncinaRefitCall_Sequential5 |      10.53 μs |       0.678 μs |      0.037 μs | 0.000 |    0.00 |    1 | 0.5341 |   8.83 KB |        0.92 |
| EncinaRefitCall_Batch10     |      26.83 μs |      43.923 μs |      2.408 μs | 0.001 |    0.00 |    2 | 1.0986 |  18.42 KB |        1.93 |
| DirectRefitCall_Baseline    |  21,324.74 μs |  35,844.444 μs |  1,964.755 μs | 1.005 |    0.11 |    3 |      - |   9.56 KB |        1.00 |
| DirectRefitCall_Batch10     |  45,054.03 μs |   8,561.560 μs |    469.288 μs | 2.124 |    0.16 |    4 |      - |  93.89 KB |        9.82 |
| DirectRefitCall_Sequential5 | 120,774.71 μs | 265,098.278 μs | 14,530.931 μs | 5.694 |    0.74 |    5 |      - |  45.85 KB |        4.79 |

Benchmarks with issues:
  RestApiRequestHandlerBenchmarks.EncinaRefitCall: ShortRun(IterationCount=3, LaunchCount=1, WarmupCount=3)
