```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]   : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                      | Mean          | Error          | StdDev        | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|---------------------------- |--------------:|---------------:|--------------:|------:|--------:|-----:|-------:|----------:|------------:|
| EncinaRefitCall             |            NA |             NA |            NA |     ? |       ? |    ? |     NA |        NA |           ? |
| EncinaRefitCall_Sequential5 |      4.187 μs |      0.9219 μs |     0.0505 μs | 0.001 |    0.00 |    1 | 0.2975 |   4.92 KB |        0.50 |
| EncinaRefitCall_Batch10     |     14.730 μs |     33.5884 μs |     1.8411 μs | 0.002 |    0.00 |    2 | 0.6409 |  10.61 KB |        1.08 |
| DirectRefitCall_Baseline    |  6,299.891 μs |  9,343.5718 μs |   512.1527 μs | 1.005 |    0.10 |    3 |      - |   9.83 KB |        1.00 |
| DirectRefitCall_Batch10     | 10,782.052 μs |  6,337.9292 μs |   347.4033 μs | 1.719 |    0.13 |    4 |      - |  97.79 KB |        9.94 |
| DirectRefitCall_Sequential5 | 44,901.568 μs | 28,856.2236 μs | 1,581.7070 μs | 7.160 |    0.57 |    5 |      - |  47.71 KB |        4.85 |

Benchmarks with issues:
  RestApiRequestHandlerBenchmarks.EncinaRefitCall: ShortRun(IterationCount=3, LaunchCount=1, WarmupCount=3)
