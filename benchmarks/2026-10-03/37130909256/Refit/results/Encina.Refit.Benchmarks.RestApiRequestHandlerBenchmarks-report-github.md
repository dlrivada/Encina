```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 2.46GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                      | Mean         | Error         | StdDev       | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|---------------------------- |-------------:|--------------:|-------------:|------:|--------:|-----:|-------:|----------:|------------:|
| EncinaRefitCall             |           NA |            NA |           NA |     ? |       ? |    ? |     NA |        NA |           ? |
| EncinaRefitCall_Sequential5 |     13.53 μs |      2.285 μs |     0.125 μs | 0.001 |    0.00 |    1 | 0.4272 |   7.15 KB |        0.75 |
| EncinaRefitCall_Batch10     |     27.06 μs |      5.022 μs |     0.275 μs | 0.001 |    0.00 |    2 | 0.9155 |  15.06 KB |        1.59 |
| DirectRefitCall_Baseline    | 20,124.67 μs | 23,427.036 μs | 1,284.115 μs | 1.003 |    0.08 |    3 |      - |   9.48 KB |        1.00 |
| DirectRefitCall_Batch10     | 27,873.29 μs | 41,776.119 μs | 2,289.890 μs | 1.389 |    0.12 |    4 |      - |   93.5 KB |        9.87 |
| DirectRefitCall_Sequential5 | 81,773.68 μs | 27,084.856 μs | 1,484.612 μs | 4.074 |    0.23 |    5 |      - |  45.81 KB |        4.83 |

Benchmarks with issues:
  RestApiRequestHandlerBenchmarks.EncinaRefitCall: ShortRun(IterationCount=3, LaunchCount=1, WarmupCount=3)
