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
| EncinaRefitCall_Sequential5 |     11.13 μs |      0.065 μs |     0.004 μs | 0.001 |    0.00 |    1 | 0.4425 |   7.42 KB |        0.75 |
| EncinaRefitCall_Batch10     |     27.26 μs |     27.257 μs |     1.494 μs | 0.003 |    0.00 |    2 | 0.9460 |  15.61 KB |        1.58 |
| DirectRefitCall_Baseline    |  8,973.00 μs |  1,267.033 μs |    69.450 μs | 1.000 |    0.01 |    3 |      - |   9.87 KB |        1.00 |
| DirectRefitCall_Batch10     | 27,522.66 μs |  7,382.628 μs |   404.667 μs | 3.067 |    0.04 |    4 |      - |  97.62 KB |        9.90 |
| DirectRefitCall_Sequential5 | 48,000.87 μs | 36,162.877 μs | 1,982.209 μs | 5.350 |    0.19 |    5 |      - |  47.77 KB |        4.84 |

Benchmarks with issues:
  RestApiRequestHandlerBenchmarks.EncinaRefitCall: ShortRun(IterationCount=3, LaunchCount=1, WarmupCount=3)
