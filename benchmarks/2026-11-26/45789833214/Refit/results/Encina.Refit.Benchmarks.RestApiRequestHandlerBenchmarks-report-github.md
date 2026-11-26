```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
Intel Xeon 6973P-C 4.19GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                      | Mean          | Error         | StdDev        | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|---------------------------- |--------------:|--------------:|--------------:|------:|--------:|-----:|-------:|----------:|------------:|
| EncinaRefitCall             |            NA |            NA |            NA |     ? |       ? |    ? |     NA |        NA |           ? |
| EncinaRefitCall_Sequential5 |      7.509 μs |     11.077 μs |     0.6072 μs | 0.001 |    0.00 |    1 | 0.1068 |   8.83 KB |        0.94 |
| EncinaRefitCall_Batch10     |     17.606 μs |     29.408 μs |     1.6120 μs | 0.001 |    0.00 |    2 | 0.2136 |  18.42 KB |        1.95 |
| DirectRefitCall_Baseline    | 12,664.760 μs |    367.583 μs |    20.1484 μs | 1.000 |    0.00 |    3 |      - |   9.43 KB |        1.00 |
| DirectRefitCall_Batch10     | 15,330.193 μs | 19,409.655 μs | 1,063.9087 μs | 1.210 |    0.07 |    3 |      - |  93.43 KB |        9.91 |
| DirectRefitCall_Sequential5 | 63,317.947 μs | 40,316.131 μs | 2,209.8632 μs | 5.000 |    0.15 |    4 |      - |  47.77 KB |        5.07 |

Benchmarks with issues:
  RestApiRequestHandlerBenchmarks.EncinaRefitCall: ShortRun(IterationCount=3, LaunchCount=1, WarmupCount=3)
