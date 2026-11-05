```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V74 3.68GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                      | Mean          | Error          | StdDev       | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|---------------------------- |--------------:|---------------:|-------------:|------:|--------:|-----:|-------:|----------:|------------:|
| EncinaRefitCall             |            NA |             NA |           NA |     ? |       ? |    ? |     NA |        NA |           ? |
| EncinaRefitCall_Sequential5 |      10.43 μs |       0.635 μs |     0.035 μs | 0.000 |    0.00 |    1 | 0.5341 |   8.79 KB |        0.91 |
| EncinaRefitCall_Batch10     |      23.27 μs |       2.883 μs |     0.158 μs | 0.001 |    0.00 |    2 | 1.0986 |  18.34 KB |        1.90 |
| DirectRefitCall_Baseline    |  35,504.19 μs |  13,694.712 μs |   750.653 μs | 1.000 |    0.03 |    3 |      - |   9.66 KB |        1.00 |
| DirectRefitCall_Batch10     |  62,899.33 μs | 163,645.435 μs | 8,969.959 μs | 1.772 |    0.22 |    4 |      - |   94.2 KB |        9.76 |
| DirectRefitCall_Sequential5 | 177,516.93 μs |  64,442.756 μs | 3,532.325 μs | 5.001 |    0.13 |    5 |      - |  46.31 KB |        4.80 |

Benchmarks with issues:
  RestApiRequestHandlerBenchmarks.EncinaRefitCall: ShortRun(IterationCount=3, LaunchCount=1, WarmupCount=3)
