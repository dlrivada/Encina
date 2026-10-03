```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.87GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                      | Mean         | Error         | StdDev       | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|---------------------------- |-------------:|--------------:|-------------:|------:|--------:|-----:|-------:|----------:|------------:|
| EncinaRefitCall             |           NA |            NA |           NA |     ? |       ? |    ? |     NA |        NA |           ? |
| EncinaRefitCall_Sequential5 |     10.75 μs |      0.758 μs |     0.042 μs | 0.002 |    0.00 |    1 | 0.4272 |   7.15 KB |        0.76 |
| EncinaRefitCall_Batch10     |     24.14 μs |      1.635 μs |     0.090 μs | 0.004 |    0.00 |    2 | 0.9155 |  15.06 KB |        1.60 |
| DirectRefitCall_Baseline    |  6,628.88 μs | 33,276.774 μs | 1,824.012 μs | 1.046 |    0.33 |    3 |      - |   9.42 KB |        1.00 |
| DirectRefitCall_Batch10     |  8,342.54 μs |  7,021.585 μs |   384.877 μs | 1.316 |    0.28 |    4 |      - |  93.28 KB |        9.91 |
| DirectRefitCall_Sequential5 | 34,895.17 μs | 36,495.213 μs | 2,000.426 μs | 5.505 |    1.17 |    5 |      - |  45.61 KB |        4.84 |

Benchmarks with issues:
  RestApiRequestHandlerBenchmarks.EncinaRefitCall: ShortRun(IterationCount=3, LaunchCount=1, WarmupCount=3)
