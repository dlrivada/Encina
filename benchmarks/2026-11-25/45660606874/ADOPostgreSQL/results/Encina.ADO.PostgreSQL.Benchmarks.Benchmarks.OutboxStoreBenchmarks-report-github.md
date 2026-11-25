```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V45 4.36GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                           | Mean     | Error    | StdDev  | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|--------------------------------- |---------:|---------:|--------:|------:|--------:|-------:|----------:|------------:|
| AddAsync_Single                  | 115.6 μs | 34.74 μs | 1.90 μs |  1.00 |    0.02 | 0.2441 |   5.34 KB |        1.00 |
| GetPendingMessagesAsync_Batch100 | 256.2 μs | 90.17 μs | 4.94 μs |  2.22 |    0.05 | 2.4414 |  40.23 KB |        7.53 |
