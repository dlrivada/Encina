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
| EncinaRefitCall_Sequential5 |  44.81 ms |    NA |  0.13 |    0.00 |    1 |   5.64 KB |        0.48 |
| EncinaRefitCall_Batch10     |  49.73 ms |    NA |  0.14 |    0.00 |    2 |  11.39 KB |        0.97 |
| DirectRefitCall_Sequential5 | 308.21 ms |    NA |  0.88 |    0.00 |    3 |  51.19 KB |        4.38 |
| DirectRefitCall_Baseline    | 348.75 ms |    NA |  1.00 |    0.00 |    4 |   11.7 KB |        1.00 |
| DirectRefitCall_Batch10     | 463.42 ms |    NA |  1.33 |    0.00 |    5 | 101.58 KB |        8.69 |

Benchmarks with issues:
  RestApiRequestHandlerBenchmarks.EncinaRefitCall: Dry(IterationCount=1, LaunchCount=1, RunStrategy=ColdStart, UnrollFactor=1, WarmupCount=1)
