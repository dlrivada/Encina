```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
Intel Xeon 6973P-C 3.92GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

```
| Method                    | Mean        | Error     | StdDev    | Ratio | RatioSD | Allocated | Alloc Ratio |
|-------------------------- |------------:|----------:|----------:|------:|--------:|----------:|------------:|
| GetDueMessagesAsync       | 1,148.50 μs | 190.50 μs | 10.442 μs |  1.00 |    0.01 | 349.16 KB |       1.000 |
| AddAsync                  |    79.99 μs |  34.47 μs |  1.889 μs |  0.07 |    0.00 |   3.42 KB |       0.010 |
| RescheduleRecurringAsync  |   264.92 μs | 307.20 μs | 16.839 μs |  0.23 |    0.01 |  13.24 KB |       0.038 |
| MarkAsFailedAsync         |   229.31 μs | 203.90 μs | 11.177 μs |  0.20 |    0.01 |  13.03 KB |       0.037 |
| GetRecurringMessagesAsync |   667.95 μs | 371.09 μs | 20.341 μs |  0.58 |    0.02 | 166.87 KB |       0.478 |
