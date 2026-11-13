```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
Intel Xeon 6975P-C 3.23GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                           | Mean     | Error     | StdDev   | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|--------------------------------- |---------:|----------:|---------:|------:|--------:|-------:|----------:|------------:|
| AddAsync_Single                  | 213.5 μs |  96.86 μs |  5.31 μs |  1.00 |    0.03 | 0.2441 |   20.8 KB |        1.00 |
| GetPendingMessagesAsync_Batch100 | 588.8 μs | 367.24 μs | 20.13 μs |  2.76 |    0.10 | 0.9766 |  93.64 KB |        4.50 |
