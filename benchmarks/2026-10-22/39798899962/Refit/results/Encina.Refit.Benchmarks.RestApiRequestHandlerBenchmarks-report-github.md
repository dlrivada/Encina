```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
Intel Xeon 6973P-C 3.58GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                      | Mean           | Error             | StdDev          | Median         | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|---------------------------- |---------------:|------------------:|----------------:|---------------:|------:|--------:|-----:|-------:|----------:|------------:|
| EncinaRefitCall             |             NA |                NA |              NA |             NA |     ? |       ? |    ? |     NA |        NA |           ? |
| EncinaRefitCall_Sequential5 |       8.491 μs |         0.4860 μs |       0.0266 μs |       8.495 μs | 0.000 |    0.00 |    1 | 0.1068 |   8.79 KB |        0.91 |
| EncinaRefitCall_Batch10     |      26.535 μs |        30.2962 μs |       1.6606 μs |      27.267 μs | 0.001 |    0.00 |    2 | 0.2136 |  18.34 KB |        1.91 |
| DirectRefitCall_Baseline    |  32,578.323 μs |    11,987.0891 μs |     657.0528 μs |  32,693.590 μs | 1.000 |    0.02 |    3 |      - |   9.62 KB |        1.00 |
| DirectRefitCall_Batch10     |  46,746.676 μs |    14,546.2651 μs |     797.3299 μs |  46,481.703 μs | 1.435 |    0.03 |    4 |      - |  93.69 KB |        9.74 |
| DirectRefitCall_Sequential5 | 280,293.439 μs | 3,424,842.0579 μs | 187,727.1506 μs | 178,083.461 μs | 8.606 |    4.99 |    5 |      - |  46.34 KB |        4.82 |

Benchmarks with issues:
  RestApiRequestHandlerBenchmarks.EncinaRefitCall: ShortRun(IterationCount=3, LaunchCount=1, WarmupCount=3)
