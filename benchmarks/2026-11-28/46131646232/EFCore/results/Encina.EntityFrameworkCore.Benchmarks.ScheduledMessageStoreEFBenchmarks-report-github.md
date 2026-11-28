```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
Intel Xeon 6975P-C 2.70GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

```
| Method                    | Mean        | Error     | StdDev    | Ratio | RatioSD | Allocated | Alloc Ratio |
|-------------------------- |------------:|----------:|----------:|------:|--------:|----------:|------------:|
| GetDueMessagesAsync       | 1,069.42 μs | 322.47 μs | 17.675 μs |  1.00 |    0.02 | 349.13 KB |       1.000 |
| AddAsync                  |    81.49 μs |  15.08 μs |  0.827 μs |  0.08 |    0.00 |   3.42 KB |       0.010 |
| RescheduleRecurringAsync  |   213.19 μs | 113.17 μs |  6.203 μs |  0.20 |    0.01 |  13.25 KB |       0.038 |
| MarkAsFailedAsync         |   211.40 μs | 219.68 μs | 12.041 μs |  0.20 |    0.01 |  13.03 KB |       0.037 |
| GetRecurringMessagesAsync |   645.94 μs | 656.91 μs | 36.008 μs |  0.60 |    0.03 | 166.67 KB |       0.477 |
