```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                      | Mean           | Error           | StdDev        | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|---------------------------- |---------------:|----------------:|--------------:|------:|--------:|-----:|-------:|----------:|------------:|
| EncinaRefitCall             |             NA |              NA |            NA |     ? |       ? |    ? |     NA |        NA |           ? |
| EncinaRefitCall_Sequential5 |       2.144 μs |       0.3419 μs |     0.0187 μs | 0.000 |    0.00 |    1 | 0.2975 |   4.92 KB |        0.50 |
| EncinaRefitCall_Batch10     |       6.685 μs |      21.1678 μs |     1.1603 μs | 0.000 |    0.00 |    2 | 0.6485 |  10.61 KB |        1.07 |
| DirectRefitCall_Baseline    |  26,197.143 μs |  12,240.0412 μs |   670.9180 μs | 1.000 |    0.03 |    3 |      - |    9.9 KB |        1.00 |
| DirectRefitCall_Batch10     |  33,758.542 μs |   5,262.8154 μs |   288.4727 μs | 1.289 |    0.03 |    4 |      - |  97.75 KB |        9.87 |
| DirectRefitCall_Sequential5 | 134,453.523 μs | 127,599.7988 μs | 6,994.1756 μs | 5.135 |    0.26 |    5 |      - |  48.27 KB |        4.88 |

Benchmarks with issues:
  RestApiRequestHandlerBenchmarks.EncinaRefitCall: ShortRun(IterationCount=3, LaunchCount=1, WarmupCount=3)
