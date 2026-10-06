```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
Intel Xeon 6973P-C 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

```
| Method                    | Mean        | Error      | StdDev    | Ratio | Allocated | Alloc Ratio |
|-------------------------- |------------:|-----------:|----------:|------:|----------:|------------:|
| GetDueMessagesAsync       | 1,152.53 μs | 252.516 μs | 13.841 μs |  1.00 | 349.16 KB |       1.000 |
| AddAsync                  |    95.14 μs | 187.767 μs | 10.292 μs |  0.08 |   3.42 KB |       0.010 |
| RescheduleRecurringAsync  |   212.14 μs | 158.299 μs |  8.677 μs |  0.18 |  13.13 KB |       0.038 |
| MarkAsFailedAsync         |   189.62 μs |   6.231 μs |  0.342 μs |  0.16 |  13.03 KB |       0.037 |
| GetRecurringMessagesAsync |   575.06 μs | 181.538 μs |  9.951 μs |  0.50 | 166.55 KB |       0.477 |
