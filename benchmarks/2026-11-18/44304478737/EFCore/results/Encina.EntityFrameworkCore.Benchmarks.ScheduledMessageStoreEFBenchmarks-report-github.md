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
| GetDueMessagesAsync       | 1,077.87 μs | 154.56 μs |  8.472 μs |  1.00 |    0.01 | 349.05 KB |       1.000 |
| AddAsync                  |    77.08 μs |  28.05 μs |  1.538 μs |  0.07 |    0.00 |   3.42 KB |       0.010 |
| RescheduleRecurringAsync  |   200.21 μs | 213.11 μs | 11.681 μs |  0.19 |    0.01 |  13.13 KB |       0.038 |
| MarkAsFailedAsync         |   210.04 μs | 300.58 μs | 16.476 μs |  0.19 |    0.01 |  13.03 KB |       0.037 |
| GetRecurringMessagesAsync |   633.49 μs | 377.15 μs | 20.673 μs |  0.59 |    0.02 |  166.7 KB |       0.478 |
