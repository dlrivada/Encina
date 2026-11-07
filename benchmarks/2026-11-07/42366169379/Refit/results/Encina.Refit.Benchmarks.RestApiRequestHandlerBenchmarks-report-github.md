```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 2.63GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                      | Mean         | Error         | StdDev       | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|---------------------------- |-------------:|--------------:|-------------:|------:|--------:|-----:|-------:|----------:|------------:|
| EncinaRefitCall             |           NA |            NA |           NA |     ? |       ? |    ? |     NA |        NA |           ? |
| EncinaRefitCall_Sequential5 |     12.04 μs |      0.712 μs |     0.039 μs | 0.002 |    0.00 |    1 | 0.5341 |   8.79 KB |        0.94 |
| EncinaRefitCall_Batch10     |     32.61 μs |     28.337 μs |     1.553 μs | 0.004 |    0.00 |    2 | 1.0986 |  18.34 KB |        1.95 |
| DirectRefitCall_Baseline    |  7,826.65 μs | 15,669.491 μs |   858.898 μs | 1.008 |    0.13 |    3 |      - |    9.4 KB |        1.00 |
| DirectRefitCall_Batch10     | 11,824.36 μs | 28,378.397 μs | 1,555.516 μs | 1.522 |    0.22 |    4 |      - |  93.72 KB |        9.97 |
| DirectRefitCall_Sequential5 | 34,323.21 μs |  6,984.284 μs |   382.832 μs | 4.419 |    0.41 |    5 |      - |  45.66 KB |        4.86 |

Benchmarks with issues:
  RestApiRequestHandlerBenchmarks.EncinaRefitCall: ShortRun(IterationCount=3, LaunchCount=1, WarmupCount=3)
