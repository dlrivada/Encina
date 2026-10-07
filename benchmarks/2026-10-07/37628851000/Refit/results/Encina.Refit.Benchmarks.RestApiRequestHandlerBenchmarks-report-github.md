```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                      | Mean          | Error         | StdDev      | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|---------------------------- |--------------:|--------------:|------------:|------:|--------:|-----:|-------:|----------:|------------:|
| EncinaRefitCall             |            NA |            NA |          NA |     ? |       ? |    ? |     NA |        NA |           ? |
| EncinaRefitCall_Sequential5 |      9.997 μs |      1.253 μs |   0.0687 μs | 0.001 |    0.00 |    1 | 0.5341 |   8.79 KB |        0.93 |
| EncinaRefitCall_Batch10     |     23.301 μs |     12.295 μs |   0.6739 μs | 0.001 |    0.00 |    2 | 1.0986 |  18.34 KB |        1.94 |
| DirectRefitCall_Baseline    | 16,123.274 μs | 14,526.492 μs | 796.2460 μs | 1.002 |    0.06 |    3 |      - |   9.46 KB |        1.00 |
| DirectRefitCall_Batch10     | 21,415.816 μs |  4,535.657 μs | 248.6147 μs | 1.330 |    0.06 |    4 |      - |  93.55 KB |        9.89 |
| DirectRefitCall_Sequential5 | 77,566.519 μs | 14,066.900 μs | 771.0542 μs | 4.819 |    0.21 |    5 |      - |  45.83 KB |        4.84 |

Benchmarks with issues:
  RestApiRequestHandlerBenchmarks.EncinaRefitCall: ShortRun(IterationCount=3, LaunchCount=1, WarmupCount=3)
