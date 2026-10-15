```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                      | Mean          | Error            | StdDev         | Ratio  | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|---------------------------- |--------------:|-----------------:|---------------:|-------:|--------:|-----:|-------:|----------:|------------:|
| EncinaRefitCall             |            NA |               NA |             NA |      ? |       ? |    ? |     NA |        NA |           ? |
| EncinaRefitCall_Sequential5 |      11.98 μs |         0.664 μs |       0.036 μs |  0.001 |    0.00 |    1 | 0.5341 |   8.79 KB |        0.93 |
| EncinaRefitCall_Batch10     |      30.51 μs |        24.756 μs |       1.357 μs |  0.001 |    0.00 |    2 | 1.0986 |  18.34 KB |        1.94 |
| DirectRefitCall_Baseline    |  22,317.74 μs |    34,044.078 μs |   1,866.071 μs |  1.004 |    0.10 |    3 |      - |   9.47 KB |        1.00 |
| DirectRefitCall_Batch10     |  46,344.79 μs |   320,055.961 μs |  17,543.347 μs |  2.086 |    0.70 |    4 |      - |  93.77 KB |        9.91 |
| DirectRefitCall_Sequential5 | 268,258.42 μs | 1,879,194.687 μs | 103,005.002 μs | 12.074 |    4.11 |    5 |      - |  46.59 KB |        4.92 |

Benchmarks with issues:
  RestApiRequestHandlerBenchmarks.EncinaRefitCall: ShortRun(IterationCount=3, LaunchCount=1, WarmupCount=3)
