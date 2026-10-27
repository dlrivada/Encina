```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
Intel Xeon 6973P-C 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                      | Mean          | Error          | StdDev        | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|---------------------------- |--------------:|---------------:|--------------:|------:|--------:|-----:|-------:|----------:|------------:|
| EncinaRefitCall             |            NA |             NA |            NA |     ? |       ? |    ? |     NA |        NA |           ? |
| EncinaRefitCall_Sequential5 |      7.203 μs |      0.3664 μs |     0.0201 μs | 0.000 |    0.00 |    1 | 0.1068 |   8.79 KB |        0.92 |
| EncinaRefitCall_Batch10     |     17.678 μs |      6.3192 μs |     0.3464 μs | 0.001 |    0.00 |    2 | 0.2136 |  18.34 KB |        1.92 |
| DirectRefitCall_Baseline    | 18,135.190 μs |  7,652.7884 μs |   419.4752 μs | 1.000 |    0.03 |    3 |      - |   9.53 KB |        1.00 |
| DirectRefitCall_Batch10     | 22,009.148 μs | 11,568.4248 μs |   634.1044 μs | 1.214 |    0.04 |    4 |      - |   93.5 KB |        9.81 |
| DirectRefitCall_Sequential5 | 79,592.455 μs | 68,078.0927 μs | 3,731.5900 μs | 4.390 |    0.20 |    5 |      - |  45.82 KB |        4.81 |

Benchmarks with issues:
  RestApiRequestHandlerBenchmarks.EncinaRefitCall: ShortRun(IterationCount=3, LaunchCount=1, WarmupCount=3)
