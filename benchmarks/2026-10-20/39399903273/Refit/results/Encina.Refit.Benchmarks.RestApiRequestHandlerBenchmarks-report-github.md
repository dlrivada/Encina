```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                      | Mean         | Error         | StdDev       | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|---------------------------- |-------------:|--------------:|-------------:|------:|--------:|-----:|-------:|----------:|------------:|
| EncinaRefitCall             |           NA |            NA |           NA |     ? |       ? |    ? |     NA |        NA |           ? |
| EncinaRefitCall_Sequential5 |     12.43 μs |      0.510 μs |     0.028 μs | 0.001 |    0.00 |    1 | 0.5341 |   8.79 KB |        0.93 |
| EncinaRefitCall_Batch10     |     32.88 μs |     80.426 μs |     4.408 μs | 0.004 |    0.00 |    2 | 1.0986 |  18.34 KB |        1.95 |
| DirectRefitCall_Baseline    |  9,115.96 μs |  6,012.488 μs |   329.565 μs | 1.001 |    0.04 |    3 |      - |   9.43 KB |        1.00 |
| DirectRefitCall_Batch10     | 10,952.26 μs |  5,388.304 μs |   295.351 μs | 1.202 |    0.05 |    4 |      - |  93.33 KB |        9.90 |
| DirectRefitCall_Sequential5 | 44,754.84 μs | 27,152.746 μs | 1,488.334 μs | 4.914 |    0.21 |    5 |      - |  45.65 KB |        4.84 |

Benchmarks with issues:
  RestApiRequestHandlerBenchmarks.EncinaRefitCall: ShortRun(IterationCount=3, LaunchCount=1, WarmupCount=3)
