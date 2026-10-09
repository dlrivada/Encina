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
| EncinaRefitCall_Sequential5 |     12.09 μs |      0.361 μs |     0.020 μs | 0.001 |    0.00 |    1 | 0.5341 |   8.79 KB |        0.93 |
| EncinaRefitCall_Batch10     |     29.11 μs |      6.267 μs |     0.344 μs | 0.002 |    0.00 |    2 | 1.0986 |  18.34 KB |        1.94 |
| DirectRefitCall_Baseline    | 13,144.74 μs |  3,448.979 μs |   189.050 μs | 1.000 |    0.02 |    3 |      - |   9.47 KB |        1.00 |
| DirectRefitCall_Batch10     | 22,815.24 μs | 41,664.011 μs | 2,283.745 μs | 1.736 |    0.15 |    4 |      - |  93.43 KB |        9.87 |
| DirectRefitCall_Sequential5 | 64,827.83 μs | 28,102.364 μs | 1,540.385 μs | 4.933 |    0.12 |    5 |      - |  45.77 KB |        4.83 |

Benchmarks with issues:
  RestApiRequestHandlerBenchmarks.EncinaRefitCall: ShortRun(IterationCount=3, LaunchCount=1, WarmupCount=3)
