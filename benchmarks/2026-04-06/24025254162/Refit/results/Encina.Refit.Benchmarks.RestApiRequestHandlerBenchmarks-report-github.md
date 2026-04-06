```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]   : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                      | Mean          | Error          | StdDev        | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|---------------------------- |--------------:|---------------:|--------------:|------:|--------:|-----:|-------:|----------:|------------:|
| EncinaRefitCall             |            NA |             NA |            NA |     ? |       ? |    ? |     NA |        NA |           ? |
| EncinaRefitCall_Sequential5 |      4.411 μs |      0.3071 μs |     0.0168 μs | 0.001 |    0.00 |    1 | 0.2975 |   4.92 KB |        0.50 |
| EncinaRefitCall_Batch10     |     12.983 μs |     10.3731 μs |     0.5686 μs | 0.002 |    0.00 |    2 | 0.6409 |  10.61 KB |        1.08 |
| DirectRefitCall_Baseline    |  6,188.461 μs |  2,814.6132 μs |   154.2784 μs | 1.000 |    0.03 |    3 |      - |   9.84 KB |        1.00 |
| DirectRefitCall_Batch10     |  8,800.361 μs |  6,470.6171 μs |   354.6764 μs | 1.423 |    0.06 |    4 |      - |   97.6 KB |        9.92 |
| DirectRefitCall_Sequential5 | 33,533.770 μs | 27,691.6927 μs | 1,517.8751 μs | 5.421 |    0.24 |    5 |      - |  47.71 KB |        4.85 |

Benchmarks with issues:
  RestApiRequestHandlerBenchmarks.EncinaRefitCall: ShortRun(IterationCount=3, LaunchCount=1, WarmupCount=3)
