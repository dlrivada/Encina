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
| EncinaRefitCall_Sequential5 |      4.251 μs |      0.3119 μs |     0.0171 μs | 0.000 |    0.00 |    1 | 0.2975 |   4.92 KB |        0.50 |
| EncinaRefitCall_Batch10     |     13.599 μs |     59.7196 μs |     3.2734 μs | 0.001 |    0.00 |    2 | 0.6409 |  10.61 KB |        1.07 |
| DirectRefitCall_Baseline    | 15,584.351 μs | 35,841.6045 μs | 1,964.5993 μs | 1.011 |    0.16 |    3 |      - |   9.93 KB |        1.00 |
| DirectRefitCall_Batch10     | 65,142.471 μs | 30,965.5078 μs | 1,697.3240 μs | 4.227 |    0.50 |    4 |      - |  98.65 KB |        9.94 |
| DirectRefitCall_Sequential5 | 77,809.895 μs | 63,032.6083 μs | 3,455.0300 μs | 5.049 |    0.61 |    4 |      - |  48.27 KB |        4.86 |

Benchmarks with issues:
  RestApiRequestHandlerBenchmarks.EncinaRefitCall: ShortRun(IterationCount=3, LaunchCount=1, WarmupCount=3)
