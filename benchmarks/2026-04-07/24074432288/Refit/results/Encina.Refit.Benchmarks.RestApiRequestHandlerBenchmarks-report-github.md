```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
Intel Xeon Platinum 8370C CPU 2.80GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]    : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v4
  MediumRun : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v4

Job=MediumRun  IterationCount=15  LaunchCount=2  
WarmupCount=10  

```
| Method                      | Mean           | Error         | StdDev        | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|---------------------------- |---------------:|--------------:|--------------:|------:|--------:|-----:|-------:|----------:|------------:|
| EncinaRefitCall             |             NA |            NA |            NA |     ? |       ? |    ? |     NA |        NA |           ? |
| EncinaRefitCall_Sequential5 |       4.267 μs |     0.0363 μs |     0.0532 μs | 0.000 |    0.00 |    1 | 0.1984 |   4.92 KB |        0.49 |
| EncinaRefitCall_Batch10     |      11.794 μs |     0.4634 μs |     0.6025 μs | 0.000 |    0.00 |    2 | 0.4272 |  10.61 KB |        1.06 |
| DirectRefitCall_Baseline    |  23,811.066 μs |   218.6136 μs |   299.2408 μs | 1.000 |    0.02 |    3 |      - |   9.98 KB |        1.00 |
| DirectRefitCall_Batch10     |  26,927.874 μs | 1,284.4551 μs | 1,800.6269 μs | 1.131 |    0.08 |    4 |      - |  97.55 KB |        9.77 |
| DirectRefitCall_Sequential5 | 123,778.739 μs | 3,298.0991 μs | 4,834.3113 μs | 5.199 |    0.21 |    5 |      - |   48.1 KB |        4.82 |

Benchmarks with issues:
  RestApiRequestHandlerBenchmarks.EncinaRefitCall: MediumRun(IterationCount=15, LaunchCount=2, WarmupCount=10)
