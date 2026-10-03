```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
Intel Xeon 6973P-C 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                      | Mean          | Error        | StdDev      | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|---------------------------- |--------------:|-------------:|------------:|------:|--------:|-----:|-------:|----------:|------------:|
| EncinaRefitCall             |            NA |           NA |          NA |     ? |       ? |    ? |     NA |        NA |           ? |
| EncinaRefitCall_Sequential5 |      6.542 μs |     2.776 μs |   0.1521 μs | 0.000 |    0.00 |    1 | 0.0839 |   7.15 KB |        0.76 |
| EncinaRefitCall_Batch10     |     16.813 μs |    15.305 μs |   0.8389 μs | 0.001 |    0.00 |    2 | 0.1831 |  15.06 KB |        1.60 |
| DirectRefitCall_Baseline    | 13,311.688 μs | 1,285.927 μs |  70.4860 μs | 1.000 |    0.01 |    3 |      - |   9.42 KB |        1.00 |
| DirectRefitCall_Batch10     | 17,930.043 μs | 5,377.903 μs | 294.7810 μs | 1.347 |    0.02 |    4 |      - |  93.47 KB |        9.92 |
| DirectRefitCall_Sequential5 | 61,557.528 μs | 9,243.217 μs | 506.6519 μs | 4.624 |    0.04 |    5 |      - |  45.77 KB |        4.86 |

Benchmarks with issues:
  RestApiRequestHandlerBenchmarks.EncinaRefitCall: ShortRun(IterationCount=3, LaunchCount=1, WarmupCount=3)
