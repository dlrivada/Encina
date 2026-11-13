```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 2.77GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                      | Mean         | Error         | StdDev       | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|---------------------------- |-------------:|--------------:|-------------:|------:|--------:|-----:|-------:|----------:|------------:|
| EncinaRefitCall             |           NA |            NA |           NA |     ? |       ? |    ? |     NA |        NA |           ? |
| EncinaRefitCall_Sequential5 |     12.41 μs |      0.379 μs |     0.021 μs | 0.001 |    0.00 |    1 | 0.5341 |   8.83 KB |        0.94 |
| EncinaRefitCall_Batch10     |     27.24 μs |      4.356 μs |     0.239 μs | 0.002 |    0.00 |    2 | 1.0986 |  18.42 KB |        1.96 |
| DirectRefitCall_Baseline    | 11,726.58 μs | 12,588.806 μs |   690.035 μs | 1.002 |    0.07 |    3 |      - |   9.42 KB |        1.00 |
| DirectRefitCall_Batch10     | 17,050.29 μs | 18,503.302 μs | 1,014.228 μs | 1.457 |    0.11 |    4 |      - |  93.49 KB |        9.92 |
| DirectRefitCall_Sequential5 | 62,491.46 μs | 72,122.042 μs | 3,953.252 μs | 5.342 |    0.40 |    5 |      - |  45.77 KB |        4.86 |

Benchmarks with issues:
  RestApiRequestHandlerBenchmarks.EncinaRefitCall: ShortRun(IterationCount=3, LaunchCount=1, WarmupCount=3)
