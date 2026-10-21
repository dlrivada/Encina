```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
Intel Xeon 6973P-C 4.03GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                      | Mean           | Error         | StdDev        | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|---------------------------- |---------------:|--------------:|--------------:|------:|--------:|-----:|-------:|----------:|------------:|
| EncinaRefitCall             |             NA |            NA |            NA |     ? |       ? |    ? |     NA |        NA |           ? |
| EncinaRefitCall_Sequential5 |       7.530 μs |      2.325 μs |     0.1274 μs | 0.000 |    0.00 |    1 | 0.1068 |   8.79 KB |        0.91 |
| EncinaRefitCall_Batch10     |      21.652 μs |     23.977 μs |     1.3143 μs | 0.001 |    0.00 |    2 | 0.2136 |  18.34 KB |        1.89 |
| DirectRefitCall_Baseline    |  32,434.905 μs | 12,583.410 μs |   689.7392 μs | 1.000 |    0.03 |    3 |      - |    9.7 KB |        1.00 |
| DirectRefitCall_Batch10     |  44,315.280 μs | 23,744.682 μs | 1,301.5262 μs | 1.367 |    0.04 |    4 |      - |  93.91 KB |        9.68 |
| DirectRefitCall_Sequential5 | 162,875.823 μs | 42,374.433 μs | 2,322.6856 μs | 5.023 |    0.11 |    5 |      - |  46.13 KB |        4.76 |

Benchmarks with issues:
  RestApiRequestHandlerBenchmarks.EncinaRefitCall: ShortRun(IterationCount=3, LaunchCount=1, WarmupCount=3)
