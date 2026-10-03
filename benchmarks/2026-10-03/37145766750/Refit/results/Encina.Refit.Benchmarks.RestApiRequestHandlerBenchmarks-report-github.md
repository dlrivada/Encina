```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                      | Mean          | Error         | StdDev       | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|---------------------------- |--------------:|--------------:|-------------:|------:|--------:|-----:|-------:|----------:|------------:|
| EncinaRefitCall             |            NA |            NA |           NA |     ? |       ? |    ? |     NA |        NA |           ? |
| EncinaRefitCall_Sequential5 |      10.81 μs |      0.433 μs |     0.024 μs | 0.001 |    0.00 |    1 | 0.4272 |   7.15 KB |        0.75 |
| EncinaRefitCall_Batch10     |      24.90 μs |      2.856 μs |     0.157 μs | 0.001 |    0.00 |    2 | 0.9155 |  15.06 KB |        1.59 |
| DirectRefitCall_Baseline    |  21,330.53 μs |  1,393.556 μs |    76.385 μs | 1.000 |    0.00 |    3 |      - |   9.48 KB |        1.00 |
| DirectRefitCall_Batch10     |  24,533.28 μs |  5,987.963 μs |   328.220 μs | 1.150 |    0.01 |    3 |      - |  93.59 KB |        9.88 |
| DirectRefitCall_Sequential5 | 116,439.72 μs | 28,163.155 μs | 1,543.718 μs | 5.459 |    0.06 |    4 |      - |  45.97 KB |        4.85 |

Benchmarks with issues:
  RestApiRequestHandlerBenchmarks.EncinaRefitCall: ShortRun(IterationCount=3, LaunchCount=1, WarmupCount=3)
