```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                      | Mean         | Error         | StdDev       | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|---------------------------- |-------------:|--------------:|-------------:|------:|--------:|-----:|-------:|----------:|------------:|
| EncinaRefitCall             |           NA |            NA |           NA |     ? |       ? |    ? |     NA |        NA |           ? |
| EncinaRefitCall_Sequential5 |     11.79 μs |      0.265 μs |     0.015 μs | 0.001 |    0.00 |    1 | 0.5341 |   8.79 KB |        0.93 |
| EncinaRefitCall_Batch10     |     26.99 μs |     19.384 μs |     1.062 μs | 0.002 |    0.00 |    2 | 1.0986 |  18.34 KB |        1.95 |
| DirectRefitCall_Baseline    | 11,433.50 μs |  6,515.143 μs |   357.117 μs | 1.001 |    0.04 |    3 |      - |   9.42 KB |        1.00 |
| DirectRefitCall_Batch10     | 14,469.20 μs |  5,766.365 μs |   316.074 μs | 1.266 |    0.04 |    4 |      - |  93.46 KB |        9.92 |
| DirectRefitCall_Sequential5 | 42,959.33 μs | 41,119.157 μs | 2,253.880 μs | 3.760 |    0.20 |    5 |      - |  45.74 KB |        4.85 |

Benchmarks with issues:
  RestApiRequestHandlerBenchmarks.EncinaRefitCall: ShortRun(IterationCount=3, LaunchCount=1, WarmupCount=3)
