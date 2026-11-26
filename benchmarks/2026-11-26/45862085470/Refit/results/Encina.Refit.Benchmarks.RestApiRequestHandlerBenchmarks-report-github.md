```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V74 3.65GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                      | Mean         | Error         | StdDev       | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|---------------------------- |-------------:|--------------:|-------------:|------:|--------:|-----:|-------:|----------:|------------:|
| EncinaRefitCall             |           NA |            NA |           NA |     ? |       ? |    ? |     NA |        NA |           ? |
| EncinaRefitCall_Sequential5 |     10.18 μs |      0.471 μs |     0.026 μs | 0.001 |    0.00 |    1 | 0.5341 |   8.83 KB |        0.94 |
| EncinaRefitCall_Batch10     |     23.90 μs |      5.655 μs |     0.310 μs | 0.002 |    0.00 |    2 | 1.0986 |  18.42 KB |        1.95 |
| DirectRefitCall_Baseline    | 12,487.44 μs | 10,713.376 μs |   587.236 μs | 1.001 |    0.06 |    3 |      - |   9.43 KB |        1.00 |
| DirectRefitCall_Batch10     | 24,546.29 μs | 98,708.423 μs | 5,410.542 μs | 1.969 |    0.38 |    4 |      - |  93.62 KB |        9.92 |
| DirectRefitCall_Sequential5 | 64,850.26 μs | 35,040.692 μs | 1,920.699 μs | 5.201 |    0.25 |    5 |      - |  45.82 KB |        4.86 |

Benchmarks with issues:
  RestApiRequestHandlerBenchmarks.EncinaRefitCall: ShortRun(IterationCount=3, LaunchCount=1, WarmupCount=3)
