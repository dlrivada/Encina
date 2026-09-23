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
| EncinaRefitCall_Sequential5 |     10.49 μs |      0.968 μs |     0.053 μs | 0.001 |    0.00 |    1 | 0.4425 |   7.42 KB |        0.75 |
| EncinaRefitCall_Batch10     |     26.19 μs |     26.157 μs |     1.434 μs | 0.003 |    0.00 |    2 | 0.9460 |  15.61 KB |        1.59 |
| DirectRefitCall_Baseline    |  8,248.68 μs |  8,744.208 μs |   479.300 μs | 1.002 |    0.07 |    3 |      - |   9.84 KB |        1.00 |
| DirectRefitCall_Batch10     | 29,702.58 μs | 10,568.377 μs |   579.288 μs | 3.609 |    0.19 |    4 |      - |  97.82 KB |        9.94 |
| DirectRefitCall_Sequential5 | 45,135.87 μs | 84,218.487 μs | 4,616.299 μs | 5.484 |    0.56 |    5 |      - |  47.98 KB |        4.87 |

Benchmarks with issues:
  RestApiRequestHandlerBenchmarks.EncinaRefitCall: ShortRun(IterationCount=3, LaunchCount=1, WarmupCount=3)
