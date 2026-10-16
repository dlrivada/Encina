```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
INTEL XEON PLATINUM 8573C 3.15GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                      | Mean          | Error           | StdDev        | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|---------------------------- |--------------:|----------------:|--------------:|------:|--------:|-----:|-------:|----------:|------------:|
| EncinaRefitCall             |            NA |              NA |            NA |     ? |       ? |    ? |     NA |        NA |           ? |
| EncinaRefitCall_Sequential5 |      9.997 μs |       0.1395 μs |     0.0076 μs | 0.001 |    0.00 |    1 | 0.1068 |   8.79 KB |        0.93 |
| EncinaRefitCall_Batch10     |     25.536 μs |      19.7567 μs |     1.0829 μs | 0.002 |    0.00 |    2 | 0.2136 |  18.34 KB |        1.94 |
| DirectRefitCall_Baseline    | 15,055.421 μs |   5,603.3567 μs |   307.1389 μs | 1.000 |    0.02 |    3 |      - |   9.44 KB |        1.00 |
| DirectRefitCall_Batch10     | 26,089.294 μs | 156,284.8162 μs | 8,566.4982 μs | 1.733 |    0.49 |    4 |      - |   93.8 KB |        9.94 |
| DirectRefitCall_Sequential5 | 83,311.357 μs |  31,046.5828 μs | 1,701.7680 μs | 5.535 |    0.14 |    5 |      - |  45.81 KB |        4.85 |

Benchmarks with issues:
  RestApiRequestHandlerBenchmarks.EncinaRefitCall: ShortRun(IterationCount=3, LaunchCount=1, WarmupCount=3)
