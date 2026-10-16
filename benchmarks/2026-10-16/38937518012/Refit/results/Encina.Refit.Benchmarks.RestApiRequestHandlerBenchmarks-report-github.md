```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                      | Mean          | Error          | StdDev        | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|---------------------------- |--------------:|---------------:|--------------:|------:|--------:|-----:|-------:|----------:|------------:|
| EncinaRefitCall             |            NA |             NA |            NA |     ? |       ? |    ? |     NA |        NA |           ? |
| EncinaRefitCall_Sequential5 |      12.11 μs |       0.537 μs |      0.029 μs | 0.000 |    0.00 |    1 | 0.5341 |   8.79 KB |        0.90 |
| EncinaRefitCall_Batch10     |      30.65 μs |      90.026 μs |      4.935 μs | 0.000 |    0.00 |    2 | 1.0986 |  18.34 KB |        1.88 |
| DirectRefitCall_Baseline    |  82,457.22 μs |  12,177.984 μs |    667.516 μs | 1.000 |    0.01 |    3 |      - |   9.78 KB |        1.00 |
| DirectRefitCall_Batch10     |  83,858.26 μs |  10,738.744 μs |    588.627 μs | 1.017 |    0.01 |    3 |      - |   93.8 KB |        9.59 |
| DirectRefitCall_Sequential5 | 421,453.44 μs | 359,559.201 μs | 19,708.653 μs | 5.111 |    0.21 |    4 |      - |  48.26 KB |        4.93 |

Benchmarks with issues:
  RestApiRequestHandlerBenchmarks.EncinaRefitCall: ShortRun(IterationCount=3, LaunchCount=1, WarmupCount=3)
