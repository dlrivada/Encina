```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
INTEL XEON PLATINUM 8573C 2.30GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                      | Mean         | Error        | StdDev     | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|---------------------------- |-------------:|-------------:|-----------:|------:|--------:|-----:|-------:|----------:|------------:|
| EncinaRefitCall             |           NA |           NA |         NA |     ? |       ? |    ? |     NA |        NA |           ? |
| EncinaRefitCall_Sequential5 |     10.08 μs |     5.748 μs |   0.315 μs | 0.001 |    0.00 |    1 | 0.1068 |   8.79 KB |        0.93 |
| EncinaRefitCall_Batch10     |     29.76 μs |    58.296 μs |   3.195 μs | 0.002 |    0.00 |    2 | 0.1831 |  18.34 KB |        1.94 |
| DirectRefitCall_Baseline    | 14,546.52 μs | 2,484.969 μs | 136.210 μs | 1.000 |    0.01 |    3 |      - |   9.43 KB |        1.00 |
| DirectRefitCall_Batch10     | 16,319.79 μs | 9,029.649 μs | 494.946 μs | 1.122 |    0.03 |    3 |      - |  93.29 KB |        9.89 |
| DirectRefitCall_Sequential5 | 60,861.25 μs | 8,761.203 μs | 480.231 μs | 4.184 |    0.04 |    4 |      - |  45.74 KB |        4.85 |

Benchmarks with issues:
  RestApiRequestHandlerBenchmarks.EncinaRefitCall: ShortRun(IterationCount=3, LaunchCount=1, WarmupCount=3)
