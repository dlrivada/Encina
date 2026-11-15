```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V74 3.70GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]    : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  MediumRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=MediumRun  IterationCount=15  LaunchCount=2  
WarmupCount=10  

```
| Method                      | Mean         | Error        | StdDev       | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|---------------------------- |-------------:|-------------:|-------------:|------:|--------:|-----:|-------:|----------:|------------:|
| EncinaRefitCall             |           NA |           NA |           NA |     ? |       ? |    ? |     NA |        NA |           ? |
| EncinaRefitCall_Sequential5 |     10.29 μs |     0.142 μs |     0.199 μs | 0.001 |    0.00 |    1 | 0.5341 |   8.83 KB |        0.93 |
| EncinaRefitCall_Batch10     |     28.39 μs |     2.439 μs |     3.650 μs | 0.002 |    0.00 |    2 | 1.0986 |  18.42 KB |        1.93 |
| DirectRefitCall_Baseline    | 17,115.04 μs |   733.689 μs | 1,052.234 μs | 1.003 |    0.08 |    3 |      - |   9.54 KB |        1.00 |
| DirectRefitCall_Batch10     | 22,323.49 μs |   602.064 μs |   824.112 μs | 1.309 |    0.09 |    4 |      - |  94.29 KB |        9.89 |
| DirectRefitCall_Sequential5 | 93,887.68 μs | 5,537.591 μs | 7,762.930 μs | 5.505 |    0.55 |    5 |      - |   45.8 KB |        4.80 |

Benchmarks with issues:
  RestApiRequestHandlerBenchmarks.EncinaRefitCall: MediumRun(IterationCount=15, LaunchCount=2, WarmupCount=10)
