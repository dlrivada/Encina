```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
Intel Xeon Platinum 8370C CPU 2.80GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host] : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v4
  Dry    : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v4

Job=Dry  IterationCount=1  LaunchCount=1  
RunStrategy=ColdStart  UnrollFactor=1  WarmupCount=1  

```
| Method                      | Mean        | Error | Ratio | RatioSD | Rank | Allocated | Alloc Ratio |
|---------------------------- |------------:|------:|------:|--------:|-----:|----------:|------------:|
| EncinaRefitCall             |          NA |    NA |     ? |       ? |    ? |        NA |           ? |
| EncinaRefitCall_Sequential5 |    42.90 ms |    NA |  0.03 |    0.00 |    1 |   5.64 KB |        0.48 |
| EncinaRefitCall_Batch10     |    49.73 ms |    NA |  0.04 |    0.00 |    2 |  11.39 KB |        0.97 |
| DirectRefitCall_Batch10     |   678.75 ms |    NA |  0.55 |    0.00 |    3 | 102.15 KB |        8.72 |
| DirectRefitCall_Baseline    | 1,242.64 ms |    NA |  1.00 |    0.00 |    4 |  11.71 KB |        1.00 |
| DirectRefitCall_Sequential5 | 2,761.99 ms |    NA |  2.22 |    0.00 |    5 |  51.37 KB |        4.39 |

Benchmarks with issues:
  RestApiRequestHandlerBenchmarks.EncinaRefitCall: Dry(IterationCount=1, LaunchCount=1, RunStrategy=ColdStart, UnrollFactor=1, WarmupCount=1)
