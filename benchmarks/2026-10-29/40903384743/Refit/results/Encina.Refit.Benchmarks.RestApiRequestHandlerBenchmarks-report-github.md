```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.72GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                      | Mean          | Error         | StdDev       | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|---------------------------- |--------------:|--------------:|-------------:|------:|--------:|-----:|-------:|----------:|------------:|
| EncinaRefitCall             |            NA |            NA |           NA |     ? |       ? |    ? |     NA |        NA |           ? |
| EncinaRefitCall_Sequential5 |      12.44 μs |      0.982 μs |     0.054 μs | 0.001 |    0.00 |    1 | 0.5341 |   8.79 KB |        0.91 |
| EncinaRefitCall_Batch10     |      34.21 μs |    136.260 μs |     7.469 μs | 0.002 |    0.00 |    2 | 1.0986 |  18.34 KB |        1.91 |
| DirectRefitCall_Baseline    |  19,501.67 μs | 29,002.254 μs | 1,589.711 μs | 1.004 |    0.10 |    3 |      - |   9.63 KB |        1.00 |
| DirectRefitCall_Batch10     |  25,828.89 μs |  8,255.374 μs |   452.505 μs | 1.330 |    0.10 |    4 |      - |  93.69 KB |        9.73 |
| DirectRefitCall_Sequential5 | 116,691.52 μs | 43,713.874 μs | 2,396.105 μs | 6.011 |    0.44 |    5 |      - |  45.96 KB |        4.78 |

Benchmarks with issues:
  RestApiRequestHandlerBenchmarks.EncinaRefitCall: ShortRun(IterationCount=3, LaunchCount=1, WarmupCount=3)
