```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                      | Mean         | Error          | StdDev       | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|---------------------------- |-------------:|---------------:|-------------:|------:|--------:|-----:|-------:|----------:|------------:|
| EncinaRefitCall             |           NA |             NA |           NA |     ? |       ? |    ? |     NA |        NA |           ? |
| EncinaRefitCall_Sequential5 |     10.26 μs |       0.310 μs |     0.017 μs | 0.001 |    0.00 |    1 | 0.5341 |   8.79 KB |        0.93 |
| EncinaRefitCall_Batch10     |     23.14 μs |      12.372 μs |     0.678 μs | 0.001 |    0.00 |    2 | 1.0986 |  18.34 KB |        1.94 |
| DirectRefitCall_Baseline    | 18,138.78 μs |  23,082.430 μs | 1,265.226 μs | 1.003 |    0.08 |    3 |      - |   9.47 KB |        1.00 |
| DirectRefitCall_Batch10     | 29,486.26 μs | 156,142.104 μs | 8,558.676 μs | 1.631 |    0.42 |    4 |      - |  93.33 KB |        9.86 |
| DirectRefitCall_Sequential5 | 95,232.19 μs |  80,302.093 μs | 4,401.629 μs | 5.267 |    0.37 |    5 |      - |  45.88 KB |        4.85 |

Benchmarks with issues:
  RestApiRequestHandlerBenchmarks.EncinaRefitCall: ShortRun(IterationCount=3, LaunchCount=1, WarmupCount=3)
