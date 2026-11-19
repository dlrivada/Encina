```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V74 3.54GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                      | Mean         | Error         | StdDev       | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|---------------------------- |-------------:|--------------:|-------------:|------:|--------:|-----:|-------:|----------:|------------:|
| EncinaRefitCall             |           NA |            NA |           NA |     ? |       ? |    ? |     NA |        NA |           ? |
| EncinaRefitCall_Sequential5 |     10.34 μs |      1.159 μs |     0.064 μs | 0.001 |    0.00 |    1 | 0.5341 |   8.83 KB |        0.94 |
| EncinaRefitCall_Batch10     |     31.96 μs |     25.207 μs |     1.382 μs | 0.004 |    0.00 |    2 | 1.0986 |  18.42 KB |        1.95 |
| DirectRefitCall_Baseline    |  8,457.93 μs |  3,234.797 μs |   177.310 μs | 1.000 |    0.03 |    3 |      - |   9.43 KB |        1.00 |
| DirectRefitCall_Batch10     | 26,872.51 μs | 32,450.347 μs | 1,778.713 μs | 3.178 |    0.19 |    4 |      - |   93.5 KB |        9.92 |
| DirectRefitCall_Sequential5 | 41,611.24 μs | 31,355.712 μs | 1,718.712 μs | 4.921 |    0.20 |    5 |      - |  45.71 KB |        4.85 |

Benchmarks with issues:
  RestApiRequestHandlerBenchmarks.EncinaRefitCall: ShortRun(IterationCount=3, LaunchCount=1, WarmupCount=3)
