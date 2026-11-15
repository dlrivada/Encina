```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V45 4.26GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                      | Mean          | Error          | StdDev      | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|---------------------------- |--------------:|---------------:|------------:|------:|--------:|-----:|-------:|----------:|------------:|
| EncinaRefitCall             |            NA |             NA |          NA |     ? |       ? |    ? |     NA |        NA |           ? |
| EncinaRefitCall_Sequential5 |      7.856 μs |      0.7396 μs |   0.0405 μs | 0.000 |    0.00 |    1 | 0.5341 |   8.83 KB |        0.93 |
| EncinaRefitCall_Batch10     |     20.466 μs |     35.0693 μs |   1.9223 μs | 0.001 |    0.00 |    2 | 1.0986 |  18.42 KB |        1.95 |
| DirectRefitCall_Baseline    | 16,851.957 μs |  3,045.1489 μs | 166.9149 μs | 1.000 |    0.01 |    3 |      - |   9.46 KB |        1.00 |
| DirectRefitCall_Batch10     | 20,658.756 μs | 14,269.5412 μs | 782.1617 μs | 1.226 |    0.04 |    4 |      - |  93.46 KB |        9.87 |
| DirectRefitCall_Sequential5 | 86,537.991 μs | 15,195.4984 μs | 832.9165 μs | 5.136 |    0.06 |    5 |      - |  45.84 KB |        4.84 |

Benchmarks with issues:
  RestApiRequestHandlerBenchmarks.EncinaRefitCall: ShortRun(IterationCount=3, LaunchCount=1, WarmupCount=3)
