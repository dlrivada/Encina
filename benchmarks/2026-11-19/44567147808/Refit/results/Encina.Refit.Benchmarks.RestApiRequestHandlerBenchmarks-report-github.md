```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
Intel Xeon 6975P-C 2.84GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                      | Mean           | Error           | StdDev        | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|---------------------------- |---------------:|----------------:|--------------:|------:|--------:|-----:|-------:|----------:|------------:|
| EncinaRefitCall             |             NA |              NA |            NA |     ? |       ? |    ? |     NA |        NA |           ? |
| EncinaRefitCall_Sequential5 |       7.891 μs |       0.2998 μs |     0.0164 μs | 0.000 |    0.00 |    1 | 0.1068 |   8.83 KB |        0.93 |
| EncinaRefitCall_Batch10     |      18.624 μs |       3.4149 μs |     0.1872 μs | 0.001 |    0.00 |    2 | 0.2136 |  18.42 KB |        1.93 |
| DirectRefitCall_Baseline    |  21,924.059 μs |  42,146.3149 μs | 2,310.1817 μs | 1.007 |    0.13 |    3 |      - |   9.53 KB |        1.00 |
| DirectRefitCall_Batch10     |  69,065.571 μs | 176,868.4656 μs | 9,694.7575 μs | 3.173 |    0.48 |    4 |      - |  94.53 KB |        9.92 |
| DirectRefitCall_Sequential5 | 121,113.403 μs | 126,361.6283 μs | 6,926.3073 μs | 5.564 |    0.56 |    5 |      - |  46.37 KB |        4.87 |

Benchmarks with issues:
  RestApiRequestHandlerBenchmarks.EncinaRefitCall: ShortRun(IterationCount=3, LaunchCount=1, WarmupCount=3)
