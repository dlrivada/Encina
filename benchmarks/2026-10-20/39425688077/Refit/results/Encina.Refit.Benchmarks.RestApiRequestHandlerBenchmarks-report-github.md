```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                      | Mean          | Error         | StdDev        | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|---------------------------- |--------------:|--------------:|--------------:|------:|--------:|-----:|-------:|----------:|------------:|
| EncinaRefitCall             |            NA |            NA |            NA |     ? |       ? |    ? |     NA |        NA |           ? |
| EncinaRefitCall_Sequential5 |      7.939 μs |      6.585 μs |     0.3609 μs | 0.000 |    0.00 |    1 | 0.5341 |   8.79 KB |        0.92 |
| EncinaRefitCall_Batch10     |     17.557 μs |      9.791 μs |     0.5367 μs | 0.001 |    0.00 |    2 | 1.0986 |  18.34 KB |        1.93 |
| DirectRefitCall_Baseline    | 18,385.955 μs |  4,138.615 μs |   226.8514 μs | 1.000 |    0.02 |    3 |      - |   9.53 KB |        1.00 |
| DirectRefitCall_Batch10     | 24,136.560 μs | 20,229.447 μs | 1,108.8443 μs | 1.313 |    0.05 |    4 |      - |     94 KB |        9.87 |
| DirectRefitCall_Sequential5 | 96,646.706 μs | 21,108.282 μs | 1,157.0161 μs | 5.257 |    0.08 |    5 |      - |  45.96 KB |        4.82 |

Benchmarks with issues:
  RestApiRequestHandlerBenchmarks.EncinaRefitCall: ShortRun(IterationCount=3, LaunchCount=1, WarmupCount=3)
