```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                      | Mean          | Error          | StdDev        | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|---------------------------- |--------------:|---------------:|--------------:|------:|--------:|-----:|-------:|----------:|------------:|
| EncinaRefitCall             |            NA |             NA |            NA |     ? |       ? |    ? |     NA |        NA |           ? |
| EncinaRefitCall_Sequential5 |      4.411 μs |      0.3294 μs |     0.0181 μs | 0.000 |    0.00 |    1 | 0.2975 |   4.92 KB |        0.50 |
| EncinaRefitCall_Batch10     |     18.980 μs |      2.5932 μs |     0.1421 μs | 0.002 |    0.00 |    2 | 0.6409 |  10.61 KB |        1.08 |
| DirectRefitCall_Baseline    | 10,461.617 μs |  4,486.8668 μs |   245.9403 μs | 1.000 |    0.03 |    3 |      - |   9.85 KB |        1.00 |
| DirectRefitCall_Batch10     | 14,238.785 μs |  7,839.3387 μs |   429.7006 μs | 1.362 |    0.05 |    4 |      - |   97.7 KB |        9.92 |
| DirectRefitCall_Sequential5 | 48,828.179 μs | 43,582.6287 μs | 2,388.9110 μs | 4.669 |    0.22 |    5 |      - |  47.75 KB |        4.85 |

Benchmarks with issues:
  RestApiRequestHandlerBenchmarks.EncinaRefitCall: ShortRun(IterationCount=3, LaunchCount=1, WarmupCount=3)
