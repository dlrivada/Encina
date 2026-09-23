```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.74GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                      | Mean          | Error          | StdDev        | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|---------------------------- |--------------:|---------------:|--------------:|------:|--------:|-----:|-------:|----------:|------------:|
| EncinaRefitCall             |            NA |             NA |            NA |     ? |       ? |    ? |     NA |        NA |           ? |
| EncinaRefitCall_Sequential5 |      4.208 μs |      0.5166 μs |     0.0283 μs | 0.000 |    0.00 |    1 | 0.2975 |   4.92 KB |        0.50 |
| EncinaRefitCall_Batch10     |     16.987 μs |     38.7707 μs |     2.1252 μs | 0.001 |    0.00 |    2 | 0.6409 |  10.61 KB |        1.08 |
| DirectRefitCall_Baseline    | 11,719.753 μs | 22,323.6914 μs | 1,223.6369 μs | 1.007 |    0.13 |    3 |      - |   9.85 KB |        1.00 |
| DirectRefitCall_Batch10     | 27,744.686 μs |  5,918.8587 μs |   324.4326 μs | 2.385 |    0.22 |    4 |      - |  97.69 KB |        9.92 |
| DirectRefitCall_Sequential5 | 51,452.076 μs | 32,342.3426 μs | 1,772.7929 μs | 4.422 |    0.42 |    5 |      - |  48.14 KB |        4.89 |

Benchmarks with issues:
  RestApiRequestHandlerBenchmarks.EncinaRefitCall: ShortRun(IterationCount=3, LaunchCount=1, WarmupCount=3)
