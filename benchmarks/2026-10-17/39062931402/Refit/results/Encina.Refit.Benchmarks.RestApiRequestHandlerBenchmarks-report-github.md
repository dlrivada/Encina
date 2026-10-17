```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                      | Mean         | Error         | StdDev       | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|---------------------------- |-------------:|--------------:|-------------:|------:|--------:|-----:|-------:|----------:|------------:|
| EncinaRefitCall             |           NA |            NA |           NA |     ? |       ? |    ? |     NA |        NA |           ? |
| EncinaRefitCall_Sequential5 |     10.17 μs |      0.866 μs |     0.047 μs | 0.001 |    0.00 |    1 | 0.5341 |   8.79 KB |        0.93 |
| EncinaRefitCall_Batch10     |     22.80 μs |      1.801 μs |     0.099 μs | 0.002 |    0.00 |    2 | 1.0986 |  18.34 KB |        1.95 |
| DirectRefitCall_Baseline    | 10,718.46 μs |  4,042.148 μs |   221.564 μs | 1.000 |    0.03 |    3 |      - |   9.43 KB |        1.00 |
| DirectRefitCall_Batch10     | 17,352.53 μs | 18,987.587 μs | 1,040.774 μs | 1.619 |    0.09 |    4 |      - |  93.56 KB |        9.92 |
| DirectRefitCall_Sequential5 | 43,786.58 μs | 39,655.623 μs | 2,173.658 μs | 4.086 |    0.19 |    5 |      - |  45.77 KB |        4.85 |

Benchmarks with issues:
  RestApiRequestHandlerBenchmarks.EncinaRefitCall: ShortRun(IterationCount=3, LaunchCount=1, WarmupCount=3)
