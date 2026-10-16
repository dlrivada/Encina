```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                      | Mean          | Error         | StdDev        | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|---------------------------- |--------------:|--------------:|--------------:|------:|--------:|-----:|-------:|----------:|------------:|
| EncinaRefitCall             |            NA |            NA |            NA |     ? |       ? |    ? |     NA |        NA |           ? |
| EncinaRefitCall_Sequential5 |      9.705 μs |      1.168 μs |     0.0640 μs | 0.001 |    0.00 |    1 | 0.5341 |   8.79 KB |        0.92 |
| EncinaRefitCall_Batch10     |     22.208 μs |      3.426 μs |     0.1878 μs | 0.001 |    0.00 |    2 | 1.0986 |  18.34 KB |        1.93 |
| DirectRefitCall_Baseline    | 16,254.307 μs |  3,902.938 μs |   213.9332 μs | 1.000 |    0.02 |    3 |      - |   9.52 KB |        1.00 |
| DirectRefitCall_Batch10     | 20,370.926 μs | 21,649.696 μs | 1,186.6929 μs | 1.253 |    0.06 |    4 |      - |  93.45 KB |        9.82 |
| DirectRefitCall_Sequential5 | 77,411.635 μs | 14,346.851 μs |   786.3993 μs | 4.763 |    0.07 |    5 |      - |  45.83 KB |        4.81 |

Benchmarks with issues:
  RestApiRequestHandlerBenchmarks.EncinaRefitCall: ShortRun(IterationCount=3, LaunchCount=1, WarmupCount=3)
