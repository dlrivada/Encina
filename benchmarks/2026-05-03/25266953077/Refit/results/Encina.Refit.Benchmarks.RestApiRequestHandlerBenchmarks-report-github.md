```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763 2.72GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.203
  [Host]    : .NET 10.0.7 (10.0.7, 10.0.726.21808), X64 RyuJIT x86-64-v3
  MediumRun : .NET 10.0.7 (10.0.7, 10.0.726.21808), X64 RyuJIT x86-64-v3

Job=MediumRun  IterationCount=15  LaunchCount=2  
WarmupCount=10  

```
| Method                      | Mean          | Error         | StdDev        | Median        | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|---------------------------- |--------------:|--------------:|--------------:|--------------:|------:|--------:|-----:|-------:|----------:|------------:|
| EncinaRefitCall             |            NA |            NA |            NA |            NA |     ? |       ? |    ? |     NA |        NA |           ? |
| EncinaRefitCall_Sequential5 |      4.363 μs |     0.0569 μs |     0.0834 μs |      4.302 μs | 0.001 |    0.00 |    1 | 0.2975 |   4.92 KB |        0.50 |
| EncinaRefitCall_Batch10     |     12.264 μs |     0.6828 μs |     0.9793 μs |     11.758 μs | 0.002 |    0.00 |    2 | 0.6409 |  10.61 KB |        1.07 |
| DirectRefitCall_Baseline    |  7,354.602 μs |   595.8201 μs |   835.2566 μs |  7,145.222 μs | 1.011 |    0.15 |    3 |      - |   9.93 KB |        1.00 |
| DirectRefitCall_Batch10     | 11,964.348 μs |   787.4589 μs | 1,178.6314 μs | 11,618.393 μs | 1.645 |    0.23 |    4 |      - |  97.68 KB |        9.83 |
| DirectRefitCall_Sequential5 | 41,167.251 μs | 4,621.9276 μs | 6,774.7622 μs | 39,483.067 μs | 5.661 |    1.08 |    5 |      - |  47.97 KB |        4.83 |

Benchmarks with issues:
  RestApiRequestHandlerBenchmarks.EncinaRefitCall: MediumRun(IterationCount=15, LaunchCount=2, WarmupCount=10)
