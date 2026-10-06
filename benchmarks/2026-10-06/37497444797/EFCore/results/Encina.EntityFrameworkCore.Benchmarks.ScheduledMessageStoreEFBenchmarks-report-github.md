```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

```
| Method                    | Mean      | Error       | StdDev    | Ratio | RatioSD | Allocated | Alloc Ratio |
|-------------------------- |----------:|------------:|----------:|------:|--------:|----------:|------------:|
| GetDueMessagesAsync       | 852.33 μs | 1,093.03 μs | 59.913 μs |  1.00 |    0.08 | 349.05 KB |       1.000 |
| AddAsync                  |  31.73 μs |    95.17 μs |  5.217 μs |  0.04 |    0.01 |   3.42 KB |       0.010 |
| RescheduleRecurringAsync  | 130.72 μs |   310.63 μs | 17.027 μs |  0.15 |    0.02 |  13.13 KB |       0.038 |
| MarkAsFailedAsync         | 138.01 μs |   551.68 μs | 30.239 μs |  0.16 |    0.03 |  13.03 KB |       0.037 |
| GetRecurringMessagesAsync | 514.51 μs |   615.05 μs | 33.713 μs |  0.61 |    0.05 | 166.63 KB |       0.477 |
