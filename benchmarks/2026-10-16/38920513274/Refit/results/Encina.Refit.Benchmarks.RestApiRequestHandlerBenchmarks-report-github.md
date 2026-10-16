```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 4.18GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                      | Mean           | Error         | StdDev      | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|---------------------------- |---------------:|--------------:|------------:|------:|--------:|-----:|-------:|----------:|------------:|
| EncinaRefitCall             |             NA |            NA |          NA |     ? |       ? |    ? |     NA |        NA |           ? |
| EncinaRefitCall_Sequential5 |       7.735 μs |      1.175 μs |   0.0644 μs | 0.000 |    0.00 |    1 | 0.5341 |   8.79 KB |        0.93 |
| EncinaRefitCall_Batch10     |      20.607 μs |     56.546 μs |   3.0995 μs | 0.001 |    0.00 |    2 | 1.0986 |  18.34 KB |        1.94 |
| DirectRefitCall_Baseline    |  30,933.052 μs |  4,197.192 μs | 230.0622 μs | 1.000 |    0.01 |    3 |      - |   9.47 KB |        1.00 |
| DirectRefitCall_Batch10     |  32,760.404 μs | 15,953.442 μs | 874.4620 μs | 1.059 |    0.03 |    3 |      - |  93.73 KB |        9.90 |
| DirectRefitCall_Sequential5 | 153,642.997 μs | 10,013.924 μs | 548.8970 μs | 4.967 |    0.04 |    4 |      - |  46.13 KB |        4.87 |

Benchmarks with issues:
  RestApiRequestHandlerBenchmarks.EncinaRefitCall: ShortRun(IterationCount=3, LaunchCount=1, WarmupCount=3)
