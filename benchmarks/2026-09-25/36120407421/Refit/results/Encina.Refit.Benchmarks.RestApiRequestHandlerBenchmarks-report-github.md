```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 3.24GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                      | Mean         | Error         | StdDev       | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|---------------------------- |-------------:|--------------:|-------------:|------:|--------:|-----:|-------:|----------:|------------:|
| EncinaRefitCall             |           NA |            NA |           NA |     ? |       ? |    ? |     NA |        NA |           ? |
| EncinaRefitCall_Sequential5 |     10.44 μs |      1.404 μs |     0.077 μs | 0.001 |    0.00 |    1 | 0.4272 |   7.15 KB |        0.73 |
| EncinaRefitCall_Batch10     |     25.13 μs |     24.489 μs |     1.342 μs | 0.002 |    0.00 |    2 | 0.9155 |  15.06 KB |        1.53 |
| DirectRefitCall_Baseline    | 11,224.52 μs |  6,691.186 μs |   366.766 μs | 1.001 |    0.04 |    3 |      - |   9.85 KB |        1.00 |
| DirectRefitCall_Batch10     | 14,240.30 μs | 10,920.744 μs |   598.603 μs | 1.270 |    0.06 |    4 |      - |  97.56 KB |        9.90 |
| DirectRefitCall_Sequential5 | 56,780.07 μs | 25,703.861 μs | 1,408.915 μs | 5.062 |    0.18 |    5 |      - |  47.79 KB |        4.85 |

Benchmarks with issues:
  RestApiRequestHandlerBenchmarks.EncinaRefitCall: ShortRun(IterationCount=3, LaunchCount=1, WarmupCount=3)
