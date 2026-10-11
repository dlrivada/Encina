```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]    : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  MediumRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

Job=MediumRun  IterationCount=15  LaunchCount=2  
WarmupCount=10  

```
| Method                      | Mean          | Error        | StdDev       | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|---------------------------- |--------------:|-------------:|-------------:|------:|--------:|-----:|-------:|----------:|------------:|
| EncinaRefitCall             |            NA |           NA |           NA |     ? |       ? |    ? |     NA |        NA |           ? |
| EncinaRefitCall_Sequential5 |      11.80 μs |     0.030 μs |     0.043 μs | 0.000 |    0.00 |    1 | 0.5341 |   8.79 KB |        0.92 |
| EncinaRefitCall_Batch10     |      27.86 μs |     1.553 μs |     2.276 μs | 0.001 |    0.00 |    2 | 1.0986 |  18.34 KB |        1.92 |
| DirectRefitCall_Baseline    |  26,624.04 μs |   802.029 μs | 1,200.440 μs | 1.002 |    0.06 |    3 |      - |   9.55 KB |        1.00 |
| DirectRefitCall_Batch10     |  35,177.14 μs | 1,181.304 μs | 1,656.023 μs | 1.324 |    0.09 |    4 |      - |  93.49 KB |        9.79 |
| DirectRefitCall_Sequential5 | 122,801.66 μs | 3,123.742 μs | 4,578.741 μs | 4.622 |    0.27 |    5 |      - |  45.98 KB |        4.82 |

Benchmarks with issues:
  RestApiRequestHandlerBenchmarks.EncinaRefitCall: MediumRun(IterationCount=15, LaunchCount=2, WarmupCount=10)
