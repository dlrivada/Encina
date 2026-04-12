```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]    : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  MediumRun : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3

Job=MediumRun  IterationCount=15  LaunchCount=2  
WarmupCount=10  

```
| Method                      | Mean          | Error         | StdDev        | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|---------------------------- |--------------:|--------------:|--------------:|------:|--------:|-----:|-------:|----------:|------------:|
| EncinaRefitCall             |            NA |            NA |            NA |     ? |       ? |    ? |     NA |        NA |           ? |
| EncinaRefitCall_Sequential5 |      4.186 μs |     0.0370 μs |     0.0554 μs | 0.000 |    0.00 |    1 | 0.2975 |   4.92 KB |        0.50 |
| EncinaRefitCall_Batch10     |     13.179 μs |     1.5071 μs |     2.2557 μs | 0.001 |    0.00 |    2 | 0.6409 |  10.61 KB |        1.07 |
| DirectRefitCall_Baseline    | 10,946.667 μs |   402.3821 μs |   602.2666 μs | 1.003 |    0.08 |    3 |      - |   9.88 KB |        1.00 |
| DirectRefitCall_Batch10     | 13,815.116 μs |   648.5115 μs |   887.6900 μs | 1.266 |    0.10 |    4 |      - |  97.79 KB |        9.90 |
| DirectRefitCall_Sequential5 | 53,852.200 μs | 1,778.3874 μs | 2,606.7374 μs | 4.933 |    0.35 |    5 |      - |   48.1 KB |        4.87 |

Benchmarks with issues:
  RestApiRequestHandlerBenchmarks.EncinaRefitCall: MediumRun(IterationCount=15, LaunchCount=2, WarmupCount=10)
