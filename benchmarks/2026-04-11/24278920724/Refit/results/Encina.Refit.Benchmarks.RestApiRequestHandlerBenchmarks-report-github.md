```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]    : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  MediumRun : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3

Job=MediumRun  IterationCount=15  LaunchCount=2  
WarmupCount=10  

```
| Method                      | Mean          | Error         | StdDev        | Median        | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|---------------------------- |--------------:|--------------:|--------------:|--------------:|------:|--------:|-----:|-------:|----------:|------------:|
| EncinaRefitCall             |            NA |            NA |            NA |            NA |     ? |       ? |    ? |     NA |        NA |           ? |
| EncinaRefitCall_Sequential5 |      3.941 μs |     0.0407 μs |     0.0609 μs |      3.924 μs | 0.001 |    0.00 |    1 | 0.2975 |   4.92 KB |        0.50 |
| EncinaRefitCall_Batch10     |     12.844 μs |     1.4476 μs |     2.1219 μs |     12.517 μs | 0.002 |    0.00 |    2 | 0.6409 |  10.61 KB |        1.08 |
| DirectRefitCall_Baseline    |  7,428.391 μs |   687.5878 μs | 1,029.1491 μs |  7,828.751 μs | 1.022 |    0.21 |    3 |      - |   9.84 KB |        1.00 |
| DirectRefitCall_Batch10     |  8,916.097 μs |   332.8036 μs |   487.8193 μs |  8,870.397 μs | 1.226 |    0.20 |    4 |      - |  97.65 KB |        9.93 |
| DirectRefitCall_Sequential5 | 32,069.185 μs | 3,983.3382 μs | 5,584.0843 μs | 28,869.276 μs | 4.411 |    1.03 |    5 |      - |  47.64 KB |        4.84 |

Benchmarks with issues:
  RestApiRequestHandlerBenchmarks.EncinaRefitCall: MediumRun(IterationCount=15, LaunchCount=2, WarmupCount=10)
