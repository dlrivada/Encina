```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

```
| Method                    | Mean      | Error     | StdDev    | Ratio | RatioSD | Allocated | Alloc Ratio |
|-------------------------- |----------:|----------:|----------:|------:|--------:|----------:|------------:|
| GetDueMessagesAsync       | 857.75 μs | 295.99 μs | 16.224 μs |  1.00 |    0.02 | 349.16 KB |       1.000 |
| AddAsync                  |  34.47 μs | 129.99 μs |  7.125 μs |  0.04 |    0.01 |   3.42 KB |       0.010 |
| RescheduleRecurringAsync  | 117.25 μs | 225.17 μs | 12.343 μs |  0.14 |    0.01 |  13.13 KB |       0.038 |
| MarkAsFailedAsync         | 113.37 μs | 391.08 μs | 21.437 μs |  0.13 |    0.02 |  13.03 KB |       0.037 |
| GetRecurringMessagesAsync | 468.29 μs | 526.73 μs | 28.872 μs |  0.55 |    0.03 | 166.55 KB |       0.477 |
