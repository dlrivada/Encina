```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V45 4.29GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                      | Mean           | Error          | StdDev        | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|---------------------------- |---------------:|---------------:|--------------:|------:|--------:|-----:|-------:|----------:|------------:|
| EncinaRefitCall             |             NA |             NA |            NA |     ? |       ? |    ? |     NA |        NA |           ? |
| EncinaRefitCall_Sequential5 |       7.738 μs |      0.5915 μs |     0.0324 μs | 0.000 |    0.00 |    1 | 0.5341 |   8.79 KB |        0.93 |
| EncinaRefitCall_Batch10     |      19.773 μs |     43.9849 μs |     2.4110 μs | 0.001 |    0.00 |    2 | 1.0986 |  18.34 KB |        1.93 |
| DirectRefitCall_Baseline    |  16,690.909 μs |  8,576.7781 μs |   470.1222 μs | 1.001 |    0.03 |    3 |      - |    9.5 KB |        1.00 |
| DirectRefitCall_Batch10     |  35,623.078 μs | 15,458.4289 μs |   847.3287 μs | 2.135 |    0.07 |    4 |      - |  93.66 KB |        9.86 |
| DirectRefitCall_Sequential5 | 126,508.380 μs | 51,586.9426 μs | 2,827.6544 μs | 7.583 |    0.23 |    5 |      - |  46.12 KB |        4.86 |

Benchmarks with issues:
  RestApiRequestHandlerBenchmarks.EncinaRefitCall: ShortRun(IterationCount=3, LaunchCount=1, WarmupCount=3)
