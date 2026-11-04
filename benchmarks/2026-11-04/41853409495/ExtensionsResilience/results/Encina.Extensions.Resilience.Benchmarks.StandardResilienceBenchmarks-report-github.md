```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
Intel Xeon Platinum 8370C CPU 2.80GHz (Max: 2.76GHz), 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                        | Mean         | Error       | StdDev     | Ratio  | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|---------------------------------------------- |-------------:|------------:|-----------:|-------:|--------:|-----:|-------:|----------:|------------:|
| NoResilience_Baseline                         |     2.629 μs |   0.0897 μs |  0.0049 μs |   1.00 |    0.00 |    1 | 0.0725 |   1.83 KB |        1.00 |
| StandardResilience_Success                    |     6.431 μs |   0.5594 μs |  0.0307 μs |   2.45 |    0.01 |    2 | 0.0839 |   2.15 KB |        1.18 |
| StandardResilience_ConcurrentRequests         |    63.766 μs |   6.3598 μs |  0.3486 μs |  24.25 |    0.12 |    3 | 0.8545 |  21.84 KB |       11.95 |
| StandardResilience_MultipleSequentialRequests |    64.302 μs |   2.3697 μs |  0.1299 μs |  24.46 |    0.06 |    3 | 0.7324 |  20.38 KB |       11.15 |
| StandardResilience_WithRetry                  | 1,806.982 μs | 604.1146 μs | 33.1136 μs | 687.24 |   10.96 |    4 | 1.9531 |  52.52 KB |       28.73 |
