```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                      | Mean          | Error         | StdDev       | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|---------------------------- |--------------:|--------------:|-------------:|------:|--------:|-----:|-------:|----------:|------------:|
| EncinaRefitCall             |            NA |            NA |           NA |     ? |       ? |    ? |     NA |        NA |           ? |
| EncinaRefitCall_Sequential5 |      11.51 μs |      0.718 μs |     0.039 μs | 0.001 |    0.00 |    1 | 0.4425 |   7.42 KB |        0.75 |
| EncinaRefitCall_Batch10     |      26.80 μs |      0.486 μs |     0.027 μs | 0.001 |    0.00 |    2 | 0.9155 |  15.61 KB |        1.58 |
| DirectRefitCall_Baseline    |  21,799.88 μs |  6,247.389 μs |   342.440 μs | 1.000 |    0.02 |    3 |      - |   9.89 KB |        1.00 |
| DirectRefitCall_Batch10     |  26,898.20 μs | 22,547.791 μs | 1,235.921 μs | 1.234 |    0.05 |    4 |      - |  97.67 KB |        9.87 |
| DirectRefitCall_Sequential5 | 107,940.81 μs |  9,009.100 μs |   493.819 μs | 4.952 |    0.07 |    5 |      - |  48.07 KB |        4.86 |

Benchmarks with issues:
  RestApiRequestHandlerBenchmarks.EncinaRefitCall: ShortRun(IterationCount=3, LaunchCount=1, WarmupCount=3)
