```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
INTEL XEON PLATINUM 8573C 3.07GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                      | Mean         | Error          | StdDev       | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|---------------------------- |-------------:|---------------:|-------------:|------:|--------:|-----:|-------:|----------:|------------:|
| EncinaRefitCall             |           NA |             NA |           NA |     ? |       ? |    ? |     NA |        NA |           ? |
| EncinaRefitCall_Sequential5 |     10.11 μs |       2.944 μs |     0.161 μs | 0.001 |    0.00 |    1 | 0.1068 |   8.83 KB |        0.93 |
| EncinaRefitCall_Batch10     |     31.88 μs |      49.263 μs |     2.700 μs | 0.002 |    0.00 |    2 | 0.2136 |  18.42 KB |        1.95 |
| DirectRefitCall_Baseline    | 17,657.76 μs |  10,949.357 μs |   600.171 μs | 1.001 |    0.04 |    3 |      - |   9.47 KB |        1.00 |
| DirectRefitCall_Batch10     | 27,512.05 μs | 129,381.287 μs | 7,091.825 μs | 1.559 |    0.35 |    4 |      - |  93.71 KB |        9.90 |
| DirectRefitCall_Sequential5 | 89,763.37 μs |  33,838.158 μs | 1,854.784 μs | 5.087 |    0.18 |    5 |      - |  45.88 KB |        4.85 |

Benchmarks with issues:
  RestApiRequestHandlerBenchmarks.EncinaRefitCall: ShortRun(IterationCount=3, LaunchCount=1, WarmupCount=3)
