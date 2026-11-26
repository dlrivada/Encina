```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
Intel Xeon 6975P-C 3.20GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                      | Mean          | Error          | StdDev        | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|---------------------------- |--------------:|---------------:|--------------:|------:|--------:|-----:|-------:|----------:|------------:|
| EncinaRefitCall             |            NA |             NA |            NA |     ? |       ? |    ? |     NA |        NA |           ? |
| EncinaRefitCall_Sequential5 |      7.707 μs |      0.6145 μs |     0.0337 μs | 0.001 |    0.00 |    1 | 0.1068 |   8.83 KB |        0.94 |
| EncinaRefitCall_Batch10     |     18.784 μs |      1.6116 μs |     0.0883 μs | 0.002 |    0.00 |    2 | 0.2136 |  18.42 KB |        1.95 |
| DirectRefitCall_Baseline    | 11,658.109 μs |  7,507.5081 μs |   411.5119 μs | 1.001 |    0.04 |    3 |      - |   9.42 KB |        1.00 |
| DirectRefitCall_Batch10     | 19,464.099 μs | 38,607.8694 μs | 2,116.2276 μs | 1.671 |    0.17 |    4 |      - |  93.73 KB |        9.94 |
| DirectRefitCall_Sequential5 | 78,552.991 μs | 25,776.9360 μs | 1,412.9208 μs | 6.744 |    0.23 |    5 |      - |  45.83 KB |        4.86 |

Benchmarks with issues:
  RestApiRequestHandlerBenchmarks.EncinaRefitCall: ShortRun(IterationCount=3, LaunchCount=1, WarmupCount=3)
