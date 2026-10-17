```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
INTEL XEON PLATINUM 8573C 2.30GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                      | Mean          | Error         | StdDev        | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|---------------------------- |--------------:|--------------:|--------------:|------:|--------:|-----:|-------:|----------:|------------:|
| EncinaRefitCall             |            NA |            NA |            NA |     ? |       ? |    ? |     NA |        NA |           ? |
| EncinaRefitCall_Sequential5 |      8.080 μs |      4.319 μs |     0.2368 μs | 0.000 |    0.00 |    1 | 0.1068 |   8.79 KB |        0.93 |
| EncinaRefitCall_Batch10     |     20.953 μs |     16.138 μs |     0.8846 μs | 0.001 |    0.00 |    2 | 0.1831 |  18.34 KB |        1.94 |
| DirectRefitCall_Batch10     | 20,836.781 μs | 23,562.432 μs | 1,291.5364 μs | 1.009 |    0.14 |    3 |      - |  93.46 KB |        9.86 |
| DirectRefitCall_Baseline    | 21,001.229 μs | 61,621.385 μs | 3,377.6761 μs | 1.017 |    0.20 |    3 |      - |   9.48 KB |        1.00 |
| DirectRefitCall_Sequential5 | 97,402.685 μs | 60,653.667 μs | 3,324.6321 μs | 4.715 |    0.64 |    4 |      - |  46.06 KB |        4.86 |

Benchmarks with issues:
  RestApiRequestHandlerBenchmarks.EncinaRefitCall: ShortRun(IterationCount=3, LaunchCount=1, WarmupCount=3)
