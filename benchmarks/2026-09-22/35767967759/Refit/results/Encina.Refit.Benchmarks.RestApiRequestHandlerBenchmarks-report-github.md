```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                      | Mean           | Error         | StdDev        | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|---------------------------- |---------------:|--------------:|--------------:|------:|--------:|-----:|-------:|----------:|------------:|
| EncinaRefitCall             |             NA |            NA |            NA |     ? |       ? |    ? |     NA |        NA |           ? |
| EncinaRefitCall_Sequential5 |       4.121 μs |      1.086 μs |     0.0595 μs | 0.000 |    0.00 |    1 | 0.2975 |   4.92 KB |        0.50 |
| EncinaRefitCall_Batch10     |      10.806 μs |      2.423 μs |     0.1328 μs | 0.000 |    0.00 |    2 | 0.6409 |  10.61 KB |        1.07 |
| DirectRefitCall_Baseline    |  25,632.042 μs |  6,590.405 μs |   361.2424 μs | 1.000 |    0.02 |    3 |      - |   9.89 KB |        1.00 |
| DirectRefitCall_Batch10     |  36,448.778 μs | 15,816.383 μs |   866.9493 μs | 1.422 |    0.03 |    4 |      - |  98.13 KB |        9.92 |
| DirectRefitCall_Sequential5 | 121,167.459 μs | 29,827.531 μs | 1,634.9476 μs | 4.728 |    0.08 |    5 |      - |  48.12 KB |        4.86 |

Benchmarks with issues:
  RestApiRequestHandlerBenchmarks.EncinaRefitCall: ShortRun(IterationCount=3, LaunchCount=1, WarmupCount=3)
