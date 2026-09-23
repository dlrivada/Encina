```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                      | Mean          | Error          | StdDev       | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|---------------------------- |--------------:|---------------:|-------------:|------:|--------:|-----:|-------:|----------:|------------:|
| EncinaRefitCall             |            NA |             NA |           NA |     ? |       ? |    ? |     NA |        NA |           ? |
| EncinaRefitCall_Sequential5 |      10.50 μs |       0.284 μs |     0.016 μs | 0.000 |    0.00 |    1 | 0.4425 |   7.42 KB |        0.75 |
| EncinaRefitCall_Batch10     |      24.54 μs |      17.861 μs |     0.979 μs | 0.001 |    0.00 |    2 | 0.9460 |  15.61 KB |        1.58 |
| DirectRefitCall_Baseline    |  24,531.13 μs |  12,394.819 μs |   679.402 μs | 1.001 |    0.03 |    3 |      - |   9.89 KB |        1.00 |
| DirectRefitCall_Batch10     |  33,161.34 μs |  13,497.262 μs |   739.830 μs | 1.353 |    0.04 |    4 |      - |  97.89 KB |        9.90 |
| DirectRefitCall_Sequential5 | 148,417.08 μs | 108,202.977 μs | 5,930.970 μs | 6.053 |    0.26 |    5 |      - |  48.24 KB |        4.88 |

Benchmarks with issues:
  RestApiRequestHandlerBenchmarks.EncinaRefitCall: ShortRun(IterationCount=3, LaunchCount=1, WarmupCount=3)
