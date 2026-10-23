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
| EncinaRefitCall_Sequential5 |     12.05 μs |      4.924 μs |     0.270 μs | 0.001 |    0.00 |    1 | 0.5341 |   8.79 KB |        0.93 |
| EncinaRefitCall_Batch10     |     28.08 μs |     11.224 μs |     0.615 μs | 0.002 |    0.00 |    2 | 1.0986 |  18.34 KB |        1.94 |
| DirectRefitCall_Baseline    | 16,317.85 μs | 39,072.687 μs | 2,141.706 μs | 1.012 |    0.17 |    3 |      - |   9.48 KB |        1.00 |
| DirectRefitCall_Batch10     | 44,862.40 μs | 82,324.917 μs | 4,512.507 μs | 2.782 |    0.41 |    4 |      - |  93.57 KB |        9.88 |
| DirectRefitCall_Sequential5 | 60,916.39 μs | 83,312.694 μs | 4,566.650 μs | 3.777 |    0.50 |    5 |      - |  45.81 KB |        4.83 |

Benchmarks with issues:
  RestApiRequestHandlerBenchmarks.EncinaRefitCall: ShortRun(IterationCount=3, LaunchCount=1, WarmupCount=3)
