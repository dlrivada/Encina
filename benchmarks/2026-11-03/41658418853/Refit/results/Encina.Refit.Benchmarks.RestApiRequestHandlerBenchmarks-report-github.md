```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V74 3.69GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                      | Mean          | Error         | StdDev       | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|---------------------------- |--------------:|--------------:|-------------:|------:|--------:|-----:|-------:|----------:|------------:|
| EncinaRefitCall             |            NA |            NA |           NA |     ? |       ? |    ? |     NA |        NA |           ? |
| EncinaRefitCall_Sequential5 |      10.21 μs |      1.226 μs |     0.067 μs | 0.000 |    0.00 |    1 | 0.5341 |   8.79 KB |        0.93 |
| EncinaRefitCall_Batch10     |      25.12 μs |     52.587 μs |     2.882 μs | 0.001 |    0.00 |    2 | 1.0986 |  18.34 KB |        1.94 |
| DirectRefitCall_Baseline    |  22,792.56 μs |  6,865.665 μs |   376.330 μs | 1.000 |    0.02 |    3 |      - |   9.47 KB |        1.00 |
| DirectRefitCall_Sequential5 | 132,875.70 μs | 21,158.549 μs | 1,159.771 μs | 5.831 |    0.09 |    4 |      - |  46.63 KB |        4.93 |
| DirectRefitCall_Batch10     | 143,717.55 μs | 12,663.449 μs |   694.126 μs | 6.307 |    0.09 |    4 |      - |   94.2 KB |        9.95 |

Benchmarks with issues:
  RestApiRequestHandlerBenchmarks.EncinaRefitCall: ShortRun(IterationCount=3, LaunchCount=1, WarmupCount=3)
