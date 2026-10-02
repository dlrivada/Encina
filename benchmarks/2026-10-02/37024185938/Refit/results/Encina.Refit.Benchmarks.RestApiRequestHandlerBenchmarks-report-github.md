```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                      | Mean          | Error         | StdDev       | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|---------------------------- |--------------:|--------------:|-------------:|------:|--------:|-----:|-------:|----------:|------------:|
| EncinaRefitCall             |            NA |            NA |           NA |     ? |       ? |    ? |     NA |        NA |           ? |
| EncinaRefitCall_Sequential5 |      10.65 μs |      0.576 μs |     0.032 μs | 0.001 |    0.00 |    1 | 0.4272 |   7.15 KB |        0.75 |
| EncinaRefitCall_Batch10     |      26.36 μs |     37.607 μs |     2.061 μs | 0.001 |    0.00 |    2 | 0.9155 |  15.06 KB |        1.59 |
| DirectRefitCall_Baseline    |  20,118.55 μs |  8,755.759 μs |   479.933 μs | 1.000 |    0.03 |    3 |      - |   9.47 KB |        1.00 |
| DirectRefitCall_Batch10     |  23,634.22 μs | 31,302.089 μs | 1,715.773 μs | 1.175 |    0.08 |    3 |      - |  93.66 KB |        9.89 |
| DirectRefitCall_Sequential5 | 104,156.35 μs | 32,621.180 μs | 1,788.077 μs | 5.179 |    0.13 |    4 |      - |  45.98 KB |        4.85 |

Benchmarks with issues:
  RestApiRequestHandlerBenchmarks.EncinaRefitCall: ShortRun(IterationCount=3, LaunchCount=1, WarmupCount=3)
