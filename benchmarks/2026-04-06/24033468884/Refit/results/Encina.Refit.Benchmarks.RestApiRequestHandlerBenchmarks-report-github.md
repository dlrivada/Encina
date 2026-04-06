```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]    : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  MediumRun : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3

Job=MediumRun  IterationCount=15  LaunchCount=2  
WarmupCount=10  

```
| Method                      | Mean          | Error          | StdDev         | Median        | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|---------------------------- |--------------:|---------------:|---------------:|--------------:|------:|--------:|-----:|-------:|----------:|------------:|
| EncinaRefitCall             |            NA |             NA |             NA |            NA |     ? |       ? |    ? |     NA |        NA |           ? |
| EncinaRefitCall_Sequential5 |      4.526 μs |      0.0619 μs |      0.0907 μs |      4.475 μs | 0.000 |    0.00 |    1 | 0.2975 |   4.92 KB |        0.49 |
| EncinaRefitCall_Batch10     |     15.544 μs |      1.8420 μs |      2.7571 μs |     16.071 μs | 0.001 |    0.00 |    2 | 0.6409 |  10.61 KB |        1.07 |
| DirectRefitCall_Baseline    | 11,967.383 μs |  2,074.8683 μs |  3,041.3152 μs | 10,791.151 μs | 1.067 |    0.39 |    3 |      - |   9.94 KB |        1.00 |
| DirectRefitCall_Batch10     | 17,481.960 μs |  2,843.9016 μs |  4,078.6390 μs | 15,952.642 μs | 1.559 |    0.54 |    4 |      - |  98.29 KB |        9.88 |
| DirectRefitCall_Sequential5 | 58,873.075 μs | 10,427.3302 μs | 14,954.5666 μs | 61,073.673 μs | 5.249 |    1.89 |    5 |      - |  47.97 KB |        4.82 |

Benchmarks with issues:
  RestApiRequestHandlerBenchmarks.EncinaRefitCall: MediumRun(IterationCount=15, LaunchCount=2, WarmupCount=10)
