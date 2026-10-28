```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.61GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                      | Mean          | Error          | StdDev        | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|---------------------------- |--------------:|---------------:|--------------:|------:|--------:|-----:|-------:|----------:|------------:|
| EncinaRefitCall             |            NA |             NA |            NA |     ? |       ? |    ? |     NA |        NA |           ? |
| EncinaRefitCall_Sequential5 |      12.35 μs |       0.848 μs |      0.046 μs | 0.000 |    0.00 |    1 | 0.5341 |   8.79 KB |        0.88 |
| EncinaRefitCall_Batch10     |      28.33 μs |      12.047 μs |      0.660 μs | 0.000 |    0.00 |    2 | 1.0986 |  18.34 KB |        1.84 |
| DirectRefitCall_Baseline    | 120,930.43 μs |  19,561.670 μs |  1,072.241 μs | 1.000 |    0.01 |    3 |      - |   9.96 KB |        1.00 |
| DirectRefitCall_Batch10     | 134,139.41 μs |  65,230.731 μs |  3,575.517 μs | 1.109 |    0.03 |    3 |      - |  95.37 KB |        9.58 |
| DirectRefitCall_Sequential5 | 607,357.48 μs | 205,760.989 μs | 11,278.454 μs | 5.023 |    0.09 |    4 |      - |  48.27 KB |        4.85 |

Benchmarks with issues:
  RestApiRequestHandlerBenchmarks.EncinaRefitCall: ShortRun(IterationCount=3, LaunchCount=1, WarmupCount=3)
