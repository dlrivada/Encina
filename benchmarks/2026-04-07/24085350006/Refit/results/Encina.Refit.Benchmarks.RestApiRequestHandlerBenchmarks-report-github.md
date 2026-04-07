```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
Intel Xeon Platinum 8370C CPU 2.80GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]   : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                      | Mean           | Error           | StdDev         | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|---------------------------- |---------------:|----------------:|---------------:|------:|--------:|-----:|-------:|----------:|------------:|
| EncinaRefitCall             |             NA |              NA |             NA |     ? |       ? |    ? |     NA |        NA |           ? |
| EncinaRefitCall_Sequential5 |       4.227 μs |       0.2695 μs |      0.0148 μs | 0.000 |    0.00 |    1 | 0.1984 |   4.92 KB |        0.50 |
| EncinaRefitCall_Batch10     |      11.651 μs |       1.5866 μs |      0.0870 μs | 0.000 |    0.00 |    2 | 0.4272 |  10.61 KB |        1.07 |
| DirectRefitCall_Baseline    |  24,793.506 μs |   3,831.1335 μs |    209.9974 μs | 1.000 |    0.01 |    3 |      - |    9.9 KB |        1.00 |
| DirectRefitCall_Batch10     |  31,313.937 μs |  72,228.9044 μs |  3,959.1100 μs | 1.263 |    0.14 |    4 |      - |  97.76 KB |        9.87 |
| DirectRefitCall_Sequential5 | 131,736.984 μs | 268,497.5208 μs | 14,717.2552 μs | 5.314 |    0.52 |    5 |      - |  48.27 KB |        4.88 |

Benchmarks with issues:
  RestApiRequestHandlerBenchmarks.EncinaRefitCall: ShortRun(IterationCount=3, LaunchCount=1, WarmupCount=3)
