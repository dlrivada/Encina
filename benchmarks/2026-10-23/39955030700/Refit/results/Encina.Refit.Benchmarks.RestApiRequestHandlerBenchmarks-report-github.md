```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
INTEL XEON PLATINUM 8573C 2.30GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                      | Mean         | Error         | StdDev       | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|---------------------------- |-------------:|--------------:|-------------:|------:|--------:|-----:|-------:|----------:|------------:|
| EncinaRefitCall             |           NA |            NA |           NA |     ? |       ? |    ? |     NA |        NA |           ? |
| EncinaRefitCall_Sequential5 |     10.36 μs |      2.574 μs |     0.141 μs | 0.001 |    0.00 |    1 | 0.1068 |   8.79 KB |        0.93 |
| EncinaRefitCall_Batch10     |     32.11 μs |     41.130 μs |     2.254 μs | 0.002 |    0.00 |    2 | 0.1831 |  18.34 KB |        1.94 |
| DirectRefitCall_Baseline    | 17,405.87 μs |  9,562.275 μs |   524.141 μs | 1.001 |    0.04 |    3 |      - |   9.47 KB |        1.00 |
| DirectRefitCall_Batch10     | 25,786.07 μs | 26,236.302 μs | 1,438.100 μs | 1.482 |    0.08 |    4 |      - |  93.52 KB |        9.88 |
| DirectRefitCall_Sequential5 | 90,167.94 μs | 39,763.663 μs | 2,179.581 μs | 5.184 |    0.18 |    5 |      - |  45.88 KB |        4.85 |

Benchmarks with issues:
  RestApiRequestHandlerBenchmarks.EncinaRefitCall: ShortRun(IterationCount=3, LaunchCount=1, WarmupCount=3)
