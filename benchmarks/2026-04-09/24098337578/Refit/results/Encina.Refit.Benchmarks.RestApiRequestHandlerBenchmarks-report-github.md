```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]    : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  MediumRun : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3

Job=MediumRun  IterationCount=15  LaunchCount=2  
WarmupCount=10  

```
| Method                      | Mean          | Error         | StdDev        | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|---------------------------- |--------------:|--------------:|--------------:|------:|--------:|-----:|-------:|----------:|------------:|
| EncinaRefitCall             |            NA |            NA |            NA |     ? |       ? |    ? |     NA |        NA |           ? |
| EncinaRefitCall_Sequential5 |      4.281 μs |     0.0373 μs |     0.0547 μs | 0.001 |    0.00 |    1 | 0.2975 |   4.92 KB |        0.50 |
| EncinaRefitCall_Batch10     |     14.826 μs |     1.8284 μs |     2.7366 μs | 0.002 |    0.00 |    2 | 0.6409 |  10.61 KB |        1.08 |
| DirectRefitCall_Baseline    |  7,487.158 μs |   888.7292 μs | 1,330.2081 μs | 1.030 |    0.25 |    3 |      - |   9.83 KB |        1.00 |
| DirectRefitCall_Batch10     |  9,481.319 μs |   360.8488 μs |   517.5186 μs | 1.305 |    0.23 |    4 |      - |  97.74 KB |        9.94 |
| DirectRefitCall_Sequential5 | 34,758.427 μs | 2,828.4416 μs | 4,056.4668 μs | 4.784 |    0.99 |    5 |      - |   47.7 KB |        4.85 |

Benchmarks with issues:
  RestApiRequestHandlerBenchmarks.EncinaRefitCall: MediumRun(IterationCount=15, LaunchCount=2, WarmupCount=10)
