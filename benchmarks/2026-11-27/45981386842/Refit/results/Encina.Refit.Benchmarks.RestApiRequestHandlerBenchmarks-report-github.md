```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V74 3.66GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                      | Mean         | Error          | StdDev       | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|---------------------------- |-------------:|---------------:|-------------:|------:|--------:|-----:|-------:|----------:|------------:|
| EncinaRefitCall             |           NA |             NA |           NA |     ? |       ? |    ? |     NA |        NA |           ? |
| EncinaRefitCall_Sequential5 |     10.61 μs |       1.728 μs |     0.095 μs | 0.001 |    0.00 |    1 | 0.5341 |   8.83 KB |        0.93 |
| EncinaRefitCall_Batch10     |     28.56 μs |      58.558 μs |     3.210 μs | 0.002 |    0.00 |    2 | 1.0986 |  18.42 KB |        1.95 |
| DirectRefitCall_Baseline    | 18,076.60 μs |  38,212.520 μs | 2,094.557 μs | 1.009 |    0.15 |    3 |      - |   9.46 KB |        1.00 |
| DirectRefitCall_Batch10     | 21,950.66 μs |  33,662.340 μs | 1,845.146 μs | 1.226 |    0.16 |    4 |      - |  93.53 KB |        9.88 |
| DirectRefitCall_Sequential5 | 80,264.20 μs | 127,665.576 μs | 6,997.781 μs | 4.482 |    0.58 |    5 |      - |  45.89 KB |        4.85 |

Benchmarks with issues:
  RestApiRequestHandlerBenchmarks.EncinaRefitCall: ShortRun(IterationCount=3, LaunchCount=1, WarmupCount=3)
