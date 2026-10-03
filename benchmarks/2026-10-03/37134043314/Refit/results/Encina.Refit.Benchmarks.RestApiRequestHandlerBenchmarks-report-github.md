```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 3.09GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                      | Mean         | Error         | StdDev       | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|---------------------------- |-------------:|--------------:|-------------:|------:|--------:|-----:|-------:|----------:|------------:|
| EncinaRefitCall             |           NA |            NA |           NA |     ? |       ? |    ? |     NA |        NA |           ? |
| EncinaRefitCall_Sequential5 |     10.54 μs |      1.338 μs |     0.073 μs | 0.001 |    0.00 |    1 | 0.4272 |   7.15 KB |        0.76 |
| EncinaRefitCall_Batch10     |     25.38 μs |      6.176 μs |     0.339 μs | 0.003 |    0.00 |    2 | 0.9155 |  15.06 KB |        1.60 |
| DirectRefitCall_Baseline    |  7,345.54 μs |  4,129.856 μs |   226.371 μs | 1.001 |    0.04 |    3 |      - |   9.41 KB |        1.00 |
| DirectRefitCall_Batch10     | 13,929.76 μs | 16,620.204 μs |   911.009 μs | 1.898 |    0.12 |    4 |      - |  93.41 KB |        9.92 |
| DirectRefitCall_Sequential5 | 37,044.44 μs | 36,933.907 μs | 2,024.472 μs | 5.046 |    0.27 |    5 |      - |  48.59 KB |        5.16 |

Benchmarks with issues:
  RestApiRequestHandlerBenchmarks.EncinaRefitCall: ShortRun(IterationCount=3, LaunchCount=1, WarmupCount=3)
