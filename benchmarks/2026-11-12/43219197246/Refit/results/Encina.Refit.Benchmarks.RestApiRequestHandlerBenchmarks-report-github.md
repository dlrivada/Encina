```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 11.0.100
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                      | Mean          | Error         | StdDev       | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|---------------------------- |--------------:|--------------:|-------------:|------:|--------:|-----:|-------:|----------:|------------:|
| EncinaRefitCall             |            NA |            NA |           NA |     ? |       ? |    ? |     NA |        NA |           ? |
| EncinaRefitCall_Sequential5 |      12.06 μs |      1.182 μs |     0.065 μs | 0.000 |    0.00 |    1 | 0.5341 |   8.83 KB |        0.93 |
| EncinaRefitCall_Batch10     |      31.78 μs |     41.750 μs |     2.288 μs | 0.001 |    0.00 |    2 | 1.0986 |  18.42 KB |        1.95 |
| DirectRefitCall_Baseline    |  25,280.82 μs |  3,271.029 μs |   179.296 μs | 1.000 |    0.01 |    3 |      - |   9.47 KB |        1.00 |
| DirectRefitCall_Batch10     |  32,984.99 μs | 19,313.226 μs | 1,058.623 μs | 1.305 |    0.04 |    4 |      - |  93.68 KB |        9.90 |
| DirectRefitCall_Sequential5 | 127,696.26 μs | 23,251.951 μs | 1,274.518 μs | 5.051 |    0.05 |    5 |      - |  46.11 KB |        4.87 |

Benchmarks with issues:
  RestApiRequestHandlerBenchmarks.EncinaRefitCall: ShortRun(IterationCount=3, LaunchCount=1, WarmupCount=3)
