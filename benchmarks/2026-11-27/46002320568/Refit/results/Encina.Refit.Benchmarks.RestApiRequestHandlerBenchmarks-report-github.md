```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
INTEL XEON PLATINUM 8573C 3.50GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                      | Mean          | Error          | StdDev        | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|---------------------------- |--------------:|---------------:|--------------:|------:|--------:|-----:|-------:|----------:|------------:|
| EncinaRefitCall             |            NA |             NA |            NA |     ? |       ? |    ? |     NA |        NA |           ? |
| EncinaRefitCall_Sequential5 |      7.998 μs |      0.6758 μs |     0.0370 μs | 0.000 |    0.00 |    1 | 0.1068 |   8.83 KB |        0.93 |
| EncinaRefitCall_Batch10     |     19.994 μs |      4.0097 μs |     0.2198 μs | 0.001 |    0.00 |    2 | 0.2136 |  18.42 KB |        1.94 |
| DirectRefitCall_Baseline    | 16,952.231 μs |  7,806.2385 μs |   427.8863 μs | 1.000 |    0.03 |    3 |      - |   9.48 KB |        1.00 |
| DirectRefitCall_Batch10     | 22,247.521 μs | 16,442.5435 μs |   901.2713 μs | 1.313 |    0.05 |    4 |      - |  93.49 KB |        9.86 |
| DirectRefitCall_Sequential5 | 89,926.647 μs | 27,460.7740 μs | 1,505.2177 μs | 5.307 |    0.14 |    5 |      - |  54.13 KB |        5.71 |

Benchmarks with issues:
  RestApiRequestHandlerBenchmarks.EncinaRefitCall: ShortRun(IterationCount=3, LaunchCount=1, WarmupCount=3)
