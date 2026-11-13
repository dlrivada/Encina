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
| EncinaRefitCall_Sequential5 |     10.50 μs |      1.134 μs |     0.062 μs | 0.001 |    0.00 |    1 | 0.5341 |   8.83 KB |        0.94 |
| EncinaRefitCall_Batch10     |     28.46 μs |     80.885 μs |     4.434 μs | 0.002 |    0.00 |    2 | 1.0986 |  18.42 KB |        1.95 |
| DirectRefitCall_Baseline    | 11,506.00 μs |  3,977.366 μs |   218.013 μs | 1.000 |    0.02 |    3 |      - |   9.43 KB |        1.00 |
| DirectRefitCall_Batch10     | 32,369.81 μs | 79,507.764 μs | 4,358.089 μs | 2.814 |    0.33 |    4 |      - |  93.47 KB |        9.91 |
| DirectRefitCall_Sequential5 | 65,121.37 μs | 11,398.949 μs |   624.815 μs | 5.661 |    0.10 |    5 |      - |  45.76 KB |        4.85 |

Benchmarks with issues:
  RestApiRequestHandlerBenchmarks.EncinaRefitCall: ShortRun(IterationCount=3, LaunchCount=1, WarmupCount=3)
