```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V74 3.69GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                      | Mean          | Error          | StdDev        | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|---------------------------- |--------------:|---------------:|--------------:|------:|--------:|-----:|-------:|----------:|------------:|
| EncinaRefitCall             |            NA |             NA |            NA |     ? |       ? |    ? |     NA |        NA |           ? |
| EncinaRefitCall_Sequential5 |      10.20 μs |       1.051 μs |      0.058 μs | 0.000 |    0.00 |    1 | 0.5341 |   8.83 KB |        0.92 |
| EncinaRefitCall_Batch10     |      26.34 μs |      95.182 μs |      5.217 μs | 0.001 |    0.00 |    2 | 1.0986 |  18.42 KB |        1.93 |
| DirectRefitCall_Baseline    |  35,233.83 μs |   4,579.887 μs |    251.039 μs | 1.000 |    0.01 |    3 |      - |   9.56 KB |        1.00 |
| DirectRefitCall_Batch10     |  50,724.93 μs | 188,711.635 μs | 10,343.922 μs | 1.440 |    0.25 |    4 |      - |  93.65 KB |        9.80 |
| DirectRefitCall_Sequential5 | 185,521.95 μs |  62,776.841 μs |  3,441.010 μs | 5.266 |    0.09 |    5 |      - |  46.36 KB |        4.85 |

Benchmarks with issues:
  RestApiRequestHandlerBenchmarks.EncinaRefitCall: ShortRun(IterationCount=3, LaunchCount=1, WarmupCount=3)
