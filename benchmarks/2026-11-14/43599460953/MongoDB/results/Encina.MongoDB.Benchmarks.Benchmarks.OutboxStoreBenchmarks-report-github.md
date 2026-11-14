```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
Intel Xeon 6975P-C 3.27GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                           | Mean     | Error     | StdDev  | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|--------------------------------- |---------:|----------:|--------:|------:|--------:|-------:|----------:|------------:|
| AddAsync_Single                  | 221.9 μs |  62.44 μs | 3.42 μs |  1.00 |    0.02 | 0.2441 |   20.8 KB |        1.00 |
| GetPendingMessagesAsync_Batch100 | 630.0 μs | 134.31 μs | 7.36 μs |  2.84 |    0.05 | 0.9766 |  93.64 KB |        4.50 |
