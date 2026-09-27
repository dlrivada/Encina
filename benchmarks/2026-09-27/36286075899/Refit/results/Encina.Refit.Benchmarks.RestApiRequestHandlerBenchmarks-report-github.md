```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]    : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  MediumRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

Job=MediumRun  IterationCount=15  LaunchCount=2  
WarmupCount=10  

```
| Method                      | Mean         | Error        | StdDev       | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|---------------------------- |-------------:|-------------:|-------------:|------:|--------:|-----:|-------:|----------:|------------:|
| EncinaRefitCall             |           NA |           NA |           NA |     ? |       ? |    ? |     NA |        NA |           ? |
| EncinaRefitCall_Sequential5 |     10.70 μs |     0.063 μs |     0.092 μs | 0.001 |    0.00 |    1 | 0.4272 |   7.15 KB |        0.72 |
| EncinaRefitCall_Batch10     |     25.34 μs |     0.601 μs |     0.823 μs | 0.002 |    0.00 |    2 | 0.9155 |  15.06 KB |        1.52 |
| DirectRefitCall_Baseline    | 10,841.36 μs |   177.904 μs |   260.769 μs | 1.001 |    0.03 |    3 |      - |   9.88 KB |        1.00 |
| DirectRefitCall_Batch10     | 15,284.63 μs |   600.092 μs |   879.608 μs | 1.411 |    0.09 |    4 |      - |   97.6 KB |        9.88 |
| DirectRefitCall_Sequential5 | 55,807.49 μs | 1,112.209 μs | 1,664.702 μs | 5.151 |    0.19 |    5 |      - |  48.01 KB |        4.86 |

Benchmarks with issues:
  RestApiRequestHandlerBenchmarks.EncinaRefitCall: MediumRun(IterationCount=15, LaunchCount=2, WarmupCount=10)
