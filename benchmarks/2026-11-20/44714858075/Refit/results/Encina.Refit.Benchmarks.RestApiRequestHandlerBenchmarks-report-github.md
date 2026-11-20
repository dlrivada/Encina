```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V45 4.51GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                      | Mean          | Error          | StdDev        | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|---------------------------- |--------------:|---------------:|--------------:|------:|--------:|-----:|-------:|----------:|------------:|
| EncinaRefitCall             |            NA |             NA |            NA |     ? |       ? |    ? |     NA |        NA |           ? |
| EncinaRefitCall_Sequential5 |      7.570 μs |      0.4771 μs |     0.0262 μs | 0.000 |    0.00 |    1 | 0.5341 |   8.83 KB |        0.93 |
| EncinaRefitCall_Batch10     |     19.523 μs |     43.0245 μs |     2.3583 μs | 0.001 |    0.00 |    2 | 1.0986 |  18.42 KB |        1.94 |
| DirectRefitCall_Baseline    | 15,573.830 μs |  6,737.2054 μs |   369.2890 μs | 1.000 |    0.03 |    3 |      - |    9.5 KB |        1.00 |
| DirectRefitCall_Batch10     | 18,343.298 μs | 20,675.5223 μs | 1,133.2952 μs | 1.178 |    0.07 |    3 |      - |   93.7 KB |        9.86 |
| DirectRefitCall_Sequential5 | 83,107.678 μs | 88,476.7795 μs | 4,849.7109 μs | 5.338 |    0.29 |    4 |      - |  45.98 KB |        4.84 |

Benchmarks with issues:
  RestApiRequestHandlerBenchmarks.EncinaRefitCall: ShortRun(IterationCount=3, LaunchCount=1, WarmupCount=3)
