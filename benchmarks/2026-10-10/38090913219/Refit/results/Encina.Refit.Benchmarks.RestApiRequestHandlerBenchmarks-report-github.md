```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.59GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                      | Mean         | Error         | StdDev       | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|---------------------------- |-------------:|--------------:|-------------:|------:|--------:|-----:|-------:|----------:|------------:|
| EncinaRefitCall             |           NA |            NA |           NA |     ? |       ? |    ? |     NA |        NA |           ? |
| EncinaRefitCall_Sequential5 |     12.20 μs |      0.212 μs |     0.012 μs | 0.002 |    0.00 |    1 | 0.5341 |   8.79 KB |        0.93 |
| EncinaRefitCall_Batch10     |     34.23 μs |     31.584 μs |     1.731 μs | 0.005 |    0.00 |    2 | 1.0986 |  18.34 KB |        1.95 |
| DirectRefitCall_Baseline    |  6,952.71 μs |  4,460.985 μs |   244.522 μs | 1.001 |    0.04 |    3 |      - |   9.41 KB |        1.00 |
| DirectRefitCall_Batch10     | 16,132.72 μs | 12,826.028 μs |   703.038 μs | 2.322 |    0.11 |    4 |      - |  93.41 KB |        9.93 |
| DirectRefitCall_Sequential5 | 40,977.78 μs | 32,274.050 μs | 1,769.050 μs | 5.899 |    0.28 |    5 |      - |  45.63 KB |        4.85 |

Benchmarks with issues:
  RestApiRequestHandlerBenchmarks.EncinaRefitCall: ShortRun(IterationCount=3, LaunchCount=1, WarmupCount=3)
