```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                      | Mean         | Error         | StdDev     | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|---------------------------- |-------------:|--------------:|-----------:|------:|--------:|-----:|-------:|----------:|------------:|
| EncinaRefitCall             |           NA |            NA |         NA |     ? |       ? |    ? |     NA |        NA |           ? |
| DirectRefitCall_Sequential5 |           NA |            NA |         NA |     ? |       ? |    ? |     NA |        NA |           ? |
| EncinaRefitCall_Sequential5 |     12.75 μs |      0.719 μs |   0.039 μs | 0.000 |    0.00 |    1 | 0.5341 |   8.79 KB |        0.91 |
| EncinaRefitCall_Batch10     |     32.06 μs |     16.227 μs |   0.889 μs | 0.001 |    0.00 |    2 | 1.0986 |  18.34 KB |        1.90 |
| DirectRefitCall_Baseline    | 50,614.47 μs |  6,023.854 μs | 330.188 μs | 1.000 |    0.01 |    3 |      - |   9.67 KB |        1.00 |
| DirectRefitCall_Batch10     | 56,373.42 μs | 10,175.618 μs | 557.760 μs | 1.114 |    0.01 |    3 |      - |  93.72 KB |        9.69 |

Benchmarks with issues:
  RestApiRequestHandlerBenchmarks.EncinaRefitCall: ShortRun(IterationCount=3, LaunchCount=1, WarmupCount=3)
  RestApiRequestHandlerBenchmarks.DirectRefitCall_Sequential5: ShortRun(IterationCount=3, LaunchCount=1, WarmupCount=3)
