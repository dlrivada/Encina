```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V45 4.20GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]    : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  MediumRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=MediumRun  InvocationCount=1  IterationCount=15  
LaunchCount=2  UnrollFactor=1  WarmupCount=10  

```
| Method                    | Mean      | Error     | StdDev    | Ratio | RatioSD | Allocated | Alloc Ratio |
|-------------------------- |----------:|----------:|----------:|------:|--------:|----------:|------------:|
| GetDueMessagesAsync       | 827.70 μs | 11.126 μs | 16.308 μs |  1.00 |    0.03 | 349.05 KB |       1.000 |
| AddAsync                  |  31.13 μs |  2.525 μs |  3.456 μs |  0.04 |    0.00 |   3.42 KB |       0.010 |
| RescheduleRecurringAsync  | 111.85 μs |  5.650 μs |  8.104 μs |  0.14 |    0.01 |  13.13 KB |       0.038 |
| MarkAsFailedAsync         | 150.67 μs | 15.221 μs | 22.782 μs |  0.18 |    0.03 |  13.03 KB |       0.037 |
| GetRecurringMessagesAsync | 483.27 μs | 12.526 μs | 17.965 μs |  0.58 |    0.02 | 166.55 KB |       0.477 |
