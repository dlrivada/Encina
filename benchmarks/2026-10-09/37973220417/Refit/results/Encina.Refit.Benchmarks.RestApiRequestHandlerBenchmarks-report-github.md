```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                      | Mean          | Error           | StdDev        | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|---------------------------- |--------------:|----------------:|--------------:|------:|--------:|-----:|-------:|----------:|------------:|
| EncinaRefitCall             |            NA |              NA |            NA |     ? |       ? |    ? |     NA |        NA |           ? |
| EncinaRefitCall_Sequential5 |      7.270 μs |       0.4439 μs |     0.0243 μs | 0.000 |    0.00 |    1 | 0.5341 |   8.79 KB |        0.92 |
| EncinaRefitCall_Batch10     |     18.032 μs |      54.9078 μs |     3.0097 μs | 0.001 |    0.00 |    2 | 1.0986 |  18.34 KB |        1.93 |
| DirectRefitCall_Baseline    | 18,879.214 μs |  21,494.2509 μs | 1,178.1724 μs | 1.003 |    0.08 |    3 |      - |   9.51 KB |        1.00 |
| DirectRefitCall_Batch10     | 26,128.203 μs | 108,501.3051 μs | 5,947.3227 μs | 1.387 |    0.28 |    4 |      - |  93.65 KB |        9.85 |
| DirectRefitCall_Sequential5 | 94,280.488 μs |  58,297.9982 μs | 3,195.5100 μs | 5.006 |    0.30 |    5 |      - |  45.89 KB |        4.83 |

Benchmarks with issues:
  RestApiRequestHandlerBenchmarks.EncinaRefitCall: ShortRun(IterationCount=3, LaunchCount=1, WarmupCount=3)
