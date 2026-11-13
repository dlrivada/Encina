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
| EncinaRefitCall_Sequential5 |     10.39 μs |      0.855 μs |     0.047 μs | 0.001 |    0.00 |    1 | 0.5341 |   8.83 KB |        0.94 |
| EncinaRefitCall_Batch10     |     32.40 μs |      1.528 μs |     0.084 μs | 0.002 |    0.00 |    2 | 1.0986 |  18.42 KB |        1.95 |
| DirectRefitCall_Baseline    | 14,111.88 μs |  6,072.888 μs |   332.875 μs | 1.000 |    0.03 |    3 |      - |   9.43 KB |        1.00 |
| DirectRefitCall_Batch10     | 26,637.05 μs | 28,297.710 μs | 1,551.093 μs | 1.888 |    0.10 |    4 |      - |  93.58 KB |        9.92 |
| DirectRefitCall_Sequential5 | 67,016.13 μs | 35,970.843 μs | 1,971.683 μs | 4.751 |    0.15 |    5 |      - |   45.9 KB |        4.87 |

Benchmarks with issues:
  RestApiRequestHandlerBenchmarks.EncinaRefitCall: ShortRun(IterationCount=3, LaunchCount=1, WarmupCount=3)
