```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                      | Mean          | Error          | StdDev        | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|---------------------------- |--------------:|---------------:|--------------:|------:|--------:|-----:|-------:|----------:|------------:|
| EncinaRefitCall             |            NA |             NA |            NA |     ? |       ? |    ? |     NA |        NA |           ? |
| EncinaRefitCall_Sequential5 |      4.343 μs |      0.6726 μs |     0.0369 μs | 0.000 |    0.00 |    1 | 0.2975 |   4.92 KB |        0.50 |
| EncinaRefitCall_Batch10     |     13.141 μs |     52.0919 μs |     2.8553 μs | 0.001 |    0.00 |    2 | 0.6409 |  10.61 KB |        1.07 |
| DirectRefitCall_Baseline    | 21,673.009 μs |  4,605.7935 μs |   252.4591 μs | 1.000 |    0.01 |    3 |      - |   9.93 KB |        1.00 |
| DirectRefitCall_Batch10     | 31,880.257 μs |  4,083.0585 μs |   223.8062 μs | 1.471 |    0.02 |    4 |      - |  97.86 KB |        9.86 |
| DirectRefitCall_Sequential5 | 65,922.825 μs | 26,780.0515 μs | 1,467.9050 μs | 3.042 |    0.07 |    5 |      - |  47.98 KB |        4.83 |

Benchmarks with issues:
  RestApiRequestHandlerBenchmarks.EncinaRefitCall: ShortRun(IterationCount=3, LaunchCount=1, WarmupCount=3)
