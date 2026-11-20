```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
Intel Xeon 6975P-C 3.29GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

```
| Method                    | Mean        | Error     | StdDev    | Ratio | RatioSD | Allocated | Alloc Ratio |
|-------------------------- |------------:|----------:|----------:|------:|--------:|----------:|------------:|
| GetDueMessagesAsync       | 1,063.04 μs | 189.12 μs | 10.366 μs |  1.00 |    0.01 | 349.05 KB |       1.000 |
| AddAsync                  |    76.88 μs |  39.70 μs |  2.176 μs |  0.07 |    0.00 |   3.42 KB |       0.010 |
| RescheduleRecurringAsync  |   217.92 μs | 213.96 μs | 11.728 μs |  0.21 |    0.01 |  13.13 KB |       0.038 |
| MarkAsFailedAsync         |   208.26 μs |  28.51 μs |  1.563 μs |  0.20 |    0.00 |  13.03 KB |       0.037 |
| GetRecurringMessagesAsync |   631.66 μs | 636.76 μs | 34.903 μs |  0.59 |    0.03 | 166.67 KB |       0.478 |
