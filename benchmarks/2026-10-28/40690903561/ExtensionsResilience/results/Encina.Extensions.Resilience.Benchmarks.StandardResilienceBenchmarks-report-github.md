```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 4.12GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                        | Mean         | Error       | StdDev     | Ratio  | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|---------------------------------------------- |-------------:|------------:|-----------:|-------:|--------:|-----:|-------:|----------:|------------:|
| NoResilience_Baseline                         |     1.976 μs |   0.2354 μs |  0.0129 μs |   1.00 |    0.01 |    1 | 0.1106 |   1.83 KB |        1.00 |
| StandardResilience_Success                    |     3.999 μs |   2.0421 μs |  0.1119 μs |   2.02 |    0.05 |    2 | 0.1297 |   2.15 KB |        1.18 |
| StandardResilience_MultipleSequentialRequests |    39.443 μs |   4.3207 μs |  0.2368 μs |  19.96 |    0.15 |    3 | 1.2207 |  20.38 KB |       11.15 |
| StandardResilience_ConcurrentRequests         |    41.294 μs |  10.7542 μs |  0.5895 μs |  20.90 |    0.28 |    3 | 1.2817 |  21.84 KB |       11.95 |
| StandardResilience_WithRetry                  | 1,788.733 μs | 986.5004 μs | 54.0734 μs | 905.31 |   24.24 |    4 | 1.9531 |  52.56 KB |       28.75 |
