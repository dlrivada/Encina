```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V45 4.18GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                      | Mean          | Error          | StdDev        | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|---------------------------- |--------------:|---------------:|--------------:|------:|--------:|-----:|-------:|----------:|------------:|
| EncinaRefitCall             |            NA |             NA |            NA |     ? |       ? |    ? |     NA |        NA |           ? |
| EncinaRefitCall_Sequential5 |      7.949 μs |      0.8657 μs |     0.0475 μs | 0.000 |    0.00 |    1 | 0.5341 |   8.79 KB |        0.92 |
| EncinaRefitCall_Batch10     |     17.947 μs |      7.3554 μs |     0.4032 μs | 0.001 |    0.00 |    2 | 1.0986 |  18.34 KB |        1.92 |
| DirectRefitCall_Baseline    | 16,687.570 μs | 13,567.9136 μs |   743.7031 μs | 1.001 |    0.06 |    3 |      - |   9.54 KB |        1.00 |
| DirectRefitCall_Batch10     | 27,796.164 μs |  7,726.6754 μs |   423.5252 μs | 1.668 |    0.07 |    4 |      - |  93.54 KB |        9.81 |
| DirectRefitCall_Sequential5 | 94,169.889 μs | 34,279.4622 μs | 1,878.9730 μs | 5.651 |    0.24 |    5 |      - |  45.84 KB |        4.81 |

Benchmarks with issues:
  RestApiRequestHandlerBenchmarks.EncinaRefitCall: ShortRun(IterationCount=3, LaunchCount=1, WarmupCount=3)
