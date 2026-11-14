```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V74 3.68GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                      | Mean          | Error          | StdDev        | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|---------------------------- |--------------:|---------------:|--------------:|------:|--------:|-----:|-------:|----------:|------------:|
| EncinaRefitCall             |            NA |             NA |            NA |     ? |       ? |    ? |     NA |        NA |           ? |
| EncinaRefitCall_Sequential5 |      9.977 μs |      0.6202 μs |     0.0340 μs | 0.001 |    0.00 |    1 | 0.5341 |   8.83 KB |        0.94 |
| EncinaRefitCall_Batch10     |     23.528 μs |     15.5896 μs |     0.8545 μs | 0.001 |    0.00 |    2 | 1.0986 |  18.42 KB |        1.96 |
| DirectRefitCall_Baseline    | 15,800.954 μs |  6,747.5095 μs |   369.8538 μs | 1.000 |    0.03 |    3 |      - |   9.42 KB |        1.00 |
| DirectRefitCall_Batch10     | 20,898.196 μs | 14,129.7575 μs |   774.4997 μs | 1.323 |    0.05 |    4 |      - |  93.54 KB |        9.93 |
| DirectRefitCall_Sequential5 | 85,285.348 μs | 43,539.5215 μs | 2,386.5481 μs | 5.399 |    0.17 |    5 |      - |  45.87 KB |        4.87 |

Benchmarks with issues:
  RestApiRequestHandlerBenchmarks.EncinaRefitCall: ShortRun(IterationCount=3, LaunchCount=1, WarmupCount=3)
