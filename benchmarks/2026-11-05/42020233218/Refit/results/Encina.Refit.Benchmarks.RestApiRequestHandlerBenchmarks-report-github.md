```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V74 3.69GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                      | Mean          | Error          | StdDev        | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|---------------------------- |--------------:|---------------:|--------------:|------:|--------:|-----:|-------:|----------:|------------:|
| EncinaRefitCall             |            NA |             NA |            NA |     ? |       ? |    ? |     NA |        NA |           ? |
| EncinaRefitCall_Sequential5 |      10.34 μs |       1.645 μs |      0.090 μs | 0.000 |    0.00 |    1 | 0.5341 |   8.79 KB |        0.91 |
| EncinaRefitCall_Batch10     |      23.80 μs |      18.355 μs |      1.006 μs | 0.001 |    0.00 |    2 | 1.0986 |  18.34 KB |        1.90 |
| DirectRefitCall_Baseline    |  33,648.92 μs |  19,178.353 μs |  1,051.230 μs | 1.001 |    0.04 |    3 |      - |   9.67 KB |        1.00 |
| DirectRefitCall_Batch10     |  48,470.82 μs | 193,197.406 μs | 10,589.802 μs | 1.441 |    0.28 |    4 |      - |  93.66 KB |        9.68 |
| DirectRefitCall_Sequential5 | 164,298.33 μs |  64,621.457 μs |  3,542.120 μs | 4.886 |    0.16 |    5 |      - |  46.28 KB |        4.78 |

Benchmarks with issues:
  RestApiRequestHandlerBenchmarks.EncinaRefitCall: ShortRun(IterationCount=3, LaunchCount=1, WarmupCount=3)
