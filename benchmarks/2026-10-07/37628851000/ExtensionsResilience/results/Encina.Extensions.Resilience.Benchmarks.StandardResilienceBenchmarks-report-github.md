```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                        | Mean         | Error       | StdDev     | Ratio  | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|---------------------------------------------- |-------------:|------------:|-----------:|-------:|--------:|-----:|-------:|----------:|------------:|
| NoResilience_Baseline                         |     3.045 μs |   0.1661 μs |  0.0091 μs |   1.00 |    0.00 |    1 | 0.1144 |   1.93 KB |        1.00 |
| StandardResilience_Success                    |     6.610 μs |   0.2437 μs |  0.0134 μs |   2.17 |    0.01 |    2 | 0.1373 |   2.25 KB |        1.17 |
| StandardResilience_MultipleSequentialRequests |    65.843 μs |   3.1465 μs |  0.1725 μs |  21.62 |    0.07 |    3 | 1.2207 |   21.4 KB |       11.09 |
| StandardResilience_ConcurrentRequests         |    69.034 μs |   1.6773 μs |  0.0919 μs |  22.67 |    0.06 |    3 | 1.3428 |  22.86 KB |       11.85 |
| StandardResilience_WithRetry                  | 2,004.138 μs | 924.6264 μs | 50.6819 μs | 658.11 |   14.51 |    4 | 1.9531 |  52.98 KB |       27.46 |
