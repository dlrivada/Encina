```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
Intel Xeon Platinum 8370C CPU 2.80GHz (Max: 2.56GHz), 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                      | Mean           | Error          | StdDev        | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|---------------------------- |---------------:|---------------:|--------------:|------:|--------:|-----:|-------:|----------:|------------:|
| EncinaRefitCall             |             NA |             NA |            NA |     ? |       ? |    ? |     NA |        NA |           ? |
| EncinaRefitCall_Sequential5 |       8.594 μs |      0.5035 μs |     0.0276 μs | 0.000 |    0.00 |    1 | 0.2899 |   7.15 KB |        0.75 |
| EncinaRefitCall_Batch10     |      21.706 μs |      9.5126 μs |     0.5214 μs | 0.001 |    0.00 |    2 | 0.6104 |  15.06 KB |        1.59 |
| DirectRefitCall_Baseline    |  24,148.475 μs | 51,304.8736 μs | 2,812.1933 μs | 1.009 |    0.14 |    3 |      - |   9.47 KB |        1.00 |
| DirectRefitCall_Batch10     |  28,114.880 μs | 13,878.1914 μs |   760.7105 μs | 1.174 |    0.11 |    4 |      - |  93.56 KB |        9.88 |
| DirectRefitCall_Sequential5 | 114,191.615 μs | 96,302.6574 μs | 5,278.6736 μs | 4.769 |    0.49 |    5 |      - |  46.37 KB |        4.90 |

Benchmarks with issues:
  RestApiRequestHandlerBenchmarks.EncinaRefitCall: ShortRun(IterationCount=3, LaunchCount=1, WarmupCount=3)
