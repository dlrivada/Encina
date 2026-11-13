```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V45 4.25GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

```
| Method                    | Mean      | Error     | StdDev    | Ratio | RatioSD | Allocated | Alloc Ratio |
|-------------------------- |----------:|----------:|----------:|------:|--------:|----------:|------------:|
| GetDueMessagesAsync       | 853.20 μs | 741.87 μs | 40.665 μs |  1.00 |    0.06 | 349.19 KB |       1.000 |
| AddAsync                  |  35.85 μs |  67.04 μs |  3.675 μs |  0.04 |    0.00 |   3.42 KB |       0.010 |
| RescheduleRecurringAsync  | 168.32 μs | 399.54 μs | 21.900 μs |  0.20 |    0.02 |  13.13 KB |       0.038 |
| MarkAsFailedAsync         | 143.20 μs | 293.82 μs | 16.105 μs |  0.17 |    0.02 |  13.03 KB |       0.037 |
| GetRecurringMessagesAsync | 476.43 μs | 552.44 μs | 30.281 μs |  0.56 |    0.04 | 166.55 KB |       0.477 |
