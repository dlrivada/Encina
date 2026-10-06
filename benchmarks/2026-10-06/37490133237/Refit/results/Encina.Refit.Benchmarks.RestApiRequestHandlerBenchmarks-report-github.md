```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
INTEL XEON PLATINUM 8573C 2.30GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                      | Mean           | Error           | StdDev        | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|---------------------------- |---------------:|----------------:|--------------:|------:|--------:|-----:|-------:|----------:|------------:|
| EncinaRefitCall             |             NA |              NA |            NA |     ? |       ? |    ? |     NA |        NA |           ? |
| EncinaRefitCall_Sequential5 |       9.963 μs |       0.1715 μs |     0.0094 μs | 0.000 |    0.00 |    1 | 0.1068 |   8.79 KB |        0.91 |
| EncinaRefitCall_Batch10     |      33.488 μs |      26.3803 μs |     1.4460 μs | 0.001 |    0.00 |    2 | 0.2136 |  18.34 KB |        1.89 |
| DirectRefitCall_Baseline    |  45,357.929 μs |   7,818.7042 μs |   428.5696 μs | 1.000 |    0.01 |    3 |      - |   9.71 KB |        1.00 |
| DirectRefitCall_Batch10     |  53,727.075 μs |  60,552.3651 μs | 3,319.0795 μs | 1.185 |    0.06 |    4 |      - |  93.74 KB |        9.66 |
| DirectRefitCall_Sequential5 | 202,752.658 μs | 123,659.0217 μs | 6,778.1683 μs | 4.470 |    0.13 |    5 |      - |  46.34 KB |        4.77 |

Benchmarks with issues:
  RestApiRequestHandlerBenchmarks.EncinaRefitCall: ShortRun(IterationCount=3, LaunchCount=1, WarmupCount=3)
