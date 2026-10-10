```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 4.20GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                      | Mean           | Error         | StdDev        | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|---------------------------- |---------------:|--------------:|--------------:|------:|--------:|-----:|-------:|----------:|------------:|
| EncinaRefitCall             |             NA |            NA |            NA |     ? |       ? |    ? |     NA |        NA |           ? |
| EncinaRefitCall_Sequential5 |       7.641 μs |      5.780 μs |     0.3168 μs | 0.000 |    0.00 |    1 | 0.5341 |   8.79 KB |        0.92 |
| EncinaRefitCall_Batch10     |      17.881 μs |     14.718 μs |     0.8067 μs | 0.001 |    0.00 |    2 | 1.0986 |  18.34 KB |        1.92 |
| DirectRefitCall_Baseline    |  26,045.909 μs |    749.943 μs |    41.1069 μs | 1.000 |    0.00 |    3 |      - |   9.54 KB |        1.00 |
| DirectRefitCall_Batch10     |  29,327.696 μs | 10,685.150 μs |   585.6891 μs | 1.126 |    0.02 |    3 |      - |   93.6 KB |        9.82 |
| DirectRefitCall_Sequential5 | 143,855.759 μs | 71,123.640 μs | 3,898.5267 μs | 5.523 |    0.13 |    4 |      - |  46.89 KB |        4.92 |

Benchmarks with issues:
  RestApiRequestHandlerBenchmarks.EncinaRefitCall: ShortRun(IterationCount=3, LaunchCount=1, WarmupCount=3)
