```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
Intel Xeon Platinum 8370C CPU 2.80GHz (Max: 2.79GHz), 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                      | Mean          | Error          | StdDev        | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|---------------------------- |--------------:|---------------:|--------------:|------:|--------:|-----:|-------:|----------:|------------:|
| EncinaRefitCall             |            NA |             NA |            NA |     ? |       ? |    ? |     NA |        NA |           ? |
| EncinaRefitCall_Sequential5 |      9.919 μs |      0.7384 μs |     0.0405 μs | 0.001 |    0.00 |    1 | 0.3510 |   8.79 KB |        0.92 |
| EncinaRefitCall_Batch10     |     25.842 μs |     35.6773 μs |     1.9556 μs | 0.002 |    0.00 |    2 | 0.7324 |  18.34 KB |        1.92 |
| DirectRefitCall_Baseline    | 15,179.451 μs | 19,209.2196 μs | 1,052.9221 μs | 1.003 |    0.09 |    3 |      - |   9.54 KB |        1.00 |
| DirectRefitCall_Batch10     | 17,614.612 μs | 22,165.2929 μs | 1,214.9545 μs | 1.164 |    0.10 |    3 |      - |  93.55 KB |        9.81 |
| DirectRefitCall_Sequential5 | 73,436.137 μs | 51,059.1908 μs | 2,798.7265 μs | 4.854 |    0.34 |    4 |      - |  45.74 KB |        4.79 |

Benchmarks with issues:
  RestApiRequestHandlerBenchmarks.EncinaRefitCall: ShortRun(IterationCount=3, LaunchCount=1, WarmupCount=3)
