```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
INTEL XEON PLATINUM 8573C 2.30GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                      | Mean          | Error          | StdDev         | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|---------------------------- |--------------:|---------------:|---------------:|------:|--------:|-----:|-------:|----------:|------------:|
| EncinaRefitCall             |            NA |             NA |             NA |     ? |       ? |    ? |     NA |        NA |           ? |
| EncinaRefitCall_Sequential5 |      8.167 μs |       1.914 μs |      0.1049 μs | 0.000 |    0.00 |    1 | 0.0763 |   7.42 KB |        0.75 |
| EncinaRefitCall_Batch10     |     22.589 μs |      18.471 μs |      1.0125 μs | 0.001 |    0.00 |    2 | 0.1831 |  15.61 KB |        1.58 |
| DirectRefitCall_Baseline    | 18,065.497 μs |  30,753.331 μs |  1,685.6938 μs | 1.006 |    0.11 |    3 |      - |   9.89 KB |        1.00 |
| DirectRefitCall_Batch10     | 64,683.054 μs | 379,643.368 μs | 20,809.5341 μs | 3.601 |    1.05 |    4 |      - |  98.58 KB |        9.96 |
| DirectRefitCall_Sequential5 | 77,480.460 μs |  37,732.143 μs |  2,068.2261 μs | 4.314 |    0.36 |    4 |      - |  47.98 KB |        4.85 |

Benchmarks with issues:
  RestApiRequestHandlerBenchmarks.EncinaRefitCall: ShortRun(IterationCount=3, LaunchCount=1, WarmupCount=3)
