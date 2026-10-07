```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
Intel Xeon Platinum 8370C CPU 2.80GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                      | Mean          | Error          | StdDev        | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|---------------------------- |--------------:|---------------:|--------------:|------:|--------:|-----:|-------:|----------:|------------:|
| EncinaRefitCall             |            NA |             NA |            NA |     ? |       ? |    ? |     NA |        NA |           ? |
| EncinaRefitCall_Sequential5 |      9.610 μs |      0.1040 μs |     0.0057 μs | 0.001 |    0.00 |    1 | 0.3510 |   8.79 KB |        0.93 |
| EncinaRefitCall_Batch10     |     24.933 μs |     29.3028 μs |     1.6062 μs | 0.002 |    0.00 |    2 | 0.7324 |  18.34 KB |        1.93 |
| DirectRefitCall_Baseline    | 16,435.217 μs | 21,965.6002 μs | 1,204.0087 μs | 1.003 |    0.09 |    3 |      - |    9.5 KB |        1.00 |
| DirectRefitCall_Batch10     | 23,210.690 μs | 16,943.6485 μs |   928.7386 μs | 1.417 |    0.10 |    4 |      - |  93.46 KB |        9.84 |
| DirectRefitCall_Sequential5 | 84,040.557 μs | 64,830.7876 μs | 3,553.5942 μs | 5.131 |    0.37 |    5 |      - |     46 KB |        4.84 |

Benchmarks with issues:
  RestApiRequestHandlerBenchmarks.EncinaRefitCall: ShortRun(IterationCount=3, LaunchCount=1, WarmupCount=3)
