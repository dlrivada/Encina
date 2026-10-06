```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 3.02GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                      | Mean          | Error         | StdDev       | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|---------------------------- |--------------:|--------------:|-------------:|------:|--------:|-----:|-------:|----------:|------------:|
| EncinaRefitCall             |            NA |            NA |           NA |     ? |       ? |    ? |     NA |        NA |           ? |
| EncinaRefitCall_Sequential5 |      12.29 μs |      0.145 μs |     0.008 μs | 0.001 |    0.00 |    1 | 0.5341 |   8.79 KB |        0.92 |
| EncinaRefitCall_Batch10     |      29.05 μs |     20.475 μs |     1.122 μs | 0.002 |    0.00 |    2 | 1.0986 |  18.34 KB |        1.93 |
| DirectRefitCall_Baseline    |  16,918.99 μs |  3,800.278 μs |   208.306 μs | 1.000 |    0.02 |    3 |      - |   9.52 KB |        1.00 |
| DirectRefitCall_Batch10     |  28,278.82 μs | 20,777.603 μs | 1,138.891 μs | 1.672 |    0.06 |    4 |      - |   93.6 KB |        9.83 |
| DirectRefitCall_Sequential5 | 100,581.05 μs | 47,251.480 μs | 2,590.013 μs | 5.945 |    0.15 |    5 |      - |  45.87 KB |        4.82 |

Benchmarks with issues:
  RestApiRequestHandlerBenchmarks.EncinaRefitCall: ShortRun(IterationCount=3, LaunchCount=1, WarmupCount=3)
