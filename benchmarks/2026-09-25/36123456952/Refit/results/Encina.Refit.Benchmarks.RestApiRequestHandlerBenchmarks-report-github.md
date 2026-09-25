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
| EncinaRefitCall_Sequential5 |     10.55 μs |      0.425 μs |     0.023 μs | 0.001 |    0.00 |    1 | 0.4272 |   7.15 KB |        0.73 |
| EncinaRefitCall_Batch10     |     26.36 μs |     28.572 μs |     1.566 μs | 0.003 |    0.00 |    2 | 0.9155 |  15.06 KB |        1.53 |
| DirectRefitCall_Baseline    |  9,755.43 μs |  5,924.518 μs |   324.743 μs | 1.001 |    0.04 |    3 |      - |   9.84 KB |        1.00 |
| DirectRefitCall_Batch10     | 10,197.31 μs |  6,660.738 μs |   365.098 μs | 1.046 |    0.04 |    3 |      - |  97.52 KB |        9.91 |
| DirectRefitCall_Sequential5 | 54,683.41 μs | 89,342.297 μs | 4,897.153 μs | 5.610 |    0.47 |    4 |      - |  47.79 KB |        4.85 |

Benchmarks with issues:
  RestApiRequestHandlerBenchmarks.EncinaRefitCall: ShortRun(IterationCount=3, LaunchCount=1, WarmupCount=3)
