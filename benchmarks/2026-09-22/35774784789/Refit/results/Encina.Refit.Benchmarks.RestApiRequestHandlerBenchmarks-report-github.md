```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.91GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                      | Mean           | Error          | StdDev        | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|---------------------------- |---------------:|---------------:|--------------:|------:|--------:|-----:|-------:|----------:|------------:|
| EncinaRefitCall             |             NA |             NA |            NA |     ? |       ? |    ? |     NA |        NA |           ? |
| EncinaRefitCall_Sequential5 |       4.193 μs |      0.3359 μs |     0.0184 μs | 0.000 |    0.00 |    1 | 0.2975 |   4.92 KB |        0.50 |
| EncinaRefitCall_Batch10     |      18.015 μs |      2.4151 μs |     0.1324 μs | 0.001 |    0.00 |    2 | 0.6409 |  10.61 KB |        1.07 |
| DirectRefitCall_Baseline    |  17,075.070 μs | 11,985.7097 μs |   656.9772 μs | 1.001 |    0.05 |    3 |      - |   9.89 KB |        1.00 |
| DirectRefitCall_Batch10     |  31,489.584 μs | 12,747.1450 μs |   698.7140 μs | 1.846 |    0.07 |    4 |      - |  97.83 KB |        9.89 |
| DirectRefitCall_Sequential5 | 103,724.452 μs | 39,706.9958 μs | 2,176.4744 μs | 6.081 |    0.23 |    5 |      - |  48.13 KB |        4.86 |

Benchmarks with issues:
  RestApiRequestHandlerBenchmarks.EncinaRefitCall: ShortRun(IterationCount=3, LaunchCount=1, WarmupCount=3)
