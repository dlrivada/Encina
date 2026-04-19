```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763 2.62GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.202
  [Host]    : .NET 10.0.6 (10.0.6, 10.0.626.17701), X64 RyuJIT x86-64-v3
  MediumRun : .NET 10.0.6 (10.0.6, 10.0.626.17701), X64 RyuJIT x86-64-v3

Job=MediumRun  IterationCount=15  LaunchCount=2  
WarmupCount=10  

```
| Method                      | Mean          | Error         | StdDev        | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|---------------------------- |--------------:|--------------:|--------------:|------:|--------:|-----:|-------:|----------:|------------:|
| EncinaRefitCall             |            NA |            NA |            NA |     ? |       ? |    ? |     NA |        NA |           ? |
| EncinaRefitCall_Sequential5 |      4.429 μs |     0.1202 μs |     0.1645 μs | 0.001 |    0.00 |    1 | 0.2975 |   4.92 KB |        0.50 |
| EncinaRefitCall_Batch10     |     13.014 μs |     1.5231 μs |     2.2325 μs | 0.002 |    0.00 |    2 | 0.6409 |  10.61 KB |        1.07 |
| DirectRefitCall_Baseline    |  7,636.422 μs |   283.5086 μs |   415.5633 μs | 1.003 |    0.08 |    3 |      - |   9.87 KB |        1.00 |
| DirectRefitCall_Batch10     | 11,846.997 μs |   483.7638 μs |   693.7996 μs | 1.556 |    0.12 |    4 |      - |  97.59 KB |        9.89 |
| DirectRefitCall_Sequential5 | 37,487.527 μs | 3,003.7069 μs | 4,495.8074 μs | 4.923 |    0.64 |    5 |      - |  47.72 KB |        4.83 |

Benchmarks with issues:
  RestApiRequestHandlerBenchmarks.EncinaRefitCall: MediumRun(IterationCount=15, LaunchCount=2, WarmupCount=10)
