```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.59GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                      | Mean          | Error          | StdDev       | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|---------------------------- |--------------:|---------------:|-------------:|------:|--------:|-----:|-------:|----------:|------------:|
| EncinaRefitCall             |            NA |             NA |           NA |     ? |       ? |    ? |     NA |        NA |           ? |
| EncinaRefitCall_Sequential5 |      10.98 μs |       0.298 μs |     0.016 μs | 0.000 |    0.00 |    1 | 0.4425 |   7.42 KB |        0.75 |
| EncinaRefitCall_Batch10     |      27.04 μs |      57.034 μs |     3.126 μs | 0.001 |    0.00 |    2 | 0.9155 |  15.61 KB |        1.58 |
| DirectRefitCall_Baseline    |  25,151.56 μs |   2,671.929 μs |   146.457 μs | 1.000 |    0.01 |    3 |      - |   9.89 KB |        1.00 |
| DirectRefitCall_Batch10     |  29,743.63 μs | 101,064.930 μs | 5,539.710 μs | 1.183 |    0.19 |    3 |      - |  97.69 KB |        9.87 |
| DirectRefitCall_Sequential5 | 129,676.54 μs |  56,689.062 μs | 3,107.319 μs | 5.156 |    0.11 |    4 |      - |  48.26 KB |        4.88 |

Benchmarks with issues:
  RestApiRequestHandlerBenchmarks.EncinaRefitCall: ShortRun(IterationCount=3, LaunchCount=1, WarmupCount=3)
