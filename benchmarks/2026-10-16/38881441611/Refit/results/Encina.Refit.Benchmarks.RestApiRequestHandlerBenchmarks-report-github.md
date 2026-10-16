```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                      | Mean          | Error          | StdDev        | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|---------------------------- |--------------:|---------------:|--------------:|------:|--------:|-----:|-------:|----------:|------------:|
| EncinaRefitCall             |            NA |             NA |            NA |     ? |       ? |    ? |     NA |        NA |           ? |
| EncinaRefitCall_Sequential5 |      9.802 μs |      1.7445 μs |     0.0956 μs | 0.001 |    0.00 |    1 | 0.5341 |   8.79 KB |        0.93 |
| EncinaRefitCall_Batch10     |     22.152 μs |      0.6128 μs |     0.0336 μs | 0.002 |    0.00 |    2 | 1.0986 |  18.34 KB |        1.94 |
| DirectRefitCall_Baseline    | 12,354.329 μs | 39,114.6833 μs | 2,144.0078 μs | 1.021 |    0.22 |    3 |      - |   9.44 KB |        1.00 |
| DirectRefitCall_Batch10     | 24,766.039 μs | 46,418.5850 μs | 2,544.3593 μs | 2.046 |    0.36 |    4 |      - |  93.54 KB |        9.90 |
| DirectRefitCall_Sequential5 | 59,894.975 μs | 35,416.7869 μs | 1,941.3136 μs | 4.949 |    0.77 |    5 |      - |  45.74 KB |        4.84 |

Benchmarks with issues:
  RestApiRequestHandlerBenchmarks.EncinaRefitCall: ShortRun(IterationCount=3, LaunchCount=1, WarmupCount=3)
