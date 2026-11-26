```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V74 3.69GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                      | Mean         | Error         | StdDev       | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|---------------------------- |-------------:|--------------:|-------------:|------:|--------:|-----:|-------:|----------:|------------:|
| EncinaRefitCall             |           NA |            NA |           NA |     ? |       ? |    ? |     NA |        NA |           ? |
| EncinaRefitCall_Sequential5 |     10.08 μs |      0.334 μs |     0.018 μs | 0.001 |    0.00 |    1 | 0.5341 |   8.83 KB |        0.94 |
| EncinaRefitCall_Batch10     |     25.99 μs |     95.878 μs |     5.255 μs | 0.002 |    0.00 |    2 | 1.0986 |  18.42 KB |        1.96 |
| DirectRefitCall_Baseline    | 10,693.63 μs |  1,024.494 μs |    56.156 μs | 1.000 |    0.01 |    3 |      - |   9.42 KB |        1.00 |
| DirectRefitCall_Batch10     | 15,874.22 μs | 20,466.258 μs | 1,121.825 μs | 1.484 |    0.09 |    4 |      - |  93.39 KB |        9.91 |
| DirectRefitCall_Sequential5 | 57,599.99 μs | 11,706.372 μs |   641.666 μs | 5.386 |    0.06 |    5 |      - |   45.7 KB |        4.85 |

Benchmarks with issues:
  RestApiRequestHandlerBenchmarks.EncinaRefitCall: ShortRun(IterationCount=3, LaunchCount=1, WarmupCount=3)
