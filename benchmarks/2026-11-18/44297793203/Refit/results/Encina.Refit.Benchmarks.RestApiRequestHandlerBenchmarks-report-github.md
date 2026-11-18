```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
INTEL XEON PLATINUM 8573C 2.92GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                      | Mean          | Error         | StdDev       | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|---------------------------- |--------------:|--------------:|-------------:|------:|--------:|-----:|-------:|----------:|------------:|
| EncinaRefitCall             |            NA |            NA |           NA |     ? |       ? |    ? |     NA |        NA |           ? |
| EncinaRefitCall_Sequential5 |      10.43 μs |      4.150 μs |     0.227 μs | 0.001 |    0.00 |    1 | 0.1068 |   8.83 KB |        0.93 |
| EncinaRefitCall_Batch10     |      32.05 μs |     75.633 μs |     4.146 μs | 0.002 |    0.00 |    2 | 0.2136 |  18.42 KB |        1.95 |
| DirectRefitCall_Baseline    |  17,173.66 μs |  6,401.880 μs |   350.909 μs | 1.000 |    0.03 |    3 |      - |   9.46 KB |        1.00 |
| DirectRefitCall_Batch10     |  23,503.34 μs | 49,845.534 μs | 2,732.202 μs | 1.369 |    0.14 |    4 |      - |  93.44 KB |        9.87 |
| DirectRefitCall_Sequential5 | 105,903.46 μs | 13,762.499 μs |   754.369 μs | 6.168 |    0.12 |    5 |      - |  45.92 KB |        4.85 |

Benchmarks with issues:
  RestApiRequestHandlerBenchmarks.EncinaRefitCall: ShortRun(IterationCount=3, LaunchCount=1, WarmupCount=3)
