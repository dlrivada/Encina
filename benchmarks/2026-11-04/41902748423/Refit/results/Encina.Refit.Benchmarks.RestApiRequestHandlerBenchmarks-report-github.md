```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V74 3.69GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                      | Mean          | Error          | StdDev       | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|---------------------------- |--------------:|---------------:|-------------:|------:|--------:|-----:|-------:|----------:|------------:|
| EncinaRefitCall             |            NA |             NA |           NA |     ? |       ? |    ? |     NA |        NA |           ? |
| EncinaRefitCall_Sequential5 |      10.49 μs |       2.499 μs |     0.137 μs | 0.001 |    0.00 |    1 | 0.5341 |   8.79 KB |        0.92 |
| EncinaRefitCall_Batch10     |      29.34 μs |      63.593 μs |     3.486 μs | 0.001 |    0.00 |    2 | 1.0986 |  18.34 KB |        1.92 |
| DirectRefitCall_Baseline    |  20,952.88 μs |  16,893.424 μs |   925.986 μs | 1.001 |    0.05 |    3 |      - |   9.53 KB |        1.00 |
| DirectRefitCall_Batch10     |  25,538.64 μs |  17,438.581 μs |   955.867 μs | 1.220 |    0.06 |    4 |      - |   93.8 KB |        9.84 |
| DirectRefitCall_Sequential5 | 100,218.07 μs | 130,352.318 μs | 7,145.050 μs | 4.789 |    0.35 |    5 |      - |  45.88 KB |        4.81 |

Benchmarks with issues:
  RestApiRequestHandlerBenchmarks.EncinaRefitCall: ShortRun(IterationCount=3, LaunchCount=1, WarmupCount=3)
