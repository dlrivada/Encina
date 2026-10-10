```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
INTEL XEON PLATINUM 8573C 2.30GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                      | Mean         | Error         | StdDev     | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|---------------------------- |-------------:|--------------:|-----------:|------:|--------:|-----:|-------:|----------:|------------:|
| EncinaRefitCall             |           NA |            NA |         NA |     ? |       ? |    ? |     NA |        NA |           ? |
| EncinaRefitCall_Sequential5 |     10.00 μs |      0.062 μs |   0.003 μs | 0.001 |    0.00 |    1 | 0.1068 |   8.79 KB |        0.93 |
| EncinaRefitCall_Batch10     |     26.19 μs |     12.888 μs |   0.706 μs | 0.002 |    0.00 |    2 | 0.2136 |  18.34 KB |        1.94 |
| DirectRefitCall_Baseline    | 14,156.17 μs |  9,787.474 μs | 536.484 μs | 1.001 |    0.05 |    3 |      - |   9.45 KB |        1.00 |
| DirectRefitCall_Batch10     | 21,439.68 μs |  5,591.644 μs | 306.497 μs | 1.516 |    0.05 |    4 |      - |  93.46 KB |        9.90 |
| DirectRefitCall_Sequential5 | 72,463.86 μs | 15,414.484 μs | 844.920 μs | 5.124 |    0.18 |    5 |      - |  45.83 KB |        4.85 |

Benchmarks with issues:
  RestApiRequestHandlerBenchmarks.EncinaRefitCall: ShortRun(IterationCount=3, LaunchCount=1, WarmupCount=3)
