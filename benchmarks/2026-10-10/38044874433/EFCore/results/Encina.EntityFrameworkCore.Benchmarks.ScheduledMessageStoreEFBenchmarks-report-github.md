```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
Intel Xeon 6973P-C 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

```
| Method                    | Mean      | Error     | StdDev    | Ratio | RatioSD | Allocated | Alloc Ratio |
|-------------------------- |----------:|----------:|----------:|------:|--------:|----------:|------------:|
| GetDueMessagesAsync       | 993.44 μs | 222.00 μs | 12.168 μs |  1.00 |    0.02 | 349.05 KB |       1.000 |
| AddAsync                  |  75.39 μs |  12.38 μs |  0.679 μs |  0.08 |    0.00 |   3.42 KB |       0.010 |
| RescheduleRecurringAsync  | 192.46 μs |  46.93 μs |  2.572 μs |  0.19 |    0.00 |  13.13 KB |       0.038 |
| MarkAsFailedAsync         | 198.38 μs | 285.55 μs | 15.652 μs |  0.20 |    0.01 |  13.03 KB |       0.037 |
| GetRecurringMessagesAsync | 611.29 μs | 757.05 μs | 41.497 μs |  0.62 |    0.04 | 166.87 KB |       0.478 |
