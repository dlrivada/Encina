```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
Intel Xeon Platinum 8370C CPU 2.80GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                      | Mean          | Error          | StdDev      | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|---------------------------- |--------------:|---------------:|------------:|------:|--------:|-----:|-------:|----------:|------------:|
| EncinaRefitCall             |            NA |             NA |          NA |     ? |       ? |    ? |     NA |        NA |           ? |
| EncinaRefitCall_Sequential5 |      9.724 μs |      0.1407 μs |   0.0077 μs | 0.001 |    0.00 |    1 | 0.3510 |   8.79 KB |        0.93 |
| EncinaRefitCall_Batch10     |     24.752 μs |      6.2960 μs |   0.3451 μs | 0.002 |    0.00 |    2 | 0.7324 |  18.34 KB |        1.95 |
| DirectRefitCall_Baseline    | 14,375.360 μs |  5,393.0324 μs | 295.6103 μs | 1.000 |    0.03 |    3 |      - |   9.42 KB |        1.00 |
| DirectRefitCall_Batch10     | 18,944.880 μs | 14,077.8855 μs | 771.6564 μs | 1.318 |    0.05 |    4 |      - |  93.33 KB |        9.91 |
| DirectRefitCall_Sequential5 | 78,814.299 μs |  6,511.9511 μs | 356.9420 μs | 5.484 |    0.10 |    5 |      - |  45.81 KB |        4.86 |

Benchmarks with issues:
  RestApiRequestHandlerBenchmarks.EncinaRefitCall: ShortRun(IterationCount=3, LaunchCount=1, WarmupCount=3)
