```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 4.30GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

```
| Method                    | Mean      | Error     | StdDev    | Ratio | RatioSD | Allocated | Alloc Ratio |
|-------------------------- |----------:|----------:|----------:|------:|--------:|----------:|------------:|
| GetDueMessagesAsync       | 804.48 μs | 209.20 μs | 11.467 μs |  1.00 |    0.02 | 349.39 KB |       1.000 |
| AddAsync                  |  37.89 μs |  65.56 μs |  3.594 μs |  0.05 |    0.00 |   3.42 KB |       0.010 |
| RescheduleRecurringAsync  | 117.08 μs | 244.64 μs | 13.410 μs |  0.15 |    0.01 |  13.13 KB |       0.038 |
| MarkAsFailedAsync         | 103.28 μs | 211.30 μs | 11.582 μs |  0.13 |    0.01 |  13.03 KB |       0.037 |
| GetRecurringMessagesAsync | 483.74 μs | 289.66 μs | 15.877 μs |  0.60 |    0.02 | 166.55 KB |       0.477 |
