```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                      | Mean          | Error         | StdDev       | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|---------------------------- |--------------:|--------------:|-------------:|------:|--------:|-----:|-------:|----------:|------------:|
| EncinaRefitCall             |            NA |            NA |           NA |     ? |       ? |    ? |     NA |        NA |           ? |
| EncinaRefitCall_Sequential5 |      12.00 μs |      1.955 μs |     0.107 μs | 0.000 |    0.00 |    1 | 0.5341 |   8.79 KB |        0.91 |
| EncinaRefitCall_Batch10     |      30.04 μs |     16.149 μs |     0.885 μs | 0.000 |    0.00 |    2 | 1.0986 |  18.34 KB |        1.89 |
| DirectRefitCall_Baseline    |  60,887.06 μs | 27,740.122 μs | 1,520.530 μs | 1.000 |    0.03 |    3 |      - |    9.7 KB |        1.00 |
| DirectRefitCall_Batch10     |  69,765.64 μs | 70,306.411 μs | 3,853.732 μs | 1.146 |    0.06 |    3 |      - |  93.94 KB |        9.69 |
| DirectRefitCall_Sequential5 | 301,713.72 μs | 44,359.887 μs | 2,431.515 μs | 4.957 |    0.11 |    4 |      - |  46.79 KB |        4.82 |

Benchmarks with issues:
  RestApiRequestHandlerBenchmarks.EncinaRefitCall: ShortRun(IterationCount=3, LaunchCount=1, WarmupCount=3)
