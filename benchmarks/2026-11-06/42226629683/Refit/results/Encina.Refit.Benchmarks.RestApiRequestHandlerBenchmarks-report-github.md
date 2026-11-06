```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V74 2.87GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                      | Mean          | Error         | StdDev     | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|---------------------------- |--------------:|--------------:|-----------:|------:|--------:|-----:|-------:|----------:|------------:|
| EncinaRefitCall             |            NA |            NA |         NA |     ? |       ? |    ? |     NA |        NA |           ? |
| EncinaRefitCall_Sequential5 |      13.18 μs |      1.560 μs |   0.086 μs | 0.000 |    0.00 |    1 | 0.5341 |   8.79 KB |        0.93 |
| EncinaRefitCall_Batch10     |      32.05 μs |     29.058 μs |   1.593 μs | 0.001 |    0.00 |    2 | 1.0986 |  18.34 KB |        1.94 |
| DirectRefitCall_Baseline    |  26,748.09 μs |  7,321.743 μs | 401.329 μs | 1.000 |    0.02 |    3 |      - |   9.47 KB |        1.00 |
| DirectRefitCall_Batch10     |  28,304.68 μs |  6,334.222 μs | 347.200 μs | 1.058 |    0.02 |    3 |      - |  93.49 KB |        9.88 |
| DirectRefitCall_Sequential5 | 139,619.83 μs | 11,922.521 μs | 653.514 μs | 5.221 |    0.07 |    4 |      - |   46.1 KB |        4.87 |

Benchmarks with issues:
  RestApiRequestHandlerBenchmarks.EncinaRefitCall: ShortRun(IterationCount=3, LaunchCount=1, WarmupCount=3)
