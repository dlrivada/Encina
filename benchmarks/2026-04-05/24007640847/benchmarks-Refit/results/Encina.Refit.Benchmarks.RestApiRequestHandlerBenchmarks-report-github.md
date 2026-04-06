```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host] : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  Dry    : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3

Job=Dry  IterationCount=1  LaunchCount=1  
RunStrategy=ColdStart  UnrollFactor=1  WarmupCount=1  

```
| Method                      | Mean      | Error | Ratio | RatioSD | Rank | Allocated | Alloc Ratio |
|---------------------------- |----------:|------:|------:|--------:|-----:|----------:|------------:|
| EncinaRefitCall             |        NA |    NA |     ? |       ? |    ? |        NA |           ? |
| EncinaRefitCall_Sequential5 |  46.39 ms |    NA |  0.12 |    0.00 |    1 |   5.64 KB |        0.48 |
| EncinaRefitCall_Batch10     |  50.30 ms |    NA |  0.13 |    0.00 |    2 |  11.39 KB |        0.98 |
| DirectRefitCall_Sequential5 | 325.70 ms |    NA |  0.87 |    0.00 |    3 |  51.18 KB |        4.39 |
| DirectRefitCall_Batch10     | 349.03 ms |    NA |  0.93 |    0.00 |    4 |  101.1 KB |        8.67 |
| DirectRefitCall_Baseline    | 374.18 ms |    NA |  1.00 |    0.00 |    5 |  11.66 KB |        1.00 |

Benchmarks with issues:
  RestApiRequestHandlerBenchmarks.EncinaRefitCall: Dry(IterationCount=1, LaunchCount=1, RunStrategy=ColdStart, UnrollFactor=1, WarmupCount=1)
