```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                      | Mean         | Error         | StdDev       | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|---------------------------- |-------------:|--------------:|-------------:|------:|--------:|-----:|-------:|----------:|------------:|
| EncinaRefitCall             |           NA |            NA |           NA |     ? |       ? |    ? |     NA |        NA |           ? |
| EncinaRefitCall_Sequential5 |     12.68 μs |      0.206 μs |     0.011 μs | 0.000 |    0.00 |    1 | 0.5341 |   8.83 KB |        0.93 |
| EncinaRefitCall_Batch10     |     29.23 μs |      8.277 μs |     0.454 μs | 0.001 |    0.00 |    2 | 1.0986 |  18.42 KB |        1.94 |
| DirectRefitCall_Baseline    | 26,929.95 μs | 12,151.810 μs |   666.082 μs | 1.000 |    0.03 |    3 |      - |   9.51 KB |        1.00 |
| DirectRefitCall_Batch10     | 30,903.22 μs | 52,833.656 μs | 2,895.991 μs | 1.148 |    0.10 |    3 |      - |  93.63 KB |        9.85 |
| DirectRefitCall_Sequential5 | 72,459.79 μs | 34,543.537 μs | 1,893.448 μs | 2.692 |    0.08 |    4 |      - |  45.78 KB |        4.82 |

Benchmarks with issues:
  RestApiRequestHandlerBenchmarks.EncinaRefitCall: ShortRun(IterationCount=3, LaunchCount=1, WarmupCount=3)
