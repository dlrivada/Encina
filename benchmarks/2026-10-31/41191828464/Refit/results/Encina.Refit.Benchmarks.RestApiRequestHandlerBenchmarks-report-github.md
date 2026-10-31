```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
Intel Xeon Platinum 8370C CPU 2.80GHz (Max: 2.77GHz), 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                      | Mean          | Error         | StdDev        | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|---------------------------- |--------------:|--------------:|--------------:|------:|--------:|-----:|-------:|----------:|------------:|
| EncinaRefitCall             |            NA |            NA |            NA |     ? |       ? |    ? |     NA |        NA |           ? |
| EncinaRefitCall_Sequential5 |      9.933 μs |      1.515 μs |     0.0830 μs | 0.001 |    0.00 |    1 | 0.3510 |   8.79 KB |        0.93 |
| EncinaRefitCall_Batch10     |     27.635 μs |     73.128 μs |     4.0084 μs | 0.002 |    0.00 |    2 | 0.7324 |  18.34 KB |        1.95 |
| DirectRefitCall_Baseline    | 14,795.496 μs |  2,688.711 μs |   147.3773 μs | 1.000 |    0.01 |    3 |      - |   9.42 KB |        1.00 |
| DirectRefitCall_Batch10     | 28,097.687 μs | 39,294.630 μs | 2,153.8713 μs | 1.899 |    0.13 |    4 |      - |  94.33 KB |       10.01 |
| DirectRefitCall_Sequential5 | 70,061.793 μs |  9,688.721 μs |   531.0715 μs | 4.736 |    0.05 |    5 |      - |  45.76 KB |        4.86 |

Benchmarks with issues:
  RestApiRequestHandlerBenchmarks.EncinaRefitCall: ShortRun(IterationCount=3, LaunchCount=1, WarmupCount=3)
