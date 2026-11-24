```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V74 3.67GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

```
| Method                    | Mean        | Error     | StdDev    | Ratio | RatioSD | Allocated | Alloc Ratio |
|-------------------------- |------------:|----------:|----------:|------:|--------:|----------:|------------:|
| GetDueMessagesAsync       | 1,087.35 μs |  96.63 μs |  5.297 μs |  1.00 |    0.01 | 349.05 KB |       1.000 |
| AddAsync                  |    59.11 μs | 122.63 μs |  6.722 μs |  0.05 |    0.01 |   3.42 KB |       0.010 |
| RescheduleRecurringAsync  |   144.51 μs |  59.03 μs |  3.236 μs |  0.13 |    0.00 |  13.13 KB |       0.038 |
| MarkAsFailedAsync         |   197.97 μs | 575.62 μs | 31.552 μs |  0.18 |    0.03 |  13.03 KB |       0.037 |
| GetRecurringMessagesAsync |   659.09 μs | 375.29 μs | 20.571 μs |  0.61 |    0.02 | 166.66 KB |       0.477 |
