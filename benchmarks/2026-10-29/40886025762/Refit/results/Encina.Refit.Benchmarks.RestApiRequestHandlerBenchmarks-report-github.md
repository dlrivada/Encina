```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.68GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                      | Mean          | Error          | StdDev       | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|---------------------------- |--------------:|---------------:|-------------:|------:|--------:|-----:|-------:|----------:|------------:|
| EncinaRefitCall             |            NA |             NA |           NA |     ? |       ? |    ? |     NA |        NA |           ? |
| EncinaRefitCall_Sequential5 |      11.81 μs |       0.941 μs |     0.052 μs | 0.000 |    0.00 |    1 | 0.5341 |   8.79 KB |        0.91 |
| EncinaRefitCall_Batch10     |      28.02 μs |       5.105 μs |     0.280 μs | 0.001 |    0.00 |    2 | 1.0986 |  18.34 KB |        1.91 |
| DirectRefitCall_Baseline    |  42,766.18 μs |   2,641.700 μs |   144.800 μs | 1.000 |    0.00 |    3 |      - |   9.62 KB |        1.00 |
| DirectRefitCall_Batch10     | 129,385.40 μs |  22,227.227 μs | 1,218.349 μs | 3.025 |    0.03 |    4 |      - |  94.22 KB |        9.80 |
| DirectRefitCall_Sequential5 | 222,072.36 μs | 132,027.882 μs | 7,236.894 μs | 5.193 |    0.15 |    5 |      - |  46.39 KB |        4.82 |

Benchmarks with issues:
  RestApiRequestHandlerBenchmarks.EncinaRefitCall: ShortRun(IterationCount=3, LaunchCount=1, WarmupCount=3)
