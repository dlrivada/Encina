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
| EncinaRefitCall_Sequential5 |     10.59 μs |      0.695 μs |     0.038 μs | 0.001 |    0.00 |    1 | 0.4425 |   7.42 KB |        0.75 |
| EncinaRefitCall_Batch10     |     25.92 μs |     11.750 μs |     0.644 μs | 0.002 |    0.00 |    2 | 0.9460 |  15.61 KB |        1.58 |
| DirectRefitCall_Baseline    | 12,303.61 μs |  4,458.817 μs |   244.403 μs | 1.000 |    0.02 |    3 |      - |   9.85 KB |        1.00 |
| DirectRefitCall_Batch10     | 19,172.64 μs | 26,273.662 μs | 1,440.148 μs | 1.559 |    0.10 |    4 |      - |  97.57 KB |        9.91 |
| DirectRefitCall_Sequential5 | 64,086.34 μs | 17,930.397 μs |   982.826 μs | 5.210 |    0.11 |    5 |      - |  47.85 KB |        4.86 |

Benchmarks with issues:
  RestApiRequestHandlerBenchmarks.EncinaRefitCall: ShortRun(IterationCount=3, LaunchCount=1, WarmupCount=3)
