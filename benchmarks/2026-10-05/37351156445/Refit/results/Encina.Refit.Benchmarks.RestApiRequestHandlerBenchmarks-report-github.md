```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                      | Mean         | Error         | StdDev       | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|---------------------------- |-------------:|--------------:|-------------:|------:|--------:|-----:|-------:|----------:|------------:|
| EncinaRefitCall             |           NA |            NA |           NA |     ? |       ? |    ? |     NA |        NA |           ? |
| EncinaRefitCall_Sequential5 |     12.41 μs |      0.448 μs |     0.025 μs | 0.001 |    0.00 |    1 | 0.5341 |   8.75 KB |        0.92 |
| EncinaRefitCall_Batch10     |     27.50 μs |      3.787 μs |     0.208 μs | 0.001 |    0.00 |    2 | 1.0986 |  18.27 KB |        1.91 |
| DirectRefitCall_Baseline    | 24,592.20 μs | 26,947.779 μs | 1,477.099 μs | 1.002 |    0.07 |    3 |      - |   9.54 KB |        1.00 |
| DirectRefitCall_Sequential5 | 79,984.03 μs | 62,010.887 μs | 3,399.026 μs | 3.260 |    0.21 |    4 |      - |  45.81 KB |        4.80 |
| DirectRefitCall_Batch10     | 89,186.65 μs |  4,219.288 μs |   231.273 μs | 3.635 |    0.19 |    4 |      - |  93.73 KB |        9.83 |

Benchmarks with issues:
  RestApiRequestHandlerBenchmarks.EncinaRefitCall: ShortRun(IterationCount=3, LaunchCount=1, WarmupCount=3)
