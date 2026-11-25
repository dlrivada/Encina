```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
Intel Xeon 6975P-C 2.70GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                      | Mean          | Error          | StdDev        | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|---------------------------- |--------------:|---------------:|--------------:|------:|--------:|-----:|-------:|----------:|------------:|
| EncinaRefitCall             |            NA |             NA |            NA |     ? |       ? |    ? |     NA |        NA |           ? |
| EncinaRefitCall_Sequential5 |      7.582 μs |      0.3450 μs |     0.0189 μs | 0.001 |    0.00 |    1 | 0.1068 |   8.83 KB |        0.94 |
| EncinaRefitCall_Batch10     |     18.755 μs |     15.4994 μs |     0.8496 μs | 0.001 |    0.00 |    2 | 0.2136 |  18.42 KB |        1.95 |
| DirectRefitCall_Baseline    | 14,498.933 μs | 11,399.5645 μs |   624.8486 μs | 1.001 |    0.05 |    3 |      - |   9.43 KB |        1.00 |
| DirectRefitCall_Batch10     | 22,051.588 μs | 63,509.7111 μs | 3,481.1816 μs | 1.523 |    0.22 |    4 |      - |   93.6 KB |        9.93 |
| DirectRefitCall_Sequential5 | 68,525.129 μs | 31,269.5419 μs | 1,713.9891 μs | 4.732 |    0.21 |    5 |      - |  45.79 KB |        4.86 |

Benchmarks with issues:
  RestApiRequestHandlerBenchmarks.EncinaRefitCall: ShortRun(IterationCount=3, LaunchCount=1, WarmupCount=3)
