```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
Intel Xeon 6975P-C 2.97GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

```
| Method                    | Mean        | Error     | StdDev    | Ratio | RatioSD | Allocated | Alloc Ratio |
|-------------------------- |------------:|----------:|----------:|------:|--------:|----------:|------------:|
| GetDueMessagesAsync       | 1,040.92 μs | 118.20 μs |  6.479 μs |  1.00 |    0.01 | 349.05 KB |       1.000 |
| AddAsync                  |    77.03 μs |  17.85 μs |  0.978 μs |  0.07 |    0.00 |   3.42 KB |       0.010 |
| RescheduleRecurringAsync  |   206.09 μs |  92.13 μs |  5.050 μs |  0.20 |    0.00 |  13.13 KB |       0.038 |
| MarkAsFailedAsync         |   202.16 μs |  94.63 μs |  5.187 μs |  0.19 |    0.00 |  13.03 KB |       0.037 |
| GetRecurringMessagesAsync |   645.58 μs | 336.79 μs | 18.461 μs |  0.62 |    0.02 | 166.55 KB |       0.477 |
