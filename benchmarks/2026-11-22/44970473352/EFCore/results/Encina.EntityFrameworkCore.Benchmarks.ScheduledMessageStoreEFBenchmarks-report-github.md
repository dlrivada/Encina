```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
Intel Xeon 6973P-C 4.09GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]    : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  MediumRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=MediumRun  InvocationCount=1  IterationCount=15  
LaunchCount=2  UnrollFactor=1  WarmupCount=10  

```
| Method                    | Mean      | Error     | StdDev    | Ratio | RatioSD | Allocated | Alloc Ratio |
|-------------------------- |----------:|----------:|----------:|------:|--------:|----------:|------------:|
| GetDueMessagesAsync       | 999.06 μs |  4.566 μs |  6.549 μs |  1.00 |    0.01 | 349.05 KB |       1.000 |
| AddAsync                  |  77.34 μs |  5.674 μs |  8.317 μs |  0.08 |    0.01 |   3.42 KB |       0.010 |
| RescheduleRecurringAsync  | 204.36 μs | 15.453 μs | 21.663 μs |  0.20 |    0.02 |  13.13 KB |       0.038 |
| MarkAsFailedAsync         | 191.17 μs |  3.301 μs |  4.518 μs |  0.19 |    0.00 |  13.03 KB |       0.037 |
| GetRecurringMessagesAsync | 585.88 μs |  9.882 μs | 14.791 μs |  0.59 |    0.02 | 166.55 KB |       0.477 |
