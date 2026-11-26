```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V74 2.85GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                      | Mean         | Error         | StdDev       | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|---------------------------- |-------------:|--------------:|-------------:|------:|--------:|-----:|-------:|----------:|------------:|
| EncinaRefitCall             |           NA |            NA |           NA |     ? |       ? |    ? |     NA |        NA |           ? |
| EncinaRefitCall_Sequential5 |     13.18 μs |      0.178 μs |     0.010 μs | 0.001 |    0.00 |    1 | 0.5341 |   8.83 KB |        0.93 |
| EncinaRefitCall_Batch10     |     33.11 μs |     72.379 μs |     3.967 μs | 0.003 |    0.00 |    2 | 1.0986 |  18.42 KB |        1.94 |
| DirectRefitCall_Baseline    | 12,837.63 μs |  7,060.384 μs |   387.003 μs | 1.001 |    0.04 |    3 |      - |   9.52 KB |        1.00 |
| DirectRefitCall_Batch10     | 28,181.46 μs | 26,497.984 μs | 1,452.444 μs | 2.197 |    0.11 |    4 |      - |  93.57 KB |        9.83 |
| DirectRefitCall_Sequential5 | 58,651.53 μs | 57,534.482 μs | 3,153.659 μs | 4.571 |    0.24 |    5 |      - |  45.72 KB |        4.80 |

Benchmarks with issues:
  RestApiRequestHandlerBenchmarks.EncinaRefitCall: ShortRun(IterationCount=3, LaunchCount=1, WarmupCount=3)
