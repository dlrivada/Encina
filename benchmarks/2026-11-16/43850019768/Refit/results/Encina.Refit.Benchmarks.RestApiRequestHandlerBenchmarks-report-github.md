```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V74 3.69GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                      | Mean         | Error          | StdDev        | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|---------------------------- |-------------:|---------------:|--------------:|------:|--------:|-----:|-------:|----------:|------------:|
| EncinaRefitCall             |           NA |             NA |            NA |     ? |       ? |    ? |     NA |        NA |           ? |
| EncinaRefitCall_Sequential5 |     10.14 μs |       1.527 μs |      0.084 μs | 0.001 |    0.00 |    1 | 0.5341 |   8.83 KB |        0.94 |
| EncinaRefitCall_Batch10     |     26.73 μs |      46.553 μs |      2.552 μs | 0.002 |    0.00 |    2 | 1.0986 |  18.42 KB |        1.96 |
| DirectRefitCall_Baseline    | 14,454.97 μs |   6,741.136 μs |    369.504 μs | 1.000 |    0.03 |    3 |      - |   9.42 KB |        1.00 |
| DirectRefitCall_Batch10     | 21,631.79 μs |  49,324.184 μs |  2,703.625 μs | 1.497 |    0.17 |    4 |      - |  93.49 KB |        9.92 |
| DirectRefitCall_Sequential5 | 84,747.81 μs | 221,565.924 μs | 12,144.776 μs | 5.865 |    0.74 |    5 |      - |  45.81 KB |        4.86 |

Benchmarks with issues:
  RestApiRequestHandlerBenchmarks.EncinaRefitCall: ShortRun(IterationCount=3, LaunchCount=1, WarmupCount=3)
