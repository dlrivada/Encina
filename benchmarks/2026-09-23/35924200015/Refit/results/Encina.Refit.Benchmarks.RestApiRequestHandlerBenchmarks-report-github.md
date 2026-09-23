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
| EncinaRefitCall_Sequential5 |     11.06 μs |      0.457 μs |     0.025 μs | 0.001 |    0.00 |    1 | 0.4425 |   7.42 KB |        0.75 |
| EncinaRefitCall_Batch10     |     27.24 μs |     24.296 μs |     1.332 μs | 0.003 |    0.00 |    2 | 0.9460 |  15.61 KB |        1.58 |
| DirectRefitCall_Baseline    |  9,610.34 μs | 33,313.033 μs | 1,826.000 μs | 1.023 |    0.23 |    3 |      - |   9.88 KB |        1.00 |
| DirectRefitCall_Batch10     | 17,717.34 μs | 48,434.606 μs | 2,654.864 μs | 1.886 |    0.38 |    4 |      - |  98.37 KB |        9.96 |
| DirectRefitCall_Sequential5 | 41,537.73 μs | 20,828.837 μs | 1,141.699 μs | 4.421 |    0.69 |    5 |      - |  47.73 KB |        4.83 |

Benchmarks with issues:
  RestApiRequestHandlerBenchmarks.EncinaRefitCall: ShortRun(IterationCount=3, LaunchCount=1, WarmupCount=3)
