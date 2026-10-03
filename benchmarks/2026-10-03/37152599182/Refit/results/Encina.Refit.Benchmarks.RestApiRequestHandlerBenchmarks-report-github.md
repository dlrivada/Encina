```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
Intel Xeon Platinum 8370C CPU 2.80GHz (Max: 2.79GHz), 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                      | Mean           | Error           | StdDev        | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|---------------------------- |---------------:|----------------:|--------------:|------:|--------:|-----:|-------:|----------:|------------:|
| EncinaRefitCall             |             NA |              NA |            NA |     ? |       ? |    ? |     NA |        NA |           ? |
| EncinaRefitCall_Sequential5 |       8.620 μs |       0.4699 μs |     0.0258 μs | 0.000 |    0.00 |    1 | 0.2899 |   7.15 KB |        0.75 |
| EncinaRefitCall_Batch10     |      21.747 μs |       5.8451 μs |     0.3204 μs | 0.001 |    0.00 |    2 | 0.6104 |  15.06 KB |        1.59 |
| DirectRefitCall_Baseline    |  17,357.161 μs |   8,648.9450 μs |   474.0779 μs | 1.001 |    0.03 |    3 |      - |   9.48 KB |        1.00 |
| DirectRefitCall_Batch10     |  19,285.697 μs |   6,911.8558 μs |   378.8621 μs | 1.112 |    0.03 |    3 |      - |  93.46 KB |        9.86 |
| DirectRefitCall_Sequential5 | 104,180.979 μs | 180,114.2193 μs | 9,872.6682 μs | 6.005 |    0.51 |    4 |      - |  45.98 KB |        4.85 |

Benchmarks with issues:
  RestApiRequestHandlerBenchmarks.EncinaRefitCall: ShortRun(IterationCount=3, LaunchCount=1, WarmupCount=3)
