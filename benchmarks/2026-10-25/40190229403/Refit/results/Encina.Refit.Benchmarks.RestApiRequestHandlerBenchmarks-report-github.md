```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 4.25GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                      | Mean          | Error          | StdDev        | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|---------------------------- |--------------:|---------------:|--------------:|------:|--------:|-----:|-------:|----------:|------------:|
| EncinaRefitCall             |            NA |             NA |            NA |     ? |       ? |    ? |     NA |        NA |           ? |
| EncinaRefitCall_Sequential5 |      7.584 μs |      0.1545 μs |     0.0085 μs | 0.001 |    0.00 |    1 | 0.5341 |   8.79 KB |        0.93 |
| EncinaRefitCall_Batch10     |     21.864 μs |     12.4384 μs |     0.6818 μs | 0.002 |    0.00 |    2 | 1.0986 |  18.34 KB |        1.95 |
| DirectRefitCall_Baseline    | 14,314.316 μs |  3,475.0992 μs |   190.4819 μs | 1.000 |    0.02 |    3 |      - |   9.42 KB |        1.00 |
| DirectRefitCall_Batch10     | 22,246.897 μs | 50,044.6120 μs | 2,743.1141 μs | 1.554 |    0.17 |    4 |      - |  93.44 KB |        9.91 |
| DirectRefitCall_Sequential5 | 76,070.820 μs | 46,148.7056 μs | 2,529.5663 μs | 5.315 |    0.16 |    5 |      - |  45.96 KB |        4.88 |

Benchmarks with issues:
  RestApiRequestHandlerBenchmarks.EncinaRefitCall: ShortRun(IterationCount=3, LaunchCount=1, WarmupCount=3)
