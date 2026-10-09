```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                      | Mean           | Error          | StdDev        | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|---------------------------- |---------------:|---------------:|--------------:|------:|--------:|-----:|-------:|----------:|------------:|
| EncinaRefitCall             |             NA |             NA |            NA |     ? |       ? |    ? |     NA |        NA |           ? |
| EncinaRefitCall_Sequential5 |       7.496 μs |       2.958 μs |     0.1621 μs | 0.000 |    0.00 |    1 | 0.5341 |   8.79 KB |        0.91 |
| EncinaRefitCall_Batch10     |      18.907 μs |      48.864 μs |     2.6784 μs | 0.001 |    0.00 |    2 | 1.0986 |  18.34 KB |        1.91 |
| DirectRefitCall_Baseline    |  37,621.154 μs |   7,277.390 μs |   398.8983 μs | 1.000 |    0.01 |    3 |      - |   9.62 KB |        1.00 |
| DirectRefitCall_Batch10     |  41,287.285 μs |  11,415.080 μs |   625.6990 μs | 1.098 |    0.02 |    3 |      - |   93.6 KB |        9.73 |
| DirectRefitCall_Sequential5 | 182,829.680 μs | 139,086.042 μs | 7,623.7753 μs | 4.860 |    0.18 |    4 |      - |  46.36 KB |        4.82 |

Benchmarks with issues:
  RestApiRequestHandlerBenchmarks.EncinaRefitCall: ShortRun(IterationCount=3, LaunchCount=1, WarmupCount=3)
