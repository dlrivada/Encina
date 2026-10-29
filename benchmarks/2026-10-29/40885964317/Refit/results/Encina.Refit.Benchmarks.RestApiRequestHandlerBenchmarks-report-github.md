```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.59GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                      | Mean          | Error         | StdDev     | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|---------------------------- |--------------:|--------------:|-----------:|------:|--------:|-----:|-------:|----------:|------------:|
| EncinaRefitCall             |            NA |            NA |         NA |     ? |       ? |    ? |     NA |        NA |           ? |
| EncinaRefitCall_Sequential5 |      12.22 μs |      1.258 μs |   0.069 μs | 0.000 |    0.00 |    1 | 0.5341 |   8.79 KB |        0.92 |
| EncinaRefitCall_Batch10     |      27.78 μs |      1.856 μs |   0.102 μs | 0.001 |    0.00 |    2 | 1.0986 |  18.34 KB |        1.91 |
| DirectRefitCall_Baseline    |  37,683.11 μs |  2,436.446 μs | 133.550 μs | 1.000 |    0.00 |    3 |      - |   9.59 KB |        1.00 |
| DirectRefitCall_Batch10     |  38,453.57 μs | 12,110.613 μs | 663.824 μs | 1.020 |    0.02 |    3 |      - |  93.64 KB |        9.77 |
| DirectRefitCall_Sequential5 | 179,020.40 μs | 11,196.543 μs | 613.720 μs | 4.751 |    0.02 |    4 |      - |  46.29 KB |        4.83 |

Benchmarks with issues:
  RestApiRequestHandlerBenchmarks.EncinaRefitCall: ShortRun(IterationCount=3, LaunchCount=1, WarmupCount=3)
