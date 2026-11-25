```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 3.24GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                      | Mean         | Error         | StdDev       | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|---------------------------- |-------------:|--------------:|-------------:|------:|--------:|-----:|-------:|----------:|------------:|
| EncinaRefitCall             |           NA |            NA |           NA |     ? |       ? |    ? |     NA |        NA |           ? |
| EncinaRefitCall_Sequential5 |     11.66 μs |      0.758 μs |     0.042 μs | 0.001 |    0.00 |    1 | 0.5341 |   8.83 KB |        0.93 |
| EncinaRefitCall_Batch10     |     42.25 μs |     12.655 μs |     0.694 μs | 0.002 |    0.00 |    2 | 1.0986 |  18.42 KB |        1.95 |
| DirectRefitCall_Baseline    | 19,329.38 μs |    955.855 μs |    52.394 μs | 1.000 |    0.00 |    3 |      - |   9.47 KB |        1.00 |
| DirectRefitCall_Batch10     | 21,306.13 μs | 39,058.504 μs | 2,140.928 μs | 1.102 |    0.10 |    3 |      - |  93.74 KB |        9.90 |
| DirectRefitCall_Sequential5 | 52,278.71 μs | 53,040.024 μs | 2,907.303 μs | 2.705 |    0.13 |    4 |      - |  45.88 KB |        4.85 |

Benchmarks with issues:
  RestApiRequestHandlerBenchmarks.EncinaRefitCall: ShortRun(IterationCount=3, LaunchCount=1, WarmupCount=3)
