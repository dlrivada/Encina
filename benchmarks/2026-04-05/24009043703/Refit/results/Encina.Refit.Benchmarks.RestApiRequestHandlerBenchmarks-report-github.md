```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
Intel Xeon Platinum 8370C CPU 2.80GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host] : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v4
  Dry    : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v4

Job=Dry  IterationCount=1  LaunchCount=1  
RunStrategy=ColdStart  UnrollFactor=1  WarmupCount=1  

```
| Method                      | Mean      | Error | Ratio | RatioSD | Rank | Allocated | Alloc Ratio |
|---------------------------- |----------:|------:|------:|--------:|-----:|----------:|------------:|
| EncinaRefitCall             |        NA |    NA |     ? |       ? |    ? |        NA |           ? |
| EncinaRefitCall_Sequential5 |  43.73 ms |    NA |  0.12 |    0.00 |    1 |   5.64 KB |        0.48 |
| EncinaRefitCall_Batch10     |  48.00 ms |    NA |  0.14 |    0.00 |    2 |  11.39 KB |        0.97 |
| DirectRefitCall_Sequential5 | 348.25 ms |    NA |  0.99 |    0.00 |    3 |  51.19 KB |        4.38 |
| DirectRefitCall_Baseline    | 353.30 ms |    NA |  1.00 |    0.00 |    4 |   11.7 KB |        1.00 |
| DirectRefitCall_Batch10     | 481.94 ms |    NA |  1.36 |    0.00 |    5 | 100.91 KB |        8.63 |

Benchmarks with issues:
  RestApiRequestHandlerBenchmarks.EncinaRefitCall: Dry(IterationCount=1, LaunchCount=1, RunStrategy=ColdStart, UnrollFactor=1, WarmupCount=1)
