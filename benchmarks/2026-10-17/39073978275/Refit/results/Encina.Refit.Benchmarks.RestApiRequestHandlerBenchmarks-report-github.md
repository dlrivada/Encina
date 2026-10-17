```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                      | Mean         | Error         | StdDev       | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|---------------------------- |-------------:|--------------:|-------------:|------:|--------:|-----:|-------:|----------:|------------:|
| EncinaRefitCall             |           NA |            NA |           NA |     ? |       ? |    ? |     NA |        NA |           ? |
| EncinaRefitCall_Sequential5 |     11.93 μs |      0.543 μs |     0.030 μs | 0.001 |    0.00 |    1 | 0.5341 |   8.79 KB |        0.91 |
| EncinaRefitCall_Batch10     |     27.82 μs |     27.310 μs |     1.497 μs | 0.003 |    0.00 |    2 | 1.0986 |  18.34 KB |        1.89 |
| DirectRefitCall_Baseline    |  9,336.79 μs |  3,705.880 μs |   203.132 μs | 1.000 |    0.03 |    3 |      - |   9.71 KB |        1.00 |
| DirectRefitCall_Batch10     | 15,052.88 μs | 21,607.912 μs | 1,184.403 μs | 1.613 |    0.11 |    4 |      - |  93.49 KB |        9.63 |
| DirectRefitCall_Sequential5 | 44,970.87 μs | 10,616.239 μs |   581.912 μs | 4.818 |    0.11 |    5 |      - |  45.88 KB |        4.73 |

Benchmarks with issues:
  RestApiRequestHandlerBenchmarks.EncinaRefitCall: ShortRun(IterationCount=3, LaunchCount=1, WarmupCount=3)
