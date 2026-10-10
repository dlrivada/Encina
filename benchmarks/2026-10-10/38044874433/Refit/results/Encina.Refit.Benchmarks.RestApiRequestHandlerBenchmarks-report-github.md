```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                      | Mean          | Error          | StdDev        | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|---------------------------- |--------------:|---------------:|--------------:|------:|--------:|-----:|-------:|----------:|------------:|
| EncinaRefitCall             |            NA |             NA |            NA |     ? |       ? |    ? |     NA |        NA |           ? |
| EncinaRefitCall_Sequential5 |      9.951 μs |      0.0158 μs |     0.0009 μs | 0.001 |    0.00 |    1 | 0.5341 |   8.79 KB |        0.93 |
| EncinaRefitCall_Batch10     |     23.272 μs |      3.7762 μs |     0.2070 μs | 0.003 |    0.00 |    2 | 1.0986 |  18.34 KB |        1.95 |
| DirectRefitCall_Baseline    |  7,026.984 μs | 10,549.9432 μs |   578.2780 μs | 1.005 |    0.10 |    3 |      - |   9.41 KB |        1.00 |
| DirectRefitCall_Batch10     |  8,513.196 μs |  8,926.1168 μs |   489.2706 μs | 1.217 |    0.11 |    4 |      - |  93.39 KB |        9.92 |
| DirectRefitCall_Sequential5 | 34,391.954 μs | 33,855.5831 μs | 1,855.7388 μs | 4.918 |    0.43 |    5 |      - |  45.66 KB |        4.85 |

Benchmarks with issues:
  RestApiRequestHandlerBenchmarks.EncinaRefitCall: ShortRun(IterationCount=3, LaunchCount=1, WarmupCount=3)
